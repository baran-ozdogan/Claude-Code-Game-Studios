using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEK SEFERLİK kurulum (seviye-sahne-gecisi Story 002): kalıcı
/// `Assets/Scenes/Foundation.unity` sahnesine `SceneTransitionManager` taşıyan
/// tek bir `GameObject` ekler.
///
/// Neden bir betik: `.unity` YAML'ını elle düzenlemek kırılgan ve gözden
/// geçirilemez. Emsal: `AnlatiStory003AddressablesSetup` (anlati Story 003).
///
/// İDEMPOTENT — obje zaten varsa hiçbir şey yapmaz, yani güvenle yeniden
/// koşulabilir.
/// </summary>
public static class SahneGecisiStory002FoundationSceneSetup
{
    private const string FoundationScenePath = "Assets/Scenes/Foundation.unity";
    private const string ManagerObjectName = "SceneTransitionManager";

    public static void Run()
    {
        Scene foundation = EditorSceneManager.OpenScene(FoundationScenePath, OpenSceneMode.Single);
        if (!foundation.IsValid())
        {
            Debug.LogError($"[SahneGecisiStory002Setup] '{FoundationScenePath}' açılamadı.");
            EditorApplication.Exit(1);
            return;
        }

        foreach (GameObject root in foundation.GetRootGameObjects())
        {
            // GetComponentInChildren: biri objeyi ileride bir FoundationRoot
            // altina yuvalarsa kok taramasi onu KACIRIR ve betik ikinci bir
            // kopya ekler (LP gate bulgusu).
            if (root.GetComponentInChildren<SceneTransitionManager>(true) != null)
            {
                Debug.Log($"[SahneGecisiStory002Setup] '{root.name}' zaten SceneTransitionManager taşıyor — no-op.");
                return;
            }
        }

        var host = new GameObject(ManagerObjectName);
        SceneManager.MoveGameObjectToScene(host, foundation);
        host.AddComponent<SceneTransitionManager>();

        EditorSceneManager.MarkSceneDirty(foundation);
        if (!EditorSceneManager.SaveScene(foundation))
        {
            // Donus degeri KONTROL EDILIYOR: kaydetme sessizce basarisiz olsa
            // betik "eklendi" diye loglar ve 0 ile cikardi — ADR-0008'in kendi
            // unity-specialist gecisinin SetActiveScene icin yakaladigi hata
            // sinifinin aynisi (LP gate bulgusu).
            Debug.LogError($"[SahneGecisiStory002Setup] '{FoundationScenePath}' KAYDEDILEMEDI.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[SahneGecisiStory002Setup] '{ManagerObjectName}' '{FoundationScenePath}' sahnesine eklendi.");
    }
}
