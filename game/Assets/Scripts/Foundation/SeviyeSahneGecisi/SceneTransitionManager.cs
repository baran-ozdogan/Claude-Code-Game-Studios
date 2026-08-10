using System;
using UnityEngine;

/// <summary>
/// Sahne geçişinin İNCE SÜRÜCÜSÜ — `MonoBehaviour`, kalıcı "Foundation"
/// sahnesinde tek bir `GameObject` üzerinde yaşar (ADR-0008 Execution context).
///
/// **ADR-0001'in belgelenmiş TEK istisnası.** Diğer beş Foundation servisi düz
/// statik servistir; bu sistem `MonoBehaviour` çünkü 0.5-2s ertelenmiş unload
/// GERÇEK bir zamanlı gecikme gerektiriyor ve `Coroutine` bu projenin en
/// kanıtlanmış mekanizması (ADR-0005'in per-zone ticker emsali). Alternatifler
/// —`Awaitable` (projenin engine-reference'ında hiç belgelenmemiş) ve
/// `Task.Delay` (Editor Play-mode Stop'tan kopuk çalışır)— kullanıcı kararıyla
/// reddedildi.
///
/// **`FoundationBootstrap.ResetAll()`'a KAYITLI DEĞİLDİR** ve `ResetOnLoad()`'ı
/// YOKTUR: yaşam döngüsü kendi kalıcı sahnesinin `Awake()`'iyle sıfırlanır,
/// tıpkı UI (ADR-0002) ve Player (ADR-0003) sahneleri gibi — o ikisi de
/// `ResetAll()`'da değil. ADR-0008 bunu keşfedince ADR-0001'i "altı servis"ten
/// "beş servis"e düzeltti.
///
/// **KARAR VERMEZ, İŞ YAPAR.** Altı durumlu makine, hakemlik ve bekleyen slot
/// saf C# `SceneTransitionState`'te (Story 001). Bu sınıf yalnız `Awake` guard'ı,
/// event forward'ı, coroutine'ler ve `SceneManager` çağrılarını sahiplenir.
///
/// **ABONELER LAZY OLMAK ZORUNDA** (manifest'te kayıtlı forbidden pattern):
/// `Instance` bir sahnenin `Awake()`'i tarafından set edilir, yani
/// `FoundationBootstrap.ResetAll()`'ın `SubsystemRegistration` zamanında HENÜZ
/// YOKTUR — sıralamadan bağımsız olarak, çünkü onu `FoundationBootstrap` değil
/// sahne kuruyor. Hiçbir servis bu event'lere constructor'ında abone olamaz.
/// </summary>
public sealed class SceneTransitionManager : MonoBehaviour, ISceneTransitionManager
{
    private static SceneTransitionManager _instance;

    // DÜZ ALAN — statik facade DEĞİL (`ShiftZone`'un `ShiftProgressMachine`'i
    // şekli). `ResetOnLoad()`'lı bir facade'a sarmak, ADR-0008'in doğru şekilde
    // kapattığı ADR-0001 sorusunu yeniden açardı.
    private readonly SceneTransitionState _state = new SceneTransitionState();

    /// <summary>
    /// Statik facade — diğer Foundation servisleriyle aynı çağrı konvansiyonu
    /// (`X.Instance.Method()`), implementasyon `MonoBehaviour` destekli olsa bile.
    /// Tip somut sınıf değil ARAYÜZDÜR: tüketiciler sözleşmeye derlenir.
    ///
    /// **TÜKETİCİLER NULL KONTROLÜ YAPMAK ZORUNDA.** İki ayrı sebep:
    /// (1) `Instance`, Foundation sahnesi yüklenene kadar null'dur — abonelikler
    /// bu yüzden lazy olmalı (kayıtlı forbidden pattern);
    /// (2) Foundation sahnesi unload edilince `OnDestroy` `_instance`'ı GERÇEK
    /// null yapar. Play-mode stop'ta sahne yok etme sırası belirsizdir, yani
    /// `OnDisable` içinde `Instance.OnTransitionStateChanged -= ...` yazan bir
    /// tüketici (ADR-0009'un `AdaptifSesController`'ı, ADR-0012'nin
    /// `DialogueSceneController`'ı) Foundation önce giderse
    /// `NullReferenceException` alır. Kalıp: `if (SceneTransitionManager.Instance
    /// != null) { ... -= ... }`.
    /// </summary>
    public static ISceneTransitionManager Instance => _instance;

    /// <inheritdoc />
    public TransitionState CurrentState => _state.CurrentState;

    /// <summary>Çekirdeğin event'ini forward eder — aboneler `_state`'i hiç görmez.</summary>
    public event Action<TransitionState, TransitionType> OnTransitionStateChanged
    {
        add => _state.OnTransitionStateChanged += value;
        remove => _state.OnTransitionStateChanged -= value;
    }

