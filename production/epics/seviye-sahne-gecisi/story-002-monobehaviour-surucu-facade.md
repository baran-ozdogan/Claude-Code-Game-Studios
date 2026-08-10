# Story 002: MonoBehaviour sürücü + Foundation persistent sahnesi + facade

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (Interactions with Other Systems — dışa açılan arayüz)
**Requirement**: `TR-sahne-gecisi-001` (barındırma/wiring yarısı — mantık yarısı Story 001)
*(Requirement metni `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0008 (primary — Execution context); ADR-0003 (secondary — `PlayerStateProvider` persistent-sahne + duplicate-guard emsali)
**ADR Decision Summary**: `SceneTransitionManager`, yeni değil MEVCUT `Assets/Scenes/Foundation.unity` persistent sahnesinde tek bir `GameObject` üzerinde `MonoBehaviour`'dır — ADR-0001'in "no MonoBehaviour" deseninin belgelenmiş TEK istisnası, çünkü 0.5-2s ertelenmiş unload gerçek bir zamanlı gecikme gerektiriyor ve `Coroutine` bu projenin en kanıtlanmış mekanizması. `FoundationBootstrap.ResetAll()`'a KAYITLI DEĞİLDİR — yaşam döngüsü kendi sahnesinin `Awake()`'iyle sıfırlanır, tıpkı UI ve Player sahneleri gibi.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: MEDIUM
**Engine Notes**: `Awake()` Play Mode DIŞINDA hiç koşmaz (bu projede fpc Story 001'de ampirik olarak doğrulandı — Edit Mode'da da koşmaz, `yield return null` sonrası da). Bu yüzden duplicate-guard testi ZORUNLU olarak PlayMode'dur. `_instance` yalnız `Awake()`'te set edildiği için, Unity'nin "Reload Scene" Enter Play Mode ayarı KAPALIYKEN Stop→Play sınırında `Awake()` yeniden koşmayabilir ve `Instance` bayat bir referansa işaret edebilir — ADR-0003'ün `PlayerStateProvider`'ı ile birebir aynı risk, aynı şekilde iki-oturum testiyle doğrulanır (ADR-0008 Validation Criteria).

**Control Manifest Rules (bu katman)**:
- Required: sahne swap'larını hayatta geçirmesi gereken nesneler ÜÇ persistent sahneden birinde yaşar (UI, Player, Foundation), boot'ta bir kez additive yüklenir, asla unload edilmez
- Forbidden: `SceneTransitionManager.Instance`'a bir constructor ya da `ResetOnLoad()` içinden referans (kayıtlı forbidden pattern — `Instance`, `ResetAll()` zamanında YOKTUR)
- Guardrail: aktif geçiş dışında per-frame `Update()` işi yok

---

## Acceptance Criteria

- [x] `SceneTransitionManager : MonoBehaviour, ISceneTransitionManager`, Story 001'in `SceneTransitionState`'ini DÜZ BİR ALAN olarak tutar (`private readonly SceneTransitionState _state = new()`), statik facade olarak DEĞİL
- [x] `public static ISceneTransitionManager Instance` facade'ı, projenin diğer Foundation servisleriyle aynı çağrı konvansiyonunu (`X.Instance.Method()`) sunar
- [x] Duplicate-instance guard: ikinci bir instance `Awake()`'te koşulsuz `Debug.LogError` basar ve kendini `Destroy` eder — derlenip yok olan `Debug.Assert` DEĞİL (ADR-0003 emsali)
- [x] Manager, MEVCUT `Assets/Scenes/Foundation.unity` persistent sahnesinde yaşar; yeni bir sahne oluşturulmaz
- [x] `FoundationBootstrap.ResetAll()`'a KAYITLI DEĞİLDİR — bu servisin `ResetOnLoad()`'ı yoktur; ADR-0001'in beş statik servis sayımı bozulmaz
- [x] Sürücü, çekirdeğin `OnTransitionStateChanged`/`OnSoftTransitionRejected` event'lerini kendi public yüzeyine forward eder; aboneler `_state`'i hiç görmez
- [x] **İki-oturum bayatlık testi**: Unity'nin "Reload Scene" Enter Play Mode ayarı KAPALIYKEN iki ardışık simüle oturum koşulur ve `Instance` ikinci oturumda asla bayat/yok edilmiş bir referans değildir (ADR-0008 Validation Criteria; ADR-0003'ün `PlayerStateProvider` testiyle aynı şekil)

---

## Implementation Notes

*ADR-0008 Execution context + ADR-0003 emsali doğrultusunda:*

- Sürücü İNCE kalır: `Awake` guard'ı, event forward'ı, ve (sonraki story'lerde) coroutine başlatma + `SceneManager` çağrıları. Hiçbir durum kararı burada VERİLMEZ — hepsi `_state`'te (Story 001).
- `Instance` tipi `ISceneTransitionManager`'dır, somut sınıf değil — tüketiciler arayüze derlenir.
- **Lazy abonelik kuralı** bu story'de yalnız BELGELENIR, uygulanmaz: `Adaptif Ses Sistemi` gibi gelecekteki hiçbir servis `OnTransitionStateChanged`'e kendi constructor'ında abone olamaz, çünkü `Instance` `FoundationBootstrap.ResetAll()`'ın `SubsystemRegistration` zamanında HENÜZ YOKTUR (sahne `Awake()`'i onu set eder). Manifest'te kayıtlı forbidden pattern; bu story onu ihlal etmediğini gösterir.
- Sahne kurulumu `game/Assets/Scenes/Foundation.unity`'ye tek bir `GameObject` eklemektir — proje-kurulumu epic'inin kurduğu sahne düzenini bozmadan.

---

## Out of Scope

- Herhangi bir gerçek geçiş: `RequestSoftTransition`/`RequestHardCut`/`PreloadHardCut` gövdeleri bu story'de BOŞ ya da `NotImplementedException` olabilir (Story 003+ doldurur)
- Kabul/ret hakemliği (Story 006)
- `SceneEnvironmentSettings`, anchor repozisyonu, ertelenmiş unload (Story 004)

---

## QA Test Cases

- **AC-1 (otomatik, PlayMode)**: Duplicate guard
  - Given: Foundation sahnesi yüklü, bir `SceneTransitionManager` mevcut
  - When: ikinci bir `GameObject`'e `SceneTransitionManager` eklenir
  - Then: `Debug.LogError` basılır (`LogAssert.Expect`) ve ikinci `GameObject` yok edilir; `Instance` HÂLÂ ilk instance'ı gösterir
  - Edge cases: `Awake()` Edit Mode'da koşmadığı için bu test ZORUNLU olarak `[UnityTest]`; EditMode'a taşınırsa sessizce hiçbir şey test etmez

- **AC-2 (otomatik, PlayMode)**: İki-oturum bayatlık
  - Given: "Reload Scene" Enter Play Mode ayarı kapalı, birinci oturumda `Instance` alınır
  - When: ikinci bir oturum simüle edilir
  - Then: `Instance` non-null VE yok edilmiş bir nesneye işaret etmiyor (Unity'nin `==` aşırı yüklemesiyle kontrol); ikinci oturumda çağrılan bir metot çalışır
  - Edge cases: `Instance` bayatsa `ReferenceEquals(Instance, null)` false ama `Instance == null` true olur — bu ayrımı açıkça assert et

- **AC-3 (otomatik)**: Bootstrap'a kayıtlı DEĞİL
  - Given: `FoundationBootstrap`'ın reset girdileri
  - Then: hiçbiri `SceneTransitionManager`/`SeviyeSahneGecisi` adını taşımaz; ADR-0001'in beş statik servis sayımı korunur
  - Edge cases: bu, ADR-0008'in ADR-0001'de yaptığı düzeltmenin regresyon testidir — birinin "tutarlılık için" ekleme dürtüsünü yakalar

- **AC-4 (otomatik)**: Event forward'ı
  - Given: sürücünün public `OnTransitionStateChanged`'ine bağlı bir abone
  - When: çekirdek `SetState(Preloading, Soft)` yapar
  - Then: abone `(Preloading, Soft)` alır — payload dönüştürülmeden, tam bir kez

---

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/sahne_gecisi_surucu_test.cs` (4 test) + `game/Assets/Tests/EditMode/sahne_gecisi_bootstrap_test.cs` (2 test)
**Status**: [x] Oluşturuldu ve geçiyor (2026-08-10)

| Profil | Sonuç |
|---|---|
| EditMode | **261/261** |
| PlayMode (normal) | **88/88** |
| PlayMode (`m_EnterPlayModeOptions: 3` — Reload Domain+Scene KAPALI) | **88/88** |

Üçüncü satır ADR-0008 Validation Criteria'sının açık şartı. Ayar koşudan sonra
geri alındı (`ProjectSettings` git'te temiz).

---

## Completion Notes

### Foundation sahnesi boştu

`Assets/Scenes/Foundation.unity` sıfır MonoBehaviour taşıyordu. `.unity` YAML'ını
elle düzenlemek kırılgan ve gözden geçirilemez olurdu; anlati Story 003'ün
emsalini izleyip tek seferlik idempotent bir editör betiği yazıldı
(`SahneGecisiStory002FoundationSceneSetup`). Sahne diff'i temiz: tek bir
`GameObject` + Transform + tek MonoBehaviour, `SceneRoots` girdisi; kamera/ışık/
RenderSettings değişikliği YOK.

### Gate bulguları ve uygulananlar

**QL-TEST-COVERAGE → GAPS.** En önemlisi: **iki-oturum testim TİYATROYDU.**
Unload/reload objeyi yok edip yeniden yarattığı için `Awake()` her zaman koşuyor
ve `SceneTransitionManager.cs`'te testi kırmızıya döndüren HİÇBİR mutasyon yoktu —
`OnDestroy`'u tamamen silmek bile yeşil kalıyordu. Üçüncü iddiası
(`CurrentState == Idle`) de ayırt edici değildi: `CurrentState` yönetilen bir alan
okuduğu için yok edilmiş bir karkasta bile çalışır. Dört ayırt edici iddiayla
değiştirildi:

1. 1. oturumun çekirdeği KİRLETİLİR, 2. oturumda taze olduğu doğrulanır
2. unload sonrası `Instance` GERÇEK null (`OnDestroy`'daki temizlik silinirse kırmızı)
3. `AreNotSame` — `DontDestroyOnLoad` eklenirse kırmızı (emsallerin ikisi de bunu taşıyor, benimki düşürmüştü)
4. NATIVE tarafa dokunuş (`gameObject.scene.name`) — karkasta `MissingReferenceException`

Ayrıca AC'nin "yeni sahne oluşturulmaz" yarısı hiçbir yerde kapsanmıyordu; artık
sürücünün Foundation sahnesinde yaşadığı assert ediliyor.

**QL: "Reload Scene kapalı" şartı karşılanmamıştı.** `EditorSettings.asset`
`m_EnterPlayModeOptions: 0` — hiçbir şey kapalı değil, yani hiçbir in-session test
ADR-0008'in tarif ettiği arıza moduna ULAŞAMIYORDU. Projenin kendi emsali var
(proje-kurulumu Story 004, "hem normal hem options=3 profillerinde"). Süit
`options=3` ile ikinci kez koşuldu: yeşil — ama artık kırmızıya dönebilecek bir
koşudan gelen yeşil.

**LP-CODE-REVIEW → CONCERNS:**

- **Stub'lar kilidi sonsuza kadar açık bırakıyordu.** `RequestSoftTransition`/
  `RequestHardCut` sessizce dönüyordu, ama arayüzün sözleşmesi "`onComplete`/
  `onFailed`'den TAM OLARAK BİRİ çağrılır" ve çağıran deseni (ADR-0011/0015)
  "önce kilitle, iki callback'te de bırak". Bir log uyarısının arkasında oyuncu
  KALICI hareket-kilitli kalırdı. İkisi de artık `onFailed("NotImplemented")`
  çağırıyor.
- **`Instance` teardown'da GERÇEK null oluyor** ve bu yeni bir tehlike yaratıyor:
  `OnDisable` içinde `Instance.OnTransitionStateChanged -= ...` yazan bir tüketici
  (ADR-0009'un `AdaptifSesController`'ı, ADR-0012'nin `DialogueSceneController`'ı)
  Play-mode stop'ta Foundation sahnesi önce giderse `NullReferenceException` alır.
  `OnDestroy` korunuyor (arayüz-tipli `Instance` fake-null'dan yararlanamıyor, bu
  yüzden `PlayerStateProvider`'ın aksine gerekli) ama null-kontrol şartı `Instance`'ın
  doc'una yazıldı. **Manifest satırı gerekiyor — iş kalemi.**
- **`GetCurrentHardCutAbrupt()` YARIM sevk edildi.** Yalnız aktif config'i okuyor,
  sözleşme "aktif YA DA preload edilmiş" diyor ve ADR-0015'in ikili eşik yolu tam
  da preload edilmiş bir HARD CUT'ı `RequestHardCut`'tan ÖNCE sorguluyor. Bugün
  yazan hiçbir yol olmadığı için test edilemez; `<remarks>`'a **Story 005'in
  bağlamak zorunda olduğu** madde olarak yazıldı. Bağlanmazsa Adaptif Ses yanlış
  bitiş tonunu çalar.
- `InternalState` → `InternalStateForTests` olarak yeniden adlandırıldı ve doc'u
  düzeltildi: eski hâli ADR-0001'in test desenini kaynak gösteriyordu ama o desen
  bunun TERSİNİ söylüyor (testler taze state kurup enjekte eder, facade'a uzanmaz).
  Ayrıca tüm oyun kodu tek `Foundation.asmdef`'te olduğu için `internal` bir
  bariyer değil — garanti konvansiyon + doc + code review.
- Kurulum betiği: `SaveScene`'in dönüş değeri artık kontrol ediliyor (sessiz
  başarısızlıkta "eklendi" loglayıp 0 ile çıkardı — ADR-0008'in kendi
  `SetActiveScene` bulgusuyla aynı hata sınıfı) ve idempotency taraması
  `GetComponentInChildren` kullanıyor.

**Her iki gate'in ortak bulgusu**: bootstrap sıra testim
`foundation_bootstrap_order_test.cs`'in BİREBİR kopyasıydı — her yeni Foundation
servisinde iki dosyada iki sabit dizi güncellenecekti, sıfır ek ayırt edicilik.
Kırılgan pin kaldırıldı; yalnız "absence iddiası vakumda değil" kontrolü kaldı.

### Açık iş kalemleri

| # | Kalem | Nereye |
|---|-------|--------|
| 1 | `Instance` null-kontrol şartı için manifest satırı (tüketiciler `OnDisable`'da `-=` yaparken Foundation sahnesi önce gitmiş olabilir) | Manifest güncellemesi |
| 2 | `GetCurrentHardCutAbrupt()` preload yolunu okumalı | Story 005 (kodda `<remarks>`'ta yazılı) |
| 3 | Foundation sahnesinde TAM BİR `SceneTransitionManager` olduğunu asset seviyesinde doğrulayan EditMode testi (`fpc_player_scene_asset_test.cs` emsali) — bugün ikinci bir kopya duplicate guard'ı her PlayMode fixture'ında tetikler, gürültülü ama yanlış teste atfedilir | Opsiyonel sertleştirme |

---

## Dependencies

- Depends on: Story 001 (`SceneTransitionState` çekirdeği)
- Unlocks: Story 003 (gerçek geçiş dizisi bu sürücüde koşar)
