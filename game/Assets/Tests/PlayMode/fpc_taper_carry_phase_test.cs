using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// birinci-sahis-kontrolcu Story 005: taper wiring (`d` gerçek registry'den),
/// `SetCarrying` yüzeyi ve faz akümülatörünün gerçek sürücüye bağlanması
/// (TR-fpc-004/013/014'ün wiring yarıları). Story 002'nin saf-matematik
/// testlerinin ENTEGRASYON kanıtı — formüller burada yeniden sınanmaz,
/// gerçek `InteractableRegistry` + `FirstPersonController` üzerinden sürülür.
/// </summary>
public class FpcTaperCarryPhaseTest
{
    private const float Dt = 1f / 60f;

    private GameObject _ground;
    private GameObject _player;
    private PlayerStateProvider _state;
    private FirstPersonController _fpc;
    private readonly List<GameObject> _interactables = new List<GameObject>();

    /// <summary>Sahnedeki en sade gerçek IInteractable — registry'ye kendini kaydeder.</summary>
    private sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        public InteractionType Type => InteractionType.Instant;
        public float HoldDuration => 0f;
        public bool CanInteract => true;
        public string PromptText => "test";
        public void OnFocusEnter() { }
        public void OnFocusExit() { }
        public void OnInteract() { }
        public void OnHoldProgress(float t) { }
        public void OnHoldComplete() { }
        public void OnHoldCancelled() { }
        public void OnHoldBlocked() { }
        public bool SuppressDefaultHoldFill => false;

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Deregister(this);
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ground.transform.position = new Vector3(0f, -0.5f, 0f);
        _ground.transform.localScale = new Vector3(200f, 1f, 200f);

        GameObject prefab = LoadPlayerPrefab();
        Assert.IsNotNull(prefab, "Assets/Prefabs/Player.prefab bulunmalı.");
        _player = Object.Instantiate(prefab, new Vector3(0f, 0.1f, 0f), Quaternion.identity);
        _state = _player.GetComponent<PlayerStateProvider>();
        _fpc = _player.GetComponent<FirstPersonController>();

        Physics.SyncTransforms(); // autoSyncTransforms=false — bkz. Story 003 test notu.
        yield return null;

        for (int frame = 0; frame < 30 && !_player.GetComponent<CharacterController>().isGrounded; frame++)
        {
            yield return null;
        }

        _fpc.enabled = false; // tek tick kaynağı = test.
        yield return null;

        // Test izolasyonu: `InteractableRegistry._live` süreç-statiktir. Başka bir
        // fixture'dan sızan bir kayıt bu dosyadaki "taper yok" ön koşullarını sessizce
        // bozar ve sıra-bağımlı kırmızı üretir (`test-standards.md`: sıra bağımsızlığı).
        Assert.AreEqual(0, InteractableRegistry.Snapshot().Count,
            "Ön koşul: registry boş başlamalı — başka bir testten kayıt sızmış.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject interactable in _interactables)
        {
            if (interactable != null) Object.Destroy(interactable);
        }
        _interactables.Clear();

        if (_player != null) Object.Destroy(_player);
        if (_ground != null) Object.Destroy(_ground);
        yield return null;
    }

    private GameObject SpawnInteractableAt(Vector3 position)
    {
        var go = new GameObject("TestInteractable");
        go.transform.position = position;
        go.AddComponent<TestInteractable>();
        _interactables.Add(go);
        return go;
    }

    private static Vector3 Horizontal(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private static GameObject LoadPlayerPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
#else
        return null;
#endif
    }

