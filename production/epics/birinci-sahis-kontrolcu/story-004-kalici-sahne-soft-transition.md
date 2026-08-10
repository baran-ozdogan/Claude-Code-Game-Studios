# Story 004: Persistent Player sahnesi + SOFT transition repozisyonu

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/birinci-sahis-kontrolcu.md` (Player Fantasy — "Bedenin Sürekliliği"; Edge Cases — Asansör/platform-delta)
**Requirement**: `TR-fpc-010`
*(Requirement metni `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0003 (primary)
**ADR Decision Summary**: Player GameObject'i kalıcı "Player" sahnesinde yaşar — proje-kurulumu Story 004'te ADR-0002/0008 ile aynı mekanizmayla boş kök olarak zaten yüklü (`PersistentSceneBootLoader`, UI→Player→Foundation sıralı-awaited). SOFT transition, oyuncunun Transform'unu hedef sahnenin `SoftTransitionAnchor`'ına repozisyone eder (yalnız translation/rotation — GameObject asla yok edilip yeniden yaratılmaz).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Reload-Scene-off iki-oturum deseni ADR-0001'in kendi emsaliyle aynı (`gece_oturum_two_session_test.cs`/`boot_persistent_scenes_test.cs`'in izlediği idiyom — `ResetOnLoad`/sahne unload-reload simülasyonu, gerçek Editor ayarı değiştirilmez).

**Control Manifest Rules (bu katman)**:
- Required: kalıcı-sahne singleton deseni — `Awake()`-set static `Instance` + duplicate guard (koşulsuz `Debug.LogError`+`Destroy`); iki-oturum Editor testleri her statik facade/kalıcı-sahne singleton'ı için ZORUNLU
- Forbidden: `DontDestroyOnLoad` (kalıcı-sahne deseni bu projenin TEK cevabı)
- Guardrail: —

## Acceptance Criteria

