# Story 004: Build-blocking doğrulama dörtlüsü

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/anlati-durum-ipucu-takibi.md` (Core Rules — requiredShiftIds boş olamaz, merkezi tek kayıt; Edge Cases; AC8a/AC8b) + `design/gdd/isik-volume-durum-sistemi.md` (AC22 — devralınan çapraz kontrol)
**Requirement**: `TR-anlati-008` (build-blocking yarısı — uyarı yarısı Story 005), `TR-anlati-009` (YENİ — Addressable anahtar çözümlemesi), `TR-isik-021` (AC22 yarısı, isik-volume epic'inden DEVRALINDI)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da — `TR-anlati-009` bu story'de mint edilir)*

**ADR Governing Implementation**: ADR-0007 (primary — Edit-time validation bölümü); ADR-0014 (secondary — paylaşılan build-doğrulama çatısı)
**ADR Decision Summary**: İki katmanlı doğrulamanın build-blocking katmanı. Kontroller paylaşılan `BuildValidationRegistry.Checks`'e kaydedilir — **ikinci bağımsız `IPreprocessBuildWithReport` manifest tarafından YASAK** (proje TEK utility kullanır). `OnValidate()` aynı paylaşılan doğrulama metodunu çağırır (Inspector-time erken geri bildirim); anahtar-çözümleme kontrolünün `OnValidate()` karşılığı YOK (bağlanacak tekil bir asset instance'ı yok).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `IPreprocessBuildWithReport` uzun süredir stabil. Fixture'lar runtime-created (`ScriptableObject.CreateInstance`) — on-disk test asset'i YASAK (isik-volume Story 006 / fpc Story 006 emsali).

**Control Manifest Rules (bu katman)**:
- Required: check `IBuildCheck` implementasyonu + `BuildValidationRegistry` satırı; pointed hata mesajları (suçlu asset adlı)
- Forbidden: ikinci bağımsız `IPreprocessBuildWithReport`
- Guardrail: —

## Acceptance Criteria

- [x] **Check (b) — boş `RequiredShiftIds`**: `ClueRegistry.Definitions`'taki herhangi bir `ClueDefinition`'ın `RequiredShiftIds`'i boşsa `BuildFailedException`, mesaj suçlu asset'i adlandırır (GDD AC8a). Gerekçe: `SeenShiftIds ⊇ ∅` her zaman doğru olduğundan boş liste, sistem hiç başlatılmadan "Known" sayılırdı — sessiz vacuous-truth hatası
- [x] **Check (c) — çift `clueId`**: iki farklı `ClueDefinition` aynı `ClueId`'yi taşıyorsa `BuildFailedException`, mesaj **çakışan İKİ kaydı da** işaret eder (GDD AC8b)
- [x] **Check (a) — `"ClueRegistry"` Addressable anahtarı çözülür**: anahtar `AddressableAssetSettings` üzerinden (runtime yükleme DEĞİL) çözülemezse `BuildFailedException` (ADR-0007 Risks mitigasyonu + Validation Criteria; GDD'de karşılığı YOK — bu yüzden `TR-anlati-009` mint edilir). **Test dikişi ZORUNLU**: check, `AddressableAssetSettingsDefaultObject.Settings`'i doğrudan ÇAĞIRMAZ; `internal interface IAddressableKeyResolver { bool KeyResolves(string key); }` üzerinden çağırır (üretimde gerçek settings'i sarmalar, testte iki satırlık sahte). `IBuildSceneWalker` ile aynı şekil — böylece test in-memory kalır ve "AddressableAssetSettings in-memory fabrikeedilebilir mi" belirsizliği tamamen atlanır
- [x] **Check (d) — AC22 çapraz kontrolü (isik-volume epic'inden DEVRALINDI, TR-isik-021)**: zorunlu `TriggerMode=Automatic` bölgenin `shiftId`'si hiçbir `ClueDefinition.requiredShiftIds`'inde yer ALMAMALI; alırsa `BuildFailedException`. Gerekçe: pasif/ambiyans bir bölge, ipucu kazanımının `ManualOnly` rıza ön koşulunu sessizce atlatırdı. *Bu kontrol isik-volume Story 006'da bilinçli ertelendi (`ClueDefinition` tipi bu epic'te doğduğu için) — `BuildValidationRegistry` TODO satırı ve o story'nin Completion Notes'u bunu kayıt altına almış*
- [x] Dört kontrol de paylaşılan `BuildValidationRegistry.Checks` dizisine kaydedilir; ikinci bir `IPreprocessBuildWithReport` YAZILMAZ; registry'nin `TODO(epic:anlati-durum-ipucu-takibi)` satırı gerçek kayda dönüşür ve `BuildValidation/README.md`'nin kayıtlı-check tablosu güncellenir
- [x] **Kapsam kilidi**: check'ler `ClueRegistry.Definitions` üzerinde çalışır, `AssetDatabase.FindAssets<ClueDefinition>` üzerinde DEĞİL. (ADR'ın kendi ifadesi: "her `ClueDefinition` referenced by `ClueRegistry.Definitions`". `FindAssets`'e kaymak — AssetScan fazının adı bunu davet ediyor — testleri in-memory fixture'larla imkânsız kılar ve README'nin uyardığı on-disk-test-verisi tuzağına düşer)
- [x] `OnValidate()`, check (b) ve (c)'nin PAYLAŞILAN doğrulama metodunu çağırır (Inspector-time geri bildirim). Test edilen birim o paylaşılan metottur; `OnValidate()`/`IBuildCheck.Run()` ince çağrı yerleridir ve reflection-invoke jimnastiğiyle ayrıca test EDİLMEZ

## Implementation Notes

- isik-volume Story 006'nın `IsikVolumeBuildChecks.cs` + `isik_volume_build_checks_test.cs` deseni birebir izlenir: throws/doesn't-throw çiftleri, runtime-created fixture'lar, mesajda suçlu adlar.
- Check (d) için Automatic bölgenin `shiftId`'si: `ShiftZone._shiftId` + `_triggerMode` (isik-volume Complete; `internal` alanlar `BuildValidation`'a zaten görünür — `AssemblyInfo.cs` `InternalsVisibleTo("BuildValidation")`).
- **TR-anlati-009 mint'i**: `docs/architecture/tr-registry.yaml`'a EKLENİR (mevcut ID'ler asla yeniden numaralanmaz — yalnız append). Metin: "`\"ClueRegistry\"` Addressable anahtarının build-time çözümlenebilirliği (ADR-0007 Risks mitigasyonu; GDD'de karşılığı yok)".

