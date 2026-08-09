/// <summary>
/// Proje geneli epsilon sabitleri — TEK statik ev (isik-volume GDD Formulas,
/// systems-designer round 3: "epsilon" kelimesi iki tur boyunca sayısız kaldı,
/// implementer-uyuşmazlığı riskine karşı burada sabitlendi). Yeni bir epsilon
/// ihtiyacı doğarsa buraya eklenir; sistem-lokal kopya tanımlamak yasak
/// (manifest: "epsilon sabitleri proje geneli tek yerde").
/// </summary>
public static class ProjectEpsilon
{
    /// <summary>Süre değerleri için (s) — ör. Duration'ın sıfıra bölünme guard'ı.</summary>
    public const float TIME_EPSILON = 0.01f;

    /// <summary>Yarıçap değerleri için (m) — ör. R_trigger'ın ölü-bölge guard'ı.</summary>
    public const float RADIUS_EPSILON = 0.01f;

    /// <summary>Birimsiz k_hysteresis faktörü için — k=1.0 dejenerasyonunu da dışlar.</summary>
    public const float HYSTERESIS_EPSILON = 0.001f;
}
