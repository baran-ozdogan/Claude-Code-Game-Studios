using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// birinci-sahis-kontrolcu Story 003: FirstPersonController sürücüsü. `TickWithInput`
/// (ham Move/Look Vector2 + deltaTime alan internal çekirdek) doğrudan çağrılır —
/// gerçek Input System cihaz simülasyonuna gerek yok (thin-driver split, control
/// manifest BLOCKING kural); yalnız AC-3 (asset denetimi) canlı bir rig gerektirmez.
/// Gerçek `CharacterController`/collider fiziği gerekiyor — PlayMode.
/// </summary>
public class FpcControllerDriverTest
{
    private const int InterestLayer = 8;

    // GDD guardrail'inin GERÇEK senaryosu: "varsayılan statik-geometri katmanı".
    // Rastgele bir kullanıcı katmanı yerine Default(0) kullanılır — hem üretimdeki
    // asıl vakayı sınar, hem zemin de bu katmanda olduğu için her karede gerçek
    // çarpışma üretir (AC-9'un "tekrarlanan per-frame hit" kenar durumu).
    private const int DisinterestedLayer = 0;
    private const float Dt = 1f / 60f;

    private GameObject _player;
    private CharacterController _characterController;
    private PlayerStateProvider _state;
    private FirstPersonController _fpc;
    private GameObject _ground;
    private GameObject _wall;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ground.name = "Ground";
        _ground.transform.position = new Vector3(0f, -0.5f, 0f);
        _ground.transform.localScale = new Vector3(100f, 1f, 100f);

        GameObject prefab = LoadPlayerPrefab();
        Assert.IsNotNull(prefab, "Assets/Prefabs/Player.prefab bulunmalı (Story003PlayerPrefabSetup çıktısı).");
        // Zeminin biraz ÜSTÜNDE doğ — tam temas hâlinde CharacterController kendini
        // grounded saymayabilir; birkaç kare oturma bunu deterministik kılar.
        _player = Object.Instantiate(prefab, new Vector3(0f, 0.1f, 0f), Quaternion.identity);

        _characterController = _player.GetComponent<CharacterController>();
        _state = _player.GetComponent<PlayerStateProvider>();
        _fpc = _player.GetComponent<FirstPersonController>();

        // ZORUNLU: `Physics.autoSyncTransforms` bu projede FALSE. Kod içinden kurulan
        // test geometrisinin Transform'u PhysX'e KENDİLİĞİNDEN yansımaz — senkronlanmazsa
        // collider'lar varsayılan yerinde (orijin, 1x1x1) kalır ve testler tamamen yanlış
        // bir dünyada koşar (ampirik: duvar orijinde duruyordu, oyuncu içine doğuyordu ve
        // "çarpışma oldu" sayacı bu yüzden yanlışlıkla artıyordu).
        Physics.SyncTransforms();
        yield return null; // Awake/OnEnable otursun.

        // Oyuncu zemine otursun (kendi Update()'i sürerken).
        for (int frame = 0; frame < 30 && !_characterController.isGrounded; frame++)
        {
            yield return null;
        }

        // KRİTİK: bileşenin kendi Update()'i kapatılır — testler `TickWithInput`'u
        // DOĞRUDAN çağırır. Aksi hâlde Update() her karede gerçek (sıfır) girdiyle
        // bir tick daha koşar ve testin sürdüğü hızı geri çeker (ampirik: kararlı-durum
        // 1.6 yerine ~1.55'te dengelenir). Tek tick kaynağı = test.
        _fpc.enabled = false;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Duvar da yıkılmalı — aksi hâlde testler arasında sızıp SONRAKİ testlerin
        // oyuncusunu bloklar (ampirik: kilit-bırakma testi sızan bir duvara çarpıyordu).
        if (_player != null) Object.Destroy(_player);
        if (_ground != null) Object.Destroy(_ground);
        if (_wall != null) Object.Destroy(_wall);
        yield return null;
    }

    private static Vector3 Horizontal(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // Prefab/asset erişimi yalnız Editor'da anlamlı — bu testler her zaman Editor'da
    // (batchmode PlayMode) koşar, ama PlayModeTests asmdef'i platform-kısıtlı DEĞİL
    // (kısıtlanırsa PlayMode runner assembly'yi hiç keşfetmiyor — ampirik bulgu).
    private static GameObject LoadPlayerPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
#else
        return null;
#endif
    }

    private static UnityEngine.InputSystem.InputActionAsset LoadGameplayInputActions()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/Input/Gameplay.inputactions");
