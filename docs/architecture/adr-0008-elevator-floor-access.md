# ADR-0008: Elevator/Floor-Access System — Cross-Scene State Machine & Composition-Root Self-Registration

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (cross-scene state machine) / Input (direct Interact read) / Animation (door timing) |
| **Knowledge Risk** | LOW-MEDIUM — every wiring pattern this ADR uses (static Foundation-tier state class, `scene_object_self_registration`, `FoundationCompositionRoot`) is already established and Unity-version-neutral. The one net-new engine-specific behavior is a `CharacterController` hard teleport (position set outside `Move()`). |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/physics.md`, `breaking-changes.md`, `deprecated-apis.md`, `docs/architecture/adr-0001-scene-transition-manager.md`, `docs/architecture/adr-0002-first-person-controller.md`, `docs/architecture/adr-0003-session-state.md`, `docs/architecture/adr-0005-lighting-volume-state.md`, `docs/architecture/adr-0006-adaptive-audio.md`, `docs/architecture/adr-0007-interaction-system.md` |
| **Post-Cutoff APIs Used** | None — `SceneManager` additive loading (via `ISceneTransitionManager`), `CharacterController`, and the Input System's generated-class pattern are all pre-6.0 APIs already confirmed stable by ADR-0001/ADR-0002. |
| **Verification Required** | ~~`CharacterController` teleport disable/set/re-enable pattern~~ — **CONFIRMED by unity-specialist review**: correct and sufficient for 6.3; `CharacterController` uses its own move-and-slide sweep, not the `Rigidbody` contact solver, so the 6.0+ solver-iteration change (6→8) does not affect it. No open verification items remain. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Scene Transition — `RequestSoftTransition`/`OnSoftTransitionRejected` via `ISceneTransitionManager`, contract-frozen), ADR-0002 (First-Person Controller — `RequestMovementLock`/`ReleaseMovementLock` via `ISceneTransitionAware`; this ADR further amends it with `IPlayerTeleport` and a self-registration hook), ADR-0003 (Session State — `SessionState.IsSessionActive`, direct read, no interface indirection needed since the producer already exists) |
| **Enables** | Future ADR for Görev/Taşıma Döngüsü (Carry Loop) — the GDD's material transport route (depo ↔ balo salonu) depends on this system existing |
| **Blocks** | Carry Loop *stories* (not its ADR) until this ADR reaches `Accepted` — the Carry Loop ADR may be authored against this ADR's frozen `ElevatorState`/`ElevatorFloorNode` contract immediately, per this project's ADR lifecycle rule |
| **Ordering Note** | Core layer, Batch 2 priority #2. Amends ADR-0002 (`IPlayerTeleport`, `PlayerController` self-registers into `FoundationCompositionRoot` via `OnEnable`/`OnDisable`) and ADR-0006 (`FoundationCompositionRoot` gains `TransitionManager`, `MovementLock`, `PlayerTeleport`, and its first **scene-object self-registration** sub-case for a cross-static dependency — closing the "two-way dependency" gap ADR-0006's Risks flagged, using the project's *existing* registration pattern rather than a new one). Explicitly independent of ADR-0007 (Interaction System) — the GDD requires the call button to bypass `IInteractable` entirely (see Decision → Why not the Interaction System; see also Decision → Shared Interact-Action Arbitration for the one real cross-system input concern, resolved as a content constraint). |

## Context

### Problem Statement
The Elevator/Floor-Access System is the player's only way to move between the depot floor and the ballroom floor. Its call-button state machine (`Idle → Called → DoorsOpening → DoorsOpen → DoorsClosing → Waiting`) must survive the one genuinely hard architectural problem in the GDD: the `Waiting` state spans `RequestSoftTransition`'s asynchronous cross-scene load, meaning the state that entered `Waiting` (on the origin floor's scene) and the state that must exit it (opening doors on the destination floor's scene) are represented by two different, separately-loaded scene objects. This ADR also resolves the GDD's own Open Question #1 (shared vs. per-floor cabin instancing) and closes a "two-way dependency" gap ADR-0006's own Risks section explicitly flagged as unsolved: a static Foundation-tier consumer (this system) needing a reference to a scene-resident `MonoBehaviour` singleton (`PlayerController`) that does not exist yet when `FoundationCompositionRoot.Wire()` runs at `BeforeSceneLoad`.

### Constraints
- The call button must never use `IInteractable`/the Interaction System — GDD Core Rules decision, already reflected in `docs/registry/architecture.yaml`'s `flagged_object_registry` entry (does not list `elevator-floor-access` as a consumer)
- The cabin never physically moves — no platform-delta injection, only `RequestMovementLock(this, MovementLockScope.MoveOnly)` (Move frozen, Look free) plus cosmetic camera-space shake/hum
- Exactly one busy-guard must hold even across two separately-loaded floor scenes' button objects (AC12) — must work correctly even in a PlayMode test that instantiates two `ElevatorFloorNode`s side by side
- `OnSoftTransitionRejected` fires synchronously at the `RequestSoftTransition` call site per ADR-0001's 2026-08-03 revision — `Waiting` must never be entered if the request itself is rejected
- MVP scope: single elevator, single cabin, one call button per floor — no queueing, no multi-cabin dispatch logic
- **New (TD-ADR finding)**: the call button and the Interaction System's focus/SphereCast both read the same shared `Interact` action — their input consumption must not silently double-fire on one press
- **New (TD-ADR finding)**: any composition-root reference to a scene `MonoBehaviour` must have symmetric teardown — a destroyed object left behind an interface-typed static reference does not safely degrade to "null" (Unity's fake-null does not engage through an interface reference), so it must actively unregister, not just register

### Requirements
See `docs/architecture/tr-registry.yaml` for the extracted TR-elevator-* requirements from `design/gdd/asansor-kat-erisim-sistemi.md`, plus consumer-side requirements TR-fpc-011/012 (movement lock) and TR-scene-001/025/026 (soft transition contract).

## Decision

### `ElevatorState` — static Foundation-tier state machine (single source of truth)
Following the precedent established by `SessionState`/`NarrativeState`/`AdaptiveAudioState` (ADR-0003/0004/0006) for session-lifetime cross-scene state, `ElevatorState` is a static plain C# class — not a `MonoBehaviour`, not `DontDestroyOnLoad`:

```csharp
public enum ElevatorPhase { Idle, Called, DoorsOpening, DoorsOpen, DoorsClosing, Waiting }

