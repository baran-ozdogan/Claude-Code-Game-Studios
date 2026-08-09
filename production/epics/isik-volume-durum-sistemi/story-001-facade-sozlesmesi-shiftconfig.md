# Story 001: Facade sözleşmesi + ShiftConfig (addendum)

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/isik-volume-durum-sistemi.md`
**Requirement**: `TR-isik-005` (API yüzeyi), `TR-isik-013`, `TR-isik-014` (sorgu yarısı), `TR-isik-015`, `TR-isik-019` (tek raise yolu)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0005 — 2026-08-09 facade addendum (primary); ADR-0001/0015 (desen + in-place rejim) secondary
**ADR Decision Summary**: `IIsikVolumeState`/`IsikVolumeState`/statik `IsikVolumeDurumSistemi` addendum bloğundaki BİREBİR şekil: shiftId→`ShiftZone` routing tablosu (OnEnable register / OnDisable deregister), `RaiseShiftStateChanged` TEK raise yolu, in-place `ResetOnLoad` (yalnız `_zonesByShiftId.Clear()` — delegate listesi bilinçli dokunulmaz), bilinmeyen shiftId → Dormant-eşdeğeri default (false/false/0f, throw yok).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `ShiftZone` tipi henüz yok (Story 003) — routing tablosu bu story'de `ShiftZone` yerine dar bir internal arayüz/forward tip üzerinden derlenebilir YA DA Register/Deregister imzaları Story 003'e kadar object-typed placeholder olur; tercih: `IShiftZoneHandle` internal arayüzü (Trigger/Revert/IsActive/IsPersistent/StingerAudioRadius üyeleri) — Story 003 `ShiftZone`'u buna implement eder, facade değişmez.

**Control Manifest Rules (bu katman)**:
- Required: üçlü desen + in-place reset; `ResetAll()` sırasına GeceOturumDurumu'ndan ÖNCE ekleme; tek raise yolu
- Forbidden: `ShiftZone` üzerinde static event (addendum'un süperseded ettiği şekil); wholesale replacement
- Guardrail: sorgular allocation'sız sıcak yol

## Acceptance Criteria

- [ ] `IIsikVolumeState` addendum'daki imzalarla birebir (`TriggerShift/RevertShift/IsShiftActive/IsShiftPersistent/GetStingerAudioRadius` + `event Action<string, ShiftState, Vector3, float> OnShiftStateChanged`)
- [ ] `IsikVolumeState`: routing tablosu + `RegisterZone/DeregisterZone` + `RaiseShiftStateChanged` (internal, TEK yol) + in-place `ResetOnLoad` (yalnız tablo temizlenir, event delegate'i kalır)
- [ ] Bilinmeyen/yüklü-olmayan shiftId sorguları Dormant-eşdeğeri döner (false/false/0f) — throw yok
- [ ] `ShiftConfig` tipi: WB/CA hedefleri, `Duration`, `bool Persistent`, `float StingerAudioRadius` (default 0 = ayarlanmamış); `TriggerMode { Automatic, ManualOnly }` enum'u
- [ ] `ShiftState` sahipliği devralındı: geçici `Assets/Scripts/Foundation/ShiftState.cs` yorumu güncellendi/dosya bu sistemin klasörüne taşındı (GeceOturumDurumu handler'ı derlenmeye devam eder)
- [ ] `FoundationBootstrap._resetSequence`: `IsikVolumeDurumSistemi` satırı `GeceOturumDurumu`'ndan ÖNCE + `ExpectedActiveOrder` güncel
- [ ] Duplicate shiftId register denemesi tanımlı davranış (son-kazanır YA DA hata logu — build-time zaten engelliyor; testte belgelenir)

## Implementation Notes

- Addendum bloğu implementasyon kaynağı — kopyala, türetme. Routing delegasyonları `IShiftZoneHandle` üzerinden; per-zone durum zone'da kalır.
- Bu story kapanınca **gece-oturum Story 004'ün kilidi açılır** (facade + event + IsShiftPersistent hazır).

## Out of Scope

- Story 002: progress/formül mantığı — Story 003: gerçek `ShiftZone`
- GOD Story 004'ün kendisi (ayrı epic)

## QA Test Cases

*(QL-STORY-READY atlandı — qa-lead subagent'ı mevcut değil; spec'ler ADR addendum + GDD sözleşme maddelerinden.)*

- **AC-1/3 (otomatik)**: bilinmeyen shiftId — `IsShiftActive/IsShiftPersistent`→false, `GetStingerAudioRadius`→0, `RevertShift` sessiz no-op, `TriggerShift`→false
- **AC-2 (otomatik)**: fake `IShiftZoneHandle` register → sorgular handle'a yönlenir; deregister → default'lara döner
- **AC-2b (otomatik)**: `RaiseShiftStateChanged` → abone tam bir kez doğru payload'la; `ResetOnLoad` sonrası aynı abone hâlâ alır (delegate korunur), tablo boşalır
- **AC-6 (otomatik)**: sıra testi — `ExpectedActiveOrder = ["IsikVolumeDurumSistemi","GeceOturumDurumu"]`

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/isik_volume_facade_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: proje-kurulumu Story 003 (DONE); gece-oturum Story 001 (DONE — sıra testi ortak)
- Unlocks: Story 002/003; **gece-oturum Story 004**
