using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tek bir ipucunun içerik tanımı (ADR-0007 Data model). `clueId`, bir ya da daha
/// fazla `requiredShiftIds` girdisinin TAMAMI Held'e ulaşınca bilinir — semantik
/// **ALL**'dur (GDD Core Rules, küme-kapsama kontrolü).
///
/// **`RequiredShiftIds` BOŞ OLAMAZ**: `SeenShiftIds ⊇ ∅` matematiksel olarak her
/// zaman doğru olduğundan, boş bir liste hiçbir tetikleyici ateşlenmeden ipucunu
/// "Known" sayardı — sessiz bir vacuous-truth hatası. Runtime'da clamp/varsayılan
/// YAPILMAZ; bu, edit-time doğrulamanın yakalayıp build'i engellediği bir tasarım
/// hatasıdır (Story 004, GDD AC8a).
///
/// **MVP notu**: game-concept'in 2-3 tetikleyicisiyle her liste tam olarak 1
/// eleman taşır — davranışsal olarak 1:1 — ama kod bunu VARSAYMAZ. Full Vision'da
/// (bir ipucunun oturması için 2 farklı anı parçası gerekmesi) yalnız listeye
/// ikinci bir `shiftId` eklenir; çalışma zamanı mantığı değişmez (GDD Core Rules).
///
/// Runtime state DEĞİLDİR — yalnız authored config (manifest: SO-backed runtime
/// state YASAK). Çalışma zamanında asla mutasyona uğratılmaz.
/// </summary>
// Menü kökü ASCII "Yankilar" — mevcut `ShiftConfig` ile AYNI string olmalı, yoksa
// Unity iki ayrı üst-seviye Create menüsü çizer (LP gate bulgusu).
[CreateAssetMenu(fileName = "ClueDefinition", menuName = "Yankilar/Anlati/Clue Definition")]
public sealed class ClueDefinition : ScriptableObject
{
    [SerializeField] private string _clueId;

    [Tooltip("Boş bırakılamaz — build-time doğrulama engeller (GDD AC8a). Yinelenen girdi zararsızdır.")]
    [SerializeField] private List<string> _requiredShiftIds = new List<string>();

    public string ClueId => _clueId;

    public IReadOnlyList<string> RequiredShiftIds => _requiredShiftIds;

#if UNITY_EDITOR
    /// <summary>
    /// Inspector-time erken geri bildirim (Story 004). YALNIZ kendi kendine
    /// yeten kuralı çağırır — çift `ClueId` tespiti buraya AİT DEĞİL: o,
    /// asset'ler ARASI bir sorgudur ve `OnValidate`'in tek-asset görüşünden
    /// yapılamaz (ADR-0014, cross-asset OnValidate yasağı). Onun sahibi
    /// `ClueRegistry.OnValidate` (bütün listeyi görür) ve build check (c).
    ///
    /// **`LogWarning`, `LogError` DEĞİL — bilinçli**: `SerializedObject`'in
    /// `ApplyModifiedPropertiesWithoutUndo()`'su `OnValidate`'i tetikler ve
    /// projenin test fixture'ları tam olarak bunu kullanarak KASTEN ihlalli
    /// tanımlar kurar (`anlati_clue_definition_test.cs`'in vacuous-truth ve
    /// blank-clueId testleri). Unity Test Framework beklenmeyen bir `LogError`'ı
    /// test BAŞARISIZLIĞI sayar; `LogError` bugün yeşil olan testleri kırardı.
    /// Bloklayan kapı zaten build check'idir — burası yalnız erken uyarı.
    /// </summary>
    private void OnValidate()
    {
        string violation = AnlatiContentValidation.FindEmptyRequiredShiftIdsViolation(this);
        if (violation != null)
        {
            Debug.LogWarning($"[Anlati] {violation}", this);
        }
    }
#endif
}
