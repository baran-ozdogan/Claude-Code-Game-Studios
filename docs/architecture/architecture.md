# Yankılar — Master Architecture

## Document Status
- **Version**: 1.0
- **Last Updated**: 2026-08-05
- **Engine**: Unity 6.3 LTS (6000.3.0f1)
- **GDDs Covered**: 12 MVP systems (9 Full GDDs + 3 Quick Specs) — see `design/gdd/systems-index.md`
- **ADRs Referenced**: ADR-0001 … ADR-0015 — the complete Required ADRs list, all written 2026-08-05 → 2026-08-08 and Accepted 2026-08-09 *(updated 2026-08-09, `/architecture-review` follow-up; the original line — "None yet, written before any ADR" — was true at authoring time and is preserved in the ADR Audit section's history note)*
- **Technical Director Sign-Off**: 2026-08-05 — APPROVED WITH CONDITIONS (self-review; 1 finding — `internal`-visibility does not enforce single-caller restriction for `SetRoundState`/`EndSession`, recorded as QQ-03, resolve at `/create-control-manifest`)
- **Lead Programmer Feasibility**: CONCERNS → RESOLVED 2026-08-05 — 4 findings from independent review, all fixed same session: 3 API Boundaries signature mismatches corrected against their Approved GDD contracts (Işık/Volume's `TriggerShift`/`RevertShift`, Seviye/Sahne Geçişi's `RequestSoftTransition`/`RequestHardCut`/`PreloadHardCut`, `InteractableRegistry`'s missing `Register`/`Deregister`); Domain Reload risk (QQ-05) and testability/DI conflict with `coding-standards.md`'s BLOCKING unit-test rule (QQ-06) both routed into Required ADR #1 as must-resolve items before Foundation implementation starts

## Engine Knowledge Gap Summary

**Engine**: Unity 6.3 LTS (6000.3.0f1), released Dec 2025 | **LLM training cutoff**: ~May 2025 | **Pinned**: 2026-08-01

### HIGH risk domains (verify against `docs/engine-reference/unity/` before deciding)
- **Input System**: Legacy `Input.*` class is deprecated — mandatory `UnityEngine.InputSystem` (`Keyboard.current`, `Gamepad.current`). All GDDs already specify Input System correctly (`birinci-sahis-kontrolcu.md`'s single "Gameplay" action map).
- **URP RenderGraph**: Custom `ScriptableRendererFeature` passes now use `RecordRenderGraph(RenderGraph, ContextContainer)` — the old `Execute(ScriptableRenderContext, ref RenderingData)` pattern is deprecated. Directly relevant to Işık/Volume Durum Sistemi's per-zone lighting mechanism — see Module Ownership, Işık/Volume row, for the explicit call on whether a custom pass is needed.
- **UI Toolkit vs. UGUI**: UI Toolkit (UXML/USS) is Unity's recommended runtime-UI path for new Unity 6 projects; no GDD specifies either for this game's UI (crosshair/prompt, Hold-fill ring, stinger caption). This document makes an explicit call in Module Ownership below.

### MEDIUM risk domains
- **Addressables** (6.2+ throws on load failure instead of returning null) — not currently used by any GDD; noted for completeness, no action needed at MVP scope.

### Correction (ADR-0003 / unity-specialist validation, 2026-08-05)
This section previously listed "Physics solver default (6→8 iterations) — behavioral change affecting `CharacterController`/collider tuning" as MEDIUM risk. **This was a factual error, removed here.** `CharacterController` is a kinematic sweep-based controller driven by its own `Move()`/`skinWidth`/`stepOffset`/`slopeLimit` parameters — it has never consumed `Physics.defaultSolverIterations`, which governs only the dynamic Rigidbody/joint solver. This project has no Rigidbody-driven gameplay (confirmed in Module Ownership below), so the solver-iteration default change has no relevance to this project at all, not just "limited impact." Caught during ADR-0003's engine-specialist validation; corrected here to prevent the same error propagating into future ADRs that cite this summary.

### LOW risk / not applicable
- **DOTS/Entities**, **Netcode for GameObjects**, **NavMesh/AI pathfinding**, **Cinemachine 3.0**: none are used by any MVP system. DOTS is an explicit engine mis-fit for this project's small, hand-authored scope; Cinemachine is explicitly not used (project rejects camera-owned post-process/virtual cameras per `birinci-sahis-kontrolcu.md`).
- **Animation, Audio (AudioMixer/AudioSource)**: stable, well-covered by LLM training data, matches GDD architecture directly (`adaptif-ses-sistemi.md`'s 4-mixer-group design).

## System Layer Map

This project's `design/gdd/systems-index.md` already carries a Foundation → Core → Feature → Presentation dependency-order classification for all 12 MVP systems, refined across 4 rounds of `/review-all-gdds`. This architecture document adopts that classification directly rather than re-deriving it, and resolves the **two cross-layer exceptions** the index itself had left as open architectural questions (see below) — plus adds the Platform layer this document's own template requires, which the design-side index had no reason to track.

```
┌──────────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER  (Vertical Slice — NOT built this pass)       │
│   Hibrit Tepkisellik · Çoklu Gece İlerlemesi ·                   │
│   Arkadaş Karakteri/NPC · Ana Menü/Başlangıç Akışı               │
├──────────────────────────────────────────────────────────────────┤
│ FEATURE LAYER  (depends on Core)                                 │
│   Görev/Taşıma Döngüsü · Anı-Tetikleyici Etkileşim ·             │
│   Sahne Kesmeli Anlatı                                           │
├──────────────────────────────────────────────────────────────────┤
│ CORE LAYER  (depends on Foundation)                              │
│   Etkileşim Sistemi · Asansör/Kat-Erişim Sistemi ·               │
│   Diyalog/Anlatı İçeriği                                         │
├──────────────────────────────────────────────────────────────────┤
│ FOUNDATION LAYER  (no dependency reaches into a higher layer)    │
│   Birinci Şahıs Kontrolcü · Işık/Volume Durum Sistemi ·          │
│   Gece/Oturum Durumu · Anlatı Durum/İpucu Takibi ·               │
│   Seviye/Sahne Geçişi · Adaptif Ses Sistemi ·                    │
│   InteractableRegistry (relocated here, see below)               │
├──────────────────────────────────────────────────────────────────┤
│ PLATFORM LAYER                                                    │
│   Unity 6.3 LTS runtime · PC (Steam/Epic) · no custom platform   │
│   abstraction needed — single-target MVP, no console/mobile      │
└──────────────────────────────────────────────────────────────────┘
```

**Vertical Slice / Full Vision systems** (`systems-index.md` rows 13-17: Hibrit Tepkisellik, Çoklu Gece İlerlemesi, Arkadaş Karakteri/NPC, Ana Menü/Başlangıç Akışı, Plot Twist/Final Sekansı) are out of scope for this architecture pass — none have approved GDDs yet, and none block MVP implementation. They're listed above only so the Foundation/Core/Feature contracts below are designed with obvious extension points in mind, not so this document specifies their internals.

### Resolved cross-layer exceptions (both were open questions in `systems-index.md`)

**1. `InteractableRegistry` relocated Core → Foundation.** Birinci Şahıs Kontrolcü's approach-slow-taper formula needs to read the set of nearby `IInteractable`s, but the registry was owned by Etkileşim Sistemi (Core) — a Foundation-layer system reading upward into Core. Fix: `IInteractable` (the interface) and `InteractableRegistry` (the self-registering static collection) become a **Foundation-layer contract**, alongside `IPlayerState`. Etkileşim Sistemi (Core) remains the primary *behavioral* owner — it still implements focus detection, Hold state machines, and the crosshair — but it now **consumes** a Foundation-owned data structure rather than owning it outright. This is the same resolution shape as `IPlayerState` itself: a shared read contract lives at the lowest layer that needs to read it, and every higher-layer system that also needs it (Etkileşim, and later Görev/Taşıma's pickups, Anı-Tetikleyici's triggers) consumes it downward, which is the only direction layering allows.

**2. Round counter relocated Feature → Foundation.** Adaptif Ses Sistemi's `tension_gain` formula reads Görev/Taşıma Döngüsü's `CurrentRoundIndex`/`TotalRoundCount` — a 2-layer upward read (Foundation reading Feature), the more serious of the two violations. Fix: round progress is genuinely session-level state, not carry-loop-exclusive logic, and Gece/Oturum Durumu (Foundation) already owns adjacent session facts (`FiredTriggerIds`, `PersistentShiftIds`, `SettledTriggerIds`). `CurrentRoundIndex`/`TotalRoundCount` move to Gece/Oturum Durumu as new fields; Görev/Taşıma Döngüsü (Feature) **writes** to them on every round transition (it still computes and owns the round-advancement *logic*, it just publishes the resulting counters to the Foundation-owned session-state object instead of exposing them as its own public API), and Adaptif Ses Sistemi (Foundation) reads them from Gece/Oturum Durumu — an intra-Foundation read, same pattern as its existing `PersistentShiftIds`/`OnTransitionStateChanged` dependencies.

Both fixes become required ADRs (see Required ADRs, Foundation layer) rather than being treated as implicit — the GDDs themselves (`etkilesim-sistemi.md` Open Questions #1, `gorev-tasima-dongusu.md`/`adaptif-ses-sistemi.md` cross-reference) will need small updates once the ADRs are written to point at the new ownership, not the old one.

## Module Ownership

Two engine-level decisions resolved this phase (both confirmed, not just assumed):
- **UI framework: UI Toolkit** (UXML/USS) for the crosshair/prompt, Hold-fill ring, stinger caption, and dialogue subtitles — Unity 6's recommended path for new runtime UI, and this game's entire UI surface is small enough (4 elements total) that Canvas overhead isn't a factor either way, so the forward-compatible choice wins.
- **No custom URP RenderGraph pass needed for Işık/Volume Durum Sistemi.** Every locked value in `isik-volume-durum-sistemi.md` (White Balance/Tint/Post Exposure/Saturation via Volume Profile, Light `color`/`intensity` via direct component access) is achievable with a single shared Volume Profile + per-zone `Volume.weight` blending (built into URP, no custom pass) plus a script-driven `Light` property lerp on Mixed-mode lights. This sidesteps the HIGH-risk RenderGraph API surface entirely — flagged in the Engine Knowledge Gap Summary as a real risk, resolved here as: not applicable to this system.

### Foundation Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|---|---|---|---|---|
| **Birinci Şahıs Kontrolcü** | Player `Transform`, `CharacterController`, camera pitch, `IPlayerState` impl, reference-counted movement-lock registry, shared head-bob/footstep phase accumulator | `IPlayerState` (`EyeCamera`, `Velocity`, `IsGrounded`, `MovementLocked`, `IsCarrying`, `IsLocked`, `MovementLockChanged` event), `RequestMovementLock(requester, scope)` / `ReleaseMovementLock(requester)` | `InteractableRegistry` (intra-Foundation, approach-slow-taper) | `CharacterController`, Input System (`Keyboard`/`Mouse` actions), `Camera` |
| **InteractableRegistry** *(relocated from Core this phase)* | `IInteractable` interface definition, static self-registering collection (`OnEnable`/`OnDisable`) | Frame-start read-only snapshot, registration API | — | none (pure C#) |
| **Işık/Volume Durum Sistemi** | Per-zone `Volume` components (shared Profile), `TriggerMode`/`Persistent` config, per-zone `ShiftProgress` state machine, ticker coroutine | `TriggerShift`/`RevertShift`/`IsShiftActive`/`IsShiftPersistent`/`GetStingerAudioRadius`, `OnShiftStateChanged` event | Gece/Oturum Durumu (intra-Foundation, Persistent-restore on reload), FPC's `PlayerMaxSpeed` constant (intra-Foundation) | URP `Volume`, `Light` (Mixed mode), Coroutines — **no custom RenderGraph pass** (see above) |
| **Gece/Oturum Durumu** | `IsSessionActive`, `CurrentNightNumber`, `FiredTriggerIds`, `PersistentShiftIds`, `SettledTriggerIds`, **`CurrentRoundIndex`/`TotalRoundCount`** *(relocated from Feature this phase)* | `EndSession()`, `OnTriggerFired`/`OnTriggerSettled` events, round-counter reads | Işık/Volume's `OnShiftStateChanged` (intra-Foundation, writes `PersistentShiftIds`/`SettledTriggerIds`); Görev/Taşıma Döngüsü **writes** round counters here (Feature→Foundation write, the allowed direction) | none — pure in-memory C# static service |
| **Anlatı Durum/İpucu Takibi** | `KnownClueIds`, `SeenShiftIds`, centralized `ClueDefinition` registry | `MarkClueKnown`/`IsClueKnown`/`GetKnownClueIds`/`OnClueKnown` event | Işık/Volume's `OnShiftStateChanged` (intra-Foundation, `Held` only) | `ScriptableObject` |
| **Seviye/Sahne Geçişi** | `SceneTransitionManager` state machine, `SoftTransitionAnchor` contract, `SceneEnvironmentSettings` | `RequestSoftTransition`/`RequestHardCut`, `OnTransitionStateChanged`/`OnSoftTransitionRejected` events, `GetCurrentHardCutAbrupt()` | — (no upstream Foundation dependency) | `SceneManager.LoadSceneAsync(Additive)`/`SetActiveScene`, `RenderSettings` |
| **Adaptif Ses Sistemi** | `AmbientZoneVolume` triggers, 4 mixer groups (Ambiance/Stinger/CutSting/SFX), stinger pool state machine, `HeldSessionAlreadyPlayed` | `PlayFootstep(speed)` | Işık/Volume's `OnShiftStateChanged` (intra-Foundation), Seviye/Sahne Geçişi's `OnTransitionStateChanged` (intra-Foundation), Gece/Oturum Durumu's round counters + `IsShiftPersistent` (intra-Foundation, **both relocated dependencies land here**) | `AudioMixer`, pooled `AudioSource`s |

### Core Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|---|---|---|---|---|
| **Etkileşim Sistemi** | Focus detection (`SphereCast`), Hold state machine, crosshair/prompt UI, default Hold-fill rendering | `IInteractable` consumption pattern (implemented by Feature-layer objects), crosshair UI | `InteractableRegistry` + `IPlayerState` (Foundation), `RequestMovementLock` (Foundation) | `Physics.SphereCastNonAlloc`, UI Toolkit (`UIDocument`) |
| **Asansör/Kat-Erişim Sistemi** | Cabin state machine, call-button trigger zone | — (self-contained gameplay object, no public API consumed elsewhere) | `IPlayerState`/`RequestMovementLock` (Foundation), Seviye/Sahne Geçişi's `RequestSoftTransition` (Foundation), Gece/Oturum Durumu's `IsSessionActive` (Foundation) | Trigger `Collider`, Input System |
| **Diyalog/Anlatı İçeriği** | `DialogueSceneConfig`, `CallbackPool`, `UsedCallbackIds` | Scene-entry dialogue playback hook | Anlatı Durum's `IsClueKnown` (Foundation) | UI Toolkit (subtitle display) |

### Feature Layer

| Module | Owns | Exposes | Consumes | Engine APIs |
|---|---|---|---|---|
| **Görev/Taşıma Döngüsü** | `TaskList`/`CarryRound` state, `CollectedItemIds`, pooled carry-slot visuals, `HasCarriedInFinalRound` | `OnTaskListCompleted`/`OnFinalRoundStarted`/`OnFinalRoundItemPickedUp` events, `IsFinalRoundActive` | `InteractableRegistry`+`IPlayerState` (Foundation), Etkileşim's `IInteractable.Instant` (Core), Asansör (Core, soft/indirect) — **writes** `CurrentRoundIndex`/`TotalRoundCount` into Gece/Oturum Durumu (Foundation) | Trigger `Collider` (delivery zone), object pooling |
| **Anı-Tetikleyici Etkileşim** | `MemoryTriggerDef` assets, `MemoryTriggerObject` Committed state | — (pure thin orchestration, no downstream consumers) | Etkileşim's `Hold` type (Core), Işık/Volume's `TriggerShift`/`RevertShift` (Foundation), Gece/Oturum Durumu's `FiredTriggerIds` (Foundation, Committed-restore) | none specific |
| **Sahne Kesmeli Anlatı** | End-condition OR-logic orchestration, `HasTriggeredThisNight` | — (pure orchestration) | Görev/Taşıma's events (Feature), Gece/Oturum Durumu's `SettledTriggerIds`/`OnTriggerSettled` (Foundation), Seviye/Sahne Geçişi's `RequestHardCut` (Foundation), FPC's movement lock (Foundation) | none specific |

### Dependency Diagram (module-level, arrows point "depends on")

```
Sahne Kesmeli Anlatı ──┬──> Görev/Taşıma Döngüsü
                        ├──> Gece/Oturum Durumu (SettledTriggerIds)
                        ├──> Seviye/Sahne Geçişi (RequestHardCut)
                        └──> Birinci Şahıs Kontrolcü (movement lock)

Anı-Tetikleyici Etkileşim ──┬──> Etkileşim Sistemi (Hold)
                             ├──> Işık/Volume Durum Sistemi (TriggerShift)
                             └──> Gece/Oturum Durumu (FiredTriggerIds)

Görev/Taşıma Döngüsü ──┬──> InteractableRegistry, IPlayerState
                        ├──> Etkileşim Sistemi (Instant)
                        ├──> Asansör/Kat-Erişim Sistemi
                        └──> Gece/Oturum Durumu (writes round counters) ⟵ NEW

Etkileşim Sistemi ──┬──> InteractableRegistry, IPlayerState
                     └──> RequestMovementLock (FPC)

Asansör/Kat-Erişim ──┬──> IPlayerState/RequestMovementLock
                      ├──> Seviye/Sahne Geçişi
                      └──> Gece/Oturum Durumu (IsSessionActive)

Diyalog/Anlatı İçeriği ──┬──> Anlatı Durum/İpucu Takibi (IsClueKnown)
                          └──> UIRoot (ADR-0010's accessor, subtitle element — edge added
                               2026-08-09 per ADR-0012's own stale-diagram flag)

Adaptif Ses Sistemi ──┬──> Işık/Volume (OnShiftStateChanged)
                       ├──> Seviye/Sahne Geçişi (OnTransitionStateChanged)
                       └──> Gece/Oturum Durumu (round counters, IsShiftPersistent) ⟵ NEW target

Anlatı Durum/İpucu Takibi ──> Işık/Volume (OnShiftStateChanged)

Işık/Volume Durum Sistemi ──┬──> Gece/Oturum Durumu (Persistent-restore)
                             └──> Birinci Şahıs Kontrolcü (PlayerMaxSpeed)

Birinci Şahıs Kontrolcü ──> InteractableRegistry  ⟵ NEW (was Core→Foundation violation, now intra-Foundation)

InteractableRegistry, Gece/Oturum Durumu, Seviye/Sahne Geçişi ── no upstream dependencies (true Foundation roots)
```

## Data Flow

### 1. Frame Update Path

```
Input System (Keyboard/Mouse actions)
   │
   ▼
Birinci Şahıs Kontrolcü  — movement/camera, IPlayerState fields updated,
   │                        shared head-bob/footstep phase accumulator advances
   ▼
Etkileşim Sistemi        — SphereCast focus detection from FPC's EyeCamera (every frame),
   │                        Hold `t` progress if a Hold is active
   ▼
Işık/Volume ticker(s)    — one coroutine per active zone advances ShiftProgress;
   │                        position-sampling frozen during SOFT co-residency (TR-isik-011),
   │                        time-based progress for in-flight shifts never freezes
   ▼
Adaptif Ses Sistemi      — footstep volume reads FPC's phase accumulator inline (no polling,
   │                        no race — TR-ses-012); ambiance crossfade/tension_gain updates
   ▼
URP rendering            — Volume weight blending + Light color/intensity already set by
                            Işık/Volume's script-driven lerp; no custom RenderGraph pass (Module Ownership)
```

Nothing above requires cross-frame ordering guarantees beyond Unity's default Update loop — each system reads the *previous* frame's committed state from the ones above it in this chain, one-directional, no cycles.

### 2. Event/Signal Path

**Explicit principle: no generic event bus / mediator class.** Every cross-system signal is a narrowly-typed C# `event`/`delegate` declared on the *owning* module (e.g. `Işık/Volume.OnShiftStateChanged`, `Gece/Oturum Durumu.OnTriggerSettled`), subscribed to directly by consumers. This was a deliberate, repeated choice throughout the GDD design history (`systems-index.md`'s own Circular Dependencies note explicitly rejected a shared `MemoryTriggerEvent` God Object in favor of each system owning its own narrow event) — this architecture document ratifies that pattern project-wide rather than introducing a new bus abstraction.

| Event | Owner | Subscribers |
|---|---|---|
| `OnShiftStateChanged(shiftId, newState, zoneCenter, radius)` | Işık/Volume Durum Sistemi | Gece/Oturum Durumu, Anlatı Durum/İpucu Takibi, Adaptif Ses Sistemi |
| `OnTriggerFired(shiftId)` / `OnTriggerSettled(shiftId)` | Gece/Oturum Durumu | Anı-Tetikleyici Etkileşim (restore), Sahne Kesmeli Anlatı (saturation re-eval), Seviye/Sahne Geçişi (preload-eager signal) |
| `OnTransitionStateChanged(newState, type)` / `OnSoftTransitionRejected(reason)` | Seviye/Sahne Geçişi | Adaptif Ses Sistemi (HARD CUT sting filter) |
| `OnTaskListCompleted` / `OnFinalRoundStarted` / `OnFinalRoundItemPickedUp` | Görev/Taşıma Döngüsü | Sahne Kesmeli Anlatı |
| `OnClueKnown(clueId)` | Anlatı Durum/İpucu Takibi | (no MVP subscriber currently — Diyalog/Anlatı İçeriği polls `IsClueKnown` instead of subscribing, since callback selection is deliberately deferred to scene-active time, not event time — see Data Flow §3/TR-diyalog-002) |
| `MovementLockChanged` | Birinci Şahıs Kontrolcü | (available for any system that needs to react to lock state; no MVP system currently subscribes — all current consumers just read `IsLocked` synchronously before requesting) |

### 3. Save/Load Path — No Disk Persistence in MVP

This project's core mechanic (a service elevator swaps additive scenes mid-shift) means "persistence" here means **surviving a scene load within one play session**, not surviving an app restart. There is no save file, no `PlayerPrefs` use, no `JsonUtility` serialization anywhere in the MVP architecture — this is an explicit non-goal per `game-concept.md` (single continuous night, no save/resume mid-shift).

**The pattern used project-wide**: state that must outlive a scene swap lives in an **in-memory static/singleton C# service** — never a scene-local `MonoBehaviour`, never a `DontDestroyOnLoad` GameObject. A static field survives a scene unload for the lifetime of the process; that's the entire mechanism.

| Static service | Owns | Cleared when |
|---|---|---|
| Gece/Oturum Durumu | `IsSessionActive`, `FiredTriggerIds`, `PersistentShiftIds`, `SettledTriggerIds`, `CurrentRoundIndex`/`TotalRoundCount` | Never within MVP (one night = one process lifetime; app close discards everything) |
| Görev/Taşıma Döngüsü | Round/slot state, `CollectedItemIds`, `HasCarriedInFinalRound` | Never within MVP |
| Anlatı Durum/İpucu Takibi | `KnownClueIds`, `SeenShiftIds` | Never within MVP |
| Adaptif Ses Sistemi | `HeldSessionAlreadyPlayed` | Never within MVP |
| Diyalog/Anlatı İçeriği | `UsedCallbackIds` | Never within MVP — **known gap, not yet solved**: no cross-night persistence plan exists because MVP only has one night; this is explicitly owned by the future Çoklu Gece İlerlemesi (Vertical Slice) GDD, not this architecture pass |

**On-reload restore behavior** *(revised by ADR-0013, 2026-08-08 — closes QQ-07)*: when a scene loads (or reloads, e.g. after a HARD CUT) and a system finds relevant state already set in one of these static services (a fired memory trigger, a delivered item), it must query that state **before** self-registering into any per-scene collection (e.g. `InteractableRegistry`). The query lives at the **top of `OnEnable()`, before the `Register` call in the same body** — NOT in `Awake()`, as `TR-ani-tetik-002`/`TR-gorev-005` originally specified: under the Editor's independent "Reload Scene: Off" Enter Play Mode Setting, `Awake()` does not re-run on surviving scene objects across a Play Stop→Play boundary while `FoundationBootstrap.ResetAll()` *does* clear the restore-source state, so an `Awake()`-only query silently skips the re-check; `OnEnable` re-fires in that scenario (see ADR-0013's `CarryItemPickup` for the canonical shape — the "before registration" guarantee is preserved within the same `OnEnable` call). No cross-object Script Execution Order configuration is required — the ordering is within one method body. `ani-tetikleyici-etkilesim.md`'s Committed-restore should adopt the same shape when ADR-0014 is written.

### 4. Initialization Order

1. **Static services have no explicit boot step** — by design. `Gece/Oturum Durumu`, `InteractableRegistry`, `Anlatı Durum/İpucu Takibi` etc. initialize lazily on first access (static field default values / static constructor), not via a bootstrapper `MonoBehaviour`. `Anlatı Durum`'s subscription to `Işık/Volume.OnShiftStateChanged` happens at first-access time specifically to avoid depending on any particular scene's `Awake`/`OnEnable` order (`TR-anlati-006`).
2. **Within a single scene**, per-object `Awake()` → `OnEnable()` → `Start()` ordering is Unity's default guarantee and is sufficient for every same-object requirement in the TR baseline (see §3 above) — no custom Script Execution Order asset is required for MVP.
3. **`Seviye/Sahne Geçişi`'s `PreloadHardCut`** requires the target scene's `LoadSceneAsync` to reach 100% (not the `allowSceneActivation=false`/~90% pattern) before entering `Ready` — this guarantees every object's `Awake`/`Start` cost in the target scene is paid *before* the zero-frame `SetActiveScene` swap, which is the actual mechanism protecting the "0 fully-black frames" HARD CUT requirement (`TR-sahne-gecisi-004`/`006`).
4. **SOFT-transition co-residency** (both scenes loaded simultaneously, 0.5-2s window) is the one place two scenes' objects genuinely run `Update()` concurrently — `Işık/Volume` and `Adaptif Ses`'s `AmbientZoneVolume` both handle this explicitly by gating on `SceneManager.GetActiveScene()` match rather than assuming single-scene execution (`TR-isik-011`, `TR-ses-004`) — this is a cross-cutting concern any *new* Foundation-layer system with per-frame scene-aware logic must also account for, not just these two.

## API Boundaries

Pseudocode/C# contracts for every module boundary named in Phase 2. Engine-specific types are flagged with their risk level per the Engine Knowledge Gap Summary — none are post-cutoff/HIGH risk at the type-signature level (the risk in this project lives in *how* `Volume`/`Light` are driven, already resolved in Module Ownership, not in the types themselves).

### Foundation Layer

**`IPlayerState`** (Birinci Şahıs Kontrolcü)
```csharp
public interface IPlayerState {
    Transform EyeCamera { get; }              // read-only
    Vector3 Velocity { get; }
    bool IsGrounded { get; }
    bool MovementLocked { get; }
    bool IsCarrying { get; }
    bool IsLocked { get; }                     // pre-check before requesting a Hold
    event Action MovementLockChanged;
}
public MovementLockScope RequestMovementLock(object requester, MovementLockScope scope);
public void ReleaseMovementLock(object requester);
```
- *Invariants (caller must respect)*: `requester` must be a stable object identity (e.g. `this`) reused for the matching `Release` call — the lock is reference-counted per requester, not a global bool. Callers must check `IsLocked` before requesting a `Hold`-type interaction (soft-lock avoidance, `TR-etkilesim-007`).
- *Guarantees (module provides)*: effective lock scope is always the most restrictive of all active holders; `Release` is idempotent (releasing an already-released or never-held requester is a no-op, never throws).

**`IInteractable` / `InteractableRegistry`** (Foundation, relocated)
```csharp
public interface IInteractable {
    InteractionType Type { get; }              // Instant | Hold
    float HoldDuration { get; }
    bool CanInteract { get; }
    string PromptText { get; }
    bool SuppressDefaultHoldFill { get; }       // default false
    void OnFocusEnter(); void OnFocusExit();
    void OnInteract();                          // Instant only
    void OnHoldProgress(float t); void OnHoldComplete(); void OnHoldCancelled();
    void OnHoldBlocked();
}
public static class InteractableRegistry {
    public static void Register(IInteractable interactable);    // called from OnEnable
    public static void Deregister(IInteractable interactable);  // called from OnDisable
    public static IReadOnlyList<IInteractable> Snapshot();       // frame-start snapshot, not live collection
}
```
- *Invariants*: implementers register in `OnEnable`/deregister in `OnDisable`; any implementer restoring Committed/delivered state from a static service (Gece/Oturum Durumu, Görev/Taşıma) must do so at the **top of its own `OnEnable()`, before the `Register` call in the same body** (Data Flow §3, revised by ADR-0013 — the original `Awake()`-time rule breaks under "Reload Scene: Off", QQ-07) — a fired/delivered object must never appear in a post-restore snapshot; `Deregister` of a never-registered object is a safe no-op.
- *Guarantees*: `Snapshot()` is stable for the lifetime of one frame (never mutates mid-iteration, even if an object registers/deregisters that same frame).

**Işık/Volume Durum Sistemi**
```csharp
public bool TriggerShift(string shiftId, ShiftConfig config);  // true = new transition started,
                                                                 // false = shiftId already active (no-op)
public void RevertShift(string shiftId);        // silent no-op if shiftId not currently active
public bool IsShiftActive(string shiftId);
public bool IsShiftPersistent(string shiftId);   // returns the Persistent flag from shiftId's last TriggerShift call
public float GetStingerAudioRadius(string shiftId);  // returns config.StingerAudioRadius from shiftId's last TriggerShift call
public event Action<string, ShiftState, Vector3, float> OnShiftStateChanged;
```
*(Corrected in LP-FEASIBILITY review, 2026-08-05 — the first draft of this signature dropped the `ShiftConfig` parameter `TriggerShift` needs to carry `MemoryColor`/`Duration`/`Persistent`/`StingerAudioRadius` per-call, and printed `RevertShift` with an undocumented `bool` return instead of the Approved GDD's `void`. Both are now verified against `isik-volume-durum-sistemi.md`'s own "Sözleşme" section verbatim.)*
- ⚠️ `Volume` (`UnityEngine.Rendering.Volume` — URP-6.3-verified against `docs/engine-reference/unity/modules/rendering.md`), `Light` (Mixed mode) — stable API surface, the *behavior* risk (RenderGraph) is already resolved to not-applicable in Module Ownership.
- *Invariants*: `TriggerShift`/`RevertShift` are idempotent — calling `TriggerShift` on an already-active shift and `RevertShift` on an already-Dormant one are both safe no-ops (this is what makes Anı-Tetikleyici's "fire once" and this system's own reload-restore path safe without extra guards).
- *Guarantees*: `OnShiftStateChanged` fires exactly once per genuine state transition, including exactly once on reload-restore for an already-Persistent shift (Data Flow, `TR-isik-019`); light color and intensity are updated from the **same** `ShiftProgress` value every frame, never desynced.

**Gece/Oturum Durumu**
```csharp
public bool IsSessionActive { get; }
public int CurrentRoundIndex { get; }           // NEW, relocated from Görev/Taşıma
public int TotalRoundCount { get; }             // NEW, relocated from Görev/Taşıma
internal void SetRoundState(int index, int total);  // Feature-layer write, Görev/Taşıma only
public bool HasFired(string shiftId);            // FiredTriggerIds membership
public bool HasSettled(string shiftId);           // SettledTriggerIds membership
public bool IsPersistent(string shiftId);          // PersistentShiftIds membership
public void EndSession();                          // Sahne Kesmeli Anlatı only
public event Action<string> OnTriggerFired;
public event Action<string> OnTriggerSettled;
```
- *Invariants*: `EndSession()` is called exactly once per night, only by Sahne Kesmeli Anlatı's `onComplete` path — no other caller in the MVP dependency graph is authorized to call it (enforced by convention/code review, not a compiler guarantee — worth a control-manifest rule, see Phase 6).
- *Guarantees*: `FiredTriggerIds`/`PersistentShiftIds`/`SettledTriggerIds`/round counters are write-once-per-fact and never cleared within a session — any reader can treat a `true`/populated value as permanent for the rest of the night.

**Anlatı Durum/İpucu Takibi**
```csharp
public void MarkClueKnown(string clueId);
public bool IsClueKnown(string clueId);
public IReadOnlySet<string> GetKnownClueIds();
public event Action<string> OnClueKnown;
```
- *Invariants*: `ClueDefinition.requiredShiftIds` is never empty (edit-time validated) — callers may assume `IsClueKnown` can never spuriously return `true` for a clue with no requirements.
- *Guarantees*: no sequencing/timestamp data is ever exposed by this API — deliberate (Pillar 1/5), not an omission a future caller should try to work around.

**Seviye/Sahne Geçişi**
```csharp
public void PreloadHardCut(string toScene);      // background additive load, holds at Ready; re-call is no-op
public void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config,
                                   Action onComplete, Action<string> onFailed);
public void RequestHardCut(string toScene, HardCutConfig config,
                            Action onComplete, Action<string> onFailed);
public bool GetCurrentHardCutAbrupt();
public event Action<TransitionState, TransitionType> OnTransitionStateChanged;
public event Action<string> OnSoftTransitionRejected;
```
*(Corrected in LP-FEASIBILITY review, 2026-08-05 — the first draft dropped `PreloadHardCut` entirely, dropped `fromScene`/`toScene`, and dropped both `onComplete`/`onFailed` callbacks even though this document's own Invariants line below already described their behavior — an internal contradiction. It also substituted `SoftTransitionAnchor` [a scene-placed marker component in the target scene, not a caller-supplied parameter] for the actual `SoftTransitionConfig` parameter. Now verified against `seviye-sahne-gecisi.md`'s own "Dışa açılan arayüz" section verbatim.)*
- ⚠️ `SceneManager.LoadSceneAsync(mode: Additive)` / `SetActiveScene` — stable API, but the **zero-frame swap timing guarantee** (`TR-sahne-gecisi-004`) is a behavioral contract this module owns, not something the engine enforces for free.
- *Invariants*: `onComplete` and `onFailed` are mutually exclusive — exactly one fires per transition, never both, never neither. A `RequestHardCut` during an active SOFT is queued (single slot); a `RequestSoftTransition` during an active HARD CUT is rejected via `OnSoftTransitionRejected`, never queued — callers must not assume both request types share the same queueing behavior. Callers are responsible for their own movement-lock lifecycle around the callbacks (request lock → call → release lock in both `onComplete` and `onFailed`), this module does not manage locks on a caller's behalf.
- *Guarantees*: exactly 0 fully-black rendered frames during a HARD CUT swap; `onComplete`/`onFailed` callback exceptions never escape the manager (state machine always reaches `Idle`); `Failed` auto-transitions back to `Idle` immediately after `onFailed` fires.

### Core Layer

**Etkileşim Sistemi** — no public API beyond the `IInteractable` contract itself (Foundation). It is the primary *implementer* of Hold-state-machine behavior and the crosshair, but other systems interact with it only by implementing `IInteractable`, never by calling into Etkileşim directly.

**Asansör/Kat-Erişim Sistemi** — no public API. Self-contained; consumes Foundation contracts, exposes nothing (confirmed correctly "leaf" in the Phase 2 dependency diagram).

**Diyalog/Anlatı İçeriği** — no public API consumed elsewhere in MVP; scene-entry playback is a self-triggered hook, not a called entry point.

### Feature Layer

**Görev/Taşıma Döngüsü**
```csharp
public bool IsFinalRoundActive { get; }
public event Action OnTaskListCompleted;
public event Action OnFinalRoundStarted;
public event Action OnFinalRoundItemPickedUp;
```
- *Invariants*: this module is the **only** authorized writer to Gece/Oturum Durumu's `CurrentRoundIndex`/`TotalRoundCount` (via `SetRoundState`, internal-visibility) — Adaptif Ses and any other Foundation reader must never attempt to write these fields themselves.
- *Guarantees*: `OnFinalRoundItemPickedUp` fires exactly once per night, on first pickup while the final round is active (write-once pattern, same class as `FiredTriggerIds`).

**Anı-Tetikleyici Etkileşim**, **Sahne Kesmeli Anlatı** — no public API; both are pure orchestration consumers of the Foundation/Core contracts above, exposing nothing downstream (both correctly terminal in the Phase 2 dependency diagram).

## ADR Audit

### ADR Quality Check

*(Refreshed 2026-08-09, `/architecture-review` follow-up. History note: this section originally read "No ADRs exist yet … nothing to audit" — true when this document was written on 2026-08-05, stale after ADR-0001…0015 landed between 2026-08-05 and 2026-08-08.)*

All 15 Required ADRs exist and are **Accepted** (status flip 2026-08-09). Every ADR carries the full required section set, an Engine Compatibility table pinned to 6000.3.0f1, a dated unity-specialist validation record, and a TD-ADR review record. The `/architecture-review` 2026-08-09 pass found no blocking cross-ADR conflict; full findings (including the two Partial items below) live in `architecture-review-2026-08-09.md`.

### Traceability Coverage Check

*(Refreshed 2026-08-09 — the original all-GAP table from the pre-ADR pass is superseded by this one.)*

| System | TR-ID range | ADR Coverage | Status |
|---|---|---|---|
| In-memory static service pattern (cross-cutting) | `TR-oturum-001`, `TR-ani-tetik-002/003`, `TR-gorev-004/005`, `TR-anlati-001`, `TR-ses-008`, `TR-diyalog-004` | ADR-0001 | ✅ |
| Birinci Şahıs Kontrolcü | `TR-fpc-001..016` | ADR-0003 | ✅ |
| InteractableRegistry *(relocated)* | `TR-etkilesim-001/002`, `TR-fpc-004` | ADR-0004 | ✅ |
| Işık/Volume Durum Sistemi | `TR-isik-001..021` | ADR-0005 (+ 2026-08-09 facade addendum) | ✅ |
| Gece/Oturum Durumu | `TR-oturum-001..006` (+ relocated round counters, + `TotalConfiguredTriggerCountForNight` via ADR-0014) | ADR-0006 | ✅ |
| Anlatı Durum/İpucu Takibi | `TR-anlati-001..008` | ADR-0007 | ✅ |
| Seviye/Sahne Geçişi | `TR-sahne-gecisi-001..014` | ADR-0008 | ✅ |
| Adaptif Ses Sistemi | `TR-ses-001..017` | ADR-0009 — `TR-ses-016` (stinger caption) covered by the 2026-08-09 addendum (mechanism/timing), text+style routed to `/ux-design` per the GDD's own AC 14b | ✅ |
| UI Framework (crosshair/hold-fill/caption/subtitle) | `TR-etkilesim-006/009`, `TR-ses-016` | ADR-0002 | ✅ |
| Etkileşim Sistemi | `TR-etkilesim-003..008/010` | ADR-0010 (+ Focused-branch `CanInteract` re-poll revision via ADR-0014) | ✅ |
| Asansör/Kat-Erişim Sistemi | `TR-asansor-001..008` | ADR-0011 | ✅ |
| Diyalog/Anlatı İçeriği | `TR-diyalog-001..005` | ADR-0012 | ✅ |
| Görev/Taşıma Döngüsü | `TR-gorev-001..018` | ADR-0013 | ✅ |
| Anı-Tetikleyici Etkileşim | `TR-ani-tetik-001..010` | ADR-0014 | ✅ |
| Sahne Kesmeli Anlatı | `TR-sahne-kesme-001..009` | ADR-0015 | ✅ |

**Count: all modules covered at system level; 0 gaps.** Known residual (tracked in `traceability-index.md`): `tr-registry.yaml` is still unpopulated — the TR-IDs above live in narrative text only and must be extracted into the registry before `/create-stories` runs.

## Required ADRs

Grouped by urgency. Foundation-layer ADRs are listed first within "must have" so implementation can start there while Core/Feature ADRs are still being written — this mirrors `systems-index.md`'s own "Recommended Design Order" (Foundation batch 1 → Core batch 2 → Feature batch 3).

### Must have before coding starts (Foundation & Core decisions)

1. **`/architecture-decision "In-Memory Static Service Pattern for Session-Scoped State"`** — the shared idiom every other Foundation ADR below depends on: why static/singleton C# services (not `DontDestroyOnLoad`, not scene-local `MonoBehaviour`s) are this project's one persistence mechanism, and the `Awake()`-before-`OnEnable()` restore-ordering rule. **Must also resolve 2 gaps found in LP-FEASIBILITY review (2026-08-05)**: (a) Unity's "Disable Domain Reload" Editor setting (common for fast indie iteration) means static fields/static-constructor subscriptions do **not** reset between Play-mode sessions unless explicitly handled — this ADR must specify either a `[RuntimeInitializeOnLoadMethod]` reset hook per static service, or an explicit project rule that Domain Reload stays enabled; (b) `.claude/docs/coding-standards.md`'s BLOCKING unit-test requirement for state-machine logic conflicts with "dependency injection over singletons" when every state machine in this document is a static singleton — this ADR must specify a test-reset mechanism (e.g. an internal `ResetForTests()` per service, or a thin substitutable interface) or the Foundation layer's state machines cannot satisfy this project's own test-evidence gate. Covers: `TR-oturum-001`, `TR-ani-tetik-002/003`, `TR-gorev-004/005`, `TR-anlati-001`, `TR-ses-008`, `TR-diyalog-004`.
2. **`/architecture-decision "UI Framework: UI Toolkit"`** — cross-cutting, blocks UI work in 3 different systems (Etkileşim's crosshair, Adaptif Ses's stinger caption, Diyalog's subtitles). Covers: `TR-etkilesim-006/009`, `TR-ses-016`.
3. **`/architecture-decision "Player State and Movement Lock Architecture"`** — `IPlayerState` contract + reference-counted, scope-aware movement lock. Covers: `TR-fpc-001..013`.
4. **`/architecture-decision "InteractableRegistry Foundation Ownership"`** — records this session's Core→Foundation relocation and why (Module Ownership). Covers: `TR-etkilesim-001/002`, `TR-fpc-004`.
5. **`/architecture-decision "Işık/Volume Rendering Architecture — No Custom RenderGraph Pass"`** — records the Volume-Profile-weight-blend + script-driven-Light-lerp approach and the explicit decision that no `ScriptableRendererFeature` is needed. Covers: `TR-isik-001..021`.
6. **`/architecture-decision "Session State Service and Round-Counter Ownership"`** — `Gece/Oturum Durumu` as the canonical session-fact store, including this session's Feature→Foundation round-counter relocation and the write-authority rule (`Görev/Taşıma` writes, everyone else reads). Covers: `TR-oturum-002..006`.
7. **`/architecture-decision "Clue Tracking Architecture"`** — `Anlatı Durum/İpucu Takibi`'s `ClueDefinition` data model and event contract. Covers: `TR-anlati-001..008`.
8. **`/architecture-decision "Scene Transition State Machine (SOFT/HARD unified)"`** — `Seviye/Sahne Geçişi`'s single state machine, zero-frame HARD CUT swap mechanism, co-residency window handling. Covers: `TR-sahne-gecisi-001..014`.
9. **`/architecture-decision "Audio Architecture — Mixer Groups and Stinger Pooling"`** — `Adaptif Ses Sistemi`'s 4-mixer-group design, stinger pool state machine, no-dynamic-duck rule. Covers: `TR-ses-001..015, 017`.

### Should have before the relevant system is built (Core & Feature)

10. **`/architecture-decision "Interaction State Machine (Focus/Hold)"`** — `Etkileşim Sistemi`'s `SphereCast` focus detection + Hold state machine + default crosshair fill. Covers: `TR-etkilesim-003..008/010`.
11. **`/architecture-decision "Elevator State Machine"`** — `Asansör/Kat-Erişim Sistemi`'s cabin state machine and cosmetic-only movement. Covers: `TR-asansor-001..008`.
12. **`/architecture-decision "Dialogue Callback Selection Timing"`** — `Diyalog/Anlatı İçeriği`'s deferred-until-scene-active callback selection (the mechanism that prevents reproducing the saturation-timing bug). Covers: `TR-diyalog-001..005` (and flags the `UsedCallbackIds` cross-night persistence gap as explicitly out of scope for this ADR).
13. **`/architecture-decision "Carry Loop and Round State"`** — `Görev/Taşıma Döngüsü`'s task/round data model, pooled carry-slot visuals, and the round-counter write path into Gece/Oturum Durumu. Covers: `TR-gorev-001..018`.
14. **`/architecture-decision "Memory Trigger Orchestration"`** — `Anı-Tetikleyici Etkileşim`'s thin orchestration over Etkileşim/Işık-Volume, `Persistent=true` enforcement. Covers: `TR-ani-tetik-001..010`.
15. **`/architecture-decision "End-Condition Orchestration (Sahne Kesmeli Anlatı)"`** — the OR-logic, three-flag saturation gate, and tie-break rule. Covers: `TR-sahne-kesme-001..009`.

### Can defer to implementation

- Exact object-pool sizes (stinger pool count, carry-slot pool count) — these are tuning knobs already identified in their owning GDDs, not architectural decisions.
- `SphereCast` radius/range fine-tuning — already a locked GDD tuning knob (`etkilesim-sistemi.md`), no ADR needed.
- Material/shader specifics for the modular kit — owned by `design/art/art-bible.md` Section 8 (Asset Standards), not this document.
- **Boot Sequence** (added, TD-ADR review on ADR-0003, 2026-08-05; reconciled by ADR-0015, 2026-08-08) — the project now has **3** persistent boot scenes (UI: ADR-0002, Player: ADR-0003, Foundation: ADR-0008), so the original deferral trigger's first half ("a third persistent scene is added") has fired. Deferral still holds because the half that would give a Boot ADR real content has not: no same-`Awake()` cross-scene dependency exists, and sequential awaited loads (ADR-0003's provisional order, extended to Foundation) remain sufficient. ADR-0015 adds the one binding boot rule this project has: **the build's initial load set contains ONLY the persistent scenes; the first level scene (depot) loads exclusively via `SahneKesmeliAnlatiController`'s post-night-begin-setup call** — which is what guarantees ADR-0013/0014's setup-before-depot-activation constraint structurally. ADR-0015's boot flow is explicitly the minimal MVP fronting of the not-yet-designed "Ana Menü/Başlangıç Akışı" (Vertical Slice). A dedicated Boot Sequence ADR becomes warranted when that flow is designed, or if a genuine cross-scene `Awake()` dependency emerges.

## Architecture Principles

1. **Persistent state lives in in-memory static C# services, never `DontDestroyOnLoad` GameObjects or scene-local `MonoBehaviour`s.** This is the one mechanism that makes the depot↔ballroom elevator-transit scene swap work project-wide (Data Flow §3). Any new system needing to survive a scene load follows this same pattern — don't invent a second persistence mechanism.
2. **Cross-system communication is narrow, typed C# events owned by the producing module — never a shared event bus or mediator.** This was fought for repeatedly across this project's GDD design history (`systems-index.md`'s explicit rejection of a shared `MemoryTriggerEvent` God Object) and is now a standing architectural rule, not just a past decision. A new cross-system signal gets its own `event Action<...>` on the module that owns the fact, not a new case in a generic dispatcher.
3. **Layers only read downward. When a lower layer needs a fact a higher layer computes, the fact's *storage* moves down — the computing logic does not move.** Both cross-layer violations resolved this session (`InteractableRegistry`, round counters) followed this exact shape: the layer that needed to *read* got a Foundation-owned place to read from, while the layer that *computes* the value kept its computation and gained a narrow write path down. Apply this shape to any future layer-ordering violation before reaching for an exception.
4. **Public write APIs are idempotent by default.** `TriggerShift`/`RevertShift`, movement-lock release, and every "write-once" flag (`FiredTriggerIds`, `HasCarriedInFinalRound`, etc.) are safe to call redundantly. This isn't incidental — this project's narrative logic frequently re-derives the same fact from more than one triggering event (Data Flow §2's event table shows several facts with 2-3 independent triggering paths), so every write boundary must tolerate being hit twice.
5. **Gameplay values are data-driven ScriptableObjects, never hardcoded constants in behavior scripts** (`MemoryTriggerDef`, `CarryItemDef`, `ClueDefinition`, `ShiftConfig`, `DialogueSceneConfig`) — already a project-wide coding standard (`.claude/docs/coding-standards.md`), restated here because every module ownership table in this document assumes it: "owns config" and "owns runtime state" are always two separate objects, never fields on the same MonoBehaviour.

## Open Questions

| ID | Summary | Priority | Resolution Path |
|----|---------|----------|-----------------|
| QQ-01 | `Diyalog/Anlatı İçeriği`'s `UsedCallbackIds` has no cross-night persistence plan | Low (doesn't block MVP — one night only) | Owned by the future Çoklu Gece İlerlemesi (Vertical Slice) GDD, not this architecture pass |
| QQ-02 | Exact pool sizes (stinger `AudioSource` pool, carry-slot visual pool) not yet numerically fixed | Low | Defer to implementation — these are tuning knobs, not architectural decisions (Required ADRs, "Can defer") |
| QQ-03 | `Gece/Oturum Durumu.EndSession()`'s "only Sahne Kesmeli Anlatı may call this" rule AND `SetRoundState()`'s "only Görev/Taşıma may call this" rule (API Boundaries) are both currently convention-only — `internal` visibility alone does **not** enforce single-caller restriction in a default single-assembly Unity project (any class in `Assembly-CSharp` can call an `internal` member, not just the intended caller) | Medium (self-review finding, TD-ARCHITECTURE 2026-08-05) | Decide during `/create-control-manifest` whether splitting into a dedicated assembly definition + `InternalsVisibleTo` is worth the friction for these 2 specific single-caller methods, or whether a control-manifest rule + code review is sufficient for a solo/small-team project — do not assume `internal` alone solves this |
| QQ-04 | Psychiatrist NPC / friend-character visual-and-technical representation (raised independently in `design/art/art-bible.md` Section 5's Open Questions) has no architectural footprint yet — no Vertical Slice character GDD exists | Low (Vertical Slice scope, not MVP) | Deferred to the future character GDD + its own architecture addendum, consistent with the art bible's own deferral |
| QQ-05 | Domain Reload dependency: static-service state (Foundation layer) does not reset between Editor Play-mode sessions if "Disable Domain Reload" is enabled — flagged by LP-FEASIBILITY (2026-08-05) | High (silently reproduces the exact class of state-corruption bug this architecture was built to prevent, if hit) | Must be resolved in Required ADR #1 before any Foundation-layer static service is implemented |
| QQ-06 | Testability conflict: `coding-standards.md`'s BLOCKING unit-test requirement for state machines vs. this document's static-singleton pattern for every Foundation state machine — flagged by LP-FEASIBILITY (2026-08-05) | High (blocks satisfying this project's own test-evidence gate for Foundation-layer stories) | Must be resolved in Required ADR #1 — a reset/substitution mechanism, decided before Foundation implementation starts |
| QQ-07 | ~~Data Flow §3's `Awake()`-time restore breaks under "Reload Scene: Off"~~ **RESOLVED by ADR-0013 (2026-08-08)**: the restore query moved to the top of `OnEnable()`, before the `Register` call in the same body (exactly the fix ADR-0001's Risks section anticipated) — Data Flow §3 and the `IInteractable` invariant above are updated; `gorev-tasima-dongusu.md`'s Kalıcılık rule synced. Residual DISCHARGED by ADR-0014 (2026-08-08): `MemoryTriggerObject` adopts the `OnEnable`-top shape (stay-visible variant — no `SetActive(false)`, just skip `Register`), and `ani-tetikleyici-etkilesim.md`/`gece-oturum-durumu-2026-08-02.md`'s `Awake()` wordings are synced | Closed | Both adopters now on record (ADR-0013: deactivate-entirely; ADR-0014: stay-visible) |
