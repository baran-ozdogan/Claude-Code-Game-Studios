using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// interactable-registry Story 002: iki-oturum self-correction + cross-session
/// cache-collision ampirik kanıtı (ADR-0004 Validation Criteria bullet 2/3).
/// `gece_oturum_two_session_test.cs`'in kurduğu idiyomla aynı: gerçek Enter Play
/// Mode Settings'i değiştirmez, `ResetOnLoad()`'u doğrudan çağırarak oturum
/// sınırını simüle eder — Unity'nin gerçek yaşam döngüsü garantilerini
/// (`Awake` bir kez, `OnEnable`/`OnDisable` her `SetActive` geçişinde) kullanır.
/// </summary>
public class InteractableRegistrySessionTest
{
    /// <summary>
    /// Amaca özel test-double — `foundation_bootstrap_timing_test.cs`'deki
    /// `ResetCountProbe` sayaç-probe emsalini takip eder. `Awake` bir kez,
    /// `OnEnable`/`OnDisable` Register/Deregister'ı sürer.
    /// </summary>
    private sealed class TestInteractableProbe : MonoBehaviour, IInteractable
    {
        public int AwokeCount { get; private set; }

        public InteractionType Type => InteractionType.Instant;
        public float HoldDuration => 0f;
        public bool CanInteract => true;
        public string PromptText => "Probe";
        public bool SuppressDefaultHoldFill => false;
        public void OnFocusEnter() { }
        public void OnFocusExit() { }
        public void OnInteract() { }
        public void OnHoldProgress(float t) { }
        public void OnHoldComplete() { }
        public void OnHoldCancelled() { }
        public void OnHoldBlocked() { }

        private void Awake() => AwokeCount++;
        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Deregister(this);
    }

    private sealed class FakeInteractable : IInteractable
    {
        public InteractionType Type => InteractionType.Instant;
        public float HoldDuration => 0f;
        public bool CanInteract => true;
        public string PromptText => "Test";
        public bool SuppressDefaultHoldFill => false;
        public void OnFocusEnter() { }
        public void OnFocusExit() { }
        public void OnInteract() { }
        public void OnHoldProgress(float t) { }
        public void OnHoldComplete() { }
        public void OnHoldCancelled() { }
        public void OnHoldBlocked() { }
    }

    [TearDown]
    public void TearDown() => InteractableRegistry.ResetOnLoad();

    private static bool SnapshotContains(IInteractable target)
    {
        foreach (IInteractable item in InteractableRegistry.Snapshot())
        {
            if (ReferenceEquals(item, target))
            {
                return true;
            }
        }
        return false;
    }

    // ── AC-1/2: OnEnable/OnDisable self-correction — Awake YENİDEN ATEŞLENMEZ ──

    [UnityTest]
    public IEnumerator SimulatedSessionBoundary_AwakeDoesNotRerun_ButOnEnableReregisters()
    {
        // Arrange — oturum 1: instantiate + aktif et.
        var go = new GameObject("InteractableRegistrySessionProbe", typeof(TestInteractableProbe));
        var probe = go.GetComponent<TestInteractableProbe>();

        try
        {
            yield return null; // Awake + OnEnable koşsun

            Assert.AreEqual(1, probe.AwokeCount, "Oturum 1: Awake tam bir kez koşmalı.");
            Assert.IsTrue(SnapshotContains(probe), "Oturum 1: OnEnable, probe'u kaydetmiş olmalı.");

            // Act — oturum-1 teardown'unu simüle et (OnDisable fırlar, Deregister).
            go.SetActive(false);
            yield return null;

            // Assert — AC-2 edge case: disable sırasında GERÇEKTEN kayıp,
            // yalnız "sonunda düzelir" değil.
            Assert.IsFalse(SnapshotContains(probe),
                "SetActive(false) sonrası probe Snapshot()'tan kaybolmalı (OnDisable→Deregister fiilen fırladı).");

            // Act — oturum sınırı: FoundationBootstrap.ResetAll()'un bu servise
            // yaptığı GERÇEK çağrı (Story 001 AC-6'nın kanıtladığı satır).
            InteractableRegistry.ResetOnLoad();

            // Act — oturum-2 başlangıcı: Reload-Scene-off'ta hayatta kalan bir
            // nesnenin re-enable'ını simüle et.
            go.SetActive(true);
            yield return null;

            // Assert (AC-1) — Awake YENİDEN ATEŞLENMEDİ (belgelenen Reload-Scene-off
            // boşluğu sadakatle yeniden üretildi) AMA OnEnable yeniden kaydetti.
            Assert.AreEqual(1, probe.AwokeCount,
                "Oturum 2: Awake yeniden koşmamalı (Reload Scene OFF, Unity'nin gerçek garantisi).");
            Assert.IsTrue(SnapshotContains(probe),
                "Oturum 2: OnEnable, Awake'e hiç güvenmeden probe'u yeniden kaydetmiş olmalı.");
        }
        finally
        {
            // Object.Destroy tek başına ertelenmiş OnDisable'a güvenir — Test 2'nin
            // kendi savunmacı deseniyle tutarlı olsun diye açık Deregister de eklendi
            // (InteractableRegistry.ResetOnLoad() _live'a BİLEREK dokunmuyor; bu test
            // sınıfı 2 test paylaşıyor, LP review bulgusu).
            InteractableRegistry.Deregister(probe);
            Object.Destroy(go);
        }
    }

