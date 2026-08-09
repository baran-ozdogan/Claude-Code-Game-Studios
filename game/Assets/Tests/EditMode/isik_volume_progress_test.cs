using NUnit.Framework;
using UnityEngine;

/// <summary>
/// isik-volume Story 002: saf shift-progress çekirdeği + guard rail'ler +
/// formül yardımcıları. Tüm sayısal beklentiler GDD Formulas bölümünün kendi
/// örneklerinden alınmıştır (AC12/13/14c2) — uydurma örnek yok; boundary
/// değerlerinde sayının kendisi test konusudur (test-standartları istisnası).
/// </summary>
public class IsikVolumeProgressTest
{
    private const float Duration = 3.0f; // GDD: geçiş süresi, prototipten kilitli

    // ── AC-1: smoothstep — GDD örnekleri + uçlarda yumuşak başlangıç ──

    [Test]
    public void SmoothStep_GddExamples_MatchExactly()
    {
        // GDD: x=0.25 → 3(0.0625) − 2(0.015625) = 0.15625; x=0.5 → 0.5; uçlar sabit.
        Assert.AreEqual(0.15625f, IsikVolumeFormulas.SmoothStep(0.25f), 0.001f);
        Assert.AreEqual(0.5f, IsikVolumeFormulas.SmoothStep(0.5f), 0.0001f);
        Assert.AreEqual(0f, IsikVolumeFormulas.SmoothStep(0f));
        Assert.AreEqual(1f, IsikVolumeFormulas.SmoothStep(1f));
    }

    [Test]
    public void SmoothStep_NearZero_SlowerThanLinear()
    {
        // Türev uçlarda ~0: x=0.01'de eğri lineerin belirgin altında kalmalı
        // ("fısıltı, alarm değil" — yumuşak başlangıç).
        Assert.Less(IsikVolumeFormulas.SmoothStep(0.01f), 0.01f);
        Assert.Greater(IsikVolumeFormulas.SmoothStep(0.99f), 0.99f); // simetrik yumuşak bitiş
    }

    // ── AC-1: makine — başlangıç durumu, x birikimi, clamp, taze progress ──

    [Test]
    public void Machine_FreshInstance_IsDormantEquivalent()
    {
        // Story 003'ün ticker'ının güveneceği sözleşme: taze makine x=0,
        // Out yönü, progress 0.
        var machine = new ShiftProgressMachine();

        Assert.AreEqual(0f, machine.X);
        Assert.IsFalse(machine.IsShiftingIn);
        Assert.AreEqual(0f, machine.ShiftProgress);
    }

    [Test]
    public void Machine_TickAccumulatesX_AndProgressIsFresh()
    {
        // Arrange
        var machine = new ShiftProgressMachine();
        machine.BeginShiftIn();

        // Act — GDD örneği: Duration=3.0, ElapsedTime=0.75 → x=0.25.
        machine.Tick(0.75f, Duration);

        // Assert
        Assert.AreEqual(0.25f, machine.X, 0.0001f);
        Assert.AreEqual(0.15625f, machine.ShiftProgress, 0.001f);
    }

    [Test]
    public void Machine_XClampsAtBothEnds()
    {
        // Arrange
        var machine = new ShiftProgressMachine();
        machine.BeginShiftIn();

        // Act — toplam 6s > Duration: üst uçta clamp.
        machine.Tick(6f, Duration);
        Assert.AreEqual(1f, machine.X);
        Assert.AreEqual(1f, machine.ShiftProgress);

        // Out yönünde fazla tick: alt uçta clamp.
        machine.BeginShiftOut();
        machine.Tick(10f, Duration);

        // Assert
        Assert.AreEqual(0f, machine.X);
        Assert.AreEqual(0f, machine.ShiftProgress);
    }

    // ── AC-2: yön-flip interrupt — x asla sıfırlanmaz, süreklilik pop'suz ──