public static class ElevatorState {
    public static ElevatorPhase CurrentPhase { get; private set; } = ElevatorPhase.Idle;
    public static bool IsBusy => CurrentPhase != ElevatorPhase.Idle;

    // Only entry point for a button press. IsSessionActive is checked by the
    // CALLER (ElevatorFloorNode.OnButtonPressed), not here -- keeps ElevatorState's
    // own dependency surface to ISceneTransitionAware/IPlayerTeleport/ISceneTransitionManager
    // only, no SessionState coupling. Returns false (no-op) if already busy.
    public static bool TryBeginCall(ElevatorFloorNode caller, string destinationFloorId);

    // Called by the currently-ACTIVE ElevatorFloorNode's own local timers/anim
    // callbacks to progress the phase. TD-ADR finding: validated two ways --
    // (1) `caller` must be reference-equal to the node currently registered as
    // active for this ride (rejects a stale/wrong node calling in, logged as an
    // error -- an authoring bug, not player input); (2) `next` must be the legal
    // successor of CurrentPhase per the GDD's own state table (Idle->Called,
    // Called->DoorsOpening, DoorsOpening->DoorsOpen, DoorsOpen->DoorsClosing,
    // DoorsClosing->Idle [nobody boarded] -- DoorsClosing->Waiting is NOT reachable
    // through this overload, only through CompleteDoorsClosing below).
    public static void AdvancePhase(ElevatorFloorNode caller, ElevatorPhase next);

    // Called by the origin ElevatorFloorNode when its local DoorsClosing
    // animation completes with the player aboard. Requests the movement lock,
    // calls RequestSoftTransition, and enters Waiting -- or, if the transition
    // is synchronously rejected, returns to DoorsOpening without ever entering
    // Waiting (matches AC8a).
    public static void CompleteDoorsClosing(ElevatorFloorNode caller);

    public static event Action<ElevatorPhase> PhaseChanged;

    // scene_object_self_registration pattern (ADR-0005/ADR-0007 precedent),
    // OnEnable/OnDisable branch (floor nodes come and go with additive scene
    // load/unload). Duplicate floorId registration is a build-time validation
    // error (per this pattern's established rule 3 in docs/registry/architecture.yaml
    // -- caught at edit time, never silently overwritten at runtime).
    internal static void RegisterFloorNode(string floorId, ElevatorFloorNode node);
    internal static void UnregisterFloorNode(string floorId, ElevatorFloorNode node); // reference-checked

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForNewSession() {
        // TD-ADR finding: must clear everything, not just CurrentPhase, to be
        // safe under Domain-Reload-disabled Editor Play Mode re-entry.
        CurrentPhase = ElevatorPhase.Idle;
        _originFloorId = _destinationFloorId = _activeFloorId = null;
        _floorNodes.Clear();
        PhaseChanged = null;   // ADR-0003's leaked-subscriber convention
        // Defensive: release any lock THIS system might still be holding from a
        // prior session's unfinished ride. Idempotent/safe no-op if not held
        // (ReleaseMovementLock's existing silent-no-op-on-non-owner behavior,
        // ADR-0002). Does not attempt to solve PlayerController's own lock-
        // dictionary reset scope -- that remains ADR-0002/SessionState's concern.
        FoundationCompositionRoot.MovementLock.ReleaseMovementLock(ElevatorLockToken.Instance);
    }

