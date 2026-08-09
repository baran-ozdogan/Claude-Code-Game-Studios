using NUnit.Framework;

/// <summary>
/// GOD Story 001: initial state, EndSession semantics, and the in-place reset
/// contract of GeceOturumDurumuState. Tests construct fresh instances directly —
/// the static facade is touched only by the facade-invariant test, whose subject
/// IS the facade (ADR-0015 in-place regime).
/// </summary>
public class GeceOturumStateTest
{
    [Test]
    public void InitialState_Defaults_AreSessionActiveNightOneAndEmpty()
    {
        // Arrange + Act
        var state = new GeceOturumDurumuState();

        // Assert
        Assert.IsTrue(state.IsSessionActive);
        Assert.AreEqual(1, state.CurrentNightNumber);
        Assert.AreEqual(0, state.CurrentRoundIndex);
        Assert.AreEqual(0, state.TotalRoundCount);
        Assert.AreEqual(0, state.FiredCount);
        Assert.AreEqual(0, state.SettledCount);
        Assert.AreEqual(0, state.TotalConfiguredTriggerCountForNight);
        Assert.IsFalse(state.HasFired("any"));
        Assert.IsFalse(state.HasSettled("any"));
        Assert.IsFalse(state.IsPersistent("any"));
    }

    [Test]
    public void EndSession_SetsInactive_AndIsIdempotent()
    {
        // Arrange
        var state = new GeceOturumDurumuState();

        // Act
        state.EndSession();
        state.EndSession(); // ikinci çağrı — hata/side-effect üretmemeli

        // Assert
        Assert.IsFalse(state.IsSessionActive);
        Assert.IsFalse(state.HasFired("any"), "EndSession sonrası okuma yüzeyi çalışmaya devam etmeli.");
    }

    [Test]
    public void ResetOnLoad_ClearsInPlace_AndReinitializesSessionActive()
    {
        // Arrange — oturumu "kirlet": EndSession çağrılmış olsun.
        var state = new GeceOturumDurumuState();
        state.EndSession();

        // Act
        state.ResetOnLoad();

        // Assert — IsSessionActive explicit re-init (non-default alan, ADR-0015).
        Assert.IsTrue(state.IsSessionActive);
        Assert.AreEqual(0, state.FiredCount);
        Assert.AreEqual(0, state.SettledCount);
        Assert.AreEqual(0, state.CurrentRoundIndex);
        Assert.AreEqual(0, state.TotalRoundCount);
        Assert.AreEqual(0, state.TotalConfiguredTriggerCountForNight);
    }

    [Test]
    public void Facade_ResetOnLoad_KeepsSameInstance()
    {
        // Arrange — bu testin öznesi facade'ın kendisi: in-place invariant.
        GeceOturumDurumuState before = GeceOturumDurumu.InternalInstance;

        // Act
        GeceOturumDurumu.ResetOnLoad();

        // Assert — instance asla değiştirilmez (event aboneleri hayatta kalır).
        Assert.AreSame(before, GeceOturumDurumu.InternalInstance);
        Assert.AreSame(before, (GeceOturumDurumuState)GeceOturumDurumu.Instance);
        Assert.IsTrue(GeceOturumDurumu.Instance.IsSessionActive);
    }
}
