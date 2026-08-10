/// <summary>
/// İpucu takibi servisinin statik facade'ı (ADR-0001 deseni, ikinci tam örnek —
/// ADR-0007). Üretim kodu `AnlatiDurumIpucuTakibi.Instance`'ı okur; testler taze
/// bir `AnlatiDurumState` kurar ve bu facade'a HİÇ dokunmaz (ADR-0001 test
/// edilebilirlik deseni, manifest Required kuralı).
/// </summary>
public static class AnlatiDurumIpucuTakibi
{
    // ASLA değiştirilmez (ADR-0015 in-place rejimi) — reset aynı instance'ın
    // alanlarını temizler. Story 003 buraya Işık/Volume aboneliğini ekleyecek
    // (gece-oturum facade'ının static constructor deseniyle aynı): süreç başına
    // bir kez bağlanır, her ResetAll()'ı hayatta geçirir, re-wire yok.
    private static readonly AnlatiDurumState _current = new AnlatiDurumState();

    public static IAnlatiDurumState Instance => _current;

    /// <summary>
    /// Story 002/003'ün ihtiyaç duyduğu somut-tipli erişim (ters indeks enjeksiyonu,
    /// Held handler'ı). Dış tüketiciler her zaman `Instance`'ı okur.
    /// </summary>
    internal static AnlatiDurumState InternalInstance => _current;

    /// <summary>Yalnız FoundationBootstrap.ResetAll() çağırır — Işık/Volume ve Gece/Oturum reset'lerinden SONRA.</summary>
    internal static void ResetOnLoad() => _current.ResetOnLoad();
}
