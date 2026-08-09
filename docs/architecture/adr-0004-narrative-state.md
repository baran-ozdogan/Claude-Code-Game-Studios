# ADR-0004: Narrative State/Clue Tracking — Static Clue Registry & Consistency Validation

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Persistence / Core (narrative state), Editor Tooling (content validation) |
| **Knowledge Risk** | LOW — pure C# static-class pattern (no engine-version-specific API); `IPreprocessBuildWithReport` and ScriptableObject-based content registries confirmed unchanged post-2022-LTS |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `current-best-practices.md`, `breaking-changes.md`, `deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None formally. Reuses ADR-0003's `ILightingVolumeQuery` interface contract (registered in `docs/registry/architecture.yaml` as `lighting_shift_state`) rather than defining a parallel one — confirmed with the user before drafting. **Amends ADR-0003**: `NullLightingVolumeQuery` was declared `internal sealed` in ADR-0003, which is now incorrect since this ADR is a second consumer that may live in a different assembly definition — this ADR requires that type (and `ILightingVolumeQuery`/`ShiftState`) be `public`, not `internal`. See the corresponding one-line amendment to `adr-0003-session-state.md`. |
| **Enables** | Future ADR for Diyalog/Anlatı İçeriği (Dialogue Content) — its callback-selection logic queries `IsClueKnown`/`GetKnownClueIds` directly |
| **Blocks** | Stories for Diyalog/Anlatı İçeriği, until this ADR reaches `Accepted` |
| **Ordering Note** | Foundation layer, Batch 1 priority #4 per `docs/architecture/architecture-review-2026-08-05.md` — also the project's top dependency bottleneck (5 dependents per `systems-index.md`, though only Dialogue Content is Foundation/Core-layer-relevant at this stage). Same cross-batch open item as ADR-0003: this ADR's `ILightingVolumeQuery` consumption is inert (silently produces no clues) until the future Lighting/Volume ADR (Batch 1 #5) assigns a real implementation to this system's `LightingQuery` property. **Critical distinction from ADR-0003's version of this same risk**: the future Lighting/Volume ADR's bootstrap must assign **two separate** consumer properties — `SessionState.LightingQuery` AND `NarrativeState.LightingQuery` — independently. They are two distinct static properties, each with its own null-object default; assigning one does not satisfy the other. This is called out explicitly in Risks below and reflected in the registry update accompanying this ADR. |

## Context

### Problem Statement
The game needs to track which narrative "clues" (memory fragments/hints) the player has semantically learned — distinct from Gece/Oturum Durumu's raw "which trigger fired" bookkeeping — persisted across scene loads, computed from a many-to-one mapping of lighting-shift completions to narrative clues, with zero false positives (a clue must never register "known" before every one of its required shifts has been fully experienced) and zero silent content-authoring gaps (an unreachable clue must be caught before it ships, not discovered by a player).

### Constraints
- Must survive every scene load/unload in a session (Foundation layer, same requirement as ADR-0003)
- No disk persistence in MVP — data model must not require structural changes to gain that later (Çoklu Gece İlerlemesi, Vertical Slice)
- Zero "feel"/tuning parameters — pure flag/set logic, no Formulas or Tuning Knobs in the GDD
- Must never produce a false "Known" result — an empty `requiredShiftIds` list would make a clue vacuously "known" via `SeenShiftIds ⊇ ∅` being trivially true; this must be caught at edit time, not runtime
- Must structurally prevent one `clueId` from being defined inconsistently across two content records

### Requirements
See `docs/architecture/tr-registry.yaml`: TR-narrative-001 through TR-narrative-028 (extracted from `design/gdd/anlati-durum-ipucu-takibi.md`), plus consumer-side requirements TR-dialogue-001/002/009.

## Decision

A **static plain C# class** (`NarrativeState`) — same pattern as ADR-0003's `SessionState`, confirmed with the user for consistency rather than re-deriving alternatives, since the GDD itself explicitly names this exact pattern by reference to the sibling system.

