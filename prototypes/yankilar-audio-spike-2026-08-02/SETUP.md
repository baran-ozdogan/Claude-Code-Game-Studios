# Yankılar — Audio-Paired Follow-Up Spike

// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does adding the Adaptif Ses Sistemi's audio layers on top of the
// same lighting-shift trigger produce the unease that the light-alone
// prototype (prototypes/yankilar-lighting-concept/) failed to create?
// Date: 2026-08-02

## Why this spike exists

The first concept prototype tested a URP Volume lighting shift alone and
concluded (see `prototypes/yankilar-lighting-concept/REPORT.md`): technically
clean, but **not enough on its own** to create unease. `game-concept.md` now
defines Pillar 2 (Sessiz Gerilim, Şok Değil) as a **light + sound compound
effect** — this spike tests that compound effect directly, using the mixing
philosophy and content direction from `design/gdd/adaptif-ses-sistemi.md`.

This is a spike (`--spike` mode), not a new concept prototype: no GDD
prerequisites beyond what already exists, no phase-gate implications, ~4 hour
build cap.

## 1. Create the project

1. Open Unity Hub → New Project → **Unity 6.3 LTS** (6000.3.0f1) → template **URP (3D)**.
2. Name it anything (e.g. `YankilarAudioSpike`) — throwaway, keep it separate
   from any real project folder. You do **not** need to reuse the lighting
   prototype's Unity project — this spike is self-contained.

## 2. Copy the scripts and audio

Copy this folder's `Scripts/` subfolder (all `.cs` files, including the
`Scripts/Editor/` subfolder) into the new project's `Assets/Scripts/`,
preserving the `Editor/` subfolder structure. Unity will compile automatically
— check the Console for errors before continuing.

Copy this folder's `Audio/StingerStrike.wav` into the new project's
`Assets/PrototypeAudio/StingerStrike.wav` (create the folder if needed).
Unity will import it as an `AudioClip` automatically.

**Where StingerStrike.wav came from:** it's a 1.3s trim of the first isolated
"swell" phrase from the reference file you provided (`Scary Horror Ambience
(Intense Violin Strikes) - Sound Effect for editing.wav`), faded in/out for
clean one-shot playback. Several rounds of procedural synthesis (sine tones,
noise bursts, harmonic+noise hybrids tuned to measurements of your reference
audio) didn't land — this uses the actual reference clip directly instead.
Placeholder audio is explicitly fine for a throwaway prototype/spike; this
never ships.

## 3. Build the scene (one click)

In the Unity Editor menu bar: **Yankilar Prototype → Build Audio Spike Scene**.

This programmatically builds the same corridor blockout as the lighting-only
prototype, plus:
- An **AmbientAudio** object playing a continuous, procedurally generated low
  mechanical hum (no external audio files needed — see `AmbientHum.cs`)
- A **StingerVoice** component on the trigger cube that plays
  `Assets/PrototypeAudio/StingerStrike.wav` in the same frame as the lighting
  shift starts (if the Console shows a warning that the clip wasn't found,
  make sure step 2's copy happened before building the scene)

Saves to `Assets/PrototypeGenerated/YankilarAudioSpike.unity`.

## 4. Play

Press **Play**. **WASD** to move, **mouse** to look. You should immediately
hear the ambient hum (continuous, quiet, mechanical). Walk to the small cube
(~2m away) and press **E** — the lighting shifts from warm amber to cold
blue over 3 seconds *and* the stinger tone plays once, in sync with the start
of the shift.

No menu, no restart button — stop Play mode and press Play again to reset.

## What to listen/watch for

- Does the ambient hum read as "the building is quietly alive," or is it
  distracting/annoying?
- Does the stinger feel like a **contrast in timbre** against the ambient (per
  the GDD's mixing philosophy), or does it just sound like a volume spike?
- Compared to the original light-only prototype: does the *combination* create
  a "something is wrong" reaction that light alone didn't?

## If something doesn't compile

Paste the exact Console error back and it'll get fixed — this is expected;
engine-path prototypes typically take 2-4 iteration rounds before they run clean.
