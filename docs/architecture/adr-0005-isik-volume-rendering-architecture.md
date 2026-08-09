# ADR-0005: Işık/Volume Rendering Architecture — No Custom RenderGraph Pass

> **Unity Specialist Validation**: BLOCKING (2 findings, found and fixed) 2026-08-05 — (1) adjacent `ShiftZone`s' deliberately-oversized Volume-trigger boxes can overlap even with zero shared lights, causing non-linear grade amplification when URP composites two weighted local Volumes sharing one `VolumeProfile`; the existing "don't share lights" discipline never covered this — fixed with a new build-blocking box-overlap validation check. (2) The original `TickShift()` sketch never accounted for a `ShiftZone`'s `GameObject` being destroyed mid-transition during a SOFT transition's deferred scene unload — reachable given the two GDDs' own timing numbers, and would silently, permanently drop a `Held`-gated clue reveal with no error. Fixed with an `OnDestroy()` completion guarantee that force-completes the transition and fires `OnShiftStateChanged` before teardown. Also corrected: the "Dormant costs nothing" claim only holds for `ManualOnly` zones — `Automatic` zones must position-monitor continuously even while `Dormant`.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-05 — 4 findings, all fixed: (1) the `OnDestroy()` fix's own stated safety justification was itself wrong ("scene not active" doesn't gate a local Volume's spatial effect, per the GDD's own SOFT co-residency rule) — corrected to the real invariant, spatial distance via the Box Safety Margin's own sizing plus SOFT-transition timing; (2) confirmed no new inconsistency with `PersistentShiftIds`' `Shifting-In`-time write (independently verified against `gece-oturum-durumu-2026-08-02.md`); (3) the box-overlap check's real-world feasibility was unverified — added an explicit, formula-derived Minimum Zone Center Spacing guideline (~20-40m) and flagged MVP area feasibility as needing confirmation before the check ships as build-blocking; (4) Consequences → Negative and GDD Requirements Addressed hadn't been updated to reflect the 3 fixes — both extended.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Rendering |
| **Knowledge Risk** | HIGH (per `architecture.md`'s Engine Knowledge Gap Summary) **for the domain in general** — URP's `RenderGraph` API is a genuine post-cutoff architecture change (`RecordRenderGraph(RenderGraph, ContextContainer)` replaces the old `Execute(ScriptableRenderContext, ref RenderingData)` pattern). **This ADR's actual risk is LOW**, because the Decision below confirms no custom `ScriptableRendererFeature`/`RenderGraph` code is used anywhere — the entire mechanism is standard URP `Volume`/`Light` component manipulation, which is stable, pre-cutoff API. This ADR is the formal record of *why* the domain's HIGH risk doesn't apply here, not a case where it was overlooked. |
| **References Consulted** | `docs/engine-reference/unity/modules/rendering.md`, `docs/engine-reference/unity/breaking-changes.md`, `docs/engine-reference/unity/current-best-practices.md`, `prototypes/yankilar-volume-weight-spike/` (empirical validation, not just docs) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None beyond what the cited prototype spike already confirmed — `Volume.weight` driven directly per-frame from script, `blendDistance=0`, is confirmed working via an actual empirical spike (2026-08-01), not just architectural reasoning. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (In-Memory Static Service Pattern) — `OnShiftStateChanged` subscribers (Adaptif Ses, Anlatı Durum, Gece/Oturum Durumu) follow that pattern; this ADR's own zone/ticker components are plain `MonoBehaviour`s, not session-state services, so they don't participate in `FoundationBootstrap` themselves |
| **Enables** | ADR-0007 (Clue Tracking Architecture), the future Audio Architecture ADR (#9) — both subscribe to `OnShiftStateChanged`, defined here |
| **Blocks** | Any story implementing a memory-trigger zone, the automatic ambient shift zone, or any `OnShiftStateChanged` consumer |
| **Ordering Note** | `Gece/Oturum Durumu`'s own dedicated ADR (Required ADRs #6) is not yet written — this ADR's `IsShiftPersistent`/reload-restore behavior references that system's `PersistentShiftIds`/`HasFired` reads, but does not depend on its ADR being Accepted first, since ADR-0001 already establishes the general session-state pattern both systems follow |

## Context

### Problem Statement

`isik-volume-durum-sistemi.md` already fully specifies this system's contract (`TriggerShift`/`RevertShift`/`IsShiftActive`/`IsShiftPersistent`/`GetStingerAudioRadius`, `OnShiftStateChanged` event, the `ShiftProgress = 3x²-2x³` smoothstep formula, the Box Collider Safety Margin formula, and the "shared Volume Profile, `Volume.weight` driven directly by `ShiftProgress`, `blendDistance=0`" mechanism — the last of these already empirically validated by `prototypes/yankilar-volume-weight-spike/`, not just designed on paper). `architecture.md`'s Module Ownership phase already made the single highest-stakes engine decision (no custom `RenderGraph` pass needed). What remains undecided, and what this ADR resolves, is the concrete `MonoBehaviour`/coroutine shape that drives this mechanism zone-by-zone, and where a zone's Light array is authored.

### Constraints
- `Volume.weight`'s only writer must be this system's own ticker (`isik-volume-durum-sistemi.md` Core Rules, "tek yazıcısı") — no other system may touch it.
- Every `Light` referenced in a `ShiftConfig` must be Light Mode = **Mixed**, never Baked — a hard, silent-failure-risk engine constraint (Baked lights are excluded from the real-time forward pass) already flagged in the GDD as critical.
- Real-time shadowed lights capped at 2-3 per room (performance budget, already in `architecture.md` Module Ownership).
- `ShiftProgress` must be recomputed fresh from the current `x` every frame, never stored/inverted — an interrupt (`TriggerShift` during Shifting-Out, or `RevertShift` during Shifting-In) must flip the sign of the per-tick `x` delta and continue from wherever it was, with no pop.
- Overlapping trigger zones must not share lights (manual level-design discipline, no automated tooling required per the GDD).
- **(unity-specialist validation, 2026-08-05, BLOCKING finding)** Two `ShiftZone`s' Volume-trigger **boxes** must not overlap in the same scene — a stricter, separate requirement from "must not share lights." Every zone shares one `VolumeProfile` asset; if two zones' boxes overlap, both zones' `Volume`s can be simultaneously weighted, and URP's stack composites multiple weighted local Volumes referencing the same profile as sequential `Lerp` over-compositing, not a simple average — non-linear amplification of the *entire screen-wide* grade (White Balance/Color Adjustments), invisible to a designer reasoning only about lights. This is reachable even with zero shared lights, because the Box Collider Safety Margin formula (below) deliberately oversizes the trigger box well beyond the light-affected `R_trigger` radius specifically to outlast the Shifting-Out ramp — two zones can have non-overlapping `R_trigger` circles while their (much larger) boxes overlap.
- **Minimum Zone Center Spacing (added, TD-ADR review, 2026-08-05)** — derived directly from the Box Collider Safety Margin formula, not previously stated anywhere: two `ShiftZone`s' centers must be at least `BoxHalfExtentMin_A + BoxHalfExtentMin_B` apart to guarantee non-overlapping boxes. The GDD's own worked examples put `BoxHalfExtentMin` around 10-20m depending on `R_trigger`, against this project's 4m×4m modular grid (`art-bible.md` §3.2) — meaning legitimate zone placements likely require 20-40m of separation, a real level-design constraint level designers must plan around *before* hitting the build-blocking check, not discover after. This number is a guideline for content authoring, not a hard API contract — MVP's actual area footprints (Depo, Servis Koridoru, Balo Salonu) should get an explicit level-design feasibility check against this spacing requirement (each area needs 2-3 memory triggers, and one area also needs the mandatory `Automatic` zone) before this validation check ships as build-blocking, since a spacing requirement that legitimate MVP content can't actually satisfy would be a check that blocks correct work, not just catches bugs.
- The zone ticker must correctly handle the SOFT-transition co-residency window (position-sampling frozen when the zone's scene ≠ active scene, but time-based progress for an in-flight shift never freezes) — already resolved in `architecture.md` Data Flow §4, restated here as a hard requirement on the concrete ticker implementation.

## Decision

**Each named zone is a `MonoBehaviour` (`ShiftZone`) placed in the scene, carrying: a local URP `Volume` component (`isGlobal=false`, referencing the one shared project-wide `VolumeProfile` asset), a box `Collider` (`isTrigger=true`, sized per the Box Collider Safety Margin formula), a `shiftId` string, a `TriggerMode` enum (`Automatic`/`ManualOnly`), and an Inspector-assigned array of `(Light, baseColor, memoryColor, baseIntensity, memoryIntensity)` tuples — level-designer-authored, not auto-discovered.** When a shift starts (`TriggerShift` called externally, or the zone's own proximity check for `Automatic` zones), the zone starts a **per-zone `Coroutine`** that advances its own `x`/`ShiftProgress` every frame, writes `Volume.weight = ShiftProgress` directly, and lerps each of its own Light array entries' color/intensity by the same `ShiftProgress` value in lockstep — matching the GDD's own "tek per-zone ticker coroutine" language exactly.

**Correction (unity-specialist validation, 2026-08-05, BLOCKING findings, fixed below in full detail — see Decision subsections, Risks, and Key Interfaces)**: two claims in this paragraph's original draft were incomplete or wrong and are corrected here:
1. "A `Dormant` zone runs no coroutine and costs nothing per-frame" was only true for `ManualOnly` zones. `Automatic` zones must continuously position-sample for `R_trigger` entry *while Dormant* — this is a real, GDD-required per-frame cost this ADR's original `TickShift()` sketch never showed. See "Automatic-zone monitoring" below.
2. The Decision said nothing about what happens if a `ShiftZone`'s `GameObject` is destroyed (scene unload) while mid-transition — a real, reachable race (see "Scene-unload completion guarantee" below) that could silently and permanently drop a narrative clue reveal.

### Automatic-zone monitoring (closes Finding 2a)

`TriggerMode.Automatic` zones start a lightweight **position-monitor coroutine in `OnEnable()`** (not on external trigger) that runs for the zone's entire lifetime while `Dormant`, checking `Vector3.Distance(player, zoneCenter)` against `R_trigger` once per frame (or at a coarser fixed interval if profiling ever shows this matters — not needed at MVP's single-automatic-zone-per-night scale). `TriggerMode.ManualOnly` zones start nothing in `OnEnable()` — they remain genuinely zero-cost while `Dormant`, exactly as originally claimed, since they can only ever be started by an explicit external `TriggerShift` call. Once **any** zone (either mode) leaves `Dormant`, the same coroutine also takes over `R_exit` hysteresis checking (both modes need this) and the `ShiftProgress`/`Volume`/`Light` tick — one coroutine, two responsibilities gated by state, not two separate coroutines.

### Scene-unload completion guarantee (closes Finding 2b — the serious one)

**Problem, traced precisely**: `seviye-sahne-gecisi.md`'s SOFT transition defers `UnloadSceneAsync` on the origin scene by 0.5-2s after `Complete`, specifically to keep the zero-frame guarantee out of the critical path — but during that window, an origin-scene `ShiftZone` mid-`Shifting-In` (`Duration≈3.0s`) can have its `GameObject` destroyed (synchronous `OnDestroy`, coroutine killed with no warning) before it ever reaches `Held`. Since `Anlatı Durum/İpucu Takibi` only reveals a clue on `newState==Held` (never on `Shifting-In`), this silently and **permanently** drops that clue reveal for the rest of the night — chaining the two GDDs' own numbers (a `Preloading` near its 2s floor + a shift started just before the transition request), this is reachable, not a contrived edge case.

**Fix**: `ShiftZone.OnDestroy()` checks `_state != Dormant`. If mid-transition, it **synchronously forces the transition to its natural terminal state** (`Shifting-In`→`Held`, `Shifting-Out`→`Dormant`) — setting `Volume.weight`/light values to their final target instantly and firing `OnShiftStateChanged` with the terminal state — **before** the object finishes tearing down.

**Safety justification, corrected (TD-ADR review, 2026-08-05)**: the original draft argued this is safe because the origin scene is "never active" by the time `OnDestroy` fires — that reasoning is **wrong**, and the TD-ADR review caught it directly: `SceneManager.SetActiveScene` only controls default-instantiation-target and which scene's `RenderSettings` resolve (per `seviye-sahne-gecisi.md`'s own RenderSettings Core Rule) — it has **no effect** on whether a local (`isGlobal=false`) `Volume` spatially affects the camera, which depends purely on collider containment, not scene-active status. This is exactly why `isik-volume-durum-sistemi.md`'s own Core Rules had to separately add the "tick freezes for inactive-scene zones, but time-based progress never freezes" rule — zones in an inactive-but-loaded scene remain spatially live during the SOFT co-residency window, which directly contradicts the original "not active = not visible" claim.

**The real safety argument is spatial distance, not scene-active status**: the Box Collider Safety Margin formula (`BoxHalfExtentMin = R_exit + PlayerMaxSpeed×Duration + SafetyBuffer`) guarantees the trigger box only through the Shifting-Out ramp (~3s) plus a small buffer — worked-example boxes run ~10-20m half-extent (20-40m diameter). By the time a SOFT transition's deferred unload actually fires — `Preloading`'s own pacing floor (2-8s) + the swap + the 0.5-2s unload delay, on top of however long the player walked to reach the transition trigger after the shift began — the player's camera is, by construction, already well outside the ~20-40m box the shift's own Volume is scoped to. **This is the actual invariant this fix relies on**, and it's a spatial-distance argument, not a scene-activity one — corrected throughout this section and in Risks.

### Architecture Diagram

```
Scene-placed ShiftZone (MonoBehaviour)
   │
   ├── Volume (isGlobal=false, shared VolumeProfile, blendDistance=0)
   ├── BoxCollider (isTrigger=true, sized per Box Collider Safety Margin)
   ├── string shiftId
   ├── TriggerMode { Automatic, ManualOnly }
   └── (Light, baseColor, memoryColor, baseIntensity, memoryIntensity)[]
         — Inspector-assigned, level-designer-authored

TriggerShift(shiftId, config) [external call, e.g. from Anı-Tetikleyici]
   │
   ▼
StartCoroutine(TickShift())  ── one coroutine per active zone
   │
   │  every frame:
   │    x = clamp(elapsed / Duration, 0, 1)
   │    ShiftProgress = 3x² - 2x³            (recomputed fresh, never stored)
   │    Volume.weight = ShiftProgress         (THE only writer of this field)
   │    foreach (light, base, memory, baseI, memI) in lightArray:
   │        light.color = Lerp(base, memory, ShiftProgress)
   │        light.intensity = Lerp(baseI, memI, ShiftProgress)
   │
   ▼
OnShiftStateChanged(shiftId, newState, zoneCenter, radius)  ── fired on
   real state transitions (Dormant→Shifting-In→Held→Shifting-Out→Dormant)
```

### Key Interfaces

```csharp
public enum ShiftState { Dormant, ShiftingIn, Held, ShiftingOut }
public enum TriggerMode { Automatic, ManualOnly }

[System.Serializable]
public struct ZoneLight {
    public Light light;              // must be Light Mode = Mixed (edit-time validated)
    public Color baseColor;
    public Color memoryColor;
    public float baseIntensity;
    public float memoryIntensity;
}

public sealed class ShiftZone : MonoBehaviour {
    [SerializeField] private string _shiftId;
    [SerializeField] private TriggerMode _triggerMode;
    [SerializeField] private ZoneLight[] _lights;   // Inspector-assigned
    [SerializeField] private Volume _volume;         // isGlobal=false, shared profile

    private ShiftState _state = ShiftState.Dormant;
    private float _x;                                 // 0..1, direction-flippable on interrupt
    private Coroutine _tickCoroutine;

    public bool TriggerShift(ShiftConfig config) { /* starts/redirects _tickCoroutine */ return true; }
    public void RevertShift() { /* no-op if Dormant; else reverses direction */ }
    public bool IsShiftActive => _state != ShiftState.Dormant;
    public bool IsShiftPersistent { get; private set; }   // from the config that last triggered this zone
    public float StingerAudioRadius { get; private set; } // from the same config

    void OnEnable() {
        // Automatic zones self-monitor for R_trigger entry even while Dormant;
        // ManualOnly zones start nothing here — genuinely zero-cost until an
        // external TriggerShift call (fix per unity-specialist validation, 2026-08-05)
        if (_triggerMode == TriggerMode.Automatic) _tickCoroutine = StartCoroutine(MonitorAndTick());
    }

    void OnDestroy() {
        // Scene-unload completion guarantee (fix per unity-specialist validation,
        // 2026-08-05, BLOCKING finding) — force any in-flight transition to its
        // terminal state and fire the event before teardown, so a Held reveal
        // (and anything gated on it, e.g. Anlatı Durum's clue-known write) is
        // never silently dropped by a mid-transition scene unload. Safe because
        // of SPATIAL DISTANCE, not scene-active status (corrected, TD-ADR review,
        // 2026-08-05 — scene-active does not gate a local Volume's effect): by
        // the time a deferred unload fires, the camera is well outside this
        // zone's Box-Safety-Margin-sized trigger box, so the instant, non-eased
        // jump to terminal values is never actually seen.
        if (_state == ShiftState.ShiftingIn) ForceCompleteTo(ShiftState.Held);
        else if (_state == ShiftState.ShiftingOut) ForceCompleteTo(ShiftState.Dormant);
    }

    // REVISED (2026-08-09 addendum, below): the original sketch declared
    // OnShiftStateChanged as a `public static event` here on ShiftZone.
    // Superseded — the event is owned by the IIsikVolumeState facade
    // (which every subscriber ADR already codes against as
    // `IsikVolumeDurumSistemi.Instance.OnShiftStateChanged`); a ShiftZone
    // raises it via the facade's internal RaiseShiftStateChanged(...).
    // See "Addendum (2026-08-09): the system-wide facade contract".
}
```

`Işık/Volume Durum Sistemi`'s public API (`TriggerShift(shiftId, config)`, `IsShiftActive(shiftId)`, etc. — the *system-wide*, `shiftId`-keyed versions consumers actually call, per ADR-0001's worked-example style) is a thin static lookup layer over the collection of `ShiftZone` instances currently in loaded scenes (itself a small, scene-scoped registry — not a Foundation session-state service, since `ShiftZone`s are ordinary scene objects that come and go with scene loads, unlike `InteractableRegistry`'s Foundation relocation in ADR-0004). ~~This lookup layer's own concrete shape (how it finds the right `ShiftZone` for a given `shiftId` across loaded scenes) is a small remaining implementation detail deferred to the implementation story~~ — **superseded by the addendum below**: the `/architecture-review` 2026-08-09 pass found four later ADRs (0006, 0007, 0009, 0015) building against a facade shape this ADR had never pinned, so the facade contract is now fixed here rather than left to implementation.

### Addendum (2026-08-09): the system-wide facade contract (closes review Finding T4)

Written as a follow-up to `/architecture-review` 2026-08-09, which found a contract-shape mismatch: this ADR's sketch put `OnShiftStateChanged` as a **static event on `ShiftZone`**, while ADR-0006/0007 (constructor subscriptions), ADR-0009 (`OnEnable` subscription), and ADR-0001's `ResetAll()` comments all treat it as an **instance event on `IsikVolumeDurumSistemi.Instance`** — and ADR-0015's in-place-reset regime conversion presupposes a stateful facade instance this ADR never declared. The facade is now pinned, matching what every subscriber already codes against:

```csharp
public interface IIsikVolumeState {
    bool TriggerShift(string shiftId, ShiftConfig config);   // routes to the registered ShiftZone; false if
                                                              // shiftId already active (no-op) — semantics
                                                              // unchanged from this ADR's main Decision
    void RevertShift(string shiftId);                         // silent no-op if not active / no such zone
    bool IsShiftActive(string shiftId);
    bool IsShiftPersistent(string shiftId);
    float GetStingerAudioRadius(string shiftId);
    event Action<string, ShiftState, Vector3, float> OnShiftStateChanged;
}

public sealed class IsikVolumeState : IIsikVolumeState {
    // shiftId → ShiftZone routing table. ShiftZones self-register in
    // OnEnable / self-deregister in OnDisable (the same scene-scoped
    // registration shape as ADR-0009's AmbientZoneVolume registry) —
    // this is the "thin lookup layer" the original text deferred, now
    // concrete: a Dictionary<string, ShiftZone>, keyed by the zone's
    // shiftId Core Rule field.
    private readonly Dictionary<string, ShiftZone> _zonesByShiftId = new();

    public event Action<string, ShiftState, Vector3, float> OnShiftStateChanged;

    internal void RegisterZone(ShiftZone zone) { /* add by shiftId; duplicate shiftId in loaded
                                                    scenes is already build-blocked (Edge Cases) */ }
    internal void DeregisterZone(ShiftZone zone) { /* remove */ }
    // Called by a ShiftZone on every genuine state transition (including
    // the OnDestroy completion guarantee) — the ONE raise path:
    internal void RaiseShiftStateChanged(string shiftId, ShiftState s, Vector3 center, float radius)
        => OnShiftStateChanged?.Invoke(shiftId, s, center, radius);

    internal void ResetOnLoad() {
        // IN PLACE (ADR-0015's regime — this facade has constructor-time
        // subscribers in GeceOturumDurumu/AnlatiDurumIpucuTakibi and a
        // persistent MonoBehaviour subscriber in AdaptifSesController;
        // the instance is never replaced). Clears _zonesByShiftId only —
        // zones re-register via their own OnEnable each session; the
        // event's delegate list is deliberately untouched (once-per-
        // process subscriptions survive every reset, per ADR-0015).
        _zonesByShiftId.Clear();
    }
    // TriggerShift/RevertShift/IsShiftActive/IsShiftPersistent/
    // GetStingerAudioRadius delegate to the routed ShiftZone (per-zone
    // state stays on the zone, exactly as the main Decision specifies);
    // queries for an unknown/unloaded shiftId return the Dormant-
    // equivalent defaults (false / false / 0f) rather than throwing.
}

public static class IsikVolumeDurumSistemi {
    private static readonly IsikVolumeState _current = new();   // never replaced (ADR-0015)
    public static IIsikVolumeState Instance => _current;
    internal static IsikVolumeState InternalInstance => _current;  // ShiftZone register/raise access —
                                                                    // ADR-0006/0009/0012's established
                                                                    // InternalInstance escape-hatch shape
    internal static void ResetOnLoad() => _current.ResetOnLoad();   // FoundationBootstrap.ResetAll(), in place
}
```

Consequences of pinning this: (1) `ShiftZone`'s formerly-static `OnShiftStateChanged` event is superseded — zones raise through `InternalInstance.RaiseShiftStateChanged(...)` (see the revised comment in the Key Interfaces sketch above); (2) ADR-0001's `ResetAll()` comment for this service ("exposes OnShiftStateChanged but subscribes to nothing itself — IN-PLACE reset") is now backed by a declared type instead of an implied one — no edit needed there, the comment was already correct; (3) every existing subscriber call site (`IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += …` in ADR-0006/0007/0009) compiles against this interface verbatim, which is the point — this addendum ratifies the shape the consumers already assumed rather than inventing a new one.

## Alternatives Considered

### Alternative 1: Custom `ScriptableRendererFeature`/`RenderGraph` Pass
- **Description**: Write a custom URP render pass (`RecordRenderGraph(RenderGraph, ContextContainer frameData)`) to apply the color/intensity shift directly in the render pipeline, rather than through standard `Volume`/`Light` components.
- **Pros**: Could theoretically offer more control over exactly how the blend is composited; matches Unity's current recommended pattern *if* a custom pass were actually needed.
- **Cons**: Every value this system needs to control (White Balance, Tint, Post Exposure, Saturation via Volume Profile; Light color/intensity via direct component access) is already fully achievable through standard, stable, pre-cutoff `Volume`/`Light` APIs — a custom pass would duplicate functionality URP already provides, while introducing the project's single highest-risk engine domain (`RenderGraph`, explicitly HIGH-risk/post-cutoff per `architecture.md`'s own Engine Knowledge Gap Summary) for zero functional gain.
- **Rejection Reason**: This decision was already made in `architecture.md`'s Module Ownership phase; this ADR formalizes it rather than reopening it. No requirement in `isik-volume-durum-sistemi.md` needs render-pipeline-level access — everything is expressible as component data manipulation.

### Alternative 2: Baked Lightmap Set Switching
- **Description**: Pre-bake two lightmap sets (reality/memory) per area and swap which set is active based on shift state, instead of driving real-time `Light` properties.
- **Pros**: Could look highly polished for static geometry, since baked GI captures indirect bounce lighting the direct-light-only approach here doesn't.
- **Cons**: Already explicitly rejected in `isik-volume-durum-sistemi.md`'s own "Yazım modeli kararı (açık soruyu çözer)" — baked-set swapping cannot animate smoothly through the `ShiftProgress` curve (lightmap sets are discrete, not blendable at runtime the way `Volume.weight`/`Light` properties are), and would require re-baking on every content iteration, a production-cost the project's own Art Bible (Section 6.2) already reasoned against for a different but related reason (baked shadows would "freeze" through a memory shift, violating "only light lies").
- **Rejection Reason**: Already closed in the GDD; restated here only for completeness, since this ADR is the concrete-implementation record for a decision the GDD itself already settled.

### Alternative 3: Auto-Discovered Zone Lights (Collider-Bounds Query) Instead of Inspector-Assigned Array
- **Description**: A `ShiftZone` automatically finds every `Light` within its own collider bounds at scene load, instead of a level designer manually assigning a `ZoneLight[]` array.
- **Pros**: Less manual Inspector work per zone; a light physically inside a zone's bounds is "obviously" that zone's light.
- **Cons**: "Physically inside the bounds" is not actually reliable for this project's zones — the Box Collider Safety Margin formula deliberately over-sizes the trigger collider well beyond the visually-affected area (to guarantee the collider catches the player before the shift's own ~3s ramp could be outrun), so an auto-discovery query would very plausibly catch lights that visually belong to an adjacent, unrelated space. Silent, hard-to-debug misattribution is a worse failure mode than a manual assignment step.
- **Rejection Reason**: The GDD's own contract already frames the light array as "`ShiftConfig` ışık dizisinde referans verilen" — an explicit, authored reference list, not a spatial query — and the manual "overlapping trigger zones must not share lights" discipline (Edge Cases) already assumes deliberate, visible authorship rather than inferred membership.

