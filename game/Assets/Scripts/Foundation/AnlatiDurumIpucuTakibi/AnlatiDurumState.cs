using System;
using System.Collections.Generic;

/// <summary>
/// İpucu takibi implementasyonu (ADR-0007) — plain C# sınıf, hiçbir Unity yaşam
/// döngüsü yok. ADR-0001'in üçlü deseninin ikinci tam örneği (birincisi
/// Gece/Oturum Durumu).
///
/// Durum geçişi TEK YÖNLÜDÜR: Unknown → Known, geri dönüş yok (oyuncu bir şeyi
/// "unutmaz" — GDD States and Transitions). Üçüncü bir ara durum ("bilinen ama
/// henüz diyaloğa bağlanmamış") KASITLI olarak yok: o, tüketen sistemin
/// (Diyalog/Anlatı İçeriği) kendi bookkeeping'idir.
///
/// `_seenShiftIds` bu story'de yalnız TANIMLANIR — onu yazan Held handler'ı
/// Story 002'de, gerçek Işık/Volume aboneliği Story 003'te gelir. `MarkClueKnown`
/// bu kümeye karşı doğrulama yapmaz (GDD AC9).
/// </summary>
public sealed class AnlatiDurumState : IAnlatiDurumState
{
    private readonly HashSet<string> _knownClueIds = new HashSet<string>();

    // Held'e ulaşan her shiftId burada işaretlenir (Story 002'nin handler'ı yazar).
    // Gece/Oturum Durumu'nun FiredTriggerIds'inden AYRI ve bağımsız — farklı
    // sahiplik, birbirine sorgu yok (GDD Overview, sistem sınırı notu).
    private readonly HashSet<string> _seenShiftIds = new HashSet<string>();

    /// <summary>
    /// Held'e ulaşan bir shiftId'yi işaretler — idempotent. Story 002'nin handler'ı
    /// tek yazıcıdır. Ham koleksiyon BİLEREK dışa açılmaz: `GeceOturumDurumuState`
    /// emsali de üyeliği sorgu metotlarıyla verir, asla ham küme ile
    /// (`IGeceOturumDurumuState` doc'u bunu açıkça kural olarak yazıyor).
    /// </summary>
    internal void MarkShiftSeen(string shiftId) => _seenShiftIds.Add(shiftId);

    /// <summary>Story 002'nin ALL-semantiği kapsama kontrolü için salt-okunur görünüm.</summary>
    internal IReadOnlyCollection<string> SeenShiftIds => _seenShiftIds;

    /// <summary>Bir shiftId'nin Held'e ulaşmış sayılıp sayılmadığı (test/handler sorgusu).</summary>
    internal bool HasSeenShift(string shiftId) => _seenShiftIds.Contains(shiftId);

    // shiftId -> onu `RequiredShiftIds`'inde listeleyen HER ClueDefinition.
    // Ters indeks BİLİNÇLİ tercih (ADR-0007 Alternative 1): per-event lineer
    // tarama reddedildi — indeks neredeyse bedava (bir avuç sözlük ekleme, bir
    // kez) ve GDD'nin 15-20 tetikleyicilik Full Vision ölçeğini yapısal
    // değişiklik olmadan karşılıyor. null = henüz yüklenmedi (Story 003'ün lazy
    // Addressables yüklemesi doldurur).
    private Dictionary<string, List<ClueDefinition>> _byRequiredShiftId;

    /// <summary>
    /// Story 003'ün lazy Addressables yüklemesinin bağlanma noktası — süreç
    /// başına bir kez bağlanır, in-place reset'i atlatır (gece-oturum'un
    /// `BindIsShiftPersistentQuery` deseniyle aynı). Story 002'nin testleri
    /// indeksi DOĞRUDAN kurar ve bu kancayı hiç bağlamaz: böylece Held mantığı
    /// Addressables'a dokunmadan sınanabilir (Logic story izolasyon kuralı).
    /// </summary>
    private Action _ensureRegistryLoaded;

    /// <summary>Story 003'ün bağlama noktası — process başına bir kez.</summary>
    internal void BindEnsureRegistryLoaded(Action ensureRegistryLoaded) =>
        _ensureRegistryLoaded = ensureRegistryLoaded;

