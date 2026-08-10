# Story 004: Persistent Player sahnesi + SOFT transition repozisyonu

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

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

- [ ] `Player.unity` (proje-kurulumu Story 004'ün boş kökü) artık Story 003'ün prefab'ının bir instance'ını içerir: `CharacterController`+`Camera`+`FirstPersonController`+`PlayerStateProvider`; sahne yüklenince `PlayerStateProvider.Current` non-null, o instance'a işaret eder
- [ ] **Duplicate-guard'ın sahne-yükleme seviyesinde ampirik kanıtı** (Story 001'in çıplak-GameObject testinden AYRI — ADR-0003'ün gerçek Risk senaryosunu, "Player sahnesinin yanlışlıkla iki kez instantiate edilmesi"ni, sınar): Player sahnesi zaten yüklüyken (`Current` set) gerçek Player GameObject'i ikinci kez instantiate edilirse, ikincisi guard'a göre kendini yok eder, `Current` değişmez
- [ ] **Sanctioned sarmalanmış repozisyon API'si**: `FirstPersonController.RepositionTo(Transform anchor)` — Story 003'ün "ham CharacterController dışa açılmaz" kısıtının TEK istisnası, translation+rotation'ı hedef `anchor`'dan kopyalar; SOFT transition'ın (gelecekteki `seviye-sahne-gecisi` epic'i) çağıracağı TEK sanctioned yol budur — bu epic o entegrasyonu yapmaz, yalnız primitive'i kilitler
- [ ] `RepositionTo` çağrısı sonrası: GameObject identity ve `PlayerStateProvider.Current` DEĞİŞMEZ; `Velocity`/`IsGrounded`/kamera pitch ETKİLENMEZ (yalnız translation/rotation — örtük bir reset YOK, ADR-0003 Context'in "görünür pop/süreksizlik olmaz, Bedenin Sürekliliği" kısıtı)
- [ ] Reload-Scene-off iki-oturum testi: Player sahnesi her simüle oturumda TAZE yüklenir, stale kök objesi kalmaz, `Current` hiçbir zaman bayat bir instance'a işaret etmez (ADR-0001'in emsaliyle aynı desen)

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

**Story Type**: Integration → `game/Assets/Tests/PlayMode/fpc_persistent_scene_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 003 (Player prefab'ı)
- Unlocks: `seviye-sahne-gecisi` epic'inin gerçek SOFT-transition entegrasyonu (RepositionTo'yu tüketecek)
