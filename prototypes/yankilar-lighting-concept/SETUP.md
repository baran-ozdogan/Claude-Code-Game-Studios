# Yankılar — Lighting Prototype Setup

// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a lighting/color-temperature shift alone (no new geometry,
// no creature, no sound) create a felt sense of "something is wrong"?
// Date: 2026-08-01

## 1. Create the project

1. Open Unity Hub → New Project → **Unity 6.3 LTS** (6000.3.0f1) → template **URP (3D)**.
2. Name it anything (e.g. `YankilarLightingPrototype`) — this is throwaway, keep
   it separate from any real project folder.

## 2. Copy the scripts

Copy this folder's `Scripts/` subfolder (all `.cs` files, including the
`Scripts/Editor/` subfolder) into the new project's `Assets/Scripts/`, preserving
the `Editor/` subfolder structure. Unity will compile automatically — check the
Console for errors before continuing.

## 3. Build the scene (one click)

In the Unity Editor menu bar: **Yankilar Prototype → Build Prototype Scene**.

This programmatically builds the entire test scene — corridor blockout, two warm
amber lights, a Global Volume with a pre-authored cold/desaturated profile
(weight starts at 0), a first-person player with a CharacterController, and one
small interactable cube — and saves it to
`Assets/PrototypeGenerated/YankilarLightingPrototype.unity`. No manual GameObject
placement or Inspector wiring needed.

## 4. Play

Press **Play**. **WASD** to move, **mouse** to look, **E** to interact when the
screen center is near the small trigger cube (~2m away). On interact, the
corridor's lighting blends from warm amber to cold sodium-green/blue over 3
seconds (`MemoryTrigger` component on the cube — tune `Shift Duration` and
`Cold Color` there to try variations, e.g. swap the default cold blue
`#8FA88C`-ish blue for a more sodium-green tone).

No menu, no restart button — stop Play mode and press Play again to reset.

## If something doesn't compile

Paste the exact Console error back and it'll get fixed — this is expected;
engine-path prototypes typically take 2-4 iteration rounds before they run clean.
