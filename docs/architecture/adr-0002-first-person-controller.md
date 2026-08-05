# ADR-0002: First-Person Controller — Movement, Camera, and Reference-Counted Movement Lock

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Physics / Input / Core (player controller) |
| **Knowledge Risk** | LOW-MEDIUM — `CharacterController.Move()` is unaffected by Unity 6's physics-solver changes (those apply to `Rigidbody` only); the Input System package itself is stable, well past its own post-cutoff transition period, and this project's GDD already correctly specifies it over the deprecated legacy `Input` class |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/physics.md`, `modules/input.md`, `breaking-changes.md`, `deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None — `CharacterController`, `SphereCast`-adjacent physics queries used by consumers, and the Input System's generated C# class pattern are all stable pre-6.0 APIs confirmed unchanged in `breaking-changes.md` |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (Foundation layer, self-contained for movement/camera/lock mechanics) |
| **Enables** | Future ADRs for Etkileşim Sistemi (Interaction), Asansör/Kat-Erişim Sistemi (Elevator), Görev/Taşıma Döngüsü (Carry Loop), Adaptif Ses Sistemi (footstep audio), Işık/Volume Durum Sistemi (reads `PlayerMaxSpeed`), Sahne Kesmeli Anlatı (Cutscene) — all six consume this ADR's `IPlayerState` interface and/or the movement-lock contract |
| **Blocks** | Stories for all six consuming systems above, until this ADR reaches `Accepted` |
| **Ordering Note** | Foundation layer, Batch 1 priority #2 per `docs/architecture/architecture-review-2026-08-05.md` (behind ADR-0001, which this ADR does not depend on — the two were sequenced together only because both are Foundation, not because of a real dependency). **Open cross-batch item**: this ADR defines a consumer-side interface for the approach-slow-taper formula's registry read (see Decision → Flagged-Object Registry Consumption); the registry's *ownership* is intentionally deferred to the future Interaction System ADR (Batch 2) — see Alternatives Considered, Alternative 2. |

## Context

