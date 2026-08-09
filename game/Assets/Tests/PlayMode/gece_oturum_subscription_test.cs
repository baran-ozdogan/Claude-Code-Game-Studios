using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// GOD Story 004: Işık/Volume aboneliğinin GERÇEK wiring'i (Integration).
/// Statik facade'lara bilerek dokunur — test edilen şey tam olarak facade'lar
/// arası bağ: static-init aboneliği ResetAll sınırını atlatır (AC-3) ve
/// tekrarlanan reset abonelik biriktirmez (AC-4, ADR-0015 rejimi).
/// </summary>
public class GeceOturumSubscriptionTest
{
    /// <summary>Işık/Volume routing tablosuna enjekte edilen sahte bölge (Story 003'e kadar).</summary>
    private sealed class FakeZone : IShiftZoneHandle
    {
        public string ShiftId { get; set; }
        public bool IsShiftActive { get; set; }
        public bool IsShiftPersistent { get; set; }
        public float StingerAudioRadius { get; set; }
        public bool TriggerShift(ShiftConfig config) => true;
        public void RevertShift() { }
    }

    /// <summary>
    /// FoundationBootstrap._resetSequence'ın belgeli sırasının simülasyonu
    /// (ADR-0001/0006): Işık/Volume ÖNCE, GeceOturumDurumu SONRA.
    /// </summary>
    private static void SimulateResetBoundary()
    {
        IsikVolumeDurumSistemi.ResetOnLoad();
        GeceOturumDurumu.ResetOnLoad();
    }

    /// <summary>OnShiftStateChanged'in canlı abone sayısı (field-like event backing alanı üzerinden).</summary>
    private static int ShiftEventSubscriberCount()
    {
        FieldInfo backing = typeof(IsikVolumeState).GetField(
            nameof(IIsikVolumeState.OnShiftStateChanged),
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(backing,
            "OnShiftStateChanged backing alanı bulunamadı — field-like event yapısı değişmiş olabilir.");
        var del = (Delegate)backing.GetValue(IsikVolumeDurumSistemi.InternalInstance);
        return del?.GetInvocationList().Length ?? 0;
    }

    [SetUp]
    public void SetUp() => SimulateResetBoundary(); // başka dosyanın olası artığına karşı temiz başlangıç

    [TearDown]
    public void TearDown() => SimulateResetBoundary(); // test artığı state sonraki teste sızmasın

    // ── AC-1 + AC-3: iki-oturum taze abonelik — sınır sonrası İLK event kaybolmaz ──

    [UnityTest]
    public IEnumerator SecondSession_FirstEventAfterResetBoundary_ReachesHandler_AndSessionOneDataIsGone()
    {
        // Arrange — oturum 1: persistent bölge kayıtlı, tetikleyici Fired.
        var zone1 = new FakeZone { ShiftId = "s1-persistent", IsShiftPersistent = true };
        IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone1);
        GeceOturumDurumu.InternalInstance.AddFiredTrigger("s1-persistent");

        // Act — oturum 1 akışı: Shifting-In (Persistent yazımı) + Held (Settled yazımı).
        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged(
            "s1-persistent", ShiftState.ShiftingIn, Vector3.zero, 5f);
        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged(
            "s1-persistent", ShiftState.Held, Vector3.zero, 5f);

        // Assert — ön koşul: oturum 1 wiring'i sağlıklı (AC-1: gerçek IsShiftPersistent
        // sorgusu bölgeden true okudu; handler event'ten çağrıldı).
        Assert.IsTrue(GeceOturumDurumu.Instance.IsPersistent("s1-persistent"),
            "Oturum 1: Shifting-In, gerçek IsShiftPersistent sorgusuyla Persistent yazmalıydı (AC-1).");
        Assert.IsTrue(GeceOturumDurumu.Instance.HasSettled("s1-persistent"),
            "Oturum 1: Fired-kapılı Held, Settled yazmalıydı.");

        yield return null;

        // Act — ResetAll sınırı + oturum 2: sınırdan hemen sonraki İLK event.
        SimulateResetBoundary();
        var zone2 = new FakeZone { ShiftId = "s2-persistent", IsShiftPersistent = true };
        IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone2); // bölgeler OnEnable ile yeniden kaydolur
        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged(
            "s2-persistent", ShiftState.ShiftingIn, Vector3.zero, 5f);

        // Assert — abonelik kopmadı: oturum 2'nin ilk event'i handler'a ULAŞTI...
        Assert.IsTrue(GeceOturumDurumu.Instance.IsPersistent("s2-persistent"),
            "Oturum 2: sınırdan sonraki ilk Shifting-In handler'a ulaşmalıydı (abonelik in-place reset'i atlatır, AC-3).");

        // ...ve oturum 1'in verisi görünmez (taze oturum).
        Assert.IsFalse(GeceOturumDurumu.Instance.HasFired("s1-persistent"),
            "Oturum 2: oturum 1'in Fired kaydı görünmemeli.");
        Assert.IsFalse(GeceOturumDurumu.Instance.HasSettled("s1-persistent"),
            "Oturum 2: oturum 1'in Settled kaydı görünmemeli.");
        Assert.IsFalse(GeceOturumDurumu.Instance.IsPersistent("s1-persistent"),
            "Oturum 2: oturum 1'in Persistent kaydı görünmemeli.");
    }

    // ── AC-4: accumulation — ×2 reset → abone sayısı sabit, 1 event = 1 yazım ──

    [UnityTest]
    public IEnumerator DoubleResetBoundary_DoesNotAccumulateOrDropSubscription_OneEventOneWrite()
    {
        // Arrange — static-init aboneliğinin kurulduğundan emin ol (facade'a dokun).
        Assert.IsNotNull(GeceOturumDurumu.Instance);
        int before = ShiftEventSubscriberCount();
        Assert.GreaterOrEqual(before, 1,
            "GeceOturumDurumu'nun static-init aboneliği kurulmuş olmalıydı (AC-1).");

        // Act — iki ardışık reset sınırı (Reload Domain OFF profilinin simülasyonu).
        SimulateResetBoundary();
        SimulateResetBoundary();

        // Assert — delegate listesi ne büyüdü (accumulation) ne küçüldü (kopma).
        // DİKKAT: AC-4'ün yük taşıyan kontrolü BU satırdır — aşağıdaki davranışsal
        // yarı, handler idempotent olduğu için (HashSet.Add) çift aboneliği TEK
        // BAŞINA yakalayamaz; bu reflection sayımı ileride sadeleştirmeyle silinmemeli.
        Assert.AreEqual(before, ShiftEventSubscriberCount(),
            "İki ResetOnLoad sonrası abone sayısı değişmemeli — re-wire yok, accumulation yok (AC-4, ADR-0015).");

        // Davranışsal yarı: tek Held event'i tam bir kez Settled yazımı üretir.
        var zone = new FakeZone { ShiftId = "acc-shift" };
        IsikVolumeDurumSistemi.InternalInstance.RegisterZone(zone);
        GeceOturumDurumu.InternalInstance.AddFiredTrigger("acc-shift");

        int settledCount = 0;
        Action<string> onSettled = _ => settledCount++;
        GeceOturumDurumu.Instance.OnTriggerSettled += onSettled;
        try
        {
            IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged(
                "acc-shift", ShiftState.Held, Vector3.zero, 5f);

            Assert.AreEqual(1, settledCount,
                "Tek Held event'i tam bir kez OnTriggerSettled üretmeli (çift işleme yok).");
        }
        finally
        {
            GeceOturumDurumu.Instance.OnTriggerSettled -= onSettled;
        }

        yield return null;
    }
}
