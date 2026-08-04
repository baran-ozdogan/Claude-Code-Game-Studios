// PROTOTYPE - NOT FOR PRODUCTION
// Question: does Volume.weight direct-write bypass Unity's collider-distance
// blend entirely, as the GDD's Core Rules claim, or does blendDistance still
// multiply against it?
// Date: 2026-08-01
//
// Editor-only automation. Builds one of three test corridors in one click.
// Menu: Yankilar Spike > Build Corridor A/B/C

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VolumeWeightSpikeSceneBuilder
{
    private const float RTrigger = 4f;
    private const float RExit = 4.6f;
    private const float CorridorLength = 26f;
    private const float CorridorWidth = 3f;
    private const float CorridorHeight = 3f;

    [MenuItem("Yankilar Spike/Build Corridor A (undersized box, blendDistance=2)")]
    public static void BuildCorridorA() => BuildCorridor("CorridorA_undersized_blend2", boxHalfExtent: RTrigger, blendDistance: 2f);

    [MenuItem("Yankilar Spike/Build Corridor B (undersized box, blendDistance=0)")]
    public static void BuildCorridorB() => BuildCorridor("CorridorB_undersized_blend0", boxHalfExtent: RTrigger, blendDistance: 0f);

    [MenuItem("Yankilar Spike/Build Corridor C (correctly-sized box, blendDistance=0)")]
    public static void BuildCorridorC() => BuildCorridor("CorridorC_correctsize_blend0", boxHalfExtent: 10f, blendDistance: 0f);

    private static void BuildCorridor(string sceneName, float boxHalfExtent, float blendDistance)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Corridor runs along +Z, centered at Z=0, from Z=-13 to Z=+13.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(CorridorWidth / 10f, 1f, CorridorLength / 10f);

        CreateWall("Wall_Left", new Vector3(-CorridorWidth / 2f, CorridorHeight / 2f, 0f), new Vector3(0.2f, CorridorHeight, CorridorLength));
        CreateWall("Wall_Right", new Vector3(CorridorWidth / 2f, CorridorHeight / 2f, 0f), new Vector3(0.2f, CorridorHeight, CorridorLength));
        CreateWall("Ceiling", new Vector3(0f, CorridorHeight, 0f), new Vector3(CorridorWidth, 0.2f, CorridorLength));

        // Baseline warm-amber lights spaced down the corridor.
        for (float z = -10f; z <= 10f; z += 5f)
            CreateLight($"RealityLight_{z}", new Vector3(0f, CorridorHeight - 0.4f, z));

        // Local Volume (isGlobal = false) at the corridor midpoint.
        GameObject zoneGO = new GameObject("TriggerZone");
        zoneGO.transform.position = Vector3.zero;

        BoxCollider box = zoneGO.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(CorridorWidth, CorridorHeight, boxHalfExtent * 2f);

        Volume volume = zoneGO.AddComponent<Volume>();
        volume.isGlobal = false;
        volume.blendDistance = blendDistance;
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
        AssetDatabase.CreateAsset(profile, $"Assets/PrototypeGenerated/{sceneName}_Profile.asset");
        volume.sharedProfile = profile;

        zoneGO.AddComponent<BlendDistanceGuard>();

        // Player: starts well before the trigger, walks straight down +Z.
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, -12f);
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        GameObject camGO = new GameObject("PlayerCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<UniversalAdditionalCameraData>();

        SpikeFirstPersonWalker walker = player.AddComponent<SpikeFirstPersonWalker>();
        walker.eyeCamera = cam;

        SpikeZoneController zoneController = zoneGO.AddComponent<SpikeZoneController>();
        zoneController.player = player.transform;
        zoneController.targetVolume = volume;
        zoneController.zoneBox = box;
        zoneController.rTrigger = RTrigger;
        zoneController.rExit = RExit;
        zoneController.shiftOutDuration = 3f;

        string path = $"Assets/PrototypeGenerated/{sceneName}.unity";
        EditorSceneManager.SaveScene(scene, path);

        Debug.Log($"Spike scene built: {path} — boxHalfExtent={boxHalfExtent}m, blendDistance={blendDistance}. " +
                   "Press Play. WASD to move (capped 1.6 m/s), mouse to look. Walk straight down +Z and don't stop.");
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
