# Control Manifest

> **Engine**: Unity 6.5 (6000.5.6f1) — re-pinned 2026-08-09 from 6000.3.0f1 (see VERSION.md)
> **Last Updated**: 2026-08-09
> **Manifest Version**: 2026-08-09
> **ADRs Covered**: ADR-0001, ADR-0002, ADR-0003, ADR-0004, ADR-0005 (+2026-08-09 facade addendum), ADR-0006, ADR-0007, ADR-0008, ADR-0009 (+2026-08-09 caption addendum), ADR-0010, ADR-0011, ADR-0012, ADR-0013, ADR-0014, ADR-0015 — all Accepted 2026-08-09
> **Status**: Active — regenerate with `/create-control-manifest update` when ADRs change
> **Review note**: TD-MANIFEST gate skipped — `technical-director` subagent unavailable in the generating session; all source ADRs individually carry completed TD-ADR reviews, and rule extraction is verbatim from those Accepted texts.

`Manifest Version` is the date this manifest was generated. Story files embed
this date when created. `/story-readiness` compares a story's embedded version
to this field to detect stories written against stale rules.

This manifest is a programmer's quick-reference extracted from all Accepted ADRs,
technical preferences, engine reference docs, and `design/ux/accessibility-requirements.md`.
For the reasoning behind each rule, see the referenced source.

---

## Foundation Layer Rules

*Applies to: static state services, persistent scenes, event architecture, boot, scene transitions*