    private static readonly Dictionary<string, ElevatorFloorNode> _floorNodes = new();
    private static string _originFloorId, _destinationFloorId, _activeFloorId;
}

// Named sentinel type (TD-ADR minor note) -- debugger-visible identity for the
// movement-lock requester, instead of an anonymous `new object()`.
internal sealed class ElevatorLockToken {
    internal static readonly ElevatorLockToken Instance = new();
    private ElevatorLockToken() { }
}
```

**Why the phase machine — including the pre-`Waiting` phases — lives here and not on `ElevatorFloorNode`**: AC12 requires the busy-guard to be checked from *any* floor's button, not just the currently-active one. Splitting "local phases live on the MonoBehaviour, only `Waiting` is special-cased into a static class" would mean the busy-guard genuinely only exists while `Waiting` — pressing a second button during `Called`/`DoorsOpening`/`DoorsOpen`/`DoorsClosing` would need a *second*, differently-implemented guard. One state machine, one authority, no special-cased subset. **Confirmed correct by TD-ADR review** as the right tradeoff given the phase enum's low cost.

**Timers stay local, not centralized**: `ArrivalDuration`, `DoorOpenAnim`, `DwellTime`, `DoorCloseAnim` are driven by the currently-active `ElevatorFloorNode`'s own `Update()`/coroutine — that node is guaranteed loaded and alive for the entire `Called → DoorsOpening → DoorsOpen → DoorsClosing` sequence. Only the phase enum, busy-guard, and the `Waiting`-spanning orchestration need to survive the scene boundary.

**Post-arrival symmetry**: after `OnArrival()`, the destination node becomes the new active node and runs the same local `DoorsOpen → (dwell/player-exits) → DoorsClosing` sequence the origin node ran outbound, ending with `AdvancePhase(destinationNode, Idle)` — no second `RequestSoftTransition` is involved, this is a purely local wind-down that makes the destination floor callable again.

### `ElevatorFloorNode` — per-floor MonoBehaviour view/trigger
One instance per floor scene, attached to that floor's own `ElevatorCabin` prefab (see Cabin Instancing below):

```csharp
public class ElevatorFloorNode : MonoBehaviour {
    [SerializeField] string _floorId;
    [SerializeField] string _otherFloorId;       // MVP: exactly one other floor
    [SerializeField] Transform _cabinInteriorAnchor;
    // door Animator, button light, cosmetic shake/hum AudioSource refs omitted (level-design/asset-spec concern)

    void OnEnable()  => ElevatorState.RegisterFloorNode(_floorId, this);
    void OnDisable() => ElevatorState.UnregisterFloorNode(_floorId, this);

    // Reads the shared "Gameplay" Interact action DIRECTLY -- never through
    // IInteractable, per GDD Core Rules. See Decision -> Shared Interact-Action
    // Arbitration for the cross-system input note.
    void OnButtonPressed() {
        if (!SessionState.IsSessionActive) return;                       // diegetic non-response, no state change at all
        if (!ElevatorState.TryBeginCall(this, _otherFloorId)) return;    // busy no-op
        // local timers begin: ArrivalDuration -> ElevatorState.AdvancePhase(this, DoorsOpening) -> ...
    }

    void OnPhaseChanged(ElevatorPhase phase) { /* drives local Animator/light/audio, filtered to phases concerning this node */ }

