using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// isik-volume Story 006: dört build-blocking sahne-scan check'i. Sahte sahne
/// içeriği runtime-created GameObject'lerle kurulur (on-disk sahne/asset YASAK —
/// FindAssets tuzağı); edit-mode'da MonoBehaviour yaşam döngüsü koşmadığı için
/// zone'lar facade'a kaydolmaz. Her test throws/doesn't-throw çifti + mesajda
/// suçlu adlar (manifest: pointed hata mesajları).
/// </summary>
public class IsikVolumeBuildChecksTest
{
    private readonly List<GameObject> _spawned = new List<GameObject>();

    private sealed class FakeWalker : IBuildSceneWalker
    {
        public IReadOnlyList<string> EnabledScenePaths { get; } = new[] { "Assets/Scenes/Fake.unity" };
        public void OpenScene(string scenePath) { } // objeler zaten açık edit sahnesinde
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    private ShiftZone CreateZone(string name, TriggerMode mode = TriggerMode.ManualOnly,
        Vector3? position = null, float boxSize = 8f, params Light[] lights)
    {
        var go = new GameObject(name);
        go.transform.position = position ?? Vector3.zero;
        _spawned.Add(go);

        var zone = go.AddComponent<ShiftZone>(); // RequireComponent BoxCollider'ı ekler
        var box = go.GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(boxSize, 4f, boxSize);

        zone._shiftId = name;
        zone._triggerMode = mode;
        var entries = new ZoneLight[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            entries[i] = new ZoneLight { light = lights[i], baseIntensity = 1f, memoryIntensity = 0.6f };
        }
        zone._lights = entries;
        return zone;
    }

    private Light CreateLight(string name, LightmapBakeType bakeType)
    {
        var go = new GameObject(name);
        _spawned.Add(go);
        var light = go.AddComponent<Light>();
        light.lightmapBakeType = bakeType;
        return light;
    }

    private static BuildCheckContext Context(IBuildCheck check) =>
        new BuildCheckContext(check.Name, "Assets/Scenes/Fake.unity");

    // ── (1) Light Mode = Mixed zorunlu ──

    [Test]
    public void BakedLightCheck_BakedReference_FailsWithLightAndZoneName()
    {
        // Arrange
        Light baked = CreateLight("depo-tavan-baked", LightmapBakeType.Baked);
        CreateZone("bolge-baked", lights: baked);
        var check = new IsikVolumeBakedLightCheck();

        // Act + Assert
        var ex = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("depo-tavan-baked", ex.Message);
        StringAssert.Contains("bolge-baked", ex.Message);
    }

    [Test]
    public void BakedLightCheck_RealtimeReference_AlsoFails_MixedIsSilent()
    {
        // Arrange — Mode != Mixed'in HER hali reddedilir (GDD: "asla Baked değil, Mixed").
        Light realtime = CreateLight("realtime-isik", LightmapBakeType.Realtime);
        ShiftZone zone = CreateZone("bolge-realtime", lights: realtime);
        var check = new IsikVolumeBakedLightCheck();

        // Act + Assert
        Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));

