using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// birinci-sahis-kontrolcu Story 006: `DecoyInteractable`'ın ÇALIŞMA ZAMANI değeri —
/// registry kaydı. Build check'i decoy'un sahnede VAR olduğunu garantiler; ama decoy
/// kaydolmazsa taper'a hiç katkı vermez, kamuflaj yine çöker ve build yine yeşil kalır
/// (QL-TEST-COVERAGE bulgusu: bu, story'nin tüm runtime değeriydi ve test edilmemişti).
///
/// EditMode bunu kapsayamaz — MonoBehaviour yaşam döngüsü orada koşmaz. Diğer testler
/// register/deregister deyimini yerel test double'larıyla sınıyor; burada GERÇEK tip
/// sınanır. `interactable_registry_session_test.cs` deseniyle aynı.
/// </summary>
public class FpcDecoyRegistryTest
{
    private GameObject _decoyObject;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_decoyObject != null) Object.Destroy(_decoyObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DecoyInteractable_RegistersOnEnable_DeregistersOnDisable()
    {
        Assert.IsFalse(InteractableRegistry.Snapshot().Any(i => i is DecoyInteractable),
            "Ön koşul: kayıtlı decoy olmamalı.");

        _decoyObject = new GameObject("KapiKolu");
        var decoy = _decoyObject.AddComponent<DecoyInteractable>();
        yield return null;

        Assert.IsTrue(InteractableRegistry.Snapshot().Contains(decoy),
            "Decoy OnEnable'da registry'ye kaydolmalı — kaydolmazsa taper'a katkı vermez " +
            "ve kamuflaj build yeşilken sessizce çöker.");

        _decoyObject.SetActive(false);
        yield return null;

        Assert.IsFalse(InteractableRegistry.Snapshot().Contains(decoy),
            "Decoy OnDisable'da registry'den çıkmalı.");

        _decoyObject.SetActive(true);
        yield return null;

        Assert.IsTrue(InteractableRegistry.Snapshot().Contains(decoy),
            "Yeniden etkinleştirilen decoy tekrar kaydolmalı.");
    }
}
