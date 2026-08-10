using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot batch setup for Story 003: builds the reusable Player prefab
/// (CharacterController + Eye/Camera child + PlayerStateProvider +
/// FirstPersonController, serialized fields wired) and saves it to
/// Assets/Prefabs/Player.prefab. Story 004 places an instance of this prefab
/// into Player.unity — the controller is NOT built twice. Run via:
///   Unity.exe -batchmode -quit -executeMethod Story003PlayerPrefabSetup.Run
/// Deletable once the prefab is committed.
/// </summary>
public static class Story003PlayerPrefabSetup
{
    public static void Run()
    {
        var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/Input/Gameplay.inputactions");
        if (inputActions == null)
        {
            Debug.LogError("[Story003PlayerPrefabSetup] Gameplay.inputactions not found at Assets/Input/Gameplay.inputactions.");
            EditorApplication.Exit(1);
            return;
        }

        var root = new GameObject("Player");
        // Control manifest (Core katmanı, ADR-0011): oyuncu `CompareTag("Player")` ile bulunur.
        root.tag = "Player";

        var characterController = root.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.35f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = FirstPersonController.LockedStepOffset;
        characterController.skinWidth = characterController.radius * FirstPersonController.SkinWidthToRadiusRatio;

        var eyeGo = new GameObject("Eye");
        // Camera.main'in çözülebilmesi için (tüketiciler bunu bekler).
        eyeGo.tag = "MainCamera";
        eyeGo.transform.SetParent(root.transform, worldPositionStays: false);
        eyeGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var eyeCamera = eyeGo.AddComponent<Camera>();
        eyeCamera.fieldOfView = 75f;
        eyeGo.AddComponent<AudioListener>();

        root.AddComponent<PlayerStateProvider>();
        var fpc = root.AddComponent<FirstPersonController>();

        var serialized = new SerializedObject(fpc);
        serialized.FindProperty("_characterController").objectReferenceValue = characterController;
        serialized.FindProperty("_eyeCamera").objectReferenceValue = eyeGo.transform;
        serialized.FindProperty("_inputActions").objectReferenceValue = inputActions;
        serialized.FindProperty("_controllerHitInterestMask").FindPropertyRelative("m_Bits").intValue = 0;
        serialized.FindProperty("_rampTime").floatValue = 0.2f;
        serialized.FindProperty("_mouseSensitivity").floatValue = 0.12f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Player.prefab");
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        Debug.Log("[Story003PlayerPrefabSetup] Player prefab created at Assets/Prefabs/Player.prefab.");
    }
}
