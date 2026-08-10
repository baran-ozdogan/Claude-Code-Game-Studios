# Epic: Birinci Şahıs Kontrolcü

> **Layer**: Foundation
> **GDD**: design/gdd/birinci-sahis-kontrolcu.md
> **Architecture Module**: Birinci Şahıs Kontrolcü (Player scene, `FirstPersonController` + `PlayerStateProvider`)
> **Governing ADRs**: ADR-0003 (+ADR-0004 registry okuması)
> **Engine Risk**: LOW (solver-iteration MEDIUM iddiası ADR-0003'te faktüel hata olarak düzeltildi)
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 6 stories

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

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | IPlayerState + PlayerStateProvider (referans-sayımlı hareket kilidi) | Logic | Complete | ADR-0003 |
| 002 | Hareket matematiği — ivmelenme, taper, head-bob (saf) | Logic | Complete | ADR-0003 (secondary) |
| 003 | FirstPersonController sürücüsü — CharacterController + kamera + Input System | Integration | Complete | ADR-0003 |
| 004 | Persistent Player sahnesi + SOFT transition repozisyonu | Integration | Ready | ADR-0003 |
| 005 | Taper wiring + IsCarrying aynası + faz akümülatörü entegrasyonu | Integration | Ready | ADR-0003/0004 |
| 006 | Decoy içerik build-time doğrulaması | Logic | Ready | ADR-0003 (secondary) |

## Next Step

Story 001+002+003 Complete (3/6). Run `/dev-story production/epics/birinci-sahis-kontrolcu/story-004-kalici-sahne-soft-transition.md` next (Story 003'ün ürettiği `Assets/Prefabs/Player.prefab`'ı Player.unity'ye yerleştirir + `RepositionTo` API'sini kilitler).
