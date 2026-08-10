# Story 003: Addressables lazy-load + gerçek Işık/Volume aboneliği

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/anlati-durum-ipucu-takibi.md` (Core Rules — abonelik zamanlaması; Interactions — Persistent re-fire notu; AC11/AC12)
**Requirement**: `TR-anlati-006`, `TR-anlati-005` (wiring yarısı — mantık yarısı Story 002)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0007 (primary — Registry loading bölümü + Alternative 4'ün reddi); ADR-0015 (secondary — in-place rejim, ResetOnLoad Addressables'a dokunmaz)
**ADR Decision Summary**: `ClueRegistry`, `Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry").WaitForCompletion()` ile yüklenir — `Resources.Load` DEĞİL (proje `deprecated-apis.md`'de onu deprecated listeliyor; ADR'ın ilk taslağı bunu UYDURMA bir alıntıyla savunmuştu, unity-specialist yakaladı). Yükleme constructor'dan ÇIKARILMIŞTIR: ilk gerçek `OnShiftStateChanged(Held)` içinde lazy — böylece `FoundationBootstrap.ResetAll()`'ın ultra-erken `SubsystemRegistration` zamanlamasındaki Addressables-hazırlık belirsizliği tamamen atlanır (gerçek bir Held ancak fiilî oynanışta olur, boot sırasında imkânsız). Abonelik ise constructor'da kalır (saf C# event, engine API'si değil — o riski taşımaz).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: MEDIUM — *bu projenin İLK gerçek Addressables tüketicisi*. Mekanizma stabil ama **kullanım yeni**: repoda şu an `AddressableAssetsData` (settings asset'i) HİÇ YOK; paket (`com.unity.addressables` 4.0.1) kurulu ama proje hiç initialize edilmemiş. Bu story o kurulumu da yapar.
**Engine Notes**: `WaitForCompletion()` çağıran thread'i bloklar — burada kabul edilebilir (oturum başına EN ÇOK bir kez, nadir bir oynanış olayında, tek küçük asset; per-frame DEĞİL, loading screen'de DEĞİL). Lazy guard (`if (_byRequiredShiftId != null) return;`) bunu yapısal olarak garanti eder; çağrı yerine bunu belirten yorum ZORUNLU ki ileride biri onu sıcak yola taşımasın (ADR Risks).

**Control Manifest Rules (bu katman)**:
- Required: Asset yükleme Addressables üzerinden, **lazy**, herhangi bir `FoundationBootstrap`-yolu constructor'ının DIŞINDA; constructor-time abonelikler süreç başına bir kez bağlanır ve her `ResetAll()`'ı hayatta geçirir (hiçbir `ResetOnLoad()` içinde yeniden-wire YOK)
- Forbidden: `ResetOnLoad()`'ın bir engine asset API'sine dokunması; `Resources.Load()`
- Guardrail: bloklayan yükleme oturum başına en çok bir kez

## Acceptance Criteria

