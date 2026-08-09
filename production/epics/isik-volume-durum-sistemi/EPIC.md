# Epic: Işık/Volume Durum Sistemi

> **Layer**: Foundation
> **GDD**: design/gdd/isik-volume-durum-sistemi.md
> **Architecture Module**: Işık/Volume Durum Sistemi (`ShiftZone` + `IIsikVolumeState` facade)
> **Governing ADRs**: ADR-0005 (+2026-08-09 facade addendum)
> **Engine Risk**: LOW — domain HIGH'ı (URP RenderGraph) "custom pass yok" kararıyla yapısal olarak devre dışı; mekanizma prototiple ampirik doğrulanmış (`prototypes/yankilar-volume-weight-spike/`)
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 6 stories (2026-08-09)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Facade sözleşmesi + ShiftConfig (addendum) | Logic | Ready | ADR-0005 addendum (+0001/0015) |
| 002 | Shift progress çekirdeği + guard rail'ler (saf) | Logic | Ready | ADR-0005 |
| 003 | ShiftZone + ticker + lockstep | Integration | Ready | ADR-0005 |
| 004 | Automatic izleme + histerezis + co-residency + OnDestroy | Integration | Ready | ADR-0005 |
| 005 | Persistent semantiği + reload restore | Integration | Ready | ADR-0005 (+0006) |
| 006 | Build-blocking doğrulamalar | Logic | Ready | ADR-0005 (+0014) |

Bağımlılık: 001 → 002 → 003 → {004 → 005, 006}. **001 biter bitmez gece-oturum Story 004'ün kilidi açılır.** AC22 çapraz kontrolü anlati epic'ine ertelendi (ClueDefinition tipi orada doğar).

## Overview

Oyunun görsel çekirdeği: bölge başına `ShiftZone` MonoBehaviour'ı (lokal Volume + paylaşılan VolumeProfile, `blendDistance=0`, Inspector-atanmış ışık dizisi), per-zone ticker coroutine (`ShiftProgress = 3x²−2x³`, kesintiye yön-flip'le devam), `Dormant→Shifting-In→Held→Shifting-Out` durum makinesi, `OnDestroy` tamamlama garantisi, Automatic bölgelerin Dormant-iken pozisyon-izleme coroutine'i, co-residency tick kuralı, ve addendum'un sabitlediği `IIsikVolumeState` facade'i (shiftId→zone yönlendirme tablosu, tek `RaiseShiftStateChanged` yolu, in-place reset). Build-blocking editör kontrolleri: Mixed-mode ışık, ışık paylaşım yasağı, box-overlap, zorunlu Automatic bölge içeriği.

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-isik-001..006 | Volume/profil/weight tek-yazıcı; smoothstep; durum makinesi; idempotent API; ışık lockstep | ADR-0005 ✅ |
| TR-isik-007..010 | Mixed-mode + 2-3 gölge sınırı; Box Safety Margin; TriggerMode; R_exit histerezisi | ADR-0005 ✅ |
| TR-isik-011 | Co-residency: pozisyon donar, zaman-tabanlı x asla | ADR-0005 ✅ |
| TR-isik-012..015 | Persistent semantiği; IsShiftPersistent; StingerAudioRadius; shiftId anahtar kuralı | ADR-0005 ✅ |
| TR-isik-016..018 | Build-blocking doğrulamalar; post-process-only; PlayerMaxSpeed okuması | ADR-0005 ✅ |
| TR-isik-019 | OnShiftStateChanged tam-bir-kez (reload-restore dahil) | ADR-0005 + addendum ✅ |
| TR-isik-020..021 | Zorunlu Automatic ambient bölge + içerik doğrulaması | ADR-0005 ✅ |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0005 Validation Criteria testleri geçiyor (lockstep `[UnityTest]`, yön-flip, OnDestroy Held garantisi, box-overlap/Baked/paylaşım EditMode build testleri, Automatic-monitor testleri)
- Facade addendum sözleşmesi: aboneler `Instance.OnShiftStateChanged`'e derleniyor, in-place reset iki-oturum testi geçiyor

## Next Step

Run `/create-stories isik-volume-durum-sistemi`.
