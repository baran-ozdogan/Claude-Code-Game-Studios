# Concept Prototype Report: Yankılar (Echoes) — Lighting/Subjective Reality Shift

> **Date**: 2026-08-01
> **Prototype Path**: Engine (Unity 6.3/6.5, URP)
> **Concept File**: design/gdd/game-concept.md

---

## Hypothesis

If the player interacts with a memory-trigger object, the room's lighting/color
shifts from warm amber to cold sodium-green/blue via URP Volume blending — we
will know this creates unease if the tester describes the shift as "unsettling"
or "something is wrong" without being told what to look for.

---

## Riskiest Assumption Tested

That a lighting/color-temperature shift alone (no new geometry, no creature, no
sound) is enough to create a felt sense of "something is wrong." The entire
visual identity anchor ("Otel Senin Yerine Hatırlıyor") and Pillar 1 (Subjective
Reality) depend on this technique carrying emotional weight on its own.

**Result: this assumption did not hold on its own.** The visual technique reads
as an aesthetic color shift, not as dread — see Result below.

---

## Approach

Built a Unity Editor automation script (`Scripts/Editor/PrototypeSceneBuilder.cs`)
that programmatically constructs a blockout corridor, two warm-amber point
lights, a Global Volume (pre-authored cold/desaturated profile, weight driven
0→1 at runtime), a first-person `CharacterController` player, and one
interactable trigger cube — reducing manual Editor setup to one menu click.

**Path chosen:** Engine
**Reason for path:** Atmosphere/render feel cannot be reliably judged through
browser latency — this hypothesis is specifically about real-time rendered
lighting feel.

**Shortcuts taken (intentional):**
- No sound design (intentionally excluded to isolate the visual variable)
- No menus, no restart flow, no UI
- Single room, single trigger object, no task loop
- Placeholder geometry (primitive cubes/planes, no materials beyond default)
- Player never encounters more than one shift in a session

---

## Result

The tester (project owner) reported the color/light transition looked
technically clean ("geçiş güzel" — the transition is nice) but did **not**
produce the intended emotional response. Direct quote: *"rahatsız edici
bulmadım... öyle 'eyvah naptım' hissi ya da kalpte adrenalin peak salgılatmadı,
rahatsız ediciliği yüksek olmalı"* (did not find it unsettling — no "oh no what
did I do" feeling, no adrenaline peak; the unsettling quality needs to be
higher). No "best moment" was identified. No unexpected behavior surfaced. The
tester independently attributed the missing effect to the absence of sound:
*"ses efektleri eklenince olur"* (it'll work once sound effects are added).

---

## Metrics

| Metric | Value |
|--------|-------|
| Path used | Engine (Unity 6.3/6.5 LTS, URP) |
| Iterations to playable | 2 (one round: menu command not run on first attempt, scene appeared empty; resolved by running Build Prototype Scene before Play) |
| Prototype duration | ~1 session (same day as setup) |
| Playtesters | 1 internal (project owner) |
| Feel assessment | Visually clean but emotionally flat — described as pleasant, not unsettling; no physiological/emotional spike reported |
| Hypothesis verdict | PARTIALLY CONFIRMED — the lighting/color mechanism itself works and reads cleanly, but is not sufficient alone to produce dread |

---

## Recommendation: PROCEED

The core visual technique (URP Volume weight blend + light color/intensity
lerp) is confirmed as technically sound and clean to execute — no rework needed
on the mechanism itself. The emotional payload was under-delivered in isolation,
but this was expected: the prototype deliberately excluded sound to isolate the
visual variable, and the tester's own read is that sound is the missing
ingredient, not that the visual approach is wrong. Given this matches the
Technical Director's independent earlier flag (audio design is the most
commonly underestimated cost for this kind of no-combat tension game), this is
a design-sequencing finding, not a concept-killing one. Proceed, but do not
treat the visual layer as sufficient on its own in the GDD/tuning work ahead.

---

## If Proceeding

