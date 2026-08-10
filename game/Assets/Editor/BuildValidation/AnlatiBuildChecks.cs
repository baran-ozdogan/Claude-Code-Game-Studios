using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

/// <summary>
/// Addressable anahtarının build-time ÇÖZÜLEBİLİRLİĞİ için test dikişi (story
/// AC-3 zorunlu kılıyor). `IBuildSceneWalker` ile aynı şekil: internal interface,
/// ctor'dan enjekte, üretimde gerçek `AddressableAssetSettings`'i saran tek sınıf.
/// Böylece testler in-memory kalır ve "AddressableAssetSettings in-memory
/// fabrikelenebilir mi" belirsizliği tamamen atlanır.
///
/// İmzada HİÇBİR `UnityEditor.AddressableAssets.*` tipi geçmez — `EditModeTests`
/// o assembly'yi referans etmiyor ve `overrideReferences: true`; imzaya sızan bir
/// editor tipi test assembly'sini CS0012 ile kırardı.
/// </summary>
internal interface IAddressableKeyResolver
{
    /// <summary>
    /// Sözleşme: "çözülür" = TAM OLARAK BİR entry bu adresi taşır, asset yolu boş
    /// değil VE entry'nin grubu build'e dahil. Üçünden biri bile tutmazsa false.
    ///
    /// Belirsizlik (iki entry aynı adresi taşıması) neden "çözülmez" sayılır:
    /// Addressables hiçbir yerde adres tekilliğini ZORLAMIYOR (paket kaynağı
    /// tarandı), ilk eşleşme sözlük sırasına bağlıdır ve çalışma zamanının
    /// çözdüğü entry aynı olmak zorunda değildir.
    /// Nüans: `ResourceLocationMap.Locate` aday konumları İSTENEN TİPE göre
    /// filtreler, yani FARKLI tipte iki asset aynı adresi taşısa runtime yine
    /// deterministik olarak `ClueRegistry`'yi bulurdu. Check yine de bloklar —
    /// yön muhafazakâr ve bir build kapısı için doğru olan bu.
    /// </summary>
    bool KeyResolves(string key);
}

/// <summary>
/// İçerik check'lerinin `ClueRegistry`'ye erişim dikişi.
///
/// **`Load()` NULL DÖNEBİLİR ve bu bilinçlidir**: "kayıt bulunamadı" ile "kayıt
/// bulundu ama boş" AYRI durumlardır. Boş liste döndüren bir dikiş ikisini tek
/// değere çökertirdi — ve gerçek kayıt BUGÜN boş olduğundan hiçbir test üretim
/// dikişinin canlı mı ölü mü olduğunu ayırt edemezdi (fail-open by construction).
/// Null-ayırt edilebilir şekil, `MissingRegistry_IsSilent` testini ve pozitif
/// `Assert.IsNotNull(...Load())` tripwire'ını mümkün kılan tek şeydir.
/// </summary>
internal interface IClueRegistrySource
{
    /// <summary>Kayıt asset'i; çözülemezse null.</summary>
    ClueRegistry Load();
}

/// <summary>
/// Üretim erişimi — her iki dikişi de aynı traversal üzerinden karşılar.
///
/// **Kayıt ADRES üzerinden bulunur, sabit yol sabitiyle DEĞİL.** Addressable
/// entry'si GUID'e bağlıdır: `ClueRegistry.asset` taşınır ya da yeniden
/// adlandırılırsa Unity entry'yi sessizce yeni yola repoint eder. Sabit bir yol
/// sabiti kullansaydık anahtar check'i (a) YEŞİL kalır, `LoadAssetAtPath` null
/// döner ve (b)(c)(d) SONSUZA DEK sessizleşirdi — bloklayıcı bir kapının içinde
/// fail-open. Adresten `entry.AssetPath`'e giderek çalışma zamanının
/// (`AnlatiDurumIpucuTakibi.EnsureRegistryLoaded`) yükleyeceği asset'in TA
/// KENDİSİNİ doğruluyoruz: tek doğruluk kaynağı.
///
/// Addressables 4.0.1'de adrese göre arama API'si YOKTUR (`FindAssetEntry` yalnız
/// GUID alır) — grup/entry gezinmesi tek yol.
/// </summary>
internal sealed class AddressableClueRegistryAccess : IAddressableKeyResolver, IClueRegistrySource
{
    internal static readonly AddressableClueRegistryAccess Default = new AddressableClueRegistryAccess();

    public bool KeyResolves(string key) => TryResolveAssetPath(key, out _);

