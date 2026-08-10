# Story 003: FirstPersonController sürücüsü — CharacterController + kamera + Input System

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/birinci-sahis-kontrolcu.md` (Core Rules — hareket/bakış/çarpışma; Interactions with Other Systems; Edge Cases — CharacterController/OnControllerColliderHit)
**Requirement**: `TR-fpc-011`, `TR-fpc-012`, `TR-fpc-015` (+ Story 001/002'nin wiring'i)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0003 (primary)
**ADR Decision Summary**: `FirstPersonController`, `PlayerStateProvider`'dan AYRI bir `MonoBehaviour` — kendi `Awake()`'inde `GetComponent<PlayerStateProvider>()` ile aynı GameObject'teki instance'a bağlanır (Unity'nin `GetComponent` garantisi, `Awake()` sırasına bağımlı değil — ADR-0003'ün "confirmed non-issue" notu). Her karede `EffectiveScope()`'u okuyup `Move`/`Look`'u ona göre uygular; `Velocity`/`IsGrounded`/`IsCarrying` alanlarını `PlayerStateProvider`'a `internal set` ile yazar.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `CharacterController` kinematik — `Physics.defaultSolverIterations` bu bileşeni HİÇ etkilemez (ADR-0003'ün düzelttiği faktüel hata — solver-iteration değişikliği bu ADR'a alakasız). Yeni Input System (Input Actions asset + üretilmiş C# sınıfı), legacy `Input.*` YASAK.

**Control Manifest Rules (bu katman)**:
- Required: Pure C# state machine + thin MonoBehaviour driver split (Story 002'nin matematiği burada TÜKETİLİR, yeniden yazılmaz); `Dereference X.Instance kullanım noktasında canlı`
- Forbidden: `Input.*` legacy sınıfı (`GetKey`/`GetAxis`/`mousePosition`) — yeni Input System kullanılır
- Guardrail: `OnControllerColliderHit`, açık bir ilgi maskesinde olmayan katmanlara karşı erken çıkar (statik geometri spam'i önlenir)

## Acceptance Criteria

- [x] `CharacterController` tabanlı kinematik hareket, Story 002'nin ivmelenme formülüyle sürülür — kararlı-durum hızı yüksüz 1.6 m/s'ye (±0.02), yüklü 1.35 m/s'ye (±0.02) yakınsar (AC1/2)
- [x] Koşu yok — Input Actions asset'i taranınca hiçbir bağlama yürüme hızının üzerine çıkarmaz (sprint eylemi yok) (AC3)
- [x] Kamera pitch tam olarak -80°/+80°'de kelepçelenir, daha fazla dönüş olmaz; yaw sınırsız devam eder (AC14)
- [x] `PlayerStateProvider.EffectiveScope()` her karede okunur: `MoveOnly` → Move donuk, Look serbest; `Full` → ikisi de donuk
- [x] Hareket kilidi `v(t)>0` iken tetiklenirse: `v_target=0` olur ama Formül 1 çalışmaya devam eder — `v(t)` ışınlanmadan normal `T_ramp` süresinde sıfıra söner; `IsCarrying` ve taşıma görselleri Locked boyunca DOKUNULMADAN kalır (AC13)
- [x] Tek "Gameplay" action map — `Move` (Vector2), `Look` (Vector2), `Interact` (Button); legacy `Input.*` hiçbir yerde kullanılmaz (TR-fpc-015)
- [x] `CharacterController` step offset ~2cm ALTINDA, skin width ≈ kapsül yarıçapının %10'u (GDD'nin kilitli varsayılan değerleri, Tuning Knob olarak işaretli)
- [x] `OnControllerColliderHit`, açık bir ilgi maskesinde (hareketli platformlar, dinamik tehlikeler) olmayan katmanlara karşı erken çıkar — varsayılan statik-geometri katmanına karşı hiçbir mantık çalıştırılmaz (GDD Edge Case, performans guardrail'i)
- [x] **Sarmalanmış `Move()` yüzeyi**: oyuncunun pozisyonunu dışarıdan etkilemek isteyen HERHANGİ bir sistem yalnız `FirstPersonController` üzerindeki sarmalanmış bir metodu kullanabilir — ham `CharacterController` hiçbir public erişimciyle dışa açılmaz (GDD: "diğer sistemler pozisyonu etkilemek isterse sarmalanmış bir Move() çağrısı kullanır")

## Implementation Notes

- **Teslim edilen artifact**: bu story, Player GameObject'inin TAMAMLANMIŞ, yeniden kullanılabilir bir prefab'ını (`CharacterController`+`Camera`+`FirstPersonController`+`PlayerStateProvider`) üretir — Story 004 bu prefab'ın bir instance'ını `Player.unity`'ye yerleştirip yalnız kalıcı-sahne/SOFT-transition garantilerini üzerine test eder. Kontrolcü İKİ KEZ İNŞA EDİLMEZ.
- **İleri bayrak (Story 001'den devraldı)**: `FirstPersonController`/`PlayerStateProvider`, UI-sahne durumunu ASLA `Awake()`/`OnEnable()` içinde okumamalı (ADR-0003'ün UI/Player boot-sırası gizli riski — şu an hiçbir tüketici buna ihtiyaç duymuyor, ama gelecekte bir tüketici eklenirse bu kısıt bozulmamalı).
- `Story 002`'nin saf metotlarını doğrudan çağır — matematiği burada yeniden türetme.
- Sarmalanmış `Move()` yüzeyinin tam imzası Story 004'te kilitlenir (SOFT-transition repozisyon çağrısıyla aynı API ailesi) — bu story yalnız "ham CharacterController dışa açılmaz" yapısal kısıtını sağlar.

## Out of Scope

- Player sahnesinin kalıcı-sahne yerleşimi + duplicate-guard'ın sahne-yükleme seviyesinde ampirik kanıtı + SOFT transition (Story 004)
- Taper'ın `d`'sinin `InteractableRegistry`'den gerçek okunması + `SetCarrying` yüzeyi + faz akümülatörünün gerçek wiring'i (Story 005)
- Elevator'ün `MoveOnly` kilit talebi (Asansör/Kat-Erişim epic'i — bu story yalnız kilit TÜKETİCİ tarafını sağlar)

## QA Test Cases

*(QL-STORY-READY full modda koştu; OnControllerColliderHit + sarmalanmış-Move AC'leri gate bulgusuyla eklendi.)*

- **AC-1/2 (otomatik)**: Kararlı-durum hızları
  - Given: yüksüz oyuncu, yerde, boşta
  - When: maksimum girdiyle ileri basılı tutulur
  - Then: hız 1.6 m/s'ye (±0.02) yakınsar
  - Given: `IsCarrying`=true
  - When: aynı girdi
  - Then: 1.35 m/s'ye (±0.02) yakınsar

- **AC-3 (otomatik)**: Koşu yok
  - Given: Input Actions asset'i ve tüm bağlamalar
  - When: incelenir
  - Then: yürüme değerinin üzerine çıkaran hiçbir bağlama yok

- **AC-4 (otomatik)**: MoveOnly Look'u serbest bırakır
  - Given: `EffectiveScope()`=MoveOnly
  - When: Move+Look girdisi uygulanır
  - Then: Move sıfır yer değiştirme üretir, Look normal döner

- **AC-5 (otomatik)**: Full ikisini de dondurur
  - Given: `EffectiveScope()`=Full
  - When: Move+Look uygulanır
  - Then: hiçbiri değişmez

- **AC-6 (otomatik)**: Kilit-ortası yavaşlama, ışınlanmaz
  - Given: v>0
  - When: dış sistem kilit ister
  - Then: v normal T_ramp eğrisinde söner, `IsCarrying`/görseller Locked boyunca değişmez

- **AC-7 (otomatik)**: Pitch kelepçesi, yaw sınırsız
  - Given: sürekli look girdisi limit ötesine
  - When: uygulanır
  - Then: pitch tam -80°/+80°'de kelepçelenir, yaw hiç kelepçelenmez

- **AC-8 (otomatik)**: Step offset/skin width kilitli varsayılanlar
  - Given: CharacterController konfigürasyonu
  - When: incelenir
  - Then: stepOffset≈2cm altı, skinWidth≈yarıçapın %10'u

- **AC-9 (otomatik)**: OnControllerColliderHit katman guard'ı
  - Given: statik-geometri katmanında simüle bir çarpışma
  - Then: handler erken çıkar, hiçbir mantık çalışmaz
  - Given: açık ilgi-maskesi katmanında bir çarpışma
  - Then: mantık devam eder
  - Edge cases: tekrarlanan per-frame statik-geometri hit'leri sıfır allocation/mantık üretir

- **AC-10 (otomatik)**: Sarmalanmış Move()-yalnız yüzey
  - Given: oyuncu pozisyonunu etkilemek isteyen dış bir sistem
  - When: public API taranır
  - Then: yalnız `FirstPersonController` üzerindeki sarmalanmış metot mevcut — ham `CharacterController` hiçbir public erişimciyle açığa çıkmaz

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/fpc_controller_driver_test.cs`
**Status**: [x] Created — PlayMode 15/15 (süit geneli 50/50)

