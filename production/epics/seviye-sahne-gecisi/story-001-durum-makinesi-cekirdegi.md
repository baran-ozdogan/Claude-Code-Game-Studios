# Story 001: Saf durum makinesi çekirdeği

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (States and Transitions; Core Rules; AC-1'in dizi iddiası)
**Requirement**: `TR-sahne-gecisi-001`, `TR-sahne-gecisi-009`, `TR-sahne-gecisi-011`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0008 (primary — Data model, 2026-08-10 yerinde düzeltmesi)
**ADR Decision Summary**: `Idle→Preloading→Ready→Swapping→Complete→Idle` + `Failed` durum makinesi, `OnTransitionStateChanged(newState, type)` ile yayınlanır. **2026-08-10 kullanıcı kararı**: durum ve tüm hakemlik saf C# `SceneTransitionState`'te yaşar; `MonoBehaviour` yalnız sürücüdür.

> **AYRIM KARARI (kullanıcı, 2026-08-10) — SİLMEYİN**
> ADR-0008'in ilk taslağı alanları doğrudan `MonoBehaviour`'a koyuyordu ve bir
> Negative maddesinde bunu "testler `AddComponent` gerektirir" diye kaydediyordu.
> Manifest'in "saf C# çekirdek + ince sürücü ayrımı (BLOCKING)" kuralı bu ADR'dan
> SONRA yazıldı ve bölüm bazında *Core Layer* altında olduğu için sahne
> geçişlerini biçimsel olarak KAPSAMIYOR — yani bu bağlayıcı bir ihlal değil,
> bilinçli bir tercihti. Tercih edilme sebebi: sevk edilmiş emsaller aynı yönü
> gösteriyor (`ShiftZone` saf bir `ShiftProgressMachine` tutuyor; ADR-0011
> `ElevatorController`/`ElevatorStateMachine` ayrımını yapıyor) ve ayrım,
> ADR-0008'in kendi Validation Criteria'sının çoğunu `AddComponent` PlayMode
> testi olmaktan çıkarıp düz EditMode `[Test]`'e indiriyor.
> ADR-0008 story'ler yazılmadan ÖNCE yerinde düzeltildi (henüz hiç kod yoktu).
>
> **Bu, ADR-0001'in statik servis desenini geri getirmez**: `SceneTransitionState`
> sürücünün düz bir alanıdır (`ShiftZone` şekli), `ResetOnLoad()`'lı bir statik
> facade DEĞİL (`ElevatorSystem` şekli). Sistem ADR-0001'in belgelenmiş tek
> istisnası olmaya devam ediyor.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Bu story hiçbir Unity API'sine dokunmaz — tüm dosya saf C#. `UnityEngine` using'i bile gerekmez (`Debug.LogException` Story 007'de gelir). Testler düz `[Test]`, `AddComponent` YOK.

**Control Manifest Rules (bu katman)**:
- Required: durum makinesi mantığı BLOCKING birim testli; `SceneTransitionManager`'ın event'lerine abonelikler lazy (bu story abone üretmez ama sözleşmeyi kurar)
- Forbidden: `SceneTransitionManager.Instance`'a constructor ya da `ResetOnLoad()` içinden referans (kayıtlı forbidden pattern)
- Guardrail: —

---

## Acceptance Criteria

- [x] `TransitionState { Idle, Preloading, Ready, Swapping, Complete, Failed }` ve `TransitionType { Soft, Hard }` enum'ları ile `ISceneTransitionManager` arayüzü, GDD'nin "Dışa açılan arayüz" bölümüyle BİREBİR tanımlanır
- [x] `SceneTransitionState` saf C#'tır: hiçbir `UnityEngine` tipine bağlanmaz, düz `new SceneTransitionState()` ile kurulabilir
- [x] `SetState(newState, type)` `CurrentState`'i ve `_activeType`'ı yazar ve `OnTransitionStateChanged(newState, type)`'ı TAM BİR KEZ fırlatır; `type` her zaman o geçişi başlatan çağrının türüdür
- [x] Normal dizi hiçbir durumu ATLAMADAN ve sırası değişmeden ilerler: `Idle→Preloading→Ready→Swapping→Complete→Idle` (GDD AC-1'in dizi iddiasının saf-durum yarısı; gerçek sahne yüklemesiyle uçtan uca doğrulaması Story 003'te)
- [x] `Failed`, `onFailed` çağrıldıktan HEMEN SONRA otomatik olarak `Idle`'a döner — `Failed` "bu istek başarısız oldu" demektir, "yönetici kalıcı olarak bozuk" DEĞİL (GDD AC-11a; tek bozuk sahne referansı oturumu soft-lock'lamamalı)
- [x] `Failed→Idle` dönüşünden sonra durum makinesi yeni bir isteği normal kabul eder (kabul/ret hakemliğinin kendisi Story 006'da, burada yalnız `CurrentState`'in `Idle` olduğu doğrulanır)

---

## Implementation Notes

*ADR-0008 Data model (2026-08-10 düzeltilmiş hâli) doğrultusunda:*

- `SceneTransitionState` **karar verir, iş yapmaz**. Hakemlik metotları sürücüye ne YAPACAĞINI söyleyen bir talimat döner (`ElevatorStateMachine.TryCall()` şekli) — coroutine başlatmak, `SceneManager` çağırmak sürücünün işi. Bu story yalnız `SetState` + durum dizisi + `Failed` auto-return'ü kurar; `TryBeginSoft`/`TryBeginHard` hakemliği Story 006'da eklenir.
- `_hardCutPreloadState`, `_hardCutPreloadScene`, `_pendingHardCut` alanları bu story'de BEYAN EDİLİR ama kullanılmaz (Story 005/006 doldurur) — beyan etmek, `CurrentState`'ten ayrı oldukları sözleşmesini baştan sabitler.
- `OnTransitionStateChanged` ve `OnSoftTransitionRejected` düz C# event'leridir; sürücü bunları kendi public yüzeyine forward eder (Story 002).
- `internal` görünürlük + `AssemblyInfo.cs`'in `InternalsVisibleTo("EditModeTests")` satırı, testlerin çekirdeğe doğrudan erişmesini sağlar (proje emsali: `IsikVolumeState`, `AnlatiDurumState`).

---

## Out of Scope

- `MonoBehaviour` sürücü, `Instance` facade, persistent sahne barındırma (Story 002)
- Gerçek `LoadSceneAsync`/`SetActiveScene` — bu story hiçbir sahne yüklemez (Story 003+)
- Kabul/ret/kuyruk hakemliği (Story 006) — burada yalnız durumların SIRASI
- `SafeInvoke`, `onComplete`/`onFailed` sözleşmesi, `Abrupt` (Story 007)

---

## QA Test Cases

*(QL-STORY-READY koştu. Bu story'nin ilk hâli "ADR-0008'in kendi taslağı saf çekirdek beyan etmiyor" diye REDDEDİLDİ; kullanıcı kararıyla ayrım seçildi ve ADR story yazımından önce düzeltildi, böylece bu story artık gerçekten sevk edilecek koda karşı test yazıyor.)*

- **AC-1 (otomatik)**: Enum/arayüz sözleşmesi
  - Given: `ISceneTransitionManager`
  - Then: GDD'nin listelediği altı üye (`CurrentState`, `RequestSoftTransition`, `RequestHardCut`, `PreloadHardCut`, `GetCurrentHardCutAbrupt`, iki event) tam imzalarıyla mevcut; `TransitionState` tam altı değer taşır
  - Edge cases: fazladan bir enum değeri ya da eksik bir üye testi kırmalı

- **AC-2 (otomatik)**: Saf kurulabilirlik
  - Given: —
  - When: `new SceneTransitionState()` düz bir `[Test]` içinde çağrılır
  - Then: kurulur, `CurrentState == Idle`; tip hiçbir `UnityEngine` tipine bağlanmaz (reflection ile assembly bağımlılığı DEĞİL, testin kendisinin `AddComponent`'sız derlenip koşması bunu zaten kanıtlar)

- **AC-3 (otomatik)**: Event tam bir kez, doğru type ile
  - Given: `OnTransitionStateChanged`'e bağlı bir kaydedici
  - When: `SetState(Preloading, Soft)` çağrılır
  - Then: tam bir çağrı, `(Preloading, Soft)` payload'ıyla; `CurrentState == Preloading`
  - Edge cases: aynı duruma İKİNCİ kez `SetState` çağrısı yine fırlar (bastırma YOK — durum makinesi idempotent değildir, bu bilinçli)

- **AC-4 (otomatik)**: Dizi hiçbir durumu atlamaz
  - Given: `Idle`'daki taze bir çekirdek
  - When: dizi `Preloading→Ready→Swapping→Complete→Idle` olarak sürülür
  - Then: kaydedilen `(state, type)` listesi TAM OLARAK bu beş girdi, bu sırada
  - Edge cases: `Ready`'yi atlayan bir sürüş testi kırmalı (dizinin kendisi assert ediliyor, yalnız son durum değil)

- **AC-5 (otomatik)**: `Failed` otomatik `Idle`'a döner
  - Given: `Preloading`'deki bir çekirdek
  - When: başarısızlık sinyali verilir
  - Then: kaydedilen dizi `Failed` ARDINDAN `Idle` içerir, bu sırada; `CurrentState == Idle`
  - Edge cases: `Failed`'de kalıp `Idle`'a dönmeyen bir implementasyon testi kırmalı (soft-lock regresyonu — GDD bunu "üretim durdurucu boşluk" diye adlandırıyor)

---

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/sahne_gecisi_durum_makinesi_test.cs` (20 test, hepsi düz `[Test]`)
**Status**: [x] Oluşturuldu ve geçiyor — **EditMode 259/259, PlayMode 84/84** (2026-08-10)

---

## Completion Notes

### Ayrımın somut kazancı ölçüldü

**20 testin hepsi düz `[Test]`** — `AddComponent` yok, `[UnityTest]` yok. ADR-0008'in
ilk taslağıyla bunların tamamı PlayMode'a çıkacaktı. Testin buradan PlayMode'a
taşınması gerekmesi, ayrımın kaybolduğunun ilk işareti olur.

### Gate bulguları ve uygulananlar

**LP-CODE-REVIEW → CONCERNS** (hepsi giderildi):

- **`Fail`'in hook'u sarmalanmamıştı — gerçek bir soft-lock.** Callback fırlatırsa
  `SetState(Idle)` hiç koşmuyor ve durum makinesi kalıcı olarak `Failed`'de
  kalıyordu: GDD'nin "üretim durdurucu boşluk" dediği ve **bu story'nin var oluş
  sebebi olan** hatanın ta kendisi. Pencere Story 003-006 — sürücü, `SafeInvoke`
  daha yokken ham bir `onFailed` geçebilir. `try/finally` ile dönüş koşulsuz
  yapıldı; istisna YUTULMUYOR (yakalama+loglama sürücünün işi).
- **`GetCurrentHardCutAbrupt()` yanlış tonu çalardı.** GDD "aktif YA DA preload
  edilmiş" HARD CUT'a hizmet etmesini istiyor ama yalnız `_hardCutPreloadConfig`
  vardı; GDD AC-2'nin senkron-bekleme fallback'inin config'ini park edecek yeri
  yoktu → sorgu `false` döner → Adaptif Ses yanlış bitiş tonunu çalardı.
  `_activeHardCutConfig` eklendi.
- **Story 007'nin Engine Notes'u `SafeInvoke`'u ÇEKİRDEĞE koyuyordu** — uygulansa
  `UnityEngine.Debug` bağımlılığı gelir ve bu story'nin AC-2'sini doğrudan ihlal
  ederdi. Ayrım altı story sonra sessizce bozulacaktı. Story 007 düzeltildi:
  `SafeInvoke` sürücüde yaşar, çekirdeğin `Fail(type, Action)` hook'u zaten bunun için.
- Arayüzün dört public üyesine XML doc eklendi; `fromScene`'in meşru olarak `null`
  olabileceği (ADR-0015 boot çağrısı) artık sözleşmede yazıyor.

**QL-TEST-COVERAGE → GAPS** (hepsi kapatıldı):

- **AC-2'nin saflık "kanıtı" hiçbir şey kanıtlamıyordu.** Testin düz `[Test]`
  olarak derlenip koşmasını kanıt saymıştım; ama `EditModeTests` de `Foundation`
  da UnityEngine'e referanslı, yani çekirdeğe `using UnityEngine; private Vector3 _a;`
  eklense 14 testin HEPSİ yeşil kalırdı. 2026-08-10 ayrım kararının tamamı bu
  garantiye dayanıyordu ve garantiyi hiçbir şey uygulamıyordu. İki test eklendi:
  imza yüzeyi taraması (reflection) + kaynak taraması (gövdedeki `Debug.Log`
  imzalara yansımaz).
- **Arayüz testi iddia ettiğinden azını pinliyordu**: dönüş tipleri, parametre
  ADLARI, `CurrentState`'in salt-okunurluğu ve `HardCutConfig.Abrupt` kapsam
  dışıydı. En tehlikelisi parametre adları — `RequestSoftTransition`'ın ilk iki
  parametresi ikisi de `string`, yer değiştirmeleri tip kontrolüne GÖRÜNMEZ ama
  yükleme yönünü tersine çevirir. Ayrıca "birebir" tek yönlü test ediliyordu
  (fazla üye geçiyordu) — iki yönlü hâle getirildi.
- **Preload lane'i yalnız TAZE örnekte assert ediliyordu**, yani initializer'ı
  tekrarlıyordu. `SetState`'e yanlışlıkla `_hardCutPreloadState` yazan bir satır —
  dosyanın kendi yorumunun uyardığı hata — hiçbir testi kırmıyordu. Tam diziyi
  sürüp lane'in dokunulmadığını gösteren test eklendi.
- Hook'un TAM BİR KEZ çağrıldığı sayaçla pinlendi (çift invoke yeşil geçiyordu).
- `ActiveType` assertion'ı `Idle`'a dönüşten sonra KALDIRILDI: Story 006 ret
  gerekçesini yalnız `CurrentState != Idle` iken türetiyor, yani o değer
  `Idle`'da hiçbir tüketici tarafından okunamaz — Story 006'nın makul şekilde
  temizleyebileceği bir detayı pinlemek yanlıştı.

### Saflık guard'ı MUTASYONLA doğrulandı

Bu projede beş kez yanlış-yeşil yakalandığı için, "koruduğunu iddia eden" yeni
guard'ı bir kez fiilen kırdım: çekirdeğe `using UnityEngine;` + bir `Vector3`
alanı enjekte edildi ve süit koşuldu. Sonuç: **tam olarak iki yeni guard kırmızı
oldu, diğer 18 test yeşil kaldı** — hem QL'in bulgusunun doğru olduğunu (eski
testler bunu göremiyordu) hem de yeni guard'ın tam hedefini yakaladığını
kanıtlıyor. Mutasyon geri alındı, 259/259 temiz.

### Sonraki story'lere taşınan notlar

- AC-4 ("hiçbir durum atlanmaz") burada TEST TARAFINDAN sürülüyor; çekirdeğin
  yasal ardıl kavramı yok. Gerçek dizi iddiası **Story 003'ün sorumluluğu** ve
  orada da SON DURUMA değil TÜM KAYDEDİLEN LİSTEYE assert edilmeli — aksi hâlde
  GDD AC-1'in dizi iddiası hiçbir yerde doğrulanmamış olur.
- `OnSoftTransitionRejected` Story 006'ya kadar hiç fırlatılmıyor (CS0067 uyarısı
  üretmiyor — kontrol edildi).

---

## Dependencies

- Depends on: None (epic'in ilk story'si)
- Unlocks: Story 002 (sürücü bu çekirdeği sarar)
