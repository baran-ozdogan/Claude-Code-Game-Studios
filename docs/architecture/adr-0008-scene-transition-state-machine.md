# ADR-0008: Scene Transition State Machine (SOFT/HARD unified)

> **Unity Specialist Validation**: BLOCKING (1 finding, found and fixed) 2026-08-06 — `SafeInvoke`'s single-method delegate-cast trick (`(Action<string>)(object)callback`) does not compile and would throw `InvalidCastException` even if it did; split into two overloads matching `onComplete`/`onFailed`'s real, unrelated delegate types. Also found and fixed: an unchecked `SetActiveScene` return value (AC-9's guarantees are staked on this call succeeding), a missing code branch for the ADR's own highest-stakes path (a preloaded-and-`Ready` HARD CUT swapping directly, with no `LoadSceneAsync` re-run), a missing Reload-Scene risk (this is a third `Awake()`-only static-field instance, same shape as ADR-0003's `PlayerStateProvider`, which already documented this risk), and an Engine Compatibility row citing an API (`AsyncOperation.completed`) the code sketch doesn't actually use.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-06 — 4 findings, all fixed: (1) `docs/registry/architecture.yaml` still listed `seviye-sahne-gecisi` as a `session_scoped_state_static_facade` consumer and still cited "6 Foundation services" — both stale, directly contradicted by this ADR; corrected in the registry directly. (2) Risks/Validation Criteria claimed a forbidden-pattern registry entry for the lazy-subscription rule already existed — it doesn't yet (pending this ADR's own Step 6); corrected to say so honestly. (3) `_activeType` (and the `SetState()` method itself) were referenced throughout the code sketch but never declared/defined — would not compile; added. (4) The newly-added preloaded-`Ready` fast path skipped publishing the public `Ready` state before `Swapping`, contradicting the GDD's own "Ready→Swapping" wording (AC-9) — added the missing `SetState(Ready, Hard)` call; also fixed a narrative/code mismatch describing this path as "a coroutine resuming" when it's actually a direct synchronous call.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-06

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (Scene Management) |
| **Knowledge Risk** | LOW — `SceneManager.LoadSceneAsync(Additive)`/`SetActiveScene`/`UnloadSceneAsync` and `Coroutine`s are all long-stable, pre-cutoff APIs. **Corrected during unity-specialist validation (2026-08-06)**: an earlier draft of this row also listed `AsyncOperation.completed`, which the actual Decision code never uses — the sketch polls `while (!op.isDone) yield return null` inside a `Coroutine` instead, chosen for loop simplicity within the coroutine shape already established by `Işık/Volume`'s ticker (ADR-0005); both are valid pre-cutoff mechanisms, the table just shouldn't cite an API the code doesn't exercise. `seviye-sahne-gecisi.md`'s own Open Questions flags a possible Unity 6 RenderGraph risk for multi-scene camera stacking/lighting — resolved below (not applicable, no custom render pass is used here, same resolution ADR-0005 already reached for Işık/Volume). |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/modules/rendering.md`, `docs/engine-reference/unity/breaking-changes.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md`, `docs/architecture/adr-0002-ui-framework-ui-toolkit.md`, `docs/architecture/adr-0003-player-state-and-movement-lock.md`, `docs/architecture/adr-0005-isik-volume-rendering-architecture.md` |
| **Post-Cutoff APIs Used** | None. Unity 6's `Awaitable` API was considered (see Alternatives Considered) but rejected in favor of a plain `Coroutine`, which is not post-cutoff. |
| **Verification Required** | None new beyond what ADR-0002/ADR-0003 already flagged for the persistent-additive-scene pattern this ADR extends to a third scene. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0002 (UI Framework) and ADR-0003 (Player State) — this ADR extends their "persistent scene, loaded once at boot, not `DontDestroyOnLoad`" pattern to a third scene ("Foundation"). Not a dependency on ADR-0001's static-service pattern — see Decision for why this system is the one documented exception to it. |
| **Enables** | Future "Elevator State Machine" ADR (Required ADR #11) — will consume `RequestSoftTransition`. Future "End-Condition Orchestration" ADR (Required ADR #15) — will consume `RequestHardCut`/`PreloadHardCut`. Future "Audio Architecture" ADR (Required ADR #9) — will consume `OnTransitionStateChanged`, and must respect this ADR's lazy-subscription constraint (see Decision). |
| **Blocks** | Any story implementing `Seviye/Sahne Geçişi` itself; any Asansör/Kat-Erişim Sistemi story (needs `RequestSoftTransition`); any Sahne Kesmeli Anlatı story (needs `RequestHardCut`/`PreloadHardCut`). |
| **Ordering Note** | This ADR also edits ADR-0001, removing `Seviye/Sahne Geçişi` from `FoundationBootstrap.ResetAll()` and correcting its "six current consumers" framing to five throughout — see that file's own inline correction note. |

## Context

### Problem Statement

`seviye-sahne-gecisi.md` (Approved, extensively design-reviewed across 3+ rounds) fully specifies a single unified state machine driving both SOFT transitions (elevator floor changes, additive co-residency, zero player-visible interruption) and HARD CUT transitions (memory-trigger saturation, zero-frame scene swap, no fade) through one shared mechanism — `Idle → Preloading → Ready → Swapping → Complete → Idle`, plus a `Failed` terminal state with an auto-return to `Idle`, a separate internal `_hardCutPreloadState` tracked independently of the public `CurrentState`, and a single-slot HARD-CUT queue for the one case where a HARD CUT is requested while a SOFT transition is already in flight. None of this has a concrete Unity implementation mechanism yet — `architecture.md`'s own Module Ownership sketch (line 86) names the public surface (`RequestSoftTransition`/`RequestHardCut`/`PreloadHardCut`/events) but not how the state machine is actually hosted, ticked, or how its one genuine timed requirement (a 0.5-2s deferred scene unload after `Complete`) is implemented.

This is also the first Foundation-layer system in this project that needs both `SceneManager`'s async load-completion callbacks (already naturally event-driven, no polling needed) **and** a genuine timed delay unrelated to any external event — a combination none of the five prior Foundation ADRs (all plain static services, ADR-0001's pattern) needed to solve. This ADR is the concrete implementation contract: the execution-context decision (Decision below), the zero-frame `SetActiveScene` swap mechanism with its `SWAP_FRAME_EPSILON` validation, the "swap vs. unload" split with its deferred-unload timing, the single-slot HARD-CUT queue, `Failed`'s auto-return to `Idle`, `onComplete`/`onFailed` exception safety, the `SoftTransitionAnchor` coordinate-alignment contract, and the `SceneEnvironmentSettings`-driven `RenderSettings` sync.

### Constraints

- Must not deviate from `seviye-sahne-gecisi.md`'s already-Approved, multiply-revised Core Rules, States and Transitions, and Edge Cases — this ADR formalizes, it does not redesign. In particular: the SOFT-duration Tuning Knob (2-8s) is explicitly **not** a completion gate (only real `LoadSceneAsync` completion is), the `Swapping` step is **only** `SetActiveScene` (zero-frame), and `UnloadSceneAsync` is a **separate**, deliberately-delayed background step.
- `SWAP_FRAME_EPSILON = 1 frame (≤16.6ms at 60fps)` for the `RequestHardCut`-to-active-scene-change measurement; the count of fully-black rendered frames must be **exactly 0** in every case — this second guarantee is binary, not epsilon-bounded (GDD Core Rules, AC-9).
- `PreloadHardCut`'s `Ready` transition must wait for genuine 100% `LoadSceneAsync` completion (`allowSceneActivation=true`), never the `allowSceneActivation=false`/~90%-then-hold pattern — this pre-pays every target scene `Awake`/`Start` cost before the zero-frame swap.
- Movement-lock ownership stays with the caller (Asansör, Sahne Kesmeli Anlatı) — this system never touches `RequestMovementLock`/`ReleaseMovementLock` itself.
- RenderSettings/lightmap: baked lightmap data stays per-scene (never merged); skybox/ambient sync via a script-driven read of each scene's own `SceneEnvironmentSettings` component at `SetActiveScene` time — the GDD explicitly abandoned an earlier "shared Environment scene" idea.

### Requirements

- `RequestSoftTransition`/`RequestHardCut`/`PreloadHardCut`/`GetCurrentHardCutAbrupt()`/`CurrentState`/`OnTransitionStateChanged(newState, type)`/`OnSoftTransitionRejected(reason)` must match `seviye-sahne-gecisi.md`'s "Dışa açılan arayüz" section verbatim, including `HardCutConfig.Abrupt` and the explicit, mutually-exclusive `onComplete`/`onFailed` callback contract.
- The two validation tiers from AC-9 (frame-delay epsilon, zero-black-frame binary guarantee) must both be independently testable.
- `PreloadHardCut`'s internal progress must be tracked separately from `CurrentState` (GDD Edge Cases) — a HARD CUT preload must be able to advance in the background while `CurrentState` reflects an unrelated, already-in-flight SOFT transition.

## Decision

### Execution context: `MonoBehaviour` in a third persistent scene, not a plain static service (ADR-0001 exception)

**Surfaced as a design question and confirmed by the user (`AskUserQuestion`, 2026-08-06)**: this is the first Foundation system needing a genuine timed delay (the 0.5-2s deferred unload) in addition to `SceneManager`'s async, event-driven load-completion callbacks. Three options were weighed:

1. Plain static C# service (ADR-0001's pattern, used by all five other Foundation services) using Unity 6's `Awaitable.WaitForSecondsAsync` for the one delay — keeps full consistency with the established pattern, but `Awaitable` is undocumented anywhere in this project's `docs/engine-reference/unity/` (a real gap, would need its own verification pass before relying on it).
2. Same plain static service, using `System.Threading.Tasks.Task.Delay` instead — standard .NET, zero engine-version risk, but not Unity-native (doesn't integrate with `Time.timeScale` or Unity's pause state the way a `Coroutine`/`Awaitable` would).
3. `MonoBehaviour`-hosted, living in a new persistent "Foundation" scene (extending ADR-0002's "UI" scene / ADR-0003's "Player" scene pattern to a third scene) — uses an ordinary `Coroutine` for the delay, the most familiar, lowest-risk Unity mechanism, at the cost of being a deliberate, documented exception to ADR-0001's "no `MonoBehaviour`" pattern.

**User chose option 3.** `SceneTransitionManager` is a `MonoBehaviour` on a single `GameObject` in a new persistent "Foundation" scene, loaded additively once at boot (same mechanism/timing family as the existing "UI" and "Player" scenes), never unloaded. This is now the third application of this project's "persistent scene, not `DontDestroyOnLoad`" pattern (state: ADR-0001/ADR-0006/ADR-0007's static facades — a different mechanism family; UI: ADR-0002; player: ADR-0003; scene transitions: here).

**Consequence discovered while drafting this ADR, corrected forward into ADR-0001** (see that file's own inline correction note): ADR-0001's `FoundationBootstrap.ResetAll()` previously listed `SeviyeSahneGecisi.ResetOnLoad()` as one of six uniform Foundation services, assuming it would follow the plain-static-service shape. Since it's now `MonoBehaviour`-hosted instead, it has no `ResetOnLoad()` — its lifecycle resets via Domain Reload / process restart, driven by its own persistent scene's `Awake()`, exactly like the UI and Player scenes (neither of which is in `FoundationBootstrap.ResetAll()` either). ADR-0001 is corrected to five current static-service consumers, with this system as the one documented exception.

**Second consequence, load-bearing for a future ADR**: `architecture.md`'s Module Ownership table already states `Adaptif Ses Sistemi` (Required ADR #9, not yet written) will subscribe to `OnTransitionStateChanged` to fire its HARD CUT sting. If `Adaptif Ses Sistemi` follows ADR-0001's pattern (a plain static service, subscribing to upstream Foundation events **inside its own constructor**, as `Gece/Oturum Durumu`/`Anlatı Durum/İpucu Takibi` already do for `Işık/Volume`'s event), that constructor runs at `FoundationBootstrap.ResetAll()`'s `SubsystemRegistration` time — **before** this system's persistent "Foundation" scene has loaded, meaning `SceneTransitionManager.Instance` would not yet exist. **This ADR mandates**: any Foundation service subscribing to `OnTransitionStateChanged` must do so lazily, on first real use (the same pattern ADR-0007 established for its own Addressables load — `EnsureXLoaded()`-style, not constructor-time), never assuming `SceneTransitionManager.Instance` is available at `ResetAll()` time. ADR-0009 (Audio Architecture) must design `Adaptif Ses Sistemi`'s subscription around this constraint from the start.

### Data model

> **CORRECTED IN PLACE (2026-08-10, user decision — before any story was written)**: the sketch below
> originally declared the state machine's fields directly on the `MonoBehaviour`. It is now split into a
> **pure C# `SceneTransitionState`** (the six-state machine, `_activeType`, `_hardCutPreloadState`, the
> single-slot pending queue, and every accept/reject/queue *decision*) plus a **thin
> `SceneTransitionManager : MonoBehaviour` driver** (coroutines, `SceneManager` calls, `Awake`, timing).
>
> Nothing in this ADR's actual Decision changes: the MonoBehaviour hosting in the persistent Foundation
> scene, chosen specifically so an ordinary `Coroutine` can implement the 0.5–2s deferred unload, is
> untouched — that was the argued decision, and the state's physical location was never part of it.
>
> Why: `docs/architecture/control-manifest.md`'s "pure C# state machine + thin MonoBehaviour driver split
> (BLOCKING unit-test rule)" postdates this ADR. Read by section that rule sits under *Core Layer Rules*
> and does not formally govern scene transitions (a Foundation-layer concern), so this is a **choice, not
> a compelled correction** — but the shipped precedents point the same way (`ShiftZone` holds a pure
> `ShiftProgressMachine`; ADR-0011 splits `ElevatorController`/`ElevatorStateMachine`), and the split makes
> most of this ADR's own Validation Criteria plain EditMode `[Test]`s instead of `AddComponent` PlayMode
> tests. Every guarantee that is genuinely engine-coupled (`SWAP_FRAME_EPSILON`, zero black frames, the
> Reload-Scene staleness check) stays a `[UnityTest]` and is unaffected.
>
> **Do NOT read this as reintroducing ADR-0001's static-service pattern.** `SceneTransitionState` is a
> plain field on the driver (the `ShiftZone` shape), *not* a static facade with `ResetOnLoad()` registered
> in `FoundationBootstrap.ResetAll()` (the `ElevatorSystem` shape). This system remains the one documented
> exception to ADR-0001 — that framing is correct and orthogonal to this split.

```csharp
public enum TransitionState { Idle, Preloading, Ready, Swapping, Complete, Failed }
public enum TransitionType { Soft, Hard }

public interface ISceneTransitionManager {
    TransitionState CurrentState { get; }

    void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config,
                                Action onComplete, Action<string> onFailed);
    void RequestHardCut(string toScene, HardCutConfig config,
                         Action onComplete, Action<string> onFailed);
    void PreloadHardCut(string toScene);  // no-op if a preload is already in flight (GDD Edge Cases);
                                           // ALSO a no-op if a preload for the SAME scene already sits at
                                           // Ready (clarified by ADR-0015, 2026-08-08 — its dual preload
                                           // thresholds can re-call after Ready; the in-flight case was
                                           // documented, the post-Ready case was unspecified)
    bool GetCurrentHardCutAbrupt();       // undefined result if no HARD CUT preloaded/active — GDD's own contract

    event Action<TransitionState, TransitionType> OnTransitionStateChanged;
    event Action<string> OnSoftTransitionRejected;
}

