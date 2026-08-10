using UnityEngine;

/// <summary>
/// Birinci Şahıs Kontrolcü'nün saf formül yardımcıları (GDD Formulas — birebir).
/// Hepsi statik ve allocation'sız — sıcak yol güvenli. `MonoBehaviour` yaşam
/// döngüsü yok; isik-volume'un `IsikVolumeFormulas` ayrımıyla aynı desen.
/// </summary>
public static class MovementMathFormulas
{
    /// <summary>Yüksüz yürüme hızı (m/s) — Core Rules kilitli sabit, Formül 2/3'ün ortak referansı.</summary>
    public const float WalkSpeed = 1.6f;

    /// <summary>Yaklaşma-yavaşlaması yarıçapı (m) — Core Rules kilitli sabit.</summary>
    public const float TaperRadius = 1.5f;

    /// <summary>d=0'daki minimum TaperMult (maks. %30 yavaşlama) — Core Rules kilitli sabit.</summary>
    public const float TaperMinMultiplier = 0.7f;

    /// <summary>ease(x)'in TaperMult'a katkı aralığı (0.7 + BU×ease(x)) — Core Rules kilitli sabit.</summary>
    public const float TaperBonusRange = 0.3f;

    /// <summary>Yük taşırkenki yürüme hızı (m/s) — Core Rules kilitli sabit (`SetCarrying(true)`).</summary>
    public const float CarrySpeed = 1.35f;

    /// <summary>CarryMult = CarrySpeed/WalkSpeed = 0.84375 — `TargetSpeed`'in `carryMultiplier` girdisinin kilitli değeri.</summary>
    public const float CarryMultiplier = CarrySpeed / WalkSpeed;

    /// <summary>
    /// Formül 1 — İvmelenme: v(t+Δt) = v_target + (v(t)-v_target)×e^(-k·Δt), k=3/T_ramp.
    /// Analitik çözüm (Euler entegrasyonu değil) — büyük Δt'de bile kararlı (Edge Case:
    /// frame hitch, e^(-k·Δt)→0, v doğrudan v_target'a yakınsar). T_ramp guard'lı tüketilir
    /// (ProjectEpsilon.TIME_EPSILON — sıfıra bölünmeyi önler).
    /// </summary>
    public static float SpeedSmooth(float currentSpeed, float targetSpeed, float deltaTime, float rampTime)
    {
        float k = 3f / Mathf.Max(ProjectEpsilon.TIME_EPSILON, rampTime);
        float decay = Mathf.Exp(-k * Mathf.Max(0f, deltaTime));
        return targetSpeed + (currentSpeed - targetSpeed) * decay;
    }

    /// <summary>
    /// Formül 2 (birinci yarı) — Taper çarpanı: x=clamp(d/1.5,0,1), ease(x)=x²(3-2x),
    /// TaperMult=0.7+0.3×ease(x). d≥TaperRadius'ta 1.0 (etkisiz); d=0'da 0.7 (maksimum).
    /// </summary>
    public static float TaperMultiplier(float distanceToNearestFlagged)
    {
        float x = Mathf.Clamp01(distanceToNearestFlagged / TaperRadius);
        float ease = x * x * (3f - 2f * x);
        return TaperMinMultiplier + TaperBonusRange * ease;
    }

    /// <summary>
    /// Formül 2 (tam) — v_target = 1.6×CarryMult×TaperMult. Çarpımsal bileşim —
    /// toplamsal DEĞİL (GDD: sıra önemsiz, a×b=b×a, "biri öncelikli" kuralı yok).
    /// </summary>
    public static float TargetSpeed(float carryMultiplier, float distanceToNearestFlagged) =>
        WalkSpeed * carryMultiplier * TaperMultiplier(distanceToNearestFlagged);

    /// <summary>
    /// Formül 3 — Head-Bob Genliği: Amplitude(v,S) = A_max×(v/1.6)×(S/100). Çarpımsal
    /// terim S=0'da veya v=0'da tam 0 garantiler ("çok düşük" değil, GDD Çıktı Aralığı).
    /// `speed`/`accessibilitySliderPercent` guard'lı tüketilir (GDD Çıktı Aralığı "0–A_max"
    /// garantisi — bir üst sistem dejenere/aralık-dışı girdi geçirse bile, isik-volume
    /// `IsikVolumeFormulas` emsaliyle aynı defense-in-depth ilkesi).
    /// </summary>
    public static float HeadBobAmplitude(float speed, float accessibilitySliderPercent, float maxAmplitude) =>
        maxAmplitude * (Mathf.Clamp(speed, 0f, WalkSpeed) / WalkSpeed) * (Mathf.Clamp01(accessibilitySliderPercent / 100f));
}
