# Yankılar — Volume Weight/Blend Distance Spike

// PROTOTYPE - NOT FOR PRODUCTION
// Origin: `/design-review design/gdd/isik-volume-durum-sistemi.md`, round 3 —
// the same mechanism drew a different (and each time wrong) explanation in
// rounds 1, 2, and 3 of paper review. This spike settles it empirically
// instead of a fourth round of argument.
// Date: 2026-08-01

## Questions this spike answers

The GDD's Core Rules claim the per-zone ticker writes `Volume.weight =
ShiftProgress` every frame, "bypassing Unity's automatic collider-distance
blend calculation entirely." A `unity-specialist` review round disputed this:
URP's `VolumeManager` computes `interpFactor` from collider distance /
`blendDistance`, then does `interpFactor *= volume.weight` — so the scripted
weight is only ever *one multiplicand*, never a full bypass, unless
`blendDistance` is also forced to (and kept at) 0.

Three concrete questions, each with its own test corridor below:

1. **Does `blendDistance = 0` actually decouple the effect from distance, or
   produce a hard cutoff right at the collider surface?** (Corridor B vs. C)
2. **What happens the instant the player's position exits the physical
   collider bounds while `ShiftProgress` (our scripted weight) is still > 0?**
   Specifically: during the ~3s Shifting-Out decay, the player keeps walking
   at ~1.6 m/s (the FPC's max speed per `birinci-sahis-kontrolcu.md`) — does
   the visual effect keep fading smoothly, or does it snap/vanish the moment
   they cross the collider surface? (all three corridors, but A and B use a
   deliberately *undersized* box relative to that walk distance; C uses a
   box sized with the margin the GDD fix now recommends)
3. **Does an `OnValidate` guard on a sibling script actually catch someone
   changing the Volume's own `Blend Distance` field later in the Inspector,
   or does it only fire for edits to its own fields?** (quick manual check,
   instructions at the bottom)

## 1. Create the project

1. Open Unity Hub → New Project → **Unity 6.3 LTS** (6000.3.0f1) → template
   **URP (3D)**.
2. Name it anything (e.g. `YankilarWeightSpike`) — throwaway, keep it
   separate from any real project folder.

## 2. Copy the scripts

Copy this folder's `Scripts/` subfolder (all `.cs` files, including
`Scripts/Editor/`) into the new project's `Assets/Scripts/`, preserving the
`Editor/` subfolder structure. Check the Console for compile errors before
continuing.

## 3. Build the three test corridors

Three separate menu items, each builds and saves its own scene (so you can
run them one at a time and compare):

- **Yankilar Spike → Build Corridor A (undersized box, blendDistance=2)**
- **Yankilar Spike → Build Corridor B (undersized box, blendDistance=0)**
- **Yankilar Spike → Build Corridor C (correctly-sized box, blendDistance=0)**

Each builds a 3m-wide, 26m-long corridor with a warm-amber baseline light, a
local Volume (cold/desaturated profile, same locked values as the real GDD:
White Balance Temperature -60/Tint +10, Post Exposure -0.5/Saturation -20),
and a trigger zone centered at the corridor's midpoint with `R_trigger = 4m`,
`R_exit = 4.6m` — the GDD's own worked example values, so results map
directly back to the doc.

Box collider sizing per corridor (this is the variable under test):
- **A & B**: box half-extent = 4m (matches `R_trigger`, deliberately
  undersized relative to `R_exit` and especially relative to the ~4.8m the
  player keeps walking during the 3s Shifting-Out decay at 1.6 m/s).
- **C**: box half-extent = 10m (covers `R_exit` + the full Shifting-Out walk
  distance with margin — the sizing the GDD fix currently recommends).

## 4. Play each scenario

Press **Play**. **WASD** to move (capped at 1.6 m/s, the FPC's real max
speed), **mouse** to look. Walk straight down the corridor at a steady pace
and **do not stop** once the shift begins — the whole point is testing the
continued-walking case.

An on-screen HUD (top-left) shows, live and updating every frame:
- `State`: Dormant / Held / ShiftingOut
- `Distance to zone center`
- `Inside box collider`: YES/NO
- `Scripted weight` (what the ticker is writing to `Volume.weight` this frame)
- `Shifting-Out timer` (0 → 3s, once active)

The Console also logs a line the instant `Inside box collider` flips from
YES to NO, with the exact `State`/`Scripted weight` at that moment — this is
the critical data point: **if the logged weight is still > 0 when you exit
the box, and you visually see the color effect pop/vanish at that same
moment (rather than continuing its smooth 3s fade), that confirms Unity's
own distance-based blend is still active despite the scripted weight — i.e.
the "bypass" claim is false for that scenario.**

## 5. What to report back

For each of the three corridors, note:
1. Did you see any visual snap/pop, and if so, at what moment relative to
   the HUD's `Inside box collider` flipping to NO?
2. Compare A vs. B — does `blendDistance = 0` change the outcome at all,
   or does the undersized box break both the same way?
3. Does C (the correctly-sized box) look clean start to finish, with no pop?

Paste the Console log lines (the "exited box" ones) and your visual read for
all three back into the chat — that becomes the confirmed basis for
rewriting the GDD's Core Rules paragraph and AC14c from actual behavior
instead of a fourth guessed description.

## 6. Quick manual check for Question 3 (OnValidate)

Each corridor's Volume GameObject also has a `BlendDistanceGuard` component
attached. It logs the Volume's current `Blend Distance` once when the object
is created/scene loads. **While in Edit mode (not Play mode)**, select the
Volume GameObject in the Hierarchy and manually change its **Blend Distance**
field in the Inspector to a different number. Check the Console: does a new
log line appear, or does nothing happen? Report which.

## If something doesn't compile

Paste the exact Console error back and it'll get fixed — expected, engine-path
spikes typically take a couple of iteration rounds before they run clean.