    // Called once by ElevatorState's onComplete handler on the DESTINATION
    // node specifically -- see Cross-Scene Handoff below.
    internal void OnArrival() { /* plays local DoorsOpen visuals, starts local DwellTime timer */ }
}
```

### Cross-scene handoff (the core mechanism)
1. Origin node's local `DoorsClosing` animation completes with the player aboard → origin node calls `ElevatorState.CompleteDoorsClosing(this)`.
2. `CompleteDoorsClosing` validates `caller` is the active node, then calls `FoundationCompositionRoot.MovementLock.RequestMovementLock(ElevatorLockToken.Instance, MovementLockScope.MoveOnly)`, then `FoundationCompositionRoot.TransitionManager.RequestSoftTransition(originScene, destinationScene, config, OnArrived, OnFailed)`.
3. **Synchronous-rejection path** (matches AC8a): if `RequestSoftTransition` itself triggers `OnSoftTransitionRejected` synchronously, `ElevatorState` immediately calls `ReleaseMovementLock` and transitions back to `DoorsOpening` on the origin node — `Waiting` is never entered.
4. If accepted, `CurrentPhase` becomes `Waiting`, `PhaseChanged` fires — the origin node plays cosmetic shake/hum, which stops the instant `Waiting` ends (`onComplete`/`onFailed`, never a fade).
5. During `Preloading → Ready → Swapping`, the destination scene loads additively; its `ElevatorFloorNode.OnEnable()` self-registers into `ElevatorState`'s floor-node registry **before** `Complete`/`onComplete` fires (**confirmed by unity-specialist**: this guarantee is actually stronger than "before Complete" — every object's `Awake`/`OnEnable` in the target scene completes before `Ready`, since `allowSceneActivation` is always `true`-to-100%, never held at 90%, per ADR-0001's own Decision).
6. `onComplete` (`OnArrived`) looks up the destination floor's registered node. **New (TD-ADR finding, missing edge case)**: if the lookup fails (misconfigured `_floorId`/`_otherFloorId`, or the node was destroyed) — log an error, release the movement lock, force `CurrentPhase = Idle`. This is a content/authoring-bug failure mode, not a reachable state with correctly configured scenes; it must fail safe rather than leave the player permanently locked. On success: calls `FoundationCompositionRoot.PlayerTeleport.Teleport(...)` to the destination node's `CabinInteriorAnchor`, releases the lock, sets the destination node as the new active node, and calls its `OnArrival()`.
7. `onFailed` (`OnFailed`) does the same release + active-node handling, but targets the **origin** node instead (position never changed — the cabin never physically moved, so "returning" is a pure state/UI operation, per GDD Edge Cases "Failed çağrıya Asansör'in tepkisi").

**Edge case, resolved by existing GDD rule (TD-ADR finding, "session ends mid-ride")**: the GDD's own Edge Cases section is explicit — "Eğer `IsSessionActive`, Called/DoorsOpening/DoorsOpen/Waiting sırasında false'a dönerse: Hiçbir etkisi yoktur... Devam eden döngü normal şekilde tamamlanır." A ride already in progress is designed to always complete via `onComplete`/`onFailed`, regardless of `IsSessionActive` changing mid-ride — there is no abort-mid-ride path in the GDD, so this ADR does not add one. The separate concern of a stale lock surviving into a genuinely new session (not the same session's flag flipping) is handled by `ResetForNewSession()`'s defensive `ReleaseMovementLock` call above.

### Cabin Instancing — resolves GDD Open Question #1
Each floor scene contains its own `ElevatorCabin` prefab instance — visually identical, level-designer authored, each with its own door `Animator`, cosmetic shake/hum `AudioSource`, button light, and `CabinInteriorAnchor` child `Transform`. There is no shared "Environment" scene to hang a single object off of — ADR-0001 already rejected that concept for `RenderSettings`/lightmap reasons, and this ADR extends the same reasoning to the cabin. **Authoring note**: per-floor cabin prefabs should be Prefab Variants of one base prefab to prevent visual/behavioral drift between floors as content is authored.

### Wiring — `FoundationCompositionRoot` gains a self-registration sub-case (closes ADR-0006's flagged gap, revised per TD-ADR review)
Two new interface-typed properties, plus `TransitionManager`:

```csharp
public static class FoundationCompositionRoot {
    // ... existing LightingQuery/TransitionSource/FlaggedObjectRegistry ...

    public static ISceneTransitionManager TransitionManager { get; private set; } = new NullSceneTransitionManager();
    public static ISceneTransitionAware MovementLock { get; private set; } = new NullMovementLockController();
    public static IPlayerTeleport PlayerTeleport { get; private set; } = new NullPlayerTeleport();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Wire() {
        // ... existing assignments ...
        TransitionManager = new SceneTransitionManagerAdapter();   // SceneTransitionManager is itself a static class
                                                                     // (ADR-0001) -- no instance-existence timing issue,
                                                                     // safe pull-wire here, same as TransitionSource.
    }

