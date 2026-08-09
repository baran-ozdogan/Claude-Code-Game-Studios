# Story 006: Paylaşılan IPreprocessBuildWithReport doğrulama utility'si

> **Epic**: Proje Kurulumu
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: `ani-tetikleyici-etkilesim.md` (desenin kaynak GDD'si) + tüm check-sahibi sistemler
**Requirement**: — (ev sahibi altyapı; check'lerin TR'ları kendi epic'lerinde: TR-anlati-008, TR-isik-016/021, TR-gorev-018, TR-ani-tetik-007/010, TR-diyalog-005, TR-fpc-016)
**ADR Governing Implementation**: ADR-0014 (mekanizmanın en net tarifi — primary); ADR-0007/0012/0013/0015 (check katkıcıları — secondary)
**ADR Decision Summary**: TEK paylaşılan `IPreprocessBuildWithReport` editör utility'si; asset-scan (`AssetDatabase.FindAssets`) + ayrı sahne-scan adımı (`EditorBuildSettings.scenes` iterasyonu); ihlalde `BuildFailedException`. Her sistem kendi check'ini bu eve KAYDeder — asla ikinci bağımsız implementasyon.
**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `FindAssets` dosya-başına GUID döndürür — sub-asset yasağı konvansiyonu; EditMode fixture'ları `CreateInstance` (aksi build'i test verisiyle kırar — ADR-0014 kaydı).

**Control Manifest Rules (bu katman)**:
- Required: tek paylaşılan utility; BuildFailedException (asla runtime clamp); pointed hata mesajları (offending asset adlı)
- Forbidden: dördüncü bağımsız IPreprocessBuildWithReport; on-disk test fixture'ı
- Guardrail: sahne-scan pahalı adım — yalnız sahne-bağımlı check'ler için, asset-scan'den ayrı

## Acceptance Criteria

- [ ] `game/Assets/Editor/BuildValidation/` altında tek `IPreprocessBuildWithReport` implementasyonu; `callbackOrder` tanımlı
- [ ] Check-kayıt deseni: `IBuildCheck` benzeri küçük arayüz (Name, Phase: AssetScan|SceneScan, Run(context)) + statik kayıt listesi — sistem epic'leri buraya ekler
- [ ] Asset-scan ve sahne-scan iki ayrı faz olarak akıyor; sahne-scan `EditorBuildSettings.scenes` üzerinden açıp-kapatıyor
- [ ] Herhangi bir check ihlali `BuildFailedException` ile build'i durduruyor; mesaj offending asset/sahne yolunu içeriyor
- [ ] Kalıp testi: sahte bir check kayıtlıyken throws/doesn't-throw çifti EditMode'da geçiyor (fixture'lar `CreateInstance`)
- [ ] README/yorum: hangi sistem hangi check'i ekleyecek (ADR referanslı liste) — sistem epic'lerinin kayıt noktası belli

## Implementation Notes

- Gerçek check'ler BURADA YAZILMAZ — yalnız çatı + bir örnek/sahte check. İlk gerçek check muhtemelen anlati epic'inden gelir (boş requiredShiftIds).
- `report.SummarizeErrors` yerine doğrudan `BuildFailedException` — GDD'nin kendi tarif ettiği mekanizma.

## Out of Scope

- Sistem check'leri (ADR-0007'nin 3'lüsü, ADR-0013'ün TaskList seti, ADR-0014'ün 6'lısı, ADR-0012/0015'inkiler) — kendi epic'lerinde

## QA Test Cases

- **AC-5 (otomatik, EditMode)**:
  - Given: kayıtlı sahte check (koşula göre fail/pass)
  - When: utility'nin Run akışı test-harness üzerinden çağrılır
  - Then: fail koşulunda `BuildFailedException` (mesajda check adı + hedef yol); pass koşulunda sessiz
  - Edge cases: sıfır kayıtlı check → no-op; iki fazlı check karışımında faz sırası AssetScan→SceneScan
- **AC-3 (otomatik)**: sahne-scan izolasyonu
  - Given: yalnız AssetScan check'leri kayıtlı
  - Then: hiçbir sahne açılmıyor (pahalı adım atlanıyor)

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/build_validation_harness_test.cs` (8 test)
**Status**: [x] Created — EditMode 15/15 (CLI)

## Dependencies

- Depends on: Story 002
- Unlocks: anlati/isik/gorev/ani-tetik/diyalog/sahne-kesme epic'lerinin doğrulama story'leri; TR-fpc-016 decoy check'i

## Completion Notes

**Completed**: 2026-08-09
**Criteria**: 6/6 (`BuildValidationRunner : IPreprocessBuildWithReport` tek implementasyon, `callbackOrder=0`; `IBuildCheck`+`BuildCheckPhase`+`BuildCheckContext.Fail` deseni; iki faz `RunAll`'da ayrık, sahne-scan `IBuildSceneWalker` soyutlamasıyla `EditorBuildSettings.scenes` üzerinden; ihlal `BuildFailedException` + offending yol; 8'li throws/doesn't-throw harness testi fake'lerle; README + registry TODO listesi ADR referanslı)
**Deviations**: ADVISORY — `BuildValidation.asmdef` (editor-only) eklendi: EditModeTests custom asmdef olduğundan Assembly-CSharp-Editor'a referans veremiyor, test edilebilirlik için zorunlu. Sahne açma `IBuildSceneWalker` arayüzü arkasında (AC-3'ün "hiçbir sahne açılmıyor" testi ancak böyle fake'lenebiliyor); production yolu `EditorBuildSceneWalker`.
**Test Evidence**: EditMode 15/15 CLI (`build_validation_harness_test.cs` 8 test)
**Code Review**: Skipped — gate subagent'ları mevcut değil (emsal kayıtlı)
