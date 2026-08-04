# Cross-GDD Review Report — Full Re-Verification
Date: 2026-08-04
GDDs Reviewed: 14 (12 system docs + game-concept.md + systems-index.md)
Systems Covered: Birinci Şahıs Kontrolcü, Işık/Volume Durum Sistemi, Anlatı Durum/İpucu Takibi, Gece/Oturum Durumu, Seviye/Sahne Geçişi, Adaptif Ses Sistemi, Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Diyalog/Anlatı İçeriği, Görev/Taşıma Döngüsü, Anı-Tetikleyici Etkileşim, Sahne Kesmeli Anlatı

**Context**: This is the real full parallel-agent re-run that was owed after the previous attempt hit the session's API usage limit mid-run (see `production/session-state/active.md`, "Targeted manual re-verification" entry). Both parallel passes (Phase 2 Consistency, Phase 3 Design Theory) were run as independent agents with no memory of prior review rounds, plus a Phase 4 scenario walkthrough done directly. All three lenses converged independently on the same top finding (#1 below), which is a strong signal it's real.

---

### Consistency Issues

#### Blocking (must resolve before architecture begins)

🔴 **`AmbientZoneVolume`'s initial-zone check can never fire — the co-residency guard and the "preload fully completes before Ready" rule cancel each other out**

Files: `adaptif-ses-sistemi.md` (Core Rules, "`ZoneChanged` sahipliği" / AC1b / AC1c) vs. `seviye-sahne-gecisi.md` (Core Rules, "SOFT'un gerçek tamamlanma garantisi" and "Preload tam tamamlanmalı, kısmi bırakılmamalı").

Seviye/Sahne Geçişi guarantees `Preloading → Ready` waits for full `LoadSceneAsync` completion with `allowSceneActivation=true`, explicitly so the target scene's `Start()` runs *before* `Swapping` changes the active scene. But Adaptif Ses's co-residency guard says an `AmbientZoneVolume`'s ticker "kendi sahnesi `SceneManager.GetActiveScene()` ile eşleşmediği sürece işlenmez." At the moment the target scene's `Start()` runs, the origin scene is still active — so the one-shot `Physics.OverlapSphere` initial-zone check is suppressed, and `Start()` never runs again. Nothing re-runs it after `SetActiveScene`. Net effect: after every elevator ride and every HARD CUT, ambience never (re)starts in the new scene — AC1b, AC1c, and AC13 cannot all pass. Işık/Volume's version of this guard is safe because its zones are per-frame tickers; the fix was copied onto a one-shot `Start()` mechanism where "skip this frame" semantics don't hold.

*Needs*: an explicit "re-run the initial overlap check on the frame this volume's scene becomes active" rule (or a deferred-until-active flag).

🔴 **Hold-fill ownership contradicts itself in three places; AC14 and AC14a cannot both pass, and AC14 has no valid test subject in MVP**

Files: `etkilesim-sistemi.md` (Core Rules, UI Requirements, AC14, AC14a) vs. `ani-tetikleyici-etkilesim.md` (Core Rules, Visual/Audio Requirements, UI Requirements).

- AC14 is written universally ("herhangi bir `Hold` tipi `IInteractable`... görülebilir şekilde ilerler"). AC14a says the opposite for `SuppressDefaultHoldFill=true`. Both cannot pass for the same object; AC14 needs an explicit `SuppressDefaultHoldFill==false` precondition.
- At MVP scope there is exactly one Hold interactable (Anı-Tetikleyici Etkileşim; carry items and decoys are `Instant`, elevator/drop-off are trigger zones) and it returns `true`. So AC14 is unsatisfiable in MVP content — the "0.6-1.5s with zero feedback" gap it was written to close is still 100% present in MVP.
- `ani-tetikleyici-etkilesim.md`'s **UI Requirements** still says it "olduğu gibi kullanır... bu artık gerçek bir sözleşme" — directly opposite its own Core Rules/Visual-Audio Requirements. `etkilesim-sistemi.md`'s own **UI Requirements** still says "bu gösterge *nesnenin* sorumluluğunda" — contradicting its own Core Rules, Visual/Audio Requirements, and AC14 in the same file.

