# Işık/Volume Durum Sistemi (Lighting/Volume State System)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`
> ve `design/gdd/gdd-cross-review-2026-08-03-verification.md`) — header
> önceki `In Design`/2026-08-01'de yanlışlıkla kalmıştı, bu belge
> `/review-all-gdds`'in bulduğu `TriggerMode` kritik bulgusu dahil önemli
> ölçüde revize edildi ama header hiç güncellenmemişti (2026-08-03'te
> düzeltildi, aynı sınıf hata `adaptif-ses-sistemi.md`/
> `seviye-sahne-gecisi.md`'de de bulunmuştu). N8 bu turda çözüldü (bkz.
> Core Rules "Örnekleme tam olarak ne kapsar").
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **Implements Pillar**: Pillar 1 (Öznel Gerçeklik), Pillar 2 (Sessiz Gerilim, Şok Değil)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (kabul edildi, notlar eklendi) 2026-08-01 — çoklu bölge görünürlüğü ve Persistent birikme riski (bkz. Open Questions)

## Overview

Işık/Volume Durum Sistemi, otelin "gerçeklik" ve "anı" durumları arasındaki
görsel geçişi yönetir: URP Global Volume ağırlığını ve sahne ışıklarının
renk/yoğunluğunu, sabit bir sıcak-amber temel durumdan soğuk/desatüre bir
"anı" durumuna kademesiz olarak kaydırır. Teknik olarak bu, önceden
yapılandırılmış bir Volume profili (White Balance + Color Adjustments
override'ları) ile sahne ışıklarının renk/yoğunluk interpolasyonunu
senkronize eden bir durum makinesidir; bir konsept prototibinde zaten
doğrulanmış ve somut ayar değerleriyle kanıtlanmıştır.

Oyuncuya yönelik etkisi doğrudandır: bu sistem, Görsel Kimlik Çapası'nın
("Otel Senin Yerine Hatırlıyor") teknik motorudur — oyuncunun "bir şeyler
değişti ama emin değilim" hissini üreten tek mekanizmadır. Otel geometrisi
hiç değişmez; sadece ışık ve renk sıcaklığı yalan söyler.

## Player Fantasy

Bu sistem korku yaratmaz — korkunun **kanıtını** verir. Oyuncu bir anlığına
gördüğüne güvenemez hale gelir: aynı koridor, bir saniye önce sıcak ve
tanıdıkken, şimdi soğuk ve yabancıdır. Bu bir korku anı değil, bir
*gerçeklik testi başarısızlığı* hissidir — "ben mi yanlış hatırlıyorum,
yoksa oda mı değişti?" (**Zemin Kayıyor**, Pillar 1: Öznel Gerçeklik).

Renk kayması bir alarm değil, bir *fısıltıdır* — oyuncuya "buraya bak" der
ama neden bakması gerektiğini söylemez (**Sahnenin Arkasındaki Şey**).
Dedektif değil, kazara bir şeye tanık olan biri hissi. Bu sistem tek başına
rahatsız edici olmayı hedeflemez (prototip bunu doğruladı) — dread'i
tamamlayan asıl parça Adaptif Ses Sistemi'dir; bu sistemin işi sadece "bir
şeyler değişti" şüphesini ekmek.

## Detailed Design

### Core Rules

- **Bölge başına bağımsız Volume**: Her tetikleyici bölgesi (`is Global = false`,
  kutu collider ile sınırlı) kendi Volume instance'ını taşır, ama hepsi **tek
  bir paylaşılan Volume Profile asset'ini** kullanır (sanatçı tek bir White
  Balance/Color Adjustments preset'ini ayarlar). Aynı anda birden fazla bölge
  bağımsız olarak "shift" olabilir.
- **Kutu collider ile R_trigger/R_exit ilişkisi (netleştirme)**: Kutu collider,
  URP'nin yerel (`isGlobal = false`) Volume component'inin zorunlu kıldığı
  collider'dır — sadece Volume'un kendi etki alanını tanımlar, aşağıdaki
  R_trigger/R_exit hesaplamasıyla DOĞRUDAN bağlı değildir. R_trigger/R_exit
  tamamen ayrı, kod-tabanlı bir mesafe kontrolüdür: bölge merkezi
  (`zoneCenter`) ile oyuncu pozisyonu arasındaki mesafe, tick başına bir kez
  örneklenir (bkz. Edge Cases, "Tick" tanımı aşağıda). `zoneCenter` zorunlu
  bir `Vector3` alanıdır; açıkça ayarlanmazsa **collider'ın world-space
  `bounds.center` değerine varsayılan olarak düşer** (Awake/OnValidate'te
  hesaplanır) — bu sadece bir kolaylık varsayılanıdır, kasıtlı yer
  değiştirme (ör. göz hizası) için level designer'ın bunu elle geçersiz
  kılması gerekir. Level designer kutuyu görsel/mekansal referans için
  serbestçe boyutlandırabilir; R_trigger/R_exit değerlerini kutudan
  bağımsız olarak Inspector'da ayrı ayarlar.
  **Volume weight kontrolü — spike ile doğrulandı (2026-08-01):** Bu
  paragraf üç review turunda üç farklı (ve ilk ikisi teknik olarak yanlış
  bulunan) açıklama aldı; round 3 sonunda kağıt üzerinde tartışmayı kesip
  `prototypes/yankilar-volume-weight-spike/`'ta ampirik olarak test edildi.
  **Doğrulanan pratik kural** (Corridor C, box half-extent=10m,
  `blendDistance=0`): ticker `Volume.weight`'i her karede doğrudan
  `ShiftProgress`'e eşitler; kutu, aşağıdaki Box Collider Safety Margin
  formülüne göre yeterince büyük boyutlandırılırsa (R_exit + oyuncunun
  Shifting-Out sırasında yürüyebileceği maksimum mesafe + güvenlik payı) ve
  `blendDistance=0` ise, oyuncu 3 saniyelik Shifting-Out çürümesi tamamen
  bitene kadar (t=11.69→14.69, kayıttan) collider'ın fiziksel sınırının
  içinde kalır — collider'dan gerçekten çıkış (t=15.08) ancak weight zaten
  0'a ulaştıktan SONRA gerçekleşir. Sonuç: pürüzsüz, sıçramasız bir geçiş,
  doğrudan gözlemlendi. **Not (dürüstlük için)**: hangi iç Unity API
  mekanizmasının bunu sağladığı (scripted weight'in Unity'nin kendi mesafe
  hesaplamasını tam olarak "ezip ezmediği") bu spike'ta ayırt edilmedi —
  sadece doğru boyutlandırılmış kutu + blendDistance=0 kombinasyonunun
  gözlemlenebilir sonucu doğrulandı (Corridor A/B, yani yetersiz boyutlu
  kutu senaryoları, koşulmadı). Pratik kural için bu yeterli: **her zaman
  Box Collider Safety Margin formülüne göre boyutlandır, mekanizmayı
  varsaymaya gerek yok.**
  **Ek kural (değişmedi)**: `Volume.weight`'in **tek yazıcısı** bu sistemin
  ticker'ı olmalı — hiçbir Animator/Timeline aynı alanı ayrıca key'lememeli
  (aksi halde son-yazan-kazanır çakışması, örtüşen bölge ışıkları için
  zaten belgelenen riskle aynı sınıfta).
- **"Tick" tanımı**: Bu belgede "tick", bölge başına çalışan
  pozisyon-örnekleme coroutine/ticker'ının bir iterasyonunu ifade eder —
  varsayılan olarak `yield return null` ile kare başına bir kez (frame-rate'e
  bağlı). Performans gerekçesiyle sabit bir aralığa (ör. her 0.1s)
  seyreltilebilir; bunu yapmak hızlı-hareket-atlama edge case'inin
  penceresini orantılı olarak genişletir — hikaye açısından kritik
  bölgelerde varsayılan kare-başına örnekleme önerilir, ve R_trigger'ın
  minimum güvenli değeri = oyuncunun maksimum hızı × tick aralığı olmalıdır.
  **Tick, sahnesi aktif olmayan bölgeler için durur (design-review,
  2026-08-03 — `/review-all-gdds` bulgusu, eklendi, kritik bulgu)**:
  Seviye/Sahne Geçişi'nin `Swapping`/eski-sahne-unload ayrımı (bkz.
  `seviye-sahne-gecisi.md` Core Rules), aktivasyon ile unload arasında
  0.5-2s'lik bir pencerede **iki sahnenin additive olarak eş-zamanlı
  resident** kalmasına yol açar. Bu pencerede, köken sahnedeki bölgelerin
  ticker'ı hâlâ çalışıyor olsaydı, oyuncunun (artık hedef sahnedeki)
  pozisyonunu köken sahnenin bölge merkezlerine karşı örneklemeye devam
  ederdi — additive yüklemede iki sahne aynı world-space'i paylaştığından,
  bu sahte bir `OnShiftStateChanged` fırlatabilir ve Anlatı Durum'un
  `SeenShiftIds`'i (asla temizlenmez) ya da Adaptif Ses'in
  `HeldSessionAlreadyPlayed`'i (Persistent shift'ler için asla temizlenmez)
  gibi kalıcı kayıtları, oyuncunun hiç deneyimlemediği bir olay için
  kalıcı olarak bozardı. **Kural**: bir bölgenin ticker'ı, kendi sahnesi
  `SceneManager.GetActiveScene()` ile eşleşmediği her karede **örnekleme
  yapmadan atlanır** (pas geçilir, durdurulmaz/yok edilmez — sahne
  gerçekten unload edilene kadar nesne hâlâ var olabilir) — bu, Seviye/Sahne
  Geçişi'nin `Swapping` adımıyla aynı karede devreye girer, ek bir event
  aboneliği gerektirmez (`SceneManager.GetActiveScene()` her tick'te zaten
  ucuz bir sorgu).
  **"Örnekleme" tam olarak ne kapsar (design-review, 2026-08-03 —
  verification N8 bulgusu, netleştirildi)**: Yukarıdaki kural sadece
  **pozisyon-tabanlı kontrolleri** kapsar — `Automatic` bölgeler için
  giriş tespiti (`R_trigger` içine girme) ve her iki mod için de çıkış
  histerezisi (`R_exit` dışına çıkma). **Kapsamadığı**: hâlihazırda
  `Shifting-In`/`Shifting-Out`'ta olan bir bölgenin `x` ilerleme
  değişkeni (bkz. Formulas > Shift Progress > Durum takibi notu) —
  bu saf zaman-tabanlıdır (`x += DeltaTime/Duration`), pozisyon verisi
  gerektirmez, ve sahnesi aktif olmasa bile **her karede ilerlemeye devam
  eder**. Önceki taslakta bu ayrım hiç yapılmamıştı — "örnekleme atlanır"
  ifadesi, `x`'in de donduğu şeklinde okunabilirdi, bu da elevatöre
  binmeden hemen önce tamamlanan bir Hold'un (`ani-tetikleyici-etkilesim.md`'nin
  "en sıradan yol" dediği senaryo) bağlı olduğu bölgeyi 0.5-2s'lik
  eş-varlık penceresinde `Shifting-In`'de **sonsuza kadar donmuş**
  bırakabilirdi (bölge sahnesi bir daha hiç aktif olmayabilir). `x`'in
  pozisyondan bağımsız ilerlemeye devam etmesi bu donmayı yapısal olarak
  imkansız kılar — bölge, kimse izlemese bile zamanlaması geldiğinde
  `Held`'e ulaşır ve `OnShiftStateChanged` normal şekilde fırlar (bu,
  yukarıdaki "sahne yüklenirken zaten kalıcı bir shift aktifse" Edge
  Case'iyle aynı felsefe: sistem, kimse tanık olmasa da ilerlemeyi
  garanti eder). **Bunun kapatmadığı**: bölgenin sesi kimse duymadan
  "çalınmış" sayılması riski (B2, hâlâ açık — bkz.
  `gdd-cross-review-2026-08-03-verification.md`) bu düzeltmeyle
  kasıtlı olarak kapsam dışı bırakıldı; Adaptif Ses'in kendi
  `HeldSessionAlreadyPlayed` bariyeri bunu ayrıca ele almalı.
