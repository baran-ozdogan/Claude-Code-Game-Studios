# ADR-0004: InteractableRegistry Foundation Ownership

> **Unity Specialist Validation**: BLOCKING (found and fixed) 2026-08-05 — the original draft's blanket "no `FoundationBootstrap` participation needed" claim was wrong for the frame-snapshot cache fields (`_snapshotFrame`/`_frameSnapshot`, keyed on `Time.frameCount`, which resets every Play session): with Domain Reload disabled, these survive across sessions and can return a stale, possibly-destroyed-object-referencing snapshot if two sessions' frame counts collide — a concrete, plausible bug, not theoretical. Fixed with a minimal `ResetOnLoad()` for just those 2 fields, registered in ADR-0001's `FoundationBootstrap.ResetAll()` (which, notably, already listed this call before this ADR was even drafted). The self-correcting reasoning for the live `_live` list itself was independently confirmed sound.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-05 — 4 findings, all fixed: (1) the Decision headline and Performance Implications still stated the pre-fix "no participation at all" claim, contradicting the rest of the document — corrected; (2) a missing Consequences → Negative acknowledgment of the new cross-file coordination cost with ADR-0001 — added; (3) the mid-frame async-unload risk's "low severity" claim was asserted without reasoning — backed with the actual argument (elevator ride uses `MoveOnly` lock so SphereCast keeps running, but its 2m range means departing-scene interactables are never in range during the unload); (4) `_live`'s self-correction assumption (every `IInteractable` lives in a scene that's actually torn down) made explicit as a Constraint, since it would silently break for a future interactable placed in a persistent scene. A secondary, non-blocking finding (an ADR-0001 numbering error conflating sequential file numbers with Required-ADRs-list ordinal position) was also fixed in ADR-0001 while here.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core |
| **Knowledge Risk** | LOW — this is a plain C# static collection with `OnEnable`/`OnDisable` self-registration, a pattern that predates Unity 6 by many years and is unaffected by any post-cutoff change. |
| **References Consulted** | `docs/engine-reference/unity/current-best-practices.md` (no directly relevant post-cutoff item), `docs/architecture/adr-0001-in-memory-static-service-pattern.md` (Reload Scene / `OnEnable`/`OnDisable` behavior, reused here) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | **ADR-0001 (In-Memory Static Service Pattern)** — corrected from "None" (unity-specialist validation, 2026-08-05): `InteractableRegistry.ResetOnLoad()` must be added to `FoundationBootstrap.ResetAll()`'s explicit dependency-ordered call sequence (as a root, no upstream dependency, alongside `Gece/Oturum Durumu`/`Seviye/Sahne Geçişi`) — a real, not just conceptual, dependency now that the frame-snapshot cache needs the same reset hook. |
| **Enables** | Any story implementing `IInteractable`-consuming systems: Etkileşim Sistemi (Core), Görev/Taşıma Döngüsü's pickups, Anı-Tetikleyici Etkileşim's memory triggers, and `birinci-sahis-kontrolcu.md`'s approach-slow-taper formula |
| **Blocks** | Any interactable object (real or decoy) cannot be implemented until this ADR is Accepted, since `IInteractable` registration is the shared entry point every one of them uses |
| **Ordering Note** | This ADR formalizes a decision `architecture.md`'s Phase 1 (System Layer Map) already made — relocating `InteractableRegistry` from Core (originally owned by Etkileşim Sistemi) to Foundation, to resolve a Foundation→Core layer-ordering violation (`birinci-sahis-kontrolcu.md`'s approach-slow-taper needed to read it). This ADR does not reopen that relocation decision, only formalizes its concrete implementation. |

## Context

### Problem Statement

`etkilesim-sistemi.md` already fully specifies the `IInteractable` interface's member list and behavioral contract (`Type`, `HoldDuration`, `CanInteract`, `PromptText`, `SuppressDefaultHoldFill`, `OnFocusEnter`/`OnFocusExit`/`OnInteract`/`OnHoldProgress`/`OnHoldComplete`/`OnHoldCancelled`/`OnHoldBlocked`) and the registry's read contract (`Register`/`Deregister` from `OnEnable`/`OnDisable`, frame-start read-only snapshot for iteration). What's undecided is the registry's concrete Foundation-layer implementation shape and whether it needs to participate in ADR-0001's `FoundationBootstrap` reset ordering the way the other six Foundation services do.

