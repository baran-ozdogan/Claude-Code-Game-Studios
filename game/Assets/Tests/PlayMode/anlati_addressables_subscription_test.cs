using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// anlati-durum-ipucu-takibi Story 003: Addressables lazy-load + GERÇEK
/// Işık/Volume aboneliği (Integration; TR-anlati-006, TR-anlati-005'in wiring
/// yarısı).
///
/// Statik facade'lara BİLEREK dokunur — test edilen şey tam olarak facade'lar
/// arası bağ (`gece_oturum_subscription_test.cs` emsaliyle aynı gerekçe).
/// Otomatik testler Addressables'ın gerçek çözümlemesine BAĞIMLI DEĞİL: ters
/// indeks test tarafından doğrudan kurulur, böylece abonelik/reset/re-fire
/// davranışı Addressables kurulumundan bağımsız doğrulanır. Gerçek anahtar
/// çözümlemesi DEFERRED manuel smoke'tur (AC-5, kanıt dosyası) — editor içi bir
/// PlayMode yüklemesi AssetDatabase provider'ından geçtiği için BUILD player'ın
/// content catalog'u hakkında hiçbir şey kanıtlamaz.
/// </summary>
public class AnlatiAddressablesSubscriptionTest
{
    private sealed class FakeZone : IShiftZoneHandle
    {
        public string ShiftId { get; set; }
        public bool IsShiftActive { get; set; }
        public bool IsShiftPersistent { get; set; }
        public float StingerAudioRadius { get; set; }
        public bool TriggerShift(ShiftConfig config) => true;
        public void RevertShift() { }
    }

    private readonly List<ClueDefinition> _createdDefinitions = new List<ClueDefinition>();