    // TD-ADR finding: NOT a raw property setter. PlayerController is a real
    // MonoBehaviour that can be destroyed (scene reload, PlayMode test teardown);
    // an interface-typed static reference to a destroyed UnityEngine.Object does
    // NOT safely degrade -- Unity's fake-null override does not engage through an
    // interface reference, so a stale reference throws MissingReferenceException
    // on next use instead of falling back to the null-object default. This
    // REUSES the project's existing scene_object_self_registration pattern
    // (ADR-0005/ADR-0007 precedent) instead of inventing a third wiring mechanism.
    internal static void RegisterPlayer(PlayerController player) {
        MovementLock = player;
        PlayerTeleport = player;
    }
    internal static void UnregisterPlayer(PlayerController player) {
        // Reference-checked, same rule as scene_object_self_registration's
        // rule 4 -- only clear if the currently-registered instance is still
        // this one (prevents a stale Unregister from clobbering a newer
        // registration, e.g. in a test harness with two player objects).
        if (ReferenceEquals(MovementLock, player)) MovementLock = new NullMovementLockController();
        if (ReferenceEquals(PlayerTeleport, player)) PlayerTeleport = new NullPlayerTeleport();
    }
}
```

`TransitionManager` keeps the existing pull-direction shape (`Wire()` assigns it) because `SceneTransitionManager` is itself a static class, available at `BeforeSceneLoad` with no timing concern — same reasoning ADR-0001/ADR-0006 already established for `TransitionSource`.

`MovementLock`/`PlayerTeleport` cannot follow that shape: `PlayerController` is a real `MonoBehaviour`, and per ADR-0007's own TD-ADR finding, `Wire()` runs before any scene's `MonoBehaviour`s exist. This ADR closes ADR-0006's flagged "two-way dependency" gap not with a new push-property (which TD-ADR review rejected — no symmetric teardown, unsafe on destroy) but by extending the project's *existing* self-registration convention to a new lifetime direction — amending ADR-0002:

```csharp
// PlayerController.cs (amends ADR-0002)
void OnEnable() {
    FoundationCompositionRoot.RegisterPlayer(this);
}
void OnDisable() {
    FoundationCompositionRoot.UnregisterPlayer(this);
}
```
This gives the composition root one consistent rule, not two incompatible ones: **static-implemented dependencies are assigned in `Wire()`; MonoBehaviour-implemented dependencies self-register via `OnEnable`/`OnDisable`, reference-checked on the way out.** `PlayerController` already implements `ISceneTransitionAware` (defined but unwired in ADR-0002's own Key Interfaces — "not exposed directly"); this ADR is the first real consumer and explicitly amends that "not exposed directly" clause — `ISceneTransitionAware` and the new `IPlayerTeleport` are now globally reachable via `FoundationCompositionRoot` for any future system that needs them (Cutscene's movement lock, most likely), not private to `PlayerController` alone anymore.

**Note on `PlayerController.Awake()`/`OnEnable()` growing amendment stack**: this is now the third ADR to amend `PlayerController`'s bootstrap (ADR-0002 original, ADR-0007's composition-root-adopt fallback in `Awake()`, this ADR's self-registration in `OnEnable()`/`OnDisable()`). Flagged for discoverability, not solved here — if a fourth amendment lands, consolidating `PlayerController`'s own bootstrap into a named method (mirroring `FoundationCompositionRoot`'s own origin story) is worth considering then.

### `IPlayerTeleport` (new interface, amends ADR-0002 — split from `ISceneTransitionAware` per TD-ADR review)
```csharp
public interface IPlayerTeleport {
    void Teleport(Vector3 position, Quaternion rotation);
}
```
**Why a separate interface (TD-ADR finding)**: `ISceneTransitionAware` already carries `RequestMovementLock`/`ReleaseMovementLock`. Adding `Teleport` to the same interface would mean every future holder of `FoundationCompositionRoot.MovementLock` (Cutscene, anything else that only needs to freeze movement) automatically gains the ability to hard-teleport the player — an Interface Segregation violation with real blast radius, since ADR-0002 explicitly scoped `CharacterController` as "never exposed directly" and treats any external position-setting outside `Move()` as exceptional. Splitting into `IPlayerTeleport` means only a consumer that actually needs teleportation (this ADR; a future Cutscene HARD CUT spawn-point placement) depends on it.

`PlayerController` implements both interfaces:
```csharp
void Teleport(Vector3 position, Quaternion rotation) {
    _characterController.enabled = false;
    transform.SetPositionAndRotation(position, rotation);
    _verticalVelocity = 0f;   // unity-specialist finding: reset persistent vertical-velocity/grounded
                              // state, or the player free-falls/snaps on the first Move() after arrival
    _characterController.enabled = true;
}
```
`CharacterController` must be disabled before a raw `Transform` write and re-enabled after (**confirmed correct for Unity 6.3 by unity-specialist review** — `CharacterController` uses its own move-and-slide sweep resolver, unaffected by the 6.0+ `Rigidbody` solver-iteration change). This is the only place in the codebase permitted to move the player by a hard position set rather than `Move()`'s velocity-delta.

### Shared Interact-Action Arbitration (new, TD-ADR finding)
The call button (`ElevatorFloorNode`) and the Interaction System's focus/SphereCast (ADR-0007) both read the same shared `Interact` action independently — they are deliberately decoupled (see below), so neither knows about the other at runtime, and the Input System has no built-in "consumed" flag shared between two independent readers of one action. A single press could theoretically fire both if an `IInteractable` were in SphereCast focus range at the same moment the player is within a call button's trigger-zone. **Resolution: a level-design content constraint, not a runtime arbitration mechanism** — no `IInteractable` may be placed within a call button's trigger-zone radius or inside a cabin interior. This is already effectively guaranteed by the GDD's own "kabin içi tam kapalı hacim" requirement (Visual/Audio Requirements — the cabin interior must be fully enclosed with nothing else present) and is analogous to how ADR-0001 delegated SOFT transition's coordinate-frame alignment to level-design/prefab convention rather than an architectural mechanism. Adding runtime arbitration code here would re-couple the two systems, contradicting the GDD's explicit decoupling decision. New AC14 (below) makes this convention testable at the content level.

### Why not the Interaction System
The GDD is explicit and this ADR does not reopen it: the call button reads the shared `Interact` action directly (`IInteractInput`, ADR-0002) rather than going through `IInteractable`/`InteractableRegistry` (ADR-0007). `docs/registry/architecture.yaml`'s `flagged_object_registry` entry does not list `elevator-floor-access` as a consumer, and this ADR does not add it as one.

### Architecture Diagram
```
        ┌───────────────────────────────────────────┐
        │                  ElevatorState                  │
        │   (static: CurrentPhase, busy-guard,             │
        │    floor-node registry, cross-scene handoff)      │
        └───────────────────────────────────────────┘
           ▲                    ▲                    │
   TryBeginCall /        TransitionManager /    PhaseChanged
   AdvancePhase /        MovementLock /          (drives local
   CompleteDoorsClosing  PlayerTeleport           view/anim)
           │             (FoundationCompositionRoot)  ▼
  ┌──────────────────┐         │             ┌──────────────────┐
  │ ElevatorFloorNode │         │             │ ElevatorFloorNode │
  │  (origin floor)    │         │             │ (destination floor)│
  │  local timers/anim │         │             │  local timers/anim │
  └──────────────────┘         │             └──────────────────┘
                        ┌──────────────────┐
                        │ SceneTransition-  │
                        │ Manager (ADR-0001)│
                        │ PlayerController   │
                        │ (ADR-0002, self-   │
                        │  registers via     │
                        │  OnEnable/OnDisable)│
                        └──────────────────┘
