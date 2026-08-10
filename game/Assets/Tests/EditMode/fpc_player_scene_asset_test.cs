using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// birinci-sahis-kontrolcu Story 004 AC-1'in asset-seviyesi yarısı: `Player.unity`,
/// Story 003'ün prefab'ının bir INSTANCE'ını taşımalı — elle yeniden inşa edilmiş
/// (ya da unpack edilmiş) bir kopya değil ("kontrolcü İKİ KEZ İNŞA EDİLMEZ").
///
/// Neden EditMode: prefab bağlantısı yalnız Editor metadata'sıdır — PlayMode'da
/// yüklenmiş bir sahnede `PrefabUtility.IsPartOfPrefabInstance` false döner
/// (ampirik). Bu yüzden iddia sahne ASSET'i üzerinden sınanır.
/// </summary>
public class FpcPlayerSceneAssetTest
{
    private const string PlayerScenePath = "Assets/Scenes/Player.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    [Test]
    public void PlayerScene_ContainsPrefabInstance_NotAHandBuiltCopy()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Assert.IsNotNull(prefab, $"{PlayerPrefabPath} bulunmalı (Story 003 çıktısı).");

        Scene scene = EditorSceneManager.OpenScene(PlayerScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject playerRoot = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<FirstPersonController>() != null)
                {
                    playerRoot = root;
                    break;
                }
            }

            Assert.IsNotNull(playerRoot, "Player.unity bir FirstPersonController taşıyan kök içermeli.");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(playerRoot),
                "Sahnedeki oyuncu, Player.prefab'ın bir INSTANCE'ı olmalı (elle inşa edilmiş kopya değil).");

            Object source = PrefabUtility.GetCorrespondingObjectFromSource(playerRoot);
            Assert.AreEqual(PlayerPrefabPath, AssetDatabase.GetAssetPath(source),
                "Instance'ın kaynağı tam olarak Assets/Prefabs/Player.prefab olmalı.");

            // Kök tek olmalı — ikinci bir oyuncu kökü duplicate-guard'ı her boot'ta tetiklerdi.
            int playerRootCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<PlayerStateProvider>() != null)
                {
                    playerRootCount++;
                }
            }
            Assert.AreEqual(1, playerRootCount, "Player.unity'de tam olarak bir oyuncu kökü olmalı.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }
    }
}
