# ADR-0006: Adaptive Audio System — Ambience, Stinger Timing & Foundation Composition Root

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Audio |
| **Knowledge Risk** | LOW — `AudioSource`/`AudioMixer`/`PlayOneShot` all confirmed stable and unchanged post-2022-LTS; Unity specialist review confirmed the app-level `Idle/Playing/Cooldown` state machine over `PlayOneShot` is still necessary (`AudioSource.isPlaying` still doesn't cleanly map to "is my logical stinger busy" in 6.3) and confirmed mixer-group effect isolation (a Limiter on one sibling group doesn't leak to another) works as expected |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/audio.md`, `breaking-changes.md`, `deprecated-apis.md`, `docs/architecture/adr-0001-scene-transition-manager.md`, `docs/architecture/adr-0005-lighting-volume-state.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Scene Transition — consumes `OnTransitionStateChanged`/`GetCurrentHardCutAbrupt` via a new narrow `ISceneTransitionQuery` interface, see Decision and the accompanying ADR-0001 amendment), ADR-0005 (Lighting/Volume — third independent consumer of `ILightingVolumeQuery`, via the established injectable-interface-plus-null-object pattern) |
| **Enables** | Future ADR for Görev/Taşıma Döngüsü (Carry Loop) — this ADR owns and defines the "SFX" mixer group topology that system will route its pickup/delivery/jostle sounds into |
| **Blocks** | Carry Loop stories that need SFX routing, until this ADR reaches `Accepted` |
| **Ordering Note** | Foundation layer, Batch 1 priority #6 — the **last** Foundation-layer ADR. Amends ADR-0001 (adds `ISceneTransitionQuery`) and ADR-0005 (redirects its bootstrap to the new `FoundationCompositionRoot` this ADR introduces, ending the pattern of hand-patching ADR-0005 once per new consumer). |

## Context

### Problem Statement
The game's audio needs (diegetic ambience per area, a timing-critical memory-trigger stinger synchronized with the light system, a HARD CUT safety-net sting, and speed-scaled footsteps) must all be implemented on Unity's built-in AudioMixer/AudioSource (FMOD/Wwise explicitly rejected in the GDD — no adaptive/branching-music needs justify the licensing/integration cost). Architecturally, this is also the point where a real problem in this project's own accumulating pattern must be fixed: this is the **third** system needing `ILightingVolumeQuery`, and each prior consumer required a hand-edit to ADR-0005's bootstrap method to add one more property assignment — a pattern that doesn't scale to Batch 2/3's remaining systems.

### Constraints
- No adaptive/branching music, no RTPC networks — Unity built-in AudioMixer + AudioSource only (already resolved in the GDD, not reopened here)
- Stinger must fire in sync with the light system's ramp start (not its completion) for `Persistent` shifts — a `~2-5s` gap between light and sound was the single most severe finding across this GDD's own multi-round review history
- Exactly 2 pooled `AudioSource`s for ambience crossfade regardless of how many zones are visited in sequence
- Stinger mixer group needs isolated dynamics processing (a static brickwall limiter) that must not affect the Ambiance group
- `HeldSessionAlreadyPlayed` must guarantee "heard at most once per session" across two possible trigger paths (`Shifting-In`-early and `Held`-normal) that share one guard

### Requirements
See `docs/architecture/tr-registry.yaml`: TR-audio-001 through TR-audio-038 (extracted from `design/gdd/adaptif-ses-sistemi.md`), plus consumer-side requirements TR-scene-038, TR-lighting-027/029, TR-fpc-028/029.

## Decision

