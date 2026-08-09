using UnityEngine;

/// <summary>
/// Authored per-trigger shift konfigürasyonu (ADR-0005; GDD "Interactions with
/// Other Systems"). SO = yalnız authored config — çalışma zamanı durumu asla
/// burada yaşamaz (manifest, ADR-0001 Alt 2); per-zone runtime durum ShiftZone'da
/// kalır (Story 003). WB/CA hedef default'ları paylaşılan Volume Profile'ın
/// kilitli değerleridir (GDD Visual/Audio Requirements); per-light MemoryColor
/// hedefleri ShiftZone'un ZoneLight dizisinde yaşar, burada değil.
/// </summary>
[CreateAssetMenu(fileName = "ShiftConfig", menuName = "Yankilar/Shift Config")]
public sealed class ShiftConfig : ScriptableObject
{
    [Header("White Balance hedefleri (kilitli değerler: -60 / +10)")]
    public float WhiteBalanceTemperature = -60f;
    public float WhiteBalanceTint = 10f;

    [Header("Color Adjustments hedefleri (kilitli değerler: -0.5 / -20)")]
    public float PostExposure = -0.5f;
    public float Saturation = -20f;

    /// <summary>Geçiş süresi, saniye. Güvenli aralık 2–5 s (GDD Tuning Knobs).</summary>
    [Header("Geçiş")]
    public float Duration = 3f;

    /// <summary>
    /// Finale-özel kalıcılık escape hatch'i — true işaretli shift oturumun geri
    /// kalanı boyunca Shifting-Out'u atlar (GDD "Kalıcılık escape hatch'i").
    /// </summary>
    public bool Persistent;

    /// <summary>
    /// Stinger/ambiyans ses yarıçapı. 0 = ayarlanmamış (TR-isik-014'ün &gt;0
    /// edit-time doğrulaması paylaşılan build-validation'da yaşar, bu tipte değil).
    /// </summary>
    public float StingerAudioRadius;
}
