using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Story 004 AC-5/6: the boot flow loads exactly the three persistent scenes
/// (UI/Player/Foundation), is idempotent, and reloads them fresh in a simulated
/// second session. Runs inside the test runner's own scene, so assertions count
/// the persistent scenes explicitly instead of raw sceneCount (the production
/// "sceneCount == 3" claim maps to "3 persistent + the runner's test scene" here).
/// </summary>
public class BootPersistentScenesTest
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
        // Each test leaves the world as it found it (test isolation rule).
        // In-flight async loads (e.g. the UI scene's own Start-driven boot)
        // must finish loading before they can be unloaded — loop until no
        // persistent scene remains in the live list, with a frame guard.
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

    private static int LoadedCount(string sceneName)
    {
        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name == sceneName)
            {
                count++;
            }
        }
        return count;
    }

    [UnityTest]
    public IEnumerator Boot_LoadsAllThreePersistentScenes_AndNothingElse()
    {
        // Arrange
        int scenesBeforeBoot = SceneManager.sceneCount;

        // Act
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();

        // Assert — üç persistent sahne tam birer kez yüklü…
        foreach (string name in PersistentScenes)
        {
            Assert.AreEqual(1, LoadedCount(name), $"'{name}' tam bir kez yüklü olmalı (AC-5).");
        }

        // …ve boot başka hiçbir sahne eklememiş (test sahnesi hariç toplam +3).
        Assert.AreEqual(scenesBeforeBoot + 3, SceneManager.sceneCount,
            "Boot yalnız 3 persistent sahneyi eklemeli — başka sahne yok (AC-5).");
    }

    [UnityTest]
    public IEnumerator Boot_CalledTwice_IsIdempotent()
    {
        // Arrange
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        int sceneCountAfterFirstBoot = SceneManager.sceneCount;

        // Act — çift boot çağrısı (AC-5 edge case).
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();

        // Assert
        Assert.AreEqual(sceneCountAfterFirstBoot, SceneManager.sceneCount,
            "İkinci boot çağrısı hiçbir sahneyi yeniden yüklememeli (idempotent).");
        foreach (string name in PersistentScenes)
        {
            Assert.AreEqual(1, LoadedCount(name), $"'{name}' hâlâ tam bir kez yüklü olmalı.");
        }
    }

    [UnityTest]
    public IEnumerator Boot_SimulatedSecondSession_LoadsFreshScenes()
    {
        // Arrange — birinci "oturum": boot + Foundation sahnesine stale-marker bırak.
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();
        Scene foundation = SceneManager.GetSceneByName(PersistentSceneBootLoader.FoundationSceneName);
        var marker = new GameObject("StaleSessionMarker");
        SceneManager.MoveGameObjectToScene(marker, foundation);

        // Act — oturum sınırı simülasyonu: persistent sahneler kapanır, boot yeniden koşar
        // (AC-6'nın iki-oturum kalıbı; gerçek Reload-Scene-OFF profili CLI kanıt koşusunda).
        foreach (string name in PersistentScenes)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(name));
            while (!unload.isDone)
            {
                yield return null;
            }
        }
        yield return PersistentSceneBootLoader.EnsurePersistentScenesLoaded();

        // Assert — sahneler taze: stale kök objesi yok, her sahne tam bir kez.
        Assert.IsNull(GameObject.Find("StaleSessionMarker"),
            "İkinci oturumda stale kök objesi kalmamalı — sahneler taze yüklenmeli (AC-6).");
        foreach (string name in PersistentScenes)
        {
            Assert.AreEqual(1, LoadedCount(name), $"'{name}' ikinci oturumda tam bir kez yüklü olmalı.");
        }
    }
}
