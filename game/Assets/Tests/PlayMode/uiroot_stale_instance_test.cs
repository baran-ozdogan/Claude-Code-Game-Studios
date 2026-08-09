using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// Story 005 AC-5 (ADR-0010 Validation Criteria kalıbı): UIRoot.Instance must
/// be fresh and valid in a second simulated session (persistent scenes unloaded
/// and re-booted), with no duplicate-guard error — the Reload-Scene-off staleness
/// class. Also verifies the 4 named skeleton elements exist and start hidden.
/// </summary>
public class UIRootStaleInstanceTest
{
    private static readonly string[] PersistentScenes =
    {
        PersistentSceneBootLoader.UISceneName,
        PersistentSceneBootLoader.PlayerSceneName,
        PersistentSceneBootLoader.FoundationSceneName,
    };

    [UnityTearDown]
    public IEnumerator TearDown()
    {
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

    [UnityTest]
    public IEnumerator UIRoot_SecondSimulatedSession_InstanceIsFreshAndValid()
    {
        // Arrange — birinci "oturum".
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        yield return null; // UI layout/Awake otursun
        Assert.IsNotNull(UIRoot.Instance, "Birinci oturumda UIRoot.Instance set olmalı.");
        UIRoot firstSessionInstance = UIRoot.Instance;
        Assert.IsNotNull(UIRoot.Instance.Root, "Birinci oturumda Root geçerli olmalı.");

        // Act — oturum sınırı: persistent sahneler kapanır, boot yeniden koşar.
        foreach (string name in PersistentScenes)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(name));
            while (!unload.isDone)
            {
                yield return null;
            }
        }
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        yield return null;

        // Assert — ikinci oturumda Instance taze ve geçerli; duplicate-guard
        // tetiklenmedi (LogError üretse test kendiliğinden kırmızıya düşerdi).
        Assert.IsNotNull(UIRoot.Instance, "İkinci oturumda UIRoot.Instance set olmalı (stale değil).");
        Assert.AreNotSame(firstSessionInstance, UIRoot.Instance,
            "İkinci oturum taze bir UIRoot instance'ı kurmalı.");
        Assert.IsNotNull(UIRoot.Instance.Root, "İkinci oturumda Root geçerli olmalı.");
    }

    [UnityTest]
    public IEnumerator MainUI_SkeletonElements_ExistAndStartHidden()
    {
        // Arrange
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        yield return null; // layout pass

        VisualElement root = UIRoot.Instance.Root;
        string[] elementNames = { "crosshair-container", "crosshair", "hold-fill-ring", "stinger-caption", "dialogue-subtitle" };

        foreach (string name in elementNames)
        {
            // Act
            VisualElement element = root.Q<VisualElement>(name);

            // Assert — adlar birebir (sahip epic'lerinin sorgu sözleşmesi).
            Assert.IsNotNull(element, $"'{name}' MainUI.uxml ağacında olmalı.");
        }

        // İskelet başlangıç durumu: üç üst-seviye öğe görünmez. Stil çözümleme
        // asenkron — panel güncellenene kadar bekle (batch koşularda gecikebilir).
        VisualElement container = root.Q<VisualElement>("crosshair-container");
        for (int frame = 0; frame < 60 && container.resolvedStyle.display != DisplayStyle.None; frame++)
        {
            yield return null;
        }

        foreach (string name in new[] { "crosshair-container", "stinger-caption", "dialogue-subtitle" })
        {
            VisualElement element = root.Q<VisualElement>(name);
            Assert.AreEqual(DisplayStyle.None, element.resolvedStyle.display,
                $"'{name}' iskelette görünmez başlamalı (display: none).");
        }
    }
}
