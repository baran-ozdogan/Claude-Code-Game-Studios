# ADR-0005: Lighting/Volume State — Per-Zone Volume Control & Central shiftId Registry

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Rendering (URP Volume system) |
| **Knowledge Risk** | MEDIUM — the core Volume-weight mechanism is empirically spike-validated (`prototypes/yankilar-volume-weight-spike/`, Corridor C), and Unity specialist review confirmed the *why* behind that result (see Decision → Volume weight control), closing the GDD's own "internal mechanism not determined" caveat. Remaining risk is scoped entirely to ADR-0001's already-flagged, still-open RenderGraph multi-scene camera-stacking spike — this ADR does not introduce new rendering-pipeline risk, only more Volumes for that existing spike to test against. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/rendering.md`, `breaking-changes.md`, `deprecated-apis.md`, `docs/architecture/adr-0001-scene-transition-manager.md` (Volume Layer Mask decision), `prototypes/yankilar-volume-weight-spike/` (empirical spike results referenced throughout the GDD) |
| **Post-Cutoff APIs Used** | None beyond what ADR-0001 already documents (`Volume`, `VolumeProfile`, `TryGet<T>` — all confirmed unchanged pre/post-Unity-6) |
| **Verification Required** | None new. This ADR adds one test case to ADR-0001's existing RenderGraph spike (see ADR Dependencies): confirm two co-resident scenes' zone `Volume`s do not double-blend during the SOFT-transition co-residency window. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Volume Layer Mask per-scene isolation — this ADR's zones must be placed on scene-specific Layers to satisfy it). **New, made explicit by this review**: ADR-0003 (Session State) — this system reads `SessionState.PersistentShiftIds` at zone initialization for the Persistent-restore-on-scene-load Edge Case (previously only a one-directional GDD dependency; this ADR's own Decision now specifies exactly how, making it a concrete `Depends On` rather than a conceptual one). |
| **Enables** | This ADR is the **producer** ADR that ADR-0003 and ADR-0004 have both been waiting on — it implements `ILightingVolumeQuery`, closing the "silently inert until producer assigns" risk both flagged. Also enables future ADRs for Anı-Tetikleyici Etkileşim (Memory-Trigger — calls `TriggerShift`/`RevertShift`/`IsShiftActive`), Adaptif Ses Sistemi (Audio — subscribes to the event, calls `IsShiftPersistent`/`GetStingerAudioRadius`). |
| **Blocks** | Stories for Memory-Trigger and Adaptive Audio, until this ADR reaches `Accepted`. Also: `SessionState.LightingQuery`/`NarrativeState.LightingQuery` remain at their null-object defaults (features work but produce no clues/persistence) until this ADR's bootstrap runs — this ADR's own stories are what unblocks ADR-0003/0004's full behavior, not just this system's own. |
| **Ordering Note** | Foundation layer, Batch 1 priority #5 — the last major registry-binding ADR in Batch 1 (Adaptif Ses Sistemi is next and last, but doesn't produce a contract anything else consumes as a producer-TBD entry). Amends ADR-0001's Verification Required (adds one spike test case, see below). |

## Context

### Problem Statement
The game's core visual identity mechanic — a per-zone lighting/color-temperature shift between a warm "reality" state and a cold "memory" state — is already prototype-validated at the rendering level (spike-confirmed weight-control formula, exact locked color values, empirically measured collider-sizing formula). What remains is architectural: how this becomes a `shiftId`-addressable central service satisfying the `ILightingVolumeQuery` contract two other Foundation-layer systems already depend on, how it integrates with ADR-0001's additive scene-loading/co-residency model without producing false events or permanently stuck state, and how per-zone `Volume`s coexist with ADR-0001's per-scene Volume Layer Mask isolation.

### Constraints
- Must satisfy `ILightingVolumeQuery` exactly as already consumed by ADR-0003/ADR-0004 (`OnShiftStateChanged(shiftId, newState, zoneCenter, radius)`, `IsShiftPersistent(shiftId)`) — this ADR cannot redefine that interface, only implement it
- Must integrate with ADR-0001's additive co-residency window (0.5-2s both scenes resident) without producing spurious events or corrupting permanent state (`SeenShiftIds`, `HeldSessionAlreadyPlayed`) for events the player never actually experienced
- Zone `Volume`s must respect ADR-0001's per-scene Volume Layer Mask isolation
- `Volume.weight`'s sole writer must be this system's own per-zone ticker — no Animator/Timeline may key the same field
- Every `Light` referenced by a `ShiftConfig` must be Light Mode = Mixed, never Baked (URP excludes Baked lights from the realtime forward pass entirely)
- No baked-lightmap-set swapping — post-process + light color/intensity lerp only (already resolved in the GDD, not reopened here)

### Requirements
See `docs/architecture/tr-registry.yaml`: TR-lighting-001 through TR-lighting-049 (extracted from `design/gdd/isik-volume-durum-sistemi.md`), plus consumer-side requirements TR-fpc-032, TR-session-008/009/010/013, TR-narrative-011/016/017/018, TR-memory-004/010/011, TR-audio-013/014/016/017/022/023.

## Decision

### Per-zone component
`LightingTriggerZone : MonoBehaviour` — one per trigger zone, each with:
- An independent local (`isGlobal=false`) URP `Volume` component; all zones share **one** `Volume Profile` asset (locked color values: White Balance Temperature -60, Tint +10, Post Exposure -0.5, Saturation -20)
- Inspector-assigned `string shiftId` (the dispatch key)
- `TriggerMode { Automatic, ManualOnly }` — `Automatic` zones self-trigger on `R_trigger` proximity; `ManualOnly` zones never self-trigger, only an external `TriggerShift(shiftId, config)` call can start `Shifting-In`. Any zone linked to a future `MemoryTriggerDef` must be `ManualOnly`, enforced by the shared `IPreprocessBuildWithReport` editor utility (owned by the future Memory-Trigger ADR, this ADR's zones are simply what that check inspects)
- Placed on a **scene-specific Unity Layer** (`VolumeLayer_Depo`, `VolumeLayer_ServisKoridoru`, `VolumeLayer_BaloSalonu`) — per the user's confirmed decision, this is what makes ADR-0001's per-scene camera `Volume Layer Mask` isolation actually work for local Volumes. **Implementation note (Unity specialist finding)**: the Volume Layer Mask setting lives on the *Camera* component (`UniversalAdditionalCameraData.volumeLayerMask`), not the URP Renderer asset — ADR-0001's implementation must configure it there.
- A box collider sized per the Box Collider Safety Margin formula (spike-confirmed), `blendDistance = 0`

### Volume weight control (spike-validated; mechanism now confirmed, not just observed)
The zone's own ticker sets `Volume.weight = ShiftProgress` directly every tick. **Unity specialist review closed the GDD's own "internal mechanism not determined" caveat**: Unity's `VolumeManager` computes each local Volume's contribution as `distanceFactor × Volume.weight`, where `distanceFactor` derives from collider proximity relative to `blendDistance`. With `blendDistance = 0`, `distanceFactor` becomes binary (1 inside the trigger collider, 0 outside) rather than a smooth falloff — so once the player is inside the collider, the scripted `Volume.weight` write alone drives the effective contribution, exactly matching the spike's observed result. This is confirmed unchanged pre/post-Unity-6 and not listed in `breaking-changes.md`. The GDD's Core Rules can now state this mechanism confidently rather than as an open question.

### Central static registry
```csharp
public static class LightingVolumeState {
    public static bool TriggerShift(string shiftId, ShiftConfig config);
    public static void RevertShift(string shiftId);
    public static bool IsShiftActive(string shiftId);
    public static bool IsShiftPersistent(string shiftId);
    public static float GetStingerAudioRadius(string shiftId);
    public static event Action<string, ShiftState, Vector3, float> OnShiftStateChanged;

