# Story 004: DoSwap üçlüsü — anchor repozisyonu, RenderSettings senkronu, ertelenmiş unload

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (Core Rules — "Swap ile unload ayrımı", "RenderSettings/lightmap stratejisi"; Formulas — "Koordinat çerçevesi hizalama kuralı")
**Requirement**: `TR-sahne-gecisi-003` (SOFT co-residency + anchor), `TR-sahne-gecisi-005` (ertelenmiş unload), `TR-sahne-gecisi-013` (RenderSettings senkronu)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0008 (primary — `DoSwap`, `DelayedUnload`); ADR-0003 (secondary — `FirstPersonController.RepositionTo` göreli aşırı yüklemesi); ADR-0015 (secondary — boot-time `RequestSoftTransition(null, ...)` null-guard'ı)
**ADR Decision Summary**: `Swapping` adımı **YALNIZCA** `SetActiveScene`'dir (senkron, sıfır-kare). `UnloadSceneAsync` bu adımın parçası DEĞİLDİR — `Complete`'ten 0.5-2s sonra ayrı bir arka plan coroutine'i olarak başlar. `DoSwap` ayrıca SOFT için anchor kopyalamayı ve her iki tür için RenderSettings senkronunu yapar.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: MEDIUM
**Engine Notes**: `SetActiveScene`, hedef Scene yüklü/geçerli değilse **exception atmaz, `false` DÖNER** — AC-9'un tüm garantisi bu çağrının başarısına dayandığı için dönüş değeri AÇIKÇA kontrol edilmelidir (ADR-0008 unity-specialist bulgusu). `WaitForSeconds` `Time.timeScale == 0` iken durur; MVP'de pause menüsü yok ama bir gün eklenirse `WaitForSecondsRealtime` düzeltmesi gerekir (ADR-0008 Risks).

**Control Manifest Rules (bu katman)**:
- Required: sahne swap'ını hayatta geçirmesi gereken nesneler persistent sahnelerde
- Forbidden: `UnloadSceneAsync`'i `Swapping` adımına dahil etmek (sıfır-kare garantisini bozar)
- Guardrail: ertelenmiş unload `Complete`'ten 0.5-2s sonra, fire-and-forget, `Idle`'a dönüşü asla bloklamaz

---

## Acceptance Criteria

- [ ] `DoSwap(fromScene, toScene, type)` sırası: (SOFT ise) anchor repozisyonu → RenderSettings senkronu → `SetActiveScene` → dönüş değeri kontrolü → `SetState(Swapping, type)`. `SetActiveScene` `false` dönerse `DoSwap` `false` döner ve çağıran `Failed` yoluna girer
- [ ] **Anchor repozisyonu YENİDEN YAZILMAZ, DEVREDİLİR**: `SceneTransitionManager` her iki sahnedeki `SoftTransitionAnchor`'ı bulur ve `FirstPersonController.RepositionTo(fromAnchor.transform, toAnchor.transform)` göreli aşırı yüklemesini TAM BİR KEZ çağırır. Kabin-yerel offset/rotasyon korunumunun matematiği FPC epic'inde zaten uygulandı ve test edildi — burada tekrar türetilmez *(QL-STORY-READY bulgusu: "katmanlar aşağı okur", ve `fpc_persistent_scene_test.cs` bu matematiği zaten pinliyor)*
- [ ] **`fromScene == null` boot vakası**: ADR-0015 boot'ta `RequestSoftTransition(null, ...)` çağırıyor — kaynak anchor YOKTUR. `RepositionTo` bu durumda HİÇ ÇAĞRILMAZ ve hata/uyarı üretilmez; başlangıç oyuncu yerleşimi ADR-0015'in `InitialSpawnAnchor`'ının işi, `onComplete` içinde uygulanır
- [ ] **RenderSettings senkronu ALAN ALAN belirlenir**: `SyncRenderSettingsFromSceneEnvironmentSettings(toScene)`, hedef sahnenin yazarlı `SceneEnvironmentSettings` bileşeninden okuyup şu alanları yazar: `RenderSettings.skybox`, `RenderSettings.ambientMode`, ve `ambientMode`'un ima ettiği ambient alanları (`ambientLight` Flat için; `ambientSkyColor`/`ambientEquatorColor`/`ambientGroundColor` Trilight için), artı `ambientIntensity`. **Alan listesi implementasyondan ÖNCE bu story'de sabitlenir, implementer yorumuna bırakılmaz** *(QL-STORY-READY bulgusu: "skybox + ambient" ifadesi tek başına altı alanlık bir yüzeyi belirsiz bırakıyor)*
- [ ] Baked lightmap verisi sahne başına AYRI kalır — asla birleştirilmez, `LightmapSettings.lightmaps`'e dokunulmaz (GDD Core Rules; "paylaşılan Environment sahnesi" fikri terk edildi)
- [ ] **`UnloadSceneAsync` `Swapping`'in parçası DEĞİLDİR**: `Complete`'e ulaşıldıktan sonra, `Idle`'a dönüşü BLOKLAMADAN, 0.5-2s gecikmeyle ayrı bir coroutine'de başlar (fire-and-forget — hiçbir şey onu beklemez)
- [ ] `DelayedUnload` yalnız SOFT yolunda tetiklenir — `fromScene == null` olan HARD CUT'ta hiç çağrılmaz

---

## Implementation Notes

*ADR-0008'in `DoSwap`/`DelayedUnload` taslağı doğrultusunda:*

- **`SceneEnvironmentSettings`** bu story'de doğan yeni bir `MonoBehaviour`'dur: her alan sahnesi kendi kopyasını taşır, Inspector'dan yazarlanır. Senkron onu hedef sahnede arar; bulunamazsa `Debug.LogWarning` basılır ve `RenderSettings` DEĞİŞTİRİLMEZ (sessiz varsayılan yazmak, yazarın eksik bileşeni fark etmesini engellerdi).
- `SoftTransitionAnchor` da bu story'de doğan bir işaretçi bileşendir (`MonoBehaviour`, veri taşımaz — yalnız Transform'u önemli). Her SOFT hedef sahnesi bir tane taşır.
- Anchor arama sahne-kapsamlıdır: `SceneManager.GetSceneByName(x).GetRootGameObjects()` üzerinden aramak, `FindObjectsByType`'ın sahne-körlüğünden kaçınır — iki sahne co-resident'ken hangi anchor'ın hangi sahneye ait olduğu KRİTİKtir.
- `DelayedUnload` gecikmesi bir Tuning Knob alanıdır (0.5-2s güvenli aralık), sabit değil.

