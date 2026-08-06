# Quick Design Spec: Sahne Kesmeli Anlatı (Cutscene/Scene-Cut Narrative)

**Type**: New Small System
**Scope**: Gecenin bitişini psikiyatri seansı cutscene'ine ani kesmeyle (HARD CUT) tetikleyen orkestrasyon sistemi — *ne zaman* ve *hangi sırayla* kararını verir. Diyalog içeriğinin kendisini yazmaz (Diyalog/Anlatı İçeriği'nin işi), sahne yükleme mekaniğini yazmaz (Seviye/Sahne Geçişi'nin işi).
**Date**: 2026-08-02
**Estimated Implementation**: ~1-2 gün

## Player Fantasy

*(design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi: bu
sistem oyunun en pillar-yüklü kararını (gecenin ne zaman ve nasıl biteceği)
sahipleniyordu ama hiçbir hissedilen duygu tanımlamıyordu.)*

İki farklı bitiş, iki farklı his taşımalı — ikisi de "başarı" değil,
sadece farklı türde bir durma noktası. Görev tamamlama biterken, iş
biter — Görev/Taşıma Döngüsü'nün kendi "Dikkatin Göçü" yayının doğal
sonucu, sakin bir teslim anı. Doygunluk biterken, dünya seni durdurur —
Seviye/Sahne Geçişi'nin "Bedenin Çalınması" kaymasıyla aynı an, ama bu
sefer neden orada olduğunu biliyorsun: çok fazla anıyı aynı anda taşıyor
olman. Hiçbiri kutlanmaz (Pillar 4: Bağ, Güvenlik Değil ile tutarlı);
ikisi de sadece "artık burada değilsin."

## Overview

Sahne Kesmeli Anlatı, bir gecenin ne zaman biteceğine ve psikiyatri seansı sahnesine ne zaman kesileceğine karar veren saf orkestrasyon katmanıdır. İki bağımsız sistemden gelen "bitiş sinyali" izler — görev listesinin tamamlanması ve anı-tetikleyicilerin doygunluğu — ve hangisi önce gerçekleşirse Seviye/Sahne Geçişi'nin `RequestHardCut`'ını tetikler. Kendi diyalog/ses/görsel mantığı yoktur; sadece "şimdi" der.

## Core Rules

- **İki bağımsız bitiş sinyali, OR mantığı**: Gece, aşağıdakilerden **hangisi önce gerçekleşirse** biter:
  (a) **Görev tamamlama** — Görev/Taşıma Döngüsü'nün `OnTaskListCompleted` event'i fırlar.
  (b) **Anı-tetikleyici doygunluğu** — o gece için yapılandırılmış TÜM `MemoryTriggerDef`'ler **Held'e ulaşmış** olur (design-review, 2026-08-04 — full re-verification bulgusuyla `Committed`/`FiredTriggerIds`'ten `Held`/`SettledTriggerIds`'e değiştirildi, bkz. aşağıdaki "Saturation-timing düzeltmesi" notu — kritik bulgu), **VE** Görev/Taşıma Döngüsü'nün `IsFinalRoundActive` bayrağı `true`'dur (design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi, kritik bulgu — bkz. aşağıdaki not), **VE Görev/Taşıma Döngüsü'nün `HasCarriedInFinalRound` bayrağı `true`'dur** (design-review, 2026-08-04 — verification design-theory bulgusu, eklendi, en kritik bulgu — bkz. aşağıdaki ayrı not). Held sayımı **Gece/Oturum Durumu'nun `SettledTriggerIds.Count`** ile `TotalConfiguredTriggerCountForNight`'a karşı yapılır (design-review, 2026-08-03'te `FiredTriggerIds.Count`'a düzeltilmişti — önceki hali Anlatı Durum'un `GetKnownClueIds().Count`'unu kullanıyordu; 2026-08-04'te `SettledTriggerIds.Count`'a tekrar düzeltildi, bkz. aşağıdaki not).
  **Saturation-timing düzeltmesi (design-review, 2026-08-04 — full
  re-verification bulgusu, kullanıcı kararıyla çözüldü, en kritik bulgu)**:
  Önceki taslak `FiredTriggerIds.Count`'u kullanıyordu — bu, bir
  tetikleyicinin Hold'unun tamamlandığı anı (`Shifting-In` girişi)
  işaretler, ışığın ~3sn'lik rampası ve stinger'ın çalması henüz yeni
  başlamışken. Sonuç: gecenin son tetikleyicisi aynı zamanda doygunluk
  koşulunu tamamlayan tetikleyiciyse, `RequestHardCut` **aynı karede**
  tetikleniyordu — ışık+ses bileşik etkisi (Işık/Volume'un rampası,
  Adaptif Ses'in stinger'ı), ilgili ipucunun bilinir olması (`Anlatı
  Durum` sadece `Held`'i işler, `Shifting-In`'i değil) ve dolayısıyla o
  gecenin en son callback'i **her zaman** kesiliyordu — oyuncunun en son,
  en kasıtlı "bile bile yaptım" eylemi kendi payoff'unu asla
  tamamlayamıyordu (bkz. `gdd-cross-review-2026-08-04-verification.md`,
  üç bağımsız inceleme yönteminin (consistency, design-theory, senaryo
  yürüyüşü) bağımsız olarak aynı sonuca vardığı tek madde). **Düzeltme**:
  koşul artık `Gece/Oturum Durumu`'nun `SettledTriggerIds.Count`'unu
  kullanır — bu küme sadece bir `shiftId`'nin gerçekten `Held`'e
  ulaştığı karede dolar (bkz. `gece-oturum-durumu-2026-08-02.md` Core
  Rules, `OnTriggerSettled`). `MemoryTriggerDef`-bağlı her shift zaten
  `Persistent=true` olduğundan, Işık/Volume'un kendi garantisi gereği
  `Held`'e her zaman ulaşır (~3sn gecikmeyle) — yani bu düzeltme
  doygunluğu imkânsız kılmaz, sadece HARD CUT'ı ışığın rampası
  tamamlandıktan, stinger'ın (1-1.5sn, zaten `Shifting-In`'de başlamıştı)
  doğal olarak bittiğinden, ve ipucunun bilinir olduğundan **sonraya**
  erteler. Preload zamanlaması (aşağıda) bu değişiklikten etkilenmez —
  hâlâ erken/eager sinyal olarak `FiredTriggerIds` kullanır, çünkü
  preload'un amacı hazırlık, gerçek tetikleme değil.
  Diğer sinyal daha sonra gelirse anlamsızdır — gece zaten bitmiştir (bkz. tekrar-tetiklenme guard'ı).
  **`HasCarriedInFinalRound` guard'ı neden eklendi (design-review, 2026-08-04
  — verification design-theory bulgusu, en kritik bulgu, kullanıcı kararıyla
  çözüldü)**: `IsFinalRoundActive` guard'ı tek başına yetersizdi —
  `OnFinalRoundStarted`, round'un **aktive olduğu** karede fırlar, bu
  yüzden tüm tetikleyicileri son rounddan önce bulmuş bir oyuncu için
  doygunluk koşulu son round'un **ilk karesinde** `true` oluyordu: son
  round hiç oynanmadan, `Highlight` sönme eğrisinin bu round için hiç
  gösterilmeden, VE preload'un `Ready`'ye ulaşacak zamanı bulamadan
  (görev-tarafı preload eşiği de aynı karede, "son round aktifken" —
  bkz. aşağıdaki "Preload zamanlaması" notu — tetiklendiği için
  `RequestHardCut` `Preloading` sırasında senkron-bekleme fallback'ine
  düşüyor, `MovementLockScope.Full` altında gözle görülür bir yükleme
  takılmasına yol açıyordu). **Düzeltme**: doygunluk artık oyuncunun son
  round'un malzemesinden en az birini fiilen elde almış olmasını da
  zorunlu kılar — bu hem son round'un en az bir parçasının deneyimlenmesini
  garanti eder hem de preload'a (görev-tarafı eşik zaten round-aktivasyonunda
  tetiklendiği için) gerçek bir hazırlanma süresi tanır, hem de HARD
  CUT'ı her zaman oyuncu elinde yükle yürürken gerçekleştirerek Seviye/Sahne
  Geçişi'nin "Bedenin Çalınması" fantazisiyle tam örtüştürür (bkz. Player
  Fantasy yukarıda — bu artık tesadüf değil, mekanizma tarafından
  garanti ediliyor).
  **(b)'nin değerlendirme tetiği (design-review, 2026-08-03 — verification
  N5 bulgusu, eklendi, kritik bulgu)**: Önceki taslakta (b) koşulu için
  **hiçbir değerlendirme tetiği tanımlı değildi** — Gece/Oturum Durumu
  hiçbir event fırlatmıyordu, `IsFinalRoundActive`'ın da bir değişim
  bildirimi yoktu; sonuç, saf bir polling varsayımıydı ki bu hiçbir yerde
  yazılı değildi. Somut sonuç: round 1-2'de tüm tetikleyicileri bulan bir
  oyuncuda (guard'ın **tam olarak korumak için** yazıldığı senaryo)
  doygunluk koşulu `false` olur ve **hiçbir şey onu son round başladığında
  yeniden değerlendirmezdi** — dal kendi motive edici senaryosunda ölüydü.
  **Düzeltme**: bu sistem artık **üç** event'e abone olur — Gece/Oturum
  Durumu'nun `OnTriggerSettled(shiftId)`'i (design-review, 2026-08-04 —
  full re-verification bulgusuyla `OnTriggerFired`'dan değiştirildi, bkz.
  yukarıdaki "Saturation-timing düzeltmesi" ve
  `gece-oturum-durumu-2026-08-02.md` Core Rules), Görev/Taşıma
  Döngüsü'nün `OnFinalRoundStarted`'ı, VE Görev/Taşıma Döngüsü'nün
  `OnFinalRoundItemPickedUp`'ı (design-review, 2026-08-04 — verification
  design-theory bulgusu, üçüncü event eklendi; bkz. `gorev-tasima-dongusu.md`
  Core Rules) — **hangisi önce fırlarsa**, (b) koşulu o anda yeniden
  değerlendirilir. Bu, guard'ın motive edici senaryosunu kapatır:
  erken-doyan oyuncuda `OnTriggerSettled` (Hold tamamlanmasından ~3sn
  sonra, Held'e ulaşınca) erken fırlar ama `IsFinalRoundActive` henüz
  `false` olduğu için tetiklemez; oyuncu sonunda son round'a ulaştığında
  `OnFinalRoundStarted` fırlar ama artık `HasCarriedInFinalRound` henüz
  `false` olduğu için **hâlâ** tetiklemez (2026-08-04 düzeltmesi — bu,
  son round'un hiç oynanmadan bitmesini önler); oyuncu son round'un ilk
  eşyasını aldığında `OnFinalRoundItemPickedUp` fırlar ve **bu kez**
  koşul yeniden değerlendirilip `true` bulunur — döngü asla erken
  kesilmeden, dal artık canlı VE her zaman oyuncu elinde yükle yürürken
  tetiklenir.
  **`IsFinalRoundActive` guard'ı neden eklendi (design-review, 2026-08-03
  — `/review-all-gdds` Design Theory bulgusu, en önemli bulgulardan biri)**:
  Önceki taslakta (b), oyuncu tüm tetikleyicileri erken bulursa (round
  1-2'de), MVP'nin kendi çekirdek döngüsünü doğrulamak için var olduğu
  deneyimi sessizce kısaltabiliyordu — Görev/Taşıma Döngüsü'nün kendi
  Player Fantasy'si kümülatif bir yay olarak tasarlanmıştı (round-indexed
  prominence eğrisi), ve `game-concept.md`'nin kendi Core Loop'u "3-5
  taşıma turu, turlar ilerledikçe ortam gerilimi birikir" diyordu. Bu
  guard, doygunluğun geceyi bitirebilmesi için oyuncunun **zaten** son
  round'a ulaşmış olmasını zorunlu kılar — döngü asla erken kesilmez,
  sadece son round'un normal süresi içinde erken bitebilir (görev
  tamamlanmadan önce doygunluk yetişirse). `IsFinalRoundActive` zaten
  `gorev-tasima-dongusu.md`'nin kendi arayüzünde mevcuttu (2026-08-02'de
  Seviye/Sahne Geçişi'nin eski bir isteği için eklenmişti), yeni bir API
  gerekmiyor.
