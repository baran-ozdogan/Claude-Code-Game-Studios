using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projedeki TÜM `ClueDefinition` kayıtlarının TEK merkezi listesi (ADR-0007;
/// GDD Core Rules "merkezi kayıt"). Sahne başına kopyalanmış asset'ler DEĞİL —
/// tek doğruluk kaynağı olması, aynı `clueId`'nin iki farklı sahnede tutarsız
/// tanımlanmasını YAPISAL olarak imkânsız kılar (ADR-0007 Alternative 2, GDD
/// seviyesinde zaten reddedilmiş bir alternatif).
///
/// Addressable olarak işaretlenir, anahtar TAM OLARAK `"ClueRegistry"` — Story
/// 003 onu lazy yükler, Story 004 anahtarın build-time çözülebilirliğini
/// doğrular.
///
/// Runtime state DEĞİLDİR (manifest: SO-backed runtime state YASAK). Ters indeks
/// bu listeden BİR KEZ kurulur; liste çalışma zamanında mutasyona uğratılırsa
/// indeks sessizce bayatlar — hiçbir tüketici bunu yapmamalıdır (ADR-0007 Risks).
/// </summary>
[CreateAssetMenu(fileName = "ClueRegistry", menuName = "Yankilar/Anlati/Clue Registry")]
public sealed class ClueRegistry : ScriptableObject
{
    [Tooltip("Projedeki tüm ClueDefinition'lar. Aynı clueId iki kez geçemez — build-time doğrulama engeller (GDD AC8b).")]
    [SerializeField] private List<ClueDefinition> _definitions = new List<ClueDefinition>();

    /// <summary>Boş bir asset'te de BOŞ ve non-null döner — çağıranlar null kontrolü yapmaz.</summary>
    public IReadOnlyList<ClueDefinition> Definitions => _definitions;
}
