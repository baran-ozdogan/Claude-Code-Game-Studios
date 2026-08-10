using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// birinci-sahis-kontrolcu Story 001 AC-2 (duplicate-instance guard,
/// ADR-0003). PlayMode'da, KOŞULSUZ olarak — Unity, Play Mode dışında
/// (`[ExecuteAlways]` olmadıkça) Awake()'i hiç çağırmaz; bu davranış
/// `Tests/EditMode/fpc_player_state_lock_test.cs`'te ampirik olarak
/// doğrulandı (yield'li denemeler dahil Awake() hiç ateşlemedi). Guard
/// mantığı Awake() üzerinden çalıştığı için gerçek Play Mode gerekli —
/// aynı desen UIRootStaleInstanceTest'te de kullanılıyor.
/// </summary>
public class FpcPlayerStateDuplicateGuardTest
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var provider in Object.FindObjectsByType<PlayerStateProvider>(FindObjectsSortMode.None))
        {
            Object.Destroy(provider.gameObject);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator Awake_SecondInstanceWhileFirstExists_DestroysSelf_CurrentUnchanged()
    {
        // Arrange — ilk instance.
        var firstGo = new GameObject("PlayerStateProviderTest");
        var first = firstGo.AddComponent<PlayerStateProvider>();
        yield return null;
        Assert.AreSame(first, PlayerStateProvider.Current, "Birinci instance Current'ı set etmeli.");

        // Act — ikinci bir instance başka bir GameObject'te.
        var secondGo = new GameObject("SecondProvider");
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Duplicate PlayerStateProvider"));
        var second = secondGo.AddComponent<PlayerStateProvider>();
        yield return null;

        // Assert — ikincisi yok edildi (destroyed sentinel — Unity ==null override'ı),
        // Current hâlâ ilkini gösteriyor.
        Assert.IsTrue(second == null, "İkinci instance'ın GameObject'i yok edilmiş olmalı.");
        Assert.AreSame(first, PlayerStateProvider.Current, "Current, ikinci instance yok edildikten sonra hâlâ ilkini göstermeli.");

        // Assert — QL-TEST-COVERAGE bulgusu: hayatta kalan instance'ın kilit
        // API'si, üzerinde bir duplicate-guard olayı geçtikten SONRA da çalışmalı
        // (Awake() HashSet'lere dokunmuyor ama bu varsayımı doğrudan sına).
        first.RequestMovementLock(this);
        Assert.IsTrue(first.IsLocked, "Duplicate-guard olayından sonra hayatta kalan instance'ın kilit API'si çalışmalı.");
        first.ReleaseMovementLock(this);
    }
}
