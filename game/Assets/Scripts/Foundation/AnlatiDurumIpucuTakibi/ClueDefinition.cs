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
}