    public ClueRegistry Load() =>
        TryResolveAssetPath(AnlatiDurumIpucuTakibi.ClueRegistryAddressableKey, out string assetPath)
            ? AssetDatabase.LoadAssetAtPath<ClueRegistry>(assetPath)
            : null;

    private static bool TryResolveAssetPath(string key, out string assetPath) =>
        AddressableKeySelection.TrySelectUnique(CollectCandidates(), key, out assetPath);

    /// <summary>
    /// Adayları düz veriye indirger — editor tipleri burada BİTER, seçim mantığı
    /// (`AddressableKeySelection`) saf kalır ve doğrudan test edilebilir.
    /// `AddressableAssetSettings` in-memory fabrikelenemediği için grup/entry
    /// gezinmesinin kendisi test dışıdır; kural kısmı değildir.
    /// </summary>
    private static List<AddressableEntryCandidate> CollectCandidates()
    {
        var candidates = new List<AddressableEntryCandidate>();

        // `Settings`, `EditorApplication.isUpdating`/`isCompiling` sırasında null
        // döner (paket XML doc'u). Boş aday listesi → çözülmedi → check bloklar:
        // yön fail-CLOSED, ama mesaj bu sebebi de saymak zorunda.
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || settings.groups == null)
        {
            return candidates;
        }

        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null || group.entries == null)
            {
                continue;
            }

            // `IncludeInBuild` Addressables 4.0.0'da GRUBA taşındı; schema'daki
            // aynı adlı property artık saf bir forwarder (`Group == null ||
            // Group.IncludeInBuild`) ve tek başına okunması SIFIR bilgi taşır —
            // üstelik `Group` null'sa koşulsuz `true` döndürerek fail-OPEN olur.
            if (!group.IncludeInBuild)
            {
                continue;
            }

            // Addressables'ın KENDİ kapısı iki bayrak: buildable schema ENABLED
            // olmalı, ve bundled grupta adres katalogda YER ALMALI. Biri kapalıysa
            // entry build'e girse bile `LoadAssetAsync(adres)` çalışma zamanında
            // `InvalidKeyException` atar — check'in var olma sebebi tam olarak bu.
            // (İkisi de Group Inspector'da tek tıkla kapatılabilir.)
            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            var contentDirectory = group.GetSchema<ContentDirectoryGroupSchema>();

            bool addressShipsInCatalog =
                (bundled != null && bundled.IsEnabled && bundled.IncludeAddressInCatalog) ||
                (contentDirectory != null && contentDirectory.IsEnabled);

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry != null)
                {
                    candidates.Add(new AddressableEntryCandidate(
                        entry.address, entry.AssetPath, addressShipsInCatalog));
                }
            }
        }

        return candidates;
    }
}

/// <summary>Bir Addressable entry'sinin seçim için gereken düz verisi.</summary>
internal readonly struct AddressableEntryCandidate
{
    internal AddressableEntryCandidate(string address, string assetPath, bool shipsInCatalog)
    {
        Address = address;
        AssetPath = assetPath;
        ShipsInCatalog = shipsInCatalog;
    }

    internal string Address { get; }

    internal string AssetPath { get; }

    /// <summary>Entry'nin adresi build edilmiş katalogda gerçekten yer alacak mı.</summary>
    internal bool ShipsInCatalog { get; }
}

/// <summary>
/// Anahtar seçim KURALI — saf, editor tipi içermez, doğrudan test edilir.
/// Grup/entry gezinmesi (`AddressableClueRegistryAccess.CollectCandidates`)
/// test edilemez; kural burada olduğu için ayırt edici testler mümkün.
/// </summary>
internal static class AddressableKeySelection
{
    /// <summary>
    /// TAM OLARAK BİR ship eden aday bu adresi taşımalı. Sıfır → çözülmedi.
    /// İkiden fazla → çözülmedi: Addressables adres tekilliğini hiçbir yerde
    /// ZORLAMIYOR, ilk eşleşme sözlük sırasına bağlıdır ve check'in ortadan
    /// kaldırmak için var olduğu belirsizliğin ta kendisidir.
    /// </summary>
    internal static bool TrySelectUnique(
        IReadOnlyList<AddressableEntryCandidate> candidates, string key, out string assetPath)
    {
        assetPath = null;
        if (candidates == null)
        {
            return false;
        }

        string selected = null;
        int matchCount = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            AddressableEntryCandidate candidate = candidates[i];

            // Ordinal — çalışma zamanındaki Addressables anahtar eşlemesiyle aynı.
            if (!candidate.ShipsInCatalog ||
                !string.Equals(candidate.Address, key, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(candidate.AssetPath))
            {
                continue;
            }

            matchCount++;
            selected = candidate.AssetPath;
        }

        if (matchCount != 1)
        {
            return false;
        }

        assetPath = selected;
        return true;
    }
}

