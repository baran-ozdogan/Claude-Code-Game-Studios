# Story 005: Persistent semantiği + reload restore

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: `design/gdd/isik-volume-durum-sistemi.md`
**Requirement**: `TR-isik-012` (+GDD AC4/5 ve ertelenmiş AC17'nin kapanışı)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0005 (primary); ADR-0006 (PersistentShiftIds okuma yönü) secondary
**ADR Decision Summary**: `Persistent=true` shift Held'de kalıcı — R_exit kontrolü hiç koşmaz, Shifting-Out atlanır; Held-Persistent'e tekrar giriş saf no-op (`TriggerShift` false, event yok). Sahne yüklemesinde bölge, `GeceOturumDurumu.Instance.IsPersistent(shiftId)` true ise Dormant + Shifting-In'i tamamen atlayıp doğrudan Held-Persistent + ShiftProgress=1 başlar ve yükleme sonrası `OnShiftStateChanged(Held)` TAM BİR KEZ fırlar (ses senkronu için).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Restore sorgusu `OnEnable` başında (QQ-07 / `scene_object_state_restore_timing` deseni — Awake DEĞİL, Reload Scene OFF güvenliği).

**Control Manifest Rules (bu katman)**:
- Required: restore OnEnable-top, Register'dan önce; `X.Instance` canlı dereference
- Forbidden: restore'da Shifting-In coroutine'i koşturmak; ikinci bir event
- Guardrail: restore yolu tek-kare, allocation'sız

## Acceptance Criteria

- [ ] `Persistent=true` Held bölge: R_exit geçilse de Shifting-Out'a girmez, oturum boyunca Shifted kalır (AC4)
- [ ] Held-Persistent'e tekrar giriş/`TriggerShift`: false döner, restart yok, event yok (AC5)
- [ ] OnEnable restore: `GeceOturumDurumu.Instance.IsPersistent(shiftId)` true → doğrudan Held-Persistent, `Volume.weight=1`, ışıklar memory hedefinde, `OnShiftStateChanged(Held)` yükleme sonrası tam bir kez (AC17 — GDD'nin ertelenmiş AC'si kapanır)
- [ ] IsPersistent false → normal Dormant başlangıç, event yok
- [ ] Restore edilen bölgede `IsShiftPersistent(shiftId)` true döner (Gece/Oturum'un yeniden-yazımı idempotent kalır)

## Implementation Notes

- Bu story iki epic'in entegrasyon noktası: GDD AC17'nin "gerçek sahne-yeniden-yükleme senaryosu" şartı PlayMode testinde bölge objesini destroy/re-create ederek simüle edilir (sahne unload/load eşdeğeri).
- GOD Story 004 (abonelik wiring'i) bu story'den bağımsız — ikisi de Story 001'in facade'ına dayanır; sıra serbest.

## Out of Scope

- Gece/Oturum tarafındaki abonelik (GOD Story 004)
- Sahne Kesmeli / Adaptif Ses tüketimleri

## QA Test Cases

*(QL-STORY-READY atlandı.)*

- **AC-1/2 (UnityTest)**: Persistent Held + sampler R_exit dışına → durum değişmez, event yok; TriggerShift → false
- **AC-3 (UnityTest)**: GeceOturumDurumu.InternalInstance'a Persistent kaydı ek → yeni bölge instantiate (aynı shiftId) → ilk karede Held-Persistent + weight=1 + tek event
- **AC-4 (UnityTest)**: kayıt yokken instantiate → Dormant, event yok

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/isik_volume_persistent_restore_test.cs`
**Status**: [x] Created — 5 UnityTest, PlayMode süiti 32/32 (2026-08-09)

## Dependencies

- Depends on: Story 004 (R_exit yolunun Persistent kapısı onun üstüne yazılır)
- Unlocks: sahne-kesmeli/adaptif-ses epic'lerinin tüketici story'leri

## Completion Notes
**Completed**: 2026-08-09
**Criteria**: 5/5 passing (EditMode 53/53, PlayMode 32/32 lokal CLI); GDD'nin ertelenmiş AC17'si kapandı
**Deviations**: (1) Dış `RevertShift` de Persistent Held'de no-op — story AC'leri yalnız R_exit'i sayıyordu; LP üç gerekçeyle onayladı (GDD AC4'ün oturum-değişmezi ifadesi, manifest'in gameplay `RevertShift` CI-ban'ı, restore edilen bölgenin null-`_activeConfig` NRE koruması). (2) Persistent Held coroutine'i bitirir (terminal durum — LP önerisi, canlı/restore tutarlı). (3) AC-3 testi GOD kaydını elle enjekte etmek yerine GERÇEK abonelik wiring'iyle yazdırır (story metnine göre upgrade — QA lead teyitli). (4) StingerAudioRadius restore edilmez (GOD yalnız bool saklar) — restore sonrası sorgu 0; **Adaptif Ses epic'ine bayrak: tüketici event payload'ındaki R_trigger'ı kullanmalı.**
**ADR/GDD backlog bayrağı**: GOD gerçeği Shifting-In'de yazılır, revert kilidi Held'de — mid-In revert edilen persistent bölge reload'da Held dirilir; mevcut davranış `PersistentConfig_RevertMidShiftIn_...` testiyle sabitlendi, kural değişirse test bilinçli güncellenir (LP review gözlemi).
**Test Evidence**: Integration — `game/Assets/Tests/PlayMode/isik_volume_persistent_restore_test.cs` (5 UnityTest)
**Code Review**: Complete — LP-CODE-REVIEW: APPROVE, QL-TEST-COVERAGE: GAPS→3 test eklendi (full mod, general-purpose subagent gate'leri)
