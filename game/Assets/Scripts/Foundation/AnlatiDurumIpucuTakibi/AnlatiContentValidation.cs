#if UNITY_EDITOR
using System;
using System.Collections.Generic;

/// <summary>
/// `ClueDefinition`/`ClueRegistry` içerik kurallarının PAYLAŞILAN doğrulama
/// çekirdeği (Story 004, GDD AC8a/AC8b). İki çağrı yeri vardır ve ikisi de
/// İNCE'dir: build-blocking check'ler (`AnlatiBuildChecks.cs`) ve `OnValidate`
/// (Inspector-time erken geri bildirim). Test edilen birim BU metotlardır —
/// çağrı yerleri reflection jimnastiğiyle ayrıca test EDİLMEZ (story AC-6).
///
/// **Metotlar FIRLATMAZ ve LOGLAMAZ** — ihlal mesajını (ya da ihlal yoksa null)
/// DÖNDÜRÜRLER. Zorunluluk: Foundation, `UnityEditor.Build.BuildFailedException`'ı
/// referans edemez ve `OnValidate` asla fırlatmamalıdır. Yan etki de gerekli
/// değil — sonucu assert etmek exception plumbing'inden basit.
///
/// **Neden Foundation'da, `#if UNITY_EDITOR` sarmalı içinde**: kural, tiplerin
/// kendi yanında yaşamalı (`OnValidate` da buradan çağırıyor), ama uzun Türkçe
/// düzeltme metinleri ve doğrulayıcının kendisi player build'ine GİRMEMELİ.
/// Sarmal, `BuildValidation` ve `EditModeTests`'in (ikisi de Editor-only)
/// görüşünü etkilemez.
///
/// **Karşılaştırma `StringComparison.Ordinal`** — çalışma zamanındaki
/// `AnlatiDurumState`'in `HashSet&lt;string&gt;`/`Dictionary&lt;string,…&gt;`
/// yapıları VARSAYILAN comparer'ı, yani ordinal kullanıyor. Burada
/// `OrdinalIgnoreCase`'e ya da `Trim`'e "iyileştirmek" doğrulamayı runtime'dan
/// AYIRIR ve yanlış-pozitif/yanlış-negatif üretir.
/// </summary>
internal static class AnlatiContentValidation
{
    /// <summary>
    /// Tek asset kuralı (GDD AC8a) — `ClueDefinition.OnValidate` ve check (b)
    /// ikisi de bunu çağırır. `SeenShiftIds ⊇ ∅` her zaman doğru olduğundan boş
    /// liste, hiçbir tetikleyici ateşlenmeden ipucunu "Known" sayardı.
    /// </summary>
    internal static string FindEmptyRequiredShiftIdsViolation(ClueDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        IReadOnlyList<string> required = definition.RequiredShiftIds;
        if (required != null && required.Count > 0)
        {
            return null;
        }

        return $"{Label(definition)} kaydının RequiredShiftIds listesi BOŞ. Boş liste, hiçbir " +
               "shift Held'e ulaşmadan ipucunu \"Known\" sayardı (SeenShiftIds ⊇ ∅ her zaman " +
               "doğrudur) — sessiz bir vacuous-truth hatası. En az bir shiftId yaz (GDD AC8a).";
    }

    /// <summary>
    /// Check (b)'nin liste hâli. Ayrıca `Definitions` içindeki NULL slot'ları da
    /// bu kural sahiplenir: çalışma zamanında `BuildReverseIndex` null girdileri
    /// SESSİZCE atlıyor, yani yazılmış bir ipucu izsiz kaybolurdu.
    ///
    /// Determinizm: null slot → boş liste sırasıyla, indeks sırasına göre ilk
    /// ihlal döner. (Sıra değişirse her mesaj iddiası sessizce değişir.)
    /// </summary>
    internal static string FindEmptyRequiredShiftIdsViolation(IReadOnlyList<ClueDefinition> definitions)
    {
        if (definitions == null)
        {
            return null;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] == null)
            {
                return $"ClueRegistry.Definitions[{i}] NULL. Çalışma zamanında ters indeks null " +
                       "girdileri sessizce atlar — yazılmış bir ipucu izsiz kaybolur. Boş slot'u " +
                       "sil ya da doldur.";
            }
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            string violation = FindEmptyRequiredShiftIdsViolation(definitions[i]);
            if (violation != null)
            {
                return violation;
            }
        }

        return null;
    }

    /// <summary>
    /// Check (c) — aynı `ClueId` iki kayıtta geçemez (GDD AC8b). Merkezi kaydın
    /// tek doğruluk kaynağı olması, aynı clueId'nin tutarsız tanımlanmasını
    /// yapısal olarak engellemek içindir; çift kayıt tam da o garantiyi bozar.
    ///
    /// Determinizm: BOŞ/whitespace `ClueId` çift-gruplamadan ÖNCE bildirilir —
    /// aksi hâlde iki boş kayıt yanıltıcı bir "çift clueId ''" mesajı üretirdi.
    /// Üç ya da daha fazla kayıt aynı ID'yi taşıyorsa İLK çakışan ÇİFT bildirilir.
    /// Null slot'lar burada ATLANIR — onların sahibi check (b).
    /// </summary>
    internal static string FindDuplicateClueIdViolation(IReadOnlyList<ClueDefinition> definitions)
    {
        if (definitions == null)
        {
            return null;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            ClueDefinition definition = definitions[i];
            if (definition != null && string.IsNullOrWhiteSpace(definition.ClueId))
            {
                return $"{Label(definition)} kaydının ClueId'si boş. Her ipucunun tekil, boş " +
                       "olmayan bir kimliği olmalı (GDD AC8b).";
            }
        }

        var seen = new Dictionary<string, ClueDefinition>(StringComparer.Ordinal);
        for (int i = 0; i < definitions.Count; i++)
        {
            ClueDefinition definition = definitions[i];
            if (definition == null)
            {
                continue;
            }

            if (seen.TryGetValue(definition.ClueId, out ClueDefinition first))
            {
                return $"'{definition.ClueId}' clueId'si İKİ kayıtta birden geçiyor: " +
                       $"{Label(first)} ve {Label(definition)}. Merkezi kayıt tek doğruluk " +
                       "kaynağıdır; çift ID onu bozar (GDD AC8b).";
            }

            seen[definition.ClueId] = definition;
        }

        return null;
    }

    /// <summary>
    /// `ClueRegistry.OnValidate`'in TEK giriş noktası — iki kuralı da aynı
    /// geçişte uygular (GDD "aynı geçişte"). Build tarafında kurallar AYRI
    /// `IBuildCheck` kayıtları olarak koşar (story AC-5 dört kayıt istiyor);
    /// "aynı geçiş" ifadesi AssetScan FAZINI kastediyor, tek metodu değil.
    /// </summary>
    internal static string FindContentViolation(IReadOnlyList<ClueDefinition> definitions) =>
        FindEmptyRequiredShiftIdsViolation(definitions) ?? FindDuplicateClueIdViolation(definitions);

    /// <summary>
    /// Suçluyu HEM asset adıyla HEM clueId'siyle adlandırır: asset adı yeniden
    /// adlandırmayla, clueId ise içerik düzenlemesiyle değişir — ikisi birlikte
    /// suçluyu her hâlükârda bulunur kılar.
    /// </summary>
    private static string Label(ClueDefinition definition) =>
        $"'{definition.name}' (clueId='{definition.ClueId}')";
}
#endif
