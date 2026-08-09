# ADR-0009: Audio Architecture — Mixer Groups and Stinger Pooling

> **Unity Specialist Validation**: BLOCKING (2 findings, found and fixed) 2026-08-07 — (1) `AmbientZoneVolume`'s `FindObjectOfType<AdaptifSesController>()` calls used a `[Obsolete]`-since-2023.1 API, and were unnecessary regardless — replaced with a direct `AdaptifSesController.Instance` read. (2) `AdaptifSesController.Instance` had no Reload-Scene staleness risk/test, repeating an omission ADR-0008 already named and fixed once for the identical `Awake()`-only-static-field shape (ADR-0003's `PlayerStateProvider`, ADR-0008's `SceneTransitionManager`) — added the matching Risks bullet and `[UnityTest]`. Also fixed 2 MINOR findings: an `Invoke()` call that couldn't actually carry the `AudioSource` parameter it needed (switched to a `Coroutine`, this project's established idiom for exactly this shape), and an ADR-0001 comment-correction description that fixed only 1 of 3 now-stale clauses.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-07 — verified the ADR's central claim (ADR-0001's static-facade-subscribes text genuinely contradicts its own "Note on scope" carve-out, not a quiet redesign) and the Alternatives/architecture.md-citation/GDD-fidelity checks all held up. 1 real finding, fixed: the stinger pool tracked a single Idle/Playing/Cooldown state keyed by `AudioSource`, contradicting the GDD's explicit "shiftId başına" (per-shiftId) Cooldown model and its Edge Case that a freed source is immediately available to a *different* shiftId even while its previous occupant is still in Cooldown — the original design would have kept a source unavailable to every other shiftId for the full cooldown window, risking a Persistent shiftId's one-time-ever stinger never playing if the small pool happened to be saturated. Split into two independent trackers (pool availability vs. per-shiftId cooldown) and added the missing test coverage. Also documented (not a code change) why the Abrupt-mute loop is safe against ADR-0008's overlapping deferred-unload windows, per the reviewer's own request.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-07

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Audio |
| **Knowledge Risk** | LOW — `AudioMixer`/`AudioMixerGroup`/`AudioSource` (`PlayOneShot`, `spatialBlend`, `minDistance`/`maxDistance`), brickwall-limiter mixer effects, and `OnTriggerEnter`/`Physics.OverlapSphere` are all long-stable, pre-cutoff APIs. `docs/engine-reference/unity/modules/audio.md` itself flags a documentation gap ("Knowledge Gap: Unity 6 audio mixer improvements") — nothing this ADR relies on touches whatever that gap covers (no snapshot transitions, no sidechain ducking, no new Unity-6-era mixer feature; the GDD explicitly rejects dynamic ducking/snapshots in favor of static gain-staging, see Decision). |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/modules/audio.md`, `docs/engine-reference/unity/deprecated-apis.md`, `docs/engine-reference/unity/breaking-changes.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md`, `docs/architecture/adr-0005-isik-volume-rendering-architecture.md`, `docs/architecture/adr-0006-session-state-service-and-round-counter-ownership.md`, `docs/architecture/adr-0008-scene-transition-state-machine.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None new. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (In-Memory Static Service Pattern) — this ADR corrects it, see Decision. ADR-0005 (Işık/Volume) — source of `OnShiftStateChanged`/`IsShiftPersistent`/`GetStingerAudioRadius`. ADR-0006 (Session State) — source of `CurrentRoundIndex`/`TotalRoundCount`. ADR-0008 (Scene Transition) — source of `OnTransitionStateChanged`/`GetCurrentHardCutAbrupt()`, and the lazy-subscription constraint this ADR must satisfy. |
| **Enables** | Nothing else in the Required ADRs list depends on this one — it's the last Foundation "must have" ADR. Future stories for `Görev/Taşıma Döngüsü` (SFX routing) and `Birinci Şahıs Kontrolcü` (footstep calls) consume this ADR's public surface directly, not via a future ADR. |
| **Blocks** | Any story implementing `Adaptif Ses Sistemi` itself; any `Görev/Taşıma Döngüsü` story needing pickup/delivery/jostle SFX; any `Birinci Şahıs Kontrolcü` story wiring footstep calls. |
| **Ordering Note** | This ADR also edits ADR-0001's `FoundationBootstrap.ResetAll()` — not to reorder `AdaptifSesSistemi.ResetOnLoad()`, but to correct its justifying comment (it does not subscribe to anything in its constructor — see Decision). |

## Context

### Problem Statement

`adaptif-ses-sistemi.md` (Approved, 3+ design-review rounds, still carrying a "Needs Revision" header artifact per its own note — same class of stale-header state this project's other GDDs have, not unique to this one, and not a blocker per the session's established precedent of proceeding on fix-converged-but-formally-unre-verified GDDs) fully specifies: 4 mixer groups (Ambiance/Stinger/CutSting/SFX) with a static-brickwall-limiter-only mixing philosophy (explicitly no dynamic ducking/snapshots — a deliberate rejection, not an oversight); a 2-3-layer diegetic ambient system per area with `ZoneChanged`-triggered crossfade plus a round-index-driven 3rd tension layer; a pooled (3-4 source) memory-trigger stinger system with an `Idle`/`Playing`/`Cooldown` per-source state machine and a session-persistent `HeldSessionAlreadyPlayed` write-once guard; a HARD CUT sting gated on `Abrupt==true`; an instant-mute rule for `Abrupt==true` HARD CUTs (vs. crossfade-to-silence for `Abrupt==false`); and a footstep system driven inline by the FPC's own stride-phase accumulator. None of this has a concrete Unity implementation mechanism yet.

`architecture.md`'s own Module Ownership row (line 87) and ADR-0001 already correctly anticipated that this system is a **hybrid** — ADR-0001's "Note on scope" explicitly carves out that `Adaptif Ses Sistemi` "owns `AudioMixer`/pooled `AudioSource`s and exposes side-effecting methods like `PlayFootstep(speed)` (MonoBehaviour-driven, outside this ADR), while its `HeldSessionAlreadyPlayed` guard follows this pattern" — i.e., the state slice is a plain static-facade class (ADR-0001 shape), everything else is `MonoBehaviour`-driven. **What ADR-0001 got wrong, corrected here**: its `FoundationBootstrap.ResetAll()` comment for `AdaptifSesSistemi` claims the static-facade class itself "subscribes to Işık/Volume's `OnShiftStateChanged` ... in its constructor." Re-reading the GDD's own Core Rules against ADR-0001's own scope carve-out, this can't be right — the *decision* of whether/when to play a stinger (checking `IsShiftPersistent`, checking the guard, pulling a pooled `AudioSource`, calling `PlayOneShot`) is playback orchestration, not "state/event slice." A plain C# class with `HeldSessionAlreadyPlayed` as its only field has no legitimate reason to subscribe to an event whose entire handler body is about triggering audio playback it doesn't itself own. This ADR moves the subscription (and all playback-triggering logic) to the `MonoBehaviour` side, leaving the static facade as pure state — matching ADR-0001's own scope carve-out for the first time, rather than contradicting it.

### Constraints

- Must not deviate from `adaptif-ses-sistemi.md`'s already-Approved Core Rules — this ADR formalizes, it does not redesign. In particular: no dynamic ducking/snapshot transitions (static gain-staging + a one-time brickwall limiter insert only); the `Held`/`Shifting-In` dual-trigger-path-with-`IsShiftPersistent`-gate for stingers; the `Abrupt`-gated instant-mute-vs-crossfade-to-silence split for HARD CUT; the co-residency guard for `AmbientZoneVolume` (same class of fix ADR-0005 already applied to Işık/Volume's own zones).
- Must satisfy ADR-0008's `constructor_time_subscription_to_scenetransitionmanager` forbidden pattern — no reference to `SceneTransitionManager.Instance` from any `FoundationBootstrap.ResetAll()`-time constructor.
- `HeldSessionAlreadyPlayed` must survive scene loads within a session exactly like every other Foundation static-facade state (ADR-0001 pattern), reset once per genuine session via `FoundationBootstrap.ResetAll()`.
- `AmbientZoneVolume`'s co-residency and deferred-first-tick-overlap-check logic (already fully specified in GDD Core Rules) must be implemented as designed, not re-derived.

### Requirements

- `PlayFootstep(float speed)` must remain the FPC's sole entry point into this system for footstep audio — no independent `Velocity`/`IsGrounded` subscription (GDD Interactions with Other Systems).
- A public, read-only routing target for `Görev/Taşıma Döngüsü`'s own pickup/delivery/jostle `AudioSource`s to reach the "SFX" mixer group (GDD Dependencies, "kendisine bağımlı olanlar").
- The `HeldSessionAlreadyPlayed` guard's query/write surface must be reachable from whatever component ends up owning the stinger-trigger decision (now the `MonoBehaviour` side, per Decision), without breaking ADR-0001's "state accessed only through its declared interface" convention.

## Decision

### Corrected split: static facade is pure state, `MonoBehaviour` owns every subscription and every playback decision

**Corrected from ADR-0001's own text (2026-08-07, this ADR)**: `AdaptifSesSistemi` (the static facade) owns exactly one thing — `HeldSessionAlreadyPlayed` — and subscribes to nothing, reads nothing from another Foundation service, and has no future subscription pending either. It has **no upstream Foundation dependency**, unlike ADR-0001's current comment, which claims three things that are all now wrong under this ADR's design: that it "subscribes to Işık/Volume's `OnShiftStateChanged` ... in its constructor," that it "reads Gece/Oturum's round counters," and that it has "a FUTURE subscription to Seviye/Sahne Geçişi's `OnTransitionStateChanged` (ADR-0009)" that must stay lazy. All three clauses describe behavior that belongs entirely to `AdaptifSesController` (the `MonoBehaviour`) now — the round-counter read moved to `AmbientZoneVolume.Update()`, and both subscriptions moved to `AdaptifSesController.Start()` (see below). **Corrected during unity-specialist validation (2026-08-07)**: an earlier draft of this note only fixed the subscription clause and left the other two stale, which would have left ADR-0001 still misattributing behavior to the wrong class. The full replacement comment (all three lines, not just one) is: `// pure state (HeldSessionAlreadyPlayed only) — no upstream Foundation dependency, no subscriptions. All playback orchestration (including the OnTransitionStateChanged subscription) lives in AdaptifSesController (ADR-0009), a MonoBehaviour outside FoundationBootstrap's scope.` The *ordering* itself is unchanged — `AdaptifSesSistemi.ResetOnLoad()` can stay in its current last position, since a position change buys nothing now that it has no dependency to order against, and minimizing the diff to an already-Accepted-pending-review file is preferable to a needless reshuffle.

```csharp
public interface IAdaptifSesState {
    bool HasAlreadyPlayed(string shiftId);
}

public sealed class AdaptifSesState : IAdaptifSesState {
    private readonly HashSet<string> _heldSessionAlreadyPlayed = new();
    public bool HasAlreadyPlayed(string shiftId) => _heldSessionAlreadyPlayed.Contains(shiftId);
    internal void MarkPlayed(string shiftId) => _heldSessionAlreadyPlayed.Add(shiftId);
    internal void ClearPlayed(string shiftId) => _heldSessionAlreadyPlayed.Remove(shiftId);
}

public static class AdaptifSesSistemi {
    public static IAdaptifSesState Instance => _current;
    internal static AdaptifSesState InternalInstance => _current;  // MarkPlayed/ClearPlayed access,
                                                                     // same shape as ADR-0006's
                                                                     // GeceOturumDurumu.InternalInstance
    internal static void ResetOnLoad() => _current = new AdaptifSesState();
    private static AdaptifSesState _current = new();
}
```

All subscriptions, all playback decisions, and all `AudioSource`/`AudioMixerGroup` ownership move to a `MonoBehaviour`, `AdaptifSesController`, living on the same persistent "Foundation" scene ADR-0008 already introduced for `SceneTransitionManager` (no 4th persistent scene — this is a natural, low-cost reuse, not a new pattern). Its subscriptions happen in `Start()` (not `Awake()` — `Start()` runs after every object's `Awake()` in the same frame, an extra safety margin beyond what's strictly required, cheap to take):

```csharp
public sealed class AdaptifSesController : MonoBehaviour {
    [SerializeField] private AudioMixerGroup _ambianceGroup, _stingerGroup, _cutStingGroup, _sfxGroup;
    [SerializeField] private AudioSource[] _stingerPool;      // 3-4 sources, Stinger group
    [SerializeField] private AudioSource _cutStingSource;     // CutSting group, own pool of one
    [SerializeField] private AudioSource _footstepSource;     // dedicated, not pooled

    public AudioMixerGroup SfxGroup => _sfxGroup;  // Görev/Taşıma routes its own SFX sources here

    // Corrected during TD-ADR review (2026-08-07): an earlier draft tracked
    // a single Idle/Playing/Cooldown enum keyed by AudioSource — but the GDD
    // is explicit that pool-source availability and per-shiftId Cooldown are
    // TWO SEPARATE concerns (States and Transitions: "Stinger (shiftId
    // başına)"; Edge Cases: "O boşa çıkan kaynak hemen uygun sayılır —
    // Cooldown sadece kendi shiftId'inin yeniden tetiklenmesini kısıtlar,
    // kaynağın başka bir shiftId'e uygunluğunu değil"). Keying Cooldown by
    // AudioSource would have kept a just-finished source unavailable to
    // EVERY other shiftId for the full ~1s cooldown window, not just its
    // own previous occupant — directly contradicting that Edge Case, and
    // capable of silently dropping a Persistent shiftId's one-time-ever
    // stinger if the pool's 3-4 sources happened to be saturated by
    // unrelated zones' cooldowns. Split into two independent trackers:
    private readonly HashSet<AudioSource> _playingStingerSources = new();  // pool availability only —
                                                                             // a source is free to ANY
                                                                             // shiftId the instant its
                                                                             // clip ends, cooldown or not
    private readonly HashSet<string> _shiftIdsInCooldown = new();          // per-shiftId re-trigger guard
                                                                             // only, unrelated to pool state
    // AmbientZoneVolume registry — same OnEnable/OnDisable-registration shape as
    // InteractableRegistry (ADR-0004) — needed so an Abrupt HARD CUT's instant-mute
    // rule can reach scene-local AmbientZoneVolume instances this controller
    // doesn't itself own.
    private readonly List<AmbientZoneVolume> _activeZoneVolumes = new();

    // Duplicate-instance guard — same shape as SceneTransitionManager (ADR-0008)
    // and PlayerStateProvider (ADR-0003): unconditional Debug.LogError + Destroy,
    // not a compiled-out Debug.Assert. Included from the first draft this time,
    // not added after a specialist pass had to catch its absence (as happened
    // for both of those precedents when they were first drafted).
    public static AdaptifSesController Instance { get; private set; }

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("Duplicate AdaptifSesController — destroying this instance.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // REVISED by ADR-0015 (2026-08-08) — companion revision to the in-place
    // reset regime conversion: subscriptions moved from Start() to OnEnable()
    // with symmetric OnDisable() unsubscription. Under the original shape
    // (Start()-time, never unsubscribed) the conversion of Işık/Volume to a
    // never-replaced in-place-reset facade would have made this an
    // accumulation bug in the Editor's Domain-off + Scene-Reload-ON config:
    // each Play session's destroyed controller left its handler permanently
    // on the persistent event — session N fired every stinger N times.
    // (The OnTransitionStateChanged subscription was unaffected —
    // SceneTransitionManager is itself destroyed/recreated in that config —
    // but moves too for symmetry.) Same OnEnable/OnDisable shape as
    // ADR-0013's CarrySlotRigController and ADR-0015's own controller.
    private void OnEnable() {
        // Safe here: Işık/Volume is a plain ADR-0001 static service, guaranteed
        // constructed by the time any persistent-scene MonoBehaviour's OnEnable
        // runs (well after FoundationBootstrap.ResetAll()'s SubsystemRegistration
        // timing). SceneTransitionManager.Instance is set by its own Awake() in
        // this same scene's load — guaranteed before this OnEnable only on
        // re-enable passes; the initial-load subscription is completed in
        // Start() if Instance was not yet available here (implementation
        // detail; the null-check-and-defer is one line).
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += OnShiftStateChanged;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.OnTransitionStateChanged += OnTransitionStateChanged;
    }

    private void Start() {
        // Completes the OnTransitionStateChanged subscription for the initial
        // scene-load pass if OnEnable ran before SceneTransitionManager's
        // Awake (same-scene OnEnable ordering is not guaranteed; by Start()
        // every Awake in the scene has run). Idempotent via the unsubscribe-
        // first shape below.
        SceneTransitionManager.Instance.OnTransitionStateChanged -= OnTransitionStateChanged;
        SceneTransitionManager.Instance.OnTransitionStateChanged += OnTransitionStateChanged;
    }

    private void OnDisable() {
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= OnShiftStateChanged;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.OnTransitionStateChanged -= OnTransitionStateChanged;
    }

    internal void RegisterZoneVolume(AmbientZoneVolume zone) => _activeZoneVolumes.Add(zone);
    internal void DeregisterZoneVolume(AmbientZoneVolume zone) => _activeZoneVolumes.Remove(zone);

    public void PlayFootstep(float speed) {
        // 4-6-clip repeat-protected selection, ±5% pitch, volume = speed / 1.6
        // (footstep_volume formula) — PlayOneShot on _footstepSource, per Core Rules.
    }

    private void OnShiftStateChanged(string shiftId, ShiftState newState, Vector3 zoneCenter, float radius) {
        // Guard cleanup: Shifting-Out or Dormant clears the session guard,
        // regardless of Persistent — matches GDD's corrected clear-condition
        // (previously mis-scoped to "!= Held", fixed in the GDD's own
        // 2026-08-04 revision; implemented here verbatim, not re-derived).
        if (newState is ShiftState.ShiftingOut or ShiftState.Dormant) {
            AdaptifSesSistemi.InternalInstance.ClearPlayed(shiftId);
            return;
        }

        bool isAttemptFrame = (newState == ShiftState.ShiftingIn || newState == ShiftState.Held)
                               && IsikVolumeDurumSistemi.Instance.IsShiftPersistent(shiftId);
        if (!isAttemptFrame) return;
        if (AdaptifSesSistemi.Instance.HasAlreadyPlayed(shiftId)) return;  // no-op, shared guard
        // Per-shiftId Cooldown re-trigger guard (GDD Edge Case, "Cooldown
        // re-entry") — mainly matters for reversible (non-Persistent)
        // shifts, since Persistent ones are already fully blocked by the
        // HeldSessionAlreadyPlayed guard above; a reversible shift's rapid
        // Held→Shifting-Out→Held cycle clears that guard on Shifting-Out,
        // so THIS check is what actually swallows the double-trigger case.
        if (_shiftIdsInCooldown.Contains(shiftId)) return;

        var source = FindIdleStingerSource();  // any source NOT in _playingStingerSources
        if (source == null) return;  // pool exhausted — silently dropped, per GDD Edge Cases

        float stingerRadius = IsikVolumeDurumSistemi.Instance.GetStingerAudioRadius(shiftId);
        source.transform.position = zoneCenter;
        source.spatialBlend = 1f;
        source.minDistance = stingerRadius * 0.3f;
        source.maxDistance = stingerRadius * 1.0f;
        source.PlayOneShot(/* stinger clip */);
        _playingStingerSources.Add(source);
        // Corrected during unity-specialist validation (2026-08-07): the
        // original sketch used Invoke(nameof(EnterCooldownFor), clip.length)
        // — but MonoBehaviour.Invoke has no way to carry the per-source
        // AudioSource reference EnterCooldownFor needs (no parameterized
        // overload exists). A Coroutine, already this project's established
        // idiom for exactly this "wait N seconds then flip a state" shape
        // (Işık/Volume's per-zone ticker, ADR-0005; Seviye/Sahne Geçişi's
        // DelayedUnload, ADR-0008), closes over both `source` and `shiftId`
        // naturally instead.
        StartCoroutine(EnterCooldownAfter(shiftId, source, /* clip.length */ 1.2f));

        AdaptifSesSistemi.InternalInstance.MarkPlayed(shiftId);  // written on attempt, not on completion
    }

    private IEnumerator EnterCooldownAfter(string shiftId, AudioSource source, float clipLength) {
        yield return new WaitForSeconds(clipLength);
        // Pool resource freed immediately — available to ANY shiftId from
        // this point on, per GDD Edge Cases ("O boşa çıkan kaynak hemen
        // uygun sayılır"). This is deliberately NOT gated on the shiftId
        // cooldown below — the two trackers are independent by design.
        _playingStingerSources.Remove(source);

        _shiftIdsInCooldown.Add(shiftId);
        yield return new WaitForSeconds(stingerCooldownSeconds);  // Tuning Knob, 0.5-2s
        _shiftIdsInCooldown.Remove(shiftId);
    }

    private void OnTransitionStateChanged(TransitionState newState, TransitionType type) {
        if (newState != TransitionState.Swapping || type != TransitionType.Hard) return;
        bool abrupt = SceneTransitionManager.Instance.GetCurrentHardCutAbrupt();
        if (abrupt) {
            // Instant-mute rule — order matters (GDD Edge Cases): mute first,
            // THEN play CutSting, so the safety-net mute doesn't silence its
            // own trigger frame.
            foreach (var zone in _activeZoneVolumes) zone.StopImmediate();
            // "Tüm stinger pool elemanları Idle'a sıfırlanır" (GDD Edge
            // Cases) — both trackers reset, not just playback: stopping a
            // source mid-clip skips its own EnterCooldownAfter coroutine's
            // remaining yields, so the cooldown/availability state must be
            // cleared explicitly here rather than left to that coroutine.
            foreach (var source in _playingStingerSources) source.Stop();
            _playingStingerSources.Clear();
            _shiftIdsInCooldown.Clear();
            _cutStingSource.PlayOneShot(/* cut-sting clip */);
        }
        // Abrupt == false: no mute here at all — ambient/stinger sources
        // continue into their existing ambient_crossfade-to-silence path
        // (AmbientZoneVolume's own crossfade, retargeted to "silence" —
        // see AmbientZoneVolume below), CutSting never plays.
    }
}
```

### `AmbientZoneVolume` (scene-local, one per area) and round-based tension

```csharp
public sealed class AmbientZoneVolume : MonoBehaviour {
    [SerializeField] private string _zoneId;
    [SerializeField] private AudioSource _sourceA, _sourceB;   // ping-pong pair, Ambiance group
    [SerializeField] private AudioSource _tensionLayer;        // 3rd, round-gated layer, Ambiance group
    private bool _initialCheckDone;

    // Corrected during unity-specialist validation (2026-08-07): the
    // original sketch used FindObjectOfType<AdaptifSesController>() here —
    // Object.FindObjectOfType has been [Obsolete] since Unity 2023.1
    // (FindFirstObjectByType/FindAnyObjectByType are the replacements),
    // and it's unnecessary regardless: AdaptifSesController.Instance is
    // already a public static property (see the duplicate-instance guard
    // above), reliably non-null by the time any level-scene
    // AmbientZoneVolume.OnEnable() runs (the persistent "Foundation"
    // scene loads before any level scene, same ordering reasoning this
    // ADR already relies on for Start()-time subscriptions).
    private void OnEnable() => AdaptifSesController.Instance?.RegisterZoneVolume(this);
    private void OnDisable() => AdaptifSesController.Instance?.DeregisterZoneVolume(this);

    private void Update() {
        // Co-residency guard, same class of fix ADR-0005 already applied to
        // Işık/Volume's own zones (architecture.md Data Flow §4's own
        // cross-cutting note names this exact pattern) — pass, don't
        // process, unless this GameObject's scene is the active one.
        if (gameObject.scene != SceneManager.GetActiveScene()) return;

        // Deferred first-tick overlap check — GDD's own fix for the
        // Start()-vs-co-residency-guard conflict: a plain Start()-time
        // check would run while the origin scene is still active during
        // SOFT co-residency, firing on the wrong frame or not at all.
        if (!_initialCheckDone) {
            _initialCheckDone = true;
            if (PlayerIsInsideThisZone()) FireZoneChanged(_zoneId);
        }

        // ambient_crossfade tick (A/B lerp) + tension_gain tick (round-based
        // 3rd-layer volume, reading Gece/Oturum Durumu's CurrentRoundIndex/
        // TotalRoundCount — intra-Foundation read, ADR-0006) happen here,
        // per-frame, whenever a crossfade or tension update is in flight.
    }

    private void OnTriggerEnter(Collider other) {
        if (gameObject.scene != SceneManager.GetActiveScene()) return;  // same guard
        if (/* other is FPC */) FireZoneChanged(_zoneId);
    }

    public void StopImmediate() {
        _sourceA.Stop(); _sourceB.Stop(); _tensionLayer.Stop();
    }
}
```

`TensionGain(roundIndex) = ease(clamp(roundIndex / (TotalRoundCount - 1), 0, 1))` (GDD Formulas) is read directly from `GeceOturumDurumu.Instance.CurrentRoundIndex`/`TotalRoundCount` every tick this layer updates — no event, matching the GDD's own "ayrı bir event gerekmez, her ambiyans güncellemesinde doğrudan okunur" note. The `TotalRoundCount ≤ 1` guard (GDD Formulas, "Guard rail") — `TensionGain` pinned to `1` rather than dividing by zero — is implemented at the read site, not deferred.

### Addendum (2026-08-09): stinger caption UI ownership (closes review Finding T3)

Written as a follow-up to `/architecture-review` 2026-08-09, which found the stinger closed-caption contract **ownerless**: ADR-0002 deferred the concrete `#stinger-caption` contract to this ADR, but this ADR's original draft left the caption out of scope entirely (per ADR-0010's note, "deferred to a future dialogue/UI pass") — leaving `adaptif-ses-sistemi.md`'s caption requirement (a non-diegetic closed caption synced to the stinger clip's window, shown unconditionally, visually distinct from dialogue subtitles) mapped to no ADR at all. **Ownership is now fixed here**:

- **Owner**: `AdaptifSesController` — the same component that decides stinger playback owns showing/hiding the caption, so the sync-to-playback requirement is trivially satisfiable (both happen at the same call site).
- **Mechanism**: `#stinger-caption` sub-tree under the shared `UIDocument` (ADR-0002), reached via `UIRoot.Instance.Root.Q<VisualElement>("stinger-caption")` (ADR-0010's established accessor), queried once in `OnEnable()` with a defensive null-check, `ses-*` USS class-name prefix — all three per ADR-0002's shared-UXML mitigations, matching ADR-0010/ADR-0012's precedent exactly.
- **Timing contract**: shown in the same call that issues the stinger's `PlayOneShot` (immediately after, inside `OnShiftStateChanged`'s attempt branch), hidden after the clip's length via the already-existing `EnterCooldownAfter` coroutine (one extra hide call at its first `yield` boundary — no new coroutine). Display is **unconditional** — no settings gate exists in MVP (GDD Core Rule: "koşulsuz gösterim").
- **Explicitly still deferred, now to a *named* owner**: the caption's text content (object-naming vs. impressionistic — GDD Open Questions #2) and its visual style tokens (font/color/distinction from dialogue subtitles) belong to `design/ux/accessibility-requirements.md`, to be written by `/ux-design` — the GDD's own AC 14b already routes them there. This addendum owns the *mechanism and timing*; the UX pass owns the *content and style*. That split is the difference between "deferred" and "ownerless."

