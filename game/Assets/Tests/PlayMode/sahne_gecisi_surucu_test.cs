using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// seviye-sahne-gecisi Story 002: `MonoBehaviour` sürücü + kalıcı Foundation
/// sahnesi + `Instance` facade'ı (TR-sahne-gecisi-001'in barındırma yarısı).
///
/// Bu dosya ZORUNLU olarak PlayMode'dur: `Awake()` bu projede Play Mode DIŞINDA
/// hiç koşmuyor (fpc Story 001'de ampirik olarak doğrulandı — Edit Mode'da da
/// koşmaz, `yield return null` sonrası da). Duplicate guard ve `Instance`
/// yaşam döngüsü yalnız burada sınanabilir.
///
/// Oturum sınırı, `fpc_persistent_scene_test.cs`/`UIRootStaleInstanceTest`
/// emsaliyle unload/reload ile SİMÜLE edilir — gerçek Editor "Reload Scene"
/// ayarı DEĞİŞTİRİLMEZ.
/// </summary>
public class SahneGecisiSurucuTest
{
    private static readonly string[] PersistentScenes =
    {
        PersistentSceneBootLoader.UISceneName,
        PersistentSceneBootLoader.PlayerSceneName,
        PersistentSceneBootLoader.FoundationSceneName,
    };

