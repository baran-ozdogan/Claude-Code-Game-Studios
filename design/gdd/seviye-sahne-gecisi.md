# Seviye/Sahne Geçişi (Scene Transition)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`)
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **Implements Pillar**: Pillar 2 (Sessiz Gerilim, Şok Değil), Pillar 3 (Görev Gerçekliği)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (kabul edildi, notlar eklendi) 2026-08-02 — reddedilen SOFT isteğine bildirim event'i eklendi, sıfır-kare HARD CUT algısal riski Open Questions'a taşındı
> **Design Review (2026-08-03)**: NEEDS REVISION → aynı oturumda revize
> edildi (Swapping/unload çelişkisi giderildi, zero-frame swap için
> `SWAP_FRAME_EPSILON` tanımlandı; SOFT'un gerçek yükleme-tamamlanma
> garantisi Duration knob'undan ayrıştırıldı; `Failed`→`Idle` çıkış
> yolu eklendi (AC-11a); `OnSoftTransitionRejected` kapsamı tüm SOFT
> reddetme durumlarına genişletildi (AC-3/AC-7); RenderSettings/lightmap
> stratejisi somutlaştırıldı, "paylaşılan Environment sahnesi" fikri
> terk edildi; `PreloadHardCut`'ın `CurrentState`'ten ayrı takibi
> netleştirildi; AC-12 için Blocked Acceptance Criteria tablosu
> eklendi) → APPROVED (2026-08-03, re-review yapılmadan) —
> bkz. `design/gdd/reviews/seviye-sahne-gecisi-review-log.md`
> **`/review-all-gdds` (2026-08-03)**: Sonradan Needs Revision'a
> düşürüldü — HARD CUT sting bağımlılığı, `onFailed`, `MovementLockScope`
> eklendi; N6 (sting SOFT/HARD ayrımı yapamıyor) 2026-08-03'te bu
> dosyanın kendi `OnTransitionStateChanged(newState, type)` imza
> değişikliğiyle çözüldü — bkz. Interactions with Other Systems. 2026-08-04
> verification turu bu dosyada N6'yı "hâlâ açık" diye tarif eden 4 ayrı
> bayat referans buldu (bu satır dahil, Open Questions, "Bağımlılık yönü",
> Dependencies) — hepsi bu turda düzeltildi.

## Overview

