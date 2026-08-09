# Story 001: Unity 6.3 projesi init (URP + Input System + paketler)

> **Epic**: Proje Kurulumu
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Config/Data
> **Estimate**: S (~2h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: — (altyapı; kaynak: `.claude/docs/technical-preferences.md`, `docs/engine-reference/unity/VERSION.md`)
**Requirement**: — (altyapı story'si; sistem TR'ları kendi epic'lerinde)
**ADR Governing Implementation**: N/A — proje init; mimari desen içermez. Sürüm/paket seçimleri tech-prefs + VERSION.md'den.
**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Editör sürümü tam 6000.5.6f1 olmalı (pin — 2026-08-09'da 6000.3.0f1'den re-pin edildi, kurulu editör + greybox prototip sürümü). Legacy Input Manager KAPALI — yalnız yeni Input System (deprecated-apis.md).

**Control Manifest Rules (bu katman)**:
- Required: adlandırma konvansiyonları (PascalCase dosya=sınıf); veri-güdümlü config ilkesi
- Forbidden: UGUI/Canvas paket kalıntısı yeni koda girmez; legacy `Input.*`
- Guardrail: 60fps/16.6ms hedefi boş sahnede triviyal sağlanmalı

## Acceptance Criteria

- [ ] `my-game/game/` altında self-contained Unity 6000.5.6f1 projesi var (`Assets/`, `Packages/`, `ProjectSettings/`); kod yerleşimi `game/Assets/Scripts/[Foundation|Core|Feature|Presentation]/` (repo `src/` eşlemesi — bu story'nin yapısal kararı)
- [ ] URP yapılandırılmış (URP asset + Renderer; HDRP yok), boş test sahnesi render alıyor
- [ ] Paketler: Input System (aktif backend; legacy kapalı), Universal RP, Addressables, Unity Test Framework
- [ ] `game/.gitignore` Unity standardı (Library/, Temp/, Logs/, obj/, UserSettings/)
- [ ] Proje ayarları: Linear color space, PC standalone hedef; Api Compatibility Level .NET Standard 2.1
- [ ] Boş sahne hedef donanımda 60fps (smoke)

## Implementation Notes

- Konum kararı: tek self-contained proje `game/`; prototipler `prototypes/`'ta izole kalır (mevcut greybox demo'ya dokunulmaz).
- "Gameplay" action map asset'i burada BOŞ oluşturulabilir; gerçek eylemler FPC epic'inde (TR-fpc-015).
- CI (`.github/workflows/tests.yml`) proje kökünü otomatik bulur; game-ci `projectPath` gerekirse `game` olarak eklenir — story-002'de doğrulanacak.

## Out of Scope

- Story 002: asmdef'ler + ilk test + CI
- Story 004: persistent sahneler

## QA Test Cases

*(QL-STORY-READY atlandı — qa-lead agent'ı mevcut değil; spec'ler ADR/tech-prefs doğrulama maddelerinden derlendi.)*

- **Manual check**: proje açılışı
  - Setup: `game/` Unity Hub'da 6000.5.6f1 ile aç
  - Verify: konsol hatasız; URP sahnesi render; Package Manager'da 4 paket
  - Pass condition: sıfır hata/uyarı (ilk import uyarıları hariç), boş sahne Stats'ta 60fps+
- **Manual check**: legacy input kapalı
  - Setup: Project Settings → Player → Active Input Handling
  - Verify: "Input System Package (New)" seçili
  - Pass condition: `Input.GetKey` çağrısı derlemede exception path'ine düşer (legacy kapalı)

## Test Evidence

**Story Type**: Config/Data → smoke check kaydı `production/qa/smoke-2026-08-09.md`
**Status**: [x] Created — PASS (6/6 check)

## Dependencies

- Depends on: None (zincirin ilk halkası)
- Unlocks: Story 002

## Completion Notes

**Completed**: 2026-08-09
**Criteria**: 6/6 passing (AC-2/3/6 manuel onay + otomatik kontrol; smoke kaydı `production/qa/smoke-2026-08-09.md`)
**Deviations**: ADVISORY — engine re-pin 6000.3.0f1 → 6000.5.6f1 (kullanıcı kararı; canlı dokümanlar senkronlandı, tarihî kayıtlar bilerek eski sürümde — bkz. VERSION.md re-pin notu). Unity'nin otomatik ürettiği `DefaultVolumeProfile.asset` + `UniversalRenderPipelineGlobalSettings.asset` Assets kökünde; `Assets/Editor/ProjectInitSetup.cs` tek seferlik kurulum betiği, silinebilir.
**Test Evidence**: Config/Data → smoke check PASS (`production/qa/smoke-2026-08-09.md`)
**Code Review**: Skipped — LP-CODE-REVIEW gate'i için `lead-programmer` subagent mevcut değil; runtime kodu yok (yalnız config + tek seferlik editör betiği), proje emsalindeki gate-skip kayıtlarıyla tutarlı.
