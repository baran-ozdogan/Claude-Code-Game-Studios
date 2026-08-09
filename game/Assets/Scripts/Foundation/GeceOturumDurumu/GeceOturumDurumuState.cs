using System;
using System.Collections.Generic;

/// <summary>
/// Night/session bookkeeping implementation (ADR-0006) — plain C# class, no
/// Unity lifecycle. Session facts are write-once-per-fact: nothing is removed
/// or cleared within a night; the ONLY clear path is ResetOnLoad (in-place,
/// called by FoundationBootstrap.ResetAll at session start).
///
/// Mutation paths (ADR-0006, corrected by ADR-0014): EndSession(),
/// SetRoundState(), SetTotalConfiguredTriggerCountForNight(), AddFiredTrigger()
/// (all internal, single-caller by convention), plus the OnShiftStateChanged
/// handler's Persistent (Shifting-In) and Settled (Held, Fired-gated) writes.
/// </summary>
public sealed class GeceOturumDurumuState : IGeceOturumDurumuState
{
    private readonly HashSet<string> _fired = new HashSet<string>();
    private readonly HashSet<string> _settled = new HashSet<string>();
    private readonly Dictionary<string, bool> _persistent = new Dictionary<string, bool>();

    // Işık/Volume'un IsShiftPersistent sorgusu — event Persistent bilgisini
    // TAŞIMAZ (quick-spec N2 düzeltmesi), bu senkron sorgu zorunlu. Story 004
    // gerçek facade'a bağlar; testler sahte delegate enjekte eder. Delegate
    // in-place reset'i atlatır (process ömrü boyunca bir kez bağlanır).
    private Func<string, bool> _isShiftPersistent = _ => false;

    /// <summary>Story 004'ün (ve testlerin) bağlama noktası — process başına bir kez.</summary>
    internal void BindIsShiftPersistentQuery(Func<string, bool> query) =>
        _isShiftPersistent = query ?? (_ => false);

    public bool IsSessionActive { get; private set; } = true;

    // MVP: single night — constant 1 (Çoklu Gece İlerlemesi is out of MVP scope).
    public int CurrentNightNumber => 1;

    public int CurrentRoundIndex { get; private set; }
    public int TotalRoundCount { get; private set; }

    public bool HasFired(string shiftId) => _fired.Contains(shiftId);
    public bool HasSettled(string shiftId) => _settled.Contains(shiftId);
    public bool IsPersistent(string shiftId) => _persistent.TryGetValue(shiftId, out bool persistent) && persistent;

    // Plain pass-throughs — no separate counter field, no drift possible (ADR-0006).
    public int FiredCount => _fired.Count;
    public int SettledCount => _settled.Count;

    public int TotalConfiguredTriggerCountForNight { get; private set; }

    /// <summary>Yalnız Sahne Kesmeli Anlatı çağırır (QQ-03 konvansiyonu). Idempotent, geri dönüşsüz.</summary>
    public void EndSession() => IsSessionActive = false;

    public event Action<string> OnTriggerFired;
    public event Action<string> OnTriggerSettled;

    /// <summary>
    /// Direct Fired write — yalnız Anı-Tetikleyici Etkileşim'in OnHoldComplete()'i
    /// çağırır (ADR-0014; konvansiyon + code review). Idempotent (HashSet.Add);
    /// OnTriggerFired yalnız İLK eklemede, aynı karede senkron fırlar.
    /// NOT written by the OnShiftStateChanged subscription — that handler writes
    /// Persistent (Shifting-In) and Settled (Held) only; Fired's direct write is
    /// what keeps the Automatic ambient zone's id out of the Settled gate.
    /// </summary>
    internal void AddFiredTrigger(string shiftId)
    {
        if (_fired.Add(shiftId))
        {
            OnTriggerFired?.Invoke(shiftId);
        }
    }

    /// <summary>
    /// Atomik çift-alan round yazımı — yalnız Görev/Taşıma Döngüsü'nün round
    /// geçişi çağırır (ADR-0006 QQ-03 konvansiyonu). currentRoundIndex 0-based;
    /// totalRoundCount gece boyunca sabittir (TaskList.Length).
    /// </summary>
    internal void SetRoundState(int currentRoundIndex, int totalRoundCount)
    {
        CurrentRoundIndex = currentRoundIndex;
        TotalRoundCount = totalRoundCount;
    }

    /// <summary>
    /// Gece-başı tek yazım — yalnız gece-başı orkestratörü
    /// (SahneKesmeliAnlatiController, ADR-0015) StartNight öncesinde çağırır.
    /// Build-time eşitlik kontrolü (gerçek MemoryTriggerDef asset sayısıyla)
    /// paylaşılan build-validation utility'sinde yaşar (ani-tetikleyici epic'i).
    /// </summary>
    internal void SetTotalConfiguredTriggerCountForNight(int count)
    {
        TotalConfiguredTriggerCountForNight = count;
    }

    /// <summary>
    /// OnShiftStateChanged handler'ının saf mantığı (quick-spec Core Rules):
    /// • Shifting-In → aynı karede IsShiftPersistent(shiftId) sorgusu; true ise
    ///   PersistentShiftIds kaydı HEMEN (Held beklenmez — ~3sn'lik boşluk kapanır).
    /// • Held → yalnız id FiredTriggerIds'teyse Settled'a ekle + OnTriggerSettled
    ///   tam bir kez (Fired kapısı: Automatic ambient zone id'leri Settled'a sızamaz;
    ///   doygunluk her zaman SettledCount üzerinden — 2026-08-04 saturation-timing
    ///   düzeltmesi). SettledCount, FiredCount'a ~3sn gecikmeyle yetişir; geçici
    ///   SettledCount &lt; FiredCount penceresi hata değildir.
    /// Fired'a BURADAN asla yazılmaz (AddFiredTrigger'ın tek-yol notu).
    /// Story 004 bu metodu gerçek OnShiftStateChanged aboneliğine bağlar.
    /// </summary>
    internal void ProcessShiftStateChanged(string shiftId, ShiftState newState)
    {
        if (newState == ShiftState.ShiftingIn)
        {
            if (_isShiftPersistent(shiftId))
            {
                _persistent[shiftId] = true;
            }
        }
        else if (newState == ShiftState.Held)
        {
            if (_fired.Contains(shiftId) && _settled.Add(shiftId))
            {
                OnTriggerSettled?.Invoke(shiftId);
            }
        }
    }

    /// <summary>
    /// IN-PLACE session reset — clears every session fact on the SAME instance
    /// (event subscriptions survive; the instance is never replaced, ADR-0015
    /// regime) and explicitly re-initializes non-default fields the old
    /// replacement shape restored for free — notably IsSessionActive = true.
    /// Called only via GeceOturumDurumu.ResetOnLoad ← FoundationBootstrap.ResetAll().
    /// </summary>
    internal void ResetOnLoad()
    {
        _fired.Clear();
        _settled.Clear();
        _persistent.Clear();
        CurrentRoundIndex = 0;
        TotalRoundCount = 0;
        TotalConfiguredTriggerCountForNight = 0;
        IsSessionActive = true;
    }
}