    private static readonly Dictionary<string, LightingTriggerZone> _zonesByShiftId = new();
}
```
Zones self-register into `_zonesByShiftId`, mirroring this project's `InteractableRegistry` self-registration pattern (used elsewhere in the GDD ecosystem, e.g. the future Interaction System). **Registration timing (Unity specialist + prior review finding)**: registration happens in `Awake()` (fires for every instantiated object regardless of active state, unlike `OnEnable`) rather than `OnEnable`, so a zone inactive at scene load is still registered in time for the future Narrative State ADR's `ClueConsistencyValidator.ValidateScene` (which runs on `SceneManager.sceneLoaded`, after `Awake()` but before `Start()` — see ADR-0004). Unregistration happens in `OnDestroy()`, guarded by a reference check:
```csharp
void OnDestroy() {
    if (LightingVolumeState.TryGetZone(shiftId, out var registered) && registered == this)
        LightingVolumeState.Unregister(shiftId);
}
```
**Required fix (Unity specialist finding, blocking without it)**: this reference check is mandatory, not optional. During ADR-0001's co-residency window, a reloaded scene's new zone can register the same `shiftId` (e.g., the player leaves and returns to Depo, which unloads and reloads) *before* the outgoing old zone's deferred `OnDestroy` fires. A blind `Dictionary.Remove(shiftId)` in the old zone's teardown would delete the *new* zone's already-current registration. The reference check (`registered == this`) makes the old zone's teardown a safe no-op once superseded, leaving the new zone's registration intact.

Static dispatch methods route to the correct zone via this dictionary; each zone's own state-machine transitions relay into the single aggregated `OnShiftStateChanged` event.

### Unknown-`shiftId` contract (new, filling a GDD gap TD-ADR review flagged)
All five static methods must never throw on an unrecognized `shiftId` (no zone was ever registered under that key — a content/typo error, distinct from "a zone exists but was never triggered," which the GDD already covers as "undefined, caller must query same-frame"):
- `TriggerShift` → returns `false`, logs a `Debug.LogWarning` naming the unmatched `shiftId` (content-authoring error signal, not a crash)
- `RevertShift` → silent no-op (consistent with the GDD's existing "not active → no-op" rule)
- `IsShiftActive` → returns `false`
- `IsShiftPersistent` / `GetStingerAudioRadius` → return their documented safe defaults (`false` / `0`), never throw — consistent with this project's established "never throw, return a safe default" convention (`IsClueKnown`, etc.)

### Persistent-restore on scene load (Decision addition — makes ADR-0003 a concrete dependency)
At `Awake()`, each zone checks `SessionState.PersistentShiftIds` (read-only) for its own `shiftId`. If present and `true`, the zone initializes directly to `Shifted/Held-Persistent` with `ShiftProgress = 1`, skipping `Dormant`/`Shifting-In` entirely, and fires `OnShiftStateChanged(Held)` exactly once after this initialization completes (not during `Awake()` itself, to guarantee subscribers — which may not have finished their own bootstrap yet at the exact `Awake()` instant — reliably receive it; deferred to the zone's first `Update()`/`LateUpdate()` tick).

### Forced completion on destroy (new Decision, closing a gap the GDD itself never addressed — TD-ADR finding)
The GDD's own "tick skip only applies to position checks, `x` always keeps advancing" rule (Core Rules, "Tick tanımı") guarantees an in-flight transition completes on schedule *as long as the zone GameObject still exists*. It does not address the case where the zone's **scene is unloaded and the object destroyed** before `x` reaches 1 — which ADR-0001's deferred-unload timing (0.5-2s after `Complete`) does not strictly guarantee is impossible for every transition duration/timing combination, particularly the task-completion HARD CUT ending, which (unlike the saturation ending) has no gate requiring all memory triggers to have reached `Held` first.

**Decision**: `OnDestroy()` checks whether this zone's shift is currently `Shifting-In` or `Shifting-Out` (not yet `Held`/`Dormant`). If so, it force-completes to the terminal state of its current direction (`Shifting-In → Held`, `Shifting-Out → Dormant`) and synchronously fires the corresponding final `OnShiftStateChanged` event **before** destruction proceeds. This guarantees `SessionState.SettledTriggerIds` and `Narrative State.SeenShiftIds` (both of which key off `Held`) can never be permanently stuck behind `FiredTriggerIds` due to a scene-unload race — ADR-0003's own documented invariant ("`SettledTriggerIds.Count < FiredTriggerIds.Count` is expected transient state") stays true as a *transient* state, never becoming permanent.

### Tick/co-residency rule (GDD-specified, reproduced here as the ADR's authoritative statement)
A zone's ticker skips **position-based** checks (Automatic entry detection, exit hysteresis for both modes) whenever its own scene doesn't match `SceneManager.GetActiveScene()` — pass, not stop/destroy, since the object may still exist until the deferred unload. This does **not** apply to the time-based `x` progress accumulator, which always advances regardless of scene-active state (this is what the Forced Completion On Destroy decision above backstops for the case where the object is destroyed before `x` naturally reaches 1).

### Interface adapter (design decision, confirmed with user)
```csharp
public sealed class LightingVolumeQueryAdapter : ILightingVolumeQuery {
    public event Action<string, ShiftState, Vector3, float> OnShiftStateChanged {
        add => LightingVolumeState.OnShiftStateChanged += value;
        remove => LightingVolumeState.OnShiftStateChanged -= value;
    }
    public bool IsShiftPersistent(string shiftId) => LightingVolumeState.IsShiftPersistent(shiftId);
}
```
**Per TD-ADR review**: a single `static readonly LightingVolumeQueryAdapter` instance is created once and assigned to consumer properties from a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`-attributed bootstrap method — deterministic, matching ADR-0003's own established initialization-timing pattern, never from a scene-local `Awake()`. The assignment is idempotent (assigning the same instance twice is harmless, since consumer properties' setters correctly unsubscribe-then-resubscribe). This closes, concretely, the exact risk ADR-0004 flagged: properties are assigned by one bootstrap action, not several independent ones that could be forgotten separately.

