using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-shot batch-mode setup for Story 001 (Unity proje init).
/// Creates the URP pipeline asset + renderer, assigns them, sets player
/// settings (Linear, .NET Standard 2.1), creates the layer folder structure
/// and an empty URP test scene. Run via:
///   Unity.exe -batchmode -quit -executeMethod ProjectInitSetup.Run
/// This script lives in Assets/Editor/ and can be deleted once setup is verified.
/// </summary>
public static class ProjectInitSetup
{
    public static void Run()
    {
        AddAddressablesPackage();
        CreateFolders();
        var pipeline = CreateAndAssignUrp();
        ConfigurePlayerSettings();
        CreateEmptyTestScene(pipeline);
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectInitSetup] Completed successfully.");
    }

    private static void AddAddressablesPackage()
    {
        AddRequest request = Client.Add("com.unity.addressables");
        while (!request.IsCompleted)
        {
            Thread.Sleep(200);
        }

        if (request.Status == StatusCode.Success)
        {
            Debug.Log($"[ProjectInitSetup] Addressables added: {request.Result.version}");
        }
        else
        {
            Debug.LogError($"[ProjectInitSetup] Addressables add FAILED: {request.Error?.message}");
            EditorApplication.Exit(1);
        }
    }

    private static void CreateFolders()
    {
        string[] folders =
        {
            "Assets/Scripts/Foundation",
            "Assets/Scripts/Core",
            "Assets/Scripts/Feature",
            "Assets/Scripts/Presentation",
            "Assets/Scenes",
            "Assets/Settings",
            "Assets/Input",
        };
        foreach (string folder in folders)
        {
            Directory.CreateDirectory(folder);
        }
        AssetDatabase.Refresh();
    }

    private static UniversalRenderPipelineAsset CreateAndAssignUrp()
    {
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, "Assets/Settings/PC_Renderer.asset");

        var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(pipelineAsset, "Assets/Settings/PC_RPAsset.asset");

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;
        Debug.Log("[ProjectInitSetup] URP asset + renderer created and assigned.");
        return pipelineAsset;
    }

    private static void ConfigurePlayerSettings()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
        Debug.Log("[ProjectInitSetup] Linear color space + .NET Standard 2.1 set.");
    }

    private static void CreateEmptyTestScene(UniversalRenderPipelineAsset pipeline)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/EmptyTest.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/EmptyTest.unity", true),
        };
        Debug.Log("[ProjectInitSetup] EmptyTest scene saved and added to build settings.");
    }
}
