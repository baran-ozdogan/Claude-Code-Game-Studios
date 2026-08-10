using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// anlati-durum-ipucu-takibi Story 002: `ClueDefinition`/`ClueRegistry` veri
/// modeli, ters indeks ve Held handler mantığı (ADR-0007; TR-anlati-002/005'in
/// mantık yarısı).
///
/// **Addressables'a HİÇ dokunulmaz** — ters indeks `BuildReverseIndex` ile
/// DOĞRUDAN kurulur, `BindEnsureRegistryLoaded` kancası hiç bağlanmaz. Bu, bu
/// story'nin bir Logic story olarak kapanabilmesinin ön koşulu
/// (`coding-standards.md`: unit testler dış API çağırmaz) ve QL-STORY-READY
/// gate'inin iki lenste birden yakaladığı bulgunun düzeltmesi.
///
/// Fixture'lar `ScriptableObject.CreateInstance` ile runtime-created —
/// on-disk `ClueDefinition`/`ClueRegistry` asset'i YASAK (projenin
/// `AssetDatabase.FindAssets` taramalarını kirletir; isik-volume/fpc emsali).
/// </summary>
public class AnlatiClueDefinitionTest
{
    private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();
    private AnlatiDurumState _state;

    [SetUp]
    public void SetUp()
    {
        _state = new AnlatiDurumState();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (UnityEngine.Object asset in _spawned)
        {
            if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
        }
        _spawned.Clear();
    }

