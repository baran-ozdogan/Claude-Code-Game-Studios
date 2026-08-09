# Test Evidence — Story 005: UIRoot + MainUI.uxml İskeleti

> **Story**: `production/epics/proje-kurulumu/story-005-uiroot-mainui-iskeleti.md`
> **Tarih**: 2026-08-09
> **Tür**: UI (ADVISORY) + stale-Instance PlayMode testi (otomatik)

## Otomatik doğrulama (CLI, PlayMode 6/6)

| Kontrol | Yöntem | Sonuç |
|---|---|---|
| `UIRoot.Instance` ikinci simüle oturumda taze ve geçerli, duplicate-guard sessiz | `uiroot_stale_instance_test.cs::UIRoot_SecondSimulatedSession_InstanceIsFreshAndValid` | PASS |
| 5 adlı öğe ağaçta birebir adlarla (`crosshair-container`, `crosshair`, `hold-fill-ring`, `stinger-caption`, `dialogue-subtitle`) | `MainUI_SkeletonElements_ExistAndStartHidden` | PASS |
| Üç üst-seviye öğe görünmez başlıyor (`display: none` — USS + UXML inline) | aynı test, resolvedStyle poll'lu | PASS |
| USS bağlanmış (`MainUI.uss` → UXML stylesheet listesi) ve önekli sınıflar öğelerde | editör teşhis çıktısı (2026-08-09, batch log) | PASS |
| PanelSettings bağlı (ScaleWithScreenSize, 1920×1080 referans) | `Story005UISetup` batch log + UIDocument serileştirmesi | PASS |

## Manuel görsel doğrulama (AC-4 — kullanıcı)

| Kontrol | Adım | Sonuç |
|---|---|---|
| 1080p'de Game görünümünde hiçbir öğe görünmüyor; UI Toolkit Debugger'da 4 adlı öğe ağaçta | Play → Window > UI Toolkit > Debugger | [ ] Approved |
| 1440p'de aynı doğrulama (Game view çözünürlük preset'i değiştirerek) | Game view çözünürlük menüsü → 2560×1440 | [ ] Approved |

*(Solo geliştirici: iki satır da Baran tarafından imzalanır. Öğeler bilinçli görünmez —
"görünüm" doğrulaması ağacın Debugger'da doğru, ekranın boş olduğunu görmektir.)*

## Sign-off

| Rol | Kişi | Tarih | Onay |
|---|---|---|---|
| Geliştirici (otomatik kanıt) | Claude (CLI koşuları) | 2026-08-09 | [x] Approved |
| Görsel QA (manuel tablo) | Baran | — | [ ] Approved |
