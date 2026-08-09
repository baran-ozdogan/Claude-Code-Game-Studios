# Story 004: Persistent sahneler + boot yükleyici

> **Epic**: Proje Kurulumu
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: — (altyapı; sözleşmeler ADR'lerde)
**Requirement**: — (ADR-0015 boot sözleşmesi; ADR-0002/0003 persistent-sahne deseni)
**ADR Governing Implementation**: ADR-0015 (boot sözleşmesi — primary); ADR-0002, ADR-0003 (secondary)
**ADR Decision Summary**: Build'in initial load set'i YALNIZ persistent sahneler (UI, Player, Foundation); boot UI→Player'ı sıralı-awaited yükler; depot asla initial set'te değil — yalnız gece-başı setup'ından sonra controller çağrısıyla yüklenir.
**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `SceneManager.LoadSceneAsync(Additive)` stabil API; "Reload Scene: Off" davranışı için boot her gerçek oturumda persistent sahneleri taze yüklemeyi garanti etmeli (ADR-0003 riski).

**Control Manifest Rules (bu katman)**:
- Required: persistent-sahne deseni (asla DontDestroyOnLoad); boot sözleşmesi; iki-oturum test kalıbı
- Forbidden: `DontDestroyOnLoad`; initial set'te level sahnesi
- Guardrail: boot yük maliyeti sub-frame sınıfı (3 küçük sahne)

## Acceptance Criteria

- [ ] `UI.unity`, `Player.unity`, `Foundation.unity` sahne asset'leri var (içerikleri iskelet: UI'da UIRoot objesi [story-005], Player'da boş Player kökü [FPC epic'i doldurur], Foundation'da boş yönetici kökü)
- [ ] Build Settings scene list: yalnız bu 3 sahne + (sonradan eklenecek level sahneleri işaretli-değil politikası belgeli)
- [ ] Boot akışı: ilk sahne (UI) açılışta; küçük bir boot script'i Player ve Foundation'ı sıralı-awaited additive yükler (UI→Player sırası ADR-0003'ün geçici kararı)
- [ ] Depot/level yükleme çağrısı YOK — o çağrı ADR-0015'in controller'ına ait (sahne-kesme epic'i); boot script'inde açık yorumla belgeli
- [ ] Integration testi: Play başlangıcında 3 persistent sahne yüklü, başka sahne yok
- [ ] İki-oturum kalıbı: Reload Scene OFF ile iki ardışık Play'de sahneler taze yükleniyor (stale kök objesi yok)

## Implementation Notes

- Boot script'i minimal bir MonoBehaviour (UI sahnesinin kökünde) ya da ilk sahnenin kendi yükleyicisi — ADR-0003'ün "sequentially awaited" ifadesi coroutine `while(!op.isDone)` kalıbıyla (proje idiomu, ADR-0008 ile tutarlı; `Awaitable` yasak-listede).
- Bu story ADR-0015'in tam gece-başı orkestrasyonunu KURMAZ — yalnız persistent katmanı ayağa kaldırır.

## Out of Scope

- Story 005: UIRoot içeriği
- sahne-gecisi epic'i: SceneTransitionManager'ın kendisi
- sahne-kesme (Feature): gece-başı + depot yüklemesi

## QA Test Cases

- **AC-5 (otomatik, PlayMode)**:
  - Given: build settings 3 persistent sahne
  - When: Play başlar, boot tamamlanır
  - Then: `SceneManager.sceneCount == 3`, adlar {UI, Player, Foundation}, aktif sahne tanımlı
  - Edge cases: çift boot çağrısı ikinci kez yüklememeli (idempotent)
- **AC-6 (UnityTest, Reload Scene OFF)**:
  - Given: iki ardışık simüle oturum
  - Then: her oturumda sahneler taze; kök objeler duplicate-guard loglamıyor

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/boot_persistent_scenes_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 002 (test altyapısı), Story 003 (ResetAll zamanlama referansı)
- Unlocks: Story 005; FPC ve sahne-gecisi epic'leri
