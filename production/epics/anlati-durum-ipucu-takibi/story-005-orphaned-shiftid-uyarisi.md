# Story 005: Orphaned shiftId uyarısı (build-time aggregate, non-blocking)

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/anlati-durum-ipucu-takibi.md` (Edge Cases — "hiçbir Işık/Volume tetikleyicisinin ateşlemediği bir shiftId"; AC8)
**Requirement**: `TR-anlati-008` (uyarı yarısı — build-blocking yarısı Story 004)
*(Requirement metni `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0007 (primary — Edit-time validation katman 2); ADR-0014 (secondary — paylaşılan build-doğrulama çatısı), isik-volume Story 006 (`IBuildCheckAggregate` deseni)
**ADR Decision Summary**: İkinci doğrulama katmanı — orphaned `shiftId` (hiçbir tetikleyicinin ateşlemeyeceği bir `requiredShiftIds` girdisi) **build'i ENGELLEMEZ**, yalnız `Debug.LogWarning` üretir ve `GetOrphanedClueIds()`'e düşer. ADR bunu bilinçli olarak Editor-only tuttu (player build'lerine atıl bir kontrol göndermemek için; Alternative 3 — runtime Play-mode kontrolü — gerekçeli reddedildi).

> **MEKANİZMA SAPMASI (kullanıcı kararı, 2026-08-10 — QL-STORY-READY bulgusu)**
> ADR-0007 mekanizma olarak `ClueConsistencyValidator.ValidateScene(sceneId)` +
> `EditorSceneManager.sceneOpened`/`sceneSaved` seçmişti — **tek sahne** gören bir
> tetikleyici. Ama GDD'nin iddiası proje geneli: "HİÇBİR tetikleyicinin ateşlemediği".
> MVP'de Depot ve Ballroom AYRI sahneler ve `ClueDefinition` merkezi/sahne-üstü bir
> kayıt — Depot açıkken Ballroom'un `shiftId`'sini isteyen her meşru clue YANLIŞLIKLA
> "orphaned" görünürdü. Tek-sahne bir mekanizma, GDD'nin proje-geneli iddiasını
> yapısal olarak veremez.
>
> **Karar**: kontrol, isik-volume Story 006'nın tam bu iş için kurduğu
> `IBuildCheckAggregate` (`BeginWalk`/`Run`/`FinalizeWalk`) desenine taşınır —
> runner zaten TÜM Build-Settings sahnelerini geziyor, `shiftId`'ler birleştirilir,
> yürüyüş sonunda tek seferde değerlendirilir. ADR'ın iki KISITI da korunur:
> non-blocking (`Debug.LogWarning`, `context.Fail` DEĞİL) ve player build'lerine
> girmez (build pipeline'ı Editor tarafıdır). Sapan tek şey TETİKLEYİCİ
> (`sceneOpened/sceneSaved` → build preprocess). **ADR-0007'ye addendum gerekir** —
> bu story kapanırken açılacak ileri bayrak.
>
> Bu notu SİLMEYİN: ADR'ın literal metnine "geri düzeltmek" isteyen bir sonraki
> okuyucu, yanlış-pozitif uyarı sorununu geri getirir.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `IBuildCheckAggregate` bu projede zaten kurulu ve LP-review'dan geçmiş (isik-volume Story 006). Sıfırlama yürüyüşün BAŞINDA (`BeginWalk`) olmak ZORUNDA — ortada patlayan bir yürüyüşün bayat gözlemi sonraki build'e sızarsa kontrol yanlış geçer (o story'de LP'nin yakaladığı kritik bug).

**Control Manifest Rules (bu katman)**:
- Required: check `IBuildCheck` implementasyonu + `BuildValidationRegistry` satırı; pointed mesajlar (suçlu clueId/shiftId adlı)
- Forbidden: ikinci bağımsız `IPreprocessBuildWithReport`; build'i engellemek (bu katman non-blocking)
- Guardrail: —

## Acceptance Criteria

- [x] Kontrol `IBuildCheck` + `IBuildCheckAggregate` implement eder, `SceneScan` fazında, `BuildValidationRegistry.Checks`'e kayıtlı (ikinci `IPreprocessBuildWithReport` YOK)
- [x] `BeginWalk()` biriken gözlemi yürüyüşün BAŞINDA sıfırlar (sonda self-reset DEĞİL — aborted-walk sızıntısı, isik-volume Story 006'nın LP bulgusu)
- [x] `Run(context)` her sahnede o sahnenin `ShiftZone`'larının `_shiftId`'lerini biriktirir (inaktif objeler dâhil — build içeriği hepsi)
- [x] `FinalizeWalk(context)` yürüyüş sonunda TEK SEFERDE değerlendirir: `ClueRegistry.Definitions`'taki her `requiredShiftIds` girdisi, birleştirilmiş `shiftId` kümesinde YOKSA o `(clueId, shiftId)` çifti orphaned sayılır
- [x] Her orphaned çift için bir `Debug.LogWarning` basılır ve çift `GetOrphanedClueIds()` üzerinden okunabilir; **build ENGELLENMEZ** (`context.Fail` ASLA çağrılmaz) — bu bir content-authoring uyarısıdır, çalışma zamanı davranışını bozmaz (GDD AC8)
- [x] **Yanlış-pozitif yok**: bir clue'nun `requiredShiftIds`'i FARKLI sahnelerdeki iki `shiftId` içeriyorsa ve ikisi de Build Settings'teki herhangi bir sahnede mevcutsa, uyarı ÜRETİLMEZ (mekanizma sapmasının var oluş sebebi — tek-sahne bir kontrol burada yanlış uyarırdı)
- [x] Sıfır sahne yürüyüşünde (henüz seviye sahnesi yok) sessiz kalır — hiçbir `ClueDefinition` yoksa da sessiz; "hiç yok" da geçerli bir toplam sonuçtur
- [x] Test şekli: `ValidateScene`/`FinalizeWalk` mantığı DOĞRUDAN test edilir; `[InitializeOnLoad]`/runner kancası ince wiring'dir ve ayrıca reflection'la test edilmez

## Implementation Notes

- isik-volume Story 006'nın `IsikVolumeAutomaticPresenceCheck`'i birebir emsaldir (aynı aggregate şekli, aynı BeginWalk gerekçesi) — farkı: o `context.Fail` çağırır, bu `Debug.LogWarning` basar.
- `GetOrphanedClueIds()` GDD'nin adlandırdığı yüzey — sorgulanabilir kalsın (ileride bir editör penceresi tüketebilir), ama tek zorunlu gözlemlenebilir çıktı `Debug.LogWarning`'dir (`LogAssert.Expect` ile test edilir).
- `ClueConsistencyValidator` adı GDD'den korunabilir; sınıf artık `EditorSceneManager` callback'leri yerine build yürüyüşüne bağlanır.
- Fixture'lar runtime-created (`ScriptableObject.CreateInstance` + sahne objeleri), on-disk asset YOK.

## Out of Scope

- Build-blocking dörtlüsü (Story 004)
- ADR-0007 addendum'unun kendisi (`/architecture-decision` ile ayrı açılır — bu story yalnız sapmayı belgeler ve bayrağı diker)
- Orphaned uyarısını gösteren bir editör penceresi/UI

## QA Test Cases

*(QL-STORY-READY üç lensle koştu. Testability GAPS→giderildi: `ValidateScene`'in tek-sahne mekanizmasının GDD'nin proje-geneli iddiasını veremeyeceği ve çok-sahneli clue'larda yanlış-pozitif üreteceği tespit edildi — birinci-sahis-kontrolcu Story 006'daki blocking bulgunun aynı sınıfı; mekanizma kullanıcı kararıyla build-time aggregate'e taşındı. Fidelity: GDD-vs-ADR öncelik notunun story'de AÇIKÇA yer alması istendi — yukarıda kutu içinde.)*

- **AC-1 (otomatik)**: Orphaned shiftId uyarı üretir, build'i kırmaz
  - Given: `requiredShiftIds = ["yok-boyle-bir-shift"]` olan bir clue; sahnelerde o shiftId yok
  - When: yürüyüş tamamlanır
  - Then: `Debug.LogWarning` basılır (clueId + shiftId mesajda), `GetOrphanedClueIds()` çifti içerir, **exception YOK**

- **AC-2 (otomatik)**: Karşılanan shiftId sessiz
  - Given: `requiredShiftIds = ["a"]`, "a" bir sahnedeki `ShiftZone`'da mevcut
  - When: yürüyüş tamamlanır
  - Then: uyarı YOK

- **AC-3 (otomatik)**: Çok sahneli clue yanlış-pozitif üretmez
  - Given: `requiredShiftIds = ["a", "b"]`; "a" birinci sahnede, "b" İKİNCİ sahnede
  - When: iki sahne de gezilir
  - Then: uyarı YOK (tek-sahne bir kontrol burada yanlış uyarırdı — bu testin var oluş sebebi)

- **AC-4 (otomatik)**: Aggregate hijyeni
  - Given: iki ardışık yürüyüş; birincisinde orphaned bir clue var, ikincisinde düzeltilmiş
  - When: ikinci yürüyüş koşar
  - Then: bayat gözlem sızmaz, ikinci yürüyüş temiz
  - Edge cases: sıfır sahne yürüyüşü sessiz; `ClueRegistry` boşsa sessiz

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/anlati_orphaned_clue_test.cs` (17 test) + kayıt/faz iddiaları `anlati_build_checks_test.cs`'te (2 test)
**Status**: [x] Oluşturuldu ve geçiyor — **EditMode 239/239, PlayMode 84/84** (2026-08-10)

| AC | Test |
|----|------|
| AC-1 | `OrphanedShiftId_WarnsAndDoesNotFailBuild`, `OrphanWarning_NamesTheAssetAndTheClueId` |
| AC-2 | `SatisfiedShiftId_DoesNotWarn` |
| AC-3 | `MultiSceneClue_DoesNotWarn`, `MultiSceneClue_WarnsOnlyForTheGenuinelyMissingHalf` (iki sahneli) |
| AC-4 | `SecondWalk_DoesNotInheritStaleObservations`, `AbortedWalk_DoesNotLeakStaleShiftIdsIntoTheNextWalk`, `ZeroSceneWalk_IsSilent`, `ZeroSceneWalk_AfterAPopulatedWalk_IsStillSilent`, `EmptyRegistry_IsSilent` |
| Sınır | `MissingRegistry_IsSilent_…`, `NullSlotInDefinitions_IsSkipped_NotCrashed`, `BlankRequiredShiftIdEntry_IsReportedAsOrphaned`, `BlankZoneShiftId_DoesNotSatisfyAnything`, `ShiftIdMatching_IsCaseSensitive_…`, `MultipleOrphans_AreAllReported`, `DuplicateRequiredEntry_IsReportedOnce` |
| Kayıt | `AllAnlatiChecks_AreRegistered_WithTheirExpectedPhases`, `RegisteredAnlatiChecks_AreWiredToTheProductionSeams` |

---

## Completion Notes

### Mekanizma sapması artık ÜÇ yerde belgeli

Story'nin "bu notu SİLMEYİN" uyarısı gereği sapma yalnız bu dosyada kalmadı:
`ClueConsistencyValidator.cs`'in sınıf doc'unda (gerekçesiyle), `BuildValidation/README.md`'de,
ve **ADR-0007'nin kendisinde** — hem mekanizma maddesinin (satır 152) başına bir
"UYGULAMADA SÜPERSEDE EDİLDİ" bloğu, hem izlenebilirlik tablosunun satırına
üstü-çizili + yeni mekanizma. Addendum hâlâ borç, ama artık ADR'ı açan biri
sapmayı görmeden geçemez.

### Gate bulguları ve uygulananlar

**LP-CODE-REVIEW → CONCERNS.** Kodu onayladı; bulguların HEPSİ çevredeki
dokümantasyondaydı ve en önemlisi benim Story 004'te yazdığım nottu:

- `BuildValidationRegistry.cs` **kendi kendisiyle çelişiyordu**: satır 11-12
  "orphaned requiredShiftId BU LİSTEDE DEĞİL — o non-blocking bir uyarı,
  `IBuildCheck` değil" diyordu, 36 satır altında `new ClueConsistencyValidator()`
  duruyordu. Story'nin korktuğu "mekanizmayı geri alan okuyucu" senaryosu için
  bundan iyi yem olamazdı. Sınıf doc'unun "every build-blocking validation check"
  açılışı da artık yanlıştı — ikisi de düzeltildi.
- `README.md` aynı yanlış iddiayı taşıyordu; kayıtlı-check tablosuna beşinci
  check non-blocking işaretiyle eklendi ve "Nasıl check eklenir" adımı 3'e
  non-blocking varyant yazıldı (çatının artık böyle bir örneği var).
- `control-manifest.md` satır 102 "hepsi bloklar" gibi okunuyordu — açıklayıcı
  cümle eklendi (non-blocking olabilir AMA yine aynı utility'ye kaydolur).
- LP ayrıca doğruladı: `BeginWalk` üç alanı da sıfırlıyor ve `FinalizeWalk` hiç
  self-reset yapmıyor; `context.Fail`'e giden hiçbir yol yok; paylaşılan static
  instance'ta biriktirme aggregate sözleşmesinin MEŞRU istisnası (Story 004'ün
  uyarısı harici KAYNAK cache'lemekle ilgiliydi, yürüyüş gözlemiyle değil) ve bu
  check kaydı `FinalizeWalk`'ta bir kez yüklüyor — Story 004'ün sahne-başına
  yüklemesinden daha iyi.

**QL-TEST-COVERAGE → GAPS** (hepsi kapatıldı):

- **`_scenesWalked = 0` sıfırlaması PİNLENMEMİŞTİ**: o satır silinse 15 testin
  hepsi yeşil kalıyordu, çünkü her test taze bir validator kuruyor ve hiçbiri
  dolu bir yürüyüşün ARDINDAN sıfır sahneli yürüyüş yapmıyordu. AC-2'nin
  doğrudan gereği, üç alandan biri kapsamsız. Test eklendi.
- **Yinelenen girdi (`["x","x"]`) kararsız ve testsizdi** — iki özdeş uyarı
  basıyordu. `ClueDefinition`'ın tooltip'i yinelenen girdiyi "zararsız" diye
  yazdığı ve runtime da öyle davrandığı için karar: tanım başına tekilleştir.
  Pinlendi.
- **`BlankRequiredShiftIdEntry_…` regex'i yalnız asset adına bakıyordu** — o alt
  dize `AnlatiContentValidation.Label()`'ın da ürettiği bir şey, yani fixture
  ileride `OnValidate`'i tetiklerse iddia yanlış log'la tatmin olurdu (Story
  004'te tam olarak bu yaşandı). `shiftId='…'` şekline bağlandı.
- **`NullSlotInDefinitions_…` beyan edilmemiş bir uyarı üretiyordu**
  (`ClueRegistry.OnValidate`'in null-slot ihlali). Açıkça `LogAssert.Expect`
  edildi ki yan etki kayıtlı olsun.
- `MultiSceneClue_WarnsOnlyForTheGenuinelyMissingHalf` tek sahneliydi; iki
  sahneli yapıldı — birleştirme kanıtı ile "blanket-silence değil" kanıtı artık
  aynı testte.
- `GetOrphanedClueIds()` CANLI liste dönüyordu; ileride bir editör penceresi
  tüketirse sonraki build'in `BeginWalk`'ı elindeki sonucu altından temizlerdi.
  Kopya dönüyor, doc'taki "son TAMAMLANMIŞ yürüyüş" ifadesi de düzeltildi
  (iptal edilen yürüyüşten sonra BOŞ döner).
- QL doğruladı: `LogAssert.NoUnexpectedReceived()` uyarılarda GERÇEKTEN düşüyor
  (paket kaynağı okundu), yani sessizlik iddiaları güçlü; ve hiçbir
  `LogAssert.Expect` `OnValidate` log'uyla tatmin olamıyor.

### Test fixture hatası (üretim kodu değil)

`AbortedWalk_…` ilk koşuda kırmızıydı: iptal edilen yürüyüşte kurulan bölge yok
edilmeden ikinci yürüyüşe kalıyordu, `FindObjectsByType` sahne-kör olduğu için
onu hâlâ görüyordu — yani test kendi fixture sızıntısını ölçüyordu, ölçmek
istediği gözlem sızıntısını değil. Bölgeler elle yok ediliyor.

### Bilinen sınırlar

| Sınır | Not |
|-------|-----|
| Sıfır-sahne guard'ı üretimde fiilen ölü | `IsikVolumeAutomaticPresenceCheck` daha önce kayıtlı ve sıfır sahnede `Fail` ediyor, yani finalize döngüsü buraya ulaşmıyor. Guard yine de doğru ve korunuyor (kayıt sırası değişirse gerekir) |
| Uyarı yalnız aksi hâlde yeşil build'lerde görünür | Check en sona kayıtlı; bloklayan bir SceneScan hatası önce build'i düşürür. Doğru öncelik (hatalar uyarılardan önce) ama bilinmesi gerekiyor |
| `FindObjectsByType` sahne başına 6. tam tarama | 2 MVP sahnesinde ihmal edilebilir; paylaşılan bir cache invalidation disiplini gerektirir, ayrı iş kalemi |

## Dependencies

- Depends on: Story 002 (`ClueDefinition`/`ClueRegistry` tipleri), isik-volume Story 006 (Complete — `IBuildCheckAggregate` çatısı + `ShiftZone._shiftId`)
- Unlocks: içerik yazımında "sonsuza kadar tamamlanamaz" ipuçları erken yakalanır