        // Mixed'e çevrilince sessiz.
        realtime.lightmapBakeType = LightmapBakeType.Mixed;
        Assert.DoesNotThrow(() => check.Run(Context(check)));
        Assert.IsNotNull(zone); // zone yaşıyor — check hiçbir şeyi mutate etmedi
    }

    // ── (2) İki bölge ışık paylaşamaz ──

    [Test]
    public void SharedLightCheck_LightInTwoZones_FailsWithBothZoneNames()
    {
        // Arrange
        Light shared = CreateLight("paylasilan-isik", LightmapBakeType.Mixed);
        CreateZone("bolge-a", position: new Vector3(-100f, 0f, 0f), lights: shared);
        CreateZone("bolge-b", position: new Vector3(100f, 0f, 0f), lights: shared);
        var check = new IsikVolumeSharedLightCheck();

        // Act + Assert
        var ex = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("bolge-a", ex.Message);
        StringAssert.Contains("bolge-b", ex.Message);
    }

    [Test]
    public void SharedLightCheck_SeparateLights_Silent()
    {
        // Arrange — ayrı ışıklar (aynı bölgede iki girdi de meşru).
        Light lightA = CreateLight("isik-a", LightmapBakeType.Mixed);
        Light lightB = CreateLight("isik-b", LightmapBakeType.Mixed);
        CreateZone("bolge-a", position: new Vector3(-100f, 0f, 0f), lights: lightA);
        CreateZone("bolge-b", position: new Vector3(100f, 0f, 0f), lights: lightB);
        var check = new IsikVolumeSharedLightCheck();

        // Act + Assert
        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    // ── (3) Volume-trigger box'ları kesişemez ──

    [Test]
    public void BoxOverlapCheck_IntersectingBoxes_FailsWithRationale()
    {
        // Arrange — merkezler 5m arayla, kutular 8m: kesişir (ışık paylaşımı YOK —
        // ADR'ın vurgusu: bu, ayrı ve daha sinsi bir hata sınıfı).
        CreateZone("kesisen-a", position: Vector3.zero, boxSize: 8f);
        CreateZone("kesisen-b", position: new Vector3(5f, 0f, 0f), boxSize: 8f);
        var check = new IsikVolumeBoxOverlapCheck();

        // Act + Assert — mesajda iki bölge adı + paylaşılan-profil gerekçesi.
        var ex = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("kesisen-a", ex.Message);
        StringAssert.Contains("kesisen-b", ex.Message);
        StringAssert.Contains("VolumeProfile", ex.Message);
    }

    [Test]
    public void BoxOverlapCheck_SeparatedBoxes_Silent()
    {
        // Arrange — 8m kutular, 40m ara (Minimum Zone Center Spacing rehberine uygun).
        CreateZone("ayrik-a", position: Vector3.zero, boxSize: 8f);
        CreateZone("ayrik-b", position: new Vector3(40f, 0f, 0f), boxSize: 8f);
        var check = new IsikVolumeBoxOverlapCheck();

        // Act + Assert
        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    // ── (4) Automatic-varlık — sahneler-arası toplam iddia (RunAll üzerinden) ──

    [Test]
    public void AutomaticPresence_NoAutomaticZoneAnywhere_OnlyThisCheckFails()
    {
        // Arrange — SIFIR bölgeli "sahne": dört check de kayıtlı sırayla koşar.
        IBuildCheck[] all =
        {
            new IsikVolumeBakedLightCheck(),
            new IsikVolumeSharedLightCheck(),
            new IsikVolumeBoxOverlapCheck(),
            new IsikVolumeAutomaticPresenceCheck(),
        };

        // Act + Assert — yalnız Automatic-varlık check'i patlar (QA case: diğerleri sessiz).
        var ex = Assert.Throws<BuildFailedException>(() => BuildValidationRunner.RunAll(all, new FakeWalker()));
        StringAssert.Contains("AutomaticZonePresence", ex.Message);
        StringAssert.Contains("Automatic", ex.Message);
    }

    [Test]
    public void AutomaticPresence_AutomaticZoneExists_WalkIsSilent_AndStateResetsBetweenWalks()
    {
        // Arrange — Automatic bölge mevcut.
        CreateZone("ambient-oto", TriggerMode.Automatic, position: new Vector3(200f, 0f, 0f));
        var aggregate = new IsikVolumeAutomaticPresenceCheck();
        IBuildCheck[] all = { aggregate };

        // Act + Assert — yürüyüş sessiz.
        Assert.DoesNotThrow(() => BuildValidationRunner.RunAll(all, new FakeWalker()));

        // Bölge kalkınca SONRAKİ yürüyüş patlamalı — geçen yürüyüşün gözlemi
        // bayat kalmamalı (self-reset; registry instance'ları kalıcı).
        TearDown();
        Assert.Throws<BuildFailedException>(() => BuildValidationRunner.RunAll(all, new FakeWalker()),
            "FinalizeWalk biriken gözlemi sıfırlamalı — bayat 'görüldü' sonraki build'i geçirmemeli.");
    }

    // ── Registry kaydı: dörtlü gerçekten kayıtlı (AC-1) ──

    [Test]
    public void Registry_ContainsAllFourIsikVolumeChecks_AsSceneScan()
    {
        // Faz assert'i YALNIZ isik-volume check'lerine — gelecekteki epic'ler
        // AssetScan check'i kaydedecek (registry TODO listesi), tümüne assert
        // o gün sahte kırmızı üretirdi (LP review bulgusu).
        var names = new List<string>();
        foreach (IBuildCheck check in BuildValidationRegistry.Checks)
        {
            names.Add(check.Name);
            if (check.Name.StartsWith("IsikVolume/"))
            {
                Assert.AreEqual(BuildCheckPhase.SceneScan, check.Phase, $"{check.Name} SceneScan fazında olmalı.");
            }
        }

        CollectionAssert.IsSubsetOf(new[]
        {
            "IsikVolume/LightModeMixed",
            "IsikVolume/NoSharedLights",
            "IsikVolume/NoBoxOverlap",
            "IsikVolume/AutomaticZonePresence",
        }, names, "Dört isik-volume check'i registry'ye kayıtlı olmalı (AC-1).");
    }

    // ── Çatı genişlemesi: aggregate sözleşmesi (BeginWalk/FinalizeWalk) ──

    /// <summary>Hook çağrılarını sayan sahte aggregate — çatı sözleşmesinin doğrudan testi.</summary>
    private sealed class FakeAggregate : IBuildCheck, IBuildCheckAggregate
    {
        public int BeginCount, RunCount, FinalizeCount;
        public string FinalizeScenePath = "SENTINEL";
        public string Name => "Fake/Aggregate";
        public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;
        public void BeginWalk() => BeginCount++;
        public void Run(BuildCheckContext context) => RunCount++;
        public void FinalizeWalk(BuildCheckContext context)
        {
            FinalizeCount++;
            FinalizeScenePath = context.ScenePath;
        }
    }

    private sealed class ThrowingCheck : IBuildCheck
    {
        public string Name => "Fake/Throwing";
        public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;
        public void Run(BuildCheckContext context) => context.Fail("kasitli patlama");
    }

    private sealed class MultiSceneWalker : IBuildSceneWalker
    {
        public MultiSceneWalker(params string[] paths) => EnabledScenePaths = paths;
        public IReadOnlyList<string> EnabledScenePaths { get; }
        public void OpenScene(string scenePath) { }
    }

    [Test]
    public void AggregateContract_BeginOnceRunPerSceneFinalizeOnce_NullScenePath()
    {
        // Arrange — iki sahnelik yürüyüş.
        var aggregate = new FakeAggregate();

        // Act
        BuildValidationRunner.RunAll(new IBuildCheck[] { aggregate },
            new MultiSceneWalker("Assets/A.unity", "Assets/B.unity"));

        // Assert — begin 1, run sahne başına, finalize 1 ve ScenePath null.
        Assert.AreEqual(1, aggregate.BeginCount);
        Assert.AreEqual(2, aggregate.RunCount);
        Assert.AreEqual(1, aggregate.FinalizeCount);
        Assert.IsNull(aggregate.FinalizeScenePath, "FinalizeWalk context'i ScenePath=null taşımalı.");
    }

    [Test]
    public void AggregateContract_ZeroScenes_StillBeginsAndFinalizes()
    {
        // Arrange — sahne listesi BOŞ (sözleşme: sıfır sahnede de finalize koşar).
        var aggregate = new FakeAggregate();

        // Act
        BuildValidationRunner.RunAll(new IBuildCheck[] { aggregate }, new MultiSceneWalker());

        // Assert
        Assert.AreEqual(1, aggregate.BeginCount);
        Assert.AreEqual(0, aggregate.RunCount);
        Assert.AreEqual(1, aggregate.FinalizeCount, "Sıfır sahnede de toplam iddia değerlendirilmeli.");
    }

    [Test]
    public void AbortedWalk_StaleObservation_DoesNotLeakIntoNextWalk()
    {
        // Arrange — yürüyüş 1: Automatic bölge GÖRÜLÜR ama sonraki check patlar
        // (FinalizeWalk hiç koşmaz). LP bulgusu: reset sonda olsaydı bu gözlem
        // bayat kalır, yürüyüş 2'yi yanlış geçirirdi.
        CreateZone("gecici-oto", TriggerMode.Automatic, position: new Vector3(300f, 0f, 0f));
        var aggregate = new IsikVolumeAutomaticPresenceCheck();
        IBuildCheck[] withFailure = { aggregate, new ThrowingCheck() };
        Assert.Throws<BuildFailedException>(
            () => BuildValidationRunner.RunAll(withFailure, new FakeWalker()),
            "Yürüyüş 1 kasıtlı olarak ortada patlamalı.");

        // Act — bölge kalkar; yürüyüş 2 temiz check listesiyle koşar.
        TearDown();
        IBuildCheck[] cleanRun = { aggregate };

        // Assert — bayat 'görüldü' sızmadı: Automatic'siz yürüyüş 2 PATLAMALI.
        var ex = Assert.Throws<BuildFailedException>(
            () => BuildValidationRunner.RunAll(cleanRun, new FakeWalker()),
            "BeginWalk yürüyüş başında sıfırlamalı — aborted yürüyüşün gözlemi sonraki build'i geçirtemez.");
        StringAssert.Contains("Automatic", ex.Message);
    }

    // ── Ek edge'ler: null ışık girdisi, rotasyonlu kutu, mesajda sahne yolu ──

    [Test]
    public void NullLightEntry_AllChecksSilent()
    {
        // Arrange — yeni eklenmiş boş ZoneLight slotu (en yaygın authoring hali):
        // light alanı null. Hiçbir check patlamamalı, NRE olmamalı.
        var go = new GameObject("bos-slot");
        _spawned.Add(go);
        var zone = go.AddComponent<ShiftZone>();
        go.GetComponent<BoxCollider>().size = Vector3.one * 4f;
        zone._shiftId = "bos-slot";
        zone._triggerMode = TriggerMode.Automatic; // Automatic-varlık da sessiz kalsın diye
        zone._lights = new ZoneLight[1]; // default entry — light null

        IBuildCheck[] all =
        {
            new IsikVolumeBakedLightCheck(),
            new IsikVolumeSharedLightCheck(),
            new IsikVolumeBoxOverlapCheck(),
            new IsikVolumeAutomaticPresenceCheck(),
        };

        // Act + Assert
        Assert.DoesNotThrow(() => BuildValidationRunner.RunAll(all, new FakeWalker()));
    }

    [Test]
    public void BoxOverlapCheck_RotatedBoxes_IntersectionStillDetected()
    {
        // Arrange — 45° dönük iki kutu, merkezler 6m arayla, 8m kutular: world
        // AABB'leri kesişir. 8-köşe transform'unun rotasyon kolu test edilir.
        ShiftZone a = CreateZone("donuk-a", position: Vector3.zero, boxSize: 8f);
        ShiftZone b = CreateZone("donuk-b", position: new Vector3(6f, 0f, 0f), boxSize: 8f);
        a.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        b.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        var check = new IsikVolumeBoxOverlapCheck();

        // Act + Assert
        Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
    }

    [Test]
    public void FailureMessage_CarriesScenePathPrefix()
    {
        // AC-2'nin "mesajda sahne adı" yarısı — Fail prefix'i context'ten gelir.
        Light baked = CreateLight("sahne-yolu-isik", LightmapBakeType.Baked);
        CreateZone("sahne-yolu-bolge", lights: baked);
        var check = new IsikVolumeBakedLightCheck();

        var ex = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("Fake.unity", ex.Message, "Sahne yolu mesaj prefix'inde olmalı (AC-2).");
    }
}
