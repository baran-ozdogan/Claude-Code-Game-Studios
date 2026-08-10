# Story 003: SOFT geçiş dizisi + gerçek %100 yükleme

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (Core Rules — "SOFT'un gerçek tamamlanma garantisi Duration knob'undan bağımsızdır"; States and Transitions; AC-1)
**Requirement**: `TR-sahne-gecisi-001` (dizi), `TR-sahne-gecisi-002` (API sözleşmesi), `TR-sahne-gecisi-014` (movement-lock'a dokunmaz)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

> **TR-KAYIT BOŞLUĞU (QL-STORY-READY bulgusu, 2026-08-10)**: GDD'nin en çok
> vurguladığı kurallardan biri — **"SOFT geçiş minimum süresi (2-8s) bir
> TABAN/pacing değeridir, bir tamamlanma tetikleyicisi DEĞİL"** — hiçbir TR-ID'ye
> sahip değil. `TR-sahne-gecisi-006` bu boşluğu kapatmaz: o kayıt açıkça
> `PreloadHardCut`/HARD CUT hakkındadır, "soft yarısı" diye bir şey yoktur.
> Bu story kuralı yine de test eder ve `TR-sahne-gecisi-001`'e demirler;
> `/architecture-review`'ın bir sonraki turunda kendi ID'sini mint etmesi için
> boşluk buraya kaydedildi. **Uydurma bir "TR-006 soft yarısı" atfı YAPMAYIN.**

**ADR Governing Implementation**: ADR-0008 (primary — "Zero-frame swap and the deferred unload", `RunTransition`)
**ADR Decision Summary**: `RunTransition`, `TransitionType` ile PARAMETRELENMİŞ TEK bir coroutine'dir — SOFT ve HARD CUT'ın senkron-bekleme fallback'i aynı gövdeyi paylaşır (GDD AC-2'nin "ayrı kod yolu yok" garantisi). `LoadSceneAsync(Additive)` `allowSceneActivation` VARSAYILAN (true) ile koşar ve gerçek `isDone`'a kadar beklenir.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: MEDIUM
**Engine Notes**: `allowSceneActivation=false` + ~%90'da bekletme deseni **YASAK** — hedef sahnenin tüm `Awake`/`Start` maliyeti `Ready`'ye ulaşılmadan ödenmelidir, yoksa sıfır-kare swap garantisi bozulur. Testler için gerçek additive yüklenebilir bir sahne gerekir; `Assets/Scenes/EmptyTest.unity` mevcut ve Build Settings'te olmayan bir sahnenin `LoadSceneAsync`'i başarısız olur — bu, Story 007'nin `Failed` yolu için de kullanılabilecek doğal bir negatif fixture'dır.

**Control Manifest Rules (bu katman)**:
- Required: durum makinesi mantığı BLOCKING birim testli; boot contract — build'in ilk yükleme kümesi YALNIZ persistent sahneleri içerir
- Forbidden: `allowSceneActivation=false`/~%90 bekletme deseni (manifest Performance Guardrails, ADR-0008 kaynaklı)
- Guardrail: ertelenmiş unload `Complete`'ten 0.5-2s sonra, fire-and-forget, `Idle`'ı asla bloklamaz (uygulaması Story 004)

---

## Acceptance Criteria

- [ ] `RequestSoftTransition(fromScene, toScene, config, onComplete, onFailed)` GDD'nin imzasıyla birebir; `Idle`'dayken `RunTransition`'ı `TransitionType.Soft` ile başlatır
- [ ] **GDD AC-1**: `OnTransitionStateChanged`, hiçbir durum atlanmadan/sırası değişmeden TAM OLARAK `Preloading→Ready→Swapping→Complete→Idle` sırasıyla fırlar, her biri tam bir kez, her biri `TransitionType.Soft` taşıyarak
- [ ] `Preloading→Ready` geçişi **her zaman** gerçek `LoadSceneAsync` tamamlanmasını (`isDone`) bekler; `allowSceneActivation` varsayılan (true) kalır — ~%90 bekletme deseni kullanılmaz
- [ ] **Duration knob bir tamamlanma kapısı DEĞİLDİR**: "SOFT geçiş minimum süresi" bir taban/pacing değeridir; süre dolsa bile yükleme bitmeden `Ready`'ye geçilmez, ve yükleme erken bitse bile `Ready` gecikmez *(GDD Core Rules; bu kuralın kendi TR-ID'si yok — yukarıdaki kayıt boşluğu notuna bakın)*
- [ ] `RunTransition` `TransitionType`-GENERİK yazılır — SOFT varsayımları gövdeye gömülmez, çünkü Story 005'in HARD CUT senkron-bekleme fallback'i AYNI gövdeyi kullanacak (GDD AC-2)
- [ ] **`SceneTransitionManager` `RequestMovementLock`/`ReleaseMovementLock`'a HİÇ dokunmaz** (TR-014) — kilit yaşam döngüsü çağıranındır. *Doğrulama tek bir davranış testiyle değil, YAPISAL bir kontrolle yapılır: `SceneTransitionManager.cs` + `SceneTransitionState.cs` dosyalarının tamamında bu iki API adı geçmez.* Gerekçe: davranış testi yalnız bir kod yolunu kapsar, sonraki story'ler (005 HARD CUT, 007 Failed) aynı sınıfa yeni yollar ekliyor ve sessizce ihlal edebilir (QL-STORY-READY bulgusu)
- [ ] **Minimal `Failed` regresyon guard'ı**: yüklenemeyen bir hedef sahnede `CurrentState` `Failed`'e ulaşır ve otomatik `Idle`'a döner. *Tam istisna-güvenliği/callback-dışlayıcılık kapsamı Story 007'nin işidir* — bu AC yalnız, bu story'de sevk edilen `RunTransition` failure dalının Story 007'ye kadar test edilmeden kalmamasını sağlar

