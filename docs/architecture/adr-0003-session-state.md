# ADR-0003: Night/Session State — Static In-Memory Bookkeeping Service

## Status
Proposed

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Persistence / Core (session state) |
| **Knowledge Risk** | LOW — this is a pure C# static-class pattern with no Unity-engine-version-specific API surface; `docs/engine-reference/unity/current-best-practices.md` was checked and has no singleton-pattern guidance that changed post-2022-LTS |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `current-best-practices.md`, `breaking-changes.md`, `deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None formally. Conceptually built on ADR-0001's additive-scene-loading model (this system must survive the scene loads ADR-0001 defines) — no direct API call to `SceneTransitionManager`, so not a blocking dependency. |
| **Enables** | Future ADRs for Anı-Tetikleyici Etkileşim (Memory-Trigger), Asansör/Kat-Erişim Sistemi (Elevator), Görev/Taşıma Döngüsü (Carry Loop), Sahne Kesmeli Anlatı (Cutscene) — all four consume this ADR's public API directly |
| **Blocks** | Stories for the four consuming systems above, until this ADR reaches `Accepted` |
| **Ordering Note** | Foundation layer, Batch 1 priority #3 per `docs/architecture/architecture-review-2026-08-05.md`. **Open cross-batch item, same pattern as ADR-0002's registry deferral**: this system subscribes to Işık/Volume Durum Sistemi's `OnShiftStateChanged` event and calls its `IsShiftPersistent(shiftId)` query — but Lighting/Volume's own ADR (Batch 1 #5) doesn't exist yet. This ADR defines the consumer-side expectation of that contract (see Decision → Lighting/Volume Event Contract) and registers it in `docs/registry/architecture.yaml` as a producer-TBD entry, binding the future Lighting/Volume ADR to satisfy it. |

## Context

### Problem Statement
A "night" session has state that must be readable and writable from any scene (Depo, Servis Koridoru, Balo Salonu, the psychiatry-session scene) without being destroyed or duplicated when scenes load/unload via ADR-0001's additive transitions. Four other systems depend on this state existing reliably from the moment the game starts, including one system (Session State itself) that must subscribe to an event from a system that initializes independently, without missing early firings regardless of scene load order.

### Constraints
- Must survive every scene load/unload in a session without being destroyed, duplicated, or requiring re-initialization
- No disk persistence in MVP (explicitly deferred to the future Vertical-Slice-tier Çoklu Gece İlerlemesi system) — the data model must not require structural changes to gain that later
- Must never miss an early `OnShiftStateChanged` firing from Işık/Volume Durum Sistemi, regardless of which scene's objects initialize first
- Zero "feel"/tuning parameters — this is pure bookkeeping, no Tuning Knobs in the GDD

### Requirements
See `docs/architecture/tr-registry.yaml`: TR-session-001 through TR-session-017 (extracted from `design/quick-specs/gece-oturum-durumu-2026-08-02.md`), plus consumer-side requirements TR-memory-006/007/008/029, TR-elevator-003/004/018, TR-carry-010/022, TR-cutscene-002/003/005/011/016.

## Decision

A **static plain C# class** (no `GameObject`, no `MonoBehaviour`, no `DontDestroyOnLoad`) — per the user's confirmed decision, matching the pattern already locked into sibling Foundation-layer systems' own GDDs (Narrative State, Carry Loop both explicitly reject `DontDestroyOnLoad` in favor of a static/singleton plain C# service). Static fields are not scene-bound in the CLR, so "survives scene loads" is a structural property of the choice, not something that needs guarding against duplicate-instance bugs the way a `MonoBehaviour` singleton would.

