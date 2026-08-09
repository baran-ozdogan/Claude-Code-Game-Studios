using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

/// <summary>
/// isik-volume Story 003: ShiftZone + ticker + lockstep (Integration).
/// Sahne runtime'da kurulur (GameObject + BoxCollider + Volume + gerçek Light
/// bileşenleri); paylaşılan profil CreateInstance — asla on-disk asset.
/// AC numaraları GDD'nin kendi numaralı listesinden. Bu story'de bölgeler
/// yalnız dış çağrıyla sürülür (Automatic izleme/histerezis Story 004).
/// </summary>
public class IsikVolumeShiftZoneTest
{
    private const float TestDuration = 0.3f; // kısa ama TIME_EPSILON'un çok üstünde
    private const int FrameCap = 10000; // bekleme tavanı — süre assert'i değil, sonsuz döngü emniyeti (batch dt'si çok küçük olabilir)

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly List<Object> _assets = new List<Object>();

    [SetUp]
    public void SetUp() => IsikVolumeDurumSistemi.ResetOnLoad();

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
        GeceOturumDurumu.ResetOnLoad(); // zone eventleri GOD handler'ına da akar — artık bırakma
    }

    private ShiftConfig CreateConfig(float duration = TestDuration, bool persistent = false, float stinger = 0f)
    {
        var config = ScriptableObject.CreateInstance<ShiftConfig>();
        config.Duration = duration;
        config.Persistent = persistent;
        config.StingerAudioRadius = stinger;
        _assets.Add(config);
        return config;
    }

    private ShiftZone CreateZone(string shiftId, float rTrigger = 4f, Vector3? position = null, int lightCount = 2)
    {
        var go = new GameObject($"zone-{shiftId}");
        go.SetActive(false); // alanlar atanana kadar OnEnable/Awake koşmasın
        go.transform.position = position ?? Vector3.zero;
        _spawned.Add(go);

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(20f, 4f, 20f); // Box Safety Margin pratiğine uygun büyük kutu

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _assets.Add(profile);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = false;
        volume.blendDistance = 0f;
        volume.sharedProfile = profile;
        volume.weight = 0f;

        var lights = new ZoneLight[lightCount];
        for (int i = 0; i < lightCount; i++)
        {
            var lightGo = new GameObject($"light-{shiftId}-{i}");
            lightGo.transform.SetParent(go.transform);
            _spawned.Add(lightGo);
            lights[i] = new ZoneLight
            {
                light = lightGo.AddComponent<Light>(),
                baseColor = new Color(255f / 255f, 191f / 255f, 128f / 255f), // GDD AC13 örneği
                memoryColor = new Color(128f / 255f, 179f / 255f, 255f / 255f),
                baseIntensity = 1.2f,
                memoryIntensity = 1.2f * 0.6f, // mutlak hedef ≡ Base × M (ADR şekli)
            };
        }

        var zone = go.AddComponent<ShiftZone>();
        zone._shiftId = shiftId;
        zone._triggerMode = TriggerMode.ManualOnly;
        zone._lights = lights;
        zone._volume = volume;
        zone._rTrigger = rTrigger;

        go.SetActive(true); // Awake (zoneCenter fallback) + OnEnable (register)
        return zone;
    }

    private static IEnumerator WaitUntilActive(ShiftZone zone, bool expectedActive)
    {
        for (int i = 0; i < FrameCap && zone.IsShiftActive != expectedActive; i++)
        {
            yield return null;
        }
        Assert.AreEqual(expectedActive, zone.IsShiftActive, "Beklenen aktiflik durumuna FrameCap içinde ulaşılamadı.");
    }

    private static IEnumerator WaitUntilWeight(Volume volume, float target)
    {
        // KESİN eşitlik — Approximately değil: uç değerler clamp'li ve tam
        // (smoothstep(1)=1, smoothstep(0)=0). Approximately, weight 1'e
        // YAKLAŞIRKEN (x<1, Held henüz fırlamadan) erken çıkıp durum
        // geçişini kaçırtıyordu (ilk koşuda AC15 4→3 event bulgusu).
        for (int i = 0; i < FrameCap && volume.weight != target; i++)
        {
            yield return null;
        }
        Assert.AreEqual(target, volume.weight, "Beklenen weight değerine FrameCap içinde ulaşılamadı.");
    }

    // ── AC6/7/8/9/10/11: TriggerShift/RevertShift/IsShiftActive sözleşme matrisi ──

    [UnityTest]
    public IEnumerator ContractMatrix_IdempotencyFlipsAndNoOps()
    {
        // Arrange — uzun Duration: tek büyük dt'li batch karesi weight'i yarının
        // altına düşüremesin (asserts dt-varyansına dayanıklı kalsın).
        ShiftZone zone = CreateZone("sozlesme");
        Volume volume = zone._volume;
        const float contractDuration = 1f;

        // AC9 — inaktifken Revert sessiz no-op, AC10-öncesi: Dormant inaktif.
        Assert.IsFalse(zone.IsShiftActive);
        zone.RevertShift(); // exception/event yok
        Assert.IsFalse(zone.IsShiftActive);

        // Act — tetikle.
        Assert.IsTrue(zone.TriggerShift(CreateConfig(contractDuration)), "Dormant'tan TriggerShift true dönmeli.");
        Assert.IsTrue(zone.IsShiftActive);

        // AC6 — aktifken ikinci Trigger false, progress değişmez (aynı kare).
        float weightBefore = volume.weight;
        Assert.IsFalse(zone.TriggerShift(CreateConfig()), "Aktifken TriggerShift false/no-op olmalı (AC6).");
        Assert.AreEqual(weightBefore, volume.weight, 0.0001f, "No-op Trigger progress'i değiştirmemeli.");

        // Held'e çık.
        yield return WaitUntilWeight(volume, 1f);

        // AC8 — Shifting-In/Held'de Revert → Out; weight mevcut değerden azalır, sıçramaz.
        zone.RevertShift();
        Assert.IsTrue(zone.IsShiftActive, "AC10 — Shifting-Out hâlâ aktif sayılır.");
        float wPrev = volume.weight;
        yield return null;
        Assert.LessOrEqual(volume.weight, wPrev, "Out yönünde weight azalmalı.");
        Assert.Greater(volume.weight, 0.5f, "Weight 1'den sıçramadan, sürekli azalmalı (pop yok).");

        // AC7 — Shifting-Out'ta Trigger → true, mevcut x'ten In'e döner (sıçrama yok).
        float wAtFlip = volume.weight;
        Assert.IsTrue(zone.TriggerShift(CreateConfig(contractDuration)), "Shifting-Out'ta TriggerShift true dönmeli (AC7).");
        yield return null;
        Assert.GreaterOrEqual(volume.weight + 0.0001f, wAtFlip, "Flip sonrası weight mevcut değerden artmalı, sıfırlanmamalı.");
        Assert.LessOrEqual(Mathf.Abs(volume.weight - wAtFlip), 1.5f * (Time.deltaTime / contractDuration) + 0.02f,
            "Ardışık kare weight deltası tek tick sınırında kalmalı (süreklilik).");

        // Held'e geri çık; AC11 — kaç Trigger çağranı olursa olsun TEK Revert yeter.
        yield return WaitUntilWeight(volume, 1f);
        Assert.IsFalse(zone.TriggerShift(CreateConfig(contractDuration)), "Held'de Trigger false dönmeli (AC6, ikinci çağıran).");
        Assert.IsFalse(zone.TriggerShift(CreateConfig(contractDuration)), "Held'de Trigger false dönmeli (AC6, üçüncü çağıran).");
        zone.RevertShift();                // İLK Revert geri dönüşü başlatır
        yield return WaitUntilWeight(volume, 0f);
        Assert.IsFalse(zone.IsShiftActive, "Tek Revert, referans sayımı olmadan Dormant'a döndürmeli (AC11).");

        // Tam tur SONRASI yeniden tetikleme — ticker'ın yield break + null'dan
        // sonra ??= ile gerçekten yeniden başladığının kanıtı (QL gap bulgusu).
        Assert.IsTrue(zone.TriggerShift(CreateConfig(contractDuration)), "Dormant'a dönen bölge yeniden tetiklenebilmeli.");
        for (int i = 0; i < FrameCap && volume.weight <= 0f; i++)
        {
            yield return null;
        }
        Assert.Greater(volume.weight, 0f, "Yeniden tetiklenen bölgenin ticker'ı weight'i tekrar sürmeli (coroutine restart).");
    }

    // ── AC8 (bölge seviyesi): Shifting-In ORTASINDA Revert — Held atlanır, In→Out event'i ──

    [UnityTest]
    public IEnumerator RevertMidShiftIn_SkipsHeld_ContinuesFromCurrentWeight()
    {
        // Arrange — In'i başlat, ARA karede yakala (0 < weight < 1).
        var states = new List<ShiftState>();
        void Handler(string id, ShiftState s, Vector3 c, float r) => states.Add(s);
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone zone = CreateZone("orta-revert");
            Volume volume = zone._volume;
            zone.TriggerShift(CreateConfig(duration: 1f));
            for (int i = 0; i < FrameCap && volume.weight <= 0f; i++)
            {
                yield return null;
            }
            Assert.Greater(volume.weight, 0f);
            Assert.Less(volume.weight, 1f, "Ara kare yakalanamadı — Duration'ı büyüt.");

            // Act — Shifting-In ortasında Revert (GDD AC8'in birebir GIVEN'ı).
            float wAtRevert = volume.weight;
            zone.RevertShift();
            yield return WaitUntilWeight(volume, 0f);

            // Assert — Held HİÇ fırlamadı: In → Out → Dormant.
            CollectionAssert.AreEqual(
                new[] { ShiftState.ShiftingIn, ShiftState.ShiftingOut, ShiftState.Dormant }, states,
                "Ortada Revert, Held'i atlamalı — event dizisi In→Out→Dormant (AC8).");
            Assert.LessOrEqual(volume.weight, wAtRevert, "Out, mevcut weight'ten devam etmeli (sıçrama yok).");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── Disable ortada / re-enable: ticker geri gelir (bu story'nin OnEnable dalı) ──

    [UnityTest]
    public IEnumerator DisableMidShift_ReEnable_TickerRecoversToHeld()
    {
        // Arrange — In ortasında disable et.
        ShiftZone zone = CreateZone("disable-kurtarma");
        Volume volume = zone._volume;
        zone.TriggerShift(CreateConfig(duration: 0.5f));
        for (int i = 0; i < FrameCap && volume.weight <= 0f; i++)
        {
            yield return null;
        }
        Assert.Greater(volume.weight, 0f);
        zone.gameObject.SetActive(false); // coroutine öldü, OnDisable referansı temizledi
        float wAtDisable = volume.weight;
        yield return null;

        // Act — re-enable: OnEnable aktif bölgenin ticker'ını geri başlatmalı.
        zone.gameObject.SetActive(true);
        yield return WaitUntilWeight(volume, 1f);

        // Assert — donmadı, Held'e ulaştı; facade yeniden yönlendiriyor.
        Assert.GreaterOrEqual(volume.weight, wAtDisable, "Re-enable sonrası ilerleme mevcut değerden devam etmeli.");
        Assert.IsTrue(IsikVolumeDurumSistemi.Instance.IsShiftActive("disable-kurtarma"),
            "Re-enable, bölgeyi facade'a yeniden kaydetmeli.");
    }

    // ── AC14a (mutlak form): dejenere memoryIntensity ışığa asla sızmaz ──

    [UnityTest]
    public IEnumerator NegativeMemoryIntensity_ClampedAtLight_NeverNegative()
    {
        // Arrange — kasıtlı dejenere girdi: mem = -0.5 (guard 0'a clamp'lemeli).
        ShiftZone zone = CreateZone("dejenere-mem", lightCount: 1);
        zone._lights[0].memoryIntensity = -0.5f;
        zone.TriggerShift(CreateConfig(duration: 0.2f));

        // Act — Held'e kadar sür; her karede intensity negatif olamaz.
        for (int i = 0; i < FrameCap && zone._volume.weight < 1f; i++)
        {
            Assert.GreaterOrEqual(zone._lights[0].light.intensity, 0f,
                "LightIntensity hiçbir karede negatif olamaz (GDD AC14a).");
            yield return null;
        }

        // Assert — tam Shifted'da clamp'li hedef: Lerp(base, 0, 1) = 0.
        Assert.AreEqual(0f, zone._lights[0].light.intensity, 0.0001f,
            "Negatif mem hedefi 0'a clamp'lenmeli (ClampMemoryIntensity mutlak formu).");
    }

    // ── AC13: lockstep — renk ve yoğunluk aynı karede aynı ShiftProgress'ten ──

    [UnityTest]
    public IEnumerator Lockstep_LightColorAndIntensity_DeriveFromSameProgressEveryFrame()
    {
        // Arrange
        ShiftZone zone = CreateZone("lockstep");
        Volume volume = zone._volume;
        zone.TriggerShift(CreateConfig(duration: 0.5f)); // birkaç ara kare yakalayacak kadar uzun

        // İlk ApplyProgress karesini bekle — ondan önce ışıklar Light'ın
        // fabrika default'unda, weight/light çifti henüz sisteme ait değil.
        for (int i = 0; i < FrameCap && volume.weight <= 0f; i++)
        {
            yield return null;
        }
        Assert.Greater(volume.weight, 0f, "İlk tick FrameCap içinde uygulanmış olmalı.");

        // Act + Assert — geçiş boyunca HER örneklenen karede: weight = ShiftProgress
        // (tek yazıcı) ve her ışığın color/intensity çifti AYNI progress'ten.
        int sampledMidFrames = 0;
        for (int i = 0; i < FrameCap && zone.IsShiftActive && volume.weight < 1f; i++)
        {
            float p = volume.weight;
            foreach (ZoneLight entry in zone._lights)
            {
                Color expectedColor = Color.Lerp(entry.baseColor, entry.memoryColor, p);
                float expectedIntensity = Mathf.Lerp(entry.baseIntensity, entry.memoryIntensity, p);
                Assert.AreEqual(expectedColor.r * 255f, entry.light.color.r * 255f, 1f, "AC13: renk aynı p'den türemeli.");
                Assert.AreEqual(expectedColor.g * 255f, entry.light.color.g * 255f, 1f);
                Assert.AreEqual(expectedColor.b * 255f, entry.light.color.b * 255f, 1f);
                Assert.AreEqual(expectedIntensity, entry.light.intensity, 0.01f, "AC13: yoğunluk aynı p'den türemeli.");
            }
            if (p > 0f && p < 1f) sampledMidFrames++;
            yield return null;
        }

        Assert.Greater(sampledMidFrames, 0, "Geçişin ara karelerinden en az biri örneklenmiş olmalı (test anlamlı).");

        // GDD AC13 sayısal örneği dolaylı doğrulanır: p=0.5 karesi yakalanamasa da
        // formül çifti her karede birebir tutuyor; sayısal örnek EditMode'da
        // isik_volume_progress_test.LightBlend_GddExample'da zaten sabit.
    }

    // ── AC15: event — geçiş başına tam bir kez, payload birebir ──

    [UnityTest]
    public IEnumerator Event_FiresExactlyOncePerTransition_WithFullPayload()
    {
        // Arrange
        var received = new List<(string shiftId, ShiftState state, Vector3 center, float radius)>();
        void Handler(string id, ShiftState s, Vector3 c, float r) => received.Add((id, s, c, r));
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone zone = CreateZone("event-payload", rTrigger: 7.5f, position: new Vector3(3f, 1f, -2f));
            Vector3 expectedCenter = zone.ZoneCenter;

            // Act — Dormant→Shifting-In→Held→Shifting-Out→Dormant tam turu.
            zone.TriggerShift(CreateConfig());
            yield return WaitUntilWeight(zone._volume, 1f);
            zone.RevertShift();
            yield return WaitUntilWeight(zone._volume, 0f);
            yield return null; // Dormant geçişi event'inin işlendiği kare

            // Assert — dört gerçek geçiş, dörder payload, fazlası yok.
            Assert.AreEqual(4, received.Count, "Tam tur 4 gerçek geçiş = tam 4 event (AC15).");
            CollectionAssert.AreEqual(
                new[] { ShiftState.ShiftingIn, ShiftState.Held, ShiftState.ShiftingOut, ShiftState.Dormant },
                new[] { received[0].state, received[1].state, received[2].state, received[3].state });
            foreach (var evt in received)
            {
                Assert.AreEqual("event-payload", evt.shiftId);
                Assert.AreEqual(expectedCenter, evt.center, "zoneCenter payload'ı bölgenin merkezi olmalı.");
                Assert.AreEqual(7.5f, evt.radius, "radius payload'ı yapılandırılmış R_trigger olmalı (AC15).");
            }
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── AC1: iki bölge bağımsız — eventler ayrı, durumlar bağımsız ──

    [UnityTest]
    public IEnumerator TwoZones_IndependentHeld_SeparateEvents()
    {
        // Arrange
        var eventsByZone = new Dictionary<string, int> { ["bolge-a"] = 0, ["bolge-b"] = 0 };
        void Handler(string id, ShiftState s, Vector3 c, float r) => eventsByZone[id]++;
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone zoneA = CreateZone("bolge-a", position: new Vector3(-50f, 0f, 0f));
            ShiftZone zoneB = CreateZone("bolge-b", position: new Vector3(50f, 0f, 0f));

            // Act — ikisini de facade üzerinden tetikle (routing dahil uçtan uca).
            Assert.IsTrue(IsikVolumeDurumSistemi.Instance.TriggerShift("bolge-a", CreateConfig()));
            Assert.IsTrue(IsikVolumeDurumSistemi.Instance.TriggerShift("bolge-b", CreateConfig()));
            yield return WaitUntilWeight(zoneA._volume, 1f);
            yield return WaitUntilWeight(zoneB._volume, 1f);

            // Assert — ikisi de bağımsız Held/aktif (AC1).
            Assert.IsTrue(IsikVolumeDurumSistemi.Instance.IsShiftActive("bolge-a"));
            Assert.IsTrue(IsikVolumeDurumSistemi.Instance.IsShiftActive("bolge-b"));
            Assert.AreEqual(2, eventsByZone["bolge-a"], "A: Shifting-In + Held = 2 event, B'ninkiler karışmaz.");
            Assert.AreEqual(2, eventsByZone["bolge-b"], "B: Shifting-In + Held = 2 event.");

            // Birinin Revert'ü diğerini etkilemez.
            zoneA.RevertShift();
            yield return WaitUntilActive(zoneA, false);
            Assert.IsTrue(IsikVolumeDurumSistemi.Instance.IsShiftActive("bolge-b"),
                "A'nın geri dönüşü B'yi etkilememeli (bölge bağımsızlığı).");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── AC14c-pre: zoneCenter fallback ──

    [UnityTest]
    public IEnumerator ZoneCenter_Unset_FallsBackToColliderBoundsCenter()
    {
        // Arrange + Act — merkez açıkça ayarlanmadan, sıfır-olmayan pozisyonda kur.
        var position = new Vector3(12f, 3f, -7f);
        ShiftZone zone = CreateZone("merkez-fallback", position: position);
        yield return null; // Awake koştu

        // Assert — collider'ın world-space kutu merkezine düştü (AABB bounds.center
        // ile birebir aynı nokta, fizik senkronu beklenmeden); asla zero değil.
        var box = zone.GetComponent<BoxCollider>();
        Vector3 expected = box.transform.TransformPoint(box.center);
        Assert.AreEqual(expected, zone.ZoneCenter, "zoneCenter, collider merkezine düşmeli (AC14c-pre).");
        Assert.AreNotEqual(Vector3.zero, zone.ZoneCenter);
    }

    // ── Kayıt yaşam döngüsü: OnEnable register / OnDisable deregister ──

    [UnityTest]
    public IEnumerator RegisterLifecycle_EnableRoutesDisableStops()
    {
        // Arrange
        ShiftZone zone = CreateZone("kayit");
        zone.TriggerShift(CreateConfig());
        yield return WaitUntilWeight(zone._volume, 1f);
        Assert.IsTrue(IsikVolumeDurumSistemi.Instance.IsShiftActive("kayit"), "Enable sonrası facade bölgeye yönlenmeli.");

        // Act — disable: deregister.
        zone.gameObject.SetActive(false);
        yield return null;

        // Assert — facade artık default döner (bölge tablodan düştü).
        Assert.IsFalse(IsikVolumeDurumSistemi.Instance.IsShiftActive("kayit"),
            "Disable sonrası facade Dormant-eşdeğeri default dönmeli (tablodan silindi).");
    }
}
