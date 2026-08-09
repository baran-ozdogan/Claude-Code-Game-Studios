# Story 005: UIRoot + MainUI.uxml iskeleti

> **Epic**: Proje Kurulumu
> **Status**: Complete
> **Layer**: Foundation
> **Type**: UI
> **Estimate**: S (~2h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: `design/gdd/etkilesim-sistemi.md` / `adaptif-ses-sistemi.md` / `diyalog-anlati-icerigi-2026-08-02.md` (4 UI öğesinin sahipleri) — bu story yalnız İSKELETİ kurar
**Requirement**: TR-etkilesim-006/009, TR-ses-016, diyalog altyazısı — **davranışları kendi epic'lerinde**; burada yalnız adlandırılmış boş öğeler
**ADR Governing Implementation**: ADR-0002: UI Framework — UI Toolkit (primary); ADR-0010 (`UIRoot.Instance` şekli — secondary)
**ADR Decision Summary**: Tek paylaşılan `UIDocument`, persistent UI sahnesi, 4 adlı alt-ağaç; erişim `UIRoot.Instance` static accessor; sahip başına USS öneki; GDD-kilitli animasyonlar C#'ta (bu story'de animasyon yok).
**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW-MEDIUM (UI Toolkit ekosistem kayması — ADR-0002)
**Engine Notes**: `PanelSettings` referans çözünürlüğü PC masaüstü aralığında bir kez görsel doğrulanmalı (ADR-0002 Verification).

**Control Manifest Rules (bu katman)**:
- Required: UI Toolkit; UIRoot.Instance; önekli USS sınıfları; OnEnable-cached null-checked Q<> sorguları (tüketiciler için şablon)
- Forbidden: UGUI/Canvas/TMPro; GameObject.Find
- Guardrail: retained-mode — yalnız değişen öğe güncellenir

## Acceptance Criteria

- [ ] `UIRoot` MonoBehaviour'ı UI sahnesinin kökünde: `Instance` (Awake-set, unconditional LogError+Destroy duplicate guard), `Root => _uiDocument.rootVisualElement`
- [ ] `MainUI.uxml`: `#crosshair-container`(`#crosshair`+`#hold-fill-ring`), `#stinger-caption`, `#dialogue-subtitle` — hepsi boş/görünmez başlangıç durumunda
- [ ] `MainUI.uss`: `etkilesim-*`, `ses-*`, `diyalog-*` önek iskeletleri; a11y §2a/2b taban belirteçleri (font tabanı, konumlar) yorumlu placeholder olarak
- [ ] PanelSettings asset'i bağlı; 1080p ve 1440p'de öğe konumları görsel doğrulandı
- [ ] Reload-Scene-off iki-oturum PlayMode testi: `UIRoot.Instance` ikinci oturumda stale değil (ADR-0010 Validation Criteria kalıbı)

## Implementation Notes

- Bu story hiçbir davranış yazmaz — crosshair durum geçişleri Etkileşim (Core) epic'inin, caption gösterimi Ses epic'inin işi. Buradaki tek mantık UIRoot accessor'ı.
- Gate koşulu #2 (`design/ux/hud.md`) bu story'yle birlikte kapatılabilir: 4 öğenin yerleşimini tek sayfada belgeleyen kısa spec — dev-story sırasında yaz, evidence'a bağla.

## Out of Scope

- Crosshair/fill davranışı (etkilesim epic'i), caption zamanlaması (ses epic'i), altyazı akışı (diyalog epic'i)

## QA Test Cases

- **Manual check**: iskelet görünümü
  - Setup: Play; UI sahnesi boot'ta yüklü
  - Verify: Game görünümünde hiçbir öğe görünmüyor (hepsi hidden başlangıç); UI Toolkit Debugger'da 4 adlı öğe ağaçta
  - Pass condition: adlar birebir (`crosshair`, `hold-fill-ring`, `stinger-caption`, `dialogue-subtitle`); önekli USS sınıfları yüklü
- **AC-5 (UnityTest)**: stale-Instance
  - Given: Reload Scene OFF, iki oturum
  - Then: `UIRoot.Instance.Root` ikinci oturumda geçerli; duplicate-guard tetiklenmedi

## Test Evidence

**Story Type**: UI → `production/qa/evidence/uiroot-iskelet-evidence.md` + stale-Instance PlayMode testi (`uiroot_stale_instance_test.cs`, 2 UnityTest)
**Status**: [x] Created — PlayMode 6/6 CLI; evidence'ta manuel görsel satırları (1080p/1440p) kullanıcı imzası bekliyor (ADVISORY)

## Dependencies

- Depends on: Story 004
- Unlocks: etkilesim (Core), adaptif-ses caption story'si, diyalog altyazı story'si; gate koşulu #2 (`hud.md`)

## Completion Notes

**Completed**: 2026-08-09
**Criteria**: 5/5 — AC-4'ün görsel yarısı ADVISORY manuel imza bekliyor (evidence tablosu). (`UIRoot` ADR-0010'un birebir şekli, UI sahnesi kökünde, `_uiDocument` SerializeField wire'lı; `MainUI.uxml` 4 adlı öğe + inline `display:none` başlangıç; `MainUI.uss` önekli iskelet + a11y §2a/2b placeholder'ları; `MainPanelSettings` ScaleWithScreenSize 1920×1080 + tema TSS; stale-Instance iki-oturum testi geçiyor)
**Deviations**: ADVISORY — başlangıç gizliliği USS'e ek olarak UXML inline `style="display: none"` ile de yazıldı ve test resolvedStyle'ı poll'luyor (stil çözümlemesi asenkron; ilk koşular bu yüzden kırmızıydı). Gate koşulu #2 kapatıldı: `design/ux/hud.md` yazıldı. `Story005UISetup.cs` tek seferlik (silinebilir; `ProjectInitSetup.cs`/`Story004SceneSetup.cs` ile birlikte temizlik adayı).
**Test Evidence**: PlayMode 6/6 CLI + `production/qa/evidence/uiroot-iskelet-evidence.md`
**Code Review**: Skipped — gate subagent'ları mevcut değil (emsal kayıtlı)
