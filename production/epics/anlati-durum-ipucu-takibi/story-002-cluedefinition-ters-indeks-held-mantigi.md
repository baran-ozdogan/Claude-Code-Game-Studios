# Story 002: ClueDefinition/ClueRegistry + ters indeks + Held handler mantığı

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/anlati-durum-ipucu-takibi.md` (Core Rules — shiftId→clueId N:1 ALL-semantiği, iç takip; Interactions — Işık/Volume aboneliği handler tarifi; Edge Cases — paylaşılan shiftId, yinelenen shiftId)
**Requirement**: `TR-anlati-002`, `TR-anlati-005` (mantık yarısı — wiring yarısı Story 003)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0007 (primary — Data model + Alternative 1'in ters-indeks kararı)
**ADR Decision Summary**: `ClueDefinition` ve `ClueRegistry` iki ayrı `[CreateAssetMenu]` ScriptableObject; `ClueRegistry.Definitions` Inspector-doldurulan `List<ClueDefinition>`. Ters indeks (`Dictionary<string, List<ClueDefinition>>`, shiftId → onu `requiredShiftIds`'inde listeleyen HER tanım) bilinçli tercih (Alternative 1: per-event lineer tarama reddedildi — indeks neredeyse bedava ve GDD'nin 15-20 tetikleyicilik Full Vision ölçeğini yapısal değişiklik olmadan karşılıyor). Tamamlanma testi `_seenShiftIds.IsSupersetOf(def.RequiredShiftIds)` — ALL semantiği.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `ScriptableObject` uzun süredir stabil, post-cutoff API yok. Test fixture'ları **`ScriptableObject.CreateInstance` ile runtime-created** olmalı — on-disk `ClueDefinition`/`ClueRegistry` test asset'i YASAK (projenin `AssetDatabase.FindAssets` taramalarını kirletir; isik-volume Story 006 / fpc Story 006 emsali).

**Control Manifest Rules (bu katman)**:
- Required: `ScriptableObject` = yalnız authored config; runtime state her zaman ayrı nesne
- Forbidden: `ScriptableObject`-backed runtime state (registry ASLA çalışma zamanında mutasyona uğratılmaz)
- Guardrail: ters indeks bir kez kurulur; Held başına O(1) sözlük araması + O(requiredShiftIds.Count) superset kontrolü

## Acceptance Criteria

- [ ] `ClueDefinition` ScriptableObject: `ClueId` (string), `RequiredShiftIds` (`List<string>`); `ClueRegistry` ScriptableObject: `Definitions` (`List<ClueDefinition>`) — ikisi de `[CreateAssetMenu]` (TR-anlati-002)
- [ ] `BuildReverseIndex(IReadOnlyList<ClueDefinition>)` → `Dictionary<string, List<ClueDefinition>>`: her `shiftId`, onu `RequiredShiftIds`'inde listeleyen TÜM tanımlara eşlenir (N:1 destekli)
- [ ] **ALL-semantiği, sıra bağımsız**: `requiredShiftIds = [A, B]` olan bir tanım için yalnız A Held'e ulaşınca `IsClueKnown` `false` ve `OnClueKnown` FIRLAMAZ; sonra B Held'e ulaşınca `IsClueKnown` `true` ve `OnClueKnown` TAM BİR KEZ fırlar — B-önce-A sırası da AYNI sonucu verir (GDD AC1+AC2, tek ilerleyen senaryo olarak yazılır; AC1 tek başına no-op bir handler'ı da geçirirdi)
- [ ] **Yalnız `ShiftState.Held` işlenir**: Shifting-In/Shifting-Out/Dormant geçişleri `_seenShiftIds`'e HİÇBİR giriş eklemez ve hiçbir tamamlanma kontrolü çalıştırmaz (GDD AC4, TR-anlati-005 mantık yarısı). *Test şekli davranışsal olmalı* (`_seenShiftIds` private): iki-shift'li bir tanımda A için Shifting-In gönder → B için Held gönder → hâlâ Known DEĞİL; sonra A için Held → şimdi Known. Bu tek senaryo hem "yalnız Held sayılır" hem "önceki Shifting-In sızmadı" iddiasını kanıtlar
- [ ] **Paylaşılan shiftId, bağımsız değerlendirme**: iki farklı `ClueDefinition` aynı `shiftId`'yi listeliyorsa tek bir Held geçişi sıfır, bir ya da İKİSİNİ birden tamamlayabilir — her tanım bağımsız değerlendirilir (GDD AC10; ilk eşleşmede erken-return eden bir hata AC1/AC2'yi geçer ama bunu geçemez)
- [ ] **Yinelenen `shiftId` zararsız**: `requiredShiftIds = [A, A, B]`, `[A, B]` ile birebir aynı davranır — `HashSet` kapsaması yinelenenleri tekilleştirir (GDD Edge Cases)
- [ ] **Test dikişi (ZORUNLU)**: Held mantığı, Addressables'a DOKUNMAYAN, ters indeksin zaten dolu olduğunu varsayan ayrı bir metotta yaşar (ör. `ProcessHeldShift(string shiftId)`); ters indeks testlere doğrudan enjekte edilebilir. Bu story'nin hiçbir testi `EnsureRegistryLoaded()`'ı ya da Addressables'ı çağırmaz

## Implementation Notes

- **KRİTİK — story sınırı**: ADR-0007'nin kod bloğunda `OnShiftStateChanged` metodu `EnsureRegistryLoaded()` (Addressables) ile BAŞLAR ve ardından Held-filtresi/superset/`MarkClueKnown` mantığını yapar; yani ADR tek metotta iki story'nin kapsamını birleştiriyor. Bu story o metodu OLDUĞU GİBİ YAZMAZ: mantık, ters indeksin dolu olduğunu varsayan saf bir metoda (`ProcessHeldShift`) çıkarılır. Story 003 `OnShiftStateChanged`'i ince bir sarmalayıcı olarak ekler (Held filtresi → `EnsureRegistryLoaded()` → bu metodu çağır). **Bu diki\ş olmadan bu story bir Logic story olarak kapanamaz** — testleri gerçek bir Addressables çözümlemesi tetiklerdi (`coding-standards.md` Testing Standards: unit testler dış API çağırmaz). Emsal: `gece-oturum-durumu` Story 003, `ProcessShiftStateChanged`'i tam bu şekilde saf/enjekte-edilebilir tuttu, Story 004 yalnız `+=` satırını ekledi.
- `MarkClueKnown` Story 001'den TÜKETİLİR, yeniden yazılmaz.
- Fixture'lar: `ScriptableObject.CreateInstance<ClueDefinition>()` — on-disk asset YOK.

## Out of Scope

- Addressables yüklemesi (`EnsureRegistryLoaded`) ve gerçek `OnShiftStateChanged` aboneliği (Story 003)
- `ClueDefinition` içeriğinin edit-time doğrulaması (Story 004/005)
- Facade/idempotency/sorgu yüzeyi (Story 001)

## QA Test Cases

*(QL-STORY-READY üç lensle koştu. Testability: ADEQUATE [AC1'in tek başına zayıflığı ve AC12b'nin AC2 ile fazlalığı not edildi, AC4'ün davranışsal test şekli somutlaştırıldı]. Scope: GAPS→giderildi [ProcessHeldShift dikişi zorunlu kılındı, tahmin M'ye çıkarıldı]. Fidelity: GAPS→giderildi [aynı dikiş, + yinelenen-shiftId kenar durumu AC'ye eklendi].)*

- **AC-1 (otomatik)**: Ters indeks N:1
  - Given: iki tanım, biri `[A]`, diğeri `[A, B]`
  - When: `BuildReverseIndex` çalışır
  - Then: `A` anahtarı İKİ tanımı da taşır; `B` anahtarı yalnız ikincisini

- **AC-2 (otomatik)**: ALL-semantiği, sıra bağımsız (tek ilerleyen senaryo)
  - Given: `requiredShiftIds = [A, B]`
  - When: yalnız A Held
  - Then: `IsClueKnown` false, `OnClueKnown` fırlamadı
  - When: sonra B Held
  - Then: `IsClueKnown` true, `OnClueKnown` tam bir kez
  - Edge cases: B-önce-A sırası aynı sonucu verir

- **AC-3 (otomatik)**: Yalnız Held işlenir
  - Given: `requiredShiftIds = [A, B]`
  - When: A için Shifting-In, sonra B için Held gönderilir
  - Then: hâlâ Known DEĞİL (Shifting-In sızmadı)
  - When: A için Held gönderilir
  - Then: Known
  - Edge cases: Dormant ve Shifting-Out da hiçbir etki üretmez

- **AC-4 (otomatik)**: Paylaşılan shiftId
  - Given: iki tanım, ikisi de `[A]` listeliyor
  - When: A Held'e ulaşır
  - Then: İKİ clue da Known, iki ayrı `OnClueKnown` fırladı
  - Edge cases: biri `[A]` diğeri `[A, B]` ise yalnız birincisi tamamlanır

- **AC-5 (otomatik)**: Yinelenen shiftId zararsız
  - Given: `requiredShiftIds = [A, A, B]`
  - When: A ve B Held'e ulaşır
  - Then: `[A, B]` ile birebir aynı davranış

- **AC-6 (otomatik)**: Addressables'a dokunulmadı
  - Given: bu story'nin tüm testleri
  - Then: hiçbiri `EnsureRegistryLoaded`/Addressables çağırmaz — ters indeks doğrudan enjekte edilir

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/anlati_clue_definition_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 001 (`MarkClueKnown`/`_seenShiftIds` çekirdeği)
- Unlocks: Story 003 (bu mantığı sarmalar), Story 004 (bu tipleri doğrular), Story 005 (bu tipleri okur)
