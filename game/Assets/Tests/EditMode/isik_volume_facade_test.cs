using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// isik-volume Story 001: facade sözleşmesi (ADR-0005 addendum) — bilinmeyen
/// shiftId default'ları, IShiftZoneHandle routing'i, TEK raise yolu + in-place
/// reset'in delegate-koruma garantisi, duplicate-register davranışı ve
/// ShiftConfig default'ları. Testler taze IsikVolumeState kurar — statik
/// facade'a asla dokunulmaz (ADR-0001 testability deseni).
/// </summary>
public class IsikVolumeFacadeTest
{
    /// <summary>Story 003'ün gerçek ShiftZone'u yerine geçen enjekte edilebilir sahte handle.</summary>
    private sealed class FakeShiftZoneHandle : IShiftZoneHandle
    {
        public string ShiftId { get; set; } = "test-shift";
        public bool IsShiftActive { get; set; }
        public bool IsShiftPersistent { get; set; }
        public float StingerAudioRadius { get; set; }

        public bool TriggerResult = true;
        public int TriggerCallCount;
        public ShiftConfig LastConfig;
        public int RevertCallCount;

        public bool TriggerShift(ShiftConfig config)
        {
            TriggerCallCount++;
            LastConfig = config;
            return TriggerResult;
        }

        public void RevertShift() => RevertCallCount++;
    }

    // ── AC-1/AC-3: bilinmeyen shiftId — Dormant-eşdeğeri default'lar, throw yok ──

    [Test]
    public void UnknownShiftId_Queries_ReturnDormantEquivalentDefaults()
    {
        // Arrange
        var state = new IsikVolumeState();

        // Act + Assert — false / false / 0f, hiçbiri throw etmez (addendum).
        Assert.IsFalse(state.IsShiftActive("bilinmeyen"));
        Assert.IsFalse(state.IsShiftPersistent("bilinmeyen"));
        Assert.AreEqual(0f, state.GetStingerAudioRadius("bilinmeyen"));
    }

    [Test]
    public void UnknownShiftId_TriggerReturnsFalse_AndRevertIsSilentNoOp()
    {
        // Arrange
        var state = new IsikVolumeState();

        // Act
        bool triggered = state.TriggerShift("bilinmeyen", null);
        state.RevertShift("bilinmeyen"); // sessiz no-op — assert: exception yok

        // Assert
        Assert.IsFalse(triggered, "Kayıtlı bölge yoksa TriggerShift false dönmeli (no-op).");
    }

    // ── AC-2: routing — kayıtlı handle'a delegasyon, deregister sonrası default ──

    [Test]
    public void RegisteredZone_Queries_RouteToHandle()
    {
        // Arrange
        var state = new IsikVolumeState();
        var zone = new FakeShiftZoneHandle
        {
            ShiftId = "depo-rampa",
            IsShiftActive = true,
            IsShiftPersistent = true,
            StingerAudioRadius = 7.5f,
        };

        // Act
        state.RegisterZone(zone);

        // Assert — per-zone durum zone'dan okunur, facade kopyalamaz.
        Assert.IsTrue(state.IsShiftActive("depo-rampa"));
        Assert.IsTrue(state.IsShiftPersistent("depo-rampa"));
        Assert.AreEqual(7.5f, state.GetStingerAudioRadius("depo-rampa"));
    }

