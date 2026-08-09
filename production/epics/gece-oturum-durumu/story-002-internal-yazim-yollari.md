# Story 002: Internal yazım yolları (InternalInstance)

> **Epic**: Gece/Oturum Durumu
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S (~2h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/quick-specs/gece-oturum-durumu-2026-08-02.md`
**Requirement**: `TR-oturum-003`, `TR-oturum-007`, `TR-oturum-008` (yalnız alan+setter; build check kapsam dışı)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0006 (primary); ADR-0014 (`AddFiredTrigger`/`SetTotalConfiguredTriggerCountForNight`'ın eklenme gerekçesi — secondary)
**ADR Decision Summary**: Yazım yolları instance-metodu + `internal` (testler `new GeceOturumDurumuState()` üzerinden çağırır — static-facade-only metod QQ-06 testability açığını yeniden üretirdi); tek-çağıran kısıtı convention + XML-doc + code review (QQ-03 kararı, derleyici değil). `AddFiredTrigger` idempotent, `OnTriggerFired` yalnız İLK eklemede; Fired'ı `OnShiftStateChanged` handler'ı ASLA yazmaz (Automatic ambient zone id'sinin Settled kapısına sızmaması bunun sayesinde).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: —

**Control Manifest Rules (bu katman)**:
- Required: internal yazıcılar tek-çağıran XML-doc'lu; atomik çift-alan yazımı (`SetRoundState`)
- Forbidden: yazıcıları `IGeceOturumDurumuState`'e taşımak; ikinci bir Fired yazım yolu
- Guardrail: event invoke maliyeti — yalnız gerçek ilk eklemede

## Acceptance Criteria

- [ ] `internal void AddFiredTrigger(string shiftId)`: `HashSet.Add` idempotent; `OnTriggerFired(shiftId)` yalnız ilk eklemede tam bir kez, aynı karede senkron
- [ ] `internal void SetRoundState(int currentRoundIndex, int totalRoundCount)`: iki alan atomik (tek çağrıda birlikte) yazılır; 0-based index; XML-doc "yalnız Görev/Taşıma Döngüsü çağırır"
- [ ] `internal void SetTotalConfiguredTriggerCountForNight(int count)`: gece-başı tek yazım; XML-doc "yalnız gece-başı orkestratörü (ADR-0015) çağırır"
- [ ] Üç yazıcı da instance metodu; `GeceOturumDurumu.InternalInstance` üzerinden erişilebilir; hiçbiri interface'te değil
- [ ] Testler yazıcıları doğrudan `new GeceOturumDurumuState()` üzerinde çağırıyor (facade'sız)

## Implementation Notes

- ADR-0006 Data model bloğundaki yorumlar (özellikle `AddFiredTrigger`'ın "NOT written by the OnShiftStateChanged subscription" notu) kod yorumu olarak korunmalı — gelecek epic'lerin yanlış yol seçmemesi bu yorumlara bakar.
- `TotalConfiguredTriggerCountForNight` MemoTriggerDef build-eşitlik kontrolü ani-tetikleyici epic'inde `BuildValidationRegistry`'ye eklenecek — burada yalnız alan + setter.

## Out of Scope

- Build-time eşitlik check'i (ani-tetikleyici epic'i, Story 006 çatısına kayıt)
- `OnShiftStateChanged` kaynaklı Persistent/Settled yazımları (Story 003)

## QA Test Cases

*(QL-STORY-READY atlandı — qa-lead subagent'ı mevcut değil.)*

- **AC-1 (otomatik)**: AddFiredTrigger idempotent + event
  - Given: taze state + `OnTriggerFired` sayaç dinleyicisi
  - When: `AddFiredTrigger("a")` ×2, `AddFiredTrigger("b")`
  - Then: `HasFired("a")=true`, `FiredCount=2`, event "a" için 1, "b" için 1 kez
  - Edge cases: event handler'ı exception fırlatırsa ekleme geri alınmaz (Add önce)
- **AC-2 (otomatik)**: SetRoundState atomikliği
  - Given: taze state
  - When: `SetRoundState(2, 5)`
  - Then: `CurrentRoundIndex=2` VE `TotalRoundCount=5` birlikte görünür
  - Edge cases: ardışık çağrı son değeri yansıtır (3,5 → 2,5 üzerine yazar)
- **AC-3 (otomatik)**: count tek yazım
  - Given: taze state
  - When: `SetTotalConfiguredTriggerCountForNight(3)`
  - Then: property 3 döner; ikinci yazım son değeri alır (yazılım kısıtı konvansiyonda — test davranışı belgeler)

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/gece_oturum_internal_writers_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003; gorev-tasima ve sahne-kesmeli epic'lerinin yazım story'leri
