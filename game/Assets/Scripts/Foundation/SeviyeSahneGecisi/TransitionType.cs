/// <summary>
/// Geçişi başlatan çağrının türü — `OnTransitionStateChanged`'in ikinci parametresi.
///
/// **Neden var (GDD N6 düzeltmesi)**: SOFT ve HARD CUT TEK bir paylaşılan durum
/// makinesinden geçtiği için (GDD AC-2, "ayrı kod yolu yok"), `type` olmadan
/// `Swapping` event'i iki geçiş türü için ayırt EDİLEMEZDİ — Adaptif Ses
/// Sistemi'nin HARD CUT sting'i sıradan bir asansör SOFT geçişinde de çalardı,
/// bu da Pillar 2 (Sessiz Gerilim, Şok Değil) ihlali riski taşıyordu.
///
/// `type` yalnız dinleyicilere bilgi ekler; durum makinesinin kendisi paylaşılan
/// ve tek kalır — AC-2'nin garantisini BOZMAZ.
/// </summary>
public enum TransitionType
{
    Soft,
    Hard,
}