### Core mechanism
```csharp
public static class SessionState {
    public static bool IsSessionActive { get; private set; }
    public static int CurrentNightNumber { get; private set; }
    public static IReadOnlySet<string> FiredTriggerIds => _firedTriggerIds;
    public static IReadOnlySet<string> SettledTriggerIds => _settledTriggerIds;
    public static IReadOnlyDictionary<string, bool> PersistentShiftIds => _persistentShiftIds;

    public static event Action<string> OnTriggerFired;
    public static event Action<string> OnTriggerSettled;

    // Callable only by the future Memory-Trigger system. Idempotent: fires
    // OnTriggerFired only on the transition from absent to present.
    public static void RecordTriggerFired(string shiftId) {
        if (_firedTriggerIds.Add(shiftId)) OnTriggerFired?.Invoke(shiftId);
    }

    // Callable only by the future Cutscene system. Idempotent: no-op if the
    // session is already inactive.
    public static void EndSession() {
        if (!IsSessionActive) return;
        IsSessionActive = false;
    }

    // Fires earliest of any RuntimeInitializeOnLoadMethod phase — clears all
    // state to initial values. Closes the Editor Play-Mode staleness risk
    // (Consequences → Negative) without relying on a manually-called
    // convention, and also covers the Domain-Reload-disabled case.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForNewSession() {
        IsSessionActive = true;
        CurrentNightNumber = 1;
        _firedTriggerIds.Clear();
        _settledTriggerIds.Clear();
        _persistentShiftIds.Clear();
    }

    private static readonly HashSet<string> _firedTriggerIds = new();
    private static readonly HashSet<string> _settledTriggerIds = new();
    private static readonly Dictionary<string, bool> _persistentShiftIds = new();
}
```
- `IsSessionActive` starts `true` at night start, `CurrentNightNumber` fixed to `1` for MVP
- `FiredTriggerIds`/`OnTriggerFired`: written by the future Memory-Trigger system's `OnHoldComplete()`, same frame as entry into `Shifting-In`
- `SettledTriggerIds`/`OnTriggerSettled`: written by this system itself, from the Lighting/Volume Event Contract handler (below), only when a `shiftId` already in `FiredTriggerIds` reaches `Held`
- `PersistentShiftIds`: written by this system itself, from the same event handler, on `Shifting-In` if `IsShiftPersistent(shiftId)` returns true — does not wait for `Held`
- All collections exposed as read-only interfaces (`IReadOnlySet<T>`/`IReadOnlyDictionary<K,V>`) — only this static class's own internal methods mutate the backing fields; consumers cannot write to `FiredTriggerIds` etc. directly, closing off an entire class of cross-system data-race bugs by construction

### Lighting/Volume Event Contract (consumer-side, producer ADR pending, interface + null-object per TD-ADR finding)
This system depends only on an interface it defines, never on a concrete type Işık/Volume Durum Sistemi's future ADR owns — the same deferred-producer shape ADR-0002 uses for `IFlaggedObjectRegistry`, corrected here after TD-ADR review flagged an earlier draft's direct static reference as a phantom compile-time dependency on a type no ADR yet owns:
```csharp
public interface ILightingVolumeQuery {
    event Action<string, ShiftState, Vector3, float> OnShiftStateChanged; // shiftId, newState, zoneCenter, radius
    bool IsShiftPersistent(string shiftId); // synchronous query, must be called same-frame as the event
}

public sealed class NullLightingVolumeQuery : ILightingVolumeQuery {
    // Visibility corrected from `internal` to `public` (amendment via ADR-0004,
    // Narrative State review, 2026-08-05): a second independent consumer
    // (NarrativeState) needs this type, potentially from a different
    // assembly definition — `internal` would have made it inaccessible.
    public event Action<string, ShiftState, Vector3, float> OnShiftStateChanged { add { } remove { } }
    public bool IsShiftPersistent(string shiftId) => false;
}
```
`SessionState` exposes a settable static property, defaulting to the null object; assigning it moves the event subscription as a side effect of the setter — the future Lighting/Volume ADR's own bootstrap is expected to assign this once, deterministically, rather than this system reaching out to find it:
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
    if (newState == ShiftState.ShiftingIn && _lightingQuery.IsShiftPersistent(shiftId)) {
        _persistentShiftIds[shiftId] = true;
    } else if (newState == ShiftState.Held && _firedTriggerIds.Contains(shiftId)) {
        if (_settledTriggerIds.Add(shiftId)) OnTriggerSettled?.Invoke(shiftId);
    }
}
```
This mirrors the GDD's own explicit reasoning: `PersistentShiftIds` must be written on `Shifting-In` (not `Held`) because `IsShiftPersistent` is a synchronous query answerable only at `TriggerShift`-time, and any `MemoryTriggerDef`-linked shift is guaranteed `Persistent=true` and therefore guaranteed to reach `Held` — so marking early is safe, not speculative. `SettledTriggerIds` deliberately lags `FiredTriggerIds` by design (the ~3s lighting ramp) — this lag is the entire fix for the saturation-ending timing defect the GDD's own history documents (`FiredTriggerIds` alone caused `RequestHardCut` to cut off the light+sound payoff for the player's final trigger).

### Subscription timing (design decision, confirmed with user; tightened per Unity specialist + TD-ADR review)
Per the user's confirmed decision, matching Narrative State's GDD-specified solution to the identical problem, this system must never subscribe from a scene-local `MonoBehaviour.Awake()`. The user-confirmed draft originally offered "static constructor or bootstrap `Initialize()`" as equivalent options — both reviewers independently flagged this as a blocking correctness gap: **C# static constructors are lazy**, triggered only on first access to the type, so a static constructor alone does not deterministically guarantee the subscription exists before some other system's first event firing — it only happens to work if something else touches `SessionState` first, which is exactly the ordering dependency this section exists to eliminate.

**Revised, deterministic mechanism**: there is no separate `Initialize()` step at all. The subscription is a side effect of `LightingQuery`'s property setter (see Lighting/Volume Event Contract above), and `ResetForNewSession()` runs via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` — Unity's earliest initialization phase, guaranteed to run before any scene's `Awake()`, with no lazy-triggering ambiguity. The future Lighting/Volume ADR's own bootstrap (expected to use `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` or earlier) assigns `SessionState.LightingQuery` to its real implementation as one of its first actions, which deterministically establishes the subscription before any scene's objects can fire the event.

