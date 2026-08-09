# ADR-0013: Carry Loop and Round State

> **Unity Specialist Validation**: BLOCKING (5 findings, found and fixed) 2026-08-08 — (1) ADR-0001 had explicitly forwarded `architecture.md` QQ-07 (Reload Scene: Off suppresses `Awake` re-runs while `ResetAll()` clears `CollectedItemIds`) to this exact ADR, and the draft's `Awake()`-time restore never addressed it — moved to the top of `OnEnable()`, before the `Register` call in the same body, which re-fires in that scenario; closes QQ-07. (2) `CarryItemPickup` never actually registered with `InteractableRegistry` — the load-bearing `OnEnable`/`OnDisable` Register/Deregister calls were elided from the sketch entirely; added explicitly. (3) A hard, unstated ordering constraint: if the depot scene's objects activate before `StartNight()` (`CurrentRoundIndexForRestore == -1`), every round-0 item permanently self-deactivates — the constraint is now binding on ADR-0015/the boot story, with a defined all-inactive state at index -1. (4) The GDD's AC3 ("slots full → `CanInteract=false` AND prompt shows 'Eller Dolu'") is internally contradictory under ADR-0010's focus gate — a `CanInteract=false` object can never be focused, and prompts render only for the focused target; resolved by user decision: slots-full items stay focusable, rejection moves inside `TryPickUp` (`CanPickUp` renamed `IsFocusable`, `AreSlotsFull` added to the interface). (5) `HoldDuration` was missing — the class didn't compile against ADR-0004's `IInteractable`. Plus 5 MINOR (session-inactive delivery freeze added to `DeliverAll`; `ResetAll()` registration documented in Migration Plan; `DropOffZone` same-GameObject collider rule stated positively as a control-manifest candidate; `IsCollected` exposed on the machine directly instead of a LINQ round-trip; `CarryItemDef` gained accessors + a `DisplayName` field so the prompt never shows a raw asset filename).
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-08 — 5 findings, fixed: (1) the Constraints bullet still carried the pre-revision `CanInteract = (slots < N)` formula the Decision itself had revised away — the exact stale-claim class this project's reviews hunt for; corrected. (2) The facade was mislabeled the pattern's "third Core-layer consumer" — Görev/Taşıma is Feature-layer per architecture.md's System Layer Map, making this the pattern's **first Feature-layer consumer**; relabeled, carried into the registry note. (3) The GDD-sync obligation was undercounted — the AC3 flag covered roughly a third of the texts this ADR actually revises; expanded into a seven-item sync list (three GDD locations for the `CanInteract=false` letter, the Edge Cases N=0 prose, Kalıcılık's `Awake()` sentence, AC8's letter, and architecture.md's Data Flow §3/`IInteractable` invariant/QQ-07 row). (4) The build validation had no scene-vs-`TaskListDef` per-round item-count cross-check, so a desync could silently break AC6 (a round "completing" with an item still in the world) or soft-lock (never completing) — added; also aligned the N-floor with the GDD's letter (build-block N=0 only; 2-4 is an `OnValidate` warning band). (5) The Migration Plan's "append after the existing five consumers" count was stale — and investigating it surfaced a real accumulated cross-file debt: ADR-0011/0012 both claim `ResetAll()` registration but ADR-0001's code block was never edited; the write-time edit now reconciles all three pending entries. Verified sound explicitly: the AC3-contradiction claim (checked against the GDD and ADR-0010 side by side), the logical/physical activation split, the 6→4 state collapse, all Loading/Carrying transition traces, the session-freeze semantics, and full registry/forbidden-pattern compliance.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (gameplay state machine, ScriptableObject data, Physics trigger zone, cross-scene shared state) |
| **Knowledge Risk** | LOW — `ScriptableObject`/`[CreateAssetMenu]`, `OnTriggerEnter` relay (ADR-0011's validated pattern), `IInteractable` (ADR-0004/0010), and the ADR-0001 static-facade pattern are all already-validated Unity 6.3 APIs and project patterns. No new engine mechanism introduced. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md`, `docs/architecture/adr-0003-player-state-and-movement-lock.md`, `docs/architecture/adr-0004-interactableregistry-foundation-ownership.md`, `docs/architecture/adr-0006-session-state-service-and-round-counter-ownership.md`, `docs/architecture/adr-0008-scene-transition-state-machine.md`, `docs/architecture/adr-0010-interaction-state-machine.md`, `docs/architecture/adr-0011-elevator-state-machine.md`, `docs/architecture/adr-0012-dialogue-callback-selection-timing.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | The `OnEnable()`-top self-restore on `CarryItemPickup` (deactivate before the `Register` call in the same `OnEnable` body) should be exercised in Play mode across a real depot→ballroom→depot round trip, including a delivery performed inside the SOFT transition's co-residency window (see Decision → "Logical vs. physical round activation"). |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (static-facade pattern, in-place reset per ADR-0011's forbidden pattern) — `GorevTasimaDongusu` state. ADR-0003 (Player State) — `IsCarrying` mirror via `FirstPersonController.SetCarrying(bool)`. ADR-0004 (InteractableRegistry) — `CarryItemPickup : IInteractable` registration/snapshot semantics. ADR-0006 (Session State) — `GeceOturumDurumu.InternalInstance.SetRoundState(idx, total)` write path, `IsSessionActive` read. ADR-0010 (Interaction State Machine) — the `Instant` interaction pipeline items are picked up through. ADR-0011 (Elevator) — the depot↔ballroom traversal this loop rides as an opaque "passenger". |
| **Enables** | ADR-0015 (End-Condition Orchestration) — consumes `OnTaskListCompleted`/`OnFinalRoundStarted`/`OnFinalRoundItemPickedUp`/`IsFinalRoundActive`/`HasCarriedInFinalRound`, all defined here. |
| **Blocks** | Any story implementing `Görev/Taşıma Döngüsü`; any `Sahne Kesmeli Anlatı` story (its saturation/completion conditions consume this ADR's events). |
| **Ordering Note** | ADR-0015 should be authored after this ADR so its event contract consumption references these exact signatures. |

## Context

### Problem Statement

`gorev-tasima-dongusu.md` (carries a "Needs Revision" header artifact pending the next clean `/review-all-gdds` round — not a blocker per this session's established precedent; the two design-decision findings that caused it were both resolved in-GDD on 2026-08-04) is the project's most heavily-reviewed GDD (three design-review rounds plus two cross-review verification rounds) and specifies the night's core loop in unusual detail: a `TaskList` of 3-5 `CarryRound`s, slot-capped `Instant` pickups, automatic trigger-zone delivery, a six-state round machine (`Idle→Loading→Carrying→Delivering→RoundComplete→AllRoundsComplete`), a static-service persistence mandate patterned on `Gece/Oturum Durumu`, an `Awake()`-time depot-reload restore via `CollectedItemIds`, four externally-consumed events, and two relocated round counters written into Foundation per ADR-0006.

The GDD locks most mechanism decisions itself. What it leaves to this ADR: **(1)** the concrete class split satisfying `coding-standards.md`'s BLOCKING unit-test rule; **(2)** the concrete write path for `SetCarrying` (ADR-0003 marks `IsCarrying` as "mirrored from Görev/Taşıma" but no ADR has defined the mirror's mechanism); **(3)** how "round N+1 activation in the same frame as round N completion" (GDD Edge Cases/AC8) reconciles with the physical reality that round completion always happens in the ballroom while the depot scene — where round N+1's item `GameObject`s live — is unloaded; **(4)** which component owns the pooled slot-representation visuals and the `Highlight(round)` curve.

### Constraints

- Must not deviate from `gorev-tasima-dongusu.md`'s Core Rules, States and Transitions, and Edge Cases — this ADR formalizes, it does not redesign. In particular: pickup is `Instant`, with focusability = (item in active round) AND (not collected) AND (session active), and the slots-full case rejected inside `TryPickUp` with the prompt carrying "Eller Dolu" (the GDD's original `CanInteract = (slots < N) AND ...` formula was internally contradictory under ADR-0010's focus gate — revised per user decision, see Decision; corrected here too during TD-ADR review after this bullet was initially missed); delivery is automatic, buttonless, idempotent; `SetCarrying(true)` fires exactly once on 0→1 and `SetCarrying(false)` exactly once on 1→0; round completion requires ALL round items gone from the world AND slots empty (partial delivery does not complete a round); no put-back mechanic; no runtime clamping of misconfigured content (build-blocking validation instead); `IsFinalRoundActive` stays `true` at `AllRoundsComplete` (never reset).
- Must write `CurrentRoundIndex`/`TotalRoundCount` ONLY via `GeceOturumDurumu.InternalInstance.SetRoundState(idx, total)` (ADR-0006's locked write path) — this ADR's own facade does not re-expose public round-counter queries (Adaptif Ses reads them from `GeceOturumDurumu`, per ADR-0006/ADR-0009).
- Must reuse `InteractableRegistry`'s `OnEnable`/`OnDisable` registration and snapshot-iteration semantics (ADR-0004) — the GDD's pickup-deactivation safety explicitly leans on them.
- `GorevTasimaDongusu`'s facade exposes events consumed by `Sahne Kesmeli Anlatı`, so per ADR-0011's `wholesale_state_replacement_for_event_exposing_facade` forbidden pattern, its reset MUST be in-place, never field replacement.
- The state-machine logic must live in a plain, Unity-decoupled C# class per `coding-standards.md`'s BLOCKING unit-test rule — the GDD's own AC9a explicitly requires the full 3-round happy path to be testable with mocked signals, no scene/collider/elevator.

### Requirements

- Round/slot/collected state must survive depot↔ballroom scene swaps (GDD Core Rules → Kalıcılık) — an ADR-0001 static facade, as the GDD itself mandates.
- `CarryItemPickup` must self-restore on every depot load: already-collected items deactivate at the top of `OnEnable()`, before the `Register` call in the same body (GDD's structural anti-re-collection guarantee, revised from `Awake()` during unity-specialist review to survive Reload Scene: Off — closes `architecture.md` QQ-07); not-yet-active rounds' items must equally never register.
- The four events (`OnTaskListCompleted`, `OnFinalRoundStarted`, `OnFinalRoundItemPickedUp`, plus the `SetCarrying` transitions) must fire with exactly the once-only semantics the GDD's ACs specify (AC4, AC7, AC17, AC18).
- Build-time validation (both `OnValidate()` Editor-time warning AND `IPreprocessBuildWithReport` build-blocker) must reject: round count outside 3-5, any round with 0 items or more than N items, N=0, and scene-vs-`TaskListDef` per-round item-count mismatches (GDD Edge Cases, AC1, AC11; the count cross-check added during TD-ADR review).

## Decision

### Class split: pure C# `CarryLoopStateMachine` + `GorevTasimaDongusu` static facade + three thin scene/player MonoBehaviours

Same shape as ADR-0011: the pure state machine lives inside the shared facade; `MonoBehaviour`s are thin drivers. Unlike ADR-0011 there is no tick-handoff problem — this system is fully event-driven (pickups, deliveries, and scene loads are the only inputs; no per-frame `Tick()` exists at all).

```csharp
public enum CarryLoopState { Idle, Loading, Carrying, AllRoundsComplete }
// Delivering and RoundComplete (GDD States and Transitions) are synchronous,
// intra-call phases of DeliverAll() below, not observable resting states —
// the GDD's own Edge Cases mandate no frame-splitting yield between
// round-complete evaluation and next-round activation, which makes them
// unobservable between frames by construction. The four states above are
// the resting states a caller can ever observe.

// Plain C# class, no Unity types — testable via [Test] driving
// TryPickUp()/DeliverAll()/StartNight() directly with synthetic inputs
// (GDD AC9a's mocked full-happy-path test, exactly). Same testability
// split as ADR-0003/0010/0011.
public sealed class CarryLoopStateMachine {
    public CarryLoopState CurrentState { get; private set; } = CarryLoopState.Idle;
    public int CarriedCount { get; private set; }
    public bool IsFinalRoundActive { get; private set; }   // never reset once true (GDD States note)
    public bool HasCarriedInFinalRound { get; private set; } // write-once, never cleared (GDD Core Rules)
    public IReadOnlyCollection<string> CollectedItemIds => _collectedItemIds;

    public event Action OnCarryingStarted;        // 0→1 — the SetCarrying(true) trigger, exactly once per load
    public event Action OnCarryingEnded;          // 1→0 — the SetCarrying(false) trigger
    public event Action<int> OnRoundActivated;    // (0-based roundIndex) — fired for round 0 at StartNight too
    public event Action OnFinalRoundStarted;      // exactly once per night (GDD AC17)
    public event Action OnFinalRoundItemPickedUp; // exactly once per night (GDD AC18)
    public event Action OnTaskListCompleted;      // exactly once per night (GDD AC7)

    private readonly HashSet<string> _collectedItemIds = new();
    private int _currentRoundIndex = -1;
    private int _totalRoundCount;
    private int _activeRoundItemCount;
    private int _slotCapacity;
    // Injected write path — the facade wires this to
    // GeceOturumDurumu.InternalInstance.SetRoundState (ADR-0006's locked
    // single write path); tests inject a recording stub instead, keeping
    // this class Foundation-decoupled per the established split.
    private readonly Action<int, int> _writeRoundState;

    public CarryLoopStateMachine(Action<int, int> writeRoundState) { _writeRoundState = writeRoundState; }

    public void StartNight(int slotCapacity, IReadOnlyList<int> itemsPerRound) {
        _slotCapacity = slotCapacity;
        _totalRoundCount = itemsPerRound.Count;
        _roundItemCounts = itemsPerRound;   // captured for activation
        ActivateRound(0);
    }

    public bool AreSlotsFull => CarriedCount >= _slotCapacity;

    // Focusability gate (feeds IInteractable.CanInteract) — corrected
    // during unity-specialist review + user decision (2026-08-08): does
    // NOT include the slot-capacity check. Under ADR-0010's pipeline a
    // CanInteract=false object can never be focused, and PromptText is
    // only rendered for the focused target — so the GDD's AC3 ("slots
    // full → CanInteract false AND PromptText shows 'Eller Dolu'") was
    // internally contradictory as written. User-confirmed resolution:
    // slots-full items stay focusable (prompt visible), the pickup is
    // rejected inside TryPickUp instead. CanInteract=false remains only
    // for the genuinely-uninteractable cases, which are also correctly
    // silent/unfocusable (wrong round, already collected, session
    // inactive — AC10's silent reject). Requires a small GDD sync note
    // on AC3's letter (player-facing behavior unchanged).
    public bool IsFocusable(string itemId, int itemRoundIndex, bool isSessionActive) {
        if (!isSessionActive) return false;                        // GDD AC10 — silent reject
        if (itemRoundIndex != _currentRoundIndex) return false;    // not the active round
        return !_collectedItemIds.Contains(itemId);                // already collected (restore safety)
    }

    public bool IsCollected(string itemId) => _collectedItemIds.Contains(itemId);

    public bool TryPickUp(string itemId, int itemRoundIndex, bool isSessionActive) {
        if (!IsFocusable(itemId, itemRoundIndex, isSessionActive)) return false;
        if (AreSlotsFull) return false;                            // "Eller Dolu" — focusable but rejected (AC3, revised)
        _collectedItemIds.Add(itemId);
        CarriedCount++;
        if (CarriedCount == 1) {
            CurrentState = _activeRoundItemCount > 1 && _collectedItemIds.Count < _activeRoundItemCount
                ? CarryLoopState.Loading : CarryLoopState.Carrying;
            OnCarryingStarted?.Invoke();                           // exactly once per 0→1 (GDD AC2/AC4)
        } else if (CurrentState == CarryLoopState.Loading
                    && (_collectedItemIds.Count == _activeRoundItemCount || CarriedCount == _slotCapacity)) {
            CurrentState = CarryLoopState.Carrying;                // GDD States: Loading→Carrying
        }
        if (IsFinalRoundActive && !HasCarriedInFinalRound) {
            HasCarriedInFinalRound = true;                         // write-once (GDD Core Rules, AC18)
            OnFinalRoundItemPickedUp?.Invoke();
        }
        return true;
    }

    public void DeliverAll(bool isSessionActive) {
        if (!isSessionActive) return;                              // GDD Edge Cases: "round akışı donar" —
                                                                    // session-inactive freezes deliveries too,
                                                                    // not just pickups (unreachable in MVP since
                                                                    // EndSession only fires inside the hard cut,
                                                                    // but the guard keeps the freeze symmetric
                                                                    // rather than documented-unreachable —
                                                                    // unity-specialist review, 2026-08-08)
        if (CarriedCount == 0) return;                             // idempotent no-op (GDD AC12, double-fire safe)
        CarriedCount = 0;
        OnCarryingEnded?.Invoke();                                 // the SetCarrying(false) trigger (GDD AC5)
        // Round complete = ALL round items collected AND slots now empty
        // (GDD AC6/AC13 — a partial M<N delivery leaves the round open).
        if (_collectedItemIds.Count < _activeRoundItemCount) {
            CurrentState = CarryLoopState.Idle;                    // back to depot for the rest
            return;
        }
        // RoundComplete → next activation, synchronously, no yield
        // (GDD Edge Cases; see "Logical vs. physical round activation").
        if (_currentRoundIndex + 1 < _totalRoundCount) {
            ActivateRound(_currentRoundIndex + 1);
        } else {
            CurrentState = CarryLoopState.AllRoundsComplete;
            OnTaskListCompleted?.Invoke();                         // exactly once (GDD AC7)
        }
    }

    private IReadOnlyList<int> _roundItemCounts;

    private void ActivateRound(int index) {
        _currentRoundIndex = index;
        _activeRoundItemCount = _roundItemCounts[index];
        _collectedItemIds.Clear();                                 // per-round set (GDD Core Rules)
        CurrentState = CarryLoopState.Idle;
        _writeRoundState(index, _totalRoundCount);                 // ADR-0006's SetRoundState, every transition
        OnRoundActivated?.Invoke(index);
        if (index == _totalRoundCount - 1 && !IsFinalRoundActive) {
            IsFinalRoundActive = true;                             // never reset (GDD States note)
            OnFinalRoundStarted?.Invoke();                         // once per night (GDD AC17, incl. 1-round night)
        }
    }

    public int CurrentRoundIndexForRestore => _currentRoundIndex;  // read by CarryItemPickup.OnEnable() restore only

    internal void ResetOnLoad() {
        CurrentState = CarryLoopState.Idle;
        CarriedCount = 0;
        IsFinalRoundActive = false;
        HasCarriedInFinalRound = false;
        _collectedItemIds.Clear();
        _currentRoundIndex = -1;
        _totalRoundCount = 0;
        _activeRoundItemCount = 0;
        _roundItemCounts = null;
        _slotCapacity = 0;   // reset too (TD-ADR review) — harmless if stale (nothing focusable pre-StartNight), but no field left carrying prior-session state
    }
}
```

```csharp
public interface IGorevTasimaState {
    CarryLoopState CurrentState { get; }
    int CarriedCount { get; }
    bool AreSlotsFull { get; }        // feeds the "Eller Dolu" prompt (AC3, revised — see Decision)
    bool IsFinalRoundActive { get; }
    bool HasCarriedInFinalRound { get; }
    event Action OnCarryingStarted;
    event Action OnCarryingEnded;
    event Action<int> OnRoundActivated;
    event Action OnFinalRoundStarted;
    event Action OnFinalRoundItemPickedUp;
    event Action OnTaskListCompleted;
    bool IsFocusable(string itemId, int itemRoundIndex, bool isSessionActive);
}

// ADR-0001 static facade — the GDD's own Kalıcılık rule mandates exactly
// this ("Gece/Oturum Durumu'nun kurduğu desenle aynı"). The pattern's
// first FEATURE-layer consumer (ADR-0012/ADR-0011 were the first and
// second Core-layer consumers — corrected during TD-ADR review: this
// system is Feature-layer per architecture.md's System Layer Map, not
// Core). Exposes events with
// live MonoBehaviour subscribers, so reset is IN PLACE per ADR-0011's
// wholesale_state_replacement_for_event_exposing_facade forbidden pattern
// — the facade's constructor wires the ADR-0006 write path once.
public sealed class GorevTasimaState : IGorevTasimaState {
    private readonly CarryLoopStateMachine _machine =
        new(writeRoundState: (idx, total) => GeceOturumDurumu.InternalInstance.SetRoundState(idx, total));
    // NOTE: this lambda runs only at ActivateRound() time — i.e. from
    // StartNight()/DeliverAll(), both real-gameplay calls well past boot —
    // never from this constructor itself, so no
    // constructor_subscribing_foundation_service_reset_before_event_source
    // or boot-ordering concern applies (the reference to
    // GeceOturumDurumu.InternalInstance is resolved lazily inside the
    // lambda body, not captured at construction).

    public CarryLoopState CurrentState => _machine.CurrentState;
    public int CarriedCount => _machine.CarriedCount;
    public bool IsFinalRoundActive => _machine.IsFinalRoundActive;
    public bool HasCarriedInFinalRound => _machine.HasCarriedInFinalRound;
    public event Action OnCarryingStarted { add => _machine.OnCarryingStarted += value; remove => _machine.OnCarryingStarted -= value; }
    public event Action OnCarryingEnded { add => _machine.OnCarryingEnded += value; remove => _machine.OnCarryingEnded -= value; }
    public event Action<int> OnRoundActivated { add => _machine.OnRoundActivated += value; remove => _machine.OnRoundActivated -= value; }
    public event Action OnFinalRoundStarted { add => _machine.OnFinalRoundStarted += value; remove => _machine.OnFinalRoundStarted -= value; }
    public event Action OnFinalRoundItemPickedUp { add => _machine.OnFinalRoundItemPickedUp += value; remove => _machine.OnFinalRoundItemPickedUp -= value; }
    public event Action OnTaskListCompleted { add => _machine.OnTaskListCompleted += value; remove => _machine.OnTaskListCompleted -= value; }
    public bool AreSlotsFull => _machine.AreSlotsFull;
    public bool IsFocusable(string itemId, int roundIdx, bool sessionActive) => _machine.IsFocusable(itemId, roundIdx, sessionActive);

    internal bool TryPickUp(string itemId, int roundIdx, bool sessionActive) => _machine.TryPickUp(itemId, roundIdx, sessionActive);
    internal void DeliverAll(bool sessionActive) => _machine.DeliverAll(sessionActive);
    internal void StartNight(int n, IReadOnlyList<int> itemsPerRound) => _machine.StartNight(n, itemsPerRound);
    internal bool IsCollected(string itemId) => _machine.IsCollected(itemId);   // O(1), no LINQ round-trip (unity-specialist review)
    internal int CurrentRoundIndexForRestore => _machine.CurrentRoundIndexForRestore;
    internal void ResetOnLoad() => _machine.ResetOnLoad();
}

public static class GorevTasimaDongusu {
    private static readonly GorevTasimaState _state = new();   // never replaced — in-place reset only
    public static IGorevTasimaState Instance => _state;
    internal static GorevTasimaState InternalInstance => _state;
    internal static void ResetOnLoad() => _state.ResetOnLoad();  // registered in FoundationBootstrap.ResetAll()
}
```

### Data model: `CarryItemDef` / `TaskListDef` ScriptableObjects

```csharp
[CreateAssetMenu(menuName = "Beyond The Line/Carry Item")]
public sealed class CarryItemDef : ScriptableObject {
    [SerializeField] private string _displayName;         // player-facing prompt text — NOT the asset filename
                                                           // (unity-specialist review, 2026-08-08: .name is the
                                                           // raw asset name, never player-facing)
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _material;
    [SerializeField] private AudioClip[] _jostleSounds;   // optional (GDD Audio) — routed to "SFX" mixer group
    [SerializeField] private AudioClip _pickupSfx, _deliverySfx;
    public string DisplayName => _displayName;
    public Mesh Mesh => _mesh;
    public Material Material => _material;
    public IReadOnlyList<AudioClip> JostleSounds => _jostleSounds;
    public AudioClip PickupSfx => _pickupSfx;
    public AudioClip DeliverySfx => _deliverySfx;
    // hold-pose variation deferred to /asset-spec after art bible (GDD Open Questions)
}

[Serializable]
public struct CarryRound {
    public CarryItemDef[] Items;   // count 1..N — enforced by build-time validation, never runtime-clamped
}

[CreateAssetMenu(menuName = "Beyond The Line/Task List")]
public sealed class TaskListDef : ScriptableObject {
    [SerializeField] private CarryRound[] _rounds;         // 3-5 — enforced by build-time validation
    [SerializeField] private int _slotCapacity = 3;        // N, Tuning Knob 2-4
    public IReadOnlyList<CarryRound> Rounds => _rounds;
    public int SlotCapacity => _slotCapacity;
}
```

Config ("owns config") and runtime state ("owns runtime state") stay two separate objects, per `architecture.md` Principle #5. Item identity: each `CarryItemPickup`'s stable item-id is its GDD-mandated fixed spawn point, expressed as an inspector-authored `string _itemId` on the scene object — validated for uniqueness by the same build-time pass below (a duplicate id would silently corrupt `CollectedItemIds` restore).

### Scene/player MonoBehaviours: `CarryItemPickup`, `DropOffZone`, `CarrySlotRigController`

```csharp
// Lives in the DEPOT scene, one per item spawn point, scene-authored
// ACTIVE by default (GDD: depot reload returns objects to scene-authored
// state). Never in a persistent scene (interactable_in_persistent_scene
// forbidden pattern, ADR-0004).
public sealed class CarryItemPickup : MonoBehaviour, IInteractable {
    [SerializeField] private string _itemId;        // stable spawn-point id (GDD Core Rules) — uniqueness build-validated
    [SerializeField] private int _roundIndex;       // 0-based — which CarryRound this item belongs to
    [SerializeField] private CarryItemDef _def;

    public InteractionType Type => InteractionType.Instant;
    public float HoldDuration => 0f;                // Instant — never read by ADR-0010's pipeline for this type
                                                     // (added — unity-specialist review 2026-08-08: missing member,
                                                     // class didn't compile against ADR-0004's IInteractable)
    // Slots-full items stay FOCUSABLE (user decision, 2026-08-08 — see
    // CarryLoopStateMachine.IsFocusable's corrective comment): CanInteract
    // is false only for the genuinely-silent cases (wrong round, already
    // collected, session inactive). The "Eller Dolu" rejection happens
    // inside TryPickUp, with the prompt carrying the message.
    public bool CanInteract => GorevTasimaDongusu.Instance.IsFocusable(
        _itemId, _roundIndex, GeceOturumDurumu.Instance.IsSessionActive);
    public string PromptText => GorevTasimaDongusu.Instance.AreSlotsFull ? "Eller Dolu" : _def.DisplayName;
    public bool SuppressDefaultHoldFill => false;   // Instant type — hold fill N/A

    // Restore check lives in OnEnable, BEFORE Register — corrected during
    // unity-specialist review (2026-08-08). An earlier draft put it in
    // Awake(): engine-correct for fresh loads (SetActive(false) in Awake
    // does suppress the same activation's OnEnable), but ADR-0001's own
    // Risks/Validation Criteria explicitly forwarded architecture.md's
    // QQ-07 to this ADR: under "Reload Scene: Off", Awake does NOT re-run
    // on surviving scene objects across a Play Stop→Play boundary while
    // FoundationBootstrap.ResetAll() DOES clear CollectedItemIds — an
    // Awake-only check would leave session-1's collected items silently
    // missing in session 2. OnEnable re-fires in that scenario; doing the
    // check at its top, before Register, preserves the GDD's "never
    // enters the registry" guarantee within the same call. (Residual gap,
    // same as ADR-0003/0010's precedent: an object DEACTIVATED last
    // session gets no OnEnable at all — mitigated by the boot sequence
    // guaranteeing level scenes are freshly loaded each genuine session,
    // the exact mitigation those ADRs already document.) This closes
    // QQ-07.
    private void OnEnable() {
        bool collected = GorevTasimaDongusu.InternalInstance.IsCollected(_itemId);
        // CurrentRoundIndexForRestore == -1 before StartNight() — see the
        // hard ordering constraint in Risks: StartNight() MUST complete
        // before the depot scene's objects first activate, otherwise
        // every round-0 item would self-deactivate here permanently.
        bool activeRound = _roundIndex == GorevTasimaDongusu.InternalInstance.CurrentRoundIndexForRestore;
        if (collected || !activeRound) {
            gameObject.SetActive(false);   // Register below never runs; Deregister-on-OnDisable is a
                                            // safe no-op for a never-registered object (ADR-0004)
            return;
        }
        InteractableRegistry.Register(this);
    }

    private void OnDisable() {
        InteractableRegistry.Deregister(this);
    }

    public void OnInteract() {
        if (GorevTasimaDongusu.InternalInstance.TryPickUp(
                _itemId, _roundIndex, GeceOturumDurumu.Instance.IsSessionActive)) {
            gameObject.SetActive(false);   // OnDisable deregisters (above)
        }
        // TryPickUp == false: slots full — no state change, prompt already
        // reads "Eller Dolu" (AC3, revised).
    }

    // Remaining IInteractable members — all no-op stubs for the Instant
    // type (enumerated precisely, unity-specialist review 2026-08-08):
    public void OnFocusEnter() { }
    public void OnFocusExit() { }
    public void OnHoldProgress(float t) { }
    public void OnHoldComplete() { }
    public void OnHoldCancelled() { }
    public void OnHoldBlocked() { }
}
```

```csharp
// Lives in the BALLROOM scene — plain trigger zone, no IInteractable
// (GDD: delivery is automatic, buttonless).
// HARD RULE (control-manifest candidate — same class of rule as
// ADR-0010's TryGetComponent same-GameObject constraint, and ADR-0011's
// validated trigger-callback finding): this component MUST live on the
// same GameObject as the trigger Collider itself — Unity never delivers
// OnTriggerEnter to a parent. If the authored prefab puts the collider
// on a child, use ADR-0011's ElevatorTriggerZoneRelay there and have
// this component subscribe to it instead.
public sealed class DropOffZone : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;
        GorevTasimaDongusu.InternalInstance.DeliverAll(GeceOturumDurumu.Instance.IsSessionActive);
        // idempotent — 0-carried is a no-op (GDD AC12); session-inactive
        // freezes deliveries (GDD Edge Cases, "round akışı donar")
    }
}
```

```csharp
// Lives on the persistent Player GameObject (ADR-0003), alongside
// FirstPersonController — owns the pooled slot representations (N
// pre-allocated, visibility/socket-offset only, no instantiate/destroy
// per GDD Core Rules), the Highlight(round) prominence curve, and the
// SetCarrying mirror. This is the concrete answer to the mirror
// mechanism ADR-0003 left open ("IsCarrying mirrored from Görev/Taşıma"):
// this component subscribes to the facade's OnCarryingStarted/Ended and
// calls its sibling FirstPersonController.SetCarrying(bool) directly
// (same GameObject, GetComponent-wired once in Awake) — the write stays
// physically inside the Player object, matching ADR-0003's "written only
// by FirstPersonController (internal set)" registry framing: external
// systems never touch PlayerStateProvider's setter; they raise an event
// this component translates.
public sealed class CarrySlotRigController : MonoBehaviour {
    [SerializeField] private Transform _handSocket;
    [SerializeField] private Transform[] _slotRepresentations;   // N pre-allocated pooled visuals

    private FirstPersonController _fpc;

    private void Awake() { _fpc = GetComponent<FirstPersonController>(); }

    private void OnEnable() {
        GorevTasimaDongusu.Instance.OnCarryingStarted += HandleCarryingStarted;
        GorevTasimaDongusu.Instance.OnCarryingEnded += HandleCarryingEnded;
        GorevTasimaDongusu.Instance.OnRoundActivated += HandleRoundActivated;
    }
    private void OnDisable() {
        GorevTasimaDongusu.Instance.OnCarryingStarted -= HandleCarryingStarted;
        GorevTasimaDongusu.Instance.OnCarryingEnded -= HandleCarryingEnded;
        GorevTasimaDongusu.Instance.OnRoundActivated -= HandleRoundActivated;
    }

    private void HandleCarryingStarted() => _fpc.SetCarrying(true);
    private void HandleCarryingEnded() => _fpc.SetCarrying(false);

    private void HandleRoundActivated(int roundIndex) {
        // Highlight(round) = lerp(1.0, 0.30, smoothstep(roundIndex/(roundCount-1)))
        // — the GDD's locked placeholder curve, with its own mandated
        // roundCount<=1 guard (Highlight pinned to 1.0, denominator never
        // computed — GDD Visual Requirements guard rail).
        // Applied to the slot representations' light/rim values only;
        // slot presence/position readability is NEVER affected (GDD AC14).
        // Sway reads FPC's distance-based phase accumulator — the same
        // one driving head-bob/footsteps — NEVER an independent timer
        // (GDD Visual Requirements, phase-source rule).
    }

    // Per-slot visibility updates on pickup/delivery, the 0.05-0.1s
    // socket-offset settle impulse (spring curve, no Animator/blend-tree —
    // GDD AC15's component-absence test enforces this structurally), and
    // jostle one-shot triggering (movement-vector direction-change
    // threshold + min-interval guard, round-independent per GDD AC16)
    // are implementation details of this component, constrained by the
    // GDD's own ACs rather than re-specified here.
}
```

### Logical vs. physical round activation — resolving the GDD's same-frame rule against scene reality

GDD Edge Cases/AC8 require round N+1 activation "in the same frame" as round N completion, with no yield between. But round completion always happens at the ballroom drop-off while the depot scene — where round N+1's `CarryItemPickup` objects physically live — is either unloaded or (briefly, during the SOFT co-residency window) loaded-but-inactive. **This ADR resolves the apparent contradiction by splitting activation into two layers, both deterministic:**

- **Logical activation is synchronous and same-frame**, exactly as the GDD demands: `DeliverAll()` advances `_currentRoundIndex`, clears `_collectedItemIds`, writes `SetRoundState`, and fires `OnRoundActivated` — all within the same call, no yield. This is what AC8/AC9a's mocked test observes, and it is the layer `Sahne Kesmeli Anlatı`'s saturation logic depends on.
- **Physical registration happens at depot scene load**, via `CarryItemPickup.OnEnable()`'s top-of-body self-restore: on the next depot load, the new round's items find `_roundIndex == CurrentRoundIndexForRestore` and proceed to `Register`, while previous rounds' items — and any already-collected current-round items — deactivate before the `Register` line runs. No central "round spawn controller" exists: each item is self-managing, which also makes the restore correct regardless of *when* the depot loads relative to the round change (including a delivery inside the co-residency window, where the still-loaded depot's next-round items remain inactive until the depot's inevitable `DelayedUnload`+reload cycle re-runs their `OnEnable`).

The GDD's AC8 wording ("registered to `InteractableRegistry` in the same frame") is thus satisfiable only in its mocked form (AC9a) — physically, the same-frame register target does not exist in a loaded scene at completion time. This is not a deviation: the GDD's own AC9a/AC9b split already acknowledges the mocked path is the testable-now form, and the Edge Case's real intent (no observable intermediate state, no chained empty-round hazard) is fully preserved by the synchronous logical layer.

### Build-time validation

Per the GDD's Edge Cases and this project's established shared-editor-utility pattern (`ani-tetikleyici-etkilesim.md`'s `IPreprocessBuildWithReport` pass, joined by ADR-0007's and ADR-0012's checks): **(1)** an `OnValidate()` on `TaskListDef` gives immediate Editor feedback; **(2)** the shared `IPreprocessBuildWithReport` pass gains a `ValidateTaskLists` check — `AssetDatabase.FindAssets("t:TaskListDef")`, assert 3 ≤ round count ≤ 5, every round has 1..N items, N ≥ 1 (the GDD's letter blocks only N=0; the Tuning Knobs 2-4 band is a safe *range*, enforced as an `OnValidate` warning rather than a build block — aligned during TD-ADR review, which caught the draft's own sections drifting between N≥2 and N=0), all scene `CarryItemPickup._itemId`s are unique per depot scene, **and — added during TD-ADR review (2026-08-08) — for every round index, the count of scene pickups tagged with that `_roundIndex` equals `TaskListDef.Rounds[i].Items.Length`** (scene-scan step, same mechanism as `ani-tetikleyici-etkilesim.md`'s `TriggerMode` scene-scan). Without the count cross-check, a scene/asset desync would silently let a round "complete" with an item still in the world (violating AC6's letter) or never complete at all (the soft-lock class the GDD's Edge Cases treat as build-blocked) — the Consequences section already promised a "consistency scan" the validation didn't previously specify. `BuildFailedException` on any violation, no runtime clamping anywhere (GDD AC1/AC11).

## Alternatives Considered

### Alternative 1: State machine embedded in a scene `MonoBehaviour` (no pure C# split)
- **Description**: Implement round/slot logic directly in a depot-scene manager `MonoBehaviour`.
- **Pros**: Fewer types.
- **Cons**: Violates `coding-standards.md`'s BLOCKING unit-test rule and the GDD's own AC9a (mocked full-cycle test); worse, a scene-local `MonoBehaviour` cannot hold state across the depot↔ballroom swap at all — the GDD's Kalıcılık rule explicitly forbids scene-local state for exactly this reason.
- **Rejection Reason**: Contradicts both the coding standard and the GDD's own explicit persistence mandate; not a real option.

### Alternative 2: Fold all round/slot state into `GeceOturumDurumu` (extend ADR-0006)
- **Description**: Since `CurrentRoundIndex`/`TotalRoundCount` already live on `GeceOturumDurumu`, move `CollectedItemIds`/`CarriedCount`/the whole state machine there too — one fewer facade.
- **Pros**: One fewer static service; all "session facts" in one place.
- **Cons**: ADR-0006 deliberately relocated only the two counters other systems need to *read* (architecture.md Principle #3: the fact's storage moves down, the computing logic does not). The carry loop's full state is read by no one else — moving it would put Feature-layer game logic inside a Foundation service, inverting the ownership boundary ADR-0006 carefully preserved, and would bloat `IGeceOturumDurumuState` with members only one system uses.
- **Rejection Reason**: Directly contradicts ADR-0006's own scoping rationale and architecture.md Principle #3's "storage moves down, logic does not" — the logic here is the system.

### Alternative 3: Central `RoundSpawnController` in the depot scene managing item activation
- **Description**: A depot-scene manager holds per-round item lists and activates/deactivates them on `OnRoundActivated`/scene load, instead of items self-restoring in `OnEnable()`.
- **Pros**: One place to see all rounds' items; items need no `_roundIndex` field.
- **Cons**: The controller only exists while the depot is loaded, but round changes happen while it isn't — it would need its own restore-on-load logic anyway, just centralized; a serialized per-round item-list is a second source of truth that can silently drift from the items' own placement (the exact content-desync class the build-time id-uniqueness check exists to prevent); and `OnRoundActivated` firing while the depot is unloaded would need a queued-replay mechanism the self-restore approach simply doesn't need.
- **Rejection Reason**: Strictly more machinery for the same behavior — the `OnEnable()`-top self-restore is already mandated by the GDD for the collected-items case, and extending it to cover round membership costs one serialized field.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #13; supplies the exact event contract ADR-0015 (End-Condition Orchestration) consumes, so that ADR can cite signatures instead of re-deriving them.
- Defines the previously-open `SetCarrying` mirror mechanism (ADR-0003's "mirrored from Görev/Taşıma") concretely, keeping the write physically inside the Player object per the registry's framing.
- The logical/physical activation split resolves the GDD's same-frame rule against scene reality without weakening either — the mocked test (AC9a) and the real-scene test (AC9b) both remain honest.
- Fully event-driven — no per-frame cost, no tick-handoff complexity (contrast ADR-0011).

### Negative
- `CarryItemPickup`'s `_itemId`/`_roundIndex` are hand-authored serialized fields — a content-authoring desync risk (mitigated by the build-time uniqueness/consistency scan, but the scan itself is more editor code to maintain).
- The GDD's AC8, read literally ("registered same frame"), is satisfiable only in mocked form — future readers must understand the logical/physical split or the AC looks violated. Flagged prominently in Decision rather than silently reinterpreted.
- AC3's letter is revised (user decision, 2026-08-08): slots-full items keep `CanInteract=true` (focusable, prompt visible) with the rejection inside `TryPickUp`, because ADR-0010's pipeline can never show a prompt for a `CanInteract=false` object — the GDD's original wording was internally contradictory. Player-facing behavior is exactly as the GDD intended.
- **Full sync-obligation list (expanded during TD-ADR review, 2026-08-08 — the original flag covered only AC3, roughly a third of the texts this ADR actually revises)**, surfaced at write-approval:
  - `gorev-tasima-dongusu.md` AC3 (the `CanInteract=false`-AND-prompt wording);
  - `gorev-tasima-dongusu.md` Core Rules > Alma ("Slotlar dolduğunda `CanInteract=false`...") — same letter, second location;
  - `gorev-tasima-dongusu.md` UI Requirements ("'Eller Dolu' prompt'u... `CanInteract=false` durumunda") — third location;
  - `gorev-tasima-dongusu.md` Edge Cases' N=0 rationale (soft-lock now manifests via permanent `TryPickUp` rejection with a visible prompt, not via `CanInteract`) — mechanically stale prose, behavior still build-blocked;
  - `gorev-tasima-dongusu.md` Core Rules > Kalıcılık ("her `CarryItemPickup` kendi `Awake()`'inde") — the Awake→OnEnable restore move (QQ-07 fix);
  - `gorev-tasima-dongusu.md` AC8's "registered same frame" letter (satisfiable only in mocked form — see Decision's logical/physical split);
  - `architecture.md` Data Flow §3 + the API Boundaries `IInteractable` invariant (both mandate the now-superseded query-in-`Awake()` mechanism) and the QQ-07 Open Questions row (now closed by this ADR).

### Risks
- **Risk**: A delivery inside the SOFT co-residency window leaves the still-loaded depot's next-round items inactive until the depot unloads and reloads. If ADR-0008's `DelayedUnload` were ever removed/lengthened dramatically, a player could theoretically return to a depot whose new-round items never activated. **Mitigation**: unreachable under current contracts — the elevator is the only depot access, and re-entering it forces a fresh depot load (ADR-0011); the depot's unload is guaranteed 0.5-2s after swap (ADR-0008). Flagged so any future revision of those timings re-checks this dependency; the Play-mode verification item (Engine Compatibility) covers the current behavior explicitly.
- **Risk**: `GorevTasimaState`'s constructor builds a lambda referencing `GeceOturumDurumu.InternalInstance` — if that were *evaluated* at construction (FoundationBootstrap time), it would be a boot-ordering hazard. **Mitigation**: the reference is resolved lazily inside the lambda body at `ActivateRound()` time (real gameplay, well past boot), not captured at construction — noted inline in the code sketch; a `[Test]` constructing the facade before any `GeceOturumDurumu` reset would catch a regression to eager capture.
- **Risk (hardened, unity-specialist review, 2026-08-08)**: `StartNight()`'s caller is undefined in this ADR — someone must read the `TaskListDef` asset and call `StartNight(N, itemsPerRound)` at night begin. This deferral carries a **hard ordering constraint**: before `StartNight()`, `CurrentRoundIndexForRestore == -1`, so if the depot scene's objects activate first, every round-0 `CarryItemPickup` evaluates `activeRound == false` in its `OnEnable` restore and self-deactivates — and nothing ever reactivates them (`OnRoundActivated(0)` fires synchronously inside `StartNight()` with the items already dead), making round 0 permanently uncollectable: exactly the 0-item-round soft-lock class the GDD's Edge Cases treat as a build-blocked hazard. **Mitigation**: the constraint is stated here as binding on ADR-0015 / the boot-sequence story — *`StartNight()` must complete before the depot scene's objects first activate* — and a `[Test]` asserts `IsFocusable`/restore behavior at index `-1` is a defined all-inactive state, not undefined. Deliberately still deferred (night-begin orchestration is `Sahne Kesmeli Anlatı`-adjacent scope), but now with the ordering contract explicit rather than silently assumed.
- **Risk**: `OnFinalRoundStarted` firing inside `ActivateRound(0)` for a 1-round `TaskList` (GDD AC17's defensive case) happens during `StartNight()` — a subscriber that subscribes *after* night start would miss it. **Mitigation**: same class of concern every event-exposing facade here carries; `Sahne Kesmeli Anlatı` (the only consumer) subscribes at its own `Start()`, before night-begin orchestration runs, per its GDD's own sequencing — ADR-0015 must preserve this ordering and should state it explicitly.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `gorev-tasima-dongusu.md` | `TaskList`/`CarryRound`/`CarryItemDef` data model, config/state split | `TaskListDef`/`CarryRound`/`CarryItemDef` ScriptableObjects (Decision) |
| `gorev-tasima-dongusu.md` | `Instant` pickup, focus gate, "Eller Dolu" prompt (AC2/AC3 — AC3's letter revised, see Decision) | `CarryItemPickup : IInteractable`, `IsFocusable()`/`TryPickUp()`'s slots-full rejection, `PromptText` reading `AreSlotsFull` |
| `gorev-tasima-dongusu.md` | `SetCarrying` exactly once per 0→1/1→0 (AC4/AC5) | `OnCarryingStarted`/`OnCarryingEnded` fire only on those exact transitions; `CarrySlotRigController` translates to `FirstPersonController.SetCarrying` |
| `gorev-tasima-dongusu.md` | Static-service persistence, depot-reload restore via `CollectedItemIds` (Kalıcılık) | `GorevTasimaDongusu` facade (in-place reset); `CarryItemPickup.OnEnable()`-top self-restore before `Register` — closes QQ-07 |
| `gorev-tasima-dongusu.md` | Round complete = all items gone AND slots empty; partial delivery leaves round open (AC6/AC13) | `DeliverAll()`'s two-condition check |
| `gorev-tasima-dongusu.md` | Same-frame round activation, no yield (Edge Cases, AC8) | Logical activation synchronous in `DeliverAll()`; physical registration deterministic at depot load — see Decision's split |
| `gorev-tasima-dongusu.md` | `OnTaskListCompleted`/`OnFinalRoundStarted`/`OnFinalRoundItemPickedUp`/`HasCarriedInFinalRound` once-only semantics (AC7/AC17/AC18) | `CarryLoopStateMachine`'s guarded event firing |
| `gorev-tasima-dongusu.md` | Round counters written to Foundation (ADR-0006 path) | `_writeRoundState` → `GeceOturumDurumu.InternalInstance.SetRoundState`, every `ActivateRound` |
| `gorev-tasima-dongusu.md` | Delivery idempotent, 0-carried no-op, double-fire safe (AC12) | `DeliverAll()`'s `CarriedCount == 0` guard |
| `gorev-tasima-dongusu.md` | Build-blocking validation: 3-5 rounds, 1..N items, N≥2, no runtime clamp (AC1/AC11) | `OnValidate` + shared `IPreprocessBuildWithReport` `ValidateTaskLists` check (Decision) |
| `gorev-tasima-dongusu.md` | Pooled slot visuals, `Highlight(round)` curve + `roundCount≤1` guard, FPC phase-accumulator sway, no Animator (AC14/AC15) | `CarrySlotRigController` (Decision) — component-absence test per AC15 |
| `gorev-tasima-dongusu.md` | Session-inactive: silent reject, round flow frozen, state preserved (AC10, Edge Cases) | `IsFocusable`'s `isSessionActive` gate; `DeliverAll(isSessionActive)`'s freeze guard |
| `architecture.md` | Module Ownership row — `TaskList`/`CarryRound` state, `CollectedItemIds`, pooled visuals, `HasCarriedInFinalRound`, events, round-counter write path | Implemented as designed |

## Performance Implications
- **CPU**: Fully event-driven — zero per-frame cost from this system's logic; pickup/delivery are single small method calls; `Highlight` recomputes once per round change. Negligible.
- **Memory**: One `HashSet<string>` (≤ N entries per round), N pooled slot-representation Transforms, a handful of ScriptableObject assets. Negligible.
- **Load Time**: `TaskListDef` is a plain serialized reference (no Addressables needed at this size — same reasoning as ADR-0012's config).
- **Network**: N/A.

## Migration Plan
No existing code to migrate (`Görev/Taşıma Döngüsü` is not yet implemented).

**`FoundationBootstrap.ResetAll()` registration** (unity-specialist review, 2026-08-08; count corrected during TD-ADR review): `GorevTasimaDongusu.ResetOnLoad()` is inserted into ADR-0001's ordered `ResetAll()` list (a cross-file edit to ADR-0001 at write time). Position: dependency-free — this facade has no constructor-time subscription to any Foundation service (the `SetRoundState` lambda resolves `GeceOturumDurumu.InternalInstance` only at invocation time, during real gameplay), so it appends at the end without triggering the `constructor_subscribing_foundation_service_reset_before_event_source` ordering rule. **Reconciliation note (TD-ADR review, 2026-08-08)**: ADR-0011 (`ElevatorSystem.ResetOnLoad()`) and ADR-0012 (`DiyalogAnlatiIcerigi.ResetOnLoad()`) both state they are registered in `ResetAll()`, but ADR-0001's own code block was never actually edited — it still lists only the original five Foundation entries. The write-time edit to ADR-0001 therefore adds **all three** pending entries (0012, 0011, 0013 — each dependency-free, appended after the five, order among the three immaterial), closing an accumulated cross-file debt, not just this ADR's own line.

**Registry note**: at Step 6, `gorev-tasima-dongusu` should be added to `session_scoped_state_static_facade`'s consumers, a new `state_ownership` entry registered for the carry-loop state (multi-consumer: `Sahne Kesmeli Anlatı` reads events/flags, `CarrySlotRigController` drives the `IsCarrying` mirror; recorded as the pattern's **first Feature-layer consumer**, per TD-ADR review's layer correction), and the `SetCarrying`-mirror mechanism recorded against ADR-0003's `player_state` entry as a `referenced_by` update. The entry should also note the widened public event surface relative to `architecture.md`'s Module Ownership row (`OnCarryingStarted/Ended`, `OnRoundActivated`, `CurrentState`, `CarriedCount`, `AreSlotsFull` beyond the three events + `IsFinalRoundActive` listed there — all intra-system or downstream consumers, no layering change) so the architecture doc's event table doesn't silently drift.

## Validation Criteria
- A `[Test]` drives `CarryLoopStateMachine` through the GDD's AC9a mocked full-happy-path: 3 rounds, sequential `TryPickUp`→`DeliverAll` cycles, asserting per-round completion, `OnTaskListCompleted` exactly once, empty queue/slots at end — no scene/collider/elevator involved.
- A `[Test]` asserts `OnCarryingStarted` fires exactly once per 0→1 transition and never on subsequent pickups (AC4); `OnCarryingEnded` exactly once per 1→0.
- A `[Test]` asserts partial delivery (M<N collected) returns to `Idle` without completing the round; remaining items stay pickable (AC13/AC6).
- A `[Test]` asserts `DeliverAll()` with 0 carried is a pure no-op, including a double-fire simulation (AC12).
- A `[Test]` asserts `IsFocusable` rejects when: session inactive (AC10), item not in active round, or item already collected — and stays `true` when slots are full; `TryPickUp` rejects the slots-full case with no state change while `AreSlotsFull` reads `true` (AC3, revised semantics — user decision 2026-08-08).
- A `[Test]` asserts `DeliverAll(false)` (session inactive) is a pure no-op — carried state preserved, no round completion, no events (GDD Edge Cases' round-flow freeze).
- A `[Test]` asserts `HasCarriedInFinalRound`/`OnFinalRoundItemPickedUp` fire exactly once, on the first final-round pickup only (AC18); `OnFinalRoundStarted` exactly once per night, including the 1-round-night `StartNight` case (AC17); `IsFinalRoundActive` remains `true` at `AllRoundsComplete` (GDD States note).
- A `[Test]` asserts the injected `_writeRoundState` stub is called with `(0, total)` at `StartNight` and `(i, total)` on every subsequent activation (AC19's counter sequence, via ADR-0006's write path).
- An EditMode test asserts `ValidateTaskLists` throws `BuildFailedException` for: round count outside 3-5, a 0-item round, a round exceeding N, N=0, duplicate `_itemId`s, and a scene-vs-`TaskListDef` per-round item-count mismatch (AC1/AC11 + the TD-ADR count cross-check); and that `OnValidate` warns (without blocking) for N outside the 2-4 Tuning Knobs band.
- A structural `[Test]` asserts the carry-rig prefab contains no `Animator`/blend-tree components (AC15, component-absence check).
- A `[Test]` asserts the jostle selection function returns identical parameters for every round index 0..roundCount-1 (AC16, round-independence).
- A `[UnityTest]` covers AC9b's real-scene path (deferred per the GDD itself until elevator + level exist) and the co-residency-window delivery case (Engine Compatibility → Verification Required).
- A `[UnityTest]` (Reload Scene disabled, two simulated sessions) confirms in-place reset preserves live subscriptions, per ADR-0011's forbidden-pattern precedent.
- A `[UnityTest]` (Reload Scene disabled, two simulated sessions — added, unity-specialist review 2026-08-08, closing QQ-07's forwarded test obligation from ADR-0001) confirms the `OnEnable`-top restore re-runs correctly across a Play Stop→Play boundary: session 2's fresh `CollectedItemIds` (cleared by `ResetAll()`) yields all-active round-0 items, with no session-1 leftovers.
- A `[Test]` asserts restore/`IsFocusable` behavior when `CurrentRoundIndexForRestore == -1` (before `StartNight()`) is the defined all-inactive/unfocusable state — the guard behind the `StartNight`-before-depot-activation ordering constraint (Risks).

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — the facade mechanism, mandated by the GDD itself.
- ADR-0003 (Player State) — `IsCarrying` mirror target; this ADR defines the mirror mechanism ADR-0003 left open.
- ADR-0004 (InteractableRegistry) — `OnEnable`/`OnDisable` registration semantics the restore ordering depends on (`Deregister` of a never-registered object confirmed a safe no-op).
- ADR-0006 (Session State) — `SetRoundState` write path, consumed verbatim.
- ADR-0010 (Interaction State Machine) — the `Instant` pipeline `CarryItemPickup` implements against.
- ADR-0008 (Scene Transition State Machine) — source of the `DelayedUnload` 0.5-2s contract the co-residency-window delivery risk and the logical/physical activation argument both lean on.
- ADR-0011 (Elevator State Machine) — the traversal this loop rides passively; source of the in-place-reset forbidden pattern this facade obeys and the trigger-relay shape `DropOffZone` reuses.
- Future ADR-0015 (End-Condition Orchestration) — consumes this ADR's event contract; owes `StartNight()`'s caller definition (flagged in Risks).