- **Geri dönüş: yarıçap-tabanlı, histerezisli**: Bir shift, oyuncu tetikleyici
  yarıçapındayken Held'de kalır; sadece biraz daha büyük bir çıkış
  yarıçapından çıkınca Shifting-Out'a geçer (sınır titremesini önler). Sabit
  zamanlayıcı yok.
- **Bölge kimliği: `shiftId` (design-review, 2026-08-04 — verification
  bulgusu, açıkça belgelendi)**: Her tetikleyici bölgesi, sahnede
  yerleştirilen bölge bileşeninin kendi Inspector-atanmış `string shiftId`
  alanını taşır — `TriggerShift(shiftId, config)`/`RevertShift(shiftId)`/
  `IsShiftActive(shiftId)`'in hedeflediği kimlik budur. Bu alan önceden
  hiç açıkça belgelenmemişti (örtük olarak varsayılıyordu) — şimdi
  açıkça bir Core Rule: bu, `TriggerMode` gibi sahne-içi alanları
  `MemoryTriggerDef` asset'leriyle eşleştirmesi gereken edit-time
  validasyonun (bkz. `ani-tetikleyici-etkilesim.md` Core Rules,
  "TriggerMode kontrolü ayrı bir mekanizma gerektirir") eşleştirme
  anahtarıdır.
- **Giriş modu: `Automatic` (yarıçap-tabanlı) vs. `ManualOnly` (design-review,
  2026-08-03 — `/review-all-gdds` bulgusu, eklendi, kritik bulgu)**: Önceki
  taslak, `Dormant → Shifting-In` geçişinin nasıl tetiklendiğini iki farklı
  yerde iki farklı şekilde anlatıyordu — bu Core Rules bölümü (ve AC#2)
  bunu oyuncunun `R_trigger` içine **girmesiyle** otomatik olarak
  eşliyordu, ama Interactions with Other Systems'taki sözleşme
  `TriggerShift(shiftId, config)`'i **tek** giriş yolu olarak
  tanımlıyordu. Bu iki tanım, Anı-Tetikleyici Etkileşim'in tüm rıza
  öncülünü kırıyordu: bir memory-trigger'ın bağlı olduğu bölge, oyuncu
  `Etkileşim Sistemi`'nin 2.0m SphereCast menziline girip Hold'u
  başlatmadan **önce**, sadece `R_trigger` içine girerek otomatik olarak
  Shifting-In'e geçebilirdi — "bile bile yaptım" fantazisi, oyuncunun
  hiçbir eylemi olmadan zaten gerçekleşmiş bir olayla karşılaşırdı (bkz.
  `design/gdd/gdd-cross-review-2026-08-03.md`, en çok bağımsız geçişin
  aynı sonuca vardığı bulgulardan biri).
  **Düzeltme**: Her tetikleyici bölgesi artık açık bir `TriggerMode`
  alanı taşır — `Automatic` (varsayılan, önceki davranış: `R_trigger`
  içine girmek `TriggerShift`'i kendiliğinden çağırır — pasif/çevresel
  bölgeler için, ör. anı-tetikleyici olmayan ortam kaymaları) ya da
  `ManualOnly` (bölgenin kendi proximity/hysteresis tick'i **asla**
  `TriggerShift` çağırmaz — `Shifting-In`'e girmenin **tek** yolu dışarıdan
  gelen açık bir `TriggerShift(shiftId, config)` çağrısıdır).
  `MemoryTriggerDef`'e bağlı her bölge `ManualOnly` olmak **zorundadır** —
  bu, `ani-tetikleyici-etkilesim.md`'nin zaten kurduğu
  `IPreprocessBuildWithReport` edit-time validasyonuna (`Persistent != true`
  kontrolüyle aynı geçişte) yeni bir kontrol olarak eklenir: bir
  `MemoryTriggerDef`'in bağlı olduğu bölge `TriggerMode != ManualOnly`
  ise hata verilir, build engellenir. `ManualOnly` bölgeler için `R_trigger`
  hâlâ Inspector'da tanımlı kalabilir (gelecekte bir çıkış-histerezisi
  ihtiyacı doğarsa diye) ama giriş için hiç okunmaz — Persistent shift'ler
  zaten hiç çıkış kontrolü yapmadığından (bkz. Edge Cases), memory-trigger
  bölgeleri için `R_trigger` pratikte tamamen kullanılmaz kalır, bu
  kasıtlıdır. **Bu, stinger'ın ses-düşüş yarıçapı için ayrı bir alan
  gerektirir (design-review, 2026-08-03 — verification N1 bulgusu,
  eklendi)**: Adaptif Ses Sistemi'nin memory-trigger stinger'ı
  (`stinger_falloff` formülü, bkz. `adaptif-ses-sistemi.md` Formulas)
  önceden bu `radius`'tan türetiliyordu — yukarıdaki kural bu alanı
  memory-trigger bölgeleri için kasıtlı olarak "kullanılmaz" ilan ettikten
  sonra, ses düşüşünü hâlâ ondan türetmek kavramsal olarak tutarsız hale
  geldi (oyun-mekaniği açısından ölü bir alana ses tasarımı bağımlı
  kalıyordu). Düzeltme: bkz. aşağıdaki `StingerAudioRadius`.
