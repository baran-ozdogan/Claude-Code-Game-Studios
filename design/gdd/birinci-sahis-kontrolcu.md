# Birinci Şahıs Kontrolcü (First-Person Controller)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`)
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **Implements Pillar**: Pillar 2 (Sessiz Gerilim, Şok Değil), Pillar 3 (Görev Gerçekliği)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (revised and accepted) 2026-08-01 — yaklaşma-yavaşlaması tüm etkileşim nesnelerine kamuflaj için genişletildi (bkz. Core Rules notu)
> **`/review-all-gdds` (2026-08-03)**: `MovementLockScope` eklendi, AC#16 daraltıldı, stale Dependencies/platform-delta iddiaları düzeltildi — bkz. `design/gdd/gdd-cross-review-2026-08-03-verification.md`

## Overview

Birinci Şahıs Kontrolcü, oyuncunun otel içindeki fiziksel varlığını yönetir:
klavye/gamepad girdisini hareket ve bakış açısına çevirir, ve oyuncunun
pozisyon/yön/göz-kamerası verisini diğer tüm sistemlerin (Etkileşim, Asansör,
Görev Döngüsü) üzerine kurulacağı temel arayüz olarak dışa açar. Oyuncu bu
sistemle sürekli ve doğrudan etkileşir (her an hareket/bakış girdisi), ancak
sistem kendisi saf altyapıdır — ürettiği *deneyim* (meditatif ama huzursuz bir
tempo) diğer sistemler aracılığıyla hissedilir, kontrolcünün kendisi bir
"fantezi" taşımaz.

Bu sistem olmadan oyun var olamaz — her etkileşim, her keşif, her
anı-tetikleyici karşılaşma bu temel üzerine kuruludur.

## Player Fantasy

Oyuncu burada bir güç fantazisi yaşamaz — tam tersi. Beden, bu işi zaten
biliyormuş gibi hareket eder: adımlar tanıdık bir ritimde düşer, tıpkı
yüzlerce kez yürünmüş bir koridorda olduğu gibi (**Bedenin Hafızası**,
Pillar 3: Görev Gerçekliği). Ama bu tanıdıklık hiçbir zaman rahatlığa
dönüşmez — otel, koşma isteğini bastıran, "buradan çabuk çıkamazsın" diyen
bir tempo dayatır (**Otel Seni Yavaşlatıyor**, Pillar 2: Sessiz Gerilim, Şok
Değil). Hareket hızı bilinçli bir tasarım kısıtı olarak hissettirilmeli:
oyuncu bunu kendi kararıymış gibi algılar, ama aslında kontrolcünün temposu
bunu dayatıyordur.

Anlık deneyim: yükü taşırken hızlanamama hissi — işi bitirmek **istiyorsun**
(design-review, 2026-08-04 — verification bulgusuyla düzeltildi: bu satır
önceden "aciliyet var, gece bitmeden iş bitmeli" diyordu — bu, hiçbir
sistemde karşılığı olmayan bir saat/bedel ima ediyordu, bkz.
`game-concept.md` Core Loop notu; gerçek olan şey bir zaman baskısı değil,
bedenin kendi fiziksel sınırı) ama beden buna izin vermiyor — taşıma hızı
sabit, ne kadar istersen iste değişmiyor. Bir kapıya ya da anı-tetikleyici
bir alana yaklaşırken adımların istemsizce yavaşlaması, oyuncunun "neden
yavaşladım?" diye sorgulamasına yol açmalı — cevap net değil, ama his
gerçek.

## Detailed Design

### Core Rules

- **Yürüme hızı**: 1.6 m/s (yüksüz), 1.35 m/s (yük taşırken — Görev/Taşıma
  Döngüsü `SetCarrying(bool)` ile bildirir). **Koşu yok** — hiçbir tuşa
  bağlanmamış.
- **İvmelenme**: ~0.15-0.25s tam hıza çıkış, benzer yavaşlama süresi —
  "araç" değil "ritme oturan beden" hissi.
