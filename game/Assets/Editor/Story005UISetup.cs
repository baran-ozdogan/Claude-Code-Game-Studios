using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// One-shot batch setup for Story 005: creates the shared PanelSettings asset
/// and wires the UI scene's UIRoot object (UIDocument + UIRoot accessor +
/// MainUI.uxml). Run via:
///   Unity.exe -batchmode -quit -executeMethod Story005UISetup.Run
/// Deletable once the scene/assets are committed.
/// </summary>
public static class Story005UISetup
{
    public static void Run()
    {
        var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/UnityDefaultRuntimeTheme.tss");
        if (theme == null)
        {
            Debug.LogError("[Story005UISetup] Theme TSS not found at Assets/UI/UnityDefaultRuntimeTheme.tss");
            EditorApplication.Exit(1);
            return;
        }

        var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.themeStyleSheet = theme;
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1920, 1080);
        AssetDatabase.CreateAsset(panelSettings, "Assets/UI/MainPanelSettings.asset");

        var mainUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainUI.uxml");
        if (mainUxml == null)
        {
            Debug.LogError("[Story005UISetup] MainUI.uxml not found/imported.");
            EditorApplication.Exit(1);
            return;
        }

        Scene uiScene = EditorSceneManager.OpenScene("Assets/Scenes/UI.unity", OpenSceneMode.Single);
        GameObject uiRootGo = GameObject.Find("UIRoot");
        if (uiRootGo == null)
        {
            Debug.LogError("[Story005UISetup] UIRoot object not found in UI.unity (Story 004 output missing).");
            EditorApplication.Exit(1);
            return;
        }

        var uiDocument = uiRootGo.GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = uiRootGo.AddComponent<UIDocument>();
        }
        uiDocument.panelSettings = panelSettings;
        uiDocument.visualTreeAsset = mainUxml;

        var uiRoot = uiRootGo.GetComponent<UIRoot>();
        if (uiRoot == null)
        {
            uiRoot = uiRootGo.AddComponent<UIRoot>();
        }

        var serialized = new SerializedObject(uiRoot);
        serialized.FindProperty("_uiDocument").objectReferenceValue = uiDocument;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(uiScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Story005UISetup] PanelSettings created; UIRoot wired with UIDocument + MainUI.uxml.");
    }
}
