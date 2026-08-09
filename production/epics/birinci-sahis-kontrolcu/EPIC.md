# Epic: Birinci Şahıs Kontrolcü

> **Layer**: Foundation
> **GDD**: design/gdd/birinci-sahis-kontrolcu.md
> **Architecture Module**: Birinci Şahıs Kontrolcü (Player scene, `FirstPersonController` + `PlayerStateProvider`)
> **Governing ADRs**: ADR-0003 (+ADR-0004 registry okuması)
> **Engine Risk**: LOW (solver-iteration MEDIUM iddiası ADR-0003'te faktüel hata olarak düzeltildi)
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories birinci-sahis-kontrolcu`

## Overview

Persistent Player sahnesinde yaşayan oyuncu: `CharacterController`-tabanlı kinematik hareket, kamera pitch, `PlayerStateProvider` (IPlayerState + iki-HashSet referans-sayımlı hareket kilidi, `Current` static accessor, duplicate guard), approach-slow-taper (registry'den `d`), paylaşılan mesafe-tabanlı faz akümülatörü (bob/ayak sesi/sway'in tek kaynağı), yeni Input System "Gameplay" action map. Erişilebilirlik bağları: motion yoğunluk kaydırıcısı görsel genliği ölçekler, akümülatörü asla (a11y §5); decoy içerik şartı (TR-fpc-016) build-time doğrulamalı.

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-fpc-001..003 | IPlayerState alanları; ref-sayımlı kilit; most-restrictive-wins | ADR-0003 ✅ |
| TR-fpc-004 | Taper registry okuması | ADR-0004 ✅ |
| TR-fpc-005..009 | Kilit semantiği (input'la asla; idempotent; sticky; event; IsLocked) | ADR-0003 ✅ |
| TR-fpc-010 | Persistent Player sahnesi; anchor repozisyonu | ADR-0003 ✅ |
| TR-fpc-011..012 | Kinematik CC; MoveOnly'de Look serbest | ADR-0003 ✅ |
| TR-fpc-013 | IsCarrying aynası — **köprü**: mekanizma ADR-0013'te (Feature, `CarrySlotRigController`); bu epic yalnız `SetCarrying` yüzeyini sağlar | ADR-0003/0013 ✅ |
| TR-fpc-014..015 | Faz akümülatörü; Input System action map | ADR-0003 ✅ |
| TR-fpc-016 | Decoy içerik şartı + build doğrulaması | ADR-0003 ✅ (utility: proje-kurulumu) |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0003 Validation Criteria testleri geçiyor (kilit matrisi EditMode; SOFT repozisyon + Reload-Scene-off iki-oturum `[UnityTest]`; duplicate-guard)
- Motion kaydırıcısı: %0'da görsel sıfır, ayak sesi zamanlaması değişmemiş (a11y §8)

## Next Step

Run `/create-stories birinci-sahis-kontrolcu`.