    [Test]
    public void RegisteredZone_TriggerAndRevert_DelegateToHandle()
    {
        // Arrange
        var state = new IsikVolumeState();
        var zone = new FakeShiftZoneHandle { ShiftId = "depo-rampa", TriggerResult = false };
        state.RegisterZone(zone);
        var config = ScriptableObject.CreateInstance<ShiftConfig>();

        try
        {
            // Act
            bool result = state.TriggerShift("depo-rampa", config);
            state.RevertShift("depo-rampa");

            // Assert — dönüş değeri ve config handle'a aynen geçer (zaten-aktif
            // no-op semantiği zone'un kararıdır, facade'ın değil).
            Assert.IsFalse(result, "Facade, handle'ın TriggerShift dönüşünü aynen iletmeli.");
            Assert.AreEqual(1, zone.TriggerCallCount);
            Assert.AreSame(config, zone.LastConfig);
            Assert.AreEqual(1, zone.RevertCallCount);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void DeregisteredZone_Queries_ReturnDefaultsAgain()
    {
        // Arrange
        var state = new IsikVolumeState();
        var zone = new FakeShiftZoneHandle { ShiftId = "depo-rampa", IsShiftActive = true, StingerAudioRadius = 3f };
        state.RegisterZone(zone);

        // Act
        state.DeregisterZone(zone);

        // Assert
        Assert.IsFalse(state.IsShiftActive("depo-rampa"));
        Assert.AreEqual(0f, state.GetStingerAudioRadius("depo-rampa"));
        Assert.IsFalse(state.TriggerShift("depo-rampa", null));
    }

    // ── AC-2b: TEK raise yolu + in-place reset'in delegate koruması ──

    [Test]
    public void RaiseShiftStateChanged_InvokesSubscriberExactlyOnce_WithFullPayload()
    {
        // Arrange
        var state = new IsikVolumeState();
        var received = new List<(string shiftId, ShiftState newState, Vector3 center, float radius)>();
        state.OnShiftStateChanged += (id, s, c, r) => received.Add((id, s, c, r));
        var center = new Vector3(1f, 2f, 3f);

        // Act
        state.RaiseShiftStateChanged("depo-rampa", ShiftState.Held, center, 12f);

        // Assert — tam bir kez, doğru payload'la (TR-isik-019'un facade yarısı).
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual("depo-rampa", received[0].shiftId);
        Assert.AreEqual(ShiftState.Held, received[0].newState);
        Assert.AreEqual(center, received[0].center);
        Assert.AreEqual(12f, received[0].radius);
    }

    [Test]
    public void ResetOnLoad_ClearsRoutingTable_ButPreservesEventSubscribers()
    {
        // Arrange — tabloda bir bölge, event'te bir abone.
        var state = new IsikVolumeState();
        var zone = new FakeShiftZoneHandle { ShiftId = "depo-rampa", IsShiftActive = true };
        state.RegisterZone(zone);
        int callCount = 0;
        state.OnShiftStateChanged += (_, _, _, _) => callCount++;

        // Act
        state.ResetOnLoad();

        // Assert — tablo boşaldı (bölgeler OnEnable ile yeniden kaydolur)...
        Assert.IsFalse(state.IsShiftActive("depo-rampa"),
            "ResetOnLoad routing tablosunu temizlemeli — bölge artık yönlendirilmemeli.");

        // ...ama delegate listesi aynen duruyor (ADR-0015 in-place rejimi).
        state.RaiseShiftStateChanged("depo-rampa", ShiftState.ShiftingIn, Vector3.zero, 5f);
        Assert.AreEqual(1, callCount,
            "Process-ömürlü abonelik ResetOnLoad'ı atlatmalı (delegate listesi dokunulmaz).");
    }

    // ── AC-7: duplicate register — tanımlı davranış: son-kazanır + uyarı logu ──

    [Test]
    public void DuplicateShiftIdRegister_LastWins_AndLogsWarning()
    {
        // Arrange
        var state = new IsikVolumeState();
        var first = new FakeShiftZoneHandle { ShiftId = "depo-rampa", StingerAudioRadius = 1f };
        var second = new FakeShiftZoneHandle { ShiftId = "depo-rampa", StingerAudioRadius = 2f };
        state.RegisterZone(first);

        // Act — build-time doğrulama bunu normalde engeller (ADR-0005 Edge Cases);
        // yine de olursa: SON kayıt kazanır, uyarı loglanır (belgeli davranış).
        LogAssert.Expect(LogType.Warning,
            "[IsikVolumeDurumSistemi] Duplicate shiftId register: 'depo-rampa' — " +
            "build-time doğrulama bunu engellemeliydi; son kayıt kazanır (Story 001 AC-7).");
        state.RegisterZone(second);

        // Assert — sorgular artık ikinci handle'a yönlenir...
        Assert.AreEqual(2f, state.GetStingerAudioRadius("depo-rampa"));

        // ...ve bayat İLK kaydın deregister'ı yeni kaydı düşüremez.
        state.DeregisterZone(first);
        Assert.AreEqual(2f, state.GetStingerAudioRadius("depo-rampa"),
            "Bayat handle'ın DeregisterZone'u güncel kaydı tablodan silmemeli.");
    }

    // ── AC-4: ShiftConfig default'ları ──

    [Test]
    public void ShiftConfig_Defaults_MatchLockedProfileValues_AndUnsetStinger()
    {
        // Arrange + Act
        var config = ScriptableObject.CreateInstance<ShiftConfig>();

        try
        {
            // Assert — kilitli WB/CA değerleri (GDD Visual/Audio Requirements),
            // Duration güvenli aralığın ortası, StingerAudioRadius 0 = ayarlanmamış.
            Assert.AreEqual(-60f, config.WhiteBalanceTemperature);
            Assert.AreEqual(10f, config.WhiteBalanceTint);
            Assert.AreEqual(-0.5f, config.PostExposure);
            Assert.AreEqual(-20f, config.Saturation);
            Assert.AreEqual(3f, config.Duration);
            Assert.IsFalse(config.Persistent);
            Assert.AreEqual(0f, config.StingerAudioRadius, "0 = ayarlanmamış (TR-isik-014).");
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }
}
