/// <summary>
/// Işık/Volume shift lifecycle states (isik-volume-durum-sistemi.md, ADR-0005).
/// Sahiplik bu sistemde (isik-volume Story 001'de geçici Foundation-kökü evinden
/// devralındı; gece-oturum Story 003'ün handler'ı aynı global tipe derlenmeye
/// devam eder). Normal yaşam döngüsü: Dormant → ShiftingIn → Held → ShiftingOut
/// → Dormant; beşinci çıkış yolu OnDestroy tamamlama garantisidir (ADR-0005).
/// </summary>
public enum ShiftState
{
    Dormant,
    ShiftingIn,
    Held,
    ShiftingOut,
}
