using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// anlati Story 004: build-blocking doğrulama dörtlüsü (TR-anlati-008/009,
/// TR-isik-021 devri). Emsal `isik_volume_build_checks_test.cs`: runtime-created
/// fixture'lar (on-disk asset YASAK), throws/doesn't-throw çiftleri, mesajda
/// suçlu adlar.
///
/// **KAPSAM KİLİDİNİ ASIL UYGULAYAN MEKANİZMA fixture rejimidir, bir assert
/// DEĞİL**: `ScriptableObject.CreateInstance` ile kurulan tanımlar
/// `AssetDatabase.FindAssets`'e GÖRÜNMEZ. Bir check proje-geneli taramaya
/// sürüklenirse hiçbir şey bulamaz ve aşağıdaki "fırlatmalı" testler fırlatmaz
/// olur — yani kilidi kıran değişiklik testleri KIRMIZI yapar. (Diskte sıfır
/// `ClueDefinition` asset'i olduğundan, "sürüklenmeyi yakalıyorum" diye yazılan
/// bir sessizlik testi bunu kanıtlayamazdı.)
/// </summary>
public class AnlatiBuildChecksTest
{
    private readonly List<Object> _spawned = new List<Object>();

    // ── Sahte dikişler ──

    private sealed class FakeKeyResolver : IAddressableKeyResolver
    {
        private readonly bool _resolves;

        internal FakeKeyResolver(bool resolves) => _resolves = resolves;

        /// <summary>Check'in SORDUĞU anahtar — yazım hatalı bir literali yakalayan tek casus.</summary>
        internal string LastKey { get; private set; }

        public bool KeyResolves(string key)
        {
            LastKey = key;
            return _resolves;
        }
    }

    private sealed class FakeRegistrySource : IClueRegistrySource
    {
        private readonly ClueRegistry _registry;

        internal FakeRegistrySource(ClueRegistry registry) => _registry = registry;

        public ClueRegistry Load() => _registry;
    }

    private sealed class FakeWalker : IBuildSceneWalker
    {
        public IReadOnlyList<string> EnabledScenePaths { get; } = new[] { "Assets/Scenes/Fake.unity" };

        public void OpenScene(string scenePath) { } // objeler zaten açık edit sahnesinde
    }

    /// <summary>Sahne başına içerik kurabilen yürüyücü (emsal: `fpc_decoy_build_check_test.cs`).</summary>
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

    // ── Fixture'lar ──

