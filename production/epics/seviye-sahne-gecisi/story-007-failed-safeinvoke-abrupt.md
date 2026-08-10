# Story 007: Failed yolu, SafeInvoke ve Abrupt taşıma

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (~3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (Edge Cases — `onComplete` istisnası, yüklenemeyen sahne; Interactions — `HardCutConfig.Abrupt`, karşılıklı dışlayıcı callback'ler; AC-10, AC-11, AC-11a)
**Requirement**: `TR-sahne-gecisi-010` (callback istisnaları sızmaz), `TR-sahne-gecisi-012` (`Abrupt` taşınır ama yorumlanmaz)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da; `Failed`→`Idle` otomatik dönüşünün kendisi `TR-sahne-gecisi-009` ve Story 001'de kuruldu — burada onun ÇEVRESİNDEKİ callback sözleşmesi kapanır)*

**ADR Governing Implementation**: ADR-0008 (primary — `SafeInvoke` iki aşırı yüklemesi, `GetCurrentHardCutAbrupt`)
**ADR Decision Summary**: `SafeInvoke` **İKİ AYRI AŞIRI YÜKLEMEDİR** (`Action` ve `Action<string>`). ADR'ın ilk taslağı ikisini tek metotta `(Action<string>)(object)callback` cast'iyle çözmeye çalışıyordu — unity-specialist doğrulaması bunu BLOCKING olarak yakaladı: bu ifade DERLENMEZ ve derlense bile `InvalidCastException` atardı, çünkü `Action` ve `Action<string>` aralarında geçerli dönüşüm olmayan ilgisiz delege tipleridir. `HardCutConfig.Abrupt` yalnız TAŞINIR, yorumlanmaz — swap mekanizması `Abrupt` değerinden bağımsız olarak DEĞİŞMEDEN kalır.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: **`SafeInvoke` SÜRÜCÜDE (`SceneTransitionManager`) yaşar, çekirdekte DEĞİL.** `Debug.LogException` bir `UnityEngine` çağrısıdır; `SceneTransitionState`'e konsaydı Story 001'in AC-2'sini (çekirdek hiçbir `UnityEngine` tipine bağlanmaz) doğrudan ihlal eder ve ayrımı sessizce çürütürdü *(LP-CODE-REVIEW bulgusu, 2026-08-10 — bu notun ilk hâli tam tersini söylüyordu)*. Çekirdeğin `Fail(type, Action invokeOnFailed)` hook'u zaten bunun için var: sürücü `Fail(type, () => SafeInvoke(onFailed, reason))` geçer. Çekirdek `invokeOnFailed`'i `try/finally` ile sarmalar (istisna YUTMADAN `Idle`'a dönüşü koşulsuz kılar); yakalama ve loglama sürücünün `SafeInvoke`'unun işidir. Testte beklenen istisna logu `LogAssert.Expect(LogType.Exception, ...)` ile beyan edilmelidir, yoksa UTF testi düşürür.

**Control Manifest Rules (bu katman)**:
- Required: durum makinesi mantığı BLOCKING birim testli; pointed hata mesajları
- Forbidden: callback istisnasını yutup durum makinesini `Complete`'te bırakmak
- Guardrail: —

---

## Acceptance Criteria

- [ ] **GDD AC-11**: hedef sahne yüklenemezse `CurrentState` terminal `Failed`'e geçer (sessizce `Idle`'a DEĞİL), `OnTransitionStateChanged(Failed, type)` fırlar
- [ ] **`onComplete` ve `onFailed` KARŞILIKLI DIŞLAYICIDIR**: bir geçiş için ikisinden yalnız BİRİ, TAM OLARAK BİR KEZ çağrılır. Başarı yolunda `onFailed` hiç çağrılmaz; başarısızlık yolunda `onComplete` hiç çağrılmaz
- [ ] **GDD AC-11a**: `onFailed` çağrıldıktan HEMEN SONRA `CurrentState` otomatik `Idle`'a döner, VE hemen ardından gönderilen yeni bir `RequestSoftTransition`/`RequestHardCut` normal kabul edilir (AC-1/AC-2'nin standart dizisini izler) — tek bozuk sahne referansı oturumu soft-lock'lamaz
- [ ] **`SafeInvoke` İKİ AŞIRI YÜKLEME**: `SafeInvoke(Action)` ve `SafeInvoke(Action<string>, string)`. Tek metot + cast hilesi YASAK (derlenmez, `InvalidCastException` atardı — ADR-0008 unity-specialist BLOCKING bulgusu)
- [ ] **GDD AC-10**: `onComplete` istisna fırlatırsa istisna `SceneTransitionManager`'ın DIŞINA sızmaz, dahili olarak yakalanır ve loglanır, VE `CurrentState` yine de `Idle`'a ilerler. Hemen sonra gönderilen yeni bir istek, geçiş ortasındaymış gibi REDDEDİLMEZ
- [ ] Aynı garanti `onFailed` için de geçerlidir — istisna fırlatan bir `onFailed` da `Idle`'a ulaşmayı engellemez
- [ ] **`HardCutConfig.Abrupt` TAŞINIR, YORUMLANMAZ** (TR-012): `GetCurrentHardCutAbrupt()` aktif ya da preload edilmiş bir HARD CUT'ın `config.Abrupt` değerini döner; hiçbir HARD CUT preload edilmemiş/aktif değilse sonuç TANIMSIZDIR (GDD'nin kendi sözleşmesi — çağıran yalnız `OnTransitionStateChanged(Swapping, Hard)` aldığı karede sorgulamalıdır)
- [ ] **`Abrupt` swap mekanizmasını DEĞİŞTİRMEZ**: `Abrupt=true` ve `Abrupt=false` ile koşan iki HARD CUT birebir aynı durum dizisini ve aynı kare davranışını üretir — GDD AC-2'nin "ayrı kod yolu yok" garantisi bu alanla da bozulmaz