### Required Patterns
- **Session-scoped state = interface + plain C# class + static facade** (`I[System]State` / `[System]State` / static `[System]`). Tests construct a fresh `...State` directly and inject it — they never touch the static facade — source: ADR-0001
- **Every static-facade reset runs through `FoundationBootstrap.ResetAll()`** in the documented dependency order. A new service is inserted at the correct point in that one list — it never gets its own `[RuntimeInitializeOnLoadMethod]` — source: ADR-0001/0006
- **In-place reset for every event-exposing facade**: `ResetOnLoad()` clears fields on the SAME instance, never replaces it; explicitly re-initialize non-default fields replacement restored for free (e.g. `IsSessionActive = true`) — source: ADR-0011/0015
- **Constructor-time subscriptions bind once per process** on never-replaced instances and survive every `ResetAll()` — no re-wire inside any `ResetOnLoad()` — source: ADR-0015
- **Dereference `X.Instance` live at the point of use**; never cache a session-state interface reference across a Play-session boundary (sole documented carve-out: a structurally never-replaced `readonly` instance, per ADR-0015's `_machine`) — source: ADR-0001/0015
- **Objects that must survive scene swaps live in one of the three persistent scenes** (UI: ADR-0002, Player: ADR-0003, Foundation: ADR-0008), loaded additively once at boot, never unloaded — source: ADR-0002/0003/0008
- **Persistent-scene singletons**: `Awake()`-set static `Instance` + duplicate guard — unconditional `Debug.LogError` + `Destroy(gameObject)` + early return (never `Debug.Assert`) — source: ADR-0003/0008/0009/0010
- **Boot contract**: the build's initial load set contains ONLY the persistent scenes (UI → Player sequential-awaited, Foundation); the depot loads exclusively via `SahneKesmeliAnlatiController`'s post-night-begin-setup call — `StartNight()` + `SetTotalConfiguredTriggerCountForNight()` MUST complete before any depot object activates — source: ADR-0003/0013/0015
- **Restore queries live at the top of `OnEnable()`, before the `Register` call in the same body** — never in `Awake()` (Reload Scene: Off suppresses `Awake` re-runs; QQ-07) — source: ADR-0013/0014
- **`InteractableRegistry.Register`/`Deregister` are called only from `OnEnable`/`OnDisable`** — never ad hoc, never mid-session — source: ADR-0004/0014
- **Iterate the registry via `Snapshot()`** (frame-stable), never the live collection — source: ADR-0004
- **Cross-system signals are narrow, typed C# events owned by the producing module** — a new fact gets its own `event Action<...>` on its owner — source: architecture.md Principle 2
- **Single-caller writes (convention + XML-doc + code review, not compiler)**: `EndSession()` → Sahne Kesmeli Anlatı only; `SetRoundState()` → Görev/Taşıma only; `SetTotalConfiguredTriggerCountForNight()` → night-begin orchestrator only; `AddFiredTrigger()` → Anı-Tetikleyici's `OnHoldComplete` only — source: ADR-0006/0014 (QQ-03 resolution)
- **Public write APIs are idempotent by default**; session facts are write-once-per-fact and never cleared within a night — source: architecture.md Principle 4, ADR-0006
- **Subscriptions to `SceneTransitionManager`'s events are lazy** (`OnEnable`/`Start`-time on MonoBehaviours, first-use for services) — `Instance` does not exist at `ResetAll()` time — source: ADR-0008
- **Asset loading goes through Addressables**, lazily, outside any `FoundationBootstrap`-path constructor; `ResetOnLoad()` never touches an engine asset API — source: ADR-0007/0015
- **Any per-frame scene-aware logic gates on `gameObject.scene == SceneManager.GetActiveScene()`** (SOFT co-residency guard) — mandatory for every new system with per-frame scene-local behavior — source: ADR-0005/0009/0012, architecture.md Data Flow §4
- **MonoBehaviours subscribing to persistent (in-place-reset) facade events pair `OnEnable` subscribe with symmetric `OnDisable` unsubscribe** — source: ADR-0009 (revised)/0013/0015

### Forbidden Approaches
- **Never `DontDestroyOnLoad`** — the persistent-scene pattern is this project's one answer — source: ADR-0001/0002/0003
- **Never a generic event bus / mediator / shared `MemoryTriggerEvent` God Object** — source: architecture.md Principle 2
- **Never `ScriptableObject`-backed runtime state** — SO = authored config only; runtime state is always a separate object — source: ADR-0001 Alt 2, architecture.md Principle 5
- **Never a third-party DI framework** (VContainer/Zenject) at current scale — source: ADR-0001 Alt 3
- **Never wholesale state replacement for an event-exposing facade** — orphans/accumulates subscribers — source: ADR-0011/0015
- **Never reference `SceneTransitionManager.Instance` from a constructor or `ResetOnLoad()`** — source: ADR-0008 (registered forbidden pattern)
- **Never place an `IInteractable` implementer in a persistent scene** — breaks the registry's self-correcting lifecycle — source: ADR-0004
- **Never `IReadOnlySet<T>` in a public contract** — not guaranteed under Unity's Api Compatibility profiles; use `IReadOnlyCollection<T>`/membership queries — source: ADR-0006/0007
- **Never access a concrete `...State` class outside its own file/tests** — consumers code against the declared interface only — source: ADR-0001
- **Never invent a second persistence mechanism** — in-memory static service is the only one (no PlayerPrefs, no JsonUtility, no disk in MVP) — source: architecture.md Principle 1/Data Flow §3

### Performance Guardrails
- `Volume.weight` has exactly ONE writer: the owning `ShiftZone` ticker — source: ADR-0005
- HARD CUT `Swapping` = a single synchronous `SetActiveScene`; ≤1 frame request-to-swap, exactly 0 fully-black frames; preload waits genuine 100% (`allowSceneActivation=false`/~90% hold is forbidden) — source: ADR-0008
- Deferred unload fires 0.5–2s after `Complete`, fire-and-forget, never blocking `Idle` — source: ADR-0008
- `ShiftZone` build-blocked constraints: Mixed-mode lights only, no light shared across zones, no overlapping Volume-trigger boxes (~20–40m center spacing guideline), `OnDestroy` force-completes in-flight transitions — source: ADR-0005
- `Snapshot()` is cached once per frame; `Time.frameCount`-keyed caches must be reset via `ResetOnLoad()` — source: ADR-0004

---

## Core Layer Rules

*Applies to: interaction pipeline, elevator, dialogue selection*

### Required Patterns
- **Pure C# state machine + thin MonoBehaviour driver split** for every state machine (BLOCKING unit-test rule) — source: ADR-0003/0010/0011/0013/0015; coding-standards.md
- **SphereCast against `Interactable | Environment` combined mask; closest hit of ANY layer decides** (an Environment hit occludes); `TryGetComponent<IInteractable>` — **the `IInteractable` script and its collider MUST live on the same `GameObject`** — source: ADR-0010
- **`Tick()` ordering**: cancel-check always before progress update; `HoldDuration <= 0` checked BEFORE division; Focused branch re-polls `CanInteract` every tick — source: ADR-0010 (+ADR-0014 revision)
- **Elevator relevance discipline**: only the controller whose scene equals the single `ActiveFloorScene` ticks the shared machine; trigger callbacks arrive via relay components on the collider `GameObject`s; player identified by `CompareTag("Player")` — source: ADR-0011
- **Dialogue callback evaluation is deferred to this scene's own swap**: `Complete` event + `gameObject.scene == SceneManager.GetActiveScene()` filter, one-shot, unsubscribed after firing — source: ADR-0012
- **Order-preserving priority sorts use `OrderBy()` (stable)** — `List<T>.Sort()` is not stable and breaks authored tie order — source: ADR-0012
- **UI lookup = `UIRoot.Instance`**, elements queried once in `OnEnable()`, null-checked defensively, per-owner USS class prefixes (`etkilesim-*`, `ses-*`, `diyalog-*`) — source: ADR-0002/0010

### Forbidden Approaches
- **Never `GameObject.Find` for UI (or anything hot-path)** — source: ADR-0010 Alt 3
- **Never implement the elevator call button as an `IInteractable`** — direct trigger-zone + Interact read, by GDD rule — source: ADR-0011 Alt 3
- **Never auto-start a return elevator ride** — every ride begins with an explicit button press (`_isArrivalLeg` guard) — source: ADR-0011
- **Never evaluate dialogue callbacks in `Awake`/`Start`** — reproduces the saturation-timing bug via a second mechanism — source: ADR-0012 Alt 1
- **Never substitute a one-frame-delay coroutine for the scene-activation signal** — timing coincidence, not a guarantee — source: ADR-0012 Alt 2

### Performance Guardrails
- `Physics.SphereCastNonAlloc` with a fixed 8-element buffer, one cast/frame — source: ADR-0010
- USS class toggles only on state CHANGE, never per-frame — source: ADR-0010

---

## Feature Layer Rules

*Applies to: carry loop, memory triggers, end-condition orchestration*

### Required Patterns
- **Slots-full items stay focusable** ("Eller Dolu" prompt visible); the rejection happens inside `TryPickUp`; `CanInteract=false` only for silent cases (wrong round / collected / session inactive) — source: ADR-0013
- **`CarryItemPickup`/`MemoryTriggerObject` self-restore at `OnEnable`-top before `Register`** — deactivate-entirely variant (carry items) vs stay-visible-skip-register variant (memory triggers) — source: ADR-0013/0014 (registry-mandated pattern)
- **`OnHoldComplete()` = `TriggerShift(shiftId, config)` + `AddFiredTrigger(shiftId)` in the same step, no extra guard**; `OnHoldCancelled()` is a total no-op — source: ADR-0014
- **Memory triggers: `Persistent=true` mandatory, `TriggerMode=ManualOnly`, `SuppressDefaultHoldFill=true`, `HoldDuration` from the 0.6–1.5s sub-range** — all edit-time-validated — source: ADR-0014
- **Saturation counts `SettledCount` only (never Fired)**; evaluated exactly on `OnTriggerSettled`/`OnFinalRoundStarted`/`OnFinalRoundItemPickedUp`; (b) saturation beats (a) task-completion on a tie; `HasTriggeredThisNight` is set BEFORE the outbound trigger event — source: ADR-0015
- **Ending tone/lock branching**: `Abrupt=true` → `MovementLockScope.Full`; `Abrupt=false` → `MoveOnly`; lock before `RequestHardCut`, released in both callbacks; `EndSession()` on success only — source: ADR-0015
- **All edit-time content validation contributes to the ONE shared `IPreprocessBuildWithReport` utility** — never a new independent implementation; `BuildFailedException` blocks, no runtime clamping — source: ADR-0007/0012/0013/0014/0015. *Bir check bilinçli olarak NON-BLOCKING olabilir (`Debug.LogWarning`, `context.Fail` değil) — ama yine AYNI utility'ye kaydolur; ayrı bir mekanizma kurmak yasak. Tek örnek: `Anlati/OrphanedShiftId` (anlati Story 005).*
- **EditMode test fixtures use `ScriptableObject.CreateInstance` only** — on-disk fixture assets trip the project-wide `AssetDatabase.FindAssets` validation scans — source: ADR-0014

### Forbidden Approaches
- **Never call `RevertShift(` anywhere in gameplay code** — enforced by a BLOCKING CI grep/lint — source: ADR-0014 / `ani-tetikleyici-etkilesim.md` AC6
- **Never deregister a committed trigger mid-session** — retirement is `CanInteract=false`, registry exit is `OnDisable` only — source: ADR-0014 Alt 3
- **Never a central `RoundSpawnController`** — items self-manage via the restore pattern — source: ADR-0013 Alt 3
- **Never runtime-clamp misconfigured content** — build-block it — source: ADR-0013
- **Never re-arm the night after `NotifyTriggerFailed`** — a failed psychiatry load is a build defect, not a recoverable state — source: ADR-0015

---

## Presentation Layer Rules

*Applies to: rendering, audio, UI, VFX*

### Required Patterns
- **UI Toolkit exclusively; one shared `UIDocument` in the persistent UI scene** — source: ADR-0002
- **GDD-locked animation timing lives in C#, never USS `transition`** (Hold-fill strictly linear; crosshair Idle↔Focused = opacity/scale smoothstep only, no color, no flash) — scoped to locked-contract elements; a future settings menu may use ordinary USS — source: ADR-0002, art-bible §7.4
- **Lighting = shared VolumeProfile + per-zone `Volume.weight` + script-driven Mixed-light lerp** — max 2–3 real-time shadowed lights per room; zone light arrays are Inspector-authored — source: ADR-0005
- **Audio = 4 mixer groups (Ambiance/Stinger/CutSting/SFX), static gain-staging + one brickwall limiter insert**; stinger pool availability is tracked separately from per-shiftId cooldown; Abrupt HARD CUT order = mute everything THEN play CutSting — source: ADR-0009
- **Stinger caption**: impressionistic/abstract text (never object-naming), unconditional display, synced to clip window, `ses-*` prefix, visually distinct from dialogue subtitles — source: ADR-0009 addendum, `design/ux/accessibility-requirements.md` §2b
- **Accessibility invariants**: no information conveyed by color alone; the motion slider scales VISUAL amplitude only — the FPC phase accumulator is never scaled (footsteps/jostle timing untouched); toggle-hold is an `InteractionController`-level input translation, invisible to every `IInteractable` — source: `design/ux/accessibility-requirements.md` §4/§5/§6

### Forbidden Approaches
- **Never UGUI**: no `Canvas`, `UnityEngine.UI`, or `TextMeshProUGUI` anywhere (registered forbidden pattern; CI-greppable) — source: ADR-0002
- **Never a custom `ScriptableRendererFeature`/RenderGraph pass** — twice resolved not-applicable; the project's highest engine-risk surface stays untouched — source: ADR-0005/0008
- **Never baked-lightmap-set switching** for memory shifts — source: ADR-0005 Alt 2
- **Never dynamic ducking / `AudioMixerSnapshot` transitions / build-up envelopes** — Pillar 2 rejection, repeatedly locked — source: ADR-0009 Alt 3
- **Never auto-discover zone lights by collider bounds** — Inspector-authored arrays only — source: ADR-0005 Alt 3
- **Never an independent timer for head-bob/sway/carry-sway** — the FPC distance-based phase accumulator is the single phase source — source: ADR-0013 / `gorev-tasima-dongusu.md` AC15/16

---

## Global Rules (All Layers)

### Naming Conventions (technical-preferences.md)
| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `PlayerStateProvider` |
| Public/properties | PascalCase | `MoveSpeed` |
| Private fields | `_camelCase` | `_moveSpeed` |
| Events | PascalCase (+`EventHandler` suffix where idiomatic) | `MovementLockChanged` |
| Files | PascalCase = class name | `FirstPersonController.cs` |
| Scenes/Prefabs | PascalCase | `Depot`, `Ballroom` |
| Constants | PascalCase or UPPER_SNAKE_CASE | `SWAP_FRAME_EPSILON` |
| Test files | `[system]_[feature]_test.cs` | `gorev_carry_loop_test.cs` |

### Performance Budgets (technical-preferences.md)
| Target | Value |
|--------|-------|
| Framerate | 60fps |
| Frame budget | 16.6ms |
| Draw calls | ~2000 |
| Memory ceiling | 4GB |

### Approved Libraries / Addons
- **Addressables** — asset loading (first consumer: `ClueRegistry`, ADR-0007). No other third-party runtime dependency is approved; a new one requires explicit `Allowed Libraries` sign-off.

### Forbidden APIs (Unity 6.x — `docs/engine-reference/unity/deprecated-apis.md` unless noted)
- `Input.*` legacy class (`GetKey`/`GetAxis`/`mousePosition`…) → new Input System
- `Canvas`/UGUI `Text`/`Image` → UI Toolkit (also an ADR-0002 project rule)
- `Resources.Load()` → Addressables (ADR-0007 caught this live)
- `Object.FindObjectOfType` → `[Obsolete]` since 2023.1; use static `Instance` accessors (ADR-0009 caught this live)
- `Physics.RaycastAll()` → `NonAlloc` variants
- `Rigidbody.velocity` direct write → `AddForce` (moot — no Rigidbody gameplay in this project)
- `OnPreRender`/`OnPostRender`, `Camera.SetReplacementShader`, `CommandBuffer.DrawMesh` → SRP-incompatible
- `WWW`, `Application.LoadLevel()` → `UnityWebRequest`, `SceneManager`
- `Awaitable` — not forbidden by Unity but **unverified in this project's engine reference**; ADR-0008 deliberately avoided it — do not introduce without a verification pass

### Cross-Cutting Constraints
- **Layers only read downward.** When a lower layer needs a higher layer's fact, the fact's STORAGE moves down; the computing logic does not — source: architecture.md Principle 3
- **Gameplay values are data-driven `ScriptableObject` config** (`ShiftConfig`, `MemoryTriggerDef`, `CarryItemDef`, `ClueDefinition`, `TaskListDef`, `NightConfigDef`, `DialogueSceneConfig`) — config and runtime state are always two separate objects — source: architecture.md Principle 5, coding-standards.md
- **Every system has an ADR; public APIs carry doc comments; state-machine logic has BLOCKING unit tests** — source: coding-standards.md
- **Two-session Editor tests are mandatory** for every static facade / persistent-scene singleton: `[UnityTest]`, Reload Domain and/or Reload Scene disabled, two simulated sessions, asserting exactly-once event delivery and no stale `Instance` — source: ADR-0001/0003/0004/0008/0009/0010/0011/0013/0015
- **Conventional Commits** (`feat:`/`fix:`/…), story/task ID in the body — source: coding-standards.md
- **Never disable or skip a failing test to make CI pass** — source: coding-standards.md
