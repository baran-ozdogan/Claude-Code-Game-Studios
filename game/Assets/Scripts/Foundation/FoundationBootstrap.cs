using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single reset entry point for every session-scoped static service (ADR-0001).
///
/// Every session-scoped-state module in this project follows the same three-part
/// shape — copy this template, never invent a new one (ADR-0001 Key Interfaces):
///
/// <code>
/// public interface I[System]State {
///     // read-only queries and event subscriptions — the real contract
/// }
///
/// public sealed class [System]State : I[System]State {
///     // plain C# class, owns all fields, no Unity object lifecycle of any kind
/// }
///
/// public static class [System] {
///     private static I[System]State _instance = new [System]State();
///     public static I[System]State Instance => _instance;
///
///     internal static void ResetOnLoad() => _instance.ResetOnLoad();
///     // IN-PLACE — clear fields on the SAME instance, never replace it
///     // (ADR-0011/0015 regime: constructor subscriptions bind once per process
///     // and survive every ResetAll(); in-place ResetOnLoad() must explicitly
///     // re-initialize any non-default field, e.g. an initializer-true bool).
///     // No [RuntimeInitializeOnLoadMethod] here — only FoundationBootstrap has it.
/// }
/// </code>
///
/// Tests NEVER touch a static facade: they construct a fresh [System]State
/// directly and inject it (ADR-0001 testability pattern).
/// </summary>
internal static class FoundationBootstrap
{
    /// <summary>One named reset step of the documented sequence.</summary>
    internal readonly struct ResetEntry
    {
        public readonly string Name;
        public readonly Action Reset;

        public ResetEntry(string name, Action reset)
        {
            Name = name;
            Reset = reset;
        }
    }

    // The documented dependency order (ADR-0001 Decision, "Reset ordering").
    // A service is reset only after every Foundation service it reads from
    // (directly, or via a constructor-time event subscription) has already
    // been reset. Each service's epic uncomments/adds its own line when the
    // service exists; a new service is inserted at the correct point in THIS
    // list — it never gets its own [RuntimeInitializeOnLoadMethod].
    //
    // Seviye/Sahne Geçişi is deliberately ABSENT (ADR-0008 exception):
    // SceneTransitionManager is a MonoBehaviour in the persistent "Foundation"
    // scene and resets via its own scene lifecycle, not FoundationBootstrap.
    // Do not "fix" this by adding it here.
    private static readonly ResetEntry[] _resetSequence =
    {
        new ResetEntry("InteractableRegistry", InteractableRegistry.ResetOnLoad), // no upstream dependency — interactable-registry Story 001
        new ResetEntry("IsikVolumeDurumSistemi", IsikVolumeDurumSistemi.ResetOnLoad), // exposes OnShiftStateChanged, subscribes to nothing; IN-PLACE (persistent MonoBehaviour + constructor subscribers) — isik-volume Story 001
        new ResetEntry("GeceOturumDurumu", GeceOturumDurumu.ResetOnLoad), // constructor-subscribes to Işık/Volume OnShiftStateChanged (binds once per process, ADR-0015; wiring: gece-oturum Story 004)
        // TODO(epic:anlati-durum-ipucu-takibi):  new ResetEntry("AnlatiDurumIpucuTakibi",  AnlatiDurumIpucuTakibi.ResetOnLoad),  // constructor-subscribes to Işık/Volume OnShiftStateChanged
        // TODO(epic:adaptif-ses-sistemi):        new ResetEntry("AdaptifSesSistemi",       AdaptifSesSistemi.ResetOnLoad),       // pure state (HeldSessionAlreadyPlayed) — playback lives in AdaptifSesController (ADR-0009)
        // TODO(epic:diyalog-anlati-icerigi):     new ResetEntry("DiyalogAnlatiIcerigi",    DiyalogAnlatiIcerigi.ResetOnLoad),    // UsedCallbackIds only (ADR-0012) — in-place Clear()
        // TODO(epic:asansor-kat-erisim-sistemi): new ResetEntry("ElevatorSystem",          ElevatorSystem.ResetOnLoad),          // ride state (ADR-0011) — in-place, events preserved
        // TODO(epic:gorev-tasima-dongusu):       new ResetEntry("GorevTasimaDongusu",      GorevTasimaDongusu.ResetOnLoad),      // carry/round state (ADR-0013) — delegates resolve facades at invocation time only
        // TODO(epic:sahne-kesmeli-anlati):       new ResetEntry("SahneKesmeliAnlati",      SahneKesmeliAnlati.ResetOnLoad),      // end-condition machine + NightBeginPending (ADR-0015)
    };

    /// <summary>Names of the currently active reset steps, in execution order — read by the ordering test.</summary>
    internal static IReadOnlyList<string> ActiveResetOrder
    {
        get
        {
            var names = new string[_resetSequence.Length];
            for (int i = 0; i < _resetSequence.Length; i++)
            {
                names[i] = _resetSequence[i].Name;
            }
            return names;
        }
    }

    /// <summary>
    /// How many times ResetAll has run in this process — read by the timing
    /// smoke tests (exactly once per Play session; with Reload Domain OFF the
    /// counter survives and increments across sessions, with it ON it restarts at 1).
    /// </summary>
    internal static int ResetCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        for (int i = 0; i < _resetSequence.Length; i++)
        {
            _resetSequence[i].Reset();
        }

        ResetCount++;
        Debug.Log($"[FoundationBootstrap] ResetAll #{ResetCount} — {_resetSequence.Length} service(s) reset (SubsystemRegistration).");
    }
}
