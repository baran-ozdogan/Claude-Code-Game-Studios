# Story 005: HARD CUT — ayrı preload durumu, Ready fast-path, sıfır-kare swap

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: L (~4-5h — epic'in en yüksek test yükü)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (Core Rules — zero-frame swap toleransı, "Preload tam tamamlanmalı"; Edge Cases — ayrı preload durumu; AC-2, AC-8, AC-9)
**Requirement**: `TR-sahne-gecisi-004` (sıfır-kare swap + 0 siyah kare), `TR-sahne-gecisi-006` (%100 preload), `TR-sahne-gecisi-007` (ayrı preload durumu + no-op tekrar çağrılar)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0008 (primary — `RequestHardCut`'ın already-Ready dalı, `PreloadHardCut`); ADR-0015 (secondary — post-Ready aynı-sahne no-op'unu netleştiren karar)
**ADR Decision Summary**: `PreloadHardCut` ilerlemesi `_hardCutPreloadState`'te, herkese açık `CurrentState`'ten BAĞIMSIZ izlenir. `RequestHardCut`, tam bu `toScene` için `Ready` bekleyen bir preload varsa `LoadSceneAsync`'i YENİDEN KOŞMADAN doğrudan, senkron swap yapar — sıfır-kare garantisini mümkün kılan şey budur; önce `SetState(Ready, Hard)` yayınlar, sonra `DoSwap`. Eşleşen preload yoksa `RunTransition`'ın senkron-bekleme fallback'i devreye girer (GDD AC-2).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: HIGH — epic'in iki en kritik garantisi burada
**Engine Notes**: `SWAP_FRAME_EPSILON`, kodun referans etmesi gereken bir sabit DEĞİL, bir ÖLÇÜM toleransıdır — `SetActiveScene` zaten senkron olduğu için swap, `RequestHardCut`'ın çağrıldığı karede gerçekleşir. Epsilon, testin "tam sıfır" gibi fiziksel olarak imkânsız bir zaman damgası deltası iddia etmeden "bir kareden az" diyebilmesi için var.

**Control Manifest Rules (bu katman)**:
- Required: HARD CUT `Swapping` = tek senkron `SetActiveScene`; ≤1 kare istek-swap arası; tam 0 tamamen-siyah kare; preload gerçek %100 bekler
- Forbidden: `allowSceneActivation=false`/~%90 bekletme deseni
- Guardrail: preload edilmiş sahne için `RequestHardCut` `LoadSceneAsync`'i YENİDEN KOŞMAZ

---

## Acceptance Criteria

- [ ] `PreloadHardCut(toScene)` ilerlemesini `_hardCutPreloadState`/`_hardCutPreloadScene`/`_hardCutPreloadConfig` alanlarında izler; **herkese açık `CurrentState`'e ASLA YAZMAZ** ve `OnTransitionStateChanged`'i ASLA fırlatmaz
- [ ] **Ayrı-durum bağımsızlığı (GDD Edge Cases'in bu ayrımı var etme sebebi)**: aktif bir SOFT geçiş sürerken çağrılan `PreloadHardCut`, arka planda `Ready`'ye kadar ilerler; `CurrentState` bu pencerede SOFT'un kendi ilerlemesini yansıtmaya devam eder ve HARD CUT'a özgü hiçbir değer almaz *(QL-STORY-READY bulgusu: bu davranışın kendi numaralı GDD AC'si yok ama en yük taşıyan davranış — `CurrentState`'e yanlışlıkla yazan bir kopyala-yapıştır hatası aksi hâlde ancak Story 006'da dolaylı yakalanırdı)*
- [ ] **GDD AC-8**: `PreloadHardCut` in-flight bir preload varken tekrar çağrılırsa (HERHANGİ bir sahne için) no-op — ikinci yükleme başlamaz, orijinal hedef/durum değişmez
- [ ] **Post-Ready aynı-sahne no-op'u** (ADR-0015'in netleştirmesi): AYNI sahne için zaten `Ready`'de bekleyen bir preload varken `PreloadHardCut` tekrar çağrılırsa da no-op — ADR-0015'in ikili preload eşikleri `Ready` sonrası yeniden çağırabiliyor
- [ ] `RequestHardCut(toScene)`, tam bu sahne için `Ready` bekleyen bir preload varsa: `_hardCutPreloadState`'i `Idle`'a alır, **`SetState(Ready, Hard)`'ı YAYINLAR** (sessiz atlama DEĞİL — GDD "Ready→Swapping"i herkese açık `CurrentState` üzerinde tarif ediyor), sonra `DoSwap`'ı DOĞRUDAN çağırır — `LoadSceneAsync` YENİDEN KOŞMAZ
- [ ] **GDD AC-2**: hiç `PreloadHardCut` olmadan çağrılan `RequestHardCut` de AYNI `Preloading→Ready→Swapping→Complete→Idle` dizisinden geçer (senkron-bekleme fallback'i) — SOFT ve HARD CUT'ın ayrı kod yolu olmadığını kanıtlar
- [ ] Eşleşmeyen `Ready`: farklı bir sahne `Ready`'deyken `RequestHardCut(başkaSahne)` çağrılırsa sistem YÖNLENDİRME YAPMAZ — senkron-bekleme fallback'i devreye girer, kullanılmayan preload edilmiş sahne açıkça unload edilene kadar yüklü ve boşta kalır (GDD Edge Cases)
- [ ] **GDD AC-9 (a) — otomatik**: `RequestHardCut` çağrısı ile hedef sahnenin aktif olması arasındaki kare deltası `SWAP_FRAME_EPSILON` (1 kare) veya daha az
- [ ] **GDD AC-9 (b) — ÖNCE SPIKE, SONRA KARAR (kullanıcı kararı, 2026-08-10)**: "tam olarak 0 tamamen siyah render karesi" garantisi. Bu story'nin İLK işi, CI'da gerçek kare yakalamanın (`ScreenCapture.CaptureScreenshotAsTexture` / `Camera.targetTexture`+`ReadPixels` / `AsyncGPUReadback`) mümkün olup olmadığını tek bir deneme testiyle ölçmektir. **Mümkünse** gerçek piksel assertion'ı yazılır ve bu AC otomatik kapanır. **Değilse** DEFERRED manuel kanıta düşer (`production/qa/evidence/sahne-gecisi-siyah-kare-evidence.md`), gerekçe ve CI çıktısı kanıt dosyasına yazılır.
  > **UYARI (QL-STORY-READY)**: "hiçbir karede aktif sahne null/geçersiz değil" kontrolü siyah-kare garantisinin YERİNE GEÇMEZ ve öyle etiketlenmemelidir. Unity'nin additive modelinde hangi sahnenin "aktif" olduğundan bağımsız olarak tüm yüklü sahnelerin kameraları render eder — aktif-sahne geçerliliği, bir kameranın kapalı olup olmadığı ya da GPU'nun siyah olmayan piksel üretip üretmediği hakkında HİÇBİR ŞEY söylemez. Ayrı ve dürüst adlandırılmış bir sanity check olarak tutulabilir, AC-9(b)'nin kapanışı olarak DEĞİL.

---

## Implementation Notes

*ADR-0008'in `RequestHardCut` taslağı doğrultusunda:*

- Fast-path'in `DoSwap`'ı, Story 004'ün RenderSettings senkronunu ve `SetActiveScene` dönüş kontrolünü İÇERİR — yani AC-9(a) ölçümü o senkron işi de kapsar. **Bu yüzden Story 004 bu story'den ÖNCE bitmelidir** (QL-STORY-READY bulgusu: 005 önce inerse epsilon kanıtı eksik bir `DoSwap`'a karşı ölçülür ve 004 aynı kareye iş eklediğinde sessizce bayatlar).
- Fast-path bir coroutine DEĞİLDİR — doğrudan, senkron bir metot çağrısıdır (ADR-0008 TD review'ı bunu açıkça düzeltti: "already-Ready coroutine resumes" ifadesi yanlıştı).
- Fast-path başarısızlığında da tam yol koşar: `Failed` → `onFailed("ActivateSceneFailed")` → `Idle` → `TryFirePendingHardCut()`.
- AC-9(a) ölçümü `Time.frameCount` deltasıyla yapılır (zaman damgası değil) — kare bütçesi iddiası kare cinsinden ifade edilmeli.

---

## Out of Scope

- Kuyruklama ve ret hakemliği (Story 006) — bu story yalnız `Idle`'dan başlayan HARD CUT'ı ve preload'u kurar
- `SafeInvoke`, callback karşılıklı dışlayıcılığı, `Abrupt` taşıma (Story 007)
- `DoSwap`'ın kendi içeriği (Story 004 — burada yalnız çağrılır)

---

## QA Test Cases

- **AC-1 (otomatik, PlayMode)**: Ayrı preload durumu
  - Given: aktif bir SOFT geçiş (`CurrentState` Preloading/Ready/Swapping, `_activeType` Soft)
  - When: `PreloadHardCut(sceneX)` çağrılır
  - Then: `_hardCutPreloadState` bağımsız olarak `Ready`'ye ilerler; `CurrentState` bu pencerede SOFT'un kendi ilerlemesini yansıtır ve `OnTransitionStateChanged` HARD CUT kaynaklı hiçbir olay üretmez
  - Edge cases: `CurrentState`'e yazan bir implementasyon, SOFT'un kaydedilen dizisine yabancı bir girdi sokar ve test kırılır

- **AC-2 (otomatik, PlayMode)**: Preload no-op'ları
  - Given: `PreloadHardCut(sceneA)` in-flight
  - When: `PreloadHardCut(sceneB)` çağrılır
  - Then: no-op — `_hardCutPreloadScene` hâlâ `sceneA`, ikinci yükleme başlamaz
  - Edge cases: `sceneA` `Ready`'ye ULAŞTIKTAN sonra `PreloadHardCut(sceneA)` tekrar çağrılırsa da no-op (ADR-0015 netleştirmesi) — ve bu vaka in-flight vakasından AYRI assert edilir

- **AC-3 (otomatik, PlayMode)**: Ready fast-path, yeniden yükleme YOK
  - Given: `PreloadHardCut(toScene)` `Ready`'de
  - When: `RequestHardCut(toScene)` çağrılır
  - Then: `OnTransitionStateChanged` `Ready(Hard)` YAYINLAR, ardından `Swapping(Hard)`; `LoadSceneAsync` YENİDEN ÇAĞRILMAZ (yükleme sayacı gözcüsüyle doğrulanır)
  - Edge cases: `Ready`'yi atlayıp doğrudan `Swapping` yayınlayan bir implementasyon testi kırmalı — ADR-0008'in TD review'ı bunu özellikle düzeltti

- **AC-4 (otomatik, PlayMode)**: GDD AC-2 — paylaşılan dizi
  - Given: hiç preload yok
  - When: `RequestHardCut` çağrılır
  - Then: dizi TAM OLARAK `Preloading→Ready→Swapping→Complete→Idle`, hepsi `TransitionType.Hard` taşıyarak
  - Edge cases: SOFT'un Story 003'teki dizisiyle YAPI OLARAK aynı olmalı — ayrı bir kod yolu üretilmediğinin kanıtı

- **AC-5 (otomatik, PlayMode)**: Eşleşmeyen Ready yönlendirilmez
  - Given: `sceneA` preload'dan `Ready`'de
  - When: `RequestHardCut(sceneB)` çağrılır
  - Then: `sceneB` için senkron-bekleme fallback'i koşar; `sceneA` yüklü kalır ve aktif edilmez

- **AC-6 (otomatik, PlayMode)**: `SWAP_FRAME_EPSILON`
  - Given: `Ready`'de bekleyen bir preload
  - When: `RequestHardCut` çağrılır ve `Time.frameCount` çağrı anında ve hedef sahnenin aktif olduğu anda kaydedilir
  - Then: delta ≤ 1 kare
  - Edge cases: ölçüm Story 004'ün RenderSettings senkronu YÜKLÜYKEN yapılmalı — 004 bitmeden ölçülen bir değer bayattır

- **AC-7 (manuel ya da otomatik — SPIKE SONUCU BELİRLER)**: 0 siyah kare
  - Setup: `Ready`'de bekleyen preload, HARD CUT tetiklenir
  - Verify: swap penceresindeki hiçbir kare tamamen siyah render edilmemiş
  - Pass condition: siyah kare sayısı tam olarak 0. Spike CI'da kare yakalamayı mümkün gösterirse otomatik assertion; göstermezse kanıt dosyasında sürüm + tarih + gözlem

---

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/sahne_gecisi_hard_cut_test.cs` (+ spike sonucuna göre `production/qa/evidence/sahne-gecisi-siyah-kare-evidence.md`)
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004 (`DoSwap` — fast-path onu DOĞRUDAN çağırır; epsilon ölçümü onun senkron işini de kapsar)
- Unlocks: Story 006 (kuyruklanmış HARD CUT'ın "zaten Ready ise sıfır ek gecikme" iddiası bu fast-path'e dayanır)
