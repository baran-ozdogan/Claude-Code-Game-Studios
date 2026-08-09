using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Build;

/// <summary>
/// Story 006 AC-3/5: the shared build-validation shell's two-phase flow,
/// BuildFailedException behavior, and scene-scan isolation — driven entirely
/// through fakes (no real assets or scenes touched).
/// </summary>
public class BuildValidationHarnessTest
{
    private sealed class FakeCheck : IBuildCheck
    {
        private readonly Action<BuildCheckContext> _body;

        public FakeCheck(string name, BuildCheckPhase phase, Action<BuildCheckContext> body = null)
        {
            Name = name;
            Phase = phase;
            _body = body;
        }

        public string Name { get; }
        public BuildCheckPhase Phase { get; }
        public List<string> ObservedScenePaths { get; } = new List<string>();

        public void Run(BuildCheckContext context)
        {
            ObservedScenePaths.Add(context.ScenePath);
            _body?.Invoke(context);
        }
    }

    private sealed class FakeSceneWalker : IBuildSceneWalker
    {
        private readonly string[] _paths;

        public FakeSceneWalker(params string[] paths) => _paths = paths;

        public IReadOnlyList<string> EnabledScenePaths => _paths;
        public List<string> OpenedScenes { get; } = new List<string>();

        public void OpenScene(string scenePath) => OpenedScenes.Add(scenePath);
    }

    [Test]
    public void RunAll_ZeroChecks_NoOpAndNoSceneOpened()
    {
        // Arrange
        var walker = new FakeSceneWalker("Assets/Scenes/A.unity");

        // Act
        BuildValidationRunner.RunAll(Array.Empty<IBuildCheck>(), walker);

        // Assert
        Assert.IsEmpty(walker.OpenedScenes, "Sıfır check → hiçbir sahne açılmamalı (no-op).");
    }

    [Test]
    public void RunAll_PassingChecks_DoesNotThrow()
    {
        // Arrange
        var checks = new IBuildCheck[]
        {
            new FakeCheck("PassA", BuildCheckPhase.AssetScan),
            new FakeCheck("PassS", BuildCheckPhase.SceneScan),
        };
        var walker = new FakeSceneWalker("Assets/Scenes/A.unity");

        // Act + Assert
        Assert.DoesNotThrow(() => BuildValidationRunner.RunAll(checks, walker));
    }

    [Test]
    public void RunAll_FailingAssetCheck_ThrowsBuildFailedWithNameAndTarget()
    {
        // Arrange
        var checks = new IBuildCheck[]
        {
            new FakeCheck("OrphanShiftId", BuildCheckPhase.AssetScan,
                ctx => ctx.Fail("offending asset: Assets/Data/Broken.asset")),
        };
        var walker = new FakeSceneWalker();

        // Act
        var ex = Assert.Throws<BuildFailedException>(
            () => BuildValidationRunner.RunAll(checks, walker));

        // Assert — pointed message: check adı + offending yol (control manifest).
        StringAssert.Contains("OrphanShiftId", ex.Message);
        StringAssert.Contains("Assets/Data/Broken.asset", ex.Message);
    }

    [Test]
    public void RunAll_FailingSceneCheck_MessageContainsScenePath()
    {
        // Arrange
        var checks = new IBuildCheck[]
        {
            new FakeCheck("SceneRule", BuildCheckPhase.SceneScan,
                ctx => ctx.Fail("violation")),
        };
        var walker = new FakeSceneWalker("Assets/Scenes/Depot.unity");

        // Act
        var ex = Assert.Throws<BuildFailedException>(
            () => BuildValidationRunner.RunAll(checks, walker));

        // Assert
        StringAssert.Contains("Assets/Scenes/Depot.unity", ex.Message);
        StringAssert.Contains("SceneRule", ex.Message);
    }

    [Test]
    public void RunAll_OnlyAssetChecks_OpensNoScenes()
    {
        // Arrange — AC-3 edge case: pahalı sahne adımı atlanmalı.
        var checks = new IBuildCheck[]
        {
            new FakeCheck("AssetOnly1", BuildCheckPhase.AssetScan),
            new FakeCheck("AssetOnly2", BuildCheckPhase.AssetScan),
        };
        var walker = new FakeSceneWalker("Assets/Scenes/A.unity", "Assets/Scenes/B.unity");

        // Act
        BuildValidationRunner.RunAll(checks, walker);

        // Assert
        Assert.IsEmpty(walker.OpenedScenes, "Yalnız AssetScan kayıtlıyken sahne açılmamalı.");
    }

    [Test]
    public void RunAll_MixedPhases_AssetPhaseCompletesBeforeScenePhase()
    {
        // Arrange
        var executionLog = new List<string>();
        var checks = new IBuildCheck[]
        {
            new FakeCheck("S", BuildCheckPhase.SceneScan, _ => executionLog.Add("scene")),
            new FakeCheck("A", BuildCheckPhase.AssetScan, _ => executionLog.Add("asset")),
        };
        var walker = new FakeSceneWalker("Assets/Scenes/A.unity");

        // Act — kayıt sırası ters olsa bile faz sırası AssetScan→SceneScan.
        BuildValidationRunner.RunAll(checks, walker);

        // Assert
        CollectionAssert.AreEqual(new[] { "asset", "scene" }, executionLog);
    }

    [Test]
    public void RunAll_SceneChecks_RunOncePerEnabledSceneWithScenePath()
    {
        // Arrange
        var check = new FakeCheck("PerScene", BuildCheckPhase.SceneScan);
        var walker = new FakeSceneWalker("Assets/Scenes/A.unity", "Assets/Scenes/B.unity");

        // Act
        BuildValidationRunner.RunAll(new IBuildCheck[] { check }, walker);

        // Assert — her enabled sahne tam bir kez açılır ve context sahne yolunu taşır.
        CollectionAssert.AreEqual(
            new[] { "Assets/Scenes/A.unity", "Assets/Scenes/B.unity" }, walker.OpenedScenes);
        CollectionAssert.AreEqual(
            new[] { "Assets/Scenes/A.unity", "Assets/Scenes/B.unity" }, check.ObservedScenePaths);
    }

    [Test]
    public void RunAll_AssetChecks_ReceiveNullScenePath()
    {
        // Arrange
        var check = new FakeCheck("AssetCtx", BuildCheckPhase.AssetScan);

        // Act
        BuildValidationRunner.RunAll(new IBuildCheck[] { check }, new FakeSceneWalker());

        // Assert
        CollectionAssert.AreEqual(new string[] { null }, check.ObservedScenePaths);
    }
}
