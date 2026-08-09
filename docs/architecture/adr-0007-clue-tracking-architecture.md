# ADR-0007: Clue Tracking Architecture

> **Unity Specialist Validation**: BLOCKING (1 finding, found and fixed) 2026-08-06 — the draft's `Resources.Load<ClueRegistry>("ClueRegistry")` design was justified with a fabricated citation claiming the project doesn't use Addressables; the project's own `current-best-practices.md`/`deprecated-apis.md` actually prefer Addressables and list `Resources.Load()` as deprecated. Redesigned: `ClueRegistry` now loads via `Addressables.LoadAssetAsync(...).WaitForCompletion()`, deferred out of the constructor to a lazy first-use inside the `OnShiftStateChanged(Held)` handler — this also resolves a separate MINOR finding (Resources.Load's constructor-time load was the first-ever engine-API call from `FoundationBootstrap.ResetAll()`'s ultra-early `SubsystemRegistration` code path; deferring past boot sidesteps that uncertainty). A second MINOR finding (ADR-0001's own Validation Criteria section still listed the pre-ADR-0006 `FoundationBootstrap.ResetAll()` order) was also fixed, directly in ADR-0001.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-06 — 1 finding, fixed: Decision → Edit-time validation only described 2 of the 3 build-blocking checks that Consequences → Risks and Validation Criteria already assumed existed (the third — resolving the `"ClueRegistry"` Addressable key itself, not just its contents — was mentioned as a deferred mitigation in Risks but never promoted into Decision's actual validator description). Decision now describes all 3 checks in one place. Verified clean otherwise: the `FoundationBootstrap.ResetAll()` ordering claim, GDD/architecture.md/registry cross-references, the lazy-load redesign's safety against the GDD's direct-`MarkClueKnown`-bypass path, and all 4 Alternatives-Considered entries.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-06

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (C# state management / ScriptableObject data / Editor tooling) |
| **Knowledge Risk** | LOW — reuses ADR-0001's already-verified static-service pattern; `ScriptableObject`, `Addressables.LoadAssetAsync`/`WaitForCompletion`, `EditorSceneManager.sceneOpened`/`sceneSaved`, and `IPreprocessBuildWithReport` are all long-stable, pre-cutoff APIs. This ADR is, however, this project's **first actual Addressables consumer** (`architecture.md` line 22 notes Addressables is part of the declared stack but "not currently used by any GDD" as of that writing) — the mechanism is stable, but this is new *usage*, not a new API. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/current-best-practices.md`, `docs/engine-reference/unity/deprecated-apis.md`, `docs/architecture/architecture.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md`, `docs/architecture/adr-0006-session-state-service-and-round-counter-ownership.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Confirm `Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry").WaitForCompletion()` resolves correctly the first time it's called from gameplay code (i.e., well after full engine/Addressables-system initialization — see Decision for why this load is deliberately deferred out of the constructor to avoid any `SubsystemRegistration`-time uncertainty). |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (In-Memory Static Service Pattern) — this system is the pattern's second full instantiation after `Gece/Oturum Durumu`. ADR-0005 (Işık/Volume Rendering Architecture) — this ADR's only trigger is Işık/Volume's `OnShiftStateChanged`. |
| **Enables** | Future "Dialogue Callback Selection Timing" ADR (Diyalog/Anlatı İçeriği, Required ADR #12) — will consume `IsClueKnown`/`GetKnownClueIds` as given here. |
| **Blocks** | Any story implementing `Anlatı Durum/İpucu Takibi` itself; any Diyalog/Anlatı İçeriği story that gates a callback on clue state. |
| **Ordering Note** | Participates in `FoundationBootstrap.ResetAll()` (ADR-0001, reordered by ADR-0006) at its existing position — after `Işık/Volume Durum Sistemi`, since it subscribes to `OnShiftStateChanged` in its own constructor. No further reordering needed; already correctly placed by ADR-0006's fix. |

## Context

### Problem Statement

`anlati-durum-ipucu-takibi.md` (Approved) fully specifies this system's rules — `ClueDefinition` records (`clueId`, `requiredShiftIds[]`, ALL-semantics, N:1), a static/singleton persistence mechanism identical in shape to `Gece/Oturum Durumu`'s, first-access-time subscription to `Işık/Volume`'s `OnShiftStateChanged` (deliberately not `Awake`/`OnEnable`-based), a centralized single-source-of-truth `ClueDefinition` registry, and edit-time validation for two build-blocking authoring errors (empty `requiredShiftIds`, duplicate `clueId`) plus one non-blocking authoring warning (orphaned `shiftId`, via `ClueConsistencyValidator.ValidateScene(sceneId)`). None of this has a concrete Unity implementation mechanism yet — `architecture.md`'s own Module Ownership/API Boundaries sketch for this system (lines 284–292) gives the four public methods but not the internal data structures, the registry-loading mechanism, or how/when the two validation tiers actually run.

This ADR is the concrete implementation contract: the static-service shape (reusing ADR-0001), the `ClueDefinition` ScriptableObject + central registry + Addressables-based lazy loading, the shiftId→`ClueDefinition` reverse index, and the Editor-time validation mechanism for all three authoring-error checks.

### Constraints

- Must reuse ADR-0001's static-service/interface/static-facade pattern exactly.
- Must not deviate from `anlati-durum-ipucu-takibi.md`'s already-Approved, twice-revised Core Rules (subscription timing, idempotency, ALL-semantics, no sequencing data ever exposed) — this ADR formalizes, it does not redesign.
- `GetKnownClueIds()` must return `IReadOnlyCollection<string>`, matching the GDD's own signature (not `IReadOnlySet<string>`, which `architecture.md`'s sketch used — see Decision for why that's corrected here).
- Must participate correctly in `FoundationBootstrap.ResetAll()` (ADR-0001/ADR-0006) — verify its existing post-Işık/Volume position is still correct, not introduce a new ordering defect.

### Requirements

- `MarkClueKnown`/`IsClueKnown`/`GetKnownClueIds`/`OnClueKnown` must match `architecture.md`'s already-reviewed API Boundaries sketch, corrected for the `IReadOnlySet`→`IReadOnlyCollection` fix.
- The `Held`-only `OnShiftStateChanged` handling, idempotent `MarkClueKnown`, and first-access (not `Awake`/`OnEnable`) subscription timing from `anlati-durum-ipucu-takibi.md` Core Rules must be preserved exactly.
- Edit-time validation for empty `requiredShiftIds` and duplicate `clueId` must be build-blocking (per GDD AC#8a/8b); orphaned-`shiftId` validation must be Editor-only, non-blocking (per user decision, 2026-08-06 — see Decision).

## Decision

### Corrected from architecture.md's sketch: `GetKnownClueIds()` return type

`architecture.md`'s Phase 4 API Boundaries sketch for this system (line 288) used `IReadOnlySet<string> GetKnownClueIds()`. This is the same class of risk unity-specialist validation caught in ADR-0006 for `Gece/Oturum Durumu`'s interface: `IReadOnlySet<T>` is a .NET 5+ type, not guaranteed available under Unity's supported Api Compatibility Level profiles (.NET Standard 2.1 / .NET Framework) — and it also silently diverges from `anlati-durum-ipucu-takibi.md`'s own signature, which specifies `IReadOnlyCollection<string> GetKnownClueIds()` (Interactions with Other Systems, "Genel sorgu/yazma sözleşmesi"). Corrected here to `IReadOnlyCollection<string>`, matching the GDD verbatim and avoiding the BCL risk — caught proactively this time, before a specialist review had to find it a second time.

### Data model

```csharp
public interface IAnlatiDurumState {
    bool IsClueKnown(string clueId);
    IReadOnlyCollection<string> GetKnownClueIds();
    void MarkClueKnown(string clueId);

    event Action<string> OnClueKnown;
}

public sealed class AnlatiDurumState : IAnlatiDurumState {
    private readonly HashSet<string> _knownClueIds = new();
    private readonly HashSet<string> _seenShiftIds = new();

    // shiftId -> every ClueDefinition that lists it in requiredShiftIds.
    // Lazily built on first real OnShiftStateChanged(Held) — NOT in the
    // constructor — specifically to defer the Addressables load past
    // any SubsystemRegistration-time uncertainty. See "Registry loading"
    // below for the full reasoning.
    private Dictionary<string, List<ClueDefinition>> _byRequiredShiftId;

    public AnlatiDurumState() {
        // The event subscription itself happens here, at first-access
        // construction time, per anlati-durum-ipucu-takibi.md Core Rules
        // — deliberately NOT deferred, unlike the registry load below.
        // Subscribing touches only a plain C# event, no engine asset API,
        // so it carries none of the Addressables-readiness risk the
        // registry load does.
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += OnShiftStateChanged;
    }

    private void EnsureRegistryLoaded() {
        if (_byRequiredShiftId != null) return;
        var handle = Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry");
        var registry = handle.WaitForCompletion();
        _byRequiredShiftId = BuildReverseIndex(registry.Definitions);
    }

    private void OnShiftStateChanged(string shiftId, ShiftState newState, Vector3 zoneCenter, float radius) {
        if (newState != ShiftState.Held) return;
        EnsureRegistryLoaded();  // first real Held event — engine is fully initialized by now
        _seenShiftIds.Add(shiftId);  // idempotent — safe on Persistent re-fire (GDD Edge Cases)
        if (!_byRequiredShiftId.TryGetValue(shiftId, out var candidates)) return;
        foreach (var def in candidates) {
            if (_seenShiftIds.IsSupersetOf(def.RequiredShiftIds)) {
                MarkClueKnown(def.ClueId);
            }
        }
    }

    public void MarkClueKnown(string clueId) {
        if (!_knownClueIds.Add(clueId)) return;  // idempotent no-op, per Core Rules
        OnClueKnown?.Invoke(clueId);
    }

    public bool IsClueKnown(string clueId) => _knownClueIds.Contains(clueId);
    public IReadOnlyCollection<string> GetKnownClueIds() => _knownClueIds;
    public event Action<string> OnClueKnown;
}

public static class AnlatiDurumIpucuTakibi {
    public static IAnlatiDurumState Instance => _current;
    internal static void ResetOnLoad() => _current.ResetOnLoad();
    // CONVERTED TO IN-PLACE by ADR-0015 (2026-08-08): previously replaced
    // _current. Under the in-place regime (all event-involved facades
    // converted together — see ADR-0015's Constraints), this facade's
    // constructor-time Işık/Volume subscription runs once per process on
    // a never-replaced instance; a replacement reset here would have left
    // each discarded instance's handler permanently subscribed to the
    // now-persistent Işık/Volume event (stale-handler accumulation).
    // The instance ResetOnLoad() clears KnownClueIds/_seenShiftIds in
    // place; the lazily-loaded ClueRegistry cache is PRESERVED across
    // sessions (immutable config asset, no reload needed — and ResetOnLoad
    // must never touch Addressables per
    // engine_asset_api_call_in_foundation_constructor).
    private static AnlatiDurumState _current = new();
}
```

### Registry loading: Addressables, lazily loaded (not `Resources.Load`, not eager)

**Corrected during unity-specialist validation (2026-08-06)**: the first draft of this ADR chose `Resources.Load<ClueRegistry>("ClueRegistry")`, loaded eagerly in the constructor, and justified it with a citation to `docs/engine-reference/unity/VERSION.md` claiming "this project doesn't use Addressables" — **that citation was fabricated; `VERSION.md` contains no such note.** The project's actual, consulted-but-previously-unengaged guidance points the other way: `current-best-practices.md` has a section titled "Use Addressables (Not Resources)," and `deprecated-apis.md` explicitly lists `Resources.Load()` as deprecated for this pinned Unity 6.3 version, with Addressables named as the replacement. `architecture.md` line 22's actual note is narrower than the fabricated one — Addressables is part of the declared stack (`CLAUDE.md` Technology Stack) but "not currently used by any GDD... no action needed at MVP scope" — a scoping note, not a project decision to avoid it.

Surfaced to the user as a real trade-off (`AskUserQuestion`, 2026-08-06) rather than silently re-deciding: `Resources.Load` has zero initialization-order risk at the ultra-early `SubsystemRegistration` timing `FoundationBootstrap.ResetAll()` uses (no separate subsystem to be ready), but explicitly contradicts the project's own documented API preference. Addressables matches project convention, but its own initialization dependency being ready that early was genuinely uncertain — no prior Foundation-service constructor in this project has called *any* engine asset-loading API from `FoundationBootstrap.ResetAll()`'s code path before (ADR-0001 through ADR-0006 only ever touch plain `HashSet`/`Dictionary` fields and C# event subscriptions there).

**User chose Addressables, with the load deferred out of the constructor entirely** (`EnsureRegistryLoaded()`, called lazily from `OnShiftStateChanged`'s first real `Held` invocation, not from `AnlatiDurumState`'s constructor) — this resolves both problems at once: it matches the project's documented Addressables preference, and it sidesteps the `SubsystemRegistration`-timing question altogether, since a real memory-trigger reaching `Held` can only happen well after full engine/Addressables-system initialization (it requires actual gameplay — the player physically triggering a memory shift — which cannot occur during the boot sequence). `ClueRegistry` is a single project-level `ScriptableObject` asset (`[CreateAssetMenu]`), marked Addressable under a fixed key `"ClueRegistry"`. `AsyncOperationHandle<T>.WaitForCompletion()` is used for a synchronous load — acceptable here specifically because it fires at most once per session, on a rare, low-frequency gameplay event (not per-frame, not during a loading screen where a hitch would be visible), for one small asset. `ClueDefinition` is a second `[CreateAssetMenu]` `ScriptableObject` (`ClueId: string`, `RequiredShiftIds: List<string>`); `ClueRegistry.Definitions` is an Inspector-populated `List<ClueDefinition>`.

### Edit-time validation (user decision, 2026-08-06: orphaned-shiftId check is Editor-only, not runtime)

Two validation tiers, matching the GDD's own build-blocking/warning-only split exactly:

1. **Build-blocking** (`requiredShiftIds` empty, duplicate `clueId` across two `ClueDefinition` assets, **or** an unresolvable `"ClueRegistry"` Addressable key) — an `IPreprocessBuildWithReport.OnPreprocessBuild` implementation (a) resolves the `"ClueRegistry"` Addressable key via `AddressableAssetSettings` (not a runtime load) and throws `BuildFailedException` if it doesn't resolve, then (b) scans every `ClueDefinition` referenced by `ClueRegistry.Definitions` and throws on either of the two content-authoring violations, naming the offending asset(s). **Corrected during TD-ADR review (2026-08-06)**: an earlier draft of this section only described checks (b), while Consequences → Risks and Validation Criteria both already described the key-resolution check (a) as part of the same validator — Decision now matches what the rest of the ADR already assumed exists. Checks (b) also run as an `OnValidate()` check directly on `ClueDefinition`/`ClueRegistry` (Inspector-time feedback, not just at build time) — the two checks share the same validation logic, `OnValidate()` just surfaces it earlier in the authoring loop; check (a) is build-only, since there's no single asset instance for `OnValidate()` to attach to.
2. **Editor-only warning** (orphaned `shiftId` — a `requiredShiftIds` entry no scene-configured trigger will ever fire) — `ClueConsistencyValidator`, registered via `EditorSceneManager.sceneOpened`/`sceneSaved` callbacks (`[InitializeOnLoad]`), calls `ValidateScene(sceneId)` and logs `Debug.LogWarning` for each orphaned `(clueId, shiftId)` pair found in `GetOrphanedClueIds()`. Deliberately Editor-only (`#if UNITY_EDITOR`, no `[RuntimeInitializeOnLoadMethod]` counterpart) — matches the GDD's own framing of this as "a content-authoring warning," not a runtime behavior check, and keeps this code out of player builds entirely rather than shipping an inert check.

## Alternatives Considered

### Alternative 1: Per-event linear scan instead of a precomputed reverse index
- **Description**: On every `OnShiftStateChanged(Held)`, iterate all `ClueDefinition`s in the registry and check `_seenShiftIds.IsSupersetOf(def.RequiredShiftIds)` for each, instead of maintaining `_byRequiredShiftId`.
- **Pros**: Simpler code, one fewer data structure, no build-time index-construction step.
- **Cons**: O(ClueDefinition count) per Held event instead of O(1) — irrelevant at MVP's 2-3-trigger scale, but the index costs almost nothing to build (a handful of dictionary inserts once, at construction) and removes any future need to revisit this if clue count grows toward the GDD's own stated Full Vision scale (15-20 triggers).
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-06): the precomputed index is barely more code for a real, if currently-negligible, benefit — the GDD explicitly designed the data model to scale to 15-20 triggers without a "structural change," and the index is the natural way to honor that without waiting for a future ADR to add it.

### Alternative 2: Store `ClueDefinition` records per-scene rather than in one central registry
- **Description**: Each scene owns its own `ClueDefinition` list (like a scene-local trigger configuration), rather than one project-level `ClueRegistry` asset.
- **Pros**: Content for a scene lives next to that scene's other configuration.
- **Cons**: `anlati-durum-ipucu-takibi.md` Core Rules already explicitly rejects this — a single project-level registry is required specifically to make duplicate-`clueId` definition "structurally impossible" across scenes, which per-scene storage would reintroduce.
- **Rejection Reason**: Already decided at the GDD level; this ADR implements that decision rather than re-litigating it.

### Alternative 3: Orphaned-shiftId validation as a runtime Play-mode check instead of Editor-only
- **Description**: Run `ClueConsistencyValidator.ValidateScene(sceneId)` on scene load in Play mode (and therefore in builds), via `Debug.LogWarning`, rather than gating it to `EditorSceneManager` callbacks.
- **Pros**: A second safety net — catches an orphaned `shiftId` even if a content author never opened/saved the affected scene in the Editor (e.g., a mis-edited `ClueDefinition` asset edited without touching the scene at all).
- **Cons**: Ships authoring-warning code (and a small validation cost) inside player builds for a check whose entire value is "catch this before it ships," which the build-blocking tier (Alternative considered above, not this one) already guarantees for the two structural errors — the orphaned-`shiftId` check is explicitly non-blocking/best-effort by GDD design (a legitimately unreachable clue is a content gap, not a crash), so shipping it at runtime buys little beyond what Editor-time coverage already provides for any content actually played during development.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-06): Editor-only is sufficient and matches the GDD's own "content-authoring uyarısı" framing — this is a tooling concern, not a runtime behavior guarantee.

### Alternative 4: `Resources.Load`, loaded eagerly in the constructor
- **Description**: Load `ClueRegistry` via `Resources.Load<ClueRegistry>("ClueRegistry")` inside `AnlatiDurumState`'s constructor (this ADR's first-draft design).
- **Pros**: Zero initialization-order risk — `Resources.Load` has no separate subsystem that needs to be ready first, so it works identically whether called at `SubsystemRegistration` time or later. Simpler: one synchronous call, no async handle, no lazy-load indirection.
- **Cons**: Directly contradicts this project's own documented API preference — `current-best-practices.md`'s "Use Addressables (Not Resources)" section and `deprecated-apis.md`'s explicit listing of `Resources.Load()` as deprecated (Addressables as the named replacement) for this pinned Unity 6.3 version. The first draft of this ADR tried to justify this exception with a citation that turned out to be fabricated (see Decision → Registry loading) — once that justification is removed, no real argument for the exception remained.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-06): deferring the Addressables load to first real use (see Decision) removes the only genuine advantage `Resources.Load` had (init-order safety) without requiring an exception to project convention.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #7, second-to-last "must have before coding" Foundation ADR after `Gece/Oturum Durumu`.
- Fixes the `IReadOnlySet<string>` BCL-risk/GDD-fidelity divergence in `architecture.md`'s own sketch proactively, before a specialist review had to catch it a second time (same class of issue as ADR-0006's finding #1).
- `ClueRegistry`/`ClueDefinition` as `ScriptableObject` assets keeps clue content fully data-driven, matching `.claude/docs/coding-standards.md`'s project-wide rule and this system's own "content yapılandırması, tuning knob değil" framing (Tuning Knobs section).
- The two-tier validation split (build-blocking structural errors vs. Editor-only content warning) gives content authors fast feedback for the cheap-to-detect, definitely-wrong cases without over-blocking builds for the "might be an intentionally-unused clue during content iteration" case.

### Negative
- This is the project's first real Addressables consumer — `AnlatiDurumState` is the only piece of Foundation-layer code that reaches into the Addressables system at all, so any future Addressables authoring mistake (asset not marked Addressable, wrong key) has no precedent elsewhere in the project to catch it by pattern-matching; the build-blocking check (Risks below) is this ADR's own answer to that gap, not something inherited from prior art.
- `EnsureRegistryLoaded()`'s lazy-load indirection (a nullable field + a guard check on every `OnShiftStateChanged(Held)` call) is one small piece of extra structure compared to the simpler eager-constructor-load the first draft used — accepted because it's what removes the `SubsystemRegistration`-timing risk (see Decision), not optional complexity.
- `OnValidate()` and `IPreprocessBuildWithReport` both implement the same empty-list/duplicate-`clueId` check — two call sites for one rule. Accepted because they serve genuinely different moments (Inspector-time vs. build-time) and the underlying validation logic is a single shared method both call into, not duplicated logic.

### Risks
- **Risk**: `Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry")` fails (wrong/missing Addressable key, asset not marked Addressable) with no build-time check today that the key actually resolves — the `IPreprocessBuildWithReport` check (Decision → Edit-time validation) validates the *contents* of `ClueRegistry.Definitions`, not that the `"ClueRegistry"` Addressable key itself is reachable. **Mitigation**: extend the same `IPreprocessBuildWithReport` pass to also resolve the `"ClueRegistry"` key (e.g. via `AddressableAssetSettings` lookup, not a runtime load) and throw `BuildFailedException` if it's missing — a Validation Criteria item below, to be included when this ADR is implemented.
- **Risk**: `WaitForCompletion()` on an `AsyncOperationHandle` blocks the calling thread until the load finishes — acceptable at this call site (a rare, low-frequency gameplay event, not per-frame, not during a loading screen) but would be a real problem if a future edit ever moved this call to a hot path. **Mitigation**: the lazy-load guard (`if (_byRequiredShiftId != null) return;`) already ensures this blocking call happens at most once per session — worth a code comment at the call site (already present in the Data model code block above) so a future editor doesn't relocate it into a per-frame path without noticing the cost.
- **Risk**: The reverse-index (`_byRequiredShiftId`) is built once, on first load, from whatever `ClueRegistry.Definitions` contains at that moment — if `ClueRegistry`'s contents were ever mutated at runtime (they should never be; it's authored content, not runtime state), the index would silently go stale. **Mitigation**: none needed structurally — `ScriptableObject` asset data is not runtime-mutated by any consumer in this project's design (matches the project-wide data-driven-config principle), but worth stating explicitly since nothing else in this ADR enforces it.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `anlati-durum-ipucu-takibi.md` | Full Core Rules data model (`KnownClueIds`, `SeenShiftIds`, `ClueDefinition` N:1 ALL-semantics, static/singleton persistence identical to `Gece/Oturum Durumu`'s shape) | Implemented via `IAnlatiDurumState`/`AnlatiDurumState`, per ADR-0001's static-service shape |
| `anlati-durum-ipucu-takibi.md` | First-access (not `Awake`/`OnEnable`) subscription to `Işık/Volume`'s `OnShiftStateChanged`, `Held`-only handling | Subscription in `AnlatiDurumState`'s constructor, called at first `Instance` access per ADR-0001's static-facade pattern; handler filters to `ShiftState.Held` |
| `anlati-durum-ipucu-takibi.md` | `requiredShiftIds` never empty (edit-time validated); duplicate `clueId` across two `ClueDefinition`s rejected (edit-time validated) | `IPreprocessBuildWithReport` + `OnValidate()`, build-blocking (AC#8a/8b) |
| `anlati-durum-ipucu-takibi.md` | Orphaned-`shiftId` detection, `ClueConsistencyValidator.ValidateScene(sceneId)`, non-blocking `Debug.LogWarning` | Editor-only via `EditorSceneManager.sceneOpened`/`sceneSaved` + `[InitializeOnLoad]` (user decision, 2026-08-06) |
| `anlati-durum-ipucu-takibi.md` | `MarkClueKnown` idempotent no-op if already known; no sequencing/timestamp data ever exposed | `HashSet<string>.Add` return-value check; `IAnlatiDurumState` exposes no ordering data anywhere in its surface (matches AC#7) |
| `architecture.md` | API Boundaries sketch (`MarkClueKnown`/`IsClueKnown`/`GetKnownClueIds`/`OnClueKnown`) | Implemented verbatim except `GetKnownClueIds()`'s return type, corrected `IReadOnlySet<string>` → `IReadOnlyCollection<string>` to match the GDD and avoid a BCL-availability risk (see Decision) |

## Performance Implications
- **CPU**: Negligible — `OnShiftStateChanged(Held)` fires at most a handful of times per session (2-3 MVP triggers); the reverse-index lookup is O(1), the `IsSupersetOf` check is O(requiredShiftIds.Count), trivially small (1 element at MVP scale, per the GDD's own note).
- **Memory**: Negligible — two small `HashSet<string>`s, one `Dictionary<string, List<ClueDefinition>>` sized to the trigger count, and the `ClueRegistry`/`ClueDefinition` assets themselves (tiny serialized data, no textures/meshes).
- **Load Time**: One `Addressables.LoadAssetAsync(...).WaitForCompletion()` call, deferred to the first real `OnShiftStateChanged(Held)` event (not at boot) — a single small asset, negligible cost even as a blocking call, since it happens at most once per session on a rare gameplay event, not per-frame or per-scene-load.
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Anlatı Durum/İpucu Takibi` is not yet implemented).

## Validation Criteria
- `MarkClueKnown(clueId)` called twice for the same `clueId` fires `OnClueKnown` exactly once (GDD AC#3).
- A `ClueDefinition` with `requiredShiftIds = [A, B]` only completes (`IsClueKnown` → `true`, `OnClueKnown` fires) once both `A` and `B` have independently reached `Held` — order-independent (GDD AC#1/#2).
- `IPreprocessBuildWithReport` fails the build for an empty `requiredShiftIds` list and for two `ClueDefinition`s sharing a `clueId` (GDD AC#8a/#8b) — **and** for a missing/unresolvable `"ClueRegistry"` Addressable key (new, this ADR's Risks mitigation, not separately stated in the GDD).
- `Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry").WaitForCompletion()` succeeds when first invoked from a genuine `OnShiftStateChanged(Held)` event during actual gameplay (i.e., well past boot) — smoke-tested once against the pinned 6000.3.0f1 editor before relying on it project-wide (this ADR's own Verification Required item).
- A `Persistent=true` shift's post-reload re-fire of `OnShiftStateChanged(Held)` does not cause a duplicate `OnClueKnown` (GDD AC#11) — covered by `HashSet.Add`'s natural idempotency on both `_seenShiftIds` and `_knownClueIds`.
- `GetKnownClueIds()` returns an empty, non-null collection before any clue is known (GDD AC#5).

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — this ADR's foundational mechanism.
- ADR-0005 (Işık/Volume Rendering Architecture) — source of `OnShiftStateChanged`, the sole trigger for this system.
- ADR-0006 (Session State Service and Round-Counter Ownership) — established the corrected `FoundationBootstrap.ResetAll()` order this ADR's service already participates in correctly (no further change needed); also the ADR where the `IReadOnlySet<T>` BCL-risk pattern was first caught, applied proactively here.
- Future "Dialogue Callback Selection Timing" ADR (Diyalog/Anlatı İçeriği, Required ADR #12) — will consume `IsClueKnown`/`GetKnownClueIds` as its primary input.
