using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

/// <summary>
/// isik-volume Story 005: Persistent semantiği + reload restore (Integration —
/// iki epic'in entegrasyon noktası). AC-3'ün "gerçek sahne-yeniden-yükleme"
/// şartı, bölge objesini destroy/re-create ederek simüle edilir; Gece/Oturum'un
/// Persistent kaydı GERÇEK abonelik wiring'i üzerinden (GOD Story 004) yazılır —
/// test hiçbir yerde GOD'a elle kayıt enjekte etmez.
/// </summary>
public class IsikVolumePersistentRestoreTest
{
    private const int FrameCap = 10000;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly List<Object> _assets = new List<Object>();
    private Vector3 _playerPos;

    [SetUp]
    public void SetUp()
    {
        IsikVolumeDurumSistemi.ResetOnLoad();
        GeceOturumDurumu.ResetOnLoad();
        _playerPos = new Vector3(1000f, 0f, 0f);
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

    private ShiftConfig CreateConfig(bool persistent, float duration = 0.2f)
    {
        var config = ScriptableObject.CreateInstance<ShiftConfig>();
        config.Duration = duration;
        config.Persistent = persistent;
        _assets.Add(config);
        return config;
    }

    private ShiftZone CreateZone(string shiftId, float rTrigger = 4f)
    {
        var go = new GameObject($"zone-{shiftId}");
        go.SetActive(false);
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
        zone._triggerMode = TriggerMode.ManualOnly;
        zone._rTrigger = rTrigger;
        zone._kHysteresis = 1.15f;
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
        zone._playerPositionSampler = () => _playerPos;

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

    // ── AC-1/2: Persistent Held — R_exit geçilse de kalıcı; re-trigger/revert saf no-op ──

    [UnityTest]
    public IEnumerator PersistentHeld_OutsideRExit_StaysShifted_TriggerAndRevertNoOp()
    {
        // Arrange — Persistent config'le Held'e çıkar (oyuncu içeride).
        int eventCount = 0;
        void Handler(string id, ShiftState s, Vector3 c, float r) => eventCount++;
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone zone = CreateZone("kalici");
            _playerPos = new Vector3(2f, 0f, 0f);
            Assert.IsTrue(zone.TriggerShift(CreateConfig(persistent: true)));
            yield return WaitUntilWeight(zone._volume, 1f);
            int eventsAtHeld = eventCount;
            Assert.AreEqual(2, eventsAtHeld, "Held'e çıkış tam iki event üretmeli: ShiftingIn + Held.");

            // Act — oyuncu R_exit'in (4.6) çok dışına çıkar; kareler geçer.
            _playerPos = new Vector3(50f, 0f, 0f);
            for (int i = 0; i < 15; i++)
            {
                yield return null;
            }

            // Assert — AC4: Shifting-Out'a girmedi, oturum boyunca Shifted.
            Assert.AreEqual(1f, zone._volume.weight, "Persistent Held bölge R_exit geçilse de Shifted kalmalı (AC4).");
            Assert.IsTrue(zone.IsShiftActive);
            Assert.AreEqual(eventsAtHeld, eventCount, "Kalıcı Held hiçbir yeni event üretmemeli.");

            // AC5 — tekrar giriş/TriggerShift: false, restart yok, event yok.
            _playerPos = new Vector3(1f, 0f, 0f);
            Assert.IsFalse(zone.TriggerShift(CreateConfig(persistent: true)),
                "Held-Persistent'e TriggerShift false dönmeli (AC5).");
            Assert.AreEqual(eventsAtHeld, eventCount, "Re-trigger event üretmemeli (AC5).");

            // Dış RevertShift de atlanır (TR-isik-012 — Shifting-Out oturum boyunca atlanır).
            zone.RevertShift();
            yield return null;
            Assert.AreEqual(1f, zone._volume.weight, "Persistent Held'de dış RevertShift de no-op olmalı.");
            Assert.AreEqual(eventsAtHeld, eventCount);
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── AC-3: reload restore — GOD kaydı gerçek wiring'le yazılır, yeni bölge Held doğar ──

    [UnityTest]
    public IEnumerator ReloadRestore_PersistentFactInGod_NewZoneStartsHeldWithSingleEvent()
    {
        // Arrange — "önceki yükleme": Persistent bölge gerçek akışla Held'e çıkar;
        // GOD'un constructor-time aboneliği Shifting-In'de PersistentShiftIds yazar.
        ShiftZone first = CreateZone("restore-hedef");
        Assert.IsTrue(first.TriggerShift(CreateConfig(persistent: true)));
        yield return WaitUntilWeight(first._volume, 1f);
        Assert.IsTrue(GeceOturumDurumu.Instance.IsPersistent("restore-hedef"),
            "Ön koşul: GOD, Persistent gerçeğini gerçek abonelik wiring'iyle yazmış olmalı.");

        // "Sahne unload" — bölge objesi yıkılır (GOD oturum-gerçeği hayatta kalır).
        Object.Destroy(first.gameObject);
        yield return null;
        Assert.IsFalse(IsikVolumeDurumSistemi.Instance.IsShiftActive("restore-hedef"),
            "Yıkım sonrası bölge tablodan düşmüş olmalı.");

        // Act — "sahne reload": aynı shiftId ile YENİ bölge instantiate.
        var events = new List<ShiftState>();
        void Handler(string id, ShiftState s, Vector3 c, float r) { if (id == "restore-hedef") events.Add(s); }
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone reloaded = CreateZone("restore-hedef");

            // Assert — İLK karede (frame beklemeden): Held-Persistent, weight=1,
            // ışıklar memory hedefinde, TAM BİR event (Held — Shifting-In yok).
            Assert.IsTrue(reloaded.IsShiftActive, "Restore edilen bölge doğrudan Held doğmalı (AC17).");
            Assert.AreEqual(1f, reloaded._volume.weight, "Restore tek-kare: weight=1.");
            Assert.AreEqual(0.72f, reloaded._lights[0].light.intensity, 0.01f, "Işıklar memory hedefinde.");
            CollectionAssert.AreEqual(new[] { ShiftState.Held }, events,
                "Yükleme sonrası TAM BİR event: Held — Shifting-In hiç fırlamaz (AC17).");

            // AC-5 (story): facade sorgusu true; GOD'un yeniden-yazımı idempotent.
            Assert.IsTrue(IsikVolumeDurumSistemi.Instance.IsShiftPersistent("restore-hedef"));
            Assert.IsTrue(GeceOturumDurumu.Instance.IsPersistent("restore-hedef"));

            // Restore edilen Held de kalıcı: R_exit dışına çıkış revert etmez.
            _playerPos = new Vector3(50f, 0f, 0f);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
            Assert.AreEqual(1f, reloaded._volume.weight, "Restore edilen Held de kalıcı olmalı.");

            // QL gap: restore edilen bölgede TriggerShift false döner ve sonrasında
            // NRE yok — restore _activeConfig set ETMEZ; Held'in false dönüşü,
            // ticker'ın null config'e hiç dokunmamasının tek güvencesi.
            Assert.IsFalse(reloaded.TriggerShift(CreateConfig(persistent: true)),
                "Restore edilen Held bölgede TriggerShift false dönmeli (null-config invariant'ı).");
            for (int i = 0; i < 5; i++)
            {
                yield return null; // NRE olsaydı test burada patlardı
            }
            Assert.AreEqual(1f, reloaded._volume.weight);
            CollectionAssert.AreEqual(new[] { ShiftState.Held }, events, "Re-trigger yeni event üretmemeli.");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── QL gap: ResetAll sınırı kalıcılığı temizler → yeni oturum Dormant başlar ──

    [UnityTest]
    public IEnumerator ResetAllBoundary_ClearsPersistence_NextSessionStartsDormant()
    {
        // Arrange — oturum 1: kalıcı Held + GOD gerçeği yazılı.
        ShiftZone first = CreateZone("oturum-siniri");
        Assert.IsTrue(first.TriggerShift(CreateConfig(persistent: true)));
        yield return WaitUntilWeight(first._volume, 1f);
        Assert.IsTrue(GeceOturumDurumu.Instance.IsPersistent("oturum-siniri"));
        Object.Destroy(first.gameObject);
        yield return null;

        // Act — ResetAll sınırı (belgeli sıra: Işık/Volume ÖNCE) + "yeni oturum" bölgesi.
        IsikVolumeDurumSistemi.ResetOnLoad();
        GeceOturumDurumu.ResetOnLoad();
        int eventCount = 0;
        void Handler(string id, ShiftState s, Vector3 c, float r) => eventCount++;
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            ShiftZone nextSession = CreateZone("oturum-siniri");

            // Assert — kalıcılık oturum-scoped: yeni oturumda Dormant, event yok.
            Assert.IsFalse(nextSession.IsShiftActive,
                "ResetAll kalıcılığı temizlemeli — yeni oturumda bölge Dormant başlar.");
            Assert.AreEqual(0, eventCount);
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }

    // ── Davranış sabitleme: Persistent config mid-Shifting-In'de Revert SERBEST (AC8 istisnasız) ──

    [UnityTest]
    public IEnumerator PersistentConfig_RevertMidShiftIn_AllowedButGodFactResurrectsOnReload()
    {
        // GDD AC8'in Persistent istisnası YOK: revert kilidi yalnız Held'de devreye
        // girer. GOD gerçeği ise Shifting-In'de yazıldı ve write-once — bölge
        // Dormant'a dönse de gerçek hayatta kalır; sonraki yükleme Held diriltir.
        // Bu test MEVCUT belgeli davranışı sabitler (LP review gözlemi — ADR/GDD
        // backlog notu; kural değişirse bu test bilinçli güncellenir).
        ShiftZone zone = CreateZone("yarim-kalici");
        zone.TriggerShift(CreateConfig(persistent: true, duration: 1f));
        for (int i = 0; i < FrameCap && zone._volume.weight <= 0f; i++)
        {
            yield return null;
        }
        Assert.Less(zone._volume.weight, 1f, "Shifting-In'in ortası yakalanamadı.");
        Assert.IsTrue(GeceOturumDurumu.Instance.IsPersistent("yarim-kalici"),
            "GOD gerçeği Shifting-In'de çoktan yazılmış olmalı (quick-spec ~3sn boşluk kapanışı).");

        // Act — Held'e ULAŞMADAN Revert: serbest (AC8), Dormant'a iner.
        zone.RevertShift();
        yield return WaitUntilWeight(zone._volume, 0f);
        Assert.IsFalse(zone.IsShiftActive, "Held öncesi Revert serbest — bölge Dormant'a iner (AC8 istisnasız).");

        // Reload simülasyonu — GOD gerçeği hayatta: bölge Held diriltilir.
        Object.Destroy(zone.gameObject);
        yield return null;
        ShiftZone reloaded = CreateZone("yarim-kalici");
        Assert.IsTrue(reloaded.IsShiftActive,
            "Write-once GOD gerçeği yaşadığı için sonraki yükleme Held diriltir (belgeli mevcut davranış).");
        Assert.AreEqual(1f, reloaded._volume.weight);
    }

    // ── AC-4: kayıt yokken instantiate → normal Dormant, event yok ──

    [UnityTest]
    public IEnumerator NoPersistentFact_NewZoneStartsDormant_NoEvent()
    {
        // Arrange
        int eventCount = 0;
        void Handler(string id, ShiftState s, Vector3 c, float r) => eventCount++;
        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += Handler;
        try
        {
            // Act — GOD'da hiçbir Persistent kaydı yokken bölge kur.
            ShiftZone zone = CreateZone("kayitsiz");
            yield return null;

            // Assert
            Assert.IsFalse(zone.IsShiftActive, "Kayıt yokken normal Dormant başlangıç.");
            Assert.AreEqual(0f, zone._volume.weight);
            Assert.AreEqual(0, eventCount, "Restore olmayan başlangıç event üretmemeli.");
        }
        finally
        {
            IsikVolumeDurumSistemi.Instance.OnShiftStateChanged -= Handler;
        }
    }
}
