using NUnit.Framework;

/// <summary>
/// birinci-sahis-kontrolcu Story 002: Hareket matematiği (saf) — GDD Formulas
/// 1/2/3 + paylaşılan mesafe-tabanlı faz akümülatörü. Tamamı `[Test]` — saf C#,
/// `MonoBehaviour` yaşam döngüsü yok (isik-volume `isik_volume_progress_test.cs`
/// emsaliyle aynı desen).
/// </summary>
public class FpcMovementMathTest
{
    // ── AC-1: Formül 1 — ilk-kare çapası ──

    [Test]
    public void SpeedSmooth_FirstFrame_MatchesGddExample()
    {
        // Arrange — GDD örneği: v=0, v_target=1.6, T_ramp=0.2s (k=15/s), Δt=1/60s.
        float v = MovementMathFormulas.SpeedSmooth(0f, 1.6f, 1f / 60f, 0.2f);

        // Assert
        Assert.AreEqual(0.35f, v, 0.01f, "GDD örneği: 1. kare v≈0.35 m/s.");
    }

    [Test]
    public void SpeedSmooth_Deceleration_FirstFrame_SymmetricDecayRate()
    {
        // Edge case — simetrik yavaşlama: aynı Δt/T_ramp, ters yön (v=1.6→v_target=0).
        float v = MovementMathFormulas.SpeedSmooth(1.6f, 0f, 1f / 60f, 0.2f);

        Assert.AreEqual(1.246f, v, 0.01f, "Aynı çürüme oranı ters yönde de geçerli olmalı.");
    }

    // ── AC-2: Formül 1 — yakınsama ──

    [Test]
    public void SpeedSmooth_AfterRampTime_ReachesNinetyFivePercent_NeverOvershoots()
    {
        // Arrange — ~12 kare (0.2s) T_ramp=0.2s boyunca sürülür.
        float v = 0f;
        const float target = 1.6f;
        const float rampTime = 0.2f;
        const float dt = 1f / 60f;

        for (int frame = 0; frame < 12; frame++)
        {
            v = MovementMathFormulas.SpeedSmooth(v, target, dt, rampTime);
            Assert.LessOrEqual(v, target, $"Kare {frame}: v hiçbir zaman v_target'ı aşmamalı (overshoot yok).");
        }

        // Assert — GDD örneği: ~0.2s sonra v ≈ 1.6×(1-e^-3) ≈ 1.52 m/s (%95).
        Assert.GreaterOrEqual(v, 1.52f, "12 kare (0.2s) sonra v ≥ 1.52 m/s olmalı.");
    }

