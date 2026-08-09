# ADR-0012: Dialogue Callback Selection Timing

> **Unity Specialist Validation**: BLOCKING (1 finding, found and fixed) 2026-08-07 — the original deferred-evaluation mechanism assumed "the first `OnTransitionStateChanged(Complete)` received after subscribing is this scene's own swap," reasoning `SceneTransitionManager` never runs two same-type transitions concurrently. False once `PreloadHardCut` (ADR-0008) is in play — exactly the path `Sahne Kesmeli Anlatı` is expected to use to enter this scene — since a preloaded-but-inactive scene's `Awake`/`OnEnable` (and this system's subscription) can fire long before its real swap, while unrelated SOFT transitions complete freely in between. Fixed by adding a `gameObject.scene == SceneManager.GetActiveScene()` filter to the event handler, which correctly distinguishes "an unrelated transition finished" from "my own swap finished" regardless of preload timing. Also fixed 1 MINOR-but-near-guaranteed-to-manifest bug: `List<T>.Sort()` is not stable, so `Priority`-tie ordering (near-certain on any freshly-authored `CallbackPool`, since `Priority` defaults to 0) would not reliably preserve the GDD's required "writer-assigned order" — switched to `OrderBy()` (documented stable). Also fixed 2 MINOR findings: an unclamped `RemoveRange` call that would throw on a misconfigured negative `MaxCallbacksPerScene`, and an Engine Compatibility table that understated a real, newly-load-bearing Unity ordering reliance (additively-loaded scene objects' `Awake`/`OnEnable` completing before `AsyncOperation.isDone`).
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-07 — 1 major finding, fixed: the GDD's own `MaxCallbacksPerScene`-vs-per-night-clue-count build-time consistency check (Core Rules, with its own dedicated Acceptance Criterion, explicitly pointing at this project's already-established `IPreprocessBuildWithReport` pattern from `ani-tetikleyici-etkilesim.md`) was silently absent from the entire draft — unlike the `UsedCallbackIds` cross-night-persistence gap, which was correctly and explicitly carved out as out-of-scope, this requirement was simply missing with no scope note. Added `ValidateMaxCallbacksPerScene` (Decision), contributing to the same shared editor-validation pass the GDD's own "Paylaşılan araç notu" already anticipates rather than a fourth independent implementation. 2 minor findings, also fixed: `architecture.md`'s Dependency Diagram doesn't yet show the new `Diyalog/Anlatı İçeriği ──> UIRoot` edge this ADR introduces (flagged, not fixed here — a future `architecture.md` touch-up); and the ADR-0001 static-facade pattern's first Core-layer consumer wasn't called out as precedent-setting (added to Risks). Verified clean otherwise: registry consistency (all 3 relevant contracts — `session_scoped_state_static_facade`, `live_monobehaviour_state_static_accessor`, `scene_transition_execution_context` — matched exactly, no forbidden-pattern violations), the `PreloadHardCut` fix traced correctly against ADR-0008's real code, no stale claims left after the unity-specialist's fixes were applied, and no upward-layer-read violations.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-07

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (UI Toolkit + event-driven scene-lifecycle timing) |
| **Knowledge Risk** | LOW — this ADR introduces no new engine API beyond what ADR-0002 (UI Toolkit)/ADR-0008 (`SceneManager`-driven scene transitions) have already validated; it only consumes `SceneTransitionManager.Instance.OnTransitionStateChanged`, a project-owned C# event, not a raw engine API. It does, however, rely on one specific Unity ordering guarantee no prior ADR states explicitly: additively-loaded scene objects' `Awake()`/`OnEnable()` complete before `AsyncOperation.isDone` becomes `true` — correct, stable, pre-cutoff behavior, but newly load-bearing here (unity-specialist validation, 2026-08-07). |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md`, `docs/architecture/adr-0002-ui-framework-ui-toolkit.md`, `docs/architecture/adr-0007-clue-tracking-architecture.md`, `docs/architecture/adr-0008-scene-transition-state-machine.md`, `docs/architecture/adr-0010-interaction-state-machine.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | `gameObject.scene == SceneManager.GetActiveScene()` filtering (Decision, corrected 2026-08-07) should be exercised against a real `PreloadHardCut` scenario in Play mode before this system ships, since the `PreloadHardCut`-concurrent-with-unrelated-SOFT-transition case is the one this ADR's mechanism was rewritten specifically to survive. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (static-facade persistence pattern) — `UsedCallbackIds`. ADR-0002 (UI Toolkit) — `#dialogue-subtitle` element. ADR-0007 (Clue Tracking) — `AnlatiDurumIpucuTakibi.Instance.IsClueKnown(clueId)`. ADR-0008 (Scene Transition) — `SceneTransitionManager.Instance.OnTransitionStateChanged`. ADR-0010 (Interaction State Machine) — `UIRoot.Instance`, reused here per ADR-0002's own steering. |
| **Enables** | Any story implementing psychiatry-session dialogue playback (writer content itself is out of scope — see Context). |
| **Blocks** | Any story implementing `Diyalog/Anlatı İçeriği`'s scene-entry callback-selection logic. |
| **Ordering Note** | None beyond the four Depends-On ADRs already being Accepted-pending. |