## Alternatives Considered

### Alternative 1: Static facade subscribes and orchestrates playback itself (ADR-0001's original, uncorrected assumption)
- **Description**: Keep `AdaptifSesSistemi`'s static facade subscribing to `Işık/Volume`'s `OnShiftStateChanged` in its own constructor (as ADR-0001's current comment assumes), and have its handler reach into a separately-registered `MonoBehaviour` (via some static accessor) to actually trigger playback.
- **Pros**: Matches ADR-0001's current text without needing to edit it; keeps a single, familiar `FoundationBootstrap.ResetAll()`-time subscription point, consistent with every other Foundation service's shape.
- **Cons**: Contradicts ADR-0001's own "Note on scope" — the state/event slice this pattern covers was already scoped to `HeldSessionAlreadyPlayed` alone, not the stinger-triggering decision logic (which requires reaching into pooled `AudioSource`s, mixer routing, and radius/position math — none of which is "state"). Splitting the *subscription* from the *decision logic* it exists to drive (subscribe in the static class, decide-and-play in the MonoBehaviour) also means passing the entire `OnShiftStateChanged` payload across that boundary for no benefit — the MonoBehaviour would need the same information either way, so nothing is saved by NOT letting it subscribe directly.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-06): the static-facade-subscribes design was never actually decided by an ADR — it was an unexamined assumption baked into ADR-0001's ResetAll() comment before this ADR existed to check it against the GDD's own framing. Once checked, it doesn't hold up; correcting it now is cheaper than propagating a wrong assumption into implementation.

### Alternative 2: A fourth persistent scene ("Audio") instead of reusing ADR-0008's "Foundation" scene
- **Description**: Give `AdaptifSesController` its own dedicated persistent scene, parallel to "UI" (ADR-0002), "Player" (ADR-0003), and "Foundation" (ADR-0008).
- **Pros**: Keeps each persistent scene single-purpose; a developer inspecting the "Foundation" scene wouldn't be surprised by an unrelated audio controller living there.
- **Cons**: A fourth scene loaded additively at boot for one more `MonoBehaviour` with no meaningful isolation benefit — "Foundation" already exists specifically to host cross-cutting Foundation-layer `MonoBehaviour`s that don't fit the plain-static-service pattern (`SceneTransitionManager` is the first, `AdaptifSesController` is the second of exactly that kind), so a second host scene for the same *category* of exception adds boot-sequence surface (one more additive load, one more thing to get load-order-right) for a purely cosmetic organizational win.
- **Rejection Reason**: No functional benefit identified over reuse; "Foundation" scene's own name and purpose (per ADR-0008) already describes exactly this category of system.

### Alternative 3: Dynamic ducking (sidechain/snapshot) for stinger-over-ambient contrast, instead of static gain-staging
- **Description**: Use `AudioMixerSnapshot.TransitionTo` (documented in `docs/engine-reference/unity/modules/audio.md`'s own "Duck Music During Dialogue" example) to duck the Ambiance group whenever a stinger or CutSting plays, rather than relying on timbre contrast + a static brickwall limiter alone.
- **Pros**: A dynamic duck would guarantee audibility even in acoustically dense ambient moments, more robust than trusting content-authored contrast alone.
- **Cons**: `adaptif-ses-sistemi.md` Core Rules explicitly and repeatedly reject this — "Stinger asla ambiyansın üzerine RMS olarak sıçramaz... build-up/riser/crescendo yok," and the Formulas section states outright "Adding a time-based duck envelope would require its own attack/hold/release curve — that's exactly the 'build-up/riser/crescendo' the Core Rules explicitly reject." A duck-and-restore envelope is a shape of intensity ramp, which is precisely what Pillar 2 (Sessiz Gerilim, Şok Değil) forbids for this system.
- **Rejection Reason**: Already decided at the GDD level, repeatedly and explicitly; this ADR implements that decision rather than re-litigating it.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #9 — the **last** Foundation "must have before coding" ADR. All 9 Foundation-tier ADRs are now written.
- Fixes a real, load-bearing inaccuracy in ADR-0001 (the static-facade-subscribes assumption) before any code was written against it — cheaper to correct now than after an implementation story assumed it.
- Reuses ADR-0008's "Foundation" persistent scene rather than introducing a fourth, keeping the "persistent scene for non-static-service Foundation systems" category to one scene, one clear purpose.
- The `AmbientZoneVolume` registry (`RegisterZoneVolume`/`DeregisterZoneVolume`) reuses an already-established, already-validated idiom (`InteractableRegistry`'s `OnEnable`/`OnDisable` registration shape, ADR-0004) rather than inventing a new cross-system communication mechanism for the HARD CUT instant-mute requirement.

### Negative
- `AdaptifSesController` is a second `MonoBehaviour` living in the "Foundation" scene alongside `SceneTransitionManager` — that scene is no longer single-system, though still single-*category* (non-static-service Foundation exceptions). A future reader must check both this ADR and ADR-0008 to understand everything that scene contains.
- The `AmbientZoneVolume` registry duplicates (in a smaller, single-purpose way) some of the bookkeeping shape `InteractableRegistry` already provides project-wide — a deliberate, scoped duplication rather than routing ambient-zone lifecycle through the general-purpose interactable registry, which would conflate two unrelated concerns (interactable objects vs. ambient audio triggers) for a superficial code-reuse win.
- Editing ADR-0001's `AdaptifSesSistemi` comment (not its ordering) is a smaller, safer change than the ADR-0006/ADR-0008 edits to the same file, but it's still a fourth distinct correction to that document from a later ADR — worth noting as an accumulating pattern (ADR-0001 has now been corrected by ADR-0003, ADR-0004, ADR-0006, ADR-0007, ADR-0008, and this ADR) rather than a one-off.

### Risks
- **Risk (unity-specialist validation, 2026-08-07 — fixed, not just noted)**: the original draft had `AmbientZoneVolume.OnEnable()`/`OnDisable()` locate `AdaptifSesController` via `FindObjectOfType<AdaptifSesController>()` — this API has been `[Obsolete]` since Unity 2023.1 (predates this project's own May-2025 knowledge cutoff, so a current, not-a-knowledge-gap deprecation), and was unnecessary regardless since `AdaptifSesController.Instance` was already available as a public static property in the same draft. **Fixed**: both call sites now use `AdaptifSesController.Instance?.Register/DeregisterZoneVolume(this)` directly (Decision, above) — no scene search, no deprecated API, no risk to mitigate.
- **Risk**: `EnterCooldownAfter`'s two `WaitForSeconds` calls (Playing→Cooldown, then Cooldown→Idle) are paused by `Time.timeScale == 0`, same class of risk ADR-0008 already flagged for its own delayed-unload `Coroutine`. **Mitigation**: not a concern for MVP (no pause system in any current GDD); same future note as ADR-0008's equivalent risk — `WaitForSecondsRealtime` if a pause system is ever added.
- **Risk**: `AdaptifSesController.Start()` subscribes to both `IsikVolumeDurumSistemi.Instance` and `SceneTransitionManager.Instance` — if `AdaptifSesController`'s own persistent-scene `GameObject` were ever duplicated (two "Foundation" scene loads), a second `AdaptifSesController` would double-subscribe, causing every stinger/CutSting/mute event to fire twice. **Mitigation**: the same duplicate-instance guard `SceneTransitionManager`/`PlayerStateProvider` already established (ADR-0003/ADR-0008) — included directly in this ADR's own `Awake()` sketch (Decision, above), not deferred to a later fix.
- **Risk (TD-ADR review, 2026-08-07 — verified safe, documented here per the reviewer's own note that this reasoning was missing)**: during an `Abrupt==true` HARD CUT, `_activeZoneVolumes` could in principle contain `AmbientZoneVolume` instances from a scene mid-teardown (ADR-0008's `DelayedUnload` defers `UnloadSceneAsync` 0.5-2s after `Complete`, so a prior transition's origin scene can still be resident, and back-to-back transitions can genuinely overlap that window). Traced and confirmed safe: `AmbientZoneVolume.OnDisable()` deregisters from `_activeZoneVolumes` before Unity actually destroys the object (Unity's own lifecycle ordering guarantee), and this loop is a single synchronous `foreach` that never interleaves with `UnloadSceneAsync`'s own destroy pass. Worst case is a harmless redundant `Stop()` on an already-irrelevant zone from an older transition, never a `MissingReferenceException` on a destroyed one. **Mitigation**: none needed — documented here so a future reader doesn't have to re-derive this reasoning from scratch.
- **Risk (unity-specialist validation, 2026-08-07)**: `AdaptifSesController.Instance` is set exclusively in `Awake()`, with no separate reset hook — structurally identical to `PlayerStateProvider.Current` (ADR-0003) and `SceneTransitionManager._instance` (ADR-0008), both of which already document and mitigate the risk that Unity's independent "Reload Scene" Enter Play Mode Setting can suppress `Awake()` re-execution on a surviving persistent-scene object across a Play-mode Stop→Play boundary, leaving `Instance` stale (possibly pointing at a destroyed object). An earlier draft of this ADR's Risks section covered only the *duplicate-instance* hazard and omitted this *independent* one — the same omission ADR-0008 itself named and fixed once already for the identical shape, now caught here before this draft moved to Accepted rather than after. **Mitigation**: same as ADR-0003/ADR-0008 — a correctness risk only under the non-default "Reload Scene: Off" Editor setting (real player builds always fully reload); no code change needed, but a `[UnityTest]` with Reload Scene disabled should confirm `AdaptifSesController.Instance` is never stale across two simulated sessions (Validation Criteria below).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `adaptif-ses-sistemi.md` | 4 mixer groups (Ambiance/Stinger/CutSting/SFX), Stinger routed independently of Ambiance for contrast, static brickwall limiter on Stinger/CutSting only | `AdaptifSesController`'s serialized `AudioMixerGroup` fields; limiter is a one-time mixer-asset insert, not runtime code |
| `adaptif-ses-sistemi.md` | Stinger dual-trigger (`Shifting-In`+Persistent early path, `Held`+Persistent normal path), shared `HeldSessionAlreadyPlayed` guard, guard cleared on `Shifting-Out`/`Dormant` | `OnShiftStateChanged` handler's `isAttemptFrame` check + guard check, exactly as Core Rules specify |
| `adaptif-ses-sistemi.md` | HARD CUT sting gated on `type==Hard && Abrupt==true`; instant-mute (Abrupt) vs. crossfade-to-silence (not Abrupt); mute-then-CutSting ordering | `OnTransitionStateChanged` handler, explicit ordering comment matching Edge Cases |
| `adaptif-ses-sistemi.md` | `AmbientZoneVolume` co-residency guard (same class as Işık/Volume's own fix) and deferred first-tick overlap check | `AmbientZoneVolume.Update()`'s `gameObject.scene != SceneManager.GetActiveScene()` guard + `_initialCheckDone` deferred check |
| `adaptif-ses-sistemi.md` | Round-based 3rd ambient layer, `tension_gain` formula, `TotalRoundCount≤1` guard rail | `AmbientZoneVolume`'s tension-layer tick, reading `Gece/Oturum Durumu`'s round counters directly (ADR-0006) |
| `adaptif-ses-sistemi.md` | `PlayFootstep(float speed)` sole entry point, no independent `Velocity` subscription | `AdaptifSesController.PlayFootstep(float)`, called externally by FPC only |
| `adaptif-ses-sistemi.md` / `gorev-tasima-dongusu.md` | `Görev/Taşıma Döngüsü`'s pickup/delivery/jostle SFX routes through a dedicated "SFX" mixer group | `AdaptifSesController.SfxGroup` read-only property |
| `architecture.md` | Module Ownership row (line 87) — `AmbientZoneVolume`, 4 mixer groups, stinger pool, `HeldSessionAlreadyPlayed` | Implemented as designed; `HeldSessionAlreadyPlayed`'s owner corrected from "the static facade subscribes" to "the static facade is pure state, `AdaptifSesController` subscribes" (see Decision) |

## Performance Implications
- **CPU**: Negligible — `AmbientZoneVolume.Update()` runs a cheap scene-identity comparison every frame per zone (3 zones total, MVP scope), plus a float lerp only while an actual crossfade/tension update is in flight; stinger/CutSting/footstep are all event-driven `PlayOneShot` calls, not per-frame work.
- **Memory**: Negligible — 3-4 pooled stinger `AudioSource`s + 1 CutSting + 1 footstep + 2×3 ambient ping-pong sources (per-area, only the active area's pair is meaningfully active) — well within any reasonable audio-source budget for a small-scope indoor game.
- **Load Time**: Audio clip import settings (Decompress On Load for short stinger/footstep clips, Compressed In Memory or Streaming for longer ambient loops) are a content-authoring concern per `docs/engine-reference/unity/modules/audio.md`'s own guidance, not an architecture decision this ADR needs to make further.
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Adaptif Ses Sistemi` is not yet implemented).

## Validation Criteria
- A stinger for a `Persistent=true` shift plays exactly once per session regardless of how many times `OnShiftStateChanged` re-fires for it (`Shifting-In` early attempt, `Held` normal attempt, any reload-triggered re-fire) — `HeldSessionAlreadyPlayed` guard verified via a `[Test]` constructing a fresh `AdaptifSesState` directly (ADR-0001 testability pattern), not through the static facade.
- A reversible (`Persistent=false`) shift's stinger — specifically the mandatory `Automatic` ambient zone — never plays, under any `OnShiftStateChanged` sequence (GDD's `IsShiftPersistent` gate on both `Shifting-In` and `Held`).
- `AmbientZoneVolume`'s co-residency guard: during a simulated SOFT-transition co-residency window (two scenes loaded, only one active), only the active scene's zone volume fires `ZoneChanged`/processes its tick — a `[UnityTest]` matching the shape ADR-0005 already established for Işık/Volume's own equivalent guard.
- An `Abrupt==true` HARD CUT stops every registered `AmbientZoneVolume` and every `Playing` pooled stinger source before `CutSting`'s `PlayOneShot` fires (ordering-sensitive — asserted via a mock/spy on the mute calls preceding the `PlayOneShot` call, not just "both happened").
- An `Abrupt==false` HARD CUT plays no `CutSting` and does not instant-mute — ambient/stinger sources continue into the existing `ambient_crossfade`-to-silence path.
- `AdaptifSesController` has the same duplicate-instance guard as `SceneTransitionManager`/`PlayerStateProvider` (`Debug.LogError` + `Destroy(gameObject)`, unconditional) — already in the Decision code sketch's `Awake()`; a `[UnityTest]` should confirm a second instantiated `AdaptifSesController` self-destroys and never double-subscribes.
- `TensionGain(roundIndex)` with `TotalRoundCount ≤ 1` returns `1`, not a division-by-zero exception or `NaN` (GDD Formulas guard rail).
- A `[UnityTest]` with Unity's "Reload Scene" Enter Play Mode Setting disabled runs two successive simulated sessions and confirms `AdaptifSesController.Instance` is never a stale/destroyed reference in the second session — same shape as ADR-0003's `PlayerStateProvider` test and ADR-0008's `SceneTransitionManager` test (unity-specialist validation, 2026-08-07).
- **(TD-ADR review, 2026-08-07)** A pooled stinger source that just finished playing for `shiftId` A is immediately reusable by a different `shiftId` B's attempt, even while A is still within its own `_shiftIdsInCooldown` window — a `[Test]` (or `[UnityTest]`) must assert this directly, since it's the exact scenario the original per-`AudioSource` cooldown design would have gotten wrong (Decision, above). Conversely, a repeat attempt for `shiftId` A itself during that same window is ignored (no-op), matching GDD Edge Cases "Cooldown re-entry."

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — corrected by this ADR: `AdaptifSesSistemi`'s static facade does not subscribe to anything; its `FoundationBootstrap.ResetAll()` comment is updated to reflect this.
- ADR-0004 (InteractableRegistry Foundation Ownership) — precedent for the `AmbientZoneVolume` registration shape (`OnEnable`/`OnDisable` self-registration into a small list).
- ADR-0005 (Işık/Volume Rendering Architecture) — source of `OnShiftStateChanged`/`IsShiftPersistent`/`GetStingerAudioRadius`; precedent for the co-residency guard `AmbientZoneVolume` reuses.
- ADR-0006 (Session State Service and Round-Counter Ownership) — source of `CurrentRoundIndex`/`TotalRoundCount` for the round-based tension layer.
- ADR-0008 (Scene Transition State Machine) — source of `OnTransitionStateChanged`/`GetCurrentHardCutAbrupt()`; the persistent "Foundation" scene this ADR's `AdaptifSesController` reuses; the forbidden pattern this ADR's `Start()`-time subscription timing satisfies.
- This closes `architecture.md`'s Foundation-tier Required ADRs (#1-#9, all now written). The remaining 6 Required ADRs are Core/Feature-tier: Interaction State Machine (#10), Elevator State Machine (#11), Dialogue Callback Selection Timing (#12), Carry Loop and Round State (#13), Memory Trigger Orchestration (#14), End-Condition Orchestration (#15).
