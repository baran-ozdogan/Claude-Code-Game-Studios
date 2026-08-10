using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// interactable-registry Story 001: IInteractable arayüzü + InteractableRegistry
/// çekirdeği + snapshot cache (ADR-0004). Testler her testten önce/sonra
/// registry'yi TAMAMEN boşaltır — ADR-0004'ün kendi Consequences notu bunu
/// zaten bir test disiplini olarak belgeliyor (DI/interface split yok).
/// </summary>
public class InteractableRegistryCoreTest
{
    /// <summary>Sahte etkileşimli — gerçek/decoy ayrımını sızdırmayan minimal implementasyon.</summary>
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

    private readonly List<IInteractable> _registered = new List<IInteractable>();

    private FakeInteractable Spawn()
    {
        var fake = new FakeInteractable();
        _registered.Add(fake);
        InteractableRegistry.Register(fake);
        return fake;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (IInteractable fake in _registered)
        {
            InteractableRegistry.Deregister(fake);
        }
        _registered.Clear();
        InteractableRegistry.ResetOnLoad();
    }

    // ── AC-1: IInteractable üye listesi verbatim ──

    [Test]
    public void IInteractable_MemberSet_MatchesGddListExactly()
    {
        // Arrange — etkilesim-sistemi.md "Aşağı akış arayüzü" bölümünün 12 üyesi.
        var expected = new[]
        {
            "Type", "HoldDuration", "CanInteract", "PromptText", "SuppressDefaultHoldFill",
            "OnFocusEnter", "OnFocusExit", "OnInteract",
            "OnHoldProgress", "OnHoldComplete", "OnHoldCancelled", "OnHoldBlocked",
        };

        // Act — property'ler (isimleriyle) + sıradan metotlar; property
        // accessor'ları (get_Type vb. — IsSpecialName) hariç, çift saymayı önler.
        MemberInfo[] members = typeof(IInteractable).GetMembers(BindingFlags.Public | BindingFlags.Instance);
        var names = members
            .Where(m => m.MemberType == MemberTypes.Property
                || (m.MemberType == MemberTypes.Method && !((MethodInfo)m).IsSpecialName))
            .Select(m => m.Name)
            .Distinct()
            .ToArray();

        // Assert — deliberately brittle: 13. bir üye eklenirse bu test kırılır
        // (FoundationBootstrapOrderTest'le aynı felsefe).
        CollectionAssert.AreEquivalent(expected, names,
            "IInteractable üye kümesi GDD'nin 12 üyesiyle birebir eşleşmeli — ne fazla ne eksik.");
    }

    // ── AC-2: InteractionType enum şekli ──

    [Test]
    public void InteractionType_HasExactlyInstantAndHold()
    {
        string[] names = Enum.GetNames(typeof(InteractionType));
        CollectionAssert.AreEquivalent(new[] { "Instant", "Hold" }, names);
    }

    // ── AC-3: Register/Deregister, insertion order mutasyon boyunca korunur ──

    [Test]
    public void Register_InsertionOrder_PreservedAcrossMutationSequence()
    {
        // Arrange
        var a = new FakeInteractable();
        var b = new FakeInteractable();
        var c = new FakeInteractable();
        var d = new FakeInteractable();

        try
        {
            // Act — A,B,C kaydet; B'yi sil; D'yi ekle.
            InteractableRegistry.Register(a);
            InteractableRegistry.Register(b);
            InteractableRegistry.Register(c);
            InteractableRegistry.Deregister(b);
            InteractableRegistry.Register(d);

            // Assert — sıra tam olarak [A, C, D].
            IReadOnlyList<IInteractable> snapshot = InteractableRegistry.Snapshot();
            CollectionAssert.AreEqual(new IInteractable[] { a, c, d }, snapshot,
                "Insertion order korunmalı — List semantiği, Set değil (TR-etkilesim-008'in gelecekteki tie-break tabanı).");
        }
        finally
        {
            InteractableRegistry.Deregister(a);
            InteractableRegistry.Deregister(b);
            InteractableRegistry.Deregister(c);
            InteractableRegistry.Deregister(d);
        }
    }

    [Test]
    public void Deregister_NotCurrentlyRegistered_IsSilentNoOp()
    {
        // Arrange — hiç register edilmemiş nesne.
        var stray = new FakeInteractable();

        // Act + Assert — List.Remove semantiği, exception yok.
        Assert.DoesNotThrow(() => InteractableRegistry.Deregister(stray));
    }

