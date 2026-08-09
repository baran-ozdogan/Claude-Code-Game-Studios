# ADR-0001: In-Memory Static Service Pattern for Session-Scoped State

> **Unity Specialist Validation**: MINOR (not blocking) 2026-08-05 — 3 notes folded into Risks/Validation Criteria (Reload Scene interaction, cross-service reset ordering, live-read convention); core `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` mechanism confirmed correct against Unity's own manual.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-05 — 2 findings, both fixed: (1) cross-service reset ordering escalated from "risk, mitigated by convention" to "confirmed live bug in the original draft," fixed by centralizing all resets in a single ordered `FoundationBootstrap.ResetAll()`; (2) added explicit scope clarification that the pattern covers a system's state/event slice, not necessarily its whole public API, for the 2 hybrid consumers (Adaptif Ses Sistemi, Diyalog/Anlatı İçeriği).

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (C# state management / Editor & runtime lifecycle) |
| **Knowledge Risk** | LOW — `RuntimeInitializeOnLoadMethod` and Domain Reload / Enter Play Mode Settings both predate the LLM's May 2025 training cutoff by several years; neither is a Unity 6-era change |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/current-best-practices.md`, `docs/engine-reference/unity/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | **Documentation gap, not a knowledge gap**: this project's `docs/engine-reference/unity/` has no dedicated coverage of Domain Reload / Enter Play Mode Settings behavior — confirmed by grep, zero matches. The mechanism itself is stable, well-documented Unity behavior, not post-cutoff-risky, but the engine-reference library should get a short entry so this doesn't have to be re-derived from scratch for a future ADR. Smoke-test `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` timing once against the actual pinned 6000.3.0f1 editor before relying on it project-wide — low risk, but zero-cost to confirm. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (first ADR) |
| **Enables** | The future "Session State Service and Round-Counter Ownership" (Gece/Oturum Durumu) and "Clue Tracking Architecture" (Anlatı Durum) ADRs — items #6 and #7 in `architecture.md`'s Required ADRs list, not-yet-written and not-yet-numbered as of this writing (**corrected, TD-ADR review on ADR-0004, 2026-08-05**: an earlier draft of this table cited these as "ADR-0004"/"ADR-0007," conflating Required-ADRs-list ordinal position with actual sequential ADR file numbers — ADR-0004 turned out to be "InteractableRegistry Foundation Ownership" instead, since ADRs are written in whatever order the user picks next, not strictly the Required ADRs list's own order) — and every other Foundation/Core/Feature ADR that owns session-scoped state, all of which cite this pattern rather than re-deriving it |
| **Blocks** | Any story implementing `Gece/Oturum Durumu`, `Görev/Taşıma Döngüsü`'s round state, `Anlatı Durum/İpucu Takibi`, `Adaptif Ses Sistemi`'s `HeldSessionAlreadyPlayed`, `Diyalog/Anlatı İçeriği`'s `UsedCallbackIds`, or `Anı-Tetikleyici Etkileşim`'s Committed-state restore — none of these can start until this ADR is Accepted |
| **Ordering Note** | Write and Accept this ADR before any other Foundation-layer ADR — `docs/architecture/architecture.md`'s Required ADRs list already orders it first for exactly this reason |

## Context

### Problem Statement

This game's core mechanic — a night-shift wedding-setup job where a service elevator swaps between two additively-loaded Unity scenes (depot ↔ ballroom) — means several facts must survive a scene load *within one play session*: which memory triggers have fired, whether a shift is permanently held, how many carry rounds are complete, which narrative clues are known, whether a stinger has already played this session, which dialogue callbacks have been used. `docs/architecture/architecture.md` (Phase 3, Data Flow §3) already decided the shape of the fix — "in-memory static/singleton C# services, never `DontDestroyOnLoad` GameObjects or scene-local `MonoBehaviour`s" — but left the concrete mechanism, the Domain Reload reset story, and the testability story unspecified. The LP-FEASIBILITY review on that architecture document (2026-08-05) found both gaps concretely: (1) if a developer has Unity's "Disable Domain Reload" Enter Play Mode Setting turned on (a common indie fast-iteration setting), static fields and static-constructor event subscriptions silently persist across successive Play-mode sessions in the Editor, which could reproduce exactly the class of "phantom already-fired trigger" bug this project's own SOFT-transition co-residency guards were built to prevent, for an unrelated reason; (2) this project's own `.claude/docs/coding-standards.md` makes unit tests **BLOCKING** for state-machine logic and explicitly requires "dependency injection over singletons" — but every Foundation-layer module in the architecture document is exactly the kind of state machine this rule targets, with no reset or substitution mechanism specified.

This ADR is the single source of truth for the concrete pattern every session-scoped-state system in this project implements against.

### Constraints
- No disk persistence anywhere in MVP scope (`game-concept.md`: one continuous night, no save/resume) — this ADR is about surviving a scene load, not an app restart.
- Must work correctly regardless of the developer's local "Reload Domain" Enter Play Mode Setting — cannot rely on a per-developer Editor preference as the actual safety mechanism.
- Must satisfy `coding-standards.md`'s BLOCKING unit-test requirement for state-machine logic, including "dependency injection over singletons."
- Small team/solo project — a full third-party DI framework (VContainer, Zenject) is out of proportion to five Foundation-layer services (see ADR-0008 correction note in Decision for why the count dropped from six); adding one would itself need `Allowed Libraries` approval and its own learning/maintenance cost.
- Must not contradict `architecture.md`'s already-Approved-by-reference-in-GDDs decision that these are plain C# services, not `MonoBehaviour`/`DontDestroyOnLoad` objects.

### Requirements
- State must survive additive scene loads within one Play session (Editor) and one process lifetime (build).
- State must reset cleanly on every genuine session start — both a fresh Domain Reload and, critically, a Play session where Domain Reload is disabled.
- Every static service must be substitutable with a fresh, isolated instance in a `[Test]`/`[UnityTest]`, without touching the production static path.
- The pattern must be simple enough that a solo/small-team project can apply it consistently across all five current consumers (and any future one) without a framework.

## Decision

**Every session-scoped-state module is a plain C# class implementing a small interface, exposed to production code through a static facade property — never a `MonoBehaviour`, never `DontDestroyOnLoad`, never accessed directly as a bag of static fields.** Tests never touch the static facade at all: they construct a fresh instance of the underlying class directly and inject it into the system under test, which is why the interface exists in the first place.

**Note on scope**: "session-scoped-state module" refers to a system's **state/event slice**, not necessarily its entire public API. Two of the five current consumers are hybrids that also own real Unity-object behavior outside this pattern's scope: `Adaptif Ses Sistemi` owns `AudioMixer`/pooled `AudioSource`s and exposes side-effecting methods like `PlayFootstep(speed)` (MonoBehaviour-driven, outside this ADR), while its `HeldSessionAlreadyPlayed` guard follows this pattern; `Diyalog/Anlatı İçeriği` owns `CallbackPool`/UI Toolkit subtitle playback (MonoBehaviour-driven, outside this ADR) alongside its `UsedCallbackIds` bookkeeping, which follows this pattern. Implementers should not try to force an entire hybrid system's behavior into a "no Unity lifecycle" static-facade class — only the state/event slice named in GDD Requirements Addressed below does.

**Reset ordering (revised, TD-ADR review 2026-08-05)**: the original draft gave each of the five services its own independent `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset method. TD-ADR review found this unsound: Unity does not guarantee relative execution order between independent `[RuntimeInitializeOnLoadMethod]` callbacks at the same load stage, and 3 of the 5 services (`Gece/Oturum Durumu`, `Anlatı Durum/İpucu Takibi`, `Adaptif Ses Sistemi`) subscribe to `Işık/Volume Durum Sistemi`'s `OnShiftStateChanged` event **inside their own constructors** — so if a subscriber's reset ran before `Işık/Volume`'s reset, it would bind to the stale, about-to-be-discarded `Işık/Volume` instance and silently miss that event for the rest of the session, reproducing the exact class of bug this ADR exists to prevent. **Fix**: a single `FoundationBootstrap` static class owns the one `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` entry point for the whole Foundation layer, and resets all five services **in explicit dependency order**, reusing the same ordering `architecture.md`'s System Layer Map / Module Ownership already established:

> **Corrected (ADR-0006 / unity-specialist + TD-ADR review, 2026-08-06)**: the ordering below originally reset `GeceOturumDurumu` **first** (before `IsikVolumeDurumSistemi`), directly contradicting this section's own rule two sentences above — `Gece/Oturum Durumu` is one of the constructor-time subscribers to `Işık/Volume`'s event, so resetting it first bound the subscription to the stale, about-to-be-discarded `Işık/Volume` instance, reproducing the exact bug this fix exists to prevent. Caught while drafting ADR-0006 (`Gece/Oturum Durumu`'s own dedicated ADR), by cross-referencing this list against `gece-oturum-durumu-2026-08-02.md`'s and `isik-volume-durum-sistemi.md`'s Dependencies sections, both of which describe the subscription explicitly. Fixed below: `IsikVolumeDurumSistemi` now resets before `GeceOturumDurumu` (it has no constructor-time subscription of its own — its "Persistent-restore" relationship to `Gece/Oturum Durumu` is a separate, per-`ShiftZone`-instance `Awake()`-time concern, not a `ResetOnLoad()`-time read, so it can safely reset early). See ADR-0006's Decision section for the full analysis, including an Alternatives-Considered discussion of a stronger two-phase Construct/Wire fix that was weighed and deferred.

> **Corrected (ADR-0008 / user decision, 2026-08-06)**: `SeviyeSahneGecisi` (Seviye/Sahne Geçişi) is **removed** from this pattern and from `FoundationBootstrap.ResetAll()` entirely. Drafting ADR-0008 ("Scene Transition State Machine") required a genuine timed delay (a 0.5-2s post-`Complete` deferred scene unload) alongside `SceneManager`'s async scene-loading APIs — after weighing the options (a plain static service using Unity 6's undocumented-in-this-project `Awaitable` API vs. a `MonoBehaviour`-hosted implementation), the user chose the more conservative, best-precedented mechanism: `SceneTransitionManager` is a `MonoBehaviour` living in a third persistent scene ("Foundation"), loaded once at boot exactly like ADR-0002's "UI" scene and ADR-0003's "Player" scene — not a plain C# static-facade class. It therefore has no `ResetOnLoad()` to centralize here; like the UI and Player scenes before it, its state resets only via Domain Reload / process restart, driven by its own scene's normal `Awake()` lifecycle, not by `FoundationBootstrap`. This ADR's "six current consumers" framing throughout is corrected to **five** everywhere it appears. See ADR-0008's Decision section for the full reasoning, including the discovered consequence for `Adaptif Ses Sistemi` (a future consumer of `SeviyeSahneGecisi`'s `OnTransitionStateChanged`): any Foundation service subscribing to this MonoBehaviour-hosted event must do so lazily, on first real use, never in its own `FoundationBootstrap.ResetAll()`-time constructor — `SceneTransitionManager.Instance` does not exist yet at that point in the boot sequence (its persistent scene hasn't loaded), unlike every other Foundation service's `Instance`, which is guaranteed constructed by the time `ResetAll()` returns.

```csharp
internal static class FoundationBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll() {
        // Order matches architecture.md's Foundation dependency graph —
        // a service is reset only after every Foundation service it reads
        // from (directly, or via a constructor-time event subscription)
        // has already been reset. Seviye/Sahne Geçişi is NOT here — see
        // the ADR-0008 correction note above; it resets via its own
        // persistent scene's Awake(), not FoundationBootstrap.
        InteractableRegistry.ResetOnLoad();    // no upstream dependency
        IsikVolumeDurumSistemi.ResetOnLoad();  // no upstream Foundation dependency — exposes
                                                // OnShiftStateChanged but subscribes to nothing itself.
                                                // IN-PLACE reset (ADR-0015): it has persistent MonoBehaviour
                                                // subscribers (AdaptifSesController ADR-0009,
                                                // GeceOturumDurumu/AnlatiDurum constructor handlers) —
                                                // replacement would orphan them all
        GeceOturumDurumu.ResetOnLoad();        // subscribes to Işık/Volume's OnShiftStateChanged in its
                                                // own constructor. (ADR-0015, 2026-08-08: under the in-place
                                                // regime this subscription binds ONCE per process and survives
                                                // every reset — the ADR-0006 ordering fix's subscription-binding
                                                // rationale is now vestigial; the order is kept as correct
                                                // data-reset dependency documentation.)
        AnlatiDurumIpucuTakibi.ResetOnLoad();  // subscribes to Işık/Volume's OnShiftStateChanged in its constructor
        AdaptifSesSistemi.ResetOnLoad();       // pure state (HeldSessionAlreadyPlayed only) — no upstream
                                                // Foundation dependency, no subscriptions. All playback
                                                // orchestration (including the OnTransitionStateChanged
                                                // subscription) lives in AdaptifSesController (ADR-0009),
                                                // a MonoBehaviour outside FoundationBootstrap's scope.
                                                // (Corrected, ADR-0009, 2026-08-07 — this comment previously
                                                // claimed the static facade itself subscribes to Işık/Volume
                                                // and reads Gece/Oturum's round counters; both were wrong,
                                                // see ADR-0009's Decision for the full correction.)
        // ── Core/Feature-layer consumers (reconciled 2026-08-08, ADR-0013's
        // TD-ADR review: ADR-0012/ADR-0011 each stated their registration
        // here but this block was never actually edited — all three pending
        // entries added together). Each is dependency-free at reset time
        // (no constructor-time subscription to any Foundation service), so
        // relative order among the three is immaterial; all reset in place
        // where their facades expose events (ADR-0011's
        // wholesale_state_replacement_for_event_exposing_facade rule).
        DiyalogAnlatiIcerigi.ResetOnLoad();    // UsedCallbackIds only (ADR-0012) — in-place Clear()
        ElevatorSystem.ResetOnLoad();          // ride state (ADR-0011) — in-place field reset, events preserved
        GorevTasimaDongusu.ResetOnLoad();      // carry/round state (ADR-0013) — in-place field reset, events
                                                // preserved; SetRoundState lambda resolves
                                                // GeceOturumDurumu.InternalInstance only at invocation time,
                                                // never at construction/reset
        SahneKesmeliAnlati.ResetOnLoad();      // end-condition machine state + NightBeginPending (ADR-0015) —
                                                // in-place; delegates resolve facades at invocation time only
    }
}
```

Each service still exposes its own internal `ResetOnLoad()` (renamed from the original per-service `[RuntimeInitializeOnLoadMethod]`-attributed method — the attribute now lives **only** on `FoundationBootstrap.ResetAll()`), so each service's reset logic stays local to its own file; only the *triggering* and *ordering* is centralized. This directly reuses information the architecture document already derived — no new dependency analysis was needed to fix this.

### Architecture Diagram

```
Production code                         Test code
  │                                        │
  ▼                                        ▼
