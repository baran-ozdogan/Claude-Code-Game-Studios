using System.Linq;
using NUnit.Framework;

/// <summary>
/// seviye-sahne-gecisi Story 002, AC-3: `SceneTransitionManager`
/// `FoundationBootstrap.ResetAll()`'a KAYITLI DEĞİLDİR.
///
/// Bu, ADR-0008'in ADR-0001'de yaptığı düzeltmenin REGRESYON testidir. ADR-0001
/// bu servisi başlangıçta altı uniform statik servisten biri sayıyordu; sistem
/// `MonoBehaviour` barındırmaya geçince `ResetOnLoad()`'ı kalmadı ve ADR-0001
/// beş servise düzeltildi. Yaşam döngüsü kendi kalıcı sahnesinin `Awake()`'iyle
/// sıfırlanır — UI (ADR-0002) ve Player (ADR-0003) sahneleri gibi, ki onlar da
/// `ResetAll()`'da değil.
///
/// Test, "tutarlılık olsun" diye ekleme dürtüsünü yakalar: eklenirse
/// `ResetAll()` `SubsystemRegistration` zamanında var olmayan bir `Instance`'a
/// dokunmaya çalışır.
/// </summary>
public class SahneGecisiBootstrapTest
{
    [Test]
    public void SceneTransitionManager_IsNotRegisteredInFoundationBootstrap()
    {
        var offenders = FoundationBootstrap.ActiveResetOrder
            .Where(name => name.Contains("SceneTransition") || name.Contains("SahneGecisi")
                           || name.Contains("SeviyeSahneGecisi"))
            .ToList();

        CollectionAssert.IsEmpty(offenders,
            "SceneTransitionManager ResetAll()'a KAYITLI OLAMAZ: ResetOnLoad()'ı yoktur ve Instance'ı " +
            "SubsystemRegistration zamanında henüz mevcut değildir (ADR-0008, ADR-0001'i altı→beş " +
            "servise düzeltti). Bulunanlar: " + string.Join(", ", offenders));
    }

    [Test]
    public void SceneTransitionManager_AbsenceIsNotVacuous_BootstrapStillResetsRealServices()
    {
        // "SceneTransition yok" iddiası BOŞ bir dizide vakumda geçerdi — bu test
        // onu anlamlı tutuyor.
        //
        // Ama TAM SIRAYI burada PİNLEMİYORUZ: o, `foundation_bootstrap_order_test.cs`'in
        // (kendini "kasıtlı olarak kırılgan" ilan eden) işi ve her Foundation
        // epic'inde güncelleniyor. Kopyalamak, yeni bir servis eklendiğinde iki
        // dosyada iki sabit diziyi güncellemek demekti — sıfır ek ayırt edicilik,
        // iki bakım noktası (LP+QL gate bulgusu: birebir kopyaydı).
        var active = FoundationBootstrap.ActiveResetOrder.ToList();

        CollectionAssert.IsNotEmpty(active, "Reset sırası boş — absence iddiası anlamsızlaşır.");
        CollectionAssert.Contains(active, "InteractableRegistry",
            "Bilinen bir statik servis hâlâ kayıtlı olmalı (dizi gerçekten okunuyor).");
    }
}