- [x] `Player.unity` (proje-kurulumu Story 004'ün boş kökü) artık Story 003'ün prefab'ının bir instance'ını içerir: `CharacterController`+`Camera`+`FirstPersonController`+`PlayerStateProvider`; sahne yüklenince `PlayerStateProvider.Current` non-null, o instance'a işaret eder
- [x] **Duplicate-guard'ın sahne-yükleme seviyesinde ampirik kanıtı** (Story 001'in çıplak-GameObject testinden AYRI — ADR-0003'ün gerçek Risk senaryosunu, "Player sahnesinin yanlışlıkla iki kez instantiate edilmesi"ni, sınar): Player sahnesi zaten yüklüyken (`Current` set) gerçek Player GameObject'i ikinci kez instantiate edilirse, ikincisi guard'a göre kendini yok eder, `Current` değişmez
- [x] **Sanctioned sarmalanmış repozisyon API'si**: `FirstPersonController.RepositionTo(...)` — Story 003'ün "ham CharacterController dışa açılmaz" kısıtının TEK istisnası. **İKİ aşırı yükleme** (gate bulgusu, aşağıda gerekçeli): tek-anchor MUTLAK form (ADR-0015 boot spawn, `InitialSpawnAnchor`) + iki-anchor GÖRELİ form (SOFT transition'ın kabin-yerel sürekliliği). Bu epic entegrasyonu yapmaz, yalnız primitifi kilitler
- [x] `RepositionTo` çağrısı sonrası: GameObject identity ve `PlayerStateProvider.Current` DEĞİŞMEZ; `Velocity`/`IsGrounded`/kamera pitch ETKİLENMEZ (yalnız translation/rotation — örtük bir reset YOK, ADR-0003 Context'in "görünür pop/süreksizlik olmaz, Bedenin Sürekliliği" kısıtı)
- [x] Reload-Scene-off iki-oturum testi: Player sahnesi her simüle oturumda TAZE yüklenir, stale kök objesi kalmaz, `Current` hiçbir zaman bayat bir instance'a işaret etmez (ADR-0001'in emsaliyle aynı desen)

## Implementation Notes

- Story 003'ün ürettiği prefab'ı `Player.unity`'ye yerleştir — kontrolcüyü burada YENİDEN İNŞA ETME.
- `RepositionTo(Transform anchor)` imzası kilitlenir; `seviye-sahne-gecisi` epic'i ileride bunu çağıracak, kendi doğrudan `transform.position=` yazımını icat ETMEYECEK (ADR-0002'nin "3 ADR aynı soruyu ayrı ayrı icat etmesin" endişesiyle aynı disiplin).
- İki-oturum simülasyonu `boot_persistent_scenes_test.cs`'in kurduğu `PersistentSceneBootLoader.EnsurePersistentScenesLoaded()`/unload-reload kalıbını yeniden kullanır.

## Out of Scope

- Gerçek `SceneTransitionManager`/`seviye-sahne-gecisi` entegrasyonu (o epic henüz yok — bu story yalnız `RepositionTo` primitive'ini sağlar)
- Elevator'ün gerçek MoveOnly kilit talebi (Asansör/Kat-Erişim epic'i)

## QA Test Cases

*(QL-STORY-READY full modda koştu — SOFT-transition AC'sine somut metot imzası + Velocity/IsGrounded/pitch koruma assert'i gate bulgusuyla eklendi.)*

- **AC-1 (otomatik)**: Gerçek Player GameObject'i Player.unity'de
  - Given: kalıcı Player sahnesi yüklenir
  - When: sahne içeriği incelenir
  - Then: bir aktif GameObject, dört bileşenin tümüyle; `Current` non-null, o objeye işaret ediyor

- **AC-2 (otomatik)**: Sahne-yükleme seviyesinde duplicate-guard
  - Given: Player sahnesi yüklü, `Current` set
  - When: gerçek Player GameObject'i ikinci kez instantiate edilir
  - Then: ikincisi kendini yok eder, `Current` değişmez

- **AC-3 (otomatik)**: Sanctioned RepositionTo API'si
  - Given: elle yerleştirilmiş bir `SoftTransitionAnchor`
  - When: `RepositionTo(anchor)` çağrılır
  - Then: pozisyon/rotasyon anchor'la tam eşleşir, GameObject identity ve `Current` değişmez
  - Edge cases: `Velocity`/`IsGrounded`/kamera pitch çağrı öncesi değerleriyle AYNI kalır (yalnız translation/rotation, örtük reset yok)

- **AC-4 (otomatik)**: Reload-Scene-off iki-oturum tazeliği
  - Given: Reload Scene devre dışı, ilk simüle oturum boot edildi
  - When: ikinci simüle oturum başlar
  - Then: Player sahnesi taze yüklenir, stale kök objesi yok, `Current` hiçbir zaman bayat değil

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/fpc_persistent_scene_test.cs` (11 test) + `game/Assets/Tests/EditMode/fpc_player_scene_asset_test.cs` (AC-1'in asset-seviyesi yarısı)
**Status**: [x] Created — EditMode 120/120, PlayMode 61/61

## Dependencies

- Depends on: Story 003 (Player prefab'ı)
- Unlocks: `seviye-sahne-gecisi` epic'inin gerçek SOFT-transition entegrasyonu (RepositionTo'yu tüketecek)

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 5/5 AC, EditMode 120/120, PlayMode 61/61.

**Dosyalar**: `FirstPersonController.cs` (+`RepositionTo` iki aşırı yükleme + ortak `ApplyPose` + iki `internal` test kancası), `Editor/Story004PlayerSceneSetup.cs` (YENİ, tek seferlik) → `Assets/Scenes/Player.unity` artık prefab instance'ı taşıyor, `Tests/PlayMode/fpc_persistent_scene_test.cs` (YENİ, 11 test), `Tests/EditMode/fpc_player_scene_asset_test.cs` (YENİ).

**EN ÖNEMLİ GATE BULGUSU — kilitlenen API imzası YANLIŞTI.** Story yalnız `RepositionTo(Transform anchor)` (mutlak snap) öngörüyordu. LP-CODE-REVIEW bunun SOFT transition sözleşmesini İFADE EDEMEDİĞİNİ gösterdi ve üç kaynakla doğrulandı: (1) `seviye-sahne-gecisi.md` koordinat-çerçevesi kuralı — "kopyalanan, dünya-uzayı pozisyonu değil, **kabin-yerel pozisyon/rotasyondur**"; (2) ADR-0008'in `CopySoftTransitionAnchorTransform(fromScene, toScene)` imzası İKİ sahne alır; (3) ADR-0015 birebir: *"SOFT's anchor-copy semantics are relative continuity FROM a source scene."* Asansör yolculuğunda kilit `MoveOnly`'dir (Look serbest), yani oyuncunun kabin içindeki konumu/bakışı keyfîdir — mutlak snap onu siler ve GDD'nin açıkça yasakladığı "pozisyon sıçraması/dönüş atlaması"nı üretir. **Çözüm**: iki aşırı yükleme. Tek-anchor form terk edilmedi — ADR-0015'in boot spawn'ı (`InitialSpawnAnchor`, kaynak sahne YOK) tam olarak onu ister ve ADR-0015 orada ham `SetPositionAndRotation` yazımı taslaklıyor; artık bu API onu soğuruyor. **Story bu düzeltmeyle kapandı: yanlış primitifi kilitlemek, bu story'nin var oluş amacının tam tersi olurdu.**

**Diğer gate düzeltmeleri**: gövde rotasyonu artık YAW'a düzleştiriliyor (LP+QL) — gövde yapısal olarak yalnız yaw döner (pitch kamerada), eğik bir anchor `Space.Self` yaw'ı yüzünden KALICI ve kurtarılamaz gövde eğimi + bozuk hareket yönü üretirdi; guard kodda (projenin `IsikVolumeFormulas` "guard'lar tasarımcı disiplinine bırakılmaz" emsali). `Story004PlayerSceneSetup` idempotency kontrolü yıkımdan ÖNCEye alındı (interaktif Editor'da sessiz sahne mutasyonu riski). **Yeni testler**: `_lastMoveDirection` dönüşü (silinse tüm testler yeşil kalıyordu), ışınlanma-sonrası `Velocity` sıçraması yok + yeniden yere oturma, kilit altında repozisyon (kilit temizlenmiyor), eğik-anchor düzleştirme, göreli aşırı yüklemenin kabin-yerel poz koruması, ve **Story 003'ün paylaşılan-InputActionAsset bug'ının regresyon testi** (`InputActive` kancası — `.claude/rules/test-standards.md` "her bug fix'in regresyon testi olmalı"; bu testin var olabileceği tek yer AC-2'nin tam-donanımlı kopyasıydı).

**Test altyapısı bulgusu (kalıcı)**: `PrefabUtility.IsPartOfPrefabInstance` PlayMode'da yüklenmiş sahnede FALSE döner — prefab bağlantısı yalnız Editor metadata'sıdır. "Sahnedeki oyuncu prefab instance'ı mı" iddiası bu yüzden EditMode'a (`fpc_player_scene_asset_test.cs`, sahne asset'ini additive açar) taşındı.

**İleri bayraklar (advisory)**: `RepositionTo` sonrası ilk karede `isGrounded` bir kare `false` okur (controller disable/enable PhysX durumunu bırakır) — yayınlanan `IsGrounded` etkilenmez (Move'dan SONRA yazılır) ama anchor'lar zemin seviyesinde authored edilmeli. `seviye-sahne-gecisi` epic'ine devir notu: Unity `OnTriggerEnter`'ı `CharacterController.Move()` İÇİNDEN senkron dağıtır — trigger-tetiklemeli bir SOFT geçiş `RepositionTo`'yu Move'un ortasında çağırabilir; bugünkü kod güvenli (pozisyon Move içinde, ışınlanmadan sonra örnekleniyor) ve bu davranış artık testle kilitli.