Seviye/Sahne Geçişi, oyun alanları (depo, servis koridoru, balo salonu) ile
psikiyatri seansı sahnesi arasındaki yükleme/kaldırma mekanizmasını
yönetir. İki farklı geçiş türü destekler: **yumuşak geçiş** (asansörle kat
değişimi — Görev Gerçekliği'ni koruyan, kesintisiz bir deneyim) ve **sert
kesme** (anı-tetikleyici tansiyonu doruğa ulaştığında psikiyatri sahnesine
ani kesme — Pillar 2'nin "sessiz gerilim" yapısının anlatı motoru). Bu
sistem, hangi sahnenin ne zaman yüklendiğine karar vermez — sadece diğer
sistemlerin (Asansör, Sahne Kesmeli Anlatı) talep ettiği geçişi teknik
olarak gerçekleştirir.

Oyuncu bu sistemi hiçbir zaman doğrudan görmez — sadece sonucunu (pürüzsüz
bir asansör yolculuğu ya da ani bir sahne kesmesi) deneyimler. Bu sistem
olmadan oyun tek bir statik alanla sınırlı kalır; Asansör ve Sahne Kesmeli
Anlatı sistemlerinin ikisi de bunun üzerine kurulu.

## Player Fantasy

Asansör kapanıp açıldığında zaman akıp gitmiş olsa da, elde tuttuğun kutu
hâlâ elinde, adım hâlâ aynı adım — bu geçiş sende hiçbir iz bırakmaz, çünkü
iş seni asla bırakmıyor (**Beden Sürekliliği**, Pillar 3: Görev
Gerçekliği). Ama bellek seni yakaladığında öyle olmuyor: orta cümlede,
orta hareketten koparılıyorsun, sanki biri konuşmanı bitirmene izin
vermeden ışığı kapatmış gibi (**Bedenin Çalınması**, Pillar 2: Sessiz
Gerilim, Şok Değil).

Biri "bedenin sürekliliği", diğeri "bedeninin sana sorulmadan alınması" —
aynı sistemin iki farklı rıza deneyimi. Bu ikisi arasındaki fark oyuncuya
hiçbir zaman açıklanmaz, sadece hissettirilir: biri güvenilir bir iş
rutini, diğeri kontrolün elinden alındığı bir an.

## Detailed Design

### Core Rules

- **Tek mekanizma, iki sunum**: Her ikisi de Unity additive scene loading
  (`LoadSceneAsync(mode: Additive)`) kullanır — ayrı kod yolu yok, sadece
  `config` (fade eğrisi, süre, preload zamanlaması) farklılaşır. Tek
  `SceneTransitionManager` + iki config tipi.
- **SOFT**: Hedef sahne additive yüklenir, iki sahne kısa süre eş-zamanlı
  resident kalır (asansör fiziksel bir hacim, oyuncu "yükleme sınırından
  geçerek" biner). Kamera hiç kesilmez, loading-screen yok. Kapı
  kapanışı/karartma diegetik bir maskeleme sağlar — bu bir "fade-to-black
  ekranı" değil, asansör kabini zaten karanlık/dar, gerçek dünya
  kaçamağı bu.
- **SOFT'un gerçek tamamlanma garantisi, Duration knob'undan bağımsızdır
  (design-review, 2026-08-03 — game-designer bulgusu, netleştirildi)**:
  `Preloading`→`Ready` geçişi **her zaman** gerçek `LoadSceneAsync`
  tamamlanmasını bekler (`isDone`/`progress>=1.0` ya da eşdeğeri) — Tuning
  Knobs'taki "SOFT geçiş minimum süresi" (2-8s) bir **taban/pacing**
  değeridir, bir tamamlanma tetikleyicisi değil. Yani `Swapping`
  (kapı açılışı/`onComplete`), gerçek yükleme bitmeden **yapısal olarak
  asla** gerçekleşemez — Tuning Knobs'taki "yükleme tamamlanmadan kapı
  açılma riski" notu, minimum süre çok düşük ayarlanırsa geçişin *hissedilen
  tempoda* çok ani/beklenmedik olması riskini anlatır (UX pacing), load'un
  gerçekten bitmeden `Ready`'ye geçtiği bir yarış durumu değil. Bu ayrım
  önceden GDD'de açık değildi; Asansör'ün kendi `onComplete`'e kadar
  bekleyen sırası (bkz. `asansor-kat-erisim-sistemi.md` Core Rules,
  "Sıralama") bu garantiye güvenir.
- **HARD CUT**: Aynı additive mekanizma, ama preload önceden tetiklenir —
  Sahne Kesmeli Anlatı, tansiyon eğrisinin doruğa yaklaştığını bildiği an
  `PreloadHardCut(toScene)` çağırır, hedef sahne arka planda `Ready`
  bekler. `RequestHardCut` çağrıldığında tek yapılan aynı karede aktif
  sahneyi değiştirmek: **gerçek sıfır fade, 0 kare siyah ara** — "orta
  cümlede ışığın kapanması" fantazisi bir loading hitch'iyle asla
  paylaşılamaz; her hitch bir "yükleniyor" okunur, oysa bu an bir
  çalınma anı.
- **"Swap" ile "unload" ayrımı (design-review, 2026-08-03 — unity-specialist
  bulgusu, çelişki giderildi)**: Önceki taslakta States and Transitions
  "Swapping = aktif sahne değişimi + eski sahne unload tetiklenir" diyordu,
  ama Tuning Knobs ayrı, gecikmeli bir "Eski sahne unload gecikmesi"
  (0.5-2s) öneriyordu — bu iki ifade çelişiyordu. Netleştirme: **sadece
  `SceneManager.SetActiveScene(toScene)` çağrısı zero-frame `Swapping`
  adımının parçasıdır** (bu, ucuz ve senkrondur). `UnloadSceneAsync(fromScene)`
  çağrısı bu adımda **tetiklenmez** — aktivasyondan Tuning Knobs'taki
  0.5-2s gecikmeyle sonra, ayrı bir arka plan adımı olarak başlatılır.
  Bunun nedeni mühendislik: `UnloadSceneAsync`, eski sahnedeki her nesne
  için senkron `OnDestroy` çağrıları tetikler — bu, aktif sahne zaten
  değişmiş ve oyuncu yeni sahneye baktığı için görünmez bir maliyettir,
  ama `Swapping` adımının kendisine dahil edilirse zero-frame garantisini
  bozardı. Oyuncu unload'ı hiç görmez çünkü zaten yeni sahnededir.
- **Zero-frame swap için ölçülebilir tolerans (design-review, 2026-08-03
  — qa-lead + unity-specialist bulgusu, somutlaştırıldı)**: "Gerçek sıfır
  fade, 0 kare siyah ara" iddiası, `RequestHardCut` çağrısı ile hedef
  sahnenin aktif olması arasında **`SWAP_FRAME_EPSILON = 1 kare
  (60fps hedefte ≤16.6ms, technical-preferences.md'nin frame budget'ı)`**
  ile ölçülür — `SetActiveScene` çağrısının kendisi zaten senkron
  olduğundan bu, "hiç" değil "1 kareden az" olarak test edilebilir bir
  garanti. Tamamen siyah render edilmiş kare sayısı her koşulda tam
  olarak **0** olmalıdır (bu değişmez, epsilon'suz — bir sahne swap'ında
  hiçbir ara siyah kare render edilmemesi bir tolerans meselesi değil,
  binary bir davranış). Bu iki değer AC-9'un doğrulama kriteridir (bkz.
  Acceptance Criteria).
- **Preload tam tamamlanmalı, kısmi bırakılmamalı (design-review,
  2026-08-03 — unity-specialist bulgusu, eklendi)**: `PreloadHardCut`'ın
  `Ready`'ye geçişi, `LoadSceneAsync`'in `allowSceneActivation=true` ile
  **%100 tamamlanmasını** bekler (Unity'nin `allowSceneActivation=false`
  ile ~%90'da bekletme deseni burada **kullanılmaz**) — bu, sahnedeki
  tüm `Awake`/`Start` maliyetlerinin `Ready`'ye ulaşılmadan önce, yani
  zero-frame `Swapping` adımından ÖNCE ödenmiş olmasını garanti eder;
  aksi halde swap anında beklenmedik `Awake`/`Start` maliyeti zero-frame
  garantisini bozardı. Bu önceden belgede hiç adlandırılmamıştı.
- **Movement-lock sahipliği bu sistemde değil**: FPC GDD'sindeki Asansör
  edge-case'i zaten netleştirdi — Asansör kilidi `MovementLockScope.MoveOnly`
  ile tutar, **platform-delta enjeksiyonuna gerek yoktur** (kabin hiç
  hareket etmediği için — design-review, 2026-08-03: önceki "hem kilit
  hem platform-delta enjekte etmeli" ifadesi, FPC'nin kendi Edge Case'inde
  zaten retract edilmiş bir iddiayı bu dokümana yanlışlıkla taşımıştı).
  Bu sistem `RequestMovementLock`/`ReleaseMovementLock`'a hiç dokunmaz —
  çağıran sistem (Asansör, Sahne Kesmeli Anlatı) kilidi kendi bağlamına
  göre çağırır/bırakır. Scene Transition, FPC'nin kendisi gibi saf
  altyapı kalır.
- **Teknik notlar (gameplay-programmer feasibility review)**: Static
  batching additively yüklenen sahneler arası birleşmez — draw call
  bütçesi buna göre ayrılmalı, birleşik optimizasyon varsayılmamalı.
  Unity 6.3'ün RenderGraph API değişiklikleri multi-scene camera
  stacking/lighting'i etkileyebilir — Detailed Design kilitlenmeden önce
  küçük bir teknik spike önerilir (bkz. Open Questions).
- **RenderSettings/lightmap stratejisi somutlaştırıldı (design-review,
  2026-08-03 — unity-specialist bulgusu; önceki "paylaşılan Environment
  sahnesinde birleştirilmeli" cümlesi bir yer tutucuydu, `RenderSettings`/
  `LightmapSettings.lightmaps`'in Unity'de sahne-başına global olduğu
  gerçeğini çözmüyordu)**: Baked lightmap verisi **sahne başına ayrı**
  kalır (birleştirilmez) — her alan (depo/koridor/balo salonu) kendi
  `LightmapData` dizisini korur, additive yükleme sırasında renderer'ların
  yanlış lightmap index'ine çözülmesi riski böylece oluşmaz. Skybox/fog/
  ambient (`RenderSettings`) için proje zaten Işık/Volume Durum
  Sistemi'nin kendi bölge-başına Volume Profile deseninden (bkz.
  `isik-volume-durum-sistemi.md` Core Rules) bir emsal taşıyor — aynı
  desen burada da kullanılır: her alan sahnesi kendi URP Volume
  profilini (fog/post-process için) taşır, `SceneManager.SetActiveScene`
  çağrısı sırasında **script-tabanlı bir `RenderSettings` senkronu**
  (skybox materyali + ambient) her sahnenin kendi yazarlı bir
  `SceneEnvironmentSettings` bileşeninden okunarak tetiklenir — legacy
  global `RenderSettings`'e doğrudan güvenilmez, "paylaşılan Environment
  sahnesi" fikri **terk edildi**.

### States and Transitions

`Idle` → (`RequestTransition`) → `Preloading` (additive load devam
ediyor) → `Ready` (yüklendi, henüz aktif değil) → `Swapping` (**sadece**
aktif sahne değişimi, `SetActiveScene` — zero-frame) → `Complete` → `Idle`.
**Eski sahnenin unload'ı `Swapping`'in bir parçası değildir** (design-review,
2026-08-03 — düzeltildi, bkz. Core Rules "Swap ile unload ayrımı"): unload,
`Complete`'e ulaşıldıktan sonra, Tuning Knobs'taki gecikmeyle (0.5-2s)
ayrı bir arka plan işlemi olarak tetiklenir — durum makinesinin kendisini
bloklamaz, `Idle`'a dönüş unload'ın bitmesini beklemez.

HARD CUT'a özel: `PreloadHardCut` çağrılırsa durum önceden
`Preloading`→`Ready`'ye ilerler ve **orada bekler**; `RequestHardCut`
sadece `Ready`→`Swapping` adımını (tek kare) tetikler. Preload
çağrılmadan doğrudan `RequestHardCut` çağrılırsa sistem yine çalışır ama
`Preloading` süresi boyunca gözle görülür bir gecikme riski vardır —
Sahne Kesmeli Anlatı'nın preload'u erkenden çağırması zorunlu pratik,
kural değil (bkz. Edge Cases).

**`Failed` terminal durumu**: Hedef sahne yüklenemezse durum makinesi
`Failed`'e geçer (bkz. Edge Cases) — `onComplete`/`onFailed` yine de
çağrılır, kilit askıda kalmaz. **`Failed` → `Idle` (design-review,
2026-08-03 — systems-designer + qa-lead bulgusu, eklendi)**: `onFailed`
callback'i çağrıldıktan hemen sonra, durum makinesi otomatik olarak
`Idle`'a döner — `Failed` "bu belirli istek başarısız oldu" anlamına
gelen bir terminal durumdur, "yönetici artık kalıcı olarak bozuk"
anlamına gelmez. Önceki taslakta bu geçiş hiç belgelenmemişti — `Failed`
kelimenin tam anlamıyla terminal okunabilirdi, yani tek bir bozuk sahne
referansı (eksik asset, bozuk build) oturumun geri kalanı boyunca **her**
gelecekteki geçiş isteğini kalıcı olarak reddedebilirdi (Edge Case 1'in
"reddet" kuralı `Preloading`/`Ready`/`Swapping`'i listeliyordu, `Failed`'i
değil — bu belirsizlik artık kapandı). Çağıran, `onFailed` içinde kendi
hata kurtarma mantığını (ör. tekrar deneme, oyuncuya diegetik bir "asansör
arızalı" sinyali) uygular; bu sistemin kendisi bir sonraki isteği kabul
etmeye hazırdır.

**Cross-type bekleyen slot**: Bir SOFT geçiş aktifken (`Preloading`/
`Ready`/`Swapping`) gelen bir `RequestHardCut`, tek bir "bekleyen" slotta
kuyruğa alınır ve SOFT `Idle`'a ulaştığında otomatik ateşlenir (bkz.
Edge Cases) — aynı türden art arda istekler için geçerli olan "reddet,
kuyruk yok" kuralından farklı, kasıtlı bir istisna.

### Interactions with Other Systems

**Dışa açılan arayüz**:
- `void PreloadHardCut(string toScene)` — arka planda additive yükler,
  `Ready`'de bekletir; tekrar çağrılırsa no-op.
- `void RequestSoftTransition(string fromScene, string toScene, SoftTransitionConfig config, Action onComplete, Action<string> onFailed)`
- `void RequestHardCut(string toScene, HardCutConfig config, Action onComplete, Action<string> onFailed)`
  — **`HardCutConfig.Abrupt` (bool) eklendi (design-review, 2026-08-04 —
  full re-verification bulgusu, kullanıcı kararıyla çözüldü)**: Sahne
  Kesmeli Anlatı'nın iki bitiş koşulu (görev tamamlama vs. anı-tetikleyici
  doygunluğu) artık farklı bir ses/görsel tonu talep ediyor — bkz.
  `sahne-kesmeli-anlati-2026-08-02.md` Core Rules, "İki bitişin farklı
  tonu". Bu sistem `Abrupt`'ı **sadece taşır, yorumlamaz** — zero-frame
  swap mekanizmasının kendisi `Abrupt` değerinden bağımsız olarak
  değişmeden kalır (AC-2'nin "ayrı kod yolu yok" garantisi bu alanla da
  bozulmaz, tıpkı `TransitionType` gibi). `bool GetCurrentHardCutAbrupt()`
  — aktif ya da preload edilmiş bir HARD CUT'ın `config.Abrupt` değerini
  döner (Adaptif Ses'in tek tüketicisi olduğu dar bir senkron sorgu —
  event payload'ını genişletmek yerine, bu projenin `GetStingerAudioRadius`/
  `IsShiftPersistent` sorgularıyla aynı desen); hiçbir HARD CUT preload
  edilmemiş/aktif değilse sonuç tanımsızdır, çağıran bunu sadece
  `OnTransitionStateChanged(Swapping, TransitionType.Hard)` aldığı
  karede sorgulamalıdır.
  — preload edilmemişse önce kendi içinde preload'u senkron gerekliliğe
  kadar bekler. **`onFailed` artık her iki imzada da açık bir parametredir
  (design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi)**:
  önceki taslakta AC-11/AC-11a "`onComplete` (ya da ayrı bir `onFailed`
  callback'i)" diye belirsiz bırakıyordu, ama arayüzde hiç yer almıyordu
  — bu, Asansör'ün `Failed` durumunu hiç işleyememesine yol açan kök
  sebepti (bkz. Edge Cases, yeni "Failed → Asansör" notu). `onFailed`,
  hata mesajını (`string`) taşır; `onComplete` ve `onFailed` **karşılıklı
  dışlayıcıdır** — bir geçiş için ikisinden sadece biri, tam olarak bir
  kez çağrılır.
- `TransitionState CurrentState` (salt okunur), `enum TransitionType { Soft, Hard }`,
  `event OnTransitionStateChanged(TransitionState newState, TransitionType type)`
  — **`type` parametresi design-review 2026-08-03 verification bulgusu (N6)
  kapatmak için eklendi**: SOFT ve HARD CUT, tek bir paylaşılan durum
  makinesi üzerinden çalıştığı için (bkz. AC-2), `type` olmadan `Swapping`
  event'i iki geçiş türü için de ayırt edilemezdi — Adaptif Ses Sistemi'nin
  HARD CUT Sting'i (bkz. `adaptif-ses-sistemi.md` Core Rules) sıradan bir
  asansör/seviye SOFT geçişinde de çalıyordu, bu da Pillar 2 (Sessiz
  Gerilim, Şok Değil) ihlali riski taşıyordu. `type`, çağrının
  `RequestSoftTransition` mi `RequestHardCut` mı olduğunu, durum makinesinin
  kendisi hâlâ paylaşılan/tek kalırken taşır — AC-2'nin "ayrı kod yolu yok"
  garantisini bozmaz, sadece dinleyicilere ek bilgi ekler.
- `event OnSoftTransitionRejected(string reason)` — bir `RequestSoftTransition`
  isteği reddedildiği **her** durumda fırlar (aktif bir HARD CUT
  nedeniyle ya da zaten aktif başka bir SOFT nedeniyle — `reason` hangisi
  olduğunu ayırt eder; bkz. Edge Cases "Kapsam genişletmesi"), Asansör
  sisteminin her reddetme durumunda diegetik bir tepkisizlik göstergesi
  sunabilmesi için

**Kilit ile ilişki (çağıran sorumluluğu)**: Asansör, `RequestMovementLock(this)`
çağırır → `RequestSoftTransition(...)` çağırır → `onComplete` içinde
`ReleaseMovementLock(this)` **VEYA** `onFailed` içinde aynı şekilde
`ReleaseMovementLock(this)` çağrılır ve kabin köken kata döner (bkz.
Edge Cases). Sahne Kesmeli Anlatı aynı sırayı `RequestHardCut` için
izler. Bu sistem `onComplete`/`onFailed` callback'lerinden **tam olarak
biri**nin çağrılacağını garanti eder ama kilidin varlığından habersizdir.

**Bağımlılık yönü**: Foundation katmanı, bağımlılığı yok. Asansör,
Sahne Kesmeli Anlatı, ve **Adaptif Ses Sistemi** *(design-review,
2026-08-03 — `/review-all-gdds` bulgusu, eklendi)* ona bağımlı —
Adaptif Ses `OnTransitionStateChanged(newState, type)`'e abone olarak,
sadece `newState == Swapping && type == TransitionType.Hard` iken HARD
CUT sting'ini tetikler (design-review, 2026-08-04 verification
bulgusuyla `type` filtresi eklendi — bkz. `adaptif-ses-sistemi.md` Core
Rules, "HARD CUT Sting"); aynı karede `GetCurrentHardCutAbrupt()`'u da
çağırır (design-review, 2026-08-04 full re-verification bulgusuyla
eklendi — bkz. Interactions with Other Systems, `HardCutConfig.Abrupt`).
Her üçü kendi Dependencies bölümünde bunu listelemeli (Adaptif Ses artık
listeliyor).