### Per-zone ambience trigger
`AmbientZoneVolume : MonoBehaviour` — one per named zone (Depo, Servis Koridoru, Balo Salonu), a simple `Collider(isTrigger=true)`, fires a local `ZoneChanged(zoneId)` call into `AdaptiveAudioState` on FPC entry. **No central shiftId-keyed registry is needed here** (unlike Lighting/Volume's zones) — ambience zones aren't addressed by ID from outside this system, only entered/exited by the player. Reuses ADR-0005's co-residency-guard pattern **verbatim, by direct precedent, not re-derivation**: the trigger is skipped unless its own scene matches `SceneManager.GetActiveScene()`, and a deferred one-time initial-overlap check (folded into the existing per-frame scene-match comparison via an `_initialCheckDone` flag) handles both the "player spawns inside a trigger" gap (Unity's `OnTriggerEnter` doesn't fire for that) and the race between that check and the co-residency guard — this exact conflict-then-resolution sequence is already documented in the GDD's own revision history, and this ADR does not reopen it.

### Central static state
```csharp
public static class AdaptiveAudioState {
    public static void ZoneChanged(string zoneId);        // called by AmbientZoneVolume
    public static void PlayFootstep(float speed);          // called by FPC's stride-phase accumulator

    public static ILightingVolumeQuery LightingQuery { get; set; } = new NullLightingVolumeQuery();
    public static ISceneTransitionQuery TransitionSource { get; set; } = new NullSceneTransitionQuery();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForNewSession() { /* clears HeldSessionAlreadyPlayed, stops and releases all pooled AudioSources — see Consequences */ }

    private static readonly HashSet<string> _heldSessionAlreadyPlayed = new();
}
```
- **Ambience crossfade**: two pooled `AudioSource`s (A/B), `ambient_crossfade` formula (`x²(3-2x)` smoothstep, same convention as Lighting's `ShiftProgress`), resumes from current gain on re-trigger (never resets to `t=0`), generalizes to any number of sequential zone visits via a "reassign the currently-quieter source to the new target" rule — confirmed by Unity specialist review as a standard, low-risk `AudioSource.clip` reassignment mid-fade (any transient click is masked by the smoothstep curve itself)
- **Stinger pool**: 3-4 `AudioSource`s, application-level `Idle`/`Playing`/`Cooldown` state machine (confirmed necessary by Unity specialist review — `PlayOneShot` doesn't reflect "busy" via `isPlaying`), `Invoke(EnterCooldown, clip.length)` schedules the Cooldown transition (Unity specialist's minor note: prefer a coroutine/explicit timer over `Invoke`'s string-based dispatch if this pattern needs to be more directly unit-testable later — not blocking now)
- **Memory-trigger stinger timing**: play-attempt fires on `Held` (always) **and** on `Shifting-In` when `LightingQuery.IsShiftPersistent(shiftId)` returns true — both paths gated by the same `HeldSessionAlreadyPlayed` check before any pool resource is taken, guaranteeing exactly one audible play per session regardless of which path fires first or whether a scene-reload re-fire occurs
- **HARD CUT "CutSting"**: single dedicated `AudioSource` on its own mixer group, independent of the stinger pool, fires when `TransitionSource.OnTransitionStateChanged` reports `Swapping && Hard` **and** `TransitionSource.GetCurrentHardCutAbrupt() == true`; exempt from the same-frame abrupt-stop-all rule (execution order: stop-all first, then CutSting's `PlayOneShot`)
- **Footsteps**: single dedicated `AudioSource`, `PlayFootstep(float speed)` called directly by the FPC's own stride-phase accumulator (this system never independently samples `Velocity` — confirmed no race condition possible by construction, per the GDD's own `footstep_volume` formula proof)

### Reusing `ILightingVolumeQuery` as a third consumer (confirmed with user)
`AdaptiveAudioState.LightingQuery` follows the exact same shape as `SessionState.LightingQuery`/`NarrativeState.LightingQuery` (ADR-0003/0004/0005) — for consistency and testability, not because a bootstrap-ordering race forces it (ADR-0005 already exists as a written ADR by the time this one is authored, unlike when ADR-0003/0004 were written). The stinger-timing handler subscribes via this property, identically shaped to the other two consumers.

### New: `ISceneTransitionQuery` (narrow interface, amends ADR-0001)
Rather than depending on the full `ISceneTransitionManager` (which exposes command methods — `PreloadHardCut`, `RequestSoftTransition`, `RequestHardCut` — this system never calls), `AdaptiveAudioState.TransitionSource` depends on a new, narrower interface containing only what a read-only consumer needs:
```csharp
public interface ISceneTransitionQuery {
    event Action<TransitionState, TransitionType> OnTransitionStateChanged;
    bool GetCurrentHardCutAbrupt();
}
public sealed class NullSceneTransitionQuery : ISceneTransitionQuery {
    public event Action<TransitionState, TransitionType> OnTransitionStateChanged { add { } remove { } }
    public bool GetCurrentHardCutAbrupt() => false;
}
```
The concrete `SceneTransitionManager` (ADR-0001) implements both `ISceneTransitionManager` (its full command surface, for Elevator/Cutscene) and `ISceneTransitionQuery` (this narrower read-only slice, for Adaptive Audio) — no behavioral change to ADR-0001's existing decision, purely an additional, narrower interface on the same concrete type. This mirrors `ILightingVolumeQuery`'s own precedent: it was already scoped narrower than `LightingVolumeState`'s full `TriggerShift`/`RevertShift`/`IsShiftActive` surface, for the same reason (consumers that only read shouldn't depend on — or need a null-object stand-in for — methods they never call).

### `FoundationCompositionRoot` (new, TD-ADR finding — fixes the recurring per-consumer bootstrap-patching problem)
Rather than continuing to hand-edit ADR-0005's bootstrap method every time a new system needs `ILightingVolumeQuery` (this ADR is the third such edit in one session), all cross-static Foundation-layer wiring is consolidated into one named, registry-tracked composition root:
```csharp
public static class FoundationCompositionRoot {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Wire() {
        var lighting = new LightingVolumeQueryAdapter();
        SessionState.LightingQuery = lighting;
        NarrativeState.LightingQuery = lighting;
        AdaptiveAudioState.LightingQuery = lighting;

        var transition = new SceneTransitionQueryAdapter(); // wraps the concrete SceneTransitionManager
        AdaptiveAudioState.TransitionSource = transition;
    }
}
```
This class does not conceptually "belong" to Adaptive Audio alone — it is specified here because this ADR is where the recurring problem became undeniable (three `LightingQuery` assignments plus a new `TransitionSource` assignment, all needing one deterministic, ordered bootstrap). **ADR-0005 is amended** (see accompanying amendment) to note that its own bootstrap method is superseded by this composition root — `LightingVolumeQueryAdapter` itself remains ADR-0005's, only the *assignment* of it to consumer properties has moved. Future Batch 2/3 ADRs that need similar cross-static wiring add their assignment to `FoundationCompositionRoot.Wire()` and register themselves in the `foundation_composition_root` registry entry's `referenced_by` list, rather than hand-editing whichever ADR happened to define the interface first.

### Mixer topology (this ADR owns and defines)
Four groups under Master: **Ambiance** (the two crossfade sources — previously unrouted/implicit-Master in an earlier GDD draft, now explicit so the Stinger group's limiter can be isolated), **Stinger** (the pool, carries the static brickwall limiter), **CutSting** (independent single source, exempt from abrupt-stop-all), **SFX** (new — owned by this ADR, routed into by the future Carry Loop ADR's pickup/delivery/jostle sounds, no dynamic processing, explicitly no ducking per the project's "no build-up/riser/crescendo" mixing philosophy).

### Architecture Diagram
```
  [FPC: PlayFootstep(speed)]              [AmbientZoneVolume ×3: ZoneChanged(zoneId)]
              │                                          │
              ▼                                          ▼
        ┌──────────────────────────────────────────────────┐
        │                  AdaptiveAudioState                   │
        │  (static: ambience crossfade, stinger pool,            │
        │   HeldSessionAlreadyPlayed, footstep source)            │
        └──────────────────────────────────────────────────┘
              ▲                                          ▲
    LightingQuery (3rd consumer,              TransitionSource (new,
    same shape as Session/Narrative)          narrow ISceneTransitionQuery)
              │                                          │
              └──────────────┬───────────────────────────┘
                              ▼
                  ┌─────────────────────────┐
                  │   FoundationCompositionRoot   │
                  │  (ONE bootstrap, assigns ALL)  │
                  └─────────────────────────┘
                    │         │         │         │
              Session   Narrative  Adaptive-  (future
              State.    State.     Audio      Batch 2/3
              LightingQuery       .LightingQuery/  consumers
                                   TransitionSource  add here)
```

### Key Interfaces
See Central static state, `ISceneTransitionQuery`, and `FoundationCompositionRoot` above.

## Alternatives Considered

### Alternative 1: Direct reference to `SceneTransitionManager` instead of `ISceneTransitionQuery`
- **Description**: `AdaptiveAudioState` holds a direct static reference to the concrete `SceneTransitionManager` type, since ADR-0001 already exists as real code with no bootstrap-ordering race.
- **Pros**: Fewer types to define; matches the (initially proposed, user-confirmed-as-a-starting-assumption) reasoning that no ordering race exists here the way it did for Lighting/Volume when ADR-0003/0004 were written.
- **Cons**: Per TD-ADR review, "no bootstrap race" is a timing argument, not a testability or lifetime argument — a direct static reference to a scene-resident manager cannot be unit-tested headlessly and is de facto singleton coupling regardless of whether an ordering race currently exists. A future refactor of Scene Transition's implementation could break a direct reference in ways an interface wouldn't.
- **Rejection Reason**: TD-ADR review's revision applied — `ISceneTransitionQuery` + null object, matching `ILightingVolumeQuery`'s established shape, for consistency and testability rather than just race-avoidance.

### Alternative 2: Keep patching ADR-0005's bootstrap per new consumer (no composition root)
- **Description**: Add a fourth line to ADR-0005's existing bootstrap method for `AdaptiveAudioState.LightingQuery`, same as the pattern used for the first two consumers.
- **Pros**: Smaller diff, no new class.
- **Cons**: This is the third such edit in one session; the pattern doesn't scale to Batch 2/3's remaining systems, each of which may need similar wiring. Continuing to patch ADR-0005 also incorrectly implies Lighting/Volume "owns" wiring for systems that have nothing to do with lighting (this ADR's `TransitionSource` assignment has no lighting relationship at all).
- **Rejection Reason**: TD-ADR review's structural fix applied — a single named, registry-tracked composition root that any future ADR can extend without amending an unrelated earlier ADR.

## Consequences

### Positive
- Ends the recurring "amend an earlier ADR's bootstrap for every new consumer" pattern — future Batch 2/3 ADRs needing `ILightingVolumeQuery`, `ISceneTransitionQuery`, or similar cross-static wiring add one line to `FoundationCompositionRoot.Wire()` and register themselves in the registry, not amend ADR-0005 or ADR-0006 again
- `ISceneTransitionQuery`'s narrower surface (vs. the full `ISceneTransitionManager`) means `AdaptiveAudioState`'s tests only need to mock 2 members, not the full command/query surface — consistent with `ILightingVolumeQuery`'s already-established narrowing precedent
- Mixer group isolation (Ambiance unrouted-no-longer, Stinger's limiter correctly isolated per Unity specialist review) closes a real content-authoring risk the GDD's own history flagged (an earlier draft left Ambiance on implicit Master, which would have made isolating the Stinger limiter impossible)

### Negative
- `FoundationCompositionRoot` is now a class with no single clear "owner" ADR in the traditional sense — it's specified here (ADR-0006) but conceptually spans ADR-0001/0003/0004/0005 as well. This is a deliberate tradeoff (per TD-ADR review) to stop the worse alternative (repeatedly amending unrelated earlier ADRs), but means anyone reading ADR-0005's bootstrap section alone needs to know to look here for the actual current wiring
- Four independent static-field reset paths now exist across the Foundation layer (`SessionState`, `NarrativeState`, `AdaptiveAudioState`, plus `LightingVolumeState`'s own zone-registry-implicit reset via scene lifecycle) — per TD-ADR review, `AdaptiveAudioState.ResetForNewSession()` must not just clear `HeldSessionAlreadyPlayed`, it must also stop any currently-playing pooled `AudioSource`s and release references, since a mid-play stinger under a Domain-Reload-disabled Enter Play Mode configuration would otherwise leak a live audio voice across sessions — this is a real difference from the narrative/session statics, which hold no live engine resources

### Risks
- If a future Batch 2/3 ADR needs a cross-static dependency this pattern doesn't cleanly cover (e.g., a two-way dependency rather than one-directional query), `FoundationCompositionRoot` may need to grow beyond simple property assignment — flagged as a design question for whichever future ADR first needs it, not solved speculatively here
- `AdaptiveAudioState.ResetForNewSession()`'s live-`AudioSource`-cleanup requirement (Consequences → Negative) is a new responsibility this ADR's reset path carries that the narrative/session statics don't — if a future implementer copies the simpler "just clear the HashSet" pattern from `SessionState`/`NarrativeState` without accounting for live pooled resources, a subtle audio-leak bug results; called out explicitly here and in Validation Criteria to prevent that

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| adaptif-ses-sistemi.md | TR-audio-001/006/007: Unity built-in AudioMixer/AudioSource, ambience crossfade, dedicated Ambiance group | Decision → Central static state, Mixer topology |
| adaptif-ses-sistemi.md | TR-audio-008/010/011/012: AmbientZoneVolume, co-residency guard, deferred initial-overlap check | Decision → Per-zone ambience trigger |
| adaptif-ses-sistemi.md | TR-audio-013/014/017/018/022/023: stinger Shifting-In+Held timing, IsShiftPersistent/GetStingerAudioRadius queries, Idle/Playing/Cooldown state machine, HARD CUT sting filtering | Decision → Central static state |
| adaptif-ses-sistemi.md | TR-audio-019/026/027/028: RMS enforcement, abrupt-stop-all + CutSting exemption, SFX group | Decision → Mixer topology |
| adaptif-ses-sistemi.md | TR-audio-029: HeldSessionAlreadyPlayed session-lifetime guard | Decision → Central static state |
| adaptif-ses-sistemi.md | TR-audio-020/021/038: PlayFootstep(speed) called directly by FPC, no independent Velocity sampling | Decision → Central static state |
| seviye-sahne-gecisi.md (ADR-0001) | TR-scene-038: HARD CUT sting subscribes to OnTransitionStateChanged, filters type==Hard | Decision → ISceneTransitionQuery (new, amends ADR-0001) |
| isik-volume-durum-sistemi.md (ADR-0005) | TR-lighting-027/029: OnShiftStateChanged subscription, GetStingerAudioRadius | Decision → Reusing ILightingVolumeQuery as a third consumer |

## Performance Implications
- **CPU**: negligible — bounded pool sizes (2 ambience + 3-4 stinger + 1 CutSting + 1 footstep = 7-8 total `AudioSource`s), no per-frame allocations
- **Memory**: negligible at MVP content scale (~3-8 pickup/delivery events per round, per the GDD's own estimate for the future SFX consumer)
- **Load Time**: N/A
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system.

## Validation Criteria
- All Acceptance Criteria from `adaptif-ses-sistemi.md`, implemented as automated EditMode/PlayMode tests per this project's Logic-tier test-evidence rules
- New test (Unity specialist finding): footstep `pitch` set immediately before `PlayOneShot` does not retroactively affect already-overlapping playing instances
- New test (TD-ADR finding): `AdaptiveAudioState.ResetForNewSession()` with a stinger mid-`Playing` — assert the `AudioSource` is stopped and the pool slot returns to `Idle`, not left in a leaked `Playing` state across the reset
- New test: `FoundationCompositionRoot.Wire()` assigns non-null, non-default instances to all four properties (`SessionState.LightingQuery`, `NarrativeState.LightingQuery`, `AdaptiveAudioState.LightingQuery`, `AdaptiveAudioState.TransitionSource`) in one deterministic call
- New test: `AdaptiveAudioState` with `TransitionSource` left at `NullSceneTransitionQuery` default — CutSting never fires, no exceptions

## Related Decisions
- Amends ADR-0001 (adds `ISceneTransitionQuery`, a narrow read-only interface implemented by the same concrete `SceneTransitionManager`)
- Amends ADR-0005 (redirects its bootstrap method to `FoundationCompositionRoot`, ends the per-consumer bootstrap-patching pattern)
- Reuses ADR-0005's co-residency-guard pattern for `AmbientZoneVolume`, by direct precedent
- Enables the future Görev/Taşıma Döngüsü ADR (SFX mixer group routing)
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline — **this ADR completes Batch 1 (Foundation layer)**
