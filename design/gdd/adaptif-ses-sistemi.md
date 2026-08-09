# Adaptif Ses Sistemi (Adaptive Audio System)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`
> ve `design/gdd/gdd-cross-review-2026-08-03-verification.md`) — bu doküman
> `/review-all-gdds`'in ikinci turunda tekrar `Needs Revision`'a taşındı,
> önceki `Approved` başlığı header'da yanlışlıkla kalmıştı (propagation-gap
> pass sırasında `seviye-sahne-gecisi.md`'nin header'ı düzeltildi ama bu
> dosyanınki gözden kaçmıştı — 2026-08-03'te düzeltildi). N1, N6, N7,
> `ZoneChanged` sahipliği ve stinger/ışık zamanlama boşluğu 2026-08-03'te
> çözüldü. 2026-08-04 verification turu bu dosyada yeni tutarsızlıklar
> buldu (AC7/AC6c çelişkisi, guard silme-koşulu yarışı, `radius`'un iki
> yerde unutulmuş kopyası, `AmbientZoneVolume`'un eş-varlık guard'ı
> eksikliği) — hepsi bu turda düzeltildi. Hâlâ açık, tasarım kararı
> gerektiren maddeler: HARD CUT'ın "startle" riski (bkz. o raporun
> Warnings bölümü) ve stinger caption'ının koşulsuz gösterimi.
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **Implements Pillar**: Pillar 2 (Sessiz Gerilim, Şok Değil), Pillar 1 (Öznel Gerçeklik)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (revised) 2026-08-02 — stinger altyazı örnek metinleri nesne-adlandırıyordu, Open Questions #2'ye tasarım sorusu olarak taşındı
> **Design Review (2026-08-03)**: NEEDS REVISION → aynı oturumda revize
> edildi (stinger'ın `HeldSessionAlreadyPlayed` ile kalıcı "bir kez
> duyulur" garantisi eklendi — sahne-yükleme re-fire bug'ı kapatıldı;
> RMS enforcement için statik brickwall limiter + alan-başına RMS tavanı
> eklendi; nefes ritmi stinger adayı kaldırıldı; eksik
> `stinger_falloff` formülü eklendi; nonexistent `design/registry/
> entities.yaml` referansı düzeltildi; ambiyans kaynaklarına mixer grubu
> ataması eklendi; üçüncü-bölge crossfade edge case'i tanımlandı; AC14
> BLOCKING/ADVISORY olarak ayrıldı, Blocked Acceptance Criteria tablosu
> eklendi; erişilebilirlik çerçevelemesi düzeltildi — izlenimci metin
> işitme engelli oyuncu için boşluğu kapatmıyor, ayrı açık soru olarak
> işaretlendi) → **APPROVED** (re-review yapılmadan, kullanıcı kararı) —
> bkz. `design/gdd/reviews/adaptif-ses-sistemi-review-log.md`

## Overview

Adaptif Ses Sistemi, otelin ambiyans katmanlarını ve anı-tetikleyici
geçişleriyle eşleştirilmiş sting/ton kaymalarını yönetir. Teknik olarak
Unity'nin dahili AudioMixer altyapısı üzerine kurulu bir katmanlı ses
sistemidir: sürekli çalan ambiyans döngüleri, tetikleme anında devreye
giren stinger'lar ve Işık/Volume Durum Sistemi'nin `OnShiftStateChanged`
event'ine abone olarak ışık geçişiyle senkronize çalışan bir ses kayması.

Oyuncuya yönelik etkisi doğrudandır — prototip bulgusuna göre, ışık/renk
geçişi tek başına yeterli değildi; rahatsız edicilik hissini tamamlayan
asıl parça budur (Pillar 2: Sessiz Gerilim). Otel her zaman sessiz
değildir — nefes alır, uğuldar, ve bir anı tetiklendiğinde o nefes
değişir.

## Player Fantasy

Tetikleyici stinger'ı yabancı bir korku sesi değildir — ilişkiden kalma
tanıdık bir sesin (bir nefes ritmi, bir kapı sesi, bir ton) bağlamından
koparılmış hali. Oyuncu onu *tanır* ama neden burada olduğunu bilmez.
Bölge Held'e geçtiği an, bu tanıdık-yersiz ses kısa ve net bir kez
duyulur — sonra sessizliğe döner.

Işık belirsiz bir "bir şey değişti" derken, ses spesifik ama yersiz bir
"bunu daha önce duydum" der (**Tanıdık Ama Yerinde Değil**, Pillar 1 ve 2'yi
birlikte hizmet eder). İkisi birleşince oyuncu hem şüphelenir hem tanır —
dedektiften çok, kendi hafızasına yakalanmış biri gibi hisseder. Işık ve
ses asla aynı bilgiyi iki kere vermez: biri belirsizliği, diğeri
tanıdıklığı taşır.

## Detailed Design

### Core Rules

- **Middleware kararı (game-concept.md açık sorusunu çözer)**: Unity
  dahili AudioMixer + AudioSource — **onaylandı**, FMOD/Wwise'a gerek
  yok. İhtiyaç setimiz (birkaç ambiyans katmanı, one-shot stinger,
  hız-ölçekli footstep) bu araçların asıl gücü olan adaptif/branching
  müzik ya da RTPC ağlarını hiç kullanmıyor; 2 kişilik ekip için
  lisans/entegrasyon maliyeti kazanımı geçersiz kılar.
- **Ambiyans**: Alan başına 2-3 katman, tamamen diegetik (bina
  fiziğinden kaynaklanan sesler, "korku ambiyansı" değil):
  - **Depo (-5)**: soğutma ünitesi kompresör uğultusu (düzensiz), metal
    raf gıcırtısı **(temel 2 katman)**, uzak su tesisatı **(round-bazlı
    3. katman, bkz. aşağıdaki "Round-bazlı gerilim birikimi")**
  - **Servis Koridoru**: floresan balast uğultusu, temizlik ekipmanı
    tekerlek sesi **(temel 2 katman)**, uzak pnömatik kapı kapanışı
    **(round-bazlı 3. katman)**
  - **Balo Salonu**: yüksek tavan reverb'i, derin HVAC bas gürültüsü
    **(temel 2 katman)**, kristal tıngırtısı **(round-bazlı 3. katman)**
  - **Round-bazlı gerilim birikimi (design-review, 2026-08-04 —
    verification design-theory bulgusu, eklendi, kritik bulgu)**:
    `game-concept.md`'nin "turlar ilerledikçe ortam gerilimi birikir"
    iddiasının önceden hiçbir sistemde karşılığı yoktu — Görev/Taşıma
    Döngüsü'nün kendi round-bazlı eğrisi (`Highlight`) prominence'ı
    **azaltıyordu**, gerilim biriktirmiyordu (bu doğru, Dikkatin Göçü'nün
    taşıyıcısı bu — bkz. o GDD'nin Player Fantasy'si), ama gecenin genel
    gerilim eğrisinin başka hiçbir taşıyıcısı yoktu. Düzeltme: her alanın
    zaten belirsiz bırakılmış "2-3 katman" ifadesi somutlaştırıldı — temel
    2 katman gece boyunca sabit çalar, **3. katman round ilerledikçe
    kademeli olarak devreye girer**. Görev/Taşıma Döngüsü'nün
    `CurrentRoundIndex`/`TotalRoundCount` sorgularını okuyarak (bkz. o
    GDD'nin Core Rules'ı — `Highlight` formülüyle aynı sayaçlar, ayrı bir
    event gerekmez, her ambiyans güncellemesinde doğrudan okunur),
    `TensionGain(roundIndex) = ease(roundIndex / (TotalRoundCount - 1))`
    (aynı proje-geneli smoothstep kuralı: `ease(x) = x²(3-2x)`) 3. katmanın
    volümünü 0'dan (round 1) tam seviyeye (son round) taşır. 3. katman
    seçimi kasıtlı: her alanın en "yerini bulamadığın", kaynağı en belirsiz
    sesi (uzak su tesisatı, uzak pnömatik kapı, kristal tıngırtısı) —
    gece ilerledikçe otelin fark edilirliği artan, ama asla açıklanmayan
    tuhaflığı. Bu, Işık/Volume'un `ShiftProgress` ve Adaptif Ses'in kendi
    `ambient_crossfade`'iyle aynı easing kuralını, aynı "whisper not alarm"
    felsefesiyle round eksenine taşır.
  - **Crossfade**: İki `AudioSource` (A/B ping-pong) arası ~1-2s
    volume-lerp, alan trigger-collider'ı `ZoneChanged` event'i
    fırlattığında tetiklenir. Snapshot kullanılmaz — sadece bir float
    lerp, hata ayıklaması kolay, ilk kez yapan bir ekip için tek
    script'te okunabilir. **Mixer grubu ataması (design-review,
    2026-08-03 — unity-specialist bulgusu, eklendi)**: Bu iki kaynak
    (A/B) bir **"Ambiance" mixer grubuna** atanır — önceki taslakta
    sadece Stinger grubu adlandırılmıştı, Ambiance hiç bus-route
    edilmemişti. Bu, stinger'ın "tını kontrastı" miksaj felsefesinin
    ön koşuludur: kontrast, iki ayrı bus'ın ayrı işlenebilmesine (ör.
    stinger grubuna limiter eklenirken ambiance grubuna eklenmemesi)
    dayanır — ambiance Master'a doğrudan bağlıysa bu ayrım hiç mümkün
    olmaz.
  - **`ZoneChanged` sahipliği (design-review, 2026-08-03 — orijinal
    `/review-all-gdds` raporunun hiç ele alınmamış blocker'ı, şimdi
    çözüldü)**: Önceki taslak "alan trigger-collider'ı" diyordu ama
    hiçbir sistemde bu collider'ların gerçekten var olduğu, kim
    tarafından yerleştirildiği ya da event'i kimin fırlattığı
    tanımlanmamıştı — Adaptif Ses'in tüm ambiyans katmanının hiçbir
    tetikleyicisi yoktu. Düzeltme: **bu sistem kendi `AmbientZoneVolume`
    bileşenini tanımlar ve sahipliğini üstlenir** — üç adlandırılmış
    bölgenin (Depo, Servis Koridoru, Balo Salonu) her biri için sahneye
    bir tane yerleştirilen, basit bir Unity `Collider(isTrigger=true)`.
    FPC'nin karakter collider'ı girdiğinde `ZoneChanged(zoneId)` fırlatır
    — başka hiçbir sistemin işbirliğine ihtiyaç duymaz (Işık/Volume'un
    R_trigger/histerezis mekanizmasından tamamen ayrı ve daha basit;
    burada histerezise gerek yok çünkü sınırda hızlı ileri-geri gidiş
    zaten yukarıdaki "devam eden crossfade'i mevcut gain'den yeniden
    başlatma" kuralıyla zarifçe ele alınıyor). **Bitişik bölge
    volume'ları sıfır-boşluklu paylaşılan bir sınırda birleşir**
    (üst üste binme yok, boşluk yok) — tam olarak bir bölge her zaman
    aktiftir **sahnenin kendisi tek başınayken**. **Sahne eş-varlığı
    guard'ı (design-review, 2026-08-04 — verification bulgusu, eklendi,
    kritik bulgu)**: Seviye/Sahne Geçişi'nin SOFT geçişleri, 0.5-2s'lik
    bir pencerede iki sahnenin additive olarak eş-zamanlı resident
    kalmasına yol açar (bkz. `seviye-sahne-gecisi.md` Core Rules) — bu
    pencerede köken ve hedef sahnenin `AmbientZoneVolume`'ları aynı
    world-space'i paylaşabilir, "tam olarak bir bölge aktif" değişmezini
    kırar ve `Start()`'taki başlangıç-bölgesi kontrolünün oyuncu hâlâ
    köken sahnenin kabininde sayılırken erken tetiklenmesine yol açabilir.
    Işık/Volume Durum Sistemi bu **aynı sınıf** eş-varlık hatasını kendi
    bölgeleri için zaten çözmüştü ama `AmbientZoneVolume` (bu sistemde,
    ayrı bir dokümanda, daha sonra tanımlandı) o düzeltmeyi hiç miras
    almadı. **Düzeltme**: aynı kural buraya da taşınır — bir
    `AmbientZoneVolume`'un ticker'ı/tetikleyicisi, kendi sahnesi
    `SceneManager.GetActiveScene()` ile eşleşmediği sürece işlenmez
    (pas geçilir, yok edilmez). **Başlangıç bölgesi**: Unity'nin `OnTriggerEnter`'ı,
    oyuncu bir collider'ın **içinde spawn olduğunda** fırlamaz (sadece
    dışarıdan girişte) — bu yüzden her `AmbientZoneVolume`, kendi
    `Start()`'ında oyuncunun o an içinde olup olmadığını bir kerelik
    `Physics.OverlapSphere`/`bounds.Contains` kontrolüyle sorgular ve
    öyleyse `ZoneChanged`'i manuel fırlatır — Edge Cases'teki "ambiyans
    yeni sahnenin varsayılan bölgesinde yeniden başlar" ifadesinin
    somut mekanizması budur, önceki taslakta "varsayılan bölge" nasıl
    belirlendiği hiç tanımlanmamıştı.
    **Başlangıç-bölgesi kontrolü ile eş-varlık guard'ı çakışması
    (design-review, 2026-08-04 — full re-verification bulgusu, düzeltildi,
    kritik bulgu)**: Yukarıdaki iki kural birbirini iptal ediyordu.
    Seviye/Sahne Geçişi, `Preloading→Ready` geçişinin her zaman hedef
    sahnenin `LoadSceneAsync`'inin (dolayısıyla tüm `Awake`/`Start`
    maliyetlerinin) tam tamamlanmasını beklediğini garanti eder (bkz.
    `seviye-sahne-gecisi.md` Core Rules, "Preload tam tamamlanmalı") —
    yani hedef sahnenin `AmbientZoneVolume.Start()`'ı, `SetActiveScene`
    henüz çağrılmadan, köken sahne hâlâ aktifken çalışır. Bu tam da
    eş-varlık guard'ının bastırdığı an — `Start()`'taki tek seferlik
    overlap kontrolü hiç çalışmadan bastırılır ve bir daha asla tekrar
    denenmez (tek seferlik olduğu için). Sonuç: her asansör yolculuğundan
    ve her HARD CUT'tan sonra ambiyans sessizce hiç başlamaz. Işık/Volume'un
    aynı sınıf eş-varlık düzeltmesi bu soruna düşmez çünkü onun kontrolleri
    her karede tekrar eden bir ticker'a aittir (bir kare bastırılsa bile
    sahne aktif olduğunda bir sonraki kare çalışır); `AmbientZoneVolume`'un
    kontrolü ise tek seferlidir, "bu kareyi atla" semantiği burada geçerli
    değildir. **Düzeltme**: başlangıç-bölgesi overlap kontrolü `Start()`'ta
    bir kez denenmek yerine, kendi sahnesi `SceneManager.GetActiveScene()`
    ile ilk eşleştiği karede (henüz çalışmamışsa) çalışacak şekilde ertelenir
    — pratikte bir `_initialCheckDone` bool bayrağı ile, ticker'ın zaten her
    karede çalıştırdığı aynı `GetActiveScene()` karşılaştırmasının içine
    gömülü ("henüz kontrol edilmedi VE şimdi aktif" → overlap kontrolünü
    çalıştır, bayrağı `true` yap, sonra normal ticker mantığına devam et).
    Bu, ek bir event aboneliği ya da `Update` dışı bir mekanizma gerektirmez
    — sadece mevcut ticker'ın ilk "artık aktifim" karesini yakalar.
