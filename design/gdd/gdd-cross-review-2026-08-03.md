# Cross-GDD Review Report

**Date**: 2026-08-03
**GDDs Reviewed**: 12 (9 Full GDDs + 3 Quick Specs), plus `game-concept.md` and `systems-index.md` for context
**Systems Covered**: Birinci Şahıs Kontrolcü, Işık/Volume Durum Sistemi, Anlatı Durum/İpucu Takibi, Gece/Oturum Durumu, Seviye/Sahne Geçişi, Adaptif Ses Sistemi, Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Diyalog/Anlatı İçeriği, Görev/Taşıma Döngüsü, Anı-Tetikleyici Etkileşim, Sahne Kesmeli Anlatı

**Method note**: `design/registry/entities.yaml` does not exist in this project — all checks below are from full document reads, not a registry baseline. All 9 Full GDDs had individually passed `/design-review` earlier in this same session (several revised same-day, 2026-08-03) before this holistic pass began.

---

## How to read this report

Three independent review passes were run in parallel against the same 12 documents: **Consistency** (contradictions/stale references/ownership conflicts), **Design Theory** (game-design holism), and **Scenario Walkthrough** (player-perspective multi-system chains). Several findings were reached independently by two or all three passes from different angles — these are marked **[CONFIRMED ×2]** / **[CONFIRMED ×3]** and should be weighted as the strongest signal in this report: when independent methods converge on the same fault line, that fault line is real.

---

## Consistency Issues

### Blocking (must resolve before architecture begins)

