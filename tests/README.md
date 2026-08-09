# Test Infrastructure

**Engine**: Unity 6.5 (6000.5.6f1)
**Test Framework**: Unity Test Framework (NUnit)
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-08-09

## Directory Layout

```
tests/
  unit/           # Isolated unit tests (formulas, state machines, logic)
  integration/    # Cross-system and scene-lifecycle tests
  smoke/          # Critical path test list for /smoke-check gate
  evidence/       # Screenshot logs and manual test sign-off records
  EditMode/       # Unity Edit Mode test assembly (pure logic, no Play Mode)
  PlayMode/       # Unity Play Mode test assembly (scenes, coroutines, lifecycle)
```

`unit/` and `integration/` hold this project's *organizational* layout (one
subdirectory per system, e.g. `tests/unit/gorev-tasima/`); the Unity-side
assemblies that actually compile and run them live inside the Unity project:
`game/Assets/Tests/EditMode/EditModeTests.asmdef` and
`game/Assets/Tests/PlayMode/PlayModeTests.asmdef` (created in Story 002; the
`EditMode/`/`PlayMode/` folders here hold their READMEs and candidate lists).
A unit test for a pure C# state machine (e.g. `CarryLoopStateMachine`,
`EndConditionStateMachine`) belongs in the EditMode assembly; anything needing
a scene, `MonoBehaviour` lifecycle, or the "Reload Scene: Off" two-session
simulation belongs in PlayMode.

## Enabling Unity Test Framework

```
Window → General → Test Runner
(Unity Test Framework is included by default in Unity 2019+ / Unity 6)
```

## Running Tests

- **Editor**: Window → General → Test Runner → Run All (EditMode and PlayMode tabs)
- **CI**: runs automatically via `game-ci/unity-test-runner@v4` on every push/PR to `main`

## Test Naming

- **Files**: `[system]_[feature]_test.cs` (project convention) — class names PascalCase per C# standards
- **Functions**: `test_[scenario]_[expected]` → NUnit: `Test_[Scenario]_[Expected]` or descriptive PascalCase
- **Example**: `gorev_carry_loop_test.cs` → `TryPickUp_SlotsFull_RejectsWithoutStateChange()`

## Determinism & Isolation Rules (from `.claude/docs/coding-standards.md`)

- Same result every run — no random seeds, no time-dependent assertions
- Each test sets up and tears down its own state (deregister test doubles from
  `InteractableRegistry` in teardown; construct fresh `...State` instances
  directly per ADR-0001's testability pattern — never through the static facades)
- No file I/O / external APIs in unit tests — dependency injection
- EditMode test fixtures use `ScriptableObject.CreateInstance`, **never on-disk
  fixture assets** — the shared `IPreprocessBuildWithReport` validation pass
  scans the whole project with `AssetDatabase.FindAssets` and would fail the
  build on test data (ADR-0014's documented caveat)

## Story Type → Test Evidence

| Story Type | Required Evidence | Location | Gate |
|---|---|---|---|
| Logic | Automated unit test — must pass | `tests/unit/[system]/` | BLOCKING |
| Integration | Integration test OR playtest doc | `tests/integration/[system]/` | BLOCKING |
| Visual/Feel | Screenshot + lead sign-off | `tests/evidence/` | ADVISORY |
| UI | Manual walkthrough OR interaction test | `tests/evidence/` | ADVISORY |
| Config/Data | Smoke check pass | `production/qa/smoke-*.md` | ADVISORY |

## Project-Specific Test Obligations (from the Accepted ADRs)

The ADRs' Validation Criteria sections define the concrete first tests each
Foundation story must ship with. The recurring patterns:

- **Two-session Reload-Scene tests**: a `[UnityTest]` with "Reload Scene"
  disabled, running two simulated Play sessions across a
  `FoundationBootstrap.ResetAll()` boundary — required by ADR-0001/0003/0004/
  0008/0009/0010/0011/0013/0015 for every static facade and persistent-scene
  singleton (exactly-once event delivery, no stale `Instance`, no
  subscription accumulation).
- **Pure state-machine matrices**: `[Test]`-only, stub-delegate-driven —
  `InteractionStateMachine`, `ElevatorStateMachine`, `CarryLoopStateMachine`,
  `EndConditionStateMachine`.
- **EditMode build-validation tests**: the shared `IPreprocessBuildWithReport`
  utility's checks (clue registry, task lists, memory triggers, night config)
  each need a throws/doesn't-throw pair.

## CI

Tests run automatically on every push to `main` and on every pull request.
A failed test suite blocks merging. Never disable or skip a failing test to
make CI pass — fix the underlying issue.

**Setup prerequisite**: add the `UNITY_EMAIL` and `UNITY_PASSWORD` secrets to the
GitHub repository (Settings → Secrets and variables → Actions) before the first
CI run. Unity no longer supports manual `.ulf` activation for Personal licenses
(license.unity3d.com/manual now rejects them, 2026-08-09), so game-ci activates
with Unity account credentials instead — see https://game.ci/docs/github/activation.