- **Preload zamanlaması**: Seviye/Sahne Geçişi'nin `PreloadHardCut(toScene)`'i, iki bitiş koşulundan biri "bir adım kalana" ulaştığında çağrılır: görev tarafında son round aktifken, anı-tetikleyici tarafında (`IsFinalRoundActive=true` VE `FiredTriggerIds.Count == TotalConfiguredTriggerCountForNight - 1`) olduğunda. Hangisi önce olursa preload o anda başlar; ikinci preload çağrısı Seviye/Sahne Geçişi'nin kendi no-op kuralı sayesinde zaten güvenlidir.
- **Gerçek tetikleme**: (a) ya da (b) tam gerçekleştiğinde `RequestMovementLock(this, MovementLockScope.Full)` çağrılır, hemen ardından `RequestHardCut(toScene, config, onComplete, onFailed)` çağrılır.
- **İki bitişin farklı tonu — `HardCutConfig.Abrupt` (design-review,
  2026-08-04 — full re-verification bulgusu, kullanıcı kararıyla
  çözüldü)**: Bu belgenin kendi Player Fantasy'si iki bitişin farklı
  hissetmesi gerektiğini söylüyordu (görev tamamlama = "sakin bir teslim
  anı"; doygunluk = "dünya seni durdurur") ama önceki taslakta ikisi de
  aynı `HardCutConfig`'i, aynı sıfır-kare swap'ı, aynı anlık-tüm-sesi-kes
  kuralını ve aynı CutSting'i paylaşıyordu — hissedilen fark hiçbir
  mekanizmaya bağlı değildi. Movement-lock'un `Full` kapsamı **her iki**
  bitiş için de korunur (bu duygusal ton değil, teknik bir gereklilik —
  zero-frame swap sırasında kameranın oyuncu girdisiyle sürüklenmemesi
  gerekir), ama ses/görsel şiddeti artık farklılaşıyor: `RequestHardCut`
  çağrısına iletilen `config`, `HardCutConfig.Abrupt` (bool) alanı taşır
  — (b) (doygunluk) için `Abrupt=true` (mevcut davranış, değişmedi:
  anlık tüm-sesi-kes + CutSting), (a) (görev tamamlama) için
  `Abrupt=false` (yeni: ambiyans anlık kesilmez, Adaptif Ses'in zaten
  var olan `ambient_crossfade` mekanizmasıyla aynı `T` süresinde
  sessizliğe kayar; CutSting hiç çalmaz — bkz. `adaptif-ses-sistemi.md`
  Core Rules, "HARD CUT Sting"). Sahne geçişinin kendisi (zero-frame
  swap, `SceneTransitionManager`'ın tek paylaşılan durum makinesi) **her
  iki tonda da değişmez** — sadece Adaptif Ses'in tepkisi `Abrupt`
  bayrağına göre dallanır, Seviye/Sahne Geçişi'nin kendisi bu bayraktan
  habersizdir (sadece taşır, yorumlamaz).
- **Hareket kilidi (design-review, 2026-08-03 — `/review-all-gdds`
  bulgusu, eklendi)**: Önceki taslakta bu sistem hiç movement-lock
  almıyordu — ama tetikleme anı yapı gereği oyuncu hareket halindeyken
  gerçekleşir (görev-tamamlama sinyali, oyuncunun teslimat bölgesine
  *yürüyerek girmesiyle* tetiklenir), bu yüzden FPC'nin stride-phase/
  head-bob döngüsü zero-frame swap'a kesintisiz taşınırdı — Seviye/Sahne
  Geçişi'nin kendi "Bedenin Çalınması" (orta hareketten koparılma)
  fantazisinin tam tersini üretirdi. Düzeltme: `Full` kapsamlı kilit
  (`MovementLockScope.Full`, bkz. `birinci-sahis-kontrolcu.md`),
  `onComplete`/`onFailed` içinde serbest bırakılır — bu aynı zamanda
  Seviye/Sahne Geçişi'nin kendi Blocked Acceptance Criteria tablosundaki
  AC-12'nin (kilit-serbest-bırakma) eksik yarısını da kapatır.
- **Tekrar-tetiklenme guard'ı**: Sistem gece başına **tam bir kez** tetiklenir — `HasTriggeredThisNight` bool, `RequestHardCut` çağrılır çağrılmaz `true` olur; ikinci bir bitiş sinyali (örn. aynı karede iki koşul da sağlanırsa) no-op'tur. **Aynı karede öncelik sırası** (design-review, 2026-08-06 — re-verification bulgusu, kullanıcı kararıyla çözüldü, bkz. aşağıdaki not) belirler HANGİ koşulun bu tek çağrıyı ürettiği — bkz. hemen aşağıdaki madde.
- **Aynı karede iki koşulun da sağlanması — öncelik kuralı (design-review,
  2026-08-06 — re-verification bulgusu, en kritik yeni bulgu, kullanıcı
  kararıyla çözüldü)**: 2026-08-04'te eklenen `HardCutConfig.Abrupt`
  ayrımından önce bu belirsizlik zararsızdı — iki bitiş de mekanik olarak
  özdeşti. Artık değil: hangi koşulun "önce gerçekleştiği" tanımsızsa
  (görev-tamamlama sinyali ile doygunluk sinyali tam olarak aynı Update
  karesinde tetiklenirse — gerçekçi bir senaryo, çünkü `HasCarriedInFinalRound`
  zaten son roundda taşımayı gerektirir, tam da son teslimatın da
  gerçekleştiği pencere), Unity'nin event-abone sırası (bu belgede hiç
  tanımlanmamış) hangi tonun (`Abrupt=true`/`false`) oynatılacağına keyfi
  olarak karar verirdi — bu, belgenin kendi Player Fantasy'sinin ("iki
  bitiş, iki farklı his taşımalı") güvenilir şekilde teslim edilmemesi
  anlamına gelirdi. **Kural**: sistem her karede önce (a) görev-tamamlama
  koşulunu kontrol eder; `OnTaskListCompleted` bu karede zaten fırlamışsa
  VE `HasTriggeredThisNight=false` ise, **(a) kazanır** (`Abrupt=false`),
  doygunluk koşulu o karede ayrıca değerlendirilmeden geçilir. Doygunluğun
  üç event'inden biri (`OnTriggerSettled`, `OnFinalRoundStarted`,
  `OnFinalRoundItemPickedUp`) aynı karede fırlarsa ve görev-tamamlama
  sinyali fırlamamışsa, (b) normal şekilde değerlendirilir. Seçim
  kasıtlı: Pillar 2 (Sessiz Gerilim, Şok Değil) ile tutarlı — iki koşul
  da meşru şekilde `true` olduğunda, varsayılan olarak daha sakin sonuca
  gidilir, ani/şok edici olana değil; ayrıca oyuncunun görevini bitirmiş
  olması gecenin kapanışını hak eden birincil sinyal olarak okunur.
  Bu, `HasTriggeredThisNight` guard'ının **hangi** çağrının gerçekleştiğini
  belirlemesi değil, sadece ikinci bir çağrının asla olmamasını garanti
  etmesiyle aynı seviyede — öncelik kuralı, guard tetiklenmeden **önce**
  hangi koşulun kontrol edildiğini belirler.