## Dependencies

- Depends on: Story 001 (`PlayerStateProvider`), Story 002 (hareket matematiği)
- Unlocks: Story 004 (kalıcı sahne yerleşimi), Story 005 (taper/carry/faz wiring'i)

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 9/9 AC, EditMode 119/119, PlayMode 50/50.

**Dosyalar**: `Foundation/FirstPersonController.cs` (YENİ), `Assets/Input/Gameplay.inputactions` (Move/Look/Interact bağlamaları dolduruldu — önceden boş asset'ti), `Editor/Story003PlayerPrefabSetup.cs` (YENİ, tek seferlik) → `Assets/Prefabs/Player.prefab` (YENİ — CharacterController + Eye/Camera + PlayerStateProvider + FirstPersonController; Story 004 bunun bir instance'ını yerleştirecek, kontrolcü İKİ KEZ İNŞA EDİLMEZ), `Foundation.asmdef` (+Unity.InputSystem), `Tests/PlayMode/fpc_controller_driver_test.cs` (YENİ, 15 test).

**Gate'ler (full mod) — İKİSİ DE aksiyonluydu, hepsi kapanış öncesi giderildi.**

**LP-CODE-REVIEW CONCERNS → giderildi (3 GERÇEK ÜRETİM BUG'I)**:
1. **Gamepad bakışı bozuktu**: mouse `delta` (kare-başı delta) ile gamepad çubuğu (ORAN, [-1,1]) aynı ölçekle çarpılıyordu → gamepad'de ~7°/s ve kare-hızına bağımlı. Cihaz ayrımı `Tick()`'e taşındı (`activeControl.device is Gamepad` → derece/saniye × deltaTime); `TickWithInput` saf delta sözleşmesiyle kaldı.
2. **Paylaşılan `InputActionAsset` yıkıcı yan etki üretiyordu**: duplicate-guard'ın yok ettiği ikinci oyuncunun `OnDisable`'ı, HAYATTA KALAN oyuncunun girdisini de kapatıyordu (ADR-0003'ün kendi kurtarma yolunu bozan sessiz tam-girdi kaybı). Fix: `Awake`'te asset'in per-instance kopyası (`Instantiate`). Ayrıca map/action null-guard'ları + açık `Debug.LogError` eklendi (önceden stringly-typed arama sessizce NRE'ye gidiyordu).
3. **`IPlayerState.Velocity` yanıltıcıydı**: yere-yapıştırma sabiti (-2 m/s) yayınlanan değere sızdığı için DURAN oyuncu `|Velocity|=2` okuyordu ve duvara dayalı oyuncu 1.6 m/s yalanını söylüyordu. Artık GERÇEKLEŞEN yer değiştirme yayınlanıyor (`(pozisyon farkı)/deltaTime`). **`CharacterController.velocity` bilinçli KULLANILMADI** — onu Unity kendi kare delta'sıyla hesaplıyor, sürücünün `deltaTime` parametresiyle değil; dışarıdan sürülen tick'te tutarsız/şişkin değerler verdi (ampirik).
+ Prefab'a `Player` ve `MainCamera` tag'leri eklendi (control manifest Core kuralı, ADR-0011); step offset 0.02 → **0.018** (GDD "en küçük eşiğin ALTINDA" diyor, eşit değil).

**QL-TEST-COVERAGE GAPS → giderildi**: AC-7'nin yaw testi totolojikti (`!=0`, ±80 kelepçesi de geçerdi) → biriken dönüş >720° assert'i; **+80 kelepçesi hiç test edilmiyordu** → ayrı test + doygunluk sonrası "artık dönmüyor" assert'i; AC-6 eğriyi kanıtlamıyordu (ışınlama da geçerdi) → kapalı-form `v0·e^(-k·t)` örneklemesi + monotonluk + AC-6'nın test edilmemiş `IsCarrying` yarısı; AC-8 kendine-referanslıydı → GDD sınırına karşı assert; AC-1/2 yalnız durum alanını okuyordu → gerçek dünya yer değiştirmesi ölçümü eklendi; AC-3'e processor taraması + çapraz-girdi hız testi; AC-4'e pitch kontrolü; AC-9'a pozitif kontrol (çarpışmanın gerçekten olduğu); AC-10'un reflection taraması genişletildi (static + parametre tipleri + base-tip sızıntısı) ve `GetComponent` kapsam uyarısı belgelendi; kilit-BIRAKMA yolu ve duran-oyuncu-sıfır-hız testleri eklendi.

**Test altyapısı bulguları (ileride lazım)**: (1) `PlayModeTests.asmdef`'e `includePlatforms: ["Editor"]` eklemek assembly'yi PlayMode runner'ına GÖRÜNMEZ yapıyor (0 test keşfedilir) — `UnityEditor` erişimi bunun yerine `#if UNITY_EDITOR` ile sarmalanmalı. (2) Unity mesajları (`OnControllerColliderHit` dahil) YALNIZ etkin bileşenlere dağıtılır — testte bileşen kapatılıyorsa çarpışma testleri için tekrar açılmalı. (3) Test içinde üretilen yardımcı sahne objeleri (duvar) TearDown'da yıkılmazsa sonraki testlere sızıp oyuncuyu blokluyor.

**İleri bayraklar (advisory, kapanış engellemiyor)**: gerçek Input System yolu (`OnEnable`→`ReadValue`) uçtan uca test edilmedi — `InputTestFixture` ile sanal klavye/mouse smoke testi Story 004/005'e önerilir (şu an SetUp'ın ilk 31 karesi bu yolu dolaylı koşuyor). `MovementPhaseAccumulator` bu story'de beslenmiyor (Story 005). `Story003PlayerPrefabSetup.cs` tek seferlikti — prefab commit'lendikten sonra silinebilir (mevcut hâli var olan prefab'ı sessizce ezer).
