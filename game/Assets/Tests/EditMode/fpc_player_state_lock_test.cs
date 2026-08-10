using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// birinci-sahis-kontrolcu Story 001: IPlayerState + PlayerStateProvider'ın
/// referans-sayımlı hareket kilidi (ADR-0003). ADR'ın kendi Validation
/// Criteria'sı EditMode test + AddComponent&lt;PlayerStateProvider&gt;()'ı
/// öngörür — CharacterController/Camera gerekmez. Buradaki testler yalnız
/// `_provider` üzerinde doğrudan instance metodu çağırıyor — MonoBehaviour
/// yaşam döngüsünden (Awake) bağımsızlar.
///
/// AC-2 (duplicate-instance guard) BİLEREK BURADA DEĞİL: ampirik bulgu —
/// Unity, Play Mode DIŞINDA (Edit Mode dahil, `[ExecuteAlways]` olmadıkça)
/// Awake()'i HİÇ ÇAĞIRMAZ; `[UnityTest]` + `yield return null` denendi, birden
/// çok kare beklense de Awake() ateşlemedi (doğrudan `internal` bayrakla
/// doğrulandı — bkz. session log). Bu, ADR-0003'ün "Awake() edit-mode'da
/// senkron koşar" varsayımının YANLIŞ olduğunu kanıtlıyor. Guard, gerçek
/// Play Mode gerektiriyor — test `Tests/PlayMode/fpc_player_state_duplicate_guard_test.cs`'e
/// taşındı (UIRootStaleInstanceTest emsaliyle aynı desen).
/// </summary>
public class FpcPlayerStateLockTest
{
    private GameObject _go;
    private PlayerStateProvider _provider;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PlayerStateProviderTest");
        _provider = _go.AddComponent<PlayerStateProvider>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    // ── AC-1: IPlayerState alan listesi verbatim ──

    [Test]
    public void IPlayerState_MemberSet_MatchesGddListExactly()
    {
        var expected = new[] { "EyeCamera", "Velocity", "IsGrounded", "MovementLocked", "IsCarrying", "IsLocked", "MovementLockChanged" };

        MemberInfo[] members = typeof(IPlayerState).GetMembers(BindingFlags.Public | BindingFlags.Instance);
        var names = members
            .Where(m => m.MemberType == MemberTypes.Property
                || (m.MemberType == MemberTypes.Event)
                || (m.MemberType == MemberTypes.Method && !((MethodInfo)m).IsSpecialName))
            .Select(m => m.Name)
            .Distinct()
            .ToArray();

        CollectionAssert.AreEquivalent(expected, names,
            "IPlayerState üye kümesi GDD'nin 7 alanıyla birebir eşleşmeli.");
    }

    // AC-2 (duplicate-instance guard): Tests/PlayMode/fpc_player_state_duplicate_guard_test.cs'e taşındı — bkz. dosya üstü not.

    // ── AC-3 (varsayılan parametre): scope belirtilmezse Full ──

    [Test]
    public void RequestMovementLock_ScopeOmitted_DefaultsToFull()
    {
        var a = new object();

        _provider.RequestMovementLock(a);

        Assert.AreEqual(MovementLockScope.Full, _provider.EffectiveScope(),
            "scope parametresi verilmezse varsayılan Full olmalı (TR-fpc-002).");
    }

    // ── AC-3: N eşzamanlı aynı-kapsam kilit sahibi (referans sayımı) ──

    [Test]
    public void RequestMovementLock_TwoFullHolders_StaysLockedUntilBothRelease()
    {
        // Arrange
        var a = new object();
        var b = new object();

        // Act + Assert
        _provider.RequestMovementLock(a, MovementLockScope.Full);
        _provider.RequestMovementLock(b, MovementLockScope.Full);
        Assert.IsTrue(_provider.MovementLocked);

        _provider.ReleaseMovementLock(a);
        Assert.IsTrue(_provider.MovementLocked, "A release etti ama B hâlâ tutuyor — kilitli kalmalı.");

        _provider.ReleaseMovementLock(b);
        Assert.IsFalse(_provider.MovementLocked, "İkisi de release edince kilitsiz olmalı.");
    }