- **Core tuning values discovered:** 3-second shift duration, White Balance
  Temperature -60 / Tint +10, Color Adjustments Post Exposure -0.5 / Saturation
  -20 are a clean, artifact-free starting point for the "memory-cold" look —
  keep as the baseline and iterate color/intensity per-room rather than
  re-deriving the mechanism.
- **Assumptions confirmed:** The lighting/color-temperature technique is
  achievable in URP with baked-lighting-friendly, cheap runtime cost (Volume
  weight lerp + light color lerp) — matches TD-FEASIBILITY's technical
  guidance exactly, no rework needed there.
- **Assumptions disproved:** That visual/lighting change alone is sufficient to
  carry Pillar 2 (Quiet Dread, Not Shock). It is not — dread requires at least
  audio (ambience layering, a sting or tonal shift) alongside the visual cue.
  Silence read as "pretty," not "wrong."
- **Emergent mechanics:** None surfaced — session was too narrow in scope to
  reveal emergent behavior; expected, given the deliberately isolated test.

**Design implication carried into GDD work:** audio must be budgeted as a
first-class system from the start of Systems Design, not layered on late. Each
memory-trigger design (in `/design-system`) should specify a paired audio cue
alongside the lighting shift, not treat audio as a Polish-phase addition.

> Note: consider a small follow-up spike — same scene, same lighting shift, but
> with one ambient audio layer + one subtle sting on trigger — before writing
> the audio-related sections of the relevant system GDD. This isolates whether
> audio alone closes the gap, or whether the combination needs further tuning
> (e.g., longer shift duration, added subtle geometry distortion per Pillar 1).

**Next steps:**
1. `/design-review design/gdd/game-concept.md`
2. `/gate-check`
3. `/map-systems`
4. `/design-system [mechanic]` — carry the tuning values and the audio-pairing
   requirement into the relevant system's Tuning Knobs/Formulas sections

---

## Creative Director Review (CD-PLAYTEST)

> **Verdict**: CONCERNS (accepted, PROCEED stands with binding conditions) — 2026-08-01

The lighting mechanism itself is sound (matches TD-FEASIBILITY guidance) and
excluding sound to isolate the visual variable was correct spike methodology —
"sound is missing" is a sequencing note, not a flaw in the visual approach.
The risk is not the finding itself but what it could license downstream: if
"sound will fix it" is treated as settled rather than verified, Systems Design
could write memory-trigger specs around an unconfirmed compound effect, which
would be Pillar 2 drift by omission.

**Binding conditions before the memory-trigger system GDD is approved:**
1. The audio-paired follow-up spike (noted above) must be completed and its
   result folded into the GDD's Formulas/Tuning Knobs sections *before* that
   GDD is finalized — "sound will fix it" does not count as verified until a
   build confirms it.
2. A single self-reported internal playtester is thin evidence for an
   emotional-response hypothesis — get at least one outside tester once sound
   is added.
3. The GDD must explicitly state that Pillar 2 (Quiet Dread, Not Shock) is a
   **light + sound compound effect**, not a lighting effect alone, so
   downstream system design doesn't inherit the visual-only framing this
   prototype tested in isolation.

`/map-systems` and general GDD authoring may proceed in parallel — this
condition is scoped specifically to the memory-trigger system's GDD approval
(CD-GDD-ALIGN), not to the whole project.

---

## Lessons Learned

- **What assumptions were broken by actually building this?** The concept
  doc's Unique Hook and Pillar 1 leaned on lighting/color as if it alone
  produced dread. Building it showed that's an incomplete picture — light/color
  sets the *stage*, sound appears to be what actually triggers the felt unease.
- **What surprised us that didn't show up in the brainstorm?** How immediately
  and confidently the tester attributed the gap to missing audio, without being
  prompted — suggesting the intuition that audio matters most here is strong
  and worth trusting going into GDD work.
- **What would we test differently next time?** Run a paired follow-up spike
  (same visual + one audio layer) before fully committing the "visual-only"
  framing to any GDD language, so the written design doesn't undersell audio's
  role in Pillar 1/2.

---

> *Prototype code location: `prototypes/yankilar-lighting-concept/`*
> *This code is throwaway. Never refactor into production.*
