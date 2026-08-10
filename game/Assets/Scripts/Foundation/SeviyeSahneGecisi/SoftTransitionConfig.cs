/// <summary>
/// SOFT geçişin çağrı-başına ayarları (asansör kat değişimi).
///
/// **`MinimumDurationSeconds` bir TAMAMLANMA KAPISI DEĞİLDİR** — GDD Core Rules
/// bunu özellikle netleştiriyor: `Preloading→Ready` geçişi HER ZAMAN gerçek
/// `LoadSceneAsync` tamamlanmasını bekler. Bu değer bir TABAN/pacing knob'udur
/// (güvenli aralık 2-8s): asansör yolculuğunun oyuncuya çok ani gelmemesi için.
/// Çok düşük ayarlanırsa risk bir YARIŞ DURUMU değil, hissedilen tempo sorunudur —
/// yükleme bitmeden `Ready`'ye geçilen bir durum YAPISAL OLARAK imkânsızdır.
///
/// Bu ayrım GDD'de önceden açık değildi ve Asansör'ün kendi `onComplete`'e kadar
/// bekleyen sırası bu garantiye güveniyor.
/// </summary>
public readonly struct SoftTransitionConfig
{
    public SoftTransitionConfig(float minimumDurationSeconds)
    {
        MinimumDurationSeconds = minimumDurationSeconds;
    }

    /// <summary>Taban/pacing süresi (2-8s). `Ready`'ye geçişi ASLA tetiklemez ve ASLA geciktirmez-diye-beklenmez.</summary>
    public float MinimumDurationSeconds { get; }
}