---

## Out of Scope

- Geçiş dizisinin kendisi (Story 003 — bu story onun `DoSwap` adımını doldurur)
- HARD CUT'ın Ready fast-path'i (Story 005) — ama o path AYNI `DoSwap`'ı çağıracak, bu yüzden bu story'nin çıktısı onun ön koşuludur
- `FirstPersonController.RepositionTo`'nun kendi matematiği (FPC epic'i, tamamlandı)
- ADR-0015'in `InitialSpawnAnchor`'ı — bu story yalnız null-guard'ı kurar, spawn'ı uygulamaz

---

## QA Test Cases

- **AC-1 (otomatik, PlayMode)**: Anchor devri
  - Given: kaynak ve hedef sahnelerin ikisinde de `SoftTransitionAnchor`, oyuncu kaynak kabinde
  - When: `DoSwap` bir SOFT geçişte koşar
  - Then: `FirstPersonController.RepositionTo(fromAnchor, toAnchor)` göreli aşırı yüklemesi TAM BİR KEZ, doğru transform'larla çağrılır (bir gözcü/spy ile doğrulanır — kabin-yerel offset matematiği YENİDEN TÜRETİLMEZ, o `fpc_persistent_scene_test.cs`'in işi)
  - Edge cases: mutlak aşırı yüklemenin (`RepositionTo(Transform)`) çağrılması testi KIRMALI — yanlış aşırı yükleme yerel offset'i siler

- **AC-2 (otomatik, PlayMode)**: Boot null-guard'ı
  - Given: `fromScene == null` (ADR-0015 boot çağrısı)
  - When: `DoSwap` koşar
  - Then: `RepositionTo` HİÇ çağrılmaz, hiçbir hata/uyarı basılmaz, swap normal tamamlanır
  - Edge cases: `LogAssert.NoUnexpectedReceived()` ile sessizlik de assert edilir

- **AC-3 (otomatik, PlayMode)**: RenderSettings alan alan
  - Given: hedef sahnede `SkyboxMaterial` + `AmbientMode.Trilight` + üç ambient rengi + yoğunluk yazarlı bir `SceneEnvironmentSettings`
  - When: swap tamamlanır
  - Then: `RenderSettings.skybox`, `ambientMode`, `ambientSkyColor`, `ambientEquatorColor`, `ambientGroundColor`, `ambientIntensity` alanlarının HEPSİ bileşendeki değerlerle birebir eşleşir
  - Edge cases: `AmbientMode.Flat` yazarlı ikinci bir vaka `ambientLight`'ı doğrular; `SceneEnvironmentSettings` HİÇ YOKSA `Debug.LogWarning` basılır ve `RenderSettings` değişmeden kalır (sessiz varsayılan YAZILMAZ)

- **AC-4 (otomatik, PlayMode)**: Swap/unload ayrımı
  - Given: SOFT geçiş
  - When: `CurrentState` `Idle`'a ulaşır
  - Then: o anda kaynak sahne HÂLÂ yüklüdür (`isLoaded == true`) — unload `Idle`'ı beklemedi/bloklamadı; gecikme sonrası kaynak sahne unload edilir
  - Edge cases: `SetActiveScene` ile aynı karede unload eden bir implementasyon testi kırmalı — bu, GDD'nin çelişkiyi gidermek için özellikle yazdığı kuraldır

- **AC-5 (otomatik, PlayMode)**: `SetActiveScene` dönüş değeri
  - Given: yüklü OLMAYAN bir hedef sahne adı `DoSwap`'a verilir
  - Then: `DoSwap` `false` döner ve `Swapping` durumu HİÇ yayınlanmaz
  - Edge cases: dönüş değerini yok sayan bir implementasyon `Swapping`→`Complete` yayınlar ve testi kırar

---

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/sahne_gecisi_doswap_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003 (`RunTransition` dizisi — `DoSwap` onun içinde çağrılır)
- Unlocks: Story 005 (HARD CUT'ın Ready fast-path'i AYNI `DoSwap`'ı doğrudan çağırır)
