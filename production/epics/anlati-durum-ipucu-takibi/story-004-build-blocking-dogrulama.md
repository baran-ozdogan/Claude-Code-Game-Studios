# Story 004: Build-blocking doğrulama dörtlüsü

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

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

- [ ] **Check (b) — boş `RequiredShiftIds`**: `ClueRegistry.Definitions`'taki herhangi bir `ClueDefinition`'ın `RequiredShiftIds`'i boşsa `BuildFailedException`, mesaj suçlu asset'i adlandırır (GDD AC8a). Gerekçe: `SeenShiftIds ⊇ ∅` her zaman doğru olduğundan boş liste, sistem hiç başlatılmadan "Known" sayılırdı — sessiz vacuous-truth hatası
- [ ] **Check (c) — çift `clueId`**: iki farklı `ClueDefinition` aynı `ClueId`'yi taşıyorsa `BuildFailedException`, mesaj **çakışan İKİ kaydı da** işaret eder (GDD AC8b)
- [ ] **Check (a) — `"ClueRegistry"` Addressable anahtarı çözülür**: anahtar `AddressableAssetSettings` üzerinden (runtime yükleme DEĞİL) çözülemezse `BuildFailedException` (ADR-0007 Risks mitigasyonu + Validation Criteria; GDD'de karşılığı YOK — bu yüzden `TR-anlati-009` mint edilir). **Test dikişi ZORUNLU**: check, `AddressableAssetSettingsDefaultObject.Settings`'i doğrudan ÇAĞIRMAZ; `internal interface IAddressableKeyResolver { bool KeyResolves(string key); }` üzerinden çağırır (üretimde gerçek settings'i sarmalar, testte iki satırlık sahte). `IBuildSceneWalker` ile aynı şekil — böylece test in-memory kalır ve "AddressableAssetSettings in-memory fabrikeedilebilir mi" belirsizliği tamamen atlanır
- [ ] **Check (d) — AC22 çapraz kontrolü (isik-volume epic'inden DEVRALINDI, TR-isik-021)**: zorunlu `TriggerMode=Automatic` bölgenin `shiftId`'si hiçbir `ClueDefinition.requiredShiftIds`'inde yer ALMAMALI; alırsa `BuildFailedException`. Gerekçe: pasif/ambiyans bir bölge, ipucu kazanımının `ManualOnly` rıza ön koşulunu sessizce atlatırdı. *Bu kontrol isik-volume Story 006'da bilinçli ertelendi (`ClueDefinition` tipi bu epic'te doğduğu için) — `BuildValidationRegistry` TODO satırı ve o story'nin Completion Notes'u bunu kayıt altına almış*
- [ ] Dört kontrol de paylaşılan `BuildValidationRegistry.Checks` dizisine kaydedilir; ikinci bir `IPreprocessBuildWithReport` YAZILMAZ; registry'nin `TODO(epic:anlati-durum-ipucu-takibi)` satırı gerçek kayda dönüşür ve `BuildValidation/README.md`'nin kayıtlı-check tablosu güncellenir
- [ ] **Kapsam kilidi**: check'ler `ClueRegistry.Definitions` üzerinde çalışır, `AssetDatabase.FindAssets<ClueDefinition>` üzerinde DEĞİL. (ADR'ın kendi ifadesi: "her `ClueDefinition` referenced by `ClueRegistry.Definitions`". `FindAssets`'e kaymak — AssetScan fazının adı bunu davet ediyor — testleri in-memory fixture'larla imkânsız kılar ve README'nin uyardığı on-disk-test-verisi tuzağına düşer)
- [ ] `OnValidate()`, check (b) ve (c)'nin PAYLAŞILAN doğrulama metodunu çağırır (Inspector-time geri bildirim). Test edilen birim o paylaşılan metottur; `OnValidate()`/`IBuildCheck.Run()` ince çağrı yerleridir ve reflection-invoke jimnastiğiyle ayrıca test EDİLMEZ

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

**Story Type**: Logic → `game/Assets/Tests/EditMode/anlati_build_checks_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 002 (`ClueDefinition`/`ClueRegistry` tipleri), proje-kurulumu Story 006 (Complete — çatı), isik-volume Story 006 (Complete — check (d)'nin devri + `ShiftZone` alanları)
- Unlocks: içerik yazımı güvenli hâle gelir (vacuous-truth, çift ID, kırık anahtar ve rıza-atlatma yapısal olarak engellenir)
