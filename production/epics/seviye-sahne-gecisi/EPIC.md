# Epic: Seviye/Sahne Geçişi

> **Layer**: Foundation
> **GDD**: design/gdd/seviye-sahne-gecisi.md (Approved)
> **Architecture Module**: Seviye/Sahne Geçişi (`SceneTransitionManager` — Foundation persistent sahnesinde MonoBehaviour; ADR-0001 deseninin belgelenmiş tek istisnası)
> **Governing ADRs**: ADR-0008
> **Engine Risk**: LOW
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 7 stories

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

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Saf durum makinesi çekirdeği | Logic | Complete | ADR-0008 |
| 002 | MonoBehaviour sürücü + Foundation persistent sahnesi + facade | Integration | Complete | ADR-0008 (+0003) |
| 003 | SOFT geçiş dizisi + gerçek %100 yükleme | Integration | Ready | ADR-0008 |
| 004 | DoSwap üçlüsü — anchor, RenderSettings, ertelenmiş unload | Integration | Ready | ADR-0008 (+0003/0015) |
| 005 | HARD CUT — ayrı preload durumu, Ready fast-path, sıfır-kare swap | Integration | Ready | ADR-0008 (+0015) |
| 006 | Çakışma kuralları — asimetrik kuyruk ve ret event'i | Logic | Ready | ADR-0008 |
| 007 | Failed yolu, SafeInvoke ve Abrupt taşıma | Logic | Ready | ADR-0008 |

**Bağımlılık grafiği: KESİNLİKLE DOĞRUSAL** — 001 → 002 → 003 → 004 → 005 → 006 → 007.
Hiçbir ikili paralel koşamaz. En kritik bağ 004 → 005: HARD CUT'ın Ready fast-path'i
`DoSwap`'ı DOĞRUDAN çağırıyor ve AC-9'un kare-deltası ölçümü Story 004'ün RenderSettings
senkronunun senkron maliyetini de kapsıyor — 005 önce inerse epsilon kanıtı eksik bir
`DoSwap`'a karşı ölçülür ve 004 aynı kareye iş eklediğinde sessizce bayatlar
(QL-STORY-READY bulgusu).

## Story yazımında alınan iki karar (kullanıcı, 2026-08-10)

1. **Durum ayrımı**: saf C# `SceneTransitionState` çekirdeği + ince `MonoBehaviour`
   sürücü. Manifest'in "saf çekirdek + ince sürücü (BLOCKING)" kuralı bölüm bazında
   *Core Layer* altında olduğu ve sahne geçişleri *Foundation Layer*'a ait olduğu için
   bu **bağlayıcı bir ihlal değil, tercihti** — sevk edilmiş emsaller (ShiftZone'un saf
   `ShiftProgressMachine`'i, ADR-0011'in Elevator ayrımı) aynı yönü gösterdiği ve ayrım
   ADR-0008'in Validation Criteria'sının çoğunu EditMode'a indirdiği için seçildi.
   **ADR-0008 story'ler yazılmadan ÖNCE yerinde düzeltildi** (o an hiç kod yoktu):
   Data model'e ayrım notu + `SceneTransitionState` taslağı eklendi, artık doğru
   olmayan Negative maddesi üstü çizildi. MonoBehaviour barındırma kararının kendisine
   DOKUNULMADI — o zaten çatışmıyordu.
2. **AC-9(b), "tam 0 siyah kare"**: Story 005'in ilk işi CI'da gerçek kare yakalamanın
   mümkün olup olmadığını ölçen kısa bir spike. Mümkünse otomatik piksel assertion'ı;
   değilse DEFERRED manuel kanıt. Kalıcı ertelemeden önce ucuz doğrulama (QL-STORY-READY
   bulgusu). **Uyarı**: "aktif sahne geçerli" sanity check'i bu garantinin YERİNE GEÇMEZ —
   Unity'nin additive modelinde tüm yüklü sahnelerin kameraları render eder.

## Açık kayıt boşluğu

GDD'nin en çok vurguladığı kurallardan biri — **"SOFT geçiş minimum süresi (2-8s) bir
taban/pacing değeridir, bir tamamlanma tetikleyicisi DEĞİL"** — hiçbir TR-ID'ye sahip
değil. `TR-sahne-gecisi-006` bu boşluğu kapatmaz (o açıkça `PreloadHardCut`/HARD CUT
hakkında; "soft yarısı" diye bir şey yok). Story 003 kuralı yine de test ediyor ve
`TR-sahne-gecisi-001`'e demirliyor. `/architecture-review`'ın bir sonraki turunda kendi
ID'sini mint etmesi gerekiyor.

## Next Step

Run `/story-readiness production/epics/seviye-sahne-gecisi/story-001-durum-makinesi-cekirdegi.md`,
then `/dev-story`. Story'ler SIRAYLA işlenmeli — bağımlılık grafiği doğrusal.