GeceOturumDurumu.Instance          new GeceOturumDurumuState()
  (static facade property)          (fresh, isolated instance —
  │                                   implements same interface,
  │  returns the current              never touches the static
  │  IGeceOturumDurumuState            facade at all)
  │  default instance
  ▼
IGeceOturumDurumuState  ◄──────── both paths consume the same
  (interface — the real              interface, so production
   contract every consumer            and test code exercise
   codes against)                     identical logic
  │
  ▼
GeceOturumDurumuState : IGeceOturumDurumuState
  (the actual implementation — plain C# class, no MonoBehaviour,
   no GameObject, owns FiredTriggerIds/PersistentShiftIds/etc.)

Reset trigger (production only — centralized, revised per TD-ADR review):
  FoundationBootstrap.ResetAll(), attributed with
  [RuntimeInitializeOnLoadMethod(SubsystemRegistration)],
  calls every service's own ResetOnLoad() in explicit
  dependency order (see Decision, "Reset ordering") —
  fires on: Domain Reload (Editor) AND every subsystem
  registration pass (Editor Play-mode start with Reload
  Domain OFF, and every player process start), AND
  guarantees a subscriber service is never reset before
  the service(s) whose events it subscribes to in its
  own constructor.
```

### Key Interfaces

Generic shape, applied identically to all 5 current consumers (Seviye/Sahne Geçişi excluded — see ADR-0008 correction note above) (their `ResetOnLoad()` methods are called, in dependency order, by the single `FoundationBootstrap.ResetAll()` shown in Decision — no service attributes its own reset method individually):

```csharp
public interface I[System]State {
    // read-only queries and event subscriptions — the real contract
}

