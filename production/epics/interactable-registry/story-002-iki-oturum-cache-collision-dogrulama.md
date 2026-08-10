# Story 002: İki-oturum self-correction + cache-collision doğrulaması

> **Epic**: InteractableRegistry
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/etkilesim-sistemi.md` (registry Core Rules — kayıt yaşam döngüsü)
**Requirement**: `TR-etkilesim-002` (cross-session self-correction + cache-collision yarısı)
*(Requirement metni `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0004 (Validation Criteria bölümü — bu story'nin varlık nedeni)
**ADR Decision Summary**: `_live`, `OnEnable`/`OnDisable` üzerinden Reload Scene ayarından bağımsız kendi kendini düzeltir (Awake'e hiç güvenmez). Kare-snapshot cache'i, cross-session `Time.frameCount` çakışmasına karşı `ResetOnLoad()` gerektirir (unity-specialist BLOCKING bulgusu, Story 001'de implement edildi) — bu story o iki ampirik iddianın gerçek PlayMode kanıtıdır, yalnız mimari muhakeme değil.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `Time.frameCount` gerçek Editör Enter Play Mode Settings'i (Reload Scene/Domain) değiştirmeden, Unity'nin `Awake`/`OnEnable`/`OnDisable` garantilerinin gerçek davranışını kullanarak proxy'lenir — bu projenin `gece_oturum_two_session_test.cs`'de zaten kurduğu emsalle aynı idiyom (doğrudan `ResetOnLoad()` çağrısı ile oturum sınırı simülasyonu).

**Control Manifest Rules (bu katman)**:
- Required: iki-oturum Editor testleri her statik facade/kalıcı-sahne singleton'ı için ZORUNLU — `[UnityTest]`, iki simüle oturum, tam-bir-kez event/durum doğrulaması
- Forbidden: gerçek Enter Play Mode Settings'i test-zamanında değiştirmeye çalışmak (desteklenmez) — bunun yerine Unity'nin gerçek yaşam döngüsü garantilerini (SetActive toggle) kullan
- Guardrail: bu test yalnız `InteractableRegistry.ResetOnLoad()`'un KENDİSİNİN doğruluğunu kanıtlar

## Acceptance Criteria

- [x] `[UnityTest]`, amaca özel bir test-double `MonoBehaviour` (`Awake()`'te artan bir `AwokeCount`; `OnEnable`'da `Register(this)`, `OnDisable`'da `Deregister(this)`) ile OnEnable/OnDisable self-correction'ı GERÇEKTEN kanıtlar: nesne aktif→`SetActive(false)` (oturum-1 teardown'unu simüle eder, `OnDisable` fırlar)→`InteractableRegistry.ResetOnLoad()` (oturum sınırı cache temizliği)→`SetActive(true)` (Reload-Scene-off'ta hayatta kalan bir nesnenin oturum-2 başlangıcını simüle eder) sonrasında: `AwokeCount` HÂLÂ `1`'dir (Awake yeniden ATEŞLENMEDİ — belgelenen Reload-Scene-off boşluğu sadakatle yeniden üretilir) VE nesne `Snapshot()`'ta tekrar mevcuttur (OnEnable onu yeniden kaydetti)
- [x] Aynı test, `SetActive(false)` durumundayken nesnenin `Snapshot()`'tan GERÇEKTEN kaybolduğunu da assert eder (yalnız "sonunda düzelir" değil, `OnDisable`→`Deregister`'ın gerçekten fırladığının kanıtı)
- [x] `[UnityTest]`, cross-session cache-collision hasarını NEGATİF KONTROLLÜ olarak yeniden üretir: gerçek geçerli `Time.frameCount`'u oku (`staleFrame`); zehirli bir array + `staleFrame`'i `_frameSnapshot`/`_snapshotFrame`'e doğrudan yaz (Story 001'in `internal` yaptığı alanlar); **reset ÖNCESİ** `Snapshot()` çağır ve zehirli array'in (gerçek `_live` içeriği DEĞİL) döndüğünü assert et (bug'ın gerçek olduğunun kanıtı); SONRA `ResetOnLoad()` çağır; SONRA `Snapshot()` tekrar çağır ve şimdi GERÇEK `_live` içeriğinin (zehirli array değil) döndüğünü assert et (düzeltmenin bug'ı kapattığının kanıtı)
- [x] Her iki test de gerçek bir Unity PlayMode koşusu altında geçer (yalnız mimari muhakeme değil) — ADR-0004'ün unity-specialist BLOCKING bulgusunu ampirik olarak kapatır

## Implementation Notes

- Story 001'in `_frameSnapshot`/`_snapshotFrame` alanlarının `internal` olmasını GEREKTİRİR (Story 001 Implementation Notes'taki bilinçli ADR-sapması) — bu alanlar `private` kalırsa bu story implemente EDİLEMEZ.
- Bu test yalnız `InteractableRegistry.ResetOnLoad()`'un KENDİSİNİN doğruluğunu kanıtlar; `ResetOnLoad()`'un her GERÇEK oturum sınırında gerçekten ÇAĞRILDIĞINI kanıtlamaz — o yarı ayrıca kapalı: Story 001 AC-6 (`FoundationBootstrapOrderTest`, satırın sırada var olduğunu kanıtlar) + zaten var olan `FoundationBootstrapTimingTest` (servis-agnostik, `ResetAll()`'un oturum başına tam bir kez ve herhangi bir sahne nesnesinin `Awake()`'inden ÖNCE koştuğunu kanıtlar — Story 001 `_resetSequence` satırını aktif ettiği anda otomatik genişler, yeni test gerekmez). Bu, `gece_oturum_two_session_test.cs`'in aynı iş bölümünü nasıl yaptığının birebir yansımasıdır.
- Test-double `MonoBehaviour` deseni, `foundation_bootstrap_timing_test.cs`'deki mevcut sayaç-probe emsalini takip eder.
- Poison-then-reset dizisini art arda iki kez koşturarak çift-reset güvenliğini de dolaylı doğrula (edge case, ayrı AC gerekmez).

## Out of Scope

- Elevator sahne-swap sırasındaki mid-frame async-unload yarışı (ADR Risks'te düşük-önem-pratikte olarak zaten muhakeme edilmiş, ADR'ın Validation Criteria'sında YOK — test gerekmez)
- Gerçek Editor Enter Play Mode Settings (Reload Scene/Domain) değiştirme altyapısı — bu test Unity'nin gerçek `SetActive()` yaşam döngüsü garantilerini sadakatle proxy olarak kullanır, literal bir ayar-değiştirme test altyapısı değil

## QA Test Cases

*(QL-STORY-READY full modda koştu; AC'ler gate'in iki ana bulgusunu (AC1'in totolojik olması, AC2'nin negatif kontrol eksikliği) kapatacak şekilde revize edildi.)*

- **AC-1/2**: OnEnable/OnDisable self-correction, Awake YENİDEN ATEŞLENMEZ
  - Given: `TestInteractableProbe : MonoBehaviour, IInteractable` — `public int AwokeCount` (`Awake()`'te ++), `OnEnable`→`Register(this)`, `OnDisable`→`Deregister(this)`
  - When: instantiate + aktif et (oturum 1: `AwokeCount==1`, `Snapshot()`'ta mevcut); `SetActive(false)` (oturum-1 teardown, `OnDisable` fırlar); `InteractableRegistry.ResetOnLoad()`; `SetActive(true)` (oturum-2 başlangıcı)
  - Then: `AwokeCount` hâlâ `1` (Awake yeniden koşmadı) VE probe `Snapshot()`'ta tekrar mevcut (OnEnable yeniden kaydetti)
  - Edge cases: `SetActive(false)` sırasında probe `Snapshot()`'tan GERÇEKTEN kaybolmalı (yalnız "sonunda düzelir" yeterli değil — OnDisable'ın fiilen fırladığının kanıtı)

- **AC-3**: Cross-session cache-collision, negatif kontrollü red-then-green
  - Given: 2 kayıtlı sahte nesne (oturum-2'nin gerçek `_live` içeriği); zehirli bir array (içinde `_live`'da OLMAYAN bir marker nesne); gerçek geçerli `Time.frameCount` bir kez okunur (`staleFrame`)
  - When (zehirle): `InteractableRegistry._frameSnapshot = poisonedArray`, `_snapshotFrame = staleFrame` doğrudan atanır
  - Then (negatif kontrol — bug gerçek): reset ÖNCESİ `Snapshot()` çağrılır, zehirli array/marker nesne döner — GERÇEK 2 kayıtlı nesne DEĞİL
  - When (düzelt): `ResetOnLoad()` çağrılır
  - Then (pozitif — düzeltme kapatıyor): `Snapshot()` tekrar çağrılır, YENİ bir array döner, tam olarak gerçek 2 kayıtlı nesneyi içerir, marker nesne YOK
  - Edge cases: poison→reset dizisi art arda iki kez (çift-reset güvenliği); test yorum bloğu bu testin yalnız `ResetOnLoad()`'un kendi doğruluğunu kanıtladığını, "her gerçek oturum sınırında çağrıldığı" yarısının Story 001 AC-6 + `FoundationBootstrapTimingTest`'e ait olduğunu belirtir

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/interactable_registry_session_test.cs`
**Status**: [x] Created — 2 UnityTest, PlayMode'da kendi testleri 2/2 temiz (2026-08-10)

## Dependencies

- Depends on: Story 001 (ŞART — `_frameSnapshot`/`_snapshotFrame`'in `internal` olması bu story'nin ön koşulu)
- Unlocks: Epic'in Definition of Done'ı (ADR-0004 Validation Criteria testlerinin tamamı) — epic tamamlanır

## Completion Notes
**Completed**: 2026-08-10 — **EPİC 2/2 TAMAM**
**Criteria**: 3/3 passing. Kendi test dosyasının 2 testi her koşuda temiz.
**Deviations**: None. LP bulgusuyla kapanış öncesi düzeltildi: Test 1'e açık `InteractableRegistry.Deregister(probe)` eklendi (Object.Destroy'un ertelenmiş OnDisable'ına tek başına güvenmek yerine, Test 2'nin kendi savunmacı deseniyle tutarlı — registry'nin ResetOnLoad'u `_live`'a bilerek dokunmadığından iki test paylaşımlı sınıfta potansiyel kırılganlık). Çift-reset edge case'i story metnine ("poison→reset dizisi art arda iki kez") uygun şekilde gerçek bir ikinci poison→reset çiftine genişletildi.
**Ortam gözlemi (bu story'yi bloklamıyor)**: Story 001'in push'undaki CI koşusu GitHub'ın Linux runner'ında da isik-volume'un aynı önceden-bilinen flake'ini üretti — sorunun tek makineye özgü değil, ortam-bağımsız gerçek bir test-tasarımı meselesi olduğunu güçlendiren üçüncü bağımsız kanıt (Windows lokal ×2, Linux CI ×1, hepsi farklı testlerde). `task_d5aee2cb`'ye eklendi.
**Test Evidence**: Integration — `game/Assets/Tests/PlayMode/interactable_registry_session_test.cs` (2 UnityTest)
**Code Review**: Complete — LP-CODE-REVIEW: CONCERNS→giderildi, QL-TEST-COVERAGE: ADEQUATE (full mod, general-purpose subagent gate'leri)
