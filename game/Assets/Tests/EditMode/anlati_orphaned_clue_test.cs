using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// anlati Story 005: orphaned `shiftId` uyarısı (TR-anlati-008'in uyarı yarısı).
/// Build-time AGGREGATE, non-blocking.
///
/// Test edilen birim `BeginWalk`/`Run`/`FinalizeWalk` üçlüsüdür; runner kancası
/// ince wiring'dir (kayıt + faz iddiası `anlati_build_checks_test.cs`'te).
/// Fixture'lar runtime-created — on-disk asset YOK.
///
/// Uyarılar `LogAssert.Expect` ile doğrulanır. `LogAssert.Expect`'in scope'un
/// BAŞINDAN tarama davranışı burada tuzak değil: fixture'lar (dolu listeler,
/// tekil clueId'ler) `OnValidate` uyarısı ÜRETMİYOR, yani beklentiye yalnız
/// bu check'in mesajı düşebilir.
/// </summary>
public class AnlatiOrphanedClueTest
{
    private readonly List<Object> _spawned = new List<Object>();

    private sealed class FakeRegistrySource : IClueRegistrySource
    {
        private readonly ClueRegistry _registry;

        internal FakeRegistrySource(ClueRegistry registry) => _registry = registry;

        public ClueRegistry Load() => _registry;
    }

    /// <summary>Sahne başına içerik kuran yürüyücü — `OpenSceneMode.Single` öykünmesi.</summary>
    private sealed class ScriptedWalker : IBuildSceneWalker
    {
        private readonly System.Action<string> _onOpen;

        internal ScriptedWalker(System.Action<string> onOpen, params string[] paths)
        {
            _onOpen = onOpen;
            EnabledScenePaths = paths;
        }

        public IReadOnlyList<string> EnabledScenePaths { get; }

