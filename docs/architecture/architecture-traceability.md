# Architecture Traceability Index

Last Updated: 2026-08-05
Engine: Unity 6.3 LTS (6000.3.0f1)

## Coverage Summary

- Total requirements: 374
- Covered: 0 (0%)
- Partial: 0
- Gaps: 374 (100%)

No ADRs exist yet. Every requirement below is a ❌ Gap. See
`docs/architecture/architecture-review-2026-08-05.md` for the full review
report, verdict, and recommended ADR authoring order.

---

## Full Matrix

Organized by dependency layer (Foundation → Core → Feature), matching
`design/gdd/systems-index.md`'s Dependency Map. All ADR Coverage columns read
"—" and Status reads "❌ GAP" throughout this review pass.

### Foundation Layer

#### Seviye/Sahne Geçişi — Scene Transition (`scene`)
Source: `design/gdd/seviye-sahne-gecisi.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-scene-001 | Both SOFT and HARD CUT transitions use a single shared code path on Unity additive scene loading via one `SceneTransitionManager`, differentiated only by config data | Architecture | — | ❌ GAP |
| TR-scene-002 | SOFT transitions keep source and target scenes simultaneously resident; masking is diegetic, not fade-to-black UI | Gameplay | — | ❌ GAP |
| TR-scene-003 | `Preloading`→`Ready` always waits for genuine `LoadSceneAsync` completion, independent of the SOFT minimum-duration tuning knob | Architecture | — | ❌ GAP |
| TR-scene-004 | HARD CUT preload externally triggered via `PreloadHardCut(toScene)`; `RequestHardCut` swaps within the same frame — zero fade, 0 black frames | Rendering | — | ❌ GAP |
| TR-scene-005 | `Swapping` state is solely `SceneManager.SetActiveScene(toScene)`; `UnloadSceneAsync` deferred to after `Complete` | Architecture | — | ❌ GAP |
| TR-scene-006 | `SWAP_FRAME_EPSILON = 1 frame (≤16.6ms)` tolerance constant; 0 black frames is an epsilon-free invariant | Rendering | — | ❌ GAP |
| TR-scene-007 | `PreloadHardCut` waits for 100% `LoadSceneAsync` completion with `allowSceneActivation=true` — not the ~90% hold pattern | Architecture | — | ❌ GAP |
| TR-scene-008 | System never calls `RequestMovementLock`/`ReleaseMovementLock` — lock ownership belongs to the calling system | Architecture | — | ❌ GAP |
| TR-scene-009 | Static batching does not merge across additively loaded scenes — draw call budget allocated per-scene | Rendering | — | ❌ GAP |
| TR-scene-010 | Unity 6.3 RenderGraph API changes may affect multi-scene camera stacking/lighting — technical spike flagged as needed | Rendering | — | ❌ GAP |
| TR-scene-011 | Baked `LightmapData` stays per-scene, never merged across additively loaded scenes | Rendering | — | ❌ GAP |
| TR-scene-012 | Each area scene carries its own URP Volume profile; `RenderSettings` synced via `SceneEnvironmentSettings` component on `SetActiveScene` | Rendering | — | ❌ GAP |
| TR-scene-013 | State machine: `Idle`→`Preloading`→`Ready`→`Swapping`(zero-frame)→`Complete`→`Idle` | Architecture | — | ❌ GAP |
| TR-scene-014 | `PreloadHardCut` pre-advances to `Ready`/holds; `RequestHardCut` triggers only the zero-frame `Swapping` step | Architecture | — | ❌ GAP |
| TR-scene-015 | Terminal `Failed` state for load failure; `onComplete`/`onFailed` invoked exactly once | Architecture | — | ❌ GAP |
| TR-scene-016 | `Failed` auto-transitions to `Idle` immediately after `onFailed` fires | Architecture | — | ❌ GAP |
| TR-scene-017 | `RequestHardCut` during an active SOFT transition queues into one pending slot, auto-fires when SOFT reaches `Idle` | Architecture | — | ❌ GAP |
| TR-scene-018 | Public API: `void PreloadHardCut(string toScene)` | Architecture | — | ❌ GAP |
| TR-scene-019 | Public API: `void RequestSoftTransition(fromScene, toScene, config, onComplete, onFailed)` | Architecture | — | ❌ GAP |
| TR-scene-020 | Public API: `void RequestHardCut(toScene, config, onComplete, onFailed)` | Architecture | — | ❌ GAP |
| TR-scene-021 | `HardCutConfig.Abrupt` (bool) carried but not interpreted by this system — only listeners branch on it | Architecture | — | ❌ GAP |
| TR-scene-022 | `bool GetCurrentHardCutAbrupt()` synchronous query; undefined if none preloaded/active | Architecture | — | ❌ GAP |
| TR-scene-023 | `onComplete`/`onFailed` mutually exclusive, exactly one fires once | Architecture | — | ❌ GAP |
| TR-scene-024 | `TransitionState CurrentState` and `enum TransitionType { Soft, Hard }` exposed | Architecture | — | ❌ GAP |
| TR-scene-025 | `event OnTransitionStateChanged(TransitionState, TransitionType)` — type param disambiguates SOFT/HARD `Swapping` | Architecture | — | ❌ GAP |
| TR-scene-026 | `event OnSoftTransitionRejected(string reason)` fires on every rejection with cause string | Architecture | — | ❌ GAP |
| TR-scene-027 | `PreloadHardCut` progress tracked in internal `_hardCutPreloadState`, independent of public `CurrentState` | Architecture | — | ❌ GAP |
| TR-scene-028 | Same-type repeat request while active is rejected as no-op with logged warning — one active-transition slot per type | Architecture | — | ❌ GAP |
| TR-scene-029 | `PreloadHardCut(sceneB)` during active `PreloadHardCut(sceneA)` is rejected/no-op, not queued | Architecture | — | ❌ GAP |
| TR-scene-030 | `RequestHardCut` with no matching `Ready` preload falls back to synchronous-wait, doesn't redirect | Architecture | — | ❌ GAP |
| TR-scene-031 | Exceptions inside caller's `onComplete` caught internally and logged, never leak; state machine still proceeds | Architecture | — | ❌ GAP |
| TR-scene-032 | SOFT request during active HARD CUT is rejected outright and NOT queued (asymmetric vs. HARD-during-SOFT) | Architecture | — | ❌ GAP |
| TR-scene-033 | Coordinate-frame contract: `SoftTransitionAnchor` marker shares local offset/orientation with source cabin spawn point | Gameplay | — | ❌ GAP |
| TR-scene-034 | Player-position transfer is an instantaneous Transform copy per TR-scene-033, not a blend | Architecture | — | ❌ GAP |
| TR-scene-035 | Tuning knob — SOFT transition minimum duration: 2-8s | Gameplay | — | ❌ GAP |
| TR-scene-036 | Tuning knob — old-scene unload delay: 0.5-2s post-swap | Persistence | — | ❌ GAP |
| TR-scene-037 | Tuning knob — HARD CUT preload window: 1-3s ahead of tension curve peak | Gameplay | — | ❌ GAP |
| TR-scene-038 | Cross-system contract: Adaptive Audio subscribes to `OnTransitionStateChanged`, fires HARD CUT sting only on `Swapping && Hard` | Audio | — | ❌ GAP |
| TR-scene-039 | Cross-system contract: Elevator handles `onFailed` identically to `OnSoftTransitionRejected` — returns to origin floor, releases lock | Gameplay | — | ❌ GAP |

#### Birinci Şahıs Kontrolcü — First-Person Controller (`fpc`)
Source: `design/gdd/birinci-sahis-kontrolcu.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-fpc-001 | Movement uses CharacterController capsule collision; step offset/skin width tuned to 4m×4m modular grid | Physics | — | ❌ GAP |
| TR-fpc-002 | Walk speed 1.6 m/s unloaded, 1.35 m/s carrying (via `SetCarrying(bool)`); no sprint | Gameplay | — | ❌ GAP |
| TR-fpc-003 | Acceleration/deceleration ramps over ~0.15-0.25s via exponential smoothing, not instant velocity change | Physics | — | ❌ GAP |
| TR-fpc-004 | Approach-slow taper: flagged `IInteractable` within 1.5m reduces target speed to 70% at d=0, smoothstep-eased; camera shake unaffected | Gameplay | — | ❌ GAP |
| TR-fpc-005 | Formula 1 (Speed Smoothing): `v(t+Δt) = v_target + (v(t)-v_target)×e^(-k·Δt)`, k=3/T_ramp, analytic solution | Physics | — | ❌ GAP |
| TR-fpc-006 | Formula 2 (Approach-Slow Taper): `x=clamp(d/1.5,0,1); ease(x)=x²(3-2x); TaperMult=0.7+0.3×ease(x)`; d sourced from InteractableRegistry | Gameplay | — | ❌ GAP |
| TR-fpc-007 | Formula 3 (Head-Bob Amplitude): `Amplitude(v,S)=A_max×(v/1.6)×(S/100)`; shared stride-phase accumulator with footstep audio | Rendering | — | ❌ GAP |
| TR-fpc-008 | Head-bob accessibility slider (0-100%, default ~40%) plus full disable option | UI | — | ❌ GAP |
| TR-fpc-009 | Camera pitch clamped ±80°, yaw unbounded; no FOV kick/roll/post-process camera effects owned here | Rendering | — | ❌ GAP |
| TR-fpc-010 | FOV default 75-80°, adjustable 60-100° | UI | — | ❌ GAP |
| TR-fpc-011 | Mouse look sensitivity slider + invert-Y toggle | Input | — | ❌ GAP |
| TR-fpc-012 | State machine: Idle↔Walking→Carrying→Locked; player input can never break a movement lock | Architecture | — | ❌ GAP |
| TR-fpc-013 | Exposes `IPlayerState` (read-only): EyeCamera, Velocity, IsGrounded, MovementLocked, IsCarrying, IsLocked, MovementLockChanged event; CharacterController not exposed directly | Architecture | — | ❌ GAP |
| TR-fpc-014 | `RequestMovementLock(requester, scope)`/`ReleaseMovementLock(requester)` — reference-counted via `HashSet<object>`, not a bool | Architecture | — | ❌ GAP |
| TR-fpc-015 | `MovementLockScope { Full, MoveOnly }`; most-restrictive-wins when multiple locks active | Architecture | — | ❌ GAP |
| TR-fpc-016 | `IsLocked` read-only bool reports whether ANY requester holds the lock, distinct from ownership | Architecture | — | ❌ GAP |
| TR-fpc-017 | `SetCarrying(bool)` accepted only from Task/Carrying Loop | Gameplay | — | ❌ GAP |
| TR-fpc-018 | Interaction System raycasts via this system's EyeCamera; one-directional dependency (FPC never reaches into Interaction) | Architecture | — | ❌ GAP |
| TR-fpc-019 | Single "Gameplay" Input Actions map: Move, Look, Interact; gamepad secondary binding; no remap UI for MVP | Input | — | ❌ GAP |
| TR-fpc-020 | On `RequestMovementLock` mid-walk, v_target pulled to 0 but Formula 1 still governs deceleration curve | Physics | — | ❌ GAP |
| TR-fpc-021 | Multiple flagged objects in taper radius: d = minimum distance across all; TaperMult continuous across nearest-object transitions | Gameplay | — | ❌ GAP |
| TR-fpc-022 | No Δt clamping — abnormally large Δt makes v(t) jump directly to v_target next frame | Physics | — | ❌ GAP |
| TR-fpc-023 | CharacterController step offset kept below kit's smallest threshold height (~2cm); taller thresholds modeled as slope ramps | Physics | — | ❌ GAP |
| TR-fpc-024 | Skin width ~10% of capsule radius; door-frame clear width ≥ (capsule diameter + 2×skin width), tuned together | Physics | — | ❌ GAP |
| TR-fpc-025 | Convex 90° wall-junction corners chamfered by skin-width amount, or frictionless physics material, to prevent snagging | Physics | — | ❌ GAP |
| TR-fpc-026 | `OnControllerColliderHit` early-exits unless hit layer is in an explicit interest mask | Physics | — | ❌ GAP |
| TR-fpc-027 | Elevator only calls `RequestMovementLock(this, MoveOnly)`/`Release...` — no platform-delta injection needed | Architecture | — | ❌ GAP |
| TR-fpc-028 | Footstep audio: single generic material, random pitch ±5%, 4-6 samples; volume scales with v(t) | Audio | — | ❌ GAP |
| TR-fpc-029 | Breathing/effort loop tied to IsCarrying, fades ~1s on start/stop; not tied to approach-slow taper | Audio | — | ❌ GAP |
| TR-fpc-030 | Edit-time content validator fails build if no decoy `IInteractable` registered per MVP scene | Architecture | — | ❌ GAP |
| TR-fpc-031 | Cross-layer read dependency: Formula 2's `d` reads InteractableRegistry (owned by Interaction System) — open architectural question | Architecture | — | ❌ GAP |
| TR-fpc-032 | Light/Volume State reads this system's `PlayerMaxSpeed` (1.6 m/s) constant read-only for R_trigger calculation | Architecture | — | ❌ GAP |
| TR-fpc-033 | Cinematic Cutscene calls `RequestMovementLock(this, Full)`/`Release...` immediately before a HARD CUT | Architecture | — | ❌ GAP |
| TR-fpc-034 | Automated unit tests with mock Δt required to validate smoothing function convergence bounds | Architecture | — | ❌ GAP |