**Amendment (ADR-0006, Adaptive Audio, 2026-08-05) — wiring ownership moved**: this bootstrap method originally lived here and assigned exactly `SessionState.LightingQuery`/`NarrativeState.LightingQuery`. ADR-0006 needed a third assignment (`AdaptiveAudioState.LightingQuery`) plus a structurally identical assignment for an unrelated interface (`ISceneTransitionQuery`), and per that ADR's TD-ADR review, repeatedly hand-patching this bootstrap for every new consumer doesn't scale and incorrectly implies Lighting/Volume "owns" wiring for systems that have nothing to do with lighting. **The actual bootstrap method now lives in `FoundationCompositionRoot.Wire()` (defined in ADR-0006)** — `LightingVolumeQueryAdapter` itself is still this ADR's (ADR-0005's) type, only the *assignment* of it to consumer properties has moved. This ADR's own Decision above is otherwise unchanged; treat any reference to "this ADR's bootstrap" elsewhere in this document as historical — the composition root is the current source of truth for wiring.

### Architecture Diagram
```
        [future Memory-Trigger ADR]        [future Adaptive Audio ADR]
        TriggerShift/RevertShift/                event subscriber,
        IsShiftActive                      IsShiftPersistent/GetStingerAudioRadius
                    │                                    │
                    ▼                                    ▼
            ┌───────────────────────────────────────┐
            │           LightingVolumeState              │
            │   (static, Dictionary<shiftId, zone>)       │
            └───────────────────────────────────────┘
                    ▲                          │
        self-register (Awake)          OnShiftStateChanged (aggregated)
        unregister (OnDestroy,                 │
        reference-checked)                     ▼
                    │                  ┌─────────────────────┐
        ┌───────────┴──────────┐       │ LightingVolumeQueryAdapter │
        │  LightingTriggerZone    │       │   (single instance)        │
        │  (per zone, own Volume, │       └─────────────────────┘
        │  own scene-specific     │                │           │
        │  Layer, own Awake-time  │      SessionState.      NarrativeState.
        │  Persistent-restore     │      LightingQuery      LightingQuery
        │  check, own OnDestroy   │      (ADR-0003)         (ADR-0004)
        │  forced-completion)     │
        └─────────────────────┘
                    ▲
        reads SessionState.PersistentShiftIds (ADR-0003, read-only, at Awake)
```