🔴 **`systems-index.md`'s dependency graph has drifted from the GDDs again — including one Core→Feature inversion**

- Row 7 lists Etkileşim Sistemi's "Depends On" as including Anı-Tetikleyici Etkileşim. No document supports this: `etkilesim-sistemi.md` lists only FPC as a hard dependency and lists Anı-Tetikleyici as a *dependent*; the index's own Dependency Map (Core Layer, item 7) says "depends on: Birinci Şahıs Kontrolcü" only. As written the table row also inverts layer order (Core depending on Feature) — it appears to have been misused to flag a *contradiction found by review* rather than record a real dependency.
- Row 1 (FPC) still shows "—" for Depends On, even though `birinci-sahis-kontrolcu.md` and `etkilesim-sistemi.md` both now document the partial `InteractableRegistry` read. The index currently hides the project's one acknowledged layering exception.
- Rows 6/10 and Dependency Map items 6/10 don't record the new Adaptif Ses ↔ Görev/Taşıma Döngüsü link (both GDDs record it reciprocally); row 10 also omits Görev/Taşıma's soft dependency on Seviye/Sahne Geçişi.

🔴 **The `tension_gain` fix gives a Foundation-layer system an unflagged dependency on a Feature-layer system**

Files: `adaptif-ses-sistemi.md` (Dependencies) vs. `systems-index.md` (Adaptif Ses = Foundation item 6, Görev/Taşıma = Feature item 10).

Adaptif Ses now reads `CurrentRoundIndex`/`TotalRoundCount` from Görev/Taşıma Döngüsü — a Foundation→Feature read, two layers up. `birinci-sahis-kontrolcu.md`'s smaller Foundation→Core registry read is at least documented as an open architectural question; this larger one has no layering note anywhere, and the index records it in neither direction. Either the round counters need to move to a Foundation-owned host (Gece/Oturum Durumu is the obvious candidate), or this needs the same explicit open-decision treatment as the registry question.

🔴 **`tension_gain`/`Highlight` have an unguarded division-by-zero, and two ACs disagree on reachability; indexing convention also disagrees across the boundary**

Files: `adaptif-ses-sistemi.md` (Formulas, `tension_gain`) vs. `gorev-tasima-dongusu.md` (AC1, AC16, AC17, AC19, Visual Requirements `Highlight`).

`TensionGain(roundIndex)` and `Highlight(round)` both divide by `(TotalRoundCount − 1)`, undefined at `TotalRoundCount==1`. `gorev-tasima-dongusu.md` AC1 build-blocks any TaskList outside 3-5 rounds (implying 1-round lists can't exist), but AC17 explicitly specifies behavior for "if TaskList consists of a single round" — these two ACs cannot both hold. Neither formula has a guard, unlike every other formula in the project (`isik-volume-durum-sistemi.md`'s `TIME_EPSILON`/`RADIUS_EPSILON` convention). Separately, Core Rules and AC19 define `CurrentRoundIndex` as 0-based (`0,1,2,3`), but AC16 tests the jostle selector against "round index 1..roundCount" — a real off-by-one risk for an implementer following AC16.

#### Warnings (should resolve, but won't block)

⚠️ **Stale headers describing resolved items as open** — `ani-tetikleyici-etkilesim.md` (TriggerMode validation, Hold Player Fantasy contradiction — both resolved in-file), `gorev-tasima-dongusu.md` (OnFinalRoundStarted saturation gap, tension-escalation owner — both resolved in-file), `etkilesim-sistemi.md` Dependencies section (OnHoldProgress refusal marked "henüz çözülmedi" though resolved in its own Interactions section). Header-staleness is a recurring failure class in this project (three prior instances fixed) — back in three files.

⚠️ **Stinger caption/state-machine still keyed to `Held` after the timing fix** — `adaptif-ses-sistemi.md` Visual/Audio Requirements says the caption shows "`Held` durumunda stinger çalarken", but Persistent shifts now actually play on `Shifting-In` (~3s earlier); a caption gated on `Held` would appear ~3s late. AC14a is correctly written against `Playing`, so the requirement text disagrees with its own AC. The States/Transitions row is also stale (omits the Shifting-In+Persistent path).