        public void OpenScene(string scenePath) => _onOpen(scenePath);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object spawned in _spawned)
        {
            if (spawned != null)
            {
                Object.DestroyImmediate(spawned);
            }
        }
        _spawned.Clear();
    }

    private ClueDefinition Definition(string assetName, string clueId, params string[] requiredShiftIds)
    {
        var definition = ScriptableObject.CreateInstance<ClueDefinition>();
        definition.name = assetName; // mesajda adlandırılan şey — boş bırakılırsa iddialar kısır olur
        _spawned.Add(definition);

        var serialized = new UnityEditor.SerializedObject(definition);
        serialized.FindProperty("_clueId").stringValue = clueId;

        UnityEditor.SerializedProperty list = serialized.FindProperty("_requiredShiftIds");
        list.ClearArray();
        for (int i = 0; i < requiredShiftIds.Length; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).stringValue = requiredShiftIds[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return definition;
    }

    private ClueRegistry Registry(params ClueDefinition[] definitions)
    {
        var registry = ScriptableObject.CreateInstance<ClueRegistry>();
        registry.name = "TestClueRegistry";
        _spawned.Add(registry);

        var serialized = new UnityEditor.SerializedObject(registry);
        UnityEditor.SerializedProperty list = serialized.FindProperty("_definitions");
        list.ClearArray();
        for (int i = 0; i < definitions.Length; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return registry;
    }

    private ShiftZone CreateZone(string name, string shiftId)
    {
        var go = new GameObject(name);
        _spawned.Add(go);

        var zone = go.AddComponent<ShiftZone>();
        zone._shiftId = shiftId;
        zone._triggerMode = TriggerMode.ManualOnly;
        zone._lights = new ZoneLight[0];
        return zone;
    }

    /// <summary>Tek sahnelik yürüyüş: verilen shiftId'ler o sahnede mevcut.</summary>
    private void Walk(ClueConsistencyValidator validator, params string[] shiftIdsInScene)
    {
        var sceneZones = new List<GameObject>();

        void OpenScene(string scenePath)
        {
            foreach (GameObject go in sceneZones)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            sceneZones.Clear();

            foreach (string shiftId in shiftIdsInScene)
            {
                sceneZones.Add(CreateZone($"Bolge_{shiftId}", shiftId).gameObject);
            }
        }

        BuildValidationRunner.RunAll(
            new IBuildCheck[] { validator },
            new ScriptedWalker(OpenScene, "Assets/Scenes/Tek.unity"));
    }

    // ── AC-1: Orphaned shiftId uyarı üretir, build'i KIRMAZ ──

    [Test]
    public void OrphanedShiftId_WarnsAndDoesNotFailBuild()
    {
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("YetimIpucu", "clue-yetim", "yok-boyle-bir-shift"))));

        LogAssert.Expect(LogType.Warning,
            new System.Text.RegularExpressions.Regex(@"\[Anlati\].*yok-boyle-bir-shift"));

        // context.Fail ASLA çağrılmamalı: bu bir içerik-yazımı uyarısı, çalışma
        // zamanı davranışını bozmuyor ve içerik yazılırken build almak meşru.
        Assert.DoesNotThrow(() => Walk(validator, "baska-shift"));

        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);
        Assert.AreEqual("clue-yetim", validator.GetOrphanedClueIds()[0].ClueId);
        Assert.AreEqual("yok-boyle-bir-shift", validator.GetOrphanedClueIds()[0].ShiftId);
    }

    [Test]
    public void OrphanWarning_NamesTheAssetAndTheClueId()
    {
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("YetimAsset", "clue-yetim", "yok"))));

        LogAssert.Expect(LogType.Warning,
            new System.Text.RegularExpressions.Regex(@"YetimAsset.*clue-yetim"));

        Walk(validator, "baska");
    }

    // ── AC-2: Karşılanan shiftId sessiz ──

    [Test]
    public void SatisfiedShiftId_DoesNotWarn()
    {
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("Ipucu", "clue-a", "a"))));

        Walk(validator, "a");

        // Beklenmeyen bir uyarı kalmadığını da iddia et — yoksa "sessiz" iddiası
        // yalnız GetOrphanedClueIds'e dayanırdı.
        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    // ── AC-3: Çok sahneli clue YANLIŞ-POZİTİF üretmez ──

    [Test]
    public void MultiSceneClue_DoesNotWarn()
    {
        // BU TESTİN VAR OLUŞ SEBEBİ: ADR-0007'nin tarif ettiği tek-sahne
        // mekanizması (`ValidateScene` + sceneOpened/sceneSaved) burada YANLIŞ
        // uyarırdı — Depot açıkken Ballroom'un shiftId'sini isteyen her meşru clue
        // "orphaned" görünürdü. Mekanizmanın build-time aggregate'e taşınmasının
        // sebebi tam olarak budur.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("CokSahneliIpucu", "clue-ab", "a", "b"))));

        var sceneZones = new List<GameObject>();

        void OpenScene(string scenePath)
        {
            foreach (GameObject go in sceneZones)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            sceneZones.Clear();

            // "a" YALNIZ birinci sahnede, "b" YALNIZ ikinci sahnede.
            string shiftId = scenePath == "Assets/Scenes/Ikinci.unity" ? "b" : "a";
            sceneZones.Add(CreateZone($"Bolge_{shiftId}", shiftId).gameObject);
        }

        BuildValidationRunner.RunAll(
            new IBuildCheck[] { validator },
            new ScriptedWalker(OpenScene, "Assets/Scenes/Birinci.unity", "Assets/Scenes/Ikinci.unity"));

        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void MultiSceneClue_WarnsOnlyForTheGenuinelyMissingHalf()
    {
        // Birleştirme çalışıyor mu, yoksa her şeyi mi sessizleştiriyor: "a"
        // birinci sahnede, "b" İKİNCİ sahnede, "c" hiçbir yerde — TEK uyarı, "c"
        // için. İKİ SAHNELİ kurulmasının sebebi: birleştirme kanıtı ile
        // "blanket-silence değil" kanıtını AYNI testte tutmak (tek sahneyle
        // yazılsaydı ikisi ayrı testlere dağılırdı).
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("YariYetim", "clue-abc", "a", "b", "c"))));

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"shiftId='c'"));

        var sceneZones = new List<GameObject>();

        void OpenScene(string scenePath)
        {
            foreach (GameObject go in sceneZones)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            sceneZones.Clear();

            string shiftId = scenePath == "Assets/Scenes/Ikinci.unity" ? "b" : "a";
            sceneZones.Add(CreateZone($"Bolge_{shiftId}", shiftId).gameObject);
        }

        BuildValidationRunner.RunAll(
            new IBuildCheck[] { validator },
            new ScriptedWalker(OpenScene, "Assets/Scenes/Birinci.unity", "Assets/Scenes/Ikinci.unity"));

        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);
        Assert.AreEqual("c", validator.GetOrphanedClueIds()[0].ShiftId);
    }

    [Test]
    public void DuplicateRequiredEntry_IsReportedOnce()
    {
        // `ClueDefinition`'ın tooltip'i yinelenen girdiyi "zararsız" diye yazıyor
        // ve çalışma zamanı da öyle davranıyor — o hâlde iki özdeş uyarı basmak
        // gürültüdür. Karar: tanım başına tekilleştir, ve bunu pinle.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("YinelenenIpucu", "clue-x", "yok", "yok"))));

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"shiftId='yok'"));

        Walk(validator, "a");

        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count,
            "Aynı tanım içindeki yinelenen girdi TEK kez raporlanmalı.");
        LogAssert.NoUnexpectedReceived();
    }

    // ── AC-4: Aggregate hijyeni ──

    [Test]
    public void SecondWalk_DoesNotInheritStaleObservations()
    {
        ClueDefinition definition = Definition("Ipucu", "clue-a", "a");
        var validator = new ClueConsistencyValidator(new FakeRegistrySource(Registry(definition)));

        // 1. yürüyüş: "a" hiçbir sahnede yok → uyarı.
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"shiftId='a'"));
        Walk(validator, "baska");
        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);

        // 2. yürüyüş: içerik düzeltilmiş → temiz. BeginWalk sıfırlamasaydı
        // birinci yürüyüşün yetimi listede kalırdı.
        Walk(validator, "a");
        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void AbortedWalk_DoesNotLeakStaleShiftIdsIntoTheNextWalk()
    {
        // Sıfırlamanın neden SONDA değil BAŞTA olması gerektiği (isik-volume
        // Story 006'nın LP bulgusu): araya giren bir check yürüyüşü ortada
        // patlatırsa FinalizeWalk hiç koşmaz. Sondaki bir self-reset, birinci
        // yürüyüşün gördüğü "a"yı ikinciye sızdırır ve ikinci yürüyüş yetimi
        // YANLIŞLIKLA karşılanmış sayardı.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("Ipucu", "clue-a", "a"))));

        var exploding = new ExplodingCheck();
        var abortedZones = new List<GameObject>();

        Assert.Throws<UnityEditor.Build.BuildFailedException>(() =>
        {
            void OpenScene(string scenePath) => abortedZones.Add(CreateZone("Bolge_a", "a").gameObject);

            BuildValidationRunner.RunAll(
                new IBuildCheck[] { validator, exploding },
                new ScriptedWalker(OpenScene, "Assets/Scenes/Tek.unity"));
        });

        // İptal edilen yürüyüşün bölgesi ELLE yok edilmeli: `OpenSceneMode.Single`
        // öykünmesi yalnız kendi yürüyüşünün objelerini temizler, ve bu obje
        // hayatta kalırsa ikinci yürüyüş "a"yı hâlâ görür — testin ölçmek
        // İSTEDİĞİ sızıntı değil, fixture sızıntısı olurdu (bu test tam olarak
        // böyle yanlış-kırmızı verdi).
        foreach (GameObject go in abortedZones)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        // İkinci yürüyüş: "a" ARTIK hiçbir sahnede yok. Gözlem sızıntısı olsaydı sessiz kalırdı.
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"shiftId='a'"));
        Walk(validator, "baska");
        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);
    }

    private sealed class ExplodingCheck : IBuildCheck
    {
        public string Name => "Test/Exploding";

        public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

        public void Run(BuildCheckContext context) => context.Fail("kasten patlıyor");
    }

    // ── Sınır durumlar ──

    [Test]
    public void ZeroSceneWalk_AfterAPopulatedWalk_IsStillSilent()
    {
        // `_scenesWalked` sıfırlaması AYNI instance üzerinde iki yürüyüş olmadan
        // ayırt edilemez: her test taze bir validator kurarsa `BeginWalk`'taki
        // `_scenesWalked = 0` satırı silinse bile suite yeşil kalırdı (QL gate
        // bulgusu). Burada birinci yürüyüş sayacı 1'e çıkarır; ikinci yürüyüş
        // sıfır sahneli olduğu için sayaç sıfırlanmazsa sessizlik guard'ı
        // devreye girmez ve yetim uyarısı YANLIŞLIKLA basılırdı.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("Ipucu", "clue-a", "a"))));

        Walk(validator, "a"); // sayaç 1, içerik karşılanıyor → sessiz

        BuildValidationRunner.RunAll(
            new IBuildCheck[] { validator },
            new ScriptedWalker(_ => { }));

        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void ZeroSceneWalk_IsSilent()
    {
        // "Hiç sahne yok" ile "shiftId hiçbir sahnede yok" AYNI ŞEY DEĞİL. Henüz
        // seviye sahnesi olmayan bir projede her requiredShiftId yetim görünür ve
        // uyarı seli anlamsızlaşırdı.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("Ipucu", "clue-a", "a"))));

        BuildValidationRunner.RunAll(
            new IBuildCheck[] { validator },
            new ScriptedWalker(_ => { }));

        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void EmptyRegistry_IsSilent()
    {
        var validator = new ClueConsistencyValidator(new FakeRegistrySource(Registry()));

        Walk(validator, "a");

        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void MissingRegistry_IsSilent_KeyCheckOwnsThatFailure()
    {
        var validator = new ClueConsistencyValidator(new FakeRegistrySource(null));

        Assert.DoesNotThrow(() => Walk(validator, "a"));

        LogAssert.NoUnexpectedReceived();
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void NullSlotInDefinitions_IsSkipped_NotCrashed()
    {
        // Null slot'un SAHİBİ bloklayan Anlati/RequiredShiftIdsNotEmpty.
        // Burada çıplak bir NRE atmak, o check'in işaret eden mesajının yerine
        // okunaksız bir stack trace koyardı.
        // `Registry(def, null)` → `ClueRegistry.OnValidate` → null-slot ihlali →
        // `Debug.LogWarning`. Bu, bu check'in DEĞİL Story 004'ün paylaşılan
        // çekirdeğinin uyarısı; AÇIKÇA beyan ediliyor ki yan etki kayıtlı olsun
        // ve sessizlik iddiaları yanlış log'a dayanmasın.
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"Definitions\[1\] NULL"));

        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("Ipucu", "clue-a", "a"), null)));

        Assert.DoesNotThrow(() => Walk(validator, "a"));
        CollectionAssert.IsEmpty(validator.GetOrphanedClueIds());
    }

    [Test]
    public void BlankRequiredShiftIdEntry_IsReportedAsOrphaned()
    {
        // Story 004'ün BLOKLAYAN check'i `["   "]`'i bilerek geçiriyor (liste boş
        // değil), ama hiçbir bölge onu ateşleyemez — "ulaşılamaz clue" sınıfının
        // ta kendisi ve BURAYA ait. Story 004 bu boşluğu bir pin testiyle
        // bilinçli olarak kayda geçirmişti; kapanışı burası.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("BosGirdiliIpucu", "clue-bos", "   "))));

        // Regex asset adına DEĞİL, bu check'e özgü `shiftId='…'` şekline
        // bağlanıyor: asset adı `AnlatiContentValidation.Label()`'ın da ürettiği
        // bir alt dize, yani fixture ileride `OnValidate`'i tetiklerse iddia
        // yanlış log'la tatmin olurdu (Story 004'te tam olarak bu yaşandı).
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"BosGirdiliIpucu.*shiftId='   '"));

        Walk(validator, "a");

        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);
    }

    [Test]
    public void BlankZoneShiftId_DoesNotSatisfyAnything()
    {
        // Boş shiftId'li bir bölge kümeye alınsaydı, boş girdi isteyen bir clue
        // YANLIŞLIKLA karşılanmış sayılırdı (yanlış NEGATİF).
        //
        // NOT (QL gate): iki guard — `Run`'ın boş bölge filtresi ve
        // `FinalizeWalk`'ın `IsNullOrWhiteSpace(shiftId) ||` kısa devresi —
        // BİRBİRİNİ yedekliyor, yani hiçbir test yalnız birini ayırt edemez.
        // Bu BİLİNÇLİ: ikisi de kendi başına yeterli, ama `Run` filtresi ayrıca
        // `_seenShiftIds`'i whitespace ile kirletmeyi engelliyor ve ileride
        // `FinalizeWalk` kısa devresi daraltılırsa tek koruma o kalır.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("BosGirdiliIpucu", "clue-bos", "   "))));

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"BosGirdiliIpucu.*shiftId='   '"));

        Walk(validator, "   ");

        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);
    }

    [Test]
    public void ShiftIdMatching_IsCaseSensitive_MatchingRuntimeOrdinalSemantics()
    {
        // Çalışma zamanındaki ters indeks ordinal eşliyor. OrdinalIgnoreCase'e
        // "iyileştirmek" bu uyarıyı runtime'dan AYIRIR: burada sessiz kalınan
        // içerik oyunda hiç tamamlanmazdı.
        var validator = new ClueConsistencyValidator(
            new FakeRegistrySource(Registry(Definition("Ipucu", "clue-a", "A"))));

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"shiftId='A'"));

        Walk(validator, "a");

        Assert.AreEqual(1, validator.GetOrphanedClueIds().Count);
    }

    [Test]
    public void MultipleOrphans_AreAllReported()
    {
        var validator = new ClueConsistencyValidator(new FakeRegistrySource(Registry(
            Definition("YetimBir", "clue-1", "yok-1"),
            Definition("YetimIki", "clue-2", "yok-2"))));

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"yok-1"));
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"yok-2"));

        Walk(validator, "a");

        Assert.AreEqual(2, validator.GetOrphanedClueIds().Count);
        CollectionAssert.AreEquivalent(
            new[] { "yok-1", "yok-2" },
            validator.GetOrphanedClueIds().Select(o => o.ShiftId).ToList());
    }
}