### Constraints
- `IInteractable`'s member list is already Approved in `etkilesim-sistemi.md` — this ADR must not alter it.
- Registration must happen in `OnEnable`, deregistration in `OnDisable` (locked pattern, matches `birinci-sahis-kontrolcu.md`'s Data Flow discussion of the `Awake`-before-`OnEnable` restore ordering used by `MemoryTriggerObject` and `CarryItemPickup`).
- Iteration must use a frame-start snapshot, never the live collection, to avoid mutate-during-iterate exceptions when an object registers/deregisters mid-frame.
- Real and decoy `IInteractable`s must be structurally indistinguishable through this registry — no field, method, or registration-order guarantee may leak which is which (`birinci-sahis-kontrolcu.md`'s camouflage requirement, `art-bible.md` Section 3.1).
- **(TD-ADR review, 2026-08-05)** `_live`'s self-correction (Decision, below) depends on every `IInteractable` implementer living in a scene that is actually torn down at session end (`OnDisable` firing on all of them) — this holds for all current/planned interactables (depot/ballroom level-scene objects), but would silently stop holding for any future `IInteractable` placed in a persistent, never-unloaded scene (the pattern ADR-0002/ADR-0003 establish for UI/Player). `IInteractable` implementers must not live in a persistent scene — a control-manifest rule, not just a note here.

## Decision

**`InteractableRegistry` is a bare static class wrapping a plain `List<IInteractable>` (not a `HashSet` — insertion order is preserved, useful for deterministic tie-breaking, see Key Interfaces) — no interface/implementation split, and only *partial* `FoundationBootstrap` participation (corrected, TD-ADR review, 2026-08-05 — the live list itself needs no reset hook, but the frame-snapshot cache does; see below).** This is a deliberate, narrower choice than ADR-0001's full pattern for `_live` specifically, justified by a real difference in the underlying problem — but not a blanket "no `FoundationBootstrap` at all" exemption, which an earlier draft of this ADR incorrectly claimed before the unity-specialist review found the cache-staleness bug.

**Why `InteractableRegistry`'s live list does not need `FoundationBootstrap`'s reset machinery**: ADR-0001's reset mechanism exists because Foundation *session-state* services (`FiredTriggerIds` etc.) hold facts that must not survive between distinct Play sessions, and `Awake()`-based initialization is unreliable under Unity's "Reload Scene" Enter Play Mode Setting (confirmed by ADR-0001's own validated finding: `Awake()`/`Start()` don't re-run on surviving objects when Reload Scene is off, but `OnEnable`/`OnDisable`/`OnDestroy` still do). `InteractableRegistry`'s `_live` list never relies on `Awake()` at all — every `IInteractable` registers in `OnEnable` and deregisters in `OnDisable`, both of which Unity guarantees to fire correctly regardless of the Reload Scene/Reload Domain settings. This means `_live` is **self-correcting every session by construction**.

**Correction (unity-specialist validation, 2026-08-05, BLOCKING finding, fixed)**: that reasoning is true of `_live`, but the original draft wrongly extended it to the frame-snapshot **cache** fields (`_snapshotFrame`, `_frameSnapshot`) too — it does not hold for them. `Time.frameCount` resets to 0 at the start of every Play session, but with Domain Reload disabled, `_snapshotFrame`/`_frameSnapshot` (plain static fields, no reset hook) survive carrying their value from the *previous* session. If session 2's `Time.frameCount` happens to pass through the same value `_snapshotFrame` was left holding from session 1 (plausible — consumer call order across sessions is often deterministic, so this isn't a rare coincidence), the cache-validity check `_snapshotFrame != Time.frameCount` evaluates `false` and **returns the stale session-1 array without recomputing** — which can hold references to objects already destroyed at the end of session 1 (`MissingReferenceException` if touched) or simply the wrong logical contents. This is exactly the class of bug ADR-0001's `FoundationBootstrap` exists to prevent, and this ADR's original claim that "no stale fact survives here" was correct about `_live` but false about the cache — **fixed below by giving only the cache fields a minimal, `FoundationBootstrap`-registered reset, while `_live` itself still needs none.**

**Testability**: unlike `IPlayerState` (ADR-0003) or Foundation session-state (ADR-0001), `InteractableRegistry`'s registration API doesn't benefit from an interface/mock split — a test that wants to exercise focus-detection or approach-taper logic against a controlled set of interactables can register/deregister plain test doubles directly against the real static registry (`Register`/`Deregister` are trivial, side-effect-free-elsewhere operations), then deregister them in teardown. No DI indirection is needed for something this simple; adding one would be inconsistent with `coding-standards.md`'s spirit (testable code, not maximal abstraction for its own sake).

### Architecture Diagram

```
Any IInteractable implementer (MemoryTriggerObject, CarryItemPickup, decoy props)
   │
   ├── OnEnable()  → InteractableRegistry.Register(this)
   └── OnDisable() → InteractableRegistry.Deregister(this)

InteractableRegistry (Foundation, static, no reset hook needed)
   │
   └── Snapshot() ── frame-start read-only copy, consumed by:
         • Etkileşim Sistemi (Core) — SphereCast focus-detection candidate set
         • Birinci Şahıs Kontrolcü (Foundation, intra-Foundation read) —
           approach-slow-taper distance calculation (min over all registered
           interactables, per birinci-sahis-kontrolcu.md's formula)
```

### Key Interfaces

```csharp
public static class InteractableRegistry {
    // Self-correcting via OnEnable/OnDisable — no reset hook needed for _live.
    private static readonly List<IInteractable> _live = new();

    // Session-surviving cache — DOES need a reset hook (see below), corrected
    // per unity-specialist validation, 2026-08-05 (BLOCKING finding, fixed).
    private static IInteractable[] _frameSnapshot = Array.Empty<IInteractable>();
    private static int _snapshotFrame = -1;

    public static void Register(IInteractable interactable) => _live.Add(interactable);

    public static void Deregister(IInteractable interactable) => _live.Remove(interactable);

    // Frame-start snapshot: recomputed lazily, at most once per frame, on
    // first Snapshot() call that frame — cheap for the ~1-2 systems that
    // actually call it (Etkileşim's SphereCast scan, FPC's approach-taper),
    // and avoids a snapshot no one asked for on frames neither runs.
    public static IReadOnlyList<IInteractable> Snapshot() {
        if (_snapshotFrame != Time.frameCount) {
            _frameSnapshot = _live.ToArray();
            _snapshotFrame = Time.frameCount;
        }
        return _frameSnapshot;
    }

    // Called by FoundationBootstrap.ResetAll() (see ADR-0001) — clears ONLY
    // the cache fields. _live is deliberately untouched: it's already correct
    // by the time this runs, since every currently-enabled IInteractable's
    // OnEnable has already re-registered it this session.
    internal static void ResetOnLoad() {
        _frameSnapshot = Array.Empty<IInteractable>();
        _snapshotFrame = -1;
    }
}
```

`List<IInteractable>` (not `HashSet<IInteractable>`) is a deliberate choice: insertion order is preserved, which gives Etkileşim Sistemi's `TR-etkilesim-008` tie-break rule ("closest `hit.distance` wins; tie-break by lowest collider `InstanceID`") a stable, order-independent secondary key to fall back on if ever needed, and makes registry contents reproducible/debuggable in a way a hash-ordered collection wouldn't be.

## Alternatives Considered

### Alternative 1: Full ADR-0001-Style Pattern (Interface + Implementation + Static Facade + `FoundationBootstrap` Participation)
- **Description**: Apply ADR-0001's exact shape — `IInteractableRegistryState` interface, a plain `InteractableRegistryState` class, a static facade with `.Instance`, reset via `FoundationBootstrap.ResetAll()`.
- **Pros**: Perfect mechanical consistency with every other Foundation service; a future reader who's learned ADR-0001's pattern recognizes it immediately here too.
- **Cons**: Adds a full interface/implementation/facade split for a registry whose *live membership* is derivable from currently-enabled scene objects at any moment — genuinely unnecessary ceremony for `_live` specifically.
- **Rejection Reason (revised, unity-specialist validation, 2026-08-05)**: this ADR still rejects the *full* pattern (interface + implementation + static facade) for `_live` — that part of the reasoning holds: `_live` truly is self-correcting and doesn't need DI-style test substitution the way session-facts do. But the review found the original blanket rejection went too far: the frame-snapshot **cache** (`_snapshotFrame`/`_frameSnapshot`) is genuinely session-scoped state with exactly the staleness problem ADR-0001's mechanism exists to prevent, and needed a `FoundationBootstrap`-registered `ResetOnLoad()` after all (see Decision, corrected). The final shape is a deliberate middle ground: `FoundationBootstrap` participation for the 2 cache fields only, no interface/facade for anything — narrower than Alternative 1, but not the flat rejection originally argued for either.

### Alternative 2: `HashSet<IInteractable>` Instead of `List<IInteractable>`
- **Description**: Back the registry with a `HashSet` for O(1) `Register`/`Deregister`/contains-checks.
- **Pros**: Marginally faster removal for very large interactable counts.
- **Cons**: Loses insertion-order determinism; this project's interactable counts are small (a handful per scene, per `design/art/art-bible.md` Section 8.11's per-area prop ceilings — Depo's 40-60 props/room is an upper bound on *all* props, not just interactables, and only a fraction are ever `IInteractable`), so the O(1)-vs-O(n) difference is immeasurable at this scale.
- **Rejection Reason**: No performance case exists at this project's actual scale, and `List`'s deterministic ordering is a small, free debugging/tie-breaking benefit `HashSet` would give up for nothing.

## Consequences

### Positive
- Correctly narrower than ADR-0001's pattern where a narrower pattern is the honest answer — this ADR explicitly reasons about *why* it doesn't need `FoundationBootstrap`, rather than either blindly copying ADR-0001 or blindly diverging from it without explanation. Future Foundation ADRs can use this same reasoning (does this data actually need Domain-Reload-safe reset, or does its own lifecycle already self-correct?) as a real decision criterion, not just a rule to follow.
- The lazy, once-per-frame snapshot caching means systems that don't call `Snapshot()` on a given frame pay zero cost for it — no unconditional per-frame `ToArray()` regardless of demand.
- Real/decoy indistinguishability is structurally enforced — the registry has no field or method that could leak which `IInteractable` is "real," satisfying the camouflage requirement by construction, not by discipline.

### Negative
- The lazy-snapshot caching (`_snapshotFrame` check) is a small, easy-to-get-wrong pattern if a future contributor copies this class as a template for something with different consistency requirements (e.g., something that needs the *very first* snapshot of a frame to be authoritative even if computed lazily by a later caller — not a problem for this registry's actual consumers, but worth flagging as a "don't cargo-cult this" note in code comments).
- No DI/interface split (unlike ADR-0001/ADR-0003) means a test wanting a *completely isolated* registry (not sharing state with any other test in the same run) must remember to deregister its test doubles in teardown — this project's `coding-standards.md` Testing Standards already require exactly this discipline ("each test sets up and tears down its own state"), so it's not a new burden, just one this ADR doesn't structurally prevent a lazy test from skipping.
- **(TD-ADR review, 2026-08-05)** The registry is no longer a fully self-contained file: the frame-snapshot cache's `ResetOnLoad()` must stay correctly wired into ADR-0001's `FoundationBootstrap.ResetAll()` ordered call sequence. A future edit to either file (adding/removing a Foundation service, reordering the sequence) needs an author aware of both documents — a small, ongoing cross-file coordination cost this ADR's initial draft didn't have to carry, but the cache-staleness fix does.

### Risks
- **Risk (BLOCKING, unity-specialist validation, 2026-08-05 — found and fixed)**: `_snapshotFrame`/`_frameSnapshot` are plain static fields with no reset hook, but `Time.frameCount` resets to 0 every Play session. With Domain Reload disabled, the cache fields survive into the next session carrying their old value — if the new session's `Time.frameCount` happens to pass through the same number the cache was last set to (plausible, not rare, since consumer call order across sessions is often deterministic), `Snapshot()` returns the **stale previous-session array** without recomputing, potentially containing references to objects already destroyed at the end of the prior session (`MissingReferenceException` if touched). This directly falsified the Decision section's original blanket claim that "no stale fact survives here." **Fixed**: `InteractableRegistry.ResetOnLoad()` (Decision, above) clears only the 2 cache fields, registered in `FoundationBootstrap.ResetAll()` (ADR-0001) — `_live` itself remains correctly un-reset, since it's still genuinely self-correcting.
- **Risk (new, unity-specialist validation; severity reasoning made explicit at TD-ADR review, 2026-08-05)**: `Deregister` (called from `OnDisable`) removes an object from `_live` **synchronously**, but a `Snapshot()` already cached earlier the same frame can still hold that reference — for a `Destroy()`-driven removal this is safe (Unity defers actual destruction to end-of-frame, so the object stays valid through the rest of that frame), but for `SceneManager.UnloadSceneAsync` (this project's actual elevator scene-swap mechanic), `OnDisable`/`OnDestroy` can fire mid-frame relative to when a system consumes that frame's already-cached snapshot, which can in principle produce a `MissingReferenceException` if the ordering lands badly. **Why this is genuinely low-risk in practice, not just asserted**: during the elevator ride, `Asansör/Kat-Erişim Sistemi` requests `MovementLockScope.MoveOnly` (`birinci-sahis-kontrolcu.md`/ADR-0003), not `Full` — Look stays free, so Etkileşim Sistemi's `SphereCast` keeps running every frame throughout the scene-swap window. But its range is a hard-locked 2.0m (`etkilesim-sistemi.md`), and the departing scene's interactables are spatially left behind well before that — the player is inside the sealed elevator cabin (`isik-volume-durum-sistemi.md`/`asansor-kat-erisim-sistemi.md`), with no line of sight or proximity to any depot/ballroom interactable during the ride. The unload happening mid-frame is real, but nothing in SphereCast range at that moment is being unloaded, so the race is structurally never hit by the one mechanic that would otherwise trigger it constantly. If a future decoy or memory-trigger object is ever placed near an elevator threshold (close enough to be in range as the doors close), this reasoning should be re-checked — flagged here as the condition that would reopen this risk, not just a generic caveat.
- **Risk**: A test (or, worse, production code) forgets to `Deregister` a temporary/destroyed `IInteractable`, leaking a stale entry into the registry for the rest of the session. **Mitigation**: this is exactly what `OnDisable`'s guarantee protects against for real scene objects (Unity calls `OnDisable` on destroy/scene-unload reliably); the risk is scoped specifically to hand-constructed test doubles or any future code path that registers outside the `OnEnable`/`OnDisable` pattern — a control-manifest rule ("only ever `Register`/`Deregister` from `OnEnable`/`OnDisable`, never ad hoc") closes this, consistent with how the pattern is already described here.
- **Risk**: `Time.frameCount`-based snapshot caching assumes `Snapshot()` is only ever called from the main thread during normal gameplay `Update`-cycle code — if a future system ever called it from a background thread or during an unusual lifecycle window (e.g. `FixedUpdate` mixed with `Update` callers in the same frame expecting different snapshots), the once-per-frame cache could return a stale snapshot to a caller expecting fresher data. **Mitigation**: not a live risk for this project's current 2 known consumers (both `Update`-cycle, main-thread), but worth a code comment warning against `FixedUpdate` use without re-verifying this caching assumption.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `etkilesim-sistemi.md` | `InteractableRegistry`: self-registering `OnEnable`/`OnDisable`, frame-start read-only snapshot, not live collection | Implemented exactly as specified; `Register`/`Deregister`/`Snapshot()` match |
| `birinci-sahis-kontrolcu.md` | Approach-slow-taper reads a shared `InteractableRegistry` for its `d` (minimum distance over all flagged objects) variable — the Foundation→Core layer violation `architecture.md` Phase 1 resolved by relocating this registry to Foundation | `InteractableRegistry` is now a Foundation-owned type FPC can read without an upward layer dependency; this ADR is the formal record of that relocation's concrete shape |
| `birinci-sahis-kontrolcu.md` / `ani-tetikleyici-etkilesim.md` / `art-bible.md` §3.1 | Real and decoy interactables must be structurally indistinguishable | Registry API surface carries no real/decoy distinction anywhere — enforced by the type system, not a runtime check |

## Performance Implications
- **CPU**: Negligible — `Register`/`Deregister` are `List` add/remove operations on a small collection (a handful of interactables per loaded scene); `Snapshot()`'s `ToArray()` is O(n) but n is small and the call is cached per-frame, called by at most 2 systems.
- **Memory**: Negligible — one small `List` plus one cached array, both bounded by the small per-scene interactable count.
- **Load Time**: Negligible — `ResetOnLoad()`'s participation in `FoundationBootstrap.ResetAll()` (ADR-0001) clears two static fields, sub-microsecond cost; corrected from an earlier "no participation at all" claim (see Decision).
- **Network**: N/A.

## Migration Plan
N/A — greenfield.

## Validation Criteria
- A `[Test]` registers 3 test-double `IInteractable`s, calls `Snapshot()`, deregisters one, calls `Snapshot()` again within the "same frame" (mocked/controlled `Time.frameCount` if needed, or documented as an Edit Mode limitation), and confirms the snapshot reflects the cached pre-deregister state until the next frame boundary, then the updated state after.
- A `[UnityTest]` confirms that disabling Reload Scene in Enter Play Mode Settings does not corrupt registry contents across a simulated session boundary — since registration depends only on `OnEnable`/`OnDisable`, not `Awake`, this should pass without any special handling, and this test exists specifically to confirm that assumption empirically rather than leaving it as an unverified architectural claim.
- **(unity-specialist validation, 2026-08-05 — new, closes the BLOCKING finding)** A `[UnityTest]` explicitly simulates the cross-session cache-collision bug: run a first simulated session, call `Snapshot()` at a specific `Time.frameCount`, end the session without a Domain Reload (mirroring Domain-Reload-disabled), start a second simulated session, drive `Time.frameCount` back to the same value the first session's cache was left at, and confirm `Snapshot()` returns the *current* session's `_live` contents, not the stale cached array — this is the concrete, automatable proof `ResetOnLoad()`'s `FoundationBootstrap` registration actually closes the bug, mirroring ADR-0001's own Validation Criteria pattern for the identical class of hazard.
- A test confirms `Snapshot()` never returns a reference to the live `_live` list itself (mutating the returned array/list must not affect the registry's internal state).

## Related Decisions
- `docs/architecture/architecture.md` — System Layer Map (the original Core→Foundation relocation decision this ADR formalizes), Module Ownership (`InteractableRegistry` row).
- ADR-0001 (In-Memory Static Service Pattern) — `InteractableRegistry.ResetOnLoad()` already appears in ADR-0001's `FoundationBootstrap.ResetAll()` ordered call sequence (as a dependency-free root, alongside `Gece/Oturum Durumu`/`Seviye/Sahne Geçişi`), written there before this ADR was drafted — the two documents are already consistent, no cross-edit needed. This ADR's own initial draft briefly argued for skipping that participation entirely before the unity-specialist review corrected it back to matching ADR-0001's existing ordering — a useful example of a narrower-than-the-pattern decision needing to earn its exception with real reasoning, not just assert one.
- ADR-0003 (Player State and Movement Lock) — `birinci-sahis-kontrolcu.md`'s approach-slow-taper formula, the primary Foundation-layer consumer of this registry, is owned by that ADR's `FirstPersonController`.
