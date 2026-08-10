using System;
using System.Collections.Generic;

/// <summary>
/// Anlatı Durum/İpucu Takibi'nin dışa açtığı sözleşme (ADR-0007 Data model —
/// birebir). Oyuncunun hikâye içinde "bildiği" şeylerin kaydı; Gece/Oturum
/// Durumu'nun tuttuğu "hangi tetikleyici ateşlendi" ham bookkeeping'inden
/// AYRI ve bağımsızdır (GDD Overview, sistem sınırı notu) — bu iki servis
/// birbirine sorgu atmaz, alan paylaşmaz.
///
/// **Yüzey KAPALI ve bilinçli olarak dardır**: hiçbir üye sıralama ya da zaman
/// damgası verisi açmaz (GDD AC7, TR-anlati-007). Gerekçe Pillar 1 ("objektif
/// gerçeklik yok") ve Pillar 5 ("sıra değil, birikim") — tüketiciler ipuçları
/// arasında sıra çıkarımı YAPMAMALIDIR. Bu kısıt yapısal olarak burada
/// zorlanır; tüketici davranışı ise Dependencies sözleşme notudur.
///
/// `GetKnownClueIds()` dönüş tipi `IReadOnlyCollection&lt;string&gt;`'dir,
/// `IReadOnlySet&lt;string&gt;` DEĞİL: ikincisi .NET 5+ tipidir, Unity'nin
/// desteklenen Api Compatibility profillerinde garanti değildir ve GDD'nin kendi
/// imzasından sapar (ADR-0006'da bir kez yakalandı, ADR-0007 proaktif düzeltti;
/// control manifest bunu adlandırılmış YASAK desen olarak listeliyor).
/// </summary>
public interface IAnlatiDurumState
{
    /// <summary>Bilinmeyen ya da hiç tanımlanmamış bir clueId için `false` döner — istisna fırlatmaz (GDD AC6).</summary>
    bool IsClueKnown(string clueId);

    /// <summary>
    /// Hiçbir ipucu bilinmiyorken BOŞ ve non-null bir koleksiyon döner (GDD AC5).
    ///
    /// **Enumerasyon sırası ANLAM TAŞIMAZ** — `HashSet` sırası fiilen ekleme-sıralı
    /// görünebilir ama bu bir sözleşme DEĞİLDİR; tüketiciler ipuçları arasında sıra
    /// çıkarımı yapmamalıdır (GDD AC7 / Pillar 1 ve 5). Bu, tip sistemiyle
    /// zorlanamayan tek yarım — kalan garanti code review'ın işidir.
    ///
    /// Dönen koleksiyon CANLIDIR: sonradan işaretlenen ipuçları görünür, reset onu
    /// da boşaltır. Gezerken `MarkClueKnown` çağırmak `InvalidOperationException`
    /// üretir — türetilmiş ipucu işaretleyecek tüketici önce kendi kopyasını alsın.
    /// </summary>
    IReadOnlyCollection<string> GetKnownClueIds();

    /// <summary>
    /// İpucunu doğrudan bilindi işaretler. Zaten biliniyorsa sessiz no-op —
    /// `OnClueKnown` ikinci kez fırlamaz (GDD AC3). `requiredShiftIds` ön
    /// koşuluna karşı DOĞRULAMA YAPMAZ (GDD AC9, KASITLI: gelecekte bir diyalog
    /// seçimi bir ipucunu doğrudan açığa çıkarabilir).
    ///
    /// null/boş/whitespace `clueId` de sessiz no-op'tur — koleksiyona null eleman
    /// girmesini engeller (yazılmamış bir `ClueDefinition` alanına karşı guard).
    /// </summary>
    void MarkClueKnown(string clueId);

    /// <summary>Bir clueId için `Unknown→Known` geçişinde TAM OLARAK bir kez fırlar.</summary>
    event Action<string> OnClueKnown;
}
