# Smoke Test: Critical Paths

**Purpose**: Run these checks in under 15 minutes before any QA hand-off.
**Run via**: `/smoke-check` (which reads this file)
**Update**: Add new entries when new core systems are implemented.

## Core Stability (always run)

1. Game launches without crash; the three persistent scenes (UI, Player, Foundation) load at boot, depot loads after night-begin setup (ADR-0015 boot contract)
2. A night session starts: round 0 items are active and focusable in the depot (`StartNight` ran before depot activation — the restore-index −1 soft-lock never occurs)
3. Input responds: movement, look, Interact — no freeze

## Core Mechanic (update per sprint)

<!-- Add the primary mechanic for each sprint here as it is implemented -->
4. [Foundation sprint] Crosshair Idle↔Focused transitions work on a decoy interactable; Hold-fill appears on a Hold target and is suppressed on a memory trigger
5. [Carry loop sprint] Pick up → elevator → deliver → next round activates; "Eller Dolu" prompt at slot cap
6. [Memory trigger sprint] Hold-complete fires the light shift + stinger together (compound effect); trigger stays Committed after an elevator round trip
7. [End-condition sprint] Task-completion ending (Abrupt=false, crossfade) and saturation ending (Abrupt=true, cut) both reach the psychiatry scene with zero black frames

## Data Integrity (in-memory session state — no save file in MVP)

8. Fired/Persistent/Settled trigger state survives a depot↔ballroom round trip
9. Collected items never reappear after scene reload; uncollected ones always do
10. Editor: two consecutive Play sessions (Domain Reload off) start clean — no phantom fired triggers, no doubled stingers

## Performance

11. No visible frame drops on target hardware (60fps / 16.6ms budget)
12. No memory growth over 5 minutes of play (once core loop is implemented)