---

## Implementation Notes

*ADR-0008'in `SafeInvoke`/failure yolu taslağı doğrultusunda:*

- Başarısızlık yolunun tam sırası: `SetState(Failed, type)` → `SafeInvoke(onFailed, reason)` → `SetState(Idle, type)` → `TryFirePendingHardCut()`. `Idle`'ın `onFailed`'DEN SONRA yayınlanması önemli: çağıran `onFailed` içinde durumu örneklerse `Failed` görmeli, ama hemen ardından gönderdiği istek kabul edilmeli.
- Başarısızlık gerekçesi string'leri sabit ve konuşkan olmalı: yükleme başarısızlığı ile `SetActiveScene` başarısızlığı (`"ActivateSceneFailed"`) AYRI gerekçelerdir — Asansör/Sahne Kesmeli Anlatı bunları kendi kurtarma mantığında ayırt edebilmeli.
- `GetCurrentHardCutAbrupt()` dar, senkron bir sorgudur — event payload'ını genişletmek yerine bu projenin `GetStingerAudioRadius`/`IsShiftPersistent` deseniyle aynı. Adaptif Ses tek tüketicisi.
- "Tanımsız sonuç" gerçekten tanımsız bırakılır (varsayılan `false` dönmek yeterli) ama XML doc'ta AÇIKÇA yazılır — sessiz bir `false`, çağıranın yanlış karede sorgulamasını maskeler.

---

## Out of Scope

- `Failed`→`Idle` otomatik dönüşünün SAF DURUM mantığı (Story 001'de kuruldu — burada callback sözleşmesiyle birlikte uçtan uca doğrulanır)
- Çağıranın movement-lock serbest bırakması (GDD AC-12 — Asansör ve Sahne Kesmeli Anlatı'nın kendi AC'lerinde, bu sistemin testleri yakalayamaz)
- `Abrupt`'ın ses/görsel yorumu (Adaptif Ses Sistemi epic'i)