#### Gece/Oturum Durumu — Night/Session State (`session`)
Source: `design/quick-specs/gece-oturum-durumu-2026-08-02.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-session-001 | In-memory singleton service — survives scene loads, no disk persistence in MVP | Persistence | — | ❌ GAP |
| TR-session-002 | Holds `bool IsSessionActive` — true at night start, false when psychiatry cutscene begins | Persistence | — | ❌ GAP |
| TR-session-003 | Holds `int CurrentNightNumber` — fixed to 1 for MVP | Persistence | — | ❌ GAP |
| TR-session-004 | Holds `HashSet<string> FiredTriggerIds` — fired-only tracking, distinct from narrative "known" state | Persistence | — | ❌ GAP |
| TR-session-005 | Holds `Dictionary<string,bool> PersistentShiftIds` — recorded when a shift becomes Held-Persistent | Persistence | — | ❌ GAP |
| TR-session-006 | Exposes `IsSessionActive` read-only for Elevator; makes no availability decision itself | Architecture | — | ❌ GAP |
| TR-session-007 | In-memory dictionary sufficient for `PersistentShiftIds` — no serialization required in MVP | Persistence | — | ❌ GAP |
| TR-session-008 | Subscribes to Işık/Volume's `OnShiftStateChanged(shiftId, newState, zoneCenter, radius)` | Architecture | — | ❌ GAP |
| TR-session-009 | On `Shifting-In`, calls `IsShiftPersistent(shiftId)` synchronously same frame; records `PersistentShiftIds` immediately, doesn't wait for `Held` | Architecture | — | ❌ GAP |
| TR-session-010 | `OnShiftStateChanged` carries no `Persistent` field — hence the mandatory separate `IsShiftPersistent` query | Architecture | — | ❌ GAP |
| TR-session-011 | `void EndSession()` sets `IsSessionActive=false`; called only by Sahne Kesmeli Anlatı's `onComplete`; no reverse transition in MVP | Architecture | — | ❌ GAP |
| TR-session-012 | `event Action<string> OnTriggerFired` fires same frame `shiftId` added to `FiredTriggerIds`, on entry to `Shifting-In` | Architecture | — | ❌ GAP |
| TR-session-013 | Holds `HashSet<string> SettledTriggerIds` + `event OnTriggerSettled`, populated on `Held` for an already-fired `shiftId` | Architecture | — | ❌ GAP |
| TR-session-014 | Night-end saturation must check `SettledTriggerIds.Count`, never `FiredTriggerIds.Count` (fixes payoff-cutoff defect) | Gameplay | — | ❌ GAP |
| TR-session-015 | `SettledTriggerIds.Count < FiredTriggerIds.Count` is expected transient state during the ~3s ramp window | Architecture | — | ❌ GAP |
| TR-session-016 | Depended on by: Anı-Tetikleyici, Işık/Volume, Asansör, Görev/Taşıma, Sahne Kesmeli — full dependency list | Architecture | — | ❌ GAP |
| TR-session-017 | All state resets to zero on game restart — no disk load in MVP | Persistence | — | ❌ GAP |

#### Anlatı Durum/İpucu Takibi — Narrative State/Clue Tracking (`narrative`)
Source: `design/gdd/anlati-durum-ipucu-takibi.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-narrative-001 | Core "known clue" state: `HashSet<string> KnownClueIds`, no ordering/timestamp tracked | Architecture | — | ❌ GAP |
| TR-narrative-002 | `ClueDefinition { clueId, requiredShiftIds[] }` — single project-level registry, not per-scene copies | Architecture | — | ❌ GAP |
| TR-narrative-003 | clueId→requiredShiftIds is N:1 with ALL semantics — Known only when `SeenShiftIds ⊇ requiredShiftIds` | Gameplay | — | ❌ GAP |
| TR-narrative-004 | Edit-time validation blocks build for any `ClueDefinition` with empty `requiredShiftIds` (prevents vacuous-truth bug) | Architecture | — | ❌ GAP |
| TR-narrative-005 | MVP: every list holds exactly 1 element, but schema must support N-element lists for Full Vision | Architecture | — | ❌ GAP |
| TR-narrative-006 | `HashSet<string> SeenShiftIds` distinct/independent from Gece/Oturum's `FiredTriggerIds` — no cross-querying | Architecture | — | ❌ GAP |
| TR-narrative-007 | `MarkClueKnown(clueId)` idempotent — no-op and no re-fire if already known | Architecture | — | ❌ GAP |
| TR-narrative-008 | Static/singleton scene-independent plain C# service, explicitly NOT DontDestroyOnLoad; no disk persistence in MVP | Architecture | — | ❌ GAP |
| TR-narrative-009 | `ClueDefinition` records loaded from a single project-level registry, not per-scene duplicated assets | Persistence | — | ❌ GAP |
| TR-narrative-010 | Edit-time validation rejects build on duplicate `clueId` across two `ClueDefinition` records | Architecture | — | ❌ GAP |
| TR-narrative-011 | Must subscribe to `OnShiftStateChanged` at static-constructor/bootstrap time, not scene-local `Awake`/`OnEnable` | Architecture | — | ❌ GAP |
| TR-narrative-012 | State machine per clueId strictly one-way Unknown→Known, non-reversible, no intermediate state exposed | Gameplay | — | ❌ GAP |
| TR-narrative-013 | API: `MarkClueKnown`, `IsClueKnown` (never throws), `GetKnownClueIds` (empty-but-never-null) | Architecture | — | ❌ GAP |
| TR-narrative-014 | `event OnClueKnown(clueId)` fires exactly once, only on Unknown→Known transition | Architecture | — | ❌ GAP |
| TR-narrative-015 | RETRACTED: Sahne Kesmeli Anlatı no longer subscribes to `OnClueKnown` as of 2026-08-03 | Architecture | — | ❌ GAP |
| TR-narrative-016 | Subscribes to `OnShiftStateChanged`, processes only `Held` transitions; `zoneCenter`/`radius` unused | Gameplay | — | ❌ GAP |
| TR-narrative-017 | Held-transition handler: add to `SeenShiftIds` → find matching `ClueDefinition`s → `MarkClueKnown` if containment satisfied | Gameplay | — | ❌ GAP |
| TR-narrative-018 | Must tolerate duplicate `Held` re-fires for `Persistent=true` shifts without error or duplicate `OnClueKnown` | Architecture | — | ❌ GAP |
| TR-narrative-019 | Cross-night persistence deferred to Çoklu Gece İlerlemesi; data model (2 flat HashSets) already serialization-suitable | Persistence | — | ❌ GAP |
| TR-narrative-020 | `ClueConsistencyValidator.ValidateScene(sceneId)` on scene load — logs orphaned (clueId, shiftId) pairs, non-blocking | Architecture | — | ❌ GAP |
| TR-narrative-021 | No runtime clamping for empty `requiredShiftIds` — caught only by edit-time validation | Architecture | — | ❌ GAP |
| TR-narrative-022 | Multiple `ClueDefinition`s may share a `shiftId`; single Held handler run may complete 0, 1, or multiple clues | Gameplay | — | ❌ GAP |
| TR-narrative-023 | `MarkClueKnown` called directly performs no validation against prerequisites — unconditional mark, indistinguishable from earned | Gameplay | — | ❌ GAP |
| TR-narrative-024 | Duplicate shiftId entries within one `requiredShiftIds` list have no functional effect (HashSet dedup) | Architecture | — | ❌ GAP |
| TR-narrative-025 | No ordering/timestamp recorded or exposed anywhere in the API — intentional design constraint (Pillar 1/5) | Architecture | — | ❌ GAP |
| TR-narrative-026 | Structural ownership boundary vs. Gece/Oturum Durumu — independent state halves, must not query each other's data | Architecture | — | ❌ GAP |
| TR-narrative-027 | Late-subscriber gap: events not replayed — late subscribers must reconcile via `GetKnownClueIds`/`IsClueKnown` at init | Architecture | — | ❌ GAP |
| TR-narrative-028 | Query contract for Diyalog/Anlatı İçeriği & Plot Twist: queryable at scene start independent of event timing | Architecture | — | ❌ GAP |

