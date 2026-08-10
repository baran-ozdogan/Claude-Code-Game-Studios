using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot batch setup for Story 004: replaces Player.unity's empty skeleton
/// root (proje-kurulumu Story 004's placeholder) with an INSTANCE of Story 003's
/// Assets/Prefabs/Player.prefab. The controller is not rebuilt here — the prefab
/// is the single source. Run via:
///   Unity.exe -batchmode -quit -executeMethod Story004PlayerSceneSetup.Run
/// Deletable once the scene is committed.
/// </summary>
public static class Story004PlayerSceneSetup
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (prefab == null)
        {
            Debug.LogError("[Story004PlayerSceneSetup] Assets/Prefabs/Player.prefab not found (Story 003 output missing).");
            EditorApplication.Exit(1);
            return;
        }

        Scene playerScene = EditorSceneManager.OpenScene("Assets/Scenes/Player.unity", OpenSceneMode.Single);

        // Idempotency check FIRST — never destroy anything on a re-run (an early return
        // after destroying would leave the open scene silently mutated in an interactive
        // Editor, one Ctrl+S from persisting the deletion).
        foreach (GameObject root in playerScene.GetRootGameObjects())
        {
            if (root.GetComponent<FirstPersonController>() != null)
            {
                Debug.Log("[Story004PlayerSceneSetup] Player instance already present — nothing to do.");
                return;
            }
        }

        // Drop the empty placeholder root(s) — anything without a CharacterController.
        foreach (GameObject root in playerScene.GetRootGameObjects())
        {
            if (root.GetComponent<CharacterController>() == null)
            {
                Object.DestroyImmediate(root);
            }
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, playerScene);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        EditorSceneManager.MarkSceneDirty(playerScene);
        EditorSceneManager.SaveScene(playerScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Story004PlayerSceneSetup] Player.prefab instance placed into Player.unity.");
    }
}
