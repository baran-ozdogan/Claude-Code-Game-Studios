using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// isik-volume Story 004: Automatic izleme + R_exit histerezisi + SOFT
/// co-residency + OnDestroy tamamlama garantisi (Integration). Oyuncu pozisyonu
/// test-injected sampler'la sürülür (FPC wiring'i birinci-şahıs epic'inde).
/// Bekleme yardımcıları KESİN eşitlik kullanır (Story 003 debug notu).
/// </summary>
public class IsikVolumeMonitoringTest
{
    private const int FrameCap = 10000;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly List<Object> _assets = new List<Object>();
    private Vector3 _playerPos; // sampler'ın sürdüğü konum
    private Scene _extraScene;  // co-residency testinin yarattığı sahne — UnityTearDown boşaltır
    private bool _hasExtraScene;

    [SetUp]
    public void SetUp()
    {
        IsikVolumeDurumSistemi.ResetOnLoad();
        _playerPos = new Vector3(1000f, 0f, 0f); // varsayılan: her bölgenin çok dışında
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        // finally içinde yield yasak (CS1625) — sahne boşaltma burada yapılır;
        // assert ortada patlasa bile sonraki testlere sahne kirliliği sızmaz.
        if (_hasExtraScene)
        {
            _hasExtraScene = false;
            yield return SceneManager.UnloadSceneAsync(_extraScene);
        }
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned)
        {
            if (go != null) Object.Destroy(go);
        }
        _spawned.Clear();

        foreach (Object asset in _assets)
        {
            if (asset != null) Object.Destroy(asset);
        }
        _assets.Clear();

