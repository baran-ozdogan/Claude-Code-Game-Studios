/// <summary>
/// Static facade for the night/session bookkeeping service (ADR-0001 pattern,
/// first real consumer — ADR-0006). Production code reads
/// GeceOturumDurumu.Instance; tests construct a fresh GeceOturumDurumuState
/// directly and never touch this facade.
/// </summary>
public static class GeceOturumDurumu
{
    // Never replaced (ADR-0015 in-place regime) — reset clears fields on this
    // same instance, so event subscriptions and the static-init Işık/Volume
    // subscription below bind once per process and survive every ResetAll().
    private static readonly GeceOturumDurumuState _current = new GeceOturumDurumuState();

    // Işık/Volume wiring (Story 004, ADR-0006): static-init'te TAM BİR KEZ
    // kurulur, process ömrü boyunca yaşar — iki facade de in-place reset
    // (ADR-0015), re-wire yok, accumulation yok. Reset sırası bu yüzden
    // Işık/Volume ÖNCE (FoundationBootstrap belgeli sırası). Sorgu delegate'i
    // Instance'ı kullanım noktasında canlı dereference eder (manifest kuralı).
    static GeceOturumDurumu()
    {
        _current.BindIsShiftPersistentQuery(static shiftId =>
            IsikVolumeDurumSistemi.Instance.IsShiftPersistent(shiftId));

        IsikVolumeDurumSistemi.Instance.OnShiftStateChanged +=
            static (shiftId, newState, _, _) => _current.ProcessShiftStateChanged(shiftId, newState);
    }

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