- **MVP içerik gereksinimi: en az 1 `Automatic` bölge, zorunlu rota üzerinde
  (design-review, 2026-08-04 — full re-verification bulgusu, eklendi,
  kritik bulgu, kullanıcı kararıyla çözüldü)**: `TriggerMode=Automatic`
  modu (yukarıda) her zaman tanımlıydı ama hiçbir MVP içeriği ona atanmamıştı
  — MVP'nin tüm anı-tetikleyicileri zorunlu olarak `ManualOnly`, yani
  rıza-gerektiren. Sonuç: hiçbir garanti edilmiş Pillar 1 (Öznel
  Gerçeklik) anı olmadan bir oyuncu geceyi bitirebiliyordu (bkz.
  `game-concept.md` MVP Definition, madde 5 — aynı bulgunun ev sahibi
  notu). **İçerik gereksinimi**: MVP'nin 3 alanından **en az biri**, oyuncunun
  normal taşıma rotasında (kaçınılmaz biçimde geçeceği bir koridor/geçiş
  noktasında) `TriggerMode=Automatic`, `Persistent=false` (reversible —
  bu bölge kalıcı bir "final" anısı değil, sıradan bir çevresel
  tekrarlanabilir kaymadır, oyuncu istediği kadar tekrar
  tetikleyebilir/geri döndürebilir), hiçbir `MemoryTriggerDef`'e bağlı
  olmayan, hiçbir `ClueDefinition.requiredShiftIds`'te yer almayan bir
  ışık-kayması bölgesi içerir. `MemoryColor`/`shiftConfig` değerleri
  level design aşamasında seçilir (bkz. Tuning Knobs). Bu bölge,
  Anı-Tetikleyici Etkileşim'in "bile bile yaptım" rıza öncülünü hiç
  etkilemez — sadece pasif bir "otel her zaman biraz güvenilmez"
  arka planı sağlar, oyuncunun kendi kararıyla tetiklediği anılardan
  ayrı ve daha zayıf bir sinyaldir (kasıtlı — bu, oyuncu-tetiklediği
  kaymaların özel/anlamlı hissettirmesi gereken ayrımı korur).
  **Build-time doğrulama**: paylaşılan `IPreprocessBuildWithReport`
  editor utility'sine (bkz. `ani-tetikleyici-etkilesim.md` Core Rules,
  aynı mekanizma) bir kontrol daha eklenir — sahne taramasında hiçbir
  `TriggerMode=Automatic` bölge bulunmazsa build engellenir. Bu, madde 5'in
  içerik yazımı sırasında sessizce unutulmasını yapısal olarak engeller.
  **İçerik-yazım kalibrasyonu gerekiyor (design-review, 2026-08-04 —
  ikinci tur full re-verification bulgusu)**: Bu, aynı 3 küçük MVP
  alanına yerleştirilmesi gereken **üçüncü** bağımsız "X'i zorunlu rotaya
  yerleştir" gereksinimi (diğer ikisi: `birinci-sahis-kontrolcu.md`
  AC17'nin dekor/decoy nesneleri, ve 2-3 anı-tetikleyicinin kendisi).
  Hiçbir doküman üçünü birlikte değerlendirmiyor — decoy'lar kamuflaj
  için "ayırt edilemez" olmalı, bu `Automatic` bölge ise kendi başına
  görünür bir ışık kayması (kamuflajdan farklı bir sinyal türü) ve
  sadece "en az 1/3 alan" zorunluluğu, hangi alanın bunu alacağını level
  designer'a bırakıyor — bu da üç alan arasında kasıtsız bir tonal
  asimetri riski taşır (iki alan tamamen "sabit", biri "güvenilmez"
  olabilir, bu bir tasarım kararı olarak hiç işaretlenmeden). Ayrıca
  tekrar-tetiklenebilir (`Persistent=false`) olduğundan, oyuncu zorunlu
  rotayı gece boyunca (3-5 round) tekrar tekrar geçtikçe bu bölge aynı
  şekilde tekrar tekrar tetiklenip geri dönecek — bu, "kayma = önemli
  bir şey" okumasına oyuncuyu alıştırmak yerine "kaymalar önemsiz arka
  plan dokusu" alışkanlığı kazandırma riski taşır (habituation), ki bu
  gerçek anı-tetikleyicilerin etkisini zayıflatabilir. Bu üç mekanizma
  (decoy oranı, kamuflaj paylaşımı, bu bölgenin tekrar sıklığı) ayrı
  ayrı değil, tek bir level-design/tuning geçişinde birlikte
  kalibre edilmeli. Sahip: level design aşaması / `/asset-spec` sonrası
  bir tuning geçişi. Bu belge sadece gereksinimi kilitler, kesin sayı/
  sıklık/alan dağılımını değil.
