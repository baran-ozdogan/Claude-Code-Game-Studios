// PROTOTYPE ONLY — one-click construction of a short playable greybox slice.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yankilar.Greybox.Editor
{
    public static class GreyboxSceneBuilder
    {
        [MenuItem("Yankılar/Greybox Demo Sahnesini Kur")]
        public static void BuildScene()
        {
            Directory.CreateDirectory("Assets/Generated");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material wall = MaterialWithColor("Eski sıva", new Color(.28f, .24f, .21f));
            Material floor = MaterialWithColor("Yıpranmış zemin", new Color(.105f, .095f, .085f));
            Material brass = MaterialWithColor("Pirinç", new Color(.57f, .37f, .13f));
            Material linen = MaterialWithColor("Keten", new Color(.75f, .71f, .61f));
            Material dark = MaterialWithColor("Karanlık", new Color(.05f, .06f, .055f));
            Material red = MaterialWithColor("Kırmızı", new Color(.65f, .12f, .08f));
            Material pink = MaterialWithColor("Çiçek", new Color(.68f, .24f, .36f));

            var corridorLights = BuildServiceCorridor(wall, floor, brass, linen, pink);
            BuildAnomalyLanding(wall, dark, red);
            BuildBallroom(wall, floor, linen, brass);

            var player = BuildPlayer(out Camera camera, out Transform carryAnchor);
            var game = new GameObject("GameState").AddComponent<GreyboxGameState>();
            SetField(game, "player", player.GetComponent<GreyboxPlayer>());
            SetField(game, "carryAnchor", carryAnchor);
            var interactor = player.AddComponent<GreyboxInteractor>();
            SetField(interactor, "playerCamera", camera);
            SetField(interactor, "gameState", game);

            var serviceExit = ExitMarker("ServiceElevatorExit", new Vector3(0, .05f, 7.15f), Quaternion.identity);
            var ballroomExit = ExitMarker("BallroomElevatorExit", new Vector3(0, .05f, 23.1f), Quaternion.identity);
            var anomalyExit = ExitMarker("AnomalyElevatorExit", new Vector3(0, .05f, 14.2f), Quaternion.identity);
            var ambient = new GameObject("AmbientHum").AddComponent<AmbientHum>();
            ambient.GetComponent<AudioSource>().playOnAwake = true;
            var coldVolume = CreateColdVolume();

            BuildInteractables(game, player.GetComponent<GreyboxPlayer>(), serviceExit, ballroomExit, anomalyExit,
                coldVolume, corridorLights, ambient, brass, linen, pink, red);

            EditorSceneManager.SaveScene(scene, "Assets/Generated/YankilarGreyboxDemo.unity");
            Debug.Log("Yankılar vertical-slice greybox hazır. Play'e bas: WASD, fare, E.");
        }

        private static Light[] BuildServiceCorridor(Material wall, Material floor, Material brass, Material linen, Material pink)
        {
            CreateBox("ServiceFloor", new Vector3(0, -.1f, 0), new Vector3(5, .2f, 21), floor);
            CreateBox("LeftWall", new Vector3(-2.5f, 1.5f, 0), new Vector3(.2f, 3, 21), wall);
            CreateBox("RightWall", new Vector3(2.5f, 1.5f, 0), new Vector3(.2f, 3, 21), wall);
            CreateBox("ServiceCeiling", new Vector3(0, 3, 0), new Vector3(5, .2f, 21), wall);
            CreateBox("StorageEnd", new Vector3(0, 1.5f, -10.5f), new Vector3(5, 3, .2f), wall);
            CreateBox("ElevatorEnd", new Vector3(0, 1.5f, 10.5f), new Vector3(5, 3, .2f), wall);
            CreateBox("TaskBoardPanel", new Vector3(-1.75f, 1.15f, -8.35f), new Vector3(.12f, 1.25f, .85f), linen);
            CreateBox("StorageShelf_A", new Vector3(-1.65f, .8f, -5.8f), new Vector3(.65f, 1.6f, 1.15f), brass);
            CreateBox("StorageShelf_B", new Vector3(1.65f, .8f, 3.7f), new Vector3(.65f, 1.6f, 1.15f), brass);
            CreateBox("LinenCart", new Vector3(.75f, .45f, -5.4f), new Vector3(.85f, .65f, .55f), linen);
            CreateBox("ChampagneCase", new Vector3(-.75f, .34f, -.65f), new Vector3(.7f, .45f, .5f), brass);
            CreateBox("FlowerCrate", new Vector3(.85f, .42f, 4.35f), new Vector3(.78f, .5f, .6f), pink);
            var glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glass.name = "CrackedChampagneGlass";
            glass.transform.position = new Vector3(1.65f, .43f, 1.35f);
            glass.transform.localScale = new Vector3(.16f, .43f, .16f);
            glass.GetComponent<Renderer>().material = linen;
            var lights = new Light[5];
            int index = 0;
            for (int z = -6; z <= 6; z += 3)
                lights[index++] = CreateLight("AmberSconce", new Vector3(0, 2.65f, z), new Color(1f, .58f, .27f), 4.5f, 7f);
            return lights;
        }

        private static void BuildAnomalyLanding(Material wall, Material dark, Material red)
        {
            CreateBox("AnomalyFloor", new Vector3(0, -.1f, 16.5f), new Vector3(5, .2f, 5), dark);
            CreateBox("AnomalyLeft", new Vector3(-2.5f, 1.5f, 16.5f), new Vector3(.2f, 3, 5), wall);
            CreateBox("AnomalyRight", new Vector3(2.5f, 1.5f, 16.5f), new Vector3(.2f, 3, 5), wall);
            CreateBox("AnomalyBack", new Vector3(0, 1.5f, 19), new Vector3(5, 3, .2f), wall);
            CreateBox("AnomalyCeiling", new Vector3(0, 3, 16.5f), new Vector3(5, .2f, 5), wall);
            CreateBox("BreakerCabinet", new Vector3(-1.35f, 1.15f, 17.15f), new Vector3(.25f, 1.7f, 1.1f), dark);
            CreateBox("BreakerLamp", new Vector3(-1.19f, 1.45f, 17.15f), new Vector3(.06f, .18f, .18f), red);
            CreateLight("AnomalyLight", new Vector3(0, 2.6f, 16.8f), new Color(.58f, .08f, .07f), 1.5f, 4.5f);
        }

        private static void BuildBallroom(Material wall, Material floor, Material linen, Material brass)
        {
            CreateBox("BallroomFloor", new Vector3(0, -.1f, 31.5f), new Vector3(13, .2f, 17), floor);
            CreateBox("BallroomBack", new Vector3(0, 2.5f, 40), new Vector3(13, 5, .2f), wall);
            CreateBox("BallroomLeft", new Vector3(-6.5f, 2.5f, 31.5f), new Vector3(.2f, 5, 17), wall);
            CreateBox("BallroomRight", new Vector3(6.5f, 2.5f, 31.5f), new Vector3(.2f, 5, 17), wall);
            CreateBox("BallroomCeiling", new Vector3(0, 5, 31.5f), new Vector3(13, .2f, 17), wall);
            for (int x = -3; x <= 3; x += 3) CreateLight("BallroomLight", new Vector3(x, 3.9f, 32), new Color(1f, .75f, .42f), 5.5f, 8f);
            CreateBox("MainWeddingTable", new Vector3(0, .85f, 35), new Vector3(3.4f, 1.5f, 1.8f), linen);
            CreateBox("DeliveryMarker", new Vector3(0, 1.67f, 35), new Vector3(.82f, .13f, .82f), brass);
            for (int x = -4; x <= 4; x += 4)
                CreateBox("EmptyGuestTable", new Vector3(x, .65f, 30), new Vector3(1.5f, 1.15f, 1.5f), linen);
        }

        private static GameObject BuildPlayer(out Camera camera, out Transform carryAnchor)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, .05f, -9.1f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.center = new Vector3(0, .9f, 0); controller.radius = .3f;
            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0, 1.62f, 0);
            camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(.025f, .025f, .03f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var script = player.AddComponent<GreyboxPlayer>();
            SetField(script, "playerCamera", camera);
            carryAnchor = new GameObject("CarryAnchor").transform;
            carryAnchor.SetParent(cameraObject.transform, false);
            return player;
        }

        private static void BuildInteractables(GreyboxGameState game, GreyboxPlayer player, Transform serviceExit,
            Transform ballroomExit, Transform anomalyExit, Volume volume, Light[] corridorLights, AmbientHum ambient,
            Material brass, Material linen, Material pink, Material red)
        {
            GameObject.Find("TaskBoardPanel").AddComponent<TaskBoard>();
            CreateCarryItem("LinenTask", new Vector3(.75f, .88f, -5.4f), "linen", "Masa örtüsü kutusu", linen.color);
            CreateCarryItem("ChampagneTask", new Vector3(-.75f, .82f, -.65f), "champagne", "Şampanya kasası", brass.color);
            CreateCarryItem("FlowersTask", new Vector3(.85f, .89f, 4.35f), "flowers", "Çiçek aranjmanı", pink.color);

            var memoryObject = GameObject.Find("CrackedChampagneGlass");
            var memory = memoryObject.AddComponent<MemoryObject>();
            SetField(memory, "coldVolume", volume); SetField(memory, "corridorLights", corridorLights);
            SetField(memory, "ambience", ambient); SetField(memory, "objectRenderer", memoryObject.GetComponent<Renderer>());

            GameObject.Find("DeliveryMarker").AddComponent<DeliveryZone>();
            var breaker = GameObject.Find("BreakerCabinet").AddComponent<BreakerPanel>();
            SetField(breaker, "lamp", GameObject.Find("BreakerLamp").GetComponent<Renderer>());

            CreateElevatorTerminal("ServiceElevatorButton", new Vector3(1.76f, 1.1f, 9.75f), GreyboxFloor.Service, GreyboxFloor.Ballroom,
                serviceExit, ballroomExit, anomalyExit, player, brass);
            CreateElevatorTerminal("BallroomElevatorButton", new Vector3(1.76f, 1.1f, 24.35f), GreyboxFloor.Ballroom, GreyboxFloor.Service,
                serviceExit, ballroomExit, anomalyExit, player, brass);
            CreateElevatorTerminal("AnomalyElevatorButton", new Vector3(1.76f, 1.1f, 18.55f), GreyboxFloor.Anomaly, GreyboxFloor.Service,
                serviceExit, ballroomExit, anomalyExit, player, red);
        }

        private static void CreateCarryItem(string name, Vector3 position, string id, string label, Color color)
        {
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = name;
            trigger.transform.position = position;
            trigger.transform.localScale = new Vector3(.8f, .45f, .55f);
            trigger.GetComponent<Renderer>().material = MaterialWithColor(name + "Material", color);
            var item = trigger.AddComponent<CarryItem>();
            SetField(item, "itemId", id); SetField(item, "itemName", label); SetField(item, "carryColor", color);
            SetField(item, "itemRenderer", trigger.GetComponent<Renderer>());
        }

        private static void CreateElevatorTerminal(string name, Vector3 position, GreyboxFloor from, GreyboxFloor requested,
            Transform serviceExit, Transform ballroomExit, Transform anomalyExit, GreyboxPlayer player, Material material)
        {
            var button = CreateBox(name, position, new Vector3(.23f, .35f, .13f), material);
            var terminal = button.AddComponent<ElevatorTerminal>();
            SetField(terminal, "from", from); SetField(terminal, "requestedDestination", requested);
            SetField(terminal, "serviceExit", serviceExit); SetField(terminal, "ballroomExit", ballroomExit);
            SetField(terminal, "anomalyExit", anomalyExit); SetField(terminal, "player", player);
            SetField(terminal, "indicator", button.GetComponent<Renderer>());
        }

        private static Transform ExitMarker(string name, Vector3 position, Quaternion rotation)
        {
            var marker = new GameObject(name).transform; marker.position = position; marker.rotation = rotation; return marker;
        }

        private static Volume CreateColdVolume()
        {
            const string path = "Assets/Generated/MemoryShiftProfile.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);
            var go = new GameObject("MemoryShiftVolume");
            var volume = go.AddComponent<Volume>(); volume.isGlobal = true; volume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var balance = profile.Add<WhiteBalance>(true); balance.temperature.value = -65; balance.tint.value = 18;
            var adjustment = profile.Add<ColorAdjustments>(true); adjustment.saturation.value = -32; adjustment.postExposure.value = -.45f;
            AssetDatabase.CreateAsset(profile, path); volume.sharedProfile = profile;
            return volume;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name; box.transform.position = position; box.transform.localScale = scale;
            box.GetComponent<Renderer>().material = material; return box;
        }

        private static Light CreateLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name); go.transform.position = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.range = range; light.intensity = intensity; light.color = color;
            return light;
        }

        private static Material MaterialWithColor(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            return material;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, value);
        }
    }
}
