# Edit Mode Tests

Unit tests that run without entering Play Mode.
Use for pure logic: formulas, state machines, data validation, and the shared
`IPreprocessBuildWithReport` build-validation checks.

Assembly definition: `game/Assets/Tests/EditMode/EditModeTests.asmdef`
(created in Story 002 — references `UnityEngine.TestRunner`/`UnityEditor.TestRunner`,
editor-only platform; game code assembly references will be added as the
Foundation asmdefs appear from Story 003 on). First test:
`game/Assets/Tests/EditMode/foundation_sanity_test.cs`.

First candidates (per the Accepted ADRs' Validation Criteria):
- `PlayerStateProvider` lock bookkeeping (ADR-0003 — bare `AddComponent`, no rig)
- `InteractionStateMachine` full cycle + hold_progress values (ADR-0010)
- `ElevatorStateMachine` ride cycle + no-auto-return (ADR-0011)
- `DialogueCallbackSelector` filter/priority/stability (ADR-0012)
- `CarryLoopStateMachine` AC9a mocked happy path (ADR-0013)
- `EndConditionStateMachine` gate/deferral/tie matrix (ADR-0015)
- Build-validation throws/doesn't-throw pairs (ADR-0007/0012/0013/0014/0015)

Fixture rule: `ScriptableObject.CreateInstance` only — never on-disk fixture
assets (they would trip the project-wide `AssetDatabase.FindAssets` validation
scans, ADR-0014).
