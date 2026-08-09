/// <summary>
/// Routing tablosunun hedef tipi — ShiftZone'un (Story 003) facade'a görünen dar
/// yüzü. Story 001'de gerçek ShiftZone henüz yokken tablonun derlenebilmesi için
/// internal arayüz (story Engine Notes tercihi): Story 003 ShiftZone'u buna
/// implement eder, facade değişmez; testler sahte handle enjekte eder. Per-zone
/// durum (aktiflik, kalıcılık, radius) zone'da kalır (ADR-0005 addendum) — facade
/// yalnız delege eder, kopyalamaz.
/// </summary>
internal interface IShiftZoneHandle
{
    /// <summary>Bölgenin açık eşleştirme anahtarı (TR-isik-015) — kayıt bu id ile yapılır.</summary>
    string ShiftId { get; }

    /// <summary>
    /// Yeni geçiş başlattıysa true; Shifting-In/Held'de false (no-op, config
    /// değişmez — GDD AC6). Shifting-Out aktif AMA true dalıdır: mevcut x'ten
    /// yön-flip (AC7).
    /// </summary>
    bool TriggerShift(ShiftConfig config);

    /// <summary>Dormant'ken sessiz no-op; aktifken yönü tersine çevirir.</summary>
    void RevertShift();

    bool IsShiftActive { get; }

    /// <summary>Bölgeyi son tetikleyen config'in Persistent bayrağı.</summary>
    bool IsShiftPersistent { get; }

    /// <summary>Bölgeyi son tetikleyen config'in StingerAudioRadius değeri.</summary>
    float StingerAudioRadius { get; }
}