### Architecture Diagram
```
                    ┌───────────────────────────┐
                    │         SessionState          │
                    │   (static, no GameObject)      │
                    └───────────────────────────┘
        ▲                  ▲             │            │
  reads/writes       EndSession()    OnTriggerFired  OnTriggerSettled
  FiredTriggerIds    (Cutscene,       (future Memory- (this system,
  (Memory-Trigger)    onComplete)      Trigger writes  from Lighting's
                                        via this API)   OnShiftStateChanged)
        │                                  │              │
        ▼                                  ▼              ▼
  reads IsSessionActive              [future Cutscene: subscribes,
  (Elevator, Carry Loop)              queries SettledTriggerIds.Count]

  Subscribes at static-constructor/bootstrap time to:
  [future Lighting/Volume ADR: OnShiftStateChanged, IsShiftPersistent(shiftId)]
```

### Key Interfaces
See Core mechanism above for the full public surface. Summary of cross-system contracts:
- **Read-only queries** (Elevator, Carry Loop): `IsSessionActive`
- **Write + read** (Memory-Trigger): writes `FiredTriggerIds` via an internal method invoked through `OnTriggerFired`'s producer path (not directly — Memory-Trigger's future ADR must call a method this ADR exposes, e.g. `RecordTriggerFired(string shiftId)`, rather than mutating a collection); reads `FiredTriggerIds` at `Awake()` for Committed-state restore
- **Read-only** (Lighting/Volume, future ADR): reads `PersistentShiftIds` at scene load for Persistent-restore
- **Call + subscribe** (Cutscene): calls `EndSession()`; subscribes to `OnTriggerSettled`; queries `SettledTriggerIds.Count`

```csharp
public static class SessionState {
    // ... (fields/properties from Core mechanism)
    public static void RecordTriggerFired(string shiftId); // called only by the future Memory-Trigger system
}
```

## Alternatives Considered

### Alternative 1: MonoBehaviour singleton with `DontDestroyOnLoad`
- **Description**: A `SessionStateManager : MonoBehaviour` with `DontDestroyOnLoad(gameObject)` called in `Awake()`, guarded against duplicate instantiation.
- **Pros**: Inspector-visible at runtime for debugging (values visible in the Hierarchy/Inspector window without a custom debug overlay); familiar traditional Unity singleton pattern.
- **Cons**: Requires explicit duplicate-instance guarding, since an additively loaded scene could theoretically contain a second copy of the singleton prefab; ties session state's lifetime to a `GameObject`/scene-loading lifecycle for no structural benefit, when the state itself has no `Transform`, no `Update()` loop, and no other reason to be a `GameObject`; inconsistent with the pattern this project's own Narrative State and Carry Loop GDDs already lock in.
- **Rejection Reason**: user confirmed the static-class pattern for consistency with sibling systems; Inspector visibility can be recovered cheaply via a small custom Editor debug window if needed later, without constraining the production data model.