    [Test]
    public void Register_SameInstanceTwice_ProducesTwoEntries()
    {
        // Arrange — List (Set değil): aynı instance iki kez eklenirse iki girdi üretir.
        FakeInteractable fake = Spawn();
        InteractableRegistry.Register(fake);

        try
        {
            IReadOnlyList<IInteractable> snapshot = InteractableRegistry.Snapshot();
            Assert.AreEqual(2, snapshot.Count(x => ReferenceEquals(x, fake)),
                "Register iki kez çağrılırsa iki ayrı girdi üretmeli (List semantiği, bilerek).");
        }
        finally
        {
            InteractableRegistry.Deregister(fake); // ikinci kaydı da temizle
        }
    }

    // ── AC-4: Snapshot() lazy-once-per-frame, aynı cache'lenmiş instance ──

    [Test]
    public void Snapshot_CalledTwiceSameFrame_ReturnsSameCachedReference()
    {
        // Arrange
        Spawn();
        Spawn();
        Spawn();

        // Act — EditMode: aynı senkron çağrı yığını, Time.frameCount ilerlemez.
        IReadOnlyList<IInteractable> first = InteractableRegistry.Snapshot();
        IReadOnlyList<IInteractable> second = InteractableRegistry.Snapshot();

        // Assert — TAM AYNI referans, sadece eşit içerik değil.
        Assert.AreSame(first, second);
    }

    [Test]
    public void Snapshot_DeregisterSameFrame_DoesNotForceRecompute()
    {
        // Arrange
        FakeInteractable a = Spawn();
        Spawn();
        IReadOnlyList<IInteractable> cached = InteractableRegistry.Snapshot();

        // Act — aynı karede deregister + tekrar Snapshot().
        InteractableRegistry.Deregister(a);
        _registered.Remove(a);
        IReadOnlyList<IInteractable> afterDeregister = InteractableRegistry.Snapshot();

        // Assert — hâlâ orijinal cache'lenmiş referans (deregister aynı-kare
        // yeniden hesaplamayı TETİKLEMEMELİ — lazy, en çok kare başına bir kez).
        Assert.AreSame(cached, afterDeregister);
    }

    [Test]
    public void Snapshot_FirstEverCall_RecomputesFromInitialSentinel()
    {
        // Arrange — ResetOnLoad ile başlangıç sentinel'ine (_snapshotFrame=-1) dön.
        InteractableRegistry.ResetOnLoad();
        Spawn();

        // Act
        IReadOnlyList<IInteractable> snapshot = InteractableRegistry.Snapshot();

        // Assert — Array.Empty değil, gerçek kayıtlı içerik.
        Assert.AreEqual(1, snapshot.Count);
    }

    // ── AC-5: Kopya-döndürme garantisi, _live'a kapsamlı ──

    [Test]
    public void Snapshot_MutatingReturnedArray_NeverCorruptsLiveList()
    {
        // Arrange
        FakeInteractable a = Spawn();
        FakeInteractable b = Spawn();
        var foreignObject = new FakeInteractable(); // _live'da DEĞİL

        // Act — dönen koleksiyonu IInteractable[]'a cast edip mutasyona uğrat.
        var snapshotArray = (IInteractable[])InteractableRegistry.Snapshot();
        snapshotArray[0] = foreignObject;

        // 3. bir nesne register edip YENİ bir Snapshot al (reset ile kareyi ilerlet).
        InteractableRegistry.ResetOnLoad();
        FakeInteractable c = Spawn();
        IReadOnlyList<IInteractable> freshSnapshot = InteractableRegistry.Snapshot();

        // Assert — _live'ın gerçek üyeliği görünür: orijinal a,b + yeni c.
        // Mutasyon _live'a ASLA ulaşmadı (dar, ADR-literal garanti — aynı-kare
        // cache tamper-proofing İDDİA EDİLMEZ, bkz. AC-4'ün kendi sözleşmesi).
        Assert.AreEqual(3, freshSnapshot.Count);
        CollectionAssert.Contains(freshSnapshot, a);
        CollectionAssert.Contains(freshSnapshot, b);
        CollectionAssert.Contains(freshSnapshot, c);
        CollectionAssert.DoesNotContain(freshSnapshot, foreignObject);
    }

    // ── AC-6: ResetOnLoad() yalnız cache'i temizler + FoundationBootstrap kaydı ──