### Problem Statement
Every interactive system in the game (Interaction, Elevator, Carry Loop, Cutscene, Adaptive Audio, Lighting/Volume) is built on top of player movement, camera orientation, and a shared movement-freeze mechanism. This ADR fixes the technical approach for all three, plus the reference-counted lock contract that lets multiple systems (Elevator, Cutscene, Interaction's Hold gesture) request/release movement freezing without clobbering each other.

### Constraints
- 60fps / 16.6ms frame budget project-wide (`.claude/docs/technical-preferences.md`)
- No sprint input, no crouch — deliberately cut per GDD (no combat/risk to justify them)
- Movement lock must survive multiple simultaneous requesters (Elevator + Interaction's Hold could theoretically overlap) without one system's `Release` call breaking another's active lock
- Player input must never be able to break a movement lock — only the requesting system's own `Release` call can
- Head-bob and footstep audio must share one phase source — any independent-timer implementation would desync them, breaking the "body already knows this work" fantasy the GDD explicitly calls out

### Requirements
See `docs/architecture/tr-registry.yaml`: TR-fpc-001 through TR-fpc-034 (extracted from `design/gdd/birinci-sahis-kontrolcu.md`), plus consumer-side requirements TR-interact-001/002/012, TR-elevator-011/012, TR-carry-005/021, TR-audio-020/021, TR-lighting-007/040, TR-cutscene-007/009/017.

## Decision

A single `PlayerController` MonoBehaviour (scene-persistent for the duration of a session, one instance) owns `CharacterController`, camera pitch/yaw, and the movement-lock state machine. Two design questions confirmed with the user before drafting are reflected below.

### Core mechanism
- Movement uses `CharacterController.Move()` driven by the GDD's own fully-specified analytic exponential-decay smoothing formula (`v(t+Δt) = v_target + (v(t)-v_target)×e^(-k·Δt)`) — computed directly in `Update()`, not through `Rigidbody`/`AddForce`, since the formula's stability guarantee (no overshoot, stable under large `Δt`) is an analytic property of exponential decay that a physics-solver-driven approach cannot reproduce without extra damping code fighting the solver
- `v_target` is the composed output of the Carry multiplier (`SetCarrying(bool)`, called only by the future Carry Loop system) and the approach-slow taper (see Flagged-Object Registry Consumption below) — both multiplicative, per the GDD's own Formula 2 rationale
- Camera pitch/yaw is a plain `Transform` rotation on a child camera object, clamped ±80° pitch, unbounded yaw — no physics involvement
- A single shared stride-phase accumulator (distance-based, not clock-based) drives both head-bob amplitude and the `PlayFootstep(speed)` call consumed by the future Adaptive Audio ADR — implemented as one `float` field advanced by `CharacterController.velocity.magnitude * Time.deltaTime` each frame, read by both the head-bob curve and the footstep trigger, guaranteeing they cannot desync regardless of how `v(t)` fluctuates. **Wrapped, not raw-accumulating** (Unity specialist finding): the field is reduced modulo stride length each frame (`accumulator %= strideLength`) rather than growing unbounded for the session's duration — float32 precision at a 60-minute session's worst-case ~5760m accumulated distance is still sub-millimeter and would not have caused audible drift, but wrapping removes the concern architecturally rather than relying on that margin.

### Movement Lock (reference-counted, scope-aware)
```csharp
public enum MovementLockScope { Full, MoveOnly }

// Backed by Dictionary<object, MovementLockScope>, not a bool, int counter,
// or bare HashSet<object> — a HashSet cannot record each requester's own
// scope, which "most restrictive wins" requires (TD-ADR finding, ADR-0002
// review). Requesters are matched by reference identity, not by call count.
void RequestMovementLock(object requester, MovementLockScope scope = MovementLockScope.Full);
void ReleaseMovementLock(object requester);
bool IsLocked { get; }                    // true if ANY requester currently holds the lock
bool HoldsLock(object requester);         // true if THIS specific requester currently holds it
bool MovementLocked { get; }              // legacy-named alias the GDD also exposes; same value as IsLocked
event Action MovementLockChanged;
```
- A duplicate `RequestMovementLock(sameRequester, scope)` without an intervening `Release` overwrites that requester's stored scope (last-write-wins for that requester specifically) rather than being a no-op — since the dictionary already holds a value for that key, this is a deliberate update, not a duplicate-insert edge case
- `ReleaseMovementLock` from a non-owning requester is a silent no-op (`Dictionary.Remove` on a non-existent key returns `false` and does nothing) — never throws, never affects other holders
- Effective scope is the most restrictive across all active requesters: if any requester's stored scope is `Full`, `Look` freezes too, even if another requester's stored scope is only `MoveOnly` — computed by scanning the dictionary's values, not inferred from presence alone
- **Recomputation timing** (TD-ADR finding): effective scope is recomputed in `LateUpdate`, immediately before the frame's `Move()` call — not reactively inside `Request`/`Release`. This guarantees a lock requested by another system's `Update()` earlier in the same frame is guaranteed to apply to that same frame's movement, regardless of script execution order between `PlayerController` and the requesting system.
- **Event ordering invariant** (TD-ADR finding): `MovementLockChanged` fires only after the dictionary mutation and effective-scope recomputation are both complete — never mid-mutation. This allows a subscriber to safely call back into `RequestMovementLock`/`ReleaseMovementLock`/`HoldsLock` from its own handler without observing a half-updated state.
- When a lock is requested mid-walk (`v(t) > 0`), `v_target` is pulled to 0 but Formula 1 still governs the deceleration curve — no instant stop

### Flagged-Object Registry Consumption (design decision, confirmed with user)
Formula 2's `d` variable (distance to the nearest flagged interactable, driving the approach-slow taper) requires reading a shared registry that the GDD assigns to the Interaction System — whose ADR does not exist yet (Batch 2). Per the user's confirmed decision, this ADR defines **only the consumer-side interface**, not the registry's ownership or storage:

```csharp
public interface IFlaggedObjectRegistry {
    // Minimum distance from `position` to any registered flagged object.
    // Returns float.PositiveInfinity if the registry is empty.
    float NearestFlaggedDistance(Vector3 position);
}
```
`PlayerController` takes an `IFlaggedObjectRegistry` reference (constructor/inspector-injected, not a hard singleton lookup) and calls `NearestFlaggedDistance(transform.position)` once per frame to compute Formula 2's `d`. **This ADR does not decide where the implementing registry lives, who writes to it, or its exact registration API (`OnEnable`/`OnDisable` self-registration, as the GDD suggests, is one option but not locked here).** That decision belongs to the future Interaction System ADR, which must produce a concrete type implementing `IFlaggedObjectRegistry`.

**Interim default** (TD-ADR finding): until the Interaction System ADR lands, `PlayerController` defaults to injecting a `NullFlaggedObjectRegistry` — `NearestFlaggedDistance` always returns `float.PositiveInfinity` — so Formula 2's taper collapses to `TaperMult = 1.0` (no-op) and FPC remains fully functional standalone rather than half-broken while waiting on a system it doesn't own. This ADR's own validation criteria (below) additionally test `PlayerController` against a mock non-null implementation to verify the formula itself is correct, independent of `NullFlaggedObjectRegistry`'s trivial behavior.

**Amendment (ADR-0007, Interaction System, 2026-08-05) — real registry now exists, wiring closed**: `PlayerController.Awake()` now adopts `FoundationCompositionRoot.FlaggedObjectRegistry` (ADR-0006's composition root, extended by ADR-0007) as a fallback, but **only if its own constructor/Inspector-injected field is still unset**:
```csharp
void Awake() {
    _flaggedObjectRegistry = _flaggedObjectRegistry ?? FoundationCompositionRoot.FlaggedObjectRegistry;
}
```
This preserves this ADR's original constructor/Inspector-injection intent exactly — a test or a scene author can still directly assign a specific (real or mock) `IFlaggedObjectRegistry` and it takes priority — while closing the previously-open question of what assigns the real one during normal gameplay. `flagged_object_registry`'s producer is now resolved (`interaction-system`, ADR-0007) in `docs/registry/architecture.yaml`; this ADR's own Decision above is otherwise unchanged.

### Input consumption pattern (design decision, confirmed with user)
A generated C# class (`new PlayerControls()`, from Unity's Input Actions asset with "Generate C# Class" checked) is instantiated in `Awake()`, with `controls.Enable()`/`controls.Disable()` called from `OnEnable()`/`OnDisable()` — matching the engine reference's documented recommended pattern. `Move`/`Look` are read via `ReadValue<Vector2>()` each `Update()` (continuous Value-type actions).

`Interact` is exposed to the future Interaction System through a read-only `IInteractInput` wrapper, **not** the raw `InputAction` reference (TD-ADR finding: a raw `InputAction` is a mutable object — a consumer could call `.Disable()` on it — and isn't mockable, contradicting this project's dependency-injection/testability coding standard):
```csharp
public interface IInteractInput {
    bool PressedThisFrame { get; }  // wraps controls.Gameplay.Interact.WasPressedThisFrame()
    bool IsPressed { get; }         // wraps controls.Gameplay.Interact.IsPressed()
}
```
`PlayerController` implements this itself (or exposes a small internal adapter) rather than forwarding the underlying `InputAction` — keeping this ADR's surface to movement/camera/lock/input-query only, with no way for a consumer to mutate input state it doesn't own.

### Architecture Diagram
```
                    ┌─────────────────────────────┐
                    │        PlayerController        │
                    │   (CharacterController owner)   │
                    └─────────────────────────────┘
        ▲                    ▲              │           │
  SetCarrying(bool)   RequestMovementLock/   │ IPlayerState │ PlayFootstep(speed)
  (future Carry Loop) ReleaseMovementLock     │ (EyeCamera,  │ (future Adaptive Audio,
                       (Elevator, Cutscene,   │  Velocity,   │  via shared stride-phase
                        Interaction's Hold)   │  IsLocked…)  │  accumulator)
                                              ▼
                                   [future Interaction System:
                                    raycasts via EyeCamera,
                                    owns IFlaggedObjectRegistry
                                    that this ADR only consumes]
```

### Key Interfaces
```csharp
public interface IPlayerState {
    Transform EyeCamera { get; }        // read-only
    Vector3 Velocity { get; }
    bool IsGrounded { get; }
    bool MovementLocked { get; }        // read-only, alias of IsLocked
    bool IsCarrying { get; }
    bool IsLocked { get; }              // read-only, ANY requester holds lock
    bool HoldsLock(object requester);   // read-only, THIS requester holds lock
    IInteractInput Interact { get; }    // read-only wrapper, see Input consumption pattern
    event Action MovementLockChanged;
}

public interface ISceneTransitionAware {  // not exposed directly — CharacterController itself is never public
    void RequestMovementLock(object requester, MovementLockScope scope = MovementLockScope.Full);
    void ReleaseMovementLock(object requester);
}

void SetCarrying(bool isCarrying);   // callable only by the future Carry Loop system (convention, not enforced by the type system at this layer)
```
`CharacterController` is never exposed directly — any future system needing to affect position uses a wrapped `Move()` call on `PlayerController` itself, to avoid two systems issuing conflicting `.Move()` calls in the same frame.

## Alternatives Considered

### Alternative 1: Rigidbody-based movement (physics-driven)
- **Description**: Drive the player capsule via `Rigidbody.AddForce`/velocity, letting PhysX's solver handle collision response.
- **Pros**: Free interaction with other `Rigidbody` objects; potentially simpler collision response code.
- **Cons**: The GDD's Formula 1 is an analytic exponential-decay solution with a proven stability guarantee (no overshoot, stable under large `Δt`) — reproducing that exact curve on top of a physics solver means fighting the solver's own damping/drag with corrective forces every frame, and Unity 6's increased default solver iterations (`Physics.defaultSolverIterations`, 6→8) changes the solver's settling behavior in ways this project has not profiled.
- **Rejection Reason**: `CharacterController.Move()` with a hand-computed formula is strictly simpler and matches the GDD's explicit mathematical contract exactly; nothing in this game (no other dynamic rigidbodies the player needs to push) motivates the physics-driven approach's main benefit.

### Alternative 2: Settle `IFlaggedObjectRegistry` ownership in this ADR
- **Description**: Place the registry implementation in a shared Foundation-layer static utility class, decided here rather than deferred.
- **Pros**: Closes the GDD's open question immediately; no two-ADR dependency chain for Formula 2.
- **Cons**: The GDD explicitly assigns registry ownership to the Interaction System's domain (`etkilesim-sistemi.md` Core Rules) — deciding its storage/registration API from the FPC ADR would be this ADR reaching into a Core-layer system's territory, violating the project's "No Unilateral Cross-Domain Changes" coordination rule (`.claude/docs/coordination-rules.md`).
- **Rejection Reason**: user confirmed deferring ownership to the Interaction System ADR; this ADR defines only the interface it consumes (see Decision).

### Alternative 3: `PlayerInput` component with SendMessages
- **Description**: Use Unity's `PlayerInput` component in "Send Messages" mode instead of a generated C# class.
- **Pros**: Zero-code Inspector setup.
- **Cons**: `SendMessage`-based dispatch is stringly-typed (method names matched by reflection) and cannot be unit-tested without a live `GameObject`/message pump — this project's Coding Standards require public methods to be unit-testable via dependency injection, not singletons/reflection magic.
- **Rejection Reason**: fails the project's testability standard; the generated-class pattern is equally simple to wire and fully unit-testable.

## Consequences

### Positive
- Formula 1's stability guarantee (proven analytically, not just "it usually looks fine") is preserved exactly as the GDD specifies, with no physics-solver interaction to reason about
- The reference-counted lock (`HashSet<object>` keyed by identity) structurally prevents the exact class of bug a bool or naive counter would allow — one system's premature `Release` can never unlock another system's still-active hold
- Deferring `IFlaggedObjectRegistry`'s ownership keeps this ADR's scope honest about what it actually decides, and keeps the future Interaction System ADR free to choose its own registration mechanism without this ADR pre-committing it

### Negative
- `PlayerController` now has two "owners" in a loose sense during the interim: this ADR governs movement/lock, but Formula 2 cannot be fully implemented/tested against a real registry until the Interaction System ADR lands — `dev-story` work on the approach-slow taper is correctly blocked until then, tracked as a known gap, not silently ignored
- The shared stride-phase accumulator couples head-bob and footstep-audio timing at the architecture level — a future system wanting independent footstep cadence (e.g., a sprint mechanic, if ever added) would need to revisit this ADR, not just add a config value

### Risks
- If the future Interaction System ADR chooses a registry API shape that doesn't cleanly satisfy `IFlaggedObjectRegistry` as defined here, `PlayerController`'s Formula 2 call site needs a small adapter — low risk, narrow blast radius, flagged explicitly so it isn't a surprise
- `MovementLockScope`'s "most restrictive wins" rule requires correctly recomputing effective scope from the full `Dictionary<object, MovementLockScope>` on every `LateUpdate` (not caching a stale value, and not inferring scope from mere key presence as an earlier `HashSet<object>`-based draft of this ADR would have made structurally impossible) — the revised design closes this as an implementation-detail risk rather than an architectural gap; still covered by required unit tests (see Validation Criteria)

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| birinci-sahis-kontrolcu.md | TR-fpc-005: Formula 1, analytic exponential speed smoothing | Decision → Core mechanism |
| birinci-sahis-kontrolcu.md | TR-fpc-006/031: Formula 2, approach-slow taper, cross-layer registry read | Decision → Flagged-Object Registry Consumption |
| birinci-sahis-kontrolcu.md | TR-fpc-007: Formula 3, head-bob amplitude, shared stride-phase with footsteps | Decision → Core mechanism (stride-phase accumulator) |
| birinci-sahis-kontrolcu.md | TR-fpc-013/014/015/016: `IPlayerState`, reference-counted lock, `MovementLockScope`, `IsLocked` | Key Interfaces, Decision → Movement Lock |
| birinci-sahis-kontrolcu.md | TR-fpc-018: Interaction System raycasts via `EyeCamera`, FPC never reaches into Interaction | Architecture Diagram, Key Interfaces |
| birinci-sahis-kontrolcu.md | TR-fpc-019: single "Gameplay" Input Actions map | Decision → Input consumption pattern |
| birinci-sahis-kontrolcu.md | TR-fpc-023/024/025/026: CharacterController collision tuning (step offset, skin width, corner chamfering, hit-layer filtering) | Delegated to level-design/prefab tuning per GDD Tuning Knobs — not an architectural decision this ADR owns beyond confirming `CharacterController.Move()` as the mechanism |
| etkilesim-sistemi.md | TR-interact-002/012/029: shared registry, `EyeCamera` dependency, open registry-ownership question | Decision → Flagged-Object Registry Consumption; Alternatives Considered, Alternative 2 |
| asansor-kat-erisim-sistemi.md | TR-elevator-011/012: `RequestMovementLock(MoveOnly)`/`Release` on doors closing/complete | Key Interfaces |
| gorev-tasima-dongusu.md | TR-carry-005/021: `SetCarrying(bool)` single call point, shared phase accumulator for carry sway | Key Interfaces, Decision → Core mechanism |
| adaptif-ses-sistemi.md | TR-audio-020/021: `PlayFootstep(speed)` from FPC's own stride-phase, no independent Velocity sampling | Decision → Core mechanism |
| isik-volume-durum-sistemi.md | TR-lighting-007/040: `PlayerMaxSpeed` (1.6 m/s) read-only for `R_trigger`/box-collider safety margin | Constant exposed as part of `IPlayerState`-adjacent public constant, no new interface needed |
| sahne-kesmeli-anlati-2026-08-02.md | TR-cutscene-007/009/017: `RequestMovementLock(Full)`/`Release` immediately before HARD CUT | Key Interfaces |

## Performance Implications
- **CPU**: negligible — one `CharacterController.Move()` call, one exponential-decay evaluation, one registry query, and one stride-phase increment per frame, all O(1)
- **Memory**: negligible — no per-frame allocations (formula uses value types only; `HashSet<object>` for lock requesters churns at most a few entries per session, not per frame)
- **Load Time**: N/A
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system, no existing code to migrate from.

## Validation Criteria
- AC-1 through AC-17 from `birinci-sahis-kontrolcu.md`, implemented as automated EditMode/PlayMode tests per this project's Logic-tier test-evidence rules
- AC-4/5 specifically require mock-`Δt` unit tests validating Formula 1's convergence bounds (0.20-0.25s to reach ≥95% of target, no overshoot at any sampled frame)
- New test case (per Consequences → Risks): `MovementLockScope` "most restrictive wins" — requester A calls `RequestMovementLock(A, MoveOnly)`, requester B calls `RequestMovementLock(B, Full)`; assert effective scope is `Full` (Look frozen) while both are active. Then B calls `ReleaseMovementLock(B)`; assert effective scope becomes `MoveOnly` (Look unfrozen) since only A (`MoveOnly`) remains. Then A calls `ReleaseMovementLock(A)`; assert `IsLocked` is `false`.
- New test case: `RequestMovementLock(A, MoveOnly)` followed by a second `RequestMovementLock(A, Full)` from the same requester `A` (no intervening `Release`) — assert `A`'s stored scope updates to `Full` (last-write-wins) and effective scope becomes `Full`.
- New test case: `NullFlaggedObjectRegistry.NearestFlaggedDistance` returns `float.PositiveInfinity`, verifying Formula 2 correctly collapses to `TaperMult = 1.0` when no real registry is injected
- Formula 2 (approach-slow taper) additionally tests against a mock non-null `IFlaggedObjectRegistry` (distinct from `NullFlaggedObjectRegistry`) to verify the formula's math itself, until the Interaction System ADR provides a real production implementation — tracked as a known interim gap, not a silent omission

## Related Decisions
- Enables: future ADRs for Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Görev/Taşıma Döngüsü, Adaptif Ses Sistemi, Işık/Volume Durum Sistemi, Sahne Kesmeli Anlatı
- Depends conceptually (not formally) on ADR-0001's `RequestMovementLock`/`ReleaseMovementLock` non-ownership stance — ADR-0001 explicitly never calls these; this ADR is where they're actually defined
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline and recommended ADR authoring order
