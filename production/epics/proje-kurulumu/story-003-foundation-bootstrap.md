# Story 003: FoundationBootstrap.ResetAll() iskeleti

> **Epic**: Proje Kurulumu
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: — (desen altyapısı)
**Requirement**: `TR-oturum-001` (desenin evi; somut servisler kendi epic'lerinde)
**ADR Governing Implementation**: ADR-0001: In-Memory Static Service Pattern
**ADR Decision Summary**: Oturum-kapsamlı her durum, interface + saf C# sınıf + statik facade üçlüsü; TÜM resetler tek `FoundationBootstrap.ResetAll()`'da (`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`), bağımlılık sırasıyla, **in-place** (ADR-0015 rejimi).
**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `SubsystemRegistration` zamanlaması ADR-0001 Verification Required maddesi — pinlenmiş editörde bir kez ampirik smoke şart (Domain Reload açık/kapalı iki modda da tetiklenmeli).

**Control Manifest Rules (bu katman)**:
- Required: tek `ResetAll()` giriş noktası; sıra = ADR-0001 kod bloğundaki belgeli sıra; in-place reset; yeni servis kendi attribute'unu asla almaz
- Forbidden: event-exposing facade'de wholesale replacement; constructor'dan `SceneTransitionManager.Instance`
- Guardrail: reset sub-microsecond sınıfı — boot maliyeti ölçülemez düzeyde

## Acceptance Criteria

- [ ] `FoundationBootstrap` statik sınıfı `game/Assets/Scripts/Foundation/`'da; tek `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` metodu `ResetAll()`
- [ ] ADR-0001'in belgeli sırası kod yorumlarıyla birlikte iskelet olarak duruyor (henüz var olmayan servis satırları `// TODO(epic:X)` işaretli, derlenen kısmı boş olabilir)
- [ ] Sıralama-assertion testi: `ResetAll()` gövdesindeki çağrı sırası ADR-0001'in belgeli sırasıyla eşleşir (mevcut satırlar üzerinden; her servis epic'i kendi satırını ekleyince testi genişletir)
- [ ] Zamanlama smoke'u: `ResetAll()` her Editor Play girişinde (Reload Domain AÇIK ve KAPALI) ve player start'ta tam bir kez koşuyor — log-tabanlı `[UnityTest]` ya da belgeli manuel kanıt
- [ ] Smoke, herhangi bir sahne `Awake()`'inden ÖNCE koştuğunu doğruluyor

## Implementation Notes

- ADR-0001 Key Interfaces'teki jenerik üçlü şablonu (`I[System]State` / `[System]State` / statik facade, in-place `ResetOnLoad()`) burada bir `docs` yorum bloğu ya da örnek-kod referansı olarak yerleşir — sonraki epic'lerin kopyalayacağı tek şablon.
- Seviye/Sahne Geçişi bilinçli olarak listede YOK (ADR-0008 istisnası) — iskelet yorumu bunu söylesin ki gelecekte kimse "eksik" diye eklemesin.

## Out of Scope

- Somut servisler (Gece/Oturum, Işık/Volume vb.) — kendi epic'leri satırlarını ve testlerini ekler
- Story 004: sahne yükleme

## QA Test Cases

- **AC-3 (otomatik)**: sıra assertion'ı
  - Given: `ResetAll()` mevcut satırları
  - When: reflection/kaynak-liste karşılaştırması
  - Then: sıra == ADR-0001 belgeli sırası (alt-küme halinde)
  - Edge cases: satır eklendiğinde test güncellenmeden geçmemeli (kasıtlı kırılganlık)
- **AC-4/5 (UnityTest/manuel)**: zamanlama
  - Given: Reload Domain kapalı Editor profili
  - When: iki ardışık Play oturumu
  - Then: her oturumda tam bir `ResetAll()` logu, ilk sahne `Awake()` logundan önce
  - Edge cases: Reload Domain açıkken de aynı davranış

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/foundation_bootstrap_order_test.cs` (3 test) + `game/Assets/Tests/PlayMode/foundation_bootstrap_timing_test.cs` (1 UnityTest)
**Status**: [x] Created — EditMode 7/7; PlayMode 1/1 **iki modda da** (Reload Domain AÇIK ve KAPALI profillerle ayrı CLI koşuları; her ikisinde `[FoundationBootstrap] ResetAll #1` logu sahne yüklenmeden önce)

## Dependencies

- Depends on: Story 002
- Unlocks: gece-oturum-durumu epic'i (ilk gerçek servis), Story 004

## Completion Notes

**Completed**: 2026-08-09
**Criteria**: 5/5 (statik sınıf + tek attribute'lu `ResetAll()`; 9 servislik belgeli sıra `// TODO(epic:...)` satırlarıyla iskelette, ADR-0008 istisna yorumu dahil; sıra-assertion testi kasıtlı kırılganlıkla; zamanlama smoke'u iki Enter Play Mode profilinde de CLI ile kanıtlı; Awake-öncesi garanti probe testiyle)
**Deviations**: ADVISORY — reset çağrıları ADR'deki düz satırlar yerine adlandırılmış `ResetEntry[]` dizisi üzerinden (tek giriş noktası + açık sıra korunuyor; ADR'nin kendi Validation Criteria'sındaki sıra-assertion testi kaynak-dosya parse etmeden ancak böyle yazılabiliyor — servis epic'leri satır eklerken diziye ekler). `Foundation.asmdef` + `InternalsVisibleTo(EditMode/PlayModeTests)` bu story'de kuruldu (test erişimi için zorunlu altyapı).
**Test Evidence**: yukarıda — CLI sonuç XML'leri + `ResetAll #1` logları
**Code Review**: Skipped — gate subagent'ları mevcut değil (emsal kayıtlı)