#### Işık/Volume Durum Sistemi — Lighting/Volume State (`lighting`)
Source: `design/gdd/isik-volume-durum-sistemi.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-lighting-001 | Each trigger zone has an independent `Volume` (isGlobal=false); all zones share one Volume Profile asset; multiple zones may be Shifted simultaneously | Rendering | — | ❌ GAP |
| TR-lighting-002 | Zone box collider decoupled from R_trigger/R_exit — separate code-based distance check, sampled once per tick | Architecture | — | ❌ GAP |
| TR-lighting-003 | `zoneCenter` required Vector3; defaults to box collider's world-space bounds.center via Awake/OnValidate | Architecture | — | ❌ GAP |
| TR-lighting-004 | `Volume.weight` set directly to `ShiftProgress` every frame by the zone's ticker, sole writer — no Animator/Timeline may key it | Rendering | — | ❌ GAP |
| TR-lighting-005 | Box collider sized ≥ `BoxHalfExtentMin`, `blendDistance=0`, for pop-free Shifting-Out | Rendering/Physics | — | ❌ GAP |
| TR-lighting-006 | "Tick" = per-zone position-sampling coroutine, default once/frame at 60fps; may be throttled | Architecture | — | ❌ GAP |
| TR-lighting-007 | Minimum safe `R_trigger = PlayerMaxSpeed × tick interval` to guarantee no zone skip | Gameplay | — | ❌ GAP |
| TR-lighting-008 | Ticker skips position sampling when zone's scene ≠ active scene, preventing spurious co-residency triggers | Architecture | — | ❌ GAP |
| TR-lighting-009 | Scene-mismatch skip applies only to position checks; time-based `x` progress keeps advancing regardless | Architecture | — | ❌ GAP |
| TR-lighting-010 | State reversion (Held→Shifting-Out) purely radius/hysteresis-based, no fixed timer | Gameplay | — | ❌ GAP |
| TR-lighting-011 | Each zone has Inspector-assigned `string shiftId`, key for `TriggerShift`/`RevertShift`/`IsShiftActive` and edit-time matching | Architecture | — | ❌ GAP |
| TR-lighting-012 | Each zone has `TriggerMode { Automatic, ManualOnly }` | Gameplay/Architecture | — | ❌ GAP |
| TR-lighting-013 | Edit-time validation fails build if any `MemoryTriggerDef`-linked zone has `TriggerMode != ManualOnly` | Architecture | — | ❌ GAP |
| TR-lighting-014 | ≥1 MVP zone must be `Automatic`, `Persistent=false`, on mandatory route, not linked to any clue | Gameplay | — | ❌ GAP |
| TR-lighting-015 | Build-time check fails if zero `Automatic` zones found in MVP scenes | Architecture | — | ❌ GAP |
| TR-lighting-016 | `ShiftConfig.Persistent` bool — true skips `Shifting-Out`, remains `Shifted` rest of session, never evaluates exit radius | Persistence | — | ❌ GAP |
| TR-lighting-017 | Lighting-state change is post-process + per-light color/intensity lerp only; baked-lightmap-set swapping explicitly rejected | Rendering | — | ❌ GAP |
| TR-lighting-018 | Every `Light` in a `ShiftConfig`'s array must be Light Mode = Mixed, never Baked | Rendering | — | ❌ GAP |
| TR-lighting-019 | Real-time shadow-casting lights budgeted 2-3 per room | Rendering/Performance | — | ❌ GAP |
| TR-lighting-020 | Overlapping trigger zones must not share lights in their light arrays (unenforced by tooling) | Gameplay | — | ❌ GAP |
| TR-lighting-021 | Interpolation driven by one lightweight per-zone coroutine iterating a light-tuple array — not per-light `Update()` | Architecture/Performance | — | ❌ GAP |
| TR-lighting-022 | State machine: `Dormant`→`Shifting-In`(~3s)→`Shifted/Held`→`Shifting-Out`(~3s)→`Dormant`; `Persistent` skips Shifting-Out | Architecture | — | ❌ GAP |
| TR-lighting-023 | API: `bool TriggerShift(shiftId, config)` — true if new transition started, false no-op if already active | Architecture | — | ❌ GAP |
| TR-lighting-024 | API: `void RevertShift(shiftId)` — silent no-op if not active | Architecture | — | ❌ GAP |
| TR-lighting-025 | API: `bool IsShiftActive(shiftId)` — true for Shifting-In/Held/Shifting-Out, only Dormant is inactive | Architecture | — | ❌ GAP |
| TR-lighting-026 | API: `bool IsShiftPersistent(shiftId)` — must be queried same frame as `TriggerShift` | Architecture/Persistence | — | ❌ GAP |
| TR-lighting-027 | `event OnShiftStateChanged(shiftId, newState, zoneCenter, radius)` — this system never calls audio directly | Architecture | — | ❌ GAP |
| TR-lighting-028 | `ShiftConfig` schema: target White Balance/Color values, `Duration`, `Persistent`, `StingerAudioRadius` (default 0) | Architecture | — | ❌ GAP |
| TR-lighting-029 | API: `float GetStingerAudioRadius(shiftId)` | Architecture/Audio | — | ❌ GAP |
| TR-lighting-030 | Edit-time validation enforces `StingerAudioRadius > 0` for `MemoryTriggerDef`-linked zones | Architecture | — | ❌ GAP |
| TR-lighting-031 | No reference counting on `shiftId` — one `RevertShift` fully reverts regardless of caller count | Architecture | — | ❌ GAP |
| TR-lighting-032 | `ShiftProgress = 3x²-2x³`, `x=clamp(ElapsedTime/Duration,0,1)` — smoothstep ease | Gameplay | — | ❌ GAP |
| TR-lighting-033 | Running state tracked as `x` itself, never `ElapsedTime`/`ShiftProgress` alone; interrupts flip sign of per-tick delta | Architecture | — | ❌ GAP |
| TR-lighting-034 | Guard-rail epsilons: `TIME_EPSILON=0.01s`, `RADIUS_EPSILON=0.01m`, `HYSTERESIS_EPSILON=0.001` | Architecture | — | ❌ GAP |
| TR-lighting-035 | `Duration` clamped ≥ `TIME_EPSILON`, never 0 | Gameplay | — | ❌ GAP |
| TR-lighting-036 | `k_hysteresis` clamped ≥ `1.0+HYSTERESIS_EPSILON` in code | Gameplay | — | ❌ GAP |
| TR-lighting-037 | `MemoryIntensityMultiplier` clamped to `[0.0,1.0)` in code | Gameplay | — | ❌ GAP |
| TR-lighting-038 | `R_trigger` clamped ≥ `RADIUS_EPSILON`, never ≤0 | Gameplay | — | ❌ GAP |
| TR-lighting-039 | Hysteresis Radius: `R_exit = R_trigger × k_hysteresis`, default 1.15, range 1.05-1.3 | Gameplay | — | ❌ GAP |
| TR-lighting-040 | Box Collider Safety Margin: `BoxHalfExtentMin = R_exit + (PlayerMaxSpeed × Duration) + SafetyBuffer` (≥0.5m, default 0.9m) | Physics | — | ❌ GAP |
| TR-lighting-041 | Light Color/Intensity Blend: `LightColor=Lerp(Base,Memory,ShiftProgress)`, `LightIntensity=BaseIntensity×Lerp(1,MemoryIntensityMultiplier,ShiftProgress)` | Rendering | — | ❌ GAP |
| TR-lighting-042 | Discontinuous-exit edge case (elevator teleport): no event fires at exit moment, next tick outside R_exit starts Shifting-Out | Gameplay/Architecture | — | ❌ GAP |
| TR-lighting-043 | Session-restore: on load, prior `Persistent` shift initializes directly to `Shifted/Held-Persistent` (ShiftProgress=1), fires event once | Persistence | — | ❌ GAP |
| TR-lighting-044 | Dependency on Gece/Oturum Durumu: reads `PersistentShiftIds` at scene load (read-only); session writes it via event subscription | Persistence | — | ❌ GAP |
| TR-lighting-045 | Locked Volume Profile values: WB Temp -60, Tint +10, Post Exposure -0.5, Saturation -20; MemoryColor limited to 2 options | Rendering | — | ❌ GAP |
| TR-lighting-046 | No additional visual-cue channels permitted (fog, particles, geometry) — only light/Volume state carries the signal | Rendering | — | ❌ GAP |
| TR-lighting-047 | Accessibility: `MemoryIntensityMultiplier` default ≥ 0.6 (raised above 0.5 floor) for colorblind readability | Gameplay | — | ❌ GAP |
| TR-lighting-048 | No runtime UI of its own — future "reduced visual effects" toggle belongs to Settings menu, out of scope | UI | — | ❌ GAP |
| TR-lighting-049 | Edit-time `OnValidate` warns if a referenced `Light`'s mode is Baked (only checked when the `ShiftConfig` asset itself is edited) | Architecture | — | ❌ GAP |

#### Adaptif Ses Sistemi — Adaptive Audio System (`audio`)
Source: `design/gdd/adaptif-ses-sistemi.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-audio-001 | Middleware confirmed: Unity built-in AudioMixer + AudioSource, no FMOD/Wwise | Architecture | — | ❌ GAP |
| TR-audio-002 | 3 named zones each have 2 constant base ambience layers + 1 round-based fade-in layer | Audio | — | ❌ GAP |
| TR-audio-003 | Round-based layer reads `CurrentRoundIndex`/`TotalRoundCount` directly from Task/Carry Loop each update | Architecture | — | ❌ GAP |
| TR-audio-004 | `TensionGain(roundIndex)=ease(x)`, `x=clamp(roundIndex/(TotalRoundCount-1),0,1)`, `ease(x)=x²(3-2x)`; `layer3_volume=base×TensionGain` | Audio | — | ❌ GAP |
| TR-audio-005 | `TensionGain` guards divide-by-zero: `TotalRoundCount≤1` hard-clamps to 1 before division | Architecture | — | ❌ GAP |
| TR-audio-006 | Ambience crossfade: two `AudioSource`s (A/B ping-pong), ~1-2s lerp on `ZoneChanged`; no Snapshot system | Audio | — | ❌ GAP |
| TR-audio-007 | Both ambience sources assigned to new dedicated "Ambiance" AudioMixer group | Audio | — | ❌ GAP |
| TR-audio-008 | New `AmbientZoneVolume` component: one trigger collider per named zone fires `ZoneChanged(zoneId)`; no hysteresis | Architecture | — | ❌ GAP |
| TR-audio-009 | Adjacent zone volumes share boundaries with zero overlap/gap — exactly one zone always active | Architecture | — | ❌ GAP |
| TR-audio-010 | Scene co-residency guard: `AmbientZoneVolume` ticker skipped (not destroyed) unless own scene matches active scene | Persistence | — | ❌ GAP |
| TR-audio-011 | Initial-zone spawn detection: one-time overlap check since `OnTriggerEnter` doesn't fire for spawn-inside case | Physics | — | ❌ GAP |
| TR-audio-012 | Initial-check-vs-co-residency race fix: deferred to first tick where own scene matches active scene, via `_initialCheckDone` flag | Architecture | — | ❌ GAP |
| TR-audio-013 | Memory-Trigger Stinger processes both `Held` (always) and `Shifting-In` (if `IsShiftPersistent`) | Audio | — | ❌ GAP |
| TR-audio-014 | Stinger system calls `IsShiftPersistent(shiftId)` to pick early vs. late trigger path | Architecture | — | ❌ GAP |
| TR-audio-015 | Stinger pool: 3-4 `AudioSource`s on Stinger mixer group, `PlayOneShot` (not `PlayClipAtPoint`, no mixer group support) | Audio | — | ❌ GAP |
| TR-audio-016 | `stinger_falloff`: `minDistance=stingerAudioRadius×0.3; maxDistance=stingerAudioRadius×1.0`, from `GetStingerAudioRadius` not gameplay radius | Audio | — | ❌ GAP |
| TR-audio-017 | Queries `GetStingerAudioRadius(shiftId)` same frame as play-attempt trigger | Architecture | — | ❌ GAP |
| TR-audio-018 | `Idle`/`Playing`/`Cooldown` app-level state machine — never uses `source.isPlaying`; `Invoke(EnterCooldown, clip.length)` | Architecture | — | ❌ GAP |
| TR-audio-019 | Runtime RMS enforcement: static brickwall limiter on Stinger group, per-zone RMS ceiling tuning knob | Audio | — | ❌ GAP |
| TR-audio-020 | Footstep playback: FPC calls `PlayFootstep(speed)` per step; dedicated `AudioSource`, `PlayOneShot`, 4-6 samples with repeat-protection, ±5% pitch | Audio | — | ❌ GAP |
| TR-audio-021 | `footstep_volume = speed/1.6`, `speed` inline from FPC's Formula 1 output — no independent Velocity sampling | Architecture | — | ❌ GAP |
| TR-audio-022 | HARD CUT Sting subscribes to `OnTransitionStateChanged`, fires only on `Swapping && Hard && GetCurrentHardCutAbrupt()==true` | Architecture | — | ❌ GAP |
| TR-audio-023 | `GetCurrentHardCutAbrupt()` queried same frame as `Swapping` received | Architecture | — | ❌ GAP |
| TR-audio-024 | CutSting on dedicated "CutSting" mixer group, single `AudioSource`, synced to zero-frame swap | Audio | — | ❌ GAP |
| TR-audio-025 | `Abrupt=false`: CutSting never plays; ambience/stinger crossfade to silence via `ambient_crossfade` instead | Audio | — | ❌ GAP |
| TR-audio-026 | Abrupt-stop-all rule (`Abrupt=true` only): all ambience/pooled stingers stop instantly, no fade | Audio | — | ❌ GAP |
| TR-audio-027 | CutSting exempt from abrupt-stop-all — stop-all-first, then CutSting `PlayOneShot` | Architecture | — | ❌ GAP |
| TR-audio-028 | New dedicated "SFX" mixer group for Task/Carry pickup/delivery/jostle SFX, no ducking | Audio | — | ❌ GAP |
| TR-audio-029 | `HeldSessionAlreadyPlayed` guard: session-lifetime `HashSet<string>`, added on `Playing`, removed only on `Shifting-Out`/`Dormant` | Persistence | — | ❌ GAP |
| TR-audio-030 | `ambient_crossfade`: `x=clamp(elapsed/T,0,1); ease(x)=x²(3-2x); volume_B=ease(x); volume_A=1-ease(x)`, T≈1-2s | Audio | — | ❌ GAP |
| TR-audio-031 | Mid-crossfade re-trigger resumes from current gain values, not reset to t=0 | Audio | — | ❌ GAP |
| TR-audio-032 | Third-zone crossfade re-assigns quieter source to new target, still exactly 2 pooled sources | Audio | — | ❌ GAP |
| TR-audio-033 | Stinger pool concurrency: if all 3-4 sources Playing, new request silently dropped, no queueing | Audio | — | ❌ GAP |
| TR-audio-034 | Footstep overlap has no rate limit; throttle owner (FPC vs. audio system) is an open architectural question | Architecture | — | ❌ GAP |
| TR-audio-035 | Cooldown re-entry: `Held` accepted only from `Idle`, not `Cooldown` — ignored if still cooling down | Audio | — | ❌ GAP |
| TR-audio-036 | Simultaneous multi-zone `Held`: each independently acquires a free pool source if ≥2 free | Audio | — | ❌ GAP |
| TR-audio-037 | Stinger-synced closed-caption UI element, timing-synced to clip start/stop, styled distinct from dialogue subtitles | UI | — | ❌ GAP |
| TR-audio-038 | Stinger timing doesn't subscribe to FPC Velocity/IsGrounded — footstep phase stays FPC-owned to avoid drift | Architecture | — | ❌ GAP |