- **Yaklaşma-yavaşlaması**: Bayraklı **herhangi bir etkileşim nesnesine**
  (anı-tetikleyici olsun olmasın — sıradan etkileşim nesneleri de aynı
  bayrağı taşır) 1.5m yaklaşıldığında hedef hız o mesafe boyunca kademesiz
  şekilde %30'a kadar azalır — yavaşlama eğrisinin kendisi "isteksiz"
  hissettirir. (Kamera sallantısı bundan etkilenmez — öznellik işini
  tamamen Işık/Volume sistemine bırakıyoruz.)
  > **Kasıtlı kamuflaj (CD-GDD-ALIGN, 2026-08-01, CONCERNS — çözüldü)**:
  > Bu etki SADECE anı-tetikleyicilere uygulansaydı, dikkatli bir oyuncu
  > yavaşlamayı bir "metal dedektörü" gibi kullanıp her tetikleyiciyi
  > mekanik olarak haritalayabilirdi — bu Pillar 5'i (Anlam Sona Saklı)
  > zayıflatırdı. Bayrağın tüm etkileşim nesnelerinde paylaşılması,
  > yavaşlamanın kendisini anlamsız bir sinyale çevirir: oyuncu bunu
  > hissedebilir ama "gerçek" bir ipucu mu yoksa sıradan bir nesne mi
  > olduğunu ayırt edemez.
  > **Kamuflajın gerçek içerikle çökmesi ve düzeltmesi (design-review,
  > 2026-08-04 — verification design-theory bulgusu, en önemli
  > bulgulardan biri, kullanıcı kararıyla çözüldü)**: Bu kamuflaj sadece
  > registry'de anı-tetikleyicilerle **birlikte başka nesneler de** her
  > zaman kayıtlıyken işe yarar. Ama gerçek içerik dağılımında: Servis
  > Koridoru ve Balo Salonu'nda **hiç** başka `IInteractable` yok (asansör
  > çağrı düğmesi ve teslimat bölgesi kasıtlı olarak `IInteractable`
  > değil, bkz. `asansor-kat-erisim-sistemi.md`/`gorev-tasima-dongusu.md`);
  > Depo'da taşıma eşyaları sadece alınana kadar kayıtlı kalır, alınır
  > alınmaz registry'den çıkar (bkz. `gorev-tasima-dongusu.md` Core Rules,
  > "Alma"). Sonuç: oyun süresinin büyük kısmında, bu alanlarda yavaşlama
  > **%100 kesinlikle** bir anı-tetikleyiciyi işaret eder — kamuflajın
  > önlemeye çalıştığı tam senaryo. **Düzeltme**: her üç MVP alanına
  > (Depo, Servis Koridoru, Balo Salonu) **sahte/dekor `IInteractable`
  > nesneleri** içerik gereksinimi olarak eklenir — `Instant` tipinde,
  > minimal/tatsız bir tepkiyle (kapı kolu, ışık anahtarı, termostat,
  > temizlik arabası freni — Etkileşim Sistemi'nin kendi "bir kapı koluna
  > basmak" örneğiyle tutarlı, bkz. `etkilesim-sistemi.md` Core Rules),
  > hiçbir mekanik etkisi olmadan sadece registry'yi "gürültülü" tutmak
  > için var olurlar. Bunlar Pillar 3'ü (Görev Gerçekliği) de besler —
  > gerçek bir otel servis alanında zaten olması beklenen nesneler,
  > yapay bir oyun sistemi değil. **Minimum sayı ve yerleşim** içerik
  > yazımı/level tasarımı aşamasında belirlenecek (bkz.
  > `gdd-cross-review-2026-08-04.md`) — bu belge sadece gereksinimi
  > kilitler, kesin sayıyı değil.
- **Kamera sallantısı**: Sabit, düşük genlikli, footstep-tabanlı head-bob —
  yorgunluk/yük hissi verir, asla bulantı yaratacak kadar güçlü değil.
  Erişilebilirlik slider'ı (0-100%, varsayılan ~%40) + kapatma seçeneği.
- **Çömelme/eğilme**: Yok — kombat/risk olmadığı için gerekçesi yok, kasıtlı
  olarak kesildi.
- **Bakış**: Mouse-look, pitch -80°/+80° kelepçeli, yaw sınırsız. FOV
  kick/roll yok.
- **FOV**: Varsayılan 75-80°, slider 60-100°.
- **Hassasiyet**: Slider + invert-Y anahtarı, varsayılan düşük-orta.
- **Çarpışma**: CharacterController kapsülü; step offset ve skin width,
  4m×4m modüler ızgara koridorlarına göre ayarlanacak (Tuning Knob olarak
  işaretlendi, playtest ile ince ayar).

### States and Transitions

| Durum | Tetikleyici |
|---|---|
| **Idle** ↔ **Walking** | WASD/stick girdi büyüklüğü eşiği |
| → **Carrying** | Görev/Taşıma Döngüsü `SetCarrying(true)` çağırır (Walking + taşıma hız çarpanı + taşıma görsel katmanı) |
| Herhangi biri → **Locked** | Dış bir sistem `RequestMovementLock(requester, scope)` çağırır — `scope`'a göre `Move` (her zaman) ve isteğe bağlı `Look` donar (bkz. Interactions with Other Systems, "Kilit kapsamı") |
| **Locked** → önceki durum | Sadece kilidi isteyen sistem `ReleaseMovementLock(requester)` çağırdığında — **oyuncu girdisiyle asla açılmaz** (input mashing kilidi bozamaz) |

### Interactions with Other Systems

**Dışa açılan arayüz** (`IPlayerState`): `EyeCamera` (Transform, salt
okunur), `Velocity`, `IsGrounded`, `MovementLocked` (salt okunur),
`IsCarrying`, `MovementLockChanged` event, **`IsLocked`** (salt okunur
bool, design-review 2026-08-03 eklendi — bkz. aşağıda). `CharacterController`
doğrudan dışa açılmaz — diğer sistemler pozisyonu etkilemek isterse
sarmalanmış bir `Move()` çağrısı kullanır (çakışan `.Move()` çağrılarını
önlemek için).

**İçe alınan çağrılar**:
- `RequestMovementLock(object requester, MovementLockScope scope = MovementLockScope.Full)` /
  `ReleaseMovementLock(object requester)` — **referans sayaçlı**
  (`Dictionary<object, MovementLockScope>`, düzeltildi — bkz. ADR-0002,
  TD-ADR bulgusu 2026-08-05: bir `HashSet<object>` her istekçinin kendi
  kapsamını saklayamaz, bu da aynı paragrafın "en kısıtlayıcı kazanır"
  kuralını hesaplanamaz kılardı; dışa açık API imzası değişmedi, sadece
  bu iç veri yapısı), bool değil; Asansör, Sahne Kesmeli Anlatı ve
  Etkileşim (basılı tutmalı etkileşimler) aynı anda kilit isteyebilir,
  biri yanlış sırada bırakırsa diğerini bozmamalı.
  **Kilit kapsamı — açık parametre (design-review, 2026-08-03 —
  `/review-all-gdds` bulgusu, eklendi, kritik bulgu)**: Önceki taslakta
  `requester` bare bir identity'ydi, kapsam parametresi yoktu — ama üç
  farklı çağıran üç farklı davranış bekliyordu (Sahne Kesmeli Anlatı:
  Move+Look donuk, kamera dışarıdan sürülür; Asansör: sadece Move donuk,
  Look serbest; Etkileşim: sadece Move donuk, Look serbest — kendi
  Hold-iptal Edge Case'i oyuncunun bakışını çevirebilmesini
  gerektiriyor). Düzeltme: `enum MovementLockScope { Full, MoveOnly }`.
  `Full` (varsayılan) — hem `Move` hem `Look` donar, kamera dışarıdan
  sürülebilir (cutscene/Sahne Kesmeli Anlatı). `MoveOnly` — sadece
  `Move` donar, `Look` oyuncu kontrolünde kalır (Asansör, Etkileşim'in
  Hold'ları). **Birden fazla aktif kilit varsa, efektif kapsam en
  kısıtlayıcı olanıdır** (herhangi biri `Full` istiyorsa, `Look` da
  donar — referans sayımı `Move` için zaten geçerli olan "en az bir
  istekçi varsa kilitli" mantığının `Look` için genellemesi).
  **`IsLocked`**: yeni salt-okunur bool, "şu an herhangi bir istekçi
  kilidi tutuyor mu" sorusuna cevap verir — Etkileşim'in kendi mutual-
  exclusion kontrolü (`RequestMovementLock` çağırmadan önce kilidin
  BAŞKA bir sistemde olup olmadığını anlaması) için gerekli, önceki
  taslakta sadece bir bool `MovementLocked` vardı ama Etkileşim'in
  "kilit benim değil, başkasının" ayrımını yapabilmesi belirsizdi.
- `SetCarrying(bool)` — sadece Görev/Taşıma Döngüsü.
- Etkileşim sistemi kendi raycast'ini `EyeCamera` üzerinden yapar — bu
  sistem Etkileşim'e uzanmaz, saf altyapı olarak kalır.

**Input System**: Tek bir **"Gameplay"** action map — `Move` (Vector2),
`Look` (Vector2), `Interact` (Button). Gamepad ikincil binding şeması
olarak eklenir, MVP için ayrı remap UI yok.

**Açık not — retracted (design-review, 2026-08-03 — `/review-all-gdds`
verification bulgusu, düzeltildi)**: Bu not önceden CharacterController'ın
hareketli platform hızını otomatik miras almadığını, Asansör'ün ya
yeniden-ebeveynleme ya da platform-delta enjeksiyonu yapması gerektiğini
söylüyordu — bu iddia, bu dosyanın kendi Edge Case'inde ("Eğer oyuncu
Asansör'e binerse") ve Dependencies bölümünde zaten retract edilmişti
(kabin hiç hareket etmediği için gerek yok), ama bu paragrafta üçüncü
bir kopya olarak hayatta kalmıştı. Asansör sadece `RequestMovementLock
(this, MovementLockScope.MoveOnly)`/`ReleaseMovementLock(this)` çağırır
— platform-delta enjeksiyonuna hiç gerek yoktur.

## Formulas

### 1. İvmelenme (Speed Smoothing)

**İvmelenme** formülü şu şekilde tanımlanır:
`v(t+Δt) = v_target + (v(t) - v_target) × e^(-k·Δt)`, `k = 3 / T_ramp`

**Değişkenler:**

| Değişken | Sembol | Tip | Aralık | Açıklama |
|---|---|---|---|---|
| Anlık hız | v(t) | float | [0, 1.6] m/s | Bu karedeki gerçek hareket hızı |
| Hedef hız | v_target | float | [0, 1.6] m/s | Taşıma + yaklaşma-yavaşlaması çarpanlarıyla bileşik hedef (bkz. Formül 2) |
| Kare süresi | Δt | float | (0, ~0.033] s | Frame delta time |
| Yumuşatma oranı | k | float | 12–20 /s | `T_ramp`'ten türetilir |
| Ramp süresi | T_ramp | float | [0.15, 0.25] s | Kilitli Core Rule değeri |

**Çıktı Aralığı:** 0 – 1.6 m/s; `v(t)` `v_target`'a üstel olarak yaklaşır, hiç aşmaz (overshoot yok), `T_ramp` içinde ~%95'e ulaşır. Aynı formül hem hızlanma hem yavaşlama için geçerlidir (`v_target` hangi yönde olursa olsun). Δt büyüdüğünde (düşük FPS) bile kararlı kalır — analitik çözüm, Euler entegrasyonu değil. Yaklaşma-yavaşlaması ile çakışmaz çünkü taper yalnızca `v_target`'ı değiştirir; bu formül tek bir hedefi takip eder, iki ayrı sistem aynı anda hızı itmez.

**Örnek:** T_ramp = 0.2s → k = 15/s. Oyuncu duruyorken (v=0) ileri basar, v_target = 1.6 m/s. 60fps'te (Δt=0.0167s): e^(-15×0.0167) ≈ 0.779 → v = 1.6 + (0-1.6)×0.779 ≈ 0.35 m/s (1. kare). ~0.2s (12 kare) sonra v ≈ 1.6×(1-e^-3) ≈ 1.52 m/s (%95).

### 2. Yaklaşma-Yavaşlaması Taper'ı

**Taper** formülü şu şekilde tanımlanır:
`x = clamp(d/1.5, 0, 1)`; `ease(x) = x²(3-2x)`; `TaperMult = 0.7 + 0.3 × ease(x)`
`v_target = 1.6 × CarryMult × TaperMult`

**Değişkenler:**

| Değişken | Sembol | Tip | Aralık | Açıklama |
|---|---|---|---|---|
| Mesafe | d | float | [0, 1.5] m | En yakın bayraklı nesneye mesafe (anı-tetikleyici + sıradan etkileşim nesneleri dahil — kamuflaj amaçlı, bkz. Core Rules) |
| Normalize mesafe | x | float | [0, 1] | d'nin 1.5m'ye oranı |
| Taper çarpanı | TaperMult | float | [0.7, 1.0] | Smoothstep eğrisiyle yumuşatılmış hız çarpanı |
| Taşıma çarpanı | CarryMult | float | {0.84375, 1.0} | 1.35/1.6 (taşırken) veya 1.0 |

**Çıktı Aralığı:** v_target 0.945 – 1.6 m/s. d≥1.5'te TaperMult=1.0 (etkisiz); d=0'da TaperMult=0.7 (maksimum yavaşlama). Çarpanlar **çarpımsaldır**, öncelik yoktur — her iki kısıt da orantısal ("kapasitenin %X'i") olduğundan çarpımsal bileşim, taşırken de taşımazken de aynı görece %30 yavaşlama hissini korur; toplamsal olsaydı taşıyan oyuncu için orantısız büyük bir kesinti hissedilirdi. Sıra önemsizdir (a×b = b×a), bu yüzden "biri öncelikli" kuralına gerek yok.

**Örnek:** Taşırken (CarryMult=0.84375), d=0.5m → x=0.333, ease=0.259, TaperMult=0.778. v_target = 1.6×0.84375×0.778 ≈ 1.05 m/s (normal taşıma hızı 1.35'ten belirgin yavaşlama).

### 3. Head-Bob Genliği

**Head-bob genliği** formülü şu şekilde tanımlanır:
`Amplitude(v, S) = A_max × (v / 1.6) × (S / 100)`

**Değişkenler:**

| Değişken | Sembol | Tip | Aralık | Açıklama |
|---|---|---|---|---|
| Genlik | Amplitude | float | [0, A_max] cm | Çıktı — kamera dikey ofset genliği |
| Maks. genlik | A_max | float | Tuning Knob (~1–3 cm) | Tasarım sabiti, düşük tutulacak |
| Anlık hız | v | float | [0, 1.6] m/s | Formül 1'in çıktısı |
| Slider değeri | S | float | [0, 100] % | Erişilebilirlik slider'ı, varsayılan 40 |

**Çıktı Aralığı:** 0 – A_max. S=0'da Amplitude **tam olarak 0** (çarpımsal terim garanti eder, "çok düşük" değil). v=0'da (dururken) da 0 — footstep-tabanlı olduğundan doğal sonuç. S=100 ve v=1.6'da Amplitude=A_max (tavan).

**Örnek:** A_max=2.5cm varsayımıyla, unloaded tam hızda (v=1.6) varsayılan S=40: Amplitude = 2.5×1.0×0.4 = 1.0 cm. S=0: Amplitude=0 (hız ne olursa olsun).

## Edge Cases

- **Eğer birden fazla bayraklı nesne 1.5m yarıçapında aynı anda varsa**
  (bitişik iki kapı gibi): d = tüm menzildeki bayraklı nesnelere olan
  mesafelerin minimumu, "en son izlenen nesne" değil. Minimum-mesafe geçiş
  noktalarında süreklidir (iki mesafe eşitken kesişir), bu yüzden
  TaperMult bölgeler arası geçişte asla sıçramaz ya da çift uygulanmaz.
- **Eğer Δt normalden çok büyükse** (frame hitch, asset yükleme takılması):
  `e^(-k·Δt) → 0`, yani `v(t)` bir sonraki karede doğrudan `v_target`'a
  atlar, yumuşatma yapılmaz. Δt kırpmaya gerek yok — bir hitch zaten
  algılanabilir pürüzsüzlüğü bozuyor, stall boyunca bayat bir hız taşımaktansa
  anında yakalama daha iyi.
- **Eğer aynı istekçi, araya bir Release girmeden RequestMovementLock'u iki
  kez çağırırsa** (örn. basılı-tut etkileşim yeniden tetiklenirse):
  `HashSet.Add` yinelenen çağrıda no-op olur; o istekçiden gelen tek bir
  `ReleaseMovementLock` kilidi tamamen temizler. İstekçiler Request/Release'i
  sayıya göre değil, kimliğe göre eşleştirmelidir.
- **Eğer ReleaseMovementLock, o an kilit sahibi olmayan bir istekçi
  tarafından çağrılırsa** (çifte-release hatası, başka bir sistemle yarış
  durumu): `HashSet.Remove` üye olmayan bir öğede sessizce no-op olur —
  istisna fırlatmaz, durum değişmez, diğer kilit sahipleri etkilenmez.
- **Eğer RequestMovementLock, `v(t) > 0` iken tetiklenirse** (cutscene
  yürüyüş/taşıma ortasında araya girerse): `v_target` 0'a çekilir ama
  Formül 1 çalışmaya devam eder, yani `v(t)` ışınlanarak durmak yerine
  normal `T_ramp` süresinde yavaşlar. `IsCarrying` ve görsel katmanı
  Locked boyunca dokunulmadan kalır. Head-bob için ayrı bir dondurma
  mantığına gerek yok — `v→0` olunca Amplitude de otomatik olarak 0'a
  gider (Formül 3).
- **Eğer CharacterController step offset, 4m×4m kitin bir kapı eşiği/zemin
  dikişi yüksekliğine eşit ya da büyükse**: her eşik geçişinde kamera
  zıplar. Çözüm: step offset, kitin en küçük eşiğinin (~2cm) altında
  tutulur; daha yüksek eşikler step değil, slopeLimit rampası olarak
  tasarlanır — step offset sadece kasıtlı küçük basamaklar için ayrılır.
- **Eğer skin width, kapı çerçevesi boşluğundan bağımsız ayarlanırsa**:
  çok küçükse kapsül koridor duvarlarını sıyırırken titrer; çok büyükse
  kapsül kapı çerçevelerine ulaşamadan durur ya da etkileşim menziline
  girmeden bloklanır. Çözüm: skin width ≈ kapsül yarıçapının %10'u; kapı
  çerçevesi net genişliği ≥ (kapsül çapı + 2×skin width) olarak
  tasarlanır — birbirinden bağımsız ayarlanmaz, birlikte ayarlanır.
- **Eğer bir kit dikişindeki dışbükey 90° duvar birleşimi sıfır pah
  içeriyorsa**: kapsül iki yüzey boyunca kaymak yerine takılır. Çözüm:
  her dışbükey çarpışma köşesi skin width kadar pahlanır/yuvarlatılır
  (render mesh keskin kenarlı kalacaksa ayrı bir çarpışma proxy'si
  kullanılır), ya da duvar collider'larına sürtünmesiz fizik materyali
  uygulanır.
- **Eğer OnControllerColliderHit statik geometriye karşı her karede
  tetiklenirse**: filtrelenmemiş her-hit mantığı koridorlarda performansı
  spam eder. Çözüm: handler, hit katmanı açık bir ilgi maskesinde
  (hareketli platformlar, dinamik tehlikeler) olmadıkça erken çıkış
  yapmalı — varsayılan statik-geometri katmanına karşı asla mantık
  çalıştırılmaz.
- **Eğer oyuncu Asansör'e binerse**: **Güncelleme (2026-08-02,
  asansor-kat-erisim-sistemi.md tasarlanınca çözüldü)** — bu edge case
  başlangıçta gerçekten yükselen/alçalan bir platform varsayıyordu.
  Asansör'ün GDD'si, kabinin fiziksel olarak hiç hareket etmediğini
  netleştirdi (kapılar diegetik maskeleme sağlıyor, gerçek kat değişimi
  Seviye/Sahne Geçişi'nin anlık Transform kopyalamasıyla oluyor) — bu
  yüzden platform-delta enjeksiyonuna hiç gerek yok. Asansör sadece
  `RequestMovementLock`/`ReleaseMovementLock` çağırır (sadece `Move`
  girdisini dondurur, `Look` serbest kalır); enjekte edilecek gerçek bir
  fiziksel hareket olmadığı için bu yeterlidir. **Bu edge case'in orijinal
  "platform hızı miras alınmaz, delta enjeksiyonu gerekir" çözümü,
  gelecekte gerçekten hareket eden bir platform (örn. farklı bir asansör
  tasarımı) eklenirse hâlâ geçerlidir — ama mevcut Asansör tasarımı için
  uygulanmaz.**

## Dependencies

**Bağımlıdır**: **Etkileşim Sistemi** (kısmi — design-review, 2026-08-04,
verification bulgusuyla düzeltildi: bu satır önceden "Yok" diyordu, ama
Formül 2'nin (`approach_slow_taper`) `d` değişkeni Etkileşim'in
sahiplendiği `InteractableRegistry`'yi okur — bu, Foundation katmanının
Core katmanına bir okuma-bağımlılığıdır, `systems-index.md`'nin kendi
"katman içi bağımlılık serbest, üst katmana bağımlılık yok" kuralının
ihlali. Bu satır önce sadece dürüstçe düzeltildi — registry'nin
Foundation'a taşınıp taşınmaması ayrı, henüz çözülmemiş bir mimari karar,
bkz. `etkilesim-sistemi.md` Open Questions #1 ve
`gdd-cross-review-2026-08-04.md`).

**Kendisine bağımlı olanlar** *(design-review, 2026-08-03 — `/review-all-gdds`
bulgusu, dört sayımda düzeltildi: Asansör/Görev/Etkileşim artık
tasarlanmış, platform-delta iddiası bu sistemin kendi Edge Case'inde
zaten retracted, Işık/Volume eksikti)*:
- **Etkileşim Sistemi** *(tasarlandı)* — `EyeCamera` referansını raycast
  için kullanır; ayrıca bu sistemin GDD'sinde tanımlanan
  `InteractableRegistry`'yi (her `IInteractable`'ın kendini kaydettiği
  statik liste) bu sistemin yaklaşma-yavaşlaması taraması (Core Rules,
  Formül 2'nin `d` değişkeni) okur — flaglı nesne kümesi iki sistem
  arasında paylaşılan tek bir veri kaynağıdır (bkz.
  `design/gdd/etkilesim-sistemi.md` Core Rules); `RequestMovementLock`'ı
  `MovementLockScope.MoveOnly` ile çağırır (Hold sırasında)
- **Asansör/Kat-Erişim Sistemi** *(tasarlandı)* —
  `RequestMovementLock(this, MovementLockScope.MoveOnly)`/
  `ReleaseMovementLock(this)` çağırır. **Platform-delta enjeksiyonu
  gerekmez** (bu iddia bu sistemin kendi Edge Case'inde zaten retract
  edilmişti — kabin hiç hareket etmediği için).
- **Görev/Taşıma Döngüsü** *(tasarlandı)* — `SetCarrying(bool)` çağırır
- **Adaptif Ses Sistemi** *(tasarlandı)* — `PlayFootstep(float speed)`
  çağrılarını stride-phase accumulator'dan alır (bkz.
  `design/gdd/adaptif-ses-sistemi.md`)
- **Işık/Volume Durum Sistemi** *(tasarlandı)* — `PlayerMaxSpeed`
  değerini (1.6 m/s) Core Rules'taki "Tick tanımı" kuralının minimum
  güvenli `R_trigger` hesabı için okur (kısmi bağımlılık, salt-okunur
  sorgu — bkz. `isik-volume-durum-sistemi.md` Dependencies)
- **Sahne Kesmeli Anlatı** *(design-review, 2026-08-03 — eklendi,
  önceki taslakta bu listede hiç yoktu)* — `RequestMovementLock(this,
  MovementLockScope.Full)`/`ReleaseMovementLock(this)` çağırır (HARD
  CUT tetiklenmeden hemen önce — bkz.
  `design/quick-specs/sahne-kesmeli-anlati-2026-08-02.md` Core Rules)
- **Arkadaş Karakteri/NPC** (Vertical Slice, henüz tasarlanmadı) —
  muhtemelen `EyeCamera`/pozisyon verisini takip/diyalog tetikleme için
  kullanacak, kesin arayüz o GDD yazılınca netleşecek

**Not**: Sadece Arkadaş Karakteri/NPC henüz tasarlanmadı. Yazıldığında
kendi Dependencies bölümünde "Birinci Şahıs Kontrolcü"yü listelemeli
(çift yönlü tutarlılık — bkz. `design/gdd/systems-index.md`).

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| Yürüme hızı (yüksüz) | 1.2–2.0 m/s | Yorucu/bezdirici hissettirir | "Meditatif tempo" hedefini bozar, Pillar 2'yi zayıflatır | Formül 1, 2 |
| Taşıma çarpanı (CarryMult) | 0.7–0.95 | Taşımak "bozuk" hissettirir | Yüksüz halden fark edilmez olur | Formül 2 |
| İvme süresi (T_ramp) | 0.15–0.25 s | Ani/uçarı his, "ağırlıklı beden" kaybolur | Ağır/kontrole direniyormuş gibi hissettirir | Formül 1 |
| Yaklaşma-yavaşlaması yarıçapı | 1.0–2.5 m | Fark edilmez | Normal gezinme boyunca sürekli yavaşlamış hissettirir, sinyal özelliğini kaybeder | Formül 2 |
| Yaklaşma-yavaşlaması maks. oranı | %15–%45 | Fark edilmez | Neredeyse durma noktasına gelir, sinir bozucu | Formül 2 |
| Head-bob maks. genlik (A_max) | 1–3 cm | Yük/yorgunluk hissi verilmez | Hareket hastalığı riski | Formül 3 |
| FOV varsayılan/aralık | 75–80° varsayılan, 60–100° slider | Dar/klostrofobik (kasıtlı değilse) | Aksiyon-oyunu hissi verir, tona uymaz | Erişilebilirlik |
| Step offset | En küçük eşiğin altı (~2cm) | — | Kamera her eşikte zıplar | Edge Case: eşik |
| Skin width | Kapsül yarıçapının ~%10'u | Duvarlarda titreme | Kapı çerçevelerinde bloklanma | Kapı çerçevesi genişliği |

## Visual/Audio Requirements

**Ayak sesleri**: MVP'nin 3 alanı için tek jenerik materyal (depo/koridor/
balo salonu hepsi sert iç mekan zemini — yüzey-etiketleme sistemi bu
kontrolcünün sahip olmadığı bir şey). Rastgele pitch (±%5) ve 4-6 örnek
varyasyonu, tekrarı önlemek için. Ses seviyesi `v(t)`'ye (Formül 1) göre
ölçeklenir — tetikleyiciye yaklaşırken tempo düşünce ayak sesleri de
duyulur şekilde yumuşar. Yüzeye-bağlı ses MVP'de yok — modüler kit ileride
materyal etiketleme kazanırsa Vertical Slice seçeneği olarak işaretlendi.

**Nefes/efor**: Taşıma durumuna (`IsCarrying`=true) bağlı tek bir hafif
nefes döngüsü, taşıma başladığında/bittiğinde ~1s içinde içeri/dışarı
yumuşar. Yaklaşma-yavaşlamasına bağlı **değil** — taper spesifik bir
tetikleyiciyle ilgili, genel efor değil. Taper'ın kendisinden nefes
değişimi yok (bu, Işık/Volume sisteminin "bir şeyler yanlış" ambiyans
katmanıyla aynı sinyal için yarışır — Pillar 1 o sistemin alanı, bunun
değil).

**Kamera/lens**: Hiçbiri. DoF yok, vinyet yok, kromatik sapma yok, hiçbir
controller-sahipli post-process yok. Visual Identity Anchor'ın "geometri
sabit, distorsiyon ışıkta" kuralına göre, algısal distorsiyon tamamen
Işık/Volume sisteminin alanı — bu sistem o sinyali öne çıkarmamalı ya da
tekrarlamamalı. FOV (75-80° varsayılan) bu sistemin sahip olduğu tek
kamera özelliği, statik bir erişilebilirlik ayarı olarak.

**Head-bob/ayak sesi senkronu**: Evet — ikisi de aynı adım-fazı saatiyle
sürülür, bağımsız zamanlayıcılar değil. Tek bir faz biriktirici (mesafeye
göre ilerler, saat zamanına göre değil) hem head-bob eğrisini (Formül 3)
hem ayak sesi tetiklemelerini sürer — `v(t)` sürekli değişse bile
(Formül 1) asla birbirinden kaymazlar; bu senkron kaybı "bedenin işi
bildiği" fantazisini anında bozar.

## UI Requirements

Bu sistemin kendi UI'ı yok — saf hareket/kamera altyapısı. Crosshair/
etkileşim ipucu Etkileşim Sistemi'ne ait; **taşıma kapasitesi/slot
göstergesi Görev/Taşıma Döngüsü'ne ait** (bkz. Open Questions — bu GDD
sırasında ortaya çıkan araç/slot notu o sisteme devredildi).

## Acceptance Criteria

**Temel Hareket**

1. **GIVEN** oyuncu yerde, yüksüz ve boşta, **WHEN** maksimum girdiyle ileri
   basılı tutulur, **THEN** hız kararlı duruma ulaştığında 1.6 m/s'ye
   (±0.02) yakınsar.
2. **GIVEN** `IsCarrying` true, **WHEN** maksimum girdiyle ileri basılı
   tutulur, **THEN** kararlı-durum hızı 1.35 m/s'ye (±0.02) yakınsar.
3. **GIVEN** herhangi bir kontrol şeması durumu, **WHEN** Input Actions
   asset'i ve tüm tuş/buton bağlamaları incelenir, **THEN** hızı yürüme
   değerinin üzerine çıkaran hiçbir bağlama yoktur (koşu eylemi yok).
4. **GIVEN** oyuncu durgun (v=0), **WHEN** t=0'da ileri girdi uygulanır,
   **THEN** hız 0.20-0.25s içinde ≥1.52 m/s'ye (1.6'nın %95'i) ulaşır,
   hiçbir örneklenen karede 1.6 m/s'yi aşmaz.
5. **GIVEN** oyuncu 1.6 m/s'de hareket ediyor, **WHEN** ileri girdi
   bırakılır, **THEN** hız 0.20-0.25s içinde ≤0.08 m/s'ye (%95 sönümlenmiş)
   düşer, ivmelenmeyle aynı üstel eğriyi izler.

**Yaklaşma-Yavaşlaması Taper'ı**

6. **GIVEN** oyuncu yüksüz ve herhangi bir bayraklı nesneden ≥1.5m uzakta,
   **WHEN** ölçülür, **THEN** hedef hız tam olarak 1.6 m/s'dir (taper'ın
   etkisi sıfır).