public sealed class [System]State : I[System]State {
    // the actual implementation — plain C# class, owns all fields,
    // no Unity object lifecycle of any kind
}

public static class [System] {
    private static I[System]State _instance = new [System]State();
    public static I[System]State Instance => _instance;

    internal static void ResetOnLoad() => _instance.ResetOnLoad();   // IN-PLACE — clear fields on the
                                                                      // SAME instance, never replace it
    // no [RuntimeInitializeOnLoadMethod] here — called by FoundationBootstrap.ResetAll()
    // REVISED by ADR-0015 (2026-08-08): the original generic shape here was
    // `_instance = new [System]State()` (field replacement). Under ADR-0011's
    // wholesale_state_replacement_for_event_exposing_facade forbidden pattern
    // and ADR-0015's regime conversion, every facade that exposes events
    // and/or constructor-subscribes to another facade's events resets IN
    // PLACE — constructor subscriptions then run once per process on
    // never-replaced instances and simply survive ResetAll() (no re-wire,
    // no accumulation, no orphaned subscribers). In-place ResetOnLoad()
    // must explicitly re-initialize any non-default field the replacement
    // shape restored for free (e.g. an initializer-true bool).
}
```

Worked example — `Gece/Oturum Durumu`, chosen because it is the one true Foundation root (no upstream dependency, per `architecture.md`'s dependency diagram) and the pattern's first real consumer:

```csharp
public interface IGeceOturumDurumuState {
    bool IsSessionActive { get; }
    int CurrentRoundIndex { get; }
    int TotalRoundCount { get; }
    bool HasFired(string shiftId);
    bool HasSettled(string shiftId);
    bool IsPersistent(string shiftId);
    void EndSession();                          // Sahne Kesmeli Anlatı only, by convention (see QQ-03)
    event Action<string> OnTriggerFired;
    event Action<string> OnTriggerSettled;
}

