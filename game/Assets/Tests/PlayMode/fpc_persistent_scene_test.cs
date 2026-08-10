using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// birinci-sahis-kontrolcu Story 004: kalıcı Player sahnesi + SOFT transition
/// repozisyonu (TR-fpc-010, ADR-0003). `BootPersistentScenesTest`/
/// `UIRootStaleInstanceTest`'in kurduğu boot + iki-oturum kalıbını yeniden
/// kullanır (gerçek Editor "Reload Scene" ayarı DEĞİŞTİRİLMEZ — oturum sınırı
/// unload/reload ile simüle edilir, ADR-0001'in kendi emsali).
/// </summary>
public class FpcPersistentSceneTest
{
    private static readonly string[] PersistentScenes =
    {
        PersistentSceneBootLoader.UISceneName,
        PersistentSceneBootLoader.PlayerSceneName,
        PersistentSceneBootLoader.FoundationSceneName,
    };

    private GameObject _anchorObject;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_anchorObject != null)
        {
            Object.Destroy(_anchorObject);
        }

        for (int guardFrames = 0; guardFrames < 600; guardFrames++)
        {
            bool anyPresent = false;
            foreach (string name in PersistentScenes)
            {
                if (!PersistentSceneBootLoader.TryFindScene(name, out Scene scene))
                {
                    continue;
                }

                anyPresent = true;
                if (scene.isLoaded)
                {
                    AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
                    while (op != null && !op.isDone)
                    {
                        yield return null;
                    }
                }
            }

            if (!anyPresent)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail("Teardown: persistent sahneler 600 frame içinde temizlenemedi.");
    }

    private static IEnumerator BootAndSettle()
    {
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        yield return null; // Awake/OnEnable otursun.
    }

    // ── AC-1: Gerçek Player GameObject'i Player.unity'de ──

    [UnityTest]
    public IEnumerator PlayerScene_ContainsRealPlayerObject_CurrentPointsToIt()
    {
        yield return BootAndSettle();

        Assert.IsNotNull(PlayerStateProvider.Current, "Player sahnesi yüklenince Current non-null olmalı.");

        GameObject playerObject = PlayerStateProvider.Current.gameObject;
        Assert.AreEqual(PersistentSceneBootLoader.PlayerSceneName, playerObject.scene.name,
            "Oyuncu, kalıcı Player sahnesinde yaşamalı.");
        Assert.IsTrue(playerObject.activeInHierarchy, "Oyuncu aktif olmalı.");

        // Dört bileşenin tümü — Story 003'ün prefab'ının birebir instance'ı.
        Assert.IsNotNull(playerObject.GetComponent<CharacterController>(), "CharacterController bulunmalı.");
        Assert.IsNotNull(playerObject.GetComponent<PlayerStateProvider>(), "PlayerStateProvider bulunmalı.");
        Assert.IsNotNull(playerObject.GetComponent<FirstPersonController>(), "FirstPersonController bulunmalı.");
        Assert.IsNotNull(playerObject.GetComponentInChildren<Camera>(), "Camera (Eye) bulunmalı.");

        // EyeCamera sözleşmesi FirstPersonController.Awake'te bağlanmış olmalı.
        Assert.IsNotNull(PlayerStateProvider.Current.EyeCamera, "IPlayerState.EyeCamera bağlanmış olmalı.");

        // NOT: "sahnedeki obje prefab INSTANCE'ı mı" burada sınanamaz — prefab bağlantısı
        // yalnız Editor metadata'sıdır, PlayMode'da yüklenmiş bir sahnede gözlenmez
        // (ampirik: PrefabUtility.IsPartOfPrefabInstance runtime'da false döner).
        // O iddia EditMode'a taşındı: `fpc_player_scene_asset_test.cs`.
    }

    // ── AC-2: Sahne-yükleme seviyesinde duplicate-guard ──

    [UnityTest]
    public IEnumerator SecondRealPlayerInstance_DestroysItself_CurrentUnchanged()
    {
        yield return BootAndSettle();
        PlayerStateProvider firstInstance = PlayerStateProvider.Current;
        Assert.IsNotNull(firstInstance);

        // Story 001'in çıplak-GameObject testinden AYRI: burada ADR-0003'ün GERÇEK
        // Risk senaryosu sınanır — tam donanımlı Player objesinin ikinci kez var olması.
        GameObject prefab = LoadPlayerPrefab();
        Assert.IsNotNull(prefab, "Assets/Prefabs/Player.prefab bulunmalı.");

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Duplicate PlayerStateProvider"));
        GameObject duplicate = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        yield return null;

        Assert.IsTrue(duplicate == null, "İkinci Player instance'ı kendini yok etmeli.");
        Assert.AreSame(firstInstance, PlayerStateProvider.Current,
            "Current hâlâ İLK instance'ı göstermeli (guard Current'a dokunmaz).");

        // REGRESYON (Story 003 LP bug'ı #2): yok edilen kopyanın OnDisable'ı, PAYLAŞILAN
        // InputActionAsset üzerinden HAYATTA KALAN oyuncunun girdisini de kapatıyordu
        // (sessiz, hatasız tam donma). `Awake`'teki per-instance kopya bunu çözdü — bu
        // testin var olabileceği TEK yer burası (Story 001'in çıplak GameObject'inde
        // FirstPersonController yok). `.claude/rules/test-standards.md`: her bug fix'in
        // regresyon testi olmalı.
        var survivorController = firstInstance.GetComponent<FirstPersonController>();
        Assert.IsTrue(survivorController.InputActive,
            "Kopya yok edildikten SONRA hayatta kalan oyuncunun girdisi hâlâ etkin olmalı.");
    }

    // ── AC-3: Sanctioned RepositionTo API'si ──

    [UnityTest]
    public IEnumerator RepositionTo_CopiesPositionAndRotation_PreservesIdentityAndState()
    {
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();
        Transform eye = state.EyeCamera;

        // Sürücünün kendi Update()'i kapatılır: aksi hâlde repozisyondan SONRAKİ ilk kare
        // `Velocity`'yi (gerçekleşen yer değiştirmeden) yeniden yazar ve testin ölçtüğü şey
        // "RepositionTo sıfırladı mı" olmaktan çıkar. Sınanan sözleşme çağrının KENDİSİ.
        fpc.enabled = false;
        yield return null;

        // Arrange — çağrı öncesi durum, örtük reset olmadığını kanıtlamak için.
        state.Velocity = new Vector3(0.3f, 0f, 1.1f);
        state.IsGrounded = true;
        state.IsCarrying = true;
        eye.localRotation = Quaternion.Euler(-25f, 0f, 0f);

        GameObject playerObjectBefore = state.gameObject;
        Quaternion eyeRotationBefore = eye.localRotation;
        Vector3 velocityBefore = state.Velocity;

        _anchorObject = new GameObject("SoftTransitionAnchor");
        _anchorObject.transform.SetPositionAndRotation(new Vector3(12f, 3f, -7f), Quaternion.Euler(0f, 137f, 0f));

        // Act
        fpc.RepositionTo(_anchorObject.transform);

        // Assert — pozisyon/rotasyon anchor'la eşleşir.
        Assert.AreEqual(0f, Vector3.Distance(_anchorObject.transform.position, state.transform.position), 0.001f,
            "Pozisyon anchor'la tam eşleşmeli.");
        Assert.AreEqual(0f, Quaternion.Angle(_anchorObject.transform.rotation, state.transform.rotation), 0.05f,
            "Rotasyon anchor'la tam eşleşmeli.");

        // Assert — identity ve Current DEĞİŞMEDİ (yok edilip yeniden yaratılmadı).
        Assert.AreSame(playerObjectBefore, state.gameObject, "GameObject identity korunmalı.");
        Assert.AreSame(state, PlayerStateProvider.Current, "PlayerStateProvider.Current değişmemeli.");

        // Assert — örtük reset YOK (Bedenin Sürekliliği).
        Assert.AreEqual(velocityBefore, state.Velocity, "Velocity örtük olarak sıfırlanmamalı.");
        Assert.IsTrue(state.IsGrounded, "IsGrounded örtük olarak sıfırlanmamalı.");
        Assert.IsTrue(state.IsCarrying, "IsCarrying örtük olarak sıfırlanmamalı.");
        Assert.AreEqual(0f, Quaternion.Angle(eyeRotationBefore, eye.localRotation), 0.05f,
            "Kamera pitch'i korunmalı — repozisyon yalnız gövdeyi taşır.");

        // Bir kare geçse de pozisyon anchor'da kalmalı: CharacterController yazımı geri
        // çekmemeli (RepositionTo yazım süresince controller'ı kapatıyor — bunun kanıtı).
        fpc.enabled = true;
        yield return null;
        Assert.AreEqual(0f, Horizontal(_anchorObject.transform.position - state.transform.position).magnitude, 0.01f,
            "Repozisyon bir kare sonra da kalıcı olmalı (CharacterController geri çekmemeli).");
    }

    private static Vector3 Horizontal(Vector3 v) => new Vector3(v.x, 0f, v.z);

    [UnityTest]
    public IEnumerator RepositionTo_RotatesRetainedMoveDirection_WithTheBody()
    {
        // `_lastMoveDirection` döndürülmezse oyuncu, hız sönerken ESKİ dünya yönüne kayar —
        // GDD'nin yasakladığı görünür süreksizlik. Bu satır silinse diğer testler yeşil kalıyordu.
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();
        fpc.enabled = false;
        yield return null;

        // Arrange — ileri doğru sür (yön = +Z, gövde rotasyonu identity).
        for (int frame = 0; frame < 30; frame++)
        {
            fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, 1f / 60f);
            yield return null;
        }
        Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, fpc.LastMoveDirection), 1f,
            "Ön koşul: korunan yön +Z olmalı.");

        // Act — 90° yaw'lı bir anchor'a repozisyone et.
        _anchorObject = new GameObject("RotatedAnchor");
        _anchorObject.transform.SetPositionAndRotation(new Vector3(20f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f));
        fpc.RepositionTo(_anchorObject.transform);

        // Assert — korunan yön gövdeyle BİRLİKTE döndü (artık +X), eski +Z'de kalmadı.
        Assert.AreEqual(0f, Vector3.Angle(_anchorObject.transform.forward, fpc.LastMoveDirection), 1f,
            "Korunan hareket yönü gövdeyle birlikte dönmeli (gövde-göreli süreklilik).");
    }

    [UnityTest]
    public IEnumerator RepositionTo_TiltedAnchor_IsFlattenedToYaw_BodyNeverTilts()
    {
        // Gövde yapısal olarak yalnız yaw döner (pitch kamerada). Eğik bir anchor kalıcı ve
        // kurtarılamaz bir gövde eğimi üretirdi — guard KODDA (LP-CODE-REVIEW bulgusu).
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();

        _anchorObject = new GameObject("TiltedAnchor");
        _anchorObject.transform.SetPositionAndRotation(new Vector3(5f, 0f, 5f), Quaternion.Euler(30f, 45f, 15f));

        fpc.RepositionTo(_anchorObject.transform);

        Vector3 euler = state.transform.rotation.eulerAngles;
        Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.x), 0.05f, "Gövde pitch'i sıfırlanmalı (yaw-only).");
        Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.z), 0.05f, "Gövde roll'ü sıfırlanmalı (yaw-only).");
        Assert.AreEqual(45f, euler.y, 0.05f, "Anchor'ın yaw'ı korunmalı.");
    }

    [UnityTest]
    public IEnumerator RepositionTo_UnderMovementLock_StillRepositions_LockUntouched()
    {
        // SOFT geçişler kilit ALTINDA koşar — iyi niyetli bir `if (MovementLocked) return;`
        // her sahne geçişini sessizce bozardı (QL-TEST-COVERAGE bulgusu).
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();

        var requester = new object();
        state.RequestMovementLock(requester, MovementLockScope.Full);

        _anchorObject = new GameObject("LockedAnchor");
        _anchorObject.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 4f), Quaternion.Euler(0f, 20f, 0f));

        fpc.RepositionTo(_anchorObject.transform);

        Assert.AreEqual(0f, Vector3.Distance(_anchorObject.transform.position, state.transform.position), 0.001f,
            "Kilit altında da repozisyon çalışmalı.");
        Assert.IsTrue(state.IsLocked, "Repozisyon kilidi temizlememeli.");
        Assert.AreEqual(MovementLockScope.Full, state.EffectiveScope(), "Kilit kapsamı değişmemeli.");
    }

    [UnityTest]
    public IEnumerator RepositionTo_FollowedByLiveFrames_DoesNotSpikeVelocity_AndRegroundsPlayer()
    {
        // Üretim sorusu: repozisyondan SONRAKİ gerçek kare `Velocity`'ye ışınlanma mesafesini
        // enjekte ediyor mu? (Tüketiciler `.magnitude` okuyacak — Adaptif Ses/Işık-Volume.)
        // Bugün güvenli çünkü `positionBeforeMove` ApplyMove'un İÇİNDE, ışınlanmadan SONRA
        // yakalanıyor; bu testi bırakmak o özelliği kilitler (QL-TEST-COVERAGE bulgusu).
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(500f, -0.5f, 500f);
        ground.transform.localScale = new Vector3(50f, 1f, 50f);
        Physics.SyncTransforms();

        _anchorObject = new GameObject("FarAnchor");
        _anchorObject.transform.SetPositionAndRotation(new Vector3(500f, 0.1f, 500f), Quaternion.identity);

        fpc.RepositionTo(_anchorObject.transform);
        fpc.enabled = true;

        for (int frame = 0; frame < 3; frame++)
        {
            yield return null;
            Assert.LessOrEqual(Horizontal(state.Velocity).magnitude, 1.6f + 0.05f,
                $"Kare {frame}: ışınlanma mesafesi Velocity'ye sızmamalı.");
        }

        Object.Destroy(ground);
    }

    [UnityTest]
    public IEnumerator RepositionTo_RelativeOverload_PreservesCabinLocalPose()
    {
        // SOFT'un GERÇEK sözleşmesi: kabin-yerel süreklilik (GDD koordinat-çerçevesi kuralı,
        // ADR-0008'in iki-sahne imzası, ADR-0015'in "relative continuity FROM a source scene").
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();
        fpc.enabled = false;
        yield return null;

        var sourceAnchor = new GameObject("SourceAnchor");
        sourceAnchor.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f));
        _anchorObject = new GameObject("TargetAnchor");
        _anchorObject.transform.SetPositionAndRotation(new Vector3(100f, 0f, 50f), Quaternion.Euler(0f, 90f, 0f));

        // Oyuncu kabin içinde yana çekilmiş ve dönmüş olsun (ride sırasında Look serbest).
        state.transform.SetPositionAndRotation(new Vector3(0.6f, 0f, -0.4f), Quaternion.Euler(0f, 35f, 0f));
        Vector3 localPositionBefore = sourceAnchor.transform.InverseTransformPoint(state.transform.position);
        float localYawBefore = Mathf.DeltaAngle(sourceAnchor.transform.eulerAngles.y, state.transform.eulerAngles.y);

        // Act
        fpc.RepositionTo(sourceAnchor.transform, _anchorObject.transform);

        // Assert — kabin-yerel poz birebir korunmuş (mutlak snap OLSAYDI oyuncu tam
        // anchor'ın üstünde ve anchor'ın yönünde olurdu, yani yerel offset silinirdi).
        Vector3 localPositionAfter = _anchorObject.transform.InverseTransformPoint(state.transform.position);
        float localYawAfter = Mathf.DeltaAngle(_anchorObject.transform.eulerAngles.y, state.transform.eulerAngles.y);

        Assert.AreEqual(0f, Vector3.Distance(localPositionBefore, localPositionAfter), 0.005f,
            "Kabin-yerel pozisyon korunmalı (dünya-uzayı snap DEĞİL).");
        Assert.AreEqual(localYawBefore, localYawAfter, 0.1f, "Kabin-yerel bakış yönü korunmalı.");

        Object.Destroy(sourceAnchor);
    }

    [UnityTest]
    public IEnumerator RepositionTo_RelativeOverload_NullAnchor_IsLoggedNoOp()
    {
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();
        Vector3 positionBefore = state.transform.position;

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("RepositionTo"));
        fpc.RepositionTo(null, null);
        yield return null;

        Assert.AreEqual(0f, Vector3.Distance(positionBefore, state.transform.position), 0.001f,
            "null anchor no-op olmalı.");
    }

    [UnityTest]
    public IEnumerator RepositionTo_NullAnchor_IsLoggedNoOp_PlayerNotMoved()
    {
        yield return BootAndSettle();
        PlayerStateProvider state = PlayerStateProvider.Current;
        var fpc = state.GetComponent<FirstPersonController>();
        Vector3 positionBefore = state.transform.position;

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("RepositionTo"));
        fpc.RepositionTo(null);
        yield return null;

        Assert.AreEqual(0f, Vector3.Distance(positionBefore, state.transform.position), 0.001f,
            "null anchor no-op olmalı — oyuncu taşınmamalı.");
    }

    // ── AC-4: Reload-Scene-off iki-oturum tazeliği ──

    [UnityTest]
    public IEnumerator SecondSimulatedSession_PlayerSceneIsFresh_CurrentNeverStale()
    {
        // Arrange — birinci oturum.
        yield return BootAndSettle();
        PlayerStateProvider firstSessionInstance = PlayerStateProvider.Current;
        Assert.IsNotNull(firstSessionInstance);

        Scene playerScene = SceneManager.GetSceneByName(PersistentSceneBootLoader.PlayerSceneName);
        var staleMarker = new GameObject("StalePlayerSessionMarker");
        SceneManager.MoveGameObjectToScene(staleMarker, playerScene);

        // Act — oturum sınırı: persistent sahneler kapanır, boot yeniden koşar.
        foreach (string name in PersistentScenes)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(name));
            while (!unload.isDone)
            {
                yield return null;
            }
        }
        yield return BootAndSettle();

        // Assert — sahne taze: stale kök objesi yok, Current TAZE bir instance.
        Assert.IsNull(GameObject.Find("StalePlayerSessionMarker"),
            "İkinci oturumda stale kök objesi kalmamalı — Player sahnesi taze yüklenmeli.");
        Assert.IsNotNull(PlayerStateProvider.Current, "İkinci oturumda Current set olmalı (stale/null değil).");
        Assert.AreNotSame(firstSessionInstance, PlayerStateProvider.Current,
            "İkinci oturum TAZE bir PlayerStateProvider kurmalı.");
        Assert.IsNotNull(PlayerStateProvider.Current.EyeCamera,
            "Taze instance'ın sözleşmesi de bağlanmış olmalı (yarım-kurulmuş değil).");
    }

    private static GameObject LoadPlayerPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
#else
        return null;
#endif
    }
}