---

## Implementation Notes

*ADR-0008'in `RunTransition` taslağı doğrultusunda:*

- Sıra: `SetState(Preloading, type)` → `LoadSceneAsync(toScene, Additive)` → `while (!op.isDone) yield return null` → yükleme başarısızsa `Failed`+`onFailed`+`Idle` → `SetState(Ready, type)` → `DoSwap` → `Complete` → `onComplete` → `Idle`.
- `DoSwap`'ın kendisi Story 004'te gelir; bu story'de `SetActiveScene` çağrısı minimal hâliyle yapılabilir, ama **anchor kopyalama ve RenderSettings senkronu bu story'ye AİT DEĞİL** — Story 004 aynı metoda ekleyecek.
- Test fixture'ları çalışma zamanında oluşturulan sahneler yerine gerçek, Build Settings'teki bir sahneyi additive yükler; `EmptyTest.unity` bu iş için mevcut.
- Duration knob'u bir `SoftTransitionConfig` alanı olarak taşınır ama `Ready`'ye geçişi ETKİLEMEZ — testte knob'u absürt derecede küçük (0.01s) ve absürt derecede büyük (60s) verip her ikisinde de `Ready`'nin yalnız `isDone`'a bağlı olduğunu göstermek, bu kuralı ayırt edici şekilde pinler.

---

## Out of Scope

- `CopySoftTransitionAnchorTransform`, `SyncRenderSettingsFromSceneEnvironmentSettings`, `DelayedUnload` (Story 004 — hepsi `DoSwap`/post-`Complete` adımına eklenir)
- HARD CUT'ın hiçbir parçası: `PreloadHardCut`, ayrı preload durumu, Ready fast-path (Story 005)
- İkinci bir isteğin reddi/kuyruklanması (Story 006) — bu story yalnız `Idle`'dan başlayan mutlu yolu kurar
- `SafeInvoke`, `onComplete`/`onFailed` karşılıklı dışlayıcılığı, `Abrupt` (Story 007)

---

## QA Test Cases

- **AC-1 (otomatik, PlayMode)**: Tam dizi, gerçek sahne yüklemesiyle
  - Given: `Idle`'daki bir `SceneTransitionManager` (gerçek instance, `AddComponent`)
  - When: gerçek additive yüklenebilir bir hedef sahne için `RequestSoftTransition` çağrılır
  - Then: kaydedilen `(state, type)` tuple listesi TAM OLARAK `[(Preloading,Soft), (Ready,Soft), (Swapping,Soft), (Complete,Soft), (Idle,Soft)]`
  - Edge cases: son durumu değil TÜM DİZİYİ assert et — yalnız `Idle`'a bakan bir test atlanan bir durumu kaçırır

- **AC-2 (otomatik, PlayMode)**: Duration knob tamamlanmayı tetiklemez
  - Given: `SoftTransitionConfig`'in minimum süresi absürt küçük (0.01s)
  - When: geçiş koşar
  - Then: `Ready`, `LoadSceneAsync` `isDone` olmadan ÖNCE fırlamaz
  - Edge cases: aynı test absürt büyük süreyle (60s) tekrarlandığında `Ready` yine de yükleme biter bitmez fırlar — knob'un HİÇBİR yönde kapı olmadığını ikisi birlikte kanıtlar

- **AC-3 (otomatik, PlayMode)**: `allowSceneActivation` deseni
  - Given: geçiş sürüyor
  - Then: `Ready`'ye ulaşıldığında hedef sahne `SceneManager.GetSceneByName(toScene).isLoaded == true` — yani %100 yüklenmiş, ~%90'da bekletilmiş değil
  - Edge cases: `allowSceneActivation=false` kullanan bir implementasyonda sahne `Ready` anında `isLoaded == false` olur ve test kırılır

- **AC-4 (otomatik, EditMode)**: Movement-lock'a yapısal dokunmazlık
  - Given: `SceneTransitionManager.cs` ve `SceneTransitionState.cs` kaynak metinleri
  - Then: hiçbiri `RequestMovementLock` ya da `ReleaseMovementLock` adını içermez
  - Edge cases: yorum satırında geçmesi de sayılır (kasten katı — "belki ileride" yorumu bir sonraki geliştiriciyi davet eder); test mesajı TR-014'ü ve kilidin çağıranın sorumluluğu olduğunu adlandırmalı

- **AC-5 (otomatik, PlayMode)**: `Failed` minimal guard
  - Given: yüklenemeyen bir hedef sahne adı
  - When: `RequestSoftTransition` çağrılır
  - Then: dizi `Failed` ardından `Idle` içerir
  - Edge cases: tam callback sözleşmesi burada assert EDİLMEZ — Story 007'nin işi; bu test yalnız durumun soft-lock'lamadığını kanıtlar

---

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/sahne_gecisi_soft_test.cs` + `game/Assets/Tests/EditMode/sahne_gecisi_movement_lock_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (sürücü + facade)
- Unlocks: Story 004 (`DoSwap` bu dizinin içine üç sorumluluk ekler)
