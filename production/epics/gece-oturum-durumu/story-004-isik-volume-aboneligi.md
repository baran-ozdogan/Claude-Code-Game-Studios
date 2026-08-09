# Story 004: Işık/Volume aboneliği (gerçek wiring)

> **Epic**: Gece/Oturum Durumu
> **Status**: Ready — ⚠ cross-epic dependency (aşağıya bakın)
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/quick-specs/gece-oturum-durumu-2026-08-02.md`
**Requirement**: `TR-oturum-004`, `TR-oturum-005` (wiring yarısı)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0006 (primary); ADR-0015 (in-place rejim + once-per-process abonelik), ADR-0001 (reset sırası) secondary
**ADR Decision Summary**: `GeceOturumDurumu`, Işık/Volume'un `OnShiftStateChanged`'ine constructor/static-init zamanında BİR KEZ abone olur; abonelik process ömrü boyunca yaşar, her `ResetAll()`'ı atlatır (iki taraf da in-place — re-wire yok, accumulation yok). Reset sırası: Işık/Volume ÖNCE (`FoundationBootstrap` belgeli sırası).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: —

**Control Manifest Rules (bu katman)**:
- Required: constructor-time abonelik once-per-process, never-replaced instance'larda; `X.Instance` kullanım noktasında canlı dereference
- Forbidden: `ResetOnLoad()` içinde re-wire; aboneliği kaldırıp yeniden bağlama
- Guardrail: handler sıcak yolu Story 003'teki gibi allocation'sız

## Acceptance Criteria

- [ ] `GeceOturumDurumuState`'in `IsShiftPersistent` delegate'i gerçek `IsikVolumeDurumSistemi` sorgusuna bağlandı; `OnShiftStateChanged` aboneliği static-init'te tam bir kez kuruluyor
- [ ] `FoundationBootstrap._resetSequence`'ta `IsikVolumeDurumSistemi` satırı `GeceOturumDurumu`'dan ÖNCE (sıra testi güncel)
- [ ] İki-oturum taze-abonelik PlayMode testi (ADR-0001 Validation Criteria): oturum 1'de Persistent shift işlenir; `ResetAll` simülasyonu; oturum 2'de yeni bir Shifting-In event'i handler'a ULAŞIR (abonelik kopmamış) ve oturum 1'in verisi görünmez
- [ ] Abonelik accumulation testi: iki `ResetOnLoad()` sonrası tek event tek handler çağrısı üretir (çift işleme yok)

## Implementation Notes

- Bu story Işık/Volume epic'inin en az "facade + OnShiftStateChanged + IsShiftPersistent" story'sinin Complete olmasını bekler — başlamadan `/story-readiness` koş.
- Story 003'ün handler'ı değişmez; yalnız bağlama katmanı eklenir.

## Out of Scope

- Işık/Volume'un event'i fırlatma zamanlaması/rampası (o epic'te)
- Sahne Kesmeli Anlatı'nın `OnTriggerSettled` tüketimi (sahne-kesme epic'i)

## QA Test Cases

*(QL-STORY-READY atlandı — qa-lead subagent'ı mevcut değil.)*

- **AC-3 (UnityTest)**: taze-abonelik iki-oturum
  - Given: Reload Domain OFF profili davranışını simüle eden ResetAll sınırı
  - When: oturum 1: Fired+Held akışı; sınır; oturum 2: yeni Shifting-In
  - Then: oturum 2 event'i işlenir; `HasFired/HasSettled` oturum 1 id'leri için false
  - Edge cases: sınırdan hemen sonraki ilk event kaybolmaz
- **AC-4 (UnityTest)**: accumulation — ×2 reset → 1 event = 1 yazım

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/gece_oturum_subscription_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 003; **isik-volume-durum-sistemi epic'i — facade/event story'si (henüz yazılmadı)**
- Unlocks: sahne-kesmeli-anlati epic'inin doygunluk story'si; isik-volume Persistent-restore story'si