- **Anı-Tetikleyici Stinger**: Işık/Volume'un `OnShiftStateChanged`'ine
  abone. **Tetikleme zamanlaması (design-review, 2026-08-03 — orijinal
  `/review-all-gdds` raporunun hiç ele alınmamış blocker'ı, şimdi
  çözüldü)**: Önceki taslak `if (newState != Held) return` diyordu —
  yani stinger, ışığın kendi ~3s'lik `Shifting-In` rampası **tamamlandıktan
  sonra** çalıyordu, ışık geçişi zaten görsel olarak tamamlanmışken. Bu,
  üç dokümanın (`systems-index.md`, `isik-volume-durum-sistemi.md`, bu
  doküman) "ışık+ses bileşik etki" iddiasıyla doğrudan çelişiyordu —
  prototip bulgusu ışık+sesin **eşzamanlı** olması gerektiğini
  söylüyordu, 2-5 saniye arayla değil. Düzeltme, `PersistentShiftIds`
  zamanlama boşluğunu kapatan aynı akıl yürütmeyi kullanır (bkz.
  `gece-oturum-durumu-2026-08-02.md` Core Rules): **`newState == Held`
  VE `IsShiftPersistent(shiftId) == true` bir çalma-denemesi tetikler**
  (design-review, 2026-08-04 — ikinci tur full re-verification bulgusuyla
  `IsShiftPersistent` koşulu eklendi, kritik bulgu — bkz. aşağıdaki not),
  VE ek olarak `newState == Shifting-In` VE `IsShiftPersistent(shiftId) ==
  true` de artık bir çalma-denemesi tetikler** — ışığın kendi rampasıyla
  aynı karede. Bu güvenlidir çünkü Persistent bir shift, Işık/Volume'un kendi
  garantisi gereği her zaman `Held`'e ulaşır, asla geri dönmez (bkz.
  Işık/Volume Core Rules) — erken çalma hiçbir zaman "sonunda hiç
  gerçekleşmeyecek bir olay için" olmaz. `MemoryTriggerDef`'e bağlı her
  shift zaten zorunlu olarak `Persistent=true` olduğundan (bkz.
  `ani-tetikleyici-etkilesim.md` Core Rules), bu erken yol anı-tetikleyici
  stinger'ının **her zaman** aldığı yoldur.
  **`IsShiftPersistent` koşulu `Held` dalına da eklendi (design-review,
  2026-08-04 — ikinci tur full re-verification bulgusu, en kritik bulgu,
  kullanıcı kararıyla çözüldü)**: Önceki taslak `Held`'i **koşulsuz**
  bir çalma-denemesi sayıyordu — hangi kaynaktan geldiğine bakmaksızın.
  Bu, `isik-volume-durum-sistemi.md`'ye eklenen zorunlu `Automatic`
  ambient bölge (bkz. o dosyanın Core Rules'ı, "MVP içerik gereksinimi")
  ile çakıştı: o bölge kasıtlı olarak `Persistent=false` (reversible,
  hiçbir ipucu taşımayan, "daha zayıf bir sinyal" olması gereken pasif
  bir kayma) — ama koşulsuz `Held` kuralı altında, o bölge her `Held`'e
  ulaştığında **aynı** "tanıdık ama yersiz" anı-tetikleyici stinger'ını
  çalardı; üstelik `Persistent=false` olduğundan `HeldSessionAlreadyPlayed`
  her `Shifting-Out`'ta temizlenir, yani bölge oturum boyunca (zorunlu
  taşıma rotası üzerinde, tekrar tekrar geçilen) her yeniden-tetiklenmede
  stinger'ı **tekrar tekrar** çalardı — projenin özenle koruduğu, tek ve
  anlamlı "ilişkiden kalma bir ses" imasını değersizleştirip sıradan bir
  ambiyans gürültüsüne indirgerdi. Düzeltme: stinger mekanizmasının
  tamamı artık `IsShiftPersistent(shiftId)==true`'ya bağlı — bu,
  `MemoryTriggerDef`-bağlı shift'ler için davranışı değiştirmez (zaten
  her zaman `Persistent=true`), ama `Automatic`/reversible ambient
  bölgeler için stinger'ı yapısal olarak devre dışı bırakır. Yeni bir
  alan/flag icat edilmedi — proje zaten `Persistent` bayrağını bu ayrımı
  yapmak için kullanılabilir bir sinyal olarak taşıyordu.
  Aynı aktivasyonun ~3s sonra gelen `Held` çalma-denemesi (VE Işık/Volume'un reload-restore
  Edge Case'inin fırlattığı `Held` re-fire'ı) `HeldSessionAlreadyPlayed`
  guard'ı tarafından no-op'a düşürülür (aşağıya bkz.) — iki tetikleyici
  yolu aynı guard'ı paylaşır, çift çalma riski yoktur. Reversible
  (Persistent olmayan) shift'ler için hiçbir şey değişmedi: sadece
  `Held` bir çalma-denemesidir, `Shifting-In` hiç işlenmez (`Held`'e hiç
  ulaşmadan geri dönebilecekleri için erken çalmak sahte-pozitif bir
  stinger üretirdi). Küçük bir pool'dan
  (3-4 `AudioSource`, Stinger mixer grubuna atanmış) bir kaynak alınır,
  `zoneCenter`'a konumlandırılır, `spatialBlend=1`,
  `minDistance = stingerAudioRadius × 0.3`, `maxDistance =
  stingerAudioRadius × 1.0` (design-review, 2026-08-04 — verification
  bulgusu, `radius`'tan düzeltildi: bu satır N1 düzeltmesi sırasında
  atlanmıştı, Formulas bölümü doğruydu ama bu Core Rules kopyası
  eskisini koruyordu; bkz. Formulas — bu proje kuralı gereği bir formül
  olarak adlandırıldı, önceki taslakta hiç tanımlanmamıştı), `PlayOneShot` ile bir kez çalar
  (`PlayClipAtPoint` kullanılmaz — mixer grubu ataması desteklemez).
  **`Playing`/`Cooldown` gate'i açıkça bir guard'dır (design-review,
  2026-08-03 — unity-specialist bulgusu, netleştirildi)**: `PlayOneShot`'ın
  kendisi motor seviyesinde "meşgul" kavramı taşımaz — bir kaynağın
  havuzdan alınabilmesi için önce bu sistemin kendi `Idle`/`Playing`/
  `Cooldown` durum takibinin `Idle` döndürmesi **zorunludur**, `source.isPlaying`'e
  güvenilmez (`PlayOneShot` bunu doğru yansıtmaz). `Cooldown`'a giriş,
  `PlayOneShot` çağrısıyla eş zamanlı `Invoke(EnterCooldown, clip.length)`
  (ya da eşdeğeri) ile zamanlanır — klip bitişi motor tarafından ayrıca
  sinyallenmez, bu sistem kendi zamanlayıcısını tutar.
  **Miksaj felsefesi**: Stinger asla ambiyansın üzerine RMS olarak
  sıçramaz — tını kontrastından (organik/insan sesi vs. mekanik ambiyans
  katmanları) etkisini alır, 1-1.5sn, build-up/riser/crescendo yok.
  Duyulur duyulmaz ambiyansa geri döner.
  **Runtime enforcement (design-review, 2026-08-03 — game-designer +
  audio-director bulgusu, eklendi)**: "Asla RMS'i aşmaz" kuralı önceden
  sadece içerik-yazım disiplinine dayanıyordu, hiçbir runtime savunması
  yoktu — 2 kişilik bir ekipte, zamanla üretilen çok sayıda stinger
  asset'i için gerçek bir drift riski (game-designer), ve tek, alan
  başına aynı RMS hedefi zaten fiziksel olarak riskli (audio-director:
  Balo Salonu'nun derin HVAC bas gürültüsü, düşük-bas tepkisi zayıf
  hoparlör/soundbar sistemlerinde stinger'ı maskeleyebilir/kırpabilir).
  Düzeltme: Stinger mixer grubuna **statik bir brickwall limiter**
  eklenir (dinamik bir duck-envelope DEĞİL — "build-up/riser/crescendo
  yok" ilkesini ihlal etmez, tek seferlik bir gain-staging güvenlik ağı).
  Ayrıca RMS hedefi **alan başına ayrı kalibre edilir** (tek, proje
  geneli bir sayı değil) — bkz. Tuning Knobs, yeni "Stinger RMS tavanı
  (alan başına)" satırı.
  - **İçerik yönü**: "Tanıdık ama yersiz" sesler — tanıdık ama otelin kendi
    kapılarından farklı bir kapı sesi, artık kullanılmayan bir telefon
    bildirim tonu. **Nefes ritmi adayı kaldırıldı (design-review,
    2026-08-03 — audio-director bulgusu, creative-director kararı)**:
    "spesifik, sayılabilir bir panik-atak paterni" neredeyse doğrudan
    korku-skorlama dilbilgisini ("ağır nefes = yakın tehdit") ödünç
    alıyordu — bu, oyunun kendi Pillar 2'siyle (Sessiz Gerilim, Şok
    Değil) doğrudan çelişir. Kapı sesi ve telefon tonu adayları güvenli
    kalır (cansız nesneler, somatik tehdit sinyali değil). Gerçek asset
    seçimi Visual/Audio Requirements'ta detaylandırılacak.
- **Footstep**: FPC'nin stride-phase accumulator'ı her adımda bu
  sistemin `PlayFootstep(float speed)`'ini çağırır. Tek dedike
  `AudioSource`, `PlayOneShot` (üst üste binmeye izin verir, önceki
  adımı kesmez), 4-6 örnek arasından tekrar-korumalı seçim (son çalan
  index hariç tutulur), ±%5 pitch, volume=speed/1.6.
- **HARD CUT Sting (design-review, 2026-08-03 — `/review-all-gdds`
  bulgusu, üç bağımsız uzman/geçiş tarafından ayrı ayrı bulundu, eklendi;
  filtre koşulu 2026-08-03 verification N6 bulgusuyla eklendi; ikinci
  filtre 2026-08-04 full re-verification bulgusuyla eklendi, kullanıcı
  kararıyla çözüldü)**:
  Seviye/Sahne Geçişi'nin `OnTransitionStateChanged(newState, type)`'ine
  abone olunur, **sadece `newState == Swapping && type == TransitionType.Hard
  && GetCurrentHardCutAbrupt() == true`** iken tetiklenir. **İkinci koşul
  neden eklendi**: Sahne Kesmeli Anlatı'nın iki bitiş koşulu artık farklı
  bir ton talep ediyor (bkz. `sahne-kesmeli-anlati-2026-08-02.md` Core
  Rules, "İki bitişin farklı tonu") — görev-tamamlama bitişi
  `Abrupt=false` gönderir, çünkü o bitişin kendi Player Fantasy'si "sakin
  bir teslim anı" ister, "dünya seni durdurur" değil; önceki taslakta
  CutSting her HARD CUT'ta (ikisinde de) koşulsuz çalıyordu, bu da iki
  bitişin belgelenen duygusal farkını sıfırlıyordu (bkz.
  `gdd-cross-review-2026-08-04-verification.md`). `Abrupt=false`
  durumunda CutSting hiç çalmaz — bkz. aşağıdaki "Abrupt=false" notu ve
  Edge Cases'teki anlık-susturma kuralının güncellenen hâli. Bu mekanizma
  (ilk `type` filtresi hariç), sıfır-kare aktif-sahne değişiminin
  `Abrupt=true` olduğunda **tek** ses karşılığı olmaya devam eder — bu
  sistemin önceki taslağında hiç yoktu (o GDD'nin kendi Visual/Audio
  Requirements'ı bu abonelikten bahsediyordu, ama bu doküman hiç
  uygulamıyordu). **`type` filtresi olmadan** bu sting sıradan bir
  Asansör/seviye SOFT geçişinde de çalardı (SOFT ve HARD CUT aynı paylaşılan
  durum makinesini kullandığı için `Swapping` ikisinde de fırlar) — bu,
  Pillar 2'yi (Sessiz Gerilim, Şok Değil) ihlal eden istenmeyen bir
  jump-scare'e denk gelirdi; `type == Hard` kontrolü bunu yapısal olarak
  imkansız kılar. Kendi ayrı **"CutSting" mixer grubuna** atanmış,
  havuzdan bağımsız (stinger pool'unu paylaşmaz — kavramsal olarak
  farklı bir olay, aynı anda bir memory-trigger stinger'ı da çalıyor
  olabilir), tek bir `AudioSource`'tan `PlayOneShot` ile çalar,
  `Swapping` event'i geldiği karede (zero-frame swap ile senkron)
  tetiklenir. **Miksaj felsefesi, stinger'la aynı**: Stinger'ın kendi
  Runtime enforcement kuralı (statik brickwall limiter + tavan) buraya
  da uygulanır — build-up/riser/crescendo yok, 1-1.5sn, tını
  kontrastından etkisini alır. Bu, HARD CUT'ın "çalınma, hata değil"
  okumasının Seviye/Sahne Geçişi'nin kendi Open Questions'ında adlandırdığı
  **tek** taşıyıcısıdır — bu sistem bu sorumluluğu artık açıkça
  üstleniyor, üstlenmediği önceki taslak bir tasarım-teorisi
  review'ünde (üç bağımsız geçişin de aynı sonuca vardığı) en ciddi
  bulguydu.

- **`Abrupt=false` ("sakin" HARD CUT — design-review, 2026-08-04 — full
  re-verification bulgusu, kullanıcı kararıyla çözüldü)**: Görev-tamamlama
  bitişi bu tonu talep eder. CutSting hiç çalmaz (yukarıda). Aşağıdaki
  "anlık susturma kuralı" (Edge Cases) bu durumda **uygulanmaz** —
  bunun yerine mevcut ambiyans, zaten var olan `ambient_crossfade`
  formülüyle (aynı `x²(3-2x)` eğrisi, aynı `T` tuning knob'u, bkz.
  Formulas) sessizliğe kayar; herhangi bir çalmakta olan memory-trigger
  stinger'ı da aynı şekilde bu crossfade'e katılır (yeni bir mekanizma
  icat edilmez, mevcut zone-crossfade altyapısı hedefi "sessizlik" olacak
  şekilde yeniden kullanılır). Bu, "sakin bir teslim anı" fantazisinin
  ses karşılığıdır — hiçbir şey kesilmez, sadece söner. Stinger pool
  elemanları crossfade tamamlandığında `Idle`'a döner (aynı son durum,
  farklı yol). `HeldSessionAlreadyPlayed`'den etkilenmez (kalıcı kalır,
  `Abrupt=true` durumundaki gibi).

