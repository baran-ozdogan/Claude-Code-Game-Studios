# ADR-0006: Session State Service and Round-Counter Ownership

> **Unity Specialist Validation**: BLOCKING (2 findings, found and fixed) 2026-08-06 — (1) confirmed the `FoundationBootstrap.ResetAll()` ordering defect this ADR sets out to fix is real (ADR-0001's own draft reset `Gece/Oturum Durumu` before `Işık/Volume Durum Sistemi` despite subscribing to its event in its own constructor) and the fix is sound. (2) `IGeceOturumDurumuState` diverged from `architecture.md`'s already-locked Phase 4 API sketch (raw-collection properties instead of `HasFired`/`HasSettled`/`IsPersistent` membership queries) — also flagged an undisclosed `IReadOnlySet<T>` BCL-availability risk under Unity's Api Compatibility Level profiles. Fixed by matching `architecture.md`'s signatures verbatim. Also corrected an inaccurate `InteractableRegistry` precedent citation and an invalid bodiless-method C# sketch.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-06 — 3 findings, all fixed: (1) the `ResetAll()` fix's own prose mischaracterized its diff as a "1↔3 swap" when it's actually a 4-element rotation (`GeceOturumDurumu` 1→4, three others each shift forward one) — corrected, and an Alternatives-Considered entry added for the stronger "two-phase Construct/Wire split" fix that was implicitly skipped without discussion. (2) `EndSession()` had been excluded from `IGeceOturumDurumuState` entirely, contradicting both `architecture.md`'s sketch and ADR-0001's own worked example (both include it as an interface member) — restored; `SetRoundState()` correctly stays facade-only. (3) During finalization, also caught and fixed a related testability gap: `SetRoundState()` as a static-facade-only method would have been unreachable from a test constructing a bare `GeceOturumDurumuState` directly (ADR-0001's own DI/testability pattern) — moved to an instance method reached via an internal `InternalInstance` accessor. **Separately, GDD Sync Check (2026-08-06)** found `architecture.md`'s Phase 4 sketch (inherited into this ADR) never exposed a Count query, even though `sahne-kesmeli-anlati-2026-08-02.md`'s saturation condition and `seviye-sahne-gecisi`'s preload timing both require one — added `FiredCount`/`SettledCount`. A related, longer-standing open GDD item (`TotalConfiguredTriggerCountForNight`'s write-mechanism ownership) was surfaced to the user and explicitly deferred to the future Memory Trigger Orchestration ADR rather than resolved here.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-06

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (C# state management / Editor & runtime lifecycle) |
| **Knowledge Risk** | LOW — this ADR introduces no new engine API surface beyond ADR-0001's already-verified `RuntimeInitializeOnLoadMethod`/static-facade pattern; it is a data-model and ordering decision, not a new mechanism |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/current-best-practices.md`, `docs/architecture/adr-0001-in-memory-static-service-pattern.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None new. This ADR corrects `FoundationBootstrap.ResetAll()`'s ordering (see Decision) — if a future ADR adds a 7th Foundation service that itself subscribes to `Gece/Oturum Durumu`'s events in its own constructor, that ADR must re-verify this ADR's ordering still holds. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (In-Memory Static Service Pattern) — this ADR is the concrete Foundation-service instantiation ADR-0001's own "Enables" list named in advance. ADR-0005 (Işık/Volume Rendering Architecture) — this ADR's write path is driven by Işık/Volume's `OnShiftStateChanged`/`IsShiftPersistent`, both defined there. |
| **Enables** | The future "Carry Loop and Round State" ADR (Görev/Taşıma Döngüsü) — that ADR will specify the round-advancement logic that calls this ADR's `SetRoundState()`, but the write-authority contract itself is fixed here first. |
| **Blocks** | Any story implementing `Gece/Oturum Durumu` itself; any Görev/Taşıma Döngüsü story that advances rounds; any Adaptif Ses Sistemi story that reads round counters for `tension_gain`; any story implementing the psychiatry hard-cut (`Sahne Kesmeli Anlatı`'s `EndSession()` call). |
| **Ordering Note** | This ADR also revises `FoundationBootstrap.ResetAll()`'s service ordering, defined in ADR-0001 — see Decision. That edit lands in ADR-0001's own file, not duplicated here. |

## Context

### Problem Statement

`architecture.md`'s Phase 1 (System Layer Map) resolved a Feature→Foundation layer violation: `Adaptif Ses Sistemi` (Foundation) was reading `Görev/Taşıma Döngüsü`'s (Feature) `CurrentRoundIndex`/`TotalRoundCount` directly, an upward read across two layers. The fix — relocating storage of these two counters to `Gece/Oturum Durumu` (Foundation), while `Görev/Taşıma Döngüsü` keeps computing round-advancement logic and gains a narrow write path down — was decided in principle but never given a concrete mechanism. `design/quick-specs/gece-oturum-durumu-2026-08-02.md` already specifies this system's pre-existing session-fact fields (`IsSessionActive`, `CurrentNightNumber`, `FiredTriggerIds`, `PersistentShiftIds`, `SettledTriggerIds`) and their write/read contracts in detail, but was written before the round-counter relocation and does not cover it.

Separately, `architecture.md`'s self-review (TD-ARCHITECTURE, 2026-08-05) flagged QQ-03: this system's two single-caller write methods — `EndSession()` (only `Sahne Kesmeli Anlatı` may call it) and the not-yet-named round-counter write method (only `Görev/Taşıma Döngüsü` may call it) — have no enforcement mechanism beyond `internal` visibility, which does not actually restrict callers within a single default Unity assembly.

This ADR is the concrete implementation contract for `Gece/Oturum Durumu` as a whole: its full data model (pre-existing fields + the relocated round counters), its participation in ADR-0001's `FoundationBootstrap.ResetAll()` pattern, and the resolution of QQ-03 for its two single-caller methods.

### Constraints

- Must reuse ADR-0001's static-service/interface/static-facade pattern exactly — this is the sixth and final Foundation service to adopt it, not a new pattern.
- Must not reintroduce the Feature→Foundation layer violation `architecture.md` Phase 1 already resolved — `Görev/Taşıma Döngüsü` must retain ownership of round-advancement *logic*, only the counter *storage* moves.
- Must resolve QQ-03 for exactly the two methods this ADR defines (`EndSession()`, and the round-counter writer) — per the user's direction, with a convention-based (not compile-time) enforcement mechanism, consistent with this being a solo/small-team MVP project (see Decision).
- `CurrentNightNumber` is fixed at `1` for MVP (single-night scope) — this ADR does not design multi-night persistence; `design/quick-specs/gece-oturum-durumu-2026-08-02.md` explicitly defers that to a future Çoklu Gece İlerlemesi system.

### Requirements

- `FiredTriggerIds`/`OnTriggerFired`, `PersistentShiftIds`, and `SettledTriggerIds`/`OnTriggerSettled` must preserve the exact write timing already locked in `design/quick-specs/gece-oturum-durumu-2026-08-02.md` Core Rules — this ADR does not revisit that timing, only formalizes its implementation. *(Clarified by ADR-0014, 2026-08-08: the timings coincide but the WRITERS differ — Fired is a **direct** write by Anı-Tetikleyici's `OnHoldComplete()` via `AddFiredTrigger` [same frame as `TriggerShift`, which is why its timing reads as "Shifting-In"]; Persistent [Shifting-In] and Settled [Held] are written by this service's own `OnShiftStateChanged` subscription. The original wording lumped Fired under the subscription framing — the likely seed of the registry's over-generalized write_access line, both now corrected.)*
- `CurrentRoundIndex`/`TotalRoundCount` must be readable by `Adaptif Ses Sistemi` via a plain polled property read (per `gorev-tasima-dongusu.md`'s own note: round changes are low-frequency, no event needed) and writable only by `Görev/Taşıma Döngüsü`.
- Must participate correctly in `FoundationBootstrap.ResetAll()` — including fixing the ordering defect described below.

## Decision

### The FoundationBootstrap.ResetAll() ordering defect

While drafting this ADR, cross-referencing ADR-0001's `FoundationBootstrap.ResetAll()` order against `design/quick-specs/gece-oturum-durumu-2026-08-02.md`'s own Dependencies section surfaced a live defect in ADR-0001's own code:

ADR-0001's prose explicitly names `Gece/Oturum Durumu` as one of three services that subscribe to `Işık/Volume Durum Sistemi`'s `OnShiftStateChanged` event **inside their own constructors** (to populate `PersistentShiftIds`/`SettledTriggerIds` — confirmed by `gece-oturum-durumu-2026-08-02.md` Core Rules and `isik-volume-durum-sistemi.md` Dependencies, both of which describe this subscription explicitly). Yet ADR-0001's `FoundationBootstrap.ResetAll()` code block resets `GeceOturumDurumu` **first** (position 1, commented "no upstream Foundation dependency") and `IsikVolumeDurumSistemi` **fourth** (after it). This reproduces, inside ADR-0001's own fix, the exact bug the fix exists to prevent: `GeceOturumDurumu.ResetOnLoad()` would re-subscribe to the *stale, about-to-be-replaced* `IsikVolumeDurumSistemi` instance, silently dropping all real `OnShiftStateChanged` events (and therefore `PersistentShiftIds`/`SettledTriggerIds`) for the rest of the session.

The "depends on Gece/Oturum Durumu (Persistent-restore)" comment attached to `IsikVolumeDurumSistemi`'s original position (4th) conflates two different mechanisms: `isik-volume-durum-sistemi.md`'s Persistent-restore read happens per-`ShiftZone`-instance, at that `MonoBehaviour`'s own `Awake()`/`OnEnable()` on every scene load — a completely separate timing from `FoundationBootstrap.ResetAll()`, which only fires once per Play-mode session/process start. `IsikVolumeDurumSistemi.ResetOnLoad()` itself has no constructor-time subscription to any other Foundation service (confirmed: it only *exposes* `OnShiftStateChanged`, it does not subscribe to anything) — it has no upstream Foundation dependency for reset-ordering purposes and can safely reset early.

**Fix (edited into ADR-0001, not duplicated here as a second source of truth):**

```csharp
private static void ResetAll() {
    SeviyeSahneGecisi.ResetOnLoad();       // no upstream Foundation dependency
    InteractableRegistry.ResetOnLoad();    // no upstream dependency
    IsikVolumeDurumSistemi.ResetOnLoad();  // no upstream Foundation dependency — exposes
                                            // OnShiftStateChanged but subscribes to nothing itself
    GeceOturumDurumu.ResetOnLoad();        // subscribes to Işık/Volume's OnShiftStateChanged
                                            // in its own constructor — must reset AFTER Işık/Volume
                                            // (ADR-0006 fix — original draft had this reversed)
    AnlatiDurumIpucuTakibi.ResetOnLoad();  // subscribes to Işık/Volume's OnShiftStateChanged
    AdaptifSesSistemi.ResetOnLoad();       // subscribes to Işık/Volume + Seviye/Sahne Geçişi
                                            // events, reads Gece/Oturum's round counters —
                                            // must reset after both
}
```

**Corrected during TD-ADR review (2026-08-06)**: an earlier draft of this section described the change as `GeceOturumDurumu` and `IsikVolumeDurumSistemi` "swapping positions (1↔3)." That mischaracterizes the actual diff: `GeceOturumDurumu` moves from position 1 to position 4, and `SeviyeSahneGecisi`, `InteractableRegistry`, and `IsikVolumeDurumSistemi` each shift forward one slot as a result — a rotation of the first four entries, not a pairwise swap. `AnlatiDurumIpucuTakibi` and `AdaptifSesSistemi` are the only two whose position is genuinely unchanged (5th and 6th, same as ADR-0001's original), since both already reset after `IsikVolumeDurumSistemi` and neither is read by `GeceOturumDurumu` at construction time. The per-line ordering-rationale comments in the code block above are correct against this new order (re-verified during TD-ADR review); only the prose summary was wrong.

**Alternative fix considered — decoupling construction from event-wiring, rather than reordering**: a structurally stronger fix exists and was weighed against the reordering above: split `FoundationBootstrap.ResetAll()` into two passes — `ConstructAll()` (creates every fresh state instance, no event subscriptions yet) followed by `WireEvents()` (runs only after every service is already fresh, subscribing across services in any order). This eliminates the entire "reset-order-dependent constructor subscription" hazard by construction, for this service and any future one — a 7th constructor-subscribing service could never reproduce this bug class, whereas simple reordering (the chosen fix) still requires a human to get the order right every time a new subscriber is added (see Risks). Rejected for this ADR specifically because it would mean re-opening and restructuring ADR-0001's core mechanism (not just its ordering list) for a problem that, at MVP scale, affects exactly 3 known constructor-subscribing services (`Gece/Oturum Durumu`, `Anlatı Durum/İpucu Takibi`, `Adaptif Ses Sistemi`) — the simpler fix is proportionate today, but the two-phase split should be reconsidered if a future ADR adds enough new subscribers that manually maintaining `ResetAll()`'s order stops being reliable (see Risks → Mitigation).

### Data model

`Gece/Oturum Durumu` follows ADR-0001's generic shape exactly:

**Corrected during unity-specialist validation (2026-08-06)**: the first draft of this interface exposed the raw collections (`IReadOnlySet<string> FiredTriggerIds`, `IReadOnlyDictionary<string,bool> PersistentShiftIds`, `IReadOnlySet<string> SettledTriggerIds`). This diverged from `architecture.md`'s own already-locked Phase 4 API Boundaries sketch for this exact system (lines 268–279), which instead exposes membership via `HasFired(string)`/`HasSettled(string)`/`IsPersistent(string)` — and `IReadOnlySet<T>` (.NET 5+) is not guaranteed available under Unity's supported Api Compatibility Level profiles (.NET Standard 2.1 / .NET Framework), an undisclosed engine-surface risk on top of the fidelity break. Corrected below to match `architecture.md`'s signatures verbatim — this ADR implements that already-reviewed sketch rather than re-deriving a new shape.

**GDD Sync Check finding (2026-08-06)**: cross-referencing `sahne-kesmeli-anlati-2026-08-02.md`'s Acceptance Criteria and Preload-timing note (lines 149, 165–174, 293) against the interface above surfaced a real gap in `architecture.md`'s own Phase 4 sketch, inherited into this ADR's first pass: `Sahne Kesmeli Anlatı`'s saturation condition (`SettledTriggerIds.Count == TotalConfiguredTriggerCountForNight`) and `Seviye/Sahne Geçişi`'s preload-timing condition (`FiredTriggerIds.Count == TotalConfiguredTriggerCountForNight - 1`) both require a **count**, not a per-id boolean membership check — and neither `architecture.md`'s sketch nor this ADR's own first draft exposed one. `FiredCount`/`SettledCount` added above to close this. This does not fully close the underlying GDD gap — see the separate, still-open `TotalConfiguredTriggerCountForNight` field-ownership question below, presented to the user for a scope decision rather than resolved unilaterally here.

**`TotalConfiguredTriggerCountForNight` — explicitly deferred (user decision, 2026-08-06)**: `sahne-kesmeli-anlati-2026-08-02.md`'s own Tuning Knobs note (added 2026-08-03, still unresolved as of this ADR) already identifies `Gece/Oturum Durumu` as this field's natural owner, but nothing has ever decided *who sets it or when* (candidate: `Anı-Tetikleyici Etkileşim` counting its own `MemoryTriggerDef` registrations at scene load). Rather than design that write mechanism here — which would mean making assumptions about a system whose own ADR (`Required ADR #14, "Memory Trigger Orchestration"`) doesn't exist yet — this ADR leaves the field out of `IGeceOturumDurumuState` entirely and defers the decision to that future ADR. The still-open item itself is unchanged by this ADR; it is neither closed nor made worse.

```csharp
public interface IGeceOturumDurumuState {
    bool IsSessionActive { get; }
    int CurrentNightNumber { get; }
    int CurrentRoundIndex { get; }   // 0-based
    int TotalRoundCount { get; }
    bool HasFired(string shiftId);      // FiredTriggerIds membership
    bool HasSettled(string shiftId);    // SettledTriggerIds membership
    bool IsPersistent(string shiftId);  // PersistentShiftIds membership
    int FiredCount { get; }             // FiredTriggerIds.Count — NEW, see GDD Sync Check note below
    int SettledCount { get; }           // SettledTriggerIds.Count — NEW, see GDD Sync Check note below
    int TotalConfiguredTriggerCountForNight { get; }  // ADDED by ADR-0014 (2026-08-08) — the follow-up
                                        // edit this ADR's Related Decisions reserved; written once at
                                        // night begin via InternalInstance (see below), build-verified
                                        // against the real MemoryTriggerDef asset count
    void EndSession();                  // Sahne Kesmeli Anlatı only, by convention (see QQ-03)

    event Action<string> OnTriggerFired;
    event Action<string> OnTriggerSettled;
}

public sealed class GeceOturumDurumuState : IGeceOturumDurumuState {
    // backing HashSet<string>/Dictionary<string,bool> fields for the
    // membership queries above. Mutation paths (comment corrected by
    // ADR-0014, 2026-08-08 — the original "all mutation happens through
    // EndSession() and SetRoundState(), never any other path" contradicted
    // this class's own constructor-time subscription writes below):
    // EndSession(), SetRoundState(), SetTotalConfiguredTriggerCountForNight(),
    // AddFiredTrigger() (all internal, single-caller by convention), plus
    // the constructor-subscribed OnShiftStateChanged handler's
    // PersistentShiftIds (Shifting-In) and SettledTriggerIds (Held,
    // gated on FiredTriggerIds membership) writes.
    // FiredCount/SettledCount are plain _fired.Count/_settled.Count
    // pass-throughs — no separate counter field, no drift possible.
    public void EndSession() => IsSessionActive = false;

    // Deliberately NOT on IGeceOturumDurumuState — a Feature-layer write
    // path (Görev/Taşıma Döngüsü only), not something a general consumer
    // should see. Kept as an instance method (not static-facade-only) so
    // a test can construct `new GeceOturumDurumuState()` directly and call
    // this without touching the static facade at all — matching ADR-0001's
    // own testability pattern (its Architecture Diagram: test code path
    // "never touches the static facade"). A static-facade-only method
    // would have reproduced the exact DI/testability gap ADR-0001's QQ-06
    // exists to prevent.
    internal void SetRoundState(int currentRoundIndex, int totalRoundCount) {
        CurrentRoundIndex = currentRoundIndex;
        TotalRoundCount = totalRoundCount;
    }

    // ADDED by ADR-0014 (2026-08-08) — SetRoundState's twins, same
    // instance-method-for-testability shape, same convention-enforced
    // single callers (night-begin orchestrator / Anı-Tetikleyici):
    internal void SetTotalConfiguredTriggerCountForNight(int count) {
        TotalConfiguredTriggerCountForNight = count;
    }
    internal void AddFiredTrigger(string shiftId) {
        // Idempotent (HashSet.Add); fires OnTriggerFired only on first
        // add — the quick spec's locked direct-write model ("Anı-
        // Tetikleyici Etkileşim'in OnHoldComplete()'i tarafından
        // eklendiği aynı karede"). NOT written by the OnShiftStateChanged
        // subscription — that handler writes Persistent (Shifting-In)
        // and Settled (Held) only; Fired's direct write is what keeps
        // the Automatic ambient zone's id out of the Settled gate.
        if (_fired.Add(shiftId)) OnTriggerFired?.Invoke(shiftId);
    }
    // ...
}

public static class GeceOturumDurumu {
    public static IGeceOturumDurumuState Instance => _current;

    // Internal-only, concrete-typed accessor — lets Görev/Taşıma Döngüsü
    // reach SetRoundState() (an instance method, see above) without it
    // being exposed on the public read interface. Enforcement of "only
    // Görev/Taşıma calls this" is convention + XML-doc + code review only
    // (QQ-03 resolution below), not compiler-checked.
    internal static GeceOturumDurumuState InternalInstance => _current;

    internal static void ResetOnLoad() => _current.ResetOnLoad();  // called only by FoundationBootstrap.ResetAll().
    // CONVERTED TO IN-PLACE by ADR-0015 (2026-08-08): previously replaced
    // _current with a fresh instance. This facade exposes events
    // (OnTriggerFired/OnTriggerSettled) with a persistent Start()/OnEnable-
    // time MonoBehaviour subscriber (SahneKesmeliAnlatiController) —
    // replacement reset would orphan those subscriptions after the first
    // ResetAll() (ADR-0011's wholesale_state_replacement forbidden
    // pattern). The instance-level ResetOnLoad() clears all session sets/
    // counters IN PLACE and explicitly re-initializes non-default fields
    // replacement restored for free — notably IsSessionActive = true.
    // The constructor-time Işık/Volume subscription now runs ONCE per
    // process and survives every reset (Işık/Volume is likewise in-place
    // per ADR-0015) — no re-wire, no accumulation.

    private static GeceOturumDurumuState _current = new();

    // Subscribes to Işık/Volume's OnShiftStateChanged at first construction
    // (static field initializer / static constructor) — populates
    // PersistentShiftIds on Shifting-In, SettledTriggerIds on Held,
    // per gece-oturum-durumu-2026-08-02.md Core Rules. This subscription
    // is why ResetAll() must run Işık/Volume's reset first (see above).
}
```

Caller usage: `GeceOturumDurumu.InternalInstance.SetRoundState(newIndex, totalCount);` — `Görev/Taşıma Döngüsü` calls this on every round transition (see Round-counter write path below).

**Corrected during TD-ADR review (2026-08-06)**: an earlier draft of this section excluded `EndSession()` from `IGeceOturumDurumuState` entirely (facade-only, like `SetRoundState()`). That contradicted two already-established artifacts — ADR-0001's own worked example for this exact system (lines 143–153, which lists `void EndSession();` as an interface member) and `architecture.md`'s Phase 4 API Boundaries sketch (lines 268–279, same). Restored to match: `EndSession()` is a public interface member (any consumer can *observe* `IsSessionActive` going false through it, matching the rest of the read surface), while `SetRoundState()` stays facade-only and `internal` — neither `architecture.md`'s sketch nor ADR-0001's worked example lists it on the interface (ADR-0001 explicitly defers "internal-visibility write methods... SetRoundState... omitted here" to this ADR). Single-caller restriction for both methods is convention + XML-doc + code review only, not compiler-checked (see Alternatives Considered / QQ-03 resolution below). **Also corrected during unity-specialist validation**: an earlier draft of this section cited `InteractableRegistry`'s `Register`/`Deregister` (ADR-0004) as precedent for this convention-only pattern — that citation was wrong. `Register`/`Deregister` are `public` and intentionally called by *every* `IInteractable` implementer, an intentionally multi-caller API, not a single-caller-restricted one; there is no existing precedent in this project for convention-only single-caller enforcement. The QQ-03 resolution stands on its own reasoning (disproportionate cost of an assembly-definition split for 2 methods, explicit user sign-off) rather than on a false analogy.

### Round-counter write path

`Görev/Taşıma Döngüsü` still computes round advancement (its own state machine, task/slot logic — unchanged, to be formalized in the future "Carry Loop and Round State" ADR). On every round transition, it calls `GeceOturumDurumu.InternalInstance.SetRoundState(newIndex, totalCount)`. This is a **method call**, not an event subscription — deliberately: `Görev/Taşıma Döngüsü` is Feature-layer and `Gece/Oturum Durumu` is Foundation-layer; a Feature-layer system calling a Foundation-layer system's public (internal) method is the architecturally allowed direction (higher layer depends on lower layer). The reverse shape — `Gece/Oturum Durumu` subscribing to a `Görev/Taşıma Döngüsü`-owned event — was considered and rejected (see Alternatives Considered) because it would make a Foundation service depend on a Feature-layer event source, which is backwards regardless of which direction data flows once the subscription exists.

`Adaptif Ses Sistemi` reads `CurrentRoundIndex`/`TotalRoundCount` via a plain property read on `GeceOturumDurumu.Instance` inside its own ambiance-update loop — no event needed, matching `gorev-tasima-dongusu.md`'s own note that round changes are low-frequency enough to poll.

### Architecture Diagram

```
Görev/Taşıma Döngüsü (Feature)              Işık/Volume Durum Sistemi (Foundation)
       │                                              │
       │ SetRoundState(idx, total)                    │ OnShiftStateChanged(id, state, ...)
       │ (internal method call,                       │ (event, Foundation → Foundation,
       │  Feature → Foundation,                       │  GeceOturumDurumu subscribes in
       │  allowed direction)                          │  its own static ctor/field init)
       ▼                                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Gece/Oturum Durumu                          │
│  IsSessionActive · CurrentNightNumber · FiredTriggerIds ·         │
│  PersistentShiftIds · SettledTriggerIds ·                         │
│  CurrentRoundIndex/TotalRoundCount  ⟵ relocated from Feature      │
│                                                                     │
│  Public reads: IGeceOturumDurumuState (Instance) —                │
│    HasFired/HasSettled/IsPersistent membership queries,           │
│    EndSession() also lives here (interface member)                │
│  Single-caller writes (convention-enforced, not compiler):         │
│    Instance.EndSession()                — Sahne Kesmeli Anlatı only│
│    InternalInstance.SetRoundState(...)   — Görev/Taşıma only       │
└─────────────────────────────────────────────────────────────────┘
       ▲                                              ▲
       │ IsSessionActive (read)                       │ CurrentRoundIndex/TotalRoundCount (read,
       │                                               │  polled in ambiance loop, no event)
  Asansör/Kat-Erişim Sistemi                    Adaptif Ses Sistemi (Foundation)
  (Core)
```

### Key Interfaces

See Data model above. `EndSession()`/`SetRoundState()` signatures:

```csharp
void EndSession();  // IGeceOturumDurumuState member — GeceOturumDurumu.Instance.EndSession()
// Called exactly once per night, only by Sahne Kesmeli Anlatı's RequestHardCut
// onComplete path (psychiatry cutscene start). Sets IsSessionActive = false.
// No reverse (session cannot restart within MVP scope). Interface member,
// matching architecture.md's Phase 4 sketch and ADR-0001's worked example;
// restriction to its one caller is convention + XML-doc + code review only.

internal void SetRoundState(int currentRoundIndex, int totalRoundCount);
// GeceOturumDurumuState instance member, reached via GeceOturumDurumu.InternalInstance
// (not on IGeceOturumDurumuState — see Data model for why). Called by Görev/Taşıma
// Döngüsü on every round transition (RoundComplete → Idle, or once at night start
// for the first round). currentRoundIndex is 0-based; totalRoundCount is fixed
// for the night (TaskList.Length, 3-5).
```

## Alternatives Considered

### Alternative 1: Leave round counters owned by Görev/Taşıma Döngüsü, keep the upward read
- **Description**: Do not relocate `CurrentRoundIndex`/`TotalRoundCount`; `Adaptif Ses Sistemi` (Foundation) reads them directly from `Görev/Taşıma Döngüsü` (Feature).
- **Pros**: No new fields on `Gece/Oturum Durumu`; round data stays next to the logic that produces it.
- **Cons**: Reintroduces the exact 2-layer upward read `architecture.md` Phase 1 already identified and rejected — a Foundation-layer system would have a compile-time/logical dependency on a Feature-layer system, breaking the "layers only read downward" principle this project has enforced for every other Foundation service.
- **Rejection Reason**: Already rejected at the architecture-document level (Phase 1, TD-approved); this ADR implements that decision rather than re-litigating it.

### Alternative 2: Model the round-counter write as an event, not a method call
- **Description**: `Görev/Taşıma Döngüsü` fires `OnRoundChanged(index, total)`; `Gece/Oturum Durumu` subscribes and updates its own fields.
- **Pros**: Matches this project's general "narrow typed events, not direct calls" preference used elsewhere (e.g. `Işık/Volume.OnShiftStateChanged`).
- **Cons**: The event's *producer* would be a Feature-layer system and the *subscriber* a Foundation-layer system — the subscription itself is a Foundation→Feature dependency (Foundation's static constructor would need to reference `Görev/Taşıma Döngüsü`'s type to subscribe to its event), which is backwards regardless of which direction the data value flows once wired. A direct method call keeps the dependency arrow pointing the allowed way (Feature calls into Foundation's public surface).
- **Rejection Reason**: Violates the layer-dependency direction, not just the naive "who reads whose data" framing — the event-vs-call distinction matters architecturally, even though both would move the same two integers.

### Alternative 3: Split `Gece/Oturum Durumu` into two services (trigger/session bookkeeping vs. round counters)
- **Description**: Keep the pre-existing fields in one static service, put the newly-relocated round counters in a second, separate Foundation static service.
- **Pros**: Smaller class per service; a reader only interested in round counters doesn't see trigger-tracking fields.
- **Cons**: `design/quick-specs/gece-oturum-durumu-2026-08-02.md` already defines this as one cohesive bookkeeping system (its own Overview: "bir 'gece' oturumunun ne zaman aktif olduğunu... izleyen saf bir bookkeeping servisidir"), and round counters are themselves session-scoped facts, not a conceptually distinct domain — splitting would fragment one design document's system into two implementation classes for no accepted benefit, and would require every consumer of both (there are none currently, but `Sahne Kesmeli Anlatı` reads `SettledTriggerIds` while future systems may reasonably want both) to hold two static references instead of one.
- **Rejection Reason**: No benefit identified that outweighs fragmenting an already-approved, cohesive quick-spec into two classes.

### Alternative 4 (QQ-03 resolution): Compile-time single-caller enforcement via dedicated assembly definition
- **Description**: Move `Gece/Oturum Durumu` into its own `.asmdef`, use `[InternalsVisibleTo]` to expose `EndSession()`/`SetRoundState()` only to the specific assemblies containing `Sahne Kesmeli Anlatı`/`Görev/Taşıma Döngüsü`.
- **Pros**: Actually compiler-enforced — a misplaced call from an unauthorized class would fail to build, not just fail code review.
- **Cons**: Introduces the project's first assembly-definition split; every other Foundation service currently lives in the same default `Assembly-CSharp` assembly, so this ADR would be establishing a new, one-off structural pattern for the sake of 2 methods. `InternalsVisibleTo` also doesn't restrict *which class* in the granted assembly may call the member, only which assembly — so it only narrows the caller pool, it doesn't uniquely pin it to `Sahne Kesmeli Anlatı`/`Görev/Taşıma Döngüsü` either, unless those two systems are themselves split into their own assemblies too (cascading scope).
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-06): disproportionate cost for a solo/small-team MVP project with only 2 single-caller methods; convention (visibility + XML-doc "only X may call this" + code review) is sufficient at this scale. **Correction (unity-specialist validation, 2026-08-06)**: an earlier version of this rejection reason cited `InteractableRegistry`'s `Register`/`Deregister` (ADR-0004) as an accepted precedent for this pattern — that citation was inaccurate (`Register`/`Deregister` are intentionally multi-caller, not single-caller-restricted; see Decision → Data model for the corrected framing). This resolution stands on the disproportionate-cost reasoning and explicit user sign-off alone, not on a false analogy. Recorded here as the resolution of `architecture.md`'s QQ-03 for these two specific methods.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #6, one of the 4 remaining "must have before coding" Foundation ADRs.
- Resolves QQ-03 for its two owning methods — `architecture.md`'s Open Questions can mark this sub-item closed (the general assembly-definition question remains open for any *future* single-caller method elsewhere, per QQ-03's own scope).
- Fixes a genuine, previously-undetected ordering defect in ADR-0001's `FoundationBootstrap.ResetAll()` — found only because this ADR required cross-referencing ADR-0001's prose against the actual GDD dependency text, which the original ADR-0001 draft did not do for this specific service.
- `Görev/Taşıma Döngüsü` keeps full ownership of round-advancement logic — this ADR only relocates the two counters' storage, not the computation, matching the "layers only read downward, storage moves not logic" principle already established by the Interactable/round-counter relocations in `architecture.md` Phase 1.

### Negative
- `Gece/Oturum Durumu` now has a write dependency from a Feature-layer system (`Görev/Taşıma Döngüsü`) in addition to its existing Foundation-only read/write pattern — this is architecturally allowed (Feature calling down into Foundation) but means this one Foundation service has a slightly wider caller surface than the other 5, worth remembering if a future ADR ever needs to reason about "which Foundation services are purely Foundation-internal."
- The convention-only enforcement for `EndSession()`/`SetRoundState()` (Alternative 4 rejected) means a future refactor accidentally adding a second caller to either method would not fail to compile — only a code-review catch or a runtime symptom (e.g. round counters skipping/reverting unexpectedly) would surface it. Mitigated by the XML-doc "only X may call this" comment being immediately visible at the call site and by this being an explicit control-manifest rule (see Migration Plan).

### Risks
- **Risk**: A future Foundation service subscribing to `Gece/Oturum Durumu`'s own events (`OnTriggerFired`/`OnTriggerSettled`) in its own constructor would need to reset *after* `Gece/Oturum Durumu` in `FoundationBootstrap.ResetAll()` — the same class of ordering bug this ADR just fixed could recur for a 7th service. **Mitigation**: the `ResetAll()` code block now carries an explicit ordering-rationale comment per line (not just a bare list), and this ADR's Verification Required note flags it for any future ADR to re-check.
- **Risk**: `SetRoundState`'s two-parameter signature (`currentRoundIndex`, `totalRoundCount`) could be called with a stale/inconsistent pair if `Görev/Taşıma Döngüsü` ever computes them in two separate steps instead of atomically. **Mitigation**: signature takes both values in a single call specifically to prevent a caller from updating one without the other; this is a Validation Criteria item below.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `design/quick-specs/gece-oturum-durumu-2026-08-02.md` | Full Core Rules data model (`IsSessionActive`, `CurrentNightNumber`, `FiredTriggerIds`, `PersistentShiftIds`, `SettledTriggerIds`) and their exact write-timing rules | Implemented verbatim via `IGeceOturumDurumuState`/`GeceOturumDurumuState`, per ADR-0001's static-service shape |
| `design/quick-specs/gece-oturum-durumu-2026-08-02.md` | `EndSession()` public API, "only Sahne Kesmeli Anlatı çağırır" | `void EndSession()` on `IGeceOturumDurumuState` (matches `architecture.md`'s Phase 4 sketch and ADR-0001's worked example), XML-doc-restricted to that one caller (QQ-03 resolution) |
| `gorev-tasima-dongusu.md` | `CurrentRoundIndex`/`TotalRoundCount` exposed as read-only queries for Adaptif Ses's `tension_gain` mechanism | Relocated to `Gece/Oturum Durumu` per `architecture.md` Phase 1; `Görev/Taşıma Döngüsü` writes via `SetRoundState()`, `Adaptif Ses Sistemi` reads via `Instance.CurrentRoundIndex`/`TotalRoundCount` |
| `isik-volume-durum-sistemi.md` | `Gece/Oturum Durumu` subscribes to `OnShiftStateChanged` to populate `PersistentShiftIds` (`Shifting-In`) and `SettledTriggerIds` (`Held`) | Subscription happens at static construction, per ADR-0001's pattern; this ADR's `FoundationBootstrap.ResetAll()` fix guarantees the subscription is never bound to a stale instance |
| `architecture.md` | QQ-03 (single-caller enforcement for `EndSession`/`SetRoundState`) | Resolved: convention + code review, not compile-time (Alternative 4) |
| `sahne-kesmeli-anlati-2026-08-02.md` | Saturation condition needs `SettledTriggerIds.Count`; `seviye-sahne-gecisi` preload timing needs `FiredTriggerIds.Count` (both currently only expressed as raw-collection `.Count` in the GDD prose) | `FiredCount`/`SettledCount` added to `IGeceOturumDurumuState` (GDD Sync Check finding, 2026-08-06) — gap existed in `architecture.md`'s own Phase 4 sketch too, not introduced by this ADR |

## Performance Implications
- **CPU**: Negligible — plain field reads/writes and `HashSet`/`Dictionary` operations, no per-frame polling of this service beyond `Adaptif Ses Sistemi`'s already-existing ambiance update loop (which now also reads 2 extra ints).
- **Memory**: Negligible — 2 new `int` fields added to an already-small static object; no new allocations beyond what `Görev/Taşıma Döngüsü` already does for its own round bookkeeping.
- **Load Time**: None — no disk I/O, no asset loading.
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Gece/Oturum Durumu` is not yet implemented). At `/create-control-manifest` time, this ADR's convention-based single-caller rule (`EndSession()`/`SetRoundState()`) should become an explicit control-manifest line item, since it is the kind of rule `.claude/docs/coding-standards.md`'s review process would otherwise have no written source to check against.

## Validation Criteria
- `FoundationBootstrap.ResetAll()` resets `Gece/Oturum Durumu` strictly after `Işık/Volume Durum Sistemi` — a unit test (with Domain Reload disabled, simulating the Editor fast-iteration setting) should assert that after two consecutive `ResetAll()` calls, a `PersistentShiftIds` write from a freshly-fired `OnShiftStateChanged` event lands in the *current* session's dictionary, not a stale one.
- `SetRoundState(idx, total)` always updates both fields atomically — no consumer should ever observe `CurrentRoundIndex`/`TotalRoundCount` in a state where one reflects the new round and the other the old one.
- `EndSession()` called more than once in a session is idempotent (matches this project's "public write APIs are idempotent by default" architecture principle) — second and further calls are no-ops, `IsSessionActive` stays `false`.
- `SetRoundState()` called by anything other than `Görev/Taşıma Döngüsü` is a code-review-time violation, not a build-time one — this is an accepted, documented limitation (see Consequences → Negative), not a test target.

## Related Decisions
- ADR-0001 (In-Memory Static Service Pattern) — this ADR's foundational mechanism, and the file this ADR edits for the `FoundationBootstrap.ResetAll()` ordering fix.
- ADR-0004 (InteractableRegistry Foundation Ownership) — governs the same `Assembly-CSharp` single-assembly default this ADR's QQ-03 resolution relies on; does **not** establish a single-caller-enforcement precedent (its `Register`/`Deregister` are intentionally multi-caller — corrected during unity-specialist validation, see Decision → Data model).
- ADR-0005 (Işık/Volume Rendering Architecture) — source of `OnShiftStateChanged`/`IsShiftPersistent`, which this ADR's write path depends on.
- Future "Carry Loop and Round State" ADR (Görev/Taşıma Döngüsü, Required ADR #13) — will specify the round-advancement logic that calls `SetRoundState()`, using this ADR's contract as given.
- Future "Memory Trigger Orchestration" ADR (Anı-Tetikleyici Etkileşim, Required ADR #14) — must resolve `TotalConfiguredTriggerCountForNight`'s write mechanism (explicitly deferred by this ADR, see Data model) and, once decided, should add the field to `IGeceOturumDurumuState` as a small follow-up edit to this ADR rather than inventing a second home for it.
