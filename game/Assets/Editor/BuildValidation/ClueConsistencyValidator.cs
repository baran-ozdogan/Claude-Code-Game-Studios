using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Hiçbir tetikleyicinin ateşlemeyeceği bir `(clueId, shiftId)` çifti.</summary>
internal readonly struct OrphanedClueReference
{
    internal OrphanedClueReference(string clueId, string shiftId)
    {
        ClueId = clueId;
        ShiftId = shiftId;
    }

    internal string ClueId { get; }

    internal string ShiftId { get; }
}

/// <summary>
/// Doğrulamanın İKİNCİ katmanı (ADR-0007; TR-anlati-008'in uyarı yarısı —
/// bloklayan yarısı Story 004): bir `requiredShiftIds` girdisi, Build
/// Settings'teki HİÇBİR sahnede karşılığı olmayan bir `shiftId`'ye işaret
/// ediyorsa o ipucu sonsuza kadar tamamlanamaz.
///
/// **BUILD'İ ENGELLEMEZ** — `context.Fail` ASLA çağrılmaz, yalnız
/// `Debug.LogWarning`. Bu bir içerik-yazımı uyarısıdır: çalışma zamanı davranışı
/// bozulmaz (ipucu yalnız Unknown kalır), ve içerik henüz tamamlanmamışken
/// build almak MEŞRU bir iş akışıdır (GDD AC8).
///
/// ## MEKANİZMA SAPMASI (kullanıcı kararı, 2026-08-10) — SİLMEYİN
///
/// ADR-0007 bunu `ClueConsistencyValidator.ValidateScene(sceneId)` +
/// `EditorSceneManager.sceneOpened`/`sceneSaved` ile tarif ediyordu: **tek sahne**
/// gören bir tetikleyici. GDD'nin iddiası ise proje geneli ("HİÇBİR
/// tetikleyicinin ateşlemediği"). MVP'de Depot ve Ballroom AYRI sahneler ve
/// `ClueRegistry` merkezî/sahne-üstü bir kayıt — Depot açıkken Ballroom'un
/// `shiftId`'sini isteyen her MEŞRU clue yanlışlıkla "orphaned" görünürdü.
/// Tek-sahne bir mekanizma GDD'nin proje-geneli iddiasını YAPISAL OLARAK veremez.
///
/// Bu yüzden kontrol, isik-volume Story 006'nın tam bu iş için kurduğu
/// `IBuildCheckAggregate` desenine taşındı: runner zaten TÜM Build-Settings
/// sahnelerini geziyor, `shiftId`'ler birleştiriliyor, yürüyüş sonunda tek
/// seferde değerlendiriliyor. ADR'ın İKİ KISITI da korunuyor — non-blocking, ve
/// player build'ine girmiyor (build pipeline'ı Editor tarafıdır). Sapan tek şey
/// TETİKLEYİCİ. **ADR-0007 addendum'u bekliyor.**
///
/// ADR'ın literal metnine "geri düzeltmek" yanlış-pozitif uyarı sorununu geri
/// getirir. `MultiSceneClue_DoesNotWarn` testi tam olarak bunu bekliyor.
/// </summary>
internal sealed class ClueConsistencyValidator : IBuildCheck, IBuildCheckAggregate
{
    private readonly IClueRegistrySource _registrySource;

    // Yürüyüş gözlemi. Instance registry'de static readonly ve paylaşılıyor —
    // bu yüzden BeginWalk'ın sıfırlaması SÖZLEŞME (aşağıdaki gerekçeye bak).
    private readonly HashSet<string> _seenShiftIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<OrphanedClueReference> _orphaned = new List<OrphanedClueReference>();
    private int _scenesWalked;

    internal ClueConsistencyValidator() : this(AddressableClueRegistryAccess.Default) { }

    internal ClueConsistencyValidator(IClueRegistrySource registrySource) => _registrySource = registrySource;

    public string Name => "Anlati/OrphanedShiftId";

    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    /// <summary>
    /// GDD'nin adlandırdığı sorgu yüzeyi. İleride bir editör penceresi
    /// tüketebilir; bugün tek ZORUNLU gözlemlenebilir çıktı `Debug.LogWarning`.
    ///
    /// **SON YÜRÜYÜŞÜN durumunu döner, "son TAMAMLANMIŞ yürüyüşün" değil**:
    /// `BeginWalk` listeyi temizlediği için, ortada patlayan bir yürüyüşten sonra
    /// BOŞ döner. KOPYA döner — canlı liste dönseydi çağıranın elindeki sonuç bir
    /// sonraki build'in `BeginWalk`'ında altından temizlenirdi.
    /// </summary>
    internal IReadOnlyList<OrphanedClueReference> GetOrphanedClueIds() =>
        new List<OrphanedClueReference>(_orphaned);

