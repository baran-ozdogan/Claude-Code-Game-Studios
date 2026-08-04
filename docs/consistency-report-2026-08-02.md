# Consistency Check Report

**Date**: 2026-08-02
**Scope**: full
**Registry entries checked**: 0 entities, 0 items, 10 formulas, 13 constants
**GDDs scanned**: 9 (adaptif-ses-sistemi.md, ani-tetikleyici-etkilesim.md, anlati-durum-ipucu-takibi.md, asansor-kat-erisim-sistemi.md, birinci-sahis-kontrolcu.md, etkilesim-sistemi.md, gorev-tasima-dongusu.md, isik-volume-durum-sistemi.md, seviye-sahne-gecisi.md)

---

## Conflicts Found (must resolve before architecture)

None.

---

## Stale Registry Entries

None.

---

## Verified Clean

- ✅ `walk_speed_unloaded` (1.6 m/s): consistent across `birinci-sahis-kontrolcu.md` (source), `adaptif-ses-sistemi.md` (`footstep_volume` formula and worked example), and implicitly consistent with `gorev-tasima-dongusu.md` (references the carrying-state transition without restating the number — no contradiction).
- ✅ `walk_speed_carrying` (1.35 m/s) / `carry_multiplier` (0.84375): identical in `birinci-sahis-kontrolcu.md` (source, as `CarryMult`) and `adaptif-ses-sistemi.md` (worked example: 1.35/1.6 = 0.84375). `gorev-tasima-dongusu.md` now lists both in `referenced_by` (it triggers `SetCarrying`, which drives this value) but does not restate the numbers itself — no conflict.
- ✅ `footstep_volume`: formula and "never branches on carry state" rule are respected by `gorev-tasima-dongusu.md`'s Audio Requirements — the new "jostle" sound layer is explicitly kept separate/independent from this formula rather than modifying it.
- ✅ `shift_duration` (~3.0s): identical across all uses within `isik-volume-durum-sistemi.md` (formulas, acceptance criteria, empirical validation note).
- ✅ Round-count value **3–5** (Görev/Taşıma Döngüsü's `CarryRound` count): identical across `game-concept.md` (source lock), `gorev-tasima-dongusu.md`'s Overview, Formulas, Tuning Knobs, and Acceptance Criteria #1/#9 — no drift between the game-concept lock and the system GDD.
- ✅ `movement_acceleration`'s registered ceiling (1.6 m/s) is referenced correctly by `adaptif-ses-sistemi.md`'s `footstep_volume` notes ("also the registered ceiling of `movement_acceleration`") — matches registry value.

## Unverifiable References (no conflict, informational)

- `anlati-durum-ipucu-takibi.md`, `seviye-sahne-gecisi.md`, `asansor-kat-erisim-sistemi.md`, `etkilesim-sistemi.md` reference no registered formula or constant by literal name outside their own domain — expected, no shared numeric surface with those systems yet. (`hold_progress`, owned by `etkilesim-sistemi.md`, is explicitly flagged in its own registry notes as something `gorev-tasima-dongusu.md` and Anı-Tetikleyici Etkileşim *may* remap later — confirmed NOT done in `ani-tetikleyici-etkilesim.md`'s Core Rules, which explicitly declines the remap; not a conflict.)
- `ani-tetikleyici-etkilesim.md` mentions `shift_progress` and "hysteresis" only descriptively (crediting ownership to `isik-volume-durum-sistemi.md`, never restating a value) — no conflict, no new registry candidate (its own new value, the 0.6–1.5s HoldDuration sub-range, doesn't cross a system boundary yet since no other GDD references it).
- Several registry formulas (`approach_slow_taper`, `shift_progress`, `hysteresis_radius`, `light_color_intensity_blend`, `hysteresis_factor`, `memory_intensity_multiplier_default`, `memory_color_*`, `volume_profile_*`, `pitch_clamp`, `fov_default`, `headbob_amplitude`) do not appear by their exact snake_case identifier anywhere outside their own source GDD — this is expected given GDDs are authored in Turkish prose, not code-identifier style; their numeric values were not independently re-verified beyond the spot-checks above since no second GDD claims to restate them.

---

**Verdict: PASS** — no conflicts found. Registry and all 9 in-scope GDDs agree on every cross-referenced value checked.
