using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Etkileşimli nesnelerin ortak kayıt defteri (ADR-0004 — birebir Key
/// Interfaces şekli). Çıplak statik sınıf, ADR-0001'in interface/facade
/// deseninin BİLİNÇLİ istisnası: `_live` OnEnable/OnDisable üzerinden kendi
/// kendini düzeltir (Awake'e hiç güvenmez), reset hook'a ihtiyacı yok. Yalnız
/// kare-snapshot cache'i (`_frameSnapshot`/`_snapshotFrame`) session-scoped'dur
/// ve FoundationBootstrap'a kayıtlı bir ResetOnLoad() gerektirir — cross-session
/// Time.frameCount çakışması riski (unity-specialist BLOCKING bulgusu, ADR-0004
/// Risks). List (HashSet değil) bilinçli: insertion order korunur, gelecekte
/// Etkileşim Core'un tie-break kuralına (en küçük hit.distance, sonra en küçük
/// InstanceID) deterministik bir taban sağlar.
///
/// UYARI — bu deseni kopyalama: lazy-once-per-frame cache, farklı tutarlılık
/// gereksinimleri olan bir sistem için (ör. "bu karenin İLK snapshot'ı otoriter
/// olmalı, geç çağıran tarafından hesaplansa bile") yanlış sonuç verir — bu
/// registry'nin gerçek tüketicileri için sorun değil (ADR-0004 Consequences).
/// Yalnız ana iş parçacığından, Update-döngüsü zamanlamasında çağrılmalı;
/// FixedUpdate/arka plan iş parçacığı kullanımı bu varsayımı bozar (ADR Risks).
/// </summary>
public static class InteractableRegistry
{
    // Self-correcting via OnEnable/OnDisable — no reset hook needed.
    private static readonly List<IInteractable> _live = new List<IInteractable>();

    // Session-surviving cache — DOES need a reset hook. internal (ADR taslağında
    // private) — Story 002'nin PlayMode testinin cross-session cache-çakışmasını
    // doğrudan kurabilmesi için BİLİNÇLİ sapma (QL-STORY-READY gate kararı).
    internal static IInteractable[] _frameSnapshot = Array.Empty<IInteractable>();
    internal static int _snapshotFrame = -1;

    /// <summary>Yalnız OnEnable'dan çağrılır (manifest kuralı, ADR-0004/0014).</summary>
    public static void Register(IInteractable interactable) => _live.Add(interactable);

    /// <summary>Yalnız OnDisable'dan çağrılır (manifest kuralı, ADR-0004/0014).</summary>
    public static void Deregister(IInteractable interactable) => _live.Remove(interactable);

    /// <summary>
    /// Kare-başı salt-okunur kopya — canlı koleksiyon asla döndürülmez (`_live`
    /// asla döndürülen referans değildir, dönen koleksiyonu mutasyona uğratmak
    /// `_live`'ın geleceğini değiştiremez). Kare içinde tekrar çağrılırsa AYNI
    /// cache'lenmiş referansı döner — deregister bile aynı-kare yeniden
    /// hesaplamayı tetiklemez (lazy, en çok kare başına bir kez).
    /// </summary>
    public static IReadOnlyList<IInteractable> Snapshot()
    {
        if (_snapshotFrame != Time.frameCount)
        {
            _frameSnapshot = _live.ToArray();
            _snapshotFrame = Time.frameCount;
        }
        return _frameSnapshot;
    }

    /// <summary>
    /// Yalnız FoundationBootstrap.ResetAll() çağırır. YALNIZ cache alanlarını
    /// temizler — `_live` BİLEREK dokunulmaz: bu çağrı anında zaten doğrudur,
    /// çünkü şu an aktif her IInteractable'ın OnEnable'ı bu oturumda onu
    /// zaten yeniden kaydetmiştir.
    /// </summary>
    internal static void ResetOnLoad()
    {
        _frameSnapshot = Array.Empty<IInteractable>();
        _snapshotFrame = -1;
    }
}
