using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// `IPlayerState`'in tek implementasyonu (ADR-0003 — Key Interfaces bloğu
/// birebir). `FirstPersonController`'dan (Story 003) BİLEREK AYRI: test
/// edilebilirlik — `AddComponent&lt;PlayerStateProvider&gt;()` çıplak bir
/// GameObject'te, `CharacterController`/`Camera` gerekmeden kilit/durum
/// mantığını sınayabilir (ADR-0003 Alternative 1'in düzeltilmiş gerekçesi).
///
/// `Current` yalnız `Awake()`'te set edilir — `[RuntimeInitializeOnLoadMethod]`
/// reset YOK: Player sahnesi süreç başına bir kez yüklenir (proje-kurulumu
/// Story 004), ADR-0001'in "önceki oturumdan bayat veri" sorunu burada yok
/// (ADR-0003 Alternative 2'nin bilinçli reddi).
///
/// İLERİ BAYRAK (Story 003'e): UI ve Player kalıcı sahneleri eşzamanlı
/// yüklenir, Awake() sırası garanti değil (ADR-0003 Risks) — bu sınıf ve
/// FirstPersonController UI-sahne durumunu ASLA Awake()/OnEnable() içinde
/// OKUMAMALI.
/// </summary>
public sealed class PlayerStateProvider : MonoBehaviour, IPlayerState
{
    public static PlayerStateProvider Current { get; private set; }

    // IPlayerState'in "içeriden yazılır, dışarıdan salt-okunur" alanları —
    // FirstPersonController (Story 003) yazar, tüketiciler yalnız okur.
    public Transform EyeCamera { get; internal set; }
    public Vector3 Velocity { get; internal set; }
    public bool IsGrounded { get; internal set; }
    public bool IsCarrying { get; internal set; }

    public bool MovementLocked => _fullLockHolders.Count > 0 || _moveOnlyLockHolders.Count > 0;
    public bool IsLocked => MovementLocked;

    public event Action MovementLockChanged;

    private readonly HashSet<object> _fullLockHolders = new HashSet<object>();
    private readonly HashSet<object> _moveOnlyLockHolders = new HashSet<object>();

    private void Awake()
    {
        // Duplicate-instance guard — KOŞULSUZ Debug.LogError (Debug.Assert
        // DEĞİL: [Conditional("UNITY_ASSERTIONS")] shipping build'de tamamen
        // derlenmez, sıfır koruma verirdi — ADR-0003 unity-specialist BLOCKING
        // düzeltmesi). İkinci instance kendini yok eder, Current'a dokunmaz.
        if (Current != null)
        {
            Debug.LogError("Duplicate PlayerStateProvider detected — destroying this instance.", this);
            Destroy(gameObject);
            return;
        }

        Current = this;
    }

    /// <summary>
    /// Referans-sayımlı kilit isteği — N eşzamanlı sahip desteklenir. Aynı
    /// requester'dan aynı-kapsam tekrar çağrı HashSet.Add idempotency'siyle
    /// no-op (GDD Edge Case, TR-fpc-006). Kapsamlar-arası tekrar çağrı (ör.
    /// önce Full sonra MoveOnly, Release'siz) KASITLI olarak "sticky" — A her
    /// iki kümede de kalır, EffectiveScope() TEK bir Release(A)'ya kadar en
    /// kısıtlayıcıda kalır (ADR-0003 TD-ADR review — bir istekçi kendi
    /// tutuşunu yanlışlıkla gevşetemez, TR-fpc-007).
    /// </summary>
    public MovementLockScope RequestMovementLock(object requester, MovementLockScope scope = MovementLockScope.Full)
    {
        bool wasLocked = MovementLocked;
        (scope == MovementLockScope.Full ? _fullLockHolders : _moveOnlyLockHolders).Add(requester);
        if (!wasLocked)
        {
            MovementLockChanged?.Invoke();
        }
        return EffectiveScope();
    }

    /// <summary>
    /// TEK çağrı, requester'ın TÜM girdilerini (her iki HashSet'ten de) temizler
    /// — çapraz-kapsam sticky kilidin "tek Release her şeyi açar" garantisi.
    /// Kilit sahibi olmayan bir requester'dan çağrılırsa sessiz no-op
    /// (HashSet.Remove semantiği — exception yok, diğer sahipler etkilenmez).
    /// </summary>
    public void ReleaseMovementLock(object requester)
    {
        bool wasLocked = MovementLocked;
        _fullLockHolders.Remove(requester);
        _moveOnlyLockHolders.Remove(requester);
        if (wasLocked && !MovementLocked)
        {
            MovementLockChanged?.Invoke();
        }
    }

    /// <summary>
    /// En kısıtlayıcı kazanır: herhangi bir Full sahibi varsa Full, yoksa (en
    /// az bir MoveOnly sahibi varsa) MoveOnly. O(1) — Count kontrolü.
    /// </summary>
    public MovementLockScope EffectiveScope() =>
        _fullLockHolders.Count > 0 ? MovementLockScope.Full : MovementLockScope.MoveOnly;
}
