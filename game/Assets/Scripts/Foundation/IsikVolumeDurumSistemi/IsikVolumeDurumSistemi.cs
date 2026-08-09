/// <summary>
/// Işık/Volume Durum Sistemi'nin statik facade'ı (ADR-0001 deseni, ADR-0005
/// addendum'daki birebir şekil). Üretim kodu Instance'ı kullanım noktasında
/// canlı okur; testler taze IsikVolumeState kurup enjekte eder, bu facade'a
/// asla dokunmaz.
/// </summary>
public static class IsikVolumeDurumSistemi
{
    // Asla değiştirilmez (ADR-0015 in-place rejimi) — reset aynı instance'ın
    // alanlarını temizler; OnShiftStateChanged aboneleri process başına bir kez
    // bağlanır ve her ResetAll'ı atlatır.
    private static readonly IsikVolumeState _current = new IsikVolumeState();

    public static IIsikVolumeState Instance => _current;

    /// <summary>
    /// Internal-only, somut-tipli erişim — ShiftZone'un RegisterZone/DeregisterZone
    /// (OnEnable/OnDisable) ve RaiseShiftStateChanged (TEK raise yolu) çağrıları
    /// için; ADR-0006/0009/0012'nin yerleşik InternalInstance escape-hatch şekli.
    /// </summary>
    internal static IsikVolumeState InternalInstance => _current;

    /// <summary>Yalnız FoundationBootstrap.ResetAll() çağırır — GeceOturumDurumu'ndan ÖNCE, in place.</summary>
    internal static void ResetOnLoad() => _current.ResetOnLoad();
}