- [x] **Addressables proje kurulumu**: `AddressableAssetSettings` oluşturulur, `ClueRegistry` asset'i Addressable olarak işaretlenir ve anahtarı TAM OLARAK `"ClueRegistry"` olur (repoda bugün hiç Addressables altyapısı yok — bu story onu kurar)
- [x] `EnsureRegistryLoaded()`: `_byRequiredShiftId` null değilse ANINDA döner; null ise `Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry").WaitForCompletion()` ile yükler ve ters indeksi kurar. Çağrı yerinde "oturum başına en çok bir kez, sıcak yola TAŞIMA" yorumu bulunur (ADR Risks mitigasyonu)
- [x] `EnsureRegistryLoaded()` YALNIZ ilk gerçek `OnShiftStateChanged(Held)` içinden çağrılır — `AnlatiDurumState` constructor'ından ASLA (TR-anlati-006 gerekçesi: SubsystemRegistration-zamanlaması belirsizliği)
- [x] **First-access abonelik, FACADE'ın static constructor'ında** (`AnlatiDurumIpucuTakibi`), `AnlatiDurumState`'in kendi constructor'ında DEĞİL — `IsikVolumeDurumSistemi.Instance.OnShiftStateChanged`'e bağlanır; sahne-lokal bir `Awake`/`OnEnable` DEĞİL (GDD Core Rules "Abonelik zamanlaması", TR-anlati-006). Abonelik süreç başına bir kez bağlanır, `ResetOnLoad()` yeniden-wire ETMEZ.
  > **ADR-0007'den bilinçli sapma (LP-CODE-REVIEW bulgusu, anlati Story 001)**: ADR'ın Data model bloğu aboneliği `AnlatiDurumState` constructor'ına koyuyor. Bu kod tabanı için YANLIŞ: testler ADR-0001 deseni gereği taze `AnlatiDurumState` kurar (Story 001'in 13 testinin hepsi `[SetUp]`'ta bir tane kuruyor) — State-ctor aboneliği her testte kalıcı `OnShiftStateChanged` event'ine bir handler daha sızdırır ve "saf state" testlerini gerçek facade'a bağımlı kılar (manifest ihlali). Proje emsali `GeceOturumDurumu` de aboneliği FACADE static ctor'ında tutuyor. **ADR-0007'ye addendum gerekir** (Story 005'in mekanizma sapmasıyla aynı pakette açılabilir).
  > Testin şekli buna göre: abone sayısı ilk **facade** erişimi etrafında ölçülür (`gece_oturum_subscription_test.cs`'in `ShiftEventSubscriberCount` deyimi), `new AnlatiDurumState()` etrafında değil.
- [x] `OnShiftStateChanged` ince bir SARMALAYICIDIR: `newState != Held` ise erken çık → `EnsureRegistryLoaded()` → Story 002'nin `ProcessHeldShift(shiftId)` metodunu çağır. Story 002'nin mantığı burada YENİDEN YAZILMAZ
- [x] **Persistent re-fire çift event üretmez** (GDD AC11, TR-anlati-005 wiring yarısı): sahne yeniden yüklendiğinde Işık/Volume'un tek seferlik `OnShiftStateChanged(Held)` re-fire'ı, zaten Known bir ipucu için İKİNCİ bir `OnClueKnown` üretmez (`HashSet.Add` idempotency'si hem `_seenShiftIds` hem `_knownClueIds`'te)
- [x] **Reload sonrası doğrudan sorgu doğru cevap verir** (GDD AC12): bir ipucu Known olduktan ve sahne yeniden yüklendikten sonra `IsClueKnown(clueId)` — event'e HİÇ abone olmadan — `true` döner. *Bu AC bu sistemin KENDİ facade'ı üzerinden test edilir; Diyalog/Anlatı İçeriği'nin (henüz yazılmamış, yalnız quick-spec) gerçek implementasyonuna bağımlılık kurulmaz ve onun callback içeriği assert EDİLMEZ* (GDD Sahiplik notu: bu doküman o hissi garanti etmez)
- [x] `ResetOnLoad()` Addressables'a ASLA dokunmaz; yüklenmiş `ClueRegistry` cache'i (`_byRequiredShiftId`) oturumlar arası KORUNUR — değişmez authored config, yeniden yüklemeye gerek yok (ADR-0015 in-place rejim notu)
- [x] **[DEFERRED — manuel]** ADR-0007'nin "Verification Required" smoke'u: `Addressables.LoadAssetAsync<ClueRegistry>("ClueRegistry").WaitForCompletion()`'ın gerçek bir oynanış Held olayında çözüldüğü, **güncel pinlenmiş 6000.5.6f1** editöründe bir kez elle doğrulanır (ADR'ın smoke notu 6000.3.0f1'i anıyor — 2026-08-09 re-pin'inden önce yazıldı). Otomatik CI testi DEĞİL: Addressables'ın gerçek anahtar çözümlemesi on-disk asset + settings gerektirir, projenin "fixture'lar runtime-created" konvansiyonuyla çelişir. Kanıt: `production/qa/evidence/anlati-addressables-smoke-evidence.md`

## Implementation Notes