- **Ambiyans**: Global durum yok — 3 katman sürekli çalar; alan
  geçişinde `Idle(AlanX)` → `Crossfading` (~1-2s) → `Idle(AlanY)`.
- **Stinger (shiftId başına)**: `Idle` → (Held event) → `Playing` →
  (klip biter) → `Cooldown` (~1s, çift-tetikleme edge case'ini yutar) →
  `Idle`. Bu geçici durum (pool kaynağı meşgul mü) sahne-lokal
  kalabilir — asıl kalıcılık ihtiyacı aşağıdaki ayrı mekanizmadır.
  **Kalıcı "bu Held oturumunda zaten çaldı" takibi eklendi (design-review,
  2026-08-03 — systems-designer bulgusu, düzeltildi — kritik bulgu)**:
  Önceki taslak ~1s Cooldown'un "sahne-yükleme edge case'ini de
  yuttuğunu" iddia ediyordu, ama bu yanlıştı: Cooldown ~1s içinde zaten
  `Idle`'a döner, oysa gerçek bir sahne yüklemesi (ör. asansör yolculuğu)
  çok daha uzun sürer — reload anında stinger neredeyse her zaman zaten
  `Idle`'dadır, Cooldown onu hiç "yutmaz." Işık/Volume'un kendi reload
  Edge Case'i (`isik-volume-durum-sistemi.md`), bir `Persistent=true`
  shift zaten Held-Persistent iken sahne yeniden yüklendiğinde
  `OnShiftStateChanged(Held)`'i **kasıtlı olarak** bir kez daha fırlatır
  — açıkça bu sistemin senkronize olabilmesi için (bkz. o GDD'nin "Event
  genişlemesi" notu). Bu re-fire, `Idle`'ı bulur ve stinger'ı **yeniden**
  çalardı — "tanıdık ama yersiz" sesin sadece bir kez duyulması gereken
  Player Fantasy garantisini kırardı (ör. oyuncu bir anıyı tetikler,
  asansöre biner, aynı kata döner — stinger ikinci kez çalar). Düzeltme,
  geçici Idle/Playing/Cooldown durumunu değiştirmez, ona **ek** bir
  kalıcı bariyer ekler: `HashSet<string> HeldSessionAlreadyPlayed`, Gece/
  Oturum Durumu'nun `FiredTriggerIds`'i ve Anlatı Durum'un
  `SeenShiftIds`'iyle aynı desende, sahne-lokal DEĞİL oturum boyunca
  hayatta kalan statik/singleton bir alan. `shiftId`, `Playing`'e
  girerken bu kümeye eklenir; **`newState == Shifting-Out || newState ==
  Dormant`** gözlemlendiğinde kümeden çıkarılır (design-review,
  2026-08-04 — verification bulgusu, netleştirildi: önceki taslak bu
  koşulu `newState != Held` diye yazıyordu — bu, `Shifting-In`'i de
  kapsayan geniş bir koşuldu, ve `Shifting-In` artık AC6c'nin çalma-denemesi
  tetikleyicisi olduğundan, aynı event teorik olarak hem guard'a ekleme
  hem çıkarma koşulunu aynı anda sağlayabilirdi — sıralamaya bağlı bir
  hataydı. Çıkarma koşulu artık sadece gerçek geri-dönüş durumlarını
  hedefliyor). **Her iki çalma-denemesi yolu da
  (design-review, 2026-08-03 — stinger/ışık zamanlama düzeltmesiyle
  güncellendi) aynı guard'ı paylaşır**: bir `Shifting-In`+Persistent
  erken-çalma denemesi ya da bir `Held` çalma-denemesi işlenir işlenmez,
  önce bu küme kontrol edilir — zaten içindeyse (aynı aktivasyonun
  birkaç saniye sonra gelen `Held` çalma-denemesi dahil, VE reload-tetikli
  `Held` re-fire'ı dahil) tamamen no-op'tur, pool'dan kaynak bile
  alınmaz. `Persistent=true` shift'ler için bu küme girişi asla
  temizlenmez (o shiftId hiçbir zaman Held'den çıkmadığından) — bu, tam
  olarak "bir kez duyulur, sonra asla değil" garantisini üretir; artık
  bu "bir kez" ışığın rampasının **başında** duyulur, sonunda değil.
  Normal (Persistent olmayan) shift'ler için ise oyuncu bölgeden çıkıp
  tekrar girdiğinde küme girişi zaten temizlenmiş olur, stinger her yeni
  Held-girişinde meşru şekilde tekrar çalar — bu davranış değişmedi
  (bu shift'ler hiçbir zaman `Shifting-In` yolundan geçmez).

### Interactions with Other Systems

- **Işık/Volume Durum Sistemi**: `OnShiftStateChanged`'e abone olunur —
  **`Held` VE `Shifting-In`, ikisi de sadece `IsShiftPersistent(shiftId)==true`
  ise işlenir** (design-review, 2026-08-03 — stinger/ışık zamanlama
  düzeltmesiyle `Shifting-In` eklendi; `Held` dalına `IsShiftPersistent`
  koşulu 2026-08-04 ikinci tur full re-verification bulgusuyla eklendi —
  bkz. Core Rules "Anı-Tetikleyici Stinger" — önceki taslak `Held`'i
  koşulsuz işliyordu, bu da zorunlu `Automatic` ambient bölgenin
  stinger'ı yanlışlıkla tekrar tekrar tetiklemesine yol açardı), `zoneCenter`
  ikinci bir sorgu yapılmadan doğrudan kullanılır; `IsShiftPersistent(shiftId)`
  sorgusu çağrılır (hem stinger'ın erken mi geç mi çalacağına karar
  vermek için hem de artık çalıp çalmayacağına karar vermek için,
  design-review 2026-08-03 eklendi, kapsamı 2026-08-04'te genişletildi);
  **stinger ses-düşüşü için artık
  `radius` değil, `GetStingerAudioRadius(shiftId)` sorgusu çağrılır**
  (design-review, 2026-08-03 — verification N1 bulgusu, eklendi — bkz. Formulas
  "stinger_falloff" ve `isik-volume-durum-sistemi.md`'nin kendi
  `GetStingerAudioRadius` tanımı).
- **Seviye/Sahne Geçişi (design-review, 2026-08-03 — eklendi; imza
  2026-08-03 verification N6 bulgusuyla güncellendi)**:
  `OnTransitionStateChanged(newState, type)`'e abone olunur, sadece
  `newState == Swapping && type == TransitionType.Hard` işlenir (HARD CUT
  sting'i tetiklemek için — sıradan SOFT geçişler, `type == Soft` olduğu
  için filtrelenip atlanır). Bu sistem Seviye/Sahne Geçişi'ne hiç geri
  çağrı yapmaz, sadece dinler.
- **Birinci Şahıs Kontrolcü**: Bu sistem FPC'nin `Velocity`/
  `IsGrounded`'ına abone OLMAZ — FPC kendi stride-phase
  accumulator'ından `PlayFootstep(float speed)` çağırır (zamanlama
  FPC'de kalır, çünkü head-bob'la aynı faz kaynağını paylaşıyor; iki
  bağımsız faz türetimi senkron kaybına yol açardı). Ses sistemi sadece
  asset seçimi/pitch/volume mix'ini üstlenir.

## Formulas

The `ambient_crossfade` formula is defined as:
`ambient_crossfade = ease(x) applied to a single Lerp parameter driving volume_A/volume_B`
`x = clamp(elapsed / T, 0, 1); ease(x) = x²(3-2x); volume_B = ease(x); volume_A = 1 - ease(x)`

**Variables:**

| Variable | Meaning |
|---|---|
| `elapsed` | seconds since `ZoneChanged` fired |
| `T` | crossfade duration, ~1-2s (Tuning Knob) |
| `x` | normalized progress, 0-1 |
| `ease(x)` | smoothstep of `x` |
| `volume_A` / `volume_B` | outgoing / incoming source gain |

**Output Range:** 0 to 1 for each source; `volume_A + volume_B = 1` throughout. At `x=0`, A=1/B=0 (old zone alone); at `x=1`, A=0/B=1 (new zone alone); both endpoints have zero slope.
**Example:** T=1.5s. At elapsed=0.75s, x=0.5, ease=0.5 → both sources at 0.5 gain (midpoint, equal blend). At elapsed=0.375s, x=0.25, ease=0.15625 → volume_A=0.844, volume_B=0.156 (old zone still dominant).

---

The `tension_gain` formula is defined as (design-review, 2026-08-04 — verification design-theory bulgusu, eklendi):
`TensionGain(roundIndex) = ease(x), where x = clamp(roundIndex / (TotalRoundCount - 1), 0, 1); ease(x) = x²(3-2x)`

**Variables:**

| Variable | Meaning |
|---|---|
| `roundIndex` | Görev/Taşıma Döngüsü'nün `CurrentRoundIndex`'i (0-tabanlı) |
| `TotalRoundCount` | Görev/Taşıma Döngüsü'nün `TotalRoundCount`'u |
| `x` | normalize round ilerlemesi, 0-1 |
| `ease(x)` | smoothstep — `shift_progress`/`ambient_crossfade` ile aynı eğri |

**Output Range:** 0 (round 1) ile 1 (son round) arası, her alanın round-bazlı 3. ambiyans katmanının volümüne doğrudan çarpan olarak uygulanır (`layer3_volume = base_volume × TensionGain(roundIndex)`). Sıfır eğim her iki uçta da — round 1'de 3. katman fark edilmez şekilde sıfırdan başlar, son roundda tam seviyeye "whisper"la ulaşır, ani bir sıçrama olmaz.
**Example:** 4 round'luk bir gece (`TotalRoundCount=4`). Round 1 (`roundIndex=0`): x=0, TensionGain=0 (3. katman sessiz). Round 3 (`roundIndex=2`): x=2/3=0.667, ease=**0.741** (3. katman ~%74 seviyede — design-review, 2026-08-04 full re-verification: önceki değer 0.630 aritmetik hataydı, `x²(3-2x)` doğru hesabı `0.667²×(3-1.334) = 0.4449×1.666 = 0.741`'dir). Round 4/son (`roundIndex=3`): x=1, TensionGain=1 (3. katman tam seviyede).
**Guard rail (design-review, 2026-08-04 — full re-verification bulgusu, eklendi, kritik bulgu)**: `TotalRoundCount` payda için `(TotalRoundCount - 1)` kullanılır, `TotalRoundCount=1` durumunda sıfıra bölme oluşturur — projenin diğer tüm formüllerinin (`isik-volume-durum-sistemi.md`'nin `TIME_EPSILON`/`RADIUS_EPSILON` deseni) aksine bu formülde hiç guard yoktu. Düzeltme: `TotalRoundCount ≤ 1` ise `TensionGain` sabit `1`'e sabitlenir kod içinde (payda hesaplanmadan önce kontrol edilir) — tek round'luk bir gecede "3-5 round'luk bir gecenin tamamlanmışlığı" en yakın anlamlı yorum tam seviyedir, sıfır değil. **Aynı guard `gorev-tasima-dongusu.md`'nin `Highlight(round)` formülüne de uygulanmalı** (aynı `roundCount-1` paydası, aynı risk) — bkz. o dosyanın Visual Requirements bölümü. Bu senaryonun MVP içeriğinde fiilen ulaşılabilir olup olmadığı (`gorev-tasima-dongusu.md` AC1, 3-5 round'u build-time zorunlu kılıyor) ayrı bir soru — guard, ulaşılabilir olsun ya da olmasın, kod-seviyesinde degenerate girdiye karşı savunma olarak eklenir, projenin kendi "hand-waving yok" ilkesiyle tutarlı.

**Curve choice — eased, not linear:** Smoothstep is used instead of a raw linear lerp. A linear fade has nonzero velocity at `elapsed=0` and `elapsed=T`, so the moment `ZoneChanged` fires there's an audible "snap" onset — wrong for a grounded room-tone that should shift *unnoticed*. Smoothstep's zero-derivative endpoints remove that snap while staying a single float feeding one `Lerp` call — same implementation simplicity the Core Rules call for ("sadece bir float lerp"), no snapshot system needed. This also matches the `shift_progress` formula already registered for the Light/Volume system (same `x²(3-2x)` curve, same "whisper not alarm" rationale) — one easing convention across systems.

---

The `footstep_volume` formula is defined as:
`footstep_volume = speed / walk_speed_unloaded`

**Variables:**

| Variable | Meaning |
|---|---|
| `speed` | live speed (m/s) passed into `PlayFootstep(float speed)` by the FPC's stride-phase accumulator — **contractually equal to `birinci-sahis-kontrolcu.md`'s Formula 1 output `v(t)`** (design-review, 2026-08-03 — systems-designer bulgusu, netleştirildi: bu eşitlik önceden ima ediliyordu, hiçbir dokümanda açıkça sözleşme olarak belirtilmemişti) |
| `walk_speed_unloaded` | 1.6 m/s — kaynak: `birinci-sahis-kontrolcu.md` Core Rules ("Yürüme hızı: 1.6 m/s (yüksüz)") (design-review, 2026-08-03 — systems-designer bulgusu, düzeltildi: önceki "design/registry/entities.yaml" referansı projede hiç var olmayan bir dosyaya işaret ediyordu) |

**Output Range:** 0 to 1. At `speed=0`, volume=0 (no step would trigger anyway — stride accumulator stalls). At `speed=1.6` (max unloaded walk), volume=1. This never clips above 1 without a clamp because the claim is provably true, not just empirically observed (design-review, 2026-08-03 — systems-designer bulgusu, doğrulandı): `birinci-sahis-kontrolcu.md`'s Formula 1 (exponential smoothing) is a convex combination of `v(t)` and `v_target` for any `Δt ≥ 0`, so it cannot overshoot `v_target`; and `v_target = 1.6 × CarryMult × TaperMult` is bounded ≤1.6 by construction since both multipliers are ≤1.0. No race condition is possible because this system never independently samples `Velocity` — it only receives whatever `speed` the FPC hands it inline (bkz. Interactions with Other Systems).
**Example:** Walking unloaded at 1.6 m/s → volume=1.0 (full). Carrying an item at `walk_speed_carrying`=1.35 m/s → volume=1.35/1.6=0.84375 — footsteps read ~16% quieter/softer under load automatically, because the formula is **speed-relative, not state-relative**: the audio system never branches on carry state, it only reads whatever `speed` the FPC hands it. This keeps the ownership boundary clean (FPC owns locomotion state, audio only maps a scalar).

---

The `stinger_falloff` formula is defined as (design-review, 2026-08-03 — unity-specialist bulgusu, eklendi; önceki taslakta bu math hiç tanımlanmamıştı, sadece "radius'tan türetilir" deniyordu; girdi 2026-08-03 verification N1 bulgusuyla `radius`'tan `stingerAudioRadius`'a değiştirildi):
`minDistance = stingerAudioRadius × 0.3; maxDistance = stingerAudioRadius × 1.0`

**Variables:**

| Variable | Meaning |
|---|---|
| `stingerAudioRadius` | Işık/Volume'un `GetStingerAudioRadius(shiftId)` sorgusundan gelen, içerik-yazarının kasıtlı olarak belirlediği ses-düşüş yarıçapı (design-review, 2026-08-03 — verification N1 bulgusuyla eklendi; **önceki taslakta `OnShiftStateChanged`'in `radius` parametresi kullanılıyordu** — bir gameplay/hysteresis değeri, `TriggerMode=ManualOnly` + `Persistent` memory-trigger bölgelerinde artık kasıtlı olarak "kullanılmaz" ilan edilmiş durumda, bkz. `isik-volume-durum-sistemi.md` Core Rules — sesi ölü bir gameplay alanından türetmek yerine ayrı, amaca özel bir alan kullanılır) |
| `minDistance` | Unity `AudioSource.minDistance` — bu mesafenin içinde ses tam seviyede kalır |
| `maxDistance` | Unity `AudioSource.maxDistance` — bu mesafenin ötesinde ses duyulmaz hale gelir |

**Output Range:** `minDistance` her zaman `maxDistance`'ın %30'u — küçük bir bölgede (dar bir dolap tetikleyicisi) ses hızla düşer, büyük bir bölgede (balo salonu) daha geniş bir alanda duyulur. Bu oransal ilişki, Işık/Volume'un kendi Hysteresis Radius formülüyle aynı "büyüklükle orantılı tampon" mantığını izler — ama artık ondan bağımsız, ayrı bir değerle.

**Sorgu zamanlaması** (design-review, 2026-08-04 — verification bulgusu, düzeltildi: bu satır hâlâ `Held` diyordu, ama stinger/ışık zamanlama düzeltmesinden sonra memory-trigger shift'leri için asıl çalma karesi `Shifting-In`): Bu sistem `stingerAudioRadius`'u, çalma-denemesini tetikleyen `OnShiftStateChanged` karesinde (Persistent shift'ler için `Shifting-In`, Persistent olmayanlar için `Held` — bkz. Core Rules "Anı-Tetikleyici Stinger") `GetStingerAudioRadius(shiftId)`'i çağırarak okur — `zoneCenter` hâlâ event payload'ından geliyor (mekansal yerleştirme için, bkz. Interactions with Other Systems), sadece ses-düşüş hesaplaması artık `radius` yerine bu ayrı sorguyu kullanıyor.

---

**Stinger-vs-ambient ducking:** no formula — none is needed. The Core Rule ("stinger never spikes above ambient RMS") is a static mixing constraint enforced at content-authoring/gain-staging time (clip loudness normalized at or below ambient RMS, contrast comes from timbre, not level), not a runtime duck-and-restore behavior. Adding a time-based duck envelope would require its own attack/hold/release curve — that's exactly the "build-up/riser/crescendo" the Core Rules explicitly reject for the stinger. No ducking state, no formula. **Static limiter addendum (design-review, 2026-08-03)**: a brickwall limiter on the Stinger mixer group (bkz. Core Rules, "Runtime enforcement") is also not a formula — it's a one-time mixer-group insert with a fixed ceiling, not a per-frame calculation.

## Edge Cases

- **Eğer bir shift'in stinger'ı, oyuncu o anda o bölgeyi duyamayacak bir
  konumdayken çalarsa (B2, hâlâ açık — design-review, 2026-08-04,
  verification bulgusuyla bu dosyaya taşındı)**: Işık/Volume'un tick-skip
  düzeltmesi bu riski açıkça bu sisteme atamıştı (bkz.
  `isik-volume-durum-sistemi.md` Core Rules, "Bunun kapatmadığı") ama bu
  doküman önceden hiçbir karşılık vermiyordu. Durum: `HeldSessionAlreadyPlayed`
  bariyeri çalma-**denemesinde** yazılır, çalmanın oyuncuya gerçekten
  **duyulabilir** olup olmadığını hiç sormaz (ör. N8'in "en sıradan yol"u —
  Hold biter bitmez asansöre binilirse, stinger `Shifting-In`'de zaten
  eski sahnede çalar, oyuncu belki de o anda yeni sahneye geçmiş olur).
  **Kasıtlı olarak çözülmedi**: "duyulabilirlik" tanımı (mesafe? aktif
  sahne? mixer volume?) kendi başına bir tasarım kararı gerektiriyor —
  bu raporun Warnings bölümünde ayrı bir madde olarak bırakıldı, ileride
  tek başına ele alınacak.
- **Eğer `ZoneChanged`, bir crossfade zaten devam ederken tekrar
  fırlarsa** (oyuncu bir alan sınırında ileri-geri gidip gelirse):
  Crossfade, mevcut `volume_A`/`volume_B` değerlerinden zamanlamayı
  yeniden başlatır, t=0/A=1'e sıfırlanmaz, iki pooled kaynak
  giden/gelen rollerini takas eder. Üçüncü bir `AudioSource` doğmaz.
  Her yeniden tetiklemede tam gaine sıfırlamak, oyuncu sınırı her
  geçtiğinde duyulur bir ses sıçraması yaratırdı.
- **Eğer `ZoneChanged`, A→B crossfade devam ederken genuinely ÜÇÜNCÜ bir
  hedef bölge C için fırlarsa (design-review, 2026-08-03 — systems-
  designer bulgusu, eklendi)**: Yukarıdaki madde sadece aynı çiftin
  (A↔B) sınırda ileri-geri gitmesini kapsıyordu — farklı bir hedefin
  gelmesi önceden tanımsızdı. Düzeltme: aynı "takas" mekanizması
  genelleştirilir — o an **daha sessiz** olan kaynak (hangi rolde olursa
  olsun, "incoming" ya da "outgoing") C'nin ambiyans klibine
  yeniden-atanır ve crossfade zamanlayıcısı mevcut gain'den C'yi hedef
  alacak şekilde yeniden başlar (yine t=0'a sıfırlanmaz). Daha **yüksek
  sesli** kaynak dokunulmadan kendi sönme eğrisine devam eder — zaten
  sessizliğe doğru gidiyordu, bu değişmez. Sonuç: kaç farklı bölge art
  arda ziyaret edilirse edilsin, her zaman sadece 2 pooled kaynak
  yeterlidir, üçüncü bir `AudioSource` hiçbir zaman doğmaz — aynı
  yukarıdaki maddenin garantisi, genelleştirilmiş haliyle.
- **Eğer iki farklı bölge aynı karede Held'e ulaşırsa ve ikisi de
  pooled stinger kaynağına ihtiyaç duyarsa**: Her biri bağımsız olarak
  havuzdan boş bir kaynak alır; boş liste sayısı ≥2 olduğu sürece
  çakışma olmaz.
- **Eğer başka bir bölge Held'e ulaştığında havuzdaki 3-4 stinger
  kaynağının hepsi zaten Playing'se**: Yeni istek sessizce düşürülür —
  kuyruk yok, çalmakta olan bir kaynağı çalarken kesip yer açma yok.
  Zaten çalan bir "tanıdık ama yersiz" ipucunu kesmek, başka birine yer
  açmaktan daha kötüdür; neredeyse-eşzamanlı çoklu-bölge Held normal
  oynanış yolu değil, bir edge case'dir.
- **Eğer `PlayFootstep`, önceki klip bitmeden tekrar çağrılırsa** (hızlı
  hareket ya da stride-accumulator hatası): Klipler kesilmez, üst üste
  biner (belgelenmiş `PlayOneShot` davranışı gereği). Patolojik çağrı
  hızlarında (bir hatadan, hızlı yürümeden değil) hiçbir kısıtlama
  yoktur, üst üste binme sınırsız birikebilir — bu, bir Tuning Knob
  olarak minimum-aralık koruması gerektiren gerçek bir risktir,
  varsayılmaması gereken bir durum.
- **Eğer footstep örnek seti belgelenen 4-6'nın altında, sadece 1
  klipse**: Tekrar-koruma mantığının "son çalan index'i hariç tut"
  kuralı sıfır uygun aday bırakır, bu yüzden seçim istisna fırlatmak ya
  da takılmak yerine tekrara izin vermeye geri dönmelidir.
- **Eğer aynı shiftId'nin bölgesi, stinger'ı hâlâ Cooldown'dayken
  tekrar Held'e girerse**: Event alınır ama görmezden gelinir — durum
  makinesi sadece Idle'dan Held kabul eder, Cooldown'dan değil —
  Cooldown tamamlanana kadar (~1s) çalışmaya devam eder, sonra Idle'a
  döner. Bu, Işık/Volume'un kendi kalıcı-shift yeniden-yükleme
  re-fire korumasını yansıtır, iki sistem de yinelenen olayı aynı
  şekilde ele alır.
- **Eğer bir HARD CUT sahne geçişi (Seviye/Sahne Geçişi'nin sıfır-kare
  swap'ı) bir crossfade ya da stinger ortasına denk gelirse**:
  **Sadece `Abrupt=true` iken (design-review, 2026-08-04 — full
  re-verification bulgusu, netleştirildi — bkz. Core Rules, "Abrupt=false")**:
  Tüm ambiyans kaynakları ve çalmakta olan pooled stinger'lar anında
  durdurulur, fade edilmez. HARD CUT kasıtlı bir süreksizliktir; eski
  sahne sesini bunun üzerinden taşımak "sıfır-kare" öncülüyle çelişirdi.
  Ambiyans yeni sahnenin varsayılan bölgesinde yeniden başlar; tüm
  stinger pool elemanları Idle'a sıfırlanır. `Abrupt=false` iken bu
  kural hiç uygulanmaz — bkz. Core Rules, "Abrupt=false" (ambiyans
  `ambient_crossfade` ile sessizliğe kayar, anında kesilmez). **CutSting grubu bu anlık
  susturma kuralından muaftır (design-review, 2026-08-03 — verification
  N7 bulgusu, eklendi)**: CutSting, aynı `Swapping` karesinde bu kuralla
  birlikte tetiklenir (bkz. Core Rules "HARD CUT Sting") — CutSting zaten
  kendi ayrı mixer grubunda, stinger pool'unun dışında (bkz. Core Rules)
  ama bu kural önceden hangi handler'ın önce çalıştığını belirtmiyordu;
  susturma CutSting'in `PlayOneShot`'undan sonra çalışırsa, fix'in tüm
  amacı olan güvenlik ağı kendi tetiklendiği karede susturulmuş olurdu.
  Uygulama sırası: susturma **önce**, CutSting'in `PlayOneShot`'u
  **ondan sonra** aynı karede çalışır (ya da eşdeğer olarak: susturma
  mantığı CutSting grubunu hiç hedeflemez) — ikisi de aynı sonucu verir,
  implementasyon tercihi implementasyon aşamasına bırakılır.
- **Eğer bir pooled stinger kaynağı bitip Cooldown'a girerken tam o
  anda başka bir bölgenin Held'i bir kaynağa ihtiyaç duyarsa**: O boşa
  çıkan kaynak hemen uygun sayılır — Cooldown sadece kendi shiftId'inin
  yeniden tetiklenmesini kısıtlar, kaynağın başka bir shiftId'e
  uygunluğunu değil.

## Dependencies

**Bağımlıdır**:
- **Işık/Volume Durum Sistemi** — `OnShiftStateChanged` event'ine abone
  olur (`Held` VE `Shifting-In`, ikisi de sadece `IsShiftPersistent(shiftId)==true`
  ise stinger çalma-denemesi tetikler, bkz. Core Rules — design-review,
  2026-08-03 stinger/ışık zamanlama düzeltmesiyle güncellendi, `Held`
  dalına `IsShiftPersistent` koşulu 2026-08-04 ikinci tur bulgusuyla
  eklendi); `GetStingerAudioRadius(shiftId)` sorgusunu çağırır
  (design-review, 2026-08-03 — verification N1 bulgusu, eklendi);
  `IsShiftPersistent(shiftId)` sorgusunu çağırır (design-review,
  2026-08-03 — stinger/ışık zamanlama düzeltmesiyle eklendi, kapsamı
  2026-08-04'te `Held` dalını da içerecek şekilde genişletildi)
- **Birinci Şahıs Kontrolcü** — `PlayFootstep(float speed)` çağrılarını
  alır (FPC'nin stride-phase accumulator'ından)
- **Seviye/Sahne Geçişi** *(design-review, 2026-08-03 — `/review-all-gdds`
  bulgusu, eklendi)* — `OnTransitionStateChanged(newState, type)` event'ine
  abone olur (sadece `newState == Swapping && type == TransitionType.Hard`
  işlenir, HARD CUT sting'ini tetiklemek için — design-review, 2026-08-04
  verification bulgusuyla `type` filtresi eklendi, önceki hali sadece
  `Swapping` diyordu ve N6'nın kapatıldığı Core Rules'la çelişiyordu; bkz.
  Core Rules). Bu, `/review-all-gdds`'in tespit ettiği en ciddi bulguydu:
  önceki taslakta bu bağımlılık hiçbir yönde deklare edilmemişti, ve
  Seviye/Sahne Geçişi'nin kendi Visual/Audio Requirements'ının varsaydığı
  abonelik bu dokümanda hiç uygulanmamıştı. **`GetCurrentHardCutAbrupt()`
  sorgusunu da çağırır** (design-review, 2026-08-04 — full re-verification
  bulgusuyla eklendi — bkz. Core Rules, "Abrupt=false"), `Swapping`'i aldığı
  aynı karede, CutSting'i çalıp çalmayacağına ve anlık-susturma mı yoksa
  crossfade-to-silence mi uygulayacağına karar vermek için.
- **Görev/Taşıma Döngüsü** *(design-review, 2026-08-04 — verification
  design-theory bulgusu, eklendi)* — `CurrentRoundIndex`/`TotalRoundCount`
  sorgularını çağırır (round-bazlı gerilim birikimi için, bkz. Core Rules
  "Round-bazlı gerilim birikimi")

**Kendisine bağımlı olanlar**:
- **Anı-Tetikleyici Etkileşim** *(tasarlandı; design-review, 2026-08-04
  verification turuyla `Needs Revision`'a geri düştü, bkz.
  `gdd-cross-review-2026-08-04.md`)* — bu sistemin stinger playback'ini
  dolaylı olarak tetikler (Işık/Volume'un `Shifting-In`/`Held` durumları
  üzerinden, doğrudan çağrı yok — bkz. o GDD'nin kendi Dependencies
  bölümü, "decoupled" olarak listeliyor)
- **Görev/Taşıma Döngüsü** *(design-review, 2026-08-04 — verification
  bulgusu, eklendi — tek yönlü bağımlılık boşluğu kapatıldı)* —
  `CarryItemDef`'in pickup/delivery/jostle SFX'i için bu sistemin yeni
  **"SFX" mixer grubunu** kullanır (design-review, 2026-08-04 — bu grup
  önceden hiç tanımlanmamıştı, Görev/Taşıma yanlışlıkla var olmayan bir
  "ducking kuralı"na atıfta bulunuyordu — bu proje ducking'i "build-up"
  ihlali sayıp kasıtlı olarak reddediyor, bkz. Stinger-vs-ambient ducking
  notu; SFX grubu Ambiance/Stinger/CutSting'den ayrı, kendi statik
  gain-staging'i dışında hiçbir dinamik işlem taşımaz)

**Not — çapraz-referans güncellendi (design-review, 2026-08-03 —
systems-designer bulgusu)**: Önceki taslak "Anı-Tetikleyici Etkileşim
henüz tasarlanmadı" diyordu — artık tasarlanmış ve Approved. O GDD'nin
kendi (2026-08-03'te eklenen) bileşik-bypass Edge Case'i, `Persistent=false`
bir shift'in geri dönmesi ve yeniden Held'e girmesinin `OnShiftStateChanged`'i
tekrar fırlatabileceğini belgeliyor (edit-time validasyon bypass edilirse).
Bu sistemin `HeldSessionAlreadyPlayed` kümesi (bkz. States and
Transitions) bu senaryoyu zaten doğru ele alır: shiftId, `Shifting-Out`
gözlemlendiğinde kümeden çıkarılmış olacağından, sonraki bir Held
re-entry stinger'ı meşru şekilde tekrar çalar — bu, edit-time validasyon
zaten birincil savunma olduğu için sadece ikincil bir doğrulama notudur.

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| Crossfade süresi (T) | 1–2 s | Duyulur ses sıçraması | Alan geçişinde uzun süre iki ambiyans karışık kalır | Formül: ambient_crossfade |
| Round-bazlı 3. katman tavan volümü (design-review 2026-08-04 eklendi) | Base ambiyansın %20-40'ı (asla baskın olmaz) | Gerilim birikimi fark edilmez | Round 1'de bile duyulur olur, "whisper" ilkesini bozar | Formül: tension_gain |
| Stinger süresi | 1–1.5 s | Fark edilmez, "tanıdıklık" oluşmaz | Build-up gibi hissettirir, Pillar 2'yi ihlal eder | Miksaj felsefesi |
| Stinger RMS tavanı (alan başına, design-review 2026-08-03 eklendi) | Alan ambiyansının kendi ölçülen RMS'i, tek proje-geneli sayı değil — Balo Salonu'nun bas-ağırlıklı ambiyansı için ayrı kalibre edilir | Stinger duyulmaz/maskelenir (özellikle zayıf-bas hoparlörlerde) | "Asla RMS'i aşmaz" ilkesi ihlal edilir, kilit-açma/şok hissi döner | Core Rules: Runtime enforcement; Stinger mixer grubu brickwall limiter |
| Stinger cooldown | 0.5–2 s | Çift-tetikleme riski | Gecikmiş yeniden-tetikleme, oyuncu geri dönerse sessiz kalır | Edge Case: Cooldown re-entry |
| HARD CUT sting süresi/RMS tavanı (design-review 2026-08-03 eklendi) | 1–1.5 s, memory-trigger stinger'ıyla aynı Runtime enforcement (statik limiter + alan-başına tavan) | Zero-frame swap'ın "çalınma" okuması hiç desteklenmez, "hata" gibi okunur | Build-up gibi hissettirir, kendi jump-scare riskini yaratır | Core Rules: HARD CUT Sting |
| Footstep minimum aralık | 0.15–0.3 s | Üst üste binme, gürültü birikmesi | Hızlı harekette adım sesi eksik kalır | Edge Case: PlayFootstep overlap |
| Footstep pitch varyasyonu | ±%3–%8 | Monoton, robotik | Yapay/tutarsız | FPC Visual/Audio Requirements |

## Visual/Audio Requirements

**Ambiyans içeriği** (audio-director yönü): Alan başına 2-3 diegetik
katman — depo (kompresör uğultusu, raf gıcırtısı, su tesisatı), koridor
(floresan balast, uzak kapı, temizlik ekipmanı), balo salonu (tavan
reverb'i, HVAC bas, kristal tıngırtısı). Hiçbir katman "müzikal" ya da
"korku sting" niteliği taşımaz — hepsi bina fiziğinden kaynaklanır.

**Stinger içerik adayları**: Tanıdık ama otelin kendi kapılarından farklı
bir kapı sesi, artık kullanılmayan bir telefon bildirim tonu. **(Nefes
ritmi adayı kaldırıldı — bkz. Core Rules, "İçerik yönü", design-review
2026-08-03 audio-director bulgusu: korku-skorlama dilbilgisini ödünç
alıyordu, Pillar 2'yi ihlal ediyordu.)** Gerçek asset üretimi/kaydı
sonraki bir prodüksiyon adımı.

**Erişilebilirlik gereksinimi (art-director notu)**: Stinger'a özel,
sınırlı kapsamlı bir kapalı altyazı — `Held` durumunda stinger çalarken,
1-1.5sn'lik pencereye senkronize kısa bir non-diegetik metin ipucu
gösterilir (**kesin metin henüz kararlaştırılmadı — bkz. Open Questions
#2, `design/ux/accessibility-requirements.md` yazılana kadar taslak
örnekler bile bu dokümanda tutulmuyor**, design-review 2026-08-03:
önceki taslak, henüz reddedilmiş bir örneği hem burada hem UI
Requirements'ta tekrarlıyordu — implementasyon bu metne yanlışlıkla
sabitlenebilirdi), genel diyalog altyazı sisteminden görsel olarak ayrı
stilize edilir. Bu gerekli — ışık hiçbir zaman stinger'ın verdiği özgül
"tanıma" bilgisini vermez (bkz. Player Fantasy), bu yüzden işitme engelli
bir oyuncu bu altyazı olmadan Pillar 1'in bu anını tamamen kaçırır, eksik
değil, yok yaşar. Bu, `design/ux/accessibility-requirements.md`'nin ilk
girişi olmalı (henüz oluşturulmadı).

**Diyalog altyazısıyla eşzamanlılık (design-review, 2026-08-03 —
game-designer bulgusu, eklendi)**: Bir `Held` geçişi, ekranda bir diyalog
altyazısı gösterilirken de tetiklenebilir — hiçbir kural bunu
engellemiyor. Bu doküman iki altyazının aynı anda ekranda olma
senaryosunu (konum çakışması, okunabilirlik) hiç ele almıyor; sadece
"görsel olarak ayrı stilize edilmeli" diyor, eşzamanlı görünürlük
kuralı vermiyor. Sahip: `/ux-design` (bkz. Open Questions, yeni madde).

**CD-GDD-ALIGN bulgusu, düzeltildi (design-review, 2026-08-03 —
audio-director + creative-director)**: Önceki hali, nesne-adlandıran
örnek metinlerin işitme engelli olmayan oyuncunun yorumlama işini
atladığını belirtiyordu ve çözümü "izlenimci/soyut metin seç" olarak
çerçeveliyordu. **Bu çerçeveleme eksikti**: hangi metin seçilirse
seçilsin (nesne-adlandıran ya da izlenimci), işitme engelli/az işiten
bir oyuncu stinger'ın ham tını-kontrastını hiçbir zaman duymaz — bu
yüzden metin seçimi o oyuncu için "belirsizliği korumaz," çünkü zaten
hiç belirsizlik yaşamıyor. Metin seçimi sadece işiten oyuncunun
deneyimini etkiler; işitme engelli oyuncu için bu **ayrı, gerçek bir
erişilebilirlik boşluğudur** ve `accessibility-requirements.md`'de kendi
satırını hak eder, kelime seçimiyle "çözülmüş" sayılmamalı. Kesin
altyazı metni kararı hâlâ açık — bkz. Open Questions #2.

## UI Requirements

Stinger'a özel kapalı altyazı gösterimi (bkz. Visual/Audio Requirements)
— bir stinger `Playing` durumundayken ekranda 1-1.5sn'lik bir non-diegetik
metin ipucu gösteren minimal bir UI bileşeni gerekir (kesin metin Open
Questions #2'ye bağlı, henüz kararlaştırılmadı). Genel diyalog/altyazı
sisteminden görsel olarak ayrı stilize
edilmeli.

## Acceptance Criteria

1. **GIVEN** ambiyans crossfade `elapsed=0.75s`, `T=1.5s` iken çalışıyor,
   **WHEN** volümler örneklenir, **THEN** `x=0.5`, `ease(x)=0.5`,
   `volume_A=0.5` ve `volume_B=0.5` (eşit karışım, float toleransı
   içinde).
1a. **[BLOCKING, Integration-tier, design-review 2026-08-03 — orijinal
    `/review-all-gdds` raporundan, hiç ele alınmamış blocker, şimdi
    çözüldü]** **GIVEN** oyuncu Depo ve Servis Koridoru
    `AmbientZoneVolume`'ları arasındaki paylaşılan sınırı geçer, **WHEN**
    FPC collider'ı yeni volume'a girer, **THEN** `ZoneChanged(zoneId)`
    tam olarak bir kez fırlar — sistemin daha önce hiçbir gerçek
    tetikleyicisi olmadığını, ambiyans katmanının bütünüyle tetiksiz
    kaldığını kanıtlayan bir entegrasyon testi.
1b. **[Integration-tier, design-review 2026-08-03, ZoneChanged
    sahipliği bulgusuyla eklendi; mekanizma 2026-08-04 full re-verification
    bulgusuyla düzeltildi]** **GIVEN** oyuncu bir sahne yüklendiğinde
    zaten bir `AmbientZoneVolume`'un içinde spawn olur (`OnTriggerEnter`
    bunu doğal olarak yakalamaz), **WHEN** o volume'un kendi sahnesi
    `SceneManager.GetActiveScene()` ile **ilk eşleştiği kare** gelir
    (bu, tek oyunculu bir sahne yüklemesinde `Start()` ile aynı karedir,
    ama bir SOFT geçişin eş-varlık penceresinden çıkışında `Start()`'tan
    sonraki bir karede de olabilir — bkz. Core Rules, "Başlangıç-bölgesi
    kontrolü ile eş-varlık guard'ı çakışması"), **THEN** bir kerelik
    overlap kontrolüyle `ZoneChanged` manuel olarak fırlar — Edge
    Cases'teki "ambiyans yeni sahnenin varsayılan bölgesinde yeniden
    başlar" iddiasının somut mekanizmasını doğrular, hem doğrudan
    yüklemede hem SOFT geçiş sonrasında.
1c. **[BLOCKING, Integration-tier, design-review 2026-08-04 — verification
    bulgusu, eklendi]** **GIVEN** bir SOFT geçiş `Swapping`'e ulaştı ve
    köken sahne henüz unload edilmedi (0.5-2s eş-varlık penceresi), oyuncu
    fiziksel olarak hem köken hem hedef sahnenin `AmbientZoneVolume`
    sınırları içinde, **WHEN** bu pencere boyunca herhangi bir tick
    çalışır, **THEN** sadece hedef sahnenin (artık `SceneManager.GetActiveScene()`
    ile eşleşen) volume'u işlenir — köken sahnenin volume'u sessizce
    pas geçilir, sahte bir `ZoneChanged` fırlatmaz. Bu, Işık/Volume'un
    kendi eş-varlık düzeltmesinin `AmbientZoneVolume`'a da taşındığını
    doğrular.
1d. **[design-review, 2026-08-04 — verification design-theory bulgusu,
    eklendi]** **GIVEN** 4 round'luk bir gece, **WHEN** `CurrentRoundIndex`
    sırasıyla `0, 1, 2, 3` iken her alanın round-bazlı 3. katman volümü
    örneklenir, **THEN** `TensionGain(0)=0` (3. katman sessiz),
    `TensionGain(3)=1` (3. katman tavan volümde), ara değerler
    `tension_gain` formülüyle (bkz. Formulas) eşleşir — gecenin genel
    gerilim birikiminin somut, ölçülebilir bir taşıyıcısı olduğunu
    kanıtlar (bkz. `game-concept.md` Core Loop).
2. **GIVEN** 0 ile `T` arası herhangi bir `elapsed`'de devam eden bir
   crossfade, **WHEN** `volume_A` ve `volume_B` toplanır, **THEN** her
   örneklenen karede toplam 1.0'a eşittir, ve `x=0`/`x=1`'de her iki
   volümün değişim oranı sıfırdır (duyulur bir sıçrama onseti yok).
3. **GIVEN** devam eden bir crossfade (ör. `volume_A=0.844`,
   `volume_B=0.156`), **WHEN** `ZoneChanged` tamamlanmadan tekrar
   fırlar, **THEN** iki pooled kaynak mevcut gain değerlerinden
   giden/gelen rollerini takas eder (t=0/A=1'e sıfırlanmaz), üçüncü bir
   `AudioSource` oluşturulmaz.
4. **GIVEN** FPC `PlayFootstep(speed)`'i `speed=1.6` (yüksüz maksimum)
   ile çağırır, **WHEN** volüm hesaplanır, **THEN**
   `footstep_volume=1.0`.
5. **GIVEN** FPC `PlayFootstep(speed)`'i `speed=1.35` (taşıma) ile
   çağırır, **WHEN** volüm hesaplanır, **THEN**
   `footstep_volume=0.84375`, ve ses sistemi taşıma durumuna göre dallanma
   yapmaz — sadece `speed` okunur.
6. **[design-review, 2026-08-04 — ikinci tur full re-verification
   bulgusuyla `IsShiftPersistent(shiftId)=false` önkoşulu `=true`'ya
   çevrildi — bkz. aşağıdaki not]** **GIVEN** bir `shiftId` için `Idle`
   durumundaki bir stinger, VE `IsShiftPersistent(shiftId)=true`, VE
   `HeldSessionAlreadyPlayed` bu `shiftId`'yi içermiyor, **WHEN**
   `OnShiftStateChanged`, `newState=Held` ile fırlar, **THEN** stinger
   `Playing`'e geçer, havuzdan bir kaynak alır, `spatialBlend=1` ayarlar,
   `zoneCenter`'a konumlandırır, VE `shiftId` `HeldSessionAlreadyPlayed`'e
   eklenir. *(design-review, 2026-08-03: "VE shiftId eklenir" kısmı
   eklendi, aşağıdaki AC6a'nın önkoşulu. Önkoşul geçmişi: 2026-08-03
   stinger/ışık zamanlama düzeltmesinde bu AC bilinçli olarak
   `IsShiftPersistent=false` (Persistent shift'ler için asıl çalma yolu
   AC6c) test ediyordu — ama bu, `Held`'in **koşulsuz** her zaman bir
   çalma-denemesi olduğu varsayımına dayanıyordu. 2026-08-04'te
   `isik-volume-durum-sistemi.md`'ye eklenen zorunlu `Automatic` (kasıtlı
   `Persistent=false`) ambient bölge bu varsayımı gerçek bir hataya
   çevirdi — bkz. Core Rules, "`IsShiftPersistent` koşulu `Held` dalına
   da eklendi". Şimdi `Held` dalı da `IsShiftPersistent=true` gerektiriyor,
   yani bu AC artık AC6c'nin test ettiği erken-çalma yolunun **doğal
   devamını** (aynı Persistent shift, ~3s sonra gelen normal `Held`
   çalma-denemesi) test ediyor, ayrı bir "Persistent olmayan Held" yolunu
   değil — o yol artık aşağıdaki AC7'de "asla çalmaz" olarak test
   ediliyor.)*
6a. **GIVEN** bir `shiftId`, `HeldSessionAlreadyPlayed`'de zaten var
   (ör. bir Persistent shift AC6c yoluyla zaten çalmış, ya da bu
   oturumda daha önce Held'e ulaşmış bir Persistent shift'in sahnesi
   yeniden yüklendi), **WHEN** `OnShiftStateChanged`, aynı `shiftId` için
   `newState=Held` ile (tekrar) fırlar — aynı aktivasyonun ~3s sonraki
   normal `Held` fırlaması dahil, reload-tetikli re-fire dahil —,
   **THEN** tamamen no-op'tur — havuzdan kaynak alınmaz, stinger
   `Playing`'e hiç girmez, `Idle`'da kalır. *(design-review, 2026-08-03
   — systems-designer bulgusu, en kritik bulgu: önceki taslakta bu
   senaryo hiç test edilmiyordu, ~1s Cooldown'un bunu "yuttuğu"
   varsayılıyordu — gerçekte reload çok daha uzun sürer, Cooldown çoktan
   Idle'a dönmüş olur. Bu AC, "tanıdık ama yersiz" sesin oturum boyunca
   sadece bir kez duyulması garantisini doğrudan test eder — bkz. Core
   Rules, States and Transitions.)*
6c. **[BLOCKING, Integration-tier, design-review 2026-08-03 — orijinal
   `/review-all-gdds` raporunun hiç ele alınmamış blocker'ı, şimdi
   çözüldü]** **GIVEN** bir `shiftId` için `Idle` durumundaki bir
   stinger, VE `IsShiftPersistent(shiftId)=true`, VE
   `HeldSessionAlreadyPlayed` bu `shiftId`'yi içermiyor, **WHEN**
   `OnShiftStateChanged`, `newState=Shifting-In` ile fırlar, **THEN**
   stinger **aynı karede** `Playing`'e geçer (ışığın kendi `Shifting-In`
   rampasının başladığı kareyle senkron, `Held`'i beklemez) VE `shiftId`
   `HeldSessionAlreadyPlayed`'e eklenir. Bu, üç dokümanın "ışık+ses
   bileşik etki" iddiasını gerçekten doğrulayan asıl testtir — önceki
   taslakta stinger ışık rampası tamamlandıktan (~3s) sonra çalıyordu,
   bu AC o zamanlama boşluğunun kapandığını kanıtlar.
6b. **GIVEN** bir `shiftId`, `HeldSessionAlreadyPlayed`'de var, **WHEN**
   `OnShiftStateChanged`, aynı `shiftId` için **`newState == Shifting-Out`
   YA DA `newState == Dormant`** ile fırlar (design-review, 2026-08-04 —
   verification bulgusu, koşul `newState != Held`'den daraltıldı —
   `Shifting-In`'i artık kapsamıyor, bkz. Core Rules), **THEN** `shiftId`
   `HeldSessionAlreadyPlayed`'den çıkarılır — bir sonraki meşru Held
   girişi (Persistent olmayan bir shift'in bölgeye tekrar girilmesi)
   stinger'ı normal şekilde tekrar çalabilir. *(design-review, 2026-08-03
   — bu, Persistent olmayan shift'lerin tekrar-Held-girişinde stinger'ın
   hâlâ çaldığını doğrular; AC6a'nın "asla tekrar çalmaz" davranışı
   sadece Persistent shift'lere özgüdür, onlar hiçbir zaman Held'den
   çıkmadığı için.)*
7. **GIVEN** `Idle` durumundaki bir stinger, **WHEN**
   `IsShiftPersistent(shiftId) == false` iken `OnShiftStateChanged` HERHANGİ
   bir `newState` (`Held` dahil) ile fırlar, YA DA `IsShiftPersistent(shiftId)
   == true` iken `newState` `Held`/`Shifting-In` dışında bir değerle
   fırlar (design-review, 2026-08-04 — ikinci tur full re-verification
   bulgusuyla koşul yeniden yazıldı: önceki hali `newState != Held VE
   (newState != Shifting-In YA DA IsShiftPersistent==false)` diyordu —
   bu, `Held` dalının artık `IsShiftPersistent` gerektirdiğini
   yansıtmıyordu, yani `Persistent=false` bir shift `Held`'e ulaştığında
   bu AC'ye göre hâlâ çalması gerekiyormuş gibi okunuyordu, Core
   Rules'taki düzeltmeyle çelişerek), **THEN** event göz ardı edilir,
   stinger `Idle`'da kalır (kaynak alınmaz, çalma olmaz). **Bu AC artık
   zorunlu `Automatic` ambient bölgenin (her zaman `Persistent=false`)
   `Held`'e ulaştığı normal durumu da kapsıyor** — o bölge bu sistemin
   stinger'ını hiçbir zaman tetiklemez, bkz. Core Rules.
8. **GIVEN** `Playing` durumundaki bir stinger, **WHEN** klibi biter,
   **THEN** ~1s (Tuning Knob aralığı 0.5–2s) süreyle `Cooldown`'a geçer,
   sonra `Idle`'a döner.
9. **[Integration-tier, design-review 2026-08-03 — qa-lead bulgusu,
   etiketlendi: gerçek AudioSource pool durumu + playback'i kapsar, saf
   Logic değil]** **GIVEN** havuzdaki 3-4 stinger `AudioSource`'ının
   hepsi `Playing`, **WHEN** başka bir bölge `Held`'e ulaşıp kaynak talep
   eder, **THEN** istek sessizce düşürülür — kuyruk yok, çalmakta olan
   bir kaynağın kesilmesi yok, VE düşürülen `shiftId` hiçbir pool
   slotunda `Playing`/`Cooldown` durumuna girmez (yetim/yarım durum
   kalmaz — düşürülen istek pool'a hiç dokunmamış gibi davranır).
9a. **GIVEN** aynı bölge/alan içinde ambiyans crossfade N≥5 kez hızlı
   art arda tetiklenir (oyuncu bir sınırda ileri-geri gider), **WHEN**
   pool durumu her tetiklemeden sonra sayılır, **THEN** her zaman tam
   olarak 2 ambiyans `AudioSource`'u vardır — üçüncü bir kaynak hiçbir
   noktada oluşturulmamıştır. *(design-review, 2026-08-03 — qa-lead
   bulgusu: pool-sızıntısı riski önceden hiçbir AC ile test edilmiyordu,
   sadece Edge Cases'te prose olarak iddia ediliyordu.)*
10. **GIVEN** son çalan index'i bilinen 4-6 kliplik bir footstep örnek
    seti, **WHEN** `PlayFootstep` sonraki örneği seçer, **THEN** seçilen
    index son çalan index'i hariç tutar.
11. **GIVEN** tam olarak 1 kliplik bir footstep örnek seti, **WHEN**
    `PlayFootstep` sonraki örneği seçer, **THEN** aynı klip tekrar çalar
    (tekrara izin verilir), istisna fırlatmaz ya da takılmaz.
12. **GIVEN** `shiftId=X` için `Cooldown`'daki bir stinger, **WHEN**
    `OnShiftStateChanged` aynı `shiftId` için `newState=Held` ile tekrar
    fırlar, **THEN** event göz ardı edilir, durum ~1s pencere dolana
    kadar `Cooldown`'da kalır, sonra `Idle`'a geçer (doğrudan
    `Playing`'e değil).
13. **[Integration-tier, design-review 2026-08-03 — qa-lead bulgusu,
    etiketlendi]** **GIVEN** bir ambiyans crossfade ya da stinger çalma
    ortasında, **WHEN** bir HARD CUT sahne geçişi gerçekleşir, **THEN**
    tüm ambiyans kaynakları ve çalan stinger kaynakları anında durur
    (fade olmadan), tüm stinger pool elemanları `Idle`'a sıfırlanır
    (**not**: `HeldSessionAlreadyPlayed` bundan etkilenmez, kalıcı
    kalır — bkz. AC6a), ambiyans yeni sahnenin varsayılan bölgesinde
    yeniden başlar.
13a. **[Integration-tier, design-review 2026-08-03 — `/review-all-gdds`
    bulgusu, eklendi; imza 2026-08-03 verification N6 bulgusuyla
    güncellendi]** **GIVEN** `CurrentState` Seviye/Sahne Geçişi'nde
    `Ready` (bir HARD CUT preload edilmiş), **WHEN**
    `OnTransitionStateChanged(Swapping, TransitionType.Hard)` fırlar,
    **THEN** CutSting tam olarak bir kez çalar, çalma başlangıcı
    `Swapping` event'inin fırladığı kareyle senkrondur (kare-gecikmesi
    `SWAP_FRAME_EPSILON`, bkz. `seviye-sahne-gecisi.md` Core Rules, ile
    ölçülür). Bu, önceki taslakta hiç var olmayan bir sistem-arası
    bağlantıyı test eder — `/review-all-gdds`'in üç bağımsız geçişinin de
    bağımsız olarak bulduğu en ciddi bulguydu (bkz.
    `design/gdd/gdd-cross-review-2026-08-03.md`).
13a2. **[BLOCKING, Integration-tier, design-review 2026-08-04 — full
    re-verification bulgusu, eklendi]** **GIVEN** `CurrentState` Seviye/Sahne
    Geçişi'nde `Ready` (bir HARD CUT preload edilmiş) VE
    `GetCurrentHardCutAbrupt() == false` (görev-tamamlama bitişi), **WHEN**
    `OnTransitionStateChanged(Swapping, TransitionType.Hard)` fırlar,
    **THEN** CutSting **çalmaz**, VE mevcut ambiyans/stinger kaynakları
    anında durdurulmaz — bunun yerine `ambient_crossfade` formülüyle
    (aynı `T` süresinde) sessizliğe kayar. Bu, iki HARD CUT bitişinin artık
    ölçülebilir şekilde farklı ses davranışı ürettiğini kanıtlar.
13b. **[BLOCKING, Integration-tier, design-review 2026-08-03 verification
    N6 bulgusu, eklendi]** **GIVEN** bir Asansör ya da başka bir çağıranın
    başlattığı sıradan bir SOFT geçiş, **WHEN**
    `OnTransitionStateChanged(Swapping, TransitionType.Soft)` fırlar,
    **THEN** CutSting **çalmaz** — SOFT ve HARD CUT aynı paylaşılan durum
    makinesini kullandığı için (`Swapping` ikisinde de fırlar), `type`
    filtresi olmadan bu AC başarısız olurdu. Bu, HARD CUT sting'inin
    gündelik geçişlerde (asansör, seviye geçişi) yanlışlıkla çalıp Pillar 2
    (Sessiz Gerilim, Şok Değil) ihlal eden bir jump-scare üretmediğini
    kanıtlar — önceki taslakta bu ayrım hiç yoktu, sting her `Swapping`'de
    koşulsuz çalıyordu.
13c. **[BLOCKING, Integration-tier, design-review 2026-08-03 verification
    N7 bulgusu, eklendi]** **GIVEN** bir HARD CUT geçişi bir crossfade ya
    da çalmakta olan bir stinger ortasına denk gelir (bkz. Edge Cases,
    "anlık susturma kuralı"), **WHEN** `Swapping` aynı karede hem
    susturmayı hem CutSting'i tetikler, **THEN** CutSting duyulabilir
    şekilde çalar — susturma kuralı CutSting'i sessizce yutmaz. Bu,
    fix'in kendi güvenlik-ağı amacının kendi tetiklendiği karede
    kendini susturmadığını kanıtlar.
14a. **[BLOCKING, Logic-tier]** **GIVEN** `Playing` durumundaki bir
    stinger, **WHEN** 1-1.5sn'lik klip çalar, **THEN** ekranda çalma
    penceresine senkronize bir non-diegetik altyazı belirir (klip
    başlama zaman damgasıyla eşleşir) ve klip bitince kaybolur (klip
    bitiş zaman damgasıyla eşleşir) — saf zamanlama/senkron testi, metin
    içeriğinden ya da görsel stilden bağımsız. *(design-review,
    2026-08-03 — qa-lead bulgusu: önceki tek AC14, test edilebilir bir
    senkron iddiasını test edilemez bir görsel-stil iddiasıyla
    birleştiriyordu — ayrıldı.)*
14b. **[ADVISORY, henüz test edilemez — bkz. Blocked Acceptance Criteria]**
    **GIVEN** stinger altyazısı görünür, **WHEN** ekranla karşılaştırılır,
    **THEN** diyalog altyazılarından görsel olarak ayırt edilebilir
    stilize edilmiştir (font/renk/konum) — `design/ux/accessibility-
    requirements.md` henüz yazılmadığından şu an test edilemez.

### Blocked Acceptance Criteria (Deferred)

| AC | Blocked By | Closure Trigger | Owner |
|---|---|---|---|
| 14b (altyazı görsel stili) | `design/ux/accessibility-requirements.md` henüz yazılmadı; altyazı metninin nesne-adlandıran mı izlenimci/soyut mu olacağına dair açık tasarım sorusu (bkz. Open Questions #2) da aynı dosyada çözülecek | `/ux-design` çalıştırılıp `accessibility-requirements.md` yazıldığında, bu AC o dosyanın somut stil belirteçleriyle yeniden yazılmalı | ux-designer / art-director |
| Uçtan-uca tetikleme (gerçek oyuncu Hold'u → stinger playback, simüle `OnShiftStateChanged` çağrısı yerine) | **Çözüldü (design-review, 2026-08-03)**: Anı-Tetikleyici Etkileşim artık Approved — bu blocker'ın önkoşulu artık mevcut | Tam motor entegrasyon testi implementasyon sırasında yazılabilir | Adaptif Ses Sistemi implementasyon sahibi |

*(design-review, 2026-08-03 — qa-lead bulgusu: önceki hali iki maddeyi
serbest prose "ERTELENDİ" listesi olarak taşıyordu, Owner/Closure
Trigger yoktu — `isik-volume-durum-sistemi.md`'nin Blocked-ACs tablo
desenine hizalandı. İkinci madde ayrıca bayatlamıştı — Anı-Tetikleyici
Etkileşim artık tasarlanmış ve Approved, o blocker artık geçersiz.)*

## Open Questions

1. **Footstep minimum-interval throttle — hangi sistem uygular?** Tuning
   Knob olarak tanımlı (0.15–0.3s) ve Edge Cases bunu gerçek bir risk
   olarak işaretliyor, ama Core Rules/Interactions bunun FPC'nin
   stride-phase accumulator'ında mı yoksa `PlayFootstep`'in kendisinde
   mi uygulanacağını belirtmiyor. **Owner**: unity-specialist (mimari
   karar). **Hedef çözüm**: dev-story implementasyonundan önce.
2. **Stinger altyazı metni: nesne-adlandıran mı, izlenimci/soyut mu?**
   CD-GDD-ALIGN (creative-director), taslak örneklerin (`[bir nefes
   ritmi]`, `[tanıdık olmayan bir kapı]`) nesne adlandırdığını ve bunun
   işitme engelli olmayan oyuncunun yaptığı yorumlama işini (tanıdık ama
   *neden* rahatsız olduğunu bilmeme) atlayarak Pillar 1/5'in kasıtlı
   belirsizliğini erken çözdüğünü işaretledi. İzlenimci/soyut ifadeler
   (özgül nesneyi adlandırmadan ritim/doku ipucu veren) bu riski taşımaz
   ama erişilebilirlik netliğini de azaltabilir — iki uç arasında bir
   denge kararı gerekiyor. **Netleştirme (design-review, 2026-08-03 —
   audio-director önerisi, creative-director kabul etti)**: Sound-design
   açısından izlenimci/soyut yön öneriliyor (nesne-adlandırma "familiar
   but misplaced" fantazisini gereksiz erken çözüyor) — ama bu sadece bir
   **öneri**, nihai karar hâlâ `/ux-design`'da verilecek. Ayrıca bu
   kararın işitme engelli oyuncu için erişilebilirlik boşluğunu
   **kapatmadığı** artık netleştirildi (bkz. Visual/Audio Requirements,
   "CD-GDD-ALIGN bulgusu, düzeltildi") — bu ayrı bir açık soru olarak
   madde 3'e taşındı. Bu karara bağlı olarak
   `design/ux/accessibility-requirements.md` (henüz oluşturulmadı, bu
   GDD onun ilk girişi olacak) yazılacak; dosyanın kesin görsel stil
   belirteçleri (font, renk, diyalog altyazısından ayrım) de aynı
   adımda belirlenecek. **Owner**: ux-designer / art-director /
   narrative-director (metin tonu için). **Hedef çözüm**: `/ux-design`
   çalıştırıldığında.
3. **İşitme engelli/az işiten oyuncu için ayrı, çözülmemiş bir
   erişilebilirlik boşluğu (design-review, 2026-08-03 — audio-director
   bulgusu)**: Stinger altyazısının metni ne olursa olsun (madde 2),
   işitme engelli bir oyuncu stinger'ın ham tını-kontrastını hiç duymaz
   — bu yüzden "belirsizliği koru" hedefi bu oyuncu için anlamsızdır,
   çünkü zaten hiç belirsizlik yaşamıyorlar. Bu, kelime seçimiyle
   kapanmayan, `accessibility-requirements.md`'nin kendi başına ele
   alması gereken bir tasarım sorusu (ör. işitme engelli oyuncuya
   "familiar but misplaced" hissini başka bir kanaldan — ince bir görsel
   ipucu, altyazı zamanlamasının kendisi — verecek ayrı bir çözüm
   gerekebilir mi, yoksa bu deneyim farkı MVP için kabul edilebilir mi?).
   **Owner**: ux-designer / accessibility-specialist. **Hedef çözüm**:
   `/ux-design` çalıştırıldığında, madde 2 ile aynı geçişte.
4. **Diyalog altyazısıyla eşzamanlı görünürlük kuralı yok (design-review,
   2026-08-03 — game-designer bulgusu)**: Bir `Held` geçişi diyalog
   ortasında tetiklenirse, iki altyazı (stinger + diyalog) aynı anda
   ekranda olabilir — konum/okunabilirlik çakışması için hiçbir kural
   tanımlı değil. **Owner**: ux-designer (`/ux-design`, madde 2 ile aynı
   geçişte çözülebilir). **Hedef çözüm**: implementasyon öncesi.
5. **[ÇÖZÜLDÜ — design-review, 2026-08-03, verification N1 bulgusu]**
   ~~Stinger'ın algısal ses-düşüş yarıçapı, `radius`'tan bağımsız olmalı
   mı?~~ Evet — `ShiftConfig`'e `StingerAudioRadius` alanı eklendi,
   `stinger_falloff` formülü artık ondan türetiliyor (bkz. Formulas), ve
   Işık/Volume'a `GetStingerAudioRadius(shiftId)` sorgusu eklendi (bkz.
   `isik-volume-durum-sistemi.md` Interactions with Other Systems). Bu
   kararın aciliyeti arttı çünkü `TriggerMode=ManualOnly` düzeltmesi
   `radius`'u memory-trigger bölgeleri için kasıtlı olarak "kullanılmaz"
   ilan etti — artık sadece kavramsal uyumsuzluk değil, ölü bir alana
   bağımlılıktı.