        IsikVolumeDurumSistemi.ResetOnLoad();
        GeceOturumDurumu.ResetOnLoad();
    }

    private ShiftConfig CreateConfig(float duration = 0.2f)
    {
        var config = ScriptableObject.CreateInstance<ShiftConfig>();
        config.Duration = duration;
        _assets.Add(config);
        return config;
    }

    private ShiftZone CreateZone(string shiftId, TriggerMode mode, float rTrigger = 4f,
        Vector3? position = null, ShiftConfig autoConfig = null, bool injectSampler = true)
    {
        var go = new GameObject($"zone-{shiftId}");
        go.SetActive(false);
        go.transform.position = position ?? Vector3.zero;
        _spawned.Add(go);

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(20f, 4f, 20f);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _assets.Add(profile);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = false;
        volume.blendDistance = 0f;
        volume.sharedProfile = profile;
        volume.weight = 0f;

        var lightGo = new GameObject($"light-{shiftId}");
        lightGo.transform.SetParent(go.transform);
        _spawned.Add(lightGo);

        var zone = go.AddComponent<ShiftZone>();
        zone._shiftId = shiftId;
        zone._triggerMode = mode;
        zone._rTrigger = rTrigger;
        zone._kHysteresis = 1.15f; // R_exit = 4.6 (GDD örneği)
        zone._autoTriggerConfig = autoConfig;
        zone._lights = new[]
        {
            new ZoneLight
            {
                light = lightGo.AddComponent<Light>(),
                baseColor = Color.white, memoryColor = Color.blue,
                baseIntensity = 1.2f, memoryIntensity = 0.72f,
            },
        };
        zone._volume = volume;
        if (injectSampler)
        {
            zone._playerPositionSampler = () => _playerPos;
        }

        go.SetActive(true);
        return zone;
    }

    private static IEnumerator WaitUntilWeight(Volume volume, float target)
    {
        for (int i = 0; i < FrameCap && volume.weight != target; i++)
        {
            yield return null;
        }
        Assert.AreEqual(target, volume.weight, "Beklenen weight değerine FrameCap içinde ulaşılamadı.");
    }

    private static IEnumerator WaitFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
    }

    // ── AC2 + AC3a/3b: Automatic giriş + histerezis matrisi ──

    [UnityTest]
    public IEnumerator AutomaticZone_EntryHoldHysteresisExit_Matrix()
    {
        // Arrange — Automatic bölge, oyuncu uzakta.
        var events = new List<ShiftState>();
        void Handler(string id, ShiftState s, Vector3 c, float r) => events.Add(s);
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone zone = CreateZone("oto-giris", TriggerMode.Automatic, autoConfig: CreateConfig());
            Volume volume = zone._volume;

            // Uzaktayken kendi kendini tetiklemez.
            yield return WaitFrames(10);
            Assert.IsFalse(zone.IsShiftActive, "Oyuncu uzaktayken Automatic bölge Dormant kalmalı.");

            // Act — R_trigger (4m) içine gir → self-trigger → Held (AC2).
            _playerPos = new Vector3(3f, 0f, 0f);
            yield return WaitUntilWeight(volume, 1f);
            Assert.IsTrue(zone.IsShiftActive, "R_trigger girişi self-trigger etmeli (AC2).");

            // AC3b — R_trigger-R_exit bandında (4.3m) gidiş-geliş: kesintisiz Held.
            int eventsAtHeld = events.Count;
            _playerPos = new Vector3(4.3f, 0f, 0f);
            yield return WaitFrames(10);
            _playerPos = new Vector3(2f, 0f, 0f);
            yield return WaitFrames(10);
            Assert.AreEqual(1f, volume.weight, "Band içi hareket Held'i bozmamalı (AC3b).");
            Assert.AreEqual(eventsAtHeld, events.Count, "Band içi hareket hiçbir event üretmemeli (Shifting-Out yok).");

            // AC3a — R_exit (4.6m) dışına çık → Shifting-Out → Dormant.
            _playerPos = new Vector3(5.5f, 0f, 0f);
            yield return WaitUntilWeight(volume, 0f);
            Assert.IsFalse(zone.IsShiftActive, "R_exit dışına çıkış Dormant'a döndürmeli (AC3a).");
            CollectionAssert.AreEqual(
                new[] { ShiftState.ShiftingIn, ShiftState.Held, ShiftState.ShiftingOut, ShiftState.Dormant }, events);

            // Automatic izlemeye geri döner: tekrar giriş yeniden tetikler.
            _playerPos = new Vector3(1f, 0f, 0f);
            yield return WaitUntilWeight(volume, 1f);
            Assert.IsTrue(zone.IsShiftActive, "Automatic bölge cycle sonrası izlemeye dönüp yeniden tetiklenmeli.");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── AC2a: ManualOnly asla self-trigger etmez ──

    [UnityTest]
    public IEnumerator ManualOnlyZone_PlayerInsideRTrigger_NeverSelfTriggers()
    {
        // Arrange — oyuncu R_trigger'ın göbeğinde DURUYOR; auto config bile atanmış
        // (yanlışlıkla) — mod ManualOnly olduğu sürece fark etmemeli.
        ShiftZone zone = CreateZone("manuel", TriggerMode.ManualOnly, autoConfig: CreateConfig());
        _playerPos = new Vector3(0.5f, 0f, 0f);

        // Act — bekle.
        yield return WaitFrames(20);

        // Assert — Dormant, weight 0, hiçbir şey olmadı (AC2a).
        Assert.IsFalse(zone.IsShiftActive, "ManualOnly bölge içeride durulsa bile self-trigger ETMEZ (AC2a).");
        Assert.AreEqual(0f, zone._volume.weight);

        // Dış çağrı hâlâ çalışır (tek başlatma yolu).
        Assert.IsTrue(zone.TriggerShift(CreateConfig()));
        yield return WaitUntilWeight(zone._volume, 1f);
    }

    // ── AC18: ışınlanma-çıkışı — süreksiz pozisyon sıçraması → normal Shifting-Out ──

    [UnityTest]
    public IEnumerator TeleportExit_HeldZone_DetectsOutsideNextTick_NoHeldLeak()
    {
        // Arrange — Held'e getir.
        ShiftZone zone = CreateZone("isinlanma", TriggerMode.Automatic, autoConfig: CreateConfig());
        _playerPos = new Vector3(2f, 0f, 0f);
        yield return WaitUntilWeight(zone._volume, 1f);

        // Act — tek atamada çok uzağa ışınlan (tick'ler arası süreksizlik).
        _playerPos = new Vector3(500f, 0f, 0f);
        yield return WaitUntilWeight(zone._volume, 0f);

        // Assert — kalıcı Held sızıntısı yok; normal Shifting-Out yolu işledi.
        Assert.IsFalse(zone.IsShiftActive, "Işınlanma sonraki tick'te 'R_exit dışında' olarak tespit edilmeli (AC18).");
    }

    // ── AC19: tek tick'te bölgeyi atlayan hareket → hiç tetiklenmez (belgeli kısıt) ──

    [UnityTest]
    public IEnumerator FastSkip_PositionNeverSampledInside_NeverTriggers()
    {
        // Arrange — Automatic bölge; sampler bölgenin bir yanından öbür yanına
        // TEK atamayla geçer (hiçbir tick içeride örneklemez — AC19'un modeli).
        int eventCount = 0;
        void Handler(string id, ShiftState s, Vector3 c, float r) => eventCount++;
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone zone = CreateZone("hizli-atlama", TriggerMode.Automatic, autoConfig: CreateConfig());
            _playerPos = new Vector3(-100f, 0f, 0f);
            yield return WaitFrames(5);

            // Act — bölgenin üzerinden tek adımda atla.
            _playerPos = new Vector3(100f, 0f, 0f);
            yield return WaitFrames(10);

            // Assert — hiç tetiklenmedi, hiç event yok (belgeli per-frame örnekleme kısıtı).
            Assert.IsFalse(zone.IsShiftActive, "Tek tick'te atlanan bölge hiç tetiklenmemeli (AC19).");
            Assert.AreEqual(0, eventCount);
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── Co-residency: pozisyon örneklemesi donar, x asla donmaz (TR-isik-011) ──

    [UnityTest]
    public IEnumerator CoResidency_InactiveScene_PositionFrozenButXProgresses()
    {
        // Arrange — bölge şu anki aktif sahnede; sonra BAŞKA bir sahneyi aktif yap.
        var events = new List<ShiftState>();
        void Handler(string id, ShiftState s, Vector3 c, float r) => events.Add(s);
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        Scene original = SceneManager.GetActiveScene();
        Scene other = SceneManager.CreateScene("coresidency-diger");
        _extraScene = other;
        _hasExtraScene = true;
        try
        {
            ShiftZone zone = CreateZone("co-residency", TriggerMode.Automatic, autoConfig: CreateConfig(duration: 0.3f));
            SceneManager.SetActiveScene(other);
            yield return null;

            // Yarım 1: sahne aktif değilken sampler R_trigger'a girse bile YENİ tetikleme yok.
            _playerPos = new Vector3(1f, 0f, 0f);
            yield return WaitFrames(15);
            Assert.IsFalse(zone.IsShiftActive,
                "Sahnesi aktif olmayan bölge pozisyon örneklemez — giriş tespiti donar (TR-isik-011).");
            Assert.AreEqual(0, events.Count);

            // Yarım 2: dış tetikleme ile in-flight shift — x, kimse izlemese de ilerler.
            _playerPos = new Vector3(1000f, 0f, 0f); // pozisyonun alakasız olduğunu netleştir
            Assert.IsTrue(zone.TriggerShift(CreateConfig(duration: 0.3f)));
            yield return WaitUntilWeight(zone._volume, 1f);
            CollectionAssert.AreEqual(new[] { ShiftState.ShiftingIn, ShiftState.Held }, events,
                "In-flight x aktif olmayan sahnede de ilerlemeli; Held event'i normal fırlar.");

            // Yarım 3 (QL gap bulgusu): ÇIKIŞ tespiti de donar — oyuncu R_exit'in
            // çok dışında ve Held, ama sahne aktif değil → Shifting-Out ASLA başlamaz.
            yield return WaitFrames(15);
            Assert.AreEqual(1f, zone._volume.weight,
                "Sahnesi aktif olmayan Held bölge, oyuncu dışarıda olsa bile revert ETMEZ (çıkış tespiti donar).");
            Assert.AreEqual(2, events.Count, "Donmuş çıkış tespiti hiçbir yeni event üretmemeli.");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
            SceneManager.SetActiveScene(original); // yield'siz — unload UnityTearDown'da
        }
    }

    // ── OnDestroy tamamlama garantisi: mid-Shifting-In yıkım → Held event'i teardown öncesi ──

    [UnityTest]
    public IEnumerator DestroyMidShiftIn_ForcesHeld_EventFiresOnceBeforeTeardown()
    {
        // Arrange — In'in ortasında yakala.
        var events = new List<ShiftState>();
        float weightAtHeldEvent = -1f;
        ShiftZone zone = CreateZone("yikim", TriggerMode.ManualOnly);
        Volume volume = zone._volume;
        void Handler(string id, ShiftState s, Vector3 c, float r)
        {
            events.Add(s);
            if (s == ShiftState.Held)
            {
                weightAtHeldEvent = volume != null ? volume.weight : -1f; // teardown ÖNCESİ senkron okunur
            }
        }
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            zone.TriggerShift(CreateConfig(duration: 1f));
            for (int i = 0; i < FrameCap && volume.weight <= 0f; i++)
            {
                yield return null;
            }
            Assert.Less(volume.weight, 1f, "Ara kare yakalanamadı.");

            // Act — mid-transition yıkım (SOFT deferred-unload simülasyonu).
            Object.Destroy(zone.gameObject);
            yield return null; // OnDestroy bu karenin sonunda koşar

            // Assert — Held event'i teardown'dan önce TAM BİR KEZ, weight terminalde.
            CollectionAssert.AreEqual(new[] { ShiftState.ShiftingIn, ShiftState.Held }, events,
                "Mid-In yıkım Held'e zorla-tamamlamalı; event tam bir kez (ADR OnDestroy garantisi).");
            Assert.AreEqual(1f, weightAtHeldEvent, 0.0001f,
                "Held event'i anında weight terminal değerde (1) olmalı — reveal görsel olarak da tamam.");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── OnDestroy garantisinin İKİNCİ dalı: mid-Shifting-Out yıkım → Dormant ──

    [UnityTest]
    public IEnumerator DestroyMidShiftOut_ForcesDormant_EventFiresBeforeTeardown()
    {
        // Arrange — Held'e çıkar, Out'a çevir, ortada yakala.
        var events = new List<ShiftState>();
        float weightAtDormantEvent = -1f;
        ShiftZone zone = CreateZone("yikim-out", TriggerMode.ManualOnly);
        Volume volume = zone._volume;
        void Handler(string id, ShiftState s, Vector3 c, float r)
        {
            events.Add(s);
            if (s == ShiftState.Dormant)
            {
                weightAtDormantEvent = volume != null ? volume.weight : -1f;
            }
        }
        zone.TriggerShift(CreateConfig(duration: 1f)); // uzun süre: Out'un ortası tek büyük dt'de kaçmasın
        yield return WaitUntilWeight(volume, 1f);
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler; // yalnız Out+sonrasını topla
        try
        {
            zone.RevertShift();
            for (int i = 0; i < FrameCap && volume.weight >= 1f; i++)
            {
                yield return null;
            }
            Assert.Greater(volume.weight, 0f, "Out'un ortası yakalanamadı.");

            // Act — mid-Shifting-Out yıkım.
            Object.Destroy(zone.gameObject);
            yield return null;

            // Assert — Dormant event'i teardown öncesi, weight terminalde (0).
            CollectionAssert.AreEqual(new[] { ShiftState.ShiftingOut, ShiftState.Dormant }, events,
                "Mid-Out yıkım Dormant'a zorla-tamamlamalı (ADR OnDestroy garantisinin ikinci dalı).");
            Assert.AreEqual(0f, weightAtDormantEvent, 0.0001f,
                "Dormant event'i anında weight terminal değerde (0) olmalı.");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── Null sampler (production-gerçeği: FPC henüz bağlı değil) → sessizce Dormant ──

    [UnityTest]
    public IEnumerator AutomaticZone_NullSampler_StaysDormant_NoException()
    {
        // Arrange — sampler ENJEKTE EDİLMEDEN Automatic bölge (FPC yokken gerçek durum).
        ShiftZone zone = CreateZone("sampler-yok", TriggerMode.Automatic,
            autoConfig: CreateConfig(), injectSampler: false);

        // Act — birkaç frame izleme döngüsü koşsun.
        yield return WaitFrames(15);

        // Assert — NRE yok (test yeşilse), tetiklenme yok.
        Assert.IsFalse(zone.IsShiftActive, "Sampler bağlanmadan Automatic bölge sessizce Dormant kalmalı.");
        Assert.AreEqual(0f, zone._volume.weight);
    }

    // ── Component-level disable (enabled=false): coroutine yetim kalmaz, re-enable çift başlatmaz ──

    [UnityTest]
    public IEnumerator ComponentDisable_StopsTicker_ReEnableResumesWithoutDoubleStart()
    {
        // Arrange — In'in ortasında component'i (GameObject'i DEĞİL) disable et.
        ShiftZone zone = CreateZone("component-disable", TriggerMode.ManualOnly);
        Volume volume = zone._volume;
        zone.TriggerShift(CreateConfig(duration: 0.5f));
        for (int i = 0; i < FrameCap && volume.weight <= 0f; i++)
        {
            yield return null;
        }

        // Act — enabled=false: Unity coroutine'i KENDİSİ durdurmaz; OnDisable'ın
        // açık StopCoroutine'i durdurmalı (LP review bulgusunun regresyon testi).
        zone.enabled = false;
        yield return null;
        float wFrozen = volume.weight;
        yield return WaitFrames(10);
        Assert.AreEqual(wFrozen, volume.weight,
            "Component disable sonrası ticker koşmamalı — weight donmalı (yetim coroutine yok).");

        // Re-enable: tek coroutine ile devam edip Held'e ulaşır.
        zone.enabled = true;
        yield return WaitUntilWeight(volume, 1f);
        Assert.IsTrue(zone.IsShiftActive);
    }
}
