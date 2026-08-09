# Epic: Gece/Oturum Durumu

> **Layer**: Foundation
> **GDD**: design/quick-specs/gece-oturum-durumu-2026-08-02.md
> **Architecture Module**: Gece/Oturum Durumu (`GeceOturumDurumu` static facade)
> **Governing ADRs**: ADR-0006 (+ADR-0001 desen; ADR-0014'ün eklediği alanlar; ADR-0015 in-place rejim)
> **Engine Risk**: LOW
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 4 stories (2026-08-09)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Üçlü desen + oturum gerçekleri + in-place reset | Logic | Ready | ADR-0006 (+0001/0015) |
| 002 | Internal yazım yolları (InternalInstance) | Logic | Ready | ADR-0006 (+0014) |
| 003 | Shift-state handler mantığı (saf, injected) | Logic | Ready | ADR-0006 |
| 004 | Işık/Volume aboneliği (gerçek wiring) | Integration | Ready ⚠ isik-volume bekler | ADR-0006 (+0015) |

Bağımlılık: 001 → 002 → 003 → 004 (+004 cross-epic: isik-volume facade story'si)

## Overview

ADR-0001 deseninin kanonik ilk tüketicisi: oturum gerçeklerinin (Fired/Persistent/Settled trigger kümeleri, round sayaçları, `TotalConfiguredTriggerCountForNight`, `IsSessionActive`) tek evi. Constructor-time Işık/Volume aboneliği (Shifting-In→Persistent, Held→Settled, Fired-üyelik kapılı), `AddFiredTrigger`/`SetRoundState`/`SetTotalConfiguredTriggerCountForNight` internal yazım yolları (tek-çağıran konvansiyonları), `FiredCount`/`SettledCount` sorguları, in-place `ResetOnLoad` (`IsSessionActive=true` re-init dahil). Sahne Kesmeli Anlatı'nın bitiş mantığının tüm veri tabanı.

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-oturum-001 | Statik servis kalıcılığı | ADR-0001/0006 ✅ |
| TR-oturum-002 | IsSessionActive + EndSession (tek-çağıran, idempotent) | ADR-0006 ✅ |
| TR-oturum-003 | Fired doğrudan yazım + OnTriggerFired | ADR-0006/0014 ✅ |
| TR-oturum-004..005 | Persistent (Shifting-In) / Settled (Held, Fired-kapılı) abonelik yazımları | ADR-0006 ✅ |
| TR-oturum-006 | Write-once gerçekler; CurrentNightNumber=1 | ADR-0006 ✅ |
| TR-oturum-007 | Round sayaçları relocate + SetRoundState atomik | ADR-0006 ✅ |
| TR-oturum-008 | TotalConfiguredTriggerCountForNight + build eşitlik kontrolü | ADR-0014 ✅ |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0006/0001 Validation Criteria testleri geçiyor (reset-sonrası taze-abonelik iki-oturum testi, SetRoundState atomikliği, EndSession idempotentliği, AddFiredTrigger idempotent + tam-bir-kez event)
- `FoundationBootstrap.ResetAll()` sırası korunmuş (Işık/Volume'den sonra)

## Next Step

Run `/create-stories gece-oturum-durumu`.