- **Oturum kapanışı**: `onComplete` içinde Gece/Oturum Durumu'nun oturumu sonlandırılır (`IsSessionActive=false`) VE hareket kilidi serbest bırakılır. `onFailed` içinde de hareket kilidi serbest bırakılır (oturum sonlandırılmaz — geçiş başarısız oldu, gece teknik olarak bitmedi).
- **Diyalog seçimine karışmaz**: `onComplete` sonrası (psikiyatri sahnesi aktif), Diyalog/Anlatı İçeriği kendi başlangıç akışıyla `IsClueKnown` sorgular — bu sistem sadece doğru sahnenin yüklü olmasını garanti eder.

## States and Transitions

| Durum | Giriş | Çıkış |
|---|---|---|
| **Watching** | Gece başladı, `HasTriggeredThisNight=false` | Preload-eşiği → **Preloaded**; (preload atlanmışsa) doğrudan tetikleme-eşiği → **Triggering** |
| **Preloaded** | `PreloadHardCut` çağrıldı | Tetikleme-eşiği gerçekleşti → **Triggering** |
| **Triggering** | `RequestHardCut` çağrıldı, `HasTriggeredThisNight=true` | `onComplete` → **Complete** |
| **Complete** | Gece/Oturum Durumu sonlandırıldı | Terminal (bu gece için) |

