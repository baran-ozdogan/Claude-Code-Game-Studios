using UnityEngine;

/// <summary>
/// Işık/Volume sisteminin saf formül ve guard-rail yardımcıları (GDD Formulas —
/// birebir; ADR-0005). Guard'lar KODDA zorunludur, tasarımcı disiplinine
/// bırakılmaz: her formül dejenere Inspector girdisini clamp'leyerek tüketir.
/// Hepsi statik ve allocation'sız — sıcak yol güvenli.
/// </summary>
public static class IsikVolumeFormulas
{
    // MemoryIntensityMultiplier'ın "< 1.0" üst sınırı için somut tavan (GDD sayı
    // vermiyor; 1.0'ın altında herhangi bir değer erişilebilirlik amacını sağlar).
    // BİLEREK bağımsız sabit — HYSTERESIS_EPSILON'dan türetilmez: o, sınır-titreme
    // ayarı için retune edilebilir ve bu tavan onunla birlikte kaymamalı.
    private const float MemoryIntensityCeiling = 0.999f;

    /// <summary>ShiftProgress = 3x²−2x³ (x çağıran tarafça 0-1 tutulur; yine de clamp'lenir).</summary>
    public static float SmoothStep(float x)
    {
        x = Mathf.Clamp01(x);
        return 3f * x * x - 2f * x * x * x;
    }

    /// <summary>Duration guard'ı: ≥ TIME_EPSILON — Duration=0 sıfıra bölünürdü.</summary>
    public static float ClampDuration(float duration) =>
        Mathf.Max(ProjectEpsilon.TIME_EPSILON, duration);

    /// <summary>
    /// k_hysteresis guard'ı: ≥ 1.0 + HYSTERESIS_EPSILON — tam 1.0 da clamp'lenir
    /// (k=1.0'da R_exit=R_trigger olur, histerezisin önlediği sınır-titremesi
    /// bug'ı aynen geri gelirdi; GDD round 3 düzeltmesi).
    /// </summary>
    public static float ClampHysteresis(float kHysteresis) =>
        Mathf.Max(1f + ProjectEpsilon.HYSTERESIS_EPSILON, kHysteresis);

    /// <summary>
    /// MemoryIntensityMultiplier guard'ı: [0.0, 1.0) — alt sınır negatif
    /// LightIntensity'yi, üst sınır erişilebilirlik garantisini (renk körü
    /// oyuncunun okuyacağı parlaklık düşüşü) sessizce iptal etmeyi önler.
    /// </summary>
    public static float ClampMemoryIntensityMultiplier(float multiplier) =>
        Mathf.Clamp(multiplier, 0f, MemoryIntensityCeiling);

    /// <summary>R_trigger guard'ı: ≥ RADIUS_EPSILON — 0, asla girilemeyen sessiz ölü bölge üretirdi.</summary>
    public static float ClampTriggerRadius(float rTrigger) =>
        Mathf.Max(ProjectEpsilon.RADIUS_EPSILON, rTrigger);

    /// <summary>R_exit = R_trigger × k_hysteresis (iki girdi de guard'lı tüketilir; TR-isik-010).</summary>
    public static float ExitRadius(float rTrigger, float kHysteresis) =>
        ClampTriggerRadius(rTrigger) * ClampHysteresis(kHysteresis);

    /// <summary>
    /// BoxHalfExtentMin = R_exit + PlayerMaxSpeed×Duration + SafetyBuffer
    /// (TR-isik-008, spike-doğrulamalı). PlayerMaxSpeed parametredir — FPC
    /// sabitine bağlama birinci-şahıs epic'inde (TR-isik-018'in wiring yarısı).
    /// </summary>
    public static float BoxHalfExtentMin(float rExit, float playerMaxSpeed, float duration, float safetyBuffer) =>
        rExit + playerMaxSpeed * ClampDuration(duration) + safetyBuffer;

    /// <summary>
    /// LightColor = Lerp(BaseColor, MemoryColor, ShiftProgress). LightIntensity
    /// ile AYNI ShiftProgress değerinden beslenmelidir — lockstep, iki kanalın
    /// tek paylaşılan girdiden türemesiyle yapısaldır (GDD Light Blend).
    /// </summary>
    public static Color LightColor(Color baseColor, Color memoryColor, float shiftProgress) =>
        Color.Lerp(baseColor, memoryColor, shiftProgress);

    /// <summary>
    /// LightIntensity = BaseIntensity × Lerp(1, MemoryIntensityMultiplier,
    /// ShiftProgress) — multiplier guard'lı tüketilir.
    /// </summary>
    public static float LightIntensity(float baseIntensity, float memoryIntensityMultiplier, float shiftProgress) =>
        baseIntensity * Mathf.Lerp(1f, ClampMemoryIntensityMultiplier(memoryIntensityMultiplier), shiftProgress);
}