## Context

### Problem Statement

`diyalog-anlati-icerigi-2026-08-02.md` (Quick Spec, Approved) specifies that each psychiatry-session scene has a fixed base dialogue plus a `CallbackPool` of pre-written lines, each gated on a `clueId` being `Known` (per `Anlatı Durum/İpucu Takibi`, ADR-0007). The GDD's own Core Rules carry a **load-bearing timing constraint**, added during a third-round full re-verification design-review (2026-08-04, flagged critical): the candidate evaluation (`IsClueKnown` for every `CallbackPool` entry) must **not** run during the scene's own `Awake`/`Start` — those fire during `Seviye/Sahne Geçişi`'s `Preloading` step (ADR-0008), before the scene is genuinely active. If evaluated then, a last-fired memory trigger's clue could still read as not-yet-`Known` even though it will be `Known` by the time the scene is actually visible — reproducing, via a second, independent mechanism, the exact saturation-timing bug `Sahne Kesmeli Anlatı`'s own three-flag gate already exists to prevent (see `sahne-kesmeli-anlati-2026-08-02.md`). The GDD names the project's own established `MemoryTriggerObject.Awake()`-queries-`FiredTriggerIds` idiom as precisely the *wrong* instinct to follow here.

This ADR resolves the concrete engineering question the GDD leaves open: **which signal marks "the scene is genuinely active," where does the subscribing code live, and how is `UsedCallbackIds` (this system's own once-per-night replay guard, separate from `Anlatı Durum`'s `KnownClueIds`) persisted across the scene's own load/unload cycle.**