## Formulas

**N/A** — durum makinesi/event mantığı, sayısal hesaplama yok. Oyuncu
pozisyonunun kaynak sahneden hedef sahneye aktarımı anlık bir Transform
kopyalamadır (asansör kabini zaten karanlık, yumuşak bir blend'e gerek
yok) — bir formül değil.

**Koordinat çerçevesi hizalama kuralı (design-review, 2026-08-03 —
game-designer bulgusu, eklendi)**: Transform kopyalama bir formül
gerektirmese de, kaynak ve hedef kabin/oda arasında bir hizalama sözleşmesi
gerektirir — aksi halde oyuncu, "Beden Sürekliliği" fantazisini bozan bir
pozisyon sıçraması/dönüş atlaması yaşayabilir. Kural: her SOFT hedef
sahnesi, kaynak sahnenin kendi kabin-içi spawn noktasıyla **aynı yerel
(local) offset ve yön**'e sahip bir `SoftTransitionAnchor` işaretçisi
taşır — kopyalanan, dünya-uzayı pozisyonu değil, kabin-yerel pozisyon/
rotasyondur. Bu, level design/prefab kuralı olarak level-designer'a
devredilir, bu GDD sadece sözleşmeyi tanımlar.

## Edge Cases

- **Eğer `RequestSoftTransition` ya da `RequestHardCut`, `CurrentState`
  `Preloading`, `Ready` ya da `Swapping` iken **aynı türden** tekrar
  çağrılırsa**: İstek reddedilir (no-op, uyarı loglanır), devam eden
  geçiş dokunulmadan `Complete`'e ulaşır. Tek bir aktif geçiş slotu
  vardır — kuyruk ya da kesme yolu yok (aynı tür için). Sıralama
  gerektiren bir çağıran, bir sonraki isteği göndermeden önce
  `OnTransitionStateChanged(Idle)`'ı beklemelidir.