    public event Action<string> OnSoftTransitionRejected
    {
        add => _state.OnSoftTransitionRejected += value;
        remove => _state.OnSoftTransitionRejected -= value;
    }

    /// <summary>
    /// Çekirdeğe doğrudan erişim — `IsikVolumeDurumSistemi.InternalInstance`
    /// ailesiyle aynı kaçış kapısı deseni.
    ///
    /// **TEK ÇAĞIRAN: testler.** Bunu derleyici ZORLAMAZ — tüm oyun kodu tek bir
    /// `Foundation.asmdef` içinde yaşıyor, yani `internal` bir bariyer değil;
    /// garanti konvansiyon + bu doc + code review'dır. Üretim kodu geçişleri
    /// ARAYÜZ üzerinden sürmeli, çekirdeği doğrudan sürerek sürücüyü (coroutine,
    /// SceneManager, ertelenmiş unload) atlatarak DEĞİL.
    ///
    /// *(ADR-0001'in kendi test deseni bunun tersini söyler — testler taze bir
    /// state kurup ENJEKTE eder, facade'a hiç uzanmaz. Burada o mümkün değil:
    /// çekirdek sürücünün `readonly` alanı ve sürücü sahneden geliyor.)*
    /// </summary>
    internal SceneTransitionState InternalStateForTests => _state;

    private void Awake()
    {
        // Duplicate-instance guard — KOŞULSUZ `Debug.LogError`, `Debug.Assert`
        // DEĞİL: `Debug.Assert` `[Conditional("UNITY_ASSERTIONS")]` taşır ve
        // shipping build'de tamamen derlenmez, yani sıfır koruma verirdi
        // (ADR-0003'ün unity-specialist BLOCKING düzeltmesi, aynı şekil).
        if (_instance != null)
        {
            Debug.LogError("Duplicate SceneTransitionManager — destroying this instance.", this);
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        // Yalnız GERÇEKTEN aktif instance isek temizle: duplicate guard'ın yok
        // ettiği ikinci instance buraya da uğrar ve bu kontrol olmadan
        // hayattaki instance'ın `Instance`'ını sıfırlardı.
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // ── Henüz uygulanmamış geçiş yüzeyi (Story 003 ve 005 dolduracak) ──
    //
    // Bu üçü SESSİZ no-op DEĞİL: her biri uyarı basar VE `onFailed`'i çağırır.
    // `onFailed` çağrısı kritik — arayüzün sözleşmesi "onComplete/onFailed'den
    // TAM OLARAK BİRİ çağrılır" diyor ve ADR-0011/0015'in çağıran deseni
    // "önce kilitle, iki callback'te de bırak". Sessizce dönen bir stub,
    // oyuncuyu bir log uyarısının arkasında SONSUZA KADAR hareket-kilitli
    // bırakırdı (LP-CODE-REVIEW bulgusu).

    /// <inheritdoc />
    public void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config,
        Action onComplete, Action<string> onFailed)
    {
        Debug.LogWarning("[SahneGecisi] RequestSoftTransition henüz uygulanmadı (Story 003).", this);
        onFailed?.Invoke("NotImplemented");
    }

    /// <inheritdoc />
    public void RequestHardCut(string toScene, HardCutConfig config,
        Action onComplete, Action<string> onFailed)
    {
        Debug.LogWarning("[SahneGecisi] RequestHardCut henüz uygulanmadı (Story 005).", this);
        onFailed?.Invoke("NotImplemented");
    }

    /// <inheritdoc />
    public void PreloadHardCut(string toScene) =>
        // Callback taşımaz — sözleşme ihlali riski yok.
        Debug.LogWarning("[SahneGecisi] PreloadHardCut henüz uygulanmadı (Story 005).", this);

    /// <inheritdoc />
    /// <remarks>
    /// Uyarı-stub'ı DEĞİL, gerçek implementasyon — ama YARIM: yalnız AKTİF
    /// HARD CUT'ın config'ini okuyor. Sözleşme "aktif YA DA PRELOAD EDİLMİŞ"
    /// diyor ve ADR-0015'in ikili eşik yolu tam da preload edilmiş bir HARD
    /// CUT'ı `RequestHardCut`'tan ÖNCE sorgular.
    ///
    /// **Story 005 preload yolunu buraya bağlamak ZORUNDA** (fast-path
    /// preload→aktif kopyalayarak ya da burada ikisine birden bakarak); Story
    /// 007'nin AC-6 fixture'ı zaten preload edilmiş bir HARD CUT kullanıyor.
    /// Bağlanmazsa sorgu `false` döner ve Adaptif Ses YANLIŞ bitiş tonunu çalar.
    /// Bugün yazan hiçbir yol olmadığı için test edilemez — o yüzden burada
    /// yazılı (LP+QL gate bulgusu).
    /// </remarks>
    public bool GetCurrentHardCutAbrupt() => _state.ActiveHardCutConfig.Abrupt;
}