**Explicitly out of scope** (per `diyalog-anlati-icerigi-2026-08-02.md`'s own scope line and `architecture.md`'s Required ADR #12 description): the actual dialogue text (writer content); the base-dialogue/callback-line playback timing, pacing, or advance-input UX (auto-advance vs. player-advance is undesigned at the GDD level); `UsedCallbackIds`'s cross-night persistence (GDD Open Questions — explicitly owned by the future Çoklu Gece İlerlemesi Vertical Slice GDD, not this MVP architecture pass, since MVP has exactly one night).

### Constraints

- Must not deviate from `diyalog-anlati-icerigi-2026-08-02.md`'s already-Approved Core Rules — this ADR formalizes the deferred-evaluation timing mechanism the GDD explicitly leaves as "kesin mekanizma implementasyon aşamasında seçilir" (exact mechanism chosen at implementation time), it does not redesign the selection rules themselves (candidate filtering, `Priority` cap, `UsedCallbackIds` bookkeeping, zero-candidate fallback).
- Must reuse `SceneTransitionManager.Instance.OnTransitionStateChanged` (ADR-0008) rather than inventing a new scene-activation signal — ADR-0008 already established this as the mechanism `Adaptif Ses Sistemi` (ADR-0009) subscribes to for an analogous "wait for the swap to genuinely happen" need.
- Must reuse `AnlatiDurumIpucuTakibi.Instance.IsClueKnown(clueId)` (ADR-0007) verbatim — no new clue-state mechanism.
- Must reuse the ADR-0001 static-facade persistence pattern for `UsedCallbackIds` — already anticipated as a `session_scoped_state_static_facade` consumer in the registry since ADR-0001 was written, before this ADR existed.
- Must reuse `UIRoot.Instance`/`#dialogue-subtitle` (ADR-0002, ADR-0010) for subtitle rendering — no new UI lookup mechanism, per ADR-0002's explicit "ADRs #9, #10, #12" steering.

### Requirements

- The callback-candidate evaluation must be provably deferred past the scene's own `Awake`/`Start` — the GDD's own AC (first item) requires this to be testable: preloaded-but-not-yet-`Swapping` must show zero evaluation having occurred.
- `UsedCallbackIds` must survive this scene being unloaded and (in the Full Vision, multi-scene case) a different psychiatry scene being loaded later in the same night.
- The selection algorithm (filter by `IsClueKnown` AND NOT `IsCallbackUsed`, cap at `MaxCallbacksPerScene` by `Priority`, mark used) must be a plain, Unity-decoupled function so it satisfies `coding-standards.md`'s BLOCKING unit-test requirement without a live scene/transition.

## Decision

### Deferred-evaluation mechanism: subscribe to `OnTransitionStateChanged`, evaluate on the `Complete` that actually activates this scene

**Confirmed by the user (`AskUserQuestion`, 2026-08-07)**: `DialogueSceneController` (a `MonoBehaviour` living in the psychiatry scene itself — NOT a persistent scene, since it must be re-instantiated fresh on every load of that scene) subscribes to `SceneTransitionManager.Instance.OnTransitionStateChanged` in `OnEnable()` and evaluates callback selection on the first `TransitionState.Complete` event whose swap actually made *this* scene the active one, then immediately unsubscribes (both explicitly, and redundantly via `OnDisable()` when the scene later unloads).

**Corrected during unity-specialist review (2026-08-07) — the original mechanism was unsound for the `PreloadHardCut` path**: an earlier draft reasoned that "the first `Complete` received after subscribing is guaranteed to be this scene's own swap," on the theory that `SceneTransitionManager` never runs two same-type transitions concurrently. That reasoning holds only for the non-preloaded `RequestHardCut` fallback. It breaks for `PreloadHardCut` — the exact path `Sahne Kesmeli Anlatı` (this scene's real caller) is expected to use to achieve ADR-0008's zero-frame/zero-black-frame guarantee. `PreloadHardCut` loads the psychiatry scene fully (`Awake`/`OnEnable` included — i.e. `DialogueSceneController` subscribes) **while it sits inactive in the background**, tracked via `_hardCutPreloadState`, entirely independent of `CurrentState`/`OnTransitionStateChanged`. Ordinary SOFT transitions (the depot↔ballroom elevator) keep firing their own `OnTransitionStateChanged(Complete, Soft)` freely during that window, and the original handler — which filtered only on `newState == Complete`, not on which scene actually swapped — would fire on the very first unrelated elevator `Complete`, evaluating callbacks while the psychiatry scene is still an inactive background scene. That is precisely the "evaluated before the scene is genuinely active" bug this ADR exists to prevent, reproduced via the mechanism meant to prevent it.

**Fix**: filter `Complete` events by whether *this* `GameObject`'s own scene is now the active one, not merely by event ordering:

```csharp
if (_hasEvaluated || newState != TransitionState.Complete) return;
if (gameObject.scene != SceneManager.GetActiveScene()) return;  // not my swap (yet) — e.g. an
                                                                   // unrelated SOFT transition
                                                                   // completing while this scene
                                                                   // sits PreloadHardCut'd in the
                                                                   // background (ADR-0008)
```

This needs no change to ADR-0008 and no scene-identity payload added to the event — `GameObject.scene`/`SceneManager.GetActiveScene()` already carry exactly the identity needed, and `Scene` supports `==`/`!=` by value (handle comparison). While the psychiatry scene sits preloaded-but-inactive, `gameObject.scene` never equals the active scene, so every unrelated `Complete` is correctly ignored; the moment the real `RequestHardCut` (fast-path or fallback) calls `SetActiveScene` on this scene and fires its own `Complete`, the check passes and evaluation runs — still satisfying the GDD's "after `Swapping`, not during `Awake`/`Start`" requirement exactly, since `Swapping`→`Complete` fire synchronously back-to-back with no intervening `yield` (ADR-0008), and `Awake`/`OnEnable`/`Start` for this scene's own objects already ran during `Preloading`, long before either preload path's real swap.

This directly satisfies the GDD's own suggested mechanism ("aktif-sahne değişimini dinleyen ... bir tetikleyici", not `Awake`/`Start`/`OnEnable` itself performing the evaluation) and mirrors ADR-0009's already-established pattern of a scene/session-lifetime `MonoBehaviour` subscribing to `OnTransitionStateChanged` lazily rather than at construction time.

**Why not the alternatives**: a one-frame-delayed coroutine (`yield return null` once, then evaluate) was considered and rejected — see Alternative 2. A dedicated new scene-activation signal was considered and rejected — see Alternative 3.

### Selection algorithm and `UsedCallbackIds` persistence

```csharp
[Serializable]
public struct CallbackEntry {
    public string CallbackId;   // stable, author-assigned; the UsedCallbackIds key
    public string ClueId;       // gates this entry on AnlatiDurumIpucuTakibi.Instance.IsClueKnown(ClueId)
    public int Priority;        // lower value = higher priority (kept first when capped) — this ADR's
                                 // own formalization; the GDD specifies only "writer-assigned order",
                                 // not the int's direction, so this convention is stated explicitly here
                                 // rather than left to an implementer's guess.
    [TextArea] public string Text;
}

// Config asset — "owns config", per architecture.md Principle #5, kept
// separate from the runtime UsedCallbackIds state below.
[CreateAssetMenu(menuName = "Beyond The Line/Dialogue Scene Config")]
public sealed class DialogueSceneConfig : ScriptableObject {
    [SerializeField] private string[] _baseDialogueLines;      // writer content, out of scope for this ADR
    [SerializeField] private CallbackEntry[] _callbackPool;
    [SerializeField] private int _maxCallbacksPerScene = 3;    // Tuning Knob, GDD default

    public IReadOnlyList<string> BaseDialogueLines => _baseDialogueLines;
    public IReadOnlyList<CallbackEntry> CallbackPool => _callbackPool;
    public int MaxCallbacksPerScene => _maxCallbacksPerScene;
}

// using System.Linq; — required for OrderBy() below.
//
// Pure C# — no Unity types — testable via [Test] with synthetic
// CallbackEntry[]/IsClueKnown/IsCallbackUsed inputs, satisfying
// coding-standards.md's BLOCKING unit-test rule without a live scene.
public static class DialogueCallbackSelector {
    public static List<CallbackEntry> SelectCallbacks(
            IReadOnlyList<CallbackEntry> pool, int maxCallbacks,
            Func<string, bool> isClueKnown, Func<string, bool> isCallbackUsed) {
        var candidates = new List<CallbackEntry>();
        foreach (var entry in pool) {
            if (isCallbackUsed(entry.CallbackId)) continue;       // already played this night
            if (!isClueKnown(entry.ClueId)) continue;             // clue not yet known
            candidates.Add(entry);
        }
        // Corrected during unity-specialist validation (2026-08-07): the
        // original sketch used List<T>.Sort(), which is NOT stable
        // (introsort) — ties would NOT reliably keep CallbackPool's own
        // authored order, contradicting the very claim this comment used
        // to make. Since Priority defaults to 0 for any entry a writer
        // hasn't explicitly set, a freshly-authored CallbackPool ties on
        // essentially every entry, making this bug near-guaranteed to
        // manifest on the first real content pass, not just a rare edge
        // case. OrderBy() is documented as a stable sort — ties now
        // correctly preserve authored order, matching "writer-assigned
        // Priority order" (GDD Core Rules).
        var ordered = candidates.OrderBy(e => e.Priority).ToList();
        int count = Math.Max(0, Math.Min(maxCallbacks, ordered.Count));  // clamp — a misconfigured
                                                                          // negative MaxCallbacksPerScene
                                                                          // must not throw
        return ordered.GetRange(0, count);  // skipped-but-unused candidates are NOT marked used —
                                             // GDD: eligible again in a later scene
    }
}
```

```csharp
// using UnityEngine.UIElements; using UnityEngine.SceneManagement; (for
// SceneManager.GetActiveScene()/Scene comparison below) elided along
// with other UnityEngine usings throughout this ADR's sketches.
//
// Lives IN the psychiatry scene itself (loaded/unloaded with it via
// Seviye/Sahne Geçişi, ADR-0008) — deliberately NOT a persistent-scene
// singleton like UIRoot/PlayerStateProvider/SceneTransitionManager,
// since a fresh instance per scene-load is exactly what the one-shot
// subscribe/evaluate/unsubscribe shape below needs.
public sealed class DialogueSceneController : MonoBehaviour {
    [SerializeField] private DialogueSceneConfig _config;

    private bool _hasEvaluated;
    private VisualElement _subtitle;

    private void OnEnable() {
        SceneTransitionManager.Instance.OnTransitionStateChanged += HandleTransitionStateChanged;
    }

    private void OnDisable() {
        // Redundant with the explicit unsubscribe in HandleTransitionStateChanged
        // below for the common case, but this is the actual safety net if the
        // scene is ever unloaded before its own Complete is observed (e.g. a
        // Failed transition, or a future abort path) — never leave a dangling
        // subscription on a MonoBehaviour-owned event (ADR-0009's own established
        // OnEnable/OnDisable-paired-subscription convention).
        SceneTransitionManager.Instance.OnTransitionStateChanged -= HandleTransitionStateChanged;
    }

    private void HandleTransitionStateChanged(TransitionState newState, TransitionType type) {
        if (_hasEvaluated || newState != TransitionState.Complete) return;
        // Corrected during unity-specialist validation (2026-08-07): an
        // earlier draft evaluated on ANY Complete, reasoning it must be
        // this scene's own swap since SceneTransitionManager never runs
        // two same-type transitions concurrently. False for the
        // PreloadHardCut path (ADR-0008) — this scene's Awake/OnEnable
        // (and therefore this subscription) can fire while the scene
        // sits fully loaded but INACTIVE in the background, well before
        // its real swap, while unrelated SOFT transitions (the depot/
        // ballroom elevator) keep completing freely in the meantime. The
        // scene-identity check below is what actually distinguishes "an
        // unrelated transition finished" from "MY swap finished" — event
        // ordering alone cannot, once PreloadHardCut is in play.
        if (gameObject.scene != SceneManager.GetActiveScene()) return;  // not my swap (yet)
        _hasEvaluated = true;
        SceneTransitionManager.Instance.OnTransitionStateChanged -= HandleTransitionStateChanged;  // one-shot
        EvaluateAndDisplay();
    }

    private void EvaluateAndDisplay() {
        var selected = DialogueCallbackSelector.SelectCallbacks(
            _config.CallbackPool, _config.MaxCallbacksPerScene,
            AnlatiDurumIpucuTakibi.Instance.IsClueKnown,   // ADR-0007
            DiyalogAnlatiIcerigi.Instance.IsCallbackUsed); // this ADR
        foreach (var entry in selected) {
            DiyalogAnlatiIcerigi.InternalInstance.MarkCallbackUsed(entry.CallbackId);
        }
        // Handing `_config.BaseDialogueLines` + `selected` to an actual
        // line-by-line subtitle playback/advance mechanism is explicitly
        // out of scope (Context) — this ADR's contract ends at "here is
        // the ordered set of lines to play," matching architecture.md's
        // own Module Ownership framing ("scene-entry dialogue playback
        // hook", not a full playback state machine). The subtitle root
        // lookup below establishes only the rendering handle a future
        // playback implementation will use.
        _subtitle = UIRoot.Instance.Root.Q<VisualElement>("dialogue-subtitle");
        if (_subtitle == null) Debug.LogError("UIRoot's UXML is missing #dialogue-subtitle.", this);
    }
}
```

### `UsedCallbackIds` persistence: ADR-0001 static-facade pattern

```csharp
public interface IDiyalogAnlatiIcerigiState {
    bool IsCallbackUsed(string callbackId);
    IReadOnlyCollection<string> GetUsedCallbackIds();
}

// Plain C# class + interface + static facade, reset via
// FoundationBootstrap.ResetAll() — same pattern as every other
// session-scoped state in this project (ADR-0001), already anticipated
// as a session_scoped_state_static_facade consumer in the registry
// since ADR-0001 was written. No constructor-time subscriptions (unlike
// GeceOturumDurumu/AnlatiDurumIpucuTakibi) — this state is pure
// bookkeeping written only by DialogueSceneController, so there is no
// constructor_subscribing_foundation_service_reset_before_event_source
// ordering concern to resolve.
public sealed class DiyalogAnlatiIcerigiState : IDiyalogAnlatiIcerigiState {
    private readonly HashSet<string> _usedCallbackIds = new();

    public bool IsCallbackUsed(string callbackId) => _usedCallbackIds.Contains(callbackId);
    public IReadOnlyCollection<string> GetUsedCallbackIds() => _usedCallbackIds;

    internal void MarkCallbackUsed(string callbackId) => _usedCallbackIds.Add(callbackId);
    internal void ResetOnLoad() => _usedCallbackIds.Clear();
}

public static class DiyalogAnlatiIcerigi {
    private static DiyalogAnlatiIcerigiState _state = new();
    public static IDiyalogAnlatiIcerigiState Instance => _state;

    // InternalInstance: same escape hatch ADR-0006 (GeceOturumDurumu)/
    // ADR-0009 (AdaptifSesSistemi) already established for a MonoBehaviour
    // driver needing a write path the public read-only interface doesn't
    // expose — DialogueSceneController.MarkCallbackUsed(), here.
    internal static DiyalogAnlatiIcerigiState InternalInstance => _state;

    internal static void ResetOnLoad() { _state.ResetOnLoad(); }  // registered in FoundationBootstrap.ResetAll()
}
```

### Build-time consistency check: `MaxCallbacksPerScene` vs. per-night clue count

**Added during TD-ADR review (2026-08-07) — the original draft silently dropped a GDD-mandated, already-patterned requirement.** `diyalog-anlati-icerigi-2026-08-02.md`'s Core Rules require: for a night configured with only one psychiatry scene (MVP's own configuration), that scene's `MaxCallbacksPerScene` must not be smaller than the total number of clues configured for that night — otherwise at least one clue's callback can never be shown, permanently, with no in-game error (the exact bug the GDD's 2026-08-03 default-value correction, from 2 to 3, already fixed once for the un-configurable case; this check guards the still-configurable one). The GDD explicitly points at this project's own established mechanism for this class of check: `ani-tetikleyici-etkilesim.md`'s `IPreprocessBuildWithReport`-based edit-time validation (asset scan via `AssetDatabase.FindAssets`, `BuildFailedException`/`report.SummarizeErrors()` to actually block the build, `OnValidate()` explicitly ruled out since a `ScriptableObject` cannot see sibling assets from its own `OnValidate()`). That GDD's own "Paylaşılan araç notu" (shared-tool note) already anticipates `Anlatı Durum/İpucu Takibi`'s `clueId`-duplication check sharing one `IPreprocessBuildWithReport` implementation rather than being written twice — this ADR's check is a third contribution to that same eventual shared editor utility, not a fourth independent implementation:

```csharp
// Editor-only (wrapped in #if UNITY_EDITOR / a dedicated Editor assembly,
// per this project's established convention). Contributes one more check
// to the shared IPreprocessBuildWithReport pass ani-tetikleyici-etkilesim.md
// and anlati-durum-ipucu-takibi.md's own checks already anticipate sharing
// (GDD's own "Paylaşılan araç notu") — not a fourth independent
// IPreprocessBuildWithReport implementation.
private static void ValidateMaxCallbacksPerScene(BuildReport report) {
    var configGuids = AssetDatabase.FindAssets("t:DialogueSceneConfig");
    // MVP simplification: exactly one night, exactly one psychiatry scene,
    // so "total configured clue count for that night" is simply every
    // ClueDefinition asset in the project (ADR-0007's registry). This
    // equivalence is MVP-specific and breaks once Full Vision's multi-
    // night/multi-scene structure exists — flagged in Risks, below.
    int totalClueCount = AssetDatabase.FindAssets("t:ClueDefinition").Length;
    if (configGuids.Length != 1) return;  // multi-scene case: rule doesn't apply (GDD, "tek psikiyatri sahnesi")
    var config = AssetDatabase.LoadAssetAtPath<DialogueSceneConfig>(AssetDatabase.GUIDToAssetPath(configGuids[0]));
    if (config.MaxCallbacksPerScene < totalClueCount) {
        throw new BuildFailedException(
            $"{AssetDatabase.GUIDToAssetPath(configGuids[0])}: MaxCallbacksPerScene " +
            $"({config.MaxCallbacksPerScene}) is less than this night's total configured clue " +
            $"count ({totalClueCount}) — at least one callback could become permanently unreachable.");
    }
}
```

## Alternatives Considered

### Alternative 1: Evaluate directly in the scene object's own `Awake()`/`Start()` (rejected by the GDD itself)
- **Description**: `DialogueSceneController.Awake()` or `Start()` directly runs `DialogueCallbackSelector.SelectCallbacks(...)`.
- **Pros**: Simplest possible code — no event subscription, no state machine awareness at all.
- **Cons**: This is exactly the bug the GDD's Core Rules were revised to prevent (2026-08-04 critical finding) — `Awake`/`Start` fire during `Preloading`, before the scene is genuinely active, reproducing the saturation-timing class of bug via a second mechanism.
- **Rejection Reason**: Directly contradicts the GDD's own Approved Core Rules and its first Acceptance Criterion; not a real option.

### Alternative 2: One-frame-delayed coroutine
- **Description**: `Start()` launches a `Coroutine` that does `yield return null` once, then evaluates — no reference to `SceneTransitionManager` at all.
- **Pros**: Zero coupling to ADR-0008's event; matches the GDD's own alternate suggestion ("basitçe bir kare gecikmeli").
- **Cons**: Doesn't verify the swap actually completed — it waits an arbitrary engine frame, which happens to be sufficient today only because nothing else currently delays scene activation by more than one frame. If `Seviye/Sahne Geçişi`'s own timing ever changes (e.g. a future multi-frame `Swapping` step), this silently breaks with no compile-time or even obvious runtime signal.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-07): the `OnTransitionStateChanged`-based mechanism is a genuine correctness guarantee tied to the actual state machine, not a timing coincidence, and ADR-0009 already established the identical subscribe-to-this-event pattern for the same underlying need — reusing it keeps one deferred-activation idiom project-wide instead of two.

### Alternative 3: New dedicated scene-activation signal
- **Description**: Introduce a new event (e.g. `SceneActivated`), fired by a small bootstrapper object specific to the psychiatry scene, decoupled from `SceneTransitionManager` entirely.
- **Pros**: Would not require the psychiatry scene to know about `SceneTransitionManager` at all.
- **Cons**: Duplicates a signal `SceneTransitionManager.OnTransitionStateChanged` already provides; every consumer would need to independently reconstruct the same "first event after subscribing" reasoning this ADR already works out once; no other system in this project has needed a second, scene-local activation signal.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-07): disproportionate for a project already standardized on one Foundation-owned transition-state event; would be the third independently-invented "wait for the scene to be ready" mechanism (`Awake`/`Start` implicit — wrong; `OnTransitionStateChanged` — this ADR; a new signal — this alternative) where one already suffices.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #12.
- Resolves the GDD's own critical (2026-08-04) timing requirement with a mechanism that is a correctness guarantee (tied to the actual state machine transition), not a timing coincidence.
- Reuses every dependency this system already had (ADR-0001, ADR-0002, ADR-0007, ADR-0008, ADR-0010) with zero new engine mechanisms — the smallest change consistent with the project's established patterns.
- `DialogueCallbackSelector.SelectCallbacks` is a pure, statically-testable function — satisfies `coding-standards.md`'s BLOCKING unit-test rule for the one piece of real decision logic in this system without any scene/transition machinery in the test.
- Closes the GDD's `MaxCallbacksPerScene`-vs-clue-count build-time gap (added during TD-ADR review, 2026-08-07) by contributing to the same shared `IPreprocessBuildWithReport` pass `ani-tetikleyici-etkilesim.md`/`anlati-durum-ipucu-takibi.md` already anticipate, rather than leaving a third, GDD-mandated edit-time check unimplemented.