    [Test]
    public void Machine_RevertMidShiftIn_ContinuesFromCurrentX_NoPop()
    {
        // Arrange — In'de x=0.6'ya gel (1.8s / 3.0s).
        var machine = new ShiftProgressMachine();
        machine.BeginShiftIn();
        machine.Tick(1.8f, Duration);
        Assert.AreEqual(0.6f, machine.X, 0.0001f);
        float progressAtFlip = machine.ShiftProgress;

        // Act — Revert: yön flip'i x'e dokunmaz.
        machine.BeginShiftOut();

        // Assert — flip anında progress AYNI değerde (pop yok)...
        Assert.AreEqual(progressAtFlip, machine.ShiftProgress,
            "Yön flip'i x'i değiştirmemeli — ShiftProgress aynı değerden devam eder (TR-isik-003).");

        // ...ve tek tick sonra azalmaya başlar, kare farkı tek tick deltasını aşmaz.
        const float dt = 1f / 60f;
        machine.Tick(dt, Duration);
        Assert.Less(machine.ShiftProgress, progressAtFlip, "Out yönünde progress azalmalı.");
        Assert.LessOrEqual(progressAtFlip - machine.ShiftProgress, dt / Duration * 1.5f + 0.0001f,
            "Tek karelik değişim tek tick x-deltasıyla sınırlı olmalı (süreklilik).");
    }

    [Test]
    public void Machine_TriggerMidShiftOut_ContinuesFromCurrentX()
    {
        // Arrange — In'le 1.0'a çık, Out'la 0.4'e in (x = 1 − 1.8/3 = 0.4).
        var machine = new ShiftProgressMachine();
        machine.BeginShiftIn();
        machine.Tick(3f, Duration);
        machine.BeginShiftOut();
        machine.Tick(1.8f, Duration);
        Assert.AreEqual(0.4f, machine.X, 0.0001f);

        // Act — Shifting-Out'ta Trigger: In'e mevcut x'ten döner.
        machine.BeginShiftIn();
        machine.Tick(0.3f, Duration); // +0.1

        // Assert
        Assert.AreEqual(0.5f, machine.X, 0.0001f);
        Assert.IsTrue(machine.IsShiftingIn);
    }

    // ── AC-3: epsilon sabitleri tek statik evde ──

    [Test]
    public void ProjectEpsilon_Constants_MatchGddLockedValues()
    {
        Assert.AreEqual(0.01f, ProjectEpsilon.TIME_EPSILON);
        Assert.AreEqual(0.01f, ProjectEpsilon.RADIUS_EPSILON);
        Assert.AreEqual(0.001f, ProjectEpsilon.HYSTERESIS_EPSILON);
    }

    // ── AC-4: dört guard — dejenere girdi → clamp'li değer ──

    [Test]
    public void Guards_DegenerateInputs_AreClamped()
    {
        // Duration=0 → TIME_EPSILON (sıfıra bölünme yok).
        Assert.AreEqual(0.01f, IsikVolumeFormulas.ClampDuration(0f));

        // k=1.0 da clamp'lenir (tam 1.0 histerezisi sıfırlar — GDD round 3).
        Assert.AreEqual(1.001f, IsikVolumeFormulas.ClampHysteresis(1.0f), 0.00001f);
        Assert.AreEqual(1.001f, IsikVolumeFormulas.ClampHysteresis(0.8f), 0.00001f);

        // M=-0.5 → 0.0 (negatif yoğunluk yok); M=1.0 → < 1.0 (erişilebilirlik tabanı).
        Assert.AreEqual(0f, IsikVolumeFormulas.ClampMemoryIntensityMultiplier(-0.5f));
        Assert.Less(IsikVolumeFormulas.ClampMemoryIntensityMultiplier(1.0f), 1.0f);

        // R=0 → RADIUS_EPSILON (girilemez ölü bölge yok).
        Assert.AreEqual(0.01f, IsikVolumeFormulas.ClampTriggerRadius(0f));

        // Negatif girdiler de aynı tabanlara clamp'lenir.
        Assert.AreEqual(0.01f, IsikVolumeFormulas.ClampDuration(-1f));
        Assert.AreEqual(0.01f, IsikVolumeFormulas.ClampTriggerRadius(-1f));

        // Tasarım aralığındaki değerler dokunulmadan geçer.
        Assert.AreEqual(3.0f, IsikVolumeFormulas.ClampDuration(3.0f));
        Assert.AreEqual(1.15f, IsikVolumeFormulas.ClampHysteresis(1.15f));
        Assert.AreEqual(0.6f, IsikVolumeFormulas.ClampMemoryIntensityMultiplier(0.6f));
        Assert.AreEqual(4.0f, IsikVolumeFormulas.ClampTriggerRadius(4.0f));
    }