#else
        return null;
#endif
    }

    // ── AC-1/2: Kararlı-durum hızları ──

    [UnityTest]
    public IEnumerator SteadyStateSpeed_Unloaded_ConvergesToWalkSpeed()
    {
        yield return DriveToSteadyStateAndAssertRealDisplacement(1.6f);
    }

    [UnityTest]
    public IEnumerator SteadyStateSpeed_WhileCarrying_ConvergesToCarrySpeed()
    {
        _state.SetCarrying(true);
        yield return DriveToSteadyStateAndAssertRealDisplacement(1.35f);
    }

    /// <summary>
    /// Kararlı-durumu hem yayınlanan `Velocity` alanından hem de GERÇEK dünya yer
    /// değiştirmesinden doğrular. Yalnız alanı okumak yetmezdi: `Move(v*dt)` satırındaki
    /// bir regresyon (dt düşmesi, hız yarılanması) alanı doğru bırakıp oyuncuyu yanlış
    /// hızda yürütürdü — Integration story'sinin asıl kanıtı yer değiştirmedir
    /// (QL-TEST-COVERAGE bulgusu).
    /// </summary>
    private IEnumerator DriveToSteadyStateAndAssertRealDisplacement(float expectedSpeed)
    {
        for (int frame = 0; frame < 120; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.AreEqual(expectedSpeed, Horizontal(_state.Velocity).magnitude, 0.02f,
            "Yayınlanan Velocity kararlı-durum hızına yakınsamalı.");

        // Ölçüm penceresi — bilinen kare sayısı boyunca gerçek yer değiştirme.
        const int measuredFrames = 60;
        Vector3 startPosition = _player.transform.position;
        for (int frame = 0; frame < measuredFrames; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
        float measuredSpeed = Horizontal(_player.transform.position - startPosition).magnitude / (measuredFrames * Dt);

        Assert.AreEqual(expectedSpeed, measuredSpeed, 0.05f,
            "Gerçek dünya yer değiştirmesi de kararlı-durum hızını göstermeli.");
    }

    // ── AC-3: Koşu yok ──

    [Test]
    public void GameplayActionMap_HasNoSprintOrSpeedMultiplierBinding()
    {
        var asset = LoadGameplayInputActions();
        Assert.IsNotNull(asset, "Assets/Input/Gameplay.inputactions bulunmalı.");

        var map = asset.FindActionMap("Gameplay");
        Assert.IsNotNull(map, "'Gameplay' action map bulunmalı (TR-fpc-015).");

        var actionNames = map.actions.Select(a => a.name).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Move", "Look", "Interact" }, actionNames,
            "Yalnız Move/Look/Interact — yürüme hızının üzerine çıkaran bir Sprint eylemi yok.");

        // Asset denetimi tek başına yetmez: Move bağlamalarına eklenmiş bir Scale processor'ı
        // da hızı yükseltebilirdi (QL-TEST-COVERAGE bulgusu).
        var moveAction = map.FindAction("Move");
        foreach (var binding in moveAction.bindings)
        {
            Assert.IsTrue(string.IsNullOrEmpty(binding.processors),
                $"Move bağlaması '{binding.path}' bir processor taşıyor ('{binding.processors}') — hızı ölçekleyebilir.");
        }
    }

    [UnityTest]
    public IEnumerator DiagonalInput_DoesNotExceedWalkSpeed()
    {
        // Girdi büyüklüğü (1,1) için 1.414 — Clamp01 olmasaydı ~2.26 m/s'ye çıkardı.
        for (int frame = 0; frame < 120; frame++)
        {
            _fpc.TickWithInput(new Vector2(1f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.LessOrEqual(Horizontal(_state.Velocity).magnitude, 1.6f + 0.02f,
            "Çapraz girdi yürüme hızını aşmamalı (koşu yok — AC-3'ün yapısal yarısı).");
    }

    // ── AC-4: MoveOnly Look'u serbest bırakır ──

    [UnityTest]
    public IEnumerator MoveOnlyLock_FreezesMove_LeavesLookFree()
    {
        var requester = new object();
        _state.RequestMovementLock(requester, MovementLockScope.MoveOnly);

        Vector3 positionBefore = _player.transform.position;
        float yawBefore = _player.transform.eulerAngles.y;

        float pitchBefore = SignedPitch();

        _fpc.TickWithInput(new Vector2(0f, 1f), new Vector2(10f, 5f), Dt);
        yield return null;

        // Yalnız YATAY eksen: yerçekimi kilit altında da uygulanır (kilit hareket
        // GİRDİSİNİ dondurur, fiziği değil) — dikey bileşen kasıtlı olarak hariç.
        Assert.AreEqual(0f, Horizontal(_player.transform.position - positionBefore).magnitude, 0.001f,
            "MoveOnly altında yatay pozisyon değişmemeli.");
        Assert.AreNotEqual(yawBefore, _player.transform.eulerAngles.y, "MoveOnly altında Look (yaw) serbest kalmalı.");
        Assert.AreNotEqual(pitchBefore, SignedPitch(), "MoveOnly altında Look (pitch) de serbest kalmalı.");
    }

    // ── AC-5: Full ikisini de dondurur ──

    [UnityTest]
    public IEnumerator FullLock_FreezesBothMoveAndLook()
    {
        var requester = new object();
        _state.RequestMovementLock(requester, MovementLockScope.Full);

        Vector3 positionBefore = _player.transform.position;
        float yawBefore = _player.transform.eulerAngles.y;
        Quaternion eyeRotationBefore = GetEyeTransform().localRotation;

        _fpc.TickWithInput(new Vector2(0f, 1f), new Vector2(10f, 5f), Dt);
        yield return null;

        // Yalnız YATAY eksen — yerçekimi kilit altında da uygulanır (bkz. MoveOnly testi).
        Assert.AreEqual(0f, Horizontal(_player.transform.position - positionBefore).magnitude, 0.001f,
            "Full kilit altında yatay pozisyon değişmemeli.");
        Assert.AreEqual(yawBefore, _player.transform.eulerAngles.y, 0.001f, "Full kilit altında yaw değişmemeli.");
        Assert.AreEqual(eyeRotationBefore, GetEyeTransform().localRotation, "Full kilit altında pitch değişmemeli.");
    }

    // ── AC-6: Kilit-ortası yavaşlama, ışınlanmaz ──

    [UnityTest]
    public IEnumerator LockMidMotion_DecaysAlongExponentialRampCurve_NotTeleportOrLinearDrop()
    {
        // Arrange — kararlı hıza yaklaş, taşıma da açık olsun (AC-6'nın ikinci yarısı).
        _state.SetCarrying(true);
        for (int frame = 0; frame < 120; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
        float speedBeforeLock = Horizontal(_state.Velocity).magnitude;
        Assert.Greater(speedBeforeLock, 0.5f, "Kilitten önce oyuncu belirgin bir hızda olmalı.");

        // Act — kilit ortadayken tetiklenir.
        var requester = new object();
        _state.RequestMovementLock(requester, MovementLockScope.Full);

        // Assert — v, GDD Formül 1'in KAPALI FORMUNU izlemeli: v(t)=v0·e^(-k·t), k=3/T_ramp.
        // Salt "azaldı" yetmezdi: `_currentSpeed *= 0.5f` gibi bir ışınlama da onu geçerdi
        // (QL-TEST-COVERAGE bulgusu). İki ayrı noktada kapalı forma karşı örneklenir.
        const float rampTime = 0.2f;
        float k = 3f / rampTime;
        float previousSpeed = speedBeforeLock;

        for (int frame = 1; frame <= 12; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;

            float speed = Horizontal(_state.Velocity).magnitude;
            Assert.LessOrEqual(speed, previousSpeed + 1e-4f, $"Kare {frame}: hız monotonik azalmalı.");
            Assert.GreaterOrEqual(speed, 0f, $"Kare {frame}: hız sıfırın altına inmemeli (undershoot yok).");
            previousSpeed = speed;

            if (frame == 6 || frame == 12)
            {
                float expected = speedBeforeLock * Mathf.Exp(-k * frame * Dt);
                Assert.AreEqual(expected, speed, 0.05f * speedBeforeLock,
                    $"Kare {frame}: sönüm üstel T_ramp eğrisini izlemeli (ışınlama/doğrusal düşüş değil).");
            }
        }

        // Assert — AC-6'nın ikinci yarısı: IsCarrying kilit boyunca DOKUNULMADAN kalır.
        Assert.IsTrue(_state.IsCarrying, "IsCarrying kilit penceresi boyunca değişmemeli.");
    }

    [UnityTest]
    public IEnumerator LockRelease_ResumesMovement()
    {
        // Kilidin AÇILMA yolu — mandallanmış bir bool regresyonu oyuncuyu kalıcı dondururdu
        // ve diğer testlerin hiçbiri bunu yakalamazdı (QL-TEST-COVERAGE bulgusu).
        var requester = new object();
        _state.RequestMovementLock(requester, MovementLockScope.Full);
        for (int frame = 0; frame < 10; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        _state.ReleaseMovementLock(requester);
        Vector3 positionAfterRelease = _player.transform.position;
        for (int frame = 0; frame < 60; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.Greater(Horizontal(_player.transform.position - positionAfterRelease).magnitude, 0.3f,
            "Kilit bırakıldıktan sonra oyuncu tekrar hareket edebilmeli.");
    }

    [UnityTest]
    public IEnumerator IdleGroundedPlayer_PublishesZeroHorizontalVelocity()
    {
        // Yayınlanan Velocity sözleşmesi: DURAN oyuncu yatayda tam 0 okumalı. (Komut edilen
        // hız yayınlansaydı yere-yapıştırma sabiti yüzünden |Velocity|=2 okunurdu — tüketiciler
        // `.magnitude > 0` ile "hareket ediyor" çıkarımı yapacak; LP/QL bulgusu.)
        for (int frame = 0; frame < 30; frame++)
        {
            _fpc.TickWithInput(Vector2.zero, Vector2.zero, Dt);
            yield return null;
        }

        Assert.AreEqual(0f, Horizontal(_state.Velocity).magnitude, 0.01f,
            "Duran oyuncu yatay hızda tam 0 yayınlamalı.");
    }

    // ── AC-7: Pitch kelepçesi, yaw sınırsız ──

    [UnityTest]
    public IEnumerator LookClamp_PitchClampsAtNegativeEighty_YawAccumulatesPastMultipleTurns()
    {
        float previousYaw = _player.transform.eulerAngles.y;
        float accumulatedYaw = 0f;

        for (int frame = 0; frame < 200; frame++)
        {
            _fpc.TickWithInput(Vector2.zero, new Vector2(50f, 50f), Dt);
            yield return null;
            float yaw = _player.transform.eulerAngles.y;
            // Kare başına ~6° — 180°'nin çok altında, sarma (unwrap) güvenli.
            accumulatedYaw += Mathf.DeltaAngle(previousYaw, yaw);
            previousYaw = yaw;
        }

        Assert.AreEqual(-FirstPersonController.PitchClampDegrees, SignedPitch(), 0.5f,
            "Sürekli pozitif Look.y girdisi pitch'i -80°'de kelepçelemeli.");

        // Yaw "sınırsız" = BİRİKEN dönüş birden fazla tam turu geçer. Salt `!=0` yetmezdi:
        // ±80'de kelepçelenmiş bir yaw da sıfırdan farklı olurdu (QL-TEST-COVERAGE bulgusu).
        Assert.Greater(Mathf.Abs(accumulatedYaw), 720f,
            "Yaw birden fazla tam tur dönmeli — hiçbir sınırda takılmamalı.");
    }

    [UnityTest]
    public IEnumerator LookClamp_PitchClampsAtPositiveEighty_AndStopsMoving()
    {
        for (int frame = 0; frame < 200; frame++)
        {
            _fpc.TickWithInput(Vector2.zero, new Vector2(0f, -50f), Dt);
            yield return null;
        }

        Assert.AreEqual(FirstPersonController.PitchClampDegrees, SignedPitch(), 0.5f,
            "Sürekli negatif Look.y girdisi pitch'i +80°'de kelepçelemeli.");

        // Doygunluk sonrası ARTIK DÖNMEZ — "sınıra ulaştı" değil, "daha fazla dönmüyor".
        float saturatedPitch = SignedPitch();
        _fpc.TickWithInput(Vector2.zero, new Vector2(0f, -50f), Dt);
        yield return null;
        Assert.AreEqual(saturatedPitch, SignedPitch(), 1e-4f, "Kelepçede daha fazla dönüş olmamalı.");
    }

    // Unity Euler açıları 0-360 döner — işaretli forma çevir.
    private float SignedPitch()
    {
        float pitch = GetEyeTransform().localEulerAngles.x;
        return pitch > 180f ? pitch - 360f : pitch;
    }

    // ── AC-8: Step offset/skin width kilitli varsayılanlar ──

    [Test]
    public void CharacterController_StepOffsetAndSkinWidth_SatisfyGddBounds()
    {
        // GDD sınırlarına karşı assert edilir, sabitin kendisine DEĞİL — sabiti değiştirmek
        // testi sessizce geçiremesin (QL-TEST-COVERAGE bulgusu: kendine-referanslı assert).
        Assert.Less(_characterController.stepOffset, FirstPersonController.SmallestThresholdHeight,
            "Step offset, kitin en küçük eşiğinin (~2cm) ALTINDA olmalı (GDD Edge Case: eşit olursa kamera zıplar).");
        Assert.Greater(_characterController.stepOffset, 0f);

        Assert.AreEqual(_characterController.radius * 0.1f, _characterController.skinWidth, 1e-5f,
            "Skin width ≈ kapsül yarıçapının %10'u (GDD Tuning Knob).");
    }

    // ── AC-9: OnControllerColliderHit katman guard'ı ──

    [UnityTest]
    public IEnumerator ControllerColliderHit_InterestLayer_IncrementsCount()
    {
        // Unity, mesajları (OnControllerColliderHit dahil) YALNIZ etkin bileşenlere dağıtır —
        // SetUp'ta kapatılan sürücü bu iki testte tekrar açılır. Hız iddiası olmadığı için
        // bileşenin kendi Update()'inin de tick atması zararsız.
        _fpc.enabled = true;
        _fpc._controllerHitInterestMask = 1 << InterestLayer;
        SpawnWallInFrontOfPlayer(InterestLayer);
        yield return null;

        for (int frame = 0; frame < 120; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.Greater(_fpc.InterestedHitCount, 0, "İlgi-maskesindeki bir katmana çarpma InterestedHitCount'u artırmalı.");
    }

    [UnityTest]
    public IEnumerator ControllerColliderHit_DisinterestedLayer_NeverIncrementsCount()
    {
        _fpc.enabled = true; // bkz. pozitif testteki mesaj-dağıtımı notu.
        _fpc._controllerHitInterestMask = 1 << InterestLayer; // yalnız InterestLayer ilgi alanında.
        SpawnWallInFrontOfPlayer(DisinterestedLayer);
        yield return null;

        Vector3 startPosition = _player.transform.position;
        for (int frame = 0; frame < 120; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
            // Edge case — tekrarlanan per-frame statik-geometri hit'leri HİÇBİR karede sayaç üretmez.
            Assert.AreEqual(0, _fpc.InterestedHitCount, $"Kare {frame}: ilgi-dışı katman erken çıkmalı.");
        }

        // POZİTİF KONTROL — çarpışmanın GERÇEKTEN olduğunu kanıtla. Bu olmadan test,
        // duvar yanlış yerleştiği için de yeşil kalırdı (QL-TEST-COVERAGE bulgusu):
        // sayaç guard sayesinde değil, hiç çarpışma olmadığı için 0 olurdu.
        float traveled = Horizontal(_player.transform.position - startPosition).magnitude;
        Assert.Less(traveled, 1.0f,
            $"Oyuncu duvara çarpıp durmalı — çarpışma gerçekten oldu. " +
            $"DIAG: oyuncu {startPosition}→{_player.transform.position}, duvar {_wall.transform.position} " +
            $"(katman {_wall.layer}, ölçek {_wall.transform.localScale}), collider={_wall.GetComponent<Collider>() != null}");
    }

    private void SpawnWallInFrontOfPlayer(int layer)
    {
        _wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _wall.name = "Wall";
        _wall.layer = layer;
        _wall.transform.position = _player.transform.position + _player.transform.forward * 0.7f + Vector3.up * 0.9f;
        _wall.transform.localScale = new Vector3(4f, 3f, 0.2f);
        Physics.SyncTransforms(); // bkz. SetUp'taki autoSyncTransforms notu.
    }

    // ── AC-10: Sarmalanmış Move()-yalnız yüzeyi ──

    /// <summary>
    /// AC-10 — ham `CharacterController` FirstPersonController'ın public yüzeyinden
    /// (instance VE static; dönüş tipi VE parametre tipi) sızmamalı.
    /// KAPSAM UYARISI: hiçbir reflection testi `player.GetComponent&lt;CharacterController&gt;()`
    /// çağrısını engelleyemez — AC-10'un garantisi bu sınıfın kendi yüzeyiyle sınırlıdır,
    /// ötesi manifest'in "tek-çağıran" konvansiyonuna dayanır (QL/LP bulgusu).
    /// </summary>
    [Test]
    public void PublicSurface_NeverExposesRawCharacterController()
    {
        MemberInfo[] members = typeof(FirstPersonController).GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (MemberInfo member in members)
        {
            if (member is PropertyInfo property)
            {
                AssertNotCharacterController(property.PropertyType, property.Name, "property tipi");
            }
            else if (member is FieldInfo field)
            {
                AssertNotCharacterController(field.FieldType, field.Name, "alan tipi");
            }
            else if (member is MethodInfo method && !method.IsSpecialName)
            {
                AssertNotCharacterController(method.ReturnType, method.Name, "dönüş tipi");
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AssertNotCharacterController(parameter.ParameterType, method.Name, $"'{parameter.Name}' parametresi");
                }
            }
        }
    }

    private static void AssertNotCharacterController(System.Type type, string memberName, string role)
    {
        System.Type effective = type.IsByRef || type.IsArray ? type.GetElementType() : type;
        Assert.IsFalse(effective == typeof(CharacterController) || effective == typeof(Component) || effective == typeof(Object),
            $"'{memberName}' ({role}) ham CharacterController'ı dışa açabiliyor — yasak (AC-10).");
    }

    private Transform GetEyeTransform() => _player.transform.Find("Eye");
}
