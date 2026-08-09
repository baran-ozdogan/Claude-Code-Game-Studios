# ADR-0003: Player State and Movement Lock Architecture

> **Unity Specialist Validation**: BLOCKING (found and fixed) 2026-08-05 — original draft claimed Unity 6's solver-iteration default change (6→8) was a MEDIUM risk affecting `CharacterController` tuning; factually wrong (`CharacterController` is kinematic, never consumes that PhysX setting), corrected to LOW risk here and in `architecture.md`'s own Engine Knowledge Gap Summary (same inherited error, also fixed). 4 additional MINOR notes folded in: `Debug.Assert`-based duplicate-instance guard replaced with unconditional `Debug.LogError`+`Destroy`; Reload-Scene-disabled gap (parallel to ADR-0001's own finding) added to Risks + Validation Criteria; latent Player/UI boot-ordering risk documented; same-GameObject `Awake()` ordering confirmed as a non-issue.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-05 — 3 findings, all fixed: (1) `PlayerStateProvider`-is-a-`MonoBehaviour` testability language corrected (it's not a plain `new`-able object; the real benefit is avoiding `CharacterController`/`Camera` coupling, not "plain construction"); (2) a genuine, previously-undocumented cross-scope lock edge case found (`Request(A, Full)` then `Request(A, MoveOnly)` without `Release`) — documented as intentional "sticky most-restrictive" behavior with a new Validation Criteria test, and the pre-existing Risk bullet's inaccurate-for-this-subcase "no-op by construction" claim corrected; (3) the latent Player/UI boot-ordering risk given a concrete provisional answer (UI loads before Player, sequentially) instead of being left fully open, and flagged in `architecture.md`'s Required ADRs as a future "Boot Sequence" ADR candidate.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (with Physics and Input sub-aspects) |
| **Knowledge Risk** | LOW — **corrected down from an inherited MEDIUM claim (unity-specialist validation, 2026-08-05, BLOCKING finding, fixed)**. `architecture.md`'s Engine Knowledge Gap Summary listed the Unity 6 solver-iteration default change (6→8) as MEDIUM risk "affecting `CharacterController`/collider tuning" — this is factually incorrect and this ADR does not repeat it. `CharacterController` is a **kinematic** capsule controller driven entirely by its own `Move()` sweep-and-slide algorithm and its own parameters (`skinWidth`, `stepOffset`, `minMoveDistance`, `slopeLimit`); it has never consumed `Physics.defaultSolverIterations`/`defaultSolverVelocityIterations`, which govern only the **dynamic rigidbody/joint** solver. This project has no Rigidbody-driven gameplay (confirmed in `architecture.md`'s Module Ownership — the FPC uses `CharacterController` exclusively, trigger colliders for zones), so the solver-iteration change has **no relevance to this ADR at all**. The Input System aspect remains LOW/stable as already established project-wide. |
| **References Consulted** | `docs/engine-reference/unity/modules/physics.md`, `docs/engine-reference/unity/modules/input.md`, `docs/engine-reference/unity/breaking-changes.md`, `docs/engine-reference/unity/current-best-practices.md` |
| **Post-Cutoff APIs Used** | None — `CharacterController`, `UnityEngine.InputSystem` (Input Actions asset + generated C# class) both predate the Unity 6 cutoff, and remain the idiomatic, non-deprecated choice in 6.3 with no indicated replacement API. |
| **Verification Required** | None significant, following the correction above. The `CharacterController` capsule's step-offset/skin-width tuning (`birinci-sahis-kontrolcu.md`'s locked ~2cm step offset, skin width ≈ radius×10%) is governed by `CharacterController`'s own parameters, not by any post-cutoff solver change — ordinary playtesting/tuning is sufficient, no special smoke-test against a solver default is warranted. **Follow-up owed**: `architecture.md`'s own Engine Knowledge Gap Summary should be corrected to remove this same conflation — flagged, not yet fixed there as of this ADR (see Related Decisions). |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0004 (InteractableRegistry Foundation Ownership — the approach-slow-taper formula reads `IPlayerState`-adjacent position data), and every Core/Feature ADR whose system reads `IPlayerState` or calls `RequestMovementLock`/`ReleaseMovementLock` (Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Görev/Taşıma Döngüsü, Sahne Kesmeli Anlatı — effectively all of them) |
| **Blocks** | Any story implementing player movement, camera control, or any Hold/Instant interaction, elevator ride, or HARD CUT sequence — `IPlayerState` and the movement lock are this project's most widely-read Foundation contract |
| **Ordering Note** | Should be Accepted early — nearly every other Foundation/Core/Feature ADR's Key Interfaces sections (already sketched in `architecture.md`) assume this contract exists in its final shape |