    /// <summary>`OnShiftStateChanged`'in canlı abone sayısı (field-like event backing alanı).</summary>
    private static Delegate ShiftEventDelegate()
    {
        FieldInfo backing = typeof(IsikVolumeState).GetField(
            nameof(IIsikVolumeState.OnShiftStateChanged),
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(backing, "OnShiftStateChanged backing alanı bulunamadı.");

        return (Delegate)backing.GetValue(IsikVolumeDurumSistemi.InternalInstance);
    }

    private static int ShiftEventSubscriberCount() =>
        ShiftEventDelegate()?.GetInvocationList().Length ?? 0;

    /// <summary>
    /// Invocation list'te YALNIZ verilen tipte (ya da onun derleyici-üretimi iç
    /// tipinde) tanımlı handler'ları sayar. Mutlak toplam süit sırasına bağlı
    /// olduğu için ("Gece/Oturum da abone") facade'ın KENDİ aboneliği ancak böyle
    /// tekil olarak iddia edilebilir.
    /// </summary>
    private static int SubscriberCountDeclaredIn(Type owner)
    {
        Delegate multicast = ShiftEventDelegate();
        if (multicast == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Delegate handler in multicast.GetInvocationList())
        {
            for (Type declaring = handler.Method.DeclaringType; declaring != null; declaring = declaring.DeclaringType)
            {
                if (declaring == owner)
                {
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    /// <summary>FoundationBootstrap'ın belgeli sırasının simülasyonu (Işık/Volume → Gece/Oturum → Anlatı).</summary>
    private static void SimulateResetBoundary()
    {
        IsikVolumeDurumSistemi.ResetOnLoad();
        GeceOturumDurumu.ResetOnLoad();
        AnlatiDurumIpucuTakibi.ResetOnLoad();
    }

    private ClueDefinition Definition(string clueId, params string[] requiredShiftIds)
    {
#if UNITY_EDITOR
        var definition = ScriptableObject.CreateInstance<ClueDefinition>();
        _createdDefinitions.Add(definition);

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
#else
        throw new NotSupportedException(
            "Bu fixture ClueDefinition alanlarını SerializedObject ile kurar — yalnız Editor PlayMode'da koşar.");
#endif
    }

    /// <summary>Ters indeksi doğrudan kurar — Addressables yolu devre dışı kalır.</summary>
    private static void PreloadIndex(params ClueDefinition[] definitions) =>
        AnlatiDurumIpucuTakibi.InternalInstance.BuildReverseIndex(definitions);

    /// <summary>
    /// Süreç-genelinde statik durumu SIFIRLAR. `gece_oturum_subscription_test.cs`
    /// emsali gereği hem SetUp hem TearDown'da koşar (başka bir dosyanın bıraktığı
    /// tortu bu fixture'a sızmasın diye).
    ///
    /// `ResetRegistryForTests()` kritik: `ResetOnLoad()` kayıt cache'ini BİLEREK
    /// korur (ADR-0015), yani latch'i sınayan test olmasa bile dolu bir ters indeks
    /// süreç ömrü boyunca kalır ve SONRAKİ PlayMode fixture'larında rastgele bir
    /// Held sessizce ipucu işaretlerdi.
    /// </summary>
    private static void ResetProcessState()
    {
        SimulateResetBoundary();
        AnlatiDurumIpucuTakibi.InternalInstance.ResetRegistryForTests();
        AnlatiDurumIpucuTakibi.InternalInstance.BindEnsureRegistryLoaded(AnlatiDurumIpucuTakibi.RegistryLoader);
    }

    [SetUp]
    public void SetUp()
    {
        _ = AnlatiDurumIpucuTakibi.Instance;
        ResetProcessState();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        ResetProcessState();

        for (int i = 0; i < _createdDefinitions.Count; i++)
        {
            if (_createdDefinitions[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdDefinitions[i]);
            }
        }
        _createdDefinitions.Clear();

        yield return null;
    }

    // ── AC-2: Facade static-ctor aboneliği ──

    [UnityTest]
    public IEnumerator FacadeFirstAccess_SubscribesExactlyOnce_AndSurvivesReset()
    {
        // Facade'a ilk erişim static ctor'ı tetikler (bu süit içinde daha önce
        // erişilmiş olabilir — bu yüzden mutlak TOPLAM değil, facade'ın KENDİ
        // handler'ı sayılır: aksi halde Gece/Oturum'un aboneliği tek başına
        // `> 0` iddiasını karşılar ve bu testten hiçbir şey öğrenilmez).
        _ = AnlatiDurumIpucuTakibi.Instance;
        yield return null;

        Assert.AreEqual(1, SubscriberCountDeclaredIn(typeof(AnlatiDurumIpucuTakibi)),
            "Facade Işık/Volume'a TAM BİR KEZ abone olmalı.");

        int totalBefore = ShiftEventSubscriberCount();

        // Taze bir State kurmak abone sayısını ARTIRMAMALI — abonelik State'in
        // constructor'ında DEĞİL facade'da (ADR-0007'den bilinçli sapma; Story
        // 001'in saf-state testleri bu yüzden gerçek event'e hiç dokunmuyor).
        _ = new AnlatiDurumState();
        yield return null;
        Assert.AreEqual(1, SubscriberCountDeclaredIn(typeof(AnlatiDurumIpucuTakibi)),
            "new AnlatiDurumState() facade aboneliğini çoğaltmamalı.");
        Assert.AreEqual(totalBefore, ShiftEventSubscriberCount(),
            "new AnlatiDurumState() toplam abone sayısını artırmamalı.");

        // Reset sınırı abonelik BİRİKTİRMEMELİ ve KOPARMAMALI (ADR-0015 in-place).
        SimulateResetBoundary();
        SimulateResetBoundary();
        yield return null;
        Assert.AreEqual(1, SubscriberCountDeclaredIn(typeof(AnlatiDurumIpucuTakibi)),
            "Tekrarlanan reset abonelik biriktirmemeli VE koparmamalı (re-wire yok).");
        Assert.AreEqual(totalBefore, ShiftEventSubscriberCount());
    }

    // ── AC-1 + AC-3: Lazy yükleme kancası ve Persistent re-fire ──

    [UnityTest]
    public IEnumerator RealShiftEvent_ReachesClueTracking_AndPersistentRefireDoesNotDoubleFire()
    {
        PreloadIndex(Definition("clue-a", "shift-a"));

        int clueEvents = 0;
        void Count(string _) => clueEvents++;
        AnlatiDurumIpucuTakibi.Instance.OnClueKnown += Count;

        try
        {
            var zone = new FakeZone { ShiftId = "shift-a", IsShiftPersistent = true };
            IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone);

            // GERÇEK event yolu: Işık/Volume fırlatır, facade aboneliği yakalar.
            IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-a", ShiftState.Held, Vector3.zero, 0f);
            yield return null;

            Assert.IsTrue(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-a"),
                "Gerçek Işık/Volume event'i ipucu takibine ulaşmalı.");
            Assert.AreEqual(1, clueEvents);

            // Persistent shift'in reload sonrası TEK SEFERLİK re-fire'ı (GDD AC11).
            IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-a", ShiftState.Held, Vector3.zero, 0f);
            yield return null;

            Assert.AreEqual(1, clueEvents, "Persistent re-fire İKİNCİ bir OnClueKnown üretmemeli (GDD AC11).");
        }
        finally
        {
            AnlatiDurumIpucuTakibi.Instance.OnClueKnown -= Count;
        }
    }

    [UnityTest]
    public IEnumerator RegistryLoad_NeverOnNonHeld_ExactlyOnceAcrossRepeatedHelds()
    {
        // ADR-0007: lazy yükleme yalnız GERÇEK bir Held'de — boot sırasında
        // (ya da Dormant/Shifting trafiğinde) ASLA; ve tetiklendiğinde TEK KEZ.
        //
        // Prob olarak `IsRegistryLoaded` KULLANILAMAZ: indeksi hiçbir üretim yolu
        // temizlemediği için (ResetOnLoad cache'i bilerek korur) kardeş bir test
        // onu doldurmuşsa iddia kısır kalır. Bunun yerine yükleyici kancasının
        // KENDİSİ sayaç sarmalayıcıyla değiştirilir; teardown üretim yükleyicisini
        // geri bağlar.
        AnlatiDurumState state = AnlatiDurumIpucuTakibi.InternalInstance;

        int invokeCount = 0;
        int loadBodyCount = 0;
        state.BindEnsureRegistryLoaded(() =>
        {
            invokeCount++;

            // Üretim gövdesiyle AYNI guard: ikinci Held'de bloklayan yükleme
            // yeniden koşmamalı.
            if (state.IsRegistryLoaded)
            {
                return;
            }

            loadBodyCount++;
            state.BuildReverseIndex(new[] { Definition("clue-lazy", "shift-lazy") });
        });

        var zone = new FakeZone { ShiftId = "shift-lazy" };
        IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone);

        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-lazy", ShiftState.Dormant, Vector3.zero, 0f);
        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-lazy", ShiftState.ShiftingIn, Vector3.zero, 0f);
        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-lazy", ShiftState.ShiftingOut, Vector3.zero, 0f);
        yield return null;

        Assert.AreEqual(0, invokeCount, "Held olmayan event'ler kayıt yüklemesini TETİKLEMEMELİ.");
        Assert.IsFalse(state.IsRegistryLoaded);

        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-lazy", ShiftState.Held, Vector3.zero, 0f);
        yield return null;

        Assert.AreEqual(1, invokeCount, "İlk GERÇEK Held kancayı tetiklemeli.");
        Assert.AreEqual(1, loadBodyCount, "İlk Held yüklemeyi bir kez koşturmalı.");
        Assert.IsTrue(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-lazy"));

        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-lazy", ShiftState.Held, Vector3.zero, 0f);
        yield return null;

        Assert.AreEqual(2, invokeCount, "Kanca her Held'de çağrılır (once-ness kancanın İÇİNDE).");
        Assert.AreEqual(1, loadBodyCount,
            "Bloklayan yükleme gövdesi yeniden KOŞMAMALI — IsRegistryLoaded guard'ı (ADR-0007).");
    }

    // ── AC-4: Reload sonrası doğrudan sorgu + cache korunması ──

    [UnityTest]
    public IEnumerator AfterSessionBoundary_RegistryCachePreserved_KnownCluesCleared()
    {
        PreloadIndex(Definition("clue-a", "shift-a"));

        AnlatiDurumIpucuTakibi.InternalInstance.ProcessHeldShift("shift-a");
        Assert.IsTrue(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-a"));

        SimulateResetBoundary();
        yield return null;

        // Bilinen ipuçları temizlenir AMA kayıt cache'i KORUNUR — değişmez
        // authored config, yeniden yükleme yok (ADR-0015; ResetOnLoad bir engine
        // asset API'sine dokunamaz).
        Assert.IsFalse(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-a"), "İpucu durumu temizlenmeli.");
        Assert.IsTrue(AnlatiDurumIpucuTakibi.InternalInstance.IsRegistryLoaded, "Kayıt cache'i KORUNMALI.");

        // İkinci oturumda aynı shift yeniden Held'e ulaşınca ipucu yine tamamlanır
        // ve sorgu — event aboneliği OLMADAN — doğru cevap verir (GDD AC12).
        AnlatiDurumIpucuTakibi.InternalInstance.ProcessHeldShift("shift-a");
        Assert.IsTrue(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-a"),
            "Doğrudan sorgu, event'e hiç abone olmadan doğru cevap vermeli.");
    }

    // ── AC-6: İki oturum boyunca TAM-BİR-KEZ teslim (manifest cross-cutting kuralı) ──

    [UnityTest]
    public IEnumerator TwoSessions_RealEventPath_DeliversExactlyOncePerSession()
    {
        // Manifest'in her statik facade için zorunlu kıldığı iki-oturum testi:
        // reset sınırının HEM yukarı akış aboneliğini (Işık/Volume → facade) HEM
        // aşağı akış teslimini (facade → OnClueKnown) koruduğunu ve hiçbirini
        // ÇOĞALTMADIĞINI gerçek event yolundan doğrular.
        PreloadIndex(Definition("clue-6", "shift-6"));

        var zone = new FakeZone { ShiftId = "shift-6" };
        IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone);

        var received = new List<string>();
        void Record(string clueId) => received.Add(clueId);
        AnlatiDurumIpucuTakibi.Instance.OnClueKnown += Record;

        try
        {
            // ── 1. oturum ──
            IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-6", ShiftState.Held, Vector3.zero, 0f);
            yield return null;
            Assert.AreEqual(1, received.Count, "1. oturum TAM BİR teslim üretmeli.");
            Assert.AreEqual("clue-6", received[0]);

            // ── Reset sınırı ──
            SimulateResetBoundary();
            IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone);
            yield return null;

            Assert.AreEqual(1, received.Count, "Reset sınırının KENDİSİ event üretmemeli.");
            Assert.IsFalse(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-6"));

            // ── 2. oturum: aynı gerçek yol, TAM BİR teslim DAHA ──
            IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged("shift-6", ShiftState.Held, Vector3.zero, 0f);
            yield return null;

            Assert.AreEqual(2, received.Count,
                "2. oturum TAM BİR teslim daha üretmeli: 1 ise yukarı akış aboneliği reset'te koptu, " +
                "3+ ise abonelik çoğaldı (ADR-0015 in-place rejimi ikisini de yasaklar).");
            Assert.AreEqual("clue-6", received[1]);
        }
        finally
        {
            AnlatiDurumIpucuTakibi.Instance.OnClueKnown -= Record;
        }
    }

    // ── Yükleme başarısızlığı latch'i (Story 002'den devralınan karar) ──

    [UnityTest]
    public IEnumerator RegistryLoadFailure_IsLatched_NotRetriedEveryHeld()
    {
        // Bloklayan Addressables çağrısının HER Held'de yeniden denenmesini
        // önleyen latch. Üretimde Story 004'ün build-blocking anahtar kontrolü
        // bu vakayı imkânsız kılıyor — bu yalnız savunma katmanı.
        // (Latch süreç ömürlüdür; `ResetProcessState()` teardown'da açar, yoksa
        // bu test kalan tüm PlayMode oturumu için gerçek yükleme yolunu kapatırdı.)
        AnlatiDurumIpucuTakibi.InternalInstance.MarkRegistryLoadFailed();
        yield return null;

        Assert.IsTrue(AnlatiDurumIpucuTakibi.InternalInstance.RegistryLoadFailed);
        Assert.IsTrue(AnlatiDurumIpucuTakibi.InternalInstance.IsRegistryLoaded,
            "Başarısızlık BOŞ indeksle latch'lenmeli — yeniden denenmemeli.");

        // Latch'liyken Held'ler hâlâ güvenle işlenir (yalnız hiçbir ipucu tamamlanmaz).
        Assert.DoesNotThrow(() => AnlatiDurumIpucuTakibi.InternalInstance.ProcessHeldShift("shift-a"));
        Assert.AreEqual(0, AnlatiDurumIpucuTakibi.Instance.GetKnownClueIds().Count);
    }
}
