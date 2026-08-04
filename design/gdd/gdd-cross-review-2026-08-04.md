# Cross-GDD Review Report
**Date**: 2026-08-04
**GDDs Reviewed**: 14 (game-concept.md, systems-index.md, 9 Full GDDs, 3 Quick Specs)
**Systems Covered**: Birinci Şahıs Kontrolcü, Işık/Volume Durum Sistemi, Anlatı Durum/İpucu Takibi, Gece/Oturum Durumu, Seviye/Sahne Geçişi, Adaptif Ses Sistemi, Etkileşim Sistemi, Asansör/Kat-Erişim Sistemi, Diyalog/Anlatı İçeriği, Görev/Taşıma Döngüsü, Anı-Tetikleyici Etkileşim, Sahne Kesmeli Anlatı

**Context**: This review follows a FAIL-verdict `/review-all-gdds` run on 2026-08-03, three subsequent fix passes (the 8 required actions, a propagation-gap cleanup pass, and a one-at-a-time resolution of N1/N2/N5/N6/N7/N8 plus the two never-addressed original blockers `ZoneChanged` ownership and the stinger/light timing gap). This is the re-verification run requested by the user before considering the GDD phase complete.

---

## Consistency Issues

### Blocking (must resolve before architecture begins)

🔴 **Adaptif Ses AC7 directly contradicts the new dual-path stinger contract (AC6c)**

`adaptif-ses-sistemi.md` AC6c (new, this session): stinger plays immediately on `newState=Shifting-In` for Persistent shifts.
`adaptif-ses-sistemi.md` AC7 (unchanged): "GIVEN Idle durumundaki bir stinger, WHEN OnShiftStateChanged, newState != Held ile fırlar, THEN event göz ardı edilir, stinger Idle'da kalır."

`Shifting-In` satisfies `newState != Held`. AC7 was never narrowed when the stinger/light timing fix landed. Since every `MemoryTriggerDef`-linked shift is `Persistent=true`, AC7 as written fails the primary playback path for every memory trigger in the game.
→ Narrow AC7 to `newState != Held AND NOT (newState == Shifting-In AND IsShiftPersistent == true)`.

🔴 **`HeldSessionAlreadyPlayed` add-then-remove race: AC6b's removal predicate also matches the event that AC6c uses to add the guard entry**

Core Rules / States and Transitions: "shiftId, Playing'e girerken bu kümeye eklenir; `newState != Held` (Shifting-Out/Dormant) gözlemlendiğinde kümeden çıkarılır." AC6b repeats the same unqualified `newState != Held` predicate.

`Shifting-In` also satisfies `newState != Held` — so the exact event AC6c uses to populate the guard could, depending on implementation/evaluation order, also satisfy AC6b's removal trigger. The "bir kez duyulur, sonra asla değil" guarantee becomes order-of-evaluation dependent.
→ Rewrite the removal predicate explicitly as `newState == Shifting-Out || newState == Dormant`, in both Core Rules and AC6b.

🔴 **`stinger_falloff` input reverted to the deprecated `radius` inside Adaptif Ses's own Core Rules — only the Formulas section was fixed**

Core Rules, "Anı-Tetikleyici Stinger": "...`minDistance = radius × 0.3`, `maxDistance = radius × 1.0` (bkz. Formulas...)" — still says `radius`.
Formulas section (correctly fixed): "`minDistance = stingerAudioRadius × 0.3`..."

Also, Formulas → "Sorgu zamanlaması" still says the query happens "OnShiftStateChanged(Held) aldığı karede" — for memory-trigger shifts the playback frame is now `Shifting-In` (AC6c), not `Held`.
→ Walk both passages to `stingerAudioRadius` / `Shifting-In`.

🔴 **Anı-Tetikleyici's `TriggerMode` edit-time validation is structurally unimplementable as specified**

The validation mechanism is "`AssetDatabase.FindAssets("t:MemoryTriggerDef")` ile tüm proje genelinde taranarak" (asset-scan). But:
1. `MemoryTriggerDef`'s field list (`shiftId`, `shiftConfig`, `holdDurationOverride`, `promptText`) carries **no reference to a zone** — there is no authored link from the def to "bağlı olduğu bölge".
2. `TriggerMode` is a **per-zone** field (`isik-volume-durum-sistemi.md`: "Her tetikleyici bölgesi artık açık bir TriggerMode alanı taşır"), not a `ShiftConfig` field. An asset scan of `MemoryTriggerDef`s cannot see scene-placed zone components.

