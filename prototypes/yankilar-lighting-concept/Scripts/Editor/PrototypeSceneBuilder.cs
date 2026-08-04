// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a lighting/color-temperature shift alone create felt unease?
// Date: 2026-08-01
//
// Editor-only automation. Builds the entire test scene in one click so the
// tester doesn't have to hand-place GameObjects or wire Inspector references.
// Menu: Yankilar Prototype > Build Prototype Scene

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PrototypeSceneBuilder
{
    [MenuItem("Yankilar Prototype/Build Prototype Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Corridor blockout (3m wide x 8m long x 3m tall) ---
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(0.3f, 1f, 0.8f); // Unity's default Plane is 10x10

        CreateWall("Wall_Left", new Vector3(-1.5f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f));
        CreateWall("Wall_Right", new Vector3(1.5f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f));
        CreateWall("Wall_Far", new Vector3(0f, 1.5f, 4f), new Vector3(3f, 3f, 0.2f));
        CreateWall("Wall_Near", new Vector3(0f, 1.5f, -4f), new Vector3(3f, 3f, 0.2f));
        CreateWall("Ceiling", new Vector3(0f, 3f, 0f), new Vector3(3f, 0.2f, 8f));

        // --- Baseline "reality" lighting (warm amber) ---
        Light lightA = CreateLight("RealityLight_A", new Vector3(0f, 2.6f, -2.5f));
        Light lightB = CreateLight("RealityLight_B", new Vector3(0f, 2.6f, 2.5f));

        // --- Global Volume: pre-authored cold/memory profile, weight driven at runtime ---
        GameObject volumeGO = new GameObject("Global Volume");
        Volume volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 0f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.temperature.overrideState = true;
        whiteBalance.temperature.value = -60f;
        whiteBalance.tint.overrideState = true;
        whiteBalance.tint.value = 10f;

        var colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = -0.5f;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = -20f;

        Directory.CreateDirectory("Assets/PrototypeGenerated");
        AssetDatabase.CreateAsset(profile, "Assets/PrototypeGenerated/MemoryShiftProfile.asset");
        volume.sharedProfile = profile;

        // --- Player ---
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, -3.5f);
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        GameObject camGO = new GameObject("PlayerCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<UniversalAdditionalCameraData>();

        FirstPersonWalker walker = player.AddComponent<FirstPersonWalker>();
        walker.eyeCamera = cam;
        Interactor interactor = player.AddComponent<Interactor>();
        interactor.eyeCamera = cam;

        // --- Memory-trigger object ---
        GameObject triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        triggerObj.name = "MemoryTrigger";
        triggerObj.transform.position = new Vector3(0f, 0.5f, 1f);
        triggerObj.transform.localScale = Vector3.one * 0.3f;

        MemoryTrigger memTrigger = triggerObj.AddComponent<MemoryTrigger>();
        memTrigger.targetVolume = volume;
        memTrigger.realityLights = new[] { lightA, lightB };
        memTrigger.shiftDuration = 3f;
        memTrigger.coldColor = new Color(0.56f, 0.66f, 0.76f);
        memTrigger.coldIntensityMultiplier = 0.6f;

        EditorSceneManager.SaveScene(scene, "Assets/PrototypeGenerated/YankilarLightingPrototype.unity");

        Debug.Log("Prototype scene built: Assets/PrototypeGenerated/YankilarLightingPrototype.unity — " +
                  "press Play. WASD to move, mouse to look, E to interact with the small cube.");
    }

    private static GameObject CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        return wall;
    }

    private static Light CreateLight(string name, Vector3 position)
    {
        GameObject lightGO = new GameObject(name);
        lightGO.transform.position = position;
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.72f, 0.44f); // warm amber
        light.intensity = 4f;
        light.range = 6f;
        return light;
    }
}
