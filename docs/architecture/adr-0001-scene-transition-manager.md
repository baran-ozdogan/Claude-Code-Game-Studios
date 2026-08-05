# ADR-0001: Additive Scene Transition Manager (SOFT/HARD CUT Shared State Machine)

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Rendering / Core (scene management) |
| **Knowledge Risk** | HIGH — Unity 6's URP rendering pipeline (RenderGraph) is post-LLM-cutoff; multi-scene camera-stacking behavior specifically is undocumented in this project's engine reference |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/rendering.md`, `breaking-changes.md`, `deprecated-apis.md`, `current-best-practices.md` |
| **Post-Cutoff APIs Used** | None directly — `SceneManager.LoadSceneAsync(mode: Additive)`, `SceneManager.SetActiveScene`, `SceneManager.UnloadSceneAsync` all predate Unity 6 and are confirmed unchanged (not listed in `breaking-changes.md` or `deprecated-apis.md`). The *risk* is indirect: the RenderGraph pipeline these APIs render through is post-cutoff and untested for this specific multi-scene co-residency pattern. |
| **Verification Required** | Technical spike (owner: unity-specialist or delegate, required before Status can move to `Accepted`): build a 2-scene co-residency test case with `SetActiveScene` swap, each scene carrying an active zone `Volume`, and confirm via Unity 6's Rendering Debugger / Frame Debugger that no camera-stacking artifact, lighting desync, or double-applied post-process occurs during the 0.5-2s co-residency window. **Exit criteria**: swap produces 0 visible artifacts across 10 consecutive test transitions with both scenes' Volumes active. **Added by ADR-0005 (Lighting/Volume State, 2026-08-05)**: the test case must include *multiple* `LightingTriggerZone` Volumes per scene (not just one), each on its scene-specific Volume Layer per this ADR's Decision — confirm co-resident scenes' zone Volumes do not double-blend across the Layer Mask boundary, not just that a single Volume per scene behaves correctly. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | Future ADRs for Asansör/Kat-Erişim Sistemi (Elevator), Sahne Kesmeli Anlatı (Cutscene), and the HARD-CUT-sting portion of Adaptif Ses Sistemi (Adaptive Audio) |
| **Blocks** | Elevator and Cutscene *stories* (not their ADRs) — per this project's ADR lifecycle rule, a `Proposed` ADR auto-blocks stories that reference it. The Elevator and Cutscene ADRs may be authored now against this ADR's frozen interface contract (see Key Interfaces) even while this ADR remains Proposed; only implementation stories wait on the spike. |
| **Ordering Note** | Foundation layer, zero upstream dependencies — Batch 1, priority #1 per `docs/architecture/architecture-review-2026-08-05.md`. |

## Context

### Problem Statement
The game needs two visually and narratively distinct scene-transition experiences implemented in Unity 6.3 URP: a continuous, diegetically-masked transition for elevator floor changes ("SOFT"), and an abrupt, zero-perceptible-delay cut to the psychiatry-session scene when a memory-trigger/narrative tension threshold is reached ("HARD CUT"). Neither may use a loading screen or a fade-to-black UI element — both must be implemented as genuine scene-loading mechanics, not UI tricks, because the GDD's Player Fantasy for each depends on it (see Alternatives Considered, Alternative 3).

### Constraints
- No loading screens, no fade-to-black UI — all masking is diegetic (elevator door) or literally instantaneous (HARD CUT)
- Exactly one active SOFT and one active HARD CUT "slot" at a time, with fully defined queueing/rejection semantics — Elevator and Cutscene are the project's only two callers and must never silently conflict
- 60fps / 16.6ms frame budget project-wide (`.claude/docs/technical-preferences.md`) — the HARD CUT swap must fit within 1 frame
- Baked lightmap data must remain valid per scene while 2 scenes are simultaneously resident (0.5-2s window)
- Must not introduce a second, competing code path for SOFT vs. HARD CUT — the GDD's own Acceptance Criteria (AC-2) requires proof that both run through one shared mechanism

### Requirements
See `docs/architecture/tr-registry.yaml` for the full requirement list: TR-scene-001 through TR-scene-039 (extracted from `design/gdd/seviye-sahne-gecisi.md`), plus consumer-side requirements TR-elevator-014/015/016/017, TR-cutscene-014/019, and TR-audio-022/023.

## Decision

A single scene-persistent singleton `SceneTransitionManager` (matching the in-memory-persistent-service pattern already established for Gece/Oturum Durumu and other Foundation systems in the GDD set), built entirely on Unity's `SceneManager` additive-loading API.

### Core mechanism
- `SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)` is the sole loading primitive for both SOFT and HARD CUT — one shared code path, differentiated only by which config value type is passed in (`SoftTransitionConfig` vs. `HardCutConfig`)
- The `Swapping` state's entire content is one synchronous `SceneManager.SetActiveScene(toScene)` call — nothing else executes during this state, which is what makes `SWAP_FRAME_EPSILON = 1 frame (≤16.6ms at 60fps)` a provable bound rather than an aspiration
- `SceneManager.UnloadSceneAsync(fromScene)` is deliberately excluded from `Swapping` — it fires as an independent background operation starting 0.5-2s after `Complete` (tuning knob), because `UnloadSceneAsync` triggers synchronous `OnDestroy` on every object in the outgoing scene, a real per-object cost that must never land inside the zero-frame guarantee
- `PreloadHardCut`'s `LoadSceneAsync` call always runs to 100% completion with `allowSceneActivation=true` (never the `allowSceneActivation=false`-hold-at-90% pattern), so every `Awake`/`Start` cost on the target scene is paid before `Ready`, never during `Swapping`
- `PreloadHardCut`'s own Preloading→Ready progress is tracked in an internal field (`_hardCutPreloadState`), separate from the public `CurrentState` — this lets a HARD CUT preload silently in the background behind an active SOFT transition

### Type Arbitration Policy (the *only* sanctioned type-branch)
Per Technical Director review: a shared state machine risks becoming an `if/else` minefield as more transition variants are added later (Vertical Slice's Multi-Night Progression, Full Vision's Ending Sequence). To prevent that, `TransitionType` is branched on in exactly one place — this table — and nowhere else in the state machine:

| Active State | Incoming Request | Result |
|---|---|---|
| Idle | SOFT or HARD CUT | Accepted, begins Preloading |
| SOFT active (Preloading/Ready/Swapping) | SOFT (same type) | Rejected — no-op, logged warning, `OnSoftTransitionRejected("AlreadyTransitioningSoft")` |
| SOFT active | HARD CUT | **Queued** into the single pending slot — auto-fires the instant SOFT reaches `Idle` |
| HARD CUT active (Preloading/Ready/Swapping) | HARD CUT (same type) | Rejected — no-op, logged warning |
| HARD CUT active | SOFT | Rejected — `OnSoftTransitionRejected("HardCutActive")`, **not** queued (deliberate asymmetry: HARD CUT is narratively critical and must never be silently lost; SOFT is player-initiated and can be retried) |

All other state-machine logic (`Preloading`→`Ready`→`Swapping`→`Complete`/`Failed`) is type-agnostic, driven purely by whichever `ITransitionProfile` config instance is active. `SoftTransitionConfig` and `HardCutConfig` both implement a common `ITransitionProfile` marker interface; a future transition variant (e.g., a Multi-Night Progression scene change) adds a new config type implementing that interface, never a new branch in the state machine itself. **Invariant, testable via static analysis**: the state machine implementation contains zero `switch`/`if` on `TransitionType` outside this arbitration table.

### Rendering / lighting isolation (revised per Unity specialist review)
- Each area scene carries its own `SceneEnvironmentSettings` MonoBehaviour; on `SetActiveScene`, a **synchronous, non-coroutine** script call reads it and syncs global `RenderSettings` (skybox material, ambient) — a "shared Environment scene" approach is explicitly rejected, since `RenderSettings`/`LightmapData` are engine-global-per-scene in Unity, not composable across additively loaded scenes
- **New finding (Unity specialist review)**: URP `Volume` blending is driven by world-space trigger colliders and global volume lists, not scene membership — during the SOFT co-residency window, both scenes' zone `Volume`s (including this project's memory-trigger lighting-shift Volumes, see the Işık/Volume Durum Sistemi GDD) are simultaneously active and can blend/double-apply if their bounds or global-volume lists overlap. **Decision**: each scene's camera must carry a `Volume Layer Mask` scoped to only that scene's Volume layer, switched synchronously in the same call that performs the `RenderSettings` sync at `SetActiveScene` — not deferred to a later frame or a coroutine.

### Explicitly out of scope
- **Movement-lock ownership**: this system never calls `RequestMovementLock`/`ReleaseMovementLock` — the calling system (Elevator, Cutscene) owns lock lifecycle entirely. This system remains pure infrastructure.
- **`HardCutConfig.Abrupt` interpretation**: this system carries the `Abrupt` flag but does not interpret it — only Adaptive Audio's HARD CUT Sting branches on its value (via `GetCurrentHardCutAbrupt()`).
- **Asset-loading strategy for non-scene content** (ScriptableObject/prefab loading — `MemoryTriggerDef`, `ShiftConfig`, `CarryItemDef`, etc.): explicitly out of scope for this ADR. `docs/architecture/architecture-review-2026-08-05.md` (Engine Compatibility Issues #2) already flags this as undecided project-wide and recommends a dedicated future ADR (owner: `unity-addressables-specialist` domain per `technical-preferences.md`). Rejecting Addressables as this ADR's *scene-loading* mechanism (see Alternatives Considered, Alternative 2) does not imply a project-wide stance on asset loading generally.

### Architecture Diagram
```
                    ┌───────────────────────────────────┐
                    │       SceneTransitionManager        │
                    │    (scene-persistent singleton)     │
                    └───────────────────────────────────┘
                       ▲                    ▲        │
   RequestSoftTransition       RequestHardCut         │ OnTransitionStateChanged
   (future Elevator ADR)       PreloadHardCut          │ (newState, type)
                                (future Cutscene ADR)    ▼
                                                   [future Adaptive Audio ADR:
                                                    HARD CUT Sting, filters
                                                    type==Hard, queries
                                                    GetCurrentHardCutAbrupt()]

  Idle → Preloading → Ready → Swapping (1 frame: SetActiveScene
                                          + synchronous RenderSettings/
                                          Volume-mask sync only) → Complete → Idle
                                    │
                                    └─(load failure)→ Failed → (auto) → Idle
