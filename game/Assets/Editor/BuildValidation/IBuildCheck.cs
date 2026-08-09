using UnityEditor.Build;

/// <summary>Which pass of the shared build-validation utility a check runs in (ADR-0014).</summary>
internal enum BuildCheckPhase
{
    /// <summary>Project-wide asset scan (AssetDatabase.FindAssets) — cheap, always runs first.</summary>
    AssetScan,

    /// <summary>Per-scene scan over EditorBuildSettings.scenes — expensive, only for scene-dependent checks.</summary>
    SceneScan,
}

/// <summary>
/// One registered build-blocking validation check. System epics implement this
/// and add an instance to BuildValidationRegistry.Checks — never a second
/// independent IPreprocessBuildWithReport (control manifest, ADR-0014).
/// </summary>
internal interface IBuildCheck
{
    /// <summary>Short identifying name, included in every failure message.</summary>
    string Name { get; }

    /// <summary>Which pass this check runs in.</summary>
    BuildCheckPhase Phase { get; }

    /// <summary>Runs the check; report violations via context.Fail (throws, stops the build).</summary>
    void Run(BuildCheckContext context);
}

/// <summary>
/// Per-invocation context handed to a check. SceneScan checks receive the path
/// of the currently opened scene; AssetScan checks receive null.
/// </summary>
internal sealed class BuildCheckContext
{
    public string CheckName { get; }

    /// <summary>Path of the scene currently open for this check — null during AssetScan.</summary>
    public string ScenePath { get; }

    internal BuildCheckContext(string checkName, string scenePath)
    {
        CheckName = checkName;
        ScenePath = scenePath;
    }

    /// <summary>
    /// Fails the build immediately with a pointed message. Include the offending
    /// asset path / object name in <paramref name="message"/> (control manifest:
    /// pointed error messages, never a runtime clamp).
    /// </summary>
    public void Fail(string message)
    {
        string prefix = ScenePath == null
            ? $"[Build Validation] {CheckName}: "
            : $"[Build Validation] {CheckName} (scene: {ScenePath}): ";
        throw new BuildFailedException(prefix + message);
    }
}