    [Test]
    public void RequestMovementLock_TwoFullHolders_ReleaseOrderIndependent()
    {
        // Arrange
        var a = new object();
        var b = new object();
        _provider.RequestMovementLock(a, MovementLockScope.Full);
        _provider.RequestMovementLock(b, MovementLockScope.Full);

        // Act — B önce release (ters sıra).
        _provider.ReleaseMovementLock(b);

        // Assert
        Assert.IsTrue(_provider.MovementLocked, "Sıra bağımsız — B önce release etse de A hâlâ tutuyor.");
        _provider.ReleaseMovementLock(a);
        Assert.IsFalse(_provider.MovementLocked);
    }

    // ── AC-4: Kapsamlar-arası en-kısıtlayıcı-kazanır ──

    [Test]
    public void EffectiveScope_MoveOnlyPlusFull_ResolvesToFull()
    {
        var a = new object();
        var b = new object();
        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);
        _provider.RequestMovementLock(b, MovementLockScope.Full);

        Assert.AreEqual(MovementLockScope.Full, _provider.EffectiveScope());
    }

    [Test]
    public void EffectiveScope_OnlyMoveOnlyHolders_ResolvesToMoveOnly()
    {
        var a = new object();
        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);

        Assert.AreEqual(MovementLockScope.MoveOnly, _provider.EffectiveScope());
    }

    // ── AC-5: Aynı-kapsam double-Request no-op ──

    [Test]
    public void RequestMovementLock_SameScopeTwice_IsNoOp_SingleReleaseFullyUnlocks()
    {
        // Arrange
        var a = new object();
        int changedCount = 0;
        _provider.MovementLockChanged += () => changedCount++;

        // Act — aynı requester, aynı kapsam, Release'siz iki kez.
        _provider.RequestMovementLock(a, MovementLockScope.Full);
        _provider.RequestMovementLock(a, MovementLockScope.Full);

        // Assert — event yalnız İLK çağrıda fırladı.
        Assert.AreEqual(1, changedCount, "MovementLockChanged yalnız ilk gerçek geçişte fırlamalı.");
        Assert.IsTrue(_provider.MovementLocked);

        // Act — TEK Release tam açar.
        _provider.ReleaseMovementLock(a);

        // Assert
        Assert.IsFalse(_provider.MovementLocked, "Tek Release, çift Request'i de tamamen temizlemeli.");
    }

    [Test]
    public void RequestMovementLock_SameScopeTwice_MoveOnly_IsNoOp_SingleReleaseFullyUnlocks()
    {
        // Arrange — AC-5'in QA Test Case'inde belirtilen MoveOnly tekrarı.
        var a = new object();
        int changedCount = 0;
        _provider.MovementLockChanged += () => changedCount++;

        // Act — aynı requester, aynı kapsam (MoveOnly), Release'siz iki kez.
        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);
        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);

        // Assert — event yalnız İLK çağrıda fırladı.
        Assert.AreEqual(1, changedCount, "MovementLockChanged yalnız ilk gerçek geçişte fırlamalı.");
        Assert.IsTrue(_provider.MovementLocked);

        // Act — TEK Release tam açar.
        _provider.ReleaseMovementLock(a);

        // Assert
        Assert.IsFalse(_provider.MovementLocked, "Tek Release, çift Request'i de tamamen temizlemeli.");
    }

    // ── AC-6: Kapsamlar-arası sticky kilit (KASITLI) ──

    [Test]
    public void RequestMovementLock_FullThenMoveOnly_StaysStickyFull_SingleReleaseClearsBoth()
    {
        // Arrange
        var a = new object();

        // Act — önce Full, sonra Release'siz MoveOnly.
        _provider.RequestMovementLock(a, MovementLockScope.Full);
        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);

        // Assert — KASITLI: EffectiveScope Full kalır (düzeltilecek bir hata DEĞİL).
        Assert.AreEqual(MovementLockScope.Full, _provider.EffectiveScope(),
            "Çapraz-kapsam sticky davranış: A her iki kümede, en kısıtlayıcı (Full) kazanır.");

        // Act — TEK Release ikisini de temizler.
        _provider.ReleaseMovementLock(a);

        // Assert
        Assert.IsFalse(_provider.MovementLocked, "Tek Release, çapraz-kapsam girdilerin İKİSİNİ de temizlemeli.");
    }

    [Test]
    public void RequestMovementLock_MoveOnlyThenFull_StaysStickyFull_ReverseOrderSameOutcome()
    {
        // Ters sıra: önce MoveOnly, sonra Full — aynı sonuç (sticky, kasıtlı).
        var a = new object();

        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);
        _provider.RequestMovementLock(a, MovementLockScope.Full);

        Assert.AreEqual(MovementLockScope.Full, _provider.EffectiveScope());

        _provider.ReleaseMovementLock(a);
        Assert.IsFalse(_provider.MovementLocked);
    }

    // ── AC-7: Kilit-sahibi-olmayan Release sessiz no-op ──

    [Test]
    public void ReleaseMovementLock_FromNonHolder_IsSilentNoOp_OtherHoldersUnaffected()
    {
        // Arrange
        var a = new object();
        var b = new object();
        _provider.RequestMovementLock(a, MovementLockScope.Full);

        // Act + Assert — B hiç istemedi, release eder.
        Assert.DoesNotThrow(() => _provider.ReleaseMovementLock(b));
        Assert.IsTrue(_provider.MovementLocked, "A'nın kilidi B'nin no-op release'inden etkilenmemeli.");

        // Edge case — tekrarlanan B release'leri de no-op.
        Assert.DoesNotThrow(() => _provider.ReleaseMovementLock(b));
        Assert.IsTrue(_provider.MovementLocked);
    }

    // ── AC-8: MovementLockChanged yalnız gerçek geçişte ──

    [Test]
    public void MovementLockChanged_FiresOnlyOnRealTransitions()
    {
        // Arrange
        var a = new object();
        var b = new object();
        int changedCount = 0;
        _provider.MovementLockChanged += () => changedCount++;

        // 0→1: fırlar.
        _provider.RequestMovementLock(a, MovementLockScope.Full);
        Assert.AreEqual(1, changedCount);

        // 1→2: fırlamaz.
        _provider.RequestMovementLock(b, MovementLockScope.Full);
        Assert.AreEqual(1, changedCount, "İkinci sahip eklenmesi (hâlâ kilitli) event üretmemeli.");

        // 2→1: fırlamaz.
        _provider.ReleaseMovementLock(a);
        Assert.AreEqual(1, changedCount, "Bir sahip kalınca (hâlâ kilitli) event üretmemeli.");

        // 1→0: fırlar.
        _provider.ReleaseMovementLock(b);
        Assert.AreEqual(2, changedCount);
    }

    // ── AC-9: IsLocked ucuz alias ──

    [Test]
    public void IsLocked_AlwaysEqualsMovementLocked()
    {
        Assert.AreEqual(_provider.MovementLocked, _provider.IsLocked);

        var a = new object();
        _provider.RequestMovementLock(a, MovementLockScope.MoveOnly);
        Assert.AreEqual(_provider.MovementLocked, _provider.IsLocked);
        Assert.IsTrue(_provider.IsLocked);

        _provider.ReleaseMovementLock(a);
        Assert.AreEqual(_provider.MovementLocked, _provider.IsLocked);
        Assert.IsFalse(_provider.IsLocked);
    }
}