### Alternative 2: ScriptableObject-based runtime singleton
- **Description**: A `SessionStateAsset : ScriptableObject` instance referenced by other systems, mutated at runtime as a shared "blackboard."
- **Pros**: Persists automatically without `DontDestroyOnLoad`, since ScriptableObject assets aren't scene-bound; Inspector-assignable references.
- **Cons**: ScriptableObjects are conventionally treated as immutable design-time data — using one as mutable runtime state risks Play-Mode changes leaking into the on-disk asset if `Application.isPlaying`-guarded reset logic is ever missed, a known Unity footgun; adds an asset-management concern (where does the asset live, how is it referenced) for state that has exactly one instance and no reason to be asset-like.
- **Rejection Reason**: the mutability-leak risk is a real production bug class this project has no reason to accept for a system with no design-time-authoring use case at all.

## Consequences

### Positive
- Zero duplicate-instance risk class — static fields cannot be duplicated the way a `MonoBehaviour` singleton prefab could be
- Read-only collection exposure (`IReadOnlySet`/`IReadOnlyDictionary`) closes off direct external mutation as an entire bug class, forcing all writes through this ADR's explicit methods
- Trivially unit-testable — no `GameObject`/scene required to construct or exercise the state machine in an EditMode test
- The static-constructor subscription timing decision, once established here, becomes the pattern the future Narrative State ADR can point back to rather than re-deriving independently

### Negative
- Static state's reset behavior between Editor Play Mode sessions depends on the project's **Configurable Enter Play Mode** setting (`Project Settings → Editor → Enter Play Mode Options`, Unity specialist finding): with the default "Reload Domain" enabled, static fields are reinitialized fresh on every Play Mode entry, which is why `ResetForNewSession()`'s `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` attribute is sufficient — it fires on that same reload. If a teammate disables Domain Reload for faster iteration, static fields persist across Play sessions even more aggressively than a naive reading of this ADR might assume; the `SubsystemRegistration`-attributed reset still fires on Player launch and covers this case, but this setting's existence should be understood by anyone debugging apparent state leakage between manual Play Mode toggles.
- Static `event` fields (`OnTriggerFired`, `OnTriggerSettled`) risk leaked subscribers if a `MonoBehaviour` consumer subscribes without unsubscribing in `OnDisable`/`OnDestroy` — with Domain Reload disabled, a destroyed-but-still-subscribed object's handler would still fire on the next event, a duplicate-invocation bug. Consuming systems' future ADRs must document unsubscription in their own lifecycle methods; this is a project-wide convention this ADR establishes but cannot itself enforce.
- Static classes are harder to swap for automated testing via constructor injection than instance-based services — this project's Coding Standards prefer dependency injection over singletons. TD-ADR review clarified this tension is now scoped correctly: `SessionState`'s own internal state has no Unity dependency and is fully testable as-is; the outbound dependency on Lighting/Volume is injected via `ILightingVolumeQuery` (see Lighting/Volume Event Contract), not a hard reference — so the DI standard is honored for every *outbound* dependency this ADR has, and only the static-class *shape itself* (versus an instance-based service) is the accepted deviation, for consistency with sibling GDDs' own explicit decisions.