/// <summary>
/// (a) `"ClueRegistry"` Addressable anahtarı build-time çözülmeli (TR-anlati-009,
/// ADR-0007 Risks mitigasyonu). GDD'de karşılığı yoktur — bu bir mühendislik
/// güvencesidir: anahtar kırıksa çalışma zamanı yüklemesi başarısız olur,
/// `AnlatiDurumIpucuTakibi` süreç ömrü boyunca latch'lenir ve ipucu-kilitli
/// içerik sessizce hiç açılmaz (Story 003 Completion Notes, Karar 1).
///
/// Bu check "kayıt ULAŞILAMIYOR" vakasının TEK sahibidir ve bunu İKİ ADIMDA
/// yapar: anahtar çözülüyor mu, VE çözülen asset gerçekten bir `ClueRegistry` mi.
/// (b)(c)(d) ulaşılamayan kayıtta sessizce geçer — aynı arızayı dört kez
/// raporlamak suçluyu gizler.
///
/// **İkinci adım LP+QL gate bulgusudur**: yalnız `KeyResolves` sorulsaydı, adresi
/// `ClueRegistry` OLMAYAN bir asset'e işaret eden bir proje (a)'yı GEÇER, sonra
/// (b)(c)(d) kayıt null diye sessizce döner ve build fail-open olurdu — bloklayan
/// bir kapının içinde tam olarak olmaması gereken şey. Doc'un "tek sahibi" iddiası
/// ancak bu adımla DOĞRU hâle geliyor.
/// </summary>
internal sealed class AnlatiClueRegistryKeyCheck : IBuildCheck
{
    private readonly IAddressableKeyResolver _keyResolver;
    private readonly IClueRegistrySource _registrySource;

    internal AnlatiClueRegistryKeyCheck()
        : this(AddressableClueRegistryAccess.Default, AddressableClueRegistryAccess.Default) { }

    internal AnlatiClueRegistryKeyCheck(IAddressableKeyResolver keyResolver, IClueRegistrySource registrySource)
    {
        _keyResolver = keyResolver;
        _registrySource = registrySource;
    }

    public string Name => "Anlati/ClueRegistryKeyResolves";

    public BuildCheckPhase Phase => BuildCheckPhase.AssetScan;

    public void Run(BuildCheckContext context)
    {
        string key = AnlatiDurumIpucuTakibi.ClueRegistryAddressableKey;

        if (!_keyResolver.KeyResolves(key))
        {
            context.Fail(
                $"'{key}' Addressable anahtarı çözülmüyor. Çalışma zamanında `ClueRegistry` " +
                "yüklenemez, ipucu takibi süreç ömrü boyunca devre dışı latch'lenir ve " +
                "ipucu-kilitli içerik SESSİZCE hiç açılmaz. Olası sebepler: asset Addressable " +
                $"değil; adresi TAM OLARAK \"{key}\" değil; aynı tipte İKİ entry aynı adresi " +
                "taşıyor (hangisinin çözüleceği belirsiz); grup build'e dahil değil " +
                "(Include In Build kapalı); grubun buildable schema'sı DEVRE DIŞI; " +
                "\"Include Addresses in Catalog\" kapalı (adres katalogda yer almaz, " +
                "çalışma zamanı InvalidKeyException atar); ya da Addressables ayarları " +
                "yüklenemedi (derleme/asset güncellemesi sırasında). (TR-anlati-009)");
        }

        if (_registrySource.Load() == null)
        {
            context.Fail(
                $"'{key}' adresi çözülüyor AMA işaret ettiği asset bir `ClueRegistry` değil " +
                "(ya da yüklenemiyor). Çalışma zamanı yüklemesi tip uyuşmazlığıyla düşer ve " +
                "içerik check'leri sessizce devre dışı kalır. Adresi doğru asset'e bağla " +
                "(TR-anlati-009).");
        }
    }
}