The single most important consent-premise guard in the project (`ManualOnly`) has no reachable validation path as specified.
→ **Design decision needed**: either give `MemoryTriggerDef` an explicit zone reference (and switch the validation to a scene scan), or move `TriggerMode` onto `ShiftConfig` itself. This spans `isik-volume-durum-sistemi.md` and `ani-tetikleyici-etkilesim.md`.

🔴 **`seviye-sahne-gecisi.md` — the doc that owns `TransitionType` still declares N6 "open" and cites the old single-arg event in four places**

Despite its own Interactions section defining `event OnTransitionStateChanged(TransitionState newState, TransitionType type)` with an explicit note that this closes N6, the same file still says, in four separate places: header ("N6 ... hâlâ açık"), Open Questions ("henüz SOFT ile HARD CUT'ı ayırt edemiyor... her asansör yolculuğunda da çalıyor"), Interactions "Bağımlılık yönü" ("OnTransitionStateChanged(Swapping)'e abone olarak"), and Dependencies ("OnTransitionStateChanged(Swapping)'e abone olur").

`adaptif-ses-sistemi.md`'s own Dependencies section mirrors the stale form too: "sadece Swapping işlenir" — no `type == Hard` filter, contradicting its own Core Rules and AC13b.
→ Walk the `type == Hard` filter and "N6 closed" status to all of these.

🔴 **Birinci Şahıs Kontrolcü claims zero dependencies while reading a registry owned by Etkileşim Sistemi (a Core-layer system) — a Foundation→Core layer violation**

FPC Dependencies: "Bağımlıdır: Yok — Foundation katmanı, hiçbir sisteme bağımlı değil." But Formula 2 (`approach_slow_taper`, sourced to this doc) reads `d` from `InteractableRegistry`, which `etkilesim-sistemi.md` explicitly owns: "Bu registry bu GDD'de tanımlanır ve sahiplenilir; Birinci Şahıs Kontrolcü'nün GDD'sinin Dependencies bölümü buna referans vermeli." `systems-index.md` row 1 still lists "Depends On: —".
→ Either move the registry to a Foundation-owned location (already an open question in `etkilesim-sistemi.md`), or update the layer/dependency claims in three documents to reflect the real read-dependency.

🔴 **`AmbientZoneVolume` has no equivalent to the scene-active guard `isik-volume-durum-sistemi.md` already built for the identical problem**

