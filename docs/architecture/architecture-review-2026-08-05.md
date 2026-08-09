# Architecture Review Report — Yankılar (Echoes)

**Date:** 2026-08-05
**Engine:** Unity 6.3 LTS (6000.3.0f1)
**GDDs Reviewed:** 12 systems (9 Full GDDs + 3 Quick Specs) + `game-concept.md`
**ADRs Reviewed:** 0

---

## Headline Finding

`docs/architecture/` contains **zero ADRs**. Only an empty `tr-registry.yaml` skeleton
existed prior to this review — no `architecture.md`, no `control-manifest.md`.
Architecture work has not started on this project; it is still entirely in the
design phase. Per `systems-index.md`'s own Next Steps, even the GDD phase has not
been re-verified as converged since the last fix pass on 2026-08-04.

374 technical requirements were extracted across all 12 MVP systems as the
requirements baseline for when ADR authoring begins. See
`docs/architecture/architecture-traceability.md` for the full requirement-level
matrix.

---

## Traceability Summary

| | Count | % |
|---|---|---|
| Total requirements | 374 | 100% |
| ✅ Covered | 0 | 0% |
| ⚠️ Partial | 0 | 0% |
| ❌ Gap | 374 | 100% |

## Coverage Gaps by System

| System (slug) | Layer | TRs | Key Domains | Suggested ADR |
|---|---|---|---|---|
| Birinci Şahıs Kontrolcü (fpc) | Foundation | 34 | Physics, Architecture, Input | ADR: First-Person Movement & Reference-Counted Movement-Lock Architecture |
| Işık/Volume Durum Sistemi (lighting) | Foundation | 49 | Rendering, Architecture, Persistence | ADR: URP Volume-Driven Lighting State Machine |
| Gece/Oturum Durumu (session) | Foundation | 17 | Persistence, Architecture | ADR: Session State Service (in-memory singleton pattern) |
| Anlatı Durum/İpucu Takibi (narrative) | Foundation | 28 | Architecture, Persistence | ADR: Narrative Clue-State Service & Ownership Boundary vs. Session State |
| Seviye/Sahne Geçişi (scene) | Foundation | 39 | Architecture, Rendering | ADR: Additive Scene Transition Manager (SOFT/HARD CUT shared state machine) |
| Adaptif Ses Sistemi (audio) | Foundation | 38 | Audio, Architecture | ADR: AudioMixer Topology & Adaptive Ambience/Stinger Architecture |
| Etkileşim Sistemi (interact) | Core | 30 | Architecture, Physics, UI | ADR: Interaction System (SphereCast focus, IInteractable contract, InteractableRegistry) |
| Asansör/Kat-Erişim Sistemi (elevator) | Core | 30 | Architecture, Gameplay | ADR: Elevator Floor-Access State Machine |
| Diyalog/Anlatı İçeriği (dialogue) | Core | 10 | Architecture, Persistence | ADR: Dialogue Callback-Pool Selection |
| Görev/Taşıma Döngüsü (carry) | Feature | 40 | Architecture, Persistence, Audio | ADR: Task/Carry Loop State & Round Data Model |
| Anı-Tetikleyici Etkileşim (memory) | Feature | 39 | Architecture, Persistence | ADR: Memory-Trigger Event Contract & Edit-Time Validation Pipeline |
| Sahne Kesmeli Anlatı (cutscene) | Feature | 20 | Architecture, Gameplay, Audio | ADR: Night-Ending Orchestration (dual saturation/completion triggers) |
| **Total** | | **374** | | |

Full 374-row requirement-level matrix: `docs/architecture/architecture-traceability.md`.

---

## Cross-ADR Conflicts

None — no ADRs exist to conflict.

**Note for future runs:** this GDD set has an extensive history of cross-document
conflicts (see `design/gdd/gdd-cross-review-2026-08-04-verification.md`, most
recently resolved 2026-08-04). Several of those resolutions created explicit
cross-system contracts (`OnTriggerSettled`, `HardCutConfig.Abrupt`,
`IsShiftPersistent`, `MovementLockScope`) that ADRs will need to encode
faithfully. These are exactly the kind of thing Phase 4 conflict detection will
need to re-check once ADRs exist — this project's dominant failure pattern per
its own review history is "fix lands in the owning doc, doesn't propagate to
consumers."

---

## Recommended ADR Implementation Order

Matches `systems-index.md`'s own dependency layers — restated as an ADR
authoring sequence, not a new ordering conclusion.

### Batch 1 — Foundation (no cross-layer dependencies)
1. Seviye/Sahne Geçişi (scene) — self-contained, everything else builds on it
2. Birinci Şahıs Kontrolcü (fpc) — movement-lock contract everything else calls into
3. Gece/Oturum Durumu (session) — session state everything else reads/writes
4. Anlatı Durum/İpucu Takibi (narrative) — top dependency bottleneck (5 dependents)
5. Işık/Volume Durum Sistemi (lighting) — prototype-validated, high requirement count
6. Adaptif Ses Sistemi (audio) — largest/most complex, depends conceptually on scene+lighting+carry contracts existing first

### Batch 2 — Core (depends on Foundation)
7. Etkileşim Sistemi (interact)
8. Asansör/Kat-Erişim Sistemi (elevator)
9. Diyalog/Anlatı İçeriği (dialogue)