    /// <summary>
    /// Yükleme kalıcı olarak başarısız oldu mu (LATCH). Story 003'ün karar noktası:
    /// `WaitForCompletion()` patlarsa indeks null kalır ve BLOKLAYAN Addressables
    /// çağrısı HER Held'de yeniden denenirdi. Manifest'in "başarısız yükleme bir
    /// build defektidir, kurtarılabilir bir durum değildir" duruşu gereği bir kez
    /// denenir, başarısızsa BOŞ indeksle latch'lenir + `Debug.LogError` — Story
    /// 004'ün build-blocking anahtar kontrolü bu vakayı üretimde zaten imkânsız
    /// kılıyor (LP gate devri, Story 002).
    /// </summary>
    internal bool RegistryLoadFailed { get; private set; }

    /// <summary>Yükleme kalıcı olarak başarısız işaretlenir — boş indeksle latch'lenir.</summary>
    internal void MarkRegistryLoadFailed()
    {
        RegistryLoadFailed = true;
        BuildReverseIndex(null);
    }

    /// <summary>
    /// YALNIZ TEST temizliği: indeksi ve latch'i sıfırlar. Üretimde ASLA çağrılmaz —
    /// `ResetOnLoad()` cache'i bilerek korur (ADR-0015). Bu seam olmadan, latch'i
    /// sınayan bir test süreç ömrü boyunca gerçek Addressables yolunu kapatırdı
    /// (QL gate bulgusu: kalıcı proses zehirlenmesi).
    /// </summary>
    internal void ResetRegistryForTests()
    {
        _byRequiredShiftId = null;
        RegistryLoadFailed = false;
    }

    /// <summary>Ters indeks kuruldu mu — lazy yükleme guard'ı (Story 003) ve testler okur.</summary>
    internal bool IsRegistryLoaded => _byRequiredShiftId != null;