```

### Key Interfaces
```csharp
public enum TransitionType { Soft, Hard }
public enum TransitionState { Idle, Preloading, Ready, Swapping, Complete, Failed }

public interface ITransitionProfile { }
public struct SoftTransitionConfig : ITransitionProfile { /* pacing-floor duration 2-8s */ }
public struct HardCutConfig : ITransitionProfile { public bool Abrupt; }

public interface ISceneTransitionManager {
    void PreloadHardCut(string toScene);
    void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config, Action onComplete, Action<string> onFailed);
    void RequestHardCut(string toScene, HardCutConfig config, Action onComplete, Action<string> onFailed);
    TransitionState CurrentState { get; }

    // Returns config.Abrupt for the active/preloaded HARD CUT. Callers must
    // only query this in the frame they receive OnTransitionStateChanged(Swapping, Hard)
    // per the GDD contract. If queried outside that window (no HARD CUT
    // active/preloaded), returns false as a defensive default rather than
    // truly undefined behavior — this is an implementation floor, not a
    // supported alternate usage pattern.
    bool GetCurrentHardCutAbrupt();

    event Action<TransitionState, TransitionType> OnTransitionStateChanged;
    event Action<string> OnSoftTransitionRejected; // reason: "AlreadyTransitioningSoft" | "HardCutActive"
}
```

This interface is **contract-frozen** as of Technical Director approval (see below) — the Elevator and Cutscene ADRs may be authored against it immediately, even while this ADR's Status remains `Proposed` pending the RenderGraph spike.

**Amendment (ADR-0006, Adaptive Audio, 2026-08-05)**: the concrete `SceneTransitionManager` also implements a second, narrower interface for read-only consumers that only need to react to transitions, never command them:
```csharp
public interface ISceneTransitionQuery {
    event Action<TransitionState, TransitionType> OnTransitionStateChanged;
    bool GetCurrentHardCutAbrupt();
}
```
This is a strict subset of `ISceneTransitionManager` above — no new behavior, no change to this ADR's Decision. It exists so a consumer like Adaptive Audio (which only ever subscribes to the event and queries `GetCurrentHardCutAbrupt`) doesn't need to depend on — or provide a meaningless null implementation of — `PreloadHardCut`/`RequestSoftTransition`/`RequestHardCut`, which it never calls. Mirrors `ILightingVolumeQuery`'s (ADR-0003/0005) already-established precedent of narrowing a producer's interface to only what a given consumer class actually needs.

## Alternatives Considered

### Alternative 1: Single-scene, object-pooling per area (no additive load)
- **Description**: Keep everything in one scene; enable/disable pooled room geometry per area instead of loading separate scenes.
- **Pros**: Zero multi-scene RenderGraph risk; simplest possible mental model.
- **Cons**: Cannot hold two areas simultaneously resident for SOFT's masking illusion (door closes on one area, opens on another, with no load moment in between); `RenderSettings`/lightmap-per-area become manual save/restore hacks instead of Unity's native per-scene isolation; doesn't scale to Vertical Slice's multi-night/multi-area content.
- **Rejection Reason**: fails the SOFT transition's core requirement (co-residency) outright.

### Alternative 2: Addressables-based scene loading (`Addressables.LoadSceneAsync`)
- **Description**: Load area scenes via Addressables asset references instead of raw `SceneManager`.
- **Pros**: Aligns with the project's named Addressables specialist domain (`technical-preferences.md`); asset-reference-based loading scales better for remote content/DLC later.
- **Cons**: Adds a dependency (Addressables package configuration, catalog management) this MVP's 3 fixed local scenes don't need; `Addressables.LoadSceneAsync` wraps the same underlying `SceneManager` additive/active-scene semantics anyway, so it does not remove the RenderGraph/multi-scene risk this ADR must still address regardless.
- **Rejection Reason**: unnecessary complexity for MVP scope, scoped to *scene* loading only — does not foreclose a future Addressables ADR for other asset types (see Explicitly Out of Scope).

### Alternative 3: Fade-to-black UI transition for both SOFT and HARD CUT
- **Description**: Standard loading-screen/fade pattern instead of true co-residency or a zero-frame swap.
- **Pros**: Trivially simple, no additive-scene-loading engine risk at all.
- **Cons**: Directly contradicts both Player Fantasy pillars this system exists to serve — SOFT's "Beden Sürekliliği" (bodily continuity: no fade means no break in presence) and HARD CUT's "Bedenin Çalınması" (torn from mid-motion: a *loading* read defeats the "stolen" read entirely, per the GDD's own explicit reasoning).
- **Rejection Reason**: fails the design brief at the pillar level, not just as a technical tradeoff.

## Consequences

### Positive
- One code path for both transition types halves the bug surface versus separate SOFT/HARD implementations, and the Type Arbitration Policy keeps that guarantee extensible rather than eroding as new transition variants are added
- Zero-frame HARD CUT swap is provable/testable (`SWAP_FRAME_EPSILON` + 0-black-frames binary invariant), not just "should feel instant"
- Per-scene `RenderSettings`/`LightmapData` isolation is Unity's native behavior, not a custom workaround — lower maintenance risk
- Deferred unload (post-`Complete`, background) means the expensive `OnDestroy` cost never risks the frame budget

### Negative
- Static batching does not merge across additively loaded scenes — draw call budget must be planned per-scene, slightly reducing the batching win the project's ~2000 draw-call budget (`technical-preferences.md`) assumes
- Two scenes resident simultaneously for 0.5-2s means transient memory overhead (both areas' assets loaded at once) — acceptable at MVP's 3-area scope, may need revisiting for Full Vision's larger area count

### Risks
- **RenderGraph multi-scene camera-stacking behavior is unverified** (HIGH, post-cutoff) — mitigated by keeping this ADR's Status `Proposed` (not `Accepted`) until the spike in Engine Compatibility → Verification Required is run and passes its exit criteria
- **Volume co-residency during the SOFT window** (new finding, Unity specialist review) — mitigated by the per-scene `Volume Layer Mask` scoping decision above; if not implemented correctly, two scenes' lighting-shift Volumes could double-blend during the 0.5-2s co-residency window
- Exceptions thrown inside a caller's `onComplete` must be caught internally (per GDD Edge Cases) — if a future implementer misses this, an unhandled exception could leave `CurrentState` stuck at `Complete` forever, silently soft-locking every subsequent transition request

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| seviye-sahne-gecisi.md | TR-scene-001: single shared code path via additive loading | Decision → Core mechanism: one `SceneTransitionManager`, two `ITransitionProfile` config types |
| seviye-sahne-gecisi.md | TR-scene-004/006: zero-frame HARD CUT swap, `SWAP_FRAME_EPSILON` | `Swapping` = single synchronous `SetActiveScene` call |
| seviye-sahne-gecisi.md | TR-scene-005: unload deferred out of `Swapping` | Decision → Core mechanism, `UnloadSceneAsync` background timing |
| seviye-sahne-gecisi.md | TR-scene-010: RenderGraph multi-scene risk | Engine Compatibility → Verification Required; Consequences → Risks |
| seviye-sahne-gecisi.md | TR-scene-012: per-scene `RenderSettings` via `SceneEnvironmentSettings` | Decision → Rendering / lighting isolation |
| seviye-sahne-gecisi.md | TR-scene-017/032: arbitration/queueing rules (same-type reject, HARD-during-SOFT queue, SOFT-during-HARD reject) | Decision → Type Arbitration Policy table |
| seviye-sahne-gecisi.md | TR-scene-025/026: `OnTransitionStateChanged(state,type)`, `OnSoftTransitionRejected(reason)` | Key Interfaces |
| seviye-sahne-gecisi.md | TR-scene-033/034: coordinate-frame alignment for SOFT position transfer | Delegated to level-design/prefab convention per GDD — not an architectural decision this ADR owns |
| asansor-kat-erisim-sistemi.md | TR-elevator-014/015/016/017: `RequestSoftTransition` + `onFailed` handling | Key Interfaces, `Failed`→`Idle` auto-recovery in Decision |
| sahne-kesmeli-anlati-2026-08-02.md | TR-cutscene-014/019: `PreloadHardCut`/`RequestHardCut`, zero-frame guarantee at real trigger | Decision → Core mechanism, Key Interfaces |
| adaptif-ses-sistemi.md | TR-audio-022/023: HARD CUT Sting subscribes to `OnTransitionStateChanged`, filters `type==Hard`, queries `GetCurrentHardCutAbrupt()` | Key Interfaces |

## Performance Implications
- **CPU**: `SetActiveScene` itself is O(1)/negligible; async load cost is paid entirely before `Ready`, never during `Swapping`
- **Memory**: transient +1 scene's worth of assets during the 0.5-2s co-residency/unload-delay window — bounded by MVP's 3 small areas
- **Load Time**: no player-facing load time for either transition type by design (SOFT masked diegetically, HARD CUT zero-frame)
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system, no existing code to migrate from.

## Validation Criteria
- AC-1 through AC-11a from `seviye-sahne-gecisi.md` (state sequencing, rejection/queueing semantics, `Failed` recovery), implemented as automated EditMode/PlayMode tests per this project's Logic-tier test-evidence rules
- AC-9 specifically: automated frame-capture or swap-timestamp-delta test proving `SWAP_FRAME_EPSILON` ≤ 1 frame AND exactly 0 black rendered frames
- Static-analysis check enforcing the Type Arbitration Policy invariant (zero `switch`/`if` on `TransitionType` outside the arbitration table)
- Outstanding: the RenderGraph multi-scene spike (Engine Compatibility → Verification Required) — its result either closes this ADR's Risks entry (moving Status to `Accepted`) or requires a Decision revision if it surfaces a real artifact

## Related Decisions
- Enables: future ADRs for Asansör/Kat-Erişim Sistemi, Sahne Kesmeli Anlatı, and Adaptif Ses Sistemi (HARD CUT Sting portion only)
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline and recommended ADR authoring order
- See `docs/registry/architecture.yaml` — this ADR is the first to register entries (pending approval below)