### Negative
- `DialogueSceneController` must remember to re-subscribe on every scene load (via `OnEnable()`) since it is deliberately not a persistent-scene singleton — a small amount of subscribe/unsubscribe bookkeeping every other Foundation `MonoBehaviour` in this project (which subscribes once, for the whole session) doesn't carry.
- The exact base-dialogue/callback playback timing (auto-advance vs. player-advance, per-line display duration) remains undesigned after this ADR — `EvaluateAndDisplay()`'s contract stops at "here is the ordered line set and the subtitle element handle," not a full playback state machine. A future presentation-layer pass (or an implementation-time decision, per this project's "can defer to implementation" bucket) must still resolve this before the feature is playable end-to-end.
- `Priority`'s integer direction (lower = higher priority) is this ADR's own formalization, not stated by the GDD — a future writer/designer confusion risk if not clearly documented in the `DialogueSceneConfig` asset's own tooltip/inspector text at implementation time.
- `architecture.md`'s module-level Dependency Diagram currently shows only `Diyalog/Anlatı İçeriği ──> Anlatı Durum/İpucu Takibi (IsClueKnown)`; this ADR adds a real (if stateless) `Diyalog/Anlatı İçeriği ──> UIRoot (ADR-0010)` edge via `UIRoot.Instance` that the diagram does not yet reflect (added during TD-ADR review, 2026-08-07) — not a violation (ADR-0002 explicitly steered ADR-0010/0012 toward sharing one accessor), but the diagram is now stale and should gain this edge in a future `architecture.md` touch-up pass, same as ADR-0010 flagged `etkilesim-sistemi.md`'s own stale Open-Question text rather than silently leaving it.

