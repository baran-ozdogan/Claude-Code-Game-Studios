using System;

/// <summary>
/// Read surface of the night/session bookkeeping service (ADR-0006 Data model,
/// verbatim). Membership is exposed through query methods — never raw
/// collections (IReadOnlySet is not guaranteed under .NET Standard 2.1).
/// </summary>
public interface IGeceOturumDurumuState
{
    bool IsSessionActive { get; }
    int CurrentNightNumber { get; }

    /// <summary>0-based; written only by Görev/Taşıma Döngüsü's SetRoundState.</summary>
    int CurrentRoundIndex { get; }
    int TotalRoundCount { get; }

    bool HasFired(string shiftId);      // FiredTriggerIds membership
    bool HasSettled(string shiftId);    // SettledTriggerIds membership
    bool IsPersistent(string shiftId);  // PersistentShiftIds membership

    int FiredCount { get; }             // FiredTriggerIds.Count pass-through
    int SettledCount { get; }           // SettledTriggerIds.Count pass-through

    /// <summary>Written once at night begin (ADR-0014/0015 orchestrator); build-verified
    /// against the real MemoryTriggerDef asset count (check lives in the shared
    /// build-validation utility, ani-tetikleyici epic'i).</summary>
    int TotalConfiguredTriggerCountForNight { get; }

    /// <summary>Called exactly once per night, only by Sahne Kesmeli Anlatı's
    /// RequestHardCut onComplete path (QQ-03 convention — code-review enforced).
    /// Idempotent; no reverse within MVP scope.</summary>
    void EndSession();

    event Action<string> OnTriggerFired;
    event Action<string> OnTriggerSettled;
}
