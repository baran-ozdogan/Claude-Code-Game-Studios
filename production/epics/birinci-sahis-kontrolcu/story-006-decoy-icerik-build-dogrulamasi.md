# Story 006: Decoy içerik build-time doğrulaması

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

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

- [x] **`DecoyInteractable : MonoBehaviour, IInteractable`** — bu story'nin TANIMLADIĞI, minimal, mekanik etkisiz bir bileşen (`Instant` tip, yapılandırılabilir/boş prompt, `OnInteract()` hiçbir şey yapmaz — GDD'nin "kapı kolu, ışık anahtarı, termostat, temizlik arabası freni" örnekleriyle tutarlı). **Neden gerekli**: `IInteractable` arayüzü yapısal olarak gerçek/decoy ayrımını sızdırmaz (kamuflaj gereksinimi) ve `MemoryTriggerObject`/`CarryItemPickup` (gerçek içerik tipleri) henüz yazılmamış epic'lere ait — bu yüzden decoy tespiti "gerçek tipleri hariç tut" yöntemiyle DEĞİL, `GetComponent<DecoyInteractable>()` ile POZİTİF işaretleme yöntemiyle yapılır
- [x] Yeni bir `IBuildCheck` (SceneScan fazı), `BuildValidationRegistry.Checks`'e kayıtlı: taranan her MVP alanı sahnesinde (Depo, Servis Koridoru, Balo Salonu) en az bir `DecoyInteractable` bulunur (AC17)
- [x] Eksikse `BuildFailedException`, hangi sahnenin eksik olduğu mesajda
- [x] Mevcutsa sessiz (check geçer)
- [x] Test harness'ta throws/doesn't-throw çiftleri — sahte sahne içeriği runtime-created (isik-volume Story 006 `FakeWalker` emsali; gerçek MVP level sahneleri henüz Build Settings'te YOK — bu içerik yazımı Presentation/Feature aşamasına ait)

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

**Story Type**: Logic → `game/Assets/Tests/EditMode/fpc_decoy_build_check_test.cs` (18 test) + `fpc_decoy_scene_drift_test.cs` (tripwire) + `game/Assets/Tests/PlayMode/fpc_decoy_registry_test.cs` (runtime kayıt)
**Status**: [x] Created — EditMode 140/140, PlayMode 78/78

## Dependencies

- Depends on: proje-kurulumu Story 006 (Complete — çatı), interactable-registry Story 001 (Complete — `IInteractable`)
- Unlocks: level-design içerik yazımı güvenli hale gelir (kamuflaj yapısal olarak garanti altında)

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 5/5 AC, EditMode 140/140, PlayMode 78/78. **Bu story ile birinci-sahis-kontrolcu epic'i 6/6 TAMAM.**

**Dosyalar**: `Foundation/DecoyInteractable.cs` (YENİ), `Editor/BuildValidation/FpcDecoyPresenceCheck.cs` (YENİ), `BuildValidationRegistry.cs` (+kayıt, TODO→tamamlandı), `BuildValidation/README.md` (+kayıtlı check satırı), `Tests/EditMode/fpc_decoy_build_check_test.cs` (YENİ, 18 test), `Tests/EditMode/fpc_decoy_scene_drift_test.cs` (YENİ, tripwire), `Tests/PlayMode/fpc_decoy_registry_test.cs` (YENİ).

**BLOCKING GATE BULGUSU — sahne adları YANLIŞTI, check hiçbir şeyle eşleşmiyordu.** İlk implementasyon `{"Depo","ServisKoridoru","BaloSalonu"}` (Türkçe) kullanıyordu; mimarinin KİLİTLİ adları İngilizce: `control-manifest.md` Scenes/Prefabs satırı örnekleri `Depot`/`Ballroom`, ADR-0015 `_initialLevelSceneName = "Depot"`, ADR-0011 kat sahnesi adını `gameObject.scene.name`'den türetiyor, ve mevcut `build_validation_harness_test.cs` zaten `Assets/Scenes/Depot.unity` kullanıyor. Gerçek sahneler geldiğinde check hiçbirini tanımaz, sonsuza dek sessizce geçer ve story'nin "Unlocks: level-design içerik yazımı güvenli hale gelir" iddiası YANLIŞ olurdu. Ayrıca **Servis Koridoru bir SAHNE DEĞİL** — ADR-0011 MVP'yi tam olarak iki kat olarak tanımlıyor, koridor bir kat sahnesinin içindeki alan; per-scene granülerlik onun kendi gereksinimini ifade edemez (bilinçli sınır olarak belgelendi, sessiz boşluk değil).

**Fail-open'a karşı TRIPWIRE (her iki gate bağımsız olarak istedi)**: `fpc_decoy_scene_drift_test.cs` — Build Settings'e giren her sahne ya kalıcı-sahne muafiyet listesinde ya da check'in tanıdığı bir MVP alanı olmalı; tanınmayan bir sahne eklendiği an kırmızıya döner. "Sıfır sahne sessizliği" (yapısal olarak doğru) ile "başka adla gelen sahne sessizliği" (fail-open) arasındaki farkı kapatır. Ad listesi ayrıca literal olarak pinlendi (eski test listeyi kendine karşı geziyordu — totolojikti, bu sapmayı yakalayamazdı).

**AC-1'in BİLİNÇLİ daraltılması (gate bulgusu, gerekçeli)**: AC-1 "yapılandırılabilir/boş prompt" diyordu; check artık BOŞ `PromptText`i de reddediyor. Gerekçe: Etkileşim crosshair'i `PromptText` çizer — gerçek nesneler "Al"/"Çek" gösterirken promptsuz bir decoy hiçbir şey göstermez ve oyuncu "yavaşlama + yazı yok = decoy" kuralını öğrenir; kamuflaj OYUNCUNUN GÖRDÜĞÜ katmanda sızar ve metal-dedektörü istismarı geri açılır. GDD decoy'lardan "minimal/**tatsız** bir tepki" istiyor — tepkisiz değil.

**Diğer düzeltmeler**: sahne adı eşleşmesi büyük/küçük harf duyarsız (Windows'ta `depot.unity` sessizce atlanırdı — fail-open); dosya adı sınıf adına eşitlendi (`FpcDecoyPresenceCheck.cs`); README güncellendi; reflection kamuflaj taraması `Static`'i ve İMZA'yı da kapsıyor (bir `public static bool IsDecoy` ya da `OnInteract(GameObject)` aşırı yüklemesi eski taramadan kaçardı); sahne-başına atıf testi eklendi (`FakeWalker`'a `onOpen` kancası — üretimde bunu `OpenSceneMode.Single` yapar); gerçek `BuildValidationRegistry.Checks` dizisi uçtan uca koşuluyor; **PlayMode'da `DecoyInteractable`'ın registry kayıt/çıkış döngüsü** (story'nin TÜM runtime değeri buydu ve hiç test edilmemişti — kaydolmayan bir decoy taper'a katkı vermez, kamuflaj build yeşilken çöker).

**İleri bayrak**: `[RequireComponent(typeof(Collider))]` decoy'lara eklenebilir (manifest ADR-0010: interactable + collider aynı GameObject'te) — bu story'de eklenmedi, level-design içerik kuralı olarak duruyor.