    /// <summary>
    /// Kararlı-duruma sür. `trackingInteractable` verilirse o nesne her karede oyuncunun
    /// pozisyonuna taşınır — böylece `d` sabit ~0 kalır. (Aksi hâlde oyuncu ilerledikçe
    /// nesneden uzaklaşır ve taper kendiliğinden kaybolurdu.)
    /// </summary>
    private IEnumerator SettleSpeed(int frames = 150, GameObject trackingInteractable = null)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            if (trackingInteractable != null)
            {
                trackingInteractable.transform.position = _player.transform.position;
            }

            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
    }

    // ── AC-1: `d` uçtan uca gerçek registry'den ──

    [UnityTest]
    public IEnumerator TaperDistance_ComesFromRealRegistry_SlowsPlayerNearInteractable()
    {
        // Kayıtlı hiçbir interactable yokken taper etkisiz olmalı (tam yürüme hızı).
        yield return SettleSpeed();
        float speedWithNoInteractables = Horizontal(_state.Velocity).magnitude;
        Assert.AreEqual(1.6f, speedWithNoInteractables, 0.02f,
            "Kayıt yokken taper etkisiz olmalı (d=TaperRadius yapısal olarak 1.0 çarpan).");

        // d≈0 tutan bir interactable (oyuncuyla birlikte hareket eder) → TaperMult≈0.7.
        GameObject tracker = SpawnInteractableAt(_player.transform.position);
        yield return null;
        yield return SettleSpeed(trackingInteractable: tracker);

        float speedAtZeroDistance = Horizontal(_state.Velocity).magnitude;
        Assert.AreEqual(1.6f * 0.7f, speedAtZeroDistance, 0.03f,
            "d=0'da hedef hız 1.6x0.7=1.12 m/s olmalı (GDD Formül 2, gerçek registry üzerinden).");
    }

    [UnityTest]
    public IEnumerator TaperDistance_BeyondRadius_HasNoEffect()
    {
        // 1.5m'den uzaktaki bir interactable taper üretmemeli.
        SpawnInteractableAt(_player.transform.position + new Vector3(5f, 0f, 0f));
        yield return null;
        yield return SettleSpeed();

        Assert.AreEqual(1.6f, Horizontal(_state.Velocity).magnitude, 0.02f,
            "Yarıçap dışındaki interactable hızı etkilememeli.");
    }

    // ── AC-2: Çoklu bayraklı nesne — minimum kazanır, sıçrama yok ──

    [UnityTest]
    public IEnumerator TwoInteractables_MinimumWins_AndCrossoverIsContinuous()
    {
        // İki interactable, oyuncunun hattının iki yanında ve YAKIN — kesişim taper'ın
        // DİK bölgesinde olsun (QL bulgusu: eski kurulumda kesişim d≈1.35'te, yani
        // TaperMult≈0.99'da yaşanıyordu; test edilen kare neredeyse nötr noktaydı).
        SpawnInteractableAt(new Vector3(0.3f, 0f, 1.0f));
        SpawnInteractableAt(new Vector3(-0.3f, 0f, 1.6f));
        yield return null;

        float previousDistance = -1f;
        bool sawCrossover = false;
        bool sawSteepTaper = false;

        for (int frame = 0; frame < 200; frame++)
        {
            // `d`, ApplyMove'un BAŞINDA (hareket uygulanmadan) örneklenir — beklenen değer
            // de tick ÖNCESİ pozisyondan hesaplanmalı, yoksa bir karelik yer değiştirme
            // kadar sistematik sapma olur.
            Vector3 positionBeforeTick = _player.transform.position;
            float d0 = Horizontal(_interactables[0].transform.position - positionBeforeTick).magnitude;
            float d1 = Horizontal(_interactables[1].transform.position - positionBeforeTick).magnitude;
            float expectedD = Mathf.Min(d0, d1);

            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;

            // MİNİMUM KAZANIR — doğrudan `d` üzerinden. (Hız üzerinden assert etmek
            // yetersizdi: Formül 1 her sıçramayı ~0.22 kazançla yumuşatıyor, yani
            // max/ilk-kayıtlı/son-görülen kuralları da testi geçerdi — QL bulgusu.)
            Assert.AreEqual(expectedD, _fpc.CurrentTaperDistance, 1e-3f,
                $"Kare {frame}: `d` iki nesnenin YATAY mesafesinin minimumu olmalı.");

            // SÜREKLİLİK — `d` üzerinden: sabit bir noktaya olan mesafe, oyuncunun
            // o karede kat ettiği yoldan daha hızlı değişemez.
            if (previousDistance >= 0f)
            {
                Assert.Less(Mathf.Abs(_fpc.CurrentTaperDistance - previousDistance), 1.6f * Dt + 0.01f,
                    $"Kare {frame}: `d` süreksiz sıçradı ({previousDistance:F4} → {_fpc.CurrentTaperDistance:F4}).");
            }
            previousDistance = _fpc.CurrentTaperDistance;

            if (d1 < d0) sawCrossover = true;
            if (expectedD < 0.6f) sawSteepTaper = true;
        }

        Assert.IsTrue(sawCrossover, "Test kurulumu geçersiz: minimum-mesafe geçiş noktası hiç yaşanmadı.");
        Assert.IsTrue(sawSteepTaper, "Test kurulumu geçersiz: kesişim taper'ın dik bölgesinde yaşanmalı.");
    }

    [UnityTest]
    public IEnumerator TaperDistance_IsHorizontal_ElevatedPropStillSlowsPlayerFully()
    {
        // GDD'nin kamuflaj decoy'ları el yüksekliğinde monte (kapı kolu, ışık anahtarı,
        // termostat). 3B mesafe kullanılsaydı `d` ~1.2'nin altına inemez, TaperMult≈0.97
        // olurdu — tasarlanan %30 yerine %3 yavaşlama (LP+QL blocking bulgusu).
        GameObject elevated = SpawnInteractableAt(_player.transform.position + new Vector3(0f, 1.2f, 0f));
        yield return null;

        for (int frame = 0; frame < 150; frame++)
        {
            elevated.transform.position = _player.transform.position + new Vector3(0f, 1.2f, 0f);
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.AreEqual(0f, _fpc.CurrentTaperDistance, 0.01f,
            "Yükseklik `d`'yi etkilememeli — mesafe YATAY ölçülür.");
        Assert.AreEqual(1.6f * 0.7f, Horizontal(_state.Velocity).magnitude, 0.03f,
            "El yüksekliğindeki bir prop da tam %30 yavaşlama üretmeli.");
    }

    [UnityTest]
    public IEnumerator TaperAndCarry_ComposeMultiplicatively_ThroughRealWiring()
    {
        // GDD AC8 — çarpımsal bileşim, gerçek wiring'de (saf matematikte zaten kanıtlı).
        // CarryMult=0.84375, d=0.5 → TaperMult≈0.778 → v_target ≈ 1.05 m/s.
        _state.SetCarrying(true);
        GameObject prop = SpawnInteractableAt(_player.transform.position);
        yield return null;

        for (int frame = 0; frame < 200; frame++)
        {
            prop.transform.position = _player.transform.position + new Vector3(0.5f, 0f, 0f);
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.AreEqual(0.5f, _fpc.CurrentTaperDistance, 0.02f, "Ön koşul: d=0.5m tutulmalı.");
        Assert.AreEqual(1.05f, Horizontal(_state.Velocity).magnitude, 0.03f,
            "Taşıma ve taper ÇARPIMSAL bileşmeli (GDD AC8, gerçek wiring).");
    }

    [UnityTest]
    public IEnumerator NonComponentInteractable_IsSkipped_TaperUnaffected()
    {
        // Belgelenen "sessizce atlanır" sözleşmesi — saf C# bir kayıt taper'ı bozmamalı.
        var pureCSharp = new PureCSharpInteractable();
        InteractableRegistry.Register(pureCSharp);
        try
        {
            yield return SettleSpeed();
            Assert.AreEqual(1.6f, Horizontal(_state.Velocity).magnitude, 0.02f,
                "Component olmayan kayıt taper üretmemeli (sessizce atlanır).");
        }
        finally
        {
            InteractableRegistry.Deregister(pureCSharp);
        }
    }

    [UnityTest]
    public IEnumerator InteractableDestroyedMidMotion_NoException_TaperReleasesSmoothly()
    {
        // `Snapshot()` kare-başı cache olduğundan yok edilmiş bir obje aynı kare boyunca
        // listede kalır — `component == null` guard'ının GERÇEK üretim vakası (ADR-0004 Risks).
        GameObject prop = SpawnInteractableAt(_player.transform.position);
        for (int frame = 0; frame < 60; frame++)
        {
            prop.transform.position = _player.transform.position;
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
        Assert.Less(Horizontal(_state.Velocity).magnitude, 1.3f, "Ön koşul: taper etkin olmalı.");

        Object.Destroy(prop);

        for (int frame = 0; frame < 150; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.AreEqual(1.6f, Horizontal(_state.Velocity).magnitude, 0.03f,
            "Interactable yok edilince taper serbest kalmalı (exception yok).");
    }

    /// <summary>Component OLMAYAN bir kayıt — `DistanceToNearestInteractable`'ın atlama dalı için.</summary>
    private sealed class PureCSharpInteractable : IInteractable
    {
        public InteractionType Type => InteractionType.Instant;
        public float HoldDuration => 0f;
        public bool CanInteract => true;
        public string PromptText => "pure";
        public void OnFocusEnter() { }
        public void OnFocusExit() { }
        public void OnInteract() { }
        public void OnHoldProgress(float t) { }
        public void OnHoldComplete() { }
        public void OnHoldCancelled() { }
        public void OnHoldBlocked() { }
        public bool SuppressDefaultHoldFill => false;
    }

    // ── AC-3: SetCarrying yüzeyi ──

    [Test]
    public void SetCarrying_MirrorsImmediately_AndRepeatedCallsAreNoOp()
    {
        Assert.IsFalse(_state.IsCarrying, "Başlangıçta taşımıyor olmalı.");

        _state.SetCarrying(true);
        Assert.IsTrue(_state.IsCarrying, "SetCarrying(true) ANINDA aynalanmalı.");

        _state.SetCarrying(true);
        Assert.IsTrue(_state.IsCarrying, "Aynı değerle tekrar çağrı güvenli no-op olmalı.");

        _state.SetCarrying(false);
        Assert.IsFalse(_state.IsCarrying);
        _state.SetCarrying(false);
        Assert.IsFalse(_state.IsCarrying);
    }

    [UnityTest]
    public IEnumerator SetCarrying_ChangesTargetSpeed_ThroughRealDriver()
    {
        yield return SettleSpeed();
        Assert.AreEqual(1.6f, Horizontal(_state.Velocity).magnitude, 0.02f);

        _state.SetCarrying(true);
        yield return SettleSpeed();

        Assert.AreEqual(1.35f, Horizontal(_state.Velocity).magnitude, 0.02f,
            "SetCarrying(true) gerçek sürücüde taşıma hızını üretmeli.");
    }

    // ── AC-4: Faz akümülatörü gerçek sürücüye bağlı ──

    [UnityTest]
    public IEnumerator PhaseAccumulator_AdvancesWithRealDisplacement_AndDrivesBothBobAndFootsteps()
    {
        var footstepPhases = new List<float>();
        _fpc.FootstepTriggered += _ => footstepPhases.Add(_fpc.MovementPhase);

        float phaseAtStart = _fpc.MovementPhase;
        Vector3 positionAtStart = _player.transform.position;

        for (int frame = 0; frame < 240; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        float traveled = Horizontal(_player.transform.position - positionAtStart).magnitude;
        float phaseDelta = _fpc.MovementPhase - phaseAtStart;

        // Faz = KAT EDİLEN MESAFE (saat zamanı değil).
        Assert.AreEqual(traveled, phaseDelta, 0.05f,
            "Faz, gerçek kat edilen yatay mesafeyle ilerlemeli.");

        // Ayak sesleri AYNI fazdan türer — her tetikleme bir stride sınırında.
        Assert.Greater(footstepPhases.Count, 2, "Bu mesafede birden çok ayak sesi tetiklenmeli.");
        foreach (float phase in footstepPhases)
        {
            float remainder = phase % FirstPersonController.StrideLength;
            float distanceToBoundary = Mathf.Min(remainder, FirstPersonController.StrideLength - remainder);
            Assert.Less(distanceToBoundary, 0.05f,
                $"Ayak sesi faz sınırında tetiklenmeli (faz={phase:F3}), ayrı bir zamanlayıcıdan değil.");
        }
    }

    [UnityTest]
    public IEnumerator PhaseAccumulator_DoesNotAdvance_WhenBlockedByWall()
    {
        // Duvara dayanmış oyuncu ADIM ATMAZ — faz gerçek yer değiştirmeye bağlı,
        // komut edilen hıza değil (aksi hâlde duvarda ayak sesi çalardı).
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = _player.transform.position + Vector3.forward * 0.7f + Vector3.up * 0.9f;
        wall.transform.localScale = new Vector3(6f, 3f, 0.2f);
        Physics.SyncTransforms();
        yield return null;

        // Duvara daya.
        for (int frame = 0; frame < 90; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        float phaseWhenBlocked = _fpc.MovementPhase;
        for (int frame = 0; frame < 60; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.AreEqual(phaseWhenBlocked, _fpc.MovementPhase, 0.02f,
            "Duvara dayalı oyuncunun fazı ilerlememeli (gerçek yer değiştirme sıfır).");

        Object.Destroy(wall);
    }

    [UnityTest]
    public IEnumerator BobIsAppliedToEyeCamera_OscillatesAtFullSlider_ExactRestAtZero()
    {
        // Hiçbir test kameranın GERÇEKTEN hareket ettiğini okumuyordu — bob satırı silinse
        // tüm süit yeşil kalıyordu (QL bulgusu). accessibility §8'in "görsel bob sıfır"
        // maddesi ancak burada tahliye edilebilir.
        Transform eye = _state.EyeCamera;
        Vector3 restLocalPosition = eye.localPosition;

        _fpc.MotionIntensityPercent = 100f;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int frame = 0; frame < 180; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
            minY = Mathf.Min(minY, eye.localPosition.y);
            maxY = Mathf.Max(maxY, eye.localPosition.y);
        }

        float observedAmplitude = (maxY - minY) / 2f;
        Assert.AreEqual(_fpc.CurrentBobAmplitude, observedAmplitude, 0.002f,
            "Kameranın yerel Y salınımı hesaplanan bob genliğiyle eşleşmeli.");

        // S=%0 — kamera TAM dinlenme pozisyonunda kalmalı, her karede.
        _fpc.MotionIntensityPercent = 0f;
        for (int frame = 0; frame < 60; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
            Assert.AreEqual(restLocalPosition.y, eye.localPosition.y, 1e-6f,
                $"Kare {frame}: S=%0'da kamera dinlenme pozisyonunda kalmalı (görsel bob TAM sıfır).");
        }
    }

    [UnityTest]
    public IEnumerator RepositionTo_DoesNotAdvancePhase_NoPhantomFootsteps()
    {
        // Işınlanma ADIM DEĞİLDİR. Yer değiştirme kare-içinde örneklenmeseydi, 50m'lik bir
        // SOFT geçiş ~66 ayak sesi patlatırdı (QL bulgusu — en ucuz yüksek-değerli test).
        int footsteps = 0;
        _fpc.FootstepTriggered += _ => footsteps++;

        for (int frame = 0; frame < 60; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }

        float phaseBeforeTeleport = _fpc.MovementPhase;
        int footstepsBeforeTeleport = footsteps;

        var anchor = new GameObject("FarAnchor");
        anchor.transform.SetPositionAndRotation(new Vector3(50f, 0.1f, 50f), Quaternion.identity);
        _fpc.RepositionTo(anchor.transform);
        yield return null;

        Assert.AreEqual(phaseBeforeTeleport, _fpc.MovementPhase, 1e-4f,
            "Işınlanma fazı ilerletmemeli.");
        Assert.AreEqual(footstepsBeforeTeleport, footsteps,
            "Işınlanma hayalet ayak sesi üretmemeli.");

        // Işınlanmadan sonra faz normal devam eder.
        for (int frame = 0; frame < 60; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
        Assert.Greater(_fpc.MovementPhase, phaseBeforeTeleport, "Işınlanmadan sonra faz normal ilerlemeli.");

        Object.Destroy(anchor);
    }

    [UnityTest]
    public IEnumerator BackwardMovement_AlsoAdvancesPhase()
    {
        // Geri yürürken de adım atılır — `magnitude` yön-bağımsız. Bir regresyon
        // (`Dot(displacement, forward)`) geri yürüyüşte ayak seslerini susturur.
        float phaseBefore = _fpc.MovementPhase;

        for (int frame = 0; frame < 120; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, -1f), Vector2.zero, Dt);
            yield return null;
        }

        Assert.Greater(_fpc.MovementPhase - phaseBefore, 0.5f,
            "Geri yürüyüş de fazı ilerletmeli.");
    }

    [UnityTest]
    public IEnumerator SetCarrying_MidMotion_RetargetsAlongRamp_BothDirections()
    {
        yield return SettleSpeed();
        Assert.AreEqual(1.6f, Horizontal(_state.Velocity).magnitude, 0.02f);

        // Hareket ortasında taşımaya geç — ışınlanma değil, rampa boyunca yeniden hedeflenir.
        _state.SetCarrying(true);
        float previousSpeed = Horizontal(_state.Velocity).magnitude;
        for (int frame = 0; frame < 60; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
            float speed = Horizontal(_state.Velocity).magnitude;
            Assert.Less(Mathf.Abs(speed - previousSpeed), 0.08f, $"Kare {frame}: hız ışınlanmamalı.");
            previousSpeed = speed;
        }
        Assert.AreEqual(1.35f, Horizontal(_state.Velocity).magnitude, 0.02f);

        // DÖNÜŞ yolu — mandallanmış bir bool regresyonu bunu yakalayamazdı.
        _state.SetCarrying(false);
        yield return SettleSpeed();
        Assert.AreEqual(1.6f, Horizontal(_state.Velocity).magnitude, 0.02f,
            "SetCarrying(false) tam yürüme hızına dönmeli.");
    }

    // ── AC-5: Erişilebilirlik bağımsızlığı — wiring seviyesinde ──

    [UnityTest]
    public IEnumerator AccessibilitySlider_ScalesOnlyVisualBob_FootstepTimingIdentical()
    {
        // ÖNCE kararlı hıza çık — iki koşu da AYNI başlangıç durumundan başlamalı.
        // (Aksi hâlde birinci koşu rampa mesafesini öder, ikincisi ödemez ve
        // karşılaştırma S'yi değil rampa farkını ölçer.)
        yield return SettleSpeed();

        // Koşu A — S=%0.
        _fpc.MotionIntensityPercent = 0f;
        var footstepsAtZero = new List<float>();
        void RecordZero(float _) => footstepsAtZero.Add(_fpc.MovementPhase);
        _fpc.FootstepTriggered += RecordZero;

        float phaseBeforeZeroRun = _fpc.MovementPhase;
        for (int frame = 0; frame < 180; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
        _fpc.FootstepTriggered -= RecordZero;

        float phaseTravelledAtZero = _fpc.MovementPhase - phaseBeforeZeroRun;
        Assert.AreEqual(0f, _fpc.CurrentBobAmplitude, 1e-6f, "S=%0'da görsel bob genliği TAM 0 olmalı.");
        Assert.Greater(footstepsAtZero.Count, 2, "S=%0'da da ayak sesleri tetiklenmeli (zamanlama etkilenmez).");

        // Koşu B — S=%100, aynı kararlı hızdan devam, aynı girdi dizisi, aynı kare sayısı.
        _fpc.MotionIntensityPercent = 100f;
        var footstepsAtFull = new List<float>();
        void RecordFull(float _) => footstepsAtFull.Add(_fpc.MovementPhase);
        _fpc.FootstepTriggered += RecordFull;

        float phaseBeforeFullRun = _fpc.MovementPhase;
        for (int frame = 0; frame < 180; frame++)
        {
            _fpc.TickWithInput(new Vector2(0f, 1f), Vector2.zero, Dt);
            yield return null;
        }
        _fpc.FootstepTriggered -= RecordFull;

        float phaseTravelledAtFull = _fpc.MovementPhase - phaseBeforeFullRun;
        Assert.Greater(_fpc.CurrentBobAmplitude, 0f, "S=%100'de görsel bob genliği sıfırdan büyük olmalı.");

        // ZAMANLAMA KANITI: S, faz ilerlemesini HİÇ etkilemez — yalnız görsel genlik
        // değişir (manifest guardrail, wiring seviyesinde).
        Assert.AreEqual(phaseTravelledAtZero, phaseTravelledAtFull, 0.02f,
            "Aynı girdi dizisi S'den bağımsız olarak aynı faz mesafesini üretmeli.");

        // Ayak sesi sayısı YALNIZ faza bağlı: her koşuda geçilen stride sınırı sayısı
        // kadar tetiklenir. (Ham sayıları eşitlemek YANLIŞ olurdu — iki koşu stride
        // içinde farklı offset'lerden başlar, aynı mesafede 6 ya da 7 sınır geçilebilir;
        // bu S'ye değil başlangıç fazına bağlıdır.)
        AssertFootstepsFollowPhaseBoundaries(footstepsAtZero.Count, phaseBeforeZeroRun, phaseBeforeZeroRun + phaseTravelledAtZero, "S=%0");
        AssertFootstepsFollowPhaseBoundaries(footstepsAtFull.Count, phaseBeforeFullRun, phaseBeforeFullRun + phaseTravelledAtFull, "S=%100");
    }

    private static void AssertFootstepsFollowPhaseBoundaries(int actualCount, float startPhase, float endPhase, string label)
    {
        int expected = Mathf.FloorToInt(endPhase / FirstPersonController.StrideLength)
                     - Mathf.FloorToInt(startPhase / FirstPersonController.StrideLength);

        Assert.AreEqual(expected, actualCount,
            $"{label}: ayak sesi sayısı geçilen stride sınırı sayısına eşit olmalı — tetikleme YALNIZ fazdan türer.");
    }
}
