using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Story 003 AC-4/5: ResetAll runs exactly once per Play session, at
/// SubsystemRegistration — i.e. before any scene object's Awake() can observe
/// state. Log-based two-mode (Reload Domain ON/OFF) evidence is produced by
/// running this suite via CLI in both Enter Play Mode Settings profiles; the
/// assertions below are mode-agnostic by design (no absolute count).
/// </summary>
public class FoundationBootstrapTimingTest
{
    private sealed class ResetCountProbe : MonoBehaviour
    {
        public int CountAtAwake { get; private set; } = -1;

        private void Awake()
        {
            CountAtAwake = FoundationBootstrap.ResetCount;
        }
    }

    [UnityTest]
    public IEnumerator ResetAll_PlaySession_RanBeforeSceneObjectsAndExactlyOnce()
    {
        // Arrange — by the time any Play-mode code (this test included) runs,
        // SubsystemRegistration must already have fired.
        int countAtSessionObservation = FoundationBootstrap.ResetCount;
        Assert.GreaterOrEqual(countAtSessionObservation, 1,
            "ResetAll bu oturumda hiç koşmamış — SubsystemRegistration tetiklenmedi (AC-4).");

        // Act — a freshly created object's Awake must observe the SAME count:
        // no reset may happen between session start and scene-object Awake.
        var go = new GameObject("FoundationBootstrapTimingProbe", typeof(ResetCountProbe));
        yield return null;
        var probe = go.GetComponent<ResetCountProbe>();

        // Assert
        Assert.AreEqual(countAtSessionObservation, probe.CountAtAwake,
            "Awake, oturum başındakinden farklı bir ResetCount gördü — reset sahne Awake'lerinden önce tam bir kez koşmalı (AC-5).");
        Assert.AreEqual(countAtSessionObservation, FoundationBootstrap.ResetCount,
            "Oturum ortasında ikinci bir ResetAll koştu — oturum başına tam bir kez olmalı (AC-4).");

        Object.Destroy(go);
    }
}