7. **GIVEN** oyuncu yüksüz ve bayraklı bir nesneden d=0m'de duruyor,
   **WHEN** ölçülür, **THEN** hedef hız 1.12 m/s'dir (1.6 × 0.7, maksimum
   %30 azalma tabanı).
8. **GIVEN** oyuncu taşıyor (`IsCarrying`=true) ve bayraklı bir nesneden
   d=0.5m'de, **WHEN** ölçülür, **THEN** hedef hız ≈1.05 m/s'dir — taper
   ve taşıma çarpanlarının toplamsal değil **çarpımsal** birleştiğini
   doğrular (1.6 × 0.84375 × 0.778).

**Head-Bob**

9. **GIVEN** head-bob erişilebilirlik slider'ı %0'a ayarlı, **WHEN** oyuncu
   1.6 m/s dahil herhangi bir hızda yürür, **THEN** kamera bob genliği her
   karede tam olarak 0'dır.
10. **GIVEN** slider %100'e ayarlı ve oyuncu yüksüz kararlı-durum tam
    hızda, **WHEN** ölçülür, **THEN** bob genliği A_max'a eşittir
    (yapılandırılmış Tuning Knob tavanı, örn. 2.5cm).

**Hareket Kilidi (Referans Sayma)**

11. **GIVEN** istekçi A `RequestMovementLock(A)`'yı bir kez çağırdı,
    **WHEN** A araya bir release girmeden ikinci kez çağırır, **THEN**
    kilit kümesi A için tam olarak bir giriş içerir, ve tek bir sonraki
    `ReleaseMovementLock(A)` hareketi tamamen açar.
