# Systems Index: Yankılar (Echoes)

> **Status**: Draft
> **Created**: 2026-08-01
> **Last Updated**: 2026-08-04 (full parallel-agent `/review-all-gdds` re-verification — see `gdd-cross-review-2026-08-04-verification.md`. Supersedes the same-day manual targeted sweep, which was explicitly flagged as not equivalent to a real verification pass; the earlier `gdd-cross-review-2026-08-04.md` report from the same day is the still-valid prior full pass this one re-verifies against)
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

Yankılar is a psychological horror/drama exploration game built around one
concrete job — carrying wedding-event materials between a hotel's storage
floor and ballroom floor at night — that gradually reveals itself to be a
metaphor for the protagonist's suppressed trauma. The mechanical scope is
deliberately narrow: no combat, no open-world traversal, no procedural
generation. The systems below exist to serve five pillars (Subjective
Reality, Quiet Dread Not Shock, Grounded Labor, Connection Not Safety,
Meaning Deferred) through a tight loop of movement, interaction, environmental
storytelling, and a psychiatrist-scene narrative frame. A concept prototype
already validated the core visual technique (URP Volume-driven lighting
shifts) and found that the compound light+sound effect — not lighting alone —
is what carries Pillar 2.

---

## Systems Enumeration

| # | System Name | Category | Priority | Doc Type | Status | Design Doc | Depends On |
|---|-------------|----------|----------|----------|--------|------------|------------|
| 1 | Birinci Şahıs Kontrolcü (First-Person Controller) | Core | MVP | Full GDD | Needs Revision | design/gdd/birinci-sahis-kontrolcu.md | Etkileşim Sistemi (partial — reads `InteractableRegistry` for the approach-slow-taper formula's `d` variable; a documented Foundation→Core read exception, see the GDD's own Dependencies section and Open Questions #1 in etkilesim-sistemi.md — architecture decision still open; fixed 2026-08-04 full re-verification, this row previously showed "—" despite the GDD documenting the read since 2026-08-04) |
| 2 | Işık/Volume Durum Sistemi (Lighting/Volume State) | Gameplay | MVP | Full GDD | Needs Revision | design/gdd/isik-volume-durum-sistemi.md | Gece/Oturum Durumu (partial), Birinci Şahıs Kontrolcü (partial) |
| 3 | Anlatı Durum/İpucu Takibi (Narrative State/Clue Tracking) | Narrative | MVP | Full GDD | **Approved** (2026-08-02, 2 design-review rounds) | design/gdd/anlati-durum-ipucu-takibi.md | Işık/Volume Durum Sistemi |
| 4 | Gece/Oturum Durumu (Night/Session State) (inferred) | Persistence | MVP | Quick Spec | Needs Revision | design/quick-specs/gece-oturum-durumu-2026-08-02.md | Işık/Volume Durum Sistemi (partial — event subscription, added 2026-08-03; also now calls the read-only `IsShiftPersistent(shiftId)` query, added 2026-08-03 verification N2 fix — no longer "listen-only", but still not a call-based cycle in the problematic sense, see row 2's own note) |
| 5 | Seviye/Sahne Geçişi (Scene Transition) (inferred) | Core | MVP | Full GDD | Needs Revision | design/gdd/seviye-sahne-gecisi.md | — |
| 6 | Adaptif Ses Sistemi (Adaptive Audio System) | Audio | MVP | Full GDD | Needs Revision | design/gdd/adaptif-ses-sistemi.md | Işık/Volume Durum Sistemi, Birinci Şahıs Kontrolcü, Seviye/Sahne Geçişi (added 2026-08-03 — HARD CUT Sting subscription), Görev/Taşıma Döngüsü (added 2026-08-04 — reads `CurrentRoundIndex`/`TotalRoundCount` for round-based tension escalation; a Foundation→Feature read, two layers up — flagged as an open architectural question by the 2026-08-04 full re-verification, same class as row 1's FPC→Etkileşim read but larger, see gdd-cross-review-2026-08-04-verification.md) |
| 7 | Etkileşim Sistemi (Interaction System) (inferred) | Core | MVP | Full GDD | Needs Revision | design/gdd/etkilesim-sistemi.md | Birinci Şahıs Kontrolcü (fixed 2026-08-04 full re-verification: this cell previously listed "Anı-Tetikleyici Etkileşim" as a dependency, inverting Core→Feature layer order — neither `etkilesim-sistemi.md` nor the Dependency Map below ever supported this; the cell had been misused to flag a *contradiction found by review* rather than record a real dependency. Anı-Tetikleyici Etkileşim is correctly a **dependent** of this system, see below and `etkilesim-sistemi.md`'s own Dependencies section) |
| 8 | Asansör/Kat-Erişim Sistemi (Elevator/Floor-Access) | Gameplay | MVP | Full GDD | Needs Revision | design/gdd/asansor-kat-erisim-sistemi.md | Birinci Şahıs Kontrolcü, Seviye/Sahne Geçişi, Gece/Oturum Durumu |
| 9 | Diyalog/Anlatı İçeriği (Dialogue/Narrative Content) (inferred) | Narrative | MVP | Quick Spec | Needs Revision | design/quick-specs/diyalog-anlati-icerigi-2026-08-02.md | Anlatı Durum |
| 10 | Görev/Taşıma Döngüsü (Task/Carry Loop) | Gameplay | MVP | Full GDD | Needs Revision | design/gdd/gorev-tasima-dongusu.md | First-Person Controller, Interaction, Elevator, Night/Session State, Seviye/Sahne Geçişi (soft/indirect — added 2026-08-04 full re-verification, this cell was missing it despite `gorev-tasima-dongusu.md`'s own Dependencies section listing it since 2026-08-02) (previously Approved 2026-08-02; reopened by 2026-08-04 verification, see `gdd-cross-review-2026-08-04.md`). **Dependent** (not a dependency of this row): Adaptif Ses Sistemi reads `CurrentRoundIndex`/`TotalRoundCount` from this system, added 2026-08-04 — see row 6. |
| 11 | Anı-Tetikleyici Etkileşim (Memory-Trigger Interaction) | Gameplay | MVP | Full GDD | Needs Revision | design/gdd/ani-tetikleyici-etkilesim.md | Interaction, Lighting/Volume State (direct API calls), Night/Session State (partial — Committed-state persistence, added design-review 2026-08-02); Narrative State, Adaptive Audio (decoupled — event-driven via Lighting/Volume's `OnShiftStateChanged`, no direct call) |
| 12 | Sahne Kesmeli Anlatı (Cutscene/Scene-Cut Narrative) (inferred) | Narrative | MVP | Quick Spec | Needs Revision | design/quick-specs/sahne-kesmeli-anlati-2026-08-02.md | Scene Transition, Task/Carry Loop, Night/Session State, First-Person Controller (added 2026-08-03 — movement lock) (Dialogue Content, Memory-Trigger Interaction: indirect/event-driven, no direct call). **Narrative State removed 2026-08-03** — saturation signal switched from `OnClueKnown` to Night/Session State's `FiredTriggerIds.Count` |
| 13 | Hibrit Tepkisellik (Hybrid Reactivity) (inferred) | Gameplay | Vertical Slice | Full GDD | Not Started | — | Lighting/Volume State, Memory-Trigger Interaction |
| 14 | Çoklu Gece İlerlemesi (Multi-Night Progression) (inferred) | Persistence | Vertical Slice | Full GDD | Not Started | — | Night/Session State, Task/Carry Loop, Narrative State |
| 15 | Arkadaş Karakteri/NPC (Friend Companion) (inferred) | Narrative | Vertical Slice | Full GDD | Not Started | — | First-Person Controller, Dialogue Content |
| 16 | Ana Menü/Başlangıç Akışı (Main Menu/Start Flow) (inferred) | UI | Vertical Slice | Quick Spec | Not Started | — | Scene Transition, Night/Session State |
| 17 | Plot Twist/Final Sekansı (Ending Sequence) (inferred) | Narrative | Full Vision | Full GDD | Not Started | — | Narrative State, Cutscene System, Multi-Night Progression |

**Doc Type key**: *Full GDD* = all 8 required sections per `.claude/rules/design-docs.md`.
*Quick Spec* = lightweight spec via `/quick-design` — assigned per PR-SCOPE gate
guidance (2026-08-01) to systems that are content/plumbing rather than
rules-heavy mechanics, to protect the MVP's polish buffer.

---

## Categories

| Category | Description | Systems in this game |
|----------|-------------|-----------------|
| **Core** | Foundation systems everything depends on | First-Person Controller, Interaction, Scene Transition |
| **Gameplay** | The systems that make the game fun | Lighting/Volume State, Elevator/Floor-Access, Task/Carry Loop, Memory-Trigger Interaction, Hybrid Reactivity |
| **Narrative** | Story and dialogue delivery | Narrative State/Clue Tracking, Dialogue Content, Cutscene/Scene-Cut, Friend Companion, Ending Sequence |
| **Audio** | Sound and music systems | Adaptive Audio System |
| **Persistence** | Save state and continuity | Night/Session State, Multi-Night Progression |
| **UI** | Player-facing information displays | Main Menu/Start Flow |

*(Progression, Economy, and Meta categories are not used — this game has no
leveling, no economy, and no meta-progression per its design pillars.)*

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function — proves the carry-task loop and the light+sound subjective-reality technique are engaging | First playable prototype | Design FIRST |
| **Vertical Slice** | Required for a complete, polished multi-night experience — hybrid reactivity, the friend relationship, and a real start flow | Vertical slice / demo | Design SECOND |
| **Full Vision** | The ending payoff and any remaining polish | Beta / Release | Design as needed |

*(No systems are Alpha-tier by name — Alpha is existing MVP/Vertical Slice
systems extended across all planned areas/nights, not new systems.)*

> **Creative Director Note (CD-SYSTEMS, 2026-08-01, CONCERNS — accepted)**:
> The MVP-tier system set cleanly delivers the core fantasy for **Pillars 1-3**
> (Subjective Reality, Quiet Dread Not Shock, Grounded Labor) with no scope
> creep and no missing loop systems. However, **Pillar 4** (Bağ, Güvenlik
> Değil) has zero MVP-tier surface — Friend Companion NPC is Vertical-Slice
> only — and **Pillar 5** (Anlam Sona Saklı) is only partially testable, since
> its full payoff requires Ending Sequence (Full Vision). Two actions:
> (1) Everywhere "MVP validates the concept" is claimed in future docs, scope
> it explicitly to "validates Pillars 1-3" — a clean MVP playtest does not
> green-light Pillars 4-5 by proxy.
> (2) Before committing to Vertical Slice, add a cheap non-system probe for
> Pillar 4 (e.g., a single scripted voicemail/text beat from the friend, not
> a full NPC system) to get early signal on the connection-not-safety
> hypothesis before building a whole system against it.

---

## Dependency Map

> **Technical Director note (TD-SYSTEM-BOUNDARY, 2026-08-01, CONCERNS —
> accepted)**: Three boundary issues were flagged and are reflected below:
> (1) Elevator/Floor-Access now correctly depends on Night/Session State —
> the "is the elevator usable right now?" query must be owned by the
> Elevator system itself, not re-implemented inside Task/Carry Loop.
> (2) Narrative State/Clue Tracking vs. Night/Session State need an explicit
> ownership split before GDD authoring: Night/Session State owns
> session/trigger bookkeeping (which triggers fired, which night); Narrative
> State owns story-flag semantics only (which hints/fragments are narratively
> "known"). A trigger firing is both events — each system should own its own
> half, not duplicate the other's data.
> (3) Memory-Trigger Interaction's GDD must define an explicit
> `MemoryTriggerEvent` contract that Lighting/Volume State and Adaptive Audio
> subscribe to, rather than calling into their APIs directly — this avoids a
> God Object as the two open technical questions below resolve.

### Foundation Layer (no cross-layer dependencies)

> **Clarification (2026-08-01, design-review round 2 on Işık/Volume Durum
> Sistemi):** "Foundation Layer" means no dependency reaches into a
> higher layer — it does NOT mean zero dependencies of any kind. A
> Foundation-layer system may depend on another Foundation-layer system
> (intra-layer) without leaving Foundation, since no layer ordering is
> violated. This distinction wasn't previously stated here; added because
> a GDD's Dependencies section cited it without the index defining it.

1. Birinci Şahıs Kontrolcü — every other interactive system needs a player to act through; **partial Foundation→Core read dependency on Etkileşim Sistemi added 2026-08-04 full re-verification** (reads the `InteractableRegistry` Etkileşim owns, for the approach-slow-taper formula's `d` variable — see the Systems Enumeration table row 1 and Foundation-layer-definition note above; this is the one acknowledged layer-ordering exception in the project, architecture decision still open, see `etkilesim-sistemi.md` Open Questions #1)
2. Işık/Volume Durum Sistemi — prototype-validated rendering mechanism; **partial
   intra-Foundation dependencies on Gece/Oturum Durumu (session-load/Persistent-restore)
   and Birinci Şahıs Kontrolcü (max-speed value for its tick-rate guard) — corrected
   2026-08-01, design-review round 2; no longer "self-contained," see its GDD's
   Dependencies section**
3. Anlatı Durum/İpucu Takibi — pure state/flags, no upstream dependency
4. Gece/Oturum Durumu — **partial intra-Foundation dependency on Işık/Volume
   Durum Sistemi added 2026-08-03** (subscribes to `OnShiftStateChanged`
   to populate `PersistentShiftIds`) — see Circular Dependencies note below;
   no longer "pure state/flags, no upstream dependency"
5. Seviye/Sahne Geçişi — self-contained scene-management mechanism
6. Adaptif Ses Sistemi — **partial intra-Foundation dependency on Seviye/Sahne
   Geçişi added 2026-08-03** (subscribes to `OnTransitionStateChanged(newState,
   type)` — filters on `type == TransitionType.Hard` to fire the HARD CUT
   Sting only, `type` param added 2026-08-03 fixing N6) — no longer
   "self-contained". **Cross-layer Foundation→Feature dependency on Görev/Taşıma
   Döngüsü added 2026-08-04** (reads `CurrentRoundIndex`/`TotalRoundCount` for
   round-based tension escalation, `tension_gain` formula) — unlike the two
   intra-Foundation reads above, this one crosses two layers upward; flagged
   as an open architectural question by the 2026-08-04 full re-verification
   (same class as row 1's FPC→Etkileşim read, but larger), not yet resolved

### Core Layer (depends on Foundation)

7. Etkileşim Sistemi — depends on: Birinci Şahıs Kontrolcü
8. Asansör/Kat-Erişim Sistemi — depends on: Birinci Şahıs Kontrolcü, Seviye/Sahne Geçişi, Gece/Oturum Durumu
9. Diyalog/Anlatı İçeriği — depends on: Anlatı Durum/İpucu Takibi

### Feature Layer (depends on Core)

10. Görev/Taşıma Döngüsü — depends on: Birinci Şahıs Kontrolcü, Etkileşim, Asansör/Kat-Erişim, Gece/Oturum Durumu, Seviye/Sahne Geçişi (soft/indirect — added 2026-08-04 full re-verification, was missing here despite being in `gorev-tasima-dongusu.md`'s own Dependencies section since 2026-08-02). **Dependent of this row** (added 2026-08-04): Adaptif Ses Sistemi reads `CurrentRoundIndex`/`TotalRoundCount` for round-based tension escalation — see Foundation Layer item 6 above
11. Anı-Tetikleyici Etkileşim — depends on: Etkileşim, Işık/Volume Durum (direct API calls), Gece/Oturum Durumu (**partial** — Committed-state persistence read/write only, added design-review 2026-08-02, same partial-dependency pattern as Işık/Volume Durum's own Gece/Oturum Durumu dependency); Anlatı Durum, Adaptif Ses (**decoupled** — both already subscribe independently to Işık/Volume's `OnShiftStateChanged`, this system makes no direct call to either — see its own GDD's Dependencies section)
12. Sahne Kesmeli Anlatı — depends on: Seviye/Sahne Geçişi, Diyalog İçeriği, Gece/Oturum Durumu, Görev/Taşıma Döngüsü, Birinci Şahıs Kontrolcü (**added 2026-08-03** — Full-scope movement lock before HARD CUT). **Anlatı Durum/İpucu Takibi removed 2026-08-03** — saturation signal switched from `OnClueKnown` to Gece/Oturum Durumu's `FiredTriggerIds.Count`, this system no longer queries Anlatı Durum at all. **Anı-Tetikleyici Etkileşim corrected 2026-08-04** (verification finding) — this row previously listed it as a direct "same-tier dependency", contradicting the Systems Enumeration table below (row 12, "indirect/event-driven, no direct call"), `sahne-kesmeli-anlati-2026-08-02.md`'s own Dependencies section (which never lists it), and `ani-tetikleyici-etkilesim.md`'s own text ("bu sisteme doğrudan bağımlı değil"). Three of four documents agreed: **no direct dependency** — Sahne Kesmeli reads the saturation signal from Gece/Oturum Durumu's `FiredTriggerIds`/`OnTriggerFired`, decoupled from Anı-Tetikleyici entirely. This row was the outlier and is now corrected to match.

### Presentation/Vertical-Slice Layer (depends on Feature)

13. Hibrit Tepkisellik — depends on: Işık/Volume Durum, Anı-Tetikleyici Etkileşim
14. Çoklu Gece İlerlemesi — depends on: Gece/Oturum Durumu, Görev/Taşıma Döngüsü, Anlatı Durum
15. Arkadaş Karakteri/NPC — depends on: Birinci Şahıs Kontrolcü, Diyalog İçeriği
16. Ana Menü/Başlangıç Akışı — depends on: Seviye/Sahne Geçişi, Gece/Oturum Durumu

### Full Vision Layer (depends on everything)

17. Plot Twist/Final Sekansı — depends on: Anlatı Durum, Sahne Kesmeli Anlatı, Çoklu Gece İlerlemesi

---

## Recommended Design Order

> **Producer note (PR-SCOPE, 2026-08-01, OPTIMISTIC — accepted)**: Do not
> author all MVP GDDs before implementing anything. Work in batches of 2-3
> systems — design, then implement, then move to the next batch — to avoid
> the first-project trap of disappearing into documentation. Cap GDD-writing
> time at roughly 30% of the MVP budget to protect the polish buffer already
> reserved in `game-concept.md`.

| Order | System | Priority | Layer | Doc Type | Est. Effort | Batch |
|-------|--------|----------|-------|----------|-------------|-------|
| 1 | Birinci Şahıs Kontrolcü | MVP | Foundation | Full GDD | S | 1 |
| 2 | Işık/Volume Durum Sistemi | MVP | Foundation | Full GDD | S | 1 |
| 3 | Gece/Oturum Durumu | MVP | Foundation | Quick Spec | S | 1 |
| 4 | Anlatı Durum/İpucu Takibi | MVP | Foundation | Full GDD | M | 1 |
| 5 | Seviye/Sahne Geçişi | MVP | Foundation | Full GDD | S | 1 |
| 6 | Adaptif Ses Sistemi | MVP | Foundation | Full GDD | M | 1 |
| 7 | Etkileşim Sistemi | MVP | Core | Full GDD | S | 2 |
| 8 | Asansör/Kat-Erişim Sistemi | MVP | Core | Full GDD | S | 2 |
| 9 | Diyalog/Anlatı İçeriği | MVP | Core | Quick Spec | S | 2 |
| 10 | Görev/Taşıma Döngüsü | MVP | Feature | Full GDD | M | 3 |
| 11 | Anı-Tetikleyici Etkileşim | MVP | Feature | Full GDD | L | 3 (last — see High-Risk) |
| 12 | Sahne Kesmeli Anlatı | MVP | Feature | Quick Spec | S | 3 |
| 13 | Hibrit Tepkisellik | Vertical Slice | Presentation | Full GDD | M | Stretch |
| 14 | Çoklu Gece İlerlemesi | Vertical Slice | Presentation | Full GDD | M | Stretch |
| 15 | Arkadaş Karakteri/NPC | Vertical Slice | Presentation | Full GDD | M | Stretch |
| 16 | Ana Menü/Başlangıç Akışı | Vertical Slice | Presentation | Quick Spec | S | Stretch |
| 17 | Plot Twist/Final Sekansı | Full Vision | Full Vision | Full GDD | L | Post-timeline |

*(S = 1 session, M = 2-3 sessions, L = 4+ sessions.)*

**Sequencing note**: Anı-Tetikleyici Etkileşim (#11) is deliberately last in
Batch 3. Per the CD-PLAYTEST gate (2026-08-01), its Formulas/Tuning Knobs
sections cannot be finalized until the audio-paired follow-up spike
(`/prototype --spike`) completes and the two open technical questions —
audio middleware choice and lighting-state authoring model at scale — are
resolved (see High-Risk Systems below). Run that spike in parallel with
Batches 1-2.

---

## Circular Dependencies

**Updated 2026-08-03 (`/review-all-gdds` verification)**: One event-decoupled
cycle now exists — Işık/Volume Durum Sistemi reads Gece/Oturum Durumu's
`PersistentShiftIds` (data read), and Gece/Oturum Durumu subscribes to
Işık/Volume's `OnShiftStateChanged` event to populate it (event listen).
This is **not** a circular *call* dependency — Gece/Oturum never calls
into Işık/Volume, it only listens to a broadcast event; the data-read
direction is separate and one-way. Both systems are Foundation-layer, so
no layer ordering is violated either way. Aside from this one
event-decoupled pair, the graph remains a clean call-dependency DAG,
confirmed by TD-SYSTEM-BOUNDARY review.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Anı-Tetikleyici Etkileşim | Technical / Design | Simultaneously drives Lighting, Audio, and Narrative State for one compound effect — God Object risk without a clear contract; also blocked on two undecided technical questions | Define an explicit `MemoryTriggerEvent` contract (TD-SYSTEM-BOUNDARY); sequence GDD last, after the audio-paired spike |
| Adaptif Ses Sistemi | Technical | ~~Audio middleware undecided (Unity built-in vs. FMOD/Wwise)~~ **Resolved 2026-08-02**: Unity built-in AudioMixer + AudioSource confirmed in the system's GDD — no adaptive/branching-music or RTPC needs justify middleware licensing/integration cost | Closed |
| Işık/Volume Durum Sistemi | Technical | ~~Lighting-state authoring model unresolved (post-process-only vs. per-room baked lightmap sets)~~ **Resolved 2026-08-01**: post-process-only confirmed in the system's GDD Open Questions (no baked lightmap sets); `game-concept.md` updated to match | Closed |
| Anlatı Durum/İpucu Takibi | Design / Scope | Top bottleneck (5 dependents); ambiguous boundary with Night/Session State risks it becoming a dumping ground | Write an explicit ownership line in both GDDs before authoring either (TD-SYSTEM-BOUNDARY) |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 17 |
| Design docs started | 12 |
| Design docs reviewed | 9 (12 with `/review-all-gdds`, 2026-08-03; 14 with `/review-all-gdds`, 2026-08-04; 14 with full parallel-agent re-verification, 2026-08-04 — verdict FAIL, see `gdd-cross-review-2026-08-04-verification.md`) |
| Design docs approved | 1 (Anlatı Durum/İpucu Takibi) — Görev/Taşıma Döngüsü reopened to Needs Revision by the 2026-08-04 verification run, see `design/gdd/gdd-cross-review-2026-08-04.md` |
| MVP systems designed | 12/12 (**Batch 1 + Batch 2 + Batch 3 complete**) |
| Vertical Slice systems designed | 0/4 |

---

## Next Steps

- [ ] Review and approve this systems enumeration
- [ ] Run the audio-paired follow-up spike (`/prototype --spike`) in parallel with Batch 1-2 GDD authoring
- [x] Design Batch 1 (Foundation) systems — **complete 2026-08-02** (all 6: Birinci Şahıs Kontrolcü, Işık/Volume Durum Sistemi, Gece/Oturum Durumu, Anlatı Durum/İpucu Takibi, Seviye/Sahne Geçişi, Adaptif Ses Sistemi)
- [x] Batch 2 — **complete 2026-08-02** (Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Diyalog/Anlatı İçeriği)
- [x] Görev/Taşıma Döngüsü — **complete 2026-08-02** (Full GDD, CD-GDD-ALIGN: CONCERNS accepted; UX Flag issued for carry-slot indicator; two new seed entries for `design/ux/accessibility-requirements.md`, still not created)
- [x] Anı-Tetikleyici Etkileşim — **complete 2026-08-02** (Full GDD, CD-GDD-ALIGN: CONCERNS revised; no new UI/registry entries; Dependencies section clarifies decoupled-via-event vs. direct-API relationships, systems-index dependency line corrected to match)
- [x] Sahne Kesmeli Anlatı — **complete 2026-08-02** (Quick Spec via `/quick-design`; two small upstream API additions approved and applied — Gece/Oturum Durumu gained `EndSession()`, Görev/Taşıma Döngüsü gained `IsFinalRoundActive`) — **Batch 3 complete, all 12 MVP systems designed**
- [x] Anı-Tetikleyici Etkileşim `/design-review` — **complete 2026-08-03** (NEEDS REVISION → revised same session, no re-review — Committed-state persistence gap closed via Gece/Oturum Durumu, edit-time validation mechanism specified, perceptibility risk gated on the already-planned audio-paired spike, AC list cleaned — **Approved**; see `design/gdd/reviews/ani-tetikleyici-etkilesim-review-log.md`)
- [x] Seviye/Sahne Geçişi `/design-review` — **complete 2026-08-03** (NEEDS REVISION → revised same session, no re-review — Swapping/unload contradiction resolved, SWAP_FRAME_EPSILON defined, Failed→Idle exit added, OnSoftTransitionRejected scope fixed, RenderSettings/lightmap strategy concretized — **Approved**; see `design/gdd/reviews/seviye-sahne-gecisi-review-log.md`)
- [x] Adaptif Ses Sistemi `/design-review` — **complete 2026-08-03** (NEEDS REVISION → revised same session, no re-review — stinger session-persistence bug fixed (HeldSessionAlreadyPlayed), RMS enforcement added (static limiter + per-zone knob), subtitle text un-fixed pending /ux-design — **Approved**; see `design/gdd/reviews/adaptif-ses-sistemi-review-log.md`)
- [x] `/review-all-gdds` — **complete 2026-08-03, Verdict: FAIL**. Holistic pass across all 12 documents (9 Full GDDs + 3 Quick Specs) found cross-GDD issues invisible to individual `/design-review` passes — most severe: the HARD CUT scene-cut's sound effect has no implementer in either direction (found independently by all 3 review lenses), memory-trigger zones can auto-fire from proximity alone before the player completes the deliberate Hold gesture (defeats the consent premise), and `PersistentShiftIds` has no assigned writer. 9 systems moved to **Needs Revision**: Birinci Şahıs Kontrolcü, Işık/Volume Durum Sistemi, Gece/Oturum Durumu, Seviye/Sahne Geçişi, Adaptif Ses Sistemi, Asansör/Kat-Erişim Sistemi, Diyalog/Anlatı İçeriği, Anı-Tetikleyici Etkileşim, Sahne Kesmeli Anlatı. Full report + 8-item required-actions list: `design/gdd/gdd-cross-review-2026-08-03.md`.
- [x] Applied the 8 required actions from the FAIL report — **complete 2026-08-03**, same session. Verification pass (3 parallel agents re-checking against the prior report) found: 3 of 8 fixes closed cleanly (HARD CUT sting ownership, `onFailed`/Asansör softlock, `MaxCallbacksPerScene`), 4 partially closed and introduced new blocking issues via incomplete propagation (contract changed in the owning doc, not walked to every consumer doc), and 2 original blockers (`ZoneChanged` ownership, stinger/light 2-5s timing gap) were never in the 8-item list at all. Full verification: `design/gdd/gdd-cross-review-2026-08-03-verification.md`. **Verdict: still FAIL.**
- [x] Propagation-gap cleanup pass — **complete 2026-08-03**, same session (narrower scope per user decision, given two prior fix passes hadn't converged): updated the two unpatched `MovementLockScope` consumers (Asansör, Etkileşim) to pass `MoveOnly` explicitly and wired `IsLocked` into Etkileşim's mutual-exclusion check; fixed the Işık/Volume↔Gece/Oturum mutual-dependency contradiction (this file's own Circular Dependencies section, both GDDs' Dependencies sections); removed stale Sahne Kesmeli Anlatı cross-references from Anlatı Durum's GDD; fixed a retracted platform-delta claim surviving in a third location in `birinci-sahis-kontrolcu.md`; synced this file's Dependency Map and Systems Enumeration table (rows 4, 6, 12) to match today's actual dependency changes — this file itself had been the single most-required, least-applied action across all three prior passes. **Deliberately NOT fixed in this pass** (per user decision — treat as separate one-at-a-time design questions, not batched): N1 (stinger audio radius orphaned by the `TriggerMode=ManualOnly` fix), N2 (Gece/Oturum can't read `Persistent` from the event it subscribes to), N5 (Sahne Kesmeli Anlatı's saturation condition has no event to evaluate on), N6 (HARD CUT Sting fires on ordinary SOFT/elevator transitions too, undifferentiated signal), N7 (CutSting vs. abrupt-stop-all ordering undefined), N8 (co-residency tick-skip undefined for an in-flight transition). Also never addressed: `ZoneChanged` ownership, stinger/light timing gap.
- [x] N6 resolved — **complete 2026-08-03**, same session (one-at-a-time, per plan): added `TransitionType { Soft, Hard }` to Seviye/Sahne Geçişi's `OnTransitionStateChanged` event signature (`newState, type`); Adaptif Ses's HARD CUT Sting now filters on `type == Hard` before firing, so ordinary SOFT transitions (Asansör, level transitions) no longer trigger it — closes the accidental jump-scare / Pillar 2 violation risk. New AC13b added (negative case: SOFT transition must not fire CutSting). Files touched: `seviye-sahne-gecisi.md` (event signature + enum), `adaptif-ses-sistemi.md` (Core Rules, Dependencies, AC13a/13b), this file (Foundation Layer item 6).
- [x] N8, N5, N2, N1, N7 resolved — **complete 2026-08-03**, same session (one-at-a-time per plan, ordered by gameplay-criticality per user decision — worst-first: soft-lock risk > dead narrative branch > broken persistence > perceptual-only gap > audio-safety-net ordering):
  - **N8** (co-residency tick-skip could freeze a transition forever on "the most ordinary path" — Hold completed right before boarding the elevator): clarified that the tick-skip rule only pauses position-based sampling, never the time-based `x` progress accumulator of an already-in-flight `Shifting-In`/`Shifting-Out` — a transition always completes on schedule now, even while its scene is inactive. `isik-volume-durum-sistemi.md` only (also fixed a second stale header: `Status` said `In Design`/2026-08-01, index said `Needs Revision` — same header-staleness class as the two fixed in the previous pass).
  - **N5** (saturation ending had no event to evaluate on, dead in its own motivating scenario): added `OnTriggerFired(shiftId)` to Gece/Oturum Durumu and `OnFinalRoundStarted` to Görev/Taşıma Döngüsü; Sahne Kesmeli Anlatı now re-evaluates its saturation condition on either. Files: `gece-oturum-durumu-2026-08-02.md`, `gorev-tasima-dongusu.md`, `sahne-kesmeli-anlati-2026-08-02.md`.
  - **N2** (Gece/Oturum assigned to write `PersistentShiftIds` but structurally couldn't read `Persistent`): added `bool IsShiftPersistent(string shiftId)` query to Işık/Volume's contract (chose a narrow query over extending `OnShiftStateChanged`'s payload to all 3 subscribers — smaller propagation surface, only Gece/Oturum needed it). Files: `isik-volume-durum-sistemi.md` (also closed Blocked AC #17's mechanism half), `gece-oturum-durumu-2026-08-02.md`.
  - **N1** (stinger audio radius derived from a `radius` field the `TriggerMode=ManualOnly` fix had just declared vestigial for memory-trigger zones): added `ShiftConfig.StingerAudioRadius` + `GetStingerAudioRadius(shiftId)` query, required `>0` by the existing `IPreprocessBuildWithReport` edit-time validation. Files: `isik-volume-durum-sistemi.md`, `adaptif-ses-sistemi.md` (stinger_falloff formula), `ani-tetikleyici-etkilesim.md` (validation + AC4b).
  - **N7** (CutSting and the abrupt-stop-all rule fire off the same `Swapping` event with no defined order — the safety-net sting could silence itself): one clarifying passage in `adaptif-ses-sistemi.md` Edge Cases — CutSting is exempt from the abrupt-stop rule, plus new AC13c.
- [x] `ZoneChanged` ownership and stinger/light timing gap resolved — **complete 2026-08-03**, same session (the two original-report blockers that never made it into any fix-action list across all three prior passes):
  - **`ZoneChanged` ownership**: Adaptif Ses now owns and defines a new `AmbientZoneVolume` component — one per named zone (Depo, Servis Koridoru, Balo Salonu), a simple Unity trigger collider firing `ZoneChanged(zoneId)` on player entry, with an explicit one-time overlap check at `Start()` to handle the "player spawns already inside a zone" case (Unity's `OnTriggerEnter` doesn't fire for that). No coordination with any other system required — self-contained. New AC1a/1b.
  - **Stinger/light timing gap**: the memory-trigger stinger fired on `Held` (~3s after the light starts shifting), contradicting the "compound light+sound effect" the concept prototype validated. Applied the same reasoning already used to fix `PersistentShiftIds`'s timing gap: since `MemoryTriggerDef`-linked shifts are always `Persistent=true` (guaranteed to reach `Held`, never revert), the stinger now fires on `Shifting-In` for Persistent shifts — synchronized with the same frame the light begins its ramp. `Held` remains a trigger too (unchanged, still needed for non-Persistent/Automatic shifts and to safely no-op the reload-restore re-fire via the existing `HeldSessionAlreadyPlayed` guard — both trigger paths share one guard, no double-play risk). New AC6c (the core timing-fix test); AC6/6a updated to reflect the dual path.
  - Files touched: `adaptif-ses-sistemi.md` (Core Rules, Interactions, Dependencies, AC1a/1b/6/6a/6c), `isik-volume-durum-sistemi.md` (Dependents list), `ani-tetikleyici-etkilesim.md` (2 stale "`OnShiftStateChanged(Held)`-only" cross-references fixed to reflect the dual path).
- [x] `/review-all-gdds` re-verification — **complete 2026-08-04, Verdict: FAIL**. Full pass across all 14 documents (12 system docs + game-concept.md + this file). Found 12 blocking items: 9 consistency (mostly the same propagation-gap pattern as before — a fix landed in one place, a duplicate/parallel mention elsewhere was missed) and 4 design-theory (genuinely new, more severe — not propagation gaps). Full report: `design/gdd/gdd-cross-review-2026-08-04.md`.
- [x] Mechanical/consistency fixes from the 2026-08-04 report — **complete 2026-08-04**, same session (everything not requiring a new design decision): AC7/AC6c contradiction, `HeldSessionAlreadyPlayed` guard removal-predicate race, stale `radius`/`Held` mentions in Adaptif Ses's own Core Rules (2 places), stale "N6 open"/single-arg-event references in `seviye-sahne-gecisi.md` (4 places) and `adaptif-ses-sistemi.md` Dependencies, new `AmbientZoneVolume` scene-active co-residency guard (mirroring Işık/Volume's own established fix) + AC1c, 2 stale `OnClueKnown` references in `ani-tetikleyici-etkilesim.md`, a stale `RequestMovementLock`-rejection argument, `birinci-sahis-kontrolcu.md`'s false "no dependencies" claim (now honestly documents its `InteractableRegistry` read on Etkileşim — the underlying layering question stays open, see below), a new "SFX" mixer group in Adaptif Ses to close 3 references to a nonexistent "ducking" rule in `gorev-tasima-dongusu.md`, several stale "henüz tasarlanmadı/tasarlanmamış" labels, `isik-volume-durum-sistemi.md`'s AC15/AC16/Blocked-ACs stale claims, `StingerAudioRadius`'s `float?`→`float` type fix, a false stinger-caption "same pattern" parity claim in `gorev-tasima-dongusu.md`, a new Dependencies + Open Questions section for `diyalog-anlati-icerigi-2026-08-02.md` (had neither), and this file's Dependency Map/status-data corrections (Sahne Kesmeli↔Anı-Tetikleyici dependency direction — 3 of 4 documents agreed there's no direct dependency, this file's Dependency Map was the outlier; `ani-tetikleyici-etkilesim.md` and `gorev-tasima-dongusu.md` headers reopened to Needs Revision to match newly-found issues; `etkilesim-sistemi.md` flagged Needs Revision). Per user instruction: design-judgment items were explicitly NOT resolved unilaterally, see below.
- [x] **Saturation-ending timing bug — RESOLVED 2026-08-04** (user decision: option A, "final round item must be picked up"): added `bool HasCarriedInFinalRound` + `event Action OnFinalRoundItemPickedUp` to Görev/Taşıma Döngüsü (fires once, first pickup while the final round is active, never resets). Sahne Kesmeli Anlatı's saturation condition (b) now requires this flag `true` as a third clause, and subscribes to the new event as a third re-evaluation trigger alongside `OnTriggerFired`/`OnFinalRoundStarted`. This guarantees the final round is always at least partially played before saturation can end the night, resolves the preload/trigger-threshold collision (the task-side preload still fires at round-activation, now with real lead time before the later pickup-gated trigger), and — deliberately, per the user's chosen option — makes the HARD CUT always happen while the player is physically carrying the final load, which reinforces (rather than accidentally undermines) Seviye/Sahne Geçişi's existing "Bedenin Çalınması" (torn from mid-motion) Player Fantasy language instead of just coincidentally sometimes matching it. New AC18 in `gorev-tasima-dongusu.md`, updated/new ACs in `sahne-kesmeli-anlati-2026-08-02.md`.
- [x] **Hold interaction identity — RESOLVED 2026-08-04** (user decision: physical/meaning layer split, recommended option): Etkileşim's Player Fantasy narrowed from "no conscious decision moment" to "confident physical execution, no fumbling" — the general Hold mechanism no longer makes a project-wide claim about whether the *choice* to hold feels deliberate, only about how the *hand* performs it. Anı-Tetikleyici's "bile bile yaptım" (deliberate choice) fantasy stands unmodified as the layered meaning the one actual Hold interaction in the MVP carries — the two are now explicitly compatible ("eller nasıl yapacağını zaten biliyor" + "zihin bunu bilerek seçiyor" can both be true). Separately, closed the orphaned-feedback gap: Etkileşim now owns a default, plain (non-dramatic) crosshair fill for *all* Hold interactables, driven directly from its own already-computed `t` — no object needs to implement anything to get it, individual objects may layer bespoke `OnHoldProgress` VFX/SFX on top but never need to. This closes the "0.6-1.5s of literally no feedback" gap. New AC14 in `etkilesim-sistemi.md`. Files touched: `etkilesim-sistemi.md` (Player Fantasy, formula rationale, new Core Rules bullet, UI Requirements, AC14), `ani-tetikleyici-etkilesim.md` (3 passages updated from "assumes a UI that doesn't exist" to "consumes a UI that now exists").
- [x] **Tension-escalation ownership + time-pressure/risk gap — RESOLVED 2026-08-04** (bundled — same underlying question, resolved without asking per user's "continue through the critical ones, no need to ask" instruction): investigated using `MaxCallbacksPerScene` overflow as a soft narrative cost (my initial idea) and rejected it — MVP's default (3) is deliberately set equal to MVP's total trigger count (3), guaranteeing zero overflow ever, so it's structurally inert as a cost mechanism at MVP scope; inventing an artificial cost would also conflict with the already-locked "no punishing failure state, player never loses" design pillar. Decision: **retracted the time-pressure/risk framing** from `game-concept.md` (Core Loop, Core Fantasy, Key Dynamics, Core Mechanics list) and `birinci-sahis-kontrolcu.md` ("aciliyet var" line) — reframed honestly as pace/attention rather than safe/risky, since no system ever implemented a real cost and none should exist given the failure-free design. Separately, **gave tension-escalation a real owner**: Adaptif Ses now has a round-based mechanism using the previously-vague "2-3 layers per area" language — 2 base layers play constantly, a 3rd layer (each area's least-placeable, most uncanny sound — distant plumbing, a distant pneumatic door, chandelier tinkling) fades in via a new `tension_gain` formula (same smoothstep convention as `shift_progress`/`ambient_crossfade`) driven by round index. Added `CurrentRoundIndex`/`TotalRoundCount` read-only queries to Görev/Taşıma Döngüsü (same counters `Highlight(round)` already used internally, now also exposed). New AC1d/AC19. Files: `game-concept.md`, `birinci-sahis-kontrolcu.md`, `gorev-tasima-dongusu.md`, `adaptif-ses-sistemi.md`.
- [x] **`TriggerMode` edit-time validation architecture — RESOLVED 2026-08-04** (resolved without asking per user's "continue through the critical ones, no need to ask" instruction): rejected moving `TriggerMode` onto `ShiftConfig` (the seemingly-simpler fix) — it's structurally impossible, because an `Automatic` zone must know its own mode *before* `TriggerShift` is ever called (while still `Dormant`, deciding whether to self-trigger on proximity), and `ShiftConfig` only reaches the zone *at* a `TriggerShift` call. Also rejected adding a direct object reference from `MemoryTriggerDef` (a ScriptableObject asset) to its zone (a scene object) — a known fragile Unity anti-pattern, breaks silently on scene reorganization. Decision: made each zone's already-implicit `shiftId` field an explicit, documented Core Rule (it was always the de facto matching key, just never named as one), and split the edit-time validation into two steps — the existing fast asset-only scan (duplicate `shiftId`, `Persistent`, `StingerAudioRadius` — all `ShiftConfig`/asset fields) stays as-is, plus a new, separate, more expensive scene-scan step (iterates `EditorBuildSettings.scenes`, collects zone components, cross-references by `shiftId` string match) specifically for the `TriggerMode` check. Files: `isik-volume-durum-sistemi.md` (new `shiftId` Core Rule), `ani-tetikleyici-etkilesim.md` (mechanism rewrite, AC4a updated).
- [x] **Approach-taper camouflage — RESOLVED 2026-08-04** (resolved without asking, per user's "continue through the critical ones, no need to ask" instruction; chose the "add decoy interactables" option over dropping the camouflage claim — dropping it would let players use the taper as a "metal detector" for memory triggers, directly undermining Pillar 5, a real design-quality regression, whereas decoys are cheap and diegetically appropriate for a hotel service area, also feeding Pillar 3): added a content requirement — each of the 3 MVP areas must contain at least one non-memory-trigger, non-carry-item decoy `IInteractable` (door handle, light switch, thermostat, cleaning-cart brake — matching Etkileşim's own existing example), enforced by a new build-time content-validation AC (reuses the shared `IPreprocessBuildWithReport` editor utility pattern already established for other content checks in this project). Exact count/placement deferred to content-authoring/level-design phase — this only locks the requirement. New AC17 in `birinci-sahis-kontrolcu.md`.
- [x] **All 6 design decisions from the 2026-08-04 review are now resolved.**
- [x] Targeted manual re-verification — **complete 2026-08-04**, same session. A full `/review-all-gdds` re-run was attempted but the background agent hit the session's API usage limit before completing (not a real finding — infra failure, resets 18:10 Europe/Istanbul). Did a manual, targeted sweep of the highest-risk areas instead (every contract touched by the 6 design-decision fixes) rather than a full 14-doc re-read. Found and fixed **one real, serious contradiction**: the Hold-interaction-identity fix gave Etkileşim Sistemi a *universal default* crosshair fill for all Hold interactables — but Anı-Tetikleyici Etkileşim's Player Fantasy and Visual/Audio Requirements still argued forcefully for **literal zero visual feedback** during the hold ("en küçük bir titreme... bile eklense... bu his oyuncunun bedeninden oyunun geri bildirim kanalına taşınır"), directly contradicted by the new universal default actually being applied to it (the only Hold interactable in the MVP). Fix: added an opt-out — `bool SuppressDefaultHoldFill` on `IInteractable` (defaults `false`), which Anı-Tetikleyici returns `true` from, restoring a *real* zero-feedback guarantee instead of an aspirational one. New AC14a in `etkilesim-sistemi.md`; `ani-tetikleyici-etkilesim.md`'s Core Rules, Visual/Audio Requirements, and CD-GDD-ALIGN note all rewritten to reflect this. No other propagation gaps found in the targeted sweep (time-pressure/risk retraction, TriggerMode two-step validation, tension_gain dependency bidirectionality, saturation 3-clause condition — all checked clean).
- [x] **Full `/review-all-gdds` re-verification — complete 2026-08-04, Verdict: FAIL** (session limit reset, ran as planned). Two independent parallel background agents (Phase 2 consistency, Phase 3 design theory) with no memory of prior rounds, plus a direct Phase 4 scenario walkthrough. Found **8 blocking issues** — most severe, confirmed independently by all three review lenses: the saturation-ending's own trigger condition (`OnTriggerFired`, same frame as the completing Hold) fires `RequestHardCut` with no settle delay, so the compound light+sound payoff, the clue-known write, and the psychiatrist-scene callback for the player's *final* deliberate memory-trigger action are all reliably destroyed by the very event they complete — worst on exactly the playthrough shape (attention drawn away from tasks toward the hotel by the game's own dimming curve) the design steers players toward. Also found: the two HARD CUT endings (task-completion vs. saturation) are specified to feel different but share one identical mechanism; MVP has no guaranteed Pillar 1 exposure at all (every memory-trigger is consent-gated `ManualOnly`, and no `Automatic` ambient shift is assigned as MVP content, so a complete playthrough can contain zero subjective-reality shifts); an `AmbientZoneVolume` re-arm bug means ambience silently never restarts after any elevator ride or HARD CUT; the `etkilesim-sistemi.md` Hold-fill ACs (14/14a) contradict each other and are unsatisfiable given MVP's only Hold interactable opts out; `systems-index.md`'s own dependency graph (this file) drifted again (Etkileşim/Anı-Tetikleyici row, FPC row, Adaptif Ses/Görev links — none of these needed a Status change, only Dependency Map/Enumeration table fixes, tracked separately below); and `tension_gain`/`Highlight` share an unguarded division-by-zero with a live AC1-vs-AC17 contradiction over whether `TotalRoundCount=1` can occur. Full report + required-actions list: `design/gdd/gdd-cross-review-2026-08-04-verification.md`. Per user instruction, systems index Status fields were checked but not changed — every flagged system GDD was already `Needs Revision` from the prior pass; only this file's own Dependency Map/Enumeration rows need mechanical fixes (tracked in the report, not yet applied).
- [x] **All 8 blocking items from the 2026-08-04 full re-verification resolved — complete 2026-08-04**, same session. 5 mechanical fixes (AmbientZoneVolume re-arm bug, Hold-fill AC contradiction, systems-index dependency graph, tension_gain/Highlight div-by-zero guard, tension_gain arithmetic error — see prior entry) plus the 3 genuine design decisions, all resolved per user instruction ("do whatever's needed, get it done"):
  - **Saturation-ending timing**: Gece/Oturum Durumu gained `HashSet<string> SettledTriggerIds` + `event Action<string> OnTriggerSettled`, populated only when a fired trigger's shift actually reaches `Held` (~3s after Hold completion) rather than at `Shifting-In`. Sahne Kesmeli Anlatı's saturation condition now gates on `SettledTriggerIds.Count`/`OnTriggerSettled` instead of `FiredTriggerIds.Count`/`OnTriggerFired` — this guarantees the light+sound compound effect, the clue-known write, and the psychiatrist-scene callback all complete before the ending can trigger. Preload timing is unaffected (still uses the earlier `FiredTriggerIds` signal, since preload is prep, not the actual cutover).
  - **Two endings, one mechanism**: added `HardCutConfig.Abrupt` (bool) — Sahne Kesmeli Anlatı passes `Abrupt=true` for saturation (unchanged: instant audio cut + CutSting) and `Abrupt=false` for task-completion (new: ambience crossfades to silence via the existing `ambient_crossfade` mechanism, CutSting doesn't play). Seviye/Sahne Geçişi carries but doesn't interpret the flag (new `GetCurrentHardCutAbrupt()` query, same pattern as `GetStingerAudioRadius`/`IsShiftPersistent` — narrow query over event-payload expansion). Zero-frame swap mechanics unchanged for both.
  - **Guaranteed Pillar 1 MVP exposure**: added a 5th MVP content requirement (`game-concept.md`) — at least 1 `TriggerMode=Automatic`, `Persistent=false`, non-clue-bearing ambient shift zone on the mandatory carry route, distinct from the 2-3 player-triggered memory triggers. Mechanism and build-time content validation (reusing the shared `IPreprocessBuildWithReport` pattern) added to `isik-volume-durum-sistemi.md` (new AC21/AC22).
  - Files touched this round: `gece-oturum-durumu-2026-08-02.md`, `sahne-kesmeli-anlati-2026-08-02.md`, `seviye-sahne-gecisi.md`, `adaptif-ses-sistemi.md`, `game-concept.md`, `isik-volume-durum-sistemi.md`, `etkilesim-sistemi.md`, `ani-tetikleyici-etkilesim.md`, `gorev-tasima-dongusu.md`, this file.
  - **Not yet done**: a fresh full `/review-all-gdds` re-run to confirm convergence — per this project's own established discipline, Status fields are deliberately left at `Needs Revision` rather than flipped to `Approved`/cleared until a real verification pass confirms no new propagation gaps were introduced by this round's fixes (this exact failure mode — a fix introducing a new gap — has recurred multiple times across this project's review history). Recommended next: re-run `/review-all-gdds` once more before treating the GDD phase as done.
- [ ] Design in batches of 2-3, implementing between batches — do not front-load all 12 GDDs
- [ ] Run `/gate-check pre-production` when MVP systems are designed
- [ ] Validate the highest-risk systems (Memory-Trigger Interaction) with `/vertical-slice` before committing to Production