    /// <summary>
    /// Sıfırlama yürüyüşün BAŞINDA — sonda self-reset DEĞİL. Gerekçe (isik-volume
    /// Story 006'nın LP bulgusu): başka bir check yürüyüşü ORTADA patlatırsa
    /// `FinalizeWalk` hiç koşmaz; sondaki bir sıfırlama bayat gözlemi SONRAKİ
    /// build'e sızdırır ve kontrol yanlış sonuç verir.
    /// </summary>
    public void BeginWalk()
    {
        _seenShiftIds.Clear();
        _orphaned.Clear();
        _scenesWalked = 0;
    }

    public void Run(BuildCheckContext context)
    {
        _scenesWalked++;

        foreach (ShiftZone zone in IsikVolumeBuildCheckUtil.FindZones())
        {
            // Boş shiftId'li bir bölge çalışma zamanında da hiçbir ipucuna
            // eşleşemez (`ProcessHeldShift`'in guard'ı) — kümeye almak yanlış
            // pozitif DEĞİL, yanlış NEGATİF üretirdi.
            if (!string.IsNullOrWhiteSpace(zone._shiftId))
            {
                _seenShiftIds.Add(zone._shiftId);
            }
        }
    }

    public void FinalizeWalk(BuildCheckContext context)
    {
        // Sıfır sahne gezildiyse SESSİZ: "hiç sahne yok" ile "shiftId hiçbir
        // sahnede yok" aynı şey değil. Henüz seviye sahnesi olmayan bir projede
        // her requiredShiftId orphaned görünür ve uyarı seli anlamsızlaşırdı.
        if (_scenesWalked == 0)
        {
            return;
        }

        ClueRegistry registry = _registrySource.Load();
        if (registry == null)
        {
            return; // "kayıt ulaşılamıyor" vakasının sahibi Anlati/ClueRegistryKeyResolves.
        }

        IReadOnlyList<ClueDefinition> definitions = registry.Definitions;
        if (definitions == null)
        {
            return;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            ClueDefinition definition = definitions[i];
            if (definition == null || definition.RequiredShiftIds == null)
            {
                continue; // null slot'un sahibi Anlati/RequiredShiftIdsNotEmpty (bloklayan).
            }

            // Aynı tanım içindeki YİNELENEN girdi tek kez raporlanır:
            // `ClueDefinition`'ın kendi tooltip'i yinelenen girdiyi "zararsız"
            // diye yazıyor (çalışma zamanında da ters indeks kova başına tekil
            // tutuyor), yani `["x", "x"]` yazılmış içerikte MEŞRU bir şekildir ve
            // iki özdeş uyarı basmak gürültüden başka bir şey değildir.
            var reportedForThisDefinition = new HashSet<string>(StringComparer.Ordinal);

            IReadOnlyList<string> required = definition.RequiredShiftIds;
            for (int r = 0; r < required.Count; r++)
            {
                string shiftId = required[r];

                // Boş girdi: Story 004'ün bloklayan check'i bunu BİLEREK geçiriyor
                // (liste boş değil), ama hiçbir bölge onu ateşleyemez — yani
                // "ulaşılamaz clue" sınıfının ta kendisi ve buraya AİT.
                bool orphaned = string.IsNullOrWhiteSpace(shiftId) || !_seenShiftIds.Contains(shiftId);
                if (!orphaned || !reportedForThisDefinition.Add(shiftId ?? string.Empty))
                {
                    continue;
                }

                _orphaned.Add(new OrphanedClueReference(definition.ClueId, shiftId));

                Debug.LogWarning(
                    $"[Anlati] '{definition.name}' (clueId='{definition.ClueId}') " +
                    $"shiftId='{shiftId}' istiyor ama Build Settings'teki HİÇBİR sahnede o " +
                    "shiftId'yi taşıyan bir ShiftZone yok — bu ipucu asla tamamlanamaz. " +
                    "Build ENGELLENMEDİ (içerik henüz yazılıyor olabilir); yazım bitince " +
                    "shiftId'yi düzelt ya da bölgeyi ekle (TR-anlati-008, GDD AC8).");
            }
        }
    }
}
