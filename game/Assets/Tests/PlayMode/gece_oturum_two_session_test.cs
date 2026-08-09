using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// GOD Story 001 AC-7 / ADR-0001 Validation Criteria: two simulated sessions
/// across a ResetAll boundary — the second session's HasFired must be false for
/// a trigger fired only in the first session, on the facade's real instance
/// (the production path FoundationBootstrap resets). PlayMode variant so the
/// session-boundary semantics run in the player loop like production.
/// </summary>
public class GeceOturumTwoSessionTest
{
    [UnityTest]
    public IEnumerator SecondSession_HasFired_IsFalseForFirstSessionTrigger()
    {
        // Arrange — oturum 1: facade instance'ına gerçek yazım.
        GeceOturumDurumu.InternalInstance.AddFiredTrigger("session1-trigger");
        Assert.IsTrue(GeceOturumDurumu.Instance.HasFired("session1-trigger"));
        GeceOturumDurumu.Instance.EndSession();
        yield return null;

        // Act — oturum sınırı: ResetAll'ın bu servise yaptığı çağrının aynısı.
        GeceOturumDurumu.ResetOnLoad();
        yield return null;

        // Assert — oturum 2 taze: veri yok, oturum aktif, instance aynı (in-place).
        Assert.IsFalse(GeceOturumDurumu.Instance.HasFired("session1-trigger"),
            "İkinci oturum birinci oturumun Fired kaydını görmemeli (ADR-0001).");
        Assert.AreEqual(0, GeceOturumDurumu.Instance.FiredCount);
        Assert.IsTrue(GeceOturumDurumu.Instance.IsSessionActive);
    }
}
