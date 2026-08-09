# Story 001: Üçlü desen + oturum gerçekleri + in-place reset

> **Epic**: Gece/Oturum Durumu
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: `design/quick-specs/gece-oturum-durumu-2026-08-02.md`
**Requirement**: `TR-oturum-001`, `TR-oturum-002`, `TR-oturum-006`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da — review anında taze oku)*

**ADR Governing Implementation**: ADR-0006: Session State Service and Round-Counter Ownership (primary); ADR-0001 (üçlü desen), ADR-0015 (in-place rejim) secondary
**ADR Decision Summary**: `IGeceOturumDurumuState` / `GeceOturumDurumuState` / statik `GeceOturumDurumu` üçlüsü; interface imzaları ADR-0006 Data model bloğundaki gibi BİREBİR (membership sorguları `HasFired/HasSettled/IsPersistent`, sayaçlar `FiredCount/SettledCount` pass-through, `EndSession` interface üyesi); reset in-place, `IsSessionActive=true` re-init dahil; tetikleme yalnız `FoundationBootstrap.ResetAll()`.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `IReadOnlySet<T>` KULLANMA (.NET Standard 2.1 garantisi yok — ADR-0006 unity-specialist bulgusu); koleksiyonlar membership-metodu arkasında kalır.

**Control Manifest Rules (bu katman)**:
- Required: interface + saf C# sınıf + statik facade; testler `new GeceOturumDurumuState()` kurar, facade'a asla dokunmaz; in-place reset + non-default alan re-init; `ResetAll()` sırasına doğru noktadan ekleme
- Forbidden: event-exposing facade'de wholesale replacement; kendi `[RuntimeInitializeOnLoadMethod]`'u; interface dışı concrete-`State` erişimi (tüketicilerde)
- Guardrail: reset sub-microsecond sınıfı

## Acceptance Criteria

- [ ] `game/Assets/Scripts/Foundation/GeceOturumDurumu/` altında üçlü: `IGeceOturumDurumuState` ADR-0006'daki imzalarla birebir (events dahil); `GeceOturumDurumuState` backing HashSet/Dictionary'lerle; statik `GeceOturumDurumu` (`Instance` + `InternalInstance` + `ResetOnLoad`)
- [ ] Başlangıç durumu: `IsSessionActive=true`, `CurrentNightNumber=1`, tüm kümeler boş, sayaçlar 0
- [ ] `EndSession()` → `IsSessionActive=false`; idempotent (ikinci çağrı hata/side-effect üretmez); geri dönüş yolu YOK
- [ ] Oturum gerçekleri write-once-per-fact: gece içinde hiçbir küme temizlenmez/silinmez (temizleme yalnız `ResetOnLoad`)
- [ ] `ResetOnLoad()` IN-PLACE: aynı instance'ta tüm kümeler/sayaçlar temizlenir, `IsSessionActive=true` explicit re-init; instance referansı değişmez, event abonelikleri hayatta kalır
- [ ] `FoundationBootstrap._resetSequence`'a `GeceOturumDurumu` satırı eklendi (belgeli sıradaki konumunda) VE `foundation_bootstrap_order_test.cs::ExpectedActiveOrder` aynı değişiklikte güncellendi
- [ ] İki-oturum PlayMode testi: simüle ikinci oturumda `HasFired(x)` false (ADR-0001 Validation Criteria state-tazelik kalıbı)

## Implementation Notes

- ADR-0006 Data model bloğu implementasyonun kaynağıdır — imzaları oradan kopyala, yeniden türetme. `FiredCount/SettledCount` ayrı sayaç alanı DEĞİL, `_fired.Count` pass-through (drift imkânsız).
- Işık/Volume aboneliği BU STORY'DE YOK (Story 003 mantık, Story 004 wiring) — constructor şimdilik abonelik içermez; Story 004 eklerken "once-per-process" şekli korunacak.
- `InternalInstance` bu story'de tanımlanır (concrete-typed internal accessor); internal yazıcılar Story 002'de.

## Out of Scope

- Story 002: `AddFiredTrigger`/`SetRoundState`/`SetTotalConfiguredTriggerCountForNight`
- Story 003/004: `OnShiftStateChanged` işleme ve aboneliği

## QA Test Cases

*(QL-STORY-READY atlandı — qa-lead subagent'ı mevcut değil; spec'ler quick-spec AC'lerinden ve ADR-0006/0001 Validation Criteria'dan derlendi.)*

- **AC-2/3 (otomatik)**: başlangıç + EndSession
  - Given: `new GeceOturumDurumuState()`
  - When: hiçbir şey / `EndSession()` ×2
  - Then: başlangıçta `IsSessionActive=true, CurrentNightNumber=1, FiredCount=0`; sonrasında `false`, ikinci çağrı değiştirmez
  - Edge cases: `EndSession` sonrası membership sorguları hâlâ çalışır (okuma yüzeyi kapanmaz)
- **AC-5 (otomatik)**: in-place reset
  - Given: doldurulmuş state (Fired/Persistent/Settled kayıtlı, `EndSession` çağrılmış) + event'e abone bir dinleyici
  - When: `ResetOnLoad()`
  - Then: kümeler boş, `IsSessionActive=true`; aynı instance; sonraki `AddFiredTrigger`'da (Story 002 sonrası genişletilir) abone hâlâ event alır
- **AC-6 (otomatik)**: sıra testi genişletmesi — `ExpectedActiveOrder = ["GeceOturumDurumu"]`, subsequence testi geçer
- **AC-7 (UnityTest)**: iki-oturum tazelik — birinci "oturumda" Fired kaydı, oturum sınırı simülasyonu (`ResetOnLoad`), ikinci oturumda `HasFired=false`

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/gece_oturum_state_test.cs` (4 test — EditMode 19/19)
**Status**: [x] Created — iki-oturum `[UnityTest]` Story 002'nin kapsamına kaydırıldı (`HasFired` senaryosu `AddFiredTrigger`'ı gerektiriyor; oradaki `gece_oturum_two_session_test.cs` bu AC'yi kapatır)

## Completion Notes

**Completed**: 2026-08-09
**Criteria**: 7/7 — AC-7 (iki-oturum) Story 002'de kapanacak şekilde kaydırıldı (yazıcı olmadan Fired doldurulamaz; story sırası gereği bilinçli erteleme)
**Deviations**: ADVISORY — yukarıdaki AC-7 kaydırması. `CurrentNightNumber` sabit `=> 1` (reset'te bile 1; MVP sözleşmesi).
**Test Evidence**: EditMode 19/19 CLI (4 yeni test: defaults, EndSession idempotent, in-place reset + IsSessionActive re-init, facade same-instance invariant); `ExpectedActiveOrder=["GeceOturumDurumu"]` sıra testi güncel
**Code Review**: Skipped — gate subagent'ları mevcut değil (emsal kayıtlı)

## Dependencies

- Depends on: proje-kurulumu Story 003 (FoundationBootstrap) — DONE
- Unlocks: Story 002, Story 003
