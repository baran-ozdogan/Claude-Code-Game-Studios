# ADR-0015: End-Condition Orchestration (Sahne Kesmeli Anlatı)

> **Unity Specialist Validation**: BLOCKING (4 findings, resolved — 2 by user decision) 2026-08-08 — (1) `Start()` ran night-begin orchestration *before* subscribing, inverting the ordering ADR-0013's Risks made binding on this exact ADR (the defensive 1-round-`TaskList` case fires `OnFinalRoundStarted` synchronously inside `StartNight()`, which would have been silently missed) — reordered. (2) The partial in-place-reset conversion's "re-wire inside `ResetOnLoad()`" description was **internally contradictory**: correct only while `IsikVolumeDurumSistemi` stays replacement-reset, and a live per-session subscription-accumulation bug the day it converts — which ADR-0011's forbidden pattern already requires (ADR-0009's persistent subscriber); `AnlatiDurumIpucuTakibi` carried the mirror-image stale-handler hazard. **User decision: all three facades convert to in-place together** — constructor subscriptions run once per process on never-replaced instances, no re-wire anywhere. (3) The boot-SOFT call's "anchor copy is guarded per its GDD" claim was unverifiable (the method body is never defined; the GDD's anchor contract presupposes a source scene), and nothing positioned the player at the depot spawn — **user decision: depot-authored `InitialSpawnAnchor`, applied in the boot load's `onComplete`** via ADR-0003's repositioning path; the null-guard flagged as an ADR-0008 write-time clarification. (4) The task-side preload path never set `CurrentState = Preloaded`, contradicting the draft's own comments and the GDD's States table — fixed. Plus 7 MINOR (symmetric unsubscription; `_machine`-cache forbidden-pattern citation; `NightBeginPending` sketched into the contract rather than prose-only, with the re-run excluding the initial load; `NightConfigDef`/scene-name build checks; the post-Ready `PreloadHardCut` re-call pinned; `NotifyTriggerFailed`'s `Preloaded` relabeled debug-observability-only; the quick spec's AC1 stale `Full` letter added to the sync list).
> **Technical Director Review (TD-ADR)**: CONCERNS (revised, 1 finding user-confirmed) 2026-08-08 — 3 mandatory findings, fixed: (1) a **real lifecycle defect in the first fix pass**: `Start()`-time subscribe paired with `OnDisable`-unsubscribe meant session 1's exit tore down all subscriptions and session 2 (no `Start()` re-run under Reload Scene: Off) listened to nothing — the night could never end; **user decision: full `OnEnable`/`OnDisable` symmetry** (ADR-0013's actual shape; night-begin stays in `Start()` since `SceneTransitionManager.Instance` isn't guaranteed during the OnEnable pass; a recorded deviation from `adaptif_ses_execution_context`'s "subscriptions at Start()" letter). (2) The conversion turns ADR-0009's never-unsubscribed `AdaptifSesController` subscription into an accumulation bug under Scene-Reload-ON — its companion revision upgraded from "note" to mandated code change. (3) The conversion's cross-file blast radius was undercounted by four: ADR-0001's generic sketch AND worked example (which is `GeceOturumDurumu` itself) still show replacement reset; ADR-0014's reset-ordering Risks bullet becomes false; the registry's `wholesale_state_replacement` example clause was inaccurate even at registration; two ADR-0001-era forbidden patterns' `why` texts (`caching_...`, `constructor_subscribing_...`) need post-conversion revisions — all added to the Migration Plan. Plus 7 minor, fixed (the session-2 residual honestly rescoped — the carry loop's depot items are fully dead in Editor session 2 regardless, not an "Awake-suppressed subset"; the Boot-Sequence deferral argued honestly against its already-half-fired trigger; `InitialSpawnAnchor` build-validated + registered as a scene contract; and stale "flagged" phrasings synced). Verified clean explicitly: full GDD-fidelity trace (tie priority, deferral, preload thresholds, `Abrupt`/lock branching, once-guard, `EndSession`-on-success-only, States table), the AC1 stale-`Full` claim confirmed true against the spec text, and the all-three conversion regime confirmed internally closed (no replacement-reset facade with a constructor subscription remains).

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Feature (pure event orchestration; ScriptableObject config; no new engine surface) |
| **Knowledge Risk** | LOW — every mechanism consumed here (`RequestHardCut`/`PreloadHardCut`, `RequestMovementLock`, the ADR-0001/0009 hybrid facade shape, C# events, `ScriptableObject` config) is an already-validated project pattern. This ADR introduces no new engine API at all. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/architecture/adr-0003-player-state-and-movement-lock.md`, `docs/architecture/adr-0006-session-state-service-and-round-counter-ownership.md`, `docs/architecture/adr-0008-scene-transition-state-machine.md`, `docs/architecture/adr-0009-audio-architecture.md`, `docs/architecture/adr-0013-carry-loop-and-round-state.md`, `docs/architecture/adr-0014-memory-trigger-orchestration.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | The full end-to-end chain (Hold complete → `AddFiredTrigger` → `Held` → `OnTriggerSettled` → saturation evaluation → `RequestHardCut(Abrupt=true)`; and the deferred task-completion path with an in-flight trigger) should be exercised in Play mode once all consumed systems exist — this is MVP's single most cross-system code path. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (Player State) — `RequestMovementLock(this, scope)`/`ReleaseMovementLock(this)`, both scopes. ADR-0006 (Session State) — `SettledTriggerIds.Count`/`FiredTriggerIds.Count` queries, `OnTriggerFired`/`OnTriggerSettled` events, `EndSession()`, and the in-place-reset conversion this ADR mandates (see Decision). ADR-0008 (Scene Transition) — `PreloadHardCut`/`RequestHardCut`/`HardCutConfig.Abrupt`. ADR-0009 (Audio) — the hybrid execution-context shape reused; also the `Abrupt` audio branching consumer this ADR's flag feeds. ADR-0013 (Carry Loop) — `OnTaskListCompleted`/`OnFinalRoundStarted`/`OnFinalRoundItemPickedUp`/`IsFinalRoundActive`/`HasCarriedInFinalRound`, plus `StartNight()` (this ADR supplies the owed caller). ADR-0014 (Memory Trigger) — `SetTotalConfiguredTriggerCountForNight` (owed caller supplied here), `TotalConfiguredTriggerCountForNight` read. |
| **Enables** | Story-level implementation of the entire night loop end-to-end — this is the last of `architecture.md`'s 15 Required ADRs. |
| **Blocks** | Any story implementing `Sahne Kesmeli Anlatı`; the night-begin boot story. |
| **Ordering Note** | None — every dependency is already written (Accepted-pending). This ADR closes the Required ADRs list. |

## Context

### Problem Statement

`sahne-kesmeli-anlati-2026-08-02.md` (Quick Spec, the project's most heavily-revised end-condition design — five separate critical findings across three review rounds are already folded into its Core Rules) defines the night's ending: two independent signals in OR (task completion / memory-trigger saturation), a three-flag saturation gate re-evaluated on three events, an in-flight-trigger deferral for the task path, an explicit (b)-over-(a) tie priority, per-ending `Abrupt`/movement-lock-scope branching, eager preload timing, a once-per-night guard, and session closure. The GDD locks the *rules* completely; this ADR supplies the execution mechanics — **and pays two debts prior ADRs explicitly assigned here**: the night-begin orchestrator that must call ADR-0013's `StartNight()` and ADR-0014's `SetTotalConfiguredTriggerCountForNight()` before the depot scene's objects first activate (a binding ordering constraint both ADRs recorded), and the concrete evaluation trigger for the preload's eager fired-count signal (the quick spec defines evaluation triggers for the saturation gate but leaves the preload signal's trigger implicit).

### Constraints

- Must not deviate from the quick spec's Core Rules — this ADR formalizes, it does not redesign. In particular: saturation counts `SettledTriggerIds.Count` (never `FiredTriggerIds.Count` — the saturation-timing fix); the task path defers while `FiredCount > SettledCount`; (b) beats (a) when one evaluation finds both true; `Abrupt=true`→`MovementLockScope.Full`, `Abrupt=false`→`MoveOnly`; preload uses `FiredCount` as its eager signal (unchanged by the saturation-timing fix); `HasTriggeredThisNight` makes the trigger once-per-night; `onComplete` runs `EndSession()` + lock release, `onFailed` releases the lock only.
- Must follow ADR-0009's established hybrid execution shape (`adaptif_ses_execution_context` registry entry: "establishes the general shape for any future... state+MonoBehaviour hybrid"): pure-state facade reset by `FoundationBootstrap.ResetAll()`, `MonoBehaviour` in the existing persistent "Foundation" scene, subscriptions at `Start()`, never constructor-time.
- Must not reference `SceneTransitionManager.Instance` from any constructor/`ResetOnLoad()` (`constructor_time_subscription_to_scenetransitionmanager` forbidden pattern) — all `PreloadHardCut`/`RequestHardCut` calls happen inside event handlers, real-gameplay time.
- The gate/deferral/priority logic is genuine state-machine logic — `coding-standards.md`'s BLOCKING unit-test rule applies; it must be testable with zero Unity types.
- **New obligation this ADR surfaces — the in-place-reset regime conversion (user decision, 2026-08-08)**: `SahneKesmeliAnlatiController` subscribes at `Start()` — MonoBehaviour-persistent lifetime — to `GeceOturumDurumu`'s `OnTriggerFired`/`OnTriggerSettled`. `GeceOturumDurumu` currently resets by **field replacement**, which ADR-0011's `wholesale_state_replacement_for_event_exposing_facade` forbidden pattern bans exactly when an event-exposing facade gains a persistent MonoBehaviour subscriber: after the first `ResetAll()`, this controller's subscriptions would bind to the discarded old instance and silently never fire again. An earlier draft mandated only `GeceOturumDurumu`'s conversion, with a "re-wire the Işık/Volume subscription inside `ResetOnLoad()`" description — **found internally contradictory during unity-specialist review**: the re-wire is correct only while `IsikVolumeDurumSistemi` stays replacement-reset, and becomes a live per-session subscription-accumulation bug (handler fires N+1 times after N resets) the day Işık/Volume converts too — which ADR-0011's forbidden pattern *already requires*, since ADR-0009's `AdaptifSesController` is a persistent `Start()`-time subscriber to it. `AnlatiDurumIpucuTakibi` (constructor-subscribed to Işık/Volume, itself replacement-reset) carries the mirror-image hazard: once Işık/Volume is in-place, each `AnlatiDurum` replacement would leave its discarded instance's handler permanently subscribed (stale-handler accumulation + GC pin). **Resolution (user-confirmed): all three facades — `IsikVolumeDurumSistemi`, `GeceOturumDurumu`, `AnlatiDurumIpucuTakibi` — convert to in-place reset together, in this ADR's write pass.** Constructor-time subscriptions then run exactly once per process, on never-replaced instances, and simply survive every `ResetAll()` — no re-wire anywhere, no accumulation, no orphaning. `ResetAll()`'s ordering remains for *data*-reset sequencing; its subscription-binding rationale (the ADR-0006 ordering fix's original motivation) becomes vestigial and its comments are corrected, but the order itself is kept (harmless, and still the right dependency-documenting shape).

### Requirements

- All five inbound subscriptions (`OnTaskListCompleted`, `OnFinalRoundStarted`, `OnFinalRoundItemPickedUp` from ADR-0013; `OnTriggerFired`, `OnTriggerSettled` from ADR-0006) wired once, at `Start()`, surviving `ResetAll()` via in-place-reset sources.
- `HasTriggeredThisNight`/`TaskListCompletedPending` survive scene swaps and reset per session — facade state, not MonoBehaviour fields.
- Night-begin setup (`SetTotalConfiguredTriggerCountForNight` then `StartNight`) completes before the depot scene's objects first activate — ADR-0013/0014's binding constraint, satisfied by a boot-ordering contract (below).

## Decision

### Night configuration and the night-begin orchestrator

```csharp
[CreateAssetMenu(menuName = "Beyond The Line/Night Config")]
public sealed class NightConfigDef : ScriptableObject {
    [SerializeField] private TaskListDef _taskList;                       // ADR-0013
    [SerializeField] private int _totalMemoryTriggerCountForNight;        // build-verified == MemoryTriggerDef
                                                                           // asset count (ADR-0014's check #5)
    [SerializeField] private string _psychiatrySceneName;                 // RequestHardCut/PreloadHardCut target
    [SerializeField] private string _initialLevelSceneName;               // "Depot" — first gameplay scene
    public TaskListDef TaskList => _taskList;
    public int TotalMemoryTriggerCountForNight => _totalMemoryTriggerCountForNight;
    public string PsychiatrySceneName => _psychiatrySceneName;
    public string InitialLevelSceneName => _initialLevelSceneName;
}
```

This is the "night-configuration asset" ADR-0014 named as a placeholder — finalized here. The shared `IPreprocessBuildWithReport` utility gains its checks (unity-specialist review, 2026-08-08): `_taskList` non-null, both scene-name strings non-empty **and present in the build's scene list** (a typo'd psychiatry scene name would otherwise produce exactly the unfinishable-night outcome `NotifyTriggerFailed` deliberately doesn't recover from), plus an `OnValidate()` for immediate Editor feedback — ADR-0014's pointed-message precedent. The same pass scene-scans the initial level scene for **exactly one `InitialSpawnAnchor`** (TD-ADR review — a missing anchor would NRE inside the boot `onComplete`, exactly the runtime-discovery class these checks exist to prevent). `InitialSpawnAnchor` itself is a `live_monobehaviour_state_static_accessor`-class contract (Awake-set `Instance` + duplicate guard, ADR-0010's registered shape) — a new scene contract level authors must honor, registered at Step 6. The night-begin orchestrator is `SahneKesmeliAnlatiController.Start()` (below): it runs `SetTotalConfiguredTriggerCountForNight`, computes `itemsPerRound` from the `TaskListDef`, calls `StartNight`, **and only then** initiates the initial depot scene load. **Boot-ordering contract** (satisfies ADR-0013/0014's binding constraint structurally, not by convention): the build's boot flow loads only the persistent scenes (UI, Player, Foundation — ADR-0002/0003/0008); no level scene is in the initial load set. The depot scene enters play exclusively through this controller's post-setup load call — so depot objects *cannot* activate before setup, by construction. This is deliberately the smallest possible boot contract; a dedicated Boot Sequence ADR remains deferred — argued honestly against `architecture.md`'s own deferral note (TD-ADR review corrected an earlier misquote): that note's trigger is "a third persistent scene is added OR a genuine same-`Awake()` cross-scene dependency emerges," and the first half already fired at ADR-0008 (the Foundation scene). Deferral still holds because the half that matters for a Boot ADR's actual content has not: no cross-scene `Awake()` dependency exists, sequential awaited persistent-scene loads (ADR-0003's provisional order) remain sufficient, and this ADR adds only a sequencing rule, not a readiness-signal mechanism. The Migration Plan's `architecture.md` edit records this reconciliation in the deferral note itself.

### Pure C# `EndConditionStateMachine`

```csharp
public enum EndConditionState { Watching, Preloaded, Triggering, Complete }

// Plain C# class, no Unity types — the gate/deferral/priority logic is
// genuine state-machine logic under coding-standards.md's BLOCKING test
// rule. All external facts arrive as injected delegates (constructor)
// so a [Test] drives the full matrix with stubs. Same testability split
// as ADR-0003/0010/0011/0013.
public sealed class EndConditionStateMachine {
    public EndConditionState CurrentState { get; private set; } = EndConditionState.Watching;
    public bool HasTriggeredThisNight { get; private set; }
    public bool TaskListCompletedPending { get; private set; }

    // Fired at most once each — the MonoBehaviour translates these into
    // the real PreloadHardCut / (RequestMovementLock + RequestHardCut)
    // calls. bool abrupt: true = saturation tone (Full lock), false =
    // task-completion tone (MoveOnly lock).
    public event Action OnPreloadRequested;
    public event Action<bool> OnTriggerRequested;

    private readonly Func<int> _settledCount;              // GeceOturumDurumu.Instance.SettledCount
    private readonly Func<int> _firedCount;                // GeceOturumDurumu.Instance.FiredCount
    private readonly Func<int> _totalTriggerCount;         // GeceOturumDurumu.Instance.TotalConfiguredTriggerCountForNight
    private readonly Func<bool> _isFinalRoundActive;       // GorevTasimaDongusu.Instance.IsFinalRoundActive
    private readonly Func<bool> _hasCarriedInFinalRound;   // GorevTasimaDongusu.Instance.HasCarriedInFinalRound

    public EndConditionStateMachine(Func<int> settledCount, Func<int> firedCount, Func<int> totalTriggerCount,
                                     Func<bool> isFinalRoundActive, Func<bool> hasCarriedInFinalRound) {
        _settledCount = settledCount; _firedCount = firedCount; _totalTriggerCount = totalTriggerCount;
        _isFinalRoundActive = isFinalRoundActive; _hasCarriedInFinalRound = hasCarriedInFinalRound;
    }

    // ── Inbound notifications (one per subscription) ──────────────────

    public void NotifyTaskListCompleted() {
        if (HasTriggeredThisNight) return;                              // once-per-night guard (GDD)
        // In-flight deferral (GDD, "(a)'nın da in-flight tetikleyicileri
        // beklemesi gerekir"): if any fired trigger hasn't settled yet,
        // remember the completion and wait for the equalizing settle.
        if (_firedCount() > _settledCount()) { TaskListCompletedPending = true; return; }
        Trigger(abrupt: false);
    }

    public void NotifyTriggerFired() {
        if (HasTriggeredThisNight) return;
        TryPreloadFromTriggerSide();
        // NOTE: deliberately NOT a saturation evaluation trigger — the
        // saturation-timing fix counts Settled, and a fire can never
        // complete a Settled count. Fired feeds only the eager preload
        // signal. This resolves the quick spec's implicit evaluation
        // trigger for the preload's FiredCount signal (Context) —
        // OnTriggerFired (ADR-0014's AddFiredTrigger fires it) is the
        // exact moment FiredCount changes, so evaluating here rather
        // than waiting ~3s for the corresponding settle preserves the
        // full preload head start the eager signal exists to provide.
    }

    public void NotifyTriggerSettled() {
        if (HasTriggeredThisNight) return;
        // Priority rule (GDD, "İki koşul aynı anda sağlanırsa"): (b) is
        // evaluated FIRST — if this same settle completes both the
        // saturation gate and the deferred task completion, saturation
        // wins and the night ends Abrupt=true.
        if (EvaluateSaturation()) { Trigger(abrupt: true); return; }
        if (TaskListCompletedPending && _firedCount() == _settledCount()) {
            Trigger(abrupt: false);                                     // deferred task completion lands
        }
    }

    public void NotifyFinalRoundStarted() {
        if (HasTriggeredThisNight) return;
        // Corrected during unity-specialist review (2026-08-08): an
        // earlier draft invoked the preload here WITHOUT setting
        // CurrentState = Preloaded, contradicting both this file's own
        // comments and the GDD's States table (Preloaded's entry
        // condition is "PreloadHardCut çağrıldı" — this path calls it).
        // Note the "already Preloaded via the trigger side first" case
        // is unreachable: TryPreloadFromTriggerSide requires
        // IsFinalRoundActive, which flips true in the same synchronous
        // sequence that invokes this very event (ADR-0013) — the guard
        // below handles it anyway.
        if (CurrentState == EndConditionState.Watching) {
            CurrentState = EndConditionState.Preloaded;
            OnPreloadRequested?.Invoke();                               // task-side preload threshold (GDD);
                                                                        // duplicate calls are SceneTransition-
                                                                        // Manager's own no-op (see the pinned
                                                                        // Ready-re-call note in Risks)
        }
        if (EvaluateSaturation()) Trigger(abrupt: true);                // (b) re-evaluation trigger #2 (GDD)
    }

    public void NotifyFinalRoundItemPickedUp() {
        if (HasTriggeredThisNight) return;
        if (EvaluateSaturation()) Trigger(abrupt: true);                // (b) re-evaluation trigger #3 (GDD) —
                                                                        // the early-saturated player's path
    }

    // ── Internals ─────────────────────────────────────────────────────

    private bool EvaluateSaturation() =>
        _settledCount() == _totalTriggerCount()                         // all triggers genuinely Held
        && _isFinalRoundActive()                                        // final round reached (GDD guard #2)
        && _hasCarriedInFinalRound();                                   // at least one final-round pickup (guard #3)

    private void TryPreloadFromTriggerSide() {
        if (CurrentState != EndConditionState.Watching) return;         // already preloaded/beyond
        if (_isFinalRoundActive() && _firedCount() == _totalTriggerCount() - 1) {
            CurrentState = EndConditionState.Preloaded;
            OnPreloadRequested?.Invoke();
        }
    }

    private void Trigger(bool abrupt) {
        HasTriggeredThisNight = true;                                   // set BEFORE the outbound event —
        TaskListCompletedPending = false;                               // re-entrancy-safe (GDD once-guard)
        CurrentState = EndConditionState.Triggering;
        OnTriggerRequested?.Invoke(abrupt);
    }

    public void NotifyTriggerCompleted() { CurrentState = EndConditionState.Complete; }
    public void NotifyTriggerFailed() {
        // GDD: transition failed, the night is technically not over —
        // session NOT ended, lock released (controller side). The guard
        // deliberately stays true: the GDD's own once-per-night rule has
        // no retry provision, and re-arming would contradict it; a
        // failed psychiatry-scene load is a content/build defect, not a
        // runtime state to recover from. NOTE (unity-specialist review,
        // 2026-08-08): "Preloaded" here is a debug-observability label
        // only — ADR-0008's Ready fast path has already consumed the
        // transition-side preload slot (_hardCutPreloadState set to Idle
        // before DoSwap), so no live preload exists behind this state.
        CurrentState = EndConditionState.Preloaded;
    }

    // Task-side preload: the GDD's "son round aktifken" threshold is
    // NotifyFinalRoundStarted above (which sets Preloaded — B4 fix).
    // OnPreloadRequested can still reach SceneTransitionManager twice
    // across both sides' thresholds in edge orderings; in-flight
    // duplicates are ADR-0008's documented no-op, and the post-Ready
    // same-scene re-call is pinned as a clarifying line added to
    // ADR-0008 at this ADR's write time (see Migration Plan).

    internal void ResetOnLoad() {
        CurrentState = EndConditionState.Watching;
        HasTriggeredThisNight = false;
        TaskListCompletedPending = false;
    }
}
```

### Facade and controller (ADR-0009's hybrid shape, second application)

```csharp
public interface ISahneKesmeliAnlatiState {
    bool HasTriggeredThisNight { get; }
}

// Minimal pure-state facade — session-scoped truth (HasTriggeredThisNight,
// TaskListCompletedPending live inside the machine), in-place reset
// (event-exposing? The machine's events have a persistent MonoBehaviour
// subscriber — the controller — so in-place is mandatory per ADR-0011's
// forbidden pattern, and the machine instance is never replaced).
public sealed class SahneKesmeliAnlatiState : ISahneKesmeliAnlatiState {
    // Set by ResetOnLoad() each Play session; cleared by the controller
    // after RunNightBegin() — the per-session re-setup signal (M3 fix:
    // previously described in prose only, now part of the contract).
    public bool NightBeginPending { get; private set; } = true;
    internal void ClearNightBeginPending() => NightBeginPending = false;

    private readonly EndConditionStateMachine _machine = new(
        settledCount: () => GeceOturumDurumu.Instance.SettledCount,
        firedCount: () => GeceOturumDurumu.Instance.FiredCount,
        totalTriggerCount: () => GeceOturumDurumu.Instance.TotalConfiguredTriggerCountForNight,
        isFinalRoundActive: () => GorevTasimaDongusu.Instance.IsFinalRoundActive,
        hasCarriedInFinalRound: () => GorevTasimaDongusu.Instance.HasCarriedInFinalRound);
    // Delegates resolve the facades lazily at INVOCATION time (event-
    // handler time, real gameplay) — nothing is dereferenced at this
    // constructor's FoundationBootstrap-adjacent construction, same
    // lazy-lambda discipline ADR-0013 validated.

    public bool HasTriggeredThisNight => _machine.HasTriggeredThisNight;
    internal EndConditionStateMachine Machine => _machine;
    internal void ResetOnLoad() { _machine.ResetOnLoad(); NightBeginPending = true; }
}

public static class SahneKesmeliAnlati {
    private static readonly SahneKesmeliAnlatiState _state = new();   // never replaced
    public static ISahneKesmeliAnlatiState Instance => _state;
    internal static SahneKesmeliAnlatiState InternalInstance => _state;
    internal static void ResetOnLoad() => _state.ResetOnLoad();       // registered in FoundationBootstrap.ResetAll()
}
```

```csharp
// Lives in the persistent "Foundation" scene (ADR-0008), alongside
// SceneTransitionManager and AdaptifSesController — the third resident,
// per ADR-0009's "reuse the Foundation scene, not a new one" rule.
// Feature-layer logic in a Foundation-hosted MonoBehaviour is a hosting
// convenience, not a layer relocation: every fact it reads flows downward.
public sealed class SahneKesmeliAnlatiController : MonoBehaviour {
    [SerializeField] private NightConfigDef _nightConfig;

    private EndConditionStateMachine _machine;

    // Stored so OnDisable can unsubscribe symmetrically (corrected
    // during unity-specialist review, 2026-08-08: an earlier draft used
    // un-unsubscribable discard-lambdas with no OnDisable — safe under
    // full reload, but this ADR converts its event sources to
    // never-replaced in-place-reset facades, so under the Editor's
    // Domain-off + Scene-Reload-ON config each session's destroyed
    // controller would have left a stale delegate set permanently on the
    // persistent events, double-firing RequestHardCut from session 2 on.
    // ADR-0013's CarrySlotRigController symmetric shape instead.)
    private Action<string> _onTriggerFired, _onTriggerSettled;

    // Corrected during TD-ADR review (2026-08-08, user-confirmed):
    // subscriptions moved from Start() to OnEnable(), SYMMETRIC with
    // OnDisable — the previous Start()/OnDisable pairing was a real
    // lifecycle defect: under "Reload Scene: Off", session 1's exit
    // fires OnDisable (all subscriptions removed) but session 2 never
    // re-runs Start() — every end-condition event would fire into zero
    // handlers and the night could never end, the exact orphaning class
    // the regime conversion eliminates, reintroduced by the controller's
    // own lifecycle. OnEnable re-fires in that scenario, before the
    // first Update() (so still before the NightBeginPending re-run —
    // the subscriptions-before-night-begin ordering ADR-0013 made
    // binding is preserved in BOTH the boot path, OnEnable-before-Start,
    // and the session-2 path, OnEnable-pass-before-first-Update). This
    // is ADR-0013's CarrySlotRigController's actual symmetric shape,
    // and a deliberate, recorded deviation from adaptif_ses_execution_
    // context's "subscriptions at Start()" letter (see the new
    // end_condition_execution_context registry entry for the rationale).
    private void OnEnable() {
        // Cached cross-"session" reference — the LETTER of the
        // caching_session_state_interface_reference_across_session_boundary
        // forbidden pattern, safe here ONLY because the machine instance
        // is structurally never replaced (readonly field, in-place
        // reset). Do not copy this caching for any replaceable facade.
        _machine = SahneKesmeliAnlati.InternalInstance.Machine;

        GorevTasimaDongusu.Instance.OnTaskListCompleted += _machine.NotifyTaskListCompleted;
        GorevTasimaDongusu.Instance.OnFinalRoundStarted += _machine.NotifyFinalRoundStarted;
        GorevTasimaDongusu.Instance.OnFinalRoundItemPickedUp += _machine.NotifyFinalRoundItemPickedUp;
        _onTriggerFired = _ => _machine.NotifyTriggerFired();
        _onTriggerSettled = _ => _machine.NotifyTriggerSettled();
        GeceOturumDurumu.Instance.OnTriggerFired += _onTriggerFired;
        GeceOturumDurumu.Instance.OnTriggerSettled += _onTriggerSettled;

        _machine.OnPreloadRequested += HandlePreloadRequested;
        _machine.OnTriggerRequested += HandleTriggerRequested;
    }

    private void Start() {
        // Night-begin stays in Start(), NOT OnEnable —
        // SceneTransitionManager.Instance is set by its own Awake()
        // within the same Foundation-scene load and is not guaranteed
        // during the OnEnable pass; by Start() every Awake in the scene
        // has run (Unity lifecycle guarantee ADR-0008 already leans on).
        RunNightBegin();
    }

    private void OnDisable() {
        GorevTasimaDongusu.Instance.OnTaskListCompleted -= _machine.NotifyTaskListCompleted;
        GorevTasimaDongusu.Instance.OnFinalRoundStarted -= _machine.NotifyFinalRoundStarted;
        GorevTasimaDongusu.Instance.OnFinalRoundItemPickedUp -= _machine.NotifyFinalRoundItemPickedUp;
        GeceOturumDurumu.Instance.OnTriggerFired -= _onTriggerFired;
        GeceOturumDurumu.Instance.OnTriggerSettled -= _onTriggerSettled;
        _machine.OnPreloadRequested -= HandlePreloadRequested;
        _machine.OnTriggerRequested -= HandleTriggerRequested;
    }

    // ── Night-begin orchestration (the caller ADR-0013/0014 owe) ──────
    // Runs AFTER subscriptions (OnEnable precedes Start) and BEFORE the
    // initial level-scene load, satisfying the binding setup-before-
    // depot-activation constraint structurally. Re-run once per Editor Play session via
    // the NightBeginPending flag (see Update below) — the SETUP CALLS
    // ONLY are re-run; the initial load is process-boot-only (under
    // Reload Scene: Off the depot is still loaded — re-requesting the
    // load would additively load a second depot instance).
    private void RunNightBegin() {
        GeceOturumDurumu.InternalInstance.SetTotalConfiguredTriggerCountForNight(
            _nightConfig.TotalMemoryTriggerCountForNight);
        var itemsPerRound = new int[_nightConfig.TaskList.Rounds.Count];
        for (int i = 0; i < itemsPerRound.Length; i++)
            itemsPerRound[i] = _nightConfig.TaskList.Rounds[i].Items.Length;
        GorevTasimaDongusu.InternalInstance.StartNight(_nightConfig.TaskList.SlotCapacity, itemsPerRound);
        SahneKesmeliAnlati.InternalInstance.ClearNightBeginPending();

        if (!_initialLoadRequested) {
            _initialLoadRequested = true;   // controller-INSTANCE-lifetime (corrected, TD-ADR review):
                                             // under Scene-Reload-ON a fresh controller re-requests the
                                             // load — correct there (level scenes were torn down at stop);
                                             // under Scene-Reload-OFF the surviving instance never
                                             // re-requests — also correct (depot still loaded). Right
                                             // lifetime in both Editor configs.
            SceneTransitionManager.Instance.RequestSoftTransition(
                null, _nightConfig.InitialLevelSceneName, /* config */ default,
                onComplete: () => {
                    // Boot spawn (user decision, 2026-08-08): SOFT's anchor-
                    // copy semantics are relative continuity FROM a source
                    // scene — with fromScene == null nothing positions the
                    // player. The depot scene authors an InitialSpawnAnchor;
                    // the player is repositioned to it here via ADR-0003's
                    // established repositioning path (FPC Transform moved,
                    // GameObject identity unchanged) — the natural sibling
                    // of the SoftTransitionAnchor pattern.
                    PlayerStateProvider.Current.EyeCamera.root.SetPositionAndRotation(
                        InitialSpawnAnchor.Instance.transform.position,
                        InitialSpawnAnchor.Instance.transform.rotation);
                },
                onFailed: reason => Debug.LogError($"Initial level load failed: {reason}", this));
        }
    }

    private void Update() {
        // Editor-only in practice: ResetAll() sets NightBeginPending each
        // Play session; under Reload Scene: Off, Start() doesn't re-run
        // but this one-bool check does. HONEST SCOPE (corrected, TD-ADR
        // review 2026-08-08 — an earlier comment oversold this): the
        // re-run restores Editor iteration for the END-CONDITION machine
        // and the memory-trigger side ONLY. The carry loop's depot items
        // are fully dead in session 2 regardless: surviving items'
        // OnEnable re-runs during the play-entry pass — BEFORE this first
        // Update — evaluates restore-index -1, and self-deactivates
        // (nothing revives a SetActive(false) object); session-1-
        // deactivated items get no OnEnable at all. Same accepted
        // Editor-only limitation class as ADR-0003/0008/0010's
        // Reload-Scene risks; real builds and Scene-Reload-ON are fine.
        if (SahneKesmeliAnlati.InternalInstance.NightBeginPending) RunNightBegin();
    }

    private bool _initialLoadRequested;

    private void HandlePreloadRequested() {
        SceneTransitionManager.Instance.PreloadHardCut(_nightConfig.PsychiatrySceneName);
    }

    private void HandleTriggerRequested(bool abrupt) {
        var scope = abrupt ? MovementLockScope.Full : MovementLockScope.MoveOnly;   // GDD tone/agency branching
        PlayerStateProvider.Current.RequestMovementLock(this, scope);
        SceneTransitionManager.Instance.RequestHardCut(
            _nightConfig.PsychiatrySceneName,
            new HardCutConfig { Abrupt = abrupt },
            onComplete: () => {
                GeceOturumDurumu.Instance.EndSession();                 // GDD: session closes on success only
                PlayerStateProvider.Current.ReleaseMovementLock(this);
                _machine.NotifyTriggerCompleted();
            },
            onFailed: reason => {
                PlayerStateProvider.Current.ReleaseMovementLock(this);  // GDD: lock released, session NOT ended
                _machine.NotifyTriggerFailed();
            });
    }
}
```

## Alternatives Considered

### Alternative 1: Plain static service, no MonoBehaviour (pure ADR-0001)
- **Description**: All logic in the static facade; subscriptions in the constructor.
- **Pros**: One fewer component; no Foundation-scene resident.
- **Cons**: Constructor-time subscription to `SceneTransitionManager`-adjacent flows is banned outright; the night-begin orchestration needs a serialized `NightConfigDef` reference and a defined boot moment, which a plain static class cannot carry; and constructor-time subscription to `GorevTasimaDongusu`/`GeceOturumDurumu` would drag this Feature-layer system into `FoundationBootstrap.ResetAll()`'s ordering graph as a constructor-subscriber — the exact fragility class the `constructor_subscribing_foundation_service_reset_before_event_source` pattern polices.
- **Rejection Reason**: ADR-0009's hybrid shape exists precisely for this profile (state that must survive resets + behavior needing real Unity objects and `Start()`-time subscriptions); deviating from the established shape would need a reason this system doesn't have.

### Alternative 2: Poll the conditions in `Update()`
- **Description**: No subscriptions — evaluate both end conditions every frame.
- **Pros**: No subscription lifetime management at all.
- **Cons**: The quick spec's own N5 critical finding *rejected* the implicit-polling model and mandated the three-event evaluation set; polling also re-introduces same-frame ordering ambiguity for the tie-priority rule the GDD resolved explicitly.
- **Rejection Reason**: Directly contradicts the GDD's own documented correction history; not a real option.

### Alternative 3: Night-begin orchestration in a separate boot component (not this controller)
- **Description**: A dedicated `NightBootstrapper` in the Foundation scene owns `StartNight`/`SetTotalConfiguredTriggerCountForNight`/initial scene load; this controller only watches endings.
- **Pros**: Single-responsibility purity — "the system that ends the night" doesn't also start it.
- **Cons**: Both components would need the same `NightConfigDef` reference and the same boot-ordering contract; splitting them creates an inter-component ordering dependency (bootstrapper before watcher subscriptions) inside one scene for zero behavioral difference — the GDD already frames this system as the night's lifecycle orchestrator ("gecenin ne zaman biteceğine karar veren"), and beginning-of-life setup is the natural other half of end-of-life orchestration.
- **Rejection Reason**: More moving parts for the same result; revisit only if a future multi-night structure gives night-begin real independent complexity (flagged for the Çoklu Gece Vertical Slice pass).

## Consequences

### Positive
- **Closes the Required ADRs list** — all 15 of `architecture.md`'s ADRs are now written; `/architecture-review` can run (in a fresh session) against complete coverage.
- Pays both owed debts (night-begin orchestrator; preload's eager-signal evaluation trigger) with mechanisms the owing ADRs already constrained, and satisfies ADR-0013/0014's binding setup ordering *structurally* (no level scene in the initial load set) rather than by convention.
- Surfaces and fixes a latent cross-ADR hazard class before any code exists: `GeceOturumDurumu`'s replacement-reset + this ADR's persistent subscribers would have silently orphaned the end-condition system after the first Editor `ResetAll()` — the all-three in-place conversion (with ADR-0009's mandated companion unsubscription revision) closes both hazard directions at once.
- The full gate/deferral/priority matrix is a pure C# class driven by stub delegates — MVP's most consequential logic (when the night ends, and in which tone) is exhaustively unit-testable.

### Negative
- `SahneKesmeliAnlatiController` is the Foundation scene's third resident and the second Feature-layer logic host outside its own layer's scenes (after nothing — it is the first; ADR-0009's resident is Foundation-layer). Accepted as a hosting convenience with all reads flowing downward; noted so a future pass doesn't misread it as a layer relocation.
- The boot contract ("no level scene in the initial load set; depot enters via this controller's post-setup load") is a real, binding build-configuration rule enforced by convention + a `[UnityTest]`, not by the compiler — a Boot Sequence ADR would be the structural home if boot complexity ever grows (deferred, per `architecture.md`).
- `NotifyTriggerFailed` deliberately does not re-arm the night (guard stays true) — a failed psychiatry-scene load leaves the night unfinishable by design, treated as a content/build defect rather than a recoverable state. The GDD's own once-per-night rule has no retry provision; flagged rather than silently chosen.

### Risks
- **Risk**: The in-place-reset regime conversion (Constraints — all three of `IsikVolumeDurumSistemi`/`GeceOturumDurumu`/`AnlatiDurumIpucuTakibi` together, user decision 2026-08-08 after the unity-specialist proved the partial-conversion re-wire self-contradictory) touches three Foundation services many systems consume. **Mitigation**: the conversion changes only the reset mechanism — clear data fields on the same instance instead of replacing it; constructor-time subscriptions run once per process on never-replaced instances and are never re-wired (no per-session accumulation, no orphaning); behavior at every read/write site is unchanged; `ResetAll()`'s ordering is kept with its subscription-binding rationale marked vestigial in comments. The `[UnityTest]` matrix (Validation Criteria) covers the two-session **exactly-once** delivery directly.
- **Risk**: `Start()`-time night-begin runs once per *process*, but `ResetAll()` clears session state per Editor *Play session* — under "Reload Scene: Off", session 2+ would have no re-run of `StartNight`/`SetTotalConfiguredTriggerCountForNight` (and, independently, ADR-0013's depot items self-deactivate at restore-index −1). **Mitigation**: accepted as the same documented Editor-only limitation ADR-0003/0008/0009/0010 all carry for this exact setting (real builds always fully reload; the boot flow reloads level scenes each genuine session) — with one improvement: `SahneKesmeliAnlatiState.ResetOnLoad()` sets an internal `NightBeginPending` flag the controller checks once per session (first `Update()` after a reset) to re-run the night-begin block, restoring Editor iteration for everything except already-loaded level scenes' `Awake`-suppressed objects, which remain the known QQ-07-class residual.
- **Risk (corrected, unity-specialist review, 2026-08-08)**: The initial `RequestSoftTransition(null, "Depot", ...)` boot-time call. ADR-0008's deferred-unload half genuinely guards `fromScene == null`; but an earlier draft also claimed the SOFT-anchor copy "is guarded per its GDD" — **unverifiable**: `CopySoftTransitionAnchorTransform`'s body is never defined in ADR-0008, and the GDD's anchor contract presupposes a source scene (relative cabin-local continuity, "Beden Sürekliliği"). And even with a null guard, skipping the copy means nothing positions the player at the depot spawn. **Mitigation (user decision, 2026-08-08)**: the depot scene authors an `InitialSpawnAnchor`; the boot load's `onComplete` repositions the player to it via ADR-0003's repositioning path (Decision code) — spawn is now explicit and scene-owned, not a silent Player-scene-authoring coincidence. The null-`fromScene` anchor-copy guard inside ADR-0008 is flagged for implementation-time verification (a one-line guard, added as a clarifying note to ADR-0008 at write time), and this boot flow is explicitly the minimal MVP fronting of the not-yet-designed "Ana Menü/Başlangıç Akışı" (Vertical Slice) that `seviye-sahne-gecisi.md` assigns first-scene loading to — recorded so that future design knows this ADR holds its MVP placeholder.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `sahne-kesmeli-anlati-2026-08-02.md` | OR-logic, two independent signals, first-wins | `NotifyTaskListCompleted` / `EvaluateSaturation` across the three (b)-events |
| `sahne-kesmeli-anlati-2026-08-02.md` | Three-flag saturation gate on `SettledCount` (saturation-timing fix), re-evaluated on exactly three events | `EvaluateSaturation()` called from `NotifyTriggerSettled`/`NotifyFinalRoundStarted`/`NotifyFinalRoundItemPickedUp` — never from `NotifyTriggerFired` (documented inline) |
| `sahne-kesmeli-anlati-2026-08-02.md` | Task path defers while in-flight triggers exist; `TaskListCompletedPending`; lands on the equalizing settle | `NotifyTaskListCompleted`'s `FiredCount > SettledCount` branch + `NotifyTriggerSettled`'s pending branch |
| `sahne-kesmeli-anlati-2026-08-02.md` | (b) beats (a) on a same-evaluation tie | `NotifyTriggerSettled` evaluates saturation first, returns before the pending branch |
| `sahne-kesmeli-anlati-2026-08-02.md` | `Abrupt=true`→`Full` lock, `Abrupt=false`→`MoveOnly`; lock before `RequestHardCut`; release in both callbacks; `EndSession` on success only | `HandleTriggerRequested` |
| `sahne-kesmeli-anlati-2026-08-02.md` | Preload: task-side at final-round activation, trigger-side at `Fired == Total−1 && final` (eager `FiredCount`, unchanged by the timing fix); duplicate preloads safe | `NotifyFinalRoundStarted` + `TryPreloadFromTriggerSide` (evaluated on `OnTriggerFired` — the evaluation trigger the quick spec left implicit, resolved here) |
| `sahne-kesmeli-anlati-2026-08-02.md` | `HasTriggeredThisNight` once-per-night; second signal is a no-op | Guard checked at every notify entry; set before the outbound event (re-entrancy-safe) |
| `sahne-kesmeli-anlati-2026-08-02.md` | States Watching/Preloaded/Triggering/Complete | `EndConditionState` — same names, same transitions |
| ADR-0013 / ADR-0014 (owed) | Night-begin orchestrator: `StartNight` + `SetTotalConfiguredTriggerCountForNight` before depot activation | `SahneKesmeliAnlatiController.Start()` + the structural boot contract (Decision) |
| `architecture.md` | Module Ownership row — pure orchestration, `HasTriggeredThisNight`, consumes Görev/Taşıma events + `SettledTriggerIds`/`OnTriggerSettled` + `RequestHardCut` + FPC lock | Implemented as designed |

## Performance Implications
- **CPU**: Fully event-driven — zero per-frame logic (the `NightBeginPending` check is a single bool read in `Update()`, Editor-relevant only). Each notification is a handful of int/bool delegate reads. Negligible.
- **Memory**: One machine + facade + controller + one `NightConfigDef` asset. Negligible.
- **Load Time**: N/A. **Network**: N/A.

## Migration Plan
No existing code to migrate.

**Cross-file edits at write time** (blast radius recounted during TD-ADR review — items 7-9 were missing):
1. ADR-0006 — convert `GeceOturumDurumu`'s AND `AnlatiDurumIpucuTakibi`'s resets to in-place (ADR-0007 note for the latter), constructor subscriptions surviving once-per-process, correction notes referencing ADR-0011's forbidden pattern and this ADR's regime decision. **In-place `ResetOnLoad()` must explicitly re-initialize non-default fields that replacement reset restored for free** — notably `GeceOturumDurumuState.IsSessionActive = true` (initializer-true bool); ADR-0007's lazy `ClueRegistry` cache is **preserved** across sessions (immutable config, no reload — and `ResetOnLoad()` must never touch Addressables per `engine_asset_api_call_in_foundation_constructor`).
2. ADR-0001 — `IsikVolumeDurumSistemi`'s reset likewise converted; `ResetAll()` ordering comments corrected (subscription-binding rationale vestigial, data-ordering kept); `SahneKesmeliAnlati.ResetOnLoad()` appended (dependency-free); **AND the Key Interfaces generic sketch + worked example revised** (both show `ResetOnLoad() => _instance = new ...State()` — and the worked example IS `GeceOturumDurumu`, which would directly contradict its own converted shape; the generic shape gains the in-place variant per ADR-0011's rule).
3. ADR-0009 — **companion code revision, not just a note** (TD-ADR M2): the conversion makes `AdaptifSesController`'s never-unsubscribed `Start()`-time Işık/Volume subscription an accumulation bug under Scene-Reload-ON (each session's destroyed controller leaves its handler on the now-persistent event — session N fires every stinger N times); add symmetric `OnEnable`/`OnDisable` subscription pairing matching this ADR's shape, with a correction note. (The `OnTransitionStateChanged` subscription is unaffected — `SceneTransitionManager` is itself destroyed/recreated in that config.)
4. ADR-0008 — two clarifying lines: (a) `PreloadHardCut`'s re-call while an existing same-scene preload sits at `Ready` is a no-op (the in-flight no-op is documented; post-Ready was unspecified — this ADR's dual thresholds can produce it); (b) `CopySoftTransitionAnchorTransform` must guard `fromScene == null` (the boot-time call; flagged in Risks).
5. `architecture.md`'s "Boot Sequence — can defer" note — records this ADR's minimal boot contract AND the honest reconciliation of the already-half-fired deferral trigger (m2).
6. `sahne-kesmeli-anlati-2026-08-02.md` — AC1's stale "`RequestMovementLock(this, Full)` ... `Abrupt=false`" letter synced to its own Core Rules' `MoveOnly` (verified: the 2026-08-04 fix updated the Dependencies line but missed AC1).
7. ADR-0014 — its Risks bullet "if `ResetAll()`'s ordering were changed... the subscription would bind stale and settles would silently drop" is **false under the converted regime** (once-per-process binding to a never-replaced instance; ordering is data-only) — correction note.
8. Registry (beyond Step 6's own entries): `wholesale_state_replacement_for_event_exposing_facade`'s example clause ("GeceOturumDurumu's original shape" as an events-free example — inaccurate even at registration, `IGeceOturumDurumuState` has exposed events since ADR-0001's worked example) revised; `caching_session_state_interface_reference_across_session_boundary`'s `why` narrowed to replacement-reset facades with the never-replaced-instance carve-out recorded (else `/dev-story`'s registry check flags this ADR's own documented `_machine` cache); `constructor_subscribing_foundation_service_reset_before_event_source`'s `why` revised to the post-conversion hazard geometry (the live residual is a future replacement-reset facade constructor-subscribing to an in-place source — per-reset re-subscription accumulation).
9. Registry consumer annotations: in-place markers on the three converted facades in `session_scoped_state_static_facade`'s consumers list; `anlati_durum_ipucu_takibi_state`'s `referenced_by` gains this ADR (its reset shape is changed here).

**Registry note**: at Step 6 — no new state_ownership (`HasTriggeredThisNight` has no external consumer; the registry's own rules exempt internal-only state); 1 new api_decision (`end_condition_execution_context` — the hybrid shape's second application + the night-begin orchestration point + the boot contract); `referenced_by` updates to `gece_oturum_durumu_session_state` (in-place conversion + event consumers), `adaptif_ses_execution_context` (shape reuse + twin-hazard flag), `scene_transition_execution_context` (first boot-time `RequestSoftTransition` call site), and ADR-0013/0014's entries (owed-caller supplied).

## Validation Criteria
- A `[Test]` matrix drives `EndConditionStateMachine` with stub delegates through every GDD AC: plain task completion (`Abrupt=false`); saturation via each of the three evaluation events; early-saturated player (settles early → no trigger; final round starts → still no trigger while `HasCarried` false; first pickup → triggers `Abrupt=true`); in-flight deferral (task completes with `Fired>Settled` → pending; equalizing settle → `Abrupt=false`); the tie (one settle completes both → `Abrupt=true`, exactly one trigger); last-trigger-still-in-flight task completion; once-per-night guard (second signal no-op); fired-never-evaluates-saturation.
- A `[Test]` asserts preload requests: task-side on final-round start, trigger-side on the fire reaching `Total−1` with final round active, both able to fire (duplicate-safe), neither after `Triggering`.
- A `[Test]` asserts `HasTriggeredThisNight` is set before `OnTriggerRequested` fires (re-entrancy), and `NotifyTriggerFailed` does NOT clear it (no re-arm, documented design).
- A `[UnityTest]` (Reload Scene disabled, two simulated sessions) asserts the controller's `OnEnable()`-time subscriptions deliver events **exactly once per session** across a `FoundationBootstrap.ResetAll()` boundary — verifying all three halves of the lifecycle design: no orphaning (in-place sources), no accumulation (no re-wire; symmetric `OnDisable`), and no session-2 dead zone (the `OnEnable`/`OnDisable` symmetry — the TD-ADR-caught defect where `Start()`-time subscriptions torn down by `OnDisable` never returned).
- A `[Test]` asserts `NotifyFinalRoundStarted` transitions `Watching`→`Preloaded` (B4 fix) and that a 1-round `StartNight()` with subscriptions already wired delivers `OnFinalRoundStarted` to the machine (the B1 subscription-ordering regression test — GDD AC17's defensive case).
- A `[UnityTest]` asserts the boot `onComplete` repositions the player to the depot's `InitialSpawnAnchor` (user decision, 2026-08-08), and that `NightBeginPending` is cleared after setup and re-set by `ResetOnLoad()` — with the re-run performing setup calls only, never a second initial load.
- A `[UnityTest]`/EditMode check asserts the boot contract: the build's initial scene list contains only the persistent scenes; the depot loads via the controller, after `StartNight` (observable: `CurrentRoundIndexForRestore != -1` before any depot object's `OnEnable`).
- An integration `[UnityTest]` (deferred until all systems exist, per Engine Compatibility's verification note) walks the full chain once per ending tone.

## Related Decisions
- ADR-0003 (Player State) — both lock scopes consumed; the GDD's tone/agency branching.
- ADR-0006 (Session State) — count queries, events, `EndSession`; receives the in-place-reset conversion this ADR mandates.
- ADR-0008 (Scene Transition) — `PreloadHardCut`/`RequestHardCut`/`HardCutConfig.Abrupt`; first boot-time `RequestSoftTransition` call site.
- ADR-0009 (Audio) — execution-shape precedent; consumer of the `Abrupt` flag this system sets; receives the mandated companion revision (symmetric unsubscription) the regime conversion makes necessary.
- ADR-0013 (Carry Loop) — event contract consumed verbatim; `StartNight`'s owed caller supplied.
- ADR-0014 (Memory Trigger) — `SetTotalConfiguredTriggerCountForNight`'s owed caller supplied; `NightConfigDef` finalizes its named placeholder asset.
- `architecture.md` — "Boot Sequence — can defer" note: this ADR adds the minimal sequencing contract, full Boot ADR remains deferred.