### Risks
- If the future Lighting/Volume ADR's concrete implementation doesn't cleanly satisfy `ILightingVolumeQuery` as defined here, that ADR needs a small adapter — same low-risk, narrow-blast-radius pattern already accepted in ADR-0002 for `IFlaggedObjectRegistry`
- The deterministic-ordering risk originally flagged for static-constructor timing is closed by the revised design (property-setter-triggered subscription + `RuntimeInitializeOnLoadMethod`, see Subscription timing) — no remaining ordering risk at the architecture level; the future Lighting/Volume ADR must still be checked to confirm it actually assigns `SessionState.LightingQuery` early in its own bootstrap, not deferred to a scene-local `Awake()`, which would silently reintroduce the same race this ADR closes

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| gece-oturum-durumu-2026-08-02.md | TR-session-001/007/017: in-memory singleton, no disk persistence, resets on restart | Decision → Core mechanism, static plain C# class |
| gece-oturum-durumu-2026-08-02.md | TR-session-008/009/010: subscribes to `OnShiftStateChanged`, calls `IsShiftPersistent` same frame, event carries no `Persistent` field | Decision → Lighting/Volume Event Contract |
| gece-oturum-durumu-2026-08-02.md | TR-session-011: `EndSession()`, callable only by Cutscene | Key Interfaces |
| gece-oturum-durumu-2026-08-02.md | TR-session-012/013/014/015: `OnTriggerFired`/`OnTriggerSettled` timing, saturation must use `SettledTriggerIds` not `FiredTriggerIds` | Decision → Core mechanism, Lighting/Volume Event Contract |
| ani-tetikleyici-etkilesim.md | TR-memory-006/007/008/029: Committed-state restore reads `FiredTriggerIds` at `Awake()`, writes via this ADR's API, never writes to the `MemoryTriggerDef` asset | Key Interfaces (`RecordTriggerFired`) |
| asansor-kat-erisim-sistemi.md | TR-elevator-003/004/018: reads `IsSessionActive` read-only, polled only at press-time | Core mechanism, read-only property exposure |
| gorev-tasima-dongusu.md | TR-carry-010/022: own round/slot state kept in the same in-memory-persistent pattern; reads `IsSessionActive` as a guard | Decision → Core mechanism (pattern precedent), Key Interfaces |
| sahne-kesmeli-anlati-2026-08-02.md | TR-cutscene-002/003/005/011/016: saturation condition gated on `SettledTriggerIds.Count`/`OnTriggerSettled`, calls `EndSession()` | Key Interfaces, Decision → Core mechanism |

## Performance Implications
- **CPU**: negligible — `HashSet`/`Dictionary` operations are O(1) average case, event dispatch is direct delegate invocation, no per-frame polling anywhere in this system
- **Memory**: negligible — MVP's 2-3 memory-trigger content cap means these collections never exceed single-digit entry counts
- **Load Time**: N/A
- **Network**: N/A (single-player)

## Migration Plan
N/A — greenfield system, no existing code to migrate from. Forward-compatibility note (per GDD): the flat `HashSet`/`Dictionary` data model is explicitly stated to already be suitable for future disk serialization (Çoklu Gece İlerlemesi, Vertical Slice) with no structural change required — this ADR does not need to design for that now, only avoid choices that would preclude it later (the static-class choice does not).

## Validation Criteria
- All Acceptance Criteria from `gece-oturum-durumu-2026-08-02.md`, implemented as automated EditMode tests per this project's Logic-tier test-evidence rules
- Explicit test: a `Persistent=false` shift reaching `Shifting-In` must NOT populate `PersistentShiftIds` (GDD's own N2-verification-added AC)
- Explicit test: `SettledTriggerIds.Count < FiredTriggerIds.Count` is a valid, non-error transient state during the ~3s ramp window — assert this is never treated as an error condition anywhere in consuming code
- New test (per Consequences → Negative): `ResetForNewSession()` must return all state to initial values, and a test must verify no field retains a value from a prior test run — guards against the static-state Editor Play Mode staleness risk
- New test: `RecordTriggerFired` called twice with the same `shiftId` fires `OnTriggerFired` exactly once (idempotency, TD-ADR finding)
- New test: `EndSession()` called twice is a no-op the second time — `IsSessionActive` is not observably re-set or re-triggered
- New test: with `LightingQuery` left at its default `NullLightingVolumeQuery`, `OnShiftStateChangedHandler` is never invoked and `PersistentShiftIds`/`SettledTriggerIds` remain empty — verifies the null-object default is inert, not just present
- New test: assigning `LightingQuery` to a mock `ILightingVolumeQuery` correctly moves the subscription (old mock's event no longer triggers the handler after a second assignment, new mock's does) — verifies the property-setter subscription-swap logic

## Related Decisions
- Enables: future ADRs for Anı-Tetikleyici Etkileşim, Asansör/Kat-Erişim Sistemi, Görev/Taşıma Döngüsü, Sahne Kesmeli Anlatı
- Establishes the static-plain-C#-class persistence pattern and static-constructor subscription-timing pattern that the future Anlatı Durum/İpucu Takibi (Narrative State) ADR is expected to also follow, per that GDD's own explicit statement of the same pattern
- Consumer-side dependency on Işık/Volume Durum Sistemi's future ADR — see ADR Dependencies → Ordering Note
- See `docs/architecture/architecture-review-2026-08-05.md` for the full requirements baseline and recommended ADR authoring order