12. **GIVEN** istekçi A kilidi tutuyor ve istekçi B hiç istemedi, **WHEN**
    B `ReleaseMovementLock(B)` çağırır, **THEN** istisna fırlatılmaz,
    hareket kilitli kalır, A'nın kilidi etkilenmez.
13. **GIVEN** oyuncu v>0 ile yürüyor, **WHEN** dış bir sistem
    `RequestMovementLock` çağırır, **THEN** hız normal 0.15-0.25s
    ramp'ında 0'a söner (ani durma değil), `IsCarrying` ve taşıma
    görselleri boyunca değişmeden kalır.

**Kamera / Bakış**

14. **GIVEN** oyuncu kamerayı mouse-look ile tam yukarı/aşağı çeviriyor,
    **WHEN** girdi limitin ötesine devam eder, **THEN** pitch tam olarak
    -80°/+80°'de kelepçelenir, daha fazla dönüş olmaz, yaw sınırsız devam
    eder.

**Edge Case'ler**

15. **GIVEN** bir frame hitch anormal büyük bir Δt üretir (örn. simüle
    edilmiş 500ms), **WHEN** bir sonraki kare işlenir, **THEN** hız o
    karede doğrudan `v_target`'a atlar, aşma ya da NaN/kararsızlık olmaz
    — motor içi hitch zorlama yolu henüz yoksa **ERTELENDİ**; aksi halde
    mock Δt ile yumuşatma fonksiyonu üzerinde otomatik unit test ile
    doğrulanabilir.