## Consequences

### Positive
- Zero custom `RenderGraph`/`ScriptableRendererFeature` code anywhere in the project's lighting system — the single highest engine-knowledge-risk domain in this project's entire architecture is fully sidestepped, not just minimized.
- Per-zone coroutines mean `Dormant` **`ManualOnly`** zones (the majority — MVP's 2-3 memory triggers are all `ManualOnly`) cost zero per-frame CPU, not just "low" cost. **(Corrected, unity-specialist validation, 2026-08-05)**: this does NOT extend to `Automatic` zones, which must position-monitor continuously even while `Dormant` (Decision, "Automatic-zone monitoring") — a real, GDD-required cost, negligible at MVP's single-automatic-zone-per-night scale but not zero.
- The Inspector-assigned light array makes zone/light ownership visually auditable in the Editor — a level designer or reviewer can see exactly which lights belong to which zone without needing to reason about spatial containment.
- This ADR's mechanism is not just architecturally sound but **empirically validated for the single-zone case** — `prototypes/yankilar-volume-weight-spike/` already confirmed the core `Volume.weight` direct-drive approach works as designed, a stronger evidentiary basis than most of this project's other ADRs have available. **Scope correction (unity-specialist validation, 2026-08-05)**: the prototype tested only Corridor C (one zone, one Volume) — the multi-zone Volume-stacking case (Constraints/Risks, box-overlap) was never built or tested; that gap is real and is what the new box-overlap validation check exists to prevent architecturally, since it can't yet be claimed empirically validated.

### Negative
- Manual Inspector assignment of each zone's light array is real, recurring level-design labor — every new memory-trigger or automatic zone requires a level designer to correctly enumerate and assign its lights, with no tooling to catch a forgotten or misassigned light beyond visual review.
- Per-zone coroutines, while individually cheap, mean N active shifts run N independent coroutines with no shared scheduling — acceptable at this project's content scale (MVP: 2-3 memory triggers + 1 automatic zone per night, `art-bible.md` Section 8.11's per-area prop ceilings bound this further) but not a pattern that would scale to hundreds of simultaneous zones without revisiting.
- **(TD-ADR review, 2026-08-05)** The `OnDestroy()` completion guarantee is a fifth state-machine exit path (alongside the four normal `Dormant→Shifting-In→Held→Shifting-Out→Dormant` transitions) that must be reasoned about and tested independently — a real, ongoing complexity cost this ADR's fixes introduced, not present in the original simpler design.
- **(TD-ADR review, 2026-08-05)** The Minimum Zone Center Spacing requirement (Constraints) is a real, potentially significant level-design cost — 20-40m of required separation between zone centers on a 4m×4m modular grid is a substantial planning constraint for level designers to work around, not a free correctness fix; MVP area feasibility against this spacing should be confirmed before the box-overlap check ships as build-blocking (see Constraints).

