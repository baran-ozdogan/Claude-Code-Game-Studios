using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot batch setup for Story 004: creates the three persistent scene
/// assets (UI/Player/Foundation) with their skeleton roots and pins Build
/// Settings to exactly those three scenes (UI first). Run via:
///   Unity.exe -batchmode -quit -executeMethod Story004SceneSetup.Run
/// Deletable once the scenes are committed.
/// </summary>
public static class Story004SceneSetup
{
    public static void Run()
    {
        CreateScene("Assets/Scenes/UI.unity", "UIRoot", attachBootLoader: true);
        CreateScene("Assets/Scenes/Player.unity", "Player", attachBootLoader: false);
        CreateScene("Assets/Scenes/Foundation.unity", "FoundationRoot", attachBootLoader: false);

        // Build Settings: ONLY the three persistent scenes, UI first (initial
        // scene). Level scenes added later stay unchecked — they load only via
        // SceneTransitionManager (ADR-0015 boot contract).
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/UI.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Player.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Foundation.unity", true),
        };

        AssetDatabase.SaveAssets();
        Debug.Log("[Story004SceneSetup] Persistent scenes created; Build Settings pinned to UI/Player/Foundation.");
    }

    private static void CreateScene(string path, string rootName, bool attachBootLoader)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject(rootName);
        if (attachBootLoader)
        {
            root.AddComponent<PersistentSceneBootLoader>();
        }
        EditorSceneManager.SaveScene(scene, path);
    }
}
