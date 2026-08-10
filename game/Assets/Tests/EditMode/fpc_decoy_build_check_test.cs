using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// birinci-sahis-kontrolcu Story 006: decoy içerik build-time doğrulaması
/// (TR-fpc-016, GDD AC17). Sahte sahne içeriği runtime-created GameObject'lerle
/// kurulur, on-disk sahne/asset YASAK (isik-volume Story 006 `FakeWalker` emsali).
/// Her senaryo throws/doesn't-throw çifti + mesajda suçlu sahne adı.
/// </summary>
public class FpcDecoyBuildCheckTest
{
    private readonly List<GameObject> _spawned = new List<GameObject>();

    /// <summary>
    /// Verilen sahne yollarını gezmiş gibi yapar. `onOpen` verilirse her "sahne açılışında"
    /// çağrılır — üretimde `OpenSceneMode.Single` içeriği değiştirir, testte fixture'ı
    /// sahneye göre kurup yıkmanın tek yolu bu (sahne-başına atıf kanıtı).
    /// </summary>
    private sealed class FakeWalker : IBuildSceneWalker
    {
        private readonly System.Action<string> _onOpen;

        public FakeWalker(params string[] scenePaths) : this(null, scenePaths) { }

        public FakeWalker(System.Action<string> onOpen, params string[] scenePaths)
        {
            _onOpen = onOpen;
            EnabledScenePaths = scenePaths;
        }

        public IReadOnlyList<string> EnabledScenePaths { get; }
        public void OpenScene(string scenePath) => _onOpen?.Invoke(scenePath);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    private DecoyInteractable CreateDecoy(string name = "KapiKolu", string prompt = "Çevir")
    {
        var go = new GameObject(name);
        _spawned.Add(go);
        var decoy = go.AddComponent<DecoyInteractable>();

        var serialized = new UnityEditor.SerializedObject(decoy);
        serialized.FindProperty("_promptText").stringValue = prompt;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return decoy;
    }

    private GameObject CreateNonDecoyInteractable(string name = "AniTetikleyiciBenzeri")
    {
        var go = new GameObject(name);
        _spawned.Add(go);
        go.AddComponent<OtherInteractable>();
        return go;
    }

    /// <summary>Decoy OLMAYAN bir interactable — "başka içerik var ama decoy yok" senaryosu için.</summary>
    private sealed class OtherInteractable : MonoBehaviour, IInteractable
    {
        public InteractionType Type => InteractionType.Instant;
        public float HoldDuration => 0f;
        public bool CanInteract => true;
        public string PromptText => "other";
        public void OnFocusEnter() { }
        public void OnFocusExit() { }
        public void OnInteract() { }
        public void OnHoldProgress(float t) { }
        public void OnHoldComplete() { }
        public void OnHoldCancelled() { }
        public void OnHoldBlocked() { }
        public bool SuppressDefaultHoldFill => false;
    }

    private static void RunCheckOver(params string[] scenePaths) =>
        BuildValidationRunner.RunAll(new IBuildCheck[] { new FpcDecoyPresenceCheck() }, new FakeWalker(scenePaths));

    // ── AC-1: DecoyInteractable minimal implementasyon ──

    [Test]
    public void DecoyInteractable_IsMinimalInstantInteractable_WithNoMechanicalEffect()
    {
        DecoyInteractable decoy = CreateDecoy();

        Assert.AreEqual(InteractionType.Instant, decoy.Type, "Decoy her zaman Instant olmalı.");
        Assert.AreEqual(0f, decoy.HoldDuration, "Instant olduğu için HoldDuration yok sayılır.");
        Assert.IsTrue(decoy.CanInteract);
        Assert.IsFalse(decoy.SuppressDefaultHoldFill);
        Assert.DoesNotThrow(() => decoy.OnInteract(), "OnInteract hiçbir mekanik etki üretmemeli.");
    }