- **Eğer `PreloadHardCut(sceneB)`, daha önceki bir `PreloadHardCut`
  çağrısından `sceneA` hâlâ `Preloading`/`Ready` iken çağrılırsa**:
  Belgelenen "tekrar çağrılırsa no-op" kuralı gereği reddedilir — ama bu,
  sceneB'nin sessizce hiç preload edilmediği anlamına gelir, kuyruğa
  alınmaz. Sahne Kesmeli Anlatı, yeni bir hedefi preload etmeden önce
  `CurrentState == Idle` kontrolü yapmalıdır.
- **`PreloadHardCut`'ın kendi durum takibi, ana `CurrentState`'ten
  ayrıdır (design-review, 2026-08-03 — systems-designer bulgusu,
  netleştirildi)**: Önceki taslak `PreloadHardCut`'ın `Preloading`→`Ready`
  ilerlemesini paylaşılan `CurrentState` alanı üzerinden mi yoksa ayrı
  bir alan üzerinden mi takip ettiğini belirtmiyordu — bu, aktif bir
  SOFT geçiş sırasında `PreloadHardCut` çağrılırsa (olağan bir senaryo:
  asansör yolculuğu sırasında bir tansiyon tetikleyicisi önden preload
  etmek ister) doğrudan bir alan çakışmasına yol açardı. Netleştirme:
  `PreloadHardCut`'ın `Preloading`/`Ready` durumu **kendi ayrı dahili
  alanında** (`_hardCutPreloadState`, herkese açık `CurrentState`'ten
  bağımsız) takip edilir — bu, zaten var olan "Cross-type bekleyen slot"
  mekanizmasının (bkz. States and Transitions) neden mantıklı çalıştığını
  da açıklar: bir HARD CUT, aktif bir SOFT'un arkasında beklerken bile
  arka planda preload edilebilir, böylece SOFT `Idle`'a ulaştığında
  kuyruklanmış HARD CUT sıfır ek gecikmeyle (zaten `Ready`) ateşlenebilir.
  Yalnızca **herkese açık `CurrentState`** (SOFT'un kendi Preloading/
  Ready/Swapping'i ya da aktif bir HARD CUT'un Ready→Swapping'i) `OnTransitionStateChanged`
  event'ini fırlatan tekil, dışa-görünür alandır; `PreloadHardCut`'ın
  kendi arka plan ilerlemesi bunu asla değiştirmez.
- **Eğer `RequestHardCut`, hiç `PreloadHardCut` edilmemiş bir sahne için
  çağrılırsa ve farklı bir sahne önceki bir preload'dan `Ready`
  durumundaysa**: Sistem yönlendirme yapmaz — `RequestHardCut(toScene)`
  sadece `Ready` durumu `toScene` ile eşleşiyorsa onu kullanır; eşleşmeyen
  bir `Ready` sahne varken istenen sahne için senkron-bekleme fallback'i
  devreye girer, kullanılmayan preload edilmiş sahne açıkça unload
  edilene kadar yüklü ve boşa kalır.
- **Eğer hedef sahne yüklenemezse** (eksik referans, bozuk build): Yeni
  bir terminal durum olan **`Failed`**'e geçilir (sessizce `Idle`'a
  dönülmez), `OnTransitionStateChanged(Failed)` fırlar, ve `onFailed`
  (artık `onComplete`'ten ayrı, açık bir parametre — bkz. Interactions
  with Other Systems) tam olarak bir kez çağrılır — çağıranın
  movement-lock serbest bırakması askıda kalmasın diye (askıda kalan bir
  kilit bir tasarım inceliği değil, bir soft-lock hatasıdır).
  **`Failed` → Asansör tepkisi somutlaştırıldı (design-review, 2026-08-03
  — `/review-all-gdds` bulgusu, kritik bulgu)**: Önceki taslakta
  `asansor-kat-erisim-sistemi.md`'nin `Waiting` durumunun `Failed`'e
  hiçbir tepkisi yoktu (sadece `onComplete` ve
  `OnSoftTransitionRejected` işleniyordu) — bu, kabin `Waiting`'de
  sonsuza kadar takılı kalıp `RequestMovementLock`'un hiç serbest
  bırakılmadığı gerçek bir soft-lock'a yol açardı. Artık `onFailed`
  çağrıldığında Asansör bunu `OnSoftTransitionRejected` ile **aynı**
  şekilde işler — kabin köken katta `DoorsOpening`'e döner, hareket
  kilidi serbest bırakılır (bkz. `asansor-kat-erisim-sistemi.md`
  Edge Cases, güncellenen "Waiting sırasında başarısızlık" notu).