⚠️ **Two remaining bare `RequestMovementLock(this)` calls default to `Full` and contradict Asansör's own AC13** — `seviye-sahne-gecisi.md` ("Kilit ile ilişki") and `asansor-kat-erisim-sistemi.md` (Dependencies) both still show the unparameterized call, which would freeze Look and directly contradict AC13 ("Look kamerayı serbestçe döndürür"). Same class as the two leftovers fixed in the 2026-08-03 propagation pass.

⚠️ **Compound-bypass edge case in `ani-tetikleyici-etkilesim.md` reasons from pre-`ManualOnly` behavior** — claims re-entering `R_trigger` re-triggers `TriggerShift`, but under `ManualOnly` the zone never self-triggers on radius entry and the object is already Committed. Whether exit hysteresis runs at all on a `ManualOnly`+`Persistent=false` zone is left undefined by `isik-volume-durum-sistemi.md`.

⚠️ **`walk_speed_unloaded=1.6` is locked in 2 docs' formulas but a tunable 1.2-2.0 range in a 3rd** — `adaptif-ses-sistemi.md` and `isik-volume-durum-sistemi.md` both treat 1.6 as locked (the former explicitly proves no-clip on that assumption); `birinci-sahis-kontrolcu.md` Tuning Knobs allows 1.2-2.0. Any playtest bump above 1.6 breaks the no-clip proof and undersizes every zone's box collider.

⚠️ **Diyalog/Anlatı İçeriği's dependents list contradicts two other documents** — states "Kendisine bağımlı olanlar: Yok" while `systems-index.md` and `sahne-kesmeli-anlati-2026-08-02.md` both list it as a (indirect) dependency of Sahne Kesmeli Anlatı.

⚠️ **`TotalConfiguredTriggerCountForNight` still has no owning system** — `sahne-kesmeli-anlati-2026-08-02.md`'s saturation condition depends on it; the admission that no system defines it is buried in a Tuning Knobs parenthetical rather than surfaced in Open Questions.

⚠️ **Minor: footstep spec duplication and registry lag** — footstep sample count/pitch specified identically in two docs with unstated ownership; `tension_gain`/`stinger_falloff` formulas not yet in `design/registry/entities.yaml` (process lag, not a design conflict).

---

### Game Design Issues

#### Blocking

🔴 **The saturation ending destroys the exact payoff that triggers it** *(see Cross-System Scenario Issues below — converges with a consistency-pass finding and the scenario walkthrough)*

🔴 **Two night-endings are specified to feel different; one identical mechanism delivers both**

Files: `sahne-kesmeli-anlati-2026-08-02.md` (Player Fantasy vs. Core Rules), `seviye-sahne-gecisi.md`, `adaptif-ses-sistemi.md`.

Sahne Kesmeli's Player Fantasy: task-completion = "sakin bir teslim anı" (calm handover); saturation = "dünya seni durdurur" (the world stops you). But Core Rules apply the identical sequence to both — `RequestMovementLock(Full)` → `RequestHardCut` → zero-frame swap → abrupt audio cut → CutSting, one shared `HardCutConfig`. The Full-lock rule exists *because* the completion signal fires mid-walk, meaning "torn from mid-motion" now applies to the calm ending too — widening rather than closing the gap between the stated fantasies. This also sharpens `seviye-sahne-gecisi.md`'s own open, playtest-gated startle-signature question: the risk is arguably *higher* on the task-completion path, since the fiction supplies no reason for a rupture there, while the saturation path at least has narrative justification.

*Fix direction*: either accept one mechanism and revise Sahne Kesmeli's Player Fantasy to stop claiming two distinct feelings, or differentiate — give task-completion a short (1-3 frame) fade and a softer/absent CutSting, reserving the zero-frame+Full-lock+CutSting treatment for saturation.

🔴 **At MVP content scope, Pillar 1 (Subjective Reality) has no guaranteed surface — a complete playthrough can contain zero subjective-reality shifts**

