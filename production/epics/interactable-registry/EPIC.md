# Epic: InteractableRegistry

> **Layer**: Foundation
> **GDD**: design/gdd/etkilesim-sistemi.md (arayüz sözleşmesi) + design/gdd/birinci-sahis-kontrolcu.md (taper okuması)
> **Architecture Module**: InteractableRegistry (Core→Foundation relocate, architecture.md System Layer Map)
> **Governing ADRs**: ADR-0004
> **Engine Risk**: LOW
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 2 stories

## Overview

`IInteractable` arayüzünün ve statik `InteractableRegistry`'nin (List-tabanlı, frame-başı snapshot cache'li, cache alanları `FoundationBootstrap`'a kayıtlı) implementasyonu. Her etkileşimli nesnenin (pickup, decoy, anı-tetikleyici) ortak giriş kapısı; FPC'nin approach-slow-taper formülünün `d` değişkeninin veri kaynağı. Küçük ama bağımsız kök — Etkileşim Sistemi (Core) ve tüm Feature nesneleri buna derlenir.

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-etkilesim-001 | IInteractable tam üye listesi | ADR-0004 ✅ |
| TR-etkilesim-002 | OnEnable/OnDisable kayıt; snapshot iterasyonu | ADR-0004 ✅ |
| TR-fpc-004 | Taper'ın registry okuması (Foundation relocate) | ADR-0004 ✅ |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0004 Validation Criteria testleri geçiyor (snapshot kararlılığı, cross-session cache-collision `[UnityTest]`, kopya-döndürme garantisi)
- Gerçek/decoy ayrımı API yüzeyinde yapısal olarak sızdırmıyor

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | IInteractable arayüzü + Registry çekirdeği + snapshot cache | Logic | Complete | ADR-0004 |
| 002 | İki-oturum self-correction + cache-collision doğrulaması | Integration | Complete | ADR-0004 |

## Next Step

Run `/story-readiness production/epics/interactable-registry/story-001-registry-cekirdegi-snapshot-cache.md` then `/dev-story`.