- **Eğer `onComplete` callback'inin kendisi bir istisna fırlatırsa**:
  İstisna `SceneTransitionManager`'ın dışına sızmamalı ve durum
  makinesini bozmamalı — dahili olarak yakalanır, loglanır, ve durum
  makinesi yine de `Complete → Idle`'a ilerler; aksi halde yönetici
  sonsuza kadar `Complete`'te takılı kalır ve sonraki her istek yukarıdaki
  ilk edge case gereği no-op olur.
- **Eğer bir HARD CUT, bir SOFT geçiş `Preloading`/`Ready`/`Swapping`
  durumundayken istenirse** (asansör transit halindeyken bir
  anı-tetikleyici tansiyonu doruğa ulaşırsa): **Reddedilmez — tek bir
  "bekleyen" slotta kuyruğa alınır** ve SOFT geçiş `Idle`'a ulaşır
  ulaşmaz otomatik olarak ateşlenir. Bu kasıtlı bir tasarım kararıdır:
  bir anlatı anının sessizce kaybolması, birkaç saniyelik bir gecikmeden
  çok daha kötü bir sonuçtur. Sahne Kesmeli Anlatı, kuyruğa alma
  gerçekleştiğinde bunu bilmeli (event ile bildirilir) ama tetikleme
  mantığını buna göre değiştirmesi gerekmez — sistem gecikmeyi kendi
  içinde yönetir.
- **Bekleyen slot doluyken ikinci bir `RequestHardCut` neden AC-6'da
  güvenle no-op olabiliyor (design-review, 2026-08-03 — systems-designer
  bulgusu, netleştirildi)**: Yüzeyde bu, yukarıdaki maddenin "bir anlatı
  anının kaybolması gecikmeden kötüdür" gerekçesiyle çelişiyor gibi
  görünür — ikinci bir `RequestHardCut`'ın sessizce reddedilmesi de bir
  kayıp değil mi? Hayır, çünkü bu sistemin tek amaçlanan çağıranı Sahne
  Kesmeli Anlatı, kendi quick-spec'inde ("Tekrar-tetiklenme guard'ı",
  `design/quick-specs/sahne-kesmeli-anlati-2026-08-02.md`) `RequestHardCut`'ı
  gece başına **tam bir kez** çağırdığını garanti eden kendi
  `HasTriggeredThisNight` bayrağını zaten taşıyor — o sistem ikinci bir
  `RequestHardCut` çağrısını kendi tarafında zaten hiç yapmaz. AC-6'nın
  senaryosu (ikinci bir `RequestHardCut` bekleyen slotu bulur) bu yüzden
  mevcut çağıran mimarisinde asla gerçekleşmez; bu sistem yine de
  savunmacı bir reddetme davranışı tanımlar (gelecekte farklı bir
  çağıran eklenirse), ama bu, "kaybolan bir anlatı anı" riski taşımaz —
  gerçek anlatı anı zaten tek seferlik üst-katman guard'ı tarafından
  korunuyor.
- **Eğer `RequestSoftTransition` ve `RequestHardCut`, aynı `toScene` ile
  ve sistem `Idle` iken art arda çağrılırsa**: İkisi de bağımsız
  ilerler — önceden yüklenmiş bir sahnenin paylaşılan bir önbelleği yok;
  bir SOFT `Complete`'den sonra unload edilen bir sahne, sonraki bir HARD
  CUT preload'u için örtük olarak tutulmaz.
- **Eğer çağıran, `onComplete` içinde `ReleaseMovementLock`'u hiç
  çağırmazsa**: Bu sistemin sahipliği dışında (belgede açıkça
  belirtildiği gibi), ama sistemler-arası bir risk olarak işaretlenmeye
  değer — Asansör/Sahne Kesmeli Anlatı'nın Acceptance Criteria'sı kilit
  serbest bırakma yolunu ayrı ayrı test etmeli, bu sistemin kendi testleri
  bunu yakalayamaz.