16. **GÜNCELLENDİ (2026-08-02, daraltıldı 2026-08-03)** — orijinal kriter
    gerçekten yükselen/alçalan bir platform varsayıyordu; Asansör'ün GDD'si
    kabinin hiç hareket etmediğini netleştirdi. Yeni kriter: **GIVEN**
    oyuncu kabine biner ve kapılar kapanır, **WHEN** `RequestMovementLock
    (this, MovementLockScope.MoveOnly)` çağrılır, **THEN** `Move` girdisi
    donar ama `Look` girdisi serbest kalır, ve **bu sistemin kendisi**
    `ReleaseMovementLock`'a kadar oyuncunun pozisyonuna hiçbir platform-delta
    uygulamaz (platform-delta enjeksiyonu yok, çünkü enjekte edilecek
    gerçek bir hareket yok). *(design-review, 2026-08-03 — `/review-all-gdds`
    bulgusu, düzeltildi: önceki hali "dünya pozisyonu değişmeden sabit
    kalır" diye mutlak bir garanti veriyordu — ama Seviye/Sahne Geçişi'nin
    SOFT handoff'u, aynı kilitli pencere içinde, oyuncuyu hedef sahnenin
    kabin-yerel `SoftTransitionAnchor`'ına Transform-kopyalar, ki bu genel
    olarak farklı bir dünya-uzayı pozisyonudur. Bu sistem gerçekten
    garanti ettiği tek şey, KENDİSİNİN pozisyona hiçbir platform-hareketi
    eklemediğidir — sahne geçişinin kendisinin pozisyonu değiştirmemesi
    ayrı bir garantidir, iki kabinin dünya koordinatlarının eşleşmesine
    bağlıdır (bkz. `asansor-kat-erisim-sistemi.md` Open Questions #1,
    hâlâ açık) ve bu sistemin kapsamı dışındadır.)* Bkz.
    `design/gdd/asansor-kat-erisim-sistemi.md` Acceptance Criteria (tam
    entegrasyon testi orada yaşar).
