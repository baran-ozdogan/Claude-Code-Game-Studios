# Görev/Taşıma Döngüsü (Task/Carry Loop)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-04.md`)
> — 2026-08-04 verification turu, önceki "additive, Approved'ı geçersiz
> kılmıyor" değerlendirmesini geri aldı: `OnFinalRoundStarted`'ın
> `IsFinalRoundActive`'i round *aktivasyonunda* değerlendirmesi (round
> *ilerlemesinde* değil), Sahne Kesmeli Anlatı ile birleşince son round'u
> hiç oynatmadan geceyi bitirebiliyor — bu bir tasarım kararı gerektiriyor,
> henüz çözülmedi. Ayrıca gecenin round-bazlı gerilim birikiminin hiçbir
> sistemde tanımlı olmadığı bulundu (`game-concept.md`'nin vaadi
> karşılıksız) — bu da ayrı bir tasarım kararı. Mekanik düzeltmeler
> (bayat etiketler, var olmayan "ducking" referansı, tek yönlü
> bağımlılıklar) bu turda kapatıldı.
> **Author**: user + agents
> **Last Updated**: 2026-08-04
> **Implements Pillar**: Pillar 3 (Görev Gerçekliği)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (accepted) 2026-08-02
> **Design Review (2026-08-02)**: Üç tur — (1) NEEDS REVISION → aynı
> oturumda revize edildi (görsel model çelişkisi, sahne-geçişi
> kalıcılığı, edit-time validasyon, AC boşlukları, ve 4 diğer blocking
> madde); (2) re-review NEEDS REVISION (dar kapsam) → depo-reload
> uzlaşımı, görev-listesi algı kanalı, AC#15/16 promote edildi; (3)
> kalan maddeler stokastik/playtest-bağımlı risk olarak
> değerlendirildi (creative-director ayrımı) → **APPROVED**, tam
> re-review yapılmadı. Kalan riskler Vertical Slice playtest'e
> devredildi — bkz. `design/gdd/reviews/gorev-tasima-dongusu-review-log.md`

## Overview

Görev/Taşıma Döngüsü, gecenin çekirdek oyun döngüsüdür: bir görev
listesi (bu gece taşınması gereken malzemeler) ile başlar, oyuncu
Etkileşim Sistemi'nin `Instant` etkileşimiyle bir malzemeyi alır,
Birinci Şahıs Kontrolcü'nün `IsCarrying` durumunu tetikler, Asansör/
Kat-Erişim Sistemi üzerinden depo katından balo salonu katına geçer,
malzemeyi teslimat noktasına bırakır, ve bu döngü gecenin 3-5 taşıma
turu tamamlanana kadar tekrarlanır. Sistem, taşıma kapasitesini basit
bir slot modeliyle yönetir (N slot, her eşya 1 slot — Birinci Şahıs
Kontrolcü'nün Open Questions'ında bırakılan, kullanıcının gerçek iş
deneyiminden gelen karar; ağırlık/boyut karmaşıklığı yok).

Oyuncu için bu, oyunun "asıl olduğu şeydir" — otelin diğer tüm
sistemleri (ışık, ses, anı-tetikleyiciler) bu basit, tekrarlayan işin
üzerine inşa edilir (Pillar 3: Görev Gerçekliği). Bu sistem olmadan
oyunun bir iskeleti yoktur — sadece atmosferik bir keşif alanı kalır,
"iş" kaybolur. Tüm turlar tamamlandığında sistem, gecenin bitişini
tetikleyecek olan Sahne Kesmeli Anlatı sistemine bir tamamlanma sinyali
sağlar (design-review, 2026-08-04 — verification bulgusuyla "henüz
tasarlanmamış" etiketi düzeltildi — bu sistem 2026-08-02'de tasarlandı,
etiket gözden kaçmıştı).

## Player Fantasy

Her tur geçtikçe, iş elleri daha az düşünmeye zorlar — "Eller Zaten
Biliyor" ve "Bedenin Hafızası" devreye girdikçe, göreve ayrılan bilinçli
dikkat boşalır. Ama bu boşluk boş kalmaz: birinci turda ellerine bakan
oyuncu, dördüncü turda koridora bakıyordur (**Dikkatin Göçü**, Pillar 3:
Görev Gerçekliği). Döngünün asıl işlevi bu: dikkatin ihtiyaç duyduğu
fazlalığı üretmek, ve o fazlalığın gidebileceği tek yer otelin kendisi
oluyor.

Görev listesi küçüldükçe rahatlama beklenir ama gerilim onunla aynı
oranda azalmaz — söz verilen "neredeyse bitti" hissi ilk turlarda
gerçektir, ama liste sıfıra yaklaştıkça artık aynı çıkışı satın
almıyordur. "Ölü Zaman"ın asansördeki zorunlu durgunluğu, bu göçün
gidebileceği en yoğun yerdir — elin işi yokken dikkat başka hiçbir yere
gidemez.

> **Algı kanalı notu (design-review, 2026-08-02 — ikinci revizyon)**:
> Bu rahatlama bir sayaç okuyarak hissedilmez — hiçbir manifest/liste
> UI'ı yok, kasıtlı olarak (bkz. Core Rules > Round/liste tamamlanma).
> Kanalı, "Dikkatin Göçü"nü de üreten **aynı round-bazlı
> prominence/ışık sönme sinyalidir** (bkz. Visual Requirements, Tuning
> Knobs): round ilerledikçe eşyaların aldığı ışık/çerçeve belirginliği
> söner, bu da ambiyans olarak "geceye daha derindeyim" duygusunu
> üretir — kesin bir sayı değil, sezgisel/kümülatif bir "daha ileri"
> hissi (birinci turdaki parlak/net eşya ile son turdaki sönük/periferik
> eşya arasındaki fark, oyuncunun ne kadar yol katettiğinin dolaylı
> göstergesidir). Bu, AC#14'ün koruduğu *işlevsel* slot-okunabilirliğinden
> (kaç slot dolu, her zaman net) ayrı bir kanaldır — AC#14 yalnızca
> eşyanın *varlığının* ışıktan bağımsız kalmasını garanti eder, eşyanın
> *estetik* parlaklığının round'a göre değişmesini engellemez. Aynı
> mekanizma hem "Dikkatin Göçü"nü hem bu rahatlama hissini üretir; iki
> ayrı sistem gerekmez.

Bu, diğer üç fantezinin (Bedenin Hafızası, Eller Zaten Biliyor, Ölü
Zaman) yanında duran dördüncü bir an değil — onların *neden* var
olduğunun mekanizmasıdır. Pillar 3'ün somut emeği, Pillar 1'in öznel
gerçekliğini üretiyor.

> **Tasarım hipotezi notu (design-review, 2026-08-02)**: Bu nedensellik
> iddiası ("Pillar 3'ün emeği Pillar 1'i üretir") şu an tek bir somut
> mekanizmaya dayanıyor — Visual Requirements'ta tanımlanan round-bazlı
> ışık/prominence sönme eğrisi. Bu eğri aşağıda bir placeholder olarak
> kilitlendi (bkz. Visual Requirements, Tuning Knobs), ama nihai etkisi
> henüz playtest ile doğrulanmadı. Bu bölüm bir **tasarım hedefi/hipotezi**
> olarak okunmalı, kanıtlanmış bir davranış olarak değil — Vertical Slice
> playtest'i bu iddiayı doğrulamalı ya da eğriyi/mekanizmayı revize
> ettirmeli.

## Detailed Design

### Core Rules

- **Görev listesi verisi**: `TaskList` = sıralı `CarryRound[]` (3-5 eleman).
  Her `CarryRound`: `List<CarryItemDef> Items` (sayı ≤ N slot — bir round,
  tek bir tam yük gezisidir). Depodaki her eşyanın sabit bir spawn
  noktası vardır; round'un tüm eşyaları aynı anda aktif/etkileşilebilir
  olur, sıradaki round'lar henüz spawn edilmemiş/deaktif kalır (registry'ye
  hiç girmezler).
