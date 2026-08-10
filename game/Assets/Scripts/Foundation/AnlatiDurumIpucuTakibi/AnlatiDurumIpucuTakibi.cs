using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// İpucu takibi servisinin statik facade'ı (ADR-0001 deseni, ikinci tam örnek —
/// ADR-0007). Üretim kodu `AnlatiDurumIpucuTakibi.Instance`'ı okur; testler taze
/// bir `AnlatiDurumState` kurar ve bu facade'a HİÇ dokunmaz (ADR-0001 test
/// edilebilirlik deseni, manifest Required kuralı).
/// </summary>
public static class AnlatiDurumIpucuTakibi
{
    /// <summary>`ClueRegistry` asset'inin sabit Addressable anahtarı (Story 004 build-time çözülebilirliğini doğrular).</summary>
    public const string ClueRegistryAddressableKey = "ClueRegistry";

    // ASLA değiştirilmez (ADR-0015 in-place rejimi) — reset aynı instance'ın
    // alanlarını temizler.
    private static readonly AnlatiDurumState _current = new AnlatiDurumState();

    /// <summary>
    /// Işık/Volume aboneliği ve lazy-yükleme kancası, FACADE'ın static
    /// constructor'ında — süreç başına TAM BİR KEZ, ilk erişimde (GDD Core Rules
    /// "Abonelik zamanlaması": sahne-lokal `Awake`/`OnEnable` DEĞİL). İki facade de
    /// in-place reset olduğundan (ADR-0015) re-wire yok, birikme yok; reset sırası
    /// bu yüzden Işık/Volume ÖNCE (FoundationBootstrap belgeli sırası).
    ///
    /// **ADR-0007'den bilinçli sapma (LP-CODE-REVIEW, anlati Story 001)**: ADR'ın
    /// Data model bloğu aboneliği `AnlatiDurumState`'in CONSTRUCTOR'ına koyuyor.
    /// Bu kod tabanında yanlış: testler ADR-0001 deseni gereği taze `AnlatiDurumState`
    /// kurar, State-ctor aboneliği her testte kalıcı event'e bir handler daha
    /// sızdırırdı. Proje emsali `GeceOturumDurumu` de FACADE static ctor kullanıyor.
    /// ADR-0007 addendum'u bekliyor.
    /// </summary>
    static AnlatiDurumIpucuTakibi()
    {
        _current.BindEnsureRegistryLoaded(EnsureRegistryLoaded);

        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged +=
            static (shiftId, newState, _, _) => _current.ProcessShiftStateChanged(shiftId, newState);
    }

    public static IAnlatiDurumState Instance => _current;

    /// <summary>
    /// Story 002/003'ün ihtiyaç duyduğu somut-tipli erişim (ters indeks, handler).
    /// Dış tüketiciler her zaman `Instance`'ı okur.
    /// </summary>
    internal static AnlatiDurumState InternalInstance => _current;

    /// <summary>Yalnız FoundationBootstrap.ResetAll() çağırır — Işık/Volume ve Gece/Oturum reset'lerinden SONRA.</summary>
    internal static void ResetOnLoad() => _current.ResetOnLoad();

    /// <summary>
    /// YALNIZ TEST: üretim yükleyicisi. Testler `BindEnsureRegistryLoaded` ile
    /// sayaç sarmalayıcı takar; teardown'da bunu geri bağlayarak süreç ömrü
    /// boyunca yükleyicinin kopuk kalmasını önler.
    /// </summary>
    internal static System.Action RegistryLoader => EnsureRegistryLoaded;

