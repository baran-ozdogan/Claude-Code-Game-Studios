# Architecture Review Report

> **Date**: 2026-08-09
> **Mode**: `/architecture-review` (full)
> **Engine**: Unity 6.3 LTS (6000.3.0f1)
> **GDDs Reviewed**: 12 MVP system docs (9 Full GDD + 3 Quick Specs), per `design/gdd/systems-index.md`
> **ADRs Reviewed**: 15 (ADR-0001 … ADR-0015 — the complete Required ADRs list from `docs/architecture/architecture.md`)
> **Also reviewed**: `architecture.md`, `tr-registry.yaml`, `docs/registry/architecture.yaml`, engine reference library
> **Engine specialist consultation**: skipped this run — the `unity-specialist` subagent type was not available in the review session. Residual risk is low (every ADR carries a dated in-file unity-specialist validation record plus a TD-ADR review record), but no *fresh-session* independent specialist pass backs this report.

---

## Verdict: CONCERNS

All 15 Required ADRs exist and are internally consistent — no blocking cross-ADR contradiction was found. The iterative correction discipline (later ADRs editing earlier ones: `ResetAll()` ordering, the in-place reset regime, `AddFiredTrigger`, registry updates) has actually landed in the files, including `docs/registry/architecture.yaml`. The CONCERNS verdict rests on four structural gaps listed under Blocking-for-PASS below, none of which is a design contradiction.

---

## Traceability Summary

Coverage is assessed at **system level** (see the Traceability Index note below on why not TR-level):

- Total systems/modules in scope: **13** (12 MVP systems + `InteractableRegistry` as a named module)
- ✅ Covered: **13/13** — every module maps to exactly one owning ADR, mirroring `architecture.md`'s Required ADRs list 1:1
- ⚠️ Partial: **2** requirement clusters (stinger caption UI contract; Işık/Volume system-wide facade/lookup layer)
- ❌ Gaps: **0** at module level; **1 process gap** — the TR registry itself (below)

### Finding T1 — `tr-registry.yaml` is empty (most important traceability finding)

`architecture.md` and the ADRs reference ~140 TR-IDs (`TR-fpc-001..016`, `TR-isik-001..021`, `TR-ses-001..017`, …) but `docs/architecture/tr-registry.yaml` still contains only the template (`requirements: []`). The IDs live only in narrative text. `/create-stories` has no stable IDs to embed, and `/story-readiness` cannot validate TR references. **Action**: run a dedicated TR-extraction pass over all 12 GDDs to populate the registry before story creation begins.

### Finding T2 — `architecture.md`'s ADR Audit section is stale

The document still says "No ADRs exist yet … Count: 0 covered, ~140+ gaps" and its header says "ADRs Referenced: None yet", while all 15 ADRs now exist. Surgical updates were applied elsewhere (QQ-07 resolution, Boot-Sequence deferral note) but the audit table and the Dependency Diagram (missing the Diyalog→UIRoot edge flagged by ADR-0012) were never refreshed.

### Finding T3 — Stinger caption UI contract is ownerless (Partial)

