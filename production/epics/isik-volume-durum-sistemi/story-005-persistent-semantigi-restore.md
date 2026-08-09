# Story 005: Persistent semantiği + reload restore

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

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
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 004 (R_exit yolunun Persistent kapısı onun üstüne yazılır)
- Unlocks: sahne-kesmeli/adaptif-ses epic'lerinin tüketici story'leri