## Context

### Problem Statement

`birinci-sahis-kontrolcu.md` already specifies the `IPlayerState` interface's field list (`EyeCamera`, `Velocity`, `IsGrounded`, `MovementLocked`, `IsCarrying`, `IsLocked`, `MovementLockChanged`) and the reference-counted `RequestMovementLock(requester, scope)`/`ReleaseMovementLock(requester)` contract's semantics (multiple concurrent holders, most-restrictive-scope-wins, never released by player input) — but leaves three concrete engineering questions open that this ADR resolves: (1) what concrete C# type implements `IPlayerState` and how do the ~6 consumer systems (Etkileşim, Asansör, Görev/Taşıma, Anı-Tetikleyici, Sahne Kesmeli Anlatı, InteractableRegistry's own approach-taper read) actually obtain a reference to it; (2) how does the reference-counted lock's internal bookkeeping work concretely; (3) how does the player `GameObject` itself survive the depot↔ballroom elevator scene swap, given `seviye-sahne-gecisi.md` confirms the player is repositioned to a `SoftTransitionAnchor` in the target scene rather than destroyed and recreated — implying the object persists across the swap, a lifecycle question no GDD actually specifies the mechanism for.

### Constraints
- `birinci-sahis-kontrolcu.md`'s `IPlayerState` field list and `MovementLockScope { Full, MoveOnly }` semantics are already Approved and must not be altered by this ADR — only their concrete implementation is undecided.
- The reference-counted lock must never be released by player input (`birinci-sahis-kontrolcu.md` Core Rules) — only the exact requester that acquired it, or nothing, can release it.
- `IsLocked` must be cheaply pre-checkable by any system considering a `Hold` interaction, to avoid the soft-lock class of bug this project's GDD history has repeatedly had to fix (`etkilesim-sistemi.md`'s `IsLocked` pre-check, added specifically for this).
- The player must survive both SOFT transitions (repositioned to a `SoftTransitionAnchor`, not recreated) and HARD CUTs (`seviye-sahne-gecisi.md`) without losing `Velocity`/`IsGrounded`/camera state mid-transition in a way that produces a visible pop or discontinuity — `game-concept.md`'s "Bedenin Sürekliliği" (bodily continuity) fantasy depends on this.
- Must stay consistent with this project's two already-established scene-persistence precedents: ADR-0001 (static services persist via in-memory data, not `DontDestroyOnLoad`) and ADR-0002 (the UI surface persists via a dedicated, boot-loaded, never-unloaded scene, explicitly not `DontDestroyOnLoad`) — a third, different answer to "how does X survive a scene swap" would be a real inconsistency this project has twice already avoided introducing.

### Requirements
- `IPlayerState` must be readable by every consumer system without a scene-reference-wiring footgun (e.g. a broken Inspector-dragged reference after a scene reload).
- The movement lock must support N concurrent holders (confirmed reachable — Etkileşim's Hold lock and Asansör's ride lock could theoretically overlap in edge-case timing) without one holder's `Release` accidentally releasing another's.
- Must not reintroduce a testability regression relative to ADR-0001's precedent — though `IPlayerState` is inherently live per-frame `MonoBehaviour` data (not a session-scoped fact ADR-0001's static-facade pattern actually fits), the *lookup mechanism* consumers use to reach it should still avoid the "three independently-invented answers to the same question" problem ADR-0002 flagged and fixed for the UI lookup.