- **Eğer bir SOFT geçiş, bir HARD CUT `Preloading`/`Ready`/`Swapping`
  durumundayken istenirse** (ters yön — yukarıdaki HARD CUT-sırasında-SOFT
  durumunun tersi): **Reddedilir, kuyruğa alınmaz** — bu kasıtlı olarak
  asimetriktir. HARD CUT anlatısal olarak kritiktir ve kaybolmamalıdır
  (bu yüzden kuyruğa alınır), ama SOFT oyuncu tarafından başlatılır
  (asansör düğmesi) — reddedilirse oyuncu HARD CUT bittikten sonra
  düğmeye tekrar basabilir, anlatısal bir kayıp oluşmaz.
  **CD-GDD-ALIGN notu (2026-08-02, eklendi)**: Reddedilme, HARD CUT'ın
  kuyruklanma durumuyla aynı şekilde çağırana bir event ile bildirilir
  (`OnSoftTransitionRejected(reason)` ya da benzeri) — aksi halde
  Asansör sistemi düğme basımının neden yanıtsız kaldığını bilemez, bu da
  "bozuk girdi" gibi okunup Pillar 3'ün (Görev Gerçekliği) "gerçekçi/
  güvenilir" hissini zedeler. Asansör, bu event'i diegetik bir tepkisizlik
  göstergesiyle (ör. düğme ışığının yanmaması) eşleştirmelidir.
  **Kapsam genişletmesi (design-review, 2026-08-03 — systems-designer
  bulgusu, düzeltildi)**: `OnSoftTransitionRejected`, önceki taslakta
  metinsel olarak sadece "aktif bir HARD CUT nedeniyle reddedildiğinde"
  fırlıyormuş gibi kapsamlanmıştı. Ama bir SOFT isteği, `CurrentState`
  zaten **başka bir SOFT** tarafından işgal edildiğinde de (yukarıdaki
  ilk Edge Case, aynı-tür reddi) reddedilebilir — bu durumda
  `CurrentState`'in aktif türü HARD CUT değil, SOFT'un kendisidir. Event
  bu alt-durumda fırlamazsa oyuncu düğmeye basar, hiçbir tepki almaz, ve
  bu tam olarak CD-GDD-ALIGN'ın önlemeye çalıştığı "bozuk girdi" hissini
  yeniden üretir. Düzeltme: `OnSoftTransitionRejected(reason)`, bir
  `RequestSoftTransition`'ın reddedildiği **her** durumda fırlar —
  `reason` alanı hangi çakışma türü olduğunu ayırt eder (ör.
  `"AlreadyTransitioningSoft"` vs. `"HardCutActive"`), ama event'in
  fırlaması hiçbir zaman bu ayrıma bağlı değildir.

## Dependencies

**Bağımlıdır**: Yok — Foundation katmanı.

**Kendisine bağımlı olanlar**:
- **Asansör/Kat-Erişim Sistemi** *(tasarlandı)* — `RequestSoftTransition`
  çağırır, kendi movement-lock'unu yönetir, `onFailed`'i de artık işler
  (bkz. o GDD'nin kendi Edge Cases güncellemesi)
- **Sahne Kesmeli Anlatı** *(tasarlandı)* — `PreloadHardCut`/`RequestHardCut`
  çağırır, kendi movement-lock'unu yönetir
- **Görev/Taşıma Döngüsü** *(design-review, 2026-08-04 — verification
  bulgusu, eklendi — tek yönlü bağımlılık boşluğu kapatıldı)* — doğrudan
  çağrı yok, ama `gorev-tasima-dongusu.md`'nin kendi state'i (round/slot
  durumu) sahne-lokal olmayan bir kaynakta tutulur, bu sistemin
  depo↔balo salonu geçişlerinden etkilenmemesi gerektiği için
- **Adaptif Ses Sistemi** *(design-review, 2026-08-03 — eklendi, tasarlandı)* —
  `OnTransitionStateChanged(newState, type)`'e abone olur (sadece
  `newState == Swapping && type == TransitionType.Hard` işlenir —
  design-review, 2026-08-04 verification bulgusuyla düzeltildi), doğrudan
  bir çağrı yapmaz, sadece dinler
- **Ana Menü/Başlangıç Akışı** (Vertical Slice, henüz tasarlanmadı) — ilk
  sahne yüklemesi için kullanacak

**Not**: Ana Menü/Başlangıç Akışı henüz tasarlanmadı. Yazıldığında kendi
Dependencies bölümünde "Seviye/Sahne Geçişi"ni listelemeli (çift yönlü
tutarlılık — bkz. `design/gdd/systems-index.md`).

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| SOFT geçiş minimum süresi | 2–8 s | Yükleme tamamlanmadan kapı açılma riski | Gereksiz uzun asansör bekleme, Görev Gerçekliği'ni sıkıcılaştırır | Additive load süresi |
| Eski sahne unload gecikmesi | 0.5–2 s (swap sonrası) | Erken unload, geçiş sırasında pop riski | Bellekte gereksiz uzun süre iki sahne | Static batching, bellek bütçesi |
| HARD CUT preload penceresi | Sahne Kesmeli Anlatı'nın tansiyon eğrisine bağlı, tipik 1–3 s önden | Preload yetişmez, gecikme riski | Gereksiz erken bellek kullanımı | `PreloadHardCut` zamanlaması |

## Visual/Audio Requirements

Bu sistemin kendi görsel/ses varlığı yok — sadece sahne yükleme
mekanizması. HARD CUT'ın kendi ses gereksinimi (ör. kesmeyle eşleşen bir
sting) Adaptif Ses Sistemi'nin `OnTransitionStateChanged(newState, type)`'e
abone olup `type == Hard` filtresiyle sadece gerçek HARD CUT'ları
işlemesiyle sağlanacak (design-review, 2026-08-04 verification
bulgusuyla düzeltildi) — bu GDD'nin kapsamında değil.

## UI Requirements

Bu sistemin kendi UI'ı yok — saf altyapı.

## Acceptance Criteria

1. **GIVEN** `CurrentState` Idle, **WHEN** `RequestSoftTransition`
   çağrılır, **THEN** `OnTransitionStateChanged`, hiçbir durum
   atlanmadan/sırası değişmeden tam olarak
   Preloading→Ready→Swapping→Complete→Idle sırasıyla fırlar.