🔴 **[CONFIRMED ×3 — Consistency, Scenario B3, Design Theory Blocking #2] The HARD CUT sting has no owner in either direction**

`seviye-sahne-gecisi.md` (Visual/Audio Requirements): "HARD CUT'ın kendi ses gereksinimi (ör. kesmeyle eşleşen bir sting) Adaptif Ses Sistemi'nin `OnTransitionStateChanged(Swapping)`'e abone olmasıyla sağlanacak — bu GDD'nin kapsamında değil." Its own Open Questions escalate this to a named risk: *"Şu an 'çalınma, hata değil' okumasının **tek** taşıyıcısı Adaptif Ses'in senkronize sting'i — ses gecikirse/çalmazsa hiçbir yedek sinyal yok."*

`adaptif-ses-sistemi.md` never subscribes to `OnTransitionStateChanged` (the identifier appears nowhere in that file), defines no cut sting, assigns it no mixer group, sets no RMS ceiling for it, and does not list Seviye/Sahne Geçişi as a dependency. Its only HARD CUT behavior is to go silent (Edge Case: stop all ambiance/stingers instantly). Reciprocally, `seviye-sahne-gecisi.md`'s "Kendisine bağımlı olanlar" list omits Adaptif Ses entirely, despite Adaptif Ses's own Edge Cases already depending on knowing when a cut occurs.

**Design-theory consequence**: assessed against the anti-pillar "NOT ucuz jump-scare'ler," a zero-frame, unannounced audiovisual discontinuity at a moment the player didn't choose is structurally identical to a jump-scare stimulus. Every guardrail Adaptif Ses built against startle (duration cap, no riser, timbre-not-level contrast, brickwall limiter, per-area RMS ceiling) is scoped explicitly to the memory-trigger stinger — none of it covers a cut sting that doesn't exist in that document. The doc's proposed secondary safeguard (target scene opens mid-sentence, pre-framed camera) is also unowned — `sahne-kesmeli-anlati-2026-08-02.md` states it has no diyalog/ses/görsel logic of its own, and `diyalog-anlati-icerigi-2026-08-02.md` scopes itself to callback selection only.

**For contrast**: the memory-trigger stinger itself is clean and should not be revised — it fires after a ~3s zero-derivative-onset smoothstep, following a player-initiated 0.6–1.5s hold. Slow, self-initiated, capped. The HARD CUT sting is the opposite case entirely.

→ Add the cut sting to `adaptif-ses-sistemi.md` as a first-class element: subscription to `OnTransitionStateChanged(Swapping)`, its own mixer routing/RMS ceiling/limiter (reuse the stinger's template), an AC asserting frame-sync with the swap, and the Seviye/Sahne Geçişi dependency in both directions. Separately assign the mid-sentence-opening requirement to a named owner (Sahne Kesmeli Anlatı or the writer brief).

---

🔴 **[CONFIRMED ×2 — Consistency, Scenario B1] Proximity zone entry can auto-fire a memory-shift before the player ever completes the Hold — nullifying the entire mechanic's consent premise**

`isik-volume-durum-sistemi.md` AC#2: *"GIVEN R_trigger=4m … WHEN oyuncu 4m içine girer, THEN bölge Shifting-In'e sonra Held'e geçer"* — its worked examples use R_trigger values of 4m and 12m, and Core Rules frame radius/hysteresis entry as a first-class, always-on trigger mechanism. `etkilesim-sistemi.md` requires the player within a 2.0m SphereCast to even begin a Hold. For any realistic trigger radius, **the player is already inside R_trigger before the Hold can start** — the shift auto-fires environmentally, before consent.

`ShiftConfig` (owned by `isik-volume-durum-sistemi.md`) has no per-zone field to disable proximity auto-trigger — no "API-only, manual trigger" mode exists anywhere in the contract.

Consequence, walked through by the scenario agent: the stinger plays, the clue becomes Known, the light shift completes — all before the Hold. When `OnHoldComplete()` then calls `TriggerShift`, it returns `false` (already active, no-op) per `isik-volume-durum-sistemi.md`'s own contract, and `ani-tetikleyici-etkilesim.md` explicitly does not check that return value — the object still goes **Committed**. The player experiences exactly the failure state that GDD reserves for an edit-time-validation bypass ("oyuncu 'başarılı' bir tutma deneyimler ama dünyada hiçbir şey değişmez"), reached through the *normal* path, on every single memory trigger in the game. Because `Persistent=true` is a locked invariant, this is irreversible. This destroys `ani-tetikleyici-etkilesim.md`'s entire Player Fantasy: *"Fantezi keşfetmek değil — suçortaklığıdır... bunu ben, bile bile yaptım."*

→ `isik-volume-durum-sistemi.md` needs an explicit "manual-trigger-only, no radius auto-entry" mode on `ShiftConfig` (note: `R_trigger`/`R_exit` can't simply be removed, since `R_exit` derives from `R_trigger`, though Persistent shifts skip exit checks anyway) — locked by the same `IPreprocessBuildWithReport` validator `ani-tetikleyici-etkilesim.md` already specifies for `Persistent != true`.

---

🔴 **[CONFIRMED ×2 — Consistency, Scenario B4] `PersistentShiftIds` has no defined writer, and two differently-timed persistence records now exist for the same fact**

`gece-oturum-durumu-2026-08-02.md` AC#3 implies it subscribes to `OnShiftStateChanged` to populate `PersistentShiftIds` — but has no Dependencies section declaring that subscription. `isik-volume-durum-sistemi.md` Dependencies states the opposite unambiguously: *"Işık/Volume Durum Sistemi bu bilgiyi sadece **okur**; session state'i kendisi yönetmez ya da yazmaz."* No system anywhere is assigned to write it, yet `isik-volume-durum-sistemi.md`'s own AC#17 (Persistent restore on scene load) reads it and can never pass as specified.

**Compounding, introduced by this session's own revisions**: `ani-tetikleyici-etkilesim.md`'s 2026-08-03 fix added Committed-restore from `FiredTriggerIds`, written at `OnHoldComplete()` (Shifting-In start). `isik-volume-durum-sistemi.md`'s restore reads `PersistentShiftIds`, written (per its own doc) at Held — ~3 seconds later. Two records of "this memory happened," written by different systems at different moments, reconciled nowhere. A reload in that 3-second window (elevator ride timed unluckily, or a queued HARD CUT) produces split state: the object restores Committed (can never be re-held) while the zone restores Dormant. The shift, stinger, and clue become permanently unreachable on the game's central mechanic.

→ Assign a single writer for `PersistentShiftIds` (most natural: Gece/Oturum Durumu subscribes directly, matching its own AC's implication — but then declare that dependency explicitly in both directions, and update `systems-index.md`'s "clean DAG" claim if it becomes a two-way relationship). Reconcile the two persistence records' write timing, or merge them into one.

---

🔴 **Movement-lock has no scope parameter, but three consumers need three different scopes**

`birinci-sahis-kontrolcu.md`: *"Herhangi biri → Locked: … girdi donar; kamera dışarıdan sürülebilir"* — full input freeze, camera externally driven. `asansor-kat-erisim-sistemi.md`: *"sadece `Move` girdisini dondurur — `Look` serbest kalır"*. `etkilesim-sistemi.md`'s own documented Hold-cancel path (*"kamera çevrilip SphereCast kaybeder"*) requires Look to stay player-controlled while ITS lock is held. The lock's signature, `RequestMovementLock(object requester)`, has no scope parameter — three different consumers need three different behaviors from one bare identity call.

Secondary conflict on the same contract: FPC's reference-counting is justified specifically so Asansör/Sahne Kesmeli Anlatı/Etkileşim can hold the lock simultaneously — but `etkilesim-sistemi.md`'s own Edge Cases + AC#8 implement mutual exclusion on top of it (*"kilit BAŞKA bir sistem tarafından tutuluyorsa: Hold başlatılmaz"*), which requires distinguishing "locked by me" from "locked by someone else" — information `IPlayerState`'s read-only bool `MovementLocked` cannot provide.

→ FPC must define lock scope (Move-only / Move+Look / Move+Look+camera-authority) as a parameter, plus an ownership query beyond a bare bool.

---

🔴 **FPC's world-position-immutability claim during elevator lock is contradicted by Scene Transition's own Transform-copy mechanism**

`birinci-sahis-kontrolcu.md` AC#16: *"oyuncunun dünya pozisyonu `ReleaseMovementLock`'a kadar **değişmeden sabit kalır**"*. `seviye-sahne-gecisi.md` Formulas: the SOFT handoff is an instant Transform copy to the target scene's `SoftTransitionAnchor`, explicitly cabin-*local*, not world-space — meaning world position is expected to change. This happens inside the locked window, before `ReleaseMovementLock`. Both ACs can only pass if both floors' cabins share identical world coordinates, which is exactly `asansor-kat-erisim-sistemi.md`'s still-open Question #1 (cabin ownership, reassigned 2026-08-03 to level-designer + technical-director, unresolved).

→ Narrow FPC AC#16 to "no platform-delta is applied by this system" rather than an absolute world-position invariant.

---

🔴 **`ZoneChanged` — the entire ambiance-layer trigger — is owned by no system**

`adaptif-ses-sistemi.md` fires its ambient crossfade off `ZoneChanged`, but this identifier appears in no other document in the project. It's not Işık/Volume (different concept: per-`shiftId` zones), not Seviye/Sahne Geçişi (scenes, not zones), not Etkileşim, not Asansör. No area-zone colliders for Depo/Servis Koridoru/Balo Salonu are defined anywhere, and Adaptif Ses's Dependencies section names no provider.

→ Either Adaptif Ses claims ownership of the area-zone colliders directly, or a providing system must be named, before this can be architected.

---

🔴 **Stinger and light are specified 2–5 seconds apart, contradicting every document's "compound effect" rationale**

`isik-volume-durum-sistemi.md`: `Shifting-In` runs ~3s (Tuning Knob range 2–5s) before `Held`. `adaptif-ses-sistemi.md` and `anlati-durum-ipucu-takibi.md` both filter `if (newState != Held) return` — the stinger and the clue-known event fire only *after* the visual shift has fully completed and settled, not alongside it. This contradicts `systems-index.md`'s own Overview claim (*"the compound light+sound effect — not lighting alone — is what carries Pillar 2"*), both systems' own Player Fantasy sections, and `ani-tetikleyici-etkilesim.md`'s Open Questions, which quantifies the gap as a *"kısa bir 'sessiz boşluk'"* — actually the full 2–5s Duration, not short.

→ Either fire the stinger on `Shifting-In` instead of `Held` (changing two subscribers' event filter), or restate all three documents' "compound effect" language as sequential, not simultaneous.

### Warnings (should resolve, but won't block)

⚠️ `systems-index.md`'s own Dependency Map contradicts its own Systems Enumeration table on 4 of its 6 recently-touched rows (Anlatı Durum, Adaptif Ses, Sahne Kesmeli Anlatı listed inconsistently between the two sections of the same file).

⚠️ Görev/Taşıma Döngüsü's two dependencies added 2026-08-02 (Seviye/Sahne Geçişi, Adaptif Ses Sistemi) were never reciprocated in either target GDD or in `systems-index.md`.

⚠️ `birinci-sahis-kontrolcu.md`'s Dependencies section is stale on four counts: says Asansör "henüz tasarlanmadı" (it's designed), repeats a platform-delta-injection requirement its own Edge Case retracted (and that retracted claim was then propagated forward into `seviye-sahne-gecisi.md`'s Core Rules on 2026-08-03), never lists Işık/Volume as a dependent despite that GDD declaring a partial dependency on FPC.

⚠️ `OnShiftStateChanged`'s `radius` parameter is never defined as `R_trigger` or `R_exit` specifically; Adaptif Ses's new `stinger_falloff` formula (added 2026-08-03) assumes `R_exit` — a 15% falloff-radius ambiguity at default tuning.

⚠️ 1.6 m/s is a tunable (1.2–2.0 m/s range) in its owning GDD (FPC) but hardcoded as "locked" in two downstream formulas (Işık/Volume's Box Collider Safety Margin, Adaptif Ses's footstep_volume) — raising the knob silently breaks both.

⚠️ Görev/Taşıma Döngüsü routes SFX to an "SFX mixer group" with ducking rules that Adaptif Ses Sistemi doesn't define and explicitly rejects as a design principle (only "Ambiance" and "Stinger" groups exist; no runtime ducking by design).

⚠️ `gece-oturum-durumu-2026-08-02.md` cites the wrong AC number in `isik-volume-durum-sistemi.md` (says AC#14, means AC#17); that file's own Blocked-AC table is stale in the other direction (still says Gece/Oturum "henüz tasarlanmadı").

⚠️ `sahne-kesmeli-anlati-2026-08-02.md` carries two stale "GDD Update Required" markers for APIs (`IsFinalRoundActive`, `EndSession()`) that were delivered the same day, per `systems-index.md`'s own changelog.

⚠️ `TotalConfiguredClueCountForNight`, consumed by Sahne Kesmeli Anlatı, has no owning system or data field anywhere in `anlati-durum-ipucu-takibi.md` (harmless at MVP's 1-night scope, but unowned).

⚠️ `OnHoldBlocked()` is required by two implementers (`etkilesim-sistemi.md`'s own Edge Cases/AC#8, consumed conceptually by Anı-Tetikleyici) but absent from the published `IInteractable` interface code block.

⚠️ An Open Question was formally reassigned to `diyalog-anlati-icerigi-2026-08-02.md` (persistence plan for `UsedCallbackIds`) but that document never addresses it. Same pattern, smaller: footstep/jostle asset-sourcing questions assigned to Adaptif Ses from two other GDDs, neither tracked there.

⚠️ `systems-index.md`'s High-Risk table still prescribes a `MemoryTriggerEvent` contract that `ani-tetikleyici-etkilesim.md` deliberately built the opposite way (direct API call + decoupled event reuse instead).

⚠️ Status fields disagree: `systems-index.md`'s Next Steps claims "all 9 Full GDDs Approved" while its own Progress Tracker still says 6 approved, and four individual GDD headers still read "In Design" despite the index marking them Approved/Designed.

⚠️ Görev/Taşıma Döngüsü's session-pause Edge Case assumes a session resume path that `gece-oturum-durumu-2026-08-02.md` explicitly states doesn't exist in MVP ("tek gece, oturum bir kez başlar bir kez biter").

⚠️ Footstep sample count/pitch variance is authored identically in two documents (FPC, Adaptif Ses) with neither claiming ownership — a tuning change in one leaves the other stale.

---

## Game Design Issues

### Blocking

🔴 **[Relates to Scenario B7] The night's OR end-condition can silently truncate the core loop MVP exists to validate**

`sahne-kesmeli-anlati-2026-08-02.md` ends the night on whichever fires first: task completion, or memory-trigger saturation (`GetKnownClueIds().Count == TotalConfiguredClueCountForNight`). At MVP's authored content (2–3 triggers, 3–5 carry rounds), a player who finds triggers early ends the night during round 1–2. This directly contradicts: Görev/Taşıma Döngüsü's own Player Fantasy, which is explicitly *cumulative* across rounds (a round-indexed prominence curve that never leaves its first third if cut short); `game-concept.md`'s Core Loop promise of "3-5 taşıma turu, turlar ilerledikçe ortam gerilimi birikir"; and the game's own stated primary Bartle type (Explorers) and #2 aesthetic (Discovery) — the target player is precisely the one most likely to trigger the early ending, with no in-fiction signal that finding the last memory object ends the session. It's also a scope mismatch: using `KnownClueIds.Count` as a *termination* signal re-instruments a set `anlati-durum-ipucu-takibi.md` explicitly designed to avoid feeling like a collectible ("sistem ödüllendirmez, tanıklık eder").

→ Decide explicitly what the saturation ending is for and document it. Cheapest fix: drop it for MVP (task completion only); next cheapest: gate it behind `IsFinalRoundActive` (already exists on the Task/Carry interface) so saturation can't end the night before the final round.

---

🔴 **(Same finding as Consistency's HARD CUT ownership gap above — see that entry for full detail.)** Re-flagged here because the design-theory framing (anti-pillar violation risk) is a distinct enough angle to warrant its own line in the GDDs-flagged table.

---

🔴 **`MaxCallbacksPerScene = 2` silently discards the third clue at MVP's own authored content, and the spec's own claim that this can't happen is false**

`diyalog-anlati-icerigi-2026-08-02.md` claims the cap "pratikte MVP'yi kısıtlamaz" because candidate count is "zaten kapasitenin altında" — but MVP is authored at up to 3 triggers (`game-concept.md`'s own upper bound) against a cap of 2, and MVP has exactly one psychiatry scene, so the overflow rule ("atlanan aday sonraki bir sahnede tekrar aday olabilir") has nowhere to defer to. The player who found all three triggers — the most-engaged player, the one `anlati-durum-ipucu-takibi.md`'s "Otel Unutmuyor" fantasy is built for — is the one guaranteed to lose a callback with zero observable output, forever.

→ One-number fix, but pick deliberately: either raise the MVP cap to 3, or author exactly 2 triggers for MVP. Then correct the false claim and its AC, and add a build-time check that the cap covers the authored clue count whenever only one scene exists.

### Warnings

⚠️ **Approach-slowdown camouflage is defeated by the crosshair, and its precondition (decoy density) has no owner.** `Hold` is currently a 1:1 signature for memory triggers — the only `IInteractable` in the game using it — so the moment the crosshair resolves at 2.0m (outside the 1.5m taper radius), the object is identified with certainty before the ambiguous taper signal even engages. Cheapest fix: give at least one ordinary prop a Hold interaction (a stuck drawer, a jammed door) — costs nothing, serves Pillar 3 directly.

⚠️ **Tension escalation runs on two channels that never meet** — round-indexed prominence dimming (hand-held prop only) and accumulated Persistent shifts (world state, fully elective) — with no coupling between them. A player who triggers everything early has a saturated world with an early-night prop signal; a player who triggers nothing gets zero environmental escalation all night despite `game-concept.md`'s explicit promise that it builds. Recommend a single round-indexed ambient parameter (Adaptif Ses's existing smoothstep convention) as the cheapest coupling.

⚠️ **The system that decides how the night ends has no Player Fantasy and a wider-than-intended trigger.** `sahne-kesmeli-anlati-2026-08-02.md` owns the single most pillar-loaded decision in the game with no stated feeling for either ending, and its saturation proxy (`KnownClueIds.Count`) can be advanced by passive/environmental shifts or direct `MarkClueKnown` calls — not only deliberate player Holds — so the night can end from a shift the player never chose.

⚠️ **Anti-pillar risk: every memory mechanic is one-directional degradation, with no counter-signal in MVP.** `Persistent=true` is locked, `RevertShift` is never called, shifts only get colder/dimmer, and the CD-GDD-ALIGN note explicitly forecloses a future "revert some shifts" option. Nothing in MVP's mechanical vocabulary offers a warm, ambivalent, or complicating memory — and Pillar 4 (the natural counterweight) has zero MVP surface by design. Not a violation of the anti-pillar's letter (no character is ever mechanically depicted), but a composition-level risk worth a conscious design call — e.g. resolve the still-open `MemoryColor` (blue vs. sodium-green) selection question using the anti-pillar as the deciding criterion.

⚠️ **The loop manufactures attention surplus with nowhere in MVP to spend it.** Görev/Taşıma Döngüsü's own Player Fantasy states its function is producing spare attention for "the hotel itself" to receive — but MVP's environmental storytelling has no owning system anywhere in `systems-index.md`'s 17 entries, and Anı-Tetikleyici explicitly defers Discovery to "other moments" that don't exist in MVP. `/art-bible` hasn't been run yet — natural place to scope this.

⚠️ **Elevator dead time may consume 13–25% of a 15-20 minute MVP session, tuned independently by two documents with no shared budget.** Summing Asansör's and Seviye/Sahne Geçişi's knobs: ~12-26s per one-way trip, doubled round-trip, across 3-5 rounds. Each knob is individually "safe" per its own doc; the sum has no owner. Recommend a target per-round wall-clock budget in Görev/Taşıma Döngüsü, referenced by both elevator-adjacent knob tables.

⚠️ **Pillar 1's UI exemption is worded more absolutely than practice follows.** Etkileşim's crosshair exemption argues from "diegetic vs. non-diegetic," but Asansör's button light is diegetic AND deliberately, perfectly reliable — contradicting the stated boundary even though the actual design intent (hotel machinery is trustworthy; Pillar 1 is about perception/narrative, not mechanical reliability) is sound. Cheap reword to scope the exemption correctly before a future system either over-generalizes or gets wrongly flagged.

⚠️ **A zero-clue MVP playthrough is correctly designed as "expected," but nothing in the playtest plan distinguishes it from a falsified hypothesis.** Recommend making trigger-encounter rate a required `/playtest-report` measurement with a stated read: low find-rate means "inconclusive on discoverability," not "the technique failed."

---

## Cross-System Scenario Issues

**Scenarios walked: 5** — (1) memory-trigger Hold to completion, (2) elevator call and ride, (3) HARD CUT mid-gameplay, (4) memory-trigger Held landing while the elevator is in transit, (5) final delivery ending the night.

### Blockers

🔴 **B2 — The once-per-session stinger barrier burns on playback *attempt*, not on audibility.** `HeldSessionAlreadyPlayed` (added 2026-08-03 to fix the reload-replay bug) is written the instant `Playing` starts and, for `Persistent=true` shifts, never clears. But the Held event landing while the player is sealed in the elevator cabin (outside the stinger's `maxDistance`), or on the same frame as a HARD CUT (which stops all stingers instantly), or during pool exhaustion (silently dropped) — all burn the barrier with the player never having heard anything. Since every memory trigger is Persistent, there is never a second chance. → Gate the set entry on the sound actually starting *and* being in audible range, or re-arm the stinger when Held fires during a movement-lock/transition/out-of-range window.

🔴 **B5 — A `Failed` SOFT transition has no handler in Asansör, and `Waiting` has no `Failed` exit — real softlock.** `seviye-sahne-gecisi.md`'s public `RequestSoftTransition` signature has no `onFailed` parameter, yet its own recently-added AC#11/#11a promise one exists and fires. Asansör's `Waiting` state has exactly two exits (`onComplete`, `OnSoftTransitionRejected`) — neither covers Failed. Either the doors open on a scene that never loaded, or the cabin sits in `Waiting` forever holding a movement lock that (per FPC's own contract) "oyuncu girdisiyle asla açılmaz." → Add `onFailed` to the interface signature; add a `Waiting → Failed` exit to Asansör that releases the lock.

🔴 **B6 — Player/FPC object lifetime across a scene swap is unspecified everywhere, with a player-visible consequence.** Neither FPC nor Etkileşim states whether one persistent player object survives the swap or is re-instantiated — every other system in the project (Görev/Taşıma, Anlatı Durum, Adaptif Ses) states this explicitly for its own state; these two don't. Concretely breaks: movement-lock requester identity across the boundary, elevator state-machine continuity if cabins are per-floor instances (per that GDD's own still-open ownership question), and — most visibly — Görev/Taşıma's carry-slot visual pool, which is scene-local while the carried-item *count* is a persistent service value; nothing re-syncs the new scene's slot visuals to that count after an elevator ride, so hands can read empty while items are still held, with zero fallback UI since the rig IS the only slot indicator by design.

🔴 **B7 — Sahne Kesmeli Anlatı's saturation signal is specified as "all memory triggers Committed" but implemented as "total known clues," and these are different sets.** (Technical mechanism behind the Game-Design "OR end-condition" blocker above.) `anlati-durum-ipucu-takibi.md` advances `KnownClueIds` on *any* Held transition — including passive/environmental ones (see B1 above) — and via direct `MarkClueKnown` calls the contract explicitly permits without validation. A single unreachable `shiftId` (its own orphan-check is non-build-blocking by design) can also make the count permanently unreachable, silently killing the saturation path entirely in the other direction.

🔴 **B8 — Origin-scene trigger zones keep ticking against the player's new position during the 0.5–2s additive co-residency window, and can permanently corrupt persistent state.** The 2026-08-03 fix that decoupled `Swapping` from delayed `UnloadSceneAsync` (correct, and necessary for the zero-frame guarantee) created a window where both scenes are resident with no coordinate separation beyond the cabin-local anchor offset. Işık/Volume's per-zone ticker doesn't know to stop sampling for a scene that's no longer active — a spurious `OnShiftStateChanged` in this window writes to `SeenShiftIds` (permanent, never clears) and `HeldSessionAlreadyPlayed` (permanent for Persistent shifts) for a shift the player never actually experienced. Işık/Volume's AC#18 only covers the teleport-*out* half of this class of bug, never teleport-*into* a foreign zone.

### Warnings

⚠️ W1 — `Interact` is read directly by both Etkileşim and Asansör with no arbitration; a player Focused on an object inside the elevator's call radius can trigger both actions on one press.
⚠️ W2 — The completion frame's state transition (Focused vs. Idle after `CanInteract` goes false) is unspecified, risking a one-frame prompt flash on the game's most carefully-authored silent moment.
⚠️ W3 — Asansör's `OnSoftTransitionRejected` handling describes a mid-flight interruption scenario that Seviye/Sahne Geçişi's own contract rules out; its AC#10 tests an unreachable path.
⚠️ W4 — The HARD CUT path has no movement lock anywhere, and — per Scenario 5 — is *guaranteed* to fire while the player is mid-stride (delivery triggers on walking into the drop-off zone), inverting Scene Transition's own "Bedenin Çalınması" (stolen mid-motion) fantasy into "walked myself there."
⚠️ W5 — A HARD CUT can fire while the player is mid-Hold on an unrelated object; Etkileşim defines no cancel trigger for this, and its existing destroy-based self-heal is late by up to 2 seconds (tied to B6's unspecified lifetime).
⚠️ W6 — Task-loop SFX and the (undefined) psychiatry-scene ambiance have no stated behavior under the HARD CUT abrupt-stop rule.

### Info
ℹ️ I1 — The two persistent barriers (`HeldSessionAlreadyPlayed`, `SeenShiftIds`) actually agree with each other and are correctly asymmetric by design — not a bug.
ℹ️ I2 — Adaptif Ses's AC#7 and AC#6b prescribe different (compatible, but easy to misread in isolation) actions for the same non-Held event.
ℹ️ I3 — Subscriber ordering on `OnShiftStateChanged` across Adaptif Ses / Anlatı Durum / (whatever eventually writes `PersistentShiftIds`) is unspecified; matters only for whether the stinger starts before or after a saturating Held triggers `RequestHardCut`.
ℹ️ I4 — The elevator button and drop-off zone are deliberately exempt from the FPC taper camouflage (not `IInteractable`) — benign, but worth noting the camouflage isn't literally universal.

---

## GDDs Flagged for Revision

| GDD | Reason | Type | Priority |
|---|---|---|---|
| `adaptif-ses-sistemi.md` | No HARD CUT sting, no `OnTransitionStateChanged` subscription, no `ZoneChanged` ownership, no writer contract for `speed`↔FPC | Consistency + Design Theory | **Blocking** |
| `seviye-sahne-gecisi.md` | Missing `onFailed` param on `RequestSoftTransition`'s public signature; no dependency link to Adaptif Ses | Consistency + Scenario | **Blocking** |
| `isik-volume-durum-sistemi.md` | No manual-trigger-only mode on `ShiftConfig`; `radius` param ambiguous (`R_trigger` vs `R_exit`); AC#18-class bug for teleport-*into* a zone | Consistency + Scenario | **Blocking** |
| `ani-tetikleyici-etkilesim.md` | Doesn't check `TriggerShift`'s return value; entire consent premise depends on the unfixed proximity-auto-trigger bug | Consistency + Scenario | **Blocking** |
| `gece-oturum-durumu-2026-08-02.md` | No Dependencies section; unclear/unassigned writer for `PersistentShiftIds`; stale AC cross-reference (#14 should be #17) | Consistency | **Blocking** |
| `birinci-sahis-kontrolcu.md` | `RequestMovementLock` has no scope parameter; AC#16's world-position invariant overclaims; stale Dependencies section (4 counts) | Consistency | **Blocking** |
| `asansor-kat-erisim-sistemi.md` | No `Failed`-transition handler in `Waiting` (real softlock risk); `OnSoftTransitionRejected` handling targets an unreachable scenario | Scenario | **Blocking** |
| `sahne-kesmeli-anlati-2026-08-02.md` | OR end-condition can truncate the core loop; saturation proxy measures the wrong set; no Player Fantasy for the system that ends the night; no movement lock on HARD CUT path | Design Theory + Scenario | **Blocking** |
| `diyalog-anlati-icerigi-2026-08-02.md` | `MaxCallbacksPerScene=2` silently drops MVP's 3rd clue; the doc's own claim that this can't happen is false | Design Theory | **Blocking** |
| `etkilesim-sistemi.md` | `OnHoldBlocked()` missing from published interface; `Interact` input has no arbitration with Asansör; Pillar 1 exemption overclaims | Consistency + Design Theory | Warning |
| `gorev-tasima-dongusu.md` | Two 2026-08-02 dependencies never reciprocated; carry-slot visual pool doesn't re-sync across scene swap (B6) | Consistency + Scenario | Warning |
| `systems-index.md` | Internal contradiction between Dependency Map and Systems Enumeration table; stale status counts; stale `MemoryTriggerEvent` prescription | Consistency | Warning |

---

## Verdict: FAIL

Nine documents carry at least one blocking finding; three findings were independently reached by two or three separate review methods (the HARD CUT sting ownership gap by all three; proximity auto-trigger and `PersistentShiftIds` ownership by two each). These are not stylistic nitpicks — they include a real softlock path (Asansör `Failed` handling), a mechanic that can silently defeat its own core premise on every playthrough (proximity auto-trigger), and a shared safety system with no implementer (HARD CUT sting, the sole safeguard against the game's most deliberate anti-pillar risk).

### Required actions before re-running

1. Close the HARD CUT sting ownership gap in `adaptif-ses-sistemi.md` (add subscription, mixer routing, RMS ceiling, AC) and `seviye-sahne-gecisi.md` (reciprocal dependency).
2. Add a manual-trigger-only mode to `isik-volume-durum-sistemi.md`'s `ShiftConfig`, locked by edit-time validation, and stop `ani-tetikleyici-etkilesim.md` from ignoring `TriggerShift`'s return value.
3. Assign a single writer for `PersistentShiftIds` and reconcile its write-timing against `ani-tetikleyici-etkilesim.md`'s `FiredTriggerIds`.
4. Add `onFailed` to `RequestSoftTransition`'s signature and a `Waiting → Failed` exit in `asansor-kat-erisim-sistemi.md`.
5. Define movement-lock scope as an explicit parameter in `birinci-sahis-kontrolcu.md`.
6. Resolve the Sahne Kesmeli Anlatı OR-end-condition design question (drop it, or gate it on `IsFinalRoundActive`) and fix the saturation proxy to count Committed triggers, not raw clue count.
7. Fix `MaxCallbacksPerScene` vs. MVP's authored clue count in `diyalog-anlati-icerigi-2026-08-02.md`.
8. Address the additive co-residency window (B8) — either deactivate origin-scene zone ticking on `Swapping`, or accept and document the risk window explicitly.

Individually re-running `/design-review` on each flagged GDD after these fixes is recommended over a blind full re-run of this skill, given the scope of what's already been mapped here.
