# Asansör/Kat-Erişim Sistemi (Elevator/Floor-Access System)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`)
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **Implements Pillar**: Pillar 3 (Görev Gerçekliği)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (revised) 2026-08-02 — kozmetik hareket Pillar 1'e açıkça bağlandı, kabin içi tam kapalı hacim gereksinimi Visual/Audio'ya eklendi
> **`/review-all-gdds` (2026-08-03)**: `onFailed` handling, `OnSoftTransitionRejected` timing, ve `MovementLockScope.MoveOnly` çağrıları düzeltildi — bkz. `design/gdd/gdd-cross-review-2026-08-03-verification.md`

## Overview

Asansör/Kat-Erişim Sistemi, oyuncunun depo katı ile balo salonu katı
arasında geçişini yönetir: bir çağrı düğmesine (kendi trigger-zone
mantığıyla, Etkileşim Sistemi'nin `IInteractable` arayüzünü kullanmadan)
yaklaşıp etkileşime girdiğinde, kabin gelir, kapılar açılır/kapanır, ve
Seviye/Sahne Geçişi'nin `RequestSoftTransition` mekanizması üzerinden
hedef kat sahnesi arka planda yüklenirken oyuncu karanlık, **hareket
etmeyen** bir kabinde bekler — gerçek bir yükselme/alçalma yoktur,
kapıların kendisi diegetik maskeleme sağlar (bkz. Seviye/Sahne Geçişi'nin
"anlık Transform kopyalama" kararı). Sistem, Birinci Şahıs Kontrolcü'nün
hareket kilidini tutar (**sadece hareket girdisini dondurmak için** —
bakış serbest kalır) ama gerçek bir platform-delta enjeksiyonu
gerektirmez, çünkü enjekte edilecek gerçek bir fiziksel hareket yoktur;
"hareket ediyormuşsun" hissi tamamen kozmetik kamera-uzayı sarsıntı/uğultu
ile satılır. **Bu sadece bir mühendislik kolaylığı değil, Pillar 1'e
(Öznel Gerçeklik) hizmet eden kasıtlı bir seçimdir (CD-GDD-ALIGN notu)**:
oyuncunun duyuları, fiziksel gerçeklikle (durağanlık) örtüşmeyen bir
sinyal (hareket) alıyor — bu, anı-tetikleyici sistemlerinden önce
gelen, küçük ve kontrollü bir "duyuların yalan söylüyor" provası.
Kullanılabilirlik, Gece/Oturum Durumu'nun `IsSessionActive`
bayrağı üzerine kendi mantığıyla kurulur; bu karar başka bir sistemde
verilmez.

Oyuncu için asansör, günün gerçek bir işinin gerçek bir aracıdır — ne
bir "loading ekranı" ne de sihirli bir ışınlanma, kapı kapanır, birkaç
saniye beklersin, kapı açılır ve başka bir kattasındır (Pillar 3: Görev
Gerçekliği). Bu sistem olmadan oyuncu tek bir kata hapsolur — Görev/
Taşıma Döngüsü'nün malzeme taşıma rotası (depo → balo salonu) bu
sistemin varlığına bağlıdır.

## Player Fantasy

Bu, işin içindeki tek zorunlu durgunluktur. Gecenin her anında yetkinlik
oyuncu için bir şey yapıyordur — taşımak, istiflemek, yön bulmak. Burada,
ellerin tutunacak hiçbir şeyi yok — sadece bekliyorsun, iş seni hâlâ
tutuyor ama senden hiçbir şey istemiyor (**Ölü Zaman**, Pillar 3: Görev
Gerçekliği). Kutu küçük, ışık az, gözlerin neredeyse hiçliğe alışıyor;
bu korku ani değil, düşük ve sürekli bir uğultu gibi idare ediyor
(Pillar 2: Sessiz Gerilim, Şok Değil) — "korkunç asansör" değil, o gece
onlarca kez girdiğin sıradan bir kutunun aşinalığı.

Bu, Etkileşim Sistemi'nin "Eller Zaten Biliyor" fantazisinin tam
tersidir — orada eller düşünceden önce hareket ediyordu, burada ellerin
hiçbir işi yok, bu yüzden düşünce içe dönmekten başka bir yere
gidemiyor. Ve Seviye/Sahne Geçişi'nin "Beden Sürekliliği" dediği şeyin
yaşanan sebebi de bu: iş seni bu boşlukta bile bırakmadığı için, boşluk
bir mola gibi işlev göremiyor.

## Detailed Design

### Core Rules

- **Çağrı düğmesi (trigger-zone)**: Kat başına bir trigger-zone + görsel
  ışık. Oyuncu ~1.5m yarıçapındaki zona girince düğme "erişilebilir"
  sayılır ve mevcut "Gameplay" action map'in `Interact` girdisini
  **doğrudan** okur (Etkileşim Sistemi'ne dokunmadan — `IInteractable`
  yok, kararlı). Basış anında `IsSessionActive` okunur: `false` ise ışık
  yanmaz, girdi tamamen yok sayılır (diegetik tepkisizlik — Gece/Oturum
  Durumu karar vermez, bu sistem üstüne kurar).
- **Bekleme/varış**: Geçerli basışta `Called`; `ArrivalDuration` (Tuning
  Knob, 3-6s) sonra kabin "gelir" (kapılar açılır — kabinin kendisi
  zaten fiziksel olarak oradadır, hiçbir yerden gelmez).
- **Kapı zamanlaması**: `DoorOpenAnim` (~1.5s) → `DwellTime` (oyuncu
  binmesi için bekleme penceresi, ~4-6s) → `DoorCloseAnim` (~1.5s).
- **Kabin hareket etmez**: Kapılar kapandıktan sonra kabin **fiziksel
  olarak sabit kalır** — gerçek bir yükselme/alçalma yoktur. "Hareket
  ediyormuşsun" hissi tamamen kozmetik: kamera-uzayı prosedürel sarsıntı
  + sürekli düşük uğultu (bkz. Visual/Audio Requirements).
- **Hareket kilidi, sadece girdi donduruyor**: `RequestMovementLock(this,
  MovementLockScope.MoveOnly)` (design-review, 2026-08-03 —
  `/review-all-gdds` verification bulgusu, düzeltildi: önceki taslak
  parametresiz `RequestMovementLock(this)` çağırıyordu — FPC'nin yeni
  varsayılanı `Full`'dür, bu da `Look`'u da dondururdu ve bu sistemin
  kendi AC#13'üyle doğrudan çelişirdi), sadece `Move` girdisini dondurur
  — `Look` serbest kalır (oyuncu karanlık kutunun içinde etrafına
  bakabilir, "Ölü Zaman"ı pekiştirir). Gerçek bir platform-delta
  enjeksiyonuna gerek yoktur, çünkü enjekte edilecek gerçek bir fiziksel
  hareket yoktur.
- **Sıralama**: `DoorsClosing` tamamlanınca → `RequestMovementLock(this,
  MovementLockScope.MoveOnly)` → `RequestSoftTransition(fromScene, toScene, config, onComplete, onFailed)`
  çağrılır → `onComplete` içinde `ReleaseMovementLock(this)`, hedef kat
  kapıları açılır; `onFailed` içinde de `ReleaseMovementLock(this)`
  çağrılır, kabin köken katta kalır (bkz. Edge Cases).
  **`OnSoftTransitionRejected`'ın zamanlaması netleştirildi (design-review,
  2026-08-03 — `/review-all-gdds` bulgusu, düzeltildi)**: Bu event,
  Seviye/Sahne Geçişi'nin kendi 2026-08-03 revizyonu sonrası **sadece
  istek anında, senkron olarak** fırlar (bkz.
  `seviye-sahne-gecisi.md` Edge Cases, "Kapsam genişletmesi") — `Waiting`
  durumuna girildikten *sonra* ortada bir yerde gelmez, çünkü aktif bir
  SOFT geçiş asla kesintiye uğratılmaz. Bu yüzden `RequestSoftTransition`
  çağrısı **kendisi** reddedilirse, `Waiting`'e hiç girilmez — `DoorsClosing`
  tamamlandığı anda senkron olarak reddedilir ve kabin `DoorsOpening`'e
  geri döner (hareket kilidi hiç alınmamış olur, çünkü `RequestMovementLock`
  reddedilen çağrıdan önce alınmışsa hemen serbest bırakılır). `Waiting`'e
  girildiyse (istek kabul edildiyse), o geçiş artık kesintiye uğramaz —
  sadece `onComplete` ya da `onFailed` ile sonlanır.

### States and Transitions

| Durum | Giriş Koşulu | Çıkış / Sonraki |
|---|---|---|
| **Idle** | Kabin bu katta, kapı kapalı | Düğme basışı (`IsSessionActive==true`) → **Called** |
| **Called** | Basış kabul edildi, `ArrivalDuration` sayıyor | Süre dolunca → **DoorsOpening**; ikinci basış no-op |
| **DoorsOpening** | — | Anim bitince → **DoorsOpen** |
| **DoorsOpen** | Kapı açık, `DwellTime` sayıyor | Oyuncu kabine girer **veya** dwell dolar → **DoorsClosing** |
| **DoorsClosing** | Kapı kapanıyor | Anim bitince → **Waiting** (oyuncu kabindeyse) veya **Idle** (kimse binmediyse) |
| **Waiting** | `RequestMovementLock` aktif, kozmetik sarsıntı/uğultu çalıyor, `RequestSoftTransition` beklemede | `onComplete` → **DoorsOpen** (hedef kat) \| `onFailed` → **DoorsOpening** (köken katta, hareket kilidi serbest — design-review 2026-08-03 eklendi, bkz. Edge Cases) |

### Interactions with Other Systems

**Birinci Şahıs Kontrolcü'ye çağrılar**: `RequestMovementLock(this,
MovementLockScope.MoveOnly)` (DoorsClosing→Waiting girişinde, sadece
Move donuyor) → `ReleaseMovementLock(this)` (`onComplete` ya da
`onFailed` içinde). Gerçek platform-delta enjeksiyonu **yok** — kabin
hareket etmediği için gerek yok.

**Seviye/Sahne Geçişi'ne çağrılar**:
`RequestSoftTransition(fromScene, toScene, config, onComplete, onFailed)`
(kapı kapanışı bitince); `OnSoftTransitionRejected(reason)`'a abone
olunur → tetiklenirse (istek anında, senkron) düğme ışığı söner, kapı
köken katta yeniden açılır. `onFailed` de aynı tepkiyi üretir (bkz. Edge
Cases, "Failed çağrıya Asansör'in tepkisi") — sadece zamanlaması farklı
(istek-anı reddi vs. `Waiting` sırasında yükleme hatası).

**Gece/Oturum Durumu'na çağrılar**: Sadece `IsSessionActive` okunur (her
düğme basışında) — yazma yok.

**MVP kapsam notu**: Tek asansör, tek kabin, kat başına tek düğme —
eşzamanlı çağrı çakışması yok. Birden fazla asansör/kabin-atama mantığı bu
GDD'nin kapsamında değil.

## Formulas

**N/A** — durum makinesi/zamanlayıcı mantığı, sayısal hesaplama yok.
`ArrivalDuration`, `DoorOpenAnim`, `DwellTime`, `DoorCloseAnim` ve
kozmetik kamera sarsıntısı/uğultu şiddeti hepsi sabit değerlerdir
(Tuning Knobs), başka değişkenlerden türetilen bir formül değil.
Sarsıntı şiddeti kat mesafesine ya da başka bir değişkene göre
ölçeklenseydi bir formül gerekirdi — ölçeklenmiyor, düz bir sabit.

## Edge Cases

- **Eğer oyuncu düğmeye bastıktan sonra tetik bölgesinden uzaklaşırsa**
  (DoorsOpening tetiklenmeden önce): Called durumu oyuncunun fiziksel
  konumuna bağlı değildir; state machine, tetik bölgesindeki varlığı
  yalnızca *giriş* anında kontrol eder. Döngü kesintisiz devam eder:
  DoorsOpening → DoorsOpen → dwell.
- **Eğer düğmeye Called veya DoorsOpen sırasında ikinci kez basılırsa**:
  Girdi yok sayılır (no-op). Yeniden tetikleme yok, dwell sayacı
  sıfırlanmaz, kuyruğa alma yok. Yalnızca Idle durumunda basış geçerli
  sayılır.
- **Eğer başka bir katın düğmesine, tek kabin zaten Called/DoorsOpening/
  DoorsOpen/Waiting durumundayken basılırsa**: Aynı no-op kuralı
  geçerlidir — MVP'de kuyruk/çoklu talep mantığı yok, tek kabin
  meşgulken diğer tüm düğmeler etkisiz kalır (ışık yanmaz).
- **`OnSoftTransitionRejected` istek anında (design-review, 2026-08-03 —
  `/review-all-gdds` bulgusu, düzeltildi)**: Önceki taslak bu reddin
  `Waiting`'e girildikten *sonra*, geçişin ortasında herhangi bir noktada
  gelebileceğini varsayıyordu — bu artık doğru değil, Seviye/Sahne
  Geçişi'nin 2026-08-03 revizyonu aktif bir SOFT geçişin asla kesintiye
  uğramayacağını netleştirdi. Gerçek senaryo: `DoorsClosing` tamamlanıp
  `RequestSoftTransition` çağrıldığı anda (aktif bir HARD CUT nedeniyle,
  ör.) senkron olarak reddedilir — `Waiting`'e hiç girilmez, kabin
  doğrudan `DoorsOpening`'e döner (hareket kilidi hiç tutulmamış olur).
  Kabin fiziksel olarak hiç hareket etmediği için bu "dönüş" yalnızca
  state/UI seviyesinde bir geri alma işlemidir.
- **Eğer `Waiting`'e girildikten SONRA `RequestHardCut` çağrılırsa
  (SOFT aktifken)**: Bu, SOFT'u kesintiye uğratmaz — Seviye/Sahne
  Geçişi'nin kendi asimetrik kuralı gereği HARD CUT bekleyen slota
  kuyruğa alınır (bkz. `seviye-sahne-gecisi.md` Edge Cases), SOFT
  kesintisiz `onComplete`'e ulaşır. Asansör bunu fark etmez, kendi
  akışını değiştirmez.
- **`Failed` çağrıya Asansör'in tepkisi (design-review, 2026-08-03 —
  `/review-all-gdds` bulgusu, eklendi, kritik bulgu)**: `Waiting`
  sırasında `onFailed` çağrılırsa (hedef sahne yüklenemedi — bkz.
  `seviye-sahne-gecisi.md` Edge Cases), `OnSoftTransitionRejected` ile
  **aynı** tepki uygulanır: kozmetik shake/hum durur, hareket kilidi
  serbest bırakılır, kabin köken katta `DoorsOpening`'e döner. Önceki
  taslakta bu durumun hiçbir işleyicisi yoktu — kabin `Waiting`'de
  sonsuza kadar kilitli kalabilirdi (gerçek bir soft-lock, kozmetik
  bir eksiklik değil).
- **Eğer DoorsOpen dwell süresi oyuncu kabine girmeden dolarsa**: Sistem
  otomatik DoorsClosing'e geçer ve Idle'a döner; bir "yolculuk"
  başlamamış sayılır, hata/uyarı state'i yoktur.
- **Eğer `IsSessionActive`, Called/DoorsOpening/DoorsOpen/Waiting
  sırasında false'a dönerse**: Hiçbir etkisi yoktur — bayrak yalnızca
  düğmeye *basış anında* okunur, döngü boyunca poll edilmez. Devam eden
  döngü normal şekilde tamamlanır; yeni değer yalnızca bir sonraki buton
  basışında devreye girer.
- **Eğer oyuncu Waiting sırasında (movement lock aktifken) kabin
  sınırlarının dışına çıkmaya çalışırsa**: Move donmuş olduğundan
  fiziksel çıkış mümkün değildir; Look serbest kaldığından oyuncu
  çevreyi izleyebilir ama konum değişmez — kabinin "gerçek platform
  fiziği olmayan statik kutu" kuralının doğrudan bir sonucudur.
- **Eğer oyuncu DoorsOpen dwell penceresinde kabine girip sonra tekrar
  dışarı çıkarsa** (kapılar henüz kapanmadan): Giriş/çıkışın kendisi bir
  state tetiklemez — sistem yalnızca DoorsClosing anında kabin içindeki
  fiili varlığı kontrol eder; oyuncu son anda dışarıdaysa yolculuk iptal
  olur (Idle'a döner), oyuncu içerideyse Waiting başlar.

## Dependencies

**Bağımlıdır** (hard):
- **Birinci Şahıs Kontrolcü** — `RequestMovementLock(this)`/
  `ReleaseMovementLock(this)` çağırır (sadece Move donuyor, Look serbest)
- **Seviye/Sahne Geçişi** — `RequestSoftTransition(...)` çağırır,
  `OnSoftTransitionRejected` event'ine abone olur
- **Gece/Oturum Durumu** — `IsSessionActive` okur (salt okunur sorgu)

**Bağımlı DEĞİLDİR**:
- **Etkileşim Sistemi** — çağrı düğmesi kendi trigger-zone mantığına
  sahip, `IInteractable` kullanmaz (bkz. Core Rules kararı)

**Kendisine bağımlı olanlar**:
- **Görev/Taşıma Döngüsü** *(tasarlandı — design-review, 2026-08-04
  verification bulgusuyla "henüz tasarlanmadı" etiketi düzeltildi; bu
  sistem zaten kendi Dependencies bölümünde Asansör'ü listeliyor, çift
  yönlü tutarlılık sağlanmış durumda)* — malzeme taşıma rotası (depo ↔
  balo salonu) bu sistemin varlığına bağlı

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| ArrivalDuration (çağrı→kapı açılış) | 3–6 s | Anlık gelir, "gerçek bir asansör" hissi kaybolur | Sıkıcı bekleme, "Ölü Zaman"ı bezdiriciliğe çevirir | Core Rules: bekleme/varış |
| DoorOpenAnim / DoorCloseAnim süresi | 1–2 s | Ani/mekanik kapı hareketi | Gereksiz uzun bekleme | Core Rules: kapı zamanlaması |
| DwellTime (kapı açık kalma) | 4–6 s | Oyuncu binmeye yetişemez | Gereksiz bekleme | Core Rules: kapı zamanlaması |
| Kozmetik sarsıntı genliği | Düşük, göz ardı edilebilir seviye | Hareket hissi hiç yok | Rahatsız edici/mide bulandırıcı (erişilebilirlik riski) | Visual/Audio Requirements |
| Uğultu ses seviyesi | Düşük, sürekli | Sessizlik "Ölü Zaman"ı desteklemez | Dikkat dağıtıcı, Pillar 2'yi (Sessiz Gerilim) ihlal eder | Visual/Audio Requirements |

## Visual/Audio Requirements

**Kozmetik hareket hissi**: Kamera-uzayı prosedürel sarsıntı (düşük
genlik, sürekli — Tuning Knobs'da işaretli) + sürekli düşük uğultu.
İkisi de `Waiting` durumu boyunca çalar, `onComplete`/rejection anında
anında durur (fade değil, ani kesme — kabin fiziksel olarak asla
hareket etmediği için, sesin/sarsıntının kendisi de aynı ani netlikte
başlamalı/bitmeli).

**Düğme ışığı**: `IsSessionActive==false` ya da kabin meşgulken sönük;
müsaitken yanık. Basışta anlık bir "kabul edildi" flaşı (görsel geri
bildirim, crosshair/prompt olmadan — bu Etkileşim Sistemi'nin
sahiplendiği bir mekanizma değil).

**Kapı sesleri**: Açılma/kapanma animasyonlarıyla senkron mekanik kapı
sesleri — otelin "gerçek bir iş yeri" hissini pekiştirir (Pillar 3).

**Erişilebilirlik notu**: Sarsıntı genliği düşük tutulmalı (Tuning
Knobs) — hareket hastalığı riski, Birinci Şahıs Kontrolcü'nün head-bob
erişilebilirlik ilkesiyle aynı gerekçe.

**Kabin içi tam kapalı hacim gereksinimi (CD-GDD-ALIGN notu)**: Look
girdisi `Waiting` boyunca serbest kaldığı için (bkz. Core Rules, Edge
Cases), kabin içi geometrisi **her bakış yönünde** tamamen kapalı/
camera-safe olmalı — hiçbir açıdan geometri arkası, sahne yükleme
sınırı ya da hedef katın erken render'ı görünmemeli. Bu, Pillar 1'in
"kontrollü öznel algı" vaadinin en açık anda (oyuncu serbestçe
etrafına bakarken) delinmemesi için gereklidir — bir render sızıntısı
burada "gizemli" değil "bozuk" okunur.

📌 **Asset Spec** — Visual/Audio requirements tanımlandı. Art bible
onaylandıktan sonra `/asset-spec system:asansor-kat-erisim-sistemi`
çalıştırılarak asset başına görsel açıklamalar/prompt'lar üretilebilir.

## UI Requirements

Bu sistemin ayrı bir non-diegetik UI'ı yok — düğme ışığı (bkz. Visual/Audio
Requirements) tamamen diegetik bir sinyaldir, HUD/crosshair katmanına ait
değildir. Etkileşim Sistemi'nin crosshair/prompt'u bu sistemde hiç
görünmez (çünkü çağrı düğmesi `IInteractable` kullanmıyor) — bu kasıtlı
bir tutarlılıktır: oyuncu asansör düğmesine "oyun UI'ı" değil, otelin
kendi donanımı olarak bakar.

## Acceptance Criteria

**Çekirdek Kurallar**

1. **GIVEN** `IsSessionActive` çağrı anında true, **WHEN** oyuncu kat
   düğmesine basar, **THEN** düğme ışığı yanar, `Called` durumuna
   geçilir.
2. **GIVEN** `IsSessionActive` çağrı anında false, **WHEN** oyuncu
   düğmeye basar, **THEN** düğme ışığı yanmaz, girdi tamamen yok sayılır,
   state `Idle`'da kalır.
3. **GIVEN** asansör herhangi bir durumda (`Called`→`Waiting`), **WHEN**
   state machine ilerler, **THEN** kabin transformuna hiçbir
   platform-physics/`Move()`-delta uygulanmaz.
4. **GIVEN** asansör `Called`/`DoorsOpening`/`DoorsOpen`/`Waiting`
   içinde meşgul, **WHEN** aynı düğmeye tekrar basılır, **THEN** hiçbir
   yeni state/efekt tetiklenmez.

**Mutlu-Yol Döngüsü**

5. **GIVEN** `Idle` + `IsSessionActive` true, **WHEN** düğmeye basılır,
   **THEN** `Called`→`DoorsOpening` geçişi olur, ışık yanar.
6. **GIVEN** `DoorsOpening` animasyonu biter, **WHEN** open-event
   tetiklenir, **THEN** `DoorsOpen`'a geçilir, dwell timer (4-6s) başlar.
7. **GIVEN** `DoorsOpen` + oyuncu kabinde, **WHEN** dwell süresi dolar,
   **THEN** `DoorsClosing`'e geçilir.
8. **GIVEN** `DoorsClosing` tamamlanır, **WHEN** `RequestSoftTransition`
   çağrılır VE senkron olarak kabul edilir (reddedilmez), **THEN**
   `Waiting`'e geçilir (Move kilitli, kozmetik sarsıntı/uğultu başlar).
8a. **[design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi]**
   **GIVEN** `DoorsClosing` tamamlanır, **WHEN** `RequestSoftTransition`
   çağrısı senkron olarak reddedilir (`OnSoftTransitionRejected` istek
   anında fırlar — bkz. Edge Cases), **THEN** `Waiting`'e hiç girilmez,
   kabin doğrudan köken katta `DoorsOpening`'e döner, hareket kilidi hiç
   tutulmamış olur.
9. **GIVEN** `Waiting` + `onComplete` tetiklenir, **WHEN** callback
   alınır, **THEN** hedef katta `DoorsOpen`'a geçilir, Move kilidi kalkar.

**En Kritik Edge Case'ler**

10. **[design-review, 2026-08-03 — `/review-all-gdds` bulgusu, düzeltildi:
    önceki hali `Waiting` sırasında mid-flight bir ret senaryosunu test
    ediyordu, artık Seviye/Sahne Geçişi'nin kendi kontratıyla erişilemez
    — bkz. AC8a'nın yerini aldığı senaryo]** **GIVEN** `Waiting` +
    `RequestSoftTransition` `onFailed` ile sonuçlanır (hedef sahne
    yüklenemedi), **WHEN** `onFailed` çağrılır, **THEN** pozisyon
    değişmeden (kabin hiç hareket etmediği için) origin kattaki
    `DoorsOpening`'e anında dönülür, Move kilidi kalkar.
11. **GIVEN** çağrı `IsSessionActive` true iken yapıldı, `Called`/
    `DoorsOpening` içinde, **WHEN** flag daha sonra false'a döner,
    **THEN** döngü hiç etkilenmeden devam eder (yalnızca basış anında
    okunur).
12. **GIVEN** kabin başka kat için meşgul, **WHEN** farklı kattaki
    düğmeye basılır (`IsSessionActive` true olsa da), **THEN** ışık
    yanmaz, kuyruk oluşmaz.
13. **GIVEN** `Waiting` (movement lock aktif), **WHEN** oyuncu Move ve
    Look girdisi verir, **THEN** Move pozisyonu değiştirmez, Look
    kamerayı serbestçe döndürür.

## Open Questions

1. **Kabin nesnesi: paylaşılan tek GameObject mi, yoksa kat başına ayrı
   ama görsel olarak özdeş bir kabin mi?** Core Rules "kabinin kendisi
   zaten fiziksel olarak oradadır" diyor ama bunun tek bir paylaşılan
   nesne mi, yoksa her katta ayrı ama özdeş dekorlu bir kabin mi
   olduğunu netleştirmiyor — bu, additive sahne yükleme sırasında görsel
   pop riskini etkiler. **Güncelleme (design-review, 2026-08-03)**: Bu
   sorunun önceki çerçevesi ("Seviye/Sahne Geçişi'nin paylaşılan
   'Environment' kalıcı sahnesi") artık geçersiz — o GDD'nin kendi
   design-review'ünde bu fikir terk edildi (bkz.
   `seviye-sahne-gecisi.md` Core Rules, "RenderSettings/lightmap
   stratejisi somutlaştırıldı": baked lightmap verisi artık sahne
   başına ayrı kalıyor, paylaşılan bir Environment sahnesi yok). Bu
   yüzden gerçek soru artık daha dar: kabin **her katta ayrı bir
   GameObject/prefab örneği** olmalı (paylaşılan bir sahne kavramı
   zaten yok). **Owner düzeltmesi (design-review, 2026-08-03 —
   unity-specialist bulgusu)**: Önceki "Owner: unity-specialist" yanlış
   atanmıştı — bu rolün kendi tanımı tasarım kararı vermeyi kapsam dışı
   bırakıyor (sadece uygular). Gerçek sahip: level-designer +
   technical-director (birlikte, ya da bir ADR üzerinden). **Hedef
   çözüm**: implementasyondan önce.
2. **Unity 6.3 RenderGraph/multi-scene camera stacking spike'ı hâlâ
   yapılmadı.** Seviye/Sahne Geçişi'nin kendi Open Questions'ında
   işaretlendi, ama bu sistem SOFT transition'ın birincil kullanıcısı
   olduğu için doğrudan etkileniyor. **Owner**: teknik doğrulama
   (implementasyon öncesi). **Hedef çözüm**: Detailed Design
   kilitlenmeden önce.