- **Kalıcılık escape hatch'i**: `ShiftConfig`'te bir `Persistent` bayrağı —
  normal shift'ler geri döner, ama açıkça işaretlenmiş belirli finale-özel
  tetikleyiciler kalıcı kalabilir (oturumun geri kalanı boyunca
  Shifting-Out'u atlar). Bu, "otel yanlış kalıyor" hissini final için saklı
  tutar, oyunun geneli için görsel gürültüyü düşük tutar.
- **Yazım modeli kararı (açık soruyu çözer)**: Sadece post-process (Volume +
  ışık renk lerp'i) — baked lightmap seti değişimi **reddedildi**. İki ekip
  için, oda başına iki bake tutmak sürdürülemez bir içerik yükü. Prototip
  zaten bu yaklaşımı temiz ve ucuz olarak doğruladı; 15-20 tetikleyiciye
  ölçeklenmek tekniği değil sadece sayıyı değiştirir.
- **Light Mode zorunluluğu (kritik, motor-seviyesi kısıt)**: Bir
  `ShiftConfig` ışık dizisinde referans verilen her `Light`, Light Mode =
  **Mixed** olarak ayarlanmalıdır — **asla Baked değil**. URP'de Baked
  ışıklar gerçek-zamanlı forward pass'ten tamamen hariç tutulur; böyle bir
  ışığın rengini/yoğunluğunu çalışma zamanında değiştirmenin sahne üzerinde
  hiçbir görsel etkisi olmaz ve mekanik sessizce çalışmaz hale gelir. Bu,
  projenin baked/static-flagged modül yaklaşımıyla (bkz. game-concept.md,
  Technical Considerations) doğrudan çakışabilecek bir noktadır — sanatçılar
  tetikleyici bölge içindeki pratik ışıkları Baked olarak işaretlememelidir.
  Mixed modu shadow-casting'i otomatik olarak gerektirmez; oda başına 2-3
  gerçek-zamanlı gölgeli ışık bütçesi bu karardan bağımsız kalır. **Bilinen
  sınırlama (yükseltilmiş risk, round 2 review)**: Mixed ışıkların baked
  indirect/GI katkısı (duvar/tavan renk sıçraması) çalışma zamanında
  güncellenmez — sadece direkt ışık konisinin renk/yoğunluk değişimine
  tepki verir. Bounce-baskın, kapalı bir koridorda (bu projenin
  static-flagged modüler kitinde beklenen norm — bkz. game-concept.md,
  Technical Considerations) bu, donmuş-sıcak bir ortam ışığının yanında
  sadece soğuyan bir direkt koni anlamına gelebilir — "oda kaydı" hissi
  yerine yarım/yıkanmış bir geçiş riski taşır, ki bu da istemeden ikinci
  bir görsel kanal gibi okunabilir (bkz. Visual/Audio Requirements —
  "Ek görsel ipucu yok" kuralıyla gerilim). Bu, bir formül düzeltmesiyle
  kapatılamaz — level-authoring/art-direction disiplini gerektirir; bkz.
  `game-concept.md`'nin Technical Considerations / Risks bölümüne eklenen
  not (2026-08-01 round 2 escalation).
- **Örtüşen bölgeler ışık paylaşamaz (tasarım kısıtı, öneri değil)**: İki
  tetikleyici bölgesinin ışık dizileri kesişmemelidir (bkz. Edge Cases —
  ihlal durumunda tanımlı-ama-istenmeyen davranış). Şu an bunu otomatik
  doğrulayan bir araç yok; level designer'lar ölçek büyüdükçe (15-20
  tetikleyici) manuel çapraz kontrol yapmalı. Bir editor-doğrulama aracı
  ihtiyacı burada not edilir ama bu GDD'nin kapsamı dışıdır (implementasyon
  tooling backlog'una aittir).
- **Interpolasyon sürücüsü**: Işık başına `Update()` değil — bölge başına tek
  bir hafif coroutine/ticker, tüm ışıkları
  `(Light, baseColor, memoryColor, baseIntensity, memoryIntensity)` dizisi
  üzerinden tek bir döngüde günceller.
- **Gerçek-zamanlı gölgeli ışık sayısı** oda başına 2-3 ile sınırlı
  (kit-seviyesi bütçe, bu GDD'nin sahipliğinde değil — mevcut performans
  bütçesine referans).

### States and Transitions

Bölge başına: `Dormant` → (`TriggerShift`) → `Shifting-In` (~3s,
prototipten) → `Shifted/Held` → (`RevertShift` ya da yarıçap-çıkışı) →
`Shifting-Out` (~3s) → `Dormant`. `Persistent` shift'ler `Shifting-Out`'u
atlar, oturum boyunca `Shifted` kalır.

### Interactions with Other Systems

**Sözleşme** (Anı-Tetikleyici Etkileşim için — tasarlandı, Approved):
- `bool TriggerShift(string shiftId, ShiftConfig config)` — yeni bir geçiş
  başlattıysa `true`, `shiftId` zaten aktifse `false` döner (no-op, mevcut
  config'i değiştirmez — oyuncu zaten kaymış bir bölgeye tekrar girerse
  pop/restart olmaz)
- `void RevertShift(string shiftId)` — aktif olmayan bir `shiftId` üzerinde
  sessizce no-op
- `bool IsShiftActive(string shiftId)` — sorgu amaçlı
- `bool IsShiftPersistent(string shiftId)` **(design-review, 2026-08-03 —
  verification N2 bulgusu, eklendi)**: `shiftId`'nin en son `TriggerShift`
  çağrısında aldığı `config.Persistent`'i döner. Gece/Oturum Durumu'nun
  `PersistentShiftIds`'i doldurmak için atanmış olduğu (bkz. Dependencies)
  ama bunu değerlendirecek hiçbir yolu olmadığı N2 bulgusunu kapatır —
  `OnShiftStateChanged`'in kendisi `Persistent`'i taşımıyordu ve üç
  aboneden hiçbiri (Adaptif Ses, Anlatı Durum, Gece/Oturum) buna ihtiyaç
  duymuyordu, sadece Gece/Oturum duyuyordu; event payload'ını üç
  tüketicisi için de genişletmek yerine (gereksiz yayılma yüzeyi), tek
  ihtiyaç duyan tüketiciye dar bir sorgu eklemek tercih edildi. `TriggerShift`
  hiç çağrılmamış bir `shiftId` için sonuç tanımsızdır (çağıran, `Shifting-In`'e
  giren bir shift için bunu her zaman `TriggerShift`'in **hemen ardından**,
  aynı karede sorgulamalıdır — Gece/Oturum zaten `OnShiftStateChanged`
  event'ini bu karede alır, bkz. `gece-oturum-durumu-2026-08-02.md` Core
  Rules).
- `event OnShiftStateChanged(shiftId, newState, zoneCenter, radius)` —
  **Adaptif Ses Sistemi buna abone olur** (prototip bulgusu: ışık+ses
  bileşik etki); `zoneCenter`/`radius`, ses sistemi ikinci bir sorgu
  yapmadan ambiyans kaynağını mekansal olarak yerleştirebilsin diye eklendi
  (bkz. Visual/Audio Requirements). Bu sistem sesi doğrudan çağırmaz,
  sadece event fırlatır.
- `ShiftConfig`: hedef WB/Color Adjustments değerleri, geçiş süresi,
  `Persistent` bool, **`float StingerAudioRadius`** (design-review,
  2026-08-03 — verification N1 bulgusu, eklendi; **tip 2026-08-04
  verification bulgusuyla `float?`'tan `float`'a düzeltildi** — nullable
  tip, `GetStingerAudioRadius`'un non-nullable `float` dönüşüyle ve
  AC4b'nin "`<= 0` ya da hiç ayarlanmamış" ifadesiyle tutarsızdı; artık
  varsayılan `0` = "ayarlanmamış", `AC4b`'nin `> 0` kontrolü tek bir
  koşulla ikisini de kapsıyor) — (yarıçap/`R_trigger` dahil değil — o,
  çağıran tetikleyici volume'unun sahipliğinde)
- `float GetStingerAudioRadius(string shiftId)` **(design-review,
  2026-08-03 — verification N1 bulgusu, eklendi)**: `shiftId`'nin en son
  `TriggerShift` çağrısında aldığı `config.StingerAudioRadius`'u döner
  (varsayılan `0`, ayarlanmamışsa). Sadece `MemoryTriggerDef`'e bağlı
  (Persistent + ManualOnly) bölgeler için anlamlıdır — bu tür bölgeler
  için `StingerAudioRadius` edit-time validasyonla **zorunlu** kılınır
  (`> 0` olmalı, bkz. `ani-tetikleyici-etkilesim.md`'nin `IPreprocessBuildWithReport`
  kontrolü), çünkü bu bölgelerin kendi `R_trigger`'ı yukarıdaki kural
  gereği zaten kullanılmaz durumdadır — ses düşüşünü ölü bir gameplay
  alanından türetmek yerine ayrı, içerik-yazarının kasıtlı olarak
  belirlediği bir değer kullanılır. `TriggerShift` hiç çağrılmamış bir
  `shiftId` için sonuç tanımsızdır (bkz. `IsShiftPersistent`'in aynı
  kısıtlaması).
- Referans sayma **gerekmiyor** (hareket kilidinden farklı olarak) — her
  `shiftId` bağımsız olarak kendi çağıranına ait

## Formulas

### Shift Progress

The ShiftProgress formula is defined as:
`ShiftProgress = 3x² − 2x³, where x = clamp(ElapsedTime / Duration, 0, 1)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| Elapsed Time | ElapsedTime | float (s) | 0 to Duration | Time since Shifting-In/Out began |
| Duration | Duration | float (s) | ~3.0 | Transition length, locked per prototype |
| Normalized Time | x | float | 0 to 1 | Linear time fraction, clamped |
| Shift Progress | ShiftProgress | float | 0 to 1 | Smoothstep-eased blend factor driving Volume weight and light lerp in lockstep |

**Output Range:** 0 to 1. At x=0 and x=1 the curve's derivative is 0, so the shift begins and ends with near-zero visible motion — a soft onset and settle rather than a hard start/stop. A linear ramp (ShiftProgress = x) moves fastest at the instant of trigger, which reads as a snap; the smoothstep S-curve reads as a slow drift, matching "a whisper, not an alarm."

**Example:** Duration = 3.0s. At ElapsedTime = 0.75s, x = 0.25: ShiftProgress = 3(0.0625) − 2(0.015625) = 0.156 (vs. 0.25 for linear — 37% slower start, imperceptible). At ElapsedTime = 1.5s, x = 0.5: ShiftProgress = 0.5. Shifting-Out reuses the same curve with time reversed: `x = clamp(1 − ElapsedTime/Duration, 0, 1)`, so ShiftProgress eases 1→0 with matching gentleness.

**Durum takibi notu (implementability — systems-designer review, 2026-08-01):**
`3x² − 2x³` has no simple algebraic inverse, so the running state must be `x`
itself, never `ElapsedTime` or `ShiftProgress` alone. Each tick: `x +=
DeltaTime / Duration` while Shifting-In, `x -= DeltaTime / Duration` while
Shifting-Out (clamped to 0–1); `ShiftProgress` is recomputed fresh from the
current `x` every frame, never stored or inverted. An interrupt (`TriggerShift`
during Shifting-Out, or `RevertShift` during Shifting-In — see Edge Cases)
simply flips the sign of the per-tick `x` delta and continues from whatever
`x` currently holds. This guarantees continuity (no pop) without ever needing
to solve the cubic for `x` given a target `ShiftProgress`.

**Guard rails (degenerate input protection — systems-designer review,
2026-08-01, extended round 2 and round 3):** these must be enforced in
code, not left to designer discipline alone. **Project-wide epsilon
constants (round 3 — previously just the word "epsilon" with no number
across two rounds, a real implementer-disagreement risk on its own):**
`TIME_EPSILON = 0.01s` (for `Duration`), `RADIUS_EPSILON = 0.01m` (for
`R_trigger`), `HYSTERESIS_EPSILON = 0.001` (for `k_hysteresis`, unitless
factor).
- `Duration` must be clamped to `≥ TIME_EPSILON` (never 0) — an unclamped
  `Duration = 0` divides by zero in the `x` accumulation above.
- `k_hysteresis` must be clamped to `≥ 1.0 + HYSTERESIS_EPSILON` in code
  (design range 1.05–1.3, see Hysteresis Radius Relationship below) — **not
  just `≥ 1.0`** (round 3 correction: at exactly `k_hysteresis = 1.0`,
  `R_exit = R_trigger`, collapsing the hysteresis buffer to zero and
  reproducing the exact boundary-flicker bug hysteresis exists to prevent;
  rounds 1 and 2 only guarded against `< 1` inversion, not `= 1`
  degeneracy).
- `MemoryIntensityMultiplier` must be clamped to the range **[0.0, 1.0)** in
  code (design range 0.5–0.9, accessibility floor ≥ 0.6 — see Visual/Audio
  Requirements). The upper bound (< 1.0) prevents silently defeating the
  accessibility guarantee (no brightness drop for a colorblind player to
  read); the **lower bound (≥ 0.0)** prevents a negative value producing a
  negative `LightIntensity` — a degenerate output the original guard-rail
  pass missed (round 2 review).
- `R_trigger` must be clamped to `≥ RADIUS_EPSILON` (never ≤ 0 or
  unset/default-zero) — an unguarded `R_trigger = 0` collapses `R_exit` to
  0 too (via the Hysteresis Radius formula), producing a zone that can
  never be entered: a silent dead-zone bug in the same failure class as
  the three guards above, missed in the original guard-rail pass (round 2
  review).

### Hysteresis Radius Relationship

The ExitRadius formula is defined as:
`R_exit = R_trigger × k_hysteresis`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| Trigger Radius | R_trigger | float (m) | >0, per-zone | Designer-set entry radius for a trigger zone |
| Hysteresis Factor | k_hysteresis | float | 1.05–1.3 (default 1.15) | Proportional buffer multiplier, project-wide tunable |
| Exit Radius | R_exit | float (m) | > R_trigger | Radius the player must cross outward to leave Held state |

**Output Range:** R_exit always exceeds R_trigger by a fraction of R_trigger itself. As R_trigger shrinks toward 0 the buffer shrinks with it (no oversized exit zone swallowing a tiny closet trigger); as R_trigger grows the buffer grows proportionally, keeping the dead zone visually consistent relative to room scale. A fixed-buffer alternative (R_exit = R_trigger + C) needs per-zone hand-tuning to avoid the same problem anyway, so the proportional form removes a tuning step when placing many differently sized zones.

**Example:** Small closet trigger R_trigger = 4m, k=1.15 → R_exit = 4.6m (0.6m buffer). Large hall trigger R_trigger = 12m, same k=1.15 → R_exit = 13.8m (1.8m buffer) — buffer scales with zone size instead of needing a separate hand-tuned constant per trigger.

### Box Collider Safety Margin (spike-confirmed, 2026-08-01)

The BoxHalfExtentMin formula is defined as:
`BoxHalfExtentMin = R_exit + (PlayerMaxSpeed × Duration) + SafetyBuffer`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| Exit Radius | R_exit | float (m) | > R_trigger | From the Hysteresis Radius formula above |
| Player Max Speed | PlayerMaxSpeed | float (m/s) | 1.6 (locked) | From Birinci Şahıs Kontrolcü, see Dependencies |
| Duration | Duration | float (s) | ~3.0 | Shifting-Out length, same value as Shift Progress's Duration |
| Safety Buffer | SafetyBuffer | float (m) | ≥ 0.5, default 0.9 | Extra margin beyond the worst-case walk distance |
| Box Half-Extent Minimum | BoxHalfExtentMin | float (m) | > R_exit | Minimum half-extent for the zone's box collider along the player's approach axis |

**Output Range:** `BoxHalfExtentMin` always exceeds `R_exit` by at least `PlayerMaxSpeed × Duration` — the worst-case distance a player can keep walking away from the zone center during the full Shifting-Out decay before the effect finishes and the box no longer needs to contain them. Undersizing the box relative to this value risks the player physically exiting the collider while `ShiftProgress` is still > 0 (see Core Rules — "Volume weight kontrolü").

**Example (spike-verified):** `R_exit = 4.6m`, `PlayerMaxSpeed = 1.6 m/s`, `Duration = 3.0s`, `SafetyBuffer = 0.9m` → `BoxHalfExtentMin = 4.6 + 4.8 + 0.9 = 10.3m`. The spike's Corridor C used a 10m half-extent (slightly under this formula's output) and still produced a clean, pop-free transition — the player didn't physically exit the box until 0.39s *after* `ShiftProgress` had already reached 0 (Console log: Dormant reached at t=14.69, box exit at t=15.08). Level designers should use the full formula rather than the spike's exact number, since `Duration` and `SafetyBuffer` may be tuned per-project.

### Light Color/Intensity Blend

The LightBlend formula is defined as:
`LightColor = Lerp(BaseColor, MemoryColor, ShiftProgress)`
`LightIntensity = BaseIntensity × Lerp(1, MemoryIntensityMultiplier, ShiftProgress)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| Shift Progress | ShiftProgress | float | 0 to 1 | From the Shift Progress formula above |
| Base Color | BaseColor | Color (RGB) | project palette | Warm amber baseline, per light |
| Memory Color | MemoryColor | Color (RGB) | ShiftConfig | Cold blue/sodium-green target, per trigger |
| Base Intensity | BaseIntensity | float | light-specific | Light's default intensity |
| Memory Intensity Multiplier | MemoryIntensityMultiplier | float | ShiftConfig, typ. 0.5–0.9 | Target intensity scalar at full shift |

**Output Range:** At ShiftProgress=0, output equals BaseColor/BaseIntensity exactly (Dormant); at ShiftProgress=1, output equals MemoryColor and BaseIntensity×MemoryIntensityMultiplier (fully Shifted). Both channels share ShiftProgress, so color and intensity always move in lockstep — never a case where color has shifted but intensity hasn't, or vice versa.

**Example:** BaseColor = (255,191,128) warm amber, MemoryColor = (128,179,255) cold blue, BaseIntensity = 1.2, MemoryIntensityMultiplier = 0.6. At ShiftProgress=0.5: LightColor = (191.5, 185, 191.5) — a desaturated gray-blue midpoint. LightIntensity = 1.2 × Lerp(1, 0.6, 0.5) = 1.2 × 0.8 = 0.96.

## Edge Cases

- **Eğer oyuncu tek bir tick'te hem R_trigger hem R_exit'i atlayacak kadar
  hızlı hareket ederse**: Bölge pozisyonu tick başına bir kez örnekler.
  Tick'in sonu R_trigger içinde biterse tetikleme normal şekilde olur;
  tick tüm bölgeyi hiç dokunmadan atlarsa hiç tetiklenmez. Hikaye açısından
  kritik bölgelerde R_trigger, oyuncunun tick başına maksimum yer
  değiştirmesinden büyük tutulmalı.
- **Eğer TriggerShift(shiftId), shiftId Shifting-Out durumundayken
  çağrılırsa**: Bu "zaten aktif" no-op durumu değildir — mevcut `x`
  (normalize zaman, bkz. Formulas > Shift Progress > Durum takibi notu)
  değerinden yön tersine çevrilerek Shifting-In'e geri döner (0'dan başlamaz,
  renk/yoğunluk geriye sıçramaz). `true` döner (yeni bir geçiş başladı).
- **Eğer RevertShift(shiftId), shiftId Shifting-In durumundayken
  çağrılırsa**: Yukarıdakinin aynası — aynı süreklilik gerekçesiyle mevcut
  `x` değerinden yön tersine çevrilerek Shifting-Out'a döner, no-op olmaz.
- **Eğer IsShiftActive(shiftId), Shifting-Out sırasında sorgulanırsa**:
  `true` döner. "Aktif" Shifting-In, Held ve Shifting-Out'u kapsar — sadece
  Dormant "inaktif" sayılır. Bu, TriggerShift'in "zaten aktif → false"
  kuralını yukarıdaki iki tersine çevirme durumuyla tutarlı kılar.
- **Eğer oyuncu iki üst üste binen tetikleyici bölgesinin içinde aynı anda
  duruyorsa**: İkisi de bağımsız olarak radius/histerezisi değerlendirir ve
  ikisi de aynı anda Held olabilir, her biri paylaşılan Volume Profile'ı ve
  kendi ışık dizisini sürer. Bölge-başına bağımsızlık, hiçbir global
  hakemin bunu çözmediği anlamına gelir — bölgeler ışık paylaşıyorsa,
  hangi zone'un ticker'ı son çalışırsa o ışıklar için kazanır (sıraya
  bağımlı). Üst üste binen bölgeler ışık paylaşmamalı, tasarım gereği.
- **Eğer Persistent bir shift'in bölgesine, Persistent-Held olduktan sonra
  tekrar girilirse**: TriggerShift `false` döner (zaten aktif), yeniden
  başlamaz, tekrar OnShiftStateChanged fırlatmaz. Radius-çıkış kontrolleri
  Persistent shift'ler için hiç çalışmaz, bu yüzden tekrar giriş saf bir
  no-op'tur.
- **Eğer Held (Persistent olmayan) bir shift'in bölgesinden ışınlanarak**
  (örn. Asansör sistemi) **çıkılırsa**, normal R_exit geçişi yerine: Hiçbir
  geçiş eventi tetiklenmez, çünkü pozisyon güncellemesi süreksizdir. Bölge,
  "herhangi bir tick'te R_exit dışında bulunmayı" Shifting-Out'u
  başlatmak için yeterli saymalı, geçişi kendisinin tespit etmesini
  gerektirmemeli — aksi halde shift, oyuncu orada değilken sonsuza kadar
  Held sızıntısı yapar.
- **Eğer sahne yüklenirken oturum durumu, önceki bir oturumdan kalıcı bir
  shift'in zaten aktif olduğunu belirtiyorsa**: Bölge, Dormant'ı ve
  Shifting-In coroutine'ini tamamen atlayarak doğrudan Shifted/
  Held-Persistent'e ShiftProgress=1 uygulanmış şekilde başlamalı, ve
  Adaptif Ses Sistemi'nin senkronize olabilmesi için yükleme sonrası bir
  kez OnShiftStateChanged fırlatılmalı — aksi halde ses Dormant'ta
  başlarken görseller zaten kaymış olur.

## Dependencies

**Bağımlıdır**:
- **Gece/Oturum Durumu** (kısmi — sadece sahne-yükleme/Persistent-restore
  edge case'i için, bkz. Edge Cases). Bu sistem hâlâ Foundation katmanında
  kalır — Gece/Oturum Durumu da Foundation katmanında (systems-index.md),
  katmanlar arası bir ihlal yok. Bağımlılık önceden yanlış şekilde "Yok"
  olarak belirtilmişti; bu, sahne yüklemesinde hangi `shiftId`'lerin
  Persistent-Held olduğunu session state'in bilmesi gerektiğinden gerçek
  bir bağımlılıktır (systems-designer review, 2026-08-01 — düzeltildi).
  Işık/Volume Durum Sistemi bu bilgiyi sadece **okur**; session state'i
  kendisi yönetmez ya da yazmaz. **Yazar artık açıkça atanmış (design-review,
  2026-08-03 — `/review-all-gdds` bulgusu, düzeltildi)**: Gece/Oturum
  Durumu, `OnShiftStateChanged`'e kendisi abone olarak `PersistentShiftIds`'i
  doldurur (`Shifting-In`'de, `Held`'i beklemeden — bkz.
  `gece-oturum-durumu-2026-08-02.md` Core Rules) — önceki taslakta bu
  yazma sorumluluğu hiçbir sistemde tanımlanmamıştı, AC#17 hiçbir zaman
  gerçekten kapanamazdı.
- **Birinci Şahıs Kontrolcü** (kısmi — sadece Core Rules'taki "Tick tanımı"
  kuralının minimum güvenli R_trigger hesabı için: R_trigger_min =
  oyuncunun maksimum hızı × tick aralığı). Oyuncunun maksimum hızı
  **1.6 m/s** (yüksüz) olarak `birinci-sahis-kontrolcu.md`'de zaten
  tanımlıdır — bu GDD önceden bu değeri hiç referans vermiyor ve
  bağımlılığı listelemiyordu (level-designer review, 2026-08-01 round 2 —
  düzeltildi). Varsayılan kare-başına tick'te (60fps hedef,
  technical-preferences.md) bu, R_trigger_min ≈ 1.6 × 1/60 ≈ 2.7cm'e
  karşılık gelir — 4m'lik modül ızgarasına göre önemsiz küçük, yani bu
  guard pratikte hiçbir zaman bağlayıcı olmaz; yine de değer burada açıkça
  kayıtlı olmalı, iki ayrı belgede avlanmak yerine.

Bu iki kısmi bağımlılık da Foundation katmanı içi kalır (systems-index.md'de
her ikisi de Foundation Layer) — katmanlar arası bir ihlal yok, ama
"Foundation Layer = bağımlılık yok" tanımı systems-index.md'de net değil;
bkz. o dokümanın kendi düzeltme notu.

**Kendisine bağımlı olanlar**:
- **Anı-Tetikleyici Etkileşim** *(2026-08-03, Approved)* — `TriggerShift`/`RevertShift`/`IsShiftActive`
  çağırır
- **Adaptif Ses Sistemi** *(2026-08-02, tasarlandı; design-review 2026-08-03 devam ediyor)* — `OnShiftStateChanged` event'ine abone olur
  (ışık+ses senkronu, `Held` VE `Shifting-In` işlenir — design-review
  2026-08-03 stinger/ışık zamanlama düzeltmesiyle güncellendi);
  `IsShiftPersistent(shiftId)` ve `GetStingerAudioRadius(shiftId)`
  sorgularını çağırır (design-review 2026-08-03, N1 ve stinger/ışık
  zamanlama düzeltmeleriyle eklendi)
- **Anlatı Durum/İpucu Takibi** *(2026-08-02, tasarlandı)* — `OnShiftStateChanged`'e
  abone olur, sadece `newState==Held` geçişlerini işleyip `ClueDefinition`
  eşlemesi üzerinden ipuçlarını açığa çıkarır (bkz.
  `design/gdd/anlati-durum-ipucu-takibi.md`)
- **Hibrit Tepkisellik** (Vertical Slice, henüz tasarlanmadı) — muhtemelen
  `Persistent` bayrağını ve bölge-başına bağımsızlığı kullanacak
- **Gece/Oturum Durumu** *(design-review, 2026-08-03 — `/review-all-gdds`
  verification bulgusu, eklendi)* — `OnShiftStateChanged`'e abone olur
  (`PersistentShiftIds`'i doldurmak için, bkz. Core Rules'taki
  `PersistentShiftIds`-yazarı notu ve `gece-oturum-durumu-2026-08-02.md`
  Core Rules). **Bu, bağımlılığı iki yönlü yapar** — bu sistem
  Gece/Oturum'un `PersistentShiftIds`'ini okur (yukarıdaki "Bağımlıdır"),
  Gece/Oturum da bu sistemin event'ine abone olur. Bu bir çevrim
  (cycle) gibi görünse de, gerçek bir circular-call bağımlılığı
  DEĞİLDİR — Gece/Oturum bu sisteme hiç geri çağrı yapmaz, sadece
  event'i dinler (tek yönlü event akışı); veri okuma yönü ayrı ve
  ters yöndedir. Her ikisi de Foundation katmanında olduğundan
  (systems-index.md) katman ihlali yok, ama `systems-index.md`'nin
  "Circular Dependencies: None found — clean DAG" iddiası bu event-
  decoupled çevrimi netleştirmek için güncellenmeli (bkz. o dokümanın
  kendi düzeltme notu).

**Not (design-review, 2026-08-03 — güncellendi)**: Anı-Tetikleyici
Etkileşim ve Adaptif Ses Sistemi artık tasarlanmış ve Approved; sadece
Hibrit Tepkisellik hâlâ tasarlanmadı. Yazıldığında kendi Dependencies
bölümünde "Işık/Volume Durum Sistemi"ni listelemeli (çift yönlü
tutarlılık — bkz. `design/gdd/systems-index.md`).

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| Geçiş süresi (Duration) | 2–5 s | Ani/alarm gibi hissettirir, "fısıltı" hedefini bozar | Fark edilmesi çok uzun sürer, oyuncu kaçırabilir | Formül: Shift Progress |
| Histerezis faktörü (k_hysteresis) | 1.05–1.3 | Sınırda titreme geri döner | Çıkış bölgesi gereksiz büyür, "yapışkan" hissettirir | Formül: Hysteresis Radius |
| Anı yoğunluk çarpanı (MemoryIntensityMultiplier) | 0.5–0.9 | Oda neredeyse karanlığa gömülür, okunaksız | Işık değişimi fark edilmez | Formül: Light Blend |
| Tetikleyici yarıçapı (R_trigger) | Bölge başına, tasarımcı ayarlı | Tetiklemek zor, kaçırılabilir | Çok geniş alanı "bulaştırır", nesne-özgüllüğünü kaybeder | Formül: Hysteresis Radius |
| Anı rengi (MemoryColor) | Proje paleti (soğuk mavi/sodyum-yeşil) | Amber'dan ayırt edilemez | Psikiyatri ofisi paletiyle karışır (bkz. art-director notu, game-concept.md) | Formül: Light Blend |

## Visual/Audio Requirements

**Kilitli renk değerleri** (paylaşılan Volume Profile — prototipten
resmileştirildi): White Balance Temperature -60, Tint +10; Color
Adjustments Post Exposure -0.5, Saturation -20. `MemoryColor` (per-light
lerp hedefi): birincil soğuk mavi RGB(128,179,255), alternatif sodyum-yeşil
RGB(150,204,153) — level designer, tetikleyicinin "endüstriyel/yapay anı"
mı yoksa "duygusal/soğuk anı" mı hissettirmesi gerektiğine göre seçer;
ikisi de aynı Saturation -20 desatürasyonunu paylaşır, hiçbir zaman tam
doygun olmaz.

**Ek görsel ipucu yok**: Sis yoğunluğu, parçacık ya da geometri değişimi
kapsam dışı — "gerçekçi geometri, öznel ışık" kuralını korumak için; böyle
bir ipucu ikinci bir distorsiyon kanalı gibi okunup "sadece ışık yalan
söyler" kuralını sulandırır. Gölge sertliği zaten psikiyatri ofisinden
ayrım için mevcut bir ipucu (game-concept.md'de kilitli), burada yeni bir
şey gerekmiyor.

**Event genişlemesi**: `OnShiftStateChanged` artık
`(shiftId, newState, zoneCenter, radius)` taşır — Adaptif Ses Sistemi'nin
`zoneCenter`'ı ikinci bir sorgu yapmadan ambiyans kaynağını mekansal
olarak yerleştirebilmesi için (bkz. Interactions with Other Systems —
imza güncellendi). **Not (design-review, 2026-08-04 — verification
bulgusu, düzeltildi)**: "ikinci bir sorgu yapmadan" ifadesi artık sadece
`zoneCenter` için geçerli — Adaptif Ses, N1 ve N2 düzeltmeleriyle
`GetStingerAudioRadius(shiftId)` ve `IsShiftPersistent(shiftId)`
sorgularını da senkron olarak çağırıyor (bkz. Interactions with Other
Systems). `radius` parametresinin kendisi artık sadece `Automatic`
bölgeler için anlamlı — memory-trigger (`ManualOnly`) bölgelerinde
kasıtlı olarak kullanılmıyor (bkz. yukarıdaki `TriggerMode` notu).

**Erişilebilirlik**: `MemoryIntensityMultiplier` varsayılanı ≥0.6 olmalı
(Tuning Knobs aralığının alt sınırı 0.5 değil) — renk körü bir oyuncu için
bile parlaklık düşüşü tek başına okunabilir olsun. Formül zaten renk ve
yoğunluğu kilitli adımda hareket ettirdiği için ayrı bir erişilebilirlik
sistemi gerekmiyor, bu sadece varsayılan değer seçimi.

## UI Requirements

Bu sistemin kendi UI'ı yok — saf render/durum sistemi. Erişilebilirlik
ayarları (varsa gelecekte bir "azaltılmış görsel efekt" seçeneği) Ayarlar
menüsüne ait olacak, bu GDD'nin kapsamında değil.

## Acceptance Criteria

**Bölge Bağımsızlığı**

1. **GIVEN** üst üste binmeyen iki tetikleyici bölgesi A ve B, ikisi de
   Dormant, **WHEN** oyuncu aynı oturum içinde ikisinin de tetikleyici
   yarıçapında bulunur (aralarında hareket ederek), **THEN** her ikisi de
   aynı anda Shifted/Held durumuna ulaşır; `IsShiftActive(A)` ve
   `IsShiftActive(B)` her ikisi de bağımsız olarak `true` döner, ve
   `OnShiftStateChanged` her bölge için kendi geçişinde fırlar — hiçbirinin
   eventi diğerinin geçişi tarafından bastırılmaz ya da geciktirilmez.
   *(qa-lead review, 2026-08-01: orijinal ifade "hiçbir paylaşılan durum
   diğerini bloklamaz" ölçülemez bir negatif iddiaydı, somutlaştırıldı.)*

**Yarıçap-Tabanlı Tutma/Geri Dönüş, Histerezisli**

2. **[Sadece `TriggerMode=Automatic` bölgeler için — design-review,
   2026-08-03 eklendi]** **GIVEN** R_trigger=4m, k_hysteresis=1.15
   (R_exit=4.6m) olan `Automatic` modda bir bölge, Dormant, **WHEN**
   oyuncu 4m içine girer, **THEN** bölge Shifting-In'e sonra Held'e
   geçer, ve oyuncu 4.6m içinde kaldığı sürece (4m-4.6m arası dahil)
   Held kalır.
2a. **[design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi,
   kritik bulgu]** **GIVEN** `TriggerMode=ManualOnly` bir bölge, Dormant,
   **WHEN** oyuncu `R_trigger` içine girer (hatta içinde durur), **THEN**
   `TriggerShift` **hiç** çağrılmaz, bölge `Dormant`'ta kalır — sadece
   dışarıdan açık bir `TriggerShift(shiftId, config)` çağrısı `Shifting-In`'e
   geçirebilir. Bu, Anı-Tetikleyici Etkileşim'in her bölgesinin
   `ManualOnly` olmasını zorunlu kılan edit-time validasyonun doğruladığı
   davranıştır.
3a. **GIVEN** aynı bölge Held durumunda, **WHEN** oyuncu 4.6m'yi dışarı
    doğru geçer, **THEN** bölge Shifting-Out'a geçer.
3b. **GIVEN** aynı bölge Held durumunda, **WHEN** oyuncu sadece 4.3m'ye
    (R_exit=4.6m içinde) çıkar ve tekrar içeri döner, **THEN**
    Shifting-Out hiçbir noktada tetiklenmemiş olmalı, bölge kesintisiz
    Held'de kalmaya devam eder.

**Persistent Bayrağı**

4. **GIVEN** `Persistent=true` olan bir bölge, Held, **WHEN** oyuncu
   R_exit'i geçer, **THEN** bölge Shifting-Out'a girmez, oturumun geri
   kalanı boyunca Shifted kalır.
5. **GIVEN** zaten Held-Persistent olan bir Persistent bölge, **WHEN**
   oyuncu tetikleyici yarıçapına tekrar girer, **THEN** `TriggerShift`
   `false` döner, yeniden başlama olmaz, `OnShiftStateChanged` tekrar
   fırlamaz.

**TriggerShift/RevertShift/IsShiftActive Sözleşmesi**

6. **GIVEN** `shiftId` zaten aktif (Shifting-In, Held ya da Shifting-Out)
   ve bölge Shifting-Out'ta DEĞİL, **WHEN** `TriggerShift(shiftId, config)`
   tekrar çağrılır, **THEN** `false` döner, mevcut config/progress
   değişmez (yeniden başlama, sıçrama yok).
7. **GIVEN** `shiftId`, ShiftProgress=0.4'te Shifting-Out'ta, **WHEN**
   `TriggerShift(shiftId, config)` çağrılır, **THEN** `true` döner, bölge
   mevcut `x` değerinden yön tersine çevrilerek Shifting-In'e geçer (0'a
   sıfırlanmaz), görünür renk/yoğunluk sıçraması olmaz.
8. **GIVEN** `shiftId`, ShiftProgress=0.6'da Shifting-In'de, **WHEN**
   `RevertShift(shiftId)` çağrılır, **THEN** bölge mevcut `x` değerinden
   yön tersine çevrilerek Shifting-Out'a geçer, sıçrama olmaz.
9. **GIVEN** `shiftId` şu an aktif değil, **WHEN** `RevertShift(shiftId)`
   çağrılır, **THEN** sessizce no-op olur (hata yok, event fırlamaz).
10. **GIVEN** `shiftId` Shifting-Out'ta, **WHEN** `IsShiftActive(shiftId)`
    sorgulanır, **THEN** `true` döner (sadece Dormant inaktif sayılır).
11. **GIVEN** `shiftId`, birden fazla farklı çağıran tarafından
    `TriggerShift` ile tetiklenmeye çalışılmış olsa bile (AC6 gereği
    ikinci ve sonraki çağrılar `false` döner, no-op), **WHEN** herhangi bir
    çağıran `RevertShift(shiftId)` çağırır, **THEN** bölge tek bir çağrıyla
    tam olarak Shifting-Out'a geçer — referans sayımı yoktur, kaç farklı
    çağıranın daha önce `TriggerShift` çağırdığından bağımsız olarak ilk
    `RevertShift` çağrısı geri dönüşü başlatır. *(qa-lead review,
    2026-08-01: Core Rules'taki "referans sayma gerekmiyor" kuralı önceden
    hiçbir AC ile test edilmiyordu.)*

**Formüller**

12. **GIVEN** Duration=3.0s, **WHEN** ElapsedTime=0.75s (x=0.25), **THEN**
    ShiftProgress = 3(0.25)² − 2(0.25)³ = 0.15625 (±0.001) — x=0 ve x=1'de
    değişim oranının neredeyse sıfır olduğunu, lineer eğriye kıyasla
    doğrular.
13. **GIVEN** BaseColor=(255,191,128), MemoryColor=(128,179,255),
    BaseIntensity=1.2, MemoryIntensityMultiplier=0.6, **WHEN**
    ShiftProgress=0.5, **THEN** LightColor=(191.5,185,191.5) (±1) ve
    LightIntensity=0.96 (±0.01) — ikisi de aynı karede aynı ShiftProgress
    değerinden hesaplanır (biri güncellenip diğeri güncellenmeyen bir kare
    olmaz).
14. **GIVEN** `Duration` 0'a ya da `k_hysteresis` 1.0'a (ya da altına)
    ayarlanmaya çalışılır, **THEN** sistem bunları sırasıyla
    `TIME_EPSILON`'a ve `1.0 + HYSTERESIS_EPSILON`'a clamp'ler — çalışma
    zamanında sıfıra bölme, ters histerezis tamponu (R_exit < R_trigger),
    **ve** sıfır-genişlikte histerezis tamponu (R_exit = R_trigger, k=1.0
    tam sınır durumu) hiçbir zaman oluşmaz. *(systems-designer review,
    2026-08-01 round 1: Formulas'taki guard-rail notu önceden hiçbir AC ile
    test edilmiyordu. Round 3 düzeltmesi: k_hysteresis=1.0 tam sınır
    durumu, önceki `≥1.0` guard'ıyla hâlâ geçerdi — bu, iki önceki turun
    kaçırdığı ayrı bir dejenere durumdu.)*
14a. **GIVEN** `MemoryIntensityMultiplier` negatif bir değere (ör. -0.5)
    ayarlanmaya çalışılır, **THEN** sistem bunu [0.0, 1.0) aralığına
    clamp'ler — negatif `LightIntensity` çıktısı hiçbir zaman oluşmaz.
    *(qa-lead + systems-designer, round 2-3: bu guard AC14'te test
    edilmiyordu; round 3'te qa-lead, MemoryIntensityMultiplier ve R_trigger
    guard'larının nedensel olarak ilgisiz olduğunu ve tek bir AC'ye
    bindirilmemesi gerektiğini belirtti — AC3'ün 3a/3b'ye bölünmesiyle aynı
    ilke, ayrıldı.)*
14a2. **GIVEN** `R_trigger` 0'a ya da negatife ayarlanmaya çalışılır,
    **THEN** sistem bunu bir minimum pozitif epsilon'a (**0.01m**, proje
    genelinde sabit — bkz. Guard rails, Formulas) clamp'ler — girilemez
    ("ölü") bir tetikleyici bölgesi hiçbir zaman oluşmaz. *(qa-lead +
    systems-designer, round 2-3: 14a'dan ayrıldı; epsilon değeri üç turdur
    sadece kelime olarak geçiyordu, somut bir sayı yoktu — round 3'te
    sistem genelinde 0.01m/0.001 (birime göre) olarak sabitlendi.)*
14b. **[BEKLEMEDE — spike sonucuna göre son hali yazılacak, round 3]**
    **GIVEN** bir `ShiftConfig` asset'i oluşturulduğunda ya da
    Inspector'da düzenlendiğinde (`OnValidate`), **WHEN** referans verilen
    herhangi bir `Light`'ın Light Mode'u **Baked** ise, **THEN** Console'da
    o ışığı adıyla belirten bir uyarı görünmeli. **Bilinen sınırlama**: bu
    sadece `ShiftConfig`'in kendisi düzenlendiğinde tetiklenen bir
    edit-time kontroldür — ışığın modu `ShiftConfig` son doğrulandıktan
    SONRA başka bir yerde değiştirilirse bu AC onu yakalamaz (bu sınırlama
    `prototypes/yankilar-volume-weight-spike/`'ın 3. sorusuyla ampirik
    olarak doğrulanacak). *(game-designer, round 3: önceki hali bir
    edit-time mekanizmayı ("OnValidate") bir çalışma-zamanı olayına
    ("bölge Shifting-In'e geçtiğinde") bağlıyordu — OnValidate Play modu
    geçişlerinde tetiklenmez, bu iç tutarsızlık düzeltildi. qa-lead, round
    3: THEN cümlesi artık somut bir mekanizma ve sınırlama belirtiyor,
    dokümandaki diğer AC'lerin standardına daha yakın — ama build'i
    engelleyip engellemediği hâlâ açık, implementasyon aşamasında karar
    verilmeli.)*
14c-pre. **GIVEN** bir tetikleyici bölgesinin `zoneCenter` alanı açıkça
    ayarlanmamış, **WHEN** bölge Awake/OnValidate'te başlatılır, **THEN**
    `zoneCenter`, o bölgenin kutu collider'ının world-space
    `bounds.center` değerine otomatik olarak eşitlenir — hiçbir zaman
    `Vector3.zero`'da ya da tanımsız kalmaz. *(qa-lead, round 3: bu kural
    Core Rules'a round 2'de eklendi ama hiçbir AC ile test edilmiyordu —
    aynı desen, round 2'nin kendisinin round 1'deki benzer boşlukları
    kapatma çabasına rağmen tekrarlandı.)*