### Core Layer

#### Etkileşim Sistemi — Interaction System (`interact`)
Source: `design/gdd/etkilesim-sistemi.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-interact-001 | Focus detection via `SphereCast` (radius ~0.05m, range 2.0m) from EyeCamera every frame | Physics | — | ❌ GAP |
| TR-interact-002 | Shared static `InteractableRegistry` — every `IInteractable` self-registers OnEnable/OnDisable, read by both this system and FPC's taper | Architecture | — | ❌ GAP |
| TR-interact-003 | `IInteractable.Type` is `Instant` or `Hold` | Gameplay | — | ❌ GAP |
| TR-interact-004 | Crosshair/prompt UI owned by this system; changes only on Idle↔Focused transition | UI | — | ❌ GAP |
| TR-interact-005 | Default Hold-fill indicator drawn automatically by this system from its own computed `t`, no dependency on object | UI | — | ❌ GAP |
| TR-interact-006 | `IInteractable.SuppressDefaultHoldFill` (default false) opts out of the default fill entirely | UI | — | ❌ GAP |
| TR-interact-007 | Input via "Gameplay" map's `Interact` — `WasPressedThisFrame` for Instant, `IsPressed` for Hold | Input | — | ❌ GAP |
| TR-interact-008 | Race-condition protection: synchronous registry cleanup in `OnDisable`, Unity object-null comparison, not raw C# null | Architecture | — | ❌ GAP |
| TR-interact-009 | State machine: Idle→Focused→Holding→Focused; Instant fires `OnInteract()` and stays Focused | Architecture | — | ❌ GAP |
| TR-interact-010 | Entering Holding calls `RequestMovementLock(this, MoveOnly)` — MoveOnly required so Look stays free for cancel-by-look-away | Architecture | — | ❌ GAP |
| TR-interact-011 | Hold completion/cancellation both call `ReleaseMovementLock(this)`, return to Focused | Architecture | — | ❌ GAP |
| TR-interact-012 | Hard dependency on FPC's EyeCamera and IsLocked pre-check | Architecture | — | ❌ GAP |
| TR-interact-013 | `IInteractable` interface: Type, HoldDuration, CanInteract, PromptText, OnFocusEnter/Exit, OnInteract, OnHoldProgress/Complete/Cancelled, OnHoldBlocked, SuppressDefaultHoldFill | Architecture | — | ❌ GAP |
| TR-interact-014 | `hold_progress`: `t=clamp(elapsedHoldTime/HoldDuration,0,1)`; resets on cancel; HoldDuration per-object 0.1-3.0s | Gameplay | — | ❌ GAP |
| TR-interact-015 | Hold progress must be linear at core-system level — urgency/anticipation curves applied downstream only | Architecture | — | ❌ GAP |
| TR-interact-016 | `OnHoldProgress(t)` always delivers raw linear t; downstream systems remap in their own code | Architecture | — | ❌ GAP |
| TR-interact-017 | SphereCast radius/range are fixed Tuning Knob constants, not scaled by speed/FOV | Physics | — | ❌ GAP |
| TR-interact-018 | Edge case: target Destroy/SetActive(false) mid-hold — `OnHoldComplete` NOT called, lock released, returns to Idle not Focused | Architecture | — | ❌ GAP |
| TR-interact-019 | Edge case: button-release and target-loss same frame — fixed check order (target validity before button) | Architecture | — | ❌ GAP |
| TR-interact-020 | Edge case: `HoldDuration≤0` — checked before division, `OnHoldComplete` fires immediately, logs warning | Architecture | — | ❌ GAP |
| TR-interact-021 | Edge case: multiple SphereCast hits same frame — smallest `hit.distance` wins, ties by smallest InstanceID | Physics | — | ❌ GAP |
| TR-interact-022 | Edge case: lock already held elsewhere — pre-check via `IPlayerState.IsLocked`, fires `OnHoldBlocked()` instead of requesting | Architecture | — | ❌ GAP |
| TR-interact-023 | Edge case: object disables/re-enables mid-Holding — counted as target-loss, requires fresh Focused entry | Architecture | — | ❌ GAP |
| TR-interact-024 | Edge case: registry mutated during iteration — read-only snapshot at iteration start | Architecture | — | ❌ GAP |
| TR-interact-025 | Edge case: SphereCast hits different target while Holding — target locked, no auto-switching until cancel/complete | Gameplay | — | ❌ GAP |
| TR-interact-026 | Elevator/Floor-Access does not use this system — own trigger-zone logic instead | Architecture | — | ❌ GAP |
| TR-interact-027 | Crosshair state change must include shape/size, not color alone (colorblind accessibility) | UI | — | ❌ GAP |
| TR-interact-028 | Build-time content check: mock `IInteractable` test verifies default Hold-fill advances regardless of consumption | Architecture | — | ❌ GAP |
| TR-interact-029 | Open question: `InteractableRegistry` ownership/location not finalized — required before implementation | Architecture | — | ❌ GAP |
| TR-interact-030 | Open question: SphereCast occlusion/blocking behavior for transparent-but-blocking surfaces undefined | Architecture | — | ❌ GAP |

#### Asansör/Kat-Erişim Sistemi — Elevator/Floor-Access (`elevator`)
Source: `design/gdd/asansor-kat-erisim-sistemi.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-elevator-001 | Call button uses own trigger-zone logic (~1.5m), does NOT implement `IInteractable` | Architecture | — | ❌ GAP |
| TR-elevator-002 | Trigger-zone reads "Gameplay" map's `Interact` directly when player inside | Input | — | ❌ GAP |
| TR-elevator-003 | Reads `IsSessionActive` synchronously at press-time only, not polled | Architecture | — | ❌ GAP |
| TR-elevator-004 | Builds own availability logic on top of `IsSessionActive` — decision never made by Session State itself | Architecture | — | ❌ GAP |
| TR-elevator-005 | FSM: Idle→Called→DoorsOpening→DoorsOpen→DoorsClosing→Waiting→(DoorsOpen\|DoorsOpening)→Idle | Gameplay | — | ❌ GAP |
| TR-elevator-006 | `ArrivalDuration` timer (3-6s) gates Called→DoorsOpening | Gameplay | — | ❌ GAP |
| TR-elevator-007 | Door timing: `DoorOpenAnim`(~1.5s)→`DwellTime`(~4-6s)→`DoorCloseAnim`(~1.5s), constants | Gameplay | — | ❌ GAP |
| TR-elevator-008 | Cabin never receives real transform/platform movement at any state transition | Physics | — | ❌ GAP |
| TR-elevator-009 | "Movement" delivered via camera-space procedural shake + continuous hum, active only during `Waiting` | Rendering | — | ❌ GAP |
| TR-elevator-010 | Shake/hum start/stop hard-cut (no fade) exactly on `Waiting` entry/exit | Audio | — | ❌ GAP |
| TR-elevator-011 | `RequestMovementLock(this, MoveOnly)` on DoorsClosing→Waiting | Input | — | ❌ GAP |
| TR-elevator-012 | `ReleaseMovementLock(this)` inside both `onComplete` and `onFailed` | Input | — | ❌ GAP |
| TR-elevator-013 | No platform-delta injection into FPC required, cabin never physically moves | Physics | — | ❌ GAP |
| TR-elevator-014 | Calls `RequestSoftTransition(...)` on Scene Transition once DoorsClosing completes | Architecture | — | ❌ GAP |
| TR-elevator-015 | Subscribes to `OnSoftTransitionRejected` — fires synchronously only at request time | Architecture | — | ❌ GAP |
| TR-elevator-016 | Synchronous rejection: cabin returns directly to DoorsOpening on origin floor, lock never held | Architecture | — | ❌ GAP |
| TR-elevator-017 | `onFailed` during Waiting: same recovery as rejection — stop shake/hum, release lock, return to origin | Architecture | — | ❌ GAP |
| TR-elevator-018 | Reads `IsSessionActive` read-only, polled only at press instant | Architecture | — | ❌ GAP |
| TR-elevator-019 | MVP: single cabin, one button per floor, no concurrent-call queueing — other buttons inert while busy | Gameplay | — | ❌ GAP |
| TR-elevator-020 | Second press while busy is a no-op — no re-trigger, no dwell reset, no queue | Gameplay | — | ❌ GAP |
| TR-elevator-021 | `Called` not tied to continued physical presence — trigger checked only at entry (press) time | Gameplay | — | ❌ GAP |
| TR-elevator-022 | `IsSessionActive` going false mid-cycle has no effect — not polled during in-progress cycle | Architecture | — | ❌ GAP |
| TR-elevator-023 | Player exit during DoorsOpen dwell: presence checked only at instant DoorsClosing begins | Gameplay | — | ❌ GAP |
| TR-elevator-024 | Dwell expiry with no player inside auto-transitions DoorsClosing→Idle, no error state | Gameplay | — | ❌ GAP |
| TR-elevator-025 | Move frozen in Waiting makes physical exit impossible; Look stays free | Physics | — | ❌ GAP |
| TR-elevator-026 | `RequestHardCut` during SOFT Waiting doesn't interrupt SOFT — queues per Scene Transition's asymmetric rule | Architecture | — | ❌ GAP |
| TR-elevator-027 | Cabin interior must be fully enclosed/camera-safe from every look angle during Waiting | Rendering | — | ❌ GAP |
| TR-elevator-028 | No non-diegetic UI — button light is fully diegetic; shared crosshair/prompt never appears | UI | — | ❌ GAP |
| TR-elevator-029 | Hard dependency on FPC (lock), Scene Transition (soft transition), Session State (IsSessionActive) | Architecture | — | ❌ GAP |
| TR-elevator-030 | Explicitly does NOT depend on Etkileşim Sistemi | Architecture | — | ❌ GAP |

