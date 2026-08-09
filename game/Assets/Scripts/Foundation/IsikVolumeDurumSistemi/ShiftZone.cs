using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahneye yerleştirilen anı-shift bölgesi (ADR-0005 Decision — birebir):
/// lokal <see cref="Volume"/> (isGlobal=false, paylaşılan VolumeProfile,
/// blendDistance=0 — pratik kural: kutu, Box Collider Safety Margin formülüne
/// göre boyutlandırılır; spike Corridor C bunu ampirik doğruladı), box trigger
/// collider, shiftId, TriggerMode ve Inspector-atanmış ZoneLight dizisi.
///
/// Per-zone TEK coroutine, durum-kapılı İKİ sorumluluk taşır (ADR "Automatic-zone
/// monitoring"): Dormant'ta yalnız Automatic bölgeler için pozisyon izleme
/// (R_trigger giriş tespiti), Dormant dışında R_exit histerezis kontrolü + tick.
/// ManualOnly bölge Dormant'ta HİÇBİR coroutine koşturmaz — gerçek sıfır maliyet.
/// <c>Volume.weight</c>'in TEK yazıcısı bu ticker'dır (TR-isik-002). İlerleme
/// Story 002'nin saf <see cref="ShiftProgressMachine"/>'inde yaşar. Baked
/// lightmap setine hiç dokunulmaz (TR-isik-017, yapısal).
///
/// SOFT co-residency (TR-isik-011): bölgenin sahnesi aktif değilken POZİSYON
/// örneklemesi atlanır (giriş/çıkış tespiti yok) ama zaman-tabanlı x ASLA
/// donmaz. OnDestroy tamamlama garantisi (ADR): mid-transition yıkım terminal
/// duruma zorla-tamamlar + event'i teardown'dan ÖNCE fırlatır — güvenlik
/// gerekçesi MEKANSAL MESAFE (Box Safety Margin boyutlandırması), sahne-aktifliği
/// değil. Persistent'ın "çıkış kontrolü hiç koşmaz" kuralı Story 005'te.
/// Alanlar internal: test fixture'ları runtime'da kurup doğrudan atar.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class ShiftZone : MonoBehaviour, IShiftZoneHandle
{
    [SerializeField] internal string _shiftId;
    [SerializeField] internal TriggerMode _triggerMode;
    [SerializeField] internal ZoneLight[] _lights;
    [SerializeField] internal Volume _volume; // isGlobal=false, paylaşılan profil; API'de asla açığa çıkmaz (tek-yazıcı zırhı)
    [SerializeField] internal float _rTrigger;

    /// <summary>Histerezis faktörü (GDD default 1.15; guard'lı tüketilir — k=1.0 dahi clamp'lenir).</summary>
    [SerializeField] internal float _kHysteresis = 1.15f;

    /// <summary>
    /// Automatic self-trigger'ın kullanacağı authored config — ManualOnly bölgede
    /// kullanılmaz; Automatic bölgede atanmamış olması build-time'da yakalanacak
    /// (Story 006), runtime'da izleme sessiz atlar.
    /// </summary>
    [SerializeField] internal ShiftConfig _autoTriggerConfig;

    // AC14c-pre: açıkça ayarlanmamışsa (Vector3.zero sentinel) Awake/OnValidate
    // collider bounds.center'ına düşürür — asla tanımsız kalmaz. Kasti merkezi
    // TAM sıfır olan bölge de bounds.center'a düşer; pratikte aynı nokta.
    [SerializeField] internal Vector3 _zoneCenter;

    /// <summary>
    /// Oyuncu pozisyon örnekleyicisi — FPC henüz yok; birinci-şahıs epic'i
    /// production wiring'ini bağlar (TR-isik-018'in izleme yarısı), testler
    /// doğrudan enjekte eder. Null iken pozisyon tespiti (giriş/çıkış) atlanır.
    /// </summary>
    internal Func<Vector3> _playerPositionSampler;

    private readonly ShiftProgressMachine _machine = new ShiftProgressMachine();
    private ShiftState _state = ShiftState.Dormant;
    private ShiftConfig _activeConfig;
    private Coroutine _tickCoroutine;

    /// <summary>Bölgenin açık eşleştirme anahtarı (TR-isik-015).</summary>
    public string ShiftId => _shiftId;

    /// <summary>Yalnız Dormant inaktif sayılır — Shifting-Out'ta hâlâ true (GDD AC10).</summary>
    public bool IsShiftActive => _state != ShiftState.Dormant;

    /// <summary>Bölgeyi son tetikleyen config'in Persistent bayrağı (davranışı Story 005).</summary>
    public bool IsShiftPersistent { get; private set; }

    /// <summary>Bölgeyi son tetikleyen config'in StingerAudioRadius değeri.</summary>
    public float StingerAudioRadius { get; private set; }

    /// <summary>Event payload'ındaki bölge merkezi (AC15).</summary>
    public Vector3 ZoneCenter => _zoneCenter;

    private void Awake() => ResolveZoneCenterFallback();

    private void OnValidate() => ResolveZoneCenterFallback();

    private void OnEnable()
    {
        IsikVolumeDurumSistemi.InternalInstance.RegisterZone(this);

        // Automatic bölge Dormant'ta da izler → coroutine hemen başlar (ADR).
        // ManualOnly yalnız aktif kalmışsa geri alır (disable coroutine'i
        // öldürür; tam yaşam döngüsü garantisi OnDestroy'da).
        if (_triggerMode == TriggerMode.Automatic || _state != ShiftState.Dormant)
        {
            _tickCoroutine ??= StartCoroutine(MonitorAndTick());
        }
    }

    private void OnDisable()
    {
        IsikVolumeDurumSistemi.InternalInstance.DeregisterZone(this);

        // DİKKAT: Unity, coroutine'i yalnız GameObject deaktivasyonunda/yıkımda
        // kendisi durdurur — component-level disable (enabled=false) DURDURMAZ.
        // Açık StopCoroutine olmadan coroutine yetim koşmaya devam eder ve
        // re-enable ??= ile İKİNCİ coroutine başlatırdı (LP review bulgusu).
        // SetActive yolunda StopCoroutine zararsız no-op.
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }
    }

    /// <summary>
    /// Sahne-yıkımı tamamlama garantisi (ADR-0005, unity-specialist BLOCKING
    /// düzeltmesi): mid-transition yıkım, geçişi doğal terminal durumuna anında
    /// zorla-tamamlar ve terminal event'i teardown'dan ÖNCE senkron fırlatır —
    /// Held-kapılı bir ipucu reveal'i sessizce düşmez. Görsel sıçrama güvenliği
    /// MEKANSAL MESAFE argümanıdır (Box Safety Margin + SOFT zamanlaması),
    /// sahne-aktifliği değil. NOT: OnDisable bu noktada bölgeyi çoktan deregister
    /// etti — terminal event'in abonesi payload'a güvenmeli; aynı handler içinde
    /// facade'a IsShiftActive cross-query'si Dormant-default (false) döner.
    /// </summary>
    private void OnDestroy()
    {
        if (_state == ShiftState.ShiftingIn)
        {
            ApplyProgress(1f);
            TransitionTo(ShiftState.Held);
        }
        else if (_state == ShiftState.ShiftingOut)
        {
            ApplyProgress(0f);
            TransitionTo(ShiftState.Dormant);
        }
    }

    /// <summary>
    /// Yeni geçiş başlattıysa true. Aktifken (Shifting-Out hariç) false/no-op —
    /// mevcut config/progress değişmez, sıçrama yok (GDD AC6). Shifting-Out'ta
    /// yön-flip + true; x mevcut değerinden devam eder (AC7). DİKKAT: flip
    /// dalında da MEVCUT config korunur, çağıranın yeni config'i BİLEREK
    /// benimsenmez (AC7 bunu istemez; Duration sürekliliği korunur) — Persistent
    /// bir config'in Out-flip'te kaybolması Story 005'te yeniden değerlendirilecek.
    /// </summary>
    public bool TriggerShift(ShiftConfig config)
    {
        if (config == null)
        {
            return false; // config'siz geçiş tanımsız — sessiz no-op (facade null-guard'ıyla tutarlı)
        }

        switch (_state)
        {
            case ShiftState.ShiftingIn:
            case ShiftState.Held:
                return false; // AC6 — idempotent no-op

            case ShiftState.ShiftingOut:
                _machine.BeginShiftIn(); // AC7 — mevcut x'ten yön-flip, sıçrama yok
                TransitionTo(ShiftState.ShiftingIn);
                return true;

            default: // Dormant
                _activeConfig = config;
                IsShiftPersistent = config.Persistent;
                StingerAudioRadius = config.StingerAudioRadius;
                _machine.BeginShiftIn();
                TransitionTo(ShiftState.ShiftingIn);
                _tickCoroutine ??= StartCoroutine(MonitorAndTick());
                return true;
        }
    }

    /// <summary>
    /// Shifting-In/Held'de mevcut x'ten Shifting-Out'a çevirir (GDD AC8);
    /// referans sayımı yok — kaç TriggerShift çağranı olursa olsun İLK Revert
    /// geri dönüşü başlatır (AC11). İnaktifken sessiz no-op (AC9).
    /// </summary>
    public void RevertShift()
    {
        if (_state == ShiftState.Dormant || _state == ShiftState.ShiftingOut)
        {
            return; // AC9 — hata yok, event yok
        }

        _machine.BeginShiftOut();
        TransitionTo(ShiftState.ShiftingOut);
    }

    /// <summary>
    /// Per-zone TEK coroutine, durum-kapılı iki sorumluluk (ADR-0005):
    /// • Dormant — yalnız Automatic: pozisyon izleme, R_trigger girişinde
    ///   self-trigger (TR-isik-009). ManualOnly buraya yalnız Shifting-Out'tan
    ///   dönerken uğrar ve coroutine biter (Dormant'ta gerçek sıfır maliyet).
    /// • Shifting-In/Out — tick: Volume.weight = ShiftProgress + ışıklar aynı
    ///   değerle lockstep (TR-isik-006). Zaman-tabanlı x, sahne aktif olmasa da
    ///   İLERLER (TR-isik-011).
    /// • Held — R_exit histerezis kontrolü (TR-isik-010; her iki mod).
    /// Pozisyon örneklemesi her dalda sahne-aktif kapısının arkasındadır
    /// (manifest: gameObject.scene == SceneManager.GetActiveScene()).
    /// Işınlanma-çıkışı (GDD AC18) doğal düşer: sonraki tick "R_exit dışında"
    /// görür; tek tick'te bölgeyi atlayan hareket (AC19) hiç tespit edilmez —
    /// belgeli kısıt, per-frame örnekleme tasarımının sonucu.
    /// </summary>
    private IEnumerator MonitorAndTick()
    {
        while (true)
        {
            bool positionSamplingAllowed =
                _playerPositionSampler != null && gameObject.scene == SceneManager.GetActiveScene();

            switch (_state)
            {
                case ShiftState.Dormant:
                    if (_triggerMode == TriggerMode.ManualOnly)
                    {
                        _tickCoroutine = null;
                        yield break; // yalnız dış çağrı başlatabilir (GDD AC2a)
                    }

                    if (positionSamplingAllowed && _autoTriggerConfig != null
                        && IsInside(IsikVolumeFormulas.ClampTriggerRadius(_rTrigger)))
                    {
                        TriggerShift(_autoTriggerConfig); // TR-isik-009 — kendi kendini tetikler
                    }
                    break;

                case ShiftState.ShiftingIn:
                case ShiftState.ShiftingOut:
                    _machine.Tick(Time.deltaTime, _activeConfig.Duration);
                    ApplyProgress(_machine.ShiftProgress);

                    if (_state == ShiftState.ShiftingIn && _machine.X >= 1f)
                    {
                        TransitionTo(ShiftState.Held);
                    }
                    else if (_state == ShiftState.ShiftingOut && _machine.X <= 0f)
                    {
                        if (_triggerMode == TriggerMode.ManualOnly)
                        {
                            // Referans, event'ten ÖNCE temizlenir: Dormant event'ine
                            // senkron re-trigger yapan abone ??= guard'ına takılıp
                            // ticker'sız kalmasın (reentrancy — LP review bulgusu).
                            _tickCoroutine = null;
                            TransitionTo(ShiftState.Dormant);
                            yield break; // Dormant ManualOnly bölge gerçek sıfır maliyet (ADR-0005)
                        }

                        TransitionTo(ShiftState.Dormant); // Automatic: izlemeye geri döner
                    }
                    break;

                case ShiftState.Held:
                    // R_exit histerezisi (TR-isik-010) — çıkış eşiği girişten ayrı;
                    // R_trigger-R_exit bandındaki gidiş-geliş Held'i bozmaz (AC3b).
                    // Persistent'ın "bu kontrol hiç koşmaz" kuralı Story 005'te.
                    if (positionSamplingAllowed
                        && !IsInside(IsikVolumeFormulas.ExitRadius(_rTrigger, _kHysteresis)))
                    {
                        RevertShift(); // AC3a — R_exit dışına çıkış
                    }
                    break;
            }

            yield return null;
        }
    }

    private bool IsInside(float radius) =>
        Vector3.Distance(_playerPositionSampler(), _zoneCenter) <= radius;

    // Volume.weight'in projedeki TEK yazım noktası (TR-isik-002) + ışık dizisi
    // aynı p ile aynı karede (TR-isik-006 lockstep — tek paylaşılan girdi).
    // Null guard'lar OnDestroy yolunda kardeş bileşenlerin önce yıkılmış
    // olabilmesi için (destroy sırası garanti değil).
    private void ApplyProgress(float progress)
    {
        if (_volume != null)
        {
            _volume.weight = progress;
        }

        if (_lights == null)
        {
            return; // alan atanmadan AddComponent edilmiş bölge (yalnız runtime kurulumda mümkün)
        }

        for (int i = 0; i < _lights.Length; i++)
        {
            ZoneLight entry = _lights[i];
            if (entry.light == null)
            {
                continue; // eksik atama build-time'da yakalanır (Story 006); runtime sessiz atlar
            }

            entry.light.color = IsikVolumeFormulas.LightColor(entry.baseColor, entry.memoryColor, progress);
            // memoryIntensity guard'lı tüketilir (GDD AC14a'nın mutlak formu):
            // negatif intensity asla, mem ≥ base asla (erişilebilirlik düşüş garantisi).
            entry.light.intensity = Mathf.Lerp(
                entry.baseIntensity,
                IsikVolumeFormulas.ClampMemoryIntensity(entry.memoryIntensity, entry.baseIntensity),
                progress);
        }
    }

    // Her GERÇEK durum geçişinde tam bir kez, facade'ın TEK raise yolu üzerinden
    // (TR-isik-019; AC15 payload: shiftId, newState, zoneCenter, R_trigger).
    private void TransitionTo(ShiftState newState)
    {
        if (_state == newState)
        {
            return;
        }

        _state = newState;
        IsikVolumeDurumSistemi.InternalInstance.RaiseShiftStateChanged(_shiftId, newState, _zoneCenter, _rTrigger);
    }

    private void ResolveZoneCenterFallback()
    {
        if (_zoneCenter == Vector3.zero)
        {
            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                // World-space kutu merkezi — TransformPoint(box.center), AABB
                // bounds.center'a her zaman eşittir ama fizik senkronunu beklemez
                // (Awake anında deterministik; AC14c-pre).
                _zoneCenter = box.transform.TransformPoint(box.center);
            }
        }
    }
}
