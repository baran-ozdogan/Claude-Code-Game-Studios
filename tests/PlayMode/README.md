# Play Mode Tests

Integration tests that run in a real game scene.
Use for cross-system interactions, `MonoBehaviour` lifecycle, coroutines,
scene loading, and the two-session Reload-Scene simulations the ADRs require.

Assembly definition: `game/Assets/Tests/PlayMode/PlayModeTests.asmdef`
(created in Story 002 — currently empty; first PlayMode tests arrive with the
persistent-scene and two-session stories).

First candidates (per the Accepted ADRs' Validation Criteria):
- Domain-Reload-off / Reload-Scene-off two-session tests for every static
  facade and persistent-scene singleton (ADR-0001/0003/0004/0008/0009/0010/
  0011/0013/0015 — exactly-once event delivery, no stale `Instance`)
- `ShiftZone` Volume/Light lockstep + `OnDestroy` completion guarantee (ADR-0005)
- Zero-frame HARD CUT swap: frame-delta epsilon + zero-black-frame capture (ADR-0008)
- `AmbientZoneVolume` co-residency guard (ADR-0009)
- SphereCast occlusion with a physical scene setup (ADR-0010)
- `CarryItemPickup` OnEnable-top restore across depot round trip (ADR-0013)
- Boot contract: persistent scenes only in initial load; depot loads after
  `StartNight` (ADR-0015)
