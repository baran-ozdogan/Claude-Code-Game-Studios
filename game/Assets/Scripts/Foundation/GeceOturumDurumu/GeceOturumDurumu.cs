/// <summary>
/// Static facade for the night/session bookkeeping service (ADR-0001 pattern,
/// first real consumer — ADR-0006). Production code reads
/// GeceOturumDurumu.Instance; tests construct a fresh GeceOturumDurumuState
/// directly and never touch this facade.
/// </summary>
public static class GeceOturumDurumu
{
    // Never replaced (ADR-0015 in-place regime) — reset clears fields on this
    // same instance, so event subscriptions and the (Story 004) constructor-time
    // Işık/Volume subscription bind once per process and survive every ResetAll().
    private static readonly GeceOturumDurumuState _current = new GeceOturumDurumuState();

    public static IGeceOturumDurumuState Instance => _current;

    /// <summary>
    /// Internal-only, concrete-typed accessor — reaches the internal write
    /// methods (SetRoundState — Görev/Taşıma only; AddFiredTrigger —
    /// Anı-Tetikleyici only; SetTotalConfiguredTriggerCountForNight — gece-başı
    /// orkestratörü only). Single-caller restrictions are convention + XML-doc +
    /// code review (QQ-03), not compiler-checked.
    /// </summary>
    internal static GeceOturumDurumuState InternalInstance => _current;

    /// <summary>Called only by FoundationBootstrap.ResetAll(), after Işık/Volume's reset.</summary>
    internal static void ResetOnLoad() => _current.ResetOnLoad();
}
