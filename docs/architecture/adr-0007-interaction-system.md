# ADR-0007: Interaction System — SphereCast Focus, Shared Registry & Hold/Instant State Machine

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Physics (SphereCast) / Core (registry, state machine) / UI (crosshair) |
| **Knowledge Risk** | LOW — `Physics.SphereCastNonAlloc`, object-null comparison patterns, and `Physics.SphereCastAll`'s GC-allocation behavior are all confirmed stable and unchanged post-2022-LTS |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/physics.md`, `current-best-practices.md`, `breaking-changes.md`, `deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0002 (First-Person Controller — hard dependency on `EyeCamera`, `IPlayerState.IsLocked`, `RequestMovementLock`/`ReleaseMovementLock`; this ADR is also the **producer** ADR-0002 has been waiting on for `IFlaggedObjectRegistry`) |
| **Enables** | Future ADRs for Görev/Taşıma Döngüsü (Carry Loop — implements `IInteractable.Instant`), Anı-Tetikleyici Etkileşim (Memory-Trigger — implements `IInteractable.Hold`) |
| **Blocks** | Stories for Carry Loop and Memory-Trigger, until this ADR reaches `Accepted` |
| **Ordering Note** | Core layer, Batch 2 priority #1. Amends ADR-0002 (adds a composition-root-fallback path to `PlayerController.Awake()`, see Decision). Resolves the GDD's own unresolved Open Question #1 (registry ownership/location) by construction — see Decision. Carries forward Open Question #2 (SphereCast occlusion for transparent-but-blocking surfaces) as this ADR's own open item, not silently decided. |

## Context

### Problem Statement
Every direct object interaction in the game — picking up carry items, holding a memory-trigger — flows through one focus-detection and input-dispatch system. This ADR fixes the concrete implementation: how focus detection works without GC pressure, how the shared "flagged interactable" registry satisfies ADR-0002's `IFlaggedObjectRegistry` contract (which needs a world position per entry that the GDD-specified `IInteractable` interface doesn't expose), and how this system's own crosshair/Hold-fill UI is owned end-to-end.

### Constraints
- 60fps / 16.6ms frame budget — focus detection runs every frame, must not allocate GC garbage per this project's established hot-path standard (`RaycastAll`-family allocation is explicitly flagged as an anti-pattern in `deprecated-apis.md`)
- `IInteractable`'s public contract (as specified in the GDD) carries no position/Transform member — position-dependent consumers (this ADR's own SphereCast hit resolution, and ADR-0002's `NearestFlaggedDistance`) need it from somewhere else
- Hold cancellation-by-looking-away must remain possible — movement lock must use `MovementLockScope.MoveOnly`, never `Full`
- The reference-counted movement lock (ADR-0002) never rejects a request — "lock already held elsewhere" must be a pre-check, not a rejection-handling path

### Requirements
See `docs/architecture/tr-registry.yaml`: TR-interact-001 through TR-interact-030 (extracted from `design/gdd/etkilesim-sistemi.md`), plus consumer-side requirements TR-fpc-006/031, TR-carry-003/004, TR-memory-002/003/020.

## Decision

### Focus detection (corrected per Unity specialist review)
Every frame, `Physics.SphereCastNonAlloc(origin, radius, direction, buffer, range, interactableLayerMask, QueryTriggerInteraction.Collide)` — **not** `SphereCastAll` (which the GDD's prose describes for the multi-hit tie-break, but which GC-allocates a new array every call, flagged as a hot-path anti-pattern in this project's own `deprecated-apis.md`). A class-level, reused `RaycastHit[8]` buffer avoids the allocation while preserving the GDD's exact multi-hit semantics (smallest `hit.distance` wins; ties broken by smallest collider `InstanceID`, deterministic frame-to-frame). Radius ~0.05m, range 2.0m, fixed Tuning Knob constants, not scaled by player speed or FOV.

