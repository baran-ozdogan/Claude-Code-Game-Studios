using System;

/// <summary>
/// Seviye/Sahne Geçişi'nin dışa açılan arayüzü — GDD "Interactions with Other
/// Systems / Dışa açılan arayüz" bölümüyle BİREBİR (ADR-0008 Requirements bunu
/// verbatim eşleşme olarak şart koşuyor).
///
/// Tüketiciler: Asansör/Kat-Erişim (`RequestSoftTransition`), Sahne Kesmeli
/// Anlatı (`PreloadHardCut`/`RequestHardCut`), Adaptif Ses Sistemi (yalnız
/// `OnTransitionStateChanged` + `GetCurrentHardCutAbrupt`).
///
/// **KİLİT SAHİPLİĞİ BU SİSTEMDE DEĞİLDİR** (TR-sahne-gecisi-014): çağıran
/// `RequestMovementLock`/`ReleaseMovementLock`'u kendi bağlamına göre çağırır.
/// Bu sistem kilidin varlığından habersizdir; yalnız `onComplete`/`onFailed`
/// callback'lerinden TAM OLARAK BİRİNİN çağrılacağını garanti eder.
///
/// **ABONELİK LAZY OLMALIDIR** (ADR-0008 mandated constraint, manifest'te kayıtlı
/// forbidden pattern): `SceneTransitionManager.Instance` bir sahnenin `Awake()`'i
/// tarafından set edilir, yani `FoundationBootstrap.ResetAll()`'ın
/// `SubsystemRegistration` zamanında HENÜZ YOKTUR. Hiçbir servis bu event'lere
/// kendi constructor'ında abone olamaz — `OnEnable`/`Start` ya da ilk kullanımda.
/// </summary>
public interface ISceneTransitionManager
{
    /// <summary>
    /// Herkese açık geçiş durumu — SALT OKUNUR. `PreloadHardCut`'ın kendi arka
    /// plan ilerlemesi bunu ASLA değiştirmez (GDD Edge Cases): dışa-görünür tek
    /// alan budur ve `OnTransitionStateChanged`'i yalnız bu fırlatır.
    /// </summary>
    TransitionState CurrentState { get; }

    /// <summary>
    /// Asansör kat değişimi: hedef sahne additive yüklenir, iki sahne kısa süre
    /// eş-zamanlı resident kalır, oyuncu hedefin `SoftTransitionAnchor`'ına
    /// kabin-YEREL pozunu koruyarak taşınır. Kamera hiç kesilmez.
    ///
    /// **`fromScene` `null` OLABİLİR** ve bu meşrudur: ADR-0015 boot'ta
    /// `RequestSoftTransition(null, ...)` çağırıyor — o anda kaynak anchor
    /// YOKTUR, kopyalama atlanır ve başlangıç yerleşimi çağıranın
    /// `InitialSpawnAnchor`'ının işidir.
    ///
    /// `Idle` değilken çağrılırsa REDDEDİLİR ve kuyruğa ALINMAZ — `onComplete`
    /// da `onFailed` de çağrılmaz, yalnız `OnSoftTransitionRejected` fırlar.
    /// </summary>
    void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config,
        Action onComplete, Action<string> onFailed);

    /// <summary>
    /// Psikiyatri kesmesi: aynı karede aktif sahne değişir — gerçek sıfır fade,
    /// 0 kare siyah ara. Bu `toScene` için `Ready` bekleyen bir preload varsa
    /// yeniden yükleme YAPILMADAN doğrudan swap edilir; yoksa senkron-bekleme
    /// fallback'i aynı paylaşılan diziden geçer (GDD AC-2).
    ///
    /// Aktif bir SOFT geçiş sırasında çağrılırsa REDDEDİLMEZ — tek slotluk
    /// kuyruğa alınır ve `Idle`'a ulaşıldığı an otomatik ateşlenir. Anlatısal
    /// bir anın sessizce kaybolması, birkaç saniyelik gecikmeden çok daha
    /// kötüdür (GDD'nin kasıtlı asimetrisi).
    /// </summary>
    void RequestHardCut(string toScene, HardCutConfig config,
        Action onComplete, Action<string> onFailed);

    /// <summary>
    /// Arka planda additive yükler ve `Ready`'de bekletir. Bir preload zaten
    /// in-flight ise no-op (GDD AC-8); AYNI sahne için zaten `Ready`'de bekleyen
    /// bir preload varsa da no-op (ADR-0015 netleştirmesi — onun ikili preload
    /// eşikleri `Ready` sonrası yeniden çağırabiliyor).
    /// </summary>
    void PreloadHardCut(string toScene);

    /// <summary>
    /// Aktif ya da preload edilmiş bir HARD CUT'ın `Abrupt` değeri. Hiçbir HARD
    /// CUT preload edilmemiş/aktif değilse sonuç TANIMSIZDIR — GDD'nin kendi
    /// sözleşmesi. Çağıran bunu YALNIZ `OnTransitionStateChanged(Swapping,
    /// TransitionType.Hard)` aldığı karede sorgulamalıdır.
    /// </summary>
    bool GetCurrentHardCutAbrupt();

    /// <summary>
    /// Her herkese açık durum değişiminde fırlar. `type`, çağrının
    /// `RequestSoftTransition` mi `RequestHardCut` mı olduğunu taşır — durum
    /// makinesi paylaşılan/tek kalırken dinleyicilere ayırt etme imkânı verir.
    ///
    /// Adaptif Ses YALNIZ `newState == Swapping && type == Hard` iken HARD CUT
    /// sting'ini tetikler; `type` olmasaydı sting sıradan asansör geçişlerinde
    /// de çalar ve Pillar 2'yi (Sessiz Gerilim, Şok Değil) ihlal ederdi.
    /// </summary>
    event Action<TransitionState, TransitionType> OnTransitionStateChanged;

    /// <summary>
    /// Bir `RequestSoftTransition` reddedildiği **HER** durumda fırlar — aktif bir
    /// HARD CUT yüzünden ya da zaten aktif başka bir SOFT yüzünden. `reason`
    /// hangisi olduğunu ayırt eder (`"AlreadyTransitioningSoft"` /
    /// `"HardCutActive"`) ama event'in fırlaması bu ayrıma ASLA bağlı değildir
    /// (GDD Edge Cases "Kapsam genişletmesi").
    ///
    /// Asansör bunu diegetik bir tepkisizlik göstergesiyle eşler — aksi hâlde
    /// oyuncu düğmeye basar, hiçbir tepki almaz ve bu "bozuk girdi" gibi okunur.
    /// </summary>
    event Action<string> OnSoftTransitionRejected;
}