## Decision

**The player `GameObject` (with its `CharacterController`, camera, and the `PlayerStateProvider` component below) lives in a dedicated, persistent "Player" scene — loaded additively once at boot via the same mechanism ADR-0002 established for the UI scene, and never unloaded.** SOFT transitions reposition the existing player `Transform` to the target scene's `SoftTransitionAnchor` (translation/rotation only — the `GameObject` itself is never destroyed or recreated), which is exactly what `seviye-sahne-gecisi.md` already requires and gives this project its third consistent application of the same "persistent scene, not `DontDestroyOnLoad`" pattern (state: ADR-0001, UI: ADR-0002, Player: here).

**`IPlayerState` is implemented by a small `PlayerStateProvider` class, separate from the `FirstPersonController` `MonoBehaviour` that owns movement/camera/input logic** — `FirstPersonController` updates `PlayerStateProvider`'s backing fields every frame it changes them (position-derived `Velocity`, `IsGrounded` from the `CharacterController`'s own grounded check, `IsCarrying` from Görev/Taşıma's carry state), but `PlayerStateProvider` is the object every consumer actually holds a reference to. This mirrors ADR-0001's "interface separate from the concrete implementation" shape for testability (a test can construct a `PlayerStateProvider` directly and set its fields without needing a live `CharacterController`/`Camera`), while correctly *not* using ADR-0001's static-facade-with-reset pattern — `IPlayerState` is live, per-frame, mutable `MonoBehaviour`-adjacent data, not a session-scoped fact that needs a Domain-Reload-safe reset; there is exactly one player for the whole session, so there is nothing to "reset between sessions" the way Foundation state services need.

**Lookup mechanism**: `PlayerStateProvider` exposes itself via a static accessor, `PlayerStateProvider.Current` — set once when the persistent Player scene's root object initializes (not reset via `[RuntimeInitializeOnLoadMethod]` the way ADR-0001's Foundation services are, since there's no "stale previous session" case to guard against: the Player scene loads exactly once per process lifetime, same as the UI scene). This directly answers the "how do consumers find it" question ADR-0002 flagged as worth deciding centrally rather than letting three ADRs reinvent it separately — `PlayerStateProvider.Current` is this project's second application of that same static-accessor shape, alongside the still-to-be-decided `UIRoot.Instance` from ADR-0002.

**The reference-counted movement lock** (`RequestMovementLock`/`ReleaseMovementLock`) is owned by `PlayerStateProvider` alongside the state fields — backed by a `HashSet<object>` of full-lock holders and a separate `HashSet<object>` of move-only-lock holders (not a single dictionary, to make "most restrictive wins" a simple `_fullLockHolders.Count > 0` check rather than a per-holder scope comparison).

### Architecture Diagram

```
Boot sequence
   │
   ▼
"Player" scene loaded additively (once, at boot, alongside the "UI" scene
from ADR-0002) ── never unloaded, survives every SOFT transition and HARD CUT
   │
   ▼
GameObject "Player" — CharacterController, Camera, FirstPersonController
(movement/input/camera logic), PlayerStateProvider (state + lock bookkeeping)
   │
   ├── FirstPersonController writes: Velocity, IsGrounded, EyeCamera (once),
   │     IsCarrying (mirrored from Görev/Taşıma) — every frame these change
   │
   └── PlayerStateProvider.Current ── static accessor, set once at Player
         scene init, read by every consumer:
           • Etkileşim Sistemi (focus raycast origin, RequestMovementLock)
           • InteractableRegistry / FPC's own approach-taper (position read)
           • Asansör/Kat-Erişim Sistemi (RequestMovementLock, MoveOnly)
           • Sahne Kesmeli Anlatı (RequestMovementLock, Full or MoveOnly)
           • Görev/Taşıma Döngüsü (IsCarrying is written here, not read)

SOFT transition: FirstPersonController's Transform is repositioned to the
target scene's SoftTransitionAnchor (local offset/rotation copied) —
GameObject identity unchanged, PlayerStateProvider.Current unchanged.
```

