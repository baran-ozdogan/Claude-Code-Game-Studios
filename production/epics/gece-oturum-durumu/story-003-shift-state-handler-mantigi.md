# Story 003: Shift-state handler mantığı (saf, injected)

> **Epic**: Gece/Oturum Durumu
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S (~2h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/quick-specs/gece-oturum-durumu-2026-08-02.md`
**Requirement**: `TR-oturum-004`, `TR-oturum-005` (mantık yarısı — gerçek abonelik Story 004'te)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0006 (primary)
**ADR Decision Summary**: `OnShiftStateChanged(shiftId, newState, ...)` handler'ı: `Shifting-In`'de aynı karede `IsShiftPersistent(shiftId)` sorgusu → `true` ise `PersistentShiftIds[shiftId]=true` (Held'i beklemeden); `Held`'de yalnız id `FiredTriggerIds`'teyse `SettledTriggerIds`'e ekle + `OnTriggerSettled(shiftId)` tam bir kez. Settled her zaman Fired'a ~3sn gecikmeyle yetişir — `SettledCount < FiredCount` geçici penceresi hata değil.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Işık/Volume facade'ı henüz yok — bu story handler'ı SAF metod olarak yazar; `IsShiftPersistent` sorgusu injected delegate (`Func<string,bool>`) üzerinden. Böylece mantık bugün test edilir, Story 004 yalnız wiring yapar.

**Control Manifest Rules (bu katman)**:
- Required: handler yazımları yalnız Persistent (Shifting-In) ve Settled (Held) — Fired'a ASLA dokunmaz
- Forbidden: Settled'ı Fired-üyelik kapısı olmadan yazmak (Automatic ambient zone sızıntısı); polling
- Guardrail: handler aynı-kare senkron, allocation'sız sıcak yol

## Acceptance Criteria

- [ ] `GeceOturumDurumuState` üzerinde internal handler metodu (ör. `ProcessShiftStateChanged(string shiftId, ShiftState newState)`) + `IsShiftPersistent` delegate'i (ctor parametresi ya da internal alan — testler sahte delegate enjekte eder)
- [ ] `Shifting-In` + delegate `true` → `IsPersistent(shiftId)=true` hemen (Held beklenmez)
- [ ] `Shifting-In` + delegate `false` → hiçbir Persistent girişi yok
- [ ] `Held` + id Fired'da → `HasSettled=true` + `OnTriggerSettled` tam bir kez, aynı karede
- [ ] `Held` + id Fired'da DEĞİL → Settled'a yazım yok, event yok (Automatic ambient zone koruması)
- [ ] Aynı id için ikinci `Held` → ikinci event YOK (idempotent)
- [ ] `SettledCount < FiredCount` geçici penceresi mümkün ve hatasız (test belgeliyor)
- [ ] `ShiftState` enum'u yoksa bu story minimal tanımını Foundation'da yapar (Işık/Volume epic'i sahiplenene dek geçici ev — yorumla işaretli)

## Implementation Notes

- Delegate-injection yalnız test dikişi değil — Story 004'te gerçek abonelik bu delegate'i `IsikVolumeDurumSistemi.Instance.IsShiftPersistent`'a bağlar; handler değişmez.
- Quick-spec'in zamanlama düzeltme notları (N2: event `Persistent` taşımaz, sorgu zorunlu; 2026-08-04: saturation her zaman `SettledCount`) kod yorumlarına taşınmalı.

## Out of Scope

- Story 004: gerçek `OnShiftStateChanged` aboneliği + once-per-process garanti testi
- Işık/Volume'un kendi `IsShiftPersistent` implementasyonu (o epic'te)

## QA Test Cases

*(QL-STORY-READY atlandı — qa-lead subagent'ı mevcut değil.)*

- **AC-2/3 (otomatik)**: Persistent yazımı
  - Given: taze state, delegate: `id=="p"` için true
  - When: `Process("p", ShiftingIn)`, `Process("n", ShiftingIn)`
  - Then: `IsPersistent("p")=true`, `IsPersistent("n")=false`
  - Edge cases: aynı id ikinci Shifting-In → değişmez (idempotent)
- **AC-4/5/6 (otomatik)**: Settled kapısı
  - Given: `AddFiredTrigger("f")` yapılmış; "g" Fired'da değil; `OnTriggerSettled` sayacı
  - When: `Process("f", Held)`, `Process("g", Held)`, `Process("f", Held)`
  - Then: `HasSettled("f")=true` + event 1 kez; "g" için yazım/event yok; ikinci Held event üretmez
- **AC-7 (otomatik)**: lag penceresi — `AddFiredTrigger("x")` sonrası Held gelmeden `SettledCount(0) < FiredCount(1)` assert'lenir, hata durumu değil

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/gece_oturum_shift_handler_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 001, Story 002 (Fired-kapısı `AddFiredTrigger`'a dayanır)
- Unlocks: Story 004