// PURE C# CORE (2026-08-10 split) — no UnityEngine API, plain-constructible
// by an EditMode [Test]. Owns the state machine and every DECISION; the
// driver below owns every ACTION. Methods return an instruction (the
// ElevatorStateMachine.TryCall() shape) rather than performing engine work.
internal sealed class SceneTransitionState {
    public TransitionState CurrentState { get; private set; } = TransitionState.Idle;
    private TransitionType _activeType;

    // Tracked separately from CurrentState per GDD Edge Cases — a HARD CUT
    // preload can advance in the background while CurrentState still
    // reflects an unrelated, already-in-flight SOFT transition.
    private TransitionState _hardCutPreloadState = TransitionState.Idle;
    private string _hardCutPreloadScene;
    private HardCutConfig _hardCutPreloadConfig;

    // Single-slot "pending" queue for a HARD CUT requested while a SOFT
    // transition already owns CurrentState (GDD "Cross-type bekleyen slot").
    private (string toScene, HardCutConfig config, Action onComplete, Action<string> onFailed)? _pendingHardCut;

    public event Action<TransitionState, TransitionType> OnTransitionStateChanged;
    public event Action<string> OnSoftTransitionRejected;

    internal void SetState(TransitionState newState, TransitionType type) {
        CurrentState = newState;
        _activeType = type;
        OnTransitionStateChanged?.Invoke(newState, type);
    }