## Out of Scope

- Orphaned `shiftId` uyarısı — non-blocking, ayrı mekanizma (Story 005)
- `ClueDefinition`/`ClueRegistry` tiplerinin kendisi (Story 002)
- Gerçek Addressables yüklemesi (Story 003 — bu story yalnız anahtarın ÇÖZÜLEBİLİRLİĞİNİ build-time doğrular)

## QA Test Cases

*(QL-STORY-READY üç lensle koştu. Testability GAPS→giderildi: check (a) için resolver dikişi zorunlu kılındı [aksi hâlde in-memory test imkânsız], check'lerin `ClueRegistry.Definitions` üzerinde çalışması AC'ye yazıldı, `OnValidate` yerine paylaşılan metot test birimi yapıldı, eksik TR-ID tespit edildi. Scope GAPS→giderildi: 4. check [AC22/TR-isik-021] eklendi — üç yerde kayıtlı bir devir, hiçbir story'ye atanmamıştı; tahmin M'ye çıkarıldı. Fidelity GAPS→giderildi: check (a)'nın AC8a/8b'ye ait olmadığı, ayrı ID gerektirdiği yazıldı.)*

- **AC-1 (otomatik)**: Boş RequiredShiftIds → hata
  - Given: `RequiredShiftIds` boş bir `ClueDefinition` taşıyan sahte `ClueRegistry`
  - When: check koşar
  - Then: `BuildFailedException`, mesajda asset adı
  - Edge cases: dolu liste → exception YOK

- **AC-2 (otomatik)**: Çift clueId → hata
  - Given: aynı `ClueId`'yi taşıyan iki `ClueDefinition`
  - When: check koşar
  - Then: `BuildFailedException`, mesaj İKİ kaydı da adlandırır
  - Edge cases: farklı ID'ler → exception YOK