### Risks
- **Risk**: A level designer assigns a `Light` with Light Mode = Baked to a `ZoneLight` entry, which silently fails to respond to real-time intensity/color changes (Baked lights are excluded from the forward real-time pass) — a genuinely silent, hard-to-notice bug (the light would just never appear to shift). **Mitigation**: an edit-time `IPreprocessBuildWithReport` validation check (reusing this project's established two-tier validation pattern, e.g. `ani-tetikleyici-etkilesim.md`'s TriggerMode scene-scan check) that rejects any `ZoneLight.light` whose Light Mode isn't Mixed, build-blocking, not just an Editor warning.
- **Risk**: Two overlapping `ShiftZone`s are manually authored to share a `Light` reference (the GDD's own "must not share lights" discipline is manual, no automated tooling specified) — if it happens, both zones' coroutines would write conflicting color/intensity values to the same light on the same frame, with the visual result depending on coroutine execution order (Unity does not guarantee order between independent coroutines any more than it guarantees `RuntimeInitializeOnLoadMethod` order, per this session's own repeated finding in ADR-0001/ADR-0004). **Mitigation**: extend the same edit-time validation check above to also flag a `Light` referenced by more than one `ShiftZone` in the same scene — cheap to add alongside the Light-Mode check, closes a class of bug this ADR would otherwise leave as "manual discipline only."
- **Risk (BLOCKING, unity-specialist validation, 2026-08-05 — found and fixed)**: Two `ShiftZone`s' Volume-trigger **boxes** overlap (see Constraints) even with zero shared lights, because the Box Collider Safety Margin formula deliberately oversizes the box well beyond the light-affected radius. Both zones' `Volume`s can be simultaneously weighted, and since they share one `VolumeProfile` asset, URP's local-Volume stack composites this as sequential over-`Lerp`, not averaging — producing a non-linear, screen-wide grade amplification that no light-level review would catch. The `yankilar-volume-weight-spike` prototype only tested a single zone in isolation; this case was never empirically validated. **Fixed**: the same edit-time `IPreprocessBuildWithReport` scene-scan (already planned for the Baked-light and shared-light checks, reusing one scene-open/close pass for all three) also flags any two `ShiftZone`s in the same scene whose Volume-trigger box `Bounds` intersect — build-blocking, same severity as the other two checks.
- **Risk**: `Volume.weight`'s "single writer" invariant (this system only) has no compiler enforcement — any future system could technically call `volume.weight = ...` directly on a `ShiftZone`'s `Volume` component if it obtained a reference. **Mitigation**: `_volume` is a private `[SerializeField]`, not exposed on `ShiftZone`'s public API at all (see Key Interfaces) — the invariant is enforced by encapsulation, not just documentation, which is a real (if not airtight) protection.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|----------------------------|
| `isik-volume-durum-sistemi.md` | Per-zone independent `Volume`, shared `VolumeProfile`, `Volume.weight` driven directly by `ShiftProgress`, `blendDistance=0` | `ShiftZone`'s `Volume` field + `TickShift()` coroutine, exactly as validated by `prototypes/yankilar-volume-weight-spike/` |
| `isik-volume-durum-sistemi.md` | Every referenced `Light` must be Mode=Mixed | `ZoneLight.light` + new build-blocking edit-time validation (Risks) |
| `isik-volume-durum-sistemi.md` | Real-time shadowed lights capped at 2-3/room; single per-zone ticker, not per-light `Update()` | `TickShift()` is one coroutine per zone driving its whole light array, not one per light |
| `isik-volume-durum-sistemi.md` | `Volume.weight`'s single writer | Enforced by `_volume` being private, never exposed |
| `isik-volume-durum-sistemi.md` | Overlapping zones must not share lights | Extended into the new cross-zone Light-uniqueness validation check (Risks) |
| `architecture.md` Module Ownership | No custom RenderGraph pass needed | This ADR is the formal record confirming and detailing that decision |
| `isik-volume-durum-sistemi.md` Core Rules ("`x`, sahnesi aktif olmasa bile ilerlemeye devam eder") | Time-based progress for an in-flight shift never freezes during the SOFT co-residency window, even when the zone's own scene isn't active | **(added, TD-ADR review, 2026-08-05)** Directly implemented by the fact that `Coroutine`s on a still-loaded (not-yet-unloaded) `GameObject` tick normally regardless of `SceneManager.GetActiveScene()` — confirmed by unity-specialist validation; the `OnDestroy()` completion guarantee further ensures the *terminal* state is reached even if the object is destroyed before natural completion |
| `isik-volume-durum-sistemi.md` `TriggerMode` Core Rule (`Automatic` zones self-trigger on `R_trigger` entry) | An `Automatic` zone must position-monitor continuously while `Dormant` to ever detect entry, since `ManualOnly` zones never do | **(added, TD-ADR review, 2026-08-05)** `ShiftZone.OnEnable()` starts a position-monitor coroutine for `Automatic`-mode zones only; `ManualOnly` zones start nothing until externally triggered |

## Performance Implications
- **CPU**: Negligible for MVP content scale — at most 2-3 memory-trigger zones + 1 automatic zone active per night, each running one lightweight coroutine only while `Shifting-In`/`Held`/`Shifting-Out` (not `Dormant`). Well within the 16.6ms frame budget.
- **Memory**: Negligible — one `Volume` component + a small `ZoneLight[]` array per zone, bounded by MVP's small content scope.
- **Load Time**: None beyond ordinary scene load — no custom render pipeline asset changes.
- **Network**: N/A.

## Migration Plan
N/A — greenfield.

## Validation Criteria
- A `[UnityTest]` places a `ShiftZone` with 2 `ZoneLight` entries in a test scene, calls `TriggerShift`, and asserts `Volume.weight` and both lights' color/intensity track `ShiftProgress` in lockstep across several frames — the "compound light+sound" contract's light half, tested directly.
- A `[Test]` (edit-time) confirms a `ZoneLight` referencing a Baked-mode `Light` fails the new build-blocking validation check.
- A `[Test]` confirms two `ShiftZone`s in the same scene referencing the same `Light` instance fails the new cross-zone uniqueness validation check.
- A `[UnityTest]` interrupts an in-flight `Shifting-In` with `RevertShift` mid-transition and confirms `x`'s direction flips without a visible pop (continuity from current value, not a reset to 0).
- **(unity-specialist validation, 2026-08-05 — new, closes the BLOCKING scene-unload finding)** A `[UnityTest]` starts a `ShiftZone`'s `Shifting-In`, destroys its `GameObject` mid-transition (simulating a scene unload before `Held` is reached), and confirms `OnShiftStateChanged(shiftId, Held, ...)` still fires exactly once, synchronously, from `OnDestroy` — the concrete, automatable proof the clue-reveal-drop bug is actually closed, not just architecturally described.
- **(unity-specialist validation, 2026-08-05 — new, closes the BLOCKING Volume-overlap finding)** A `[Test]` (edit-time) places two `ShiftZone`s with overlapping Volume-trigger box `Bounds` (but zero shared lights) in a test scene and confirms the new build-blocking validation check fails the build.
- A `[UnityTest]` confirms an `Automatic`-mode zone's position-monitor coroutine starts in `OnEnable()` and detects `R_trigger` entry while still `Dormant`; a `[UnityTest]` confirms a `ManualOnly`-mode zone starts no coroutine at all until `TriggerShift` is called externally — verifying the corrected, mode-scoped "Dormant cost" claim.

## Related Decisions
- `docs/architecture/architecture.md` — Module Ownership (`Işık/Volume Durum Sistemi` row, the original no-RenderGraph-pass decision this ADR formalizes and details), Engine Knowledge Gap Summary (this ADR is the concrete resolution of the HIGH-risk `RenderGraph` flag for this specific system).
- `prototypes/yankilar-volume-weight-spike/` — the empirical validation this ADR's core mechanism is built on, not just architectural reasoning.
- ADR-0001 (In-Memory Static Service Pattern) — `OnShiftStateChanged`'s Foundation-layer subscribers (Gece/Oturum Durumu, Anlatı Durum, Adaptif Ses) follow that ADR's pattern; `ShiftZone` itself does not, since it's an ordinary scene-scoped `MonoBehaviour`, not session-state.
- ADR-0004 (InteractableRegistry Foundation Ownership) — contrasting precedent: that ADR's registry needed a Foundation-layer, session-surviving home because interactables' *registration* had to survive scene swaps for FPC's approach-taper read; `ShiftZone`s don't need the equivalent, since nothing reads "all zones across all scenes" the way FPC reads "all interactables in the current scene."
