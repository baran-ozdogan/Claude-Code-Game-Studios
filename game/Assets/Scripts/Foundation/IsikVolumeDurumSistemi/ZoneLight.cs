using UnityEngine;

/// <summary>
/// Bir ShiftZone'un level-designer-atanmış ışık girdisi (ADR-0005 Key
/// Interfaces — birebir). Işıklar Inspector'da açıkça atanır, spatial sorguyla
/// otomatik keşfedilmez (ADR-0005 Alternative 3 reddi: Box Safety Margin kutuyu
/// görsel alandan bilerek büyük boyutlandırır, otomatik keşif komşu odanın
/// ışığını yanlış sahiplenirdi). Intensity hedefi per-light MUTLAK değerdir
/// (Lerp(baseIntensity, memoryIntensity, p) ≡ baseIntensity × Lerp(1, M, p),
/// memoryIntensity = baseIntensity × M seçilerek — ADR şekli).
/// </summary>
[System.Serializable]
public struct ZoneLight
{
    /// <summary>Light Mode = Mixed olmalı (Baked, real-time pass'ten hariç — build-time doğrulama Story 006).</summary>
    public Light light;

    /// <summary>Sıcak amber taban rengi (Dormant).</summary>
    public Color baseColor;

    /// <summary>Soğuk mavi / sodyum-yeşil anı hedefi (tam Shifted).</summary>
    public Color memoryColor;

    /// <summary>Işığın varsayılan yoğunluğu (Dormant).</summary>
    public float baseIntensity;

    /// <summary>Tam Shifted'daki hedef yoğunluk (mutlak değer).</summary>
    public float memoryIntensity;
}