    // Arbitration lives here and returns what the driver should DO:
    //   TryBeginSoft  -> Rejected | StartCoroutine
    //   TryBeginHard  -> Rejected | Queued | SwapDirectly (preload already Ready
    //                    for this exact scene) | StartCoroutine (sync-wait fallback)
    // ... plus TryFirePendingHardCut(), the PreloadHardCut no-op rules, the
    // Failed -> Idle auto-return, and SafeInvoke's try/catch — all pure.
}

public sealed class SceneTransitionManager : MonoBehaviour, ISceneTransitionManager {
    // Static facade — matches every other Foundation service's calling
    // convention (X.Instance.Method()) even though the implementation
    // is MonoBehaviour-backed, not a plain static class (see Decision).
    public static ISceneTransitionManager Instance => _instance;
    private static SceneTransitionManager _instance;

    // Plain field, NOT a static facade — see the correction note above.
    private readonly SceneTransitionState _state = new SceneTransitionState();

    private void Awake() {
        if (_instance != null) {
            // Duplicate-instance guard — same shape as ADR-0003's
            // PlayerStateProvider, unconditional Debug.LogError + Destroy,
            // not a compiled-out Debug.Assert.
            Debug.LogError("Duplicate SceneTransitionManager — destroying this instance.", this);
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public TransitionState CurrentState { get; private set; } = TransitionState.Idle;

    // Tracked separately from CurrentState per GDD Edge Cases — a HARD CUT
    // preload can advance in the background while CurrentState still
    // reflects an unrelated, already-in-flight SOFT transition.
    private TransitionState _hardCutPreloadState = TransitionState.Idle;
    private string _hardCutPreloadScene;
    private HardCutConfig _hardCutPreloadConfig;

    // Single-slot "pending" queue for a HARD CUT requested while a SOFT
    // transition already owns CurrentState (GDD "Cross-type bekleyen slot").
    private (string toScene, HardCutConfig config, Action onComplete, Action<string> onFailed)? _pendingHardCut;

    // Corrected during TD-ADR review (2026-08-06): an earlier draft
    // referenced _activeType in RequestSoftTransition/RequestHardCut
    // (to pick a rejection reason / decide whether to queue) without
    // ever declaring or assigning it — would not compile. Declared here,
    // assigned inside SetState() below (also previously referenced
    // throughout this sketch but never itself defined).
    private TransitionType _activeType;

    public event Action<TransitionState, TransitionType> OnTransitionStateChanged;
    public event Action<string> OnSoftTransitionRejected;

    private void SetState(TransitionState newState, TransitionType type) {
        CurrentState = newState;
        _activeType = type;
        OnTransitionStateChanged?.Invoke(newState, type);
    }

    public void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config,
                                       Action onComplete, Action<string> onFailed) {
        if (CurrentState != TransitionState.Idle) {
            // Reject: either another SOFT is active, or a HARD CUT owns
            // CurrentState (GDD's deliberate asymmetry — SOFT never queues).
            string reason = _activeType == TransitionType.Soft
                ? "AlreadyTransitioningSoft" : "HardCutActive";
            OnSoftTransitionRejected?.Invoke(reason);
            return;
        }
        StartCoroutine(RunTransition(fromScene, toScene, config, TransitionType.Soft, onComplete, onFailed));
    }

    public void RequestHardCut(string toScene, HardCutConfig config,
                                Action onComplete, Action<string> onFailed) {
        if (CurrentState == TransitionState.Idle) {
            // Corrected during unity-specialist validation (2026-08-06):
            // the original sketch only showed the "no preload" fallback
            // branch below and never wrote down AC-9's own load-bearing
            // path — a PreloadHardCut already sitting at Ready for this
            // exact toScene. That case swaps directly, synchronously,
            // with NO LoadSceneAsync re-run (the scene is already loaded);
            // this is what makes the zero-frame/zero-black-frame
            // guarantee achievable in the first place.
            if (_hardCutPreloadState == TransitionState.Ready && _hardCutPreloadScene == toScene) {
                _hardCutPreloadState = TransitionState.Idle;
                // Corrected during TD-ADR review (2026-08-06): an earlier
                // draft of this fast path jumped straight to DoSwap()
                // without first publishing CurrentState == Ready — the
                // GDD's own Edge Cases text and AC-9 both describe this
                // path as "Ready→Swapping" on the PUBLIC CurrentState,
                // not a silent skip. SetState(Ready, Hard) below makes
                // that transition observable via OnTransitionStateChanged
                // before Swapping fires, matching RunTransition's fallback
                // path (which does publish Ready) and the GDD's own wording.
                SetState(TransitionState.Ready, TransitionType.Hard);
                if (!DoSwap(null, toScene, TransitionType.Hard)) {
                    SetState(TransitionState.Failed, TransitionType.Hard);
                    SafeInvoke(onFailed, "ActivateSceneFailed");
                    SetState(TransitionState.Idle, TransitionType.Hard);
                    return;
                }
                SetState(TransitionState.Complete, TransitionType.Hard);
                SafeInvoke(onComplete);
                SetState(TransitionState.Idle, TransitionType.Hard);
                TryFirePendingHardCut();
                return;
            }
            // No matching preload — synchronous-wait fallback (GDD AC-2):
            // runs the full Preloading→Ready→Swapping sequence inline.
            StartCoroutine(RunTransition(null, toScene, config, TransitionType.Hard, onComplete, onFailed));
            return;
        }
        if (_activeType == TransitionType.Soft && _pendingHardCut == null) {
            // Cross-type queueing — the one deliberate exception to
            // "reject, no queue" (GDD: a narrative moment must not be lost).
            _pendingHardCut = (toScene, config, onComplete, onFailed);
            return;
        }
        // Second HARD CUT while one is active/already queued, or any other
        // same-type collision — reject (GDD AC-4/AC-6).
    }

    // ... PreloadHardCut, GetCurrentHardCutAbrupt, RunTransition (the
    // Preloading→Ready→Swapping→Complete coroutine, wrapping onComplete/
    // onFailed in try/catch per GDD Edge Cases), the delayed-unload
    // coroutine, and the pending-HARD-CUT auto-fire on reaching Idle are
    // straightforward translations of States and Transitions / Edge Cases
    // and are not fully reproduced here.
}
```

### Zero-frame swap and the deferred unload

```csharp
private IEnumerator RunTransition(string fromScene, string toScene, /* config */ TransitionType type,
                                   Action onComplete, Action<string> onFailed) {
    SetState(TransitionState.Preloading, type);
    var op = SceneManager.LoadSceneAsync(toScene, LoadSceneMode.Additive);
    // allowSceneActivation stays true (default) — GDD Core Rules explicitly
    // rejects the ~90%-then-hold pattern; wait for genuine 100%.
    while (!op.isDone) yield return null;
    if (/* load failed */) { SetState(TransitionState.Failed, type); SafeInvoke(onFailed, "..."); SetState(TransitionState.Idle, type); yield break; }

    SetState(TransitionState.Ready, type);
    // For HARD CUT with no prior PreloadHardCut, Ready is reached here,
    // synchronously in the same coroutine — the GDD's "senkron-bekleme
    // fallback" (AC-2). For a pre-called PreloadHardCut, RunTransition
    // is never entered for the swap itself — RequestHardCut's own
    // already-Ready branch (see RequestHardCut sketch above) performs
    // the swap directly, inline, without re-running Preloading/Ready.

    if (!DoSwap(fromScene, toScene, type)) {
        SetState(TransitionState.Failed, type);
        SafeInvoke(onFailed, "ActivateSceneFailed");
        SetState(TransitionState.Idle, type);
        yield break;
    }

    SetState(TransitionState.Complete, type);
    SafeInvoke(onComplete);
    SetState(TransitionState.Idle, type);
    TryFirePendingHardCut();

    if (fromScene != null) StartCoroutine(DelayedUnload(fromScene));  // 0.5-2s, background, doesn't block Idle
}

// Swapping: ONLY SetActiveScene — zero-frame, no unload here (GDD Core
// Rules, "Swap ile unload ayrımı"). Also the path an already-Ready
// PreloadHardCut's RequestHardCut call enters directly (see
// RequestHardCut sketch above) — same swap logic, no LoadSceneAsync
// re-run, since the scene is already loaded and 100% ready.
private bool DoSwap(string fromScene, string toScene, TransitionType type) {
    // fromScene may be null for a boot-time initial load (ADR-0015's
    // RequestSoftTransition(null, ...) call — clarified 2026-08-08):
    // CopySoftTransitionAnchorTransform must guard fromScene == null and
    // skip the copy (there is no source anchor at boot; initial player
    // placement is ADR-0015's InitialSpawnAnchor, applied in onComplete).
    if (type == TransitionType.Soft) CopySoftTransitionAnchorTransform(fromScene, toScene);
    SyncRenderSettingsFromSceneEnvironmentSettings(toScene);
    bool activated = SceneManager.SetActiveScene(SceneManager.GetSceneByName(toScene));
    // SetActiveScene returns false (no exception) if the target Scene isn't
    // loaded/valid — checked explicitly rather than assumed, since AC-9's
    // zero-black-frame guarantee is staked entirely on this call succeeding.
    if (!activated) return false;
    SetState(TransitionState.Swapping, type);
    return true;
}

private IEnumerator DelayedUnload(string fromScene) {
    yield return new WaitForSeconds(unloadDelaySeconds);  // Tuning Knob, 0.5-2s
    SceneManager.UnloadSceneAsync(fromScene);
    // Fire-and-forget: the GDD's own reasoning is that UnloadSceneAsync's
    // synchronous per-object OnDestroy cost is invisible once the active
    // scene has already changed — nothing waits on this operation.
}

// Corrected during unity-specialist validation (2026-08-06): the original
// sketch used a single SafeInvoke(Action, string) with a cast trick
// ((Action<string>)(object)callback) to handle both onComplete (Action)
// and onFailed (Action<string>) through one method — this does not
// compile (CS1503 at the call site) and the cast itself would throw
// InvalidCastException even if it did, since Action and Action<string>
// are unrelated delegate types with no valid conversion between them.
// Split into two overloads matching the two real call shapes instead.
private void SafeInvoke(Action callback) {
    try { callback?.Invoke(); }
    catch (Exception e) { Debug.LogException(e); }
    // GDD Edge Cases: an exception in onComplete must never leak out of
    // SceneTransitionManager or block Complete → Idle.
}

private void SafeInvoke(Action<string> callback, string failureReason) {
    try { callback?.Invoke(failureReason); }
    catch (Exception e) { Debug.LogException(e); }
    // Same guarantee as above, for onFailed.
}
```

`SWAP_FRAME_EPSILON` (1 frame, ≤16.6ms at 60fps) is a **measurement tolerance for the `RequestHardCut`-call-to-active-scene-change timestamp delta**, not a code constant this method needs to reference — `SetActiveScene` is already synchronous, so the actual swap happens within the same frame `RequestHardCut` is called, whether via the already-`Ready` fast path in `RequestHardCut` itself (**corrected during TD-ADR review, 2026-08-06**: earlier prose here said "the already-Ready coroutine resumes," implying the fast path resumes a suspended `Coroutine` — it doesn't; it's a direct synchronous method call with no coroutine involved at all, see `RequestHardCut`'s own sketch above) or via `RunTransition`'s synchronous-fallback branch; the epsilon exists so AC-9's test can express "within one frame" without claiming a physically-impossible exact-zero timestamp delta. The **separate**, epsilon-free guarantee (exactly 0 fully-black rendered frames) is validated by asserting no frame between the pre-swap and post-swap frame renders a scene with zero active-scene content — a frame-capture test concern, not something this method's control flow needs to special-case (`SetActiveScene` never itself renders a black frame; the guarantee follows directly from `Ready` only being reached after 100% load completion, per Constraints).

### RenderGraph / multi-scene rendering (resolves GDD Open Question)

`seviye-sahne-gecisi.md`'s own Open Questions flags a possible Unity 6 RenderGraph risk for multi-scene camera stacking/lighting, recommending "a small technical spike" before implementation. **Resolved here, same reasoning ADR-0005 already applied to Işık/Volume**: this system's RenderSettings sync is a plain script-driven read of each scene's `SceneEnvironmentSettings` component (skybox material + ambient), applied via ordinary `RenderSettings.*` property writes at `SetActiveScene` time — no custom `ScriptableRendererFeature`/`RecordRenderGraph` pass is used anywhere in this mechanism. RenderGraph's Unity-6 API changes (`docs/engine-reference/unity/modules/rendering.md`) only affect **custom render passes**, which this ADR does not introduce. The GDD's flagged risk does not apply to the mechanism as designed; no technical spike is needed for this specific concern (camera behavior across additive multi-scene loading itself is standard, pre-cutoff Unity behavior, unaffected by RenderGraph).

## Alternatives Considered

### Alternative 1: Plain static service + `Awaitable.WaitForSecondsAsync` (ADR-0001 pattern, undocumented API)
- **Description**: Keep `SeviyeSahneGecisi` as a plain C# static-facade service, consistent with all five other Foundation services; use Unity 6's `Awaitable` type for the one delayed-unload need.
- **Pros**: Full consistency with ADR-0001 — no exception to document, no `FoundationBootstrap.ResetAll()` edit needed, no boot-order constraint for future subscribers (`Adaptif Ses Sistemi`'s future ADR wouldn't need the lazy-subscription rule this ADR now mandates).
- **Cons**: `Awaitable` is not documented anywhere in this project's `docs/engine-reference/unity/` — a real, unverified gap, not just an unfamiliar API; introducing the project's first use of it specifically for this one small delay is a nontrivial verification cost for a narrow benefit.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-06): preferred the best-precedented, lowest-verification-risk mechanism (`Coroutine`, already proven in this project via `Işık/Volume`'s per-zone ticker, ADR-0005) over introducing an unverified API, even at the cost of a documented ADR-0001 exception.

### Alternative 2: Plain static service + `Task.Delay`
- **Description**: Same as Alternative 1, but using standard .NET `System.Threading.Tasks.Task.Delay` instead of `Awaitable`.
- **Pros**: Zero engine-version risk — `Task.Delay` is framework-standard, not Unity-specific, so no engine-reference verification gap.
- **Cons**: Not Unity-native — doesn't respect `Time.timeScale` or Unity's own pause/Editor-Play-mode-stop semantics the way a `Coroutine`/`Awaitable` would (a `Task.Delay` in flight when Play mode stops in the Editor keeps running on a thread-pool timer, disconnected from Unity's own lifecycle) — a correctness risk for a delay whose only job is to happen "during this Play session."
- **Rejection Reason**: Same as Alternative 1 — user preferred the more Unity-native, better-precedented `Coroutine` mechanism; `Task.Delay`'s Editor-Play-mode disconnect is a real, if narrow, additional risk this option doesn't avoid the way Alternative 1's engine-reference gap is at least explicit and checkable.

### Alternative 3: `MonoBehaviour` in a persistent scene, `Coroutine` for the delay (chosen)
- **Description**: See Decision.
- **Pros**: Best-precedented mechanism in this project (`Coroutine`s already used for `Işık/Volume`'s per-zone ticker, ADR-0005); extends an already-established pattern (ADR-0002/ADR-0003's persistent scenes) rather than introducing a new one; automatically respects Unity's own pause/Play-mode-stop semantics.
- **Cons**: The one documented exception to ADR-0001's "no `MonoBehaviour`" pattern among Foundation services; imposes a real, non-optional constraint on any future service subscribing to `OnTransitionStateChanged` (must subscribe lazily, not at `FoundationBootstrap.ResetAll()` time) — a constraint that simply wouldn't exist under Alternative 1 or 2.
- **Rejection Reason**: N/A — chosen. User confirmed (`AskUserQuestion`, 2026-08-06) accepting the future-subscriber constraint (to be handled by ADR-0009) in exchange for using this project's most proven, lowest-risk async mechanism for a system whose zero-frame-swap and zero-black-frame guarantees are already its most safety-critical requirements — minimizing mechanism risk here was judged more valuable than pattern purity.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #8, one of the 2 remaining "must have before coding" Foundation ADRs.
- Resolves `seviye-sahne-gecisi.md`'s own open RenderGraph question directly (not applicable — no custom render pass), the same resolution ADR-0005 already reached for a structurally similar question.
- Reuses this project's most-proven async mechanism (`Coroutine`, per ADR-0005's precedent) for the system carrying its two most safety-critical, binary-tested guarantees (`SWAP_FRAME_EPSILON`, zero black frames) — avoids introducing an unverified API (`Awaitable`) or an Editor-Play-mode-disconnected one (`Task.Delay`) for this specific, high-stakes system.
- Extends the "persistent scene, not `DontDestroyOnLoad`" pattern (ADR-0002/0003) to a third, natural application rather than inventing a fourth persistence mechanism.

### Negative
- Breaks ADR-0001's "one pattern, uniform across all Foundation services" claim — now five plain-static-service consumers plus this one documented `MonoBehaviour` exception. Required editing ADR-0001 in ~10 places to correct the resulting "six"→"five" count.
- Imposes a real, binding constraint on a not-yet-written ADR (`Audio Architecture`, #9): `Adaptif Ses Sistemi`'s subscription to `OnTransitionStateChanged` must be lazy, not constructor-time — a constraint that adds a small amount of design complexity to that future ADR specifically because of this one's mechanism choice.
- ~~`SceneTransitionManager`'s own state (the six-state machine, the pending-HARD-CUT slot, `_hardCutPreloadState`) is `MonoBehaviour`-instance state, not a plain C# object a `[Test]` can construct in isolation the way ADR-0001's pattern allows — testing this system requires `new GameObject().AddComponent<SceneTransitionManager>()` (the same MonoBehaviour-testability shape ADR-0003 already established for `PlayerStateProvider`), not a bare `new SceneTransitionManagerState()`.~~ **No longer true — corrected 2026-08-10 (see the Data model note).** The state and all arbitration live in a plain-constructible `SceneTransitionState`; only genuinely engine-coupled behavior (coroutines, `SceneManager` calls, `Awake`'s duplicate guard, frame timing) needs `AddComponent`. This bullet was written before the manifest's split rule existed and was comparing itself to ADR-0003's `PlayerStateProvider`, whose own TD-ADR review had already corrected that framing — `PlayerStateProvider` is not plain-constructible either, but for `CharacterController`/`Camera` coupling reasons, not as a considered rejection of a pure core.

### Risks
- **Risk**: A future Foundation service (most concretely, `Adaptif Ses Sistemi`, ADR-0009) subscribes to `OnTransitionStateChanged` in its own constructor, reproducing the exact "subscribed before the event source exists" class of bug ADR-0006 already found and fixed once for `FoundationBootstrap.ResetAll()`'s internal ordering — except this time the event source (`SceneTransitionManager.Instance`) doesn't exist at `SubsystemRegistration` time *at all*, regardless of ordering, since it's set by a scene's `Awake()`, not by `FoundationBootstrap`. **Mitigation**: this ADR states the lazy-subscription requirement explicitly (Decision, "Second consequence"). **Corrected during TD-ADR review (2026-08-06)**: an earlier draft of this bullet claimed this rule was already recorded in `docs/registry/architecture.yaml`'s forbidden patterns — checked directly, it isn't; only ADR-0006's and ADR-0007's forbidden patterns exist there. This is a pending Step-6 action for this ADR (registry update, after write approval), not something already done — flagged clearly so ADR-0009's own author doesn't wrongly assume the registry check already catches a constructor-time `SceneTransitionManager.Instance` subscription before that entry actually exists.
- **Risk**: The persistent "Foundation" scene's load-order relative to the existing "UI" and "Player" persistent scenes is not specified by this ADR — if a future boot-sequence ADR ever needs `SceneTransitionManager.Instance` available before the UI or Player scenes finish loading (unlikely given none of this project's current GDDs describe such a dependency), an explicit load-order contract would need to be added. **Mitigation**: none needed now — flagged for whichever future ADR designs the actual boot/bootstrap sequence (not yet in the Required ADRs list, since MVP's "Ana Menü/Başlangıç Akışı" is explicitly Vertical-Slice-scope, per `systems-index.md`).
- **Risk**: `WaitForSeconds(unloadDelaySeconds)` inside a `Coroutine` is paused by `Time.timeScale == 0` — if any future system ever pauses the game via `Time.timeScale` during the 0.5-2s unload-delay window, the unload would stall until unpause. **Mitigation**: not a concern for MVP (no pause menu exists in any current GDD), but worth a control-manifest note if a pause system is ever added — `WaitForSecondsRealtime` would be the fix if genuinely required.
- **Risk (unity-specialist validation, 2026-08-06)**: `SceneTransitionManager._instance` is set exclusively in `Awake()`, with no separate reset hook — structurally identical to `PlayerStateProvider.Current` (ADR-0003), which already documented and mitigated the risk that Unity's independent "Reload Scene" Enter Play Mode Setting can suppress `Awake()` re-execution on a surviving persistent-scene object across a Play-mode Stop→Play boundary, leaving `Instance` pointed at a stale (possibly destroyed) object. This ADR is a third instance of the identical shape (state: ADR-0001 pattern's own Reload-Scene note; player: ADR-0003) and carries the same risk identically — omitted from an earlier draft of this section despite the draft explicitly claiming to follow `PlayerStateProvider`'s pattern "faithfully." **Mitigation**: same as ADR-0003 — this is a correctness risk only under the non-default "Reload Scene: Off" Editor setting (real player builds always fully reload); no code change needed here, but the boot sequence must guarantee the Foundation scene is freshly loaded each genuine session, and a `[UnityTest]` with Reload Scene disabled should confirm `SceneTransitionManager.Instance` is never stale across two simulated sessions (Validation Criteria below).
- **Risk (unity-specialist validation, lower-confidence, flagged not fixed)**: unloading an additively-loaded scene's baked lightmap data has, in some past Unity versions, been reported to briefly affect other concurrently-loaded scenes' renderer lightmap-index resolution via `LightmapSettings.lightmaps` array compaction — not confirmed still live in Unity 6.3, and not covered by `docs/engine-reference/unity/modules/rendering.md` at all (a documentation gap, not a contradicted claim). **Mitigation**: this ADR's own deferred-unload design (0.5-2s after the active scene has already changed) already minimizes exposure by construction; worth a cheap empirical check during the first implementation story rather than a design change now.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `seviye-sahne-gecisi.md` | Single shared state machine, both SOFT and HARD CUT through the same mechanism, no separate code paths (AC-2) | One `SceneTransitionManager`/`RunTransition` coroutine parameterized by `TransitionType`, exactly as the GDD's own AC-2 requires |
| `seviye-sahne-gecisi.md` | Zero-frame `SetActiveScene` swap, `SWAP_FRAME_EPSILON` ≤ 1 frame, exactly 0 fully-black rendered frames (Core Rules, AC-9) | `Swapping` step is a single synchronous `SetActiveScene` call, no intervening yield; see Decision → "Zero-frame swap and the deferred unload" |
| `seviye-sahne-gecisi.md` | "Swap" vs. "unload" split — `UnloadSceneAsync` fires 0.5-2s after `Complete`, not part of `Swapping` (Core Rules) | `DelayedUnload` coroutine, started after `Idle` is reached, not blocking the state machine |
| `seviye-sahne-gecisi.md` | `PreloadHardCut`'s progress tracked independently of `CurrentState` (Edge Cases) | Separate `_hardCutPreloadState`/`_hardCutPreloadScene` fields, never written to `CurrentState` |
| `seviye-sahne-gecisi.md` | Single-slot HARD-CUT queue during an active SOFT transition (Edge Cases, AC-5/AC-6) | `_pendingHardCut` nullable tuple field, auto-fired via `TryFirePendingHardCut()` on reaching `Idle` |
| `seviye-sahne-gecisi.md` | `Failed` terminal state auto-returns to `Idle` after `onFailed` fires (AC-11a) | `SetState(TransitionState.Idle, type)` immediately follows `SafeInvoke(onFailed, ...)` in the failure path |
| `seviye-sahne-gecisi.md` | Exception in `onComplete`/`onFailed` must not leak or block `Complete → Idle` (AC-10) | `SafeInvoke` wraps both callback invocations in try/catch |
| `seviye-sahne-gecisi.md` | `RenderSettings`/lightmap: per-scene, script-driven sync via `SceneEnvironmentSettings`, no shared Environment scene | `SyncRenderSettingsFromSceneEnvironmentSettings(toScene)`, called at `Swapping` time |
| `seviye-sahne-gecisi.md` | Unity 6.3 RenderGraph compatibility (Open Questions) | Resolved: not applicable, no custom render pass used (see Decision) |

## Performance Implications
- **CPU**: Negligible — one `MonoBehaviour` with no per-frame `Update()` work outside an active transition; `LoadSceneAsync`/`UnloadSceneAsync` costs are Unity-internal and already budgeted for in `technical-preferences.md`'s general performance targets, not something this ADR adds to.
- **Memory**: Negligible — one persistent `GameObject`/`MonoBehaviour`, a handful of small fields (enum state, nullable tuple, string scene names). Two scenes briefly co-resident during a SOFT transition's preload window is an existing, already-accepted cost (GDD Core Rules), not new here.
- **Load Time**: The additive `LoadSceneAsync` cost itself is scene-content-dependent, outside this ADR's scope (level design/asset budget concern, per `technical-preferences.md`'s draw-call/memory budgets).
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Seviye/Sahne Geçişi` is not yet implemented).