    [Test]
    public void Guards_ComposeInsideFormulaHelpers()
    {
        // Manifest'in asıl yasak yüzeyi: formül yardımcıları guard'sız girdi
        // TÜKETEMEZ — dejenere girdiyle çağrı, clamp'li değerlerle hesaplar.
        Assert.AreEqual(0.01f * 1.001f, IsikVolumeFormulas.ExitRadius(0f, 1.0f), 0.00001f);
        Assert.AreEqual(0f, IsikVolumeFormulas.LightIntensity(1.2f, -0.5f, 1f), 0.0001f,
            "Negatif çarpan 0'a clamp'lenmeli: 1.2 × Lerp(1, 0, 1) = 0.");
    }

    // ── AC-5/6: formül yardımcıları — GDD örnekleriyle birebir ──

    [Test]
    public void ExitRadius_GddExamples_MatchExactly()
    {
        // GDD: R=4, k=1.15 → 4.6; R=12, k=1.15 → 13.8 (buffer bölge ölçeğiyle büyür).
        Assert.AreEqual(4.6f, IsikVolumeFormulas.ExitRadius(4f, 1.15f), 0.001f);
        Assert.AreEqual(13.8f, IsikVolumeFormulas.ExitRadius(12f, 1.15f), 0.001f);
    }

    [Test]
    public void BoxHalfExtentMin_SpikeVerifiedExample_MatchesExactly()
    {
        // GDD (spike-doğrulamalı): 4.6 + 1.6×3.0 + 0.9 = 4.6 + 4.8 + 0.9 = 10.3.
        float result = IsikVolumeFormulas.BoxHalfExtentMin(4.6f, 1.6f, 3.0f, 0.9f);
        Assert.AreEqual(10.3f, result, 0.01f);
    }

    [Test]
    public void LightBlend_GddExample_MidpointColorAndIntensity()
    {
        // GDD: Base (255,191,128), Memory (128,179,255), progress 0.5 →
        // (191.5, 185, 191.5); Intensity 1.2 × Lerp(1, 0.6, 0.5) = 0.96.
        var baseColor = new Color(255f / 255f, 191f / 255f, 128f / 255f);
        var memoryColor = new Color(128f / 255f, 179f / 255f, 255f / 255f);

        Color mid = IsikVolumeFormulas.LightColor(baseColor, memoryColor, 0.5f);

        Assert.AreEqual(191.5f, mid.r * 255f, 1f);
        Assert.AreEqual(185f, mid.g * 255f, 1f);
        Assert.AreEqual(191.5f, mid.b * 255f, 1f);
        Assert.AreEqual(0.96f, IsikVolumeFormulas.LightIntensity(1.2f, 0.6f, 0.5f), 0.01f);
    }

    [Test]
    public void LightBlend_EndpointsExact_LockstepFromSharedProgress()
    {
        // Arrange
        var baseColor = new Color(1f, 0.75f, 0.5f);
        var memoryColor = new Color(0.5f, 0.7f, 1f);

        // Act + Assert — progress=0: birebir base; progress=1: birebir memory/çarpan.
        // İki kanal da aynı paylaşılan ShiftProgress girdisinden türer (lockstep yapısal).
        // Kanal-başına toleranslı karşılaştırma — lerp formu değişse bile kırılgan değil.
        AssertColorEqual(baseColor, IsikVolumeFormulas.LightColor(baseColor, memoryColor, 0f));
        Assert.AreEqual(1.2f, IsikVolumeFormulas.LightIntensity(1.2f, 0.6f, 0f), 0.0001f);
        AssertColorEqual(memoryColor, IsikVolumeFormulas.LightColor(baseColor, memoryColor, 1f));
        Assert.AreEqual(1.2f * 0.6f, IsikVolumeFormulas.LightIntensity(1.2f, 0.6f, 1f), 0.001f);
    }

    private static void AssertColorEqual(Color expected, Color actual, float tolerance = 0.0001f)
    {
        Assert.AreEqual(expected.r, actual.r, tolerance);
        Assert.AreEqual(expected.g, actual.g, tolerance);
        Assert.AreEqual(expected.b, actual.b, tolerance);
    }
}
