/// <summary>
/// Işık/Volume shift lifecycle states (isik-volume-durum-sistemi.md).
/// GEÇİCİ EV (gece-oturum Story 003): Işık/Volume epic'i kendi facade'ını
/// kurarken bu enum'un sahipliğini devralır — burada minimal tanım, yalnız
/// GeceOturumDurumu handler'ının derlenebilmesi için. Değer setine yeni durum
/// eklemek o epic'in işi.
/// </summary>
public enum ShiftState
{
    Dormant,
    ShiftingIn,
    Held,
    ShiftingOut,
}