### Key Interfaces

```csharp
public interface IPlayerState {
    Transform EyeCamera { get; }
    Vector3 Velocity { get; }
    bool IsGrounded { get; }
    bool MovementLocked { get; }
    bool IsCarrying { get; }
    bool IsLocked { get; }
    event Action MovementLockChanged;
}

public sealed class PlayerStateProvider : MonoBehaviour, IPlayerState {
    public static PlayerStateProvider Current { get; private set; }

    void Awake() {
        // Duplicate-instance guard (unity-specialist validation, 2026-08-05 —
        // Debug.Assert alone is insufficient, see Risks) — no
        // [RuntimeInitializeOnLoadMethod] reset needed here, unlike ADR-0001's
        // Foundation services, since the Player scene loads exactly once
        if (Current != null) {
            Debug.LogError("Duplicate PlayerStateProvider detected — destroying this instance.", this);
            Destroy(gameObject);
            return;
        }
        Current = this;
    }

    // IPlayerState backing fields — written by FirstPersonController, not
    // directly by consumers
    public Transform EyeCamera { get; internal set; }
    public Vector3 Velocity { get; internal set; }
    public bool IsGrounded { get; internal set; }
    public bool IsCarrying { get; internal set; }
    public bool MovementLocked => _fullLockHolders.Count > 0 || _moveOnlyLockHolders.Count > 0;
    public bool IsLocked => MovementLocked;   // alias per etkilesim-sistemi.md's pre-check naming
    public event Action MovementLockChanged;

    private readonly HashSet<object> _fullLockHolders = new();
    private readonly HashSet<object> _moveOnlyLockHolders = new();

    public MovementLockScope RequestMovementLock(object requester, MovementLockScope scope = MovementLockScope.Full) {
        bool wasLocked = MovementLocked;
        (scope == MovementLockScope.Full ? _fullLockHolders : _moveOnlyLockHolders).Add(requester);
        if (!wasLocked) MovementLockChanged?.Invoke();
        return EffectiveScope();
    }

    public void ReleaseMovementLock(object requester) {
        bool wasLocked = MovementLocked;
        _fullLockHolders.Remove(requester);
        _moveOnlyLockHolders.Remove(requester);
        if (wasLocked && !MovementLocked) MovementLockChanged?.Invoke();
    }

    // Move is always frozen while any lock is held; Look freezes only if
    // any holder requested Full (most-restrictive-wins). NOTE (TD-ADR review,
    // 2026-08-05): if the same requester calls Request(A, Full) then later
    // Request(A, MoveOnly) without an intervening Release, both entries
    // persist and EffectiveScope() stays Full until ONE Release(A) clears
    // both — "sticky most-restrictive," intentional, not a bug: a requester
    // cannot accidentally loosen its own hold mid-session.
    public MovementLockScope EffectiveScope() =>
        _fullLockHolders.Count > 0 ? MovementLockScope.Full : MovementLockScope.MoveOnly;
}
```