14c. **[SPIKE İLE DOĞRULANDI — 2026-08-01, Corridor C]** **GIVEN** bir
    tetikleyici bölgesinin kutu collider'ı Box Collider Safety Margin
    formülüne göre (ya da daha büyük) boyutlandırılmış ve `blendDistance=0`,
    **WHEN** bölge Held'den Shifting-Out'a geçip Dormant'a ulaşır, **THEN**
    oyuncu, `ShiftProgress` 0'a ulaşana kadar collider'ın fiziksel sınırı
    içinde kalır — collider'dan gerçek çıkış ancak weight zaten 0'a
    ulaştıktan sonra gerçekleşir, görsel bir sıçrama/kesilme olmaz.
    Doğrulama kaydı: box half-extent=10m, R_exit=4.6m, Duration=3.0s;
    Shifting-Out t=11.69'da başladı, Dormant'a (weight=0) t=14.69'da
    ulaştı (tam 3.00s, formülle eşleşiyor); collider'dan fiziksel çıkış
    t=15.08'de, yani weight zaten 0 olduktan 0.39s sonra. *(qa-lead +
    unity-specialist, round 2-3: önceki "Blend Distance = 0" ve "direkt
    yazma her şeyi ezer" iddiaları sırayla yanlış bulundu; round 3 sonrası
    kullanıcı `prototypes/yankilar-volume-weight-spike/` Corridor C'yi
    çalıştırıp doğruladı — Corridor A/B (yetersiz boyutlu kutu senaryoları)
    koşulmadı, bu yüzden iç mekanizma iddiası hâlâ yapılmıyor, sadece
    doğru boyutlandırılmış kutunun gözlemlenen sonucu.)*