```

### Key Interfaces
```csharp
public enum ElevatorPhase { Idle, Called, DoorsOpening, DoorsOpen, DoorsClosing, Waiting }

public static class ElevatorState {
    public static ElevatorPhase CurrentPhase { get; }
    public static bool IsBusy { get; }
    public static bool TryBeginCall(ElevatorFloorNode caller, string destinationFloorId);
    public static void AdvancePhase(ElevatorFloorNode caller, ElevatorPhase next);
    public static void CompleteDoorsClosing(ElevatorFloorNode caller);
    public static event Action<ElevatorPhase> PhaseChanged;
}

// Amends ADR-0002 -- now has a real consumer:
public interface ISceneTransitionAware {
    void RequestMovementLock(object requester, MovementLockScope scope = MovementLockScope.Full);
    void ReleaseMovementLock(object requester);
}

// NEW, this ADR -- split from ISceneTransitionAware per TD-ADR review (ISP):
public interface IPlayerTeleport {
    void Teleport(Vector3 position, Quaternion rotation);
}

// Amends ADR-0006's FoundationCompositionRoot:
public static class FoundationCompositionRoot {
    public static ISceneTransitionManager TransitionManager { get; }  // NEW, pull-wired in Wire()
    public static ISceneTransitionAware MovementLock { get; }          // NEW, self-registered by PlayerController.OnEnable/OnDisable
    public static IPlayerTeleport PlayerTeleport { get; }               // NEW, self-registered alongside MovementLock
}
```

## Alternatives Considered

### Alternative 1: State lives entirely on the origin floor's `ElevatorFloorNode` instance
- **Description**: Rely on ADR-0001's deferred-unload timing — the origin `MonoBehaviour` instance is still alive through the whole `Waiting` window, so it could hold the phase enum itself.
- **Rejection Reason**: Fails AC12 structurally — the busy-guard needs to be checkable from *any* floor's button-press handler. User confirmed Alternative A (this ADR's chosen design) during assumptions confirmation; TD-ADR review independently confirmed this is the right tradeoff.

### Alternative 2: `DontDestroyOnLoad` singleton GameObject for cabin/state
- **Description**: A single persistent cabin `GameObject` represents the elevator everywhere.
- **Rejection Reason**: Conflicts with this project's established Foundation-tier static-class precedent; reintroduces the "shared Environment object" concept ADR-0001 already rejected.

### Alternative 3: `PlayerController.Instance` static singleton accessor
- **Description**: A plain `public static PlayerController Instance`; `ElevatorState` calls it directly.
- **Rejection Reason**: Reintroduces the "hard singleton lookup" pattern ADR-0002 explicitly rejected for `IFlaggedObjectRegistry`. Superseded during design-decisions confirmation by the composition-root convention — and TD-ADR review's fix (self-registration, not raw push) makes that convention actually safe, reinforcing this rejection.

### Alternative 4 (new, considered during TD-ADR revision): raw property push (`FoundationCompositionRoot.MovementLock = this` in `Awake()`, no teardown)
- **Description**: The originally-drafted approach — `PlayerController.Awake()` assigns itself directly, no unregister path.
- **Pros**: Simplest possible code, one line.
- **Cons**: No symmetric teardown. A destroyed `PlayerController` left behind an interface-typed static reference does not safely become "null" — Unity's fake-null equality override does not engage through an interface-typed reference, so the next call throws `MissingReferenceException` instead of falling back to the null-object default. ADR-0007 (line ~63) already established the project's own rule for exactly this class of bug (Unity object-null comparison, never raw reference-only checks) — this alternative would have silently violated that rule the first time a `PlayerController` was destroyed and recreated (scene reload, PlayMode test teardown).
- **Rejection Reason**: TD-ADR review, BLOCKING finding — replaced with the self-registration pattern (`RegisterPlayer`/`UnregisterPlayer`, reference-checked) that reuses this project's existing, already-safe convention instead of introducing a third, unsafe one.

## Consequences

### Positive
- AC12's cross-floor busy-guard is satisfied structurally (single static authority), not by convention or by the 2-floor MVP's incidental one-scene-loaded-at-a-time behavior
- Closes ADR-0006's explicitly-flagged "two-way dependency" gap using the project's *existing* self-registration convention — one consistent composition-root rule (static → `Wire()`, MonoBehaviour → self-register), not a second incompatible mechanism
- `IPlayerTeleport` is now available for any future system needing a hard position set (e.g., a future Cutscene HARD CUT spawn-point placement) without also granting movement-lock authority
- Timers stay local to whichever `MonoBehaviour` is actually alive and ticking — no static-class `Update()`-polling workaround needed
- `AdvancePhase`'s caller-identity + legal-transition validation makes an authoring bug (wrong node advancing the phase, or an illegal transition) a loud failure instead of silent state corruption

### Negative
- `ElevatorState` now depends on three `FoundationCompositionRoot` properties, two of them (`MovementLock`, `PlayerTeleport`) self-registered by a MonoBehaviour rather than `Wire()`-assigned — a future reader must understand both sub-cases to fully trace the wiring, though there is now only one *rule* to learn ("who is authoritative for the dependency's lifetime"), not two arbitrary mechanisms
- The Shared Interact-Action Arbitration is a content convention, not an enforced runtime invariant — a future level designer could violate it by accident; only AC14's manual/content-audit-level check catches that, not a compile-time or runtime guard

### Risks
- If a future Batch 3 system also needs a MonoBehaviour-to-static self-registration, this ADR's `RegisterPlayer`/`UnregisterPlayer` is the second real instance of the pattern applied to a *singleton* MonoBehaviour (after ADR-0005/0007's *collection*-based registries) — worth confirming a singleton-specific variant (single slot, not a dictionary) generalizes cleanly, flagged for whichever ADR needs it next
- The content-only resolution of Shared Interact-Action Arbitration means a future area with a more complex elevator lobby (multiple interactables near a call button) would need this ADR revisited or a real runtime arbitration mechanism added — acceptable for this MVP's simple 2-floor, sealed-cabin layout, flagged for Full Vision scope growth

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| asansor-kat-erisim-sistemi.md | Call button reads Interact directly, `IsSessionActive` checked at press time, no `IInteractable` | Decision → `ElevatorFloorNode.OnButtonPressed()`, Why not the Interaction System |
| asansor-kat-erisim-sistemi.md | State machine `Idle→Called→DoorsOpening→DoorsOpen→DoorsClosing→Waiting`, second-press no-op, cross-floor busy no-op (AC1-4, AC12) | Decision → `ElevatorState` |
| asansor-kat-erisim-sistemi.md | `RequestMovementLock(this, MoveOnly)` on `DoorsClosing→Waiting`, `Release` on `onComplete`/`onFailed` (AC8, AC9) | Decision → Cross-scene handoff, steps 2 and 6-7 |
| asansor-kat-erisim-sistemi.md | Synchronous `OnSoftTransitionRejected` at request time, `Waiting` never entered on rejection (AC8a) | Decision → Cross-scene handoff, step 3 |
| asansor-kat-erisim-sistemi.md | `onFailed` produces the same recovery as synchronous rejection, cabin returns to origin `DoorsOpening`, no soft-lock (AC10) | Decision → Cross-scene handoff, step 7 |
| asansor-kat-erisim-sistemi.md | Cabin never physically moves, no platform-delta (AC3) | Decision → `ElevatorFloorNode`/`IPlayerTeleport` — position set is an instant swap, not a `Move()`-delta |
| asansor-kat-erisim-sistemi.md | Open Question #1: shared vs. per-floor cabin | Decision → Cabin Instancing |
| asansor-kat-erisim-sistemi.md | `IsSessionActive` flipping mid-ride has no effect, ride completes normally | Decision → Cross-scene handoff, "Edge case, resolved by existing GDD rule" |
| birinci-sahis-kontrolcu.md | `RequestMovementLock`/`ReleaseMovementLock` consumption, `MovementLockScope.MoveOnly` | Decision → Cross-scene handoff; amends ADR-0002 (`ISceneTransitionAware` real consumer, new `IPlayerTeleport`, self-registration) |
| seviye-sahne-gecisi.md | `RequestSoftTransition`/`onComplete`/`onFailed`/`OnSoftTransitionRejected` contract | Decision → Wiring, Cross-scene handoff |
| etkilesim-sistemi.md | Shared `Interact` action, no runtime double-fire with the call button | Decision → Shared Interact-Action Arbitration |

## Performance Implications
- **CPU**: negligible — one dictionary lookup on arrival (`_floorNodes[destinationFloorId]`), no per-frame cost from `ElevatorState` itself (all ticking stays on the locally-active `ElevatorFloorNode`)
- **Memory**: negligible — `_floorNodes` holds at most as many entries as floors are simultaneously loaded (1-2 during the co-residency window)
- **Load Time**: N/A — piggybacks entirely on ADR-0001's existing `RequestSoftTransition` timing
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system. Amends ADR-0002 (`IPlayerTeleport`, `PlayerController` self-registers into `FoundationCompositionRoot` via `OnEnable`/`OnDisable` — this is the third ADR to amend `PlayerController`'s bootstrap, flagged for future consolidation if a fourth lands) and ADR-0006's `FoundationCompositionRoot` (new `TransitionManager`/`MovementLock`/`PlayerTeleport` properties, plus its first scene-object self-registration sub-case) — all amendments are additive, no existing decision in either ADR changes.

## Validation Criteria
- AC1 through AC14 from `asansor-kat-erisim-sistemi.md` (AC14 new, this ADR — see below), implemented as automated EditMode/PlayMode tests per this project's Logic-tier test-evidence rules
- **AC14 (new, TD-ADR finding)**: content-audit-level check (not a runtime test) — no `IInteractable` exists within any elevator call button's trigger-zone radius or inside a cabin interior, verified via `/asset-audit` or manual level review before an area is marked content-complete
- AC12 specifically: PlayMode test instantiating two `ElevatorFloorNode`s — press floor B's button while `ElevatorState.IsBusy` is true from floor A's call, assert no-op (no phase change, `PhaseChanged` does not fire)
- AC8a specifically: mock `ISceneTransitionManager` that synchronously invokes `OnSoftTransitionRejected` inside `RequestSoftTransition` — assert `ElevatorState.CurrentPhase` never observably becomes `Waiting` and the movement lock is released within the same call
- New test: `FoundationCompositionRoot.MovementLock`/`PlayerTeleport` left at their null-object defaults — `ElevatorState.CompleteDoorsClosing()` does not throw
- New test (TD-ADR finding): `UnregisterPlayer` is reference-checked — registering player B, then calling `UnregisterPlayer(playerA)` (a stale/already-superseded reference) does NOT clear `MovementLock`/`PlayerTeleport` away from player B
- New test (TD-ADR finding): `AdvancePhase` called with a `caller` that is not the currently-active node is rejected (logged error, no state change); called with an illegal `next` phase throws
- New test: `Teleport()` disables `CharacterController` before the position write, resets vertical-velocity/grounded state, and re-enables the controller after

## Related Decisions
- Amends ADR-0002 (`IPlayerTeleport`, `PlayerController` self-registers `ISceneTransitionAware`/`IPlayerTeleport` into `FoundationCompositionRoot` via `OnEnable`/`OnDisable`)
- Amends ADR-0006 (`FoundationCompositionRoot` gains `TransitionManager`/`MovementLock`/`PlayerTeleport`, and its first scene-object self-registration sub-case for a cross-static dependency — closes the "two-way dependency" gap flagged in ADR-0006's own Risks, using the existing `scene_object_self_registration` convention rather than a new mechanism)
- Depends on ADR-0001's `RequestSoftTransition`/`OnSoftTransitionRejected` contract (contract-frozen, consumed as specified)
- Enables the future Görev/Taşıma Döngüsü (Carry Loop) ADR
- Explicitly independent of ADR-0007 (Interaction System) for the interactable registry — see Decision → Why not the Interaction System; the one real cross-system concern (shared Interact action) is resolved as a content constraint, see Decision → Shared Interact-Action Arbitration