`adaptif-ses-sistemi.md` requires a dedicated non-diegetic closed-caption element (1–1.5s window synced to stinger playback). ADR-0002 deferred the concrete contract to ADR-0009; ADR-0009 did not cover it (per ADR-0010's own note, it was "explicitly out of scope, deferred to a future dialogue/UI pass"). No ADR now owns the `#stinger-caption` timing/text contract. **Action**: small addendum to ADR-0009 (or fold into the UX pass).

### Finding T4 — Işık/Volume facade contract underdefined (Partial)

ADR-0005 deferred the system-wide `shiftId→ShiftZone` lookup layer to implementation and sketched `OnShiftStateChanged` as a **static event on `ShiftZone`**. But ADR-0006/0007/0009 subscribe via `IsikVolumeDurumSistemi.Instance.OnShiftStateChanged` (an **instance event on a facade**), and ADR-0015's in-place-reset conversion treats that facade as holding state — a type ADR-0005 never declares. Not a contradiction, but the one contract four Foundation services build against has no pinned shape. **Action**: short ADR-0005 addendum defining the facade interface with event forwarding (the `ElevatorSystemState` add/remove-accessor shape, ADR-0011).

---

## Cross-ADR Conflicts

**None blocking.** Checked: data ownership (single-writer rules hold — Fired/Persistent/Settled writers reconciled by ADR-0014; round counters single-writer per ADR-0006), integration contracts (LP-FEASIBILITY signature fixes verified against GDD text), performance budgets (no conflicting allocations; all systems negligible against 16.6ms), initialization/reset ordering (`FoundationBootstrap.ResetAll()` reconciled across ADR-0001/0006/0008/0011/0012/0013/0015 — the code block in ADR-0001 now contains all consumers), pattern conflicts (event-vs-call boundaries consistent; the three documented pattern exceptions — ADR-0008's MonoBehaviour hosting, ADR-0004's partial participation, ADR-0003's no-reset accessor — each carry explicit reasoning).

### Finding C1 — All 15 ADRs are still `Proposed` (process blocker)

`docs/CLAUDE.md`: "stories referencing a `Proposed` ADR are auto-blocked"; every ADR's own Blocks field says implementation cannot start until it is Accepted. All 15 have completed both unity-specialist validation and TD-ADR review, so they are content-ready — but the formal acceptance step has never been taken. Until statuses flip, the entire story pipeline is formally blocked. **Action**: deliberate user decision to move ADR-0001…0015 `Proposed → Accepted` (in dependency order, or as one batch given the reviews are complete).

## ADR Dependency Order (topologically sorted — no cycles, no missing dependencies)

```
Foundation roots:      ADR-0001 (static service pattern)
Then:                  ADR-0002 (UI Toolkit) · ADR-0003 (Player State)
Then:                  ADR-0004 (InteractableRegistry, dep 0001) · ADR-0005 (Işık/Volume, dep 0001)
Then:                  ADR-0006 (Session State, dep 0001+0005) → ADR-0007 (Clue Tracking, dep 0001+0005)
Then:                  ADR-0008 (Scene Transition, dep 0002+0003) → ADR-0009 (Audio, dep 0001+0005+0006+0008)
Core:                  ADR-0010 (Interaction, dep 0002+0003+0004)
                       ADR-0011 (Elevator, dep 0001+0003+0006+0008)
                       ADR-0012 (Dialogue Timing, dep 0001+0002+0007+0008+0010)
Feature:               ADR-0013 (Carry Loop, dep 0001/0003/0004/0006/0010/0011)
                       ADR-0014 (Memory Trigger, dep 0004/0005/0006/0010/0013)
                       ADR-0015 (End-Condition, dep 0003/0006/0008/0009/0013/0014) — closes the list
```

All `Depends On` targets exist. The only ordering issue is C1: every dependency is itself `Proposed`.

---

## GDD Revision Flags (Architecture → Design Feedback)

Owed GDD sync edits were verified file-by-file:

| Document | Owed edit | Status |
|---|---|---|
| `gorev-tasima-dongusu.md` | Awake→OnEnable restore; AC3 slots-full letter (ADR-0013) | ✅ applied |
| `gece-oturum-durumu-2026-08-02.md` | Dependencies line Awake→OnEnable (ADR-0014) | ✅ applied |
| `sahne-kesmeli-anlati-2026-08-02.md` | AC1 stale `Full` → `MoveOnly` (ADR-0015) | ✅ applied |
| `ani-tetikleyici-etkilesim.md` | Awake→OnEnable sync (ADR-0014) | ⚠️ **partial** — applied at the States/Core-Rules location (~line 138), but three stale `Awake()` mentions remain: Dependencies (~line 403), an Edge Case (~line 372), and an Acceptance Criterion (~line 633). Same propagation-gap class the project's review history repeatedly documents. |
| `etkilesim-sistemi.md` | Close Open Questions #1 (resolved by ADR-0004) and #2 (resolved by ADR-0010) | ❌ **not applied** — both still read as open; ADR-0010 explicitly recorded owing this edit. |
| `architecture.md` | ADR Audit refresh; Diyalog→UIRoot diagram edge (ADR-0012) | ❌ not applied (Finding T2) |

---

## Engine Compatibility Issues

**Engine**: Unity 6.3 LTS (6000.3.0f1). ADRs with Engine Compatibility section: **15/15**.

- **Version consistency**: all 15 pinned to 6000.3.0f1; no stale version references.
- **Post-Cutoff APIs Used**: none, anywhere. `Awaitable` was considered and rejected (ADR-0008); RenderGraph resolved to not-applicable twice (ADR-0005, ADR-0008).
- **Deprecated APIs**: clean — two violations were caught and fixed during authoring (`Resources.Load` → Addressables, ADR-0007; `FindObjectOfType` → static `Instance`, ADR-0009). No survivors found by this review.
- **Factual corrections propagated**: the solver-iteration error was fixed in both ADR-0003 and `architecture.md`'s Engine Knowledge Gap Summary (consistent).
- **Documentation gap (ADR-0001's own flag, still open)**: `docs/engine-reference/unity/` has no entry for Domain Reload / Enter Play Mode Settings behavior, despite 6+ ADRs re-deriving it. Worth a short reference doc.

---

## Architecture Document Coverage

- Every MVP system in `systems-index.md` appears in `architecture.md`'s layer map; no orphaned architecture modules (the one addition, `InteractableRegistry`, is documented as a deliberate relocation with its own ADR).
- Vertical Slice systems (rows 13–17) correctly excluded with extension points noted.
- Stale sections: see Finding T2. New scene contracts introduced by later ADRs (`UIRoot`, `InitialSpawnAnchor`, `NightConfigDef`, the Foundation persistent scene's three residents) are not yet reflected in Module Ownership — same refresh pass as T2.

---

## Blocking-for-PASS Items

1. **C1** — all ADRs `Proposed`; flip to `Accepted` (user decision).
2. **T1** — populate `tr-registry.yaml` (dedicated TR-extraction pass) before `/create-stories`.
3. **T3** — assign an owner to the stinger caption UI contract.
4. **T4** — pin the Işık/Volume facade interface (ADR-0005 addendum).

## Required Follow-ups (non-blocking)

- Pay the remaining GDD sync debt: `etkilesim-sistemi.md` Open Questions #1/#2; `ani-tetikleyici-etkilesim.md`'s three stale `Awake()` mentions.
- Refresh `architecture.md` (ADR Audit, header, dependency diagram edge, Module Ownership additions).
- Add a Domain Reload / Enter Play Mode Settings entry to the engine reference library.

## Pre-Gate Checklist (all ❌ — `/gate-check pre-production` not yet recommended)

- ❌ `tests/unit/` + `tests/integration/` — run `/test-setup`
- ❌ `.github/workflows/tests.yml` — run `/test-setup`
- ❌ `design/ux/accessibility-requirements.md` — run `/ux-design` (art-bible §7.5 and multiple GDDs defer to this file)
- ❌ `design/ux/interaction-patterns.md` — run `/ux-design`

**Rerun trigger**: re-run `/architecture-review` after the ADR status flip + TR registry population to confirm PASS.

---

## Follow-up applied (same day, 2026-08-09 — user-approved)

| Finding | Action taken |
|---|---|
| C1 (all ADRs `Proposed`) | ✅ All 15 ADRs flipped to **Accepted** with a dated status note |
| T4 (Işık/Volume facade unpinned) | ✅ ADR-0005 addendum: `IIsikVolumeState` interface + `IsikVolumeState` (zone routing table, `RaiseShiftStateChanged`, in-place reset per ADR-0015) + static facade; `ShiftZone`'s static event marked superseded |
| T3 (stinger caption ownerless) | ✅ ADR-0009 addendum: `AdaptifSesController` owns `#stinger-caption` mechanism/timing (UIRoot accessor, `ses-*` prefix, show-on-PlayOneShot / hide-via-EnterCooldownAfter); text+style routed to `/ux-design` per GDD AC 14b |
| GDD sync — `etkilesim-sistemi.md` | ✅ Open Questions #1/#2 marked resolved with pointers to ADR-0004/ADR-0010 |
| GDD sync — `ani-tetikleyici-etkilesim.md` | ✅ All 3 remaining stale `Awake()` mentions (Edge Case, Dependencies, AC10) synced to the `OnEnable()`-top restore shape |
| T2 (`architecture.md` stale) | ✅ Header ADRs-Referenced line, ADR Audit + Traceability Coverage sections refreshed; Diyalog→UIRoot diagram edge added |
| **T1 (`tr-registry.yaml` empty)** | ⏳ **Still open** — requires a dedicated TR-extraction pass over all 12 GDDs; the only remaining blocking-for-PASS item |
| Engine-reference Domain Reload entry | ⏳ Still open (non-blocking) |
| Pre-gate checklist (tests/CI/UX files) | ⏳ Still open — `/test-setup` and `/ux-design` |