- **Story 002'den devralınan iki karar (LP gate bulguları)**:
  1. **Yükleme BAŞARISIZ olursa ne olacağı burada seçilmeli.** `IsRegistryLoaded`
     "indeks var" demektir, "yükleme başarılı oldu" değil. `WaitForCompletion()`
     patlarsa `_byRequiredShiftId` null kalır ve bloklayan Addressables çağrısı
     HER Held'de yeniden denenir. Manifest'in "başarısız yükleme bir build
     defektidir, kurtarılabilir bir durum değildir" duruşu latch'lemeyi (bir kez
     dene, başarısızsa boş indeksle latch'le + `Debug.LogError`) öneriyor —
     Story 004'ün build-blocking anahtar kontrolü zaten bu vakayı üretimde
     imkânsız kılıyor. Kararı AÇIKÇA ver ve testle sabitle.
  2. **Abone istisnası artık Işık/Volume'un multicast'ine düşüyor.** Story 002
     `ProcessHeldShift`'in bir abone patlarsa kalan aday tanımları atladığını
     belgeledi ve testle sabitledi. Canlı event'e bağlandığında aynı istisna
     Işık/Volume'un abone listesinde YUKARI çıkar ve çağrı sırasına göre diğer
     aboneleri (ör. Gece/Oturum'un Settled yazımı) düşürebilir. Bu story'de
     yeniden değerlendirilmeli: sarmalayıcıda yakalanacak mı, yoksa bilinçli
     olarak yayılmaya devam mı edecek?

- Bu story'nin otomatik testleri Addressables'a BAĞIMLI OLMAMALI: `_byRequiredShiftId`'ye test-enjeksiyon dikişi (Story 002'nin dikişinin bir üst katmanı) kullanılarak AC11/AC12/abonelik davranışı Addressables kurulumundan bağımsız doğrulanır. Emsal: `gece-oturum-durumu` Story 004, gerçek wiring'i abonelik-sayısı + davranışsal yarım ile doğruladı.
- Abonelik testi için `gece_oturum_subscription_test.cs`'in reflection'la abone sayısı ölçen `ShiftEventSubscriberCount` helper deseni yeniden kullanılabilir.
- `OnShiftStateChanged` imzası Işık/Volume'un event'iyle birebir: `(string shiftId, ShiftState newState, Vector3 zoneCenter, float radius)` — `zoneCenter`/`radius` bu sistem tarafından TÜKETİLMEZ (GDD: yalnız mekansal ses/görsel tüketiciler için taşınıyor).

## Out of Scope

- Held tamamlanma mantığı (Story 002 — burada yalnız sarmalanır)
- Edit-time doğrulama (Story 004/005)
- Diyalog/Anlatı İçeriği'nin kendi seçim mantığı (o sistemin kendi epic'i; AC12 yalnız bu facade'ın sorgu doğruluğunu kanıtlar)

## QA Test Cases

