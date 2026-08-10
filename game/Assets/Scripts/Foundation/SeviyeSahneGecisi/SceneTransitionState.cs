using System;

/// <summary>
/// Sahne geçişinin SAF C# çekirdeği — hiçbir `UnityEngine` tipine bağlanmaz ve
/// düz bir `[Test]` içinde `new SceneTransitionState()` ile kurulabilir.
///
/// **Çekirdek KARAR VERİR, iş YAPMAZ.** `SceneManager` çağrıları, coroutine'ler,
/// `Awake`, zamanlama — hepsi `SceneTransitionManager` sürücüsünün işidir.
/// Hakemlik metotları (Story 006) sürücüye ne yapacağını söyleyen bir TALİMAT
/// döner (`ElevatorStateMachine.TryCall()` şekli), işi kendileri yapmaz.
///
/// **AYRIM KARARI (kullanıcı, 2026-08-10) — ADR-0008 Data model'de yerinde
/// düzeltildi**: ADR'ın ilk taslağı bu alanları doğrudan `MonoBehaviour`'a
/// koyuyordu. Manifest'in "saf çekirdek + ince sürücü (BLOCKING)" kuralı bölüm
/// bazında *Core Layer* altında olduğu ve sahne geçişleri *Foundation Layer*'a
/// ait olduğu için bu bağlayıcı bir ihlal DEĞİLDİ — bilinçli bir tercihti.
/// Sevk edilmiş emsaller aynı yönü gösteriyor (`ShiftZone` saf bir
/// `ShiftProgressMachine` tutuyor; ADR-0011 `ElevatorController`/
/// `ElevatorStateMachine` ayrımını yapıyor) ve ayrım, ADR-0008'in kendi
/// Validation Criteria'sının çoğunu `AddComponent` PlayMode testi olmaktan
/// çıkarıp düz EditMode `[Test]`'e indiriyor.
///
/// **Bu, ADR-0001'in statik servis desenini GERİ GETİRMEZ**: bu tip sürücünün düz
/// bir alanıdır (`ShiftZone` şekli), `ResetOnLoad()`'lı bir statik facade DEĞİL
/// (`ElevatorSystem` şekli). Sistem `FoundationBootstrap.ResetAll()`'a kayıtlı
/// değildir ve ADR-0001'in belgelenmiş tek istisnası olmaya devam eder.
/// </summary>
internal sealed class SceneTransitionState
{
    /// <summary>Herkese açık, dışa-görünür TEK alan — `OnTransitionStateChanged`'i yalnız bu fırlatır.</summary>
    public TransitionState CurrentState { get; private set; } = TransitionState.Idle;

    private TransitionType _activeType;

    // ── Story 005/006'nın dolduracağı alanlar ──
    //
    // Şimdiden BEYAN EDİLİYORLAR çünkü `CurrentState`'ten AYRI oldukları
    // sözleşmesi bu sistemin en yük taşıyan tasarım kararı: bir HARD CUT
    // preload'u, aktif bir SOFT geçiş sürerken arka planda ilerleyebilmeli
    // (GDD Edge Cases). `CurrentState`'e yanlışlıkla yazan bir kopyala-yapıştır
    // hatası, ayrımı baştan görünür kılmazsak çok geç yakalanır.
    // Açık başlatıcılar: Story 005'e kadar bu alanlara YAZAN yok, ve açık
    // başlatıcı olmadan derleyici CS0649 ("hiç atanmıyor") uyarısı basar.
    private TransitionState _hardCutPreloadState = TransitionState.Idle;
    private string _hardCutPreloadScene = null;
    private HardCutConfig _hardCutPreloadConfig = default;

    // AKTİF HARD CUT'ın config'i — preload edilmişinkinden AYRI (LP-CODE-REVIEW
    // bulgusu). `GetCurrentHardCutAbrupt()` GDD'ye göre "aktif YA DA preload
    // edilmiş" bir HARD CUT'a hizmet etmeli. Yalnız preload alanı olsaydı,
    // GDD AC-2'nin senkron-bekleme fallback'i (hiç preload edilmemiş
    // `RequestHardCut`) config'ini park edecek yer bulamaz, sorgu `default`
    // yani `false` dönerdi ve Adaptif Ses YANLIŞ bitiş tonunu çalardı.
    private HardCutConfig _activeHardCutConfig = default;

