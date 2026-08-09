/// <summary>
/// Bir shift bölgesinin tetiklenme modu (TR-isik-009, ADR-0005 Key Interfaces).
/// Automatic: bölge Dormant'ken sürekli pozisyon-izler, R_trigger girişinde
/// kendi kendini tetikler. ManualOnly: yalnız dışarıdan TriggerShift ile başlar,
/// Dormant'ken gerçek sıfır maliyet.
/// </summary>
public enum TriggerMode
{
    Automatic,
    ManualOnly,
}