## Dependencies

- **Seviye/Sahne Geçişi** — `PreloadHardCut`, `RequestHardCut`, `onComplete`, `onFailed`
- **Görev/Taşıma Döngüsü** — `OnTaskListCompleted`; `IsFinalRoundActive` sorgusu (2026-08-02'de eklendi, artık hem preload zamanlaması hem doygunluk guard'ı için kullanılıyor); `OnFinalRoundStarted` event'ine abone olur (design-review, 2026-08-03 — verification N5 bulgusu, eklendi — bkz. Core Rules); `HasCarriedInFinalRound` sorgusu VE `OnFinalRoundItemPickedUp` event'ine abone olur (design-review, 2026-08-04 — verification design-theory bulgusu, eklendi — doygunluğun üçüncü şartı, bkz. Core Rules)
- **Gece/Oturum Durumu** — oturum sonlandırma (`EndSession()`, 2026-08-02'de eklendi); **`SettledTriggerIds.Count`** sorgusu (design-review, 2026-08-04 — full re-verification bulgusuyla `FiredTriggerIds.Count`'tan değiştirildi, saturation-timing düzeltmesi — bkz. Core Rules) ve preload zamanlaması için hâlâ `FiredTriggerIds.Count` sorgusu (erken/eager sinyal, değişmedi); `OnTriggerSettled` event'ine abone olur (design-review, 2026-08-04 — full re-verification bulgusuyla `OnTriggerFired`'dan değiştirildi)
- **Birinci Şahıs Kontrolcü** *(design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi)* — `RequestMovementLock(this, MovementLockScope.Full)`/`ReleaseMovementLock(this)` çağırır (bkz. Core Rules, "Hareket kilidi")
- **Diyalog/Anlatı İçeriği** — dolaylı, bu sistem onu tetiklemez