    private ClueDefinition Definition(string clueId, params string[] requiredShiftIds)
    {
        var definition = ScriptableObject.CreateInstance<ClueDefinition>();
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

    /// <summary>Ters indeksi doğrudan kurar — Addressables yolu hiç kullanılmaz.</summary>
    private void LoadDefinitions(params ClueDefinition[] definitions) =>
        _state.BuildReverseIndex(definitions);

    private void Held(string shiftId) => _state.ProcessShiftStateChanged(shiftId, ShiftState.Held);

    // ── AC-1: Ters indeks N:1 ──

    [Test]
    public void BuildReverseIndex_ShiftIdBucket_CarriesEveryDefinitionListingIt()
    {
        // AYIRT EDİCİ kurgu (QL bulgusu): önce B, sonra A. Eğer A'nın kovası her
        // İKİ tanımı da taşımıyorsa clue-2 asla tamamlanmaz — "clue-2 hiç kovada
        // değildi" ile "değerlendirildi ama eksikti" bu sırada AYRIŞIR. (Önceki
        // hâli `Held("A")` ile başlıyordu ve iki durumu ayırt edemiyordu; ayrıca
        // SharedShiftId_OnlyTheSatisfiedDefinitionCompletes ile birebir aynıydı.)
        LoadDefinitions(Definition("clue-1", "A"), Definition("clue-2", "A", "B"));

        Held("B");
        Assert.IsFalse(_state.IsClueKnown("clue-1"), "B, clue-1'in gereksinimi değil.");
        Assert.IsFalse(_state.IsClueKnown("clue-2"), "clue-2 hâlâ A'yı bekliyor.");

        Held("A");
        Assert.IsTrue(_state.IsClueKnown("clue-1"), "A'nın kovası clue-1'i taşımalı.");
        Assert.IsTrue(_state.IsClueKnown("clue-2"), "A'nın kovası clue-2'yi DE taşımalı (N:1).");
    }

    [Test]
    public void BuildReverseIndex_CalledTwice_ReplacesIndex_DoesNotMerge()
    {
        // "Artımlı kayıt yükleme" gibi masum bir düzenleme indeksi birleştirir ve
        // bayat tanımları canlı bırakırdı — hiçbir test fark etmezdi (QL bulgusu).
        LoadDefinitions(Definition("clue-eski", "A"));
        LoadDefinitions(Definition("clue-yeni", "B"));

        Held("A");
        Assert.IsFalse(_state.IsClueKnown("clue-eski"), "İkinci BuildReverseIndex ilkini DEĞİŞTİRMELİ, birleştirmemeli.");

        Held("B");
        Assert.IsTrue(_state.IsClueKnown("clue-yeni"));
    }

    [Test]
    public void ClueRegistry_DefinitionsFeedTheIndex_EndToEnd()
    {
        // `ClueRegistry`'nin kendi sözleşmesi: Story 003 tam bu dikişi çağıracak
        // (`registry.Definitions` → `BuildReverseIndex`). Taze bir asset'te liste
        // BOŞ ve non-null olmalı.
        var registry = ScriptableObject.CreateInstance<ClueRegistry>();
        _spawned.Add(registry);

        Assert.IsNotNull(registry.Definitions);
        Assert.AreEqual(0, registry.Definitions.Count, "Taze ClueRegistry boş ve non-null olmalı.");

        var serialized = new UnityEditor.SerializedObject(registry);
        UnityEditor.SerializedProperty list = serialized.FindProperty("_definitions");
        list.ClearArray();
        list.InsertArrayElementAtIndex(0);
        list.GetArrayElementAtIndex(0).objectReferenceValue = Definition("clue-reg", "A");
        serialized.ApplyModifiedPropertiesWithoutUndo();

        _state.BuildReverseIndex(registry.Definitions);
        Held("A");

        Assert.IsTrue(_state.IsClueKnown("clue-reg"), "Registry'den beslenen indeks çalışmalı.");
    }

    [Test]
    public void BuildReverseIndex_UnknownShiftId_IsSilentlyIgnored()
    {
        LoadDefinitions(Definition("clue-1", "A"));

        Assert.DoesNotThrow(() => Held("hic-kayitli-olmayan-shift"));
        Assert.AreEqual(0, _state.GetKnownClueIds().Count);
        Assert.IsTrue(_state.HasSeenShift("hic-kayitli-olmayan-shift"),
            "Kayıtsız shiftId de SeenShiftIds'e girer — ileride bir tanım onu isteyebilir.");
    }

    // ── AC-2: ALL-semantiği, sıra bağımsız (tek ilerleyen senaryo) ──

    [Test]
    public void AllSemantics_CompletesOnlyWhenEveryRequiredShiftHeld()
    {
        LoadDefinitions(Definition("clue-ab", "A", "B"));
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        // Yalnız A → henüz bilinmiyor, event yok (GDD AC1).
        Held("A");
        Assert.IsFalse(_state.IsClueKnown("clue-ab"));
        Assert.AreEqual(0, eventCount);

        // Sonra B → tamamlanır, event TAM BİR KEZ (GDD AC2).
        Held("B");
        Assert.IsTrue(_state.IsClueKnown("clue-ab"));
        Assert.AreEqual(1, eventCount);
    }

    [Test]
    public void AllSemantics_IsOrderIndependent_ReverseOrderSameOutcome()
    {
        LoadDefinitions(Definition("clue-ab", "A", "B"));
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        Held("B");
        Assert.IsFalse(_state.IsClueKnown("clue-ab"), "Ters sırada da yarım kalmış tamamlanmamalı.");
        Assert.AreEqual(0, eventCount);

        Held("A");
        Assert.IsTrue(_state.IsClueKnown("clue-ab"), "Sıra bağımsız — B-önce-A aynı sonucu vermeli.");
        // "Aynı sonuç" event sayısını da kapsar (AC ifadesinin tamamı).
        Assert.AreEqual(1, eventCount, "Ters sırada da event TAM BİR KEZ fırlamalı.");
    }

    [Test]
    public void RepeatedHeld_OnAlreadyCompleteClue_DoesNotRefireEvent()
    {
        // Persistent shift'in reload sonrası re-fire'ının mantık yarısı
        // (gerçek reload senaryosu Story 003'te).
        LoadDefinitions(Definition("clue-a", "A"));
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        Held("A");
        Held("A");
        Held("A");

        Assert.AreEqual(1, eventCount, "Tekrarlanan Held ikinci bir OnClueKnown üretmemeli.");
    }

    // ── AC-3: Yalnız Held işlenir ──

    [Test]
    public void NonHeldStates_AreIgnored_AndDoNotLeakIntoSeenShiftIds()
    {
        // Davranışsal test (QL gate'in istediği şekil): A için Shifting-In
        // gönderilir, sonra B için Held — hâlâ tamamlanmamalı. A için Held
        // gelince tamamlanır. Bu tek senaryo hem "yalnız Held sayılır" hem
        // "önceki Shifting-In sızmadı" iddiasını kanıtlar.
        LoadDefinitions(Definition("clue-ab", "A", "B"));

        _state.ProcessShiftStateChanged("A", ShiftState.ShiftingIn);
        Held("B");

        Assert.IsFalse(_state.IsClueKnown("clue-ab"), "Shifting-In SeenShiftIds'e sızmamalı.");
        Assert.IsFalse(_state.HasSeenShift("A"));

        Held("A");
        Assert.IsTrue(_state.IsClueKnown("clue-ab"));
    }

    [Test]
    public void EveryNonHeldState_ProducesNoEffect()
    {
        // Enum ÜZERİNDEN sürülür: ileride beşinci bir durum eklenirse test
        // kendiliğinden onu da kapsar (elle sayılan üç durum bayatlardı).
        LoadDefinitions(Definition("clue-a", "A"));

        foreach (ShiftState state in Enum.GetValues(typeof(ShiftState)))
        {
            if (state == ShiftState.Held)
            {
                continue;
            }

            _state.ProcessShiftStateChanged("A", state);
        }

        Assert.AreEqual(0, _state.SeenShiftIds.Count, "Held olmayan hiçbir durum giriş eklememeli.");
        Assert.AreEqual(0, _state.GetKnownClueIds().Count);
    }

    // ── AC-4: Paylaşılan shiftId, bağımsız değerlendirme ──

    [Test]
    public void TwoDefinitionsSharingAShiftId_BothCompleteOnSingleHeld()
    {
        // GDD AC10: tek bir Held sıfır, bir ya da BİRDEN FAZLA ipucu tamamlayabilir.
        // İlk eşleşmede erken-return eden bir hata AC-2'yi geçer ama bunu geçemez.
        LoadDefinitions(Definition("clue-1", "A"), Definition("clue-2", "A"));
        var received = new List<string>();
        _state.OnClueKnown += received.Add;

        Held("A");

        CollectionAssert.AreEquivalent(new[] { "clue-1", "clue-2" }, received,
            "Aynı shiftId'yi paylaşan İKİ tanım da bağımsız tamamlanmalı.");
    }

    [Test]
    public void SharedShiftId_OnlyTheSatisfiedDefinitionCompletes()
    {
        LoadDefinitions(Definition("clue-1", "A"), Definition("clue-2", "A", "B"));

        Held("A");

        Assert.IsTrue(_state.IsClueKnown("clue-1"));
        Assert.IsFalse(_state.IsClueKnown("clue-2"), "Gereksinimleri eksik olan tanım tamamlanmamalı.");
    }

    // ── AC-5: Yinelenen shiftId zararsız ──

    [Test]
    public void DuplicateShiftIdInRequiredList_BehavesIdenticallyToDeduped()
    {
        // GDD Edge Cases: HashSet kapsaması yinelenenleri zaten tekilleştirir.
        LoadDefinitions(Definition("clue-dup", "A", "A", "B"));
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        Held("A");
        Assert.IsFalse(_state.IsClueKnown("clue-dup"), "[A,A,B] hâlâ B'yi bekliyor olmalı.");

        Held("B");
        Assert.IsTrue(_state.IsClueKnown("clue-dup"));
        Assert.AreEqual(1, eventCount, "Yinelenen girdi ekstra event üretmemeli.");
    }

    // ── AC-6: Addressables'a dokunulmadı (test dikişi) ──

    [Test]
    public void HeldProcessing_WorksWithoutAnyRegistryLoadHook()
    {
        // Bu story'nin TÜM testleri ters indeksi doğrudan kurar; lazy-yükleme
        // kancası hiç bağlanmaz. Kanca null'ken bile Held işleme çalışmalı —
        // Story 003 onu bağlayana kadar geçerli durum bu.
        LoadDefinitions(Definition("clue-a", "A"));

        Assert.DoesNotThrow(() => Held("A"));
        Assert.IsTrue(_state.IsClueKnown("clue-a"));
    }

    [Test]
    public void BindEnsureRegistryLoaded_HookIsInvokedOnHeld_ButOnlyAfterStateFilter()
    {
        // Story 003'ün bağlayacağı kancanın SÖZLEŞMESİ burada sabitleniyor:
        // yalnız GERÇEK bir Held'de çağrılır (ADR-0007: lazy yükleme filtreden
        // SONRA gelir, böylece boot sırasında asla tetiklenmez).
        int hookCalls = 0;
        _state.BindEnsureRegistryLoaded(() => hookCalls++);
        LoadDefinitions(Definition("clue-a", "A"));

        _state.ProcessShiftStateChanged("A", ShiftState.Dormant);
        _state.ProcessShiftStateChanged("A", ShiftState.ShiftingIn);
        Assert.AreEqual(0, hookCalls, "Held olmayan durumlar yüklemeyi TETİKLEMEMELİ.");

        Held("A");
        Assert.AreEqual(1, hookCalls, "İlk gerçek Held yüklemeyi tetiklemeli.");

        // SAHİPLİK NETLİĞİ (QL bulgusu): kanca HER Held'de çağrılır — "oturum
        // başına bir kez" guard'ı kancanın KENDİSİNDE (Story 003'ün
        // `EnsureRegistryLoaded`'ında, `IsRegistryLoaded` kontrolüyle) yaşar,
        // burada değil. Story 003 tersini varsaymasın diye sabitleniyor.
        Held("A");
        Assert.AreEqual(2, hookCalls, "Kanca her Held'de çağrılır; tek-sefer guard'ı çağrılanın sorumluluğu.");
    }

    // ── Sağlamlık: dejenere girdiler ──

    [Test]
    public void EmptyRequiredShiftIds_CanNeverComplete_VacuousTruthIsUnreachable()
    {
        // GDD'nin EN GÜRÜLTÜLÜ tehlikesi (Core Rules + AC8a + Edge Cases üç
        // paragraf ayırıyor): `SeenShiftIds ⊇ ∅` her zaman DOĞRU olduğundan, boş
        // listeli bir tanım herhangi bir shift'in ilk Held'inde anında "Known"
        // sayılırdı. Bugün bu yapısal olarak imkânsız — boş listeli tanım HİÇBİR
        // kovaya girmiyor. Story 004 bunu build-time engelliyor ama bu ÇALIŞMA
        // ZAMANI davranışı ve pinlenmemişti (QL bulgusu): ADR'ın reddettiği
        // per-event lineer taramaya dönen bir düzenleme hatayı geri getirirdi.
        LoadDefinitions(Definition("clue-bos"));
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        Held("herhangi-bir-shift");
        Held("bir-baskasi");

        Assert.IsFalse(_state.IsClueKnown("clue-bos"), "Boş requiredShiftIds ASLA tamamlanmamalı (vacuous-truth).");
        Assert.AreEqual(0, eventCount);
    }

    [Test]
    public void SubscriberThrows_RemainingCandidatesAreSkipped_ContractPinned()
    {
        // Paylaşılan shiftId senaryosunda (GDD AC10) ilk tamamlanmada patlayan bir
        // abone, KALAN adayları değerlendirilmeden bırakır. Bu bilinçli duruş
        // (`AddFiredTrigger` emsali: yutma yok) ama sonucu ağır — sabitleniyor ki
        // Story 003 canlı event'e bağlarken bilinçli bir karar olsun.
        LoadDefinitions(Definition("clue-1", "A"), Definition("clue-2", "A"));
        _state.OnClueKnown += id =>
        {
            if (id == "clue-1") throw new InvalidOperationException("abone patladı");
        };

        Assert.Throws<InvalidOperationException>(() => Held("A"));

        Assert.IsTrue(_state.IsClueKnown("clue-1"), "Patlamadan önceki yazma geri alınmaz.");
        Assert.IsFalse(_state.IsClueKnown("clue-2"), "Kalan adaylar DEĞERLENDİRİLMEZ — belgeli sözleşme.");
    }

    [Test]
    public void ClueRequiringThreeShifts_CompletesOnlyOnTheLastOne()
    {
        // Testlerin hepsi 1-2 gereksinimliydi; GDD'nin liste modelini seçme
        // gerekçesi Full Vision'ın 15-20 tetikleyicilik ölçeği.
        LoadDefinitions(Definition("clue-abc", "A", "B", "C"));

        Held("C");
        Held("A");
        Assert.IsFalse(_state.IsClueKnown("clue-abc"));

        Held("B");
        Assert.IsTrue(_state.IsClueKnown("clue-abc"));
    }

    [Test]
    public void BlankEntryInsideRequiredShiftIds_MakesClueUncompletable()
    {
        // Boş girdi indekse girmez ama `IsSupersetOf` onu YİNE DE arar — böyle bir
        // ipucu asla tamamlanamaz. Boş-liste ile aynı sessiz-tuzak sınıfı; Story
        // 005'in orphan doğrulayıcısı bunu içerik hatası olarak raporlayacak.
        LoadDefinitions(Definition("clue-bosluklu", "A", "   "));

        Held("A");

        Assert.IsFalse(_state.IsClueKnown("clue-bosluklu"),
            "Boş girdi içeren bir tanım tamamlanamaz — sessizce 'Known' olmamalı.");
    }

    [Test]
    public void EnumeratingKnownCluesWhileMarking_ThrowsInvalidOperation_LiveViewConsequence()
    {
        // `GetKnownClueIds()` canlı görünüm döndürüyor; bunun İKİNCİ sonucu
        // (gezerken işaretleme → koleksiyon-değişti) Story 001'de belgelenmiş ama
        // test edilmemişti. Çok-tamamlanmalı döngü onu ilk kez erişilebilir kılıyor.
        LoadDefinitions(Definition("clue-1", "A"));
        Held("A");

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (string _ in _state.GetKnownClueIds())
            {
                _state.MarkClueKnown("turetilmis-clue");
            }
        }, "Canlı görünümü gezerken işaretlemek koleksiyon-değişti istisnası vermeli (belgeli sonuç).");
    }

    [Test]
    public void BuildReverseIndex_SkipsNullAndBlankDefinitions_WithoutThrowing()
    {
        // Build-time doğrulama (Story 004) bunları zaten engelliyor; bu yalnız
        // çalışma zamanı sağlamlığı — bir içerik hatası NRE ile oyunu düşürmesin.
        Assert.DoesNotThrow(() => _state.BuildReverseIndex(new List<ClueDefinition>
        {
            null,
            Definition(string.Empty, "A"),
            Definition("clue-ok", "A"),
        }));

        Held("A");
        CollectionAssert.AreEquivalent(new[] { "clue-ok" }, _state.GetKnownClueIds());
    }

    [Test]
    public void BuildReverseIndex_NullList_LeavesIndexEmptyButLoaded()
    {
        _state.BuildReverseIndex(null);

        Assert.IsTrue(_state.IsRegistryLoaded, "null liste de 'yüklendi' sayılır — tekrar denenmemeli.");
        Assert.DoesNotThrow(() => Held("A"));
        Assert.AreEqual(0, _state.GetKnownClueIds().Count);
    }

    [Test]
    public void ProcessHeldShift_BeforeRegistryLoaded_StillRecordsSeenShift()
    {
        // İndeks yokken de shiftId kaydedilmeli: Story 003'te yükleme ilk Held'de
        // olur, ama bir hata yüzünden indeks kurulamazsa bile görülen shift'ler
        // kaybolmamalı (sonraki tamamlanma kontrolleri onları kullanır).
        Assert.IsFalse(_state.IsRegistryLoaded);

        _state.ProcessHeldShift("A");

        Assert.IsTrue(_state.HasSeenShift("A"));
    }

    [Test]
    public void ProcessHeldShift_NullOrBlankShiftId_IsSilentNoOp()
    {
        LoadDefinitions(Definition("clue-a", "A"));

        _state.ProcessHeldShift(null);
        _state.ProcessHeldShift("   ");

        Assert.AreEqual(0, _state.SeenShiftIds.Count);
    }

    // ── Reset etkileşimi ──

    [Test]
    public void ResetOnLoad_ClearsSeenShifts_ButKeepsRegistryIndex()
    {
        // Kayıt cache'i değişmez authored config — oturumlar arası KORUNUR
        // (ADR-0015; ResetOnLoad bir engine asset API'sine dokunamaz).
        LoadDefinitions(Definition("clue-a", "A"));
        Held("A");
        Assert.IsTrue(_state.IsClueKnown("clue-a"));

        _state.ResetOnLoad();

        Assert.IsFalse(_state.IsClueKnown("clue-a"), "İpucu durumu temizlenmeli.");
        Assert.IsFalse(_state.HasSeenShift("A"), "Görülen shift'ler temizlenmeli.");
        Assert.IsTrue(_state.IsRegistryLoaded, "Kayıt indeksi KORUNMALI — yeniden yükleme yok.");

        // İkinci oturumda aynı shift yeniden Held'e ulaşınca ipucu yine tamamlanır.
        Held("A");
        Assert.IsTrue(_state.IsClueKnown("clue-a"));
    }
}