    // ── AC-3: cross-session cache-collision — negatif kontrollü red-then-green ──

    [UnityTest]
    public IEnumerator CrossSessionCacheCollision_PoisonedCacheIsReturnedUntilReset_ThenRecomputes()
    {
        // Arrange — oturum-2'nin gerçek _live içeriği: 2 kayıtlı nesne.
        var real1 = new FakeInteractable();
        var real2 = new FakeInteractable();
        InteractableRegistry.Register(real1);
        InteractableRegistry.Register(real2);

        try
        {
            // Zehirli array: _live'da OLMAYAN bir marker nesne — gerçek geçerli
            // Time.frameCount'a eşitlenerek "önceki oturumdan kalan cache, BU
            // oturumun kare sayısıyla çakışıyor" senaryosu yeniden kurulur.
            var marker = new FakeInteractable();
            var poisoned = new IInteractable[] { marker };
            int staleFrame = Time.frameCount;

            // Act (zehirle) — internal alanlara doğrudan yaz (Story 001'in
            // bilinçli sapması).
            InteractableRegistry._frameSnapshot = poisoned;
            InteractableRegistry._snapshotFrame = staleFrame;

            // Assert (negatif kontrol — bug GERÇEK): reset ÖNCESİ Snapshot()
            // zehirli array'i döner, gerçek _live içeriğini DEĞİL.
            IReadOnlyList<IInteractable> beforeReset = InteractableRegistry.Snapshot();
            Assert.AreSame(poisoned, beforeReset,
                "Reset öncesi: cache-collision GERÇEKTEN reprodüklendi — zehirli array dönmeli (bug'ın kanıtı).");
            CollectionAssert.Contains(beforeReset, marker);
            CollectionAssert.DoesNotContain(beforeReset, real1);

            // Act (düzelt) — FoundationBootstrap.ResetAll()'un gerçek çağrısı.
            InteractableRegistry.ResetOnLoad();

            // Assert (pozitif — düzeltme kapatıyor): YENİ array, gerçek _live
            // içeriği, marker YOK.
            IReadOnlyList<IInteractable> afterReset = InteractableRegistry.Snapshot();
            Assert.AreNotSame(poisoned, afterReset);
            Assert.AreEqual(2, afterReset.Count);
            CollectionAssert.Contains(afterReset, real1);
            CollectionAssert.Contains(afterReset, real2);
            CollectionAssert.DoesNotContain(afterReset, marker);

            // Edge case — poison→reset ÇİFTİNİN kendisi art arda iki kez güvenli
            // (story metni: "poison→reset dizisi art arda iki kez"): ikinci bir
            // zehirleme de aynı şekilde ResetOnLoad ile temizlenir, _live'a dokunmaz.
            var marker2 = new FakeInteractable();
            InteractableRegistry._frameSnapshot = new IInteractable[] { marker2 };
            InteractableRegistry._snapshotFrame = Time.frameCount;
            InteractableRegistry.ResetOnLoad();
            IReadOnlyList<IInteractable> afterSecondReset = InteractableRegistry.Snapshot();
            Assert.AreEqual(2, afterSecondReset.Count);
            CollectionAssert.DoesNotContain(afterSecondReset, marker2);
        }
        finally
        {
            InteractableRegistry.Deregister(real1);
            InteractableRegistry.Deregister(real2);
        }

        yield break;
    }

    // NOT (Implementation Notes'tan): bu iki test yalnız InteractableRegistry.
    // ResetOnLoad()'un KENDİSİNİN doğruluğunu kanıtlar — ResetOnLoad()'un her
    // GERÇEK oturum sınırında gerçekten ÇAĞRILDIĞI ayrıca kapalı: Story 001
    // AC-6 (FoundationBootstrapOrderTest, satırın sırada var olduğunu kanıtlar)
    // + FoundationBootstrapTimingTest (ResetAll()'un oturum başına tam bir kez
    // ve her sahne Awake'inden ÖNCE koştuğunu kanıtlar — servis-agnostik,
    // Story 001'in _resetSequence satırını aktif ettiği andan beri otomatik
    // kapsar). gece_oturum_two_session_test.cs'in aynı iş bölümünün yansımasıdır.
}