    /// <summary>
    /// `assetName` ve `clueId` AYRI parametreler ve `.name` AÇIKÇA set edilir.
    /// `anlati_clue_definition_test.cs`'in helper'ı `.name` ATAMIYOR; birebir
    /// kopyalansaydı "mesajda asset adı geçiyor" iddiaları BOŞ string'e assert
    /// edilir ve KOŞULSUZ geçerdi (sahte yeşil).
    /// </summary>
    private ClueDefinition Definition(string assetName, string clueId, params string[] requiredShiftIds)
    {
        var definition = ScriptableObject.CreateInstance<ClueDefinition>();
        definition.name = assetName;
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

    private ShiftZone CreateZone(string name, string shiftId, TriggerMode mode)
    {
        var go = new GameObject(name);
        _spawned.Add(go);

        var zone = go.AddComponent<ShiftZone>(); // RequireComponent BoxCollider'ı ekler
        zone._shiftId = shiftId;
        zone._triggerMode = mode;
        zone._lights = new ZoneLight[0];
        return zone;
    }

    private static BuildCheckContext Context(IBuildCheck check) =>
        new BuildCheckContext(check.Name, "Assets/Scenes/Fake.unity");

    // ── AC-3: Addressable anahtarı çözülmezse hata ──

    [Test]
    public void UnresolvableKey_FailsBuild()
    {
        var check = new AnlatiClueRegistryKeyCheck(
            new FakeKeyResolver(resolves: false), new FakeRegistrySource(Registry()));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("ClueRegistry", exception.Message);
        StringAssert.Contains("TR-anlati-009", exception.Message);
    }

    [Test]
    public void ResolvableKey_DoesNotFailBuild()
    {
        var check = new AnlatiClueRegistryKeyCheck(
            new FakeKeyResolver(resolves: true), new FakeRegistrySource(Registry()));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void ResolvedKeyPointingAtNonRegistryAsset_FailsBuild()
    {
        // LP+QL gate bulgusu: adres çözülüyor ama asset bir ClueRegistry değil.
        // Bu adım olmasaydı (a) GEÇER, (b)(c)(d) "kayıt yok" diye sessizce döner
        // ve build fail-open olurdu — bloklayan bir kapının içindeki en kötü hata.
        var check = new AnlatiClueRegistryKeyCheck(
            new FakeKeyResolver(resolves: true), new FakeRegistrySource(null));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        // İlk mesaj da "değil" kelimesini taşıyor — teşhisi ayırt eden ifadeye
        // assert et, yoksa iki guard tek if'e katlansa test fark etmezdi.
        StringAssert.Contains("adresi çözülüyor AMA", exception.Message);
    }

    // ── Anahtar seçim kuralı (saf, düz veri) ──
    //
    // Bu blok olmadan `TrySelectUnique`'in TÜM kuralları test dışıydı: "tam bir
    // eşleşme" `< 1`'e çevrilse ya da ship filtresi tamamen silinse suite yeşil
    // kalıyordu (adversarial doğrulama bulgusu). Proje bugün tek grup + tek entry
    // taşıdığı için gerçek ayarlar üzerinden hiçbir kural sınanamıyor.

    private static AddressableEntryCandidate Candidate(string address, string path, bool ships = true) =>
        new AddressableEntryCandidate(address, path, ships);

    [Test]
    public void SingleShippingMatch_Resolves()
    {
        bool resolved = AddressableKeySelection.TrySelectUnique(
            new[] { Candidate("Baska", "Assets/Baska.asset"), Candidate("ClueRegistry", "Assets/R.asset") },
            "ClueRegistry", out string path);

        Assert.IsTrue(resolved);
        Assert.AreEqual("Assets/R.asset", path);
    }

    [Test]
    public void NoMatch_DoesNotResolve()
    {
        Assert.IsFalse(AddressableKeySelection.TrySelectUnique(
            new[] { Candidate("Baska", "Assets/Baska.asset") }, "ClueRegistry", out _));
    }

    [Test]
    public void TwoShippingMatches_DoNotResolve_AmbiguityIsNotResolution()
    {
        // Addressables adres tekilliğini ZORLAMIYOR. İlk eşleşmede durulsaydı
        // hangi asset'in doğrulandığı sözlük sırasına kalırdı — check'in ortadan
        // kaldırmak için var olduğu belirsizliğin ta kendisi.
        Assert.IsFalse(AddressableKeySelection.TrySelectUnique(
            new[] { Candidate("ClueRegistry", "Assets/A.asset"), Candidate("ClueRegistry", "Assets/B.asset") },
            "ClueRegistry", out _));
    }

    [Test]
    public void NonShippingMatch_DoesNotResolve()
    {
        // Grup build'e dahil değil / buildable schema devre dışı / "Include
        // Addresses in Catalog" kapalı → adres katalogda YOK → çalışma zamanı
        // InvalidKeyException atar. Editörde adres görünüyor olması yetmez.
        Assert.IsFalse(AddressableKeySelection.TrySelectUnique(
            new[] { Candidate("ClueRegistry", "Assets/R.asset", ships: false) }, "ClueRegistry", out _));
    }

    [Test]
    public void NonShippingDuplicate_DoesNotBlockAValidMatch()
    {
        // Ship etmeyen bir kopya belirsizlik SAYILMAZ: katalogda yalnız biri var.
        bool resolved = AddressableKeySelection.TrySelectUnique(
            new[]
            {
                Candidate("ClueRegistry", "Assets/Olu.asset", ships: false),
                Candidate("ClueRegistry", "Assets/Canli.asset"),
            },
            "ClueRegistry", out string path);

        Assert.IsTrue(resolved);
        Assert.AreEqual("Assets/Canli.asset", path);
    }

    [Test]
    public void BlankAssetPath_DoesNotResolve()
    {
        // Silinmiş GUID'de `entry.AssetPath` boş string döner.
        Assert.IsFalse(AddressableKeySelection.TrySelectUnique(
            new[] { Candidate("ClueRegistry", string.Empty) }, "ClueRegistry", out _));
    }

    [Test]
    public void KeyMatching_IsCaseSensitive()
    {
        Assert.IsFalse(AddressableKeySelection.TrySelectUnique(
            new[] { Candidate("clueregistry", "Assets/R.asset") }, "ClueRegistry", out _));
    }

    [Test]
    public void EmptyCandidateList_DoesNotResolve()
    {
        // `AddressableAssetSettings` yüklenemediğinde (isCompiling/isUpdating)
        // aday listesi boş gelir — yön fail-CLOSED olmalı.
        Assert.IsFalse(AddressableKeySelection.TrySelectUnique(
            new AddressableEntryCandidate[0], "ClueRegistry", out _));
    }

    [Test]
    public void KeyCheck_AsksForTheCanonicalKey()
    {
        // Check'in İÇİNDE yazım hatalı bir literal ("ClueRegisty") olsaydı
        // yukarıdaki throws/doesn't-throw çifti YİNE geçerdi — sahte dikiş her
        // anahtara aynı cevabı veriyor. Ayırt eden tek iddia bu.
        var resolver = new FakeKeyResolver(resolves: true);
        var check = new AnlatiClueRegistryKeyCheck(resolver, new FakeRegistrySource(Registry()));

        check.Run(Context(check));

        Assert.AreEqual(AnlatiDurumIpucuTakibi.ClueRegistryAddressableKey, resolver.LastKey);
        // Sabitin KENDİSİ kayarsa yukarıdaki iddia totolojiye döner — literal de sabitlenir.
        Assert.AreEqual("ClueRegistry", resolver.LastKey);
    }

    // ── AC-1: Boş RequiredShiftIds → hata ──

    [Test]
    public void EmptyRequiredShiftIds_FailsBuild_AndNamesTheAsset()
    {
        ClueRegistry registry = Registry(Definition("BosIpucuAsset", "clue-bos"));
        var check = new AnlatiRequiredShiftIdsCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("BosIpucuAsset", exception.Message);
        StringAssert.Contains("clue-bos", exception.Message);
    }

    [Test]
    public void PopulatedRequiredShiftIds_DoesNotFailBuild()
    {
        ClueRegistry registry = Registry(
            Definition("IpucuA", "clue-a", "shift-a"),
            Definition("IpucuB", "clue-b", "shift-b", "shift-c"));
        var check = new AnlatiRequiredShiftIdsCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void NullSlotInDefinitions_FailsBuild()
    {
        // Bilinçli kapsam genişletmesi: çalışma zamanında `BuildReverseIndex`
        // null girdileri SESSİZCE atlar, yani yazılmış bir ipucu izsiz kaybolur.
        ClueRegistry registry = Registry(Definition("IpucuA", "clue-a", "shift-a"), null);
        var check = new AnlatiRequiredShiftIdsCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("[1]", exception.Message);
    }

    // ── AC-2: Çift clueId → hata ──

    [Test]
    public void DuplicateClueId_FailsBuild_AndNamesBothRecords()
    {
        // İKİ asset adı FARKLI olmalı: aynı olsaydı tek bir StringAssert her iki
        // kaydın da adlandırıldığını KANITLAMIŞ gibi görünürdü.
        ClueRegistry registry = Registry(
            Definition("IlkKayit", "clue-cakisan", "shift-a"),
            Definition("IkinciKayit", "clue-cakisan", "shift-b"));
        var check = new AnlatiUniqueClueIdCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("IlkKayit", exception.Message);
        StringAssert.Contains("IkinciKayit", exception.Message);
        StringAssert.Contains("clue-cakisan", exception.Message);
    }

    [Test]
    public void ClueIdComparison_IsCaseSensitive_MatchingRuntimeOrdinalSemantics()
    {
        // `StringComparer.Ordinal` → `OrdinalIgnoreCase` değişikliği hiçbir testi
        // kırmasaydı, çift-kimlik kuralı sessizce runtime'dan (varsayılan ordinal
        // HashSet/Dictionary) AYRILIRDI. Check (d)'nin muadili zaten vardı;
        // (c) asimetrik kalmıştı (QL gate bulgusu).
        ClueRegistry registry = Registry(
            Definition("BuyukHarf", "CLUE-A", "shift-a"),
            Definition("KucukHarf", "clue-a", "shift-b"));
        var check = new AnlatiUniqueClueIdCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void NullSlot_DoesNotCrashDuplicateCheck()
    {
        // Null slot'un SAHİBİ check (b). (c) onu ATLAMAK zorunda: null guard'ı
        // düşerse burada çıplak bir NullReferenceException fırlar ve build,
        // (b)'nin işaret eden mesajı yerine okunaksız bir stack trace verir.
        ClueRegistry registry = Registry(Definition("IpucuA", "clue-a", "shift-a"), null);
        var check = new AnlatiUniqueClueIdCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void DistinctClueIds_DoNotFailBuild()
    {
        ClueRegistry registry = Registry(
            Definition("IpucuA", "clue-a", "shift-a"),
            Definition("IpucuB", "clue-b", "shift-a")); // paylaşılan shiftId MEŞRU (GDD AC10)
        var check = new AnlatiUniqueClueIdCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void BlankClueId_FailsBuild_BeforeDuplicateGrouping()
    {
        // Determinizm pini: iki BOŞ clueId, "çift clueId ''" gibi yanıltıcı bir
        // mesaj değil, boş-kimlik mesajı üretmeli.
        ClueRegistry registry = Registry(
            Definition("BosKimlikA", "   ", "shift-a"),
            Definition("BosKimlikB", "   ", "shift-b"));
        var check = new AnlatiUniqueClueIdCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("BosKimlikA", exception.Message);
        StringAssert.Contains("ClueId'si boş", exception.Message);
    }

    [Test]
    public void ThreeRecordsSharingClueId_ReportTheFirstCollidingPair()
    {
        ClueRegistry registry = Registry(
            Definition("Birinci", "clue-x", "shift-a"),
            Definition("Ikinci", "clue-x", "shift-b"),
            Definition("Ucuncu", "clue-x", "shift-c"));
        var check = new AnlatiUniqueClueIdCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("Birinci", exception.Message);
        StringAssert.Contains("Ikinci", exception.Message);
        StringAssert.DoesNotContain("Ucuncu", exception.Message);
    }

    // ── Kayıt bulunamama / boş kayıt: bilinçli sessizlik ──

    [Test]
    public void MissingRegistry_IsSilent_KeyCheckOwnsThatFailure()
    {
        // Aynı arızayı dört kez raporlamak suçluyu gizler — "kayıt yok" vakasının
        // TEK sahibi check (a). Bu dal ancak dikiş null DÖNEBİLDİĞİ için test edilebilir.
        var source = new FakeRegistrySource(null);

        Assert.DoesNotThrow(() => new AnlatiRequiredShiftIdsCheck(source).Run(
            Context(new AnlatiRequiredShiftIdsCheck(source))));
        Assert.DoesNotThrow(() => new AnlatiUniqueClueIdCheck(source).Run(
            Context(new AnlatiUniqueClueIdCheck(source))));
    }

    [Test]
    public void EmptyRegistry_IsSilent()
    {
        // Boş kayıt bu dört kuralın HİÇBİRİNİ ihlal etmez — dördü de yazılmış
        // içeriğin ŞEKLİ hakkında, varlığı hakkında değil.
        //
        // AÇIK SINIR: bugün `ClueRegistry.asset` gerçekten boş, yani (b)(c)(d)
        // hiçbir gerçek içeriğe karşı koşmuyor. "İçerik hiç yazılmadı" sessizce
        // ship olur. Bir presence check'i AYRI bir karardır (içerik henüz
        // yazılmadı) ve iş kalemi olarak kaydedildi. GDD'nin "sıfır-ipucu
        // playthrough" maddesi buraya dayanak DEĞİLDİR: o, OYUNCUNUN ipuçlarını
        // kaçırmasından bahsediyor ve ipuçlarının var olduğunu varsayıyor.
        var source = new FakeRegistrySource(Registry());

        Assert.DoesNotThrow(() => new AnlatiRequiredShiftIdsCheck(source).Run(
            Context(new AnlatiRequiredShiftIdsCheck(source))));
        Assert.DoesNotThrow(() => new AnlatiUniqueClueIdCheck(source).Run(
            Context(new AnlatiUniqueClueIdCheck(source))));
    }

    // ── AC-4: AC22 çaprazı (TR-isik-021) ──

    [Test]
    public void AutomaticZone_WhoseShiftIdIsRequiredByAClue_FailsBuild()
    {
        CreateZone("AmbiyansBolge", "amb-1", TriggerMode.Automatic);
        ClueRegistry registry = Registry(Definition("RizaAtlatanIpucu", "clue-riza", "amb-1"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("AmbiyansBolge", exception.Message);
        StringAssert.Contains("RizaAtlatanIpucu", exception.Message);
        StringAssert.Contains("amb-1", exception.Message);
    }

    [Test]
    public void ManualOnlyZone_WhoseShiftIdIsRequiredByAClue_DoesNotFailBuild()
    {
        // Rıza ön koşulu sağlandığı için bu TAM OLARAK beklenen içerik şeklidir.
        CreateZone("ElleTetiklenenBolge", "man-1", TriggerMode.ManualOnly);
        ClueRegistry registry = Registry(Definition("NormalIpucu", "clue-n", "man-1"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void AutomaticZone_NotRequiredByAnyClue_DoesNotFailBuild()
    {
        CreateZone("AmbiyansBolge", "amb-1", TriggerMode.Automatic);
        ClueRegistry registry = Registry(Definition("IlgisizIpucu", "clue-i", "man-1"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void SecondAutomaticZone_Offending_StillFails()
    {
        // ÇOĞULLUK REGRESYONU: `TriggerMode.Automatic` enum'un varsayılanı (0),
        // yani bir sahnede birden çok Automatic bölge normaldir. Check ilkinde
        // durursa (First/Single/erken return) bu ihlal sessizce kaçardı — tam da
        // `IsikVolumeAutomaticPresenceCheck`'in yaptığı şey, o yüzden ona
        // katlanmadı.
        //
        // Bu test `RunAll` üzerinden DEĞİL, check'ten DOĞRUDAN koşar: origin'deki
        // iki varsayılan BoxCollider kesişir ve `IsikVolume/NoBoxOverlap` önce patlardı.
        CreateZone("TemizAmbiyans", "amb-temiz", TriggerMode.Automatic);
        CreateZone("SucluAmbiyans", "amb-suclu", TriggerMode.Automatic);
        ClueRegistry registry = Registry(Definition("SucluIpucu", "clue-s", "amb-suclu"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("SucluAmbiyans", exception.Message);
    }

    [Test]
    public void AllOffendingClues_AreNamedInOneMessage()
    {
        // Aynı shiftId'yi birden çok ipucu isteyebilir (GDD AC10 meşru sayıyor) —
        // mesaj hepsini adlandırmalı, yoksa yazar tek tek keşfetmek zorunda kalır.
        CreateZone("AmbiyansBolge", "amb-1", TriggerMode.Automatic);
        ClueRegistry registry = Registry(
            Definition("IpucuBir", "clue-1", "amb-1"),
            Definition("IpucuIki", "clue-2", "amb-1", "shift-b"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        var exception = Assert.Throws<BuildFailedException>(() => check.Run(Context(check)));
        StringAssert.Contains("IpucuBir", exception.Message);
        StringAssert.Contains("IpucuIki", exception.Message);
    }

    [Test]
    public void BlankShiftIdAutomaticZone_IsSkipped()
    {
        // Boş shiftId çalışma zamanında da hiçbir ipucuna eşleşemez
        // (`ProcessHeldShift`'in IsNullOrWhiteSpace guard'ı) — atlamak DOĞRU.
        CreateZone("AdsizBolge", "   ", TriggerMode.Automatic);
        ClueRegistry registry = Registry(Definition("Ipucu", "clue-a", "   "));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void CrossCheck_IsCaseSensitive_MatchingRuntimeOrdinalSemantics()
    {
        // Çalışma zamanındaki ters indeks ordinal eşliyor (HashSet/Dictionary
        // varsayılan comparer'ı). Check'i OrdinalIgnoreCase'e "iyileştirmek" onu
        // runtime'dan AYIRIR: burada bloklanan içerik oyunda hiç eşleşmezdi.
        CreateZone("AmbiyansBolge", "amb-1", TriggerMode.Automatic);
        ClueRegistry registry = Registry(Definition("Ipucu", "clue-a", "AMB-1"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)));
    }

    [Test]
    public void CrossCheckViolation_IsAttributedToTheSceneItWasFoundIn()
    {
        // (d) aggregate DEĞİL, bu yüzden `context.ScenePath` `Run` içinde DOLU —
        // hata mesajı suçlu sahneyi taşır. (`FinalizeWalk` null alırdı.)
        ClueRegistry registry = Registry(Definition("SucluIpucu", "clue-s", "amb-2"));
        var check = new AnlatiAutomaticZoneNotClueBearingCheck(new FakeRegistrySource(registry));

        // `OpenSceneMode.Single` öykünmesi: YALNIZ önceki sahnenin bölgeleri yok
        // edilir. Ortak `TearDown()` burada çağrılamaz — o, yürüyüşten ÖNCE
        // kurulan `ClueRegistry`'yi de yok ederdi ve yok edilmiş bir
        // ScriptableObject Unity'nin `==` aşırı yüklemesinde `null` sayıldığı
        // için check "kayıt yok" dalına girip SESSİZCE geçerdi (bu test tam
        // olarak böyle yanlış-yeşil verdi, sonra da yanlış-kırmızı).
        var sceneZones = new List<GameObject>();

        void OpenScene(string scenePath)
        {
            foreach (GameObject zone in sceneZones)
            {
                if (zone != null)
                {
                    Object.DestroyImmediate(zone);
                }
            }
            sceneZones.Clear();

            ShiftZone created = scenePath == "Assets/Scenes/Ikinci.unity"
                ? CreateZone("IkinciSahneBolgesi", "amb-2", TriggerMode.Automatic)
                : CreateZone("BirinciSahneBolgesi", "amb-1", TriggerMode.Automatic);
            sceneZones.Add(created.gameObject);
        }

        var walker = new ScriptedWalker(OpenScene, "Assets/Scenes/Birinci.unity", "Assets/Scenes/Ikinci.unity");

        var exception = Assert.Throws<BuildFailedException>(() =>
            BuildValidationRunner.RunAll(new IBuildCheck[] { check }, walker));
        StringAssert.Contains("Assets/Scenes/Ikinci.unity", exception.Message);
        StringAssert.Contains("IkinciSahneBolgesi", exception.Message);
    }

    // ── Paylaşılan çekirdek: OnValidate'in giriş noktası ──

    [Test]
    public void FindContentViolation_ReportsEmptyListBeforeDuplicateId()
    {
        // `ClueRegistry.OnValidate`'in TEK giriş noktası; hiçbir check onu
        // çağırmadığı için `??` sırası tersine çevrilse ya da bir yarısı düşürülse
        // suite'e GÖRÜNMEZDİ (QL gate bulgusu). Sıra önemli: boş liste, çift
        // kimlikten daha temel bir içerik hatası.
        ClueDefinition[] both =
        {
            Definition("BosListe", "clue-cakisan"),
            Definition("Ikinci", "clue-cakisan", "shift-b"),
        };

        string violation = AnlatiContentValidation.FindContentViolation(both);

        StringAssert.Contains("BosListe", violation);
        StringAssert.Contains("BOŞ", violation);
        StringAssert.DoesNotContain("İKİ kayıtta", violation);
    }

    [Test]
    public void FindContentViolation_FallsThroughToDuplicateId_WhenListsArePopulated()
    {
        ClueDefinition[] duplicates =
        {
            Definition("IlkKayit", "clue-cakisan", "shift-a"),
            Definition("IkinciKayit", "clue-cakisan", "shift-b"),
        };

        string violation = AnlatiContentValidation.FindContentViolation(duplicates);

        StringAssert.Contains("IlkKayit", violation);
        StringAssert.Contains("IkinciKayit", violation);
    }

    [Test]
    public void ClueRegistryOnValidate_WarnsOnViolation_WithoutFailingTheTest()
    {
        // `LogWarning` seçimi kasıtlı: `ApplyModifiedPropertiesWithoutUndo()`
        // OnValidate'i tetikler ve fixture'lar KASTEN ihlalli asset kuruyor.
        // `LogError` olsaydı UTF bunu beklenmeyen log sayıp mevcut yeşil testleri
        // kırardı — o kararı burada sabitliyoruz.
        // İhlal olarak ÇİFT clueId seçildi, boş liste DEĞİL: `LogAssert.Expect`
        // bir kesim noktası koymaz — scope'un BAŞINDAN itibaren tüm logları
        // tarar. Boş listeyle kurulsaydı `ClueDefinition.OnValidate`'in uyarısı
        // beklentiyi karşılar ve `ClueRegistry.OnValidate` bloğu tamamen silinse
        // bile test yeşil kalırdı (adversarial doğrulama bulgusu). Çift kimlik
        // yalnız kayıt seviyesinde görülebildiği için tanım tarafı SESSİZ.
        ClueDefinition first = Definition("IlkKayit", "clue-cakisan", "shift-a");
        ClueDefinition second = Definition("IkinciKayit", "clue-cakisan", "shift-b");

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Anlati\].*İKİ kayıtta"));

        Registry(first, second); // Registry(...)'nin ApplyModifiedProperties'i OnValidate'i tetikler
    }

    [Test]
    public void WhitespaceOnlyEntryInsideRequiredShiftIds_IsDeliberatelyNotBlocked()
    {
        // SINIR PİNİ, kapsama DEĞİL. `["   "]` check (b)'yi GEÇER (Count != 0)
        // ama ipucu asla tamamlanamaz: çalışma zamanı ters indeksi boş girdileri
        // atlar, yani tanım indekse hiç girmez — "ulaşılamaz clue" sınıfı.
        // GDD AC8a yalnız BOŞ listeyi bloklamayı şart koşuyor; ulaşılamazlık
        // Story 005'in bloklamayan uyarısına ait. Bu test o boşluğun BİLİNÇLİ
        // olduğunu kayda geçirir — yoksa "kapsanmış" gibi okunur (QL gate bulgusu).
        ClueRegistry registry = Registry(Definition("UlasilamazIpucu", "clue-u", "   "));
        var check = new AnlatiRequiredShiftIdsCheck(new FakeRegistrySource(registry));

        Assert.DoesNotThrow(() => check.Run(Context(check)),
            "Bu bilinçli bir boşluk — kapanacaksa Story 005'te, bloklamayan uyarı olarak.");
    }

    // ── AC-5: Kayıt, fazlar ve üretim dikişleri ──

    [Test]
    public void AllAnlatiChecks_AreRegistered_WithTheirExpectedPhases()
    {
        // İsim→faz SÖZLÜĞÜ: anlati seti KARIŞIK fazlı (3 AssetScan + 2 SceneScan),
        // bu yüzden isik emsalinin blanket "prefix'in hepsi SceneScan" deseni
        // kopyalanamaz.
        var expected = new Dictionary<string, BuildCheckPhase>
        {
            { "Anlati/ClueRegistryKeyResolves", BuildCheckPhase.AssetScan },
            { "Anlati/RequiredShiftIdsNotEmpty", BuildCheckPhase.AssetScan },
            { "Anlati/UniqueClueIds", BuildCheckPhase.AssetScan },
            { "Anlati/AutomaticZoneNotClueBearing", BuildCheckPhase.SceneScan },
            { "Anlati/OrphanedShiftId", BuildCheckPhase.SceneScan }, // Story 005, non-blocking
        };

        List<IBuildCheck> registered = BuildValidationRegistry.Checks
            .Where(check => check.Name.StartsWith("Anlati/"))
            .ToList();

        // AreEquivalent, IsSubsetOf DEĞİL: alt-küme formu fazladan bir beşinci
        // kaydı sessizce kabul ederdi.
        CollectionAssert.AreEquivalent(expected.Keys, registered.Select(check => check.Name).ToList());

        foreach (IBuildCheck check in registered)
        {
            Assert.AreEqual(expected[check.Name], check.Phase, $"{check.Name} yanlış fazda.");
        }
    }

    [Test]
    public void RegisteredAnlatiChecks_AreWiredToTheProductionSeams()
    {
        // Kayıtlı instance'lar sessiz bir stub'a bağlansaydı hem sessizlik testi
        // hem pozitif tripwire GEÇERDİ — ikisi de ayırt edemez. AC-5'in "gerçek
        // dizi" iddiasını kapatan asıl iddia bu (QL gate bulgusu; reflection
        // emsali `fpc_decoy_build_check_test.cs`).
        List<IBuildCheck> anlatiChecks = BuildValidationRegistry.Checks
            .Where(c => c.Name.StartsWith("Anlati/")).ToList();

        // Sayı iddiası olmadan, kayıtlar düşerse döngü sıfır assert'le geçerdi.
        Assert.AreEqual(5, anlatiChecks.Count);

        foreach (IBuildCheck check in anlatiChecks)
        {
            System.Reflection.FieldInfo[] seams = check.GetType()
                .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(field => field.FieldType == typeof(IClueRegistrySource) ||
                                field.FieldType == typeof(IAddressableKeyResolver))
                .ToArray();

            Assert.IsNotEmpty(seams, $"{check.Name} hiçbir dikiş alanı taşımıyor — enjeksiyon deseni kayboldu.");

            foreach (System.Reflection.FieldInfo seam in seams)
            {
                Assert.AreSame(AddressableClueRegistryAccess.Default, seam.GetValue(check),
                    $"{check.Name}.{seam.Name} üretim dikişine bağlı değil.");
            }
        }
    }

    [Test]
    public void ProductionSeams_AreAlive_NotJustSilent()
    {
        // POZİTİF tripwire. `Assert.DoesNotThrow`'a dayanan bir "tripwire" tam da
        // koruduğu fail-open durumda GEÇERDİ (kayıt ulaşılamaz → check'ler sessiz
        // → fırlatma yok). Gerçek kayıt BUGÜN BOŞ olduğundan, üretim dikişinin
        // canlı olduğunu kanıtlayabilecek TEK iddia budur.
        //
        // Bu test kırmızıya dönerse: ya "ClueRegistry" adresi bozuldu, ya adres
        // bir ClueRegistry OLMAYAN asset'e işaret ediyor — ikisi de (b)(c)(d)'yi
        // sessizce devre dışı bırakır.
        Assert.IsTrue(
            AddressableClueRegistryAccess.Default.KeyResolves(AnlatiDurumIpucuTakibi.ClueRegistryAddressableKey),
            "'ClueRegistry' Addressable adresi çözülmüyor — üretim anahtar dikişi ölü.");

        Assert.IsNotNull(
            AddressableClueRegistryAccess.Default.Load(),
            "Üretim kayıt dikişi null döndü — içerik check'leri sessizce devre dışı kalır.");
    }

    [Test]
    public void RealRegistryArray_RunsClean_WithProductionSeams()
    {
        // AC-5, uçtan uca yön (SESSİZLİK): kayıtlı ÜRETİM instance'ları — sahte
        // dikişler değil — gerçek runner üzerinden koşar ve sağlıklı bir sahnede
        // hiçbiri patlamaz. Dört yeni kaydın gerçek diziye bağlandığını ve gerçek
        // Addressables/AssetDatabase dikişlerine karşı yanlış-pozitif üretmediğini
        // kanıtlar.
        //
        // Diskteki üretim `ClueRegistry`'sini geçici olarak DOLDURUP geri almak
        // cazip ama YASAK (README: on-disk test verisi) — ve bir test yarıda
        // düşerse üretim asset'ini bozuk bırakırdı. İhlal yönü bu yüzden bir
        // altta, enjekte edilmiş dikişlerle ama YİNE gerçek runner üzerinden
        // kanıtlanıyor.
        //
        // Fixture'ın diğer epic'lerin check'lerine karşı SESSİZLİK analizi:
        //   - sahne yolu MVP alanı değil  → Fpc/DecoyPresence sessiz
        //   - zone._lights boş dizi       → LightModeMixed + NoSharedLights sessiz
        //   - TEK bölge                   → NoBoxOverlap'ın çift döngüsü hiç eşleşmez
        //                                   (origin'de İKİ bölge kesişir ve bunu kırardı)
        //   - bölge Automatic             → AutomaticZonePresence aggregate'i tatmin
        // Bu not olmadan fixture'a dokunan biri okunaksız, epic'ler arası bir
        // kırmızıyla karşılaşır.
        CreateZone("AmbiyansBolge", "amb-e2e", TriggerMode.Automatic);

        Assert.DoesNotThrow(() =>
            BuildValidationRunner.RunAll(BuildValidationRegistry.Checks, new FakeWalker()));
    }

    [Test]
    public void AnlatiViolation_IsCaught_ThroughTheRealRunner()
    {
        // AC-5, uçtan uca yön (İHLAL): dört anlati check'i, üretimdeki DİZİLİŞİN
        // aynısıyla ama enjekte dikişlerle, gerçek `RunAll` üzerinden koşar.
        // Kanıtladığı şey runner entegrasyonu: AssetScan check'leri sahne
        // açılmadan koşuyor, SceneScan check'i yürüyüş içinde koşuyor ve suçlu
        // check adı prefix'e giriyor.
        CreateZone("AmbiyansBolge", "amb-1", TriggerMode.Automatic);
        var source = new FakeRegistrySource(Registry(Definition("SucluIpucu", "clue-s", "amb-1")));
        var resolver = new FakeKeyResolver(resolves: true);

        var checks = new IBuildCheck[]
        {
            new AnlatiClueRegistryKeyCheck(resolver, source),
            new AnlatiRequiredShiftIdsCheck(source),
            new AnlatiUniqueClueIdCheck(source),
            new AnlatiAutomaticZoneNotClueBearingCheck(source),
        };

        var exception = Assert.Throws<BuildFailedException>(() =>
            BuildValidationRunner.RunAll(checks, new FakeWalker()));
        StringAssert.Contains("Anlati/AutomaticZoneNotClueBearing", exception.Message);
        StringAssert.Contains("SucluIpucu", exception.Message);
    }
}
