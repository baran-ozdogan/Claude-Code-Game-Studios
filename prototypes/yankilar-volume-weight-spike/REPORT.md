# Spike Report: Işık/Volume Durum Sistemi — Volume Weight/Blend Distance

> **Date**: 2026-08-01
> **Prototype Path**: Engine (Unity 6.3 LTS, URP)
> **Origin**: `/design-review design/gdd/isik-volume-durum-sistemi.md`, round 3
> **GDD**: design/gdd/isik-volume-durum-sistemi.md

---

## Hypothesis

If a trigger zone's box collider is sized to `R_exit + (PlayerMaxSpeed ×
Duration) + SafetyBuffer` and the local Volume's `blendDistance` is set to 0,
then a per-zone ticker writing `Volume.weight = ShiftProgress` every frame
will produce a smooth, pop-free transition through the full Shifting-Out
decay — because the player never physically exits the collider while the
scripted weight is still non-zero.

---

## Riskiest Assumption Tested

Three prior `/design-review` rounds each described a different mechanism for
how the box collider, `Volume.weight`, and `blendDistance` interact — the
first two were found technically wrong on review. Rather than a fourth
argued-from-prose description, this spike tested the *practical, correctly-
sized configuration* empirically: does it actually produce a clean transition
in a real Unity scene, regardless of exactly which internal URP mechanism is
responsible?

---

## Approach

Built three parallel test corridors via Editor automation
(`Scripts/Editor/VolumeWeightSpikeSceneBuilder.cs`) sharing the GDD's own
worked example values (`R_trigger=4m`, `R_exit=4.6m`, `Duration=3.0s`):

- **Corridor A**: undersized box (half-extent=4m, matches `R_trigger`),
  `blendDistance=2` (non-zero)
- **Corridor B**: undersized box (half-extent=4m), `blendDistance=0`
- **Corridor C**: correctly-sized box (half-extent=10m, per the Box Collider
  Safety Margin formula), `blendDistance=0`

Each scene includes a `SpikeZoneController` driving a simplified
Held→ShiftingOut→Dormant state machine, a live on-screen HUD (state,
distance, inside-collider flag, scripted weight, Shifting-Out timer), and
Console logging of the exact frame the player's position exits the box
collider's bounds, correlated with the scripted weight and state at that
instant.

**Path chosen:** Engine
**Reason for path:** The question is specifically about real-time URP Volume
blend behavior, which cannot be judged any other way.

**Shortcuts taken (intentional):**
- Simplified zone state machine (no `x`-tracking/interrupt-resume logic from
  the real GDD — only Dormant/Held/ShiftingOut, sufficient to isolate this
  one question)
- Only Corridor C was actually run; Corridors A and B (the deliberately
  undersized-box failure-mode scenarios) were built but not tested — the
  project owner judged Corridor C's clean result sufficient to confirm the
  practical fix without needing to also reproduce the failure mode

---

## Result

Corridor C (box half-extent=10m, `R_exit=4.6m`, `blendDistance=0`) produced a
clean, pop-free transition. Console log:

```
[SpikeZone] -> Held at t=6,32, distance=4,00
[SpikeZone] -> ShiftingOut at t=11,69, distance=4,60
[SpikeZone] -> Dormant (ShiftingOut complete) at t=14,69
[SpikeZone] *** EXITED BOX COLLIDER *** at t=15,08, state=Dormant, scriptedWeight=0,000, distance=10,01, blendDistance=0,00
```

The full 3.00s Shifting-Out decay (11.69s → 14.69s) completed entirely while
the player was still inside the box collider. The player didn't physically
exit the box until t=15.08 — **0.39s after** the scripted weight had already
reached 0. Project owner's read: "gayet güzel beğendim, C onaylandı" (looks
very good, C approved) — no visible pop or snap observed.

The `BlendDistanceGuard` component's `OnValidate` fired twice in quick
succession during scene setup (16:52:51, 16:52:52) — consistent with normal
Editor object-creation/serialization behavior, not a deliberate test of
Question 3 (whether editing the Volume's `blendDistance` later triggers the
guard). Question 3 was not conclusively tested.

---

## Metrics

| Metric | Value |
|--------|-------|
| Path used | Engine (Unity 6.3 LTS, URP) |
| Corridors built | 3 (A, B, C) |
| Corridors actually run | 1 (C only) |
| Playtesters | 1 internal (project owner) |
| Hypothesis verdict | CONFIRMED for the practical configuration (correctly-sized box + blendDistance=0 → clean transition); NOT tested: whether an undersized box (Corridors A/B) actually reproduces the predicted snap failure, and whether OnValidate catches a later Blend Distance edit (Question 3) |

---

## Recommendation: PROCEED with the confirmed configuration

The Box Collider Safety Margin formula (`BoxHalfExtentMin = R_exit +
PlayerMaxSpeed × Duration + SafetyBuffer`) plus `blendDistance=0` is
confirmed to produce the intended smooth, pop-free transition. The GDD's
Core Rules and Formulas sections have been updated to state this as the
required configuration, with an explicit note that the exact internal URP
mechanism (whether `blendDistance=0` is necessary, sufficient, or incidental
given the box was never actually exited while weight > 0) was not isolated —
Corridors A and B were built but not run. This doesn't block adoption: the
GDD's guidance is now "always size the box per the formula," which sidesteps
needing to know why undersizing fails, only that correct sizing works.

**If revisited later:** running Corridors A and B would answer whether
`blendDistance=0` is load-bearing or whether box sizing alone is sufficient
regardless of `blendDistance` — useful if a future implementer wants to relax
the `blendDistance=0` requirement, but not necessary for shipping the current
GDD guidance as-is.

---

> *Prototype code location: `prototypes/yankilar-volume-weight-spike/`*
> *This code is throwaway. Never refactor into production.*
