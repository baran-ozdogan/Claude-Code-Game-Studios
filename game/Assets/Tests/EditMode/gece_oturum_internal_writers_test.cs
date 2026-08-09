using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// GOD Story 002: the three internal write paths — AddFiredTrigger idempotency +
/// exactly-once OnTriggerFired, SetRoundState atomic pair write, and the
/// night-count single write. All against fresh instances, facade untouched.
/// </summary>
public class GeceOturumInternalWritersTest
{
    [Test]
    public void AddFiredTrigger_IsIdempotent_AndFiresEventExactlyOncePerId()
    {
        // Arrange
        var state = new GeceOturumDurumuState();
        var received = new List<string>();
        state.OnTriggerFired += id => received.Add(id);

        // Act
        state.AddFiredTrigger("a");
        state.AddFiredTrigger("a"); // idempotent — ikinci event YOK
        state.AddFiredTrigger("b");

        // Assert
        Assert.IsTrue(state.HasFired("a"));
        Assert.IsTrue(state.HasFired("b"));
        Assert.AreEqual(2, state.FiredCount);
        CollectionAssert.AreEqual(new[] { "a", "b" }, received,
            "OnTriggerFired her id için yalnız İLK eklemede fırlamalı.");
    }

    [Test]
    public void AddFiredTrigger_EventHandlerThrows_WriteIsNotRolledBack()
    {
        // Arrange — Add event'ten ÖNCE gerçekleşir; handler hatası yazımı geri almaz.
        var state = new GeceOturumDurumuState();
        state.OnTriggerFired += _ => throw new System.InvalidOperationException("boom");

        // Act + Assert
        Assert.Throws<System.InvalidOperationException>(() => state.AddFiredTrigger("x"));
        Assert.IsTrue(state.HasFired("x"), "Yazım event handler'ı patlasa da kalıcı olmalı.");
    }

    [Test]
    public void SetRoundState_WritesBothFieldsTogether()
    {
        // Arrange
        var state = new GeceOturumDurumuState();

        // Act
        state.SetRoundState(2, 5);

        // Assert
        Assert.AreEqual(2, state.CurrentRoundIndex);
        Assert.AreEqual(5, state.TotalRoundCount);

        // Act — ardışık çağrı son değeri yansıtır.
        state.SetRoundState(3, 5);

        // Assert
        Assert.AreEqual(3, state.CurrentRoundIndex);
        Assert.AreEqual(5, state.TotalRoundCount);
    }

    [Test]
    public void SetTotalConfiguredTriggerCountForNight_StoresValue()
    {
        // Arrange
        var state = new GeceOturumDurumuState();

        // Act
        state.SetTotalConfiguredTriggerCountForNight(3);

        // Assert — tek-yazım kısıtı konvansiyonda; davranış son-yazım-kazanır.
        Assert.AreEqual(3, state.TotalConfiguredTriggerCountForNight);
    }

    [Test]
    public void ResetOnLoad_SubscriberSurvives_AndReceivesPostResetEvents()
    {
        // Arrange — Story 001'in in-place reset AC'sinin abone-hayatta kalıntı yarısı
        // (yazıcı gerektiği için bu story'de): reset öncesi abone, reset sonrası
        // eklemede hâlâ event alır.
        var state = new GeceOturumDurumuState();
        var received = new List<string>();
        state.OnTriggerFired += id => received.Add(id);
        state.AddFiredTrigger("pre");

        // Act
        state.ResetOnLoad();
        state.AddFiredTrigger("post");

        // Assert
        Assert.IsFalse(state.HasFired("pre"), "Reset oturum gerçeklerini temizlemeli.");
        CollectionAssert.AreEqual(new[] { "pre", "post" }, received,
            "Abonelik in-place reset'i atlatmalı — 'post' event'i ulaşmalı.");
    }
}