**Explicit `LayerMask`** (Unity specialist + TD-ADR finding): the SphereCast uses a dedicated `interactableLayerMask`, never `Physics.DefaultRaycastLayers`, to avoid false hits against world geometry. `QueryTriggerInteraction.Collide` is the starting default (interactables may reasonably use trigger colliders to avoid physically blocking the player), configurable per-project need. **The GDD's own Open Question #2 (occlusion/line-of-sight — should a visually-transparent-but-physically-blocking surface like glass block this SphereCast?) is carried forward as this ADR's own open item, not silently decided** — the layer mask's exact composition (which layers count as "occluding") is deferred to dev-story time pending that decision.

### Registry: `(IInteractable, Transform)` pairs, not bare references (confirmed with user, refined per both reviews)
```csharp
public static class InteractableRegistry {
    public static void Register(IInteractable interactable, Transform transform);   // throws if transform is null
    public static void Unregister(IInteractable interactable);                       // reference-checked, see below
    public static float NearestFlaggedDistance(Vector3 position);                    // implements IFlaggedObjectRegistry
    // internal: IReadOnlySet-style snapshot access for this system's own focus scan
}
```
**Why pairs, not bare `IInteractable`** (confirmed by both reviews): the interface itself stays position-free — per TD-ADR review, "the registry is a spatial index, not an interaction index; position belongs to the index, not the contract." This system's own SphereCast focus resolution never needs the registry's position data at all (it resolves `IInteractable` directly from the hit collider via a `Component`/`GetComponent` lookup on the hit object) — **the registry's `Transform` data exists solely to satisfy `NearestFlaggedDistance`**, its only position-dependent consumer.

Self-registration happens in `OnEnable()`/`OnDisable()` — matching the GDD's own explicit choice, which independently matches ADR-0005's `scene_object_self_registration` pattern's documented branch for objects expected to be dynamically toggled at runtime (carry items call `SetActive(false)` on pickup and must stop being both SphereCast-focusable and taper-contributing at that exact moment, not just at scene-unload — this is precisely why `OnEnable`/`OnDisable`, not `Awake`/`OnDestroy`, is correct here, unlike Lighting/Volume's zones which used `Awake`/`OnDestroy`). `Unregister` is reference-checked per ADR-0005's rule 4 (only remove if the registry's stored pair for that key is still this instance) — `OnEnable`/`OnDisable` churn on carry items makes re-registration routine, not an edge case.

**Race protection**: both this system's own focus scan and any external consumer (`NearestFlaggedDistance`) read a read-only snapshot taken at scan-start, never the live collection — this prevents collection-modified-during-iteration errors when an object disables itself from within its own `OnFocusEnter`/`OnInteract` callback. All same-frame-destroyed-object access uses Unity's object-null comparison (`if (obj)`), never raw C# `null` checks, avoiding `MissingReferenceException`.

