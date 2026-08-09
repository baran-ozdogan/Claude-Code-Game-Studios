# Epic: Adaptif Ses Sistemi

> **Layer**: Foundation
> **GDD**: design/gdd/adaptif-ses-sistemi.md (Approved)
> **Architecture Module**: Adaptif Ses Sistemi (`AdaptifSesSistemi` saf-durum facade'i + `AdaptifSesController` MonoBehaviour — hibrit, Foundation sahnesinin ikinci sakini)
> **Governing ADRs**: ADR-0009 (+2026-08-09 stinger caption addendum; ADR-0015 companion revizyonu — simetrik OnEnable/OnDisable abonelik)
> **Engine Risk**: LOW
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories adaptif-ses-sistemi`

## Overview

4 mixer grubu (Ambiance/Stinger/CutSting/SFX; statik gain-staging + brickwall limiter — ducking yasak), `AmbientZoneVolume` (bölge crossfade'i, co-residency guard, ertelenmiş ilk-tick kontrolü, round-tabanlı `tension_gain` 3. katmanı), stinger havuzu (havuz-uygunluğu ⊥ shiftId-cooldown, çift tetik yolu `IsShiftPersistent` kapılı, `HeldSessionAlreadyPlayed` write-once), HARD CUT ses dallanması (Abrupt: mute→CutSting sırası; değilse crossfade-to-silence), `PlayFootstep(speed)` (FPC faz akümülatöründen), ve **stinger caption'ı** (addendum: PlayOneShot'la senkron göster/klip sonunda gizle; metin stili izlenimci — a11y §2b belirteçleriyle).

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-ses-001..005 | Mixer mimarisi; AmbientZoneVolume; crossfade; co-residency; tension_gain + guard | ADR-0009 ✅ |
| TR-ses-006..009 | Stinger çift tetik; havuz/cooldown ayrımı; write-once guard; uzamsallaştırma | ADR-0009 ✅ |
| TR-ses-010..011 | CutSting kapıları; Abrupt mute sırası / crossfade dalı | ADR-0009 ✅ |
| TR-ses-012..013 | Faz akümülatörü inline okuma; PlayFootstep tek giriş | ADR-0009 ✅ |
| TR-ses-014..015 | Guard temizliği; havuz tükenmesi sessiz | ADR-0009 ✅ |
| TR-ses-016 | Stinger caption (izlenimci, koşulsuz, senkron) | ADR-0009 addendum + a11y §2b ✅ |
| TR-ses-017 | SFX grubu yönlendirmesi (SfxGroup) | ADR-0009 ✅ |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0009 Validation Criteria testleri geçiyor (Persistent tam-bir-kez stinger; Automatic-bölge asla-stinger; co-residency; mute→CutSting sıra assertion'ı; havuz/cooldown bağımsızlık testi; tension guard; Reload-Scene-off iki-oturum)
- Caption a11y §2b belirteçleriyle görünüyor; adaptif-ses AC14b senkronu yapıldı (bkz. a11y §9 borç listesi)

## Next Step

Run `/create-stories adaptif-ses-sistemi`.
