# Story 006: Build-blocking doğrulamalar

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/isik-volume-durum-sistemi.md`
**Requirement**: `TR-isik-016`, `TR-isik-020`, `TR-isik-021` (+GDD AC20/21)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0005 (check listesi — primary); ADR-0014 (paylaşılan utility mekanizması) secondary
**ADR Decision Summary**: Dört build-blocking sahne-scan check'i `BuildValidationRegistry.Checks`'e KAYDedilir (asla ikinci bağımsız IPreprocessBuildWithReport): (1) `ZoneLight` dizisindeki her Light Mode=Mixed (Baked → hata); (2) iki bölge aynı Light'ı paylaşamaz; (3) Volume-trigger-box overlap kontrolü (paylaşılan profil over-lerp riski — ADR-0005 BLOCKING bulgusu); (4) sahnelerde en az bir `TriggerMode=Automatic` bölge (AC21).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: SceneScan fazı — pahalı adım, yalnız bu check'ler için (Story 006 çatı guardrail'i). Fixture'lar runtime-created; on-disk test sahnesi/asset'i YASAK (FindAssets tuzağı).

**Control Manifest Rules (bu katman)**:
- Required: check'ler `IBuildCheck` implementasyonu + registry satırı; pointed hata mesajları (offending zone/light/sahne adlı)
- Forbidden: dördüncü bağımsız IPreprocessBuildWithReport; runtime clamp'e düşmek
- Guardrail: box-overlap O(n²) ama n küçük — sahne başına kabul edilebilir

## Acceptance Criteria

- [ ] `IsikVolumeBuildChecks` (SceneScan fazı): 4 check registry'ye kayıtlı
- [ ] Baked-mode Light referansı → `BuildFailedException`, mesajda ışık + bölge + sahne adı
- [ ] İki bölgenin `ZoneLight` dizilerinde aynı `Light` → hata, iki bölge adıyla
- [ ] İki bölgenin box collider'ları kesişiyor → hata (paylaşılan-profil over-lerp gerekçesi mesajda)
- [ ] Taranan sahnelerde hiç `TriggerMode=Automatic` bölge yoksa → hata (AC21)
- [ ] Test harness'ta throws/doesn't-throw çiftleri — sahte sahne içeriği runtime-created

## Implementation Notes

- AC22'nin `ClueDefinition.requiredShiftIds` çapraz kontrolü BİLİNÇLİ ertelendi — `ClueDefinition` tipi anlati epic'inde doğar; o epic bu check'i buradaki aynı eve ekler (registry TODO satırı güncellenir).
- `StingerAudioRadius > 0` zorunluluğu memory-trigger bölgeleri için ani-tetikleyici epic'inin check'i (MemoryTriggerDef eşlemesi gerekli) — burada değil.

## Out of Scope

- AC22 (anlati epic'i); MemoryTriggerDef-bağlı kontroller (ani-tetikleyici epic'i)

## QA Test Cases

*(QL-STORY-READY atlandı.)*

- Her check için: ihlalli sahte kurulum → `BuildFailedException` + mesaj içeriği (offending adlar); temiz kurulum → sessiz
- Sıfır bölgeli sahne → yalnız Automatic-varlık check'i patlar (diğerleri sessiz)

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/isik_volume_build_checks_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 003 (`ShiftZone`/`ZoneLight` tipleri); proje-kurulumu Story 006 (çatı — DONE)
- Unlocks: level-design içerik yazımı güvenli hale gelir