### Batch 3 — Feature (depends on Core)
10. Görev/Taşıma Döngüsü (carry)
11. Sahne Kesmeli Anlatı (cutscene)
12. Anı-Tetikleyici Etkileşim (memory) — deliberately last per the project's own High-Risk Systems note; also gated on the pending audio-paired spike

No dependency cycles or unresolved `Depends On` references — trivially true
with zero ADRs.

---

## GDD Revision Flags

None. Spot-checked the highest-risk GDD assumptions against
`docs/engine-reference/unity/`:

- Lighting system's post-process-only approach (no baked lightmap *sets*) is
  consistent with `game-concept.md`'s separate baked-*GI* note for static
  geometry — these are two different things (static-mesh lightmapping vs.
  per-state lightmap swapping) and don't conflict, despite similar wording.
- `Volume`/`TryGet<T>` API pattern used in the lighting GDD matches
  `modules/rendering.md`'s documented Unity 6+ pattern.
- Input System usage (Action Maps, `WasPressedThisFrame`/`IsPressed`) matches
  `deprecated-apis.md` guidance — legacy `Input.*` correctly avoided everywhere.

---

## Engine Compatibility Issues

No ADRs exist, so 0/0 have Engine Compatibility sections — vacuous by
definition. Two things worth flagging now, before ADR authoring starts, since
they will block those ADRs otherwise:

1. **Unresolved multi-scene RenderGraph risk** — `seviye-sahne-gecisi.md`
   (TR-scene-010) self-flags that Unity 6.3's RenderGraph API changes may
   affect multi-scene camera stacking/lighting and requests a technical spike
   before Detailed Design locks. `docs/engine-reference/unity/modules/rendering.md`
   documents the RenderGraph API generally but has **no multi-scene/camera-stacking
   guidance** — the GDD's own flagged risk is still genuinely open. Recommend
   running that spike (or a `/setup-engine`-style research pass to extend
   `rendering.md`) before the Scene Transition ADR is written, since that ADR
   is Batch 1's #1 priority.

2. **Asset-loading strategy is undefined project-wide** —
   `.claude/docs/technical-preferences.md` names `unity-addressables-specialist`
   as a specialist domain, and `deprecated-apis.md` flags `Resources.Load()` →
   Addressables as a hard migration. None of the 12 GDDs specify an
   asset-loading strategy (prefab pooling in `carry`/`audio` docs assumes
   pre-instantiated pools, sidestepping the question, but nothing says how
   `MemoryTriggerDef`/`ShiftConfig`/`CarryItemDef` ScriptableObjects or the 3
   area scenes themselves get loaded). This isn't a GDD gap — it's correctly
   an architecture-level decision — but it means one of the Batch 1 ADRs
   should explicitly own it rather than it falling through the cracks the way
   `ZoneChanged` ownership did in the GDD history.

---

## Architecture Document Coverage

`docs/architecture/architecture.md` does not exist. Nothing to validate
against. This file should be authored via `/create-architecture` after the
Foundation-layer ADRs above exist — it is downstream of ADRs, not a
substitute for them.

---

## Verdict: FAIL

Per the architecture-review gate rubric, zero Foundation-layer ADRs is a
blocking condition, not an advisory gap. All 6 Foundation-layer systems (and
all 374 requirements across every layer) have no architectural coverage
whatsoever.

### Blocking Issues

- 0 ADRs exist project-wide — no technical requirement in the design has been
  architecturally decided
- No `docs/architecture/architecture.md` — no system-level architecture blueprint
- No `docs/architecture/control-manifest.md` — no programmer rules sheet
- 11/12 GDD systems are still `Needs Revision` per `systems-index.md` (only
  Anlatı Durum/İpucu Takibi is `Approved`), and the project's own tracker
  explicitly notes a full `/review-all-gdds` re-verification pass is still
  outstanding to confirm the 2026-08-04 fixes didn't introduce new propagation
  gaps — writing ADRs against un-reverified GDD text carries real rework risk
  given this project's documented history (4 prior FAIL verdicts, each finding
  fixes that didn't propagate)

### Required ADRs

See "Recommended ADR Implementation Order" above. Top 3 to start immediately:
**Seviye/Sahne Geçişi**, **Birinci Şahıs Kontrolcü**, **Gece/Oturum Durumu** —
these three are read/called by nearly every other system and unblock the rest
of Batch 1.

---

## Pre-Gate Checklist

- ❌ `tests/unit/` and `tests/integration/` — run `/test-setup`
- ❌ `.github/workflows/tests.yml` — run `/test-setup`
- ❌ `design/ux/accessibility-requirements.md` — run `/ux-design`
- ❌ `design/ux/interaction-patterns.md` — run `/ux-design`

`/gate-check pre-production` is not recommended given these gaps plus the FAIL
verdict.

---

## Immediate Actions

1. Consider running a final `/review-all-gdds` re-verification pass to confirm
   the 2026-08-04 fixes converged, before investing in ADR authoring against
   text that might still shift
2. Run `/architecture-decision seviye-sahne-gecisi` (Scene Transition) first —
   Foundation-layer, zero dependencies, blocks Elevator and Cutscene
3. Run `/architecture-decision birinci-sahis-kontrolcu` (First-Person
   Controller) second — movement-lock contract is called into by nearly every
   other system
4. Run `/architecture-decision gece-oturum-durumu` (Session State) third —
   read/written by 5+ other systems
5. Re-run `/architecture-review` after each new ADR to verify coverage improves