    /// <summary>
    /// Ters indeksi verilen tanımlardan kurar. Story 003'ün `EnsureRegistryLoaded()`'ı
    /// Addressables'tan gelen `ClueRegistry.Definitions` ile çağırır; testler kendi
    /// runtime-created tanımlarıyla doğrudan çağırır. Null/boş `ClueId` taşıyan ya da
    /// null tanımlar atlanır — build-time doğrulama (Story 004) zaten engelliyor, bu
    /// yalnız çalışma zamanı sağlamlığı.
    /// </summary>
    internal void BuildReverseIndex(IReadOnlyList<ClueDefinition> definitions)
    {
        var index = new Dictionary<string, List<ClueDefinition>>();

        if (definitions != null)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                ClueDefinition definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.ClueId) || definition.RequiredShiftIds == null)
                {
                    continue;
                }

                IReadOnlyList<string> required = definition.RequiredShiftIds;
                for (int r = 0; r < required.Count; r++)
                {
                    string shiftId = required[r];
                    if (string.IsNullOrWhiteSpace(shiftId))
                    {
                        continue;
                    }

                    if (!index.TryGetValue(shiftId, out List<ClueDefinition> bucket))
                    {
                        bucket = new List<ClueDefinition>();
                        index[shiftId] = bucket;
                    }

                    // Yinelenen shiftId (ör. [A, A, B]) aynı tanımı iki kez
                    // eklemesin — kova başına tanım TEKİL olmalı, yoksa tek bir
                    // Held aynı ipucu için tamamlanma kontrolünü boşuna iki kez
                    // koşardı (GDD Edge Cases: yinelenen girdi işlevsel olarak etkisiz).
                    if (!bucket.Contains(definition))
                    {
                        bucket.Add(definition);
                    }
                }
            }
        }

        _byRequiredShiftId = index;
    }

    /// <summary>
    /// Işık/Volume'un `OnShiftStateChanged`'inin ÇEKİRDEK mantığı — yalnız
    /// `Held` işlenir (GDD AC4: Shifting-In/Out ve Dormant hiçbir giriş eklemez,
    /// hiçbir tamamlanma kontrolü çalıştırmaz; "bilinmek" için oyuncunun anıyı
    /// TAM olarak deneyimlemiş olması gerekir, yarım geçiş yetmez).
    ///
    /// Sıralama ADR-0007 ile birebir: filtre → lazy yükleme → işleme. Yükleme
    /// filtreden SONRA olmak zorunda (yalnız gerçek bir Held'de tetiklensin diye).
    /// </summary>
    internal void ProcessShiftStateChanged(string shiftId, ShiftState newState)
    {
        if (newState != ShiftState.Held)
        {
            return;
        }

        // Bozuk shiftId için BLOKLAYAN yüklemeyi boşuna tetikleme — guard
        // `ProcessHeldShift`'te de var, burada yalnız yükleme maliyetini eler.
        if (string.IsNullOrWhiteSpace(shiftId))
        {
            return;
        }

        _ensureRegistryLoaded?.Invoke();
        ProcessHeldShift(shiftId);
    }

    /// <summary>
    /// Bir Held geçişini işler — ters indeksin ZATEN dolu olduğunu varsayar
    /// (Addressables'a DOKUNMAZ; Story 002'nin testleri bunu doğrudan çağırır).
    ///
    /// `_seenShiftIds.Add` idempotent olduğundan Persistent shift'in reload
    /// sonrası re-fire'ı zararsızdır (GDD Edge Cases / AC11). Aday tanımların
    /// HEPSİ bağımsız değerlendirilir: tek bir Held sıfır, bir ya da birden
    /// fazla ipucu tamamlayabilir (GDD AC10) — erken return YOK.
    ///
    /// **ABONE İSTİSNASI (LP gate bulgusu, testle sabitlendi)**: `MarkClueKnown`
    /// `OnClueKnown`'ı SENKRON fırlatır. Bir abone patlarsa istisna bu döngüden
    /// yukarı yayılır ve KALAN aday tanımlar DEĞERLENDİRİLMEZ — paylaşılan bir
    /// shiftId senaryosunda (GDD AC10) ikinci ipucu, o shift yeniden fırlayana
    /// kadar Unknown kalır. try/catch BİLİNÇLİ olarak eklenmedi (`AddFiredTrigger`
    /// emsalinin gözden geçirilmiş duruşu: yutma yok).
    ///
    /// **Story 003 kararı (LP gate)**: try/catch HÂLÂ eklenmedi ve etki alanı
    /// ölçüldü. `FoundationBootstrap.ResetAll()` `GeceOturumDurumu`'nu (adım 3)
    /// Anlatı'dan (adım 4) ÖNCE dokunduğu için Gece'nin static ctor'ı önce koşar
    /// ve invocation list'te Anlatı'dan ÖNCE gelir — Gece'nin Settled yazımı bu
    /// handler tarafından ASLA düşürülemez. Gerçek etki alanı SONRADAN abone olan
    /// `OnEnable` MonoBehaviour'ları (planlanan `AdaptifSesController`) ve
    /// `ShiftZone`'un kendi tick coroutine'i. Bu, "tek bir story'nin tek taraflı
    /// çözeceği şey değil, kesişen bir sorun" gerekçesini GÜÇLENDİRİR: doğru
    /// koruma, tek fırlatma noktası olan `IsikVolumeState.RaiseShiftStateChanged`
    /// içinde delege başına try/catch (TR-isik-019) — hem yan aboneleri hem
    /// üreticiyi korur. isik-volume epic'ine iş kalemi olarak yazıldı.
    /// </summary>
    internal void ProcessHeldShift(string shiftId)
    {
        if (string.IsNullOrWhiteSpace(shiftId))
        {
            return;
        }

        _seenShiftIds.Add(shiftId);

        if (_byRequiredShiftId == null || !_byRequiredShiftId.TryGetValue(shiftId, out List<ClueDefinition> candidates))
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ClueDefinition definition = candidates[i];

            // ALL semantiği: tanımın TÜM requiredShiftIds'i görülmüş olmalı
            // (küme-kapsama; yinelenenler HashSet tarafından tekilleştirilir).
            if (_seenShiftIds.IsSupersetOf(definition.RequiredShiftIds))
            {
                MarkClueKnown(definition.ClueId);
            }
        }
    }

    public bool IsClueKnown(string clueId) => _knownClueIds.Contains(clueId);

    // ADR-0007 birebir: CANLI kümenin kendisi döner, kopya YOK (her sorguda
    // allocation olmaması için). Dönen arayüz yazma açmaz; "boş durumda null
    // değil boş koleksiyon" garantisi kümenin readonly alan olmasıyla yapısaldır.
    //
    // İKİ SONUCU BİLİNÇLİ (gate bulgusu, testle sabitlendi):
    // (1) Görünüm CANLIDIR — sonradan işaretlenen ipuçları daha önce alınmış bir
    //     referansta da görünür; `ResetOnLoad()` o referansı da boşaltır.
    // (2) Bir tüketici bu koleksiyonu GEZERKEN `MarkClueKnown` çağırırsa
    //     `InvalidOperationException` alır (koleksiyon-değişti). Türetilmiş ipucu
    //     işaretlemek isteyen tüketici önce kendi kopyasını almalıdır.
    public IReadOnlyCollection<string> GetKnownClueIds() => _knownClueIds;

    public void MarkClueKnown(string clueId)
    {
        // Null/boş guard (LP+QL gate bulgusu — GDD/ADR ikisi de sessiz):
        // yazılmamış bir `ClueDefinition.clueId` alanı null gelirse, null
        // `_knownClueIds`'e girer ve `GetKnownClueIds()`'i gezen tüketicilere
        // (Plot Twist/Final) null eleman teslim edilirdi. Sessiz no-op, bu
        // sistemin belgeli sessiz-başarısızlık duruşuyla tutarlı (yazım hatalı
        // clueId de istisna değil `false` üretir — GDD AC6).
        if (string.IsNullOrWhiteSpace(clueId))
        {
            return;
        }

        // Idempotency (GDD AC3, TR-anlati-003): HashSet.Add false dönerse zaten
        // biliniyordu — sessiz no-op, event İKİNCİ kez fırlamaz. Persistent
        // shift'in reload sonrası re-fire'ı da bu yolla zararsızdır (GDD AC11).
        if (!_knownClueIds.Add(clueId))
        {
            return;
        }

        // Yazma event'ten ÖNCE tamamlanır ve bir abone patlarsa GERİ ALINMAZ:
        // ipucu Known kalır (tek yönlü geçiş — GDD States and Transitions),
        // patlayan abone o bildirimi bir daha alamaz. `AddFiredTrigger` emsaliyle
        // aynı şekil ve aynı bilinçli sözleşme.
        OnClueKnown?.Invoke(clueId);
    }

    public event Action<string> OnClueKnown;

    /// <summary>
    /// IN-PLACE reset (ADR-0015 rejimi) — instance ASLA değiştirilmez, yalnız
    /// alanlar temizlenir. Gerekçe: Story 003'te FACADE'ın static constructor'ında
    /// bağlanacak Işık/Volume aboneliği süreç başına BİR KEZ bağlanır; instance
    /// değiştirilseydi her atılan instance'ın handler'ı kalıcı event'e yetim abone
    /// kalırdı (stale-handler birikimi). Varsayılan-dışı alan YOK — ikisi de boş
    /// küme (manifest'in "in-place reset varsayılan-dışı alanları açıkça yeniden
    /// kurmalı" maddesi burada boş geçer; kıyas: `IsSessionActive = true`).
    ///
    /// `OnClueKnown` aboneleri BİLEREK temizlenmez: in-place rejimin tüm amacı
    /// süreç-ömürlü abonelerin oturum sınırını sağ geçmesidir.
    ///
    /// Yüklenmiş `ClueRegistry` cache'ine (Story 003) burada ASLA dokunulmaz:
    /// değişmez authored config'dir ve `ResetOnLoad()` bir engine asset API'sine
    /// dokunamaz (manifest kuralı, ADR-0007/0015).
    /// </summary>
    internal void ResetOnLoad()
    {
        _knownClueIds.Clear();
        _seenShiftIds.Clear();
    }
}