    // Tek slotluk "bekleyen" kuyruk: aktif bir SOFT sırasında istenen HARD CUT
    // buraya alınır ve `Idle`'a ulaşıldığı an otomatik ateşlenir. Çok öğeli bir
    // kuyruk DEĞİL — GDD AC-6.
    private (string toScene, HardCutConfig config, Action onComplete, Action<string> onFailed)? _pendingHardCut = null;

    public event Action<TransitionState, TransitionType> OnTransitionStateChanged;

    public event Action<string> OnSoftTransitionRejected;

    /// <summary>Geçişi başlatan çağrının türü — ret gerekçesi bundan türetilir (Story 006).</summary>
    internal TransitionType ActiveType => _activeType;

    /// <summary>HARD CUT preload'unun `CurrentState`'ten BAĞIMSIZ ilerlemesi (Story 005 doldurur).</summary>
    internal TransitionState HardCutPreloadState => _hardCutPreloadState;

    /// <summary>Preload'un hedef sahnesi — `RequestHardCut`'ın fast-path eşleşmesi buna bakar (Story 005).</summary>
    internal string HardCutPreloadScene => _hardCutPreloadScene;

    /// <summary>Preload edilmiş HARD CUT'ın taşınan ayarları (Story 007'nin `GetCurrentHardCutAbrupt`'ı okur).</summary>
    internal HardCutConfig HardCutPreloadConfig => _hardCutPreloadConfig;

    /// <summary>AKTİF HARD CUT'ın ayarları — senkron-bekleme fallback'i buraya park eder (Story 005/007).</summary>
    internal HardCutConfig ActiveHardCutConfig => _activeHardCutConfig;

    /// <summary>Bekleyen slot dolu mu — tek slot olduğu için sayı değil bool (GDD AC-6).</summary>
    internal bool HasPendingHardCut => _pendingHardCut != null;

    /// <summary>
    /// Durumu yazar ve `OnTransitionStateChanged`'i TAM BİR KEZ fırlatır.
    ///
    /// İdempotent DEĞİLDİR: aynı duruma ikinci kez `SetState` çağrısı event'i
    /// yine fırlatır. Bu bilinçli — bastırma, bir sürücü hatasını (aynı adımı iki
    /// kez yayınlamak) sessizce gizlerdi ve dizi testleri onu yakalayamazdı.
    /// </summary>
    internal void SetState(TransitionState newState, TransitionType type)
    {
        CurrentState = newState;
        _activeType = type;
        OnTransitionStateChanged?.Invoke(newState, type);
    }

    /// <summary>
    /// Başarısızlık yolu: `Failed` yayınlanır → çağıranın `onFailed`'i çalışır →
    /// OTOMATİK `Idle`'a dönülür (GDD AC-11a).
    ///
    /// **Sıra önemlidir.** `onFailed` içinde durumu örnekleyen bir çağıran `Failed`
    /// görmeli, ama callback'ten hemen sonra gönderdiği yeni istek normal kabul
    /// edilmelidir. `Failed` "BU istek başarısız oldu" demektir; "yönetici kalıcı
    /// olarak bozuk" DEĞİL. Bu dönüş belgelenmemiş olsaydı tek bir bozuk sahne
    /// referansı oturumun geri kalanındaki her geçişi soft-lock'lardı.
    /// </summary>
    /// <param name="type">Başarısız olan geçişin türü.</param>
    /// <param name="invokeOnFailed">
    /// `Failed` ile `Idle` ARASINDA çalıştırılacak iş. Story 007 buraya
    /// sürücünün `SafeInvoke(onFailed, reason)` çağrısını geçirecek.
    /// </param>
    internal void Fail(TransitionType type, Action invokeOnFailed = null)
    {
        SetState(TransitionState.Failed, type);

        // `finally`: hook FIRLATIRSA bile `Idle`'a dönülür. İstisnayı YUTMAYIZ
        // (yakalama ve loglama Story 007'nin `SafeInvoke`'unun işi, ve o sürücüde
        // yaşar) — burada yalnız dönüşü KOŞULSUZ kılıyoruz. Sarmalanmasaydı,
        // fırlatan bir callback `Idle`'ı atlatır ve durum makinesini kalıcı
        // olarak `Failed`'de bırakırdı: GDD'nin "üretim durdurucu boşluk" dediği
        // ve bu story'nin var oluş sebebi olan soft-lock'un ta kendisi.
        // Pencere Story 003-006: sürücü, `SafeInvoke` daha yokken ham bir
        // `onFailed` geçebilir (LP-CODE-REVIEW bulgusu).
        try
        {
            invokeOnFailed?.Invoke();
        }
        finally
        {
            SetState(TransitionState.Idle, type);
        }
    }
}