- **AC-3 (otomatik)**: Addressable anahtarı çözülmezse hata
  - Given: `KeyResolves("ClueRegistry")` false dönen sahte resolver
  - When: check koşar
  - Then: `BuildFailedException`
  - Edge cases: true dönen resolver → exception YOK

- **AC-4 (otomatik)**: AC22 çapraz kontrolü
  - Given: `TriggerMode=Automatic` bir `ShiftZone` (shiftId = "amb-1") ve `requiredShiftIds = ["amb-1"]` olan bir `ClueDefinition`
  - When: check koşar
  - Then: `BuildFailedException`, mesaj hem bölgeyi hem clue'yu adlandırır
  - Edge cases: aynı clue `ManualOnly` bir bölgenin shiftId'sini isterse → exception YOK

- **AC-5 (otomatik)**: Kayıt ve kapsam
  - Given: `BuildValidationRegistry.Checks`
  - Then: dört anlati check'i de kayıtlı, fazları doğru; gerçek registry dizisi uçtan uca koşulduğunda ihlal yakalanır
  - Edge cases: check'ler `ClueRegistry.Definitions`'ı okur, `AssetDatabase.FindAssets`'i DEĞİL

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/anlati_build_checks_test.cs` (32 test)
**Status**: [x] Oluşturuldu ve geçiyor — **EditMode 222/222, PlayMode 84/84** (2026-08-10)

| AC | Test |
|----|------|
| AC-1 | `EmptyRequiredShiftIds_FailsBuild_AndNamesTheAsset`, `PopulatedRequiredShiftIds_DoesNotFailBuild`, `NullSlotInDefinitions_FailsBuild` |
| AC-2 | `DuplicateClueId_FailsBuild_AndNamesBothRecords`, `DistinctClueIds_DoNotFailBuild`, `BlankClueId_FailsBuild_BeforeDuplicateGrouping`, `ThreeRecordsSharingClueId_ReportTheFirstCollidingPair`, `ClueIdComparison_IsCaseSensitive_…`, `NullSlot_DoesNotCrashDuplicateCheck` |
| AC-3 | `UnresolvableKey_FailsBuild`, `ResolvableKey_DoesNotFailBuild`, `ResolvedKeyPointingAtNonRegistryAsset_FailsBuild`, `KeyCheck_AsksForTheCanonicalKey` + 8 saf seçim-kuralı testi |
| AC-4 | `AutomaticZone_WhoseShiftIdIsRequiredByAClue_FailsBuild`, `ManualOnlyZone_…_DoesNotFailBuild`, `AutomaticZone_NotRequiredByAnyClue_…`, `SecondAutomaticZone_Offending_StillFails`, `AllOffendingClues_AreNamedInOneMessage`, `BlankShiftIdAutomaticZone_IsSkipped`, `CrossCheck_IsCaseSensitive_…`, `CrossCheckViolation_IsAttributedToTheSceneItWasFoundIn` |
| AC-5 | `AllFourChecks_AreRegistered_WithTheirExpectedPhases`, `RegisteredAnlatiChecks_AreWiredToTheProductionSeams`, `ProductionSeams_AreAlive_NotJustSilent`, `RealRegistryArray_RunsClean_WithProductionSeams`, `AnlatiViolation_IsCaught_ThroughTheRealRunner` |
| AC-6 | `FindContentViolation_ReportsEmptyListBeforeDuplicateId`, `FindContentViolation_FallsThroughToDuplicateId_…`, `ClueRegistryOnValidate_WarnsOnViolation_…` |

---

## Completion Notes

### Story metnindeki İKİ bayat talimat (uygulanmadı, doğrulandı)

1. **Satır 15/42 "TR-anlati-009 bu story'de mint edilir"** — zaten `tr-registry.yaml:102`'de mevcut (created 2026-08-10). Yeniden eklemek dosyanın başındaki kalıcı-ID sözleşmesini ihlal ederdi.
2. **Satır 41 `ShiftZone._isPersistent`** — böyle bir alan YOK (`_shiftId`, `_triggerMode`, `_lights`, `_volume`, `_rTrigger`, `_kHysteresis`, `_autoTriggerConfig`, `_zoneCenter`; kalıcılık `IsShiftPersistent` + `ShiftConfig.Persistent`). Check (d) yalnız `_triggerMode` kullanıyor.

### Mimari kararlar (10 ajanlı tasarım paneli; üç jüri de aynı tasarımda birleşti)

- **Kayıt ADRES üzerinden bulunur** (`entry.address` → `entry.AssetPath` → `LoadAssetAtPath`), sabit yol sabitiyle DEĞİL. Addressable entry GUID'e bağlı: asset taşınsa adres sağ kalır ama sabit yol ölür — check (a) yeşil kalırken (b)(c)(d) sessizce fail-open olurdu.
- **İki ctor-enjekte seam**; statik test seam'i REDDEDİLDİ — üretim build kodunda global mutable olurdu ve EditMode testleri Editor domain'ini paylaştığı için, domain reload olmadan build alan bir geliştirici uydurma kayda karşı doğrulama yapardı (Story 003 zehirlenme sınıfı).
- **`Load()` null dönebilir** — "kayıt yok" ile "kayıt boş" ayrılmalı, yoksa üretim dikişi kalıcı olarak test edilemez (gerçek kayıt bugün BOŞ).
- **Check (d) düz SceneScan, aggregate DEĞİL** (∃-birleşim ⟺ ∃-sahne-başına; AC21 ayrışmıyor, AC22 ayrışıyor). Bedava kazanç: `context.ScenePath` dolu, mesaj suçlu sahneyi taşıyor.
- **(d) `IsikVolumeAutomaticPresenceCheck`'e KATLANMADI** — o check ilk Automatic bölgede `return` ediyor; katlansaydı ikinci ve sonraki bölgeler sessizce hiç incelenmezdi. isik dosyasındaki "bu eve eklenecek" notu düzeltildi.
- **`Debug.LogWarning`, `LogError` DEĞİL** — `ApplyModifiedPropertiesWithoutUndo()` `OnValidate`'i tetikliyor ve fixture'lar kasten ihlalli asset kuruyor; `LogError` mevcut yeşil testleri kırardı.
- **Dört kayıt** (üç değil), tek paylaşılan çekirdek; `BuildValidationRegistry.Checks` düz dizi literali kaldı.

### Gate bulguları

**LP + QL ortak ana bulgusu — BUILD-TIME FAIL-OPEN**: check (a) yalnız `KeyResolves` soruyordu. Adres çözülüp `ClueRegistry` OLMAYAN bir asset'e işaret ederse (a) geçiyor, (b)(c)(d) "kayıt yok" dalına girip sessizleşiyor, **build geçiyordu**. (a) iki adımlı yapıldı: anahtar çözülüyor mu + çözülen asset gerçekten `ClueRegistry` mi.

Diğerleri: (c)'nin case-sensitivity ve null-slot guard'ları pinlenmemişti; `FindContentViolation` sıfır kapsamdaydı; kayıtlı instance'ların üretim dikişine bağlı olduğunu hiçbir test kanıtlamıyordu (sessiz stub'a bağlı olsalar hem sessizlik testi hem tripwire geçerdi); `["   "]` boşluğu "kapsanmış gibi" okunuyordu.

### Adversarial doğrulama — İLK DÜZELTMEM YANLIŞTI

Gate düzeltmelerini 5 ajanlı bir adversarial pass'e soktum (Addressables 4.0.1 paket kaynağı okundu). Eklediğim `IncludeInBuild` kontrolü **iki blocking hata** taşıyordu:

1. **`IncludeInBuild` Addressables 4.0.0'da GRUBA taşınmış.** `BundledAssetGroupSchema.IncludeInBuild` artık saf forwarder (`Group == null || Group.IncludeInBuild`) — okuduğum ifade sıfır ek bilgi taşıyordu ve `Group` null'sa koşulsuz `true` dönerek kendi başına fail-open'dı. Artık `group.IncludeInBuild` doğrudan okunuyor.
2. **Asıl kapı `schema.IsEnabled`'dı ve hiç okunmuyordu.** Addressables'ın kendi build yolu her yerde `IsEnabled` + `IncludeInBuild` ikilisine bakıyor. Group Inspector'da schema'yı tek tıkla kapatmak yeterliydi: check yeşil, entry katalogda yok, `LoadAssetAsync` düşer, latch kalıcı — TR-anlati-009'un engellemek için var olduğu senaryonun aynısı.
3. **`IncludeAddressInCatalog` de aynı sınıf** — kapatılırsa adres editörde durur, katalogda yer almaz.
4. **`HasSchema<BundledAssetGroupSchema>()` ön koşulu yanlış-pozitif üretiyordu**: Content Directory grupları yalnız `ContentDirectoryGroupSchema` taşır ve meşru şekilde build edilir; oradaki bir entry, dördü de yanlış olan dört sebep sayan bir mesajla build'i bloklardı.
5. **Fix'lerin ayırt edici test kapsamı SIFIRDI** — proje tek grup + tek entry taşıdığı için `matches.Count != 1`'i `< 1` yapmak ya da ship filtresini tamamen silmek 214/214'ü yeşil bırakıyordu. Seçim kuralı saf bir `AddressableKeySelection.TrySelectUnique`'e (düz veri, editor tipi yok) çıkarıldı, 8 ayırt edici testle sabitlendi.
6. **`ClueRegistryOnValidate` testim yanlış log'la tatmin oluyordu** — `LogAssert.Expect` kesim noktası koymuyor, scope'un BAŞINDAN tarıyor; `ClueDefinition.OnValidate`'in uyarısı beklentiyi karşılıyordu ve `ClueRegistry.OnValidate` bloğu tamamen silinse test yeşil kalırdı. Fixture çift-clueId'ye çevrildi (o ihlal yalnız kayıt seviyesinde görülür).

### Bilinen açık sınırlar (bilinçli, kod değişikliği yapılmadı)

| # | Sınır | Sonuç |
|---|-------|-------|
| 1 | `FindZones()` yalnız AÇIK sahneyi görür — yalnız prefab içinde ya da `EditorBuildSettings.scenes`'te olmayan/disabled sahnedeki ihlal (d)'ye görünmez | Mevcut BEŞ sahne-scan check'i aynı sınırı paylaşıyor; kapatmak ayrı iş kalemi. Check doc'una + README'ye yazıldı |
| 2 | `ClueRegistry.asset` BOŞ ship ediliyor; dört kural da içeriğin ŞEKLİ hakkında, VARLIĞI hakkında değil | "İçerik hiç yazılmadı" sessizce ship olur. Presence check ayrı bir karar (içerik henüz yazılmadı) — iş kalemi |
| 3 | CI'da player-build job'ı YOK → `BuildValidationRunner.OnPreprocessBuild` CI'da HİÇ koşmuyor | Bloklayan kapı yalnız yerel/manuel build'de devreye giriyor. İş kalemi |
| 4 | `m_BuildAddressablesWithPlayerBuild: 0` = makineye özel `EditorPref` (varsayılan true, versiyon kontrolünde değil) | Kapalı bir makinede player build içerik build'i koşmaz; gate yeşil, runtime latch'ler. Belgelendi |
| 5 | `["A", "   "]` gibi içinde boş girdi olan liste check (b)'yi geçer ama ipucu asla tamamlanamaz | "Ulaşılamaz clue" sınıfı; Story 005'in bloklamayan uyarısına ait. Boşluğun BİLİNÇLİ olduğu testle pinlendi |
| 6 | Farklı TİPTE iki asset aynı adresi taşırsa runtime deterministik davranır ama check yine bloklar | Yön muhafazakâr; bir build kapısı için doğru olan bu. Doc'taki mutlak ifade yumuşatıldı |

## Dependencies

- Depends on: Story 002 (`ClueDefinition`/`ClueRegistry` tipleri), proje-kurulumu Story 006 (Complete — çatı), isik-volume Story 006 (Complete — check (d)'nin devri + `ShiftZone` alanları)
- Unlocks: içerik yazımı güvenli hâle gelir (vacuous-truth, çift ID, kırık anahtar ve rıza-atlatma yapısal olarak engellenir)