    /// <summary>
    /// `ClueRegistry`'yi Addressables'tan LAZY yükler ve ters indeksi kurar.
    ///
    /// **OTURUM BAŞINA EN ÇOK BİR KEZ — BU ÇAĞRIYI SICAK YOLA TAŞIMAYIN.**
    /// `WaitForCompletion()` çağıran thread'i BLOKLAR. Burada kabul edilebilir
    /// çünkü yalnız ilk GERÇEK `OnShiftStateChanged(Held)` olayında tetiklenir:
    /// nadir bir oynanış olayı, per-frame DEĞİL, loading screen'de DEĞİL, tek
    /// küçük asset (ADR-0007 Risks). `IsRegistryLoaded` guard'ı bunu yapısal
    /// olarak garanti eder.
    ///
    /// Dikkat: sürecin İLK `LoadAssetAsync` çağrısı Addressables'ın
    /// `InitializeAsync`'ini (katalog yüklemesi) de zincirler — bu ilk projede
    /// gerçek Addressables tüketicisi olduğundan bloklama penceresi "tek küçük
    /// asset"ten büyüktür. ADR-0007'nin "ihmal edilebilir" ifadesi bu yüzden
    /// harfiyen alınmamalı (LP-CODE-REVIEW, Story 003).
    ///
    /// Constructor'dan ASLA çağrılmaz: `FoundationBootstrap.ResetAll()`'ın
    /// ultra-erken `SubsystemRegistration` zamanlamasında Addressables'ın hazır
    /// olup olmadığı belirsizdi; gerçek bir Held ancak fiilî oynanışta olabilir,
    /// yani boot çoktan bitmiştir (ADR-0007'nin Alternative 4'ü bu yüzden reddetti).
    ///
    /// Başarısızlıkta LATCH'lenir (boş indeks + `Debug.LogError`): bloklayan çağrı
    /// her Held'de yeniden denenmez. Story 004'ün build-blocking anahtar kontrolü
    /// bu vakayı üretimde zaten imkânsız kılar — bu yalnız savunma katmanı.
    /// </summary>
    private static void EnsureRegistryLoaded()
    {
        if (_current.IsRegistryLoaded)
        {
            return;
        }

        AsyncOperationHandle<ClueRegistry> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<ClueRegistry>(ClueRegistryAddressableKey);
            ClueRegistry registry = handle.WaitForCompletion();

            if (registry == null)
            {
                Debug.LogError(
                    $"[AnlatiDurumIpucuTakibi] '{ClueRegistryAddressableKey}' Addressable anahtarı çözüldü ama " +
                    "asset null — ipucu takibi bu oturumda devre dışı (build doğrulaması bunu engellemeliydi).");
                ReleaseFailedHandle(handle);
                _current.MarkRegistryLoadFailed();
                return;
            }

            // BAŞARI yolunda handle BİLEREK serbest bırakılmaz: yüklenen
            // ClueRegistry süreç ömrü boyunca canlı kalmalı — ters indeks onun
            // ClueDefinition referanslarını tutar ve cache oturumlar arası korunur
            // (ADR-0015). Release etmek asset'i boşaltıp indeksi yetim referanslarla
            // bırakırdı. Refcount'u ResourceManager tutar; Release ETMEMEK tam da
            // bunun çalışma biçimidir — burada tutulacak ekstra bir şey yok.
            _current.BuildReverseIndex(registry.Definitions);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"[AnlatiDurumIpucuTakibi] '{ClueRegistryAddressableKey}' yüklenemedi — ipucu takibi bu oturumda " +
                $"devre dışı (bir daha denenmeyecek). {exception}");
            ReleaseFailedHandle(handle);
            _current.MarkRegistryLoadFailed();
        }
    }

    /// <summary>
    /// BAŞARISIZ yolda handle'ı serbest bırakır: hiçbir şeyin canlı kalması
    /// gerekmiyor ve serbest bırakılmamış başarısız bir operasyon
    /// ResourceManager'da adrese göre cache'lenmiş kalır. `LoadAssetAsync`'in
    /// kendisi patlarsa `handle` `default`'tur — `IsValid()` bunu güvenle eler.
    /// </summary>
    private static void ReleaseFailedHandle(AsyncOperationHandle<ClueRegistry> handle)
    {
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }
    }
}
