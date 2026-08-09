using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sahneye yerleştirilen anı-shift bölgesi (ADR-0005 Decision — birebir):
/// lokal <see cref="Volume"/> (isGlobal=false, paylaşılan VolumeProfile,
/// blendDistance=0 — pratik kural: kutu, Box Collider Safety Margin formülüne
/// göre boyutlandırılır; spike Corridor C bunu ampirik doğruladı), box trigger
/// collider, shiftId, TriggerMode ve Inspector-atanmış ZoneLight dizisi.
///
/// Per-zone TEK coroutine tüm ışık dizisini sürer (ışık başına Update yok);
/// <c>Volume.weight</c>'in TEK yazıcısı bu ticker'dır (TR-isik-002). Mantık
/// MonoBehaviour'a gömülü değildir: ilerleme Story 002'nin saf
/// <see cref="ShiftProgressMachine"/>'inde yaşar. Baked lightmap setine hiç
/// dokunulmaz — yazım modeli post-process + gerçek-zamanlı ışık lerp'idir
/// (TR-isik-017, yapısal).
///
/// Bu story'de (003) bölge yalnız dış çağrıyla sürülür: Automatic pozisyon
/// izleme + R_exit histerezisi + SOFT co-residency + OnDestroy tamamlama
/// garantisi Story 004'te; Persistent semantiği Story 005'te eklenir.
/// Alanlar internal: test fixture'ları runtime'da kurup doğrudan atar
/// (asla on-disk asset).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class ShiftZone : MonoBehaviour, IShiftZoneHandle
{
    [SerializeField] internal string _shiftId;
    [SerializeField] internal TriggerMode _triggerMode;
    [SerializeField] internal ZoneLight[] _lights;
    [SerializeField] internal Volume _volume; // isGlobal=false, paylaşılan profil; API'de asla açığa çıkmaz (tek-yazıcı zırhı)
    [SerializeField] internal float _rTrigger;

    // AC14c-pre: açıkça ayarlanmamışsa (Vector3.zero sentinel) Awake/OnValidate
    // collider bounds.center'ına düşürür — asla tanımsız kalmaz. Kasti merkezi
    // TAM sıfır olan bölge de bounds.center'a düşer; pratikte aynı nokta.
    [SerializeField] internal Vector3 _zoneCenter;

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

        // Disable, koşan coroutine'i öldürür — aktif bölge re-enable'da ticker'ını
        // geri alır (tam yaşam döngüsü/OnDestroy garantisi Story 004'ün işi).
        if (_state != ShiftState.Dormant && _tickCoroutine == null)
        {
            _tickCoroutine = StartCoroutine(TickShift());
        }
    }

    private void OnDisable()
    {
        IsikVolumeDurumSistemi.InternalInstance.DeregisterZone(this);
        _tickCoroutine = null; // Unity coroutine'i disable'da durdurdu — referansı bayat bırakma
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
                _tickCoroutine ??= StartCoroutine(TickShift());
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
    /// Per-zone TEK ticker (ADR-0005): her karede Volume.weight = ShiftProgress
    /// ve tüm ZoneLight girdileri AYNI progress değeriyle lockstep (TR-isik-006;
    /// AC13 çifti aynı karede). Held'de iş yapmaz, yalnız bekler (Story 004'ün
    /// histerezis izlemesi de bu döngüye eklenecek — ikinci coroutine değil).
    /// </summary>
    private IEnumerator TickShift()
    {
        while (true)
        {
            if (_state == ShiftState.ShiftingIn || _state == ShiftState.ShiftingOut)
            {
                _machine.Tick(Time.deltaTime, _activeConfig.Duration);
                ApplyProgress(_machine.ShiftProgress);

                if (_state == ShiftState.ShiftingIn && _machine.X >= 1f)
                {
                    TransitionTo(ShiftState.Held);
                }
                else if (_state == ShiftState.ShiftingOut && _machine.X <= 0f)
                {
                    // Referans, event'ten ÖNCE temizlenir: Dormant event'ine senkron
                    // re-trigger yapan bir abone ??= guard'ına takılıp ticker'sız
                    // kalmasın (reentrancy — LP review bulgusu).
                    _tickCoroutine = null;
                    TransitionTo(ShiftState.Dormant);
                    yield break; // Dormant ManualOnly bölge gerçek sıfır maliyet (ADR-0005)
                }
            }

            yield return null;
        }
    }

    // Volume.weight'in projedeki TEK yazım noktası (TR-isik-002) + ışık dizisi
    // aynı p ile aynı karede (TR-isik-006 lockstep — tek paylaşılan girdi).
    private void ApplyProgress(float progress)
    {
        _volume.weight = progress;

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
