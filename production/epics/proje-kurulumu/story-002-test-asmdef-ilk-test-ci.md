# Story 002: Test asmdef'leri + ilk geçen EditMode testi + CI yeşil

> **Epic**: Proje Kurulumu
> **Status**: In Progress — **GATE KOŞULU #1 (BLOCKING)**: gate-check 2026-08-09 kabul koşulu; bu story kapanmadan hiçbir sistem story'si Done sayılmaz
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S (~2h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: — (altyapı; kaynak: `tests/README.md`, gate raporu `production/gate-checks/2026-08-09-technical-setup-to-pre-production.md`)
**Requirement**: — (gate koşulu #1)
**ADR Governing Implementation**: N/A — test altyapısı; coding-standards.md Testing Standards bağlayıcı.
**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Unity Test Framework built-in; game-ci/unity-test-runner@v4 lisans aktivasyonu ister — **manuel önkoşul, kullanıcı ekler**: `UNITY_EMAIL` + `UNITY_PASSWORD` secret'ları (Unity, Personal lisans için manuel `.ulf` aktivasyonunu 2026 itibarıyla kaldırdı; plan değişikliği 2026-08-09, game.ci/docs/github/activation).

**Control Manifest Rules (bu katman)**:
- Required: iki-oturum test kalıbı ileride bu asmdef'lerde yaşayacak; fixture'lar `ScriptableObject.CreateInstance` (disk asset'i yasak)
- Forbidden: testi geçirtmek için testi kapatmak/skiplemek
- Guardrail: determinizm — random seed/zaman bağımlı assertion yok

## Acceptance Criteria

- [ ] `game/Assets/Tests/EditMode/EditModeTests.asmdef` ve `game/Assets/Tests/PlayMode/PlayModeTests.asmdef` var, derleniyor (TestRunner referansları doğru; EditMode editor-only platform)
- [ ] En az bir **geçen** EditMode testi var (öneri: `foundation_sanity_test.cs` — adlandırma konvansiyonunun ilk örneği)
- [ ] Test Runner'da EditMode+PlayMode sekmeleri yeşil (PlayMode boş geçebilir)
- [ ] CI koşusu yeşil: push'ta editmode+playmode adımları geçiyor (`UNITY_EMAIL`/`UNITY_PASSWORD` eklendikten sonra); gerekiyorsa workflow'a `projectPath: game` eklendi
- [ ] `tests/README.md`'deki "asmdef'ler proje init'te oluşturulacak" notu güncellendi (yol referanslı)

## Implementation Notes

- Repo-kök `tests/` dizini organizasyonel katman (README'ler, smoke listesi, evidence); derlenen testler Unity içinde `game/Assets/Tests/` — README zaten bu ayrımı belgeliyor, yol eşlemesini netleştir.
- İlk test gerçek bir şeyi doğrulasın: örn. adlandırma/derleme sanity'si yerine `FoundationBootstrap` henüz yoksa basit deterministik bir assert; story-003 sonrası sıralama testi buraya taşınır.

## Out of Scope

- Story 003: FoundationBootstrap (bu story'nin testi onu beklemez)

## QA Test Cases

- **AC-1..3 (otomatik)**: asmdef derlenmesi + ilk test
  - Given: story-001 projesi
  - When: Test Runner → Run All (EditMode)
  - Then: ≥1 test, 0 fail
  - Edge cases: PlayMode boş — koşucu hata vermemeli
- **AC-4 (CI)**:
  - Given: `UNITY_LICENSE` secret ekli
  - When: main'e push
  - Then: workflow iki test adımı da yeşil; artifact yüklendi
  - Edge cases: secret yokken anlaşılır lisans hatası (kod hatası değil) — README notuyla eşleşir

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/` ilk test dosyası — geçmek zorunda; CI koşu linki evidence'a not edilir
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003, Story 006, Story 004
