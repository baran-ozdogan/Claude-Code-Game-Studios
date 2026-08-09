# Epic: Anlatı Durum/İpucu Takibi

> **Layer**: Foundation
> **GDD**: design/gdd/anlati-durum-ipucu-takibi.md (Approved)
> **Architecture Module**: Anlatı Durum/İpucu Takibi (`AnlatiDurumIpucuTakibi` static facade)
> **Governing ADRs**: ADR-0007 (+ADR-0015 in-place rejim)
> **Engine Risk**: LOW — projenin ilk Addressables tüketicisi (mekanizma stabil, kullanım yeni)
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories anlati-durum-ipucu-takibi`

## Overview

`ClueDefinition`/`ClueRegistry` ScriptableObject veri modeli (N:1, ALL-semantiği), `Held`-only `OnShiftStateChanged` işleyicisi, shiftId→ClueDefinition ters indeksi, Addressables lazy-load (`EnsureRegistryLoaded`, constructor dışında — ilk gerçek Held'de), idempotent `MarkClueKnown` + `OnClueKnown`, iki-katmanlı editör doğrulaması (boş requiredShiftIds/çift clueId/çözülmeyen Addressable key build-blocking; orphaned shiftId Editor-only uyarı). Sıralama/zaman verisi asla açılmaz (Pillar 1/5).

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-anlati-001 | Statik singleton kalıcılık | ADR-0001/0007 ✅ |
| TR-anlati-002..004 | ClueDefinition ALL-semantiği; idempotent Mark; sorgu yüzeyi | ADR-0007 ✅ |
| TR-anlati-005..006 | Held-only işleme; first-access abonelik | ADR-0007 ✅ |
| TR-anlati-007 | Sıralama verisi yok | ADR-0007 ✅ |
| TR-anlati-008 | İki-katmanlı editör doğrulaması | ADR-0007 ✅ (utility: proje-kurulumu) |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0007 Validation Criteria testleri geçiyor (çift-Mark tek event; sıra-bağımsız tamamlanma; build-fail üçlüsü; Persistent re-fire çift-event yok; Addressables ilk-Held smoke)
- ClueRegistry cache'i oturumlar arası korunuyor (in-place reset, Addressables'a ResetOnLoad'da asla dokunulmuyor)

## Next Step

Run `/create-stories anlati-durum-ipucu-takibi`.