- **Alma**: Her eşya `CarryItemPickup : IInteractable`, `Type=Instant`.
  `CanInteract` = (dolu slot < N) AND (eşya aktif round'a ait). Slotlar
  dolduğunda `CanInteract=false`, `PromptText` "Eller Dolu" gösterir.
  `OnInteract()`: eşya `SetActive(false)` olur (registry'den `OnDisable`
  ile otomatik çıkar, Etkileşim Sistemi'nin zaten tanımladığı
  snapshot-iterasyon deseni sayesinde güvenli), dolu-slot +1; sayaç 0→1'de
  **tek sefer** `SetCarrying(true)` çağrılır — 2. ve sonraki alımlarda
  `SetCarrying` TEKRAR çağrılmaz (bkz. Acceptance Criteria).
  **Görsel model (design-review, 2026-08-02 — düzeltildi)**: Taşınan her
  eşyanın, oyuncunun kamera/el soketine bağlı **kendi küçük temsili**
  vardır (item-data'dan/ScriptableObject'ten mesh/materyal) — N küçük
  olduğu için (2-4) bu sabit boyutlu, sahne başına bir kez oluşturulmuş
  bir slot-temsili havuzudur (`N` adet önceden ayrılmış temsil; her
  alımda instantiate/destroy YOK, sadece görünürlük/soket-pozisyon
  değişir). Yeni alınan bir eşyanın temsili, doldurduğu slotun
  soket-pozisyonuna (1. eşya merkezi, 2. eşya altında/üstünde — bkz. UI
  Requirements) anında yerleşir; bunu satmak için ~0.05-0.1s'lik küçük
  bir soket-offset "yerleşme" darbesi (impulse — spring/offset eğrisi,
  ayrı bir animasyon state machine'i ya da cross-fade DEĞİL) eklenir. Bu,
  "no cross-fade / no separate pickup animation" kuralını bozmaz — bu bir
  geçiş efekti değil, kısa bir fiziksel tepki.
- **Slot kapasitesi**: N, dolu-slot sayısıyla sert gate'lenir — dolduğunda
  yeni alım fiziksel olarak imkansızdır.
- **Teslimat**: Balo salonundaki drop-off noktası **kendi trigger-zone'u**
  — Asansör'ün deseniyle tutarlı, `IInteractable` DEĞİL. Zone'a girince
  taşınan tüm eşyalar **otomatik** teslim edilir (buton yok) — "Dikkatin
  Göçü" fantazisine uygun: alım kasıtlı/tek-basış, teslimat varışın
  kaçınılmaz sonucu. Her teslimde dolu-slot -1; sayaç 1→0'da
  `SetCarrying(false)`; tüm görünür slot-temsilleri aynı karede
  gizlenir/havuza döner.
- **Kalıcılık (persistence)**: `TaskList`/round durumu, dolu-slot sayısı
  ve aktif round index'i, `Gece/Oturum Durumu`'nun kurduğu desenle aynı
  şekilde **in-memory, sahne-yüklemeleri arası kalıcı bir servis** olarak
  tutulur (bkz. `design/quick-specs/gece-oturum-durumu-2026-08-02.md`
  Core Rules) — sahne-lokal bir MonoBehaviour DEĞİL. Depo↔balo salonu
  arası `RequestSoftTransition` bir sahne değişimidir (bkz. Asansör
  GDD'sinin "anlık Transform kopyalama" kararı); bu sistem her iki
  sahnede de aynı kalıcı state kaynağını okur/yazar, o yüzden taşınan
  eşya sayısı asansör yolculuğu sırasında asla sıfırlanmaz/kaybolmaz.
  Disk kalıcılığı yok (MVP'de tek gece), sadece runtime-kalıcı.
  **Mekanizma ve depo-reload uzlaşımı (design-review, 2026-08-02 — ikinci
  revizyon, netleştirildi)**: Somut mekanizma, `Gece/Oturum Durumu`'nun
  kendi `HashSet<string> FiredTriggerIds` deseniyle birebir aynı şekilde
  bir statik/singleton C# servisidir (persistent GameObject/
  `DontDestroyOnLoad` DEĞİL, sahneden tamamen bağımsız plain C# state) —
  bu servis aktif round için `HashSet<string> CollectedItemIds` tutar
  (her eşyanın Overview'da tanımlı sabit spawn noktası, kararlı bir
  item-id olarak kullanılır; round değiştiğinde küme temizlenir).
  Depo sahnesi `Seviye/Sahne Geçişi`'nin `RequestSoftTransition`'ı ile
  **her yüklendiğinde** (ilk yükleme VEYA asansörle geri dönüş sonrası
  reload — bkz. Dependencies notu, depo sahnesi gerçekten unload/reload
  olur, item GameObject'leri sahne-yazılı varsayılan (aktif) duruma
  döner), her `CarryItemPickup` kendi `Awake()`'inde (yani `OnEnable`/
  registry-kaydından **ÖNCE**) kendi item-id'sinin `CollectedItemIds`
  kümesinde olup olmadığını kontrol eder; oradaysa `SetActive(false)`
  çağrılır ve nesne `InteractableRegistry`'ye hiç girmez. Bu, oyuncunun
  depoya dönüp zaten teslim ettiği eşyaları yeniden toplanabilir
  görmesini (ve slot sayısını "hackleyerek" round'u bozmasını) yapısal
  olarak imkânsız kılar.
- **Round/liste tamamlanma**: Ayrı bir manifest yapısı yok — slot-doluluk
  durumu manifestin kendisidir. Round tamamlandı = bu round'da alınan tüm
  eşyalar teslim edildi (slotlar boş). Tüm round'lar tamamlandı = ana görev
  kuyruğu boş VE slotlar boş → `event Action OnTaskListCompleted` fırlar
  (Sahne Kesmeli Anlatı'nın abone olacağı sinyal).
- **`event Action OnFinalRoundStarted` (design-review, 2026-08-03 —
  verification N5 bulgusu, eklendi)**: Aktif hale gelen round, `TaskList`'in
  son round'u olduğunda tam olarak bir kez fırlar — ya gece başlangıcında
  (`TaskList` tek round'dan oluşuyorsa), ya da bir **RoundComplete → Idle**
  geçişinde yeni aktive edilen round son roundsa. `IsFinalRoundActive`
  önceden salt okunur bir sorguydu ve hiçbir değişim bildirimi yoktu — bu
  event, Sahne Kesmeli Anlatı'nın doygunluk guard'ını (`IsFinalRoundActive`
  ne zaman `true` olur) polling olmadan yeniden değerlendirebilmesi için
  eklendi (bkz. `gdd-cross-review-2026-08-03-verification.md`, N5). Round
  sayısı 1'den fazla azalan bir yön yok (round'lar geri sarılmaz), bu yüzden
  event gece başına en fazla bir kez fırlar — tekrar-tetiklenme guard'ı
  gerekmez.
- **`bool HasCarriedInFinalRound` + `event Action OnFinalRoundItemPickedUp`
  (design-review, 2026-08-04 — verification design-theory bulgusu, eklendi,
  kritik bulgu)**: `HasCarriedInFinalRound`, aktif round `TaskList`'in son
  round'uyken oyuncu **ilk kez** bir eşya alıp `IsCarrying=true` olduğu anda
  (Idle→Loading ya da Idle→Carrying geçişi) `true`'ya set edilir ve gece
  boyunca asla `false`'a dönmez (`FiredTriggerIds`/`PersistentShiftIds`
  gibi diğer "bir kez yazılır, hiç temizlenmez" alanlarla aynı desen). Aynı
  anda `OnFinalRoundItemPickedUp` tam olarak bir kez fırlar. **Neden
  eklendi**: Sahne Kesmeli Anlatı'nın doygunluk koşulu önceden sadece
  `IsFinalRoundActive`'e bakıyordu — bu, `OnFinalRoundStarted`'ın round
  **aktive olduğu** karede fırlaması yüzünden, tüm tetikleyicileri son
  rounddan önce bulmuş bir oyuncu için son round hiç oynanmadan geceyi
  bitirebiliyordu (bkz. `gdd-cross-review-2026-08-04.md`). `HasCarriedInFinalRound`
  guard'ı, doygunluğun geceyi bitirebilmesi için oyuncunun son round'un
  malzemesinden **en az birini fiilen elde almış** olmasını zorunlu kılar
  — HARD CUT artık her zaman oyuncu elinde son yükle yürürken gerçekleşir,
  bu da Seviye/Sahne Geçişi'nin "Bedenin Çalınması" (orta hareketten
  koparılma) fantazisiyle tam örtüşür (bkz. Sahne Kesmeli Anlatı Player
  Fantasy).
- **`int CurrentRoundIndex` (salt okunur, 0-tabanlı) + `int TotalRoundCount`
  (salt okunur) (design-review, 2026-08-04 — verification design-theory
  bulgusu, eklendi)**: `Highlight(round)` formülünün zaten kullandığı
  `roundIndex`/`roundCount` ile aynı sayaçlar (bkz. Visual Requirements),
  artık **dışa açık sorgular** olarak da mevcut. **Neden eklendi**:
  `game-concept.md`'nin "turlar ilerledikçe ortam gerilimi birikir" iddiası
  hiçbir sistemde karşılığı yoktu — bu sistemin kendi round-bazlı eğrisi
  (`Highlight`) prominence'ı **azaltıyordu**, artırmıyordu, ve bu doğru
  (Dikkatin Göçü'nün taşıyıcısı bu — bkz. Player Fantasy), ama gecenin
  genel gerilim eğrisinin de bir taşıyıcısı olmalıydı. Bu sorgular Adaptif
  Ses Sistemi'nin round-bazlı gerilim birikimi mekanizmasına sinyal sağlar
  (bkz. `adaptif-ses-sistemi.md` Core Rules) — round-değişimi zaten
  düşük-frekanslı bir olay olduğundan (birkaç dakikada bir), Adaptif Ses
  bu sorguları kendi ambiyans güncelleme döngüsünde doğrudan okur, ayrı
  bir event gerekmez.
- **Teslim edilmeyen eşya için zorunlu çözüm yok**: Oyuncu bir eşyayı
  alıp hiç teslim etmezse, eşya süresiz olarak taşınan slotta kalır —
  "geri koyma" mekaniği kasıtlı olarak yok (basitlik mandatı, gerçek iş
  hissi: geri dönüş yok).

### States and Transitions

| Durum | Giriş | Çıkış |
|---|---|---|
| **Idle (Depo)** | Round aktif, 0 eşya taşınıyor | Bir eşyaya `OnInteract()` → round tek eşyalıysa doğrudan **Carrying**; birden fazla eşyalıysa **Loading** |
| **Loading** *(yalnızca round >1 eşya içeriyorsa geçerli ara-durum)* | 1..N-1 eşya taşınıyor, roundda henüz alınmamış eşya var | Daha eşya alınır / slot dolar ya da roundun kalan tüm eşyaları alınmış olur → **Carrying** |
| **Carrying** | ≥1 eşya taşınıyor, `IsCarrying=true` | Oyuncu serbestçe hareket eder (asansör yolculuğu dahil) → drop-off zone'a giriş → **Delivering** |
| **Delivering** | Tüm taşınan eşyalar otomatik teslim edilir | Slotlar boşalır, `SetCarrying(false)` → **RoundComplete** |
| **RoundComplete** | Son round mu kontrol edilir | Değilse → **Idle** (yeni round aktive); ise → **AllRoundsComplete** |
| **AllRoundsComplete** | `OnTaskListCompleted` fırlatılır | Terminal (gece sonu sinyali dış sisteme devredilir) |

> **Not (design-review, 2026-08-02)**: Önceki taslakta ayrı **InTransit**
> ve **Carrying (Balo)** satırları vardı, Asansör'ün `Waiting`/`DoorsOpen`
> state'lerine bağlıydı — ama bu sistemin Asansör'e hiç doğrudan API'si
> yok (bkz. Dependencies), yani hiçbir kod pratikte bu geçişi
> dinlemiyordu; bu tanımsız bir sinyale dayanıyordu. Asansör yolculuğu
> sırasında bu sistem tamamen pasiftir: oyuncunun hareket kilidi
> Asansör tarafından tutulur/bırakılır, bu sistem kilidin ne zaman
> açıldığından bağımsız olarak yalnızca bir sonraki drop-off zone
> girişini bekler. Tablo artık bunu tek bir **Carrying** durumu olarak
> modelliyor — InTransit/Carrying(Balo) ayrımı yalnızca oyuncu
> deneyimini betimleyen bir anlatı notuydu, kod seviyesinde ayrı,
> tetiklenmesi gereken bir state değil.

### Interactions with Other Systems

- **Birinci Şahıs Kontrolcü**: `SetCarrying(true)` (0→1 slot dolduğunda),
  `SetCarrying(false)` (1→0 slot boşaldığında) — tek çağrı noktası.
- **Etkileşim Sistemi**: Eşya prefabları `IInteractable.Instant` uygular.
- **Asansör/Kat-Erişim Sistemi**: Doğrudan API çağrısı yok — bu sistem
  asansörün kendi trigger/call-button akışına "yolcu" olarak katılır.
- **Gece/Oturum Durumu**: `IsSessionActive` okunur (guard); ayrıca bu
  sistemin kendi round/slot state'i de aynı in-memory-kalıcı desende
  tutulur (bkz. Core Rules > Kalıcılık).
- **Adaptif Ses Sistemi**: `CarryItemDef`'in `JostleSounds` ve
  pickup/delivery SFX alanları, bu sistemin **"SFX" mixer grubuna**
  yönlendirilir (design-review, 2026-08-04 — verification bulgusuyla
  düzeltildi: bu satır önceden var olmayan bir "ducking kuralı"na atıfta
  bulunuyordu — proje ducking'i kasıtlı olarak reddediyor, bkz.
  `adaptif-ses-sistemi.md` Core Rules; kendi ses çalma mantığı bu GDD'de
  kalır, yalnızca mixer routing paylaşılır — bkz. Visual/Audio
  Requirements > Audio).
- **Seviye/Sahne Geçişi** (dolaylı bağımlılık): Doğrudan çağrı yapılmaz,
  ama depo↔balo salonu arası `RequestSoftTransition` bir sahne
  değişimi/yeniden-yükleme anlamına geldiği için, bu sistemin state'i
  sahne-lokal olamaz (bkz. Core Rules > Kalıcılık) — bu dolaylı bağımlılık
  önceki taslakta hiç not edilmemişti.
- **Sahne Kesmeli Anlatı**: `OnTaskListCompleted` event'ine abone olur;
  ayrıca `OnFinalRoundStarted` event'ine abone olur (design-review,
  2026-08-03 — verification N5 bulgusu, eklendi) VE `OnFinalRoundItemPickedUp`
  event'ine abone olur (design-review, 2026-08-04 — verification
  design-theory bulgusu, eklendi — doygunluk koşulunun üçüncü şartı) VE
  `IsFinalRoundActive` (salt okunur `bool` — aktif round'un TaskList'in
  son round'u olup olmadığını sorgular) VE `HasCarriedInFinalRound` (salt
  okunur `bool`, design-review 2026-08-04 eklendi) okur, preload
  zamanlaması için kullanılır (design-review, 2026-08-02, Sahne Kesmeli
  Anlatı quick-spec'inden
  eklendi — bkz.
  `design/quick-specs/sahne-kesmeli-anlati-2026-08-02.md`).

## Formulas

**N/A** — durum makinesi/sayaç mantığı, türetilmiş bir hesaplama yok.
N (slot kapasitesi), round sayısı (3-5) ve round başına eşya sayısı
hepsi elle yazılmış içerik yapılandırmasıdır, bir formülden türetilmez.
Tamamlanma bir sınır kontrolüdür (dolu slot sayısı == 0, round kuyruğu
boş), hesaplanmış bir değer değil. Toplam eşya sayısını N'e bağlayan
bir oran (ör. "eşya = k×N") Core Rules'da yok — olsaydı burada bir
formül olurdu, ama alma/teslimat/tamamlanma mantığı böyle bir şey
gerektirmiyor/ima etmiyor.

## Edge Cases

- **Eğer `IsSessionActive` taşıma sırasında (pickup ile teslim arası)
  false olursa**: Taşınan item sayısı ve `SetCarrying(true)` durumu
  sıfırlanmadan korunur. Oturum sonu bir teslim veya drop tetiklemez;
  yalnızca pickup etkileşimleri ve round akışı donar. Oturum tekrar aktif
  olduğunda round kaldığı yerden devam eder — carried state, session
  state'ten bağımsız bir kaynaktır.
- **Eğer bir `CarryRound`, content-authoring hatasıyla N'den fazla item
  içerirse, VEYA 0 item içerirse, ya da `N` 0 olarak yapılandırılırsa**:
  Runtime'da clamp/crash olmaz. Ancak bunların hepsi tasarım hatasıdır —
  **build-blocking validasyon** zorunlu kılınmalı, ve bu iki ayrı
  mekanizma gerektirir (design-review, 2026-08-02 — netleştirildi): (1)
  `OnValidate()` tabanlı bir Editor-time uyarı, asset manuel
  kaydedildiğinde/incelendiğinde anında görünür olur; VE (2) ayrı bir
  `IPreprocessBuildWithReport` build ön-işlem adımı, tüm
  `TaskList`/`CarryRound` asset'lerini tarayıp herhangi biri N>limit, 0
  item, ya da N=0 içeriyorsa **build'i durdurur** — yalnızca
  `OnValidate` CI build'ini geçirmez, ikisi birlikte gerekli. N=0 veya
  0-item'lı round özellikle tehlikelidir: N=0, `CanInteract = (dolu
  slot < N)` ifadesini kalıcı olarak `false` yapar (hard soft-lock, Idle
  durumundan asla çıkılamaz); 0-item'lı bir round ise girer girmez
  round-complete koşulunu (slot==0 VE roundun tüm itemleri dünyada yok)
  trivial şekilde sağlar — aşağıdaki "round N tamamlanma ile round N+1
  aktivasyonu arasında yield yok" kuralıyla birleşince aynı karede
  zincirleme boş-round tamamlanması riski taşır. Runtime'da hiçbir
  koşulda silent clamp/kırpma olmaz — build zaten bu durumları
  engellemiş olmalı.
- **Eğer oyuncu 0 item taşırken drop-off trigger-zone'una girerse**:
  Hiçbir şey olmaz — `carriedCount > 0` guard'ı sayesinde no-op'tur,
  spurious delivery riski yoktur.
- **Eğer drop-off trigger'ı fizik motoru kaynaklı aynı karede iki kez
  ateşlenirse** (çift `OnTriggerEnter`): Teslimat idempotent'tir — ikinci
  çağrıda carried count zaten 0 olduğundan guard devreye girer, no-op
  olur.
- **Eğer round N'in son item'inin teslimi ile round N+1'in aktivasyonu
  aynı karede denk gelirse**: Race condition değildir — teslim işlemi
  senkron sırayla yürütülür: (1) carried itemler decrement edilir, (2)
  slot==0 kontrolüyle round-complete değerlendirilir, (3) ancak bundan
  SONRA round N+1'in itemleri `InteractableRegistry`'ye kaydedilir. Kural:
  round-complete evaluation ile sonraki round aktivasyonu arasına asla
  frame-bölücü bir await/yield konulmayacak.
- **Eğer `OnTaskListCompleted`, oyuncu asansörde `Waiting` (movement-locked)
  durumundayken tetiklenmek istenirse**: **Yapı gereği imkânsızdır** —
  teslim yalnızca balo salonu katındaki drop-off zone'una fiziksel
  girişle gerçekleşir; oyuncu asansörde `Waiting` durumundayken o zone'a
  giremez (iki state karşılıklı dışlayıcıdır). `OnTaskListCompleted` her
  zaman oyuncu serbest hareket halindeyken ateşlenir — confirmed
  non-issue by construction, gereksiz bir guard eklenmemeli.
- **Eğer oyuncu bir round'un tüm itemlerini almadan (N'den az, M item
  alıp) drop-off'a girerse**: Yalnızca elindeki M item teslim edilir,
  slot M kadar azalır; ancak round henüz alınmamış (N-M) itemler
  dünyada/registry'de aktif kaldığından **round complete sayılmaz**.
  Oyuncu geri dönüp kalanları toplayabilir; round completion tetiği
  yalnızca "bu round'a ait TÜM itemler artık dünyada yok + slotlar boş"
  koşulunda ateşlenir.

## Dependencies

**Bağımlıdır** (hard):
- **Birinci Şahıs Kontrolcü** — `SetCarrying(bool)` çağırır (tek çağrı
  noktası); ayrıca taşıma sway'i FPC'nin faz-biriktiricisini okur (bkz.
  Visual/Audio Requirements)
- **Etkileşim Sistemi** — eşya prefabları `IInteractable.Instant`'ı
  uygular
- **Asansör/Kat-Erişim Sistemi** — kat geçişleri için kullanılır
  (doğrudan API çağrısı yok, asansörün kendi akışına "yolcu" olarak
  katılır)
- **Gece/Oturum Durumu** — `IsSessionActive` okur (salt okunur guard);
  round/slot state'i aynı in-memory-kalıcı desende tutulur

**Bağımlıdır** (soft/dolaylı — design-review, 2026-08-02 eklendi):
- **Seviye/Sahne Geçişi** — doğrudan çağrı yok, ama depo↔balo salonu
  arası `RequestSoftTransition` bir sahne değişimi olduğu için bu
  sistemin state'i o değişime sahne-lokal-olmayan bir kaynaktan
  hayatta kalmalı (bkz. Core Rules > Kalıcılık)
- **Adaptif Ses Sistemi** — pickup/delivery/jostle SFX'i bu sistemin
  "SFX" mixer grubuna yönlendirilir (design-review, 2026-08-04 —
  var olmayan "ducking" referansı düzeltildi, bkz. Interactions with
  Other Systems yukarıda)

**Kendisine bağımlı olanlar**:
- **Sahne Kesmeli Anlatı** *(2026-08-02, Quick Spec olarak tasarlandı)* —
  `OnTaskListCompleted` event'ine abone olur, `IsFinalRoundActive`,
  `HasCarriedInFinalRound` okur, `OnFinalRoundStarted`/`OnFinalRoundItemPickedUp`
  event'lerine abone olur (gece sonu tetikleyicisi + preload zamanlaması)
- **Adaptif Ses Sistemi** *(design-review, 2026-08-04 — verification
  design-theory bulgusu, eklendi)* — `CurrentRoundIndex`/`TotalRoundCount`
  sorgularını okur (round-bazlı gerilim birikimi mekanizması için, bkz.
  `adaptif-ses-sistemi.md` Core Rules)
- **Çoklu Gece İlerlemesi** (Vertical Slice, henüz tasarlanmadı) —
  geceler arası görev listesi durumunu genişletecek

**Not**: Tüm dört upstream bağımlılık (FPC, Etkileşim, Asansör,
Gece/Oturum) zaten kendi Dependencies bölümlerinde bu sistemi "henüz
tasarlanmadı" olarak listelemişti — bu GDD tamamlandığında o dört
dosyadaki referanslar artık geçerli (çift yönlü tutarlılık zaten
kurulmuştu, bu GDD onu tamamlıyor).

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| N (slot kapasitesi) | 2–4 | Çok sık gidiş-geliş, tur sayısı yapaylaşır | Tek turda çok fazla taşıma, "Dikkatin Göçü"nün kademeli birikimi kaybolur | Core Rules: alma/slot kapasitesi |
| Round sayısı (gece başına) | 3–5 (game-concept.md'de kilitli) | Gerilim birikmeye vakit bulamaz | Tekrar yorucu hale gelir, Pillar 2'yi (Sessiz Gerilim) aşırı geriyor | Core Rules: görev listesi |
| Round başına eşya sayısı | 1–N | Tek eşyalı turlar döngüyü tekdüzeleştirir | N'i aşarsa edit-time hata (bkz. Edge Cases) | Core Rules: görev listesi |
| Round-bazlı prominence sönme eğrisi (round 1 → son round highlight %) | %100 → %25-35 aralığı, eased (smoothstep) | Fark edilmez, "Dikkatin Göçü" sinyali kaybolur — bu sistemin merkezi iddiasının tek taşıyıcısı olduğundan risk yüksek | Erken roundlarda bile eşya "önemsiz" hissettirir, ilk turun tanıdıklık kurma işlevini zedeler | Visual Requirements: taşınan eşya prominence |
| Jostle tetikleyici yön-değişim eşiği | Hareket vektörü yön değişimi ≥30-60°, VE minimum 0.5-1.0s tekrar aralığı guard'ı (footstep sisteminin tekrar-önleme desenine paralel) | Sürekli tetiklenir, "tek atım" niyetini bozar, gürültü kirliliği | Fark edilmez hale gelir, amacını kaybeder | Audio Requirements: Jostle |

## Visual/Audio Requirements

### Visual

- **Alma (Pickup)**: Dünya öğesi `SetActive(false)` olur, aynı karede taşınan
  temsil kamera/el soketine bağlı olarak belirir (Core Rules'da tanımlı
  mesh/materyal değişimi). Bu geçiş kesintisiz — cross-fade veya ayrı bir
  "alma" animasyonu yok; ağırlık değişimi hissi, UI cilası değil.
- **Kol/El Rigi**: Basit bir kol/el modeli, eşyayı tutar şekilde first-person
  görünümde sürekli görünür. Karmaşık blend-tree gerekmez — tek bir statik
  "tutuş" pozu + yürüyüşe bağlı hafif sallanma (soket-pozisyon offsetiyle,
  ayrı bir animasyon state machine'i değil) yeterlidir. **Faz kaynağı
  (design-review, 2026-08-02 — netleştirildi)**: Bu sallanma, Birinci
  Şahıs Kontrolcü'nün head-bob/ayak-sesi senkronunu süren **aynı
  mesafe-bazlı faz biriktiriciyi** okur (bkz.
  `design/gdd/birinci-sahis-kontrolcu.md` Visual/Audio Requirements) —
  ayrı, bağımsız bir zamanlayıcı KULLANILMAZ; bu, FPC'nin kendi GDD'sinde
  "senkron kaybı bedenin işi bildiği fantazisini anında bozar" dediği
  riski bu sistemde de önler. `CarryItemDef` başına
  bir "tutuş pozu" varyasyonu (ör. tepsi iki elle, kutu tek kolda) art
  bible onaylandıktan sonra `/asset-spec` ile detaylandırılır.
  **CD-GDD-ALIGN notu**: Rig'in sürekli görünür olması, "Dikkatin Göçü"
  fantazisiyle gerilim taşır (dikkat görevden ayrılırken elin sürekli
  görünürlüğü onu geri çekebilir) — bu gerilim, "statik poz, blend-tree
  yok" kısıtının kesinlikle korunmasıyla yönetiliyor. Bu kısıt, ilgili
  story'de bir prose önerisi değil, bir **acceptance-criteria seviyesinde
  zorunluluk** olarak işaretlenmelidir (implementasyon aşamasında
  animasyon-cilası eklenmemeli).
- **Taşınan eşyanın görsel belirginliği — round'a göre söner**: 1. round'da
  eşya normal ışık/çerçeve muamelesi alır (elde, dikkat çekici). Round
  sayacı arttıkça (aynı `CarryRound` index'i referans alınır, tuning knob
  olarak açığa çıkar — bkz. Tuning Knobs) eşyanın aldığı ışık/rim-highlight
  kademeli azaltılır ve kamera çerçevesindeki konumu biraz daha periferik
  hale getirilir. **Mesh/materyal değişmez** — yalnızca ışık/framing.
  Bu, Pillar 1'in öznel gerçeklik kaymasını "Dikkatin Göçü" fantazisi
  üzerinden somut olarak üreten mekanizmadır.
  - **Sahiplik notu (design-review, 2026-08-02)**: "Kamera çerçevesindeki
    konum" değişimi kameranın kendisini hiç hareket ettirmez — Birinci
    Şahıs Kontrolcü'nün "sıfır controller-sahipli post-process/kamera
    efekti" kuralını ihlal etmez (bkz. o GDD'nin Visual/Audio
    Requirements'ı). Periferik hâle gelme tamamen **item-soket
    offsetiyle** (eşyanın el soketine göre pozisyonu hafifçe aşağı/yana
    kayar) elde edilir; kamera FOV/rotasyon/post-process'e hiç
    dokunulmaz.
  - **Placeholder eğri (design-review, 2026-08-02 — kilitlendi, bkz.
    Tuning Knobs)**: `Highlight(round) = lerp(1.0, 0.30,
    ease(roundIndex / (roundCount-1)))`, `ease` = smoothstep. Round 1'de
    %100 (tam belirginlik), son roundda ~%30. Bu bir **tasarım hipotezi
    placeholder'ı**dır — dokümanın Player Fantasy bölümündeki "Dikkatin
    Göçü" iddiasının tek somut taşıyıcı mekanizması budur; nihai eğri
    şekli/aralığı Vertical Slice playtest'i ile doğrulanmadan
    "kanıtlanmış" sayılmamalı (bkz. Player Fantasy'deki tasarım hipotezi
    notu). `/asset-spec` veya bir sonraki tuning geçişi bu eğriyi
    content-authoring'e çevirir, ama artık tanımsız değil.
    **Guard rail (design-review, 2026-08-04 — full re-verification bulgusu,
    eklendi)**: `roundCount=1` durumunda `(roundCount-1)` payda sıfıra
    bölme oluşturur — `adaptif-ses-sistemi.md`'nin `tension_gain`
    formülüyle aynı risk (aynı payda deseni, orada da aynı turda
    guard'landı). Düzeltme: `roundCount ≤ 1` ise `Highlight` sabit `1.0`'a
    (tam belirginlik) sabitlenir kod içinde, payda hesaplanmadan önce
    kontrol edilir. **AC1/AC17 ile ilişki**: bu senaryo AC1'in build-time
    3-5 round zorunluluğu altında MVP içeriğinde şu an ulaşılamaz olsa da
    (bkz. AC1, AC17), guard yine de eklenir — kod-seviyesinde degenerate
    girdiye karşı savunma, içeriğin onu asla üretmeyeceği garantisine
    değil, projenin kendi guard-rail ilkesine dayanır (bkz.
    `isik-volume-durum-sistemi.md`'nin `TIME_EPSILON`/`RADIUS_EPSILON`
    deseni).
- **Teslimat (Delivery)**: **Sıfır adanmış VFX/UI onayı.** Tek görsel iz,
  eşyanın el soketinden kaybolmasıdır; ortam/ambiyans kesintisiz devam eder.
  Bu kasıtlı — bir onay mikro-ödülü, "Dikkatin Göçü"nün teslimatın
  fark edilmeden, beklenmedik olmadan gerçekleşmesi gereken doğasıyla
  çelişir.

### Audio

- **Pickup/Delivery SFX**: Eşya tipine göre **farklı ama round'dan bağımsız
  sabit** sesler (kavrama/bırakma — farklı transient şekilleri, ikisi de
  diegetic, müzikal "stinger" niteliği yok). Round ilerledikçe **solma veya
  basitleşme yok** — bu, ekibin anı-tetikleyici stinger'ında zaten reddettiği
  "authored build-up" hatasının sesli versiyonu olurdu.
  **Mix spec (design-review, 2026-08-02 — eklendi)**: 3D spatialized
  (`spatialBlend=1`), item-soket pozisyonundan çalar, kısa min/max mesafe
  düşüşü (~0.5-3m — oyuncu her zaman yakın olduğu için etki küçük ama
  tutarlılık için tanımlı), Adaptif Ses Sistemi'nin **"SFX" mixer
  grubuna** bağlı (ambiyans/stinger/CutSting grup'larından ayrı, hiçbir
  dinamik ducking taşımaz — design-review, 2026-08-04 verification
  bulgusuyla düzeltildi: bu satır önceden var olmayan bir "ducking
  kuralı"na atıfta bulunuyordu, proje bunu kasıtlı reddediyor — bkz.
  `adaptif-ses-sistemi.md` Core Rules).
  **Düzeltme**: "Dikkatin Göçü, görev SFX'i tarafından değil diğer
  sistemler tarafından üretilmeli" iddiası fazla iddialıydı — Adaptif Ses
  Sistemi'nin ambiyans katmanları yalnızca **fiziksel bölgeye** göre
  crossfade olur, round index'ini hiç takip etmez (bkz. o GDD). Round-bazlı
  ilerlemeyi taşıyan **tek** somut mekanizma, Visual Requirements'taki
  ışık/prominence sönme eğrisidir — bu bölümün SFX'i kasıtlı olarak
  sabit kalıyor, ama bu "başka bir sistem devralıyor" anlamına gelmez.
- **Taşıma sırasında — Jostle (tek-atım)**: Sürekli bir taşıma loop'u YOK.
  Bunun yerine, `CarryItemDef` başına opsiyonel bir "jostle" ses havuzu
  (ör. tepsi şıngırtısı, kasa gıcırtısı), **hareket vektörünün yön
  değiştirdiği** anlarda (bkz. Tuning Knobs: "Jostle tetikleyici
  yön-değişim eşiği" — açısal eşik + minimum tekrar aralığı guard'ı,
  footstep sisteminin tekrar-önleme desenine paralel) tek atım olarak
  tetiklenir — fiziksel, duygusal değil. **Düzeltme (design-review,
  2026-08-02)**: Önceki taslakta "merdiven geçişleri" de tetikleyici
  olarak listelenmişti — oyunda kat geçişi yalnızca asansörle olur (bkz.
  Asansör/Kat-Erişim Sistemi), merdiven mekaniği yok; bu referans
  kaldırıldı. `footstep_volume` formülü (hız-bağımlı, state-bağımlı
  DEĞİL) bu kararla değişmeden kalır; jostle ayrı, bağımsız bir ses
  katmanıdır. **Asansör yolculuğu sırasında**: Jostle sesleri oyuncu
  hareketsizken (Move kilitli, Asansör'ün `Waiting` state'i) doğal olarak
  tetiklenmez (yön-değişim eşiği hareket gerektirir); taşınan eşyaların
  başka bir ses katmanı yoktur — bu, Asansör'ün kendi kozmetik
  sarsıntı/uğultu sesiyle çakışmayı önler, kasıtlı bir sessizlik değil
  sistemin doğal bir sonucu.
  **CD-GDD-ALIGN notu**: Jostle'ın round'dan bağımsız/artmayan doğası
  Pillar 2'yi "authored build-up" hatasından koruyan bir tasarım
  disiplinidir — implementasyon story'sinde bu round-bağımsızlık da
  acceptance-criteria seviyesinde doğrulanmalıdır.
- **Teknik**: Unity built-in Audio (AudioSource/AudioMixer) — FMOD/Wwise
  yok, proje genelinde zaten kilitli karar. Pickup/delivery/jostle düşük
  frekanslı olaylar (round başına ~3-8), havuzlama gerekmez.
- **Veri modeli notu**: `CarryItemDef` (ScriptableObject) mevcut
  mesh/materyal alanlarına ek olarak opsiyonel `AudioClip[] JostleSounds`
  alanı gerektirir — Core Rules'daki mevcut veri modelini genişletir,
  çelişmez.

> 📌 **Asset Spec** — Visual/Audio requirements tanımlandı. Art bible
> onaylandıktan sonra `/asset-spec system:gorev-tasima-dongusu` çalıştırarak
> bu bölümden asset-bazlı görsel açıklamalar, boyutlar ve üretim
> prompt'ları üretilebilir.

## UI Requirements

- **Sıfır ekran-uzayı HUD elemanı** (sayaç/ikon yok). Mevcut kol/el rigi'nin
  kendisi slot göstergesidir: 1. eşya merkezi/iki-elle-yakın pozda, 2. eşya
  ilk eşyanın altında/üzerinde istiflenmiş şekilde görünür, N'e kadar devam
  eder. N=2-4 aralığında bu tek bakışta okunur — kullanıcının gerçek iş
  deneyimindeki fiziksel sezgiyle birebir örtüşür, yeni UI yüzeyi
  gerektirmez, Pillar 3'ü (Görev Gerçekliği) güçlendirir.
  **Çelişki düzeltmesi (design-review, 2026-08-02)**: Önceki taslakta Core
  Rules tek bir mesh-swap temsili tanımlarken bu bölüm N'e kadar
  istiflenmiş görünürlük varsayıyordu — çelişki giderildi, Core Rules
  artık N'e kadar küçük, önceden havuzlanmış slot-temsilini destekliyor
  (bkz. Core Rules > Alma). **Proaktif sinyal notu**: Bu rig sürekli
  görünür olduğu için zaten pasif/proaktif bir sinyaldir — oyuncu
  dolmadan önce de kaç slotun dolu olduğunu görebilir; "Eller Dolu" text
  prompt'u yalnızca bloklanmış bir deneme anındaki **reaktif** onay
  katmanıdır, ayrı bir proaktif HUD/ses ipucu eklenmedi (kasıtlı — "sıfır
  ekran-uzayı HUD" ilkesiyle tutarlı).
- **"Eller Dolu" prompt'u** (Core Rules'da zaten tanımlı, `CanInteract=false`
  durumunda) tek metin tabanlı dokunuş noktasıdır — ek bir UI parçası değil,
  Etkileşim Sistemi'nin zaten sahip olduğu prompt mekanizmasının bir
  kullanımı.
- **Slot okunabilirliği round-bazlı ışık sönmesinden muaf**: Visual
  Requirements'ta tanımlanan "eşyanın estetik önemi round'a göre söner"
  kuralı yalnızca eşyanın *estetik* ışık/çerçeve muamelesini kapsar — kol/
  rig üzerindeki slot-doluluk okunabilirliği bundan etkilenmez, her zaman
  net kalır. Gerekçe: dikkatin göçü yalnızca isteğe bağlı/atmosferik
  detayı hedeflemeli; işlevsel bilginin (kaç slot dolu) bulanıklaşması
  oyuncuyu fiilen kaybettirir/askıda bırakır (soft-lock riski),
  "Dikkatin Göçü"nün amaçladığı deneyim değil bir bug gibi hissettirir.
  Bu kural artık Acceptance Criteria'da da test edilebilir şekilde ifade
  edildi (bkz. AC'ler) — sadece prose değil.
- **Erişilebilirlik — opsiyonel sayısal fallback**: Ayarlardan açılabilen,
  varsayılanı KAPALI bir "N/M" metin göstergesi (Eller Dolu prompt'unun
  yanında/yerine), düşük görüşü oyuncular için. **Not (design-review,
  2026-08-04 — verification bulgusu, düzeltildi)**: bu satır önceden
  "Adaptif Ses Sistemi'ndeki stinger-caption bulgusuyla aynı desen"
  diyordu — bu **yanlıştı**: stinger caption'ı (`adaptif-ses-sistemi.md`
  AC14a) koşulsuz/varsayılan-açık, bu gösterge ise ayarlardan
  açılan/varsayılan-KAPALI. İkisi aynı desen değil; stinger caption'ının
  aynı deseni takip edip etmemesi ayrı, henüz karar verilmemiş bir
  tasarım sorusu (bkz. `gdd-cross-review-2026-08-04.md` Warnings). Bu
  gösterge ve stinger-caption sorusu birlikte
  `design/ux/accessibility-requirements.md` dosyasının iki seed girdisini
  oluşturur (bu dosya henüz oluşturulmadı, bu GDD ikinci tetikleyicidir).

> **📌 UX Flag — Görev/Taşıma Döngüsü**: Bu sistemin UI gereksinimi var.
> Pre-Production'da, epic yazımından ÖNCE bu slot göstergesi ve
> opsiyonel sayısal fallback için `/ux-design` ile bir UX spec
> oluşturulmalı (`design/ux/carry-slot-indicator.md` veya HUD spec'in
> içinde). Story'ler bu UI'a referans verirken GDD'yi değil UX spec'i
> kaynak göstermeli.

## Acceptance Criteria

> **Not (design-review, 2026-08-02)**: Bu bölüm review'de tespit edilen
> beş boşluğu kapatmak için genişletildi: "Eller Dolu" gate'inin AC'si
> yoktu (#3), `SetCarrying` tek-seferlik garantisinin negatif durumu
> test edilmiyordu (#4), tam happy-path testi gereksiz yere tamamen
> ertelenmişti (şimdi #9a mock'lanabilir, sadece #9b gerçek sahne
> gerektiriyor), round-dimming'in slot-okunabilirliğini etkilememesi
> kuralı hiç test edilmiyordu (#14), ve dokümanın kendi "AC seviyesinde
> zorunluluk" dediği iki kısıt (no-blend-tree, jostle round-bağımsızlığı)
> hiç AC olarak yazılmamıştı (#15, #16).

1. **GIVEN** bir TaskList 3-5 CarryRound içerecek şekilde tasarlanmış,
   **WHEN** edit-time validasyon çalışır, **THEN** CarryRound sayısı 3-5
   aralığında değilse validasyon hatası verir, build'e izin vermez.
2. **GIVEN** oyuncu bir dünya öğesinin `IInteractable.Instant` menziline
   girmiş, **WHEN** oyuncu etkileşime girer, **THEN** dünya öğesi deaktive
   olur, slot sayısı 1 artar; bu round'daki ilk öğeyse `SetCarrying(true)`
   tetiklenir.
3. **GIVEN** oyuncu zaten N/N slot dolu (**Eller Dolu**), **WHEN**
   oyuncu aktif rounddan bir öğeyle etkileşime girmeye çalışır, **THEN**
   `CanInteract` `false` döner, hiçbir state değişikliği olmaz,
   `PromptText` "Eller Dolu" gösterir.
4. **GIVEN** oyuncu zaten ≥1 öğe taşıyor (`IsCarrying=true`), **WHEN**
   2. veya sonraki bir öğeyi alır, **THEN** `SetCarrying(true)` TEKRAR
   çağrılmaz — yalnızca 0→1 geçişinde bir kez tetiklenir.
5. **GIVEN** oyuncu ≥1 öğe taşıyor, **WHEN** oyuncu teslimat
   trigger-zone'una girer, **THEN** taşınan TÜM öğeler tek seferde teslim
   edilir, slot sayısı sıfırlanır, `SetCarrying(false)` tetiklenir.
6. **GIVEN** round'un orijinal tüm öğeleri dünyadan kaldırılmış VE
   slotlar boş, **WHEN** tamamlanma kontrolü çalışır, **THEN** round
   "complete" sayılır; koşullardan biri eksikse round complete SAYILMAZ.
7. **GIVEN** görev kuyruğu boş VE slotlar boş, **WHEN** son kontrol
   çalışır, **THEN** `OnTaskListCompleted` bir kez tetiklenir.
8. **GIVEN** round N tamamlanma kontrolü aynı karede yapılıyor, **WHEN**
   round N complete olarak işaretlenir, **THEN** round N+1 öğeleri
   `InteractableRegistry`'ye AYNI karede, kontrolden hemen sonra register
   edilir; arada yield yoktur.
9a. **GIVEN** 3 round'luk bir `TaskList`, mock'lanmış pickup ve
   drop-off-zone-girişi sinyalleriyle (gerçek sahne/collider/asansör
   zamanlaması GEREKMEZ — bu sistem asansör state'ini hiç okumaz, bkz.
   States and Transitions notu; asansör yolculuğu bu sistem için opak
   bir bekleme), **WHEN** her round sırayla mock-toplanır → mock
   geçiş-sinyali alınır → mock teslimat tetiklenir, **THEN** her round
   sırayla complete olur, son round sonrasında `OnTaskListCompleted`
   tetiklenir, kuyruk ve slotlar boş kalır. (design-review, 2026-08-02
   — eklendi: bu senaryo saf state-machine mantığı olduğundan artık
   ERTELENMİYOR, unit/integration test ile şimdi yazılabilir.)
9b. **GIVEN** 3 round'luk aktif bir TaskList, gerçek asansör + gerçek
   teslimat alanı olan bir seviye, **WHEN** oyuncu her round'da öğeleri
   toplar → gerçek asansörle taşır → gerçek teslimat alanına girer →
   round complete → sıradaki round register olur ve bunu son round'a
   kadar tekrarlar, **THEN** #9a ile aynı sonuç gerçek sahnede de
   doğrulanır — **ERTELENDİ** (tam motor build'i gerektirir: asansör
   zamanlaması, trigger collider'ları, gerçek sahne).
10. **GIVEN** oyuncu öğe taşırken `IsSessionActive` false olur, **WHEN**
    state okunur, **THEN** slot sayısı ve carrying state korunur; yeni
    pickup girişimleri `CanInteract=false` döner ve sessizce reddedilir
    (donma/hata/uyarı YOK — design-review, 2026-08-02: "donar/reddedilir"
    ifadesindeki belirsizlik giderildi, tek davranış olarak sessiz red
    seçildi), mevcut state bozulmaz.
11. **GIVEN** bir CarryRound izin verilen N'den fazla öğeyle YA DA 0
    öğeyle yapılandırılmış, ya da `N` 0 olarak ayarlanmış, **WHEN**
    build-time validasyon (`IPreprocessBuildWithReport`) çalışır,
    **THEN** build engellenir; runtime'da clamp/kırpma YAPILMAZ (bkz.
    Edge Cases).
12. **GIVEN** oyuncu 0 öğe taşıyor, **WHEN** teslimat alanına tek veya
    çift (double-fire) girer, **THEN** guard sayesinde hiçbir state
    değişikliği tetiklenmez; davranış idempotenttir.
13. **GIVEN** round N öğe içeriyor, oyuncu yalnızca M<N öğe toplayıp
    teslim ediyor, **WHEN** teslimat sonrası tamamlanma kontrolü çalışır,
    **THEN** round complete sayılmaz; kalan N-M öğe dünyada toplanabilir
    kalmaya devam eder.
14. **GIVEN** son round'da (round-bazlı prominence dimming ~%30
    highlight'a inmiş), **WHEN** oyuncu dolu slot sayısını rig üzerinden
    okumaya çalışır, **THEN** slot-temsillerinin pozisyon/varlık
    okunabilirliği estetik ışık/rim-highlight değerinden bağımsız olarak
    değişmeden kalır — yalnızca estetik ışık kanalı söner, slot-varlık/
    pozisyon kanalı hiç etkilenmez.
15. **[Otomatik, Logic-tier — design-review, 2026-08-02, ikinci revizyon:
    ADVISORY'den yükseltildi]** **GIVEN** kol/rig prefabı, **WHEN**
    otomatik bir yapısal test çalıştırılır (`Component`/`Animator`
    varlığı üzerinden — sahne/render gerektirmez), **THEN** rig
    GameObject'inde ve child'larında hiçbir `Animator`/blend-tree/ayrı
    animasyon state machine bileşeni bulunmadığı, yalnızca soket-offset
    transform sürücüsünün var olduğu doğrulanır. **Gerekçe**: Bu bir
    "feel" değerlendirmesi değil, bir component-yokluğu kontrolüdür —
    dokümanın kendi dilinin ("acceptance-criteria seviyesinde
    zorunluluk") gerektirdiği kesinlikte, `.claude/docs/coding-standards.md`
    Logic-tier BLOCKING gate'i kapsamında.
16. **[Otomatik, Logic-tier — design-review, 2026-08-02, ikinci revizyon:
    ADVISORY'den yükseltildi; indeks kuralı 2026-08-04 full re-verification
    bulgusuyla düzeltildi]** **GIVEN** jostle ses seçim fonksiyonu
    (saf fonksiyon — round index girdi alır, ses havuzu/volume/pitch
    parametreleri döner), **WHEN** round index **`0`'dan `roundCount-1`'e**
    (0-tabanlı, `CurrentRoundIndex`/AC19 ile aynı kural — önceki hali
    "1..roundCount" diyordu, bu proje genelindeki 0-tabanlı `roundIndex`
    kuralıyla çelişiyordu ve `Highlight`/`tension_gain`'in kullandığı
    aynı değişkenle bir implementasyon off-by-one riski taşıyordu) aralığında
    her değerle çağrılır, **THEN** dönen parametrelerin round index'ten
    bağımsız/deterministik olduğu (round'lar arası fark YOK) otomatik
    unit test ile doğrulanır. **Gerekçe**: Ses seçim fonksiyonunun kendisi
    saf ve mock'lanabilir olduğundan bu "feel" değil, bir çıktı
    değişmezliği testidir — AC#15 ile aynı gerekçeyle ADVISORY'den
    Logic-tier BLOCKING'e yükseltildi.
17. **[design-review, 2026-08-03 — verification N5 bulgusu, eklendi;
    ikinci GIVEN'ın AC1 ile ilişkisi 2026-08-04 full re-verification
    bulgusuyla netleştirildi]** **GIVEN** `RoundComplete → Idle`
    geçişiyle aktive edilen yeni round `TaskList`'in son round'u, **WHEN**
    geçiş gerçekleşir, **THEN** `OnFinalRoundStarted` tam olarak bir kez
    fırlar. **GIVEN** `TaskList` tek round'dan oluşuyor, **WHEN** gece
    başlar (`Idle` ilk kez girilir), **THEN** `OnFinalRoundStarted` gece
    başlangıcında fırlar (round-tamamlanma geçişini beklemez). **Netleştirme
    (design-review, 2026-08-04)**: Bu ikinci GIVEN, AC1'in 3-5 round
    build-time zorunluluğu altında **MVP içeriğinde şu an ulaşılamaz** —
    çelişki değil, savunmacı/ileri-uyumluluk davranışıdır (ör. Full
    Vision'da round sayısı kuralı gevşerse). Sistemin state machine'i
    (States and Transitions tablosu) tek-round senaryoyu zaten doğal
    olarak destekliyor (Idle girişi → doğrudan son round kontrolü), bu
    yüzden davranışı belgelemenin maliyeti sıfıra yakın — sadece bu
    kriterin bir mock/birim testle doğrulanabileceği, gerçek bir 1-round
    `TaskList` asset'i ile değil (AC1 onu build'de reddeder).
18. **[BLOCKING, design-review 2026-08-04 — verification design-theory
    bulgusu, eklendi]** **GIVEN** aktif round son round VE oyuncu henüz
    bu roundda hiç eşya almamış (`HasCarriedInFinalRound=false`), **WHEN**
    oyuncu son round'un ilk eşyasını alır (`Idle→Loading`/`Idle→Carrying`),
    **THEN** `HasCarriedInFinalRound` `true` olur VE `OnFinalRoundItemPickedUp`
    tam olarak bir kez fırlar. Gece boyunca bu geçiş sadece bir kez
    gerçekleşir — sonraki eşya alımlarında event tekrar fırlamaz
    (`HasCarriedInFinalRound` zaten `true`).
19. **[design-review, 2026-08-04 — verification design-theory bulgusu,
    eklendi]** **GIVEN** 4 round'luk bir gece, **WHEN** her round aktive
    olduğunda `CurrentRoundIndex` sorgulanır, **THEN** sırasıyla `0, 1, 2, 3`
    döner, `TotalRoundCount=4` sabit kalır — `Highlight(round)` formülünün
    kullandığı `roundIndex`/`roundCount` ile birebir aynı değerler (aynı
    sayaç, iki farklı tüketici).

## Open Questions

- **N (slot kapasitesi) kesin değeri belirlenmedi** — Tuning Knobs'ta
  güvenli aralık (2–4) var ama nihai değer playtest ile netleşecek.
  Sahip: Vertical Slice playtest.
- **Round-bazlı ışık/prominence sönme eğrisi — placeholder kilitlendi,
  final content-authoring hâlâ açık** (design-review, 2026-08-02):
  Visual Requirements'ta artık somut bir placeholder formül var
  (`lerp(1.0, 0.30, ease(roundIndex/(roundCount-1)))`), ama bu hâlâ bir
  hipotez — Vertical Slice playtest'i bu eğrinin "Dikkatin Göçü"
  fantazisini gerçekten ürettiğini doğrulamalı; doğrulanmazsa eğri
  şekli/aralığı VEYA mekanizmanın kendisi revize edilmeli. Sahip:
  `/asset-spec` (content-authoring) + Vertical Slice playtest
  (doğrulama).
- **Jostle ses kaynağı (kayıt mı, asset paketi mi) seçilmedi** —
  Birinci Şahıs Kontrolcü'nün ayak sesi kaynağı sorusuyla aynı desen.
  Sahip: Adaptif Ses Sistemi / ses prodüksiyonu.
- **Kol/rig tutuş poz varyasyonları (`CarryItemDef` başına)
  detaylandırılmadı** — art bible onaylanmadan somutlaşmaz.
  Sahip: `/asset-spec`, art bible onayından sonra.
- **`design/ux/accessibility-requirements.md` henüz oluşturulmadı** — artık
  iki GDD (Adaptif Ses'in stinger-caption sorusu + bu GDD'nin sayısal
  slot fallback'i) bu dosyaya seed sağlıyor; dosyanın ne zaman fiilen
  oluşturulacağı (üçüncü bir sistem mi beklensin, yoksa şimdi mi
  açılsın) açık. Sahip: kullanıcı kararı / `/ux-design`.
- **AC #9b (tam entegrasyon senaryosu) ertelendi** — asansör zamanlaması,
  gerçek trigger collider'ları ve gerçek sahne gerektiriyor; Asansör/
  Kat-Erişim implementasyonu ve seviye hazır olduğunda test edilecek.
  (AC #9a artık mock'lanmış sinyallerle şimdi test edilebilir —
  design-review, 2026-08-02.) Sahip: `/dev-story`, implementasyon
  aşaması.
- **Round-to-round mekanik varyasyon eksikliği** (design-review,
  2026-08-02, game-designer bulgusu) — sabit rota, round-bağımsız jostle
  ve tek değişken lever olarak eşya sayısı, 3-5 round boyunca "fazlalık
  dikkat" yerine düz tekrar/sıkıcılık riski taşıyabilir. GDD metni
  değiştirilmedi (bu bir tasarım hipotezi riski, mekanik bir hata değil)
  ama Vertical Slice playtest'inde özellikle izlenmeli. Sahip: Vertical
  Slice playtest.
- **"Geri koyma yok" kararının oyuncu hayal kırıklığı riski** (design-review,
  2026-08-02, game-designer bulgusu) — N=2-4 gibi küçük bir kapasitede
  yanlış/istenmeyen bir eşya alımı, round boyunca kapasitenin yarısına
  kadarını kilitleyebilir. Kural thematik olarak korunuyor (kasıtlı
  basitlik mandatı) ama playtest bu sürtünmeyi özellikle ölçmeli. Sahip:
  Vertical Slice playtest.