**Not (design-review, 2026-08-03, güncellendi 2026-08-04)**: Anlatı Durum/İpucu Takibi artık bu
sistemin bir bağımlılığı **değil** — önceki taslak doygunluk sinyali için
`OnClueKnown`/`GetKnownClueIds()`'i kullanıyordu, 2026-08-03'te Gece/Oturum
Durumu'nun `FiredTriggerIds`'ine, 2026-08-04'te aynı sistemin
`SettledTriggerIds`'ine geçti (bkz. Core Rules'taki düzeltme notları).
Anlatı Durum, Diyalog/Anlatı İçeriği'nin kendi bağımlılığı olarak kalmaya
devam ediyor, bu sistemin değil.

## Tuning Knobs

**N/A** — saf event-orkestrasyonu, "feel" parametresi yok.
`TotalConfiguredTriggerCountForNight` içerik yapılandırmasıdır (MVP'de
2-3), tuning knob değil. *(design-review, 2026-08-03 — `/review-all-gdds`
verification bulgusu, düzeltildi: bu satır hâlâ eski `TotalConfiguredClueCountForNight`
adını taşıyordu, Core Rules ve ACs'in kullandığı `TotalConfiguredTriggerCountForNight`'dan
farklı — isim artık tutarlı. Değerin kendisi hâlâ bir veri alanı olarak
hiçbir sistemde tanımlı değil — doğal ev sahibi Gece/Oturum Durumu'dur
[`FiredTriggerIds`'in sayacı bu olacaktı], bu ayrı bir madde olarak açık
kalmaya devam ediyor — N5'in kendisi [değerlendirme tetiği eksikliği]
2026-08-03'te çözüldü, bkz. Core Rules "(b)'nin değerlendirme tetiği";
bu alanın veri-sahipliği N5'ten bağımsız, hâlâ ayrı bir açık madde.)*

## Acceptance Criteria

- [ ] GIVEN `OnTaskListCompleted` fırlar (anı-tetikleyiciler doymadan), WHEN sistem event'i alır, THEN `RequestMovementLock(this, Full)` ardından `RequestHardCut(toScene, config, ...)` tam bir kez çağrılır, `config.Abrupt=false` ile (design-review, 2026-08-04 — full re-verification bulgusu, `Abrupt` parametresi eklendi, bkz. Core Rules "İki bitişin farklı tonu")
- [ ] **[design-review, 2026-08-04 — full re-verification bulgusuyla `FiredTriggerIds`/`OnTriggerFired`'dan `SettledTriggerIds`/`OnTriggerSettled`'e değiştirildi, saturation-timing düzeltmesi]** GIVEN gecenin tüm `MemoryTriggerDef`'leri `Held`'e ulaşmış olur (`SettledTriggerIds.Count == TotalConfiguredTriggerCountForNight`) VE `IsFinalRoundActive=true` VE `HasCarriedInFinalRound=true`, WHEN `OnTriggerSettled`, `OnFinalRoundStarted` ya da `OnFinalRoundItemPickedUp` fırlar (hangisi bu üç koşulu `true` yapan sonuncuysa), THEN `RequestMovementLock(this, Full)` ardından `RequestHardCut(toScene, config, ...)` tam bir kez çağrılır, `config.Abrupt=true` ile
- [ ] **[design-review, 2026-08-03 — eklendi, kritik bulgu; 2026-08-04'te `OnTriggerSettled`'e güncellendi]** GIVEN gecenin tüm `MemoryTriggerDef`'leri `Held`'e ulaşmış olur AMA `IsFinalRoundActive=false` (oyuncu son round'a henüz ulaşmadı), WHEN `OnTriggerSettled` fırlar, THEN `RequestHardCut` çağrılmaz — gece, görev döngüsü kendi son round'una ulaşana (ya da tamamlanana) kadar erken bitmez
- [ ] **[design-review, 2026-08-03 — verification N5 bulgusu, eklendi; 2026-08-04'te üçüncü koşulu ve `OnTriggerSettled`'i yansıtacak şekilde güncellendi]** GIVEN bir oyuncu tüm `MemoryTriggerDef`'leri round 1-2'de `Held`'e ulaştırmış (doygunluk koşulu `IsFinalRoundActive=false` yüzünden asılı kalmış), WHEN oyuncu daha sonra son round'a ulaşır ve `OnFinalRoundStarted` fırlar AMA `HasCarriedInFinalRound=false` (henüz hiç eşya almadı), THEN `RequestHardCut` **hâlâ** çağrılmaz — gece, son round'un malzemesinden en az biri elde alınana kadar bitmez
- [ ] **[BLOCKING, design-review 2026-08-04 — verification design-theory bulgusu, en kritik bulgu, eklendi]** GIVEN yukarıdaki durumun devamı (tüm tetikleyiciler `Held`, `IsFinalRoundActive=true`, `HasCarriedInFinalRound=false`), WHEN oyuncu son round'un ilk eşyasını alır ve `OnFinalRoundItemPickedUp` fırlar, THEN doygunluk koşulu bu event üzerinden yeniden değerlendirilir ve `true` bulunur, `RequestMovementLock(this, Full)` ardından `RequestHardCut` tam bir kez çağrılır (`config.Abrupt=true` ile) — bu, önceki taslağın "son round hiç oynanmadan gece biter" hatasının somut düzeltme testidir (bkz. `gdd-cross-review-2026-08-04.md`)
- [ ] **[design-review, 2026-08-04 — full re-verification bulgusu, eklendi, saturation-timing düzeltmesinin çekirdek testi]** GIVEN son tetikleyicinin Hold'u tamamlanmış (`FiredTriggerIds`'e girmiş) ama henüz `Held`'e ulaşmamış (`SettledTriggerIds`'e girmemiş), WHEN saturation koşulunun diğer iki şartı (`IsFinalRoundActive`, `HasCarriedInFinalRound`) zaten `true`, THEN `RequestHardCut` **henüz** çağrılmaz — ışığın rampası ve stinger'ın tamamlanması için gereken süre boyunca gece bitmez, sadece `Held` gerçekten ulaşıldığında (`OnTriggerSettled` fırladığında) tetiklenir
- [ ] **[design-review, 2026-08-04 — full re-verification bulgusu, eklendi]** GIVEN saturation koşulu tetiklenir, WHEN `RequestHardCut` çağrılır, THEN `config.Abrupt=true` iletilir; GIVEN görev-tamamlama koşulu tetiklenir, WHEN `RequestHardCut` çağrılır, THEN `config.Abrupt=false` iletilir — iki bitiş asla aynı `Abrupt` değerini paylaşmaz
- [ ] **[design-review, 2026-08-06 — re-verification bulgusu, eklendi, öncelik kuralının çekirdek testi]** GIVEN `OnTaskListCompleted` fırlar VE aynı karede doygunluk koşulunun üç event'inden biri de fırlar (tüm üç doygunluk şartı zaten `true`), WHEN sistem her ikisini de aynı karede alır, THEN `RequestHardCut` tam bir kez çağrılır, `config.Abrupt=false` ile (görev-tamamlama kazanır) — sonuç Unity'nin event-abone sırasından bağımsız, deterministiktir
- [ ] GIVEN `HasTriggeredThisNight=true`, WHEN diğer bitiş sinyali de gelir, THEN hiçbir ek `RequestHardCut` çağrısı yapılmaz
- [ ] GIVEN preload-eşiği koşullarından biri sağlanır, WHEN `PreloadHardCut` çağrılır, THEN gerçek tetiklemede sıfır-kare gecikme garantisi korunur
- [ ] GIVEN `RequestHardCut`'ın `onComplete`'i çağrılır, WHEN callback çalışır, THEN Gece/Oturum Durumu'nun oturum-sonlandırma çağrısı VE hareket-kilidi-serbest-bırakma çağrısı tam bir kez yapılır
- [ ] **[design-review, 2026-08-03 — eklendi]** GIVEN `RequestHardCut`'ın `onFailed`'i çağrılır, WHEN callback çalışır, THEN hareket kilidi serbest bırakılır, oturum sonlandırılmaz (`IsSessionActive` değişmez), `HasTriggeredThisNight` durumu implementasyon kararına bağlı (yeniden deneme mi, terminal hata mı — bkz. Open Questions)

## Open Questions

*(design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi: bu
Quick Spec önceden bir Open Questions bölümü hiç taşımıyordu.)*

- **`onFailed` sonrası retry mi, terminal hata mı?**: HARD CUT'ın
  `RequestHardCut` çağrısı başarısız olursa (hedef sahne yüklenemedi),
  bu sistem otomatik yeniden mi dener, yoksa gece'yi kurtarılamaz bir
  hata durumunda mı bırakır (`HasTriggeredThisNight` kalıcı olarak
  `true` kalır, ama gece hiç bitmemiş olur)? MVP'de bu son derece nadir
  bir senaryo (bozuk build/eksik referans) — implementasyon aşamasında
  karar verilebilir. **Owner**: implementasyon sahibi, dev-story
  aşamasında.

## Systems Index

Bu sistem zaten `design/gdd/systems-index.md`'de #12 olarak kayıtlı (Narrative, MVP, Quick Spec, şu an "Not Started"). Bu spec tamamlanınca durumu "Designed" olarak güncellenecek.
