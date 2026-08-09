# Architecture Traceability Index

> **Last Updated**: 2026-08-09 (`/architecture-review` full pass)
> **Engine**: Unity 6.3 LTS (6000.3.0f1)
> **Granularity note**: this index is **system/module-level**, mapping each system's TR-ID *range* (as enumerated in `architecture.md`'s ADR Audit) to its owning ADR. Per-TR rows require `tr-registry.yaml` to be populated first (currently empty — see Known Gaps). Once the registry exists, this index should be regenerated at TR granularity.

## Coverage Summary

- Modules in scope: 13 (12 MVP systems + `InteractableRegistry`)
- ✅ Covered: 13/13 (100% at module level)
- ⚠️ Partial: 2 requirement clusters (stinger caption UI contract; Işık/Volume facade/lookup layer)
- ❌ Gaps: 0 at module level (1 process gap: empty TR registry)

## Module → ADR Matrix

| Module | Layer | TR-ID range (per architecture.md) | Owning ADR | Status |
|---|---|---|---|---|
| In-memory static service pattern (cross-cutting) | Foundation | TR-oturum-001, TR-ani-tetik-002/003, TR-gorev-004/005, TR-anlati-001, TR-ses-008, TR-diyalog-004 | ADR-0001 | ✅ (Accepted 2026-08-09) |
| UI Framework (crosshair, hold-fill, caption, subtitle) | Foundation | TR-etkilesim-006/009, TR-ses-016 | ADR-0002 | ✅ (Accepted 2026-08-09) — TR-ses-016's concrete contract now owned by ADR-0009's 2026-08-09 addendum (Finding T3 closed) |
| Birinci Şahıs Kontrolcü (Player State + Movement Lock) | Foundation | TR-fpc-001..016 | ADR-0003 | ✅ (Accepted 2026-08-09) |
| InteractableRegistry | Foundation | TR-etkilesim-001/002, TR-fpc-004 | ADR-0004 | ✅ (Accepted 2026-08-09) |
| Işık/Volume Durum Sistemi | Foundation | TR-isik-001..021 | ADR-0005 (+ 2026-08-09 facade addendum) | ✅ (Accepted 2026-08-09) — facade/lookup layer pinned by the addendum (`IIsikVolumeState`, zone routing, event forwarding, in-place reset; Finding T4 closed) |
| Gece/Oturum Durumu (Session State + round counters) | Foundation | TR-oturum-001..006 (+2 relocated fields, +TotalConfiguredTriggerCountForNight via ADR-0014) | ADR-0006 | ✅ (Accepted 2026-08-09) |
| Anlatı Durum/İpucu Takibi (Clue Tracking) | Foundation | TR-anlati-001..008 | ADR-0007 | ✅ (Accepted 2026-08-09) |
| Seviye/Sahne Geçişi (Scene Transition) | Foundation | TR-sahne-gecisi-001..014 | ADR-0008 | ✅ (Accepted 2026-08-09) |
| Adaptif Ses Sistemi (Audio) | Foundation | TR-ses-001..015, 017 | ADR-0009 | ✅ (Accepted 2026-08-09) — TR-ses-016 excluded, see ADR-0002 row |
| Etkileşim Sistemi (Interaction) | Core | TR-etkilesim-003..008/010 | ADR-0010 (+ Focused-branch CanInteract re-poll revision via ADR-0014) | ✅ (Accepted 2026-08-09) |
| Asansör/Kat-Erişim Sistemi (Elevator) | Core | TR-asansor-001..008 | ADR-0011 | ✅ (Accepted 2026-08-09) |
| Diyalog/Anlatı İçeriği (Dialogue) | Core | TR-diyalog-001..005 | ADR-0012 | ✅ (Accepted 2026-08-09) — playback timing/advance UX explicitly undesigned (deliberate deferral, not a coverage hole) |
| Görev/Taşıma Döngüsü (Carry Loop) | Feature | TR-gorev-001..018 | ADR-0013 | ✅ (Accepted 2026-08-09) |
| Anı-Tetikleyici Etkileşim (Memory Trigger) | Feature | TR-ani-tetik-001..010 | ADR-0014 | ✅ (Accepted 2026-08-09) |
| Sahne Kesmeli Anlatı (End-Condition) | Feature | TR-sahne-kesme-001..009 | ADR-0015 | ✅ (Accepted 2026-08-09) |

## Known Gaps

| ID | Gap | Status (2026-08-09 follow-up) |
|---|---|---|
| T1 | `tr-registry.yaml` empty — ~140 TR-IDs exist only in narrative text | ✅ CLOSED — registry populated 2026-08-09 (144 entries, version 2; provenance: the 15 Accepted ADRs' GDD-Requirements tables + architecture.md anchors, cited-by-number IDs preserved at their numbers — see the registry's own header note). All blocking-for-PASS items now resolved. |
| T3 | Stinger caption UI contract (TR-ses-016) ownerless | ✅ CLOSED — ADR-0009 addendum (2026-08-09): mechanism/timing owned by `AdaptifSesController`; text+style routed to `/ux-design` |
| T4 | Işık/Volume facade interface undefined | ✅ CLOSED — ADR-0005 addendum (2026-08-09): `IIsikVolumeState` pinned, event forwarding + in-place reset |
| C1 | All 15 ADRs `Proposed` — story pipeline formally auto-blocked | ✅ CLOSED — all 15 flipped to Accepted (2026-08-09, user-approved) |

## Superseded Requirements

None detected this pass — GDD contract changes made during ADR authoring (AC3 slots-full letter, AC1 lock scope, Awake→OnEnable restores) were sync-edited into the owning GDDs (verified), except the partial/unapplied items listed in the review report's GDD Revision Flags table (`ani-tetikleyici-etkilesim.md` ×3 stale `Awake()` mentions; `etkilesim-sistemi.md` Open Questions #1/#2).

## History

| Date | Coverage | Notes |
|---|---|---|
| 2026-08-09 | 13/13 modules (system-level) | Initial index; full review report: `architecture-review-2026-08-09.md` |
