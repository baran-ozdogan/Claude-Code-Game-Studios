# Cross-GDD Review Verification Report

**Date**: 2026-08-03 (same-day follow-up to `gdd-cross-review-2026-08-03.md`)
**Purpose**: Verify whether the 8 required actions applied after the initial FAIL-verdict review actually closed the gaps they targeted, and check whether the fix pass introduced any new cross-document issues.

---

## Headline

Of the 8 required actions, **3 closed cleanly** (HARD CUT sting ownership, `onFailed`/Asansör softlock, `MaxCallbacksPerScene`). **4 partially closed and introduced new blocking issues** in the process (`PersistentShiftIds` writer, movement-lock scope, Sahne Kesmeli Anlatı's end-condition, the co-residency tick-skip). **2 of the original 7 blocking issues were never in the required-actions list at all** and remain completely untouched (`ZoneChanged` ownership, the stinger/light timing gap). `systems-index.md` was never updated despite being explicitly required.

Net result: the blocking-issue count did not go down. It changed membership. **Verdict: FAIL, still.**

The dominant failure pattern across the new issues is the same one: **a contract was changed in the system that owns it, without updating every system that consumes it** (including `systems-index.md` itself). This is worth naming explicitly because it recurred at least four times independently across three different fixes — it's a process gap, not a one-off mistake.

---

## Verification of the 8 Required Actions

| # | Action | Status | Note |
|---|---|---|---|
| 1 | HARD CUT sting implemented in Adaptif Ses | ✅ **CLOSED** | Ownership, mixer group, RMS ceiling, AC, bidirectional dependency all verified present. Minor residual: the "mid-sentence scene opening" secondary safeguard still has no assigned owner. |
| 2 | Manual-trigger-only mode (`TriggerMode`) | ✅ **CLOSED** (consent bug) | Verified both directions, edit-time-validated. But see **N1** below — closing this opened a new bug. |
| 3 | `PersistentShiftIds` writer assigned | ⚠️ **PARTIALLY CLOSED** | Writer assigned and timing fixed, but see **N2, N3** — the writer literally cannot read the data it's required to write, and the resulting mutual dependency was declared in only one direction. |
| 4 | `onFailed` + Asansör `Failed` handler | ✅ **CLOSED** | Cleanest fix of the batch — verified across signature, state table, edge cases, and ACs in both files. |
| 5 | `MovementLockScope` parameter | ⚠️ **PARTIALLY CLOSED** | Enum added correctly to the owning system, but see **N4** — two of three consumers were never updated to pass it, and the new default (`Full`) actively breaks their own existing Acceptance Criteria. |
| 6 | Sahne Kesmeli Anlatı OR-condition + saturation proxy | ⚠️ **PARTIALLY CLOSED** | The design fix (gate on `IsFinalRoundActive`, count `FiredTriggerIds`) is correct and was independently confirmed sound by both the design-theory and scenario passes. But see **N5** (found independently by both passes) — the event this system used to listen to was removed and nothing replaced it. |
| 7 | `MaxCallbacksPerScene` 2→3 | ✅ **CLOSED** | Default corrected, false claim retracted, build-time check added. |
| 8 | Co-residency window tick-skip | ⚠️ **PARTIALLY CLOSED** | Closes the origin-scene case. See **N6** — undefined for a zone with an in-flight transition, which is described in this project's own docs as the *normal* elevator path, and **W-d** — the target-scene side isn't guarded. |

---

## New Blocking Issues (introduced by the fix pass)

🔴 **N1 — The memory-trigger stinger's audible radius is now explicitly "unused" by design, per the fix's own text**
Closing the proximity-auto-trigger bug required making `R_trigger` unused for `ManualOnly` zones. The fix's own language: *"memory-trigger bölgeleri için `R_trigger` pratikte tamamen kullanılmaz kalır, **bu kasıtlıdır**."* But `adaptif-ses-sistemi.md`'s `stinger_falloff` formula derives the stinger's `minDistance`/`maxDistance` directly from that same radius. A level designer has no reason to author a meaningful value for a field the design docs say is intentionally unused — the guard-rail clamp floors it at `0.01m`, making the game's single most important compound effect inaudible by default.
→ Give `ShiftConfig` a dedicated `stingerAudioRadius` field (already proposed as an Open Question in `adaptif-ses-sistemi.md`) instead of deriving audio falloff from a gameplay radius the consent fix just declared vestigial for this exact zone type.

🔴 **N2 — `Gece/Oturum Durumu` was assigned to write `PersistentShiftIds` but cannot determine which shifts are `Persistent`**
The event it subscribes to, `OnShiftStateChanged(shiftId, newState, zoneCenter, radius)`, carries no `Persistent` flag and no `ShiftConfig` reference — confirmed identical across every one of its three existing subscribers. `isik-volume-durum-sistemi.md`'s published contract has no `IsShiftPersistent()` query either. The writer this fix assigned literally cannot evaluate its own Core Rule.
→ Extend the event payload, or add a read-only `bool IsShiftPersistent(string shiftId)` query to Işık/Volume's contract.

🔴 **N3 — The Gece/Oturum ↔ Işık/Volume dependency is now mutual, but only declared in one direction — and the other file actively denies it**
`gece-oturum-durumu-2026-08-02.md` now lists Işık/Volume as a dependency. `isik-volume-durum-sistemi.md`'s dependents list doesn't include Gece/Oturum, and — worse — a sentence in that same file still reads: *"bağımlılık yönü ters: Işık/Volume Durum Sistemi ona bağımlı, o buna değil"* — directly contradicted by the fix just applied three sections earlier in the same document. `systems-index.md` was not touched; its "clean DAG" claim is now inaccurate (the cycle is event-decoupled and harmless at runtime, but the index should say so, not deny the cycle exists).

🔴 **N4 — Two of the three movement-lock consumers were never updated, and the new default (`Full`) breaks their own Acceptance Criteria**
`asansor-kat-erisim-sistemi.md` and `etkilesim-sistemi.md` both still call `RequestMovementLock(this)` with no scope argument in their Core Rules, state tables, and Interactions sections. The parameter's new default is `MovementLockScope.Full` (freezes `Look`). Both documents' own existing ACs require `Look` to stay free — Asansör's AC#13 explicitly, Etkileşim's Hold-cancel path implicitly. Taken literally, both ACs now fail against the very fix meant to give them what they needed.
→ Either flip the default to `MoveOnly` (only Sahne Kesmeli Anlatı actually wants `Full`, and it already passes the argument explicitly), or update every call site in both consumer documents.

🔴 **N5 — [Confirmed independently by both the design-theory and scenario passes] The saturation ending has no event to evaluate on, and can never fire for the exact player it was built for**
The fix moved the saturation signal from Anlatı Durum's `OnClueKnown` event (explicitly event-driven by design) to Gece/Oturum's `FiredTriggerIds.Count` — but Gece/Oturum Durumu publishes no events at all, and `IsFinalRoundActive` has no change notification either. Concretely: a player who commits every memory trigger in round 1–2 (the Explorer-type player the `IsFinalRoundActive` gate was specifically written to protect) has the saturation condition go false, and then **nothing ever re-evaluates it** when the final round later begins — the branch is dead in its own motivating scenario.
→ Add an `OnTriggerFired` event to Gece/Oturum Durumu and an `OnFinalRoundStarted`/`OnRoundChanged` event to Görev/Taşıma Döngüsü; state explicitly that the saturation condition re-evaluates on either.

🔴 **N6 — The HARD CUT sting fires on every ordinary elevator ride too, because the event it listens to doesn't distinguish SOFT from HARD swaps**
`seviye-sahne-gecisi.md`'s own contract is explicit that `OnTransitionStateChanged` is the single, undifferentiated signal for *both* transition types reaching `Swapping` — that's the whole point of "one state machine, two configs." Adaptif Ses's new HARD CUT Sting subscribes to that same signal with no way to tell which type occurred. Result: every one of the ~6–10 elevator rides per night now also triggers the cut sting and the "stop all ambiance instantly" rule — directly inverting Seviye/Sahne Geçişi's two-fantasy design (the calm "Beden Sürekliliği" ride becomes audibly identical to the "Bedenin Çalınması" theft), and diluting the one sound whose entire purpose was to mark the *rare, involuntary* cut as different from everything else.
→ Add the transition type to `OnTransitionStateChanged`'s payload (or expose a dedicated `OnHardCutSwapping` event), and gate both the CutSting and the abrupt-stop rule on it specifically.

🔴 **N7 — On the actual HARD CUT frame, the CutSting and the "stop all audio instantly" rule fire off the same event with no defined order**
Both are keyed on the same `Swapping` signal. The abrupt-stop rule (pre-existing) was never amended to exempt the new CutSting group, which sits outside the stinger pool specifically to avoid being swept up by it — but nothing states it's exempt, or what order the two handlers run in. If the stop-all handler runs after the CutSting's `PlayOneShot`, the one safeguard the whole fix exists to add gets silenced on the exact frame it's needed.
→ One sentence in `adaptif-ses-sistemi.md`: the CutSting group is exempt from the abrupt-stop rule.

🔴 **N8 — The co-residency tick-skip is undefined for a zone with an in-flight transition — the normal elevator case**
The new rule says a zone's tick is "skipped without sampling" when its scene isn't active — but doesn't say whether that also skips the `Shifting-In` progress accumulation and the eventual `Held` transition. A memory-trigger Hold completed just before boarding the elevator (explicitly called "the most ordinary path" in `ani-tetikleyici-etkilesim.md`) lands inside the 0.5–2s co-residency window on every reading: either the zone silently reaches `Held` in a scene the player has already left (reintroducing **B2**, the still-open inaudible-stinger bug, but now by construction rather than bad luck), or the transition freezes forever mid-progress.
→ State explicitly whether the skip is sampling-only or whole-tick, and define what happens to an in-flight transition when its scene deactivates.

---

## Original Blocking Issues Never Addressed (omitted from the 8 required actions)

🔴 **`ZoneChanged` still has no owning system.** No area-zone colliders for Depo/Servis Koridoru/Balo Salonu are defined anywhere; Adaptif Ses's ambient crossfade — its entire ambiance layer — has no trigger source.

🔴 **The stinger and the light are still 2–5 seconds apart**, contradicting three documents' explicit "compound effect" language (`systems-index.md`, `isik-volume-durum-sistemi.md`, `adaptif-ses-sistemi.md`). The same "Persistent shifts always reach Held, so mark it early" reasoning that fixed the `PersistentShiftIds` timing gap (Action 3) was never applied here, despite being directly applicable.

Both were correctly identified in the original report's blocking list but did not make it into the 8-item required-actions summary carried into the fix pass — a real gap in how that handoff was scoped.

---

## Confirmed Still Open (not targeted by this fix pass, as expected)

- **B2** — the memory-trigger stinger's `HeldSessionAlreadyPlayed` barrier still burns on playback *attempt*, not audibility. **N8 makes this reachable on a normal path, not just an edge case.**
- **B6** — player/FPC object lifetime across a scene swap is still unspecified everywhere; the carry-slot visual pool still doesn't re-sync after an elevator ride. The W4 movement-lock fix adds a third symptom (lock-requester identity across a possible re-instantiation).
- All prior Warning-tier items not in the 8 required actions (approach-taper camouflage defeated by the crosshair, elevator dead-time budget, anti-pillar composition risk, Pillar 1 UI-exemption wording, zero-clue playtest measurement) remain exactly as before.

---

## New Warnings (selected — see full detail in the three verification passes)

- **W-a/W-b** — The new `Full`-scope lock on the HARD CUT path doesn't actually stop player velocity before the zero-frame swap (FPC's own deceleration ramp still applies), and releasing it in `onComplete` hands control back to the player inside an unowned psychiatry-scene control model.
- **W-c** — Gece/Oturum's new event subscription has no specified timing (risk of missing early `Shifting-In` events), unlike Anlatı Durum's own explicit fix for the identical problem.
- **W-e** — `onFailed` on a HARD CUT can leave `HasTriggeredThisNight` permanently `true` with no clear retry path, potentially making a night unfinishable.
- **W-f** — A `Full` lock acquired mid-Hold (scope escalation) silently disables Etkileşim's own Hold-cancel path, which depends on `Look` staying free.
- **W-g** — The `ManualOnly` validator has no defined way to resolve "which zone does this `MemoryTriggerDef` bind to" — the mechanism named (asset scan) can't reach scene objects.
- Multiple stale reciprocal-dependency entries left behind by the fixes (Anlatı Durum still claims Sahne Kesmeli Anlatı as a dependent; `systems-index.md` reflects none of today's changes).

---

## Verdict: FAIL (unchanged from the prior review)

The design decisions underlying the fixes were sound where verified (both agents that could independently assess it — design-theory and scenario — confirmed the `IsFinalRoundActive` gate and the `FiredTriggerIds` proxy switch were the *correct* calls). The failures are almost entirely **propagation gaps**: a change made in one document's Core Rules without walking every consumer document and `systems-index.md` to match.

### Recommended path forward

Given the pattern (two fix passes, each introducing nearly as many new issues as it closed), a third blind fix-everything pass risks the same outcome. Recommend a narrower, more disciplined approach:

1. Fix the propagation gaps first (N3, N4's consumer updates, I-a/I-b's stale references, `systems-index.md` sync) — these are mechanical, low-risk, high-value.
2. For genuinely new design questions (N1's audio radius, N5's missing events, N6's undifferentiated SOFT/HARD signal), treat each as its own small decision rather than batching — these interact with each other (e.g., N6's event-type payload addition could also solve part of N5's signal problem if Görev/Taşıma's round-change also got an event).
3. Leave `ZoneChanged` ownership and the stinger/light timing gap as explicitly deferred, tracked items — they were never in scope for this fix pass and shouldn't be added under the same time pressure that produced today's propagation gaps.