Files: `game-concept.md` (MVP Definition), `isik-volume-durum-sistemi.md` (TriggerMode), `ani-tetikleyici-etkilesim.md` (no visual distinction pre-touch), `birinci-sahis-kontrolcu.md` (camouflage), `etkilesim-sistemi.md` (2.0m aimed SphereCast only).

Every `MemoryTriggerDef`-linked zone is mandatorily `ManualOnly` (correctly, to protect consent) — but no MVP content assigns a single `Automatic` (passive/ambient) zone either, and no GDD or the index owns this as an MVP content requirement. Stack that with: memory triggers have zero visual distinction before touch, the approach-taper is deliberately made a meaningless signal (camouflage + decoys), there's no UI/map/hint anywhere, and there's no incentive to investigate given the no-fail design. Result: a player who just carries boxes completes a fully valid, fully intended playthrough having never seen the technique the MVP exists to validate — and a playtest of this build cannot distinguish "the technique didn't land" from "the technique never fired."

*Fix direction*: author 1-2 mandatory `Automatic` ambient shifts on the required carry route. The mode already exists in `isik-volume-durum-sistemi.md` for exactly this purpose ("pasif/çevresel bölgeler... anı-tetikleyici olmayan ortam kaymaları") — it just has zero assigned instances. This preserves Anı-Tetikleyici's distinction that its own shifts are the player-*initiated* subset.

#### Warnings

⚠️ **`game-concept.md`'s Competence/Mastery axis has been designed out of existence, not deferred** — the concept doc claims players learn to "read the hotel's language — distinguish real clue from decoy," but the camouflage mechanic and its 2026-08-04 decoy fix exist precisely so this discrimination is *impossible* ("oyuncu... ayırt edemez"). No system provides any learnable signal. Recommend retracting the language the same way the time-pressure/risk framing was retracted, rather than leaving a stated player-motivation claim actively contradicted by a locked mechanic.

⚠️ **`tension_gain` steps discretely at round boundaries, landing on the one moment the game deliberately gives zero confirmation** — the third ambient layer's gain jumps at the round-completion frame (up to ~0.35-0.48 of ceiling in one frame, no interpolation specified), which is also the delivery frame — deliberately zero-confirmation per `gorev-tasima-dongusu.md`'s Visual Requirements ("bir onay mikro-ödülü... çelişir"). An audible swell there reinstates the completion ping that document explicitly rejected.

⚠️ **Arithmetic error in the `tension_gain` worked example** — "Round 3: x=0.667, ease=0.630" should be **0.741** (verified: `x²(3-2x)` at x=0.667 = 0.741). Both `birinci-sahis-kontrolcu.md` and `isik-volume-durum-sistemi.md` compute the identical curve correctly elsewhere; this is an isolated error in the newest formula, and AC1d inherits it.

⚠️ **The three round-indexed curves (tension rising, prominence dimming, saturation gate) compose coherently — but the saturation path truncates the peak they build toward** — `tension_gain` and `Highlight` are the same idea on two channels and share convention cleanly (no fix needed there). But both peak only at the final round, and `HasCarriedInFinalRound` can end the saturation night shortly after that round's *first* pickup — so the most thorough player experiences the built-up peak for roughly one pickup's duration while the task-completion path delivers the full final round. Same underlying pattern as the top blocking finding. Consider gating saturation on final-round *progress* rather than first pickup.

⚠️ **The Hold-identity split holds rhetorically but conceded every mechanical lever to the physical layer** — linear (not eased) `t`, no per-object presentation curve used, `SuppressDefaultHoldFill=true`, and a HoldDuration sub-range (0.6-1.5s) *below* the midpoint of Etkileşim's general range — the shortest-weighted setting belongs to the interaction meant to feel weightiest. The meaning layer's fantasy asks for a *growing* quality with nothing left to grow along except unaided time perception at a duration close to `Instant`. Also: AC14 (universal Hold-fill) has zero applicable objects at MVP scope, reinforcing the consistency-pass finding above. *Not* flagging the stinger's unpredictable arrival as a jump-scare risk — it's well inside Anti-Pillar 1 given its other properties (short, RMS-capped, timbre-contrast, voluntary interaction) — but the HoldDuration question is worth the same playtest pass.