    [Test]
    public void SpeedSmooth_DegenerateRampTime_GuardedAgainstDivideByZero()
    {
        // Edge case — T_ramp=0: 3/0 → Infinity → NaN riski. ProjectEpsilon.TIME_EPSILON'a
        // taban çekilir (isik-volume ClampDuration emsaliyle aynı guard deseni).
        float v = MovementMathFormulas.SpeedSmooth(0f, 1.6f, 1f / 60f, 0f);

        Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v));
        Assert.GreaterOrEqual(v, 0f);
        Assert.LessOrEqual(v, 1.6f);
    }

    [Test]
    public void SpeedSmooth_Deceleration_AfterRampTime_DropsToNearZero()
    {
        // Edge case — yavaşlamada 0.2s içinde ≤0.08 m/s'ye düşer.
        float v = 1.6f;
        const float dt = 1f / 60f;

        for (int frame = 0; frame < 12; frame++)
        {
            v = MovementMathFormulas.SpeedSmooth(v, 0f, dt, 0.2f);
        }

        Assert.LessOrEqual(v, 0.08f, "0.2s (T_ramp) sonra yavaşlama v'yi ≤0.08 m/s'ye düşürmeli.");
    }

    // ── AC-3: Taper d≥1.5m'de etkisiz ──

    [Test]
    public void TaperMultiplier_AtOrBeyondRadius_IsExactlyOne()
    {
        Assert.AreEqual(1.0f, MovementMathFormulas.TaperMultiplier(1.5f), 1e-6f);
        Assert.AreEqual(1.0f, MovementMathFormulas.TaperMultiplier(3.0f), 1e-6f, "Yarıçapın ötesi de clamp'lenip 1.0 vermeli.");
        Assert.AreEqual(1.6f, MovementMathFormulas.TargetSpeed(1.0f, 1.5f), 1e-6f);
    }

    // ── AC-4: Taper d=0'da maksimum ──

    [Test]
    public void TaperMultiplier_AtZeroDistance_IsExactlyMinimum()
    {
        Assert.AreEqual(0.7f, MovementMathFormulas.TaperMultiplier(0f), 1e-6f);
        Assert.AreEqual(1.12f, MovementMathFormulas.TargetSpeed(1.0f, 0f), 1e-6f);
    }

    // ── AC-5: Taşırken çarpımsal bileşim ──

    [Test]
    public void TargetSpeed_WhileCarrying_MatchesGddExample()
    {
        // Arrange — GDD örneği: CarryMult=0.84375 (1.35/1.6), d=0.5m.
        const float carryMult = MovementMathFormulas.CarryMultiplier;
        const float d = 0.5f;

        float taperMult = MovementMathFormulas.TaperMultiplier(d);
        float targetSpeed = MovementMathFormulas.TargetSpeed(carryMult, d);

        // Assert — x=0.333, ease=0.259, TaperMult≈0.778, v_target≈1.05.
        Assert.AreEqual(0.778f, taperMult, 0.001f);
        Assert.AreEqual(1.05f, targetSpeed, 0.01f);
    }

    [Test]
    public void CarryMultiplier_MatchesGddLockedRatio()
    {
        // GDD Core Rules: CarrySpeed=1.35 m/s, WalkSpeed=1.6 m/s → CarryMult=0.84375.
        Assert.AreEqual(1.35f, MovementMathFormulas.CarrySpeed, 1e-6f);
        Assert.AreEqual(0.84375f, MovementMathFormulas.CarryMultiplier, 1e-6f);
    }

    [Test]
    public void TargetSpeed_MultiplierOrderIndependent()
    {
        // Edge case — a×b=b×a: çarpım sırası sonucu değiştirmemeli.
        const float carryMult = MovementMathFormulas.CarryMultiplier;
        const float d = 0.5f;

        float viaFunction = MovementMathFormulas.TargetSpeed(carryMult, d);
        float taperMult = MovementMathFormulas.TaperMultiplier(d);
        float reversedOrder = MovementMathFormulas.WalkSpeed * taperMult * carryMult;

        Assert.AreEqual(viaFunction, reversedOrder, 1e-6f);
    }

    // ── AC-6: Head-bob S=0'da sıfır ──

    [Test]
    public void HeadBobAmplitude_AtZeroSlider_IsExactlyZero_RegardlessOfSpeed()
    {
        Assert.AreEqual(0f, MovementMathFormulas.HeadBobAmplitude(0.8f, 0f, 2.5f), 1e-6f);
        Assert.AreEqual(0f, MovementMathFormulas.HeadBobAmplitude(1.6f, 0f, 2.5f), 1e-6f);
    }

    [Test]
    public void HeadBobAmplitude_AtZeroSpeed_IsExactlyZero_RegardlessOfSlider()
    {
        // Formül 3'ün üst-seviye AC'si: "v=0'da tam 0" — footstep-tabanlı olduğundan
        // doğal sonuç (GDD Çıktı Aralığı), S ne olursa olsun.
        Assert.AreEqual(0f, MovementMathFormulas.HeadBobAmplitude(0f, 40f, 2.5f), 1e-6f);
        Assert.AreEqual(0f, MovementMathFormulas.HeadBobAmplitude(0f, 100f, 2.5f), 1e-6f);
    }

    // ── AC-7: Head-bob tavan + GDD örneği ──

    [Test]
    public void HeadBobAmplitude_AtFullSliderAndSpeed_EqualsMaxAmplitude()
    {
        Assert.AreEqual(2.5f, MovementMathFormulas.HeadBobAmplitude(1.6f, 100f, 2.5f), 1e-6f);
    }

    [Test]
    public void HeadBobAmplitude_GddExample_MatchesExactly()
    {
        // Edge case — GDD'nin TEK Formül-3 örneği: A_max=2.5cm, S=40, v=1.6 → 1.0cm.
        Assert.AreEqual(1.0f, MovementMathFormulas.HeadBobAmplitude(1.6f, 40f, 2.5f), 0.01f);
    }

    // ── AC-8: Frame-hitch guard ──

    [Test]
    public void SpeedSmooth_FrameHitch_SnapsCloseToTarget_NoOvershoot()
    {
        // Given — Δt=0.5s simüle edilmiş hitch (GDD Edge Case: e^(-k·Δt)→0).
        float v = MovementMathFormulas.SpeedSmooth(0f, 1.6f, 0.5f, 0.2f);

        Assert.AreEqual(1.6f, v, 0.001f, "Büyük Δt'de v neredeyse tam v_target'a atlamalı.");
        Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v));
    }

    [Test]
    public void SpeedSmooth_ExtremeHitch_UnderflowsToExactTarget()
    {
        // Edge case — Δt=10s: e^(-k·Δt) float32'de tam 0'a alt-taşar, v tam v_target olur.
        float v = MovementMathFormulas.SpeedSmooth(0f, 1.6f, 10f, 0.2f);

        Assert.AreEqual(1.6f, v, 1e-6f);
        Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v));
    }

    [Test]
    public void SpeedSmooth_ZeroDeltaTime_LeavesSpeedUnchanged()
    {
        // Edge case — Δt=0: e^0=1, v hiç değişmemeli.
        float v = MovementMathFormulas.SpeedSmooth(0.42f, 1.6f, 0f, 0.2f);

        Assert.AreEqual(0.42f, v, 1e-6f);
    }

    // ── AC-9: Faz akümülatörü determinizmi ──

    [Test]
    public void PhaseAccumulator_AdvancesByAccumulatedDistance()
    {
        // Given — sentetik mesafe-delta dizisi.
        var accumulator = new MovementPhaseAccumulator();

        accumulator.Advance(0.1f);
        accumulator.Advance(0.2f);
        accumulator.Advance(0.05f);
        accumulator.Advance(0f);
        accumulator.Advance(0.3f);

        Assert.AreEqual(0.65f, accumulator.Phase, 1e-5f);
    }

    [Test]
    public void PhaseAccumulator_SharedSingleSource_BobAndFootstepReadIdenticalValue()
    {
        // Then — bob-faz ve ayak-sesi-faz aynı paylaşılan değerden okunur (tek alan,
        // ayrı kopya yok — desync yapısal olarak imkânsız).
        var accumulator = new MovementPhaseAccumulator();
        accumulator.Advance(1.23f);

        float bobPhaseRead = accumulator.Phase;
        float footstepPhaseRead = accumulator.Phase;

        Assert.AreEqual(bobPhaseRead, footstepPhaseRead, 0f);
    }

    [Test]
    public void PhaseAccumulator_NoDistanceDelta_PhaseUnchanged_EvenIfCalledRepeatedly()
    {
        // Then — mesafe deltası sıfırken (zaman geçse de, API deltaTime almıyor) faz değişmez.
        var accumulator = new MovementPhaseAccumulator();
        accumulator.Advance(0.5f);

        accumulator.Advance(0f);
        accumulator.Advance(0f);

        Assert.AreEqual(0.5f, accumulator.Phase, 1e-6f);
    }

    // ── AC-10: Erişilebilirlik bağımsızlığı (manifest guardrail) ──

    /// <summary>
    /// Given: özdeş hareket dizisi. When: S=0% ve S=100%'de AYRI AYRI sürülür (iki
    /// bağımsız simülasyon koşusu — QL-TEST-COVERAGE bulgusu: tek koşuyu iki kez
    /// okumak yerine, S'nin gelecekte bir parametre olarak eklenmesi ihtimaline karşı
    /// gerçekten iki kez sürülüyor). Then: faz/ayak-sesi-tetikleme zaman damgaları
    /// birebir aynı, yalnız Amplitude çıktısı farklı.
    /// </summary>
    [Test]
    public void HeadBobAmplitude_AccessibilitySlider_TwoIndependentRuns_IdenticalPhaseTimeline_OnlyAmplitudeDiffers()
    {
        float[] distanceDeltas = { 0.1f, 0.15f, 0.2f, 0.05f };

        var runAtZeroPercent = SimulateAndSampleAmplitude(distanceDeltas, sliderPercent: 0f);
        var runAtFullPercent = SimulateAndSampleAmplitude(distanceDeltas, sliderPercent: 100f);

        for (int i = 0; i < distanceDeltas.Length; i++)
        {
            // Assert — zaman damgaları (faz/hız) iki AYRI koşuda birebir aynı.
            Assert.AreEqual(runAtZeroPercent.SpeedTimeline[i], runAtFullPercent.SpeedTimeline[i], 1e-6f,
                $"Kare {i}: hız zaman damgası S'den bağımsız olmalı.");
            Assert.AreEqual(runAtZeroPercent.PhaseTimeline[i], runAtFullPercent.PhaseTimeline[i], 1e-6f,
                $"Kare {i}: faz zaman damgası S'den bağımsız olmalı.");

            // Assert — yalnız Amplitude farklı.
            Assert.AreEqual(0f, runAtZeroPercent.AmplitudeTimeline[i], 1e-6f, $"Kare {i}: S=0'da Amplitude tam 0 olmalı.");
            Assert.Greater(runAtFullPercent.AmplitudeTimeline[i], 0f, $"Kare {i}: S=100'de Amplitude sıfırdan büyük olmalı (v>0 iken).");
        }

        Assert.AreEqual(0.5f, runAtZeroPercent.PhaseTimeline[distanceDeltas.Length - 1], 1e-5f,
            "Faz akümülatörünün ilerleme hızı S'den etkilenmemeli — toplam mesafe değişmez.");
    }

    private static (float[] SpeedTimeline, float[] PhaseTimeline, float[] AmplitudeTimeline) SimulateAndSampleAmplitude(
        float[] distanceDeltas, float sliderPercent)
    {
        var accumulator = new MovementPhaseAccumulator();
        float v = 0f;
        var speedTimeline = new float[distanceDeltas.Length];
        var phaseTimeline = new float[distanceDeltas.Length];
        var amplitudeTimeline = new float[distanceDeltas.Length];

        for (int i = 0; i < distanceDeltas.Length; i++)
        {
            v = MovementMathFormulas.SpeedSmooth(v, 1.6f, 1f / 60f, 0.2f);
            accumulator.Advance(distanceDeltas[i]);
            speedTimeline[i] = v;
            phaseTimeline[i] = accumulator.Phase;
            amplitudeTimeline[i] = MovementMathFormulas.HeadBobAmplitude(v, sliderPercent, 2.5f);
        }

        return (speedTimeline, phaseTimeline, amplitudeTimeline);
    }
}
