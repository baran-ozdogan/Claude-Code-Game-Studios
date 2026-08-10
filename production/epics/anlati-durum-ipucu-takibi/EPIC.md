# Epic: Anlatı Durum/İpucu Takibi

> **Layer**: Foundation
> **GDD**: design/gdd/anlati-durum-ipucu-takibi.md (Approved)
> **Architecture Module**: Anlatı Durum/İpucu Takibi (`AnlatiDurumIpucuTakibi` static facade)
> **Governing ADRs**: ADR-0007 (+ADR-0015 in-place rejim)
> **Engine Risk**: LOW — projenin ilk Addressables tüketicisi (mekanizma stabil, kullanım yeni)
> **Control Manifest Version**: 2026-08-09
> **Status**: Ready
> **Stories**: 5 stories

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
| TR-anlati-009 | `"ClueRegistry"` Addressable anahtarının build-time çözümlenebilirliği | ADR-0007 ✅ (Risks mitigasyonu — story yazımında mint edildi) |
| TR-isik-021 (AC22 yarısı) | Automatic bölgenin shiftId'si hiçbir `ClueDefinition`'da olamaz | isik-volume Story 006'dan DEVRALINDI → Story 004 |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0007 Validation Criteria testleri geçiyor (çift-Mark tek event; sıra-bağımsız tamamlanma; build-fail üçlüsü; Persistent re-fire çift-event yok; Addressables ilk-Held smoke)
- ClueRegistry cache'i oturumlar arası korunuyor (in-place reset, Addressables'a ResetOnLoad'da asla dokunulmuyor)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Facade çekirdeği — IAnlatiDurumState + AnlatiDurumState + statik facade | Logic | Ready | ADR-0007 (+0001/0015) |
| 002 | ClueDefinition/ClueRegistry + ters indeks + Held handler mantığı | Logic | Ready | ADR-0007 |
| 003 | Addressables lazy-load + gerçek Işık/Volume aboneliği | Integration | Ready | ADR-0007 (+0015) |
| 004 | Build-blocking doğrulama dörtlüsü | Logic | Ready | ADR-0007 (+0014) |
| 005 | Orphaned shiftId uyarısı (build-time aggregate, non-blocking) | Logic | Ready | ADR-0007 (+0014) |

**Bağımlılık grafiği** (düz zincir DEĞİL): 001 → 002 → {003, 004, 005} — son üçü 002'ye bağlı ama BİRBİRİNE bağlı değil, paralel ilerleyebilir.

## Next Step

Run `/dev-story production/epics/anlati-durum-ipucu-takibi/story-001-facade-cekirdegi.md`.

> **Açık ileri bayrak (Story 005)**: orphaned-shiftId kontrolünün mekanizması,
> kullanıcı kararıyla ADR-0007'nin `EditorSceneManager.sceneOpened/sceneSaved`
> tetikleyicisinden `IBuildCheckAggregate` build-yürüyüşüne taşındı (GDD'nin
> proje-geneli iddiası tek-sahne bir kontrolle verilemiyordu — çok-sahneli
> clue'larda yanlış-pozitif). ADR-0007'ye addendum açılmalı
> (`/architecture-decision`); Story 005 sapmayı kendi dosyasında belgeliyor.