#### Diyalog/Anlatı İçeriği — Dialogue/Narrative Content (`dialogue`)
Source: `design/quick-specs/diyalog-anlati-icerigi-2026-08-02.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-dialogue-001 | `DialogueSceneConfig` per psychiatry scene: fixed base dialogue + pre-written `CallbackPool` tagged by `clueId` | Architecture | — | ❌ GAP |
| TR-dialogue-002 | On scene start, every `CallbackPool` entry evaluated via `IsClueKnown(clueId)` against Narrative State | Architecture | — | ❌ GAP |
| TR-dialogue-003 | Tempo cap: candidates beyond `MaxCallbacksPerScene` skipped by writer-assigned `Priority`, not deleted | Gameplay | — | ❌ GAP |
| TR-dialogue-004 | Own `HashSet<string> UsedCallbackIds` bookkeeping, separate from Narrative State's `KnownClueIds` | Persistence | — | ❌ GAP |
| TR-dialogue-005 | `UsedCallbackIds` has no cross-night persistence plan — not blocking for MVP (single night) | Persistence | — | ❌ GAP |
| TR-dialogue-006 | Zero met conditions plays base dialogue only, no error flag | Gameplay | — | ❌ GAP |
| TR-dialogue-007 | Default `MaxCallbacksPerScene = 3`, matching MVP's 3-clue content cap | Gameplay | — | ❌ GAP |
| TR-dialogue-008 | Build-time consistency check: `MaxCallbacksPerScene` must not be smaller than a single-scene night's total clue count | Architecture | — | ❌ GAP |
| TR-dialogue-009 | Hard dependency on Narrative State — calls `IsClueKnown` for every pool entry | Architecture | — | ❌ GAP |
| TR-dialogue-010 | No systems depend on this one — Cutscene guarantees load, never calls its API | Architecture | — | ❌ GAP |

### Feature Layer

#### Görev/Taşıma Döngüsü — Task/Carry Loop (`carry`)
Source: `design/gdd/gorev-tasima-dongusu.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-carry-001 | `TaskList` = ordered `CarryRound[]` (3-5), each holds `List<CarryItemDef>` ≤ N slots | Architecture | — | ❌ GAP |
| TR-carry-002 | Only active round's items spawned/interactable; later rounds not yet in the registry | Architecture | — | ❌ GAP |
| TR-carry-003 | `CarryItemPickup` implements `IInteractable`(Instant); `CanInteract=(slots<N) AND (active round)`; "Eller Dolu" prompt when blocked | Gameplay | — | ❌ GAP |
| TR-carry-004 | On pickup, `SetActive(false)`, relies on Interaction System's snapshot-iteration registry pattern | Architecture | — | ❌ GAP |
| TR-carry-005 | `SetCarrying(true)` called exactly once, on 0→1 filled-slot transition only | Gameplay | — | ❌ GAP |
| TR-carry-006 | Carried-item visuals: pre-instantiated pool of N slot representations, scene-lifetime | Rendering/Performance | — | ❌ GAP |
| TR-carry-007 | Pickup "settling" impulse: ~0.05-0.1s spring/offset transform effect, not an animation state machine | Rendering | — | ❌ GAP |
| TR-carry-008 | Slot capacity N hard gate on filled-slot count | Gameplay | — | ❌ GAP |
| TR-carry-009 | Delivery point is a trigger-zone (not `IInteractable`) — auto-delivers on entry | Gameplay/Physics | — | ❌ GAP |
| TR-carry-010 | TaskList/round state kept in-memory, cross-scene-load persistent service, no disk persistence | Persistence | — | ❌ GAP |
| TR-carry-011 | Static/singleton plain C# service holding `HashSet<string> CollectedItemIds`, cleared on round change | Persistence/Architecture | — | ❌ GAP |
| TR-carry-012 | Each pickup checks `CollectedItemIds` in `Awake()` before registration, prevents re-collect after reload | Persistence/Architecture | — | ❌ GAP |
| TR-carry-013 | `OnTaskListCompleted` fires once when queue and slots both empty; no separate manifest structure | Architecture | — | ❌ GAP |
| TR-carry-014 | `OnFinalRoundStarted` fires exactly once when the activating round is the last | Architecture | — | ❌ GAP |
| TR-carry-015 | `HasCarriedInFinalRound` set true on first final-round pickup, write-once | Architecture | — | ❌ GAP |
| TR-carry-016 | `OnFinalRoundItemPickedUp` fires exactly once at same moment `HasCarriedInFinalRound` becomes true | Architecture | — | ❌ GAP |
| TR-carry-017 | `CurrentRoundIndex`/`TotalRoundCount` public read-only queries, same counters as `Highlight(round)` | Architecture | — | ❌ GAP |
| TR-carry-018 | No forced resolution for undelivered item — remains carried indefinitely, no "put back" | Gameplay | — | ❌ GAP |
| TR-carry-019 | State machine: Idle(Depo)→Loading(optional)→Carrying→Delivering→RoundComplete→(Idle\|AllComplete); elevator travel opaque | Architecture | — | ❌ GAP |
| TR-carry-020 | No direct API call to Elevator — participates only as passive passenger | Architecture | — | ❌ GAP |
| TR-carry-021 | `FirstPersonController.SetCarrying(bool)` single call point; sway reads FPC's phase accumulator | Architecture | — | ❌ GAP |
| TR-carry-022 | Reads `IsSessionActive` read-only guard before allowing pickup | Architecture | — | ❌ GAP |
| TR-carry-023 | `JostleSounds`/pickup/delivery SFX routed to Adaptive Audio's "SFX" group, no ducking | Audio | — | ❌ GAP |
| TR-carry-024 | State must survive `RequestSoftTransition` scene changes — reinforces persistent-service requirement | Persistence | — | ❌ GAP |
| TR-carry-025 | Build-blocking validation: `OnValidate()` warning + `IPreprocessBuildWithReport` failing on >N items, 0 items, or N=0 | Architecture | — | ❌ GAP |
| TR-carry-026 | Runtime must never silently clamp/crop invalid config — build validation only defense | Gameplay | — | ❌ GAP |
| TR-carry-027 | Round-complete evaluation and next-round registry registration execute synchronously same frame, no yield | Architecture | — | ❌ GAP |
| TR-carry-028 | Delivery logic idempotent, guarded by `carriedCount>0` | Gameplay/Physics | — | ❌ GAP |
| TR-carry-029 | Cutscene subscribes to `OnTaskListCompleted`, `OnFinalRoundStarted`, `OnFinalRoundItemPickedUp`; reads final-round flags | Architecture | — | ❌ GAP |
| TR-carry-030 | `Highlight(round)=lerp(1.0,0.30,ease(roundIndex/(roundCount-1)))`, guarded for roundCount≤1 | Rendering | — | ❌ GAP |
| TR-carry-031 | Round-based dimming only via light/rim-highlight + socket offset — camera FOV/rotation/post-process untouched | Rendering | — | ❌ GAP |
| TR-carry-032 | Pickup/delivery SFX type-based, constant across rounds, 3D spatialized ~0.5-3m rolloff | Audio | — | ❌ GAP |
| TR-carry-033 | Jostle sound single-shot on direction change ≥30-60°, gated 0.5-1.0s repeat interval | Audio | — | ❌ GAP |
| TR-carry-034 | Uses built-in AudioSource/AudioMixer, no pooling needed (~3-8 events/round) | Audio | — | ❌ GAP |
| TR-carry-035 | `CarryItemDef` requires added optional `AudioClip[] JostleSounds` field | Architecture | — | ❌ GAP |
| TR-carry-036 | Zero screen-space HUD for carry state — conveyed via visible arm/hand rig only | UI | — | ❌ GAP |
| TR-carry-037 | Slot-fill readability unaffected by round-based aesthetic dimming — functional channel never fades | UI | — | ❌ GAP |
| TR-carry-038 | Optional accessibility "N/M" numeric fallback, toggle default OFF | UI | — | ❌ GAP |
| TR-carry-039 | Automated structural test verifies no Animator/blend-tree exists on carry rig — socket-offset only | Architecture | — | ❌ GAP |
| TR-carry-040 | Automated unit test verifies jostle sound-selection function is pure/deterministic independent of round index | Architecture | — | ❌ GAP |

