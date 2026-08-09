using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot loader for the three persistent scenes (ADR-0002 UI, ADR-0003 Player,
/// ADR-0008 Foundation). Lives on the UI scene's root object; the UI scene is
/// the build's ONLY initial scene, and this component additively loads the
/// remaining persistent scenes sequentially awaited, in the documented
/// UI → Player → Foundation order (ADR-0003's ordering decision).
///
/// Boot contract (ADR-0015, minimal boot): the initial load set is persistent
/// scenes ONLY. The depot/level scene is NEVER loaded here — that call belongs
/// exclusively to SahneKesmeliAnlatiController's night-begin orchestration
/// (sahne-kesme epic'i), which runs setup (StartNight +
/// SetTotalConfiguredTriggerCountForNight) BEFORE the initial depot load.
/// Adding a level scene to Build Settings is fine, but it stays UNCHECKED —
/// level scenes load only through SceneTransitionManager at runtime.
/// </summary>
public sealed class PersistentSceneBootLoader : MonoBehaviour
{
    public const string UISceneName = "UI";
    public const string PlayerSceneName = "Player";
    public const string FoundationSceneName = "Foundation";

    // Documented boot order — UI first (it is the containing scene in
    // production and skips as already-loaded), then Player, then Foundation.
    private static readonly string[] _bootOrder =
    {
        UISceneName,
        PlayerSceneName,
        FoundationSceneName,
    };

    private void Start()
    {
        StartCoroutine(EnsurePersistentScenesLoaded());
    }

    /// <summary>
    /// Idempotent, sequentially-awaited additive load of every persistent scene
    /// that is not already loaded — including against a CONCURRENT boot (the UI
    /// scene's own Start-driven boot vs. a test-driven call): a scene whose
    /// async load is already in flight appears in the scene list with
    /// isLoaded=false, and is awaited instead of loaded a second time.
    /// Coroutine while(!op.isDone) is the project idiom (ADR-0008); Awaitable
    /// is deliberately not used. Internal so the boot tests can drive it.
    /// </summary>
    internal static IEnumerator EnsurePersistentScenesLoaded()
    {
        foreach (string sceneName in _bootOrder)
        {
            if (TryFindScene(sceneName, out Scene existing))
            {
                // Already loaded, or another boot's async load is in flight —
                // await it, never issue a duplicate load.
                while (TryFindScene(sceneName, out Scene pending) && !pending.isLoaded)
                {
                    yield return null;
                }
                continue;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone)
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Scans the live scene list (loading scenes included — unlike a bare
    /// GetSceneByName().isLoaded check) for a scene with the given name.
    /// </summary>
    internal static bool TryFindScene(string sceneName, out Scene found)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                found = scene;
                return true;
            }
        }

        found = default;
        return false;
    }
}
