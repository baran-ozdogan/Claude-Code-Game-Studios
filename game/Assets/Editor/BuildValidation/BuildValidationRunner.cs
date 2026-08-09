using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

/// <summary>
/// Abstraction over build-scene enumeration/opening so the test harness can
/// verify scene-scan behavior without touching real scenes (expensive step —
/// control manifest guardrail).
/// </summary>
internal interface IBuildSceneWalker
{
    IReadOnlyList<string> EnabledScenePaths { get; }

    void OpenScene(string scenePath);
}

/// <summary>Production walker over EditorBuildSettings.scenes.</summary>
internal sealed class EditorBuildSceneWalker : IBuildSceneWalker
{
    public IReadOnlyList<string> EnabledScenePaths
    {
        get
        {
            var paths = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    paths.Add(scene.path);
                }
            }
            return paths;
        }
    }

    public void OpenScene(string scenePath) =>
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
}

/// <summary>
/// The project's ONE IPreprocessBuildWithReport (ADR-0014). Runs every
/// registered check in two phases — cheap asset scan first, then the expensive
/// per-scene scan only if any scene-dependent check is registered. A violation
/// throws BuildFailedException (via BuildCheckContext.Fail) and stops the build.
/// </summary>
internal sealed class BuildValidationRunner : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) =>
        RunAll(BuildValidationRegistry.Checks, new EditorBuildSceneWalker());

    internal static void RunAll(IReadOnlyList<IBuildCheck> checks, IBuildSceneWalker sceneWalker)
    {
        // Phase 1 — AssetScan (always first).
        foreach (IBuildCheck check in checks)
        {
            if (check.Phase == BuildCheckPhase.AssetScan)
            {
                check.Run(new BuildCheckContext(check.Name, null));
            }
        }

        // Phase 2 — SceneScan: skip entirely (no scene gets opened) when no
        // scene-dependent check is registered.
        var sceneChecks = new List<IBuildCheck>();
        foreach (IBuildCheck check in checks)
        {
            if (check.Phase == BuildCheckPhase.SceneScan)
            {
                sceneChecks.Add(check);
            }
        }

        if (sceneChecks.Count == 0)
        {
            return;
        }

        // Aggregate begin — biriken gözlemler yürüyüşün BAŞINDA sıfırlanır
        // (ortada patlayan yürüyüşün bayat gözlemi sonraki build'e sızamaz).
        foreach (IBuildCheck check in sceneChecks)
        {
            if (check is IBuildCheckAggregate aggregate)
            {
                aggregate.BeginWalk();
            }
        }

        foreach (string scenePath in sceneWalker.EnabledScenePaths)
        {
            sceneWalker.OpenScene(scenePath);
            foreach (IBuildCheck check in sceneChecks)
            {
                check.Run(new BuildCheckContext(check.Name, scenePath));
            }
        }

        // Aggregate finalize — sahneler-arası toplam iddiaları yürüyüş bittikten
        // sonra değerlendirilir (sıfır sahne yürüyüşünde de: "hiç yok" da bir
        // toplam sonuçtur). Eklendi: isik-volume Story 006 (TR-isik-021).
        foreach (IBuildCheck check in sceneChecks)
        {
            if (check is IBuildCheckAggregate aggregate)
            {
                aggregate.FinalizeWalk(new BuildCheckContext(check.Name, null));
            }
        }
    }
}