#### Anı-Tetikleyici Etkileşim — Memory-Trigger Interaction (`memory`)
Source: `design/gdd/ani-tetikleyici-etkilesim.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-memory-001 | `MemoryTriggerDef` (ScriptableObject): shiftId, shiftConfig (always `Persistent=true`), holdDurationOverride (0.6-1.5s), promptText | Architecture | — | ❌ GAP |
| TR-memory-002 | Scene instance is `MemoryTriggerObject : IInteractable` referencing a `MemoryTriggerDef` | Architecture | — | ❌ GAP |
| TR-memory-003 | `OnHoldProgress` never consumed; returns `SuppressDefaultHoldFill=>true` — default crosshair fill never drawn | UI/Architecture | — | ❌ GAP |
| TR-memory-004 | `OnHoldComplete()` calls `TriggerShift(shiftId, shiftConfig)` with no additional guard, relies on `Persistent=true` invariant | Architecture | — | ❌ GAP |
| TR-memory-005 | After `TriggerShift`, object becomes permanently `CanInteract=false` (Committed) | Gameplay | — | ❌ GAP |
| TR-memory-006 | Committed state persists via Session State, not scene-local field — `Awake()` checks `FiredTriggerIds` | Persistence | — | ❌ GAP |
| TR-memory-007 | `OnHoldComplete()` also adds shiftId to `FiredTriggerIds`, same step as `TriggerShift` | Persistence/Architecture | — | ❌ GAP |
| TR-memory-008 | Fired/Committed flag never written to the `MemoryTriggerDef` asset itself — lives only in session state | Persistence/Architecture | — | ❌ GAP |
| TR-memory-009 | `OnHoldCancelled()` complete no-op — object stays Unfired, unlimited retries | Gameplay | — | ❌ GAP |
| TR-memory-010 | `shiftConfig.Persistent` must always be true, enforced by edit-time validation; `RevertShift` must never be called | Architecture | — | ❌ GAP |
| TR-memory-011 | Bound zone must always be `TriggerMode=ManualOnly`, enforced by build-time validation | Architecture | — | ❌ GAP |
| TR-memory-012 | `TriggerMode` lives on the scene-placed zone, not the shared asset; matching via shared `shiftId` string key, no direct object reference | Architecture | — | ❌ GAP |
| TR-memory-013 | Bound zone's `StingerAudioRadius` must always be >0, enforced by build-time validation | Architecture/Audio | — | ❌ GAP |
| TR-memory-014 | Edit-time validation via `IPreprocessBuildWithReport` scans all `MemoryTriggerDef` assets for duplicate shiftId, Persistent≠true, StingerAudioRadius≤0 | Architecture | — | ❌ GAP |
| TR-memory-015 | `OnValidate()` explicitly unusable for duplicate-shiftId check — can't see sibling assets | Architecture | — | ❌ GAP |
| TR-memory-016 | Optional `[MenuItem]` fast in-editor scan; only `IPreprocessBuildWithReport` actually blocks build | Architecture | — | ❌ GAP |
| TR-memory-017 | `TriggerMode` validation requires separate scene-scan step opening every scene, matching zones by shiftId | Architecture | — | ❌ GAP |
| TR-memory-018 | Asset-scan validation must share one `IPreprocessBuildWithReport` utility with Narrative State's clueId duplicate check | Architecture | — | ❌ GAP |
| TR-memory-019 | State machine: Unfired(CanInteract=true)→Committed(CanInteract=false, terminal across reloads) | Architecture | — | ❌ GAP |
| TR-memory-020 | Implements `IInteractable.Hold`, consumes OnHoldProgress/Complete/Cancelled contract; HoldDuration within 0.6-1.5s subrange | Architecture | — | ❌ GAP |
| TR-memory-021 | Decoupled/event-driven: Adaptive Audio and Narrative State never called directly, both subscribe to `OnShiftStateChanged` independently | Architecture | — | ❌ GAP |
| TR-memory-022 | No derived Formulas — pure event orchestration, `TriggerShift` is a one-shot call | Architecture | — | ❌ GAP |
| TR-memory-023 | Duplicate-shiftId bypass edge case: first `TriggerShift` succeeds, second silently no-ops but object still transitions to Committed | Architecture | — | ❌ GAP |
| TR-memory-024 | `Persistent=false` misconfiguration edge case has NO runtime fallback defense in this system — edit-time validation only line of defense | Architecture | — | ❌ GAP |
| TR-memory-025 | Compound-bypass edge case (duplicate shiftId AND Persistent=false) can hit Light/Volume's retrigger rule and reverse direction | Architecture | — | ❌ GAP |
| TR-memory-026 | Soft-lock-from-concurrent-Hold impossibility guaranteed structurally via IsLocked pre-check + OnHoldBlocked | Architecture | — | ❌ GAP |
| TR-memory-027 | Destroy/disable while Unfired: shiftId may become permanently unreachable if no other object shares it — content-authoring risk | Gameplay | — | ❌ GAP |
| TR-memory-028 | Destroy/disable while Committed has no effect — shift state independent of triggering object's lifetime | Architecture | — | ❌ GAP |
| TR-memory-029 | Scene-reload restore: re-Awoken instance checks `FiredTriggerIds`, starts directly in Committed if present | Persistence | — | ❌ GAP |
| TR-memory-030 | Visual/Audio requires literally zero feedback during Hold — mechanically enforced via SuppressDefaultHoldFill + zero own VFX/SFX | UI/Audio | — | ❌ GAP |
| TR-memory-031 | Committed state has no dedicated visual marker — absence of prompt is the only signal | UI | — | ❌ GAP |
| TR-memory-032 | Pre-interaction appearance has no special highlight/glow — relies on Interaction System's general focus behavior | UI | — | ❌ GAP |
| TR-memory-033 | AC3: duplicate shiftId caught by asset-scan, blocks build, verified by EditMode test | Architecture | — | ❌ GAP |
| TR-memory-034 | AC4: `Persistent=false` caught by asset-scan validation, verified by EditMode test | Architecture | — | ❌ GAP |
| TR-memory-035 | AC4a: `TriggerMode=Automatic` default on bound zone caught by scene-scan step | Architecture | — | ❌ GAP |
| TR-memory-036 | AC4b: `StingerAudioRadius≤0`/unset caught by asset-scan validation | Architecture | — | ❌ GAP |
| TR-memory-037 | AC6: CI static-analysis grep for `RevertShift(` calls, fails build if found — BLOCKING Logic-tier gate | Architecture | — | ❌ GAP |
| TR-memory-038 | AC7 (full end-to-end) deferred to Blocked ACs, gated on engine build + audio-paired spike | Architecture | — | ❌ GAP |
| TR-memory-039 | AC10: scene reload with shiftId already in `FiredTriggerIds` re-Awakes directly into Committed | Persistence | — | ❌ GAP |

#### Sahne Kesmeli Anlatı — Cutscene/Scene-Cut Narrative (`cutscene`)
Source: `design/quick-specs/sahne-kesmeli-anlati-2026-08-02.md`

| ID | Requirement | Domain | ADR | Status |
|---|---|---|---|---|
| TR-cutscene-001 | Night ending: OR logic — `OnTaskListCompleted` OR memory-trigger saturation, whichever first | Gameplay | — | ❌ GAP |
| TR-cutscene-002 | Saturation requires 3 flags simultaneously: `SettledTriggerIds.Count==Total`, `IsFinalRoundActive`, `HasCarriedInFinalRound` | Gameplay | — | ❌ GAP |
| TR-cutscene-003 | Saturation measured against `SettledTriggerIds` (populated at `Held`), not `FiredTriggerIds` (populated at `Shifting-In`) | Architecture | — | ❌ GAP |
| TR-cutscene-004 | Preload timing intentionally uses the earlier eager signal (`FiredTriggerIds.Count==Total-1`); second `PreloadHardCut` safe via no-op rule | Architecture | — | ❌ GAP |
| TR-cutscene-005 | Condition (b) event-driven, not polled — subscribes to `OnTriggerSettled`, `OnFinalRoundStarted`, `OnFinalRoundItemPickedUp` | Architecture | — | ❌ GAP |
| TR-cutscene-006 | `HasCarriedInFinalRound` guard prevents saturation becoming true on final round's first frame before preload is Ready | Gameplay | — | ❌ GAP |
| TR-cutscene-007 | On real trigger: `RequestMovementLock(Full)` immediately followed by `RequestHardCut(...)`, in order, exactly once | Gameplay | — | ❌ GAP |
| TR-cutscene-008 | `HardCutConfig.Abrupt` differs per ending — saturation=true, task-completion=false, must never share the same value | Audio | — | ❌ GAP |
| TR-cutscene-009 | `MovementLockScope.Full` required for both endings — prevents camera drift during zero-frame swap | Gameplay | — | ❌ GAP |
| TR-cutscene-010 | Re-trigger guard: `HasTriggeredThisNight` set true instantly on `RequestHardCut` call, guarantees exactly one call per night | Architecture | — | ❌ GAP |
| TR-cutscene-011 | Session closure: `onComplete` calls `EndSession()` + releases lock; `onFailed` releases lock but does NOT end session | Persistence | — | ❌ GAP |
| TR-cutscene-012 | Does not participate in dialogue selection — only guarantees correct scene is loaded | Architecture | — | ❌ GAP |
| TR-cutscene-013 | State machine: Watching→Preloaded→Triggering(HasTriggeredThisNight=true)→Complete; may skip directly to Triggering | Architecture | — | ❌ GAP |
| TR-cutscene-014 | Cross-system contract on Scene Transition: `PreloadHardCut`, `RequestHardCut`, `onComplete`/`onFailed` | Architecture | — | ❌ GAP |
| TR-cutscene-015 | Cross-system contract on Task/Carry Loop: `OnTaskListCompleted`, `IsFinalRoundActive`, `OnFinalRoundStarted`, `HasCarriedInFinalRound`, `OnFinalRoundItemPickedUp` | Architecture | — | ❌ GAP |
| TR-cutscene-016 | Cross-system contract on Session State: `EndSession()`, `SettledTriggerIds.Count`, `FiredTriggerIds.Count`, `OnTriggerSettled` | Persistence | — | ❌ GAP |
| TR-cutscene-017 | Cross-system contract on FPC: `RequestMovementLock(Full)`/`ReleaseMovementLock` | Input | — | ❌ GAP |
| TR-cutscene-018 | No numeric tuning knobs — pure event orchestration; `TotalConfiguredTriggerCountForNight` ownership still unresolved open item | Architecture | — | ❌ GAP |
| TR-cutscene-019 | Zero-frame delay guarantee at real trigger requires preload to have already reached Ready | Architecture | — | ❌ GAP |
| TR-cutscene-020 | On `onFailed`: whether `HasTriggeredThisNight` resets for retry or stays terminal is an unresolved open question | Architecture | — | ❌ GAP |

---

## Known Gaps

All 374 requirements above are gaps — see the Coverage Gaps by System table in
`docs/architecture/architecture-review-2026-08-05.md` for suggested ADR titles
per system and the recommended authoring order.

## Superseded Requirements

None — this is the first review pass; no prior ADRs existed to supersede.

## History

| Date | Coverage | Notes |
|------|----------|-------|
| 2026-08-05 | 0% (0/374) | Initial traceability index — architecture phase has not started. See `architecture-review-2026-08-05.md`. |
