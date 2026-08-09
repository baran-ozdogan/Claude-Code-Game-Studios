# Epic: Proje Kurulumu & Statik Servis Altyapısı

> **Layer**: Foundation (cross-cutting)
> **GDD**: — (altyapı epic'i; ADR-0001/0002 + gate koşulları kaynaklı)
> **Architecture Module**: Cross-cutting infrastructure (FoundationBootstrap, persistent scenes, UI framework, shared editor validation)
> **Governing ADRs**: ADR-0001, ADR-0002 (+ADR-0003/0008'in persistent-sahne sözleşmeleri iskelet düzeyinde)
> **Engine Risk**: LOW
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 6 created (2026-08-09)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Unity 6.3 projesi init | Config/Data | Ready | N/A (tech-prefs) |
| 002 | Test asmdef'leri + ilk test + CI — **gate koşulu #1** | Logic | Ready | N/A (gate) |
| 003 | FoundationBootstrap.ResetAll() iskeleti | Logic | Ready | ADR-0001 |
| 004 | Persistent sahneler + boot yükleyici | Integration | Ready | ADR-0015 (+0002/0003) |
| 005 | UIRoot + MainUI.uxml iskeleti | UI | Ready | ADR-0002 (+0010) |
| 006 | Paylaşılan doğrulama utility'si | Logic | Ready | ADR-0014 (+0007/0012/0013/0015) |

Bağımlılık: 001 → 002 → {003, 006}; 002 → 004 → 005

## Overview

Diğer her Foundation epic'inin üzerine oturduğu zemin: Unity 6.5 (6000.5.6f1) projesinin init'i (URP, yeni Input System, proje ayarları, `src/` yerleşimi), test asmdef'leri + **ilk geçen EditMode testi** (gate-check 2026-08-09 kabul koşulu #1 — BLOCKING), `FoundationBootstrap.ResetAll()` iskeleti (ADR-0001'in dependency-sıralı reset listesi, in-place rejim), üç persistent sahnenin iskeleti (UI/Player/Foundation — boot'ta sıralı additive yükleme, ADR-0003'ün UI→Player sırası), `UIRoot` singleton'ı + paylaşılan `MainUI.uxml` iskeleti (ADR-0002), ve paylaşılan `IPreprocessBuildWithReport` editör-doğrulama utility'sinin boş gövdesi (ADR-0007/0012/0013/0014/0015'in check'lerinin ekleneceği tek ev). CI'ın (game-ci) ilk yeşil koşusu bu epic'te alınır (`UNITY_LICENSE` secret önkoşulu).

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-oturum-001 | In-memory statik servis kalıcılık deseni (tüm tüketicilerin şablonu) | ADR-0001 ✅ |
| — (gate koşulu #1) | asmdef'ler + en az bir geçen EditMode testi | Gate raporu 2026-08-09 ✅ |
| — (ADR-0002) | UI Toolkit tek framework; persistent UI sahnesi; UIRoot | ADR-0002 ✅ |
| — (ADR-0015 boot sözleşmesi) | Initial load set = yalnız persistent sahneler | ADR-0015 ✅ |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler `/story-done` ile kapandı
- Unity projesi repo'da; CI EditMode+PlayMode yeşil; ilk EditMode testi geçiyor (gate koşulu #1 kapandı)
- `FoundationBootstrap.ResetAll()` mevcut ve iki-oturum `[UnityTest]` ile doğrulanmış (ADR-0001 Validation Criteria)
- UI/Player/Foundation sahneleri boot'ta yükleniyor; `UIRoot.Instance` duplicate-guard'lı
- Paylaşılan editör-doğrulama utility'si var (boş da olsa) ve build'e bağlı

## Next Step

Run `/create-stories proje-kurulumu`.