---

## QA Test Cases

- **AC-1 (otomatik, PlayMode)**: Yükleme başarısızlığı → Failed → Idle
  - Given: yüklenemeyen bir hedef sahne
  - When: `RequestSoftTransition` çağrılır
  - Then: dizi `Preloading`, `Failed`, `Idle` içerir bu sırada; `onFailed` TAM BİR KEZ, `onComplete` HİÇ çağrılmaz
  - Edge cases: `onFailed`'in aldığı gerekçe string'i boş olmamalı

- **AC-2 (otomatik, PlayMode)**: Failed sonrası yeni istek kabul edilir
  - Given: AC-1'in başarısızlığı yeni tamamlandı
  - When: HEMEN ardından geçerli bir hedefle yeni bir `RequestSoftTransition` gönderilir
  - Then: normal `Preloading→…→Idle` dizisi koşar — soft-lock YOK
  - Edge cases: `Failed`'de takılan bir implementasyonda ikinci istek reddedilir ve test kırılır (GDD'nin "üretim durdurucu boşluk" dediği regresyon)

- **AC-3 (otomatik, EditMode)**: Callback karşılıklı dışlayıcılığı
  - Given: gözcü `onComplete` + `onFailed`
  - When: başarılı bir geçiş sürülür
  - Then: `onComplete` tam bir kez, `onFailed` sıfır kez
  - Edge cases: başarısız geçişte tersi; İKİSİNİN de çağrıldığı ya da hiçbirinin çağrılmadığı bir implementasyon testi kırmalı

- **AC-4 (otomatik, EditMode)**: `SafeInvoke` iki aşırı yükleme
  - Given: istisna fırlatan bir `onComplete`
  - When: çekirdek `Complete`'e sürülür
  - Then: istisna dışarı SIZMAZ (`Assert.DoesNotThrow`), loglanır (`LogAssert.Expect(LogType.Exception, ...)`), `CurrentState` `Idle`'a ulaşır
  - Edge cases: istisna fırlatan bir `onFailed` için AYRI vaka — iki aşırı yüklemenin ikisi de kapsanmalı, yoksa cast-hilesi regresyonu yalnız birinde yakalanır

- **AC-5 (otomatik, PlayMode)**: İstisna sonrası sistem sağlam
  - Given: `onComplete` istisna fırlattı
  - When: hemen ardından yeni bir istek gönderilir
  - Then: kabul edilir — yönetici `Complete`'te takılı kalmadı

- **AC-6 (otomatik, PlayMode)**: `Abrupt` taşınır
  - Given: `HardCutConfig.Abrupt = true` ile preload edilmiş bir HARD CUT
  - When: `OnTransitionStateChanged(Swapping, Hard)` alındığı karede `GetCurrentHardCutAbrupt()` sorgulanır
  - Then: `true` döner
  - Edge cases: `Abrupt = false` ile ikinci vaka `false` döner

- **AC-7 (otomatik, PlayMode)**: `Abrupt` mekanizmayı değiştirmez
  - Given: `Abrupt=true` ve `Abrupt=false` ile iki ayrı HARD CUT
  - Then: kaydedilen durum dizileri BİREBİR aynı; kare deltası ikisinde de `SWAP_FRAME_EPSILON` içinde
  - Edge cases: `Abrupt`'a bakarak dallanan bir implementasyon dizileri ayrıştırır ve testi kırar — AC-2'nin "ayrı kod yolu yok" garantisinin bu alan için regresyonu

---

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/sahne_gecisi_callback_test.cs` + `game/Assets/Tests/PlayMode/sahne_gecisi_failed_abrupt_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 (epic'in hakemlik katmanı; `Abrupt` sorgusu preload/aktif HARD CUT gerektiriyor)
- Unlocks: None — epic'in son story'si. Asansör/Kat-Erişim ve Sahne Kesmeli Anlatı epic'leri bu sistemin tamamına bağlı
