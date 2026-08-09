# Epic: Seviye/Sahne Geçişi

> **Layer**: Foundation
> **GDD**: design/gdd/seviye-sahne-gecisi.md (Approved)
> **Architecture Module**: Seviye/Sahne Geçişi (`SceneTransitionManager` — Foundation persistent sahnesinde MonoBehaviour; ADR-0001 deseninin belgelenmiş tek istisnası)
> **Governing ADRs**: ADR-0008
> **Engine Risk**: LOW
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories seviye-sahne-gecisi`

## Overview

SOFT (asansör, additive co-residency, `SoftTransitionAnchor` repozisyonu) ve HARD CUT (psikiyatri kesmesi, sıfır-kare `SetActiveScene` swap'ı) geçişlerinin tek durum makinesi: `Idle→Preloading→Ready→Swapping→Complete→Idle` + `Failed` auto-return, ayrı `_hardCutPreloadState`, tek-slot HARD CUT kuyruğu (SOFT asla kuyruklanmaz — senkron `OnSoftTransitionRejected`), `SafeInvoke` callback güvenliği, 0.5-2s ertelenmiş unload, `SceneEnvironmentSettings` RenderSettings senkronu, `HardCutConfig.Abrupt` taşıma. En kritik test yükü: `SWAP_FRAME_EPSILON` ≤1 kare + **tam 0 siyah kare** — iki bağımsız test. ADR-0015'in pin'lediği boot-time `RequestSoftTransition(null, ...)` null-guard'ı dahil.

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-sahne-gecisi-001..003 | Tek durum makinesi; API sözleşmesi; SOFT co-residency + anchor | ADR-0008 ✅ |
| TR-sahne-gecisi-004 | Sıfır-kare swap + 0 siyah kare (binary) | ADR-0008 ✅ |
| TR-sahne-gecisi-005..007 | Ertelenmiş unload; %100 preload; ayrı preload durumu + no-op tekrar çağrılar | ADR-0008 ✅ |
| TR-sahne-gecisi-008..010 | Asimetrik kuyruk; Failed auto-return; SafeInvoke | ADR-0008 ✅ |
| TR-sahne-gecisi-011..013 | TransitionType'lı event; Abrupt taşıma; RenderSettings senkronu | ADR-0008 ✅ |
| TR-sahne-gecisi-014 | Kilit yaşam döngüsü çağıranın | ADR-0008 ✅ |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0008 Validation Criteria testleri geçiyor (epsilon + siyah-kare çifti ayrı assertion'lar; ret/kuyruk/Failed/istisna senaryoları; Reload-Scene-off iki-oturum `Instance` testi)
- Depot/Ballroom sahneleri greybox düzeyinde geçiş test edilebilir durumda (asset'ler `assets/art`'taki hazır FBX'lerden iskeletlenebilir)

## Next Step

Run `/create-stories seviye-sahne-gecisi`.
