# ADR-0011: Elevator State Machine

> **Unity Specialist Validation**: BLOCKING (2 findings, found and fixed) 2026-08-07 — (1) `OnTriggerEnter`/`OnTriggerExit` compared the *entering* collider (always the player's) against the controller's own trigger-zone collider fields — permanently false, so button-range/cabin-boarding detection could never fire; and Unity delivers trigger callbacks only to scripts on the same `GameObject` as a contacting collider, never to a parent, so the callbacks wouldn't even be invoked with the zones on child objects. Fixed with an `ElevatorTriggerZoneRelay` child component + `CompareTag("Player")` identity check. (2) The handoff relevance check (`OriginScene == mine OR DestinationScene == mine`) was unsound — `OriginScene` was never cleared, so during the SOFT transition's guaranteed 0.5-2s co-residency window BOTH floors' controllers matched simultaneously, double-ticking `_elapsed` and racing two different `_playerInCabin` readings on the GDD's boarding check. Fixed with a single `ActiveFloorScene` property reassigned exactly once at handoff (`OnTransitionComplete()`). Also fixed 4 MINOR findings: a `readonly`-field reset stub whose stated intent wouldn't compile (replaced with in-place `ResetOnLoad()`, which also fixes the stale-subscription hazard of wholesale state replacement); the missing Reload-Scene stale-subscription Risk/test every prior ADR-0001-pattern consumer documents; `_thisFloorSceneName` hand-authored via Inspector (now derived from `gameObject.scene.name`); and the unverified `default` `SoftTransitionConfig` at `RequestSoftTransition`'s first-ever real call site (flagged, not assumed). One further blocking-class bug was self-caught before review: the `OnSoftTransitionRejected` subscription originally happened *after* `RequestSoftTransition` — but that event fires synchronously *inside* the call, so the handler could never catch a rejection; fixed by subscribing before and tracking the outcome with an explicit closure flag.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-07 — 1 major design-scope finding, resolved by user decision: the shared `Tick()` cycle would have silently started an **automatic return ride with no button press** if the player was still in the cabin when the arrival-side `DoorsClosing` completed — behavior the GDD's Core Rules ("Geçerli basışta Called"), Edge Cases, and ACs never anticipate, decided unilaterally in a code comment rather than surfaced. User confirmed (AskUserQuestion): no auto-return — added an `_isArrivalLeg` guard so arrival-side cycles always wind down to `Idle`. 3 further findings, fixed: (1) `OnIdleReached` fired *after* `ActiveFloorScene` was cleared, so every controller's relevance check was false at the one moment the relevant floor needed to react (stop shake/hum) — reordered to invoke-then-clear; (2) `OnIdleReached`'s documented "a ride actually happened" bool was hardcoded `false` at its only call site, wrong for the post-arrival wind-down — now computed from `_isArrivalLeg`; (3) a Validation Criteria bullet asserted `ActiveFloorScene == OriginScene` throughout the pre-`Waiting` phases, but `OriginScene` is `null` until `SetTransition()` runs at the end of `DoorsClosing` — a test written to that bullet would have failed against the actual code; narrowed to the true invariant. Also flagged (minor): the pending Step-6 registry updates weren't noted anywhere — added to Migration Plan. Verified clean otherwise: registry consistency, no stale references to any pre-fix shape after the extensive unity-specialist revisions, layering, movement-lock requester-identity discipline, and the narrowly-scoped `OnSoftTransitionRejected` subscription's justification.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-07

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (Physics trigger zones, Input System, cross-scene shared state) |
| **Knowledge Risk** | LOW — `Collider` trigger events (`OnTriggerEnter`/`OnTriggerExit`), the new Input System's `WasPressedThisFrame`, and plain C# static-facade state are all already-validated Unity 6.3 APIs and project patterns (ADR-0001, ADR-0003, ADR-0010). No new engine mechanism introduced. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md`, `docs/architecture/adr-0003-player-state-and-movement-lock.md`, `docs/architecture/adr-0006-session-state-service-and-round-counter-ownership.md`, `docs/architecture/adr-0008-scene-transition-state-machine.md`, `docs/architecture/adr-0010-interaction-state-machine.md`, `docs/architecture/adr-0012-dialogue-callback-selection-timing.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | The handoff-tickable shared state machine (Decision, below) — that ticking responsibility correctly transfers from the origin floor's `ElevatorController` to the destination floor's, exactly once, with no frame where both or neither tick it — should be exercised in Play mode with both floor scenes able to be inspected around the swap boundary. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (static-facade persistence pattern) — the shared `ElevatorSystem` state. ADR-0003 (Player State) — `RequestMovementLock`/`ReleaseMovementLock`/`MovementLockScope.MoveOnly`. ADR-0006 (Session State) — `GeceOturumDurumu.Instance.IsSessionActive`. ADR-0008 (Scene Transition) — `RequestSoftTransition`/`OnSoftTransitionRejected`. |
| **Enables** | Any story implementing `Görev/Taşıma Döngüsü`'s depot↔ballroom carry route, which the elevator's own Dependencies section states is entirely blocked on this system existing. |
| **Blocks** | Any story implementing `Asansör/Kat-Erişim Sistemi` itself. |
| **Ordering Note** | None beyond the four Depends-On ADRs already being Accepted-pending. |

## Context

### Problem Statement

`asansor-kat-erisim-sistemi.md` (carries a "Needs Revision" header artifact from an earlier review round, like most GDDs in this project — not a blocker per this session's established precedent) fully specifies a call-button-driven cabin state machine (`Idle→Called→DoorsOpening→DoorsOpen→DoorsClosing→Waiting`) that moves the player between the depot and ballroom floor scenes via `Seviye/Sahne Geçişi`'s `RequestSoftTransition`, with a deliberately non-physical cabin (no real platform movement — purely cosmetic camera shake/hum during `Waiting`) and a call button that bypasses `Etkileşim Sistemi`/`IInteractable` entirely in favor of a direct trigger-zone + Input System read.

This ADR resolves two things the GDD leaves open: **(1)** the concrete Unity implementation mechanism (state machine structure, testability split, per `coding-standards.md`'s BLOCKING unit-test rule), and **(2)** the GDD's own explicitly-flagged Open Question #1 — whether the cabin is a single shared object or a separate per-floor instance, whose target owner the GDD names as "technical-director... via an ADR." Open Question #2 (a Unity 6.3 RenderGraph/multi-scene camera-stacking spike) is explicitly a Seviye/Sahne Geçişi-owned technical-verification item, not an architectural decision this ADR makes — noted in Risks, not resolved here.

### Constraints

- Must not deviate from `asansor-kat-erisim-sistemi.md`'s already-Approved Core Rules, States and Transitions, and Edge Cases — this ADR formalizes, it does not redesign. In particular: the call button never uses `IInteractable`; `IsSessionActive` is read only at the moment of button press, never polled mid-sequence; a second press while busy is a pure no-op; `OnSoftTransitionRejected` fires only synchronously at request time (per `seviye-sahne-gecisi.md`'s own 2026-08-03 revision), never mid-`Waiting`; player-boarded is checked only at the exact instant `DoorsClosing` completes, not earlier.
- Must reuse `PlayerStateProvider.Current.RequestMovementLock(this, MovementLockScope.MoveOnly)`/`ReleaseMovementLock(this)` (ADR-0003) verbatim — `Look` stays free, only `Move` freezes.
- Must reuse `GeceOturumDurumu.Instance.IsSessionActive` (ADR-0006) and `SceneTransitionManager.Instance.RequestSoftTransition`/`OnSoftTransitionRejected` (ADR-0008) verbatim — no new session-state or transition mechanism.
- The state-machine logic must live in a plain, Unity-decoupled C# class, separate from any `MonoBehaviour`, per `coding-standards.md`'s BLOCKING unit-test rule — same testability split ADR-0003/ADR-0010 already established.

### Requirements

- Since the depot and ballroom floors are separate Scenes swapped via `RequestSoftTransition` (only one loaded as the "current" gameplay scene at a time, per `seviye-sahne-gecisi.md`'s own scene model), and the GDD requires a busy cabin to lock out *every other floor's* button (AC12) even though that other floor's `GameObject`s may not exist yet, the cabin's logical state must be visible cross-scene, in a form that survives whichever floor's own scene is currently loaded or unloaded.
- The `RequestMovementLock`/`ReleaseMovementLock` pair must be requested and released by the exact same object reference (ADR-0003's reference-counted API is keyed by requester identity) — whichever `ElevatorController` instance initiates the ride must also be the one that releases the lock, even though the *destination* floor's own controller instance is a different object entirely.

## Decision

### Resolving Open Question #1: cabin is a separate per-floor prefab instance, with cross-scene state shared via an ADR-0001 static facade

**Confirmed by the user (`AskUserQuestion`, 2026-08-07)**: each floor's own scene contains its own visually-identical cabin prefab instance (no shared "Environment" persistent scene exists to host a single physical cabin — `seviye-sahne-gecisi.md`'s own design-review already abandoned that idea, per this GDD's own Open Question #1 update note). The **logical** single-cabin state (which state the ride is in, which floor called, which floor is the destination, busy/idle) is shared across both floor instances via a plain `ElevatorSystem` static facade (ADR-0001 pattern) — the same mechanism this project already uses for any fact that must survive a scene swap, rather than a fifth persistent scene (ADR-0008/ADR-0009's "Foundation" scene precedent) that this system has no genuine MonoBehaviour/timed-Unity-object need for beyond what the facade + two per-floor prefab instances already cover.

### Handoff-tickable shared state machine: one pure C# state machine, driven by whichever floor's `ElevatorController` is currently relevant

**The distinctive mechanism this ADR introduces**: the pure C# `ElevatorStateMachine` instance lives *inside* the shared `ElevatorSystem` static facade, not owned by either floor's `MonoBehaviour`. Each floor's `ElevatorController.Update()` checks whether *its own* scene equals a **single** authoritative `ActiveFloorScene` property — only the currently-relevant controller calls `Tick()`, so responsibility for advancing the ride correctly transfers from the origin floor's `ElevatorController` to the destination floor's over the ride's lifetime, with no persistent "elevator" `MonoBehaviour` ever needing to survive the scene swap itself. This is a different shape from, but the same underlying discipline as, ADR-0012's `gameObject.scene`-identity check: **a consumer of shared cross-scene state must verify the state is actually relevant to it before acting, rather than assuming its own aliveness implies relevance.**

**Corrected during unity-specialist review (2026-08-07) — an earlier draft's relevance check was unsound**: the original sketch checked `OriginScene == mine OR DestinationScene == mine`, reasoning that since a ride's origin and destination floors are never the same floor, exactly one of the two loaded controllers could ever match. That reasoning is true but doesn't establish what it was used for — `OriginScene` was never cleared for the lifetime of a ride (including its destination-side leg), so during the SOFT transition's co-residency window (guaranteed to last 0.5-2s past `Complete`, per ADR-0008's `DelayedUnload` — comfortably inside a 5s+1.5s destination-side `DoorsOpen`/`DoorsClosing` cycle at this ADR's default Tuning Knobs), **both** floors' `OriginScene == mine`/`DestinationScene == mine` checks were simultaneously true, so both controllers called `Tick()` every frame — double-advancing `_elapsed`, and worse, each reading its own stale/live `_playerInCabin` independently for the GDD's boarding check, with the outcome depending on Unity's unspecified same-frame cross-scene script-execution order. **Fixed**: replaced the two-field OR check with a single `ActiveFloorScene` property, explicitly reassigned exactly once at the moment ticking responsibility actually hands off (`OnTransitionComplete()`, below) — only one controller can ever match a single value at a time, by construction, not by an informal same-ride-different-floors argument.

```csharp
public enum ElevatorState { Idle, Called, DoorsOpening, DoorsOpen, DoorsClosing, Waiting }

// Plain C# class, no Unity types — testable via [Test] driving Tick()/
// TryCall()/EnterWaiting()/OnTransitionComplete()/OnTransitionFailed()
// directly with synthetic inputs, per coding-standards.md's BLOCKING
// unit-test rule. Same testability split ADR-0003/ADR-0010 established.
public sealed class ElevatorStateMachine {
    public ElevatorState CurrentState { get; private set; } = ElevatorState.Idle;
    // The SINGLE floor currently responsible for calling Tick() — the one
    // source of truth both floors' ElevatorController instances check for
    // relevance (see corrective note above; replaces an earlier draft's
    // unsound OR-of-two-fields check).
    public string ActiveFloorScene { get; private set; }
    public string OriginScene { get; private set; }        // this in-flight leg's departure floor
    public string DestinationScene { get; private set; }   // this in-flight leg's arrival floor

    // Fired for the ElevatorController driving the current phase to render
    // door/light visuals and cosmetic shake/hum locally — this class never
    // touches UnityEngine types directly.
    public event Action OnDoorsOpeningStarted;
    public event Action OnDoorsOpen;
    public event Action OnDoorsClosingStarted;
    public event Action<bool> OnIdleReached;                     // bool: a ride actually happened (someone boarded)
    // Fired exactly once per ride, when the ORIGIN-side DoorsClosing
    // completes WITH a boarded player, carrying the departure floor —
    // the receiving controller (whose own floor must equal this value,
    // since it's always ActiveFloorScene at the moment of firing)
    // resolves the destination floor itself (MVP: the other of the two
    // floors) and calls SetTransition() before requesting the swap.
    // NEVER fires on the arrival-side DoorsClosing — see _isArrivalLeg
    // below (user decision via AskUserQuestion, TD-ADR review 2026-08-07:
    // no automatic return ride; every ride begins with an explicit
    // button press, per GDD Core Rules' "Geçerli basışta Called").
    public event Action<string> OnReadyForTransition;            // (departureFloor)

    private float _elapsed;
    // True from OnTransitionComplete() until the arrival-side cycle winds
    // down to Idle — suppresses the boarding check on the arrival side.
    // Added during TD-ADR review (2026-08-07): without it, a player still
    // standing in the cabin when the arrival-side DoorsClosing completed
    // would trigger an automatic return ride with no button press — a
    // behavior the GDD's Core Rules ("her yolculuk bir basışla başlar"),
    // Edge Cases, and ACs never anticipate. User confirmed: no auto-return.
    private bool _isArrivalLeg;

    public bool TryCall(string floorScene, bool isSessionActive) {
        if (CurrentState != ElevatorState.Idle) return false;    // busy — no-op (GDD AC4/AC12)
        if (!isSessionActive) return false;                      // GDD AC2 — read only at press time
        ActiveFloorScene = floorScene;
        CurrentState = ElevatorState.Called;
        _elapsed = 0f;
        _isArrivalLeg = false;
        return true;
    }

    public void Tick(float deltaTime, bool playerInCabin,
                      float arrivalDuration, float doorOpenAnim, float dwellTime, float doorCloseAnim) {
        _elapsed += deltaTime;
        switch (CurrentState) {
            case ElevatorState.Called:
                if (_elapsed >= arrivalDuration) { CurrentState = ElevatorState.DoorsOpening; _elapsed = 0f; OnDoorsOpeningStarted?.Invoke(); }
                break;
            case ElevatorState.DoorsOpening:
                if (_elapsed >= doorOpenAnim) { CurrentState = ElevatorState.DoorsOpen; _elapsed = 0f; OnDoorsOpen?.Invoke(); }
                break;
            case ElevatorState.DoorsOpen:
                if (_elapsed >= dwellTime) { CurrentState = ElevatorState.DoorsClosing; _elapsed = 0f; OnDoorsClosingStarted?.Invoke(); }
                break;
            case ElevatorState.DoorsClosing:
                if (_elapsed >= doorCloseAnim) {
                    // GDD Edge Cases: boarding checked at THIS exact
                    // instant, not when dwell expired or DoorsClosing
                    // began. Arrival-side cycles never re-arm a ride
                    // (no auto-return — see _isArrivalLeg above).
                    if (playerInCabin && !_isArrivalLeg) {
                        OnReadyForTransition?.Invoke(ActiveFloorScene);
                        // The handler is expected to call SetTransition()
                        // then EnterWaiting() or RejectedSynchronously()
                        // before returning.
                    } else {
                        // Corrected during TD-ADR review (2026-08-07): an
                        // earlier draft (1) cleared ActiveFloorScene BEFORE
                        // invoking OnIdleReached, so every controller's
                        // "ActiveFloorScene == mine" relevance check was
                        // false at the exact moment the one relevant floor
                        // needed to react (stop shake/hum, finalize door
                        // visuals) — the only event in this design where
                        // the stated relevance discipline couldn't work as
                        // described; and (2) hardcoded the bool to false,
                        // making the documented "a ride actually happened"
                        // parameter permanently dead and wrong for the
                        // post-arrival wind-down (a ride DID happen).
                        // Fixed: invoke first, clear after, compute the bool.
                        CurrentState = ElevatorState.Idle;
                        OnIdleReached?.Invoke(_isArrivalLeg);
                        ActiveFloorScene = null; OriginScene = null; DestinationScene = null;
                        _isArrivalLeg = false;
                    }
                }
                break;
            // Waiting is exited only via EnterWaiting()'s later callers
            // (OnTransitionComplete/OnTransitionFailed), never by Tick()'s
            // own timer — matches GDD States and Transitions exactly.
        }
    }

    // Called by the controller whose floor == ActiveFloorScene, right
    // after OnReadyForTransition fires, before requesting the swap.
    public void SetTransition(string origin, string destination) {
        OriginScene = origin;
        DestinationScene = destination;
    }

    public void EnterWaiting() {
        CurrentState = ElevatorState.Waiting;
        _elapsed = 0f;
    }

    // GDD Edge Cases: a synchronous OnSoftTransitionRejected means Waiting
    // was never really entered — cabin returns straight to DoorsOpening at
    // the origin, movement lock never net-held. ActiveFloorScene is left
    // unchanged (still OriginScene) — the ride never left this floor.
    public void RejectedSynchronously() {
        CurrentState = ElevatorState.DoorsOpening;
        _elapsed = 0f;
        OnDoorsOpeningStarted?.Invoke();
    }

    public void OnTransitionComplete() {
        // Swap succeeded — ticking responsibility hands off to the
        // destination floor HERE, exactly once (the fix for the double-
        // tick bug described above). Arrival plays the same DoorsOpen
        // dwell/close cycle a fresh call would (GDD States and
        // Transitions: Waiting → DoorsOpen), but flagged as the arrival
        // leg — its DoorsClosing always winds down to Idle, never
        // re-arming a ride (no auto-return; TD-ADR review 2026-08-07).
        ActiveFloorScene = DestinationScene;
        CurrentState = ElevatorState.DoorsOpen;
        _elapsed = 0f;
        _isArrivalLeg = true;
        OnDoorsOpen?.Invoke();
    }

    public void OnTransitionFailed() {
        // GDD Edge Cases, "Failed çağrıya Asansör'in tepkisi" — identical
        // reaction to a synchronous rejection: cabin returns to the ORIGIN
        // floor's DoorsOpening, not the destination's. ActiveFloorScene
        // unchanged — the cabin never actually left.
        CurrentState = ElevatorState.DoorsOpening;
        _elapsed = 0f;
        OnDoorsOpeningStarted?.Invoke();
    }

    // Called only by ElevatorSystemState.ResetOnLoad() — resets fields to
    // their Idle defaults IN PLACE, without discarding this instance, so
    // already-subscribed event handlers survive a FoundationBootstrap.
    // ResetAll() reset instead of being silently orphaned (see the
    // matching Risk bullet in Consequences).
    internal void ResetOnLoad() {
        CurrentState = ElevatorState.Idle;
        ActiveFloorScene = null;
        OriginScene = null;
        DestinationScene = null;
        _elapsed = 0f;
        _isArrivalLeg = false;
    }
}
```

```csharp
public interface IElevatorSystemState {
    ElevatorState CurrentState { get; }
    string ActiveFloorScene { get; }
    string OriginScene { get; }
    string DestinationScene { get; }
    event Action OnDoorsOpeningStarted;
    event Action OnDoorsOpen;
    event Action OnDoorsClosingStarted;
    event Action<bool> OnIdleReached;
    event Action<string> OnReadyForTransition;
    bool TryCall(string floorScene, bool isSessionActive);
}

// Plain C# class + interface + static facade, reset via
// FoundationBootstrap.ResetAll() — same pattern as every other
// session-scoped state in this project (ADR-0001). This system's
// SECOND Core-layer consumer of the pattern (ADR-0012's
// DiyalogAnlatiIcerigi was the first) — following, not setting, that
// precedent this time. No constructor-time subscriptions — this state
// is driven entirely by explicit method calls from whichever
// ElevatorController is currently relevant, never by its own event
// subscription to another Foundation service.
//
// Corrected during unity-specialist review (2026-08-07): _machine is
// held for the lifetime of this facade and reset IN PLACE
// (ElevatorStateMachine.ResetOnLoad()), never replaced wholesale. An
// earlier draft replaced ElevatorSystem's whole _state field on reset —
// since this facade's events forward directly onto _machine's own event
// fields (the add/remove accessors below), replacing the backing
// instance would silently orphan any ElevatorController already
// subscribed at reset time (the same "Reload Scene: Off, Awake/OnEnable
// doesn't re-run across a Play Stop→Play boundary" stale-subscription
// hazard ADR-0001/0003/0008/0010 already document) — see the matching
// Risk bullet in Consequences.
public sealed class ElevatorSystemState : IElevatorSystemState {
    private readonly ElevatorStateMachine _machine = new();

    public ElevatorState CurrentState => _machine.CurrentState;
    public string ActiveFloorScene => _machine.ActiveFloorScene;
    public string OriginScene => _machine.OriginScene;
    public string DestinationScene => _machine.DestinationScene;
    public event Action OnDoorsOpeningStarted { add => _machine.OnDoorsOpeningStarted += value; remove => _machine.OnDoorsOpeningStarted -= value; }
    public event Action OnDoorsOpen { add => _machine.OnDoorsOpen += value; remove => _machine.OnDoorsOpen -= value; }
    public event Action OnDoorsClosingStarted { add => _machine.OnDoorsClosingStarted += value; remove => _machine.OnDoorsClosingStarted -= value; }
    public event Action<bool> OnIdleReached { add => _machine.OnIdleReached += value; remove => _machine.OnIdleReached -= value; }
    public event Action<string> OnReadyForTransition { add => _machine.OnReadyForTransition += value; remove => _machine.OnReadyForTransition -= value; }

    public bool TryCall(string floorScene, bool isSessionActive) => _machine.TryCall(floorScene, isSessionActive);

    // Internal-only surface — reached via ElevatorSystem.InternalInstance
    // by whichever ElevatorController is currently relevant (ADR-0006/
    // ADR-0009/ADR-0012's established InternalInstance escape-hatch shape).
    internal void Tick(float dt, bool playerInCabin, float arr, float open, float dwell, float close) => _machine.Tick(dt, playerInCabin, arr, open, dwell, close);
    internal void SetTransition(string origin, string destination) => _machine.SetTransition(origin, destination);
    internal void EnterWaiting() => _machine.EnterWaiting();
    internal void RejectedSynchronously() => _machine.RejectedSynchronously();
    internal void OnTransitionComplete() => _machine.OnTransitionComplete();
    internal void OnTransitionFailed() => _machine.OnTransitionFailed();
    internal void ResetOnLoad() => _machine.ResetOnLoad();
}

public static class ElevatorSystem {
    // Never replaced — see ElevatorSystemState's own corrective comment
    // above for why in-place reset is required here, not field replacement.
    private static readonly ElevatorSystemState _state = new();
    public static IElevatorSystemState Instance => _state;
    internal static ElevatorSystemState InternalInstance => _state;
    internal static void ResetOnLoad() => _state.ResetOnLoad();  // registered in FoundationBootstrap.ResetAll()
}
```

### Call button: trigger-zone + direct Input System read, per floor

**Corrected during unity-specialist review (2026-08-07) — an earlier draft's trigger detection never actually fired**: `OnTriggerEnter`/`OnTriggerExit` compared the *entering* `Collider` (always the player's own collider) against `ElevatorController`'s own trigger-zone `Collider` fields — a collider is never equal to itself's containing trigger, so `_playerInButtonRange`/`_playerInCabin` could never become `true`. Separately, Unity only delivers trigger callbacks to a script on the *same* `GameObject` as one of the two colliders in contact, never to a parent — so if the two differently-shaped/sized zones live on child `GameObject`s (as their distinct shapes require), `ElevatorController` would never receive the callback at all regardless. **Fixed**: a small relay component on each trigger-zone child `GameObject` forwards the callback up, and the check identifies the *player*, not the zone itself:

```csharp
// Placed on each trigger-zone child GameObject (button-range sphere,
// cabin-interior box) — Unity delivers OnTriggerEnter/Exit only to
// scripts on the same GameObject as one of the two colliders in contact,
// never to a parent, so this cannot live directly on ElevatorController.
public sealed class ElevatorTriggerZoneRelay : MonoBehaviour {
    public event Action<Collider> Entered;
    public event Action<Collider> Exited;
    private void OnTriggerEnter(Collider other) => Entered?.Invoke(other);
    private void OnTriggerExit(Collider other) => Exited?.Invoke(other);
}
```

```csharp
// Lives in each floor's OWN scene, one instance per floor — NOT a
// persistent-scene singleton (there are exactly two, one per floor, and
// exactly one is ever loaded at a time under normal play, briefly both
// during a SOFT transition's co-residency window before DelayedUnload).
public sealed class ElevatorController : MonoBehaviour {
    [SerializeField] private ElevatorTriggerZoneRelay _buttonRangeZone;   // ~1.5m radius, isTrigger
    [SerializeField] private ElevatorTriggerZoneRelay _cabinInteriorZone; // separate zone — GDD Edge Cases needs
                                                                            // "is the player physically inside the
                                                                            // cabin" independent of button proximity
    [SerializeField] private InputActionReference _interactAction;
    [SerializeField] private float _arrivalDuration = 4f, _doorOpenAnim = 1.5f, _dwellTime = 5f, _doorCloseAnim = 1.5f;  // Tuning Knobs

    // Corrected during unity-specialist review (2026-08-07): an earlier
    // draft hand-authored this via [SerializeField], risking silent
    // desync from the prefab's actual containing Scene (a copy-paste
    // error between the two floor prefabs would go uncaught) and weakening
    // this ADR's own claim of sharing ADR-0012's real-Scene-identity
    // discipline. Derived from the engine's own ground truth instead.
    private string _thisFloorSceneName;
    private bool _playerInButtonRange;
    private bool _playerInCabin;

    private void Awake() {
        _thisFloorSceneName = gameObject.scene.name;
        _buttonRangeZone.Entered += other => { if (IsPlayer(other)) _playerInButtonRange = true; };
        _buttonRangeZone.Exited += other => { if (IsPlayer(other)) _playerInButtonRange = false; };
        _cabinInteriorZone.Entered += other => { if (IsPlayer(other)) _playerInCabin = true; };
        _cabinInteriorZone.Exited += other => { if (IsPlayer(other)) _playerInCabin = false; };
    }

    private static bool IsPlayer(Collider other) => other.CompareTag("Player");

    private void OnEnable() {
        // Restore-on-load: if this floor is already the relevant scene
        // when this object activates (e.g. this floor was the arrival
        // target of a ride already in flight), pick up rendering/ticking
        // immediately rather than waiting for the next OnReadyForTransition-
        // adjacent event — mirrors PlayerStateProvider/MemoryTriggerObject's
        // own established "restore correctly on scene (re)load" precedent.
        ElevatorSystem.Instance.OnDoorsOpeningStarted += HandleDoorsOpeningStarted;
        ElevatorSystem.Instance.OnDoorsOpen += HandleDoorsOpen;
        ElevatorSystem.Instance.OnDoorsClosingStarted += HandleDoorsClosingStarted;
        ElevatorSystem.Instance.OnIdleReached += HandleIdleReached;
        ElevatorSystem.Instance.OnReadyForTransition += HandleReadyForTransition;
    }

    private void OnDisable() {
        ElevatorSystem.Instance.OnDoorsOpeningStarted -= HandleDoorsOpeningStarted;
        ElevatorSystem.Instance.OnDoorsOpen -= HandleDoorsOpen;
        ElevatorSystem.Instance.OnDoorsClosingStarted -= HandleDoorsClosingStarted;
        ElevatorSystem.Instance.OnIdleReached -= HandleIdleReached;
        ElevatorSystem.Instance.OnReadyForTransition -= HandleReadyForTransition;
    }

    private void Update() {
        if (_playerInButtonRange && _interactAction.action.WasPressedThisFrame()) {
            ElevatorSystem.Instance.TryCall(_thisFloorSceneName, GeceOturumDurumu.Instance.IsSessionActive);
        }
        // Only tick when THIS floor is the single floor ElevatorSystem
        // currently considers active — corrected during unity-specialist
        // review (2026-08-07) from an earlier OR-of-two-fields check that
        // could match both loaded floors simultaneously during the SOFT
        // transition's co-residency window, double-ticking _elapsed (see
        // Decision's corrective note).
        if (ElevatorSystem.Instance.ActiveFloorScene == _thisFloorSceneName
                && ElevatorSystem.Instance.CurrentState != ElevatorState.Idle) {
            ElevatorSystem.InternalInstance.Tick(Time.deltaTime, _playerInCabin,
                                                   _arrivalDuration, _doorOpenAnim, _dwellTime, _doorCloseAnim);
        }
    }

    private void HandleReadyForTransition(string departureScene) {
        if (departureScene != _thisFloorSceneName) return;  // not my ride to drive the swap for
        string toScene = _thisFloorSceneName == "Depot" ? "Ballroom" : "Depot";  // MVP: exactly 2 floors
        ElevatorSystem.InternalInstance.SetTransition(departureScene, toScene);
        PlayerStateProvider.Current.RequestMovementLock(this, MovementLockScope.MoveOnly);

        // Self-caught before review: an earlier draft subscribed to
        // OnSoftTransitionRejected AFTER calling RequestSoftTransition and
        // then tried to detect a rejection by inspecting CurrentState
        // afterward — both wrong. seviye-sahne-gecisi.md's own 2026-08-03
        // revision guarantees OnSoftTransitionRejected fires SYNCHRONOUSLY,
        // inside RequestSoftTransition itself, so subscribing after the
        // call would miss it entirely; and the CurrentState check was
        // backwards (would have re-entered Waiting even after a rejection
        // had already correctly set DoorsOpening). Fixed: subscribe BEFORE
        // the call, and track the outcome with an explicit local flag
        // rather than re-deriving it from CurrentState.
        bool rejectedSynchronously = false;
        void HandleRejectedOnce(string reason) {
            SceneTransitionManager.Instance.OnSoftTransitionRejected -= HandleRejectedOnce;
            rejectedSynchronously = true;
            PlayerStateProvider.Current.ReleaseMovementLock(this);
            ElevatorSystem.InternalInstance.RejectedSynchronously();
        }
        SceneTransitionManager.Instance.OnSoftTransitionRejected += HandleRejectedOnce;

        SceneTransitionManager.Instance.RequestSoftTransition(
            departureScene, toScene, /* config */ default,
            onComplete: () => {
                PlayerStateProvider.Current.ReleaseMovementLock(this);   // must be the SAME `this` that requested it
                ElevatorSystem.InternalInstance.OnTransitionComplete();
            },
            onFailed: (reason) => {
                PlayerStateProvider.Current.ReleaseMovementLock(this);
                ElevatorSystem.InternalInstance.OnTransitionFailed();
            });

        // Safe no-op if HandleRejectedOnce already removed itself above —
        // this call always returns by the time RequestSoftTransition does,
        // since the rejection (if any) fires synchronously within it.
        SceneTransitionManager.Instance.OnSoftTransitionRejected -= HandleRejectedOnce;

        if (!rejectedSynchronously) {
            ElevatorSystem.InternalInstance.EnterWaiting();
        }
    }

    // HandleDoorsOpeningStarted/HandleDoorsOpen/HandleDoorsClosingStarted/
    // HandleIdleReached: each checks ElevatorSystem.Instance.ActiveFloorScene
    // == _thisFloorSceneName before touching this floor's own door-anim/
    // light/cosmetic-shake-and-hum visuals — omitted here, same relevance-
    // check shape as Update()'s ticking guard above.
}
```

**Note on `RequestSoftTransition`'s `config` parameter**: the sketch above passes `default` for `SoftTransitionConfig` — valid C# regardless of whether that type is a struct or class, but if it's a reference type whose members `RunTransition`/`DoSwap` dereference without a null check, an all-default value could throw at the first real call site. This ADR is the first to actually construct a call to `RequestSoftTransition`; verifying an all-default config is safe (or supplying real values) is implementation-time work, flagged here rather than assumed (unity-specialist validation, 2026-08-07).

## Alternatives Considered

### Alternative 1: Single shared cabin `GameObject` in a new persistent "Elevator" scene (ADR-0008/ADR-0009 pattern)
- **Description**: One physical cabin object lives in a fifth persistent scene, loaded once at boot alongside UI/Player/Foundation, never unloaded; both floors' call buttons reference it directly.
- **Pros**: A single, always-alive `MonoBehaviour` instance drives the whole ride — no responsibility-handoff mechanism needed; matches the precedent ADR-0008/ADR-0009 already established for genuinely Foundation-scoped, always-relevant systems.
- **Cons**: This system has no genuine need for an always-loaded Unity object — its state is simple enough for a static facade, and a persistent "Elevator" scene would need its own camera-stacking/render-layering answer to be visible from whichever floor is active (exactly the unresolved RenderGraph/multi-scene-camera-stacking spike the GDD's own Open Question #2 already flags as unverified), adding a real engine-risk dependency this ADR would otherwise avoid entirely.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-07): per-floor prefab instances plus a shared static facade solves the cross-scene-visibility requirement without introducing a new persistent scene or depending on the unresolved camera-stacking spike; disproportionate for what this system actually needs.

### Alternative 2: State machine embedded directly in `ElevatorController` (no pure C# split)
- **Description**: Skip `ElevatorStateMachine`/`ElevatorSystemState` — implement all state transitions directly inside `ElevatorController`'s `Update()`.
- **Pros**: Fewer types; no event-based hand-off between pure logic and Unity-touching side effects.
- **Cons**: Directly violates `coding-standards.md`'s BLOCKING unit-test requirement for state-machine logic; this project has a consistent, already-established precedent (ADR-0003, ADR-0010) for exactly this situation. Additionally — and unique to this system — a `MonoBehaviour`-embedded state machine couldn't be shared cross-scene at all, since each floor's `ElevatorController` is a *different* `MonoBehaviour` instance; the shared-facade requirement (Decision) would force a redesign regardless of the testability question.
- **Rejection Reason**: Same established precedent as every prior state-machine ADR this session, reinforced here by a second, independent reason (cross-scene sharing) this system alone has.

### Alternative 3: Reuse `InteractionStateMachine`/`IInteractable` for the call button
- **Description**: Implement the call button as an `IInteractable` (`Instant` type), reusing Etkileşim Sistemi's existing focus/interact pipeline (ADR-0010) instead of a bespoke trigger-zone.
- **Pros**: Zero new interaction-detection code; consistent with how every other interactable object in the game works.
- **Cons**: The GDD's own Core Rules explicitly and deliberately reject this ("kendi trigger-zone mantığıyla, Etkileşim Sistemi'nin `IInteractable` arayüzünü kullanmadan") — the button reads `Interact` directly within a simple proximity trigger, with no crosshair/prompt UI, framed as "otelin kendi donanımı" rather than a game-UI-mediated interactable.
- **Rejection Reason**: Directly contradicts the GDD's own Approved Core Rules and UI Requirements section; not a real option.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #11 and resolves the GDD's own Open Question #1 (cabin architecture), which the GDD itself deferred to exactly this kind of ADR.
- Reuses every dependency this system already had (ADR-0001, ADR-0003, ADR-0006, ADR-0008) with zero new engine mechanisms and no new persistent scene — avoids the unresolved camera-stacking spike (GDD Open Question #2) entirely rather than depending on it.
- The state-machine/facade split satisfies `coding-standards.md`'s BLOCKING unit-test rule with a pattern this project has already validated twice (ADR-0003, ADR-0010).
- Unblocks `Görev/Taşıma Döngüsü`'s depot↔ballroom carry route, which its own Dependencies section names this system as a hard prerequisite for.

### Negative
- Two separate `ElevatorController` prefab instances (one per floor) must be kept visually/behaviorally identical by convention (art/level-design discipline), not by a shared `GameObject` reference — a content-authoring risk this ADR accepts rather than solves architecturally (mirrors ADR-0010's own acceptance of an analogous mis-layered-prop content risk).
- The responsibility-handoff ticking mechanism (Decision) is a genuinely new pattern for this project — no prior ADR has a shared pure-state-machine driven by more than one possible `MonoBehaviour` instance. Future maintainers extending this system must understand the relevance-check discipline, not just the state machine itself.
- `RequestMovementLock`/`ReleaseMovementLock`'s requester-identity coupling (ADR-0003) means the *same* `ElevatorController` instance (the origin's) must survive long enough to receive its own `onComplete`/`onFailed` callback — true under this project's `DelayedUnload` timing (0.5-2s after `Complete`, ADR-0008), but a future change to that timing could theoretically destroy the origin's `GameObject` before its callback fires; flagged in Risks.

### Risks
- **Risk (corrected, unity-specialist review, 2026-08-07)**: The handoff-tickable shared state machine (Decision) depends on exactly one floor's `ElevatorController.Update()` ever considering itself "relevant" at a time. An earlier draft's relevance check (`OriginScene == mine OR DestinationScene == mine`) was unsound — `OriginScene` was never cleared for a ride's full lifetime, so during the SOFT transition's co-residency window (guaranteed to overlap the destination-side `DoorsOpen`/`DoorsClosing` cycle at this ADR's default Tuning Knobs — see Decision's corrective note) both floors' checks were simultaneously true, double-ticking `_elapsed` and reading each floor's own independent (and differently stale) `_playerInCabin` for the GDD's boarding check. **Mitigation**: replaced with a single `ActiveFloorScene` property, reassigned exactly once at the moment ticking responsibility hands off (`OnTransitionComplete()`) — only one controller can match a single value at a time, by construction; a `[Test]` should assert `Tick()` is idempotent-safe against being called from a controller whose floor doesn't match `ActiveFloorScene` (i.e. it simply does nothing), not just that `OriginScene != DestinationScene` (which was true even under the original, broken check, and therefore not the invariant that actually matters).
- **Risk**: `ElevatorController.HandleReadyForTransition`'s narrowly-scoped `SceneTransitionManager.Instance.OnSoftTransitionRejected` subscription (subscribed immediately before `RequestSoftTransition`, unsubscribed immediately after) is a different subscription-lifetime shape from every other `OnTransitionStateChanged`/`OnSoftTransitionRejected` consumer in this project (ADR-0009/ADR-0012 both use `OnEnable`/`OnDisable`-paired, session-lifetime subscriptions). **Mitigation**: justified specifically because `seviye-sahne-gecisi.md`'s own 2026-08-03 revision guarantees `OnSoftTransitionRejected` fires *only* synchronously at request time — there is no later moment this subscription could miss, so a persistent subscription would only add stale-callback risk (the exact class of bug ADR-0012's `broadcast_transition_event_consumed_without_identity_filter` forbidden pattern warns about) for zero benefit; the local `void HandleRejectedOnce(...)` closure pattern keeps the subscription's lifetime provably matched to its one relevant call. Confirmed sound by unity-specialist review (2026-08-07) — subscribing before the call, a handler removing itself mid-invocation, and the closure-captured `bool` flag are all standard, well-defined C# behavior.
- **Risk (added, unity-specialist review, 2026-08-07)**: `ElevatorSystemState`'s events forward directly onto `_machine`'s own event fields (`add`/`remove` accessors) — this is the pattern's first ADR-0001 consumer that both exposes events AND has live `MonoBehaviour` subscribers pairing subscription lifetime to `OnEnable`/`OnDisable` rather than session lifetime, directly exposing the same "Reload Scene: Off, `Awake`/`OnEnable` doesn't re-run across a Play Stop→Play boundary" stale-subscription hazard ADR-0001/ADR-0003/ADR-0008/ADR-0010 already document. **Mitigation**: `ElevatorSystem`'s backing `_state`/`_machine` instances are never replaced — `ResetOnLoad()` resets fields in place (Decision) rather than swapping in a new instance, so an already-subscribed `ElevatorController`'s event handlers remain correctly bound across a reset; a `[UnityTest]` (Reload Scene disabled, two simulated sessions) should still confirm this directly, matching the precedent those four ADRs each carry.
- **Risk**: `ElevatorSystem`'s cabin-facing GDD Open Question #2 (Unity 6.3 RenderGraph/multi-scene camera-stacking spike, owned by `seviye-sahne-gecisi.md`) remains genuinely unresolved — this ADR's per-floor-prefab decision (Alternative 1's rejection) sidesteps needing it, but if a future revision of `Seviye/Sahne Geçişi`'s own rendering approach changes how multiple loaded scenes' geometry is composited, this system's assumption that only the active floor's cabin is ever visibly rendered should be re-checked.
- **Risk**: `RequestSoftTransition`'s `SoftTransitionConfig config` parameter is passed as `default` (Decision's closing note) — this ADR is the first to actually construct a call site for it, and an all-default value's safety against `RunTransition`/`DoSwap`'s internal usage hasn't been independently verified here (ADR-0008's own excerpt doesn't fully define the type's members/nullability). **Mitigation**: flagged for implementation-time verification rather than assumed; low risk in practice since `RequestHardCut`'s analogous config parameter is already used elsewhere in this project without incident, but this is `RequestSoftTransition`'s own first real call site.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `asansor-kat-erisim-sistemi.md` | Call button: trigger-zone + direct `Interact` read, no `IInteractable`, `IsSessionActive` read only at press time | `ElevatorController`'s `_buttonRangeZone`/`Update()`, `TryCall(floorScene, isSessionActive)` |
| `asansor-kat-erisim-sistemi.md` | `Idle→Called→DoorsOpening→DoorsOpen→DoorsClosing→Waiting` state machine, no platform-physics/`Move()`-delta applied | `ElevatorStateMachine.Tick`, exactly as States and Transitions specifies; no Transform writes anywhere in the sketch |
| `asansor-kat-erisim-sistemi.md` | `RequestMovementLock(this, MoveOnly)`→`RequestSoftTransition`→`onComplete`/`onFailed`→`ReleaseMovementLock(this)` sequencing | `HandleReadyForTransition`, same requester object throughout |
| `asansor-kat-erisim-sistemi.md` | Synchronous `OnSoftTransitionRejected` handling — `Waiting` never entered, cabin returns to origin `DoorsOpening` | `RejectedSynchronously()`, narrowly-scoped subscription (Decision) |
| `asansor-kat-erisim-sistemi.md` | `onFailed` gets the identical reaction to a synchronous rejection | `OnTransitionFailed()` — same `DoorsOpening` outcome as `RejectedSynchronously()` |
| `asansor-kat-erisim-sistemi.md` | Player-boarded check happens only at the exact instant `DoorsClosing` completes | `Tick()`'s `DoorsClosing` case reads `playerInCabin` fresh on the completing frame only |
| `asansor-kat-erisim-sistemi.md` | Busy cabin locks out every other floor's button, even a floor whose scene isn't currently loaded | Shared `ElevatorSystem` static facade (ADR-0001 pattern) — `TryCall` rejects unless `CurrentState == Idle`, visible cross-scene |
| `asansor-kat-erisim-sistemi.md` Open Questions #1 | Cabin: shared object vs. per-floor instance | Resolved: per-floor prefab instances + shared static facade (Decision) |
| `asansor-kat-erisim-sistemi.md` | Core Rules — every ride begins with an explicit button press ("Geçerli basışta Called") | `_isArrivalLeg` guard: the arrival-side DoorsClosing never re-arms a ride, regardless of `playerInCabin` — no automatic return (user-confirmed, TD-ADR review 2026-08-07, resolving an ambiguity the GDD's generic DoorsClosing wording left open) |
| `architecture.md` | Module Ownership row — cabin state machine, call-button trigger zone, `IPlayerState`/`RequestMovementLock` (Foundation), `RequestSoftTransition` (Foundation), `IsSessionActive` (Foundation) consumption | Implemented as designed |

## Performance Implications
- **CPU**: Two small trigger `Collider`s per floor (button range + cabin interior), one small state-machine `Tick()` call per frame only while a ride is relevant to that floor (idle floors do nothing) — negligible against the 16.6ms frame budget.
- **Memory**: One `ElevatorStateMachine` instance (inside the shared facade), two `ElevatorController` prefab instances (one per floor, only one loaded at a time under normal play) — negligible.
- **Load Time**: N/A — no asset loading.
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Asansör/Kat-Erişim Sistemi` is not yet implemented).

**Registry note** (added, TD-ADR review, 2026-08-07): at this ADR's Step 6 registry update, `asansor-kat-erisim-sistemi` should be added to `session_scoped_state_static_facade`'s consumers list, a new `state_ownership` entry registered for `ElevatorSystem`'s cross-scene ride state (read/written by two different `ElevatorController` instances — exactly the multi-consumer shared state the registry's own rules say to register), and the no-auto-return clarification recorded so future ADR authors don't re-derive the auto-return reading from the GDD's generic DoorsClosing wording.

## Validation Criteria
- A `[Test]` constructs a fresh `ElevatorStateMachine`, drives `TryCall`→`Tick` through a full `Idle→Called→DoorsOpening→DoorsOpen→DoorsClosing→(boarded)→OnReadyForTransition` cycle, then `SetTransition()`→`EnterWaiting()`→`OnTransitionComplete()`→`Tick` through the destination-side `DoorsOpen→DoorsClosing→Idle` cycle, asserting the exact GDD-specified event/state sequence — no `Collider`/`MonoBehaviour`/scene involved (GDD ACs 5-9).
- A `[Test]` asserts a second `TryCall` while `CurrentState != Idle` is a no-op — no state change, no event fired (GDD AC4/AC12).
- A `[Test]` asserts `TryCall` with `isSessionActive == false` is a no-op, state stays `Idle` (GDD AC2).
- A `[Test]` asserts the `DoorsClosing`-completion boarded-check uses the `playerInCabin` value passed to the *completing* `Tick()` call, not an earlier one — construct a sequence where `playerInCabin` is `true` mid-`DoorsClosing` then `false` on the completing tick, assert the ride is cancelled to `Idle`, not `Waiting` (GDD Edge Case, dwell-vs-DoorsClosing boarding).
- A `[Test]` asserts `RejectedSynchronously()` and `OnTransitionFailed()` both return `CurrentState` to `DoorsOpening`, with `ActiveFloorScene`/`OriginScene` unchanged from before the attempted transition — the two call sites the GDD requires an identical reaction from (GDD AC8a, AC10).
- A `[Test]` (corrected twice — unity-specialist review replaced a weaker `OriginScene != DestinationScene` check that was true even under the original broken relevance check; TD-ADR review then corrected this bullet's own overreach, since `OriginScene` is `null` until `SetTransition()` runs at the end of `DoorsClosing`, so "`ActiveFloorScene == OriginScene` throughout" was never the actual invariant) asserts: `ActiveFloorScene` stays constant — equal to the calling floor — from `TryCall` through `OnTransitionComplete()`; `OriginScene` equals it from the moment `SetTransition()` runs through `Waiting`; and `OnTransitionComplete()` reassigns `ActiveFloorScene` to `DestinationScene` exactly once. This is the real invariant the handoff-relevance check depends on.
- A `[Test]` (added, TD-ADR review, 2026-08-07 — the no-auto-return decision) asserts the arrival-side `DoorsClosing` completing with `playerInCabin == true` does NOT fire `OnReadyForTransition` — the ride winds down to `Idle` with `OnIdleReached(true)`, and only a fresh `TryCall` (button press) can start a new ride.
- A `[Test]` (added, TD-ADR review, 2026-08-07) asserts `OnIdleReached` fires *before* `ActiveFloorScene` is cleared (a subscriber reading `ActiveFloorScene` inside the handler sees the still-valid floor name), and that its `bool` parameter is `false` for an origin-side no-boarding cancellation but `true` for the post-arrival wind-down.
- A `[UnityTest]` (Reload Scene disabled, two simulated Play sessions — matching ADR-0001/0003/0008/0010's own established pattern for this hazard) confirms an `ElevatorController` subscribed to `ElevatorSystem.Instance`'s events before a `FoundationBootstrap.ResetAll()` reset still receives events correctly afterward (added, unity-specialist review, 2026-08-07 — verifies the in-place-reset fix actually prevents the stale-subscription hazard, not just that `ResetOnLoad()` is registered).
- A `[Test]`/inspection confirms `ElevatorSystem.ResetOnLoad()` is registered in `FoundationBootstrap.ResetAll()` (ADR-0001) — a fresh Play session must not carry over a previous session's in-flight ride state.

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — source of the `ElevatorSystem` cross-scene shared-state mechanism this ADR reuses.
- ADR-0003 (Player State) — source of `RequestMovementLock`/`ReleaseMovementLock`/`MovementLockScope.MoveOnly`; source of the state-machine/`MonoBehaviour` testability split this ADR reuses.
- ADR-0006 (Session State) — source of `GeceOturumDurumu.Instance.IsSessionActive`.
- ADR-0008 (Scene Transition State Machine) — source of `RequestSoftTransition`/`OnSoftTransitionRejected`, including its synchronous-only-at-request-time contract this ADR's narrowly-scoped subscription depends on.
- ADR-0010 (Interaction State Machine) — second precedent (after ADR-0003) for the pure-state-machine/`MonoBehaviour`-driver split.
- ADR-0012 (Dialogue Callback Selection Timing) — first Core-layer consumer of the ADR-0001 static-facade pattern (this ADR is the second); source of the "verify relevance before acting on shared cross-scene state" discipline this ADR's handoff-ticking mechanism generalizes into a different shape.