/// <summary>
/// (b) Hiçbir `ClueDefinition`'ın `RequiredShiftIds`'i boş olamaz (GDD AC8a).
/// `Definitions` içindeki null slot'ları da bu check sahiplenir.
///
/// **Kapsam kilidi**: yalnız `ClueRegistry.Definitions` okunur, ASLA
/// `AssetDatabase.FindAssets&lt;ClueDefinition&gt;` değil (ADR-0007: "her
/// ClueDefinition referenced by ClueRegistry.Definitions").
/// Kilidi asıl uygulayan mekanizma testteki bir assert değil, fixture rejimidir:
/// testler `ScriptableObject.CreateInstance` kullanır ve bu instance'lar
/// `FindAssets`'e GÖRÜNMEZDİR — proje-geneli taramaya sürüklenen bir check
/// hiçbir şey bulamaz ve "fırlatmalı" testleri fırlatmaz olur (kırmızı).
/// </summary>
internal sealed class AnlatiRequiredShiftIdsCheck : IBuildCheck
{
    private readonly IClueRegistrySource _registrySource;

    internal AnlatiRequiredShiftIdsCheck() : this(AddressableClueRegistryAccess.Default) { }

    internal AnlatiRequiredShiftIdsCheck(IClueRegistrySource registrySource) => _registrySource = registrySource;

    public string Name => "Anlati/RequiredShiftIdsNotEmpty";

    public BuildCheckPhase Phase => BuildCheckPhase.AssetScan;

    public void Run(BuildCheckContext context)
    {
        ClueRegistry registry = _registrySource.Load();
        if (registry == null)
        {
            return; // "kayıt yok" vakasının sahibi check (a).
        }

        string violation = AnlatiContentValidation.FindEmptyRequiredShiftIdsViolation(registry.Definitions);
        if (violation != null)
        {
            context.Fail(violation);
        }
    }
}

/// <summary>
/// (c) Aynı `ClueId` iki kayıtta geçemez (GDD AC8b). Boş/whitespace `ClueId` de
/// burada yakalanır — çift gruplamadan ÖNCE, yoksa iki boş kayıt yanıltıcı bir
/// "çift clueId ''" mesajı üretirdi.
///
/// (b) ile aynı AssetScan fazında ve aynı paylaşılan çekirdeği kullanır
/// (`AnlatiContentValidation`) — GDD'nin "aynı geçişte" ifadesi FAZI kastediyor,
/// tek metodu değil; story AC-5 dört ayrı kayıt istiyor.
/// </summary>
internal sealed class AnlatiUniqueClueIdCheck : IBuildCheck
{
    private readonly IClueRegistrySource _registrySource;

    internal AnlatiUniqueClueIdCheck() : this(AddressableClueRegistryAccess.Default) { }

    internal AnlatiUniqueClueIdCheck(IClueRegistrySource registrySource) => _registrySource = registrySource;

    public string Name => "Anlati/UniqueClueIds";

    public BuildCheckPhase Phase => BuildCheckPhase.AssetScan;

    public void Run(BuildCheckContext context)
    {
        ClueRegistry registry = _registrySource.Load();
        if (registry == null)
        {
            return; // "kayıt yok" vakasının sahibi check (a).
        }

        string violation = AnlatiContentValidation.FindDuplicateClueIdViolation(registry.Definitions);
        if (violation != null)
        {
            context.Fail(violation);
        }
    }
}

/// <summary>
/// (d) AC22 çaprazı (TR-isik-021, isik-volume Story 006'dan DEVRALINDI):
/// `TriggerMode=Automatic` bir bölgenin `shiftId`'si hiçbir
/// `ClueDefinition.requiredShiftIds`'inde yer ALAMAZ. Gerekçe: pasif/ambiyans bir
/// bölge oyuncunun içinden geçmesiyle kendiliğinden tetiklenir; ipucu kazanımının
/// `ManualOnly` RIZA ön koşulunu sessizce atlatırdı.
///
/// **Neden `IsikVolumeAutomaticPresenceCheck`'e KATLANMADI** (isik dosyasındaki
/// eski "bu eve eklenecek" notunun aksine): o check ilk Automatic bölgeyi bulunca
/// `return` ediyor — AC21 için doğru (varlık iddiası), ama buraya katlansaydı
/// sahnedeki İKİNCİ ve sonraki Automatic bölgeler SESSİZCE hiç incelenmezdi.
///
/// **Neden `IBuildCheckAggregate` DEĞİL**: aggregate, sahneler-ARASI TOPLAM
/// iddialar içindir. AC22 ayrışır — "birleşimde ihlal var" ⟺ "bir sahnede ihlal
/// var" — yani sahne başına değerlendirmek hiçbir bilgi kaybetmez. AC21 ise
/// ayrışmaz ("hiçbir sahnede yok" ancak yürüyüş bitince bilinir), aggregate
/// olmasının sebebi budur. Bedava kazanç: `context.ScenePath` `Run` içinde
/// DOLUDUR, yani hata mesajı suçlu sahneyi taşır (`FinalizeWalk` null alır).
///
/// **TÜM Automatic bölgeler gezilir** — `First()`/`Single()` YASAK:
/// `TriggerMode.Automatic` enum'un VARSAYILAN değeri (0), yani bir sahnede birden
/// çok Automatic bölge olması normaldir (isik GDD "en az bir" bir ALT sınırdır).
///
/// **BİLİNEN KÖR NOKTA**: `FindZones()` = `FindObjectsByType`, yalnız AÇIK
/// sahneyi görür. Yalnız bir PREFAB asset'i içinde yaşayan ya da
/// `EditorBuildSettings.scenes`'te bulunmayan/disabled bir sahnedeki
/// ipucu-taşıyan Automatic bölge bu check'e GÖRÜNMEZ. Mevcut beş sahne-scan
/// check'inin hepsi aynı sınırı paylaşıyor; kapatmak ayrı bir iş kalemidir.
/// </summary>
internal sealed class AnlatiAutomaticZoneNotClueBearingCheck : IBuildCheck
{
    private readonly IClueRegistrySource _registrySource;