    [Test]
    public void DecoyInteractable_AddsNoMemberBeyondIInteractable_CamouflageNotLeaked()
    {
        // Kamuflaj yapısal olarak korunmalı: decoy, `IInteractable` yüzeyine gerçek/decoy
        // ayrımını sızdıran HİÇBİR public üye eklememeli. Tespit yalnız bileşen TİPİYLE
        // (GetComponent) yapılır — bu da yalnız edit/build-time'da okunur.
        //
        // STATIC de taranır (gate bulgusu: `public static bool IsDecoy` ya da bir nested
        // public tip yalnız Instance taramasından kaçardı). İMZA karşılaştırılır, yalnız
        // ad değil — `OnInteract(GameObject)` gibi bir AŞIRI YÜKLEME ad eşleşmesinden
        // geçerdi. Attribute'lar KASITLI olarak taranmıyor (bir `[DecoyMarker]` sızabilir);
        // arayüzün kendi tarafı `interactable_registry_core_test.cs`'te ayrıca pinli.
        var interfaceSignatures = typeof(IInteractable)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(Signature)
            .ToHashSet();

        MemberInfo[] declared = typeof(DecoyInteractable).GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (MemberInfo member in declared)
        {
            if (member is MethodInfo method && method.IsSpecialName)
            {
                continue; // property getter/setter'lar
            }

            if (member.MemberType == MemberTypes.Constructor)
            {
                continue; // örtük ctor — yüzey değil
            }

            Assert.IsTrue(interfaceSignatures.Contains(Signature(member)),
                $"'{Signature(member)}' IInteractable'da yok — decoy public yüzeyi kamuflajı sızdırıyor.");
        }
    }

    /// <summary>Üye imzası — ad + tür + (metotsa) parametre tipleri.</summary>
    private static string Signature(MemberInfo member)
    {
        if (member is MethodInfo method)
        {
            string parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name));
            return $"M:{method.Name}({parameters})";
        }