public sealed class GeceOturumDurumuState : IGeceOturumDurumuState {
    private readonly HashSet<string> _fired = new();
    private readonly HashSet<string> _settled = new();
    private readonly Dictionary<string, bool> _persistent = new();
    public bool IsSessionActive { get; private set; } = true;
    public int CurrentRoundIndex { get; private set; }
    public int TotalRoundCount { get; private set; }
    public bool HasFired(string shiftId) => _fired.Contains(shiftId);
    public bool HasSettled(string shiftId) => _settled.Contains(shiftId);
    public bool IsPersistent(string shiftId) => _persistent.TryGetValue(shiftId, out var p) && p;
    public void EndSession() => IsSessionActive = false;
    public event Action<string> OnTriggerFired;
    public event Action<string> OnTriggerSettled;
    // internal-visibility write methods (SetRoundState, MarkFired, MarkSettled, etc.)
    // omitted here — full shape belongs to this system's own dedicated ADR
    // (Required ADRs #6, "Session State Service and Round-Counter Ownership")
}

public static class GeceOturumDurumu {
    private static IGeceOturumDurumuState _instance = new GeceOturumDurumuState();
    public static IGeceOturumDurumuState Instance => _instance;

    // Called by FoundationBootstrap.ResetAll() — first in dependency order,
    // since this service has no upstream Foundation dependency.
    internal static void ResetOnLoad() => _instance.ResetOnLoad();   // IN-PLACE (ADR-0015, 2026-08-08 —
                                                                      // previously `= new GeceOturumDurumuState()`;
                                                                      // see the generic-shape revision note above)
}
```

A test never calls `GeceOturumDurumu.Instance`. It does this instead:

```csharp
[Test]
public void SahneKesmeliAnlati_SaturationGate_RequiresAllThreeFlags() {
    var sessionState = new GeceOturumDurumuState();   // fresh, isolated, no static touched
    var sut = new SahneKesmeliAnlatiOrchestrator(sessionState, /* other injected deps */);
    // ... arrange/act/assert against sessionState directly
}
```

## Alternatives Considered

### Alternative 1: `DontDestroyOnLoad` Singleton `MonoBehaviour`
- **Description**: A single persistent GameObject per service, marked `DontDestroyOnLoad`, implementing the classic Unity singleton pattern (`public static X Instance` backed by a `MonoBehaviour` that finds/creates itself in `Awake`).
- **Pros**: Familiar to most Unity developers; visible in the Hierarchy for debugging; can use `MonoBehaviour` lifecycle methods directly.
- **Cons**: Carries real `GameObject`/`Transform`/component overhead for what is pure data and event plumbing; the classic "accidentally spawn a second instance" bug is a recurring Unity footgun, especially across additive scene loads where a duplicate could plausibly be spawned by a second scene's own bootstrap object; testing still requires a live `GameObject` in the test's scene, which is heavier than constructing a plain C# object and doesn't cleanly solve the DI requirement either.
- **Rejection Reason**: `architecture.md` already rejected this shape in Phase 3 for the same core reason restated here — no `GameObject` lifecycle is actually needed for pure state/event data, and this pattern doesn't independently solve either of the two gaps this ADR exists to close.

### Alternative 2: `ScriptableObject` "Runtime Set / Variable" Pattern
- **Description**: Each service becomes a `ScriptableObject` asset instance (the pattern popularized for Unity as "ScriptableObject architecture") — systems hold a serialized reference to the asset instead of a static class reference, and the asset's fields are reset in its own `OnEnable`.
- **Pros**: Genuinely solves injection cleanly (a test can swap in a different asset instance via the Inspector or `ScriptableObject.CreateInstance`); visible/inspectable in the Editor; is a well-known, well-regarded Unity community pattern.
- **Cons**: Has its **own** version of this ADR's Domain Reload problem — a `ScriptableObject` asset's serialized field values persist on disk between Play sessions in the Editor unless explicitly cleared in `OnEnable`/`OnDisable`, so it doesn't eliminate the reset-hook requirement, it just moves it; more invasive change than needed, since this project already has an established, working convention (Architecture Principle #5) that config lives in `ScriptableObject`s (`MemoryTriggerDef`, `CarryItemDef`, `ClueDefinition`) and **runtime state is a separate, distinct kind of object** — introducing `ScriptableObject`-backed runtime state would blur a distinction the project has consistently and deliberately kept clean everywhere else; requires asset-reference wiring (every consuming `MonoBehaviour` needs the asset dragged into an Inspector slot or loaded via Addressables) for something that's conceptually process-global.
- **Rejection Reason**: Doesn't actually eliminate the reset-hook engineering this ADR needs to do anyway, and costs more architectural consistency than it buys — the static-facade approach gets the same testability benefit with a smaller, more targeted change.

### Alternative 3: Full DI Container (VContainer / Zenject)
- **Description**: Adopt a third-party dependency-injection framework; register each service's interface/implementation pair in a composition root, inject via constructor or `[Inject]` attributes throughout the codebase.
- **Pros**: The "correct," industry-standard answer to this exact problem at larger scale; would also solve DI needs this ADR hasn't even considered yet (e.g., any future service added outside this initial five).
- **Cons**: New third-party dependency requiring its own `Allowed Libraries` approval and ongoing maintenance; learning curve and boilerplate (composition roots, lifetime scopes, installer classes) disproportionate to five Foundation services in a solo/small-team MVP; would need its own point of integration with Unity's scene-additive-load flow, which is itself new surface area to get right.
- **Rejection Reason**: Solves a problem this project doesn't have at its current scale. The static-facade-over-an-interface pattern gets ~90% of the testability benefit for a fraction of the adoption cost, and nothing about it forecloses adopting a real DI framework later if the Vertical Slice's added systems (friend NPC, multi-night progression) make the service count large enough to justify it.

## Consequences

### Positive
- Closes both LP-FEASIBILITY gaps directly: Domain Reload is handled by an attribute Unity itself guarantees fires in all three relevant cases, and DI/testability is handled by an interface every test can construct fresh, with zero dependency on the static facade.
- Keeps the change minimal and consistent with every decision already made in `architecture.md` — this ADR formalizes and completes that document's Phase 3 pattern rather than replacing it.
- One pattern, five current consumers, zero special cases among them — every plain-data Foundation service (and any future one) copies the same three-part shape (interface / implementation / static facade with reset hook). Seviye/Sahne Geçişi (ADR-0008) is the one documented exception, for a reason specific to its own async/timed-execution needs, not a weakening of the pattern for the rest.
- Test code never has to know or care whether Domain Reload is on or off in the Editor running the test — it never touches the reset hook at all, by construction.

### Negative
- Slightly more boilerplate per service than a bare static class (interface + implementation + facade, three types instead of one) — an accepted, small cost for solving the testability requirement genuinely rather than by exception.
- The static facade's `Instance` property is still a single shared mutable object *within production code* — this ADR does not eliminate the general risks of mutable global state (e.g. `EndSession()`/`SetRoundState()`'s single-caller convention, tracked separately as QQ-03 in `architecture.md`), it only makes that state swappable for tests.

### Risks
- **Risk**: A developer bypasses the interface and adds a new public field directly to a `...State` implementation class without exposing it through the interface, silently breaking the test-substitutability this ADR exists to guarantee. **Mitigation**: `/create-control-manifest` should carry an explicit rule — "session-scoped state fields are only ever accessed through the system's declared interface, never through a concrete `...State` class reference" — as a code-review-enforced convention, since C# has no compiler mechanism to force this on its own.
- **Risk (unity-specialist validation, 2026-08-05 — confirmed against Unity 6's own manual, not just recalled)**: `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` is Unity's own documented, sanctioned fix for exactly this problem and is confirmed to fire on Domain Reload, on every Play-mode entry with Reload Domain disabled, and on every player process start — the ADR's core claim holds. **But this only resets `GeceOturumDurumu.Instance` itself; it does not, on its own, guarantee every *consumer* re-reads it.** Enter Play Mode Settings has a **second, independent** toggle — "Reload Scene" — and when it's disabled, Unity does **not** call `Awake()`/`Start()` again on objects that survive from the previous Play session (only `OnEnable`/`OnDisable`/`OnDestroy` still fire). This directly affects the "query session state in `Awake()`, before `OnEnable()` registers into `InteractableRegistry`" restore pattern this ADR's GDD Requirements Addressed table cites for `ani-tetikleyici-etkilesim.md` and `gorev-tasima-dongusu.md`: with Reload Scene off, that `Awake()` query simply doesn't re-run on the next session, regardless of whether `GeceOturumDurumu.Instance` is correctly fresh. **Mitigation**: any restore-query that must re-run every Play session belongs in `OnEnable`, not `Awake`, for objects expected to survive a Reload-Scene-disabled session — this is a correction to how *consumers* of this pattern query it, not to this ADR's own reset mechanism, which is unaffected. Flagged forward as `architecture.md` QQ-07 for the specific consumer GDDs to resolve in their own ADRs (Required ADRs #12 "Memory Trigger Orchestration", #13 "Carry Loop and Round State").
- **Risk (unity-specialist validation; escalated and fixed at TD-ADR review, 2026-08-05)**: cross-service reset ordering among independent `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` methods is **not guaranteed by Unity** ("The execution order within each of the `RuntimeInitializeLoadType` callbacks is not guaranteed" — Script Reference). TD-ADR review found this **was** a live bug in the original draft, not just a theoretical one: 3 of the 5 services subscribe to `Işık/Volume`'s `OnShiftStateChanged` inside their own constructors, so an unlucky reset order would silently drop that subscription for the rest of the session — reproducing this ADR's own target bug via a different mechanism. **Fixed** by centralizing all five resets behind one `FoundationBootstrap.ResetAll()` (Decision, "Reset ordering") that calls each service's `ResetOnLoad()` in explicit dependency order — no service attributes its own `[RuntimeInitializeOnLoadMethod]` individually anymore, removing the unguaranteed-order hazard by construction rather than by convention. Any *new* Foundation service added later must be inserted into `FoundationBootstrap.ResetAll()`'s explicit order at the correct point, not given its own independent attribute — stated here as a binding rule for this pattern.
- **Risk (unity-specialist validation)**: a consumer that caches an `I...State` reference (e.g. in a field set during `Awake`) instead of always reading `X.Instance` live would, combined with the Reload-Scene-off gap above, keep listening to an orphaned pre-reset instance and silently miss all events from the canonical new one. **Mitigation**: consumers always dereference `.Instance` live at the point of use; never cache the interface reference across a Play-mode session boundary — this is cheap (a static property read) and removes the hazard entirely.
- **Risk**: `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` execution order relative to other systems' `Awake()`/static-constructor calls is not something this ADR has verified empirically against the pinned 6000.3.0f1 editor — see Verification Required above. **Mitigation**: a short smoke test during the first Foundation-layer implementation story (not this ADR) confirms the reset genuinely completes before any consuming `Awake()` could observe stale state.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `gece-oturum-durumu-2026-08-02.md` | In-memory `FiredTriggerIds`/`PersistentShiftIds`/`SettledTriggerIds` survive scene swap, no disk persistence | Canonical worked example above; concrete field-level shape deferred to this system's own dedicated ADR (Required ADRs #6) |
| `ani-tetikleyici-etkilesim.md` | Committed-state restore must query session state in `Awake()`, before `OnEnable()` registers into `InteractableRegistry` | Interface-based access means the restore query (`GeceOturumDurumu.Instance.HasFired(shiftId)`) is available identically whether or not Domain Reload ran — the ordering guarantee itself is unaffected by this ADR (same-object `Awake`-before-`OnEnable`, per `architecture.md` Data Flow §3), but the *value* returned is now guaranteed correct even across a no-Domain-Reload Play session |
| `gorev-tasima-dongusu.md` | Round/slot state + `HasCarriedInFinalRound` persist across the depot↔ballroom elevator scene swap | Same pattern; `CurrentRoundIndex`/`TotalRoundCount`'s relocated ownership (this session's earlier architecture.md decision) lives inside `GeceOturumDurumuState`, following this ADR's shape |
| `anlati-durum-ipucu-takibi.md` | `KnownClueIds`/`SeenShiftIds` static singleton, subscribes to `OnShiftStateChanged` at first-access time (`TR-anlati-006`) | The static facade's lazy `Instance` property *is* the "first-access time" subscription point this requirement already assumed — this ADR gives it a concrete, testable shape instead of an implicit one |
| `adaptif-ses-sistemi.md` | `HeldSessionAlreadyPlayed` write-once guard against stinger replay on scene reload | Same pattern; the guard survives a scene reload identically to today's design, and is now resettable between test runs without needing a live scene reload to do it |
| `diyalog-anlati-icerigi-2026-08-02.md` | `UsedCallbackIds` bookkeeping (cross-night persistence explicitly out of scope per `architecture.md` QQ-01) | Same pattern for the in-session part of this requirement; QQ-01's cross-night gap is unaffected by this ADR, still deferred to the future Çoklu Gece İlerlemesi work |

## Performance Implications
- **CPU**: Negligible — one extra virtual dispatch per call (interface vs. direct static field access), invisible against this project's 16.6ms frame budget; none of these five services are read more than a handful of times per frame.
- **Memory**: Negligible — five small plain C# objects instead of five static field bags; no `GameObject`/`Transform`/component overhead avoided either way since the rejected alternative (Alternative 1) was never adopted (except for Seviye/Sahne Geçişi, ADR-0008, which deliberately does use a `GameObject`/`MonoBehaviour` for a reason specific to that system — see Decision).
- **Load Time**: Negligible — `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` runs very early in both Editor Play-mode entry and player startup, well before any scene's `Awake()` calls.
- **Network**: N/A (no networking in this project).

## Migration Plan
N/A — greenfield. No Foundation-layer code exists yet; every consumer listed in GDD Requirements Addressed implements this pattern from its first line of code, per the system-specific ADRs still to be written (Required ADRs #6, #7, and the others that reference this one).

## Validation Criteria
- A `[UnityTest]` exists that starts Play mode with Domain Reload explicitly disabled (via `EditorSettings` or a CI-configured Enter Play Mode Settings profile), runs two successive simulated "sessions," and asserts the second session's `GeceOturumDurumu.Instance.HasFired(...)` returns `false` for a trigger that only fired in the first session — this is the concrete, automatable proof the Domain Reload gap is actually closed, not just architecturally described.
- A second `[UnityTest]` (or the same one extended) also disables **Reload Scene** and confirms that any `Awake()`-time restore query this pattern's consumers rely on (`ani-tetikleyici-etkilesim.md`'s Committed-state check, `gorev-tasima-dongusu.md`'s `CollectedItemIds` check) either re-runs correctly or has been moved to `OnEnable` per the Risks mitigation above — added per unity-specialist validation (2026-08-05), not in the original draft.
- A `[Test]` asserts `FoundationBootstrap.ResetAll()`'s hardcoded call order matches the dependency order documented in Decision (InteractableRegistry → Işık/Volume → Gece/Oturum Durumu → Anlatı Durum/İpucu Takibi → Adaptif Ses Sistemi) — a simple ordering assertion, cheap insurance against a future edit silently reintroducing the reset-order hazard TD-ADR review found. Added per TD-ADR review (2026-08-05). **Corrected (ADR-0007 / unity-specialist review, 2026-08-06)**: this bullet still listed the pre-ADR-0006-fix order (Gece/Oturum Durumu first), contradicting the corrected `ResetAll()` code block above in this same file — fixed to match. **Corrected again (ADR-0008, 2026-08-06)**: `Seviye/Sahne Geçişi` removed from this list entirely — it no longer participates in `FoundationBootstrap.ResetAll()` (see Decision's ADR-0008 correction note).
- Every Foundation-layer `[Test]`/`[UnityTest]` for logic covered by `coding-standards.md`'s BLOCKING rule constructs its own `...State` instance directly and never references a `...Instance` static facade — reviewable at code-review time per the control-manifest rule in Consequences → Risks.
- `/architecture-review` (run independently, fresh session, per this skill's own closing notice) confirms no new ADR introduces a session-scoped-state service that bypasses this pattern.

## Related Decisions
- `docs/architecture/architecture.md` — Data Flow §3 (Save/Load Path), Architecture Principle #1; this ADR formalizes both.
- Required ADRs #6 (`Session State Service and Round-Counter Ownership`) and #7 (`Clue Tracking Architecture`) — first concrete consumers, both depend on this ADR being Accepted first.
- `docs/architecture/architecture.md` Open Questions QQ-05 (Domain Reload) and QQ-06 (testability/DI) — both resolved by this ADR.