*(QL-STORY-READY üç lensle koştu. Testability GAPS→giderildi: Addressables smoke'u otomatik AC olmaktan çıkarılıp DEFERRED manuel kanıta çevrildi [repoda Addressables altyapısı hiç yok — arama ile doğrulandı], AC11/AC12 için Addressables'tan bağımsız test dikişi zorunlu kılındı, AC12 bu sistemin kendi facade'ına daraltıldı. Scope GAPS→giderildi: smoke ayrı bir AC maddesi oldu. Fidelity GAPS→giderildi: AC12'nin Diyalog'a bağımlanmaması açıkça yazıldı.)*

- **AC-1 (otomatik)**: Lazy yükleme yalnız ilk Held'de
  - Given: taze state, ters indeks henüz kurulmamış
  - When: Dormant/Shifting-In event'leri gelir
  - Then: yükleme TETİKLENMEZ
  - When: ilk Held gelir
  - Then: yükleme tam bir kez tetiklenir; sonraki Held'lerde tekrar TETİKLENMEZ

- **AC-2 (otomatik)**: Facade static-ctor aboneliği
  - Given: facade'a İLK erişim (`AnlatiDurumIpucuTakibi.Instance`)
  - When: Işık/Volume event abone sayısı ölçülür
  - Then: tam bir abone artışı; `ResetOnLoad()` sonrası abone sayısı DEĞİŞMEZ (yeniden-wire yok, birikme yok)
  - Edge cases: taze `new AnlatiDurumState()` kurmak abone sayısını ARTIRMAZ (abonelik State'te değil facade'da — Story 001'in saf-state testleri bu yüzden gerçek event'e dokunmuyor)

- **AC-6 (otomatik)**: İki-oturum tam-bir-kez teslimi (Story 001'den devralındı)
  - Given: bir ipucu Known, abone bağlı
  - When: oturum sınırı simüle edilir (`ResetOnLoad`) ve ikinci oturumda aynı shift yeniden Held'e ulaşır
  - Then: abone hayatta, event tam bir kez daha fırlar; birikme/yetim handler yok
  - *(Manifest'in Cross-Cutting kuralı her statik facade için iki-oturum `[UnityTest]` istiyor; gece-oturum bunu kendi Story 001'inde teslim etmişti — burada gerçek abonelik Story 003'te doğduğu için buraya ait)*

- **AC-3 (otomatik)**: Persistent re-fire çift event üretmez
  - Given: `[A]` gerektiren bir clue, A Held'e ulaşmış, clue Known
  - When: aynı `(A, Held)` event'i tekrar fırlar (reload re-fire simülasyonu)
  - Then: `OnClueKnown` İKİNCİ kez fırlamaz, hata yok

- **AC-4 (otomatik)**: Reload sonrası doğrudan sorgu
  - Given: bir clue Known, sonra oturum-içi sahne reload simüle edilir
  - When: `IsClueKnown(clueId)` çağrılır (event aboneliği OLMADAN)
  - Then: `true` döner
  - Edge cases: `ResetOnLoad()` çağrılırsa Known kümesi temizlenir AMA registry cache'i korunur (yeniden yükleme yok)

- **AC-5 (manuel, DEFERRED)**: Addressables smoke
  - Setup: `ClueRegistry` asset'i Addressable, anahtar `"ClueRegistry"`; editör 6000.5.6f1
  - Verify: gerçek bir oynanış Held olayında `WaitForCompletion()` asset'i çözüyor, exception yok
  - Pass condition: konsолda hata yok, `_byRequiredShiftId` doldu; kanıt dosyasına sürüm + tarih + gözlem yazıldı

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/anlati_addressables_subscription_test.cs` (6 test) + `production/qa/evidence/anlati-addressables-smoke-evidence.md` (DEFERRED manuel smoke)
**Status**: [x] Oluşturuldu ve geçiyor — **EditMode 182/182, PlayMode 84/84** (2026-08-10)

| AC | Test |
|----|------|
| AC-1 | `RegistryLoad_NeverOnNonHeld_ExactlyOnceAcrossRepeatedHelds` |
| AC-2 | `FacadeFirstAccess_SubscribesExactlyOnce_AndSurvivesReset` |
| AC-3 | `RealShiftEvent_ReachesClueTracking_AndPersistentRefireDoesNotDoubleFire` |
| AC-4 | `AfterSessionBoundary_RegistryCachePreserved_KnownCluesCleared` |
| AC-5 | DEFERRED manuel → kanıt dosyası (editor-içi PASS; player-build Story 004'e bağlı) |
| AC-6 | `TwoSessions_RealEventPath_DeliversExactlyOncePerSession` |
| — | `RegistryLoadFailure_IsLatched_NotRetriedEveryHeld` (aşağıdaki Karar 1'i sabitler) |

---

## Completion Notes

### Story 002'den devralınan iki kararın sonucu

**Karar 1 — Yükleme başarısızlığı: LATCH'lenir.** `MarkRegistryLoadFailed()`
`RegistryLoadFailed`'i set eder ve `BuildReverseIndex(null)` ile BOŞ bir indeks
kurar; `IsRegistryLoaded` böylece `true` olur ve bloklayan Addressables çağrısı
bir daha ASLA denenmez. Latch `ResetOnLoad()`'ı da hayatta geçirir — doğrusu bu:
bir build defekti oturum sınırında iyileşmez, yani guardrail "oturum başına bir
kez"den güçlü, **süreç başına bir kez**tir.
Severity `Debug.LogError`, throw DEĞİL: `TransitionTo(Held)` `ShiftZone`'un tick
coroutine'inden çağrılıyor; buradan bir throw o coroutine'i öldürür ve Persistent
olmayan bir bölgeyi ticker'sız `Held`'de bırakır. Degrade-and-log oyunu oynanır
tutar (ipucu-kilitli diyalog yalnız açılmaz) ve UTF beklenmeyen `LogError`'da
düştüğü için CI'yı yine de kırar.

**Karar 2 — Abone istisnası: try/catch EKLENMEDİ, etki alanı ölçüldü.** Story
002'nin notu "Gece/Oturum'un Settled yazımı düşebilir" diyordu; bu **yanlıştı**.
`FoundationBootstrap.ResetAll()` `GeceOturumDurumu`'nu (adım 3) Anlatı'dan (adım
4) önce dokunduğu için Gece'nin static ctor'ı önce koşar ve invocation list'te
Anlatı'dan ÖNCE gelir — bu handler onu asla düşüremez. Gerçek etki alanı
SONRADAN abone olan `OnEnable` MonoBehaviour'ları (planlanan
`AdaptifSesController`) ve `ShiftZone`'un kendi tick coroutine'i. Bu, kararı
GÜÇLENDİRİR: doğru koruma tek fırlatma noktası olan
`IsikVolumeState.RaiseShiftStateChanged` içinde delege başına try/catch olurdu —
hem yan aboneleri hem üreticiyi korur. Tek bir story'nin tek taraflı çözeceği şey
değil; **isik-volume epic'ine FD-1 olarak yazıldı**.

### Gate bulguları ve uygulananlar

**LP-CODE-REVIEW → CONCERNS** (hepsi giderildi):
- `finally { _ = handle; }` ÖLÜ KOD'du — derleyici discard'ı tamamen eliyor,
  hiçbir ömür garantisi vermiyordu ve ileride birinin gerçek bir `Release`
  eklemesini davet ediyordu. Blok silindi, gerekçe yorum olarak korundu (başarı
  yolunda Release ETMEMEK zaten refcount'un çalışma biçimi).
- **Başarısız yollarda artık `Addressables.Release`** ediliyor
  (`ReleaseFailedHandle`, `IsValid()` guard'lı): orada hiçbir şeyin canlı kalması
  gerekmiyor ve serbest bırakılmamış başarısız operasyon ResourceManager'da
  adrese göre cache'li kalıyordu.
- `ProcessShiftStateChanged` bozuk `shiftId` için bloklayan yüklemeyi boşuna
  tetikliyordu — guard yükleme çağrısının önüne alındı.
- `WaitForCompletion()` yorumu: ilk çağrının Addressables `InitializeAsync`'ini
  (katalog yüklemesi) de zincirlediği eklendi; ADR-0007'nin "ihmal edilebilir"i
  harfiyen alınmamalı.
- Kurulum betiği `Directory.CreateDirectory` yerine
  `AssetDatabase.IsValidFolder`/`CreateFolder` kullanıyor (başkalarının
  kopyalayacağı şablon).
- Ratified: latch kararı, `LogError` severity'si, facade static-ctor aboneliğinin
  `GeceOturumDurumu` emsaliyle birebir uyumu, manifest uyumu.

**QL-TEST-COVERAGE → GAPS** (hepsi kapatıldı):
- **AC-6 hiç kapsanmıyordu** (manifest cross-cutting kuralı zorunlu kılıyor).
  `TwoSessions_RealEventPath_DeliversExactlyOncePerSession` eklendi: abone bağla →
  gerçek `Held` → reset sınırı → gerçek `Held`; 1 ise yukarı akış aboneliği
  koptu, 3+ ise çoğaldı.
- **`NonHeldRealEvents_DoNotTriggerRegistryLoad` KISIRdı**: `IsRegistryLoaded`'i
  prob olarak kullanıyordu, ama indeksi hiçbir üretim yolu temizlemediği için
  kardeş test onu doldurmuşsa iddia her hâlükârda geçiyordu — ve AC-1'in "tam bir
  kez, sonra tekrar değil" yarısının hiç testi yoktu. Yükleyici kancasının
  kendisini sayan `RegistryLoad_NeverOnNonHeld_ExactlyOnceAcrossRepeatedHelds`
  ile değiştirildi; AC-1'in tamamını kapsıyor.
- **`RegistryLoadFailure_IsLatched` süreci KALICI ZEHİRLİYORDU**: latch ne
  `ResetOnLoad` ne teardown tarafından açılıyordu, yani o testten sonra gerçek
  Addressables yolu süreç ömrü boyunca kapalıydı. `ResetRegistryForTests()` seam'i
  eklendi; `[SetUp]`/`[UnityTearDown]` hem latch'i hem indeksi hem üretim
  yükleyicisi bağını geri alıyor (fixture-lar arası sızıntı da kapandı).
- **AC-2 yetersiz iddia ediyordu**: `Assert.Greater(count, 0)` tek başına
  Gece/Oturum'un aboneliğiyle karşılanıyordu — facade'ın kendi `+=` satırı
  silinse test yine geçerdi. `SubscriberCountDeclaredIn(typeof(...))` ile
  invocation list tipe göre filtrelenip **tam olarak 1** iddia ediliyor.
- Test hijyeni: `[SetUp]` sınır çağrısı (gece-oturum emsali), `ClueDefinition`
  ScriptableObject'leri teardown'da `DestroyImmediate`, `UnityEditor` kullanımı
  `#if UNITY_EDITOR` ile korundu.
- **AC-5 deferral gerekçesi DÜZELTİLDİ**: story'deki "repoda Addressables
  altyapısı yok" gerekçesi artık geçersiz (bu story onu commit etti). Geçerli
  gerekçe: editor-içi PlayMode yüklemesi AssetDatabase provider'ından geçer ve
  **build edilmiş player'ın content catalog'u** hakkında hiçbir şey kanıtlamaz —
  ADR-0007'nin işaret ettiği gerçek risk odur. Kanıt dosyası bu gerekçeyle yazıldı.

### Story 003 dışı, aynı koşuda düzeltilen

`fpc_persistent_scene_test.cs`'in iki null-anchor testi, araya giren karede
yerçekiminin oyuncuyu ~1.6mm oturtmasıyla 0.001'lik 3B toleransta kırıldı — bu
story yeni bir PlayMode dosyası ekleyip sırayı değiştirdiğinde ortaya çıkan gizli
bir kırılganlıktı. İlk düzeltme ölçümü yatay eksene indirmişti; QL'in işaret
ettiği daha güçlü form uygulandı: `fpc.enabled = false` ile sürücü durduruluyor
ve iddia **üç eksende de tam kuvvetinde** 3B `Vector3.Distance` olarak kaldı
(emsal: `RepositionTo_CopiesPositionAndRotation_PreservesIdentityAndState`).

### Bilinen açık kalemler

| # | Kalem | Sahip |
|---|-------|-------|
| 1 | **ADR-0007 addendum borcu (2 madde)**: (a) Story 005'in orphan-check mekanizması `sceneOpened/sceneSaved`'den `IBuildCheckAggregate` build-walk'a taşındı; (b) abonelik `AnlatiDurumState` ctor'ında değil FACADE static ctor'ında | anlati epic kapanışı |
| 2 | `IsikVolumeState.RaiseShiftStateChanged` delege-başına try/catch | isik-volume EPIC.md → FD-1 |
| 3 | `ClueRegistry.asset` BOŞ ship ediliyor. Üç build kontrolü de boş kayıtta geçer — "içerik hiç yazılmadı" sessizce ship olur. Story 004'te bloklamayan bir uyarı düşünülmeli | anlati Story 004 |
| 4 | Player-build catalog doğrulaması (kanıt dosyası §3) | anlati Story 004 |

## Dependencies

- Depends on: Story 002 (`ProcessHeldShift` + ters indeks), isik-volume epic (Complete — `OnShiftStateChanged` kaynağı)
- Unlocks: Diyalog/Anlatı İçeriği epic'inin `IsClueKnown` tüketimi