        return member switch
        {
            PropertyInfo property => $"P:{property.Name}:{property.PropertyType.Name}",
            FieldInfo field => $"F:{field.Name}:{field.FieldType.Name}",
            EventInfo evt => $"E:{evt.Name}",
            _ => $"?:{member.Name}",
        };
    }

    [Test]
    public void DecoyCheck_RunsThroughRealRegistry_AndReportsItself()
    {
        // Kayıtlı BEŞ check birlikte koşarken decoy ihlali gerçekten yakalanmalı
        // (üyelik testi tek başına bunu kanıtlamıyordu — isik-volume emsali).
        var exception = Assert.Throws<BuildFailedException>(() =>
            BuildValidationRunner.RunAll(BuildValidationRegistry.Checks, new FakeWalker("Assets/Scenes/Depot.unity")));

        StringAssert.Contains("Fpc/DecoyPresence", exception.Message,
            "Gerçek registry üzerinden koşulduğunda hata decoy check'inden gelmeli.");
    }

    // ── AC-2: Check kaydı ──

    [Test]
    public void DecoyCheck_IsRegistered_InSceneScanPhase()
    {
        IBuildCheck registered = BuildValidationRegistry.Checks
            .FirstOrDefault(c => c is FpcDecoyPresenceCheck);

        Assert.IsNotNull(registered, "Decoy check'i BuildValidationRegistry.Checks'e kayıtlı olmalı.");
        Assert.AreEqual(BuildCheckPhase.SceneScan, registered.Phase, "SceneScan fazında koşmalı.");
        Assert.AreEqual("Fpc/DecoyPresence", registered.Name);
    }

    // ── AC-3: Eksik decoy → hata (sahne adı mesajda) ──

    [Test]
    public void MvpAreaScene_WithNoDecoy_FailsBuild_WithSceneNameInMessage()
    {
        var exception = Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Depot.unity"));

        StringAssert.Contains("Depot", exception.Message, "Mesaj suçlu sahnenin adını taşımalı.");
        StringAssert.Contains("DecoyInteractable", exception.Message);
    }

    [Test]
    public void MvpAreaScene_WithOtherInteractablesButNoDecoy_StillFails()
    {
        // GDD'nin asıl senaryosu: sahnede başka içerik VAR ama decoy yok — kamuflaj
        // yine de çöker, çünkü o içerik (taşıma eşyası) alınınca registry'den çıkar.
        CreateNonDecoyInteractable();

        Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Ballroom.unity"),
            "Decoy olmayan interactable'lar gereksinimi karşılamamalı.");
    }

    [Test]
    public void BothMvpAreaScenes_AreChecked_ByLiteralName()
    {
        // LİTERAL adlar — listeyi kendine karşı gezmek totolojikti (bir ad düşse test
        // yine geçerdi). Ad listesinin mimariyle uyumu ayrıca FpcDecoySceneDriftTest'te.
        Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Depot.unity"));
        Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Ballroom.unity"));
    }

    [Test]
    public void SceneNameMatching_IsCaseInsensitive()
    {
        // Windows dosya sistemi `depot.unity`'ye izin verir — ordinal karşılaştırma
        // onu sessizce atlar ve kamuflaj garantisi fail-open olurdu.
        Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/depot.unity"),
            "Sahne adı eşleşmesi büyük/küçük harf duyarsız olmalı.");
    }

    [Test]
    public void SceneMatching_IsDirectoryInsensitive()
    {
        Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Levels/Depot.unity"),
            "Alt klasördeki MVP sahnesi de tanınmalı.");
    }

    // ── Prompt kamuflajı (AC-1'in bilinçli daraltılması) ──

    [Test]
    public void DecoyWithEmptyPrompt_FailsBuild()
    {
        // Promptsuz decoy, oyuncunun GÖRDÜĞÜ katmanda kamuflajı sızdırır (gate bulgusu).
        CreateDecoy(prompt: string.Empty);

        var exception = Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Depot.unity"));
        StringAssert.Contains("PromptText", exception.Message);
    }

    [Test]
    public void DecoyWithWhitespacePrompt_FailsBuild()
    {
        CreateDecoy(prompt: "   ");

        Assert.Throws<BuildFailedException>(() => RunCheckOver("Assets/Scenes/Depot.unity"),
            "Yalnız boşluktan oluşan prompt da boş sayılmalı.");
    }

    // ── AC-4: Mevcut decoy → sessiz ──

    [Test]
    public void MvpAreaScene_WithDecoy_PassesSilently()
    {
        CreateDecoy();

        Assert.DoesNotThrow(() => RunCheckOver("Assets/Scenes/Depot.unity"));
    }

    [Test]
    public void MvpAreaScene_WithInactiveDecoy_StillPasses()
    {
        // İnaktif objeler de build içeriğidir (isik-volume emsaliyle aynı kural).
        DecoyInteractable decoy = CreateDecoy();
        decoy.gameObject.SetActive(false);

        Assert.DoesNotThrow(() => RunCheckOver("Assets/Scenes/Depot.unity"),
            "İnaktif decoy da gereksinimi karşılamalı.");
    }

    // ── AC-5: throws/doesn't-throw çiftleri + kapsam sınırları ──

    [Test]
    public void NonMvpScene_IsIgnored_EvenWithoutDecoy()
    {
        // Kalıcı sahneler (UI/Player/Foundation) decoy taşımak zorunda DEĞİL.
        Assert.DoesNotThrow(() => RunCheckOver(
            "Assets/Scenes/UI.unity", "Assets/Scenes/Player.unity", "Assets/Scenes/Foundation.unity"));
    }

    [Test]
    public void EmptySceneWalk_IsSilent()
    {
        // Gerçek MVP sahneleri henüz Build Settings'te YOK — hiçbir sahne taranmazsa
        // check sessiz kalmalı. Yapısal olarak doğru: henüz içerik yok, ihlal de yok.
        Assert.DoesNotThrow(() => RunCheckOver());
    }

    [Test]
    public void MixedWalk_OnlyOffendingMvpSceneFails()
    {
        // Decoy YOK: kalıcı sahneler geçmeli, MVP alanı patlamalı — ve mesaj o sahneyi göstermeli.
        var exception = Assert.Throws<BuildFailedException>(() => RunCheckOver(
            "Assets/Scenes/UI.unity", "Assets/Scenes/Ballroom.unity"));

        StringAssert.Contains("Ballroom", exception.Message);
    }

    [Test]
    public void PerSceneAttribution_OnlyTheSceneMissingADecoyFails()
    {
        // Depot'ta decoy VAR, Ballroom'da YOK → yalnız Ballroom patlamalı ve mesaj onu
        // göstermeli. `FindObjectsByType` sahne-kör olduğu için fixture sahne açılışında
        // kurulup yıkılır — üretimde bunu `OpenSceneMode.Single` yapar (gate bulgusu:
        // bu ayrım hiç kanıtlanmamıştı).
        var walker = new FakeWalker(
            scenePath =>
            {
                foreach (GameObject go in _spawned)
                {
                    if (go != null) Object.DestroyImmediate(go);
                }
                _spawned.Clear();

                if (scenePath.Contains("Depot"))
                {
                    CreateDecoy();
                }
            },
            "Assets/Scenes/Depot.unity", "Assets/Scenes/Ballroom.unity");

        var exception = Assert.Throws<BuildFailedException>(() =>
            BuildValidationRunner.RunAll(new IBuildCheck[] { new FpcDecoyPresenceCheck() }, walker));

        StringAssert.Contains("Ballroom", exception.Message, "Yalnız decoy'suz sahne suçlanmalı.");
    }

    [Test]
    public void IsMvpAreaScene_HandlesNullAndEmptyPaths()
    {
        // AssetScan bağlamında ScenePath null gelir — check onu MVP alanı saymamalı.
        Assert.IsFalse(FpcDecoyPresenceCheck.IsMvpAreaScene(null));
        Assert.IsFalse(FpcDecoyPresenceCheck.IsMvpAreaScene(string.Empty));
        Assert.IsTrue(FpcDecoyPresenceCheck.IsMvpAreaScene("Assets/Scenes/Depot.unity"));
    }
}