### Key Interfaces
See Central static registry, Unknown-`shiftId` contract, and Interface adapter above for the full surface.

## Alternatives Considered

### Alternative 1: Each zone exposes its own `MonoBehaviour`-instance API; callers hold direct references
- **Description**: Instead of a central static dispatch, callers (Memory-Trigger, Elevator) hold a direct scene reference to the specific zone they need.
- **Pros**: No dictionary/registry to maintain.
- **Cons**: The GDD's own contract (`TriggerShift(string shiftId, ShiftConfig config)`) is called by `shiftId` string, not by object reference — callers like the future Memory-Trigger system don't hold (and shouldn't need to hold) a direct scene reference to the zone; a central dispatch is what the GDD's API shape already implies.
- **Rejection Reason**: doesn't match the GDD's already-specified API surface; would require rewriting the GDD's contract, not just this ADR's implementation choice.

### Alternative 2: `GameObject.Find`/tag-based lookup instead of a maintained dictionary
- **Description**: Resolve a `shiftId` to a zone by scanning the scene at call time.
- **Pros**: No registration bookkeeping.
- **Cons**: Slow (scene scan per call), fragile (depends on naming/tagging discipline), and doesn't work across the additive co-residency window the way a maintained dictionary with explicit lifecycle hooks does.
- **Rejection Reason**: standard anti-pattern; rejected without serious consideration, consistent with how this project's other self-registration patterns (`InteractableRegistry`) already avoid it.

### Alternative 3: Two independent `ILightingVolumeQuery` adapter instances (one per consumer) instead of one shared instance
- **Description**: Create a separate adapter instance for `SessionState.LightingQuery` and another for `NarrativeState.LightingQuery`.
- **Pros**: Zero shared state between the two assignment paths.
- **Cons**: Per TD-ADR review, the coupling already exists at the interface-contract level (both consumers depend on the same underlying `LightingVolumeState` data regardless of adapter instance count); two instances forwarding to the same static class add object overhead without removing any actual coupling, and double the bootstrap surface for the exact "did the assignment actually happen" risk this ADR is trying to close in one place.
- **Rejection Reason**: one shared adapter instance, assigned to both properties from one bootstrap action, is strictly simpler and doesn't reintroduce the two-separate-things-to-remember risk.

## Consequences

### Positive
- This ADR closes the producer-side gap both ADR-0003 and ADR-0004 have been carrying since they were written — `SessionState`/`NarrativeState` become fully functional (not just null-object-safe) once this ADR's bootstrap runs
- The empirically-spike-validated weight-control mechanism is now also mechanistically explained (Unity specialist finding), removing the GDD's own "internal mechanism not determined" caveat entirely
- Forced-completion-on-destroy closes a real gap (in-flight transitions surviving scene unload) that neither the original GDD nor either prior consumer ADR had addressed — found only because this producer ADR forced the full lifecycle to be specified end-to-end
- The registration-race fix (reference-checked `OnDestroy`) and the `Awake`-vs-`OnEnable` timing fix are both narrow, low-risk corrections that prevent real (if narrow-window) bugs rather than theoretical ones

### Negative
- Per-scene dedicated Volume Layers (`VolumeLayer_Depo`, etc.) consume Unity's finite 32-layer budget — negligible at MVP's 3 areas, but worth flagging now (TD-ADR finding) as a scaling constraint for Full Vision's larger area count, where layer budget could become a real constraint alongside gameplay/physics layers already in use
- The central static registry is one more static-class dependency in a project that already has several (Session State, Narrative State) — consistent with established precedent, but the aggregate surface area of static state across the Foundation layer is now nontrivial and should be kept in mind for testing strategy (each static class needs its own reset path, none share one)

### Risks
- **RenderGraph multi-scene camera-stacking** (ADR-0001's already-open risk) — this ADR adds one test case to that existing spike rather than a new independent risk: confirm two co-resident scenes' zone `Volume`s (now potentially many, one per zone, each on its own scene-specific Layer) do not double-blend or produce a camera-stacking artifact during the co-residency window. See the corresponding ADR-0001 amendment.
- If the future Memory-Trigger or Adaptive Audio ADRs need a different dispatch shape than the five static methods defined here, they need a small adapter — same low-risk, narrow-blast-radius pattern already accepted project-wide for this kind of producer/consumer sequencing gap
- Forced-completion-on-destroy fires a synchronous event from inside `OnDestroy()` — any subscriber that itself tries to access Unity APIs on the (mid-destruction) zone object during that handler would fail; the event payload (`shiftId, newState, zoneCenter, radius`) is captured as plain values before the call, not as live object references, so this should not be an issue in practice, but is worth calling out as an implementation constraint for `dev-story` time

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| isik-volume-durum-sistemi.md | TR-lighting-001/004/005: per-zone local Volume, shared Profile, weight-is-ShiftProgress, spike-validated box sizing | Decision → Per-zone component, Volume weight control |
| isik-volume-durum-sistemi.md | TR-lighting-008/009: tick skip for position-checks only, `x` always advances | Decision → Tick/co-residency rule |
| isik-volume-durum-sistemi.md | TR-lighting-011/012/013: `shiftId` identity, `TriggerMode` Automatic/ManualOnly, edit-time validation | Decision → Per-zone component |
| isik-volume-durum-sistemi.md | TR-lighting-023/024/025/026/027/028/029: `TriggerShift`/`RevertShift`/`IsShiftActive`/`IsShiftPersistent`/`GetStingerAudioRadius`/`OnShiftStateChanged`/`ShiftConfig` contract | Decision → Central static registry, Unknown-shiftId contract |
| isik-volume-durum-sistemi.md | TR-lighting-018: Light Mode = Mixed requirement | Context → Constraints; enforced via the GDD's own `OnValidate` mechanism (AC14b), not expanded in scope by this ADR |
| isik-volume-durum-sistemi.md | TR-lighting-043: session-restore, direct-to-Held-Persistent on scene load, one-time post-load event | Decision → Persistent-restore on scene load |
| isik-volume-durum-sistemi.md | TR-lighting-044: dependency on Gece/Oturum Durumu for `PersistentShiftIds` read | Decision → Persistent-restore on scene load; ADR Dependencies (now concrete, not conceptual) |
| ani-tetikleyici-etkilesim.md | TR-memory-004/010/011: `TriggerShift` with Persistent=true invariant, `ManualOnly` requirement | Decision → Per-zone component |
| adaptif-ses-sistemi.md | TR-audio-013/014/016/017: subscribes to event, calls `IsShiftPersistent`/`GetStingerAudioRadius` | Decision → Central static registry, Interface adapter |
| session-state (ADR-0003) | Producer role for `lighting_shift_state` | Decision → Interface adapter |
| narrative-state (ADR-0004) | Producer role for `lighting_shift_state`, second consumer | Decision → Interface adapter |

## Performance Implications
- **CPU**: negligible per-zone — one lightweight ticker per active zone (MVP: 2-3 memory-trigger zones + at least 1 Automatic zone), no per-light `Update()`
- **Memory**: negligible — dictionary keyed by a handful of `shiftId` strings at MVP scale
- **Load Time**: N/A — zones initialize during normal scene `Awake()`, no additional load step
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system. The empirical spike (`prototypes/yankilar-volume-weight-spike/`) already validates the core mechanism; this ADR's job is architectural integration, not re-validating rendering behavior already confirmed.

## Validation Criteria
- All Acceptance Criteria from `isik-volume-durum-sistemi.md` (AC-1 through AC-22), implemented as automated EditMode/PlayMode tests per this project's Logic-tier test-evidence rules
- New test (registration race, Unity specialist finding): simulate a scene reload with the same `shiftId` — new zone registers before old zone's deferred `OnDestroy` fires; assert the dictionary entry after both complete is the new zone's, not orphaned/removed
- New test (forced completion, TD-ADR finding): destroy a zone mid-`Shifting-In` (e.g. `x=0.4`); assert `OnShiftStateChanged(Held)` fires synchronously before destruction completes, and `SessionState.SettledTriggerIds`/`NarrativeState.SeenShiftIds` correctly receive it
- New test (unknown shiftId contract): all five static methods called with a `shiftId` no zone ever registered — assert no exceptions, and the documented safe-default return values
- New test (adapter assignment): after bootstrap, assert both `SessionState.LightingQuery` and `NarrativeState.LightingQuery` are the same non-null adapter instance, not the null-object default
- New test (Awake-timing): a zone that starts inactive (`GameObject.activeSelf == false`) is still present in `LightingVolumeState`'s registry immediately after scene load, before any `Start()` has run — verifies the `Awake`-not-`OnEnable` registration timing fix

## Related Decisions
- Implements the `ILightingVolumeQuery` contract ADR-0003 and ADR-0004 already registered and consumed via null-object defaults — this ADR is their producer
- Depends on and extends ADR-0001's per-scene Volume Layer Mask decision; adds one test case to that ADR's existing RenderGraph spike (see accompanying ADR-0001 amendment)
- Recommends (per TD-ADR review) registering the self-registering-MonoBehaviour-dictionary pattern itself (not just this specific interface) in `docs/registry/architecture.yaml`, to bind the future Interaction System ADR to the same shape rather than inventing a fourth variant independently — see accompanying registry update
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline and recommended ADR authoring order