### Risks
- **Risk (corrected, unity-specialist review, 2026-08-07)**: `OnTransitionStateChanged` carries no scene-identity parameter (`TransitionState`, `TransitionType` only). An earlier draft assumed "first `Complete` after subscribing is mine" was safe because `SceneTransitionManager` never runs two *same-type* transitions concurrently — false once `PreloadHardCut` is in play, since that path advances `_hardCutPreloadState` independently of `CurrentState`/`OnTransitionStateChanged` (ADR-0008), letting this scene's `OnEnable()` subscribe long before its real swap while unrelated SOFT transitions complete freely in between. **Mitigation**: `HandleTransitionStateChanged` now additionally checks `gameObject.scene == SceneManager.GetActiveScene()` before evaluating (Decision, above) — this reads the one piece of identity the broadcast event itself doesn't carry directly from the engine, rather than assuming event-arrival-order implies event-subject-identity. If a future revision adds genuine concurrent-transition support to ADR-0008, this scene-identity check (not event ordering) is still the correct guard and needs no further change.
- **Risk**: If the psychiatry scene is ever loaded via a path that does not go through `SceneTransitionManager` at all (e.g. a direct Editor "Play from this scene" during development, or a future debug/dev-menu scene loader), `OnTransitionStateChanged` never fires and callback selection never runs — the scene would show only the base dialogue, silently, with no error. **Mitigation**: matches this project's existing "eksik ipucu beklenen durumdur" (missing-clue-is-an-expected-state) tolerance for the zero-candidate case exactly — indistinguishable from the legitimate zero-known-clue outcome, which the GDD already treats as a non-error. Acceptable for MVP; a dev-loaded-scene warning could be added at implementation time if this proves confusing during testing.
- **Risk**: `DiyalogAnlatiIcerigiState`'s `ResetOnLoad()` must be registered in `FoundationBootstrap.ResetAll()` (ADR-0001) alongside the other five current consumers, even though `Diyalog/Anlatı İçeriği` is a Core-layer, not Foundation-layer, module (`architecture.md` Module Ownership). **Mitigation**: not a layering violation — `FoundationBootstrap.ResetAll()` is this project's general session-reset mechanism for any static-facade state (already used this way; the registry's `session_scoped_state_static_facade` contract lists `diyalog-anlati-icerigi` as an anticipated consumer independent of layer), not an exclusively-Foundation-layer construct. Worth noting explicitly (TD-ADR review, 2026-08-07): every current consumer of this pattern is Foundation-layer — this is the pattern's **first Core-layer consumer**, setting precedent for any future Core/Feature-layer system needing session-scoped state, the same way ADR-0009 called out being "the pattern's first consumer whose sole writer is a MonoBehaviour."
- **Risk (added during TD-ADR review, 2026-08-07)**: `ValidateMaxCallbacksPerScene`'s "total clue count = every `ClueDefinition` asset in the project" equivalence (Decision, build-time check) is an MVP-only simplification — it is only correct because MVP has exactly one night. Once Full Vision's multi-night structure exists, this check will silently validate against the *wrong* total (all nights' clues combined, not just the relevant night's) unless `ClueDefinition`/`DialogueSceneConfig` gain an explicit per-night grouping key this ADR does not define. **Mitigation**: acceptable for MVP (the equivalence is exact, not approximate, at MVP's one-night scale); flagged here so the future Çoklu Gece İlerlemesi Vertical Slice GDD — which already owns the related `UsedCallbackIds` cross-night-persistence gap (see Context) — knows this build-time check needs a matching per-night revision at the same time, not independently discovered later.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `diyalog-anlati-icerigi-2026-08-02.md` | Callback evaluation deferred past `Awake`/`Start` to genuine scene-active time (Core Rules, critical 2026-08-04 finding; first Acceptance Criterion) | `DialogueSceneController` subscribes to `OnTransitionStateChanged` in `OnEnable()`, evaluates only on the `Complete` whose swap makes `gameObject.scene` the active scene — see Decision |
| `diyalog-anlati-icerigi-2026-08-02.md` | Candidate filtering by `IsClueKnown(clueId)`, `Priority`-ordered cap at `MaxCallbacksPerScene`, skipped (not deleted) overflow candidates | `DialogueCallbackSelector.SelectCallbacks` |
| `diyalog-anlati-icerigi-2026-08-02.md` | `UsedCallbackIds` bookkeeping, separate from `Anlatı Durum`'s `KnownClueIds` | `DiyalogAnlatiIcerigiState`/`DiyalogAnlatiIcerigi`, ADR-0001 static-facade pattern |
| `diyalog-anlati-icerigi-2026-08-02.md` | Zero/missing-candidate case plays only base dialogue, no error | `SelectCallbacks` returns an empty list; `EvaluateAndDisplay()` has no special-case branch for it — the absence of selected callbacks is itself the correct "base dialogue only" outcome |
| `diyalog-anlati-icerigi-2026-08-02.md` Open Questions | `UsedCallbackIds` cross-night persistence | Explicitly out of scope (Context) — owned by the future Çoklu Gece İlerlemesi Vertical Slice GDD, per the GDD's own Open Questions and `architecture.md`'s Required ADR #12 description |
| `diyalog-anlati-icerigi-2026-08-02.md` | Build-time consistency check — single-scene night's `MaxCallbacksPerScene` must not be less than that night's total configured clue count (Core Rules; last Acceptance Criterion) | `ValidateMaxCallbacksPerScene`, `IPreprocessBuildWithReport`, added during TD-ADR review 2026-08-07 — see Decision |
| `architecture.md` | Module Ownership row (line 95) — `DialogueSceneConfig`/`CallbackPool`/`UsedCallbackIds` ownership, `IsClueKnown` (Foundation) consumption, UI Toolkit subtitle display | Implemented as designed |

## Performance Implications
- **CPU**: One `SelectCallbacks` call per psychiatry-scene load (not per-frame) — a single small `List<CallbackEntry>` filter/sort over a GDD-bounded pool (MVP: at most 3 entries); negligible.
- **Memory**: One `HashSet<string>` (`_usedCallbackIds`, session-lifetime, MVP-bounded at 3 entries); one small `DialogueSceneConfig` asset per scene.
- **Load Time**: N/A — `DialogueSceneConfig` is a plain serialized `ScriptableObject` field reference, not an Addressable (no async load mechanism needed at this pool size; revisit only if Full Vision's larger pools change this calculus).
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Diyalog/Anlatı İçeriği` is not yet implemented).

## Validation Criteria
- A `[Test]` drives `DialogueCallbackSelector.SelectCallbacks` with synthetic `isClueKnown`/`isCallbackUsed` delegates and asserts: unknown-clue entries excluded, already-used entries excluded, `Priority`-ascending ordering with **ties preserving `CallbackPool`'s authored order** (a real `OrderBy`-stability regression test, not just a distinct-Priority case — corrected, unity-specialist review, 2026-08-07), overflow beyond `MaxCallbacksPerScene` truncated (not errored), a negative/misconfigured `MaxCallbacksPerScene` clamped to zero rather than throwing, zero-candidate input returns an empty list — no scene/transition machinery involved (GDD ACs).
- A `[UnityTest]` (or equivalent Play-mode harness) confirms: while `SceneTransitionManager.CurrentState` is `Ready` (preloaded, not yet `Swapping`) for the psychiatry scene, `DialogueSceneController` has NOT yet called `SelectCallbacks` — directly covers the GDD's first, critical Acceptance Criterion.
- A `[UnityTest]` specifically exercises the `PreloadHardCut` path (corrected, unity-specialist review, 2026-08-07 — the scenario the original mechanism got wrong): `PreloadHardCut` the psychiatry scene, then complete one or more unrelated SOFT transitions while it sits preloaded, asserting `DialogueSceneController` has NOT evaluated; only after the real `RequestHardCut` swap's own `Complete` fires does evaluation run exactly once.
- A `[UnityTest]` confirms `DialogueSceneController` unsubscribes from `OnTransitionStateChanged` after its own scene's `Complete` (no duplicate evaluation on a later, unrelated transition while the scene remains loaded).
- A `[Test]`/inspection confirms `DiyalogAnlatiIcerigiState.ResetOnLoad()` is registered in `FoundationBootstrap.ResetAll()` (ADR-0001) — a fresh Play session must not carry over a previous session's `UsedCallbackIds`.
- A `tests/editor/` EditMode test (added during TD-ADR review, 2026-08-07 — matching `ani-tetikleyici-etkilesim.md`'s own established test-evidence pattern for this exact mechanism) confirms `ValidateMaxCallbacksPerScene` throws `BuildFailedException` when a single configured `DialogueSceneConfig`'s `MaxCallbacksPerScene` is less than the project's total `ClueDefinition` count, and does not throw when it is greater than or equal.

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — source of the `UsedCallbackIds` persistence mechanism this ADR reuses.
- ADR-0002 (UI Framework) — source of the `#dialogue-subtitle` element this ADR renders into.
- ADR-0007 (Clue Tracking Architecture) — source of `AnlatiDurumIpucuTakibi.Instance.IsClueKnown`.
- ADR-0008 (Scene Transition State Machine) — source of `OnTransitionStateChanged`/`TransitionState`, the mechanism this ADR's deferred-evaluation timing depends on entirely.
- ADR-0010 (Interaction State Machine) — source of `UIRoot.Instance`, reused here per ADR-0002's own steering.
- `design/gdd/ani-tetikleyici-etkilesim.md` — source of the `IPreprocessBuildWithReport` edit-time-validation pattern this ADR's `ValidateMaxCallbacksPerScene` check contributes to (Decision) — not yet formalized into its own ADR, but already an established, GDD-cited project convention.