⚠️ **Anti-Pillar 2 risk in a surviving stinger content candidate** — the "unused phone notification tone" candidate, read against `game-concept.md`'s real source material (hypervigilance from a relationship with a BPD-diagnosed partner), risks coding "the partner making contact" as the dread payload — the exact monsterization the anti-pillar forbids, arriving through sound design rather than characterization. Not necessarily wrong, but the decision has never been made *as* an anti-pillar decision — recommend an explicit narrative-director/creative-director check when real asset selection happens.

⚠️ **"Otel Unutmuyor" narrative payoff is delegated to a document that never accepts the delegation** — `anlati-durum-ipucu-takibi.md` explicitly hands the *felt experience* of this fantasy to Diyalog/Anlatı İçeriği's own verification. But `diyalog-anlati-icerigi-2026-08-02.md` has no Player Fantasy section and all-mechanical ACs (candidate counts, ordering, validation) — nothing tests the delegated experience. The entire narrative payoff of the game sits in this ownership gap.

⚠️ **AC17's decoy floor of one is arithmetically insufficient for the camouflage claim it enforces** — with 2-3 triggers across 3 areas, the likely realized distribution is ~1 trigger : 1 decoy per area, a coin flip. Recommend expressing the requirement as a ratio (e.g. decoys ≥ 3× triggers per area) rather than a floor of one.

---

### Cross-System Scenario Issues

Scenarios walked: 2 (the highest-risk multi-system moment, and a lock-ordering sanity check)

#### Blockers

🔴 **Final-trigger-completes-the-night scenario — Etkileşim, Anı-Tetikleyici Etkileşim, Işık/Volume, Gece/Oturum Durumu, Adaptif Ses, Sahne Kesmeli Anlatı, Seviye/Sahne Geçişi**

Traced independently before seeing the parallel agents' results, and it converged exactly with both the consistency-pass "Saturation-triggered HARD CUT" warning and the design-theory-pass "saturation ending destroys the payoff" blocking finding — three independent lenses landing on the same root cause is a strong signal this is real, not a false positive.

Step-by-step: player completes the Hold on the final memory-trigger during the final round (having already carried something in that round). `OnHoldComplete()` calls `TriggerShift` (zone enters `Shifting-In`, stinger starts playing per AC6c) and writes `FiredTriggerIds` (firing `OnTriggerFired`) in the same frame. Sahne Kesmeli Anlatı re-evaluates saturation — all three conditions now true — and calls `RequestMovementLock(Full)` then `RequestHardCut` with no specified delay. Since the memory-trigger-side preload threshold already fired at `Count == Total-1`, the swap is zero-frame. Adaptif Ses's HARD CUT edge case then immediately silences the just-started stinger and abandons the light ramp at ~0% progress; Anlatı Durum never marks the clue Known (`Held` never arrives before the origin scene unloads). The player's one deliberate "bile bile yaptım" act is truncated at exactly the moment the docs promise a compound light+sound payoff, and the psychiatrist scene loses precisely its most recent callback.

**Fix must address all three symptoms at once** (destroyed audio/light payoff, dropped clue, guaranteed-short psychiatrist scene) — decoupling the saturation trigger from `OnTriggerFired` in favor of `OnShiftStateChanged(Held)` (or an explicit settle delay ≥ shift Duration) does this in one move.

#### Info

ℹ️ **Lock-ordering sanity check — Etkileşim, Sahne Kesmeli Anlatı, Birinci Şahıs Kontrolcü**