`FirstPersonController` (the movement/camera/input `MonoBehaviour`, not shown in full here — its shape is otherwise already fully specified by `birinci-sahis-kontrolcu.md` and isn't re-litigated by this ADR) holds a reference to its own `PlayerStateProvider` component (same `GameObject`, `GetComponent` once in `Awake`) and writes to its `internal set` fields every frame they change; it reads `EffectiveScope()` each frame to decide whether `Move`/`Look` input is applied.

## Alternatives Considered

### Alternative 1: `FirstPersonController` Implements `IPlayerState` Directly
- **Description**: No separate `PlayerStateProvider` — the `FirstPersonController` `MonoBehaviour` itself implements `IPlayerState` and owns the lock bookkeeping.
- **Pros**: One fewer class; no `internal set` indirection between the component that computes state and the component that exposes it.
- **Cons**: Couples every consumer's contract to the concrete movement/camera/input implementation — a test exercising, say, Etkileşim's `RequestMovementLock` pre-check would need a live `CharacterController`/`Camera`-bearing `MonoBehaviour` in scene; any future change to movement/camera internals risks an accidental `IPlayerState` surface change since they're the same class.
- **Rejection Reason**: `coding-standards.md`'s "dependency injection over singletons... unit-testable" requirement (the same standard ADR-0001 was built to satisfy for Foundation state) applies here too — a separate provider class keeps the *state contract* testable independent of the *movement implementation*. **Precision correction (TD-ADR review, 2026-08-05)**: `PlayerStateProvider` is itself a `MonoBehaviour` (it must be, to live on the player `GameObject` and be found via `GetComponent`) — it is not a plain, `new`-able C# object the way ADR-0001's Foundation-state classes are, and earlier drafts of this ADR overstated the benefit in those terms. The real, narrower benefit this alternative avoids losing is specifically **decoupling from `CharacterController`/`Camera`** — a Unity Test Framework Edit Mode test can `AddComponent<PlayerStateProvider>()` on a bare, unparented `GameObject` and exercise the lock/state logic directly, which is cheap; standing up a functioning `CharacterController`+`Camera`+Input rig to test the same logic through `FirstPersonController` would not be. That is the actual, defensible testability gain — not "plain constructible object," which was never accurate for either class.

### Alternative 2: ADR-0001-Style Static Facade with Reset Hook
- **Description**: Apply ADR-0001's exact pattern — `IPlayerState`/`PlayerState` static facade, reset via `FoundationBootstrap`-style `[RuntimeInitializeOnLoadMethod]`.
- **Pros**: Perfect consistency with ADR-0001's already-established, TD-reviewed pattern; reuses a proven mechanism rather than inventing a new one.
- **Cons**: ADR-0001's reset mechanism exists to solve a **session-scoped, Domain-Reload-sensitive** problem — stale data leaking from a *previous* Play session into a *new* one. `IPlayerState`'s *data* has no equivalent failure mode: there is exactly one player, created once when the Player scene loads (once per process/session, identical to the UI scene in ADR-0002), and it's never meaningfully "reset" mid-session the way `FiredTriggerIds` is. Applying ADR-0001's `FoundationBootstrap`-style reset machinery here would be solving a data-staleness problem that doesn't exist for this data, adding ceremony without benefit.
- **Rejection Reason**: Not a case of inconsistency for its own sake — the *lookup mechanism* (static accessor) is reused from the same family of pattern for a real reason (avoiding three ADRs reinventing lookup, per ADR-0002's own steer), but the full reset *machinery* is correctly not reused, because `IPlayerState` and Foundation session-state solve genuinely different data-staleness problems. **Correction (unity-specialist validation, 2026-08-05)**: this reasoning is right about data staleness but was initially incomplete about *initialization reliability* — ADR-0001 separately discovered that bare `Awake()` is not a reliable once-per-session initialization point regardless of what data is involved, because Unity's "Reload Scene" Enter Play Mode Setting can suppress `Awake()` re-execution on a surviving object independent of any Domain-Reload-style data-freshness question. This ADR's Risks section now carries the equivalent caveat and mitigation ADR-0001 established for its own `Awake()`-time consumers, closing that gap rather than assuming it away.

### Alternative 3: `DontDestroyOnLoad` for the Player `GameObject`
- **Description**: Mark the player object `DontDestroyOnLoad` instead of placing it in a dedicated persistent scene.
- **Pros**: One line, no boot-sequence scene to configure.
- **Cons**: This would be this project's **third** independent rejection of the same pattern for the same underlying reason (ADR-0001 for state, ADR-0002 for UI) — at this point, choosing `DontDestroyOnLoad` for the player specifically would be a real inconsistency a future reader would reasonably ask "why does everything else avoid this but not the player?" The duplicate-instance footgun (same class of risk named in both prior ADRs) also applies here, arguably with higher stakes — an accidentally-duplicated player object is a much more visible, game-breaking bug than a duplicated UI or state object.
- **Rejection Reason**: Consistency with ADR-0001 and ADR-0002's established, twice-validated precedent — reusing the working pattern is simpler to explain and audit than introducing a third mechanism for a problem this project has a clean, working answer to.

## Consequences

### Positive
- `IPlayerState`/the movement lock is genuinely unit-testable in isolation (`AddComponent<PlayerStateProvider>()` on a bare `GameObject` in an Edit Mode test — no live `CharacterController`/`Camera` required, corrected wording per TD-ADR review) — satisfies `coding-standards.md`'s BLOCKING dependency-injection requirement for what is arguably this project's single most-depended-upon piece of state.
- The reference-counted lock's two-`HashSet` design makes "most restrictive scope wins" a trivial O(1) check rather than a per-holder scan, and keeps `Request`/`Release` idempotent-safe (adding an already-present requester to a `HashSet` is a no-op, removing an absent one is a no-op) — matching `architecture.md` Architecture Principle #4 (idempotent public write APIs).
- Third consistent application of the "persistent scene, not `DontDestroyOnLoad`" pattern (state → ADR-0001, UI → ADR-0002, player → here) — a future contributor learns the pattern once and recognizes it everywhere, rather than needing to learn scene-persistence case-by-case.
- The static-accessor lookup (`PlayerStateProvider.Current`) directly answers the lookup-mechanism question ADR-0002 explicitly deferred and flagged as a "don't let 3 ADRs reinvent this" risk — this ADR and ADR-0002's own eventual `UIRoot.Instance` choice can now both point at the same established shape.

### Negative
- Two classes (`FirstPersonController` + `PlayerStateProvider`) instead of one for what is conceptually "the player" — a small ongoing discipline cost: any future change touching player state must remember which class owns the write, which the read-only contract.
- `PlayerStateProvider.Current` being a bare static property (not wrapped in the same interface-injection pattern ADR-0001 used for Foundation services) means production code *can* reach for the static directly rather than accepting it via constructor injection — this ADR does not force DI the way ADR-0001's services technically allow tests to bypass the static entirely; a consumer's own testability still depends on that consumer accepting an `IPlayerState` as a constructor/field parameter rather than reaching for `PlayerStateProvider.Current` inline. This ADR provides the tool (an interface-typed, independently-constructible provider) but doesn't mandate every consumer use it that way — a control-manifest rule is the right place to close this gap, not this ADR.

### Risks
- **Risk (revised, unity-specialist validation, 2026-08-05)**: A future contributor adds a second `PlayerStateProvider` (e.g. accidentally instantiating the Player scene twice, or adding a duplicate component) — since there's no reset/uniqueness enforcement analogous to ADR-0001's `FoundationBootstrap`, `Current` would silently point at whichever instance's `Awake()` ran last. **The original mitigation was insufficient, corrected here**: `Debug.Assert` is compiled out entirely in non-development (shipping) builds (`[Conditional("UNITY_ASSERTIONS")]`), providing zero protection there, and even in Editor/dev builds it only logs — it doesn't stop `Current` from being overwritten immediately after. **Fixed mitigation**: `Awake()` uses an unconditional `Debug.LogError` (always compiled in) paired with corrective action, not just a log — the duplicate instance destroys itself and returns early instead of overwriting `Current`:
  ```csharp
  void Awake() {
      if (Current != null) {
          Debug.LogError("Duplicate PlayerStateProvider detected — destroying this instance.", this);
          Destroy(gameObject);
          return;
      }
      Current = this;
  }
  ```
- **Risk (new, unity-specialist validation)**: `PlayerStateProvider.Current` is set exclusively in `Awake()`, with no reset mechanism — the Decision section argues this is fine because the underlying *data* has no "stale fact from a previous session" failure mode the way Foundation state services do. That reasoning is correct about the data, but incomplete: ADR-0001 independently discovered (and fixed) that Unity's **"Reload Scene" Enter Play Mode Setting** (separate from "Reload Domain") means `Awake()`/`Start()` do not re-run on objects that survive a Play-mode Stop→Play boundary under that setting — if the persistent Player scene is ever "kept resident" across that boundary rather than freshly reloaded by the boot script, `Current` would point at a stale/orphaned instance, the same class of bug ADR-0001 exists to prevent, via a different mechanism. **Mitigation**: the boot script must guarantee the Player (and UI) scenes are always freshly loaded at the start of a Play session, never assumed already-resident; covered by a Validation Criteria test below, mirroring ADR-0001's own Reload-Scene test.
- **Risk (new, unity-specialist validation; given a concrete provisional answer at TD-ADR review, 2026-08-05)**: this project now has **two independent persistent boot scenes** (UI from ADR-0002, Player from this ADR) with no documented load order or readiness signal between them — Unity does not guarantee cross-scene `Awake()` ordering during concurrent additive loads. No known consumer currently needs both `PlayerStateProvider.Current` and `UIRoot`'s eventual equivalent inside its own `Awake()` (the actual race window; confirmed by checking ADR-0002's own worked example, which queries UI in `OnEnable`, not `Awake`), so this is latent, not live. **Provisional mitigation, stated here so the risk has a concrete answer rather than an open-ended one**: the boot sequence loads the **UI scene first, then the Player scene, sequentially awaited** (`await SceneManager.LoadSceneAsync(UI, Additive)` completes before `LoadSceneAsync(Player, Additive)` begins) — arbitrary but consistent, and cheap to state now. This is provisional, not a full boot-sequence ADR; if a genuine same-`Awake()` cross-scene dependency emerges later, a dedicated "Boot Sequence" ADR (not yet in `architecture.md`'s Required ADRs list — should be added there) should own the real contract, including a "BootComplete" readiness signal if needed.
- **Risk (corrected, TD-ADR review, 2026-08-05)**: A consumer calls `RequestMovementLock` twice with the same requester without an intervening `Release`. The **same-scope** case is safe by construction (`HashSet.Add` on an already-present item is a no-op), matching `birinci-sahis-kontrolcu.md`'s Edge Case exactly. The **cross-scope** case (e.g. `Request(A, Full)` then `Request(A, MoveOnly)` without releasing) is *not* a no-op — `A` ends up in both `_fullLockHolders` and `_moveOnlyLockHolders`, and `EffectiveScope()` stays `Full` until a single `Release(A)` clears both. This is now explicitly documented as intentional "sticky most-restrictive" behavior (see Decision/Validation Criteria) rather than left as an undocumented implementation quirk — the original claim that double-`Request` is "safe by construction" in all cases was only accurate for the same-scope subcase.
- **Confirmed non-issue (unity-specialist validation)**: same-`GameObject` `Awake()` ordering between `FirstPersonController` and `PlayerStateProvider` is not guaranteed by Unity, but is not a hazard here — `FirstPersonController.Awake()` uses `GetComponent<PlayerStateProvider>()`, which resolves against component existence, not against whether the target's own `Awake()` has run. No fix needed; noted for a future reader who might otherwise assume an ordering hazard exists.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `birinci-sahis-kontrolcu.md` | `IPlayerState` field list (`EyeCamera`, `Velocity`, `IsGrounded`, `MovementLocked`, `IsCarrying`, `IsLocked`, `MovementLockChanged`) | `PlayerStateProvider` implements this exactly, no fields added or removed |
| `birinci-sahis-kontrolcu.md` | Reference-counted `RequestMovementLock`/`ReleaseMovementLock`, never released by player input, `MovementLockScope { Full, MoveOnly }`, most-restrictive-wins | Two-`HashSet` bookkeeping in `PlayerStateProvider`; player input is never wired to `ReleaseMovementLock` anywhere in `FirstPersonController` by construction (only external systems hold the lock-requester identity) |
| `birinci-sahis-kontrolcu.md` | Double-`RequestMovementLock`-without-`Release` from the same requester is a safe no-op | `HashSet.Add` idempotency (Consequences → Risks) |
| `seviye-sahne-gecisi.md` | Player repositioned to `SoftTransitionAnchor` on SOFT transition, not destroyed/recreated — implies persistence across scene swap | Persistent "Player" scene, loaded once at boot, never unloaded; `FirstPersonController`'s `Transform` is repositioned, `GameObject` identity and `PlayerStateProvider.Current` unchanged |
| `etkilesim-sistemi.md` | `IsLocked` pre-check before attempting a `Hold` interaction (soft-lock avoidance) | `IsLocked` aliases `MovementLocked`, a cheap property read, no allocation |

## Performance Implications
- **CPU**: Negligible — `PlayerStateProvider`'s lock bookkeeping is two small `HashSet<object>` operations per `Request`/`Release` call (rare, interaction-triggered, not per-frame), and `MovementLocked`/`IsLocked` are O(1) count checks read every frame by multiple consumers with no measurable cost.
- **Memory**: Negligible — one additional small component per player (there is exactly one player).
- **Load Time**: One additional persistent scene loaded once at boot (alongside the UI scene from ADR-0002) — sub-frame cost.
- **Network**: N/A.

## Migration Plan
N/A — greenfield, no existing player controller code in the project.

## Validation Criteria
- A `[Test]` (Edit Mode, `AddComponent<PlayerStateProvider>()` on a bare `GameObject` — no `CharacterController`/`Camera` required, corrected wording per TD-ADR review) verifies: two different requesters both holding `Full` locks, one releasing, `MovementLocked` stays `true` until the second releases too; a `MoveOnly` holder alongside a `Full` holder resolves to `Full` (most restrictive wins); same-scope double-`Request` from the same requester without `Release` behaves as a no-op per the GDD's own edge case.
- **(TD-ADR review, 2026-08-05 — new, closes a real edge-case gap)** A `[Test]` verifies the **cross-scope** case: requester `A` calls `Request(A, Full)`, then `Request(A, MoveOnly)` without an intervening `Release` — asserts `EffectiveScope()` stays `Full` (the stricter of the two outstanding entries), asserts a single `Release(A)` clears both entries and fully unlocks (matching the GDD's "one Release clears everything" edge case), and documents this as **intentional "sticky most-restrictive-until-fully-released" behavior**, not an oversight: a requester cannot accidentally loosen its own hold mid-session without a full release/reacquire cycle, which is the safer default for a lock whose entire purpose is preventing exactly this kind of accidental state drift.
- A `[UnityTest]` confirms the Player scene loads once at boot and survives a simulated SOFT transition (position updates to the target `SoftTransitionAnchor`, `PlayerStateProvider.Current` reference stays the same object before and after).
- A `[UnityTest]` with **Reload Scene explicitly disabled** (mirroring ADR-0001's own equivalent test) confirms the Player scene is freshly loaded at the start of each simulated Play session rather than assumed resident — added per unity-specialist validation, 2026-08-05.
- A test confirms the duplicate-instance guard: instantiating a second `PlayerStateProvider` while one already exists results in the second being destroyed and `Current` still pointing at the original — verifies the corrected `Debug.LogError`-and-destroy mitigation actually works, not just the previously-insufficient `Debug.Assert`.

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — this ADR's lookup mechanism borrows its static-accessor shape but deliberately does not borrow its Domain-Reload reset mechanism, for reasons explained in Alternative 2.
- ADR-0002 (UI Framework: UI Toolkit) — this ADR is the second, and now-precedent-setting-as-a-pattern-family, application of "persistent scene loaded once at boot, never `DontDestroyOnLoad`"; also directly answers the "avoid 3 independently-invented lookup mechanisms" concern ADR-0002 raised about its own still-undecided `UIRoot` lookup.
- `docs/architecture/architecture.md` — Module Ownership (`Birinci Şahıs Kontrolcü` row), Data Flow §1 (frame update path begins with this system).