### Core mechanism
```csharp
public static class NarrativeState {
    public static IReadOnlyCollection<string> GetKnownClueIds() => _knownClueIds;
    public static bool IsClueKnown(string clueId) => _knownClueIds.Contains(clueId);
    public static event Action<string> OnClueKnown;

    // Idempotent: fires OnClueKnown only on the Unknown→Known transition.
    // Callable by any system (Lighting/Volume subscription below, or a
    // future direct-reveal path such as a dialogue choice).
    public static void MarkClueKnown(string clueId) {
        if (_knownClueIds.Add(clueId)) OnClueKnown?.Invoke(clueId);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForNewSession() {
        _knownClueIds.Clear();
        _seenShiftIds.Clear();
    }

    private static readonly HashSet<string> _knownClueIds = new();
    private static readonly HashSet<string> _seenShiftIds = new();
}
```
- `KnownClueIds` (exposed via `GetKnownClueIds()`) and the internal `SeenShiftIds` are both plain `HashSet<string>` — no ordering or timestamp data, matching the GDD's explicit design intent (Pillar 1/5 rationale: no objective ordering of discovery)
- `SeenShiftIds` is deliberately independent from `SessionState.FiredTriggerIds` — different ownership, no cross-querying, per the GDD's explicit system-boundary note (Overview)

### Lighting/Volume Event Contract — reuses ADR-0003's registered interface (design decision, confirmed with user)
This system does **not** redeclare `ILightingVolumeQuery`, `ShiftState`, or `NullLightingVolumeQuery` — those types are owned by ADR-0003 (in a shared namespace; exact assembly/`asmdef` layout is an implementation detail deferred to `dev-story` time, not an architectural decision this ADR or ADR-0003 needs to lock). `NarrativeState` becomes this interface's **second, independent consumer**:
```csharp
private static ILightingVolumeQuery _lightingQuery = new NullLightingVolumeQuery();
public static ILightingVolumeQuery LightingQuery {
    get => _lightingQuery;
    set {
        _lightingQuery.OnShiftStateChanged -= OnShiftStateChangedHandler;
        _lightingQuery = value ?? new NullLightingVolumeQuery();
        _lightingQuery.OnShiftStateChanged += OnShiftStateChangedHandler;
    }
}

static void OnShiftStateChangedHandler(string shiftId, ShiftState newState, Vector3 zoneCenter, float radius) {
    if (newState != ShiftState.Held) return;              // only Held — Shifting-In/Out/Dormant ignored
    if (!_seenShiftIds.Add(shiftId)) return;                // already processed (e.g. Persistent shift's post-reload re-fire) — safe no-op per GDD, and cheaper than always re-scanning
    foreach (var clue in ClueRegistry.Active.Definitions) {
        if (Array.IndexOf(clue.requiredShiftIds, shiftId) >= 0 && _seenShiftIds.IsSupersetOf(clue.requiredShiftIds)) {
            MarkClueKnown(clue.clueId);
        }
    }
}
```
**This is `NarrativeState`'s own independent static property** — not a shared subscription with `SessionState.LightingQuery`. Both classes subscribe to the same producer instance once it exists, but each holds its own reference and its own null-object default; assigning one does not affect the other (see Risks).

`zoneCenter`/`radius` are received but unused, matching the GDD's explicit note that these parameters exist only for spatial audio/visual consumers.

### Content model
```csharp
[Serializable]
public struct ClueDefinition {
    public string clueId;
    public string[] requiredShiftIds;   // ALL semantics — every entry must be in SeenShiftIds
}

[CreateAssetMenu(menuName = "Yankilar/Clue Registry")]
public class ClueRegistry : ScriptableObject {
    public List<ClueDefinition> Definitions;
    public static ClueRegistry Active;   // assigned from a single serialized field reference on a
                                          // game-bootstrap object — NOT Resources.Load, NOT Addressables
                                          // (per architecture-review-2026-08-05.md, project-wide
                                          // Addressables-vs-other-asset-types strategy is still undecided;
                                          // this ADR does not resolve that question, it only specifies
                                          // that THIS asset, at this content scale — 2-3 entries at MVP —
                                          // is loaded via a direct serialized reference, consistent with
                                          // ADR-0001's precedent of scoping Addressables decisions narrowly)
}
```
A single project-level asset — never per-scene copies — making it structurally impossible for the same `clueId` to be defined inconsistently in two places (single source of truth).