    [Test]
    public void ResetOnLoad_ClearsCache_ButLeavesLiveListUntouched()
    {
        // Arrange
        FakeInteractable a = Spawn();
        FakeInteractable b = Spawn();
        IReadOnlyList<IInteractable> beforeReset = InteractableRegistry.Snapshot(); // cache doldu

        // Act
        InteractableRegistry.ResetOnLoad();
        IReadOnlyList<IInteractable> afterReset = InteractableRegistry.Snapshot();

        // Assert (cache temizlendi) — farklı array referansı, yeniden hesaplandı.
        Assert.AreNotSame(beforeReset, afterReset);

        // Assert (_live dokunulmadı) — a ve b hâlâ kayıtlı.
        Assert.AreEqual(2, afterReset.Count);
        CollectionAssert.Contains(afterReset, a);
        CollectionAssert.Contains(afterReset, b);
    }

    [Test]
    public void ResetOnLoad_ZeroItemsAlreadyEmptyCache_IsSafeNoOp()
    {
        Assert.DoesNotThrow(() => InteractableRegistry.ResetOnLoad());
        Assert.AreEqual(0, InteractableRegistry.Snapshot().Count);
    }

    [Test]
    public void FoundationBootstrap_ActiveResetOrder_ContainsInteractableRegistryFirst()
    {
        IReadOnlyList<string> order = FoundationBootstrap.ActiveResetOrder;
        Assert.AreEqual("InteractableRegistry", order[0],
            "InteractableRegistry, belgeli sıranın kökünde (yukarı bağımlılığı yok) olmalı.");
    }

    // ── AC-7: Yapısal gerçek/decoy ayrım-sızdırmazlığı ──

    [Test]
    public void InteractableRegistry_PublicSurface_HasNoRealDecoyDistinction()
    {
        // Arrange + Act
        MethodInfo[] methods = typeof(InteractableRegistry)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName) // property accessor gürültüsünü ele
            .ToArray();

        // Assert — tam olarak Register, Deregister, Snapshot; başka overload/parametre yok.
        var names = methods.Select(m => m.Name).OrderBy(n => n).ToArray();
        CollectionAssert.AreEqual(new[] { "Deregister", "Register", "Snapshot" }, names);

        foreach (MethodInfo method in methods.Where(m => m.Name == "Register" || m.Name == "Deregister"))
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(IInteractable), parameters[0].ParameterType,
                "Register/Deregister tek parametre alır: IInteractable — ayrım bayrağı (bool/enum) yok.");
        }
    }

    [Test]
    public void IInteractable_HasNoRealDecoyFlagMember()
    {
        // IInteractable'ın kendisi de "gerçek"/"decoy" ayrımı yapan bir bayrak taşımaz —
        // yapısal ayrım-sızdırmazlığının ikinci yarısı.
        string[] members = typeof(IInteractable)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name.ToLowerInvariant())
            .ToArray();

        Assert.IsFalse(members.Any(n => n.Contains("real") || n.Contains("decoy") || n.Contains("fake")),
            "IInteractable, gerçek/decoy ayrımını sızdıran bir üye taşımamalı.");
    }

    // ── AC-8: Enumerasyon-ortası Deregister güvenliği (registry'ye ait GDD Edge Case) ──

    [Test]
    public void Snapshot_SelfDeregisterDuringEnumeration_DoesNotThrowOrCorruptCurrentEnumeration()
    {
        // Arrange — 3 kayıtlı nesne, bu "karede" bir Snapshot() zaten alınmış.
        FakeInteractable a = Spawn();
        FakeInteractable b = Spawn();
        FakeInteractable c = Spawn();
        IReadOnlyList<IInteractable> snapshot = InteractableRegistry.Snapshot();

        // Act — enumerasyon ORTASINDA, gezilen nesnelerden biri kendini deregister
        // eder (bir nesnenin odaklanma sırasında kendini disable etmesini simüle eder).
        var visited = new List<IInteractable>();
        Assert.DoesNotThrow(() =>
        {
            foreach (IInteractable item in snapshot)
            {
                visited.Add(item);
                if (ReferenceEquals(item, b))
                {
                    InteractableRegistry.Deregister(b);
                    _registered.Remove(b);
                }
            }
        }, "Çağıranın elindeki snapshot, eşzamanlı _live mutasyonundan etkilenmemeli (InvalidOperationException yok).");

        // Assert — gezilen küme mutasyon-öncesi haliyle TAM 3 öğe.
        CollectionAssert.AreEqual(new IInteractable[] { a, b, c }, visited);

        // Assert — SONRAKİ karenin Snapshot()'ı doğru şekilde yalnız kalan 2 öğeyi
        // yansıtır (izolasyon geçici-by-design, kalıcı desync değil).
        InteractableRegistry.ResetOnLoad();
        IReadOnlyList<IInteractable> nextFrame = InteractableRegistry.Snapshot();
        Assert.AreEqual(2, nextFrame.Count);
        CollectionAssert.DoesNotContain(nextFrame, b);
    }
}
