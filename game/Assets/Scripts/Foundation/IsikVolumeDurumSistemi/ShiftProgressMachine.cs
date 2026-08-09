using UnityEngine;

/// <summary>
/// Shift geçişinin saf ilerleme çekirdeği (ADR-0005; GDD Formulas "Durum takibi
/// notu"). KOŞAN DURUM HER ZAMAN x'tir — asla ElapsedTime ya da ShiftProgress
/// değil: 3x²−2x³'ün basit cebirsel tersi yok, saklanan bir progress geri
/// çözülemez. Interrupt = per-tick delta işaretinin flip'i; x mevcut değerinden
/// devam eder, hiçbir yolda sıfırlanmaz (süreklilik garantisi — pop yok).
/// MonoBehaviour'sız, [Test]-edilebilir; Story 003'ün ticker'ı bunu sürer.
/// </summary>
public sealed class ShiftProgressMachine
{
    // +1 = Shifting-In, -1 = Shifting-Out. Başlangıç Dormant eşdeğeri: x=0, yön out.
    private float _direction = -1f;

    /// <summary>Normalize zaman kesri, 0-1 clamp'li — makinenin TEK koşan durumu.</summary>
    public float X { get; private set; }

    /// <summary>Shifting-In yönünde mi ilerliyor (Story 003'ün durum eşlemesi için).</summary>
    public bool IsShiftingIn => _direction > 0f;

    /// <summary>
    /// Smoothstep-eased blend faktörü — HER çağrıda mevcut x'ten taze hesaplanır,
    /// asla saklanmaz (TR-isik-003). Volume.weight ve ışık lerp'i aynı değeri
    /// paylaşır (lockstep).
    /// </summary>
    public float ShiftProgress => IsikVolumeFormulas.SmoothStep(X);

    /// <summary>
    /// In yönüne geçir (TriggerShift / Shifting-Out'ta interrupt). x'e DOKUNMAZ —
    /// mevcut değerden devam (pop'suz süreklilik).
    /// </summary>
    public void BeginShiftIn() => _direction = 1f;

    /// <summary>
    /// Out yönüne geçir (RevertShift / Shifting-In'de interrupt). x'e DOKUNMAZ.
    /// </summary>
    public void BeginShiftOut() => _direction = -1f;

    /// <summary>
    /// Bir kare ilerlet: x += yön × dt / Duration, 0-1 clamp. Duration guard'lı
    /// tüketilir (TIME_EPSILON tabanı — sıfıra bölünme yapısal olarak imkânsız).
    /// Allocation'sız.
    /// </summary>
    public void Tick(float deltaTime, float duration)
    {
        X = Mathf.Clamp01(X + _direction * (deltaTime / IsikVolumeFormulas.ClampDuration(duration)));
    }

    /// <summary>
    /// Reload-restore İSTİSNASI (Story 005): tam-Shifted duruma tek adımda kur
    /// (x=1, yön in). Shifting-In rampası bilinçli atlanır — normal akışta ASLA
    /// çağrılmaz; tek çağıran ShiftZone'un OnEnable Persistent restore yolu.
    /// </summary>
    internal void RestoreShifted()
    {
        X = 1f;
        _direction = 1f;
    }
}