Işık/Volume solved "additive scene co-residency causes false cross-scene triggers" with an explicit `SceneManager.GetActiveScene()` guard. `AmbientZoneVolume` (new this session, same underlying co-residency window from `seviye-sahne-gecisi.md`'s SOFT transitions) has no such guard — during the 0.5-2s co-residency window, both the origin and target zone volumes can physically contain the player in shared world space, violating the documented "exactly one zone active" invariant on every elevator ride.
→ Walk the same scene-active guard pattern to `AmbientZoneVolume`; add the co-residency case to AC1a/1b.

🔴 **`ani-tetikleyici-etkilesim.md` still asserts (twice) that Sahne Kesmeli Anlatı reads `OnClueKnown`/`GetKnownClueIds().Count` — retracted everywhere else**

Dependencies and Open Questions both still say Sahne Kesmeli reads the old Anlatı-Durum-based saturation signal. `sahne-kesmeli-anlati-2026-08-02.md` and `anlati-durum-ipucu-takibi.md` both correctly reflect the switch to `FiredTriggerIds.Count` and the removal of the dependency. Two stale copies in the one document not walked when the signal was switched.
→ Fix both mentions.

🔴 **`systems-index.md`'s two dependency sections disagree with each other and with both GDDs about Sahne Kesmeli Anlatı → Anı-Tetikleyici Etkileşim**

Dependency Map says it's a dependency ("same-tier... confirmed not a layer inversion"); Systems Enumeration says it's "indirect/event-driven, no direct call"; `sahne-kesmeli-anlati-2026-08-02.md`'s own Dependencies section omits Anı-Tetikleyici entirely; `ani-tetikleyici-etkilesim.md` says "bu sisteme doğrudan bağımlı değil". Four documents, three different answers.
→ Determine the correct relationship and make all four consistent. (The index's Dependency Map, flagged in its own change log as "the single most-required, least-applied action across all three prior passes," is the likely outlier.)

🔴 **[Design-theory, most severe] The saturation ending's `IsFinalRoundActive` guard fires at final-round *activation*, not final-round *progress* — for the modal engaged player, this skips the final round entirely and collapses both endings into one**

`sahne-kesmeli-anlati-2026-08-02.md`'s saturation condition re-evaluates on `OnFinalRoundStarted`, which fires the instant `RoundComplete → Idle` activates the last round. With MVP content at 2-3 triggers across 3-5 rounds, a player who finds all triggers by round 2-3 (the expected engaged player, not an edge case) triggers the HARD CUT in the same frame the final round begins — before ever picking up a single final-round item.

Consequences: (1) the final round, and the `Highlight` curve's terminal ~30% value that Görev/Taşıma calls its *only* tension-carrier, is never shown to the player it was built for; (2) Sahne Kesmeli's two distinct promised endings ("iş biter, sakin bir teslim" vs. "dünya seni durdurur") collapse into one for this player; (3) the HARD CUT preload guarantee breaks — the task-side preload threshold ("son round aktifken") coincides exactly with `OnFinalRoundStarted`, so `PreloadHardCut` and `RequestHardCut` land in the same frame with no `Ready` state achieved, producing a visible loading stall under `MovementLockScope.Full` — the opposite of the intended "torn from mid-motion" HARD CUT fantasy.
→ **Design decision needed**: gate saturation on final-round *progress* (e.g. final round's items picked up / `Carrying` entered, or a minimum elapsed-time floor), not final-round *activation*. This is a design call, not a mechanical propagation fix — flagging for user decision.

---

### Warnings (should resolve, but won't block)

⚠️ Six one-way dependency edges (2a): Görev/Taşıma → Gece/Oturum Durumu, Görev/Taşıma → Seviye/Sahne Geçişi, Görev/Taşıma → Adaptif Ses, FPC → Etkileşim (see blocking item above), Asansör's stale "henüz tasarlanmadı" self-note about Görev/Taşıma (which is already Approved and already lists it), and Diyalog/Anlatı İçeriği quick-spec having no Dependencies section despite being listed as a dependent by two other docs.

⚠️ Görev/Taşıma references an Adaptif Ses "SFX mixer group" and "ducking kuralları" that do not exist — Adaptif Ses defines exactly three groups (Ambiance, Stinger, CutSting) and explicitly rejects ducking as a "build-up" violation.

⚠️ `StingerAudioRadius` ownership/nullability inconsistent across three documents (nullable `float?` on `ShiftConfig` in one place, non-nullable `float` return on the query, "unset or ≤0" conflated in the AC that also calls it a *zone* field rather than a config field).

⚠️ `isik-volume-durum-sistemi.md` AC16 and its Blocked-ACs table still claim "no second query" and cite AC6/6a/6b as the closing test — Adaptif Ses now makes two synchronous queries per event, and the actual closing test is AC6c on `Shifting-In`.

⚠️ `seviye-sahne-gecisi.md`'s Blocked AC-12 row says Sahne Kesmeli has no movement-lock ACs — it now has two.

⚠️ `ani-tetikleyici-etkilesim.md` still argues its Hold-uniqueness safety on a `RequestMovementLock` *rejection* semantics that no longer exists (the lock is reference-counted and never rejects — the actual guard is `IsLocked` pre-check + `OnHoldBlocked()`).

⚠️ The B2 "unwitnessed stinger" risk is explicitly assigned to Adaptif Ses by `isik-volume-durum-sistemi.md`'s tick-skip note, but Adaptif Ses contains no rule, edge case, or AC referencing it.

⚠️ `TotalConfiguredTriggerCountForNight` still has no owning system/field — both saturation ACs are untestable until this is assigned.

⚠️ `UsedCallbackIds` persistence question assigned to the Diyalog spec, which has no Open Questions section to hold it.

⚠️ `systems-index.md` status data contradicts three GDD headers: Anı-Tetikleyici Etkileşim's own header says Approved, the index says Needs Revision; the Progress Tracker says "2 approved" when 3 documents currently carry `Status: Approved`.

⚠️ `adaptif-ses-sistemi.md`'s own header still lists N1/N7 as open; both were closed in the same document this session (same header-staleness class already fixed twice this session).

⚠️ Two "(henüz tasarlanmamış)" labels survive for now-Approved/designed systems in `etkilesim-sistemi.md` and `gorev-tasima-dongusu.md`.

⚠️ `OnShiftStateChanged`'s `radius` parameter is untested/unsourced for the majority of zones (every `ManualOnly` memory-trigger zone) now that Core Rules call it "practically unused" for that exact case, yet AC15 still asserts its correctness.

---

## Game Design Issues

### Blocking

🔴 **The night has no escalation mechanism — the round-based tension curve `game-concept.md` promises has no implementer, and the one real per-round curve runs backward**

`game-concept.md`: "turlar ilerledikçe ortam gerilimi birikir... gerilim, sahne/tetikleyici yoğunluğuyla kontrol edilir." Adaptif Ses explicitly refuses round-awareness ("Global durum yok... round index'ini hiç takip etmez," confirmed independently in `gorev-tasima-dongusu.md`). Memory-trigger density isn't a curve — 2-3 hand-placed, order-independent triggers, no per-round distribution. Görev/Taşıma's only round-indexed value (`Highlight(round)`) *decreases* prominence, and the doc itself calls this its sole tension-carrier.
→ Pick one owner for the night's arc (Adaptif Ses's ambient layer/intensity stepped by round index is the cheapest architecturally-consistent option) and make it round-indexed, or retract the "tetikleyici yoğunluğu" claim from `game-concept.md`.

🔴 **[Same underlying bug as the Consistency section's saturation-timing finding above — the guard fires on round *activation*, not round *progress*]** — see full writeup above.

🔴 **No system implements the time pressure/risk the concept sells — the "safe vs. risky" choice is risk-free and strictly dominant**

`game-concept.md` promises real stakes ("zaman baskısı altında", "asansör kısıtlı, zaman baskılı", a safe-vs-risky Autonomy choice). No MVP system has a clock, deadline, resource drain, penalty, or failure state — "Cezalandırıcı başarısızlık yok... oyuncu asla 'kaybetmiyor'" is itself a locked design decision. Detouring for memory fragments costs literally nothing. Worse, given the finding above, thorough exploration is currently *punished* with a truncated night (early saturation ending).
→ Either add one soft, pillar-consistent cost (e.g., a hard cap via `MaxCallbacksPerScene` making "find everything" trade against "have everything land narratively" — a narrative cost, not a mechanical one), or retract the risk/time-pressure framing from `game-concept.md` and `birinci-sahis-kontrolcu.md`'s "aciliyet var" line.

🔴 **The game's only Hold interaction has two contradictory Player Fantasies and no owner for its in-progress visual feedback**

`etkilesim-sistemi.md`: "eller zaten biliyor, tereddüt yok... bilinçli bir karar anı yok gibi hissettirilmeli" (and deliberately linear, not eased, specifically to avoid a hesitation feel). `ani-tetikleyici-etkilesim.md`: "isteyerek... bile bile yaptım... bırakabilirdin, bırakmadın" — built entirely on hesitation and deliberate choice. Memory triggers are the *only* Hold-type interactable in the MVP, so these two fantasies describe the same gesture. Separately: Etkileşim assigns the fill indicator to "the object's responsibility"; Anı-Tetikleyici explicitly declines it twice ("hiçbir amaca kullanılmaz... hiçbir VFX/ses YOK") while claiming to reuse "Etkileşim Sistemi'nin zaten sağladığı... UI'ı" — which doesn't exist. As specified, holding produces no fill, no prompt change, no feedback at all for 0.6-1.5s.
→ Resolve fantasy contradiction and assign fill ownership (recommend: Etkileşim owns a minimal uniform fill; Anı-Tetikleyici treats it as pre-existing UI furniture, not a reward signal).

🔴 **The approach-slowdown camouflage protecting Pillar 5 is structurally defeated in 2 of 3 MVP areas by the actual registry composition**

The taper was deliberately extended to all interactables to prevent it functioning as a "metal detector" for memory triggers. But carry items are only registered in the storage floor and only while their round is active and un-collected; the elevator button and drop-off zone are not `IInteractable` at all. Result: in the service corridor and ballroom, the registry contains memory triggers and nothing else — the taper is a 100%-precision detector across most of the playable space, exactly what the camouflage fix was meant to prevent. No content plan assigns decoy interactables.
→ Either commission decoy `IInteractable`s per area as an MVP content requirement, or drop the camouflage claim and revise the CD-GDD-ALIGN note.

### Warnings

⚠️ The HARD CUT is engineered against "reads as a glitch" but never evaluated against "reads as a jump-scare" — zero-feedback delivery + immediate full lock + zero-frame swap + simultaneous total-ambience-kill-and-stab-in-CutSting stack into a textbook startle signature on the game's most routine action, and the anti-pillar (no jump-scares) has not been explicitly tested against this sequence.

⚠️ Two MVP GDDs (`ani-tetikleyici-etkilesim.md`, `anlati-durum-ipucu-takibi.md`) claim Pillar 4 coverage by generalizing it beyond the friend-relationship definition `game-concept.md` and `systems-index.md`'s own CD-SYSTEMS note give it (which explicitly says Pillar 4 has zero MVP surface and requires a dedicated probe before Vertical Slice) — risk that the required probe gets skipped because MVP docs already "claim" the pillar.

⚠️ The approach-taper exports a felt "reluctance" onto ordinary carry-item pickups (6-20 times/night) via the shared camouflage flag, contradicting Etkileşim's "hands already know, no hesitation" fantasy for the exact same routine gesture.

⚠️ The stinger caption is unconditional, always-on non-diegetic text at the same instant Pillar 1's wordless ambiguity is supposed to land — `gorev-tasima-dongusu.md` claims parity with an opt-in/default-off pattern that the stinger caption does not actually follow.

⚠️ Anı-Tetikleyici's Player Fantasy defines itself by contrast with passive `Automatic` ambient shifts that have no MVP content, no owner, and no authoring plan — the contrast the fantasy depends on doesn't exist in the shipped MVP.

### Checks with no findings
3a (Progression Loop Competition) — clean, one dominant loop. 3b (Attention Budget) — at the 4-system peak, not over. 3d (Economic Loop) — no economy exists, correctly so per scope.

---

## Cross-System Scenario Issues

Scenarios walked: 2

🔴 **Scenario: A memory-trigger Hold completes shortly before the night ends (via HARD CUT, either the task-completion path or the saturation path above) — the trigger's clue may never be marked "known"**

Trigger: player completes a Hold on a `MemoryTriggerDef` object, then (within the ~3s `Shifting-In` window, or during a subsequent elevator ride whose 0.5-2s scene-unload window outlives the remaining ramp) the night ends via HARD CUT before that scene is ever reloaded again this session.

Data flow: `PersistentShiftIds` and `FiredTriggerIds` are both written early (at `Shifting-In`, per this session's own timing fixes) and the stinger+light compound effect fires immediately — so the player fully experiences the trigger. But Anlatı Durum's clue-reveal (`SeenShiftIds`/`MarkClueKnown`) is gated on `Held`, which only fires when the local `x` accumulator reaches 1.0 *or* via the scene-reload restore path (`isik-volume-durum-sistemi.md`'s own Edge Case, itself keyed off `PersistentShiftIds` — confirmed by reading the exact restore condition). If the scene is never reloaded again before the session ends (no Multi-Night Progression in MVP; `EndSession()` has no reverse), `Held` never fires, and that clue is silently absent from the psychiatrist-scene dialogue selection — even though the player experienced its full audio-visual payload.

Note: this is *not* a general elevator-ride problem — the same restore mechanism means any mid-session scene revisit (the normal multi-round carry loop) self-heals it. It is specifically the boundary case of "last trigger before the night's true ending."
→ Severity: WARNING, not blocking — narrow timing window, no crash/freeze, and the visible compound effect is unaffected; only the internal "clue known" bookkeeping (used for dialogue content selection) can miss it. Worth a deliberate decision (e.g., force-resolve all in-flight Shifting-In shifts to Held at the moment `RequestHardCut`/`EndSession` fires) rather than leaving to chance.

ℹ️ **Scenario: `AmbientZoneVolume`'s target-scene `Start()` may run before the player exists in that scene, during a HARD CUT's background preload**

`seviye-sahne-gecisi.md` confirms HARD CUT's target scene loads to `Ready` via background additive load, before the player is swapped in. `AmbientZoneVolume`'s documented "check overlap at Start()" mechanism (this session's own N-fix) would find nobody present at that point, and Unity's `OnTriggerEnter` may not fire naturally when the player's collider becomes active while already positioned inside the collider at swap time. This is a specific manifestation of the already-tracked, deliberately-deferred **B6** issue ("player/FPC object lifetime across a scene swap is still unspecified everywhere") rather than a new discovery — flagging for awareness, not as a fresh blocker.

---

## GDDs Flagged for Revision

| GDD | Reason | Type | Priority |
|-----|--------|------|----------|
| adaptif-ses-sistemi.md | AC7/AC6c contradiction, guard removal-predicate race, stale `radius` in Core Rules, stale Dependencies (Swapping-only), no `AmbientZoneVolume` scene guard, B2 assignment unaddressed, stale N1/N7 header | Consistency | Blocking |
| isik-volume-durum-sistemi.md | `TriggerMode` validation unimplementable, AC16/Blocked-ACs stale "no second query" claim | Consistency | Blocking |
| ani-tetikleyici-etkilesim.md | `TriggerMode` validation unimplementable, 2 stale OnClueKnown references, stale rejection-semantics argument, contradicts Etkileşim's Player Fantasy, leans on non-existent Automatic MVP content | Consistency + Design Theory | Blocking |
| seviye-sahne-gecisi.md | 4 stale "N6 open"/single-arg-event references in the owning doc itself, Blocked AC-12 stale | Consistency | Blocking |
| birinci-sahis-kontrolcu.md | Claims zero dependencies while reading Etkileşim's registry (layer violation); approach-taper reluctance/hesitation warning | Consistency + Design Theory | Blocking |
| etkilesim-sistemi.md | Contradicts Anı-Tetikleyici's Hold Player Fantasy, Hold-fill ownership gap, stale "henüz tasarlanmamış" label, one-way FPC dependency | Consistency + Design Theory | Blocking |
| sahne-kesmeli-anlati-2026-08-02.md | Saturation-ending timing bug (fires on round activation not progress), preload/trigger threshold collision | Design Theory | Blocking |
| gorev-tasima-dongusu.md | Tension-curve ownership gap, references nonexistent Adaptif Ses mixer group/ducking, stale "henüz tasarlanmamış" label, one-way dependencies (×3) | Consistency + Design Theory | Blocking |
| systems-index.md | Dependency Map vs. Systems Enumeration disagreement on Sahne Kesmeli↔Anı-Tetikleyici, stale Approved-count/status data | Consistency | Blocking |
| game-concept.md | Unimplemented time-pressure/risk promise, unimplemented round-tension-curve promise | Design Theory | Blocking |
| anlati-durum-ipucu-takibi.md | Pillar 4 over-claim, `UsedCallbackIds` persistence question orphaned | Design Theory | Warning |
| diyalog-anlati-icerigi-2026-08-02.md | No Dependencies section, stinger-caption parity claim inaccurate, orphaned persistence question | Consistency + Design Theory | Warning |
| asansor-kat-erisim-sistemi.md | Stale self-note about Görev/Taşıma dependency | Consistency | Warning |

---

## Verdict: FAIL

12 blocking items (9 consistency, 4 design-theory, with overlap — see table) plus one blocking cross-system scenario timing bug. Two of the design-theory blockers (saturation-ending timing, the escalation/time-pressure gap) and one consistency blocker (`TriggerMode` validation architecture) require genuine design decisions, not mechanical propagation fixes — these should not be resolved unilaterally per the collaborative protocol.

### Required actions before re-running
1. Resolve the saturation-ending timing bug (design decision: gate on final-round progress, not activation).
2. Resolve the `TriggerMode` validation architecture (design decision: zone reference on `MemoryTriggerDef`, or move `TriggerMode` to `ShiftConfig`).
3. Decide on the tension-escalation and time-pressure gaps (design decision: implement an owner, or retract the promises from `game-concept.md`).
4. Resolve the Hold interaction's contradictory Player Fantasies and fill ownership (design decision).
5. Decide on the approach-taper camouflage (design decision: add decoy interactables, or drop the claim).
6. Apply the remaining consistency fixes (AC7/AC6c, guard predicate, stale `radius`/N6/OnClueKnown references, `AmbientZoneVolume` scene guard, FPC layer violation, Sahne Kesmeli↔Anı-Tetikleyici dependency direction, `systems-index.md` status data) — these are mechanical, no design judgment required.
7. Address warnings as time allows (several are small, some — HARD CUT startle-sequencing, Pillar 4 over-claim — are worth deliberate attention even though not blocking).
