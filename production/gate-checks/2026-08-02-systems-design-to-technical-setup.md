# Gate Check: Systems Design → Technical Setup

**Date**: 2026-08-02
**Checked by**: gate-check skill (review mode: full)

## Required Artifacts: 1/3

- [x] Systems index exists at `design/gdd/systems-index.md` with all MVP systems enumerated (12/12 designed — Batch 1+2+3 complete)
- [ ] All MVP-tier Full GDDs pass `/design-review` — only 3/9 (Işık/Volume Durum Sistemi, Anlatı Durum/İpucu Takibi, Görev/Taşıma Döngüsü — all **Approved**). Quick Specs (Gece/Oturum Durumu, Diyalog/Anlatı İçeriği, Sahne Kesmeli Anlatı) bypass `/design-review` by design.
- [ ] Cross-GDD review report (`/review-all-gdds`) — does not exist

## Quality Checks: 3/6

- [x] System dependencies mapped in systems index, bidirectionally consistent (verified this session; one stale High-Risk table entry corrected — lighting-authoring-model was marked unresolved but had actually been resolved 2026-08-01)
- [x] MVP priority tier defined
- [x] No stale GDD references remaining (checked and corrected this session)
- [ ] All MVP GDDs pass individual design review — 6/9 Full GDDs still unreviewed
- [ ] `/review-all-gdds` verdict — never run
- [ ] Cross-GDD consistency fully verified — `/consistency-check` (registry-level) PASSED 2026-08-02 (9 GDDs, 0 conflicts), but `/review-all-gdds` (design-theory level) has not run

## Director Panel Assessment

**Creative Director: CONCERNS**
Pillars 1–3 (Subjective Reality, Quiet Dread, Grounded Labor) cleanly covered by the 12 MVP GDDs. Pillars 4–5 have deliberately thin MVP surface — flagged and accepted at CD-SYSTEMS with a mitigation plan (a cheap Pillar-4 probe before Vertical Slice) that has not yet happened. The system carrying the actual compound fantasy (Anı-Tetikleyici Etkileşim — drives lighting, audio, and narrative state simultaneously) has not passed independent `/design-review`. No `/review-all-gdds` pass exists, so a cross-cutting Pillar 1 violation can't be ruled out (CD-GDD-ALIGN reviews were per-system, not cross-system). Secondary watch item: the frozen-GI/mixed-lighting corridor risk in `game-concept.md` Technical Risks threatens the light+sound compound effect that delivers Pillar 2 — a level-authoring discipline issue that should inform rendering ADRs.

**Technical Director: CONCERNS**
`docs/architecture/tr-registry.yaml` has zero entries — `/architecture-review` has never run. No master `architecture.md`, no ADRs (expected at this exact transition, since Technical Setup produces them — but it means this work starts from zero). High-Risk table: 2/4 risks closed (Adaptif Ses, Işık/Volume). The other two — Anlatı Durum/İpucu Takibi (bottleneck, 5 dependents) and Anı-Tetikleyici Etkileşim (God Object risk) — have no ADR yet; only a *recommended* `MemoryTriggerEvent` contract exists in the index. Writing ADRs against the 6 unreviewed GDDs (including Birinci Şahıs Kontrolcü, the Foundation-layer system everything depends on) risks rework if review surfaces rule changes. Performance budgets are documented only as generic global numbers, not decomposed per system or addressing Anı-Tetikleyici's compound-trigger cost.

**Producer: CONCERNS**
Scope/timeline still sound (OPTIMISTIC verdict, accepted 2026-08-01, mitigations holding — batching, GDD-time cap, protected polish buffer). Design order (Foundation → Core → Feature) is sound, with the highest-effort/highest-risk system (Anı-Tetikleyici Etkileşim) deliberately sequenced last — but that also means it's the least-validated system right now. Key risk: 6/9 Full GDDs — including Anı-Tetikleyici Etkileşim, Birinci Şahıs Kontrolcü, and Etkileşim Sistemi (both on the core-loop critical path) — lack independent review. Locking architecture against unreviewed GDDs risks contract-rework cost concentrated in sprint 1–2, where a two-person part-time team can least absorb it.

**Art Director: NOT READY**
`design/art/art-bible.md` does not exist (confirmed via glob, re-verified during chain-of-verification). Per this project's own Gate Coverage table, AD-ART-BIBLE is a **required** gate for Technical Setup, not optional. What exists is strong: the Visual Identity Anchor in `game-concept.md` (named direction, one-line visual rule, shape language, two differentiated color palettes with shadow-hardness rules) carried through AD-CONCEPT-VISUAL (2026-08-01). Several GDDs already have Visual/Audio Requirements sections referencing this anchor. But Technical Setup is where asset-pipeline decisions (modular kit standards, lightmap UV conventions, texel density, poly budgets, static-flagging rules) get locked — and `game-concept.md` already flags "Art Pipeline Complexity: Medium... modules must be static-flagged, second UV channel required" as unresolved specs. Recommendation: run `/art-bible` before or alongside `/create-architecture` so asset-pipeline ADRs aren't written blind.

## Blockers

1. **No art bible exists** (`design/art/art-bible.md`) — required gate artifact for this transition. Run `/art-bible`.
2. **6 of 9 Full GDDs lack independent `/design-review`**: Birinci Şahıs Kontrolcü, Seviye/Sahne Geçişi, Adaptif Ses Sistemi, Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Anı-Tetikleyici Etkileşim. All four directors flagged this; consensus priority order: **Anı-Tetikleyici Etkileşim** (highest technical/design risk, God-Object concern) → **Birinci Şahıs Kontrolcü** (Foundation-layer, everything depends on it) → **Etkileşim Sistemi** (core-loop critical path) → remaining three.
3. **No `/review-all-gdds` cross-GDD report exists** — required artifact; catches cross-cutting pillar/design-theory issues that per-system CD-GDD-ALIGN reviews cannot.

## Recommendations (non-blocking)

- Execute CD-SYSTEMS' deferred Pillar 4 mitigation (a cheap probe) before Vertical Slice — track it, don't let it quietly drop.
- Once Technical Setup begins, the `MemoryTriggerEvent` contract (Anı-Tetikleyici Etkileşim ↔ Işık/Volume Durum Sistemi) should be the **first** ADR authored.
- The frozen-GI/mixed-lighting corridor risk (`game-concept.md` Technical Risks) should inform rendering ADRs.
- Decompose the global performance budget (60fps/16.6ms, ~2000 draw calls, 4GB) per system once architecture work begins, particularly for Anı-Tetikleyici Etkileşim's compound trigger cost.

## Chain-of-Verification

5 challenge questions checked for this FAIL draft, 2 by direct re-scan (Glob/find confirmed `design/art/art-bible.md` and any `design/gdd/gdd-cross-review-*.md` both genuinely absent, not a stale assumption). Blockers are verified missing required artifacts, not inferred. Verdict unchanged: **FAIL**, resolvable — not a fundamental design problem.

## Verdict: FAIL

Driven by AD-PHASE-GATE's NOT READY (missing required art bible) plus two missing required artifacts (individual GDD reviews, cross-GDD report). Minimal path to PASS: (1) `/art-bible`, (2) `/design-review` on the 6 remaining Full GDDs (priority order above), (3) `/review-all-gdds`.
