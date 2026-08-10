using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// TRIPWIRE — `FpcDecoyPresenceCheck.MvpAreaSceneNames`'in bayatlamasını yakalar.
///
/// Neden gerekli: decoy check'i sahneleri ADA göre tanır. Gerçek seviye sahneleri
/// henüz Build Settings'te yok, bu yüzden check bugün hiçbir şey taramıyor — ve bu
/// YAPISAL OLARAK doğru (içerik yok, ihlal de yok). Ama sahneler yarın BAŞKA bir adla
/// gelirse (`Depot_01`, `Level_Storage`, ...) check sonsuza dek sessizce geçer ve
/// story'nin var oluş amacı (kamuflajı yapısal olarak garanti altına almak) sessizce
/// çöker. Bu, "sıfır sahne" durumundan FARKLI bir hata modu: fail-open.
///
/// Bu test o boşluğu kapatır: Build Settings'e giren HER sahne ya bilinen bir kalıcı
/// sahne olmalı, ya da decoy check'inin tanıdığı bir MVP alanı olmalı. Tanınmayan bir
/// sahne eklendiği an kırmızıya döner ve ad listesinin uzlaştırılmasını zorlar.
///
/// (Gate bulgusu: LP ve QL bağımsız olarak aynı fail-open riskini işaret etti — ilk
/// implementasyonun ad listesi mimarinin gerçek adlarıyla hiç eşleşmiyordu bile:
/// "Depo/ServisKoridoru/BaloSalonu" yazılmıştı, mimari `Depot`/`Ballroom` kullanıyor.)
/// </summary>
public class FpcDecoySceneDriftTest
{
    /// <summary>
    /// Decoy taşımak zorunda OLMAYAN sahneler: üç kalıcı sahne (ADR-0002/0003/0008 —
    /// oyuncunun "gezdiği alan" değiller) + proje-kurulumu'nun boş smoke sahnesi.
    /// </summary>
    private static readonly HashSet<string> ExemptSceneNames = new HashSet<string>
    {
        "UI",
        "Player",
        "Foundation",
        "EmptyTest",
    };

    [Test]
    public void EveryBuildScene_IsEitherExempt_OrRecognizedAsMvpArea()
    {
        var unrecognized = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
            {
                continue;
            }

            string sceneName = Path.GetFileNameWithoutExtension(scene.path);
            if (ExemptSceneNames.Contains(sceneName))
            {
                continue;
            }

            if (!FpcDecoyPresenceCheck.IsMvpAreaScene(scene.path))
            {
                unrecognized.Add(scene.path);
            }
        }

        Assert.IsEmpty(unrecognized,
            "Build Settings'te decoy check'inin TANIMADIĞI sahne(ler) var: " +
            string.Join(", ", unrecognized) + ". Ya FpcDecoyPresenceCheck.MvpAreaSceneNames'e " +
            "eklenmeli (oyuncunun gezdiği bir alansa) ya da bu testin ExemptSceneNames listesine " +
            "(kalıcı/araç sahnesiyse). Aksi hâlde decoy kamuflaj garantisi o sahnede sessizce çöker.");
    }

    [Test]
    public void MvpAreaSceneNames_MatchArchitectureLockedNames()
    {
        // Adlar mimarinin kilitli değerleri — literal olarak pinlenir (listeyi kendine
        // karşı doğrulayan totolojik bir test bu sapmayı yakalayamazdı, gate bulgusu).
        // Kaynaklar: control-manifest.md Scenes/Prefabs satırı (`Depot`, `Ballroom`),
        // ADR-0015 `_initialLevelSceneName = "Depot"`, ADR-0011 iki-kat MVP'si.
        CollectionAssert.AreEquivalent(
            new[] { "Depot", "Ballroom" },
            FpcDecoyPresenceCheck.MvpAreaSceneNames,
            "MVP alan adları mimarinin kilitli sahne adlarıyla eşleşmeli.");
    }
}