    internal AnlatiAutomaticZoneNotClueBearingCheck() : this(AddressableClueRegistryAccess.Default) { }

    internal AnlatiAutomaticZoneNotClueBearingCheck(IClueRegistrySource registrySource) =>
        _registrySource = registrySource;

    public string Name => "Anlati/AutomaticZoneNotClueBearing";

    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    public void Run(BuildCheckContext context)
    {
        // Kayıt instance'a CACHE'LENMEZ: check instance'ları registry'de
        // static readonly ve tüm build'ler/testler arasında paylaşılır — orada
        // park eden her şey sızar (IBuildCheck doc'u bu tehlikeyi yazıyor).
        // Maliyet: her çağrı TAM bir grup/entry yürüyüşü yapar (erken çıkış yok,
        // belirsizlik tespiti için gerekli) + `LoadAssetAtPath`. Build başına
        // N_sahne+4 tam tarama; mevcut ölçekte (tek grup, tek entry) ihmal
        // edilebilir, katalog büyürse yeniden değerlendirilmeli.
        ClueRegistry registry = _registrySource.Load();
        if (registry == null)
        {
            return; // "kayıt yok" vakasının sahibi check (a).
        }

        foreach (ShiftZone zone in IsikVolumeBuildCheckUtil.FindZones())
        {
            if (zone._triggerMode != TriggerMode.Automatic)
            {
                continue;
            }

            string shiftId = zone._shiftId;
            if (string.IsNullOrWhiteSpace(shiftId))
            {
                // Boş shiftId çalışma zamanında da hiçbir ipucuna eşleşemez
                // (ProcessHeldShift'in IsNullOrWhiteSpace guard'ı) — atlamak doğru.
                continue;
            }

            List<string> offenders = FindCluesRequiring(registry.Definitions, shiftId);
            if (offenders.Count > 0)
            {
                context.Fail(
                    $"'{zone.name}' bölgesi TriggerMode=Automatic ve shiftId='{shiftId}' — bu shiftId " +
                    $"şu ipucu tanım(lar)ında isteniyor: {string.Join(", ", offenders)}. Automatic bölge " +
                    "oyuncunun içinden geçmesiyle kendiliğinden tetiklenir; ipucu kazanımının ManualOnly " +
                    "RIZA ön koşulunu sessizce atlatır. Bölgeyi ManualOnly yap ya da shiftId'yi ipucu " +
                    "tanımından çıkar (TR-isik-021, isik GDD AC22).");
            }
        }
    }

    /// <summary>Verilen shiftId'yi isteyen TÜM tanımların etiketleri — ilkinde durmaz.</summary>
    private static List<string> FindCluesRequiring(IReadOnlyList<ClueDefinition> definitions, string shiftId)
    {
        var offenders = new List<string>();
        if (definitions == null)
        {
            return offenders;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            ClueDefinition definition = definitions[i];
            if (definition == null || definition.RequiredShiftIds == null)
            {
                continue;
            }

            IReadOnlyList<string> required = definition.RequiredShiftIds;
            for (int r = 0; r < required.Count; r++)
            {
                // Ordinal — çalışma zamanındaki ters indeks de ordinal eşliyor.
                if (string.Equals(required[r], shiftId, StringComparison.Ordinal))
                {
                    offenders.Add($"'{definition.name}' (clueId='{definition.ClueId}')");
                    break;
                }
            }
        }

        return offenders;
    }
}
