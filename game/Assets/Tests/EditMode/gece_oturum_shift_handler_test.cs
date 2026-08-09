using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// GOD Story 003: the pure shift-state handler — Persistent capture at
/// Shifting-In (query-gated), Settled capture at Held (Fired-gated, exactly-once
/// event), and the documented Settled-lag window. IsShiftPersistent is a fake
/// injected delegate; no Işık/Volume dependency (wiring is Story 004).
/// </summary>
public class GeceOturumShiftHandlerTest
{
    private static GeceOturumDurumuState NewStateWithPersistentIds(params string[] persistentIds)
    {
        var state = new GeceOturumDurumuState();
        var set = new HashSet<string>(persistentIds);
        state.BindIsShiftPersistentQuery(id => set.Contains(id));
        return state;
    }

    [Test]
    public void ShiftingIn_PersistentQueryTrue_MarksPersistentImmediately()
    {
        // Arrange
        var state = NewStateWithPersistentIds("p");

        // Act — Held beklenmeden, Shifting-In karesinde.
        state.ProcessShiftStateChanged("p", ShiftState.ShiftingIn);

        // Assert
        Assert.IsTrue(state.IsPersistent("p"));
    }

    [Test]
    public void ShiftingIn_PersistentQueryFalse_WritesNothing()
    {
        // Arrange
        var state = NewStateWithPersistentIds("p");

        // Act
        state.ProcessShiftStateChanged("n", ShiftState.ShiftingIn);
        state.ProcessShiftStateChanged("p", ShiftState.ShiftingIn); // idempotent kontrolü için ikinci giriş
        state.ProcessShiftStateChanged("p", ShiftState.ShiftingIn);

        // Assert
        Assert.IsFalse(state.IsPersistent("n"));
        Assert.IsTrue(state.IsPersistent("p"), "Tekrarlanan Shifting-In değeri bozmamalı.");
    }

    [Test]
    public void Held_FiredMember_SettlesWithExactlyOneEvent()
    {
        // Arrange
        var state = NewStateWithPersistentIds();
        var settled = new List<string>();
        state.OnTriggerSettled += id => settled.Add(id);
        state.AddFiredTrigger("f");

        // Act
        state.ProcessShiftStateChanged("f", ShiftState.Held);
        state.ProcessShiftStateChanged("f", ShiftState.Held); // ikinci Held — event YOK

        // Assert
        Assert.IsTrue(state.HasSettled("f"));
        CollectionAssert.AreEqual(new[] { "f" }, settled,
            "OnTriggerSettled aynı id için tam bir kez fırlamalı.");
    }

    [Test]
    public void Held_NotFired_DoesNotSettle()
    {
        // Arrange — Automatic ambient zone koruması: Fired'da olmayan id Settled'a giremez.
        var state = NewStateWithPersistentIds();
        int settledEvents = 0;
        state.OnTriggerSettled += _ => settledEvents++;

        // Act
        state.ProcessShiftStateChanged("ambient", ShiftState.Held);

        // Assert
        Assert.IsFalse(state.HasSettled("ambient"));
        Assert.AreEqual(0, settledEvents);
        Assert.AreEqual(0, state.SettledCount);
    }

    [Test]
    public void SettledLagWindow_SettledCountMayTrailFiredCount()
    {
        // Arrange — Fired yazıldı, Held henüz gelmedi (~3sn rampa penceresi).
        var state = NewStateWithPersistentIds();
        state.AddFiredTrigger("x");

        // Assert — geçici pencere hata değil; doygunluk HER ZAMAN SettledCount okur.
        Assert.Less(state.SettledCount, state.FiredCount);

        // Act — Held gelince yetişir.
        state.ProcessShiftStateChanged("x", ShiftState.Held);

        // Assert
        Assert.AreEqual(state.FiredCount, state.SettledCount);
    }

    [Test]
    public void OtherStates_DormantAndShiftingOut_AreIgnored()
    {
        // Arrange
        var state = NewStateWithPersistentIds("p");
        state.AddFiredTrigger("p");

        // Act
        state.ProcessShiftStateChanged("p", ShiftState.Dormant);
        state.ProcessShiftStateChanged("p", ShiftState.ShiftingOut);

        // Assert — yalnız Shifting-In/Held yazar.
        Assert.IsFalse(state.IsPersistent("p"));
        Assert.IsFalse(state.HasSettled("p"));
    }
}
