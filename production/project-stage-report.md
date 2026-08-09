# Project Stage Analysis — Beyond the Line

**Date**: 2026-08-08
**Stage**: Pre-Production (architecture phase complete as of this date; `production/stage.txt` updated from stale "Systems Design" in the same pass)
**Stage Confidence**: PASS — clearly detected (gate-check trail through Technical Setup + engine pinned + `src/` empty + all 15 Required ADRs written)

## Completeness Overview

- **Design: ~95% (MVP scope)** — 13 MVP systems designed (10 full GDDs + 3 quick-specs), 3 rounds of `/review-all-gdds` + 2 verification rounds absorbed; `game-concept.md`, `systems-index.md`, entity cross-review docs, approved `art-bible.md` all present. Gap: `design/ux/` is empty (see Gaps #3); several GDD Status headers still carry stale "Needs Revision" artifacts pending the next clean review round (documented, non-blocking).
- **Architecture: 100% of Required list** — `architecture.md` (layer map, ~140 TR-IDs, data flow, API boundaries) + **all 15 Required ADRs written** (ADR-0001..0015, each through the full unity-specialist + TD-ADR double gate; genuine defects caught and fixed in every single one). `docs/registry/architecture.yaml` carries 8 state-ownership entries, 3 interface contracts, 12 api-decisions, 12 forbidden patterns. Gaps: all ADRs still `Status: Proposed` (see Gaps #2); `tr-registry.yaml` is a skeleton awaiting `/architecture-review` Phase 8; no `control-manifest.md` yet.
- **Code: 0%** — `src/` contains only scaffolding (`.gitkeep`, `CLAUDE.md`). Expected and correct for this stage.
- **Tests: 0%** — `tests/` is empty; `/test-setup` has not been run. The ADRs collectively specify dozens of `[Test]`/`[UnityTest]`/EditMode criteria that will need this scaffold.
- **Prototypes: 4, all archived/documented** — `yankilar-volume-weight-spike` (empirically validated ADR-0005's single-zone case), `yankilar-audio-spike-2026-08-02`, `yankilar-greybox-demo`, `yankilar-lighting-concept`.
- **Production: on track for this stage** — gate-checks for Concept→Systems Design (2026-08-01) and Systems Design→Technical Setup (2026-08-02) on file; `session-state/active.md` maintained as the living checkpoint; review-mode pinned to `full`. No sprints/epics/stories yet (correct — those follow the architecture review).

## Roadmap Position

```
Concept ──► Systems Design ──► Technical Setup ──► PRE-PRODUCTION ──► Production ──► Polish ──► Release
  ✅              ✅                  ✅            ◄── HERE
                                                    architecture: DONE (15/15 ADRs, 2026-08-08)
                                                    next: /architecture-review (fresh session)
```

## What Has Been Done (chronological)

1. **Concept phase** — `/brainstorm` → `game-concept.md` with pillars; gate passed 2026-08-01.
2. **Systems design** — `/map-systems` → 34-row systems index; 13 MVP systems authored; three `/review-all-gdds` rounds plus two full re-verification rounds; entity-consistency registry; gate passed 2026-08-02.
3. **Technical setup** — Unity 6.3 LTS (6000.3.0f1) pinned; engine-reference library populated (VERSION, modules, deprecated-apis, breaking-changes, current-best-practices).
4. **Art Bible** — authored and approved (asset production gate open).
5. **Prototyping** — 4 spikes, including the Volume-weight spike that empirically de-risked the project's highest-risk rendering decision before ADR-0005 locked it.
6. **Master architecture** — `/create-architecture` → `architecture.md`: 5-layer system map, both cross-layer violations resolved (InteractableRegistry relocation, round-counter relocation), ~140 TR-IDs extracted, 15 Required ADRs enumerated.
7. **All 15 Required ADRs** (2026-08-05 → 2026-08-08) — every one through the unity-specialist + TD-ADR double review gate. Highlights of caught-before-code defects: a compile-breaking delegate cast (ADR-0008), a `ResetAll()` ordering bug inside ADR-0001's own fix (ADR-0006), a per-`AudioSource`-vs-per-`shiftId` cooldown contradiction (ADR-0009), the `PreloadHardCut` identity-filter hole (ADR-0012), a co-residency double-tick (ADR-0011), an infinite phantom re-Hold through ADR-0010's focus gate (ADR-0014), a session-2-dead-zone lifecycle asymmetry plus the full in-place-reset regime conversion (ADR-0015). Cross-file sync passes kept GDDs/architecture.md/registry consistent throughout; QQ-03/05/06/07 all resolved.

## Gaps Identified

1. **`stage.txt` was stale** ("Systems Design") — **RESOLVED in this pass**: updated to `Pre-Production`.
2. **All 15 ADRs are `Status: Proposed`** — stories referencing Proposed ADRs are auto-blocked by `/story-readiness`. Acceptance should happen as a batch after `/architecture-review` returns clean, not before.
3. **`design/ux/` is empty** — the GDDs themselves demand pre-epic UX artifacts: `carry-slot-indicator` spec (Görev/Taşıma's 📌 UX Flag) and `accessibility-requirements.md` (two seeds on file: Adaptif Ses's stinger-caption question, the numeric slot fallback). Run `/ux-design` before `/create-epics`.
4. **`tests/` scaffold missing** — `/test-setup` (Technical Setup-phase item) was never run; required before any `/dev-story` can produce its BLOCKING test evidence.
5. **`tr-registry.yaml` skeleton** — populated by `/architecture-review` Phase 8; blocked on running that review.
6. **No `control-manifest.md`** — `/create-control-manifest` is the designated home for QQ-03's convention-enforcement rules and the several "control-manifest candidate" rules ADRs flagged (ADR-0010's same-GameObject collider rule, ADR-0013's DropOffZone rule, shared `IPreprocessBuildWithReport` utility checks).
7. **Asset production not started** — art bible is approved; GDD Visual/Audio sections carry 📌 Asset Spec markers; `/asset-spec` per system when ready.

## Recommended Next Steps (priority order)

1. **`/architecture-review` — in a FRESH session** (never the ADR-authoring session; reviewer independence). Validates the 15 ADRs against all ~140 TR-IDs, populates `tr-registry.yaml`, produces PASS/CONCERNS/FAIL.
2. **Batch-accept the ADRs** once the review is clean (`Proposed` → `Accepted`), unblocking story authoring.
3. **`/create-control-manifest`** — flatten Accepted ADRs into the programmer rules sheet.
4. **`/ux-design`** — carry-slot indicator + `accessibility-requirements.md` (its two seeds are already documented in the GDDs).
5. **`/test-setup`** — scaffold `tests/` + CI (game-ci/unity-test-runner) before implementation begins.
6. **`/create-epics` → `/create-stories`** per epic → `/dev-story` implementation loop.
7. **`/asset-spec`** per system, in parallel with early implementation.
8. **Vertical Slice** (`/vertical-slice`) as the Pre-Production → Production gate.

## Longer-Horizon Notes (not current work)

- Vertical Slice-scope designs deliberately deferred: Çoklu Gece İlerlemesi (owns `UsedCallbackIds` cross-night persistence + the night-blind build-check formulas), Ana Menü/Başlangıç Akışı (ADR-0015's boot flow is its minimal MVP placeholder), psychiatrist NPC representation (QQ-04).
- A dedicated Boot Sequence ADR becomes warranted when the start-flow is designed (deferral re-argued and recorded in `architecture.md` by ADR-0015).
- Post-completion, a separate dedicated session is planned for the game's subtext revision pass (recorded in the project's long-term notes; explicitly not part of the MVP pipeline).
