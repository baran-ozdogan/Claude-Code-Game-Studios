using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// First EditMode tests of the project (Story 002). Deterministic sanity checks
/// that lock in Story 001's project-configuration decisions so a settings
/// regression fails CI instead of surfacing as a rendering/input bug later.
/// File name follows the project convention [system]_[feature]_test.cs.
/// </summary>
public class FoundationSanityTest
{
    [Test]
    public void ColorSpace_ProjectSetting_IsLinear()
    {
        // Arrange — Story 001 AC: Linear color space is a locked project setting.
        const ColorSpace expected = ColorSpace.Linear;

        // Act
        ColorSpace actual = PlayerSettings.colorSpace;

        // Assert
        Assert.AreEqual(expected, actual,
            "Project color space must stay Linear (Story 001 / URP kararı).");
    }

    [Test]
    public void ApiCompatibility_Standalone_IsNetStandard()
    {
        // Arrange — Story 001 AC: Api Compatibility Level = .NET Standard 2.1.
        const ApiCompatibilityLevel expected = ApiCompatibilityLevel.NET_Standard;

        // Act
        ApiCompatibilityLevel actual =
            PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone);

        // Assert
        Assert.AreEqual(expected, actual,
            "Standalone Api Compatibility Level must stay .NET Standard 2.1 (Story 001).");
    }

    [Test]
    public void ScriptFolders_LayerLayout_AllFourExist()
    {
        // Arrange — Story 001 AC: layer folder structure under Assets/Scripts/.
        string[] requiredFolders =
        {
            "Assets/Scripts/Foundation",
            "Assets/Scripts/Core",
            "Assets/Scripts/Feature",
            "Assets/Scripts/Presentation",
        };

        foreach (string folder in requiredFolders)
        {
            // Act
            bool exists = AssetDatabase.IsValidFolder(folder);

            // Assert
            Assert.IsTrue(exists,
                $"Layer folder missing: {folder} (Story 001'in yapısal kararı).");
        }
    }
}