17. **[design-review, 2026-08-04 — verification design-theory bulgusu,
    kullanıcı kararıyla eklendi]** **GIVEN** MVP'nin üç alanı (Depo,
    Servis Koridoru, Balo Salonu), **WHEN** bir edit-time/build-time
    içerik kontrolü her sahneyi tarar, **THEN** her alanda anı-tetikleyici
    ve taşıma-eşyası **dışında** en az bir sahte/dekor `IInteractable`
    nesnesi kayıtlı bulunur (bkz. Core Rules, "Kamuflajın gerçek içerikle
    çökmesi ve düzeltmesi") — kontrol eksikse hata verir, build engellenir
    (paylaşılan `IPreprocessBuildWithReport` editor utility'sinin bir
    parçası olarak, bkz. `ani-tetikleyici-etkilesim.md`'nin aynı
    mekanizması). Bu, kamuflajın içerik-yazımı sırasında sessizce
    unutulmasını yapısal olarak engeller.

## Open Questions

- **A_max (head-bob maksimum genlik) kesin değeri belirlenmedi** — Tuning
  Knob olarak 1-3cm aralığı önerildi, playtest ile netleştirilecek. Sahip:
  sonraki playtest oturumu.
- **Ayak sesi asset sayısı/kaynağı** — 4-6 örnek varyasyon önerildi ama
  gerçek ses kaynağı (kayıt mı, asset paketi mi) henüz seçilmedi. Sahip:
  Adaptif Ses Sistemi GDD'si.
- **→ Görev/Taşıma Döngüsü'ne not**: Bu GDD sırasında ortaya çıktı —
  taşıma arabası basit bir slot sistemine sahip olmalı (N slot, her eşya
  1 slot, ağırlık/boyut karmaşıklığı yok). Kullanıcının gerçek iş
  deneyiminden (referans fotoğraf) geliyor. O GDD yazılırken bu
  kapasite/slot UI'ı ve mantığı tasarlanacak.