## Validation Criteria
- AC-9's two guarantees are independently tested: frame-timestamp delta between `RequestHardCut` and the target scene becoming active is ≤ `SWAP_FRAME_EPSILON` (1 frame), **and** the count of fully-black rendered frames across the same window is exactly 0 (a frame-capture assertion, not a timestamp one) — two separate test assertions, not one combined "≈0" check, per the GDD's own design-review correction.
- `RequestSoftTransition`/`RequestHardCut` called a second time while `CurrentState` is `Preloading`/`Ready`/`Swapping` is rejected as a no-op, with `OnSoftTransitionRejected` firing exactly once for the SOFT case (AC-3/AC-4/AC-7).
- A `RequestHardCut` issued during an active SOFT transition is queued (not rejected) and fires automatically, with zero additional delay if already `Ready`, the instant `CurrentState` reaches `Idle` (AC-5).
- `PreloadHardCut` called twice in a row (before the first completes) is a no-op — the original preload target/state is unaffected (AC-8).
- A failed target-scene load transitions to `Failed`, fires `onFailed` exactly once, then auto-returns to `Idle` such that an immediately-following `RequestSoftTransition`/`RequestHardCut` is accepted normally (AC-11/AC-11a).
- An exception thrown from `onComplete` is caught, logged, and does not prevent `CurrentState` from reaching `Idle` (AC-10).
- Any future Foundation service's constructor never references `SceneTransitionManager.Instance` — a code-review-time check (this ADR's own Risks mitigation), intended to be backed by a new forbidden-pattern registry entry proposed at this ADR's own Step 6 (registry update, after write approval — not yet registered as of this draft, see the corrected Risks note above).
- A `[UnityTest]` with Unity's "Reload Scene" Enter Play Mode Setting disabled runs two successive simulated sessions and confirms `SceneTransitionManager.Instance` is never a stale/destroyed reference in the second session — same shape as ADR-0003's `PlayerStateProvider` test, added per unity-specialist validation (2026-08-06).

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — edited by this ADR to remove `Seviye/Sahne Geçişi` from `FoundationBootstrap.ResetAll()` and correct its consumer count from six to five.
- ADR-0002 (UI Framework) / ADR-0003 (Player State) — source of the "persistent scene, loaded once at boot, not `DontDestroyOnLoad`" pattern this ADR extends to a third scene; ADR-0003's `SoftTransitionAnchor`-driven player repositioning is the direct consumer of this ADR's SOFT-transition coordinate-alignment contract.
- ADR-0005 (Işık/Volume Rendering Architecture) — precedent for both the `Coroutine` mechanism (per-zone ticker) and the "no custom RenderGraph pass needed" resolution this ADR reapplies to its own RenderGraph Open Question.
- Future "Audio Architecture" ADR (Adaptif Ses Sistemi, Required ADR #9) — must design its `OnTransitionStateChanged` subscription as lazy/late per this ADR's mandated constraint, not constructor-time.
- Future "Elevator State Machine" (#11) and "End-Condition Orchestration" (#15) ADRs — primary consumers of `RequestSoftTransition` and `RequestHardCut`/`PreloadHardCut` respectively.