2. **GIVEN** `CurrentState` Idle, **WHEN** `RequestHardCut` hiç önceki
   bir `PreloadHardCut` olmadan çağrılır, **THEN** sistem yine aynı
   Preloading→Ready→Swapping→Complete→Idle dizisi üzerinden tamamlanır
   (senkron-bekleme fallback'i) — SOFT ve HARD CUT'ın tek bir paylaşılan
   durum makinesi üzerinden çalıştığını, ayrı kod yolları olmadığını
   kanıtlar.
3. **GIVEN** aktif bir SOFT geçişten `CurrentState` Preloading, Ready ya
   da Swapping, **WHEN** ikinci bir `RequestSoftTransition` çağrılır,
   **THEN** no-op olarak reddedilir, bir uyarı loglanır, devam eden geçiş
   etkilenmeden Idle'a tamamlanır, **VE** `OnSoftTransitionRejected
   ("AlreadyTransitioningSoft")` tam olarak bir kez fırlar. *(design-
   review, 2026-08-03 — systems-designer bulgusu: event'in kapsamı
   sadece HARD-CUT-aktifken-reddedilme durumundan, SOFT'un reddedildiği
   her duruma genişletildi — bkz. Edge Cases, "Kapsam genişletmesi.")*
4. **GIVEN** aktif bir HARD CUT'tan aynı durumlar, **WHEN** ikinci bir
   `RequestHardCut` çağrılır, **THEN** no-op olarak reddedilir, devam
   eden HARD CUT etkilenmeden tamamlanır (AC-3'ün diğer geçiş türü için
   aynası).
5. **GIVEN** aktif bir SOFT geçiş (Preloading/Ready/Swapping), **WHEN**
   `RequestHardCut` çağrılır, **THEN** reddedilmez — tek bir bekleyen
   slota kabul edilir, bir kuyruklama bildirimi fırlar, ve
   `CurrentState` Idle'a ulaştığı anda çağıranın tekrar çağrısına gerek
   kalmadan otomatik ateşlenir.
6. **GIVEN** AC-5 gereği zaten kuyruğa alınmış bir HARD CUT, **WHEN**
   o kuyruklanmış istek daha ateşlenmeden önce başka bir `RequestHardCut`
   çağrılır, **THEN** yeni çağrı no-op olarak reddedilir — sadece tek bir
   bekleyen slot vardır, çoklu öğeli bir kuyruk değil.
7. **GIVEN** aktif bir HARD CUT (Preloading/Ready/Swapping), **WHEN**
   `RequestSoftTransition` çağrılır, **THEN** reddedilir, kuyruğa
   alınmaz, **VE** `OnSoftTransitionRejected(reason)` tam olarak bir kez
   fırlar — AC-5'in kasıtlı asimetrik tersi (bkz. Edge Cases). *(design-
   review, 2026-08-03 — qa-lead bulgusu: önceki hali sadece reddedilme/
   kuyruğa-alınmama davranışını test ediyordu, CD-GDD-ALIGN'ın
   2026-08-02'de bu tam senaryo için eklediği bildirim event'inin
   fiilen fırladığını hiç doğrulamıyordu — bu AC'nin kendi gerekçe
   notunun test edilmeyen yarısıydı.)*
8. **GIVEN** `PreloadHardCut(sceneA)`'dan `CurrentState` Preloading ya da
   Ready, **WHEN** herhangi bir sahne için `PreloadHardCut` tekrar
   çağrılır, **THEN** no-op olur — ikinci bir yükleme başlamaz, orijinal
   preload hedefi/durumu değişmez.
9. **GIVEN** `PreloadHardCut(toScene)` tamamlandığı için `CurrentState`
   Ready, **WHEN** `RequestHardCut(toScene)` çağrılır, **THEN**
   Ready→Swapping aktif-sahne değişimi, `RequestHardCut` çağrısı ile
   hedef sahnenin aktif olması arasında **`SWAP_FRAME_EPSILON` (1 kare,
   60fps hedefte ≤16.6ms)**'dan az bir gecikmeyle tamamlanır (kare
   yakalama ya da swap-zaman damgası deltası ile doğrulanır), VE tamamen
   siyah render edilmiş kare sayısı her koşulda tam olarak **0**'dır
   (bu ikinci değişmez epsilon'suzdur — binary bir davranış, tolerans
   meselesi değil). *(design-review, 2026-08-03 — qa-lead + unity-
   specialist bulgusu: önceki "deltası ≈ 0 ile doğrulanabilir" ifadesi
   iki farklı iddiayı (kare-gecikmesi toleransı ve siyah-kare sayısı)
   tek, sayısız bir "≈0" ile birleştiriyordu — `isik-volume-durum-
   sistemi.md`'nin adlandırılmış epsilon sabitleri (TIME_EPSILON vb.)
   desenine hizalandı, bkz. Core Rules'taki `SWAP_FRAME_EPSILON` tanımı.
   Bu AC, swap'ın mekanik hassasiyetini test eder — geçişin oyuncuya
   "çalınma" değil "hata" gibi okunup okunmadığı ayrı, playtest-gated
   bir soru olarak Open Questions'ta (CD-PLAYTEST geçidi) kalmaya devam
   eder, bu AC'nin kapsamına girmez.)*
10. **GIVEN** bir geçiş Complete'e ulaşıp çağıranın `onComplete`'ini
    çağırır, **WHEN** `onComplete` bir istisna fırlatır, **THEN** istisna
    `SceneTransitionManager`'ın dışına sızmaz, dahili olarak
    yakalanır/loglanır, ve `CurrentState` yine de Idle'a ilerler (hemen
    sonra yeni bir istek göndererek, geçiş ortasındaymış gibi
    reddedilmediğini doğrulayarak test edilebilir).
11. **GIVEN** hedef sahne yüklenemiyor (eksik referans/bozuk build),
    **WHEN** yükleme hatası Preloading sırasında oluşur, **THEN**
    `CurrentState`, terminal `Failed` durumuna geçer (sessizce Idle'a
    değil), `OnTransitionStateChanged(Failed)` fırlar, ve
    `onComplete`/`onFailed` yine de tam olarak bir kez çağrılır.
11a. **GIVEN** `CurrentState` AC-11 gereği `Failed`'e ulaştı, **WHEN**
    `onFailed` callback'i çağrıldıktan hemen sonra durum örneklenir,
    **THEN** `CurrentState` otomatik olarak `Idle`'a döner (kalıcı bir
    kilitlenme değil), VE hemen ardından gönderilen yeni bir
    `RequestSoftTransition`/`RequestHardCut` normal şekilde kabul edilir
    (AC-1/AC-2'nin standart dizisini izler). *(design-review, 2026-08-03
    — systems-designer + qa-lead bulgusu, ikisi de bağımsız olarak
    buldu: önceki taslakta `Failed`'den bir çıkış yolu hiç
    belgelenmemişti — Edge Case 1'in reddetme kuralı sadece Preloading/
    Ready/Swapping'i listeliyordu, `Failed`'i değil, bu yüzden tek bir
    bozuk sahne referansı oturumun geri kalanı boyunca her gelecekteki
    geçişi kalıcı olarak soft-lock'layabilirdi — üretim durdurucu bir
    boşluk, kozmetik değil.)*
12. **ERTELENDİ** (bkz. aşağıdaki Blocked Acceptance Criteria tablosu):
    **GIVEN** bir çağıran movement-lock almış ve
    `RequestSoftTransition`/`RequestHardCut` çağırmış, **WHEN** geçiş
    Complete ya da Failed'e ulaşır, **THEN** çağıranın kilit-serbest
    bırakma mantığı çalışır. Bu sistemin kendi testleri sadece
    `onComplete`/`onFailed`'in fırladığını doğrulayabilir (AC-10, AC-11);
    kilidin gerçekten serbest bırakıldığını doğrulamak bu GDD'nin
    kapsamı dışında, Asansör'ün ve Sahne Kesmeli Anlatı'nın kendi
    Acceptance Criteria'sında yaşamalı.

### Blocked Acceptance Criteria (Deferred)

| AC | Blocked By | Closure Trigger | Owner |
|---|---|---|---|
| 12 (kilit-serbest bırakma, çağıran tarafında) | **Kapandı (design-review, 2026-08-04 — verification bulgusuyla düzeltildi)**: Asansör/Kat-Erişim Sistemi'nin kendi AC-9'u Asansör yarısını kapatıyor. Sahne Kesmeli Anlatı quick-spec'i artık kendi Acceptance Criteria listesinde iki kilit-serbest-bırakma AC'si taşıyor ("...VE hareket-kilidi-serbest-bırakma çağrısı tam bir kez yapılır" ve `onFailed` yolu için ayrı bir AC) — bu satır 2026-08-03'teki eklemeden sonra güncellenmemişti, bayat bir referanstı. | Kapandı — her iki taraf da kendi AC'sinde test ediyor | — |

*(design-review, 2026-08-03 — qa-lead bulgusu: önceki hali AC-12'yi
"Asansör ve Sahne Kesmeli Anlatı GDD'lerini gerektirir" olarak
ERTELENDİ işaretliyordu, ama her iki doküman da artık var — önerme
kısmen bayatlamıştı. `isik-volume-durum-sistemi.md`'nin Blocked-ACs
tablo desenine hizalandı, Owner/Closure Trigger eklendi.)*

## Open Questions

- **Unity 6.3 RenderGraph uyumluluğu**: Gameplay-programmer feasibility
  incelemesi, RenderGraph API değişikliklerinin multi-scene camera
  stacking/lighting'i etkileyebileceğini işaretledi — Detailed Design
  kilitlenmeden önce küçük bir teknik spike önerilir (henüz yapılmadı).
  Sahip: implementasyon öncesi teknik doğrulama.
- **SOFT geçiş minimum süresi kesin değeri**: Tuning Knob olarak 2-8s
  aralığı önerildi, playtest ile netleştirilecek. Sahip: sonraki
  playtest oturumu.
- **Sıfır-kare HARD CUT algısal riski (CD-GDD-ALIGN, 2026-08-02)**: Bu,
  belgede en özgün/alışılmadık karar — bir fade olmaması "kopuş" olarak
  mı yoksa "hata/glitch" olarak mı okunacak, bu tamamen Adaptif Ses
  Sistemi'nin **(artık tasarlanmış — design-review, 2026-08-03: bu
  sistem şimdi bir "HARD CUT Sting" implemente ediyor,
  `OnTransitionStateChanged(newState, type)`'e abone, sadece `type ==
  Hard` işler, bkz. `adaptif-ses-sistemi.md` Core Rules)** kesme
  karesiyle tam senkronize bir sting sağlamasına bağlı. **N6 (SOFT/HARD
  ayrımı) 2026-08-03'te çözüldü** — bu satır 2026-08-04 verification
  turuna kadar hâlâ "henüz ayırt edemiyor" diyordu, bayat bir referanstı,
  düzeltildi. Playtest ile doğrulanmalı — eğer "hata" gibi okunursa, çok
  kısa (1-3 kare) bir fade'e revize edilmesi gerekebilir. **Ayrı,
  hâlâ açık bir soru (design-review, 2026-08-04 — verification design-theory
  bulgusu)**: rupture-vs-error sorusu hiç "startle/jump-scare" sorusuyla
  birlikte değerlendirilmedi — sıfır-geri-bildirimli teslimat + anlık tam
  hareket kilidi + sıfır-kare swap + aynı karede tüm ambiyansın kesilip
  CutSting'in girmesi, üst üste bindiğinde klasik bir startle imzası
  oluşturabilir; anti-pillar ("NOT ucuz jump-scare'ler") bu spesifik
  diziye karşı hiç test edilmedi. Sahip: CD-PLAYTEST geçidi, bu soruyu da
  kapsayacak şekilde genişletilmeli.
  **İkincil/ses-bağımsız sinyal eksikliği (design-review, 2026-08-03 —
  game-designer bulgusu)**: Şu an "çalınma, hata değil" okumasının
  **tek** taşıyıcısı Adaptif Ses'in senkronize sting'i — ses gecikirse/
  çalmazsa hiçbir yedek sinyal yok. Bu GDD kendi başına bir fallback
  icat etmemeli (Visual/Audio Requirements'ın "kendi görsel/ses varlığı
  yok" ilkesiyle çelişir), ama hedef psikiyatri sahnesinin **kendi açılış
  durumu** (cümle ortasında diyalog, kamera önceden çerçevelenmiş) ikinci,
  ses-bağımsız bir süreklilik ipucu taşıyabilir — Player Fantasy metninin
  zaten ima ettiği ("orta cümlede... koparılıyorsun") ama hiçbir GDD'nin
  açık bir gereksinim olarak sahiplenmediği bir nokta. Sahip: Sahne
  Kesmeli Anlatı / Diyalog İçeriği (hedef sahnenin açılış içeriği bu
  sistemlerin kapsamında) — implementasyon öncesi netleştirilmeli.
- **Asansör kabini paylaşım sorusu, sahibi yanlış atanmış (design-review,
  2026-08-03 — unity-specialist bulgusu)**: `asansor-kat-erisim-sistemi.md`
  Open Questions #1 ("kabin nesnesi paylaşılan tek GameObject mi, kat
  başına ayrı mı") ve bu GDD'nin "RenderSettings/lightmap stratejisi"
  notu aynı sorunun iki yüzü, ama Asansör dokümanı bu sorunun sahibini
  "unity-specialist" olarak atıyor — bu rol kendi tanımı gereği tasarım
  kararı vermez, sadece uygular. Gerçek sahip bir tasarım/mimari kararı
  verecek biri olmalı (ör. level-designer + technical-director birlikte,
  ya da bir ADR). Sahip düzeltmesi bu GDD'nin kapsamı dışında —
  `asansor-kat-erisim-sistemi.md`'nin kendi Open Questions'ı bu notla
  güncellenmeli, burada sadece çapraz-referans.