### Registry-location Open Question resolved by construction
The GDD's own Open Question #1 (should `InteractableRegistry` live in a shared Foundation location or this system's own files?) is resolved by the architecture already in place: ADR-0002's `PlayerController` never depends on the concrete `InteractableRegistry` type — only on `IFlaggedObjectRegistry`, an interface it has depended on since it was written. Because that indirection already exists, **`InteractableRegistry` can live entirely within this system's own files** without creating a Foundation-layer-depends-on-Core-layer file coupling. The cross-layer *read* relationship (FPC's Formula 2 needing nearest-flagged-distance) is real and intentional — but it flows entirely through the interface, never a direct file reference.

### Wiring `IFlaggedObjectRegistry` into `FoundationCompositionRoot` (amends ADR-0002 and ADR-0006, corrected per TD-ADR review)
ADR-0002 originally specified `PlayerController` receiving `IFlaggedObjectRegistry` via constructor/Inspector injection — a MonoBehaviour-appropriate mechanism, deliberately not the static-property pattern used for static-to-static wiring elsewhere. TD-ADR review confirmed the initially-considered "just route it through `FoundationCompositionRoot` like everything else" approach doesn't actually work as stated: `FoundationCompositionRoot.Wire()` runs at `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, before any scene's `MonoBehaviour`s exist — it cannot assign an *instance* field on a not-yet-created `PlayerController`. The corrected shape, an **extension** of the established pattern (not a third, incompatible mechanism):
```csharp
public static class FoundationCompositionRoot {
    // ... existing LightingQuery/TransitionSource wiring (ADR-0005/0006) ...
    public static IFlaggedObjectRegistry FlaggedObjectRegistry { get; private set; } = new NullFlaggedObjectRegistry();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Wire() {
        // ... existing assignments ...
        FlaggedObjectRegistry = new InteractableRegistryAdapter();   // wraps the static InteractableRegistry
    }
}
```
`PlayerController.Awake()` adopts the composition root's value **only if its own injected field is still unset**:
```csharp
void Awake() {
    _flaggedObjectRegistry = _flaggedObjectRegistry ?? FoundationCompositionRoot.FlaggedObjectRegistry;
}
```
This preserves ADR-0002's original DI/testability intent exactly (a test can still construct/Inspector-inject a mock `IFlaggedObjectRegistry` and it takes priority) while closing the "who actually assigns the real one in normal gameplay" gap that was previously unaddressed. `InteractableRegistryAdapter : IFlaggedObjectRegistry` is a thin forwarding wrapper, same shape as `LightingVolumeQueryAdapter` (ADR-0005).

### Crosshair ownership (made explicit, per TD-ADR finding)
This ADR owns the crosshair **end-to-end**: the Idle/Focused/Holding state machine driving its visual state, the `t` value source for the default Hold-fill indicator (computed directly by this system every frame during `Holding`, never waiting on `OnHoldProgress(t)` — automatic for every Hold interactable unless `SuppressDefaultHoldFill` returns `true`), and the accessibility requirement that state changes are never color-only (shape/size must also change). **Rendering technology**: UI Toolkit (per `current-best-practices.md`'s Unity 6 recommendation for new runtime UI, "production-ready... replaces UGUI for new projects") — this ADR makes that call rather than leaving it unowned. **Exact visual styling/asset production is explicitly deferred** to the already-flagged `/ux-design` pass (per the GDD's own UX Flag) — this ADR owns the state machine and data contract (`PromptText`, `t`, focus state), not pixel-level presentation.

### `IInteractable` interface (unchanged from GDD, reproduced as this ADR's authoritative contract)
```csharp
public interface IInteractable {
    InteractionType Type { get; }              // Instant | Hold
    float HoldDuration { get; }
    bool CanInteract { get; }
    string PromptText { get; }
    void OnFocusEnter(); void OnFocusExit();
    void OnInteract();                                                      // Instant only
    void OnHoldProgress(float t); void OnHoldComplete(); void OnHoldCancelled();  // Hold only
    void OnHoldBlocked();                                                   // Hold only
    bool SuppressDefaultHoldFill { get; }                                   // Hold only, default false
}
```

### Movement lock (pre-check, never a rejection path)
Entering `Holding` calls `RequestMovementLock(this, MovementLockScope.MoveOnly)` — `MoveOnly` is required, not `Full`, specifically so `Look` stays free for the cancel-by-looking-away path. Before ever calling it, this system checks `IPlayerState.IsLocked`: since the reference-counted lock (ADR-0002) never rejects a request, "lock already held by another system" must be handled as a **pre-check** — if `IsLocked` is already `true` at the moment `Focused→Holding` would begin, `RequestMovementLock` is never called and `OnHoldBlocked()` fires instead.

### Architecture Diagram
```
        [Görev/Taşıma Döngüsü: implements IInteractable.Instant]
        [Anı-Tetikleyici Etkileşim: implements IInteractable.Hold]
                              │  self-register (OnEnable/OnDisable)
                              ▼
                    ┌───────────────────────┐
                    │   InteractableRegistry     │
                    │  ((IInteractable,Transform)) │
                    └───────────────────────┘
                     ▲                    │
        SphereCastNonAlloc         NearestFlaggedDistance
        (this system's own              (IFlaggedObjectRegistry)
         focus resolution,                       │
         resolves IInteractable                   ▼
         from hit collider          InteractableRegistryAdapter
         directly, doesn't                        │
         need the registry)                       ▼
                              FoundationCompositionRoot.FlaggedObjectRegistry
                                                    │
                                    PlayerController.Awake()
                                    (adopts if own field still null —
                                     ADR-0002 DI/testability preserved)
```

### Key Interfaces
See Registry, Wiring, and `IInteractable` sections above.

## Alternatives Considered

### Alternative 1: Require `IInteractable` implementers to also be `MonoBehaviour`/`Component`
- **Description**: Add a type constraint or restructure so every `IInteractable` is guaranteed to have a `.transform`, avoiding the need for pair-based registration.
- **Pros**: Simpler single-argument `Register(IInteractable)` API.
- **Cons**: Per Unity specialist review, this would break pure-C#-object unit testability, which this project's Coding Standards explicitly mandate ("all public methods must be unit-testable"). Forcing a `Component` dependency onto the interface contract itself is a real cost for a benefit (one fewer constructor argument) that doesn't justify it.
- **Rejection Reason**: pair-based registration keeps `IInteractable` testable in isolation; confirmed correct by both reviews.

### Alternative 2: Route `IFlaggedObjectRegistry` wiring entirely through `FoundationCompositionRoot`, dropping ADR-0002's constructor/Inspector injection
- **Description**: Give `PlayerController` a settable static-style property instead of instance-level DI.
- **Pros**: Uniform wiring mechanism across every cross-system dependency in the project.
- **Cons**: `PlayerController` is a scene `MonoBehaviour`, not a static class — collapsing its DI-friendly constructor/Inspector injection into a global static property would be a real regression for testability (a unit test constructing a `PlayerController` with a mock registry is more direct than one that has to reset/restore a global static before and after each test).
- **Rejection Reason**: the corrected "adopt composition-root value only if own field is unset" shape gets both benefits — normal gameplay gets a working default, tests keep direct injection — without forcing a single mechanism where two serve different consumer shapes better.

## Consequences

### Positive
- Closes the last open item from ADR-0002's original design — `PlayerController`'s approach-slow taper (Formula 2) is now fully functional in normal gameplay, not just null-object-safe
- `SphereCastNonAlloc` with a reused buffer closes a real hot-path GC-allocation risk before it ever shipped, not after a profiling pass caught it
- The registry-location Open Question (open in the GDD since 2026-08-02) is resolved without requiring any Foundation-layer file to reference a Core-layer file — the existing interface indirection already made this possible, this ADR just states it explicitly
- `FoundationCompositionRoot`'s extension pattern (static property + MonoBehaviour "adopt if unset" convention) is itself a reusable answer for any future ADR needing to wire a static-owned resource into a scene `MonoBehaviour` — a gap the pattern didn't originally cover

### Negative
- The `InteractableRegistry` is now dual-purpose (SphereCast focus resolution source AND `IFlaggedObjectRegistry` spatial index) — a future refactor separating these concerns would need to touch both consumers; acceptable now given both are genuinely small and share the same underlying self-registration lifecycle
- `PlayerController`'s "adopt if unset" convention in `Awake()` is an implicit contract or dev-story implementers to follow correctly (assigning `_flaggedObjectRegistry` too early or too late relative to `Awake()` order could reintroduce the null-object-forever bug ADR-0002 originally guarded against) — called out explicitly in Validation Criteria

### Risks
- If `InteractableRegistry`'s self-registration is ever bypassed by toggling a carry item's `enabled` flag instead of `SetActive(false)` (Unity specialist finding), `OnEnable`/`OnDisable` won't fire and a stale `Transform` entry persists without going stale-null — worth a one-line convention note for future consuming systems (Carry Loop, Memory-Trigger): always use `SetActive`, never `enabled`, to toggle interactable availability
- The occlusion/layer-mask Open Question (GDD's own #2, carried forward here) remains genuinely open — a future dev-story decision, not silently resolved by this ADR

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| etkilesim-sistemi.md | TR-interact-001/017/021: SphereCast focus detection, fixed radius/range, multi-hit tie-break | Decision → Focus detection |
| etkilesim-sistemi.md | TR-interact-002/024/029: shared registry, snapshot iteration, registry ownership/location | Decision → Registry, Registry-location Open Question |
| etkilesim-sistemi.md | TR-interact-005/006/027/028: default Hold-fill, SuppressDefaultHoldFill, colorblind accessibility, mock-object test | Decision → Crosshair ownership |
| etkilesim-sistemi.md | TR-interact-009/010/011/022: state machine, MoveOnly lock, pre-check-not-rejection, OnHoldBlocked | Decision → Movement lock |
| etkilesim-sistemi.md | TR-interact-013/014/015/016: IInteractable contract, linear hold_progress, downstream easing only | Decision → IInteractable interface |
| etkilesim-sistemi.md | TR-interact-030: SphereCast occlusion open question | Decision → Focus detection (carried forward, not decided) |
| birinci-sahis-kontrolcu.md (ADR-0002) | TR-fpc-006/031: Formula 2 registry read, cross-layer dependency | Decision → Wiring into FoundationCompositionRoot |

## Performance Implications
- **CPU**: `SphereCastNonAlloc` with an 8-entry reused buffer, once per frame — no allocation, negligible cost at MVP's low interactable-per-scene density
- **Memory**: registry pairs are a handful of entries at MVP scale; buffer is a fixed 8-`RaycastHit` array, allocated once
- **Load Time**: N/A
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system. Amends ADR-0002's `PlayerController.Awake()` with the composition-root-fallback line (additive, does not change ADR-0002's existing Decision).

## Validation Criteria
- All Acceptance Criteria from `etkilesim-sistemi.md`, implemented as automated EditMode/PlayMode tests per this project's Logic-tier test-evidence rules
- New test (Unity specialist finding): `SphereCastNonAlloc` with the reused buffer produces zero GC allocations across repeated calls (Unity Test Framework's allocation-tracking assertion)
- New test: `InteractableRegistry.Register` with a `null` `Transform` throws (fails loudly at registration time rather than producing a silently-broken `NearestFlaggedDistance` entry)
- New test (TD-ADR finding): `PlayerController` constructed/Inspector-injected with a mock `IFlaggedObjectRegistry` never adopts `FoundationCompositionRoot.FlaggedObjectRegistry` — confirms the "adopt only if unset" convention holds
- New test: an object toggling `SetActive(false)`→`SetActive(true)` mid-`Holding` is treated as target-loss (per GDD Edge Cases), verifying the reference-checked `Unregister`/re-`Register` cycle doesn't leave a stale entry

## Related Decisions
- Implements the `IFlaggedObjectRegistry` contract ADR-0002 already registered and consumed via null-object default — this ADR is its producer
- Amends ADR-0002 (`PlayerController.Awake()` composition-root fallback) and extends ADR-0006's `FoundationCompositionRoot` (new `FlaggedObjectRegistry` static property)
- Adopts ADR-0005's `scene_object_self_registration` pattern's `OnEnable`/`OnDisable` branch, as that pattern's entry anticipated
- Enables future ADRs for Görev/Taşıma Döngüsü and Anı-Tetikleyici Etkileşim
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline — **this ADR begins Batch 2 (Core layer)**