    private GameObject _duplicate;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_duplicate != null)
        {
            Object.DestroyImmediate(_duplicate);
            _duplicate = null; // NUnit fixture'i tek instance — sonraki teste tasinmasin
        }

        for (int guardFrames = 0; guardFrames < 600; guardFrames++)
        {
            bool anyPresent = false;
            foreach (string name in PersistentScenes)
            {
                if (!PersistentSceneBootLoader.TryFindScene(name, out Scene scene))
                {
                    continue;
                }

                anyPresent = true;
                if (scene.isLoaded)
                {
                    AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
                    while (op != null && !op.isDone)
                    {
                        yield return null;
                    }
                }
            }

            if (!anyPresent)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail("Teardown: persistent sahneler 600 frame içinde temizlenemedi.");
    }

    private static IEnumerator BootAndSettle()
    {
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        yield return null;
    }

    // ── AC-1: Duplicate guard ──

    [UnityTest]
    public IEnumerator DuplicateInstance_LogsErrorAndDestroysItself()
    {
        yield return BootAndSettle();

        ISceneTransitionManager original = SceneTransitionManager.Instance;
        Assert.IsNotNull(original, "Foundation sahnesi sürücüyü kurmalı.");

        // KOŞULSUZ Debug.LogError bekleniyor — Debug.Assert olsaydı shipping
        // build'de hiç derlenmez ve sıfır koruma verirdi (ADR-0003 emsali).
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Duplicate SceneTransitionManager"));

        _duplicate = new GameObject("DuplicateManager");
        _duplicate.AddComponent<SceneTransitionManager>();
        yield return null;

        Assert.AreSame(original, SceneTransitionManager.Instance,
            "Instance HÂLÂ ilk sürücüyü göstermeli — ikinci instance ona dokunmamalı.");
        Assert.IsTrue(_duplicate == null,
            "İkinci GameObject kendini yok etmeli (Unity'nin == aşırı yüklemesiyle kontrol).");
    }

    // ── AC-2: İki-oturum bayatlık ──

    [UnityTest]
    public IEnumerator Instance_IsNeverStaleAcrossTwoSessions()
    {
        // `_instance` yalnız `Awake()`'te set edilir. Unity'nin "Reload Scene"
        // Enter Play Mode ayarı KAPALIYKEN Stop→Play sınırında `Awake()`
        // yeniden koşmayabilir ve `Instance` yok edilmiş bir nesneye işaret
        // edebilir — ADR-0003'ün `PlayerStateProvider`'ıyla birebir aynı risk,
        // ADR-0008 Validation Criteria bunu açıkça istiyor.
        // QL gate bulgusu: bu testin ilk hâli TİYATROYDU. Unload/reload objeyi
        // yok edip yeniden yarattığı için `Awake()` her zaman koşuyor ve
        // `SceneTransitionManager.cs`'te bu testi kırmızıya döndüren HİÇBİR
        // mutasyon yoktu — `OnDestroy`'u tamamen silmek bile yeşil kalıyordu.
        // Aşağıdaki dört iddia bunu ayırt edici hâle getiriyor.
        yield return BootAndSettle();
        var firstSession = (SceneTransitionManager)SceneTransitionManager.Instance;
        Assert.IsNotNull(firstSession);

        // Sürücü GERÇEKTEN mevcut Foundation sahnesinde yaşıyor (AC'nin "yeni
        // sahne oluşturulmaz" yarısı, başka hiçbir yerde kapsanmıyordu).
        Assert.AreEqual(PersistentSceneBootLoader.FoundationSceneName,
            firstSession.gameObject.scene.name, "Sürücü kalıcı Foundation sahnesinde yaşamalı.");

        // 1. oturumun çekirdeğini KİRLET: 2. oturum bunu taşırsa yaşam döngüsü
        // sahne Awake'iyle sıfırlanmıyor demektir — ADR-0008'in bu servisi
        // ResetAll()'a KAYDETMEME gerekçesinin ta kendisi.
        firstSession.InternalStateForTests.SetState(TransitionState.Preloading, TransitionType.Soft);

        yield return TearDown(); // oturum sınırı

        // AYIRT EDİCİ 1: sahne unload olunca `Instance` GERÇEK null olmalı,
        // yok edilmiş bir karkas (fake-null) DEĞİL. `OnDestroy`'daki
        // `_instance = null` silinirse BURASI kırmızıya döner.
        Assert.IsTrue(ReferenceEquals(SceneTransitionManager.Instance, null),
            "Oturum bitince Instance gerçek null olmalı — bayat karkas bırakılamaz.");

        yield return BootAndSettle();
        ISceneTransitionManager secondSession = SceneTransitionManager.Instance;

        // Bayat bir referansta `ReferenceEquals(x, null)` FALSE ama `x == null`
        // TRUE olur (Unity'nin fake-null'u). `UnityEngine.Object` AÇIKÇA yazılı:
        // dosyaya bir gün `using System;` eklenirse `Object` CS0104 ile belirsiz
        // hâle gelirdi.
        Assert.IsFalse(secondSession is UnityEngine.Object carcass && carcass == null,
            "Instance ikinci oturumda YOK EDİLMİŞ bir nesneyi gösteriyor (bayat referans).");
        Assert.IsNotNull(secondSession);

        // AYIRT EDİCİ 2: TAZE nesne. Bir singleton'a en olası "düzeltme" olan
        // `DontDestroyOnLoad(gameObject)` eklenirse burası kırmızıya döner.
        Assert.AreNotSame(firstSession, secondSession,
            "İkinci oturum TAZE bir SceneTransitionManager kurmalı.");

        // AYIRT EDİCİ 3: NATIVE tarafa dokun — karkasta MissingReferenceException
        // atardı. `CurrentState` yönetilen bir alan okuduğu için karkasta bile
        // çalışır, yani tek başına canlılık kanıtı DEĞİL.
        Assert.AreEqual(PersistentSceneBootLoader.FoundationSceneName,
            ((SceneTransitionManager)secondSession).gameObject.scene.name,
            "İkinci oturumun sürücüsü de erişilebilir ve Foundation sahnesinde olmalı.");

        // AYIRT EDİCİ 4: çekirdek de TAZE — 1. oturumun Preloading'i taşınamaz.
        Assert.AreEqual(TransitionState.Idle, secondSession.CurrentState,
            "İkinci oturum taze bir çekirdekle başlamalı.");
    }

    // ── AC-4: Event forward'ı ──

    [UnityTest]
    public IEnumerator DriverForwardsCoreStateEvents_WithoutTransformingThePayload()
    {
        yield return BootAndSettle();

        var manager = (SceneTransitionManager)SceneTransitionManager.Instance;
        var observed = new List<(TransitionState state, TransitionType type)>();

        void Record(TransitionState state, TransitionType type) => observed.Add((state, type));
        manager.OnTransitionStateChanged += Record;

        try
        {
            // Çekirdeği DOĞRUDAN sürüyoruz: sürücünün geçiş gövdeleri Story 003+
            // ile geliyor. Test edilen şey forward'ın kendisi.
            manager.InternalStateForTests.SetState(TransitionState.Preloading, TransitionType.Soft);
            yield return null;

            Assert.AreEqual(1, observed.Count, "Forward TAM BİR KEZ olmalı — çift abonelik yok.");
            Assert.AreEqual((TransitionState.Preloading, TransitionType.Soft), observed[0],
                "Payload dönüştürülmeden taşınmalı.");
            Assert.AreEqual(TransitionState.Preloading, manager.CurrentState,
                "Sürücünün CurrentState'i çekirdeğe delege etmeli, kendi kopyasını tutmamalı.");
        }
        finally
        {
            manager.OnTransitionStateChanged -= Record;
        }
    }

    [UnityTest]
    public IEnumerator DriverUnsubscribe_ActuallyDetaches()
    {
        // Forward'ı `add`/`remove` accessor'larıyla yazmanın tuzağı: `remove`
        // unutulur ya da yanlış listeye giderse abone kopmaz. Aboneliğini
        // bırakan bir tüketici olay almaya devam ederse bu sessiz bir sızıntıdır.
        yield return BootAndSettle();

        var manager = (SceneTransitionManager)SceneTransitionManager.Instance;
        int calls = 0;
        void Count(TransitionState state, TransitionType type) => calls++;

        manager.OnTransitionStateChanged += Count;
        manager.OnTransitionStateChanged -= Count;

        manager.InternalStateForTests.SetState(TransitionState.Ready, TransitionType.Hard);
        yield return null;

        Assert.AreEqual(0, calls, "Abonelikten çıkan tüketici olay ALMAMALI.");
    }
}
