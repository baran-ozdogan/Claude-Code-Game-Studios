# ADR-0002: UI Framework — UI Toolkit

> **Unity UI Specialist Validation**: MINOR (not blocking) 2026-08-05 — 5 notes folded in (shared-document coordination cost, `Scale` Vector3 nit, `GameObject.Find` steer toward ADR-0001's static-facade pattern, Editor hot-reload caveat, accessibility forward-compatibility note); single-shared-`UIDocument` decision independently confirmed as the correct categorization against the specialist's own "one UXML per screen" convention.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-05 — 6 findings, all fixed: (1) art-bible §7 citation scope corrected (crosshair/hold-fill only, not all 4 elements); (2) Engine Compatibility MEDIUM risk reconciled against `architecture.md`'s pre-decision HIGH flag; (3) per-element-`UIDocument` alternative formalized (Alternative 3a); (4) `diyalog-anlati-icerigi` GDD row corrected — source doesn't specify a subtitle UI contract; (5) "C#, never USS timing" rule explicitly scoped to GDD-locked-contract elements, not a blanket rule; (6) shared-UXML "blast radius" coupling risk given a real mitigation (class-name prefixes, defensive queries) beyond "merge-conflict cost," and the `DontDestroyOnLoad` alternative's duplicate-instance argument corrected from "avoids the footgun" to "moves it somewhere more inspectable."

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-ui-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | UI |
| **Knowledge Risk** | MEDIUM — **downgraded from `architecture.md`'s Engine Knowledge Gap Summary, which flagged "UI Toolkit vs. UGUI" HIGH** (TD-ADR review, 2026-08-05: that HIGH flag was assigned pre-decision, when *whether* to adopt UI Toolkit was still an open call with no GDD committed to it yet — mirroring how the RenderGraph HIGH risk in that same summary was separately resolved to "not applicable" once Module Ownership made its call). Now that the framework choice itself is settled (here and in `architecture.md`'s Module Ownership), the residual risk is narrower: UI Toolkit is a post-cutoff-relevant *ecosystem* shift (Unity's own guidance moved from "UGUI default" to "UI Toolkit recommended" after the LLM's May 2025 cutoff), but the core API surface used here (`UIDocument`, `VisualElement`, USS, `root.Q<T>()`) is not itself a breaking change — it is stable, documented API, not new syntax. MEDIUM reflects that narrower residual risk, not a silent disagreement with the earlier HIGH flag. |
| **References Consulted** | `docs/engine-reference/unity/modules/ui.md`, `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/current-best-practices.md` |
| **Post-Cutoff APIs Used** | None that are new/unstable — UI Toolkit's basic runtime API (`UIDocument`, `VisualElement`, `PanelSettings`) predates Unity 6; what changed post-cutoff is Unity's own recommendation to use it for new projects, not the API itself. |
| **Verification Required** | None significant. The one UI Toolkit behavior worth a first-implementation smoke-test (not a knowledge gap, just worth confirming empirically): that `PanelSettings`' reference resolution correctly keeps the crosshair's fixed screen-center position and the thin outline's line weight visually consistent across this project's only target — PC desktop resolutions — since UI Toolkit's scaling model differs from UGUI's Canvas Scaler in ways worth seeing rendered once. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0009 (Audio Architecture — Mixer Groups and Stinger Pooling, owns the stinger caption UI), ADR-0010 (Interaction State Machine, owns the crosshair/Hold-fill ring), ADR-0012 (Dialogue Callback Selection Timing, owns the subtitle UI) — all three cite this ADR's framework choice rather than re-deciding it |
| **Blocks** | Any story implementing the crosshair, Hold-fill ring, stinger caption, or dialogue subtitle — none of the game's 4 UI elements can be implemented before this ADR is Accepted |
| **Ordering Note** | This ADR does not touch state/persistence (ADR-0001's domain) — no sequencing dependency between the two beyond both being early Foundation-tier decisions per `architecture.md`'s Required ADRs list |

## Context

### Problem Statement

This game's entire UI surface is small and fixed: a crosshair/prompt with a Hold-fill progress ring, a stinger closed-caption, and dialogue subtitles. **Citation correction (TD-ADR review, 2026-08-05)**: only the crosshair/Hold-fill cluster's exhaustiveness is confirmed by `design/art/art-bible.md` Section 7 — that section's own "Kapsam notu" scopes "tek bir UI yüzeyi" (one UI surface) to the crosshair, and its four subsections describe "aslında tek bir görsel nesnenin dört yönünü" (four aspects of a single visual object — the crosshair itself), explicitly ruling out inventory/health/minimap/quest-tracker elements but never asserting anything about stinger-caption or dialogue-subtitle scope. Those two elements' existence and requirements come from their own owning GDDs instead — `adaptif-ses-sistemi.md` (stinger caption, owned by Adaptif Ses Sistemi) and the Diyalog/Anlatı İçeriği quick-spec (dialogue subtitles, owned by Diyalog/Anlatı İçeriği; see GDD Requirements Addressed below for the important caveat that this source doesn't yet specify a concrete subtitle UI contract). Together, all 4 elements are still the complete MVP UI surface — just sourced from 3 different documents, not one. `architecture.md`'s Module Ownership phase already chose UI Toolkit over UGUI for this surface, but left the concrete integration shape (document structure, scene lifetime across the elevator's scene swap, and how per-frame visual state is driven) undecided — this ADR is where those choices become binding.

### Constraints
- PC-only target (Steam/Epic) — no mobile/console UI-scaling constraint to also satisfy.
- `.claude/docs/technical-preferences.md`'s ~2000 draw-call / 4GB budgets — 4 small UI elements are negligible against either, this ADR does not need to defend a performance case.
- `design/art/art-bible.md` Section 4.4/7 already locks the crosshair's visual behavior: fully screen-space (never diegetic — Section 7.1), state transitions communicated by opacity/scale only, never color or a flash/burst (Section 3.3/7.4, Pillar 2), plus a thin dark outline (~40-60% opacity, 1-2px, static/non-animated — Section 7.5, added after a UX-alignment contrast finding). This ADR's implementation choices must not make any of these harder to honor.
- The UI must survive the depot↔ballroom elevator scene swap (both the multi-second SOFT transition and the zero-frame HARD CUT) without visibly flickering, resetting, or being destroyed and recreated — `architecture.md`'s Data Flow §1 requires this to be seamless.
- Must not silently reach for `DontDestroyOnLoad` as a reflexive fix — ADR-0001 established a project-wide preference for this project's existing additive-scene-loading machinery over ad hoc persistence patterns, and this ADR should stay consistent with that even though its underlying reason (pure-state reset safety) doesn't literally apply to a rendering GameObject.

### Requirements
- One consistent UI technology for the whole game — no UGUI anywhere, to avoid two parallel UI stacks for a 4-element surface.
- The crosshair's Hold-fill ring must be driven by Etkileşim Sistemi's own already-computed `t` (0 to 1) every frame it's active, with zero animation-curve logic living in USS that could silently diverge from the GDD's "strictly linear, not eased" contract (`etkilesim-sistemi.md` Core Rules, `hold_progress` formula).
- Idle↔Focused crosshair transitions must be a small opacity/scale shift with no CSS transition curve that could reintroduce a "shock" easing Pillar 2 already rejected.

## Decision

**UI Toolkit is the exclusive UI framework for this project — UGUI is not used anywhere, including for any future menu/settings screen.** All 4 current UI elements (crosshair, Hold-fill ring, stinger caption, dialogue subtitle) live under **one shared `UIDocument`**, rooted in **one persistent "UI" scene** that is loaded additively once at game boot (via the same `SceneManager.LoadSceneAsync(Additive)` mechanism `Seviye/Sahne Geçişi` already uses for depot/ballroom) and **never unloaded** for the rest of the session — this reuses the project's existing scene-management pattern instead of introducing `DontDestroyOnLoad` as a second, inconsistent persistence mechanism for a rendering object.

Each owning system (Etkileşim Sistemi, Adaptif Ses Sistemi, Diyalog/Anlatı İçeriği) queries its own named sub-tree from the shared document's root `VisualElement` — ownership of *behavior* (when the crosshair changes state, what the stinger caption says) stays exactly where `architecture.md`'s Module Ownership already assigned it; this ADR only decides the underlying rendering technology and document lifetime, it does not consolidate UI ownership into a new "UI system."

Per-frame/per-state-change visual updates are driven by **direct C# style manipulation** (`element.style.opacity`, `.scale`, a custom fill-width/mask for the Hold-fill ring), never USS `transition`/`:hover`-style declarative animation — the owning system's own code remains the single place that decides timing and curve shape (e.g. Etkileşim's linear `t`, or a shared smoothstep helper for the Idle↔Focused opacity/scale shift).

**Scope of this rule (TD-ADR review, 2026-08-05 — narrowed from an unscoped blanket statement)**: this "C#, never USS timing" rule applies specifically to elements whose animation curve or timing is a **GDD-locked contract** — the Hold-fill ring's strict linearity (`etkilesim-sistemi.md`'s `hold_progress` formula) and the crosshair's "no shock/no easing surprise" requirement (art-bible §7.4, Pillar 2) are exactly that: a divergent `.uss` `transition` curve would silently violate a rule gameplay code is already held to. It is **not** a blanket "never use USS `:hover`/`transition` for anything, ever" project-wide rule — a future settings/options menu (not yet designed, no GDD exists) with ordinary checkboxes/sliders/hover-focus states would have no such locked contract and could legitimately use standard declarative USS styling without revisiting this ADR. Stating this narrower scope now is cheap; leaving the broader reading implicit risks it ossifying into an overly restrictive `/create-control-manifest` rule later.

### Architecture Diagram

```
Boot sequence
   │
   ▼
"UI" scene loaded additively (once, at boot) ──never unloaded, survives every
   │                                            SOFT transition and HARD CUT
   ▼
GameObject "UIRoot" — one UIDocument component,
PanelSettings → single shared root VisualElement (from MainUI.uxml)
   │
   ├── #crosshair-container   ── queried by Etkileşim Sistemi (Core)
   │     ├── #crosshair        (Idle/Focused visual, opacity+scale, outline)
   │     └── #hold-fill-ring   (t-driven fill, linear, outline)
   │
   ├── #stinger-caption        ── queried by Adaptif Ses Sistemi (Foundation)
   │
   └── #dialogue-subtitle      ── queried by Diyalog/Anlatı İçeriği (Core)

Each owning system holds its own VisualElement reference (queried once,
cached — this is a UI element reference, not session state, so ADR-0001's
"never cache across a session boundary" rule doesn't apply here; the UI
scene and its elements are never destroyed/recreated within a session).
```

### Key Interfaces

There is no new "UI system" class — each owning system exposes its UI updates as private implementation detail, consistent with `architecture.md`'s Module Ownership (none of the three owning systems' public APIs changed by this ADR). The only new shared surface is the UXML/USS naming contract every owning system's UI code queries against:

```csharp
// Inside Etkileşim Sistemi (owns this sub-tree; illustrative, not the full class)
private VisualElement _crosshair;
private VisualElement _holdFillRing;

void OnEnable() {
    var root = GameObject.Find("UIRoot").GetComponent<UIDocument>().rootVisualElement;
    _crosshair = root.Q<VisualElement>("crosshair");
    _holdFillRing = root.Q<VisualElement>("hold-fill-ring");
}

void SetCrosshairFocused(bool focused) {
    _crosshair.style.opacity = focused ? 1.0f : 0.65f;   // matches art-bible 4.4's locked opacity values
    _crosshair.style.scale = focused
        ? new StyleScale(new Scale(Vector3.one * 1.1f))   // Scale takes Vector3, not Vector2 — unity-ui-specialist nit
        : new StyleScale(new Scale(Vector3.one));
    // transition timing handled in C# (a short smoothstep lerp over a few
    // hundred ms, per art-bible 7.4), never a USS `transition` declaration
}

void SetHoldProgress(float t) {
    _holdFillRing.style.width = new StyleLength(new Length(t * 100f, LengthUnit.Percent));
    // strictly 1:1 linear per etkilesim-sistemi.md's hold_progress formula —
    // no easing applied here or in USS
}
```

`GameObject.Find` above is illustrative shorthand for "the UI scene's root object" — the exact lookup mechanism is an implementation detail for the owning ADRs (#9, #10, #12) to settle, not this one. **Steer (unity-ui-specialist validation, 2026-08-05)**: that mechanism should follow ADR-0001's static-facade pattern (e.g. a small `UIRoot.Instance` static accessor) rather than `GameObject.Find` or three independently-invented lookup strategies across ADR-9/10/12 — cheap to state now, avoids recreating the exact "three answers to one question" inconsistency this ADR's own Alternative 3b argues against for `DontDestroyOnLoad`.

## Alternatives Considered

### Alternative 1: UGUI (Canvas + TextMeshPro)
- **Description**: Classic Canvas-based UI — a `Canvas` in Screen Space - Overlay mode, `Image`/`TextMeshProUGUI` components for the crosshair/captions, driven by direct component property writes.
- **Pros**: More mature, more widely documented, easier for a solo developer already familiar with it; Canvas Scaler's "Scale With Screen Size" mode is a well-worn, predictable responsive-UI story.
- **Cons**: Unity's own current guidance (`docs/engine-reference/unity/current-best-practices.md`, `docs/engine-reference/unity/VERSION.md`) explicitly recommends UI Toolkit over UGUI for new Unity 6 projects; retained-mode UI Toolkit is faster for the kind of per-frame style mutation this project's crosshair does (`docs/engine-reference/unity/modules/ui.md`'s own Performance section); introduces a second UI stack if any future menu/settings screen (not yet designed) reaches for UI Toolkit later anyway, per the project's own forward-looking convention.
- **Rejection Reason**: No concrete requirement favors UGUI here, and the project has no existing UGUI investment to preserve (greenfield) — the forward-compatible, Unity-recommended choice wins with no real cost, consistent with `architecture.md`'s original Module Ownership decision.

### Alternative 2: IMGUI
- **Description**: Unity's immediate-mode GUI system (`OnGUI()`), typically reserved for Editor tooling and debug overlays.
- **Pros**: Zero setup, trivial for a debug overlay.
- **Cons**: Explicitly deprecated for runtime UI per Unity's own current guidance; immediate-mode redraw cost and lack of any real styling story make it unsuitable for a shipped game UI, even a small one.
- **Rejection Reason**: Not a serious contender for player-facing UI — included only for completeness.

### Alternative 3a: Per-Element `UIDocument` (one document/UXML per element, instead of one shared)
- **Description**: Each of the 4 UI elements gets its own `UIDocument`/UXML/USS, queried and owned entirely independently by its owning system — no shared file any two systems both edit.
- **Pros**: Zero cross-system coupling at the document level — one system's malformed markup or a USS class-name collision can never affect another system's rendering; matches unity-ui-specialist's own stated default convention most literally ("one UXML file per screen/panel").
- **Cons**: 4 documents (plus 4 `UIDocument` components/`PanelSettings` references) for what is, in total screen real estate and complexity, a single small HUD overlay — disproportionate ceremony; loses the ability to reason about the whole always-on UI surface as one visual composition; four separate boot-time load points instead of one.
- **Rejection Reason (formalized, TD-ADR review 2026-08-05 — previously argued only in Decision prose)**: the deciding question is whether the 4 elements are independently-navigable "screens" (unity-ui-specialist's convention target) or sub-parts of one non-modal, always-on overlay with no independent navigation lifecycle — confirmed the latter by both the art-bible's own framing (all of §7 treats the crosshair cluster as one visual object) and by there being no screen-stack/push-pop concept anywhere in this project's UI requirements. Given that, the single-document approach's real cost is narrower than "loses isolation" — it's specifically a **shared-file coupling risk** (see Consequences → Negative, "UXML blast radius"), which gets a targeted mitigation there rather than requiring the heavier 4-document split.

### Alternative 3b: `DontDestroyOnLoad` for the UI GameObject (considered as part of the scene-lifetime question, not the framework question)
- **Description**: Instead of a persistent additively-loaded "UI" scene, mark the `UIRoot` GameObject `DontDestroyOnLoad` so it survives scene swaps by being explicitly exempted from unloading.
- **Pros**: Simpler to set up than a dedicated scene — one line (`DontDestroyOnLoad(gameObject)`) instead of adding a scene to the boot sequence.
- **Cons**: Introduces a second, inconsistent persistence mechanism into a project that already has a working one (additive scene loading, used by every other cross-scene-surviving concern in `architecture.md`); `DontDestroyOnLoad`'s classic "accidental duplicate instance" footgun (ADR-0001, Alternative 1) applies here too if the UI scene/object is ever accidentally included in the depot or ballroom scene by mistake.
- **Rejection Reason**: The project already has a clean answer to "how does X survive a scene swap" (additive load, never unload) — reusing it here is both simpler to explain to a future reader and avoids introducing a second pattern for the same underlying problem. Not rejected because `DontDestroyOnLoad` is inherently wrong (ADR-0001 rejected it for a *different*, state-specific reason), but because this project doesn't need two answers to the same question. **Honesty correction (TD-ADR review, 2026-08-05)**: the chosen persistent-scene approach does not actually *eliminate* the duplicate-instance failure class — this ADR's own Risks section (below) describes an analogous case, the UI scene accidentally loading additively *inside* depot/ballroom instead of independently at boot. The real, defensible advantage isn't "avoids the footgun," it's that the persistent-scene approach **moves the footgun somewhere more inspectable** — a scene reference visible in build settings/Hierarchy is easier to catch in review than an object that silently survives via a `DontDestroyOnLoad(gameObject)` call buried in a script.

## Consequences

### Positive
- One UI technology, one document, one scene — the smallest possible footprint for a genuinely tiny UI surface; no framework-mixing complexity for a future contributor to untangle.
- Reuses the project's existing additive-scene-persistence pattern rather than introducing `DontDestroyOnLoad` as a second one, keeping `architecture.md`'s "how does state/objects survive a scene swap" story consistent across the whole codebase.
- Keeping all animation timing in C# rather than USS `transition` declarations means the art bible's Pillar-2-driven "no shock, no easing surprises" rules are enforced in the same code that's already reviewed for gameplay correctness — a designer/reviewer never has to check two places (C# and a `.uss` file) to know how a state change actually animates.
- Matches Unity's own current-best-practice guidance with zero countervailing cost, since this is a greenfield project with no legacy UGUI investment.

### Negative
- UI Toolkit has a smaller body of community tutorials/Stack Overflow answers than UGUI as of this project's training-data cutoff — a genuine, if minor, velocity cost for a solo/small-team developer who may need to look up unfamiliar UI Toolkit patterns more often than familiar UGUI ones.
- A dedicated persistent "UI" scene is one more scene in the build settings / boot sequence to keep correctly configured (must always load, must never accidentally be included as a dependency of depot/ballroom) — a small, permanent piece of project-setup discipline that `DontDestroyOnLoad` would have avoided, accepted here for the consistency benefit above.
- **(unity-ui-specialist validation, 2026-08-05; sharpened at TD-ADR review)** A single shared UXML/USS file is one file all 3 owning systems' work will touch — and this is a **deeper risk than mere merge-conflict friction**: UXML parsing is document-atomic, not sub-tree-atomic. A malformed edit to one owning system's markup (e.g. Diyalog's `#dialogue-subtitle`), or a USS class-name collision between two systems (UI Toolkit does not namespace classes per sub-tree owner), can break the whole document's parse/layout — including the *other* two systems' already-working sub-trees, not just the editing system's own piece. **Mitigation**: (1) a per-owner USS class-name prefix convention (e.g. `etkilesim-*`, `ses-*`, `diyalog-*`) to eliminate silent collisions; (2) each owning system's sub-tree is queried defensively (`root.Q<VisualElement>("...")` null-checked, not assumed present) so a malformed edit elsewhere degrades to "my element is missing" rather than an unhandled exception; a build-time UXML-validity check (mirroring the `IPreprocessBuildWithReport` pattern already used elsewhere in this project, e.g. `ani-tetikleyici-etkilesim.md`'s edit-time validation) is a candidate for one of ADR-9/10/12 to adopt if this proves to be a recurring real-world problem, not mandated here. Accepted given the alternative (3 separate documents for 4 elements total, see Alternative 3a) is disproportionate ceremony for this project's genuinely tiny UI surface — see Alternatives Considered for the formal trade-off.

### Risks
- **Risk**: A future contributor adds a UGUI `Canvas` for a new menu/settings screen out of familiarity, silently violating the "UI Toolkit exclusively" rule. **Mitigation**: `/create-control-manifest` should carry an explicit forbidden-pattern rule (also registered in `docs/registry/architecture.yaml`, see below) — no `Canvas`/UGUI component anywhere in the project.
- **Risk**: The persistent UI scene is accidentally set to load additively *inside* depot or ballroom (instead of independently at boot), creating either a duplicate `UIRoot` or an unintended dependency between the UI scene and a specific gameplay scene. **Mitigation**: the UI scene's load call belongs in the game's boot/bootstrap sequence (not in `Seviye/Sahne Geçişi`'s depot/ballroom transition logic), and should be covered by the smoke-test in Validation Criteria below.
- **Risk (unity-ui-specialist validation)**: `PanelSettings` is a project asset (`ScriptableObject`), not a scene object, so depot/ballroom's additive load/unload cycle cannot disturb it — confirmed no cross-scene `PanelSettings` gotcha exists. The one adjacent, lower-severity caveat: in the **Editor**, live UXML/USS hot-reload during Play-mode iteration recreates the visual tree, which would invalidate the "queried once in `OnEnable`, cached" `VisualElement` references each owning system holds. Editor-workflow nuance only, not a runtime/production risk — already effectively covered by this ADR's own Validation Criteria smoke test, which runs at runtime.
- **Risk (unity-ui-specialist validation)**: none of the 4 current elements are interactive (no buttons, no navigation), so there is no keyboard/gamepad focus-management or `EventSystem` risk to account for at this MVP scope. This is a scope observation, not a foreclosure — the shared UXML/USS structure does not prevent adding per-element USS variables (text scaling, background opacity) later, when `design/ux/accessibility-requirements.md` is written and ADR-9/ADR-12 define the stinger-caption/subtitle accessibility contracts art-bible §7.5's Open Questions already defer to that not-yet-written document.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `etkilesim-sistemi.md` | Crosshair/prompt UI + default Hold-fill indicator, driven directly from the system's own computed `t`, strictly linear (not eased) | `SetHoldProgress(t)` maps 1:1 to `style.width`, no USS transition/easing applied |
| `design/art/art-bible.md` §3.3/7.1 | Crosshair is the one Pillar-1 exemption — must be 100% screen-space, never diegetic, never enter the world's shape/light language | UI Toolkit's `UIDocument`/`PanelSettings` is inherently screen-space overlay rendering — no 3D-world placement is possible with this setup, structurally enforcing the exemption |
| `design/art/art-bible.md` §7.4 | Idle→Focused is a small opacity/scale shift, never a flash/burst; Hold-fill ring is strictly linear | C# `style.opacity`/`style.scale` writes with a short smoothstep lerp (opacity/scale only) and linear Hold-fill, matching both rules exactly |
| `adaptif-ses-sistemi.md` | Stinger requires a dedicated non-diegetic closed-caption UI element (1-1.5s window synced to playback) | Owned by `#stinger-caption` sub-tree under the same shared `UIDocument`; concrete timing/text contract deferred to ADR-0009 |
| `diyalog-anlati-icerigi-2026-08-02.md` | **Correction (TD-ADR review, 2026-08-05)**: this source document is entirely about callback *selection* logic (which callback fires, consistency/build-time checks, pool exhaustion) — it does not itself specify a subtitle UI display contract (no timing, no visual spec). Dialogue text obviously needs to be shown somehow, but that requirement is implied, not sourced from this document. | Owned by `#dialogue-subtitle` sub-tree once a real contract exists; concrete contract (timing, style, whether it's tied to the same accessibility work as the stinger caption) deferred to ADR-0012, not yet specified anywhere |

## Performance Implications
- **CPU**: Negligible — UI Toolkit's retained-mode model only re-lays-out changed elements, and this project's entire UI surface is 4 small elements updated on state-change or once per frame at most (Hold-fill ring, only while a Hold is active). Well inside the 16.6ms frame budget with enormous headroom.
- **Memory**: Negligible — one small UXML/USS pair, no texture atlases or complex visual trees.
- **Load Time**: One additional scene loaded once at boot (the persistent UI scene) — sub-frame cost, invisible against any other boot-time cost.
- **Network**: N/A.

## Migration Plan
N/A — greenfield, no existing UI code in the project.

## Validation Criteria
- A manual (or `[UnityTest]`-automated) smoke test confirms the UI scene loads once at boot, survives both a SOFT transition (depot→ballroom) and a HARD CUT (the psychiatry-scene interrupt) without the crosshair/caption/subtitle elements flickering, resetting their current state, or being destroyed and recreated.
- A `/create-control-manifest` rule (or a lightweight project-wide grep check in CI) confirms zero `UnityEngine.UI`/`Canvas`/`TextMeshProUGUI` references anywhere in the codebase — the "UI Toolkit exclusively" rule is enforceable, not just aspirational.
- Visual QA confirms the crosshair's locked opacity/scale/outline values (art-bible §4.4/§7.5) render correctly via this UIDocument/USS setup before this ADR is considered fully validated in practice — a pure architecture read-through can't catch a USS unit-mismatch or `PanelSettings` scaling surprise.

## Related Decisions
- `docs/architecture/architecture.md` — Module Ownership (original UI Toolkit choice, restated and completed here), Section 7 UI/HUD Visual Direction of `design/art/art-bible.md` (the locked visual contract this ADR's implementation must satisfy).
- ADR-0001 (In-Memory Static Service Pattern) — this ADR's rejection of `DontDestroyOnLoad` for the UI GameObject deliberately mirrors ADR-0001's own reasoning style, for a different underlying reason (consistency with existing scene-persistence machinery, not state-reset safety).
- ADR-0009, ADR-0010, ADR-0012 (not yet written) — each will define its own sub-tree's concrete contract (stinger caption timing/text, crosshair Idle/Focused/Hold state machine wiring, subtitle text/timing) on top of the framework and document structure this ADR establishes.
