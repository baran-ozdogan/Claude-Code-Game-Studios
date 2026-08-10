# Story 005: Orphaned shiftId uyarısı (build-time aggregate, non-blocking)

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

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

- [ ] Kontrol `IBuildCheck` + `IBuildCheckAggregate` implement eder, `SceneScan` fazında, `BuildValidationRegistry.Checks`'e kayıtlı (ikinci `IPreprocessBuildWithReport` YOK)
- [ ] `BeginWalk()` biriken gözlemi yürüyüşün BAŞINDA sıfırlar (sonda self-reset DEĞİL — aborted-walk sızıntısı, isik-volume Story 006'nın LP bulgusu)
- [ ] `Run(context)` her sahnede o sahnenin `ShiftZone`'larının `_shiftId`'lerini biriktirir (inaktif objeler dâhil — build içeriği hepsi)
- [ ] `FinalizeWalk(context)` yürüyüş sonunda TEK SEFERDE değerlendirir: `ClueRegistry.Definitions`'taki her `requiredShiftIds` girdisi, birleştirilmiş `shiftId` kümesinde YOKSA o `(clueId, shiftId)` çifti orphaned sayılır
- [ ] Her orphaned çift için bir `Debug.LogWarning` basılır ve çift `GetOrphanedClueIds()` üzerinden okunabilir; **build ENGELLENMEZ** (`context.Fail` ASLA çağrılmaz) — bu bir content-authoring uyarısıdır, çalışma zamanı davranışını bozmaz (GDD AC8)
- [ ] **Yanlış-pozitif yok**: bir clue'nun `requiredShiftIds`'i FARKLI sahnelerdeki iki `shiftId` içeriyorsa ve ikisi de Build Settings'teki herhangi bir sahnede mevcutsa, uyarı ÜRETİLMEZ (mekanizma sapmasının var oluş sebebi — tek-sahne bir kontrol burada yanlış uyarırdı)
- [ ] Sıfır sahne yürüyüşünde (henüz seviye sahnesi yok) sessiz kalır — hiçbir `ClueDefinition` yoksa da sessiz; "hiç yok" da geçerli bir toplam sonuçtur
- [ ] Test şekli: `ValidateScene`/`FinalizeWalk` mantığı DOĞRUDAN test edilir; `[InitializeOnLoad]`/runner kancası ince wiring'dir ve ayrıca reflection'la test edilmez

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

**Story Type**: Logic → `game/Assets/Tests/EditMode/anlati_orphaned_clue_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 002 (`ClueDefinition`/`ClueRegistry` tipleri), isik-volume Story 006 (Complete — `IBuildCheckAggregate` çatısı + `ShiftZone._shiftId`)
- Unlocks: içerik yazımında "sonsuza kadar tamamlanamaz" ipuçları erken yakalanır