### Edit-time validation
`IPreprocessBuildWithReport` (confirmed still the current Unity 6.3 mechanism) scans `ClueRegistry.Definitions` and fails the build on:
1. Any `ClueDefinition` with `requiredShiftIds.Length == 0` (prevents the vacuous-truth bug — `SeenShiftIds ⊇ ∅` is trivially always true)
2. Two different `ClueDefinition` records sharing the same `clueId` (error names both conflicting entries)

This shares the same editor-utility pattern the GDD ecosystem already establishes elsewhere (e.g. the Memory-Trigger system's own duplicate-`shiftId` check) — not implemented twice, though the exact shared-utility code structure is a `dev-story`-time detail, not locked here.

### Scene-load consistency check (non-blocking, content-authoring warning)
```csharp
public static class ClueConsistencyValidator {
    public static void ValidateScene(Scene scene) { /* hooked to SceneManager.sceneLoaded */ }
    public static IReadOnlyCollection<(string clueId, string shiftId)> GetOrphanedClueIds() => _orphaned;
}
```
**Timing, per Unity specialist review**: `SceneManager.sceneLoaded` fires after every object's `Awake()` in the newly loaded scene but before any `Start()`. Any scene-local trigger object that registers its `shiftId` (with whatever registry the future Lighting/Volume ADR defines) must do so in `Awake()`, not `Start()`, or this validator will see incomplete data and produce false-positive orphan warnings. This is a constraint the future Lighting/Volume ADR must honor for its trigger-zone components, flagged here since this ADR is what depends on it. Also per Unity specialist review: this validator runs **per-scene** (via the `Scene` parameter passed by `sceneLoaded`), not once globally — correct under ADR-0001's additive multi-scene residency model, where two scenes can be loaded simultaneously during a SOFT transition.

### Architecture Diagram
```
                    ┌───────────────────────────┐
                    │        NarrativeState          │
                    │   (static, own LightingQuery)   │
                    └───────────────────────────┘
        ▲                                      │
  MarkClueKnown /                          OnClueKnown
  IsClueKnown /                            (future Dialogue
  GetKnownClueIds                           Content: subscribes
  (future Dialogue Content)                 AND polls at scene start)
        │
        ▼
  Subscribes independently (own property, own null-object default) to:
  [future Lighting/Volume ADR: ILightingVolumeQuery — SAME interface
   ADR-0003's SessionState also consumes, but a SEPARATE subscription;
   the future Lighting bootstrap must assign BOTH properties]

  ClueRegistry (ScriptableObject, single project-level asset)
  → IPreprocessBuildWithReport validates at build time
  → ClueConsistencyValidator.ValidateScene() warns (non-blocking) at scene-load time
```

### Key Interfaces
See Core mechanism and Lighting/Volume Event Contract above. Summary of cross-system contracts:
- **Query + subscribe** (Dialogue Content): `IsClueKnown`, `GetKnownClueIds`, `OnClueKnown` — both patterns are part of the contract (a late subscriber must reconcile via polling at its own init, since events are not replayed)
- **Consumer of** (future Lighting/Volume ADR): `ILightingVolumeQuery` via `NarrativeState.LightingQuery`, independently of `SessionState.LightingQuery`

## Alternatives Considered

### Alternative 1: Define a separate, parallel interface instead of reusing ADR-0003's `ILightingVolumeQuery`
- **Description**: `NarrativeState` defines its own `INarrativeLightingQuery` (or similar) rather than consuming the same interface `SessionState` uses.
- **Pros**: Fully decoupled from ADR-0003's type; no shared-type dependency to manage.
- **Cons**: Forces the future Lighting/Volume ADR to implement and raise two structurally identical events for two consumers that both just need "shift reached this state" — needless duplication with no behavioral benefit, per TD-ADR review ("one contract, two consumers is standard producer-owned-contract design; two parallel interfaces would force the unwritten Lighting/Volume ADR to raise two events").
- **Rejection Reason**: user confirmed reuse; TD-ADR review confirmed this is the architecturally correct call, and that future divergence (if `NarrativeState` ever needs a payload field `SessionState` doesn't) is cheap to handle later as an additive field or second event on the same interface, not a reason to pre-split now.

### Alternative 2: `NarrativeState` and `SessionState` share one subscription/property
- **Description**: Only one system (e.g. `SessionState`) subscribes to the Lighting/Volume event, and `NarrativeState` reads from `SessionState` instead of subscribing independently.
- **Pros**: One fewer subscription to manage.
- **Cons**: Directly violates the GDD's explicit, load-bearing system-boundary rule — `NarrativeState` and `SessionState` maintain deliberately separate "halves" of state and must never query each other's data (Overview, Dependencies — both GDDs state this independently). Routing through `SessionState` would make `NarrativeState` structurally dependent on `SessionState`'s internal timing/availability.
- **Rejection Reason**: two independent subscriptions to the same producer is exactly what C# multicast events support safely and is architecturally cleaner than introducing a cross-dependency the GDD explicitly forbids.

## Consequences

### Positive
- Reusing ADR-0003's interface means the future Lighting/Volume ADR has exactly one contract to satisfy correctly, not two to keep in sync
- The `ClueDefinition` N:1 (many-shift-to-one-clue) data model costs almost nothing now (a list vs. a single string) while avoiding a 1:1 assumption that MVP's 2-3-trigger content would otherwise bake in — paying this small cost now avoids a breaking schema change for Full Vision's 15-20-trigger scenarios
- Edit-time validation (empty list, duplicate `clueId`) converts two classes of silent content bugs into build failures, per this project's own established convention (shared with the Memory-Trigger system's equivalent checks)
- The non-blocking `ClueConsistencyValidator` catches unreachable clues (a `shiftId` that no scene's triggers actually fire) as an early warning without over-constraining content authoring with a hard build failure for what might be intentional (e.g. content staged for a future night)

### Negative
- Two independent static classes (`NarrativeState`, `SessionState`) both now depend on the same not-yet-existing producer's bootstrap correctly assigning **two separate** properties — doubling the surface area for the exact "silently inert if forgotten" risk ADR-0003 already carries alone (see Risks)
- `ClueRegistry.Active`'s loading mechanism (direct serialized reference from a bootstrap object) is specified here narrowly, for this asset at this content scale — it is not a project-wide Addressables policy statement, and a future ADR resolving that broader question could require revisiting this one line, though not the rest of this ADR's decision

### Risks
- **Critical, must be flagged to the future Lighting/Volume ADR's author**: unlike ADR-0003 (a single consumer property to assign), this ADR means the Lighting/Volume bootstrap must assign **both** `SessionState.LightingQuery` and `NarrativeState.LightingQuery`. Assigning only one leaves the other silently inert — no error, no exception, clues (or session bookkeeping, if the omission is reversed) simply never update. This is exactly the kind of gap this project's own review history shows recurring (a contract changes in the producer, not every consumer is updated to match) — the registry update accompanying this ADR lists both consumers explicitly for this reason, and the future Lighting/Volume ADR's Validation Criteria should include an explicit test that both properties are assigned during its own bootstrap
- If the future Lighting/Volume ADR's concrete implementation doesn't cleanly satisfy `ILightingVolumeQuery`, both `SessionState` and `NarrativeState` are affected identically — same low-risk, narrow-blast-radius pattern already accepted for this interface in ADR-0003, now doubled in consumer count but not in kind

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| anlati-durum-ipucu-takibi.md | TR-narrative-001/006: `KnownClueIds`/`SeenShiftIds` as independent flat `HashSet<string>`s, no ordering data | Decision → Core mechanism |
| anlati-durum-ipucu-takibi.md | TR-narrative-002/004/005: `ClueDefinition` N:1 ALL-semantics data model, non-empty `requiredShiftIds`, N-element-list-ready | Decision → Content model |
| anlati-durum-ipucu-takibi.md | TR-narrative-008/011: static/singleton plain C# service, subscription timing that never misses an early firing | Decision → Core mechanism, Lighting/Volume Event Contract (reuses ADR-0003's already-fixed deterministic solution rather than the GDD's literal static-constructor suggestion) |
| anlati-durum-ipucu-takibi.md | TR-narrative-004/010: edit-time validation blocks empty `requiredShiftIds` and duplicate `clueId`s | Decision → Edit-time validation |
| anlati-durum-ipucu-takibi.md | TR-narrative-007/014: `MarkClueKnown` idempotent, `OnClueKnown` fires exactly once per transition | Decision → Core mechanism |
| anlati-durum-ipucu-takibi.md | TR-narrative-016/017/018: subscribes to `OnShiftStateChanged`, processes only `Held`, tolerates duplicate re-fires | Decision → Lighting/Volume Event Contract |
| anlati-durum-ipucu-takibi.md | TR-narrative-020: `ClueConsistencyValidator.ValidateScene`, non-build-blocking orphan warning | Decision → Scene-load consistency check |
| anlati-durum-ipucu-takibi.md | TR-narrative-026/027/028: ownership boundary vs. Session State, late-subscriber reconciliation pattern, query contract usable independent of event timing | Alternatives Considered (Alternative 2), Key Interfaces |
| diyalog-anlati-icerigi-2026-08-02.md | TR-dialogue-001/002/009: `IsClueKnown` query per `CallbackPool` entry, hard dependency | Key Interfaces |

## Performance Implications
- **CPU**: negligible — `HashSet` operations are O(1) average case; the per-`Held`-event scan over `ClueRegistry.Definitions` is bounded by MVP's 2-3 total entries, trivial even at Full Vision's 15-20-entry scale
- **Memory**: negligible — collections never exceed content-cap-bounded entry counts
- **Load Time**: `ClueConsistencyValidator.ValidateScene` adds a bounded, one-time-per-scene-load cross-check — negligible at this content scale
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system. Forward-compatibility note (per GDD, same as ADR-0003): the flat `HashSet<string>` data model is already stated to be serialization-ready for the future Çoklu Gece İlerlemesi disk-persistence extension, requiring no structural change.

## Validation Criteria
- All Acceptance Criteria from `anlati-durum-ipucu-takibi.md` (AC-1 through AC-12b), implemented as automated EditMode tests per this project's Logic-tier test-evidence rules
- AC-8a/8b specifically: `IPreprocessBuildWithReport` EditMode tests verifying the build fails on an empty `requiredShiftIds` list and on a duplicate `clueId`, with error messages naming the offending record(s)
- New test (per Risks): with `LightingQuery` left at its default `NullLightingVolumeQuery`, `OnShiftStateChangedHandler` is never invoked and `KnownClueIds`/`SeenShiftIds` remain empty — verifies the null-object default is inert on this consumer independently of `SessionState`'s own equivalent test
- New test: assigning `NarrativeState.LightingQuery` does not affect `SessionState.LightingQuery` or vice versa — two independent mock `ILightingVolumeQuery` instances, verifying no cross-talk between the two consumers
- New test: a `shiftId` shared by two different `ClueDefinition`s reaching `Held` in one event correctly evaluates both clues in the same handler invocation, completing zero, one, or both as their respective `requiredShiftIds` sets dictate

## Related Decisions
- Reuses and extends ADR-0003's `ILightingVolumeQuery` contract as a second independent consumer — requires the accompanying one-line ADR-0003 amendment (`internal sealed` → `public sealed` on `NullLightingVolumeQuery`)
- Enables: future ADR for Diyalog/Anlatı İçeriği
- Consumer-side dependency on Işık/Volume Durum Sistemi's future ADR (Batch 1 #5) — see ADR Dependencies → Ordering Note; that ADR's own Validation Criteria should explicitly test both `SessionState.LightingQuery` and `NarrativeState.LightingQuery` are assigned during its bootstrap
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline and recommended ADR authoring order
