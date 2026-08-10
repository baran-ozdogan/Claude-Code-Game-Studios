/// <summary>
/// HARD CUT'ın çağrı-başına ayarları (psikiyatri kesmesi).
///
/// **`Abrupt` yalnız TAŞINIR, YORUMLANMAZ** (TR-sahne-gecisi-012). Sahne Kesmeli
/// Anlatı'nın iki bitiş koşulu (görev tamamlama vs. anı-tetikleyici doygunluğu)
/// farklı bir ses/görsel tonu talep ediyor; bu sistem o tonu bilmez, yalnız
/// bayrağı taşır ve `GetCurrentHardCutAbrupt()` ile sorgulanabilir kılar.
///
/// Sıfır-kare swap mekanizması `Abrupt` değerinden BAĞIMSIZ olarak değişmeden
/// kalır — GDD AC-2'nin "ayrı kod yolu yok" garantisi bu alanla da bozulmaz,
/// tıpkı `TransitionType` gibi.
/// </summary>
public readonly struct HardCutConfig
{
    public HardCutConfig(bool abrupt)
    {
        Abrupt = abrupt;
    }

    /// <summary>Adaptif Ses'in tek tüketicisi olduğu dar bir sinyal. Bu sistem değerine göre ASLA dallanmaz.</summary>
    public bool Abrupt { get; }
}