14c2. **GIVEN** R_exit=4.6m, PlayerMaxSpeed=1.6 m/s, Duration=3.0s,
    SafetyBuffer=0.9m, **WHEN** Box Collider Safety Margin formülü
    hesaplanır, **THEN** BoxHalfExtentMin = 4.6 + (1.6×3.0) + 0.9 = 10.3m
    (±0.01) — spike'ın Corridor C'de kullandığı 10m değerine yakın ve onu
    hafifçe aşıyor, ki bu formülün spike sonucuyla tutarlı olduğunu
    doğrular (spike'ın 10m'si formülün önerdiğinden biraz dar olmasına
    rağmen hâlâ temiz sonuç verdi — 10.3m önerisi ek güvenlik payı sağlar).

**Event/Ses Senkronu**

15. **GIVEN** `OnShiftStateChanged`'e kayıtlı bir basit mock abone
    (Adaptif Ses Sistemi'ne ihtiyaç yok), **WHEN** herhangi bir bölge
    herhangi iki durum arasında geçiş yapar, **THEN** event tam olarak bir
    kez, doğru `shiftId`, `newState`, `zoneCenter` ve `radius` ile fırlar
    — `radius` alanı her bölgenin kendi yapılandırılmış `R_trigger`'ıyla
    eşleşir (test bunu doğrular; `ManualOnly` bölgeler için bu değerin
    pratikte hiçbir tüketicisi olmaması ayrı bir konudur, bkz.
    `GetStingerAudioRadius` ve yukarıdaki "Event genişlemesi" notu —
    bu AC sadece event'in kendisinin doğru veri taşıdığını test eder,
    o verinin her bölge tipinde kullanılıp kullanılmadığını değil).
    **[BUGÜN TEST EDİLEBİLİR — event contract'ının kendisi Adaptif Ses
    Sistemi'ne bağımlı değil; qa-lead review 2026-08-01, önceden bu AC
    audio-entegrasyonuyla birlikte yanlışlıkla ERTELENDİ olarak
    etiketlenmişti]**
16. **GIVEN** Adaptif Ses Sistemi tasarlanıp entegre edildikten sonra,
    **WHEN** bir bölge durum değiştirir, **THEN** ses sistemi
    `zoneCenter`/`radius`'ı kullanarak ambiyans kaynağını ikinci bir sorgu
    yapmadan doğru mekansal konuma yerleştirir. **[ERTELENDİ — tam Adaptif
    Ses Sistemi entegrasyonu o GDD'yi gerektirir, henüz tasarlanmadı; bkz.
    Blocked Acceptance Criteria aşağıda]**
17. **GIVEN** sahne yüklemesinde oturum durumu önceki bir oturumdan kalıcı
    bir shift'in zaten aktif olduğunu belirtiyor, **WHEN** bölge
    başlatılır, **THEN** doğrudan Shifted/Held-Persistent'e
    ShiftProgress=1 ile başlar (Dormant ve Shifting-In atlanır), ve
    `OnShiftStateChanged` yükleme sonrası tam olarak bir kez fırlar.
    **[ERTELENDİ — tam doğrulama için Gece/Oturum Durumu sistemi
    entegrasyonu gerekir; bkz. Blocked Acceptance Criteria aşağıda]**

**Süreksiz Çıkış**

18. **GIVEN** Persistent olmayan Held bir bölge, **WHEN** oyuncu tick'ler
    arasında R_exit dışına ışınlanır (örn. asansör), **THEN** bir sonraki
    pozisyon-örnekleme tick'i "R_exit içinde değil" tespit eder ve normal
    şekilde Shifting-Out'u başlatır — kalıcı Held sızıntısı olmaz.
19. **GIVEN** R_trigger, oyuncunun tick başına maksimum yer
    değiştirmesinden küçük ayarlanmış (yanlış yapılandırma), **WHEN**
    oyuncu bölgeyi tek bir tick'te tamamen atlayacak hızda hareket
    ederse, **THEN** bölge hiç tetiklenmez ve hiçbir event fırlamaz — bu
    bir hata değil, belgelenmiş bir yapılandırma kısıtıdır (bkz. Edge
    Cases ve Core Rules — "Tick" tanımı). *(qa-lead review, 2026-08-01:
    bu edge case önceden hiçbir numaralı AC ile test edilmiyordu.)*

**Bölge Örtüşmesi ve Işık Paylaşımı**

20. **GIVEN** üst üste binen iki tetikleyici bölgesi tasarım kuralına
    aykırı şekilde aynı `Light`'ı ışık dizilerinde paylaşıyor, **WHEN**
    her iki bölge de aynı anda Held/Shifting durumdaysa, **THEN** o ışık
    için hangi bölgenin ticker'ı en son çalışırsa onun hedef renk/
    yoğunluğu kazanır (sıraya bağımlı, tanımlı bir davranış — çökme ya da
    sonsuz flicker-loop olmaz); bu durum yine de bir tasarım ihlalidir ve
    level design aşamasında önlenmelidir (bkz. Core Rules). *(qa-lead
    review, 2026-08-01: örtüşen-bölgeler-ışık-paylaşamaz kuralının kendisi
    önceden hiçbir AC ile test edilmiyordu, sadece ihlal durumundaki
    fallback davranışı anlatılıyordu.)*

**MVP İçerik Gereksinimi: Garantili Pillar 1 Anı**

21. **[design-review, 2026-08-04 — full re-verification bulgusu,
    kullanıcı kararıyla eklendi]** **GIVEN** MVP'nin üç alanı (Depo,
    Servis Koridoru, Balo Salonu), **WHEN** bir edit-time/build-time
    içerik kontrolü her sahneyi tarar, **THEN** en az bir sahnede en az
    bir `TriggerMode=Automatic` bölge bulunur — kontrol eksikse hata
    verir, build engellenir (bkz. Core Rules, "MVP içerik gereksinimi:
    en az 1 `Automatic` bölge"). Bu, `game-concept.md` MVP Definition
    madde 5'in içerik-yazımı sırasında sessizce unutulmasını yapısal
    olarak engeller — `birinci-sahis-kontrolcu.md` AC17'nin decoy-nesne
    kontrolüyle aynı sınıf/aynı mekanizma.
22. **[design-review, 2026-08-04 — full re-verification bulgusu,
    eklendi]** **GIVEN** yukarıdaki `Automatic` bölge, **WHEN** hiçbir
    `ClueDefinition.requiredShiftIds`'in bu bölgenin `shiftId`'sini
    listeleyip listelemediği kontrol edilir, **THEN** listelenmez — bu
    bölge bir ipucu taşımaz, sadece pasif bir çevresel sinyaldir; aksi
    halde `TriggerMode=ManualOnly` zorunluluğunu (bkz. Core Rules ve
    `ani-tetikleyici-etkilesim.md`'nin edit-time validasyonu) atlamış bir
    ipucu-taşıyan Automatic bölge, rıza öncülünü dolaylı olarak kırardı.

### Blocked Acceptance Criteria (Deferred)

| AC | Blocked By | Closure Trigger | Owner |
|---|---|---|---|
| 16 (Event/Ses Senkronu — tam entegrasyon) | **Kapanıyor (design-review, 2026-08-04 — verification bulgusuyla güncellendi)**: Adaptif Ses Sistemi'nin `zoneCenter` tüketimi kendi AC1/AC2'siyle (ambiyans crossfade) test ediliyor; stinger'ın asıl senkron tüketimi artık **AC6c**'dir (`Shifting-In` karesinde `zoneCenter` + `GetStingerAudioRadius` kullanımı) — önceki hali hâlâ eski `AC6/6a/6b`'yi ve `Held`'i referans veriyordu, bunlar N2/N1/stinger-zamanlama düzeltmeleriyle güncel değildi | Adaptif Ses Sistemi'nin design-review'i Approved işaretlendiğinde bu AC de kapalı sayılabilir | Adaptif Ses Sistemi GDD yazarı (systems-index.md sırasına göre) |
| 17 (Session yüklemede Persistent restore) | **Mekanizma kapandı (design-review, 2026-08-03 — verification N2 çözüldü)**: Işık/Volume'a `IsShiftPersistent(shiftId)` sorgusu eklendi (bkz. Interactions with Other Systems), Gece/Oturum Durumu bunu `OnShiftStateChanged(Shifting-In)` aldığı karede çağırıyor (bkz. `gece-oturum-durumu-2026-08-02.md` Core Rules) — yazar artık kendi Core Rule'unu yapısal olarak yerine getirebiliyor. Hâlâ ERTELENMİŞ kalıyor çünkü bu AC'nin kendisi (bölge, sahne yüklemede doğrudan Held-Persistent'e başlar) gerçek bir sahne-yeniden-yükleme entegrasyon testi gerektiriyor, sadece mekanizmanın var olması değil | Gece/Oturum + Işık/Volume entegrasyonunun gerçek bir sahne-yeniden-yükleme senaryosunda (implementasyon/test aşamasında) doğrulanması | Işık/Volume Durum Sistemi + Gece/Oturum Durumu (birlikte, implementasyon/test aşamasında doğrulanacak) |

*(qa-lead review, 2026-08-01 round 1: deferred AC'ler artık onaylanmış bir
GDD'nin Acceptance Criteria listesinde test edilebilir maddelerle aynı
görünmüyor — bu tabloda ayrı takip ediliyor. Round 2 güncellemesi: tabloya
somut bir kapanış tetikleyicisi ve sahip eklendi — önceki halinde sadece
"karşılıklı listelenmeli" deniyordu, bu da bir sahip ya da doğrulama adımı
olmayan bir TODO'dan farksızdı.)*

## Open Questions

- **MemoryColor seçim kriteri belirsiz**: Level designer hangi tetikleyicide
  mavi, hangisinde sodyum-yeşil kullanacağını nasıl karar verecek? Sahip:
  `/map-systems` sonrası level tasarımı aşaması.
- **→ game-concept.md'ye not**: Bu GDD, konsept dokümanındaki "ışık-durumu
  yazma modeli" açık sorusunu **çözdü** — sadece post-process, baked
  lightmap seti yok. game-concept.md'nin Open Questions bölümü buna göre
  güncellenmeli (yapıldı, bkz. game-concept.md).
- **CD-GDD-ALIGN notu — çoklu bölge görünürlüğü vs. Pillar 5**: Oyuncu
  birden fazla Shifted bölgeyi aynı anda görebilir/aralarında gidip
  gelebilir — bu, FPC'nin yaklaşma-yavaşlaması kamuflajının çözmediği bir
  sinyal (görsel yoğunluk ipucu), tetikleyici kümelerini haritalamayı
  kolaylaştırabilir. Sistem yeniden tasarımı gerektirmiyor — level design
  aşamasında bölgeler arası sightline/mesafe disiplini olarak ele
  alınmalı. Sahip: level design/`/map-systems` sonrası.
- **CD-GDD-ALIGN notu — Persistent birikme riski**: Finale yakın eşzamanlı
  aktif/görünür Persistent shift sayısına dair bir üst sınır tanımlanmadı
  — birikme riski, bu bayrağın önlemeye çalıştığı "sessiz" hedefini tersine
  çevirebilir. Sahip: final sekansı GDD'si (Plot Twist/Final Sekansı,
  Full Vision).
