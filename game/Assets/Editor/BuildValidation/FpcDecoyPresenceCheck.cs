using System.IO;
using UnityEngine;

/// <summary>
/// TR-fpc-016 / GDD AC17: her MVP alanı sahnesi en az bir `DecoyInteractable`
/// taşımalı. Gerekçe (GDD Core Rules, design-review 2026-08-04'ün en önemli
/// bulgularından biri): yaklaşma-yavaşlaması kamuflajı YALNIZ registry'de
/// anı-tetikleyicilerle BİRLİKTE başka nesneler de kayıtlıyken işe yarar. Gerçek
/// içerik dağılımında Servis Koridoru ve Balo Salonu'nda başka `IInteractable`
/// YOK, Depo'daki taşıma eşyaları da alınır alınmaz registry'den çıkıyor — yani
/// oyun süresinin büyük kısmında yavaşlama %100 kesinlikle bir anı-tetikleyiciyi
/// işaret ederdi (Pillar 5'i zayıflatan "metal dedektörü" istismarı).
///
/// Bu check o içerik gereksinimini YAPISAL olarak zorlar — level-design
/// disiplinine bırakmaz (manifest: build-blocking, pointed mesaj).
///
/// Sahneler henüz Build Settings'te yoksa hiçbir sahne taranmaz ve check sessiz
/// kalır — YAPISAL OLARAK doğru: henüz içerik yok, henüz ihlal de yok. Gerçek MVP
/// sahnelerinin yazımı Presentation/level-design aşamasına ait (Out of Scope).
/// </summary>
internal sealed class FpcDecoyPresenceCheck : IBuildCheck
{
    /// <summary>
    /// MVP seviye sahneleri. Adlar mimarinin KİLİTLİ değerleri: `control-manifest.md`
    /// (Scenes/Prefabs satırı, örnekler `Depot`/`Ballroom`), ADR-0015
    /// (`_initialLevelSceneName = "Depot"`), ADR-0011 (kat sahnesi adı
    /// `gameObject.scene.name`'den türer; MVP = tam olarak 2 kat).
    ///
    /// **Servis Koridoru KASITLI olarak burada YOK**: o bir SAHNE değil, bir kat
    /// sahnesinin içindeki bir ALAN (ADR-0011: MVP tam olarak iki kat sahnesi).
    /// Per-scene granülerlik "koridor alt-alanında decoy olsun" iddiasını ifade
    /// EDEMEZ — bu check onu içeren kat sahnesinde en az bir decoy garantiler;
    /// koridorun kendi kamuflajı level-design'a devredilen bir içerik kuralıdır
    /// (bilinçli sınır, sessiz boşluk değil).
    ///
    /// Kalıcı sahneler (UI/Player/Foundation) bu listede DEĞİL — oyuncunun gezdiği
    /// alanlar değiller. Listenin bayatlaması `FpcDecoySceneDriftTest` ile yakalanır.
    /// </summary>
    internal static readonly string[] MvpAreaSceneNames =
    {
        "Depot",
        "Ballroom",
    };

    public string Name => "Fpc/DecoyPresence";
    public BuildCheckPhase Phase => BuildCheckPhase.SceneScan;

    public void Run(BuildCheckContext context)
    {
        if (!IsMvpAreaScene(context.ScenePath))
        {
            return;
        }

        string sceneName = Path.GetFileNameWithoutExtension(context.ScenePath);

        DecoyInteractable[] decoys = Object.FindObjectsByType<DecoyInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        if (decoys.Length == 0)
        {
            context.Fail(
                $"MVP alanı sahnesi '{sceneName}' hiç DecoyInteractable içermiyor — en az bir " +
                "tane gerekli. Kamuflaj olmadan yaklaşma-yavaşlaması anı-tetikleyici " +
                "dedektörüne dönüşür (TR-fpc-016, GDD AC17).");
        }

        // Boş prompt kamuflajı OYUNCUNUN GÖRDÜĞÜ katmanda sızdırır: Etkileşim crosshair'i
        // `PromptText`i çizer, gerçek nesneler "Al"/"Çek" gösterirken promptsuz bir decoy
        // hiçbir şey göstermez — oyuncu "yavaşlama + yazı yok = decoy" kuralını öğrenir ve
        // metal-dedektörü istismarı geri açılır. GDD decoy'lardan "minimal/TATSIZ bir tepki"
        // istiyor: tepkisiz değil, tatsız. (LP+QL gate bulgusu; story AC-1'in "boş prompt"
        // izninin BİLİNÇLİ daraltılması — gerekçe burada, Completion Notes'ta da kayıtlı.)
        foreach (DecoyInteractable decoy in decoys)
        {
            if (string.IsNullOrWhiteSpace(decoy.PromptText))
            {
                context.Fail(
                    $"'{sceneName}' sahnesindeki decoy '{decoy.name}' boş PromptText taşıyor — " +
                    "promptsuz decoy kamuflajı oyuncuya sızdırır (TR-fpc-016, GDD AC17).");
            }
        }
    }

    /// <summary>
    /// Sahne adı MVP alan listesinde mi (yol/uzantı duyarsız). Inaktif objeler de
    /// sayılır — build içeriğine dahiller.
    /// </summary>
    internal static bool IsMvpAreaScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            return false;
        }

        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        foreach (string mvpArea in MvpAreaSceneNames)
        {
            // Büyük/küçük harf duyarsız: Windows dosya sistemi `depot.unity`'ye izin verir
            // ve ordinal karşılaştırma onu sessizce atlardı (fail-open — gate bulgusu).
            if (string.Equals(sceneName, mvpArea, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
