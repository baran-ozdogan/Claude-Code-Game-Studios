# Story 006: Decoy içerik build-time doğrulaması

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/birinci-sahis-kontrolcu.md` (Core Rules — "Kamuflajın gerçek içerikle çökmesi ve düzeltmesi"; AC17)
**Requirement**: `TR-fpc-016`
*(Requirement metni `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0003 (secondary — AC17'nin kaynağı); paylaşılan utility deseni ADR-0014/proje-kurulumu Story 006, ilk sahne-scan tüketicisi isik-volume Story 006
**ADR Decision Summary**: Yaklaşma-yavaşlaması TÜM bayraklı `IInteractable`lara uygulanır, yalnız anı-tetikleyicilere değil — kamuflaj amaçlı (GDD Core Rules). Ama gerçek içerik dağılımında (Servis Koridoru/Balo Salonu'nda anı-tetikleyici dışında HİÇ `IInteractable` yok; Depo'da taşıma eşyaları alınır alınmaz registry'den çıkar) kamuflaj **%100 kesinlikle** çöker — yavaşlama tespit edilebilir bir "metal dedektörü" olur. Düzeltme: her üç MVP alanına (Depo, Servis Koridoru, Balo Salonu) en az bir sahte/dekor `IInteractable` içerik gereksinimi.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: SceneScan fazı — `BuildValidationRegistry`'nin paylaşılan `IPreprocessBuildWithReport` utility'sine kayıt. Fixture'lar runtime-created; on-disk test sahnesi/asset'i YASAK (isik-volume Story 006 emsali).

**Control Manifest Rules (bu katman)**:
- Required: check `IBuildCheck` implementasyonu + `BuildValidationRegistry` satırı; pointed hata mesajları (offending sahne adlı)
- Forbidden: dördüncü/beşinci bağımsız `IPreprocessBuildWithReport` (bu proje TEK utility kullanır — ADR-0007/0012/0013/0014/0015/isik-Story006 zaten paylaşıyor)
- Guardrail: —

## Acceptance Criteria

- [ ] **`DecoyInteractable : MonoBehaviour, IInteractable`** — bu story'nin TANIMLADIĞI, minimal, mekanik etkisiz bir bileşen (`Instant` tip, yapılandırılabilir/boş prompt, `OnInteract()` hiçbir şey yapmaz — GDD'nin "kapı kolu, ışık anahtarı, termostat, temizlik arabası freni" örnekleriyle tutarlı). **Neden gerekli**: `IInteractable` arayüzü yapısal olarak gerçek/decoy ayrımını sızdırmaz (kamuflaj gereksinimi) ve `MemoryTriggerObject`/`CarryItemPickup` (gerçek içerik tipleri) henüz yazılmamış epic'lere ait — bu yüzden decoy tespiti "gerçek tipleri hariç tut" yöntemiyle DEĞİL, `GetComponent<DecoyInteractable>()` ile POZİTİF işaretleme yöntemiyle yapılır
- [ ] Yeni bir `IBuildCheck` (SceneScan fazı), `BuildValidationRegistry.Checks`'e kayıtlı: taranan her MVP alanı sahnesinde (Depo, Servis Koridoru, Balo Salonu) en az bir `DecoyInteractable` bulunur (AC17)
- [ ] Eksikse `BuildFailedException`, hangi sahnenin eksik olduğu mesajda
- [ ] Mevcutsa sessiz (check geçer)
- [ ] Test harness'ta throws/doesn't-throw çiftleri — sahte sahne içeriği runtime-created (isik-volume Story 006 `FakeWalker` emsali; gerçek MVP level sahneleri henüz Build Settings'te YOK — bu içerik yazımı Presentation/Feature aşamasına ait)

## Implementation Notes

- isik-volume Story 006'nın `IsikVolumeBuildChecks.cs`/`FakeWalker` desenini takip et — check `BuildValidationRegistry`'ye eklenir, ikinci bağımsız `IPreprocessBuildWithReport` YASAK.
- `BuildValidationRegistry.cs`'deki `TODO(epic:birinci-sahis-kontrolcu): TR-fpc-016 decoy check'i` satırı bu story'de gerçek kayda dönüşür.
- **İleri bayrak** (registry'nin TODO listesi zaten bunu bekliyor): `ani-tetikleyici-etkilesim`/`gorev-tasima-dongusu` epic'leri kendi tiplerini (`MemoryTriggerObject`/`CarryItemPickup`) yazınca, bu check'in "gerçek içerik hariç" davranışı ayrıca gerekiyorsa (şu an gerekmiyor — pozitif `DecoyInteractable` işaretlemesi zaten yeterli, gerçek tiplerin decoy olarak yanlış sayılması riski yok) genişletilebilir; isik-volume Story 006'nın AC22 devri ile aynı desen.
- Sahne isimleri (Depo/Servis Koridoru/Balo Salonu) art-bible/GDD'den — sahneler henüz Build Settings'te yoksa check sessiz kalır (hiçbir sahne taranmaz), bu YAPISAL OLARAK doğru (henüz içerik yok, henüz ihlal de yok) — test bunu fake walker ile simüle eder, gerçek sahnelerin varlığına bağlı değildir.

## Out of Scope

- Gerçek MVP level sahnelerinin (Depo/Servis Koridoru/Balo Salonu) içeriğinin yazılması (Presentation/level-design aşaması)
- `MemoryTriggerObject`/`CarryItemPickup` tiplerinin tanımlanması (ilgili Feature epic'leri)

## QA Test Cases

*(QL-STORY-READY full modda koştu — AC17'nin literal ifadesi implemente edilemez bulundu, gate'in önerdiği pozitif-işaretleme çözümü [DecoyInteractable] AC'lere işlendi; bu epic'in en ciddi bulgusuydu.)*

- **AC-1 (otomatik)**: DecoyInteractable minimal implementasyon
  - Given: `DecoyInteractable` bileşeni bir GameObject'e eklenir
  - When: `IInteractable` yüzeyi çağrılır
  - Then: `Type`=Instant, `OnInteract()` hiçbir mekanik etki üretmez, gerçek/decoy ayrımı sızdıran hiçbir ek üye yok (yalnız `GetComponent`la tespit edilebilir olması, `IInteractable` arayüzünün kendisine hiçbir şey eklemez)

- **AC-2 (otomatik)**: Check kaydı
  - Given: `BuildValidationRegistry.Checks`
  - Then: yeni decoy check'i içerir, `SceneScan` fazında

- **AC-3 (otomatik)**: Eksik decoy → hata
  - Given: sahte bir "MVP alanı" sahnesi, hiç `DecoyInteractable` yok (anı-tetikleyici/taşıma-eşyası benzeri başka içerik olsa bile)
  - When: check koşar
  - Then: `BuildFailedException`, sahne adı mesajda

- **AC-4 (otomatik)**: Mevcut decoy → sessiz
  - Given: aynı sahte sahne, en az bir `DecoyInteractable` eklenmiş
  - When: check koşar
  - Then: exception yok

- **AC-5 (otomatik)**: throws/doesn't-throw çiftleri, runtime-created fixture'lar
  - Given: FakeWalker + runtime GameObject'ler (on-disk sahne/asset YOK)
  - Then: her senaryo çifti (ihlalli/temiz) doğru davranır

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/fpc_decoy_build_check_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: proje-kurulumu Story 006 (Complete — çatı), interactable-registry Story 001 (Complete — `IInteractable`)
- Unlocks: level-design içerik yazımı güvenli hale gelir (kamuflaj yapısal olarak garanti altında)
