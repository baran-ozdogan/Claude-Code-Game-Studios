# Review Log: Işık/Volume Durum Sistemi

## Review — 2026-08-01 (rounds 1-3 + spike) — Verdict: APPROVED
Scope signal: M (producer should verify before sprint planning)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, level-designer, creative-director (synthesis), across 3 review rounds
Blocking items: 4 (round 1) + 8 (round 2) + 1 mechanism requiring a spike (round 3) | Recommended: ~15 across all rounds, all closed or explicitly deferred with an owner

Summary: Three consecutive adversarial review rounds progressively tightened the
GDD — round 1 fixed structural gaps (unspecified Light Mode risking silent
invisible rendering, a non-invertible ShiftProgress formula breaking the
interrupt/resume contract, a falsely-declared "no dependencies" claim, and a
box-collider/radius-formula contradiction). Round 2 fixed narrower follow-on
gaps the round-1 fixes exposed (missing guard rails, an undeclared
Birinci Şahıs Kontrolcü dependency, a stale systems-index.md reference,
missing AC coverage). Round 3 found the Volume weight/blend-distance
mechanism had been described incorrectly in a different way in each prior
round — rather than a fourth argued-from-prose fix, the creative director
routed it to an empirical Unity spike (`prototypes/yankilar-volume-weight-spike/`).
The project owner ran the spike's Corridor C (box sized per the new Box
Collider Safety Margin formula, blendDistance=0) and confirmed a clean,
pop-free transition with no visible snap — the practical configuration is
now written into the GDD's Core Rules and Formulas sections, with an
explicit note that the internal URP mechanism (vs. simply "the player never
left the box while active") was not isolated, since the undersized-box
comparison corridors (A/B) were built but not run. This was judged
sufficient to close the item.

Two items remain deliberately open, not blocking: (1) the 3-second
transition's detectability under divided attention (the prototype that
validated the timing tested an idle, undistracted player, not the task-
focused core loop) — flagged as a playtest risk to watch, not a spec defect;
(2) Persistent-flag accumulation + multi-zone-visibility compounding near
the finale — both already tracked in Open Questions with named future owners
(final sequence GDD, level design phase) per the original CD-GDD-ALIGN note.

Prior verdict resolved: First review (this log did not exist before this
session; all three rounds and the spike happened within one continuous
review session, tracked here as a single consolidated entry).