Traced the case where Etkileşim's `ReleaseMovementLock(this)` (on Hold completion) and Sahne Kesmeli Anlatı's `RequestMovementLock(this, Full)` (on the same frame's saturation trigger) could interleave. Because FPC's lock is reference-counted and both calls happen synchronously within the same method-call chain (no engine tick between them), there's no actual frame where movement is unlocked — this is safe as implemented. Noting it here only because this project's own convention is to spell out ordering explicitly rather than leave it implicit (as it does elsewhere for e.g. CutSting-vs-abrupt-stop ordering); not blocking, just worth a one-line Core Rules note for consistency with that convention.

---

### GDDs Flagged for Revision

| GDD | Reason | Type | Priority |
|-----|--------|------|----------|
| `adaptif-ses-sistemi.md` | AmbientZoneVolume re-arm bug; tension_gain layering violation + div-by-zero + arithmetic error; stinger caption still keyed to Held | Consistency + Design Theory | Blocking |
| `etkilesim-sistemi.md` | AC14/AC14a contradiction, unsatisfiable at MVP scope; stale UI Requirements ownership text | Consistency | Blocking |
| `ani-tetikleyici-etkilesim.md` | Stale UI Requirements text; stale header; compound-bypass edge case reasons from pre-ManualOnly behavior | Consistency | Blocking |
| `systems-index.md` | Dependency graph drift (Etkileşim/Anı-Tetikleyici row, FPC row, Adaptif Ses/Görev links) | Consistency | Blocking |
| `gorev-tasima-dongusu.md` | tension_gain div-by-zero contradiction (AC1 vs AC17); stale header | Consistency + Design Theory | Blocking |
| `sahne-kesmeli-anlati-2026-08-02.md` | Saturation-ending payoff destruction; two endings mechanically identical despite claimed different feel | Design Theory | Blocking |
| `game-concept.md` | No guaranteed Pillar 1 MVP exposure; Competence/Mastery claim contradicted by camouflage mechanic | Design Theory | Blocking |
| `isik-volume-durum-sistemi.md` | Owns the fix location for the missing Automatic-zone MVP content requirement | Design Theory | Warning |
| `birinci-sahis-kontrolcu.md` | AC17 decoy ratio too thin; walk_speed_unloaded tuning range conflicts with locked-constant assumption elsewhere | Design Theory | Warning |
| `diyalog-anlati-icerigi-2026-08-02.md` | Dependents-list contradiction; missing Player Fantasy/experiential AC for delegated narrative payoff | Consistency + Design Theory | Warning |
| `seviye-sahne-gecisi.md` | Bare `RequestMovementLock(this)` call defaults to Full, contradicts Asansör AC13 | Consistency | Warning |
| `asansor-kat-erisim-sistemi.md` | Bare `RequestMovementLock(this)` call, same issue | Consistency | Warning |

---

### Verdict: FAIL

8 blocking issues across consistency and design-theory checks, one confirmed independently by all three review lenses (consistency, design-theory, scenario walkthrough). Several are genuinely new design questions (the two-endings-one-mechanism gap, the missing guaranteed Pillar 1 exposure, the saturation-ending payoff destruction) rather than mechanical propagation gaps — these need design decisions, not just doc edits, consistent with this project's established "resolve design judgment calls explicitly, don't fix them unilaterally" protocol.

### Required actions before re-running

1. Decouple the saturation trigger from `OnTriggerFired` (gate on `Held` or add a settle delay) — closes the top finding and its scenario-walkthrough consequences in one move.
2. Design decision: either unify or differentiate the two HARD CUT endings' feel.
3. Design decision: assign 1-2 mandatory `Automatic` ambient shifts as MVP content to guarantee Pillar 1 exposure.
4. Fix the `AmbientZoneVolume` re-arm bug (re-run the initial overlap check when the volume's own scene becomes active).
5. Resolve the AC14/AC14a Hold-fill contradiction (add the missing precondition) and fix the two stale UI Requirements passages.
6. Sync `systems-index.md`'s dependency graph (Etkileşim row, FPC row, Adaptif Ses/Görev links) to match the GDDs.
7. Fix the `tension_gain`/`Highlight` division-by-zero guard and reconcile AC1 vs AC17 on single-round TaskLists; fix the indexing convention mismatch in AC16.
8. Fix the `tension_gain` arithmetic error in the worked example.
9. Design decision (lower priority, Warning-tier but load-bearing): retract or re-scope the Competence/Mastery claim in game-concept.md; decide whether the decoy ratio needs tightening; decide on the phone-notification-tone stinger candidate as an explicit anti-pillar check; assign Diyalog/Anlatı İçeriği an experiential acceptance criterion for its delegated narrative payoff.
