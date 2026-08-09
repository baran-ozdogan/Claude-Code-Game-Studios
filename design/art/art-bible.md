# Art Bible: Yankılar (Echoes)

## Document Status
- **Version**: 0.1 (in progress)
- **Last Updated**: 2026-08-05
- **Owned By**: art-director
- **Status**: All 9 sections complete. **Art Director Sign-Off (AD-ART-BIBLE)**: CONCERNS (revised) 2026-08-05 — 2 precision fixes applied (not redesigns): (1) Section 2.4/psychiatry-scene camera composition walked back from an invented "kilitli two-shot" lock to a provisional "duyulur, illa görülmez" (heard, not necessarily seen) framing — the prior wording contradicted Section 5.1's "no visible player body in MVP" with no carve-out, and no GDD actually locked a two-shot; (2) Section 2.2 gained an explicit mood sub-note for the MVP-mandatory `TriggerMode=Automatic` passive/non-consensual shift zone, previously unaddressed since the whole section assumed Hold-based, player-initiated triggers. Both fixes cross-referenced into Section 5's Open Questions and the two other "two-shot" mentions (Section 4.5, Section 9.3) for consistency.

## Source Documents
- `design/gdd/game-concept.md` — Visual Identity Anchor, Game Pillars, Anti-Pillars, Inspiration and References
- `design/gdd/isik-volume-durum-sistemi.md` — locked lighting/color values (already-validated, must not be contradicted here)
- `.claude/docs/technical-preferences.md` — engine (Unity 6.3 LTS + URP), performance budgets

---

## 1. Visual Identity Statement

### Görsel Kural

> **Mekân asla yalan söylemez; yalnızca ışık yalan söyler — ve o yalan, ancak sesle birleştiğinde gerçek bir tehdide dönüşür.**

Bir sanatçı bu belgenin başka hiçbir satırını okumamış olsa bile bu cümleyle çalışabilmeli: geometriye dokunma, ışığa dokun; ve dokunduğun ışığı asla yalnız bırakma. ("Otel Senin Yerine Hatırlıyor" ana yönünün ve "hiçbir şey yeniden dekore edilmez" kuralının üretim diline çevrilmiş hali budur — bkz. `game-concept.md` Visual Identity Anchor.)

### Destekleyici Prensipler

**1. Gerçekçi geometri, öznel ışık** — *Pillar 3 (Görev Gerçekliği) birincil, Pillar 1 (Öznel Gerçeklik) ikincil*

Koridorlar, balo salonu, depo — otelin iskeleti sabit ve gerçekçi kalır; hiçbir anı-tetikleyici an yeni bir duvar, yeni bir mesh ya da imkânsız bir geometriyle çözülmez.

*Design test*: Bir anı anının etkisini artırmak için geometri/mesh değişikliği mi, yoksa ışık/renk sıcaklığı kayması mı gerekiyor tartışılırsa — bu prensip her zaman ikincisini seçer. Geometri değiştiği an, oyuncu artık "kendi zihnimde miyim" diye sormaz, "nerede olduğumu bilmiyorum" diye sorar — bu, Pillar 3'ün gerçek iş zeminini kırar.

**2. Görsel distorsiyon asla yalnız çalışmaz** — *Pillar 2 (Sessiz Gerilim, Şok Değil)*

Prototip bulgusu net: ışık/renk kayması tek başına rahatsız edicilik üretmiyor (bkz. `prototypes/yankilar-lighting-concept/REPORT.md`). Bu yüzden hiçbir ışık/renk distorsiyonu üretime tek başına, sessiz bir katman olarak girmez.

*Design test*: Bir anı-tetikleyici an sadece görsel bir efektle mi verilsin, yoksa eşleştirilmiş bir ses katmanıyla mı tartışılırsa — bu prensip ikincisini seçer; sesi olmayan bir ışık kayması onaylanmaz, prodüksiyona alınmaz.

**3. Sıcaklık bir bağ sinyalidir, güvenlik sinyali değil** — *Pillar 4 (Bağ, Güvenlik Değil)*

Amber, bu oyunda "iyisin" demez, "yalnız değilsin" der. Arkadaş karakterinin bulunduğu alanlar/anlar sıcak ışıkla işaretlenebilir ama bu sıcaklık hiçbir zaman tehditten tam arınmış bir "güvenli oda" hissi üretecek kadar yumuşatılmaz.

*Design test*: Arkadaşla paylaşılan bir sahne, oyuncuyu bir sonraki anı-tetikleyiciden tamamen koruyacak kadar rahatlatıcı/güvenli bir ışıkla ödüllendirilsin mi tartışılırsa — hayır der; bu, Pillar 4'ü doğrudan ihlal eder. (Not: MVP'de bu prensibi tetikleyecek bir sistem yok — arkadaş karakteri Vertical Slice kapsamında — ama bu ilke şimdiden sabitleniyor ki o iş başladığında sanat ekibi geriye dönüp palet felsefesini yeniden tartışmasın.)

---

## 2. Mood & Atmosphere

Bölüm 1'in kuralı burada dört somut duruma çevriliyor. Otelin dört hali var —
gerçeklik, anı, ölü zaman, muayene — ve her biri farklı bir *duygu*
taşımak zorunda, çünkü oyuncunun "hangi katmandayım" sorusuna verdiği tek
cevap budur. Aşağıdaki dört durum, `isik-volume-durum-sistemi.md`'nin
kilitli teknik değerlerini değiştirmiyor; onları bir sanatçının elinde
nasıl hissettirmesi gerektiğine çeviriyor.

**Önemli çerçeve notu**: Bu dört durum bu oyunun MDA "state" haritasıdır —
combat/victory/defeat yok, çünkü Anti-Pillar zaten "NOT kombat sistemi"
diyor. Bir sanatçı bu bölümü "boss fight lighting" ya da "victory fanfare"
gibi kalıplarla düşünmemeli; buradaki dört durumun hepsi *aynı gecenin*
içindeki emek, sızıntı, boşluk ve yeniden çerçeveleme anlarıdır.

---

### 2.1 Temel Otel Keşfi / Emek (Depo, Servis Koridoru, Balo Salonu)

| | |
|---|---|
| **Duygu hedefi** | **Kanıksanmış tetiktelik.** Bu rahatlık değil — bir bedenin o kadar uzun süre tetikte kalmayı öğrenmiş olması ki artık bu duruş "normal" hissettiriyor. Oyuncu güvende değil, sadece *alışkın*. Amber sıcaklık burada "iyisin" demez (Prensip 3), "bu iş, bu saat, bu koridor tanıdık" der. |
| **Işık karakteri** | Sıcak amber temel (`BaseColor` ailesi, ~(255,191,128) civarı — kilitli LightBlend formülünün Dormant ucu). Orta-düşük kontrast: gece vardiyası pratik/servis aydınlatması, dekoratif değil işlevsel — depo lambaları, koridor apliği, balo salonu hazırlık ışığı hâlâ yanık ama "sahne" aydınlatması değil. Zaman hissi: gecenin en derin saati, otelin geri kalanı uyuyor, sadece çalışılan hacimler aydınlık. |
| **Atmosferik sıfatlar** | işlevsel, tanıdık, yorgun, sessiz, hafif ağır (havada asılı kalan bir yorgunluk) |
| **Enerji seviyesi** | **Meditatif ama huzursuz** (`game-concept.md` Core Loop'un kendi tanımı — burada tekrar sabitleniyor: ne rahat ne gergin, ikisinin ortasında asılı bir tempo). |
| **Işık-dışı görsel eşlikçi** | **Set dekoru: emeğin gerçek dağınıklığı.** İstiflenmiş kutular, yarım bırakılmış masa düzenleri, katlanmış örtüler, kullanılmış servis arabaları — hiçbiri "sahne dekoru" gibi düzenli değil, gerçek bir gece vardiyasının ortasında yakalanmış gibi durmalı (Pillar 3: Görev Gerçekliği). Kamera/çerçeveleme eğilimi: sabit göz hizası, sinematik pan yok — kompozisyon asla "güzel" bir kare aramaz, işin ortasında olma hissini korur. |

---

### 2.2 Anı-Tetikleyici Kayma (Shifting-In / Held)

| | |
|---|---|
| **Duygu hedefi** | **Zemin Kayıyor** (`isik-volume-durum-sistemi.md` Player Fantasy'nin kendi adı). Korku değil — bir *gerçeklik testi başarısızlığı*: "ben mi yanlış hatırlıyorum, yoksa oda mı değişti?" Bir alarm değil, bir fısıltı; oyuncu tanık, dedektif değil. |
| **Işık karakteri** | Kilitli hedef: soğuk mavi RGB(128,179,255) ya da sodyum-yeşil RGB(150,204,153) — level designer tetikleyicinin "endüstriyel/yapay" mı yoksa "duygusal/soğuk" mu hissettirmesi gerektiğine göre seçer. White Balance Temperature -60 / Tint +10, Post Exposure -0.5 / Saturation -20 — asla tam doygun, asla tam karanlık (MemoryIntensityMultiplier ≥0.6 erişilebilirlik tabanı). Geçiş smoothstep eğrisiyle (~3s, "3x²-2x³") yumuşak başlar ve yumuşak yerleşir — **hiçbir zaman ani, hiçbir zaman alarm gibi çakmaz.** Gölgeler **yumuşak ve dağınık, kaynağı belirsiz** — bu bir çevresel sızıntı, tek bir lambanın kararı değil. Aynı geometri, aynı zaman dilimi — sadece ışığın rengi yalan söylüyor. |
| **Atmosferik sıfatlar** | yabancılaşmış, dondurucu, kaynağı belirsiz, sessiz, kayan |
| **Enerji seviyesi** | **Askıda / nefes tutan durgunluk** — oyuncu genelde bu anda fiziksel olarak durmuş haldedir (Hold etkileşimi), bu yüzden enerji dışa değil içe döner. |
| **Işık-dışı görsel eşlikçi** | **Kompozisyon daralması** — yeni bir efekt katmanı değil (bkz. `isik-volume-durum-sistemi.md` Visual/Audio Requirements, "Ek görsel ipucu yok" kuralı: sis/parçacık/geometri değişimi kesin yasak, bu kuralı ihlal etmeyin). Bunun yerine: oyuncu Hold sırasında fiziksel olarak sabit durur (adım/head-bob sarsıntısı kesilir), kamera doğal olarak tetikleyici nesneye kilitlenmiş gibi okunur; çevredeki set dekoru, düşük `MemoryIntensityMultiplier` ve düşürülmüş doygunluk altında ayrı ayrı okunan nesneler olmaktan çıkıp tek bir siluet kütlesine düşer. Dikkat, ışığın kendisi tarafından değil, sahnenin *durağanlığı ve çevrenin okunaksızlaşması* tarafından tek noktaya toplanır. |

**Ek not — `TriggerMode=Automatic` bölge (AD-ART-BIBLE 2026-08-05 revizyonu, eklendi)**: Yukarıdaki satırların tamamı `ManualOnly` (Hold ile bilerek tetiklenen) anı-tetikleyicilerini varsayıyor — "oyuncu fiziksel olarak durmuş" ifadesi bunun somut kanıtı. Ama MVP'nin zorunlu rotasında en az bir `TriggerMode=Automatic`, `Persistent=false` bölge de var (`isik-volume-durum-sistemi.md` içerik gereksinimi) — oyuncu bunun içinden **yürüyerek** geçer, hiçbir Hold/rıza anı yok, ve kayma geri dönüşlüdür (bölgeden çıkınca `Dormant`'a döner). Bu, yukarıdaki tablonun tarif ettiği duygudan gerçek bir farkla ayrılır:

- **Duygu hedefi farkı**: "Zemin Kayıyor" hâlâ geçerli ama *rızasız* bir versiyonu — oyuncu bunu seçmedi, sadece içinden geçti. Bu daha çok bir "fark ettim ama ne olduğunu anlamadan geçtim" hissi olmalı, yukarıdaki "tanık, dedektif değil" çerçevesinin daha da pasif bir ucu — asla bir "beni durdurdu" hissi olmamalı (Pillar 2, şok değil).
- **Işık karakteri aynı kalır** (soğuk mavi/sodyum-yeşil, aynı smoothstep geçişi, aynı `MemoryIntensityMultiplier ≥0.6` tabanı) — sadece süre/temas farklı, palet farklı değil.
- **Kompozisyon daralması UYGULANMAZ** — bu, Hold'un fiziksel durağanlığına bağlı bir teknikti (adım sarsıntısının kesilmesi); yürüyerek geçilen bir bölgede oyuncu hareket halinde kalır, bu yüzden dikkat toplama tekniği bu bölge için mevcut değil, sadece ışık/renk kendi başına taşır. Bu bir eksiklik değil — pasif bölge zaten Pillar 1'in daha hafif, arka-planda kalan bir provası olmak için var, memory-trigger'ların taşıdığı ağırlıkla yarışmıyor.
- **Geri dönüşlü olması bir görsel ipucu değil**: bölgeden çıkınca ışık `Dormant`'a smoothstep ile geri döner — bu geri dönüş de aynı yumuşak eğriyi kullanır, farklı/daha "iptal" hissi veren bir geri-sarma değil.

*Design test*: Bir level designer bu bölge için "biraz daha dikkat çekici olsun, oyuncu fark etsin" isterse — reddedilir; bu bölgenin işlevi köşeye/arka plana düşmesi, Hold-tetiklenen anılarla aynı prominansı hiç talep etmemesi. Fark edilmemesi bir hata değil.

---

### 2.3 Asansör "Ölü Zaman" (Waiting)

| | |
|---|---|
| **Duygu hedefi** | **Ölü Zaman** (`asansor-kat-erisim-sistemi.md`'nin kendi adı). Ellerin işi bitince düşüncenin içe kıvrılması — gecenin *tek* zorunlu durgunluğu. "Korkunç asansör" değil; o gece onlarca kez girilen sıradan bir kutunun aşinalığı. Yetkinlik burada askıya alınır ama tutulmaz — iş seni bu boşlukta bile bırakmıyor, bu yüzden boşluk bir mola gibi işlev göremiyor. |
| **Işık karakteri** | Gerçeklik paletinin sıcak ailesinden ama uçta: **az ışık, tek zayıf pratik kaynak, neredeyse karanlığa yakın.** Yeni bir renk değeri icat edilmiyor — bu, anı-soğukluğu değil, gerçekliğin en loş ucu. Yüksek kontrast (tek zayıf kaynak + geniş karanlık), kapalı kutu içinde zaman hissi askıya alınmış (gece de olsa gündüz de olsa fark etmez, kabin kendi izole zaman kapsülü). |
| **Atmosferik sıfatlar** | kapalı, dar, tekdüze, ağır, sessiz-ama-uğultulu |
| **Enerji seviyesi** | **Durgun / içe dönük** — oyunun geri kalanında hep bir şey yapılırken, burası hareketsizliğin kendisinin anlamlı olduğu tek an. |
| **Işık-dışı görsel eşlikçi** | **Kabinin tam kapalı hacmi ve kozmetik kamera sarsıntısı.** Look serbest kaldığı için (`asansor-kat-erisim-sistemi.md` CD-GDD-ALIGN notu) kabin içi **her bakış açısında** tamamen kapalı/camera-safe olmalı — hiçbir sahne yükleme sızıntısı, hiçbir render açığı görünmemeli; bakılacak her yer "gerçek" olmalı, çünkü kaçış/bilgi sunmayan bu tamlığın kendisi Ölü Zaman'ı görsel olarak somutlaştırır. Buna ek olarak düşük genlikli, sürekli kamera-uzayı sarsıntısı (ses/uğultudan ayrı, saf görsel bir bileşen) kabin fiziksel olarak hiç hareket etmezken "hareket ediyormuşsun" yalanını taşır — Pillar 1'in ilk küçük provası. |

---

### 2.4 Psikiyatri Ofisi Kesmesi (Sahne Kesmeli Anlatı)

| | |
|---|---|
| **Duygu hedefi** | **Tanı odasının soğuk netliği.** Burası hatırlamanın değil, *incelenmenin* mekânı — her şey aydınlık ama hiçbir şey sıcak değil. Sabit, mesafeli, dışarıdan bakılan bir an. **Not**: gecenin iki bitişi (sakin teslim vs. dünyanın seni durdurması, bkz. `sahne-kesmeli-anlati-2026-08-02.md`) bu odanın paletini DEĞİŞTİRMEZ — palet ikisinde de birebir aynıdır; hissedilen fark tamamen ses kanalında (Abrupt açılış/kapanışı) ve hareket kilidi kapsamında yaşar, ışıkta değil. Bir sanatçı iki bitiş için iki farklı ofis ışığı tasarlamaya kalkmamalı — bu, sistemin kendi kararına aykırı olur. |
| **Işık karakteri** | Kilitli: **teal-gri palet, sert/tek kaynaklı gölge, tek pratik lamba + jaluzi çizgileri.** Yüksek kontrast — net, keskin gölge kenarları (anı-soğukluğunun yumuşak/dağınık gölgelerinin tam tersi). Kaynak belirsiz değil, **tam tersine iddialı biçimde tek ve görünür** — oyuncu bu ışığın nereden geldiğini her zaman bilir. Zaman hissi: kapalı, pencere jaluzili bir gece ofisi — dışarısı yok, sadece bu oda var. |
| **Atmosferik sıfatlar** | klinik, sabit, açığa çıkarılmış, mesafeli, keskin |
| **Enerji seviyesi** | **Sahnelenmiş durağanlık** — kamera ve ışık kompozisyonu bilinçli surette "tiyatral" bir sabitlik taşır, keşif segmentlerinin işlevsel/gündelik enerjisinden kasıtlı olarak kopar. |
| **Işık-dışı görsel eşlikçi** | **Jaluzi kanatları, fiziksel bir set objesi olarak.** Çizgili gölge deseni sadece bir ışık matematiği değil — jaluzinin kendisi sahnede duran, tek lambanın içinden kestiği somut bir nesnedir; duvarlarda oluşan kafes-benzeri çizgi deseni "İki Oda, Tek Işık" motifinin fiziksel imzasıdır. Kamera/çerçeveleme eğilimi: **sabit, kilitli kamera** — keşif bölümlerinin göz-hizası/serbest-bakış hissinin tam zıddı; bu sabitlik tek başına oyuncuya "artık farklı bir sahnedeyim" sinyalini ışıktan bile önce verir. **Kadraj içeriği (revize, AD-ART-BIBLE 2026-08-05)**: bu kompozisyonun daha önceki bir taslağı "two-shot" (iki kişi kadrajda) diye kilitlemişti — bu, Bölüm 5.1'in "MVP'de oyuncunun görünür bedeni yok" kuralıyla hiçbir istisna tanımlamadan çelişiyordu, ve hiçbir GDD (`sahne-kesmeli-anlati-2026-08-02.md` dahil) bunu gerçekten kilitlemiyor — sadece art bible'ın kendi icadıydı. Doğru, MVP-kapsamlı çağrı: **psikiyatrist duyulur, illa görülmez** — sahne oyuncu-POV'una yakın bir açıdan kurulur (jaluzi/gölge deseni, masa, oda kendi kimliğini taşımaya yeter), psikiyatristin tam gövdeyle kadrajda olup olmayacağı Bölüm 5'in açık sorusu (psikiyatrist NPC'nin görsel temsili) çözülene kadar **kilitli değil, provizyonel**. Sabit/kilitli olan tek şey kameranın kendisi (hareketsizlik, tiyatral durağanlık) — kimin kadrajda olduğu değil. |

---

### Ayrım Notu: İki "Soğuk"un Kasıtlı Farkı

`game-concept.md`'nin kendi art-director notu bunu zaten bir risk olarak
işaretliyor: **Anı-soğukluğu (2.2) ve psikiyatri ofisi (2.4) ikisi de
"soğuk" ama aynı hissi vermemeli**, yoksa oyuncu hangi katmanda olduğunu
kaybeder. Bir sanatçı için tek satırlık test:

> **Gölge kaynağını göster.** Anı-soğukluğunda gölgenin nereden geldiğini
> gösteremiyorsan doğru yoldasın (yumuşak, dağınık, kaynağı belirsiz —
> çevresel bir sızıntı). Psikiyatri ofisinde gölgenin nereden geldiğini
> *hemen* gösterebiliyorsan doğru yoldasın (sert, tek kaynaklı, jaluzi
> çizgileriyle imzalı — sabit bir mekân kimliği).

İkinci bir ayrım katmanı: anı-soğukluğu **geçicidir ve geri dönüşlüdür**
(Shifting-In/Out eğrisiyle yumuşakça gelir ve gider, otel geometrisi asla
"kendi mekânı" değildir, sadece geçici olarak yalan söyler); psikiyatri
ofisi **kendi sabit mekânıdır** — oraya "kayılmaz", sahne kesmesiyle
doğrudan girilir ve çıkılır, kendi ışığı hiç geçiş yapmaz, hep aynı
teal-gridedir. Biri bir *durumdur* (otelin geçici bir hali), diğeri bir
*yerdir* (kendi sabit kimliği olan başka bir oda).

## 3. Shape Language

### Şekil Kuralı

> **Biçim asla ipucu vermez; hiyerarşi biçimde değil ışıkta yaşar.**

Bölüm 1'in kuralı ("mekân asla yalan söylemez, yalnızca ışık yalan söyler") burada bir adım ileri gidiyor: bu oyunda biçim, dikkat çekmek için var olan bir kanal bile değil. Bir sanatçı bu bölümü şu tek cümleyle özetleyebilmeli: **bir nesnenin önemli olup olmadığını asla siluetinden anlaşılır kılma — o iş zaten ışığın işi.** Bu, çoğu art bible'ın "önemli şey göze çarpsın" diyen standart şekil-dili mantığının doğrudan tersidir; burada tersine çevrilmiş olması kasıtlı ve bu bölümün geri kalanı bunu neden ve nasıl olduğunu açıklıyor.

### 3.1 Nesne/Prop Siluet Felsefesi — Kamuflaj vs. Okunabilirlik

**Prensip: Okunabilirlik nesnede değil, mekânda yaşar.** Kamuflaj mekaniği (`birinci-sahis-kontrolcu.md`, yaklaşma-yavaşlaması) anı-tetikleyicilerle sahte/dekor `IInteractable`'ların (kapı kolu, ışık anahtarı, termostat, temizlik arabası freni) **aynı** bayrağı paylaşmasını zorunlu kılıyor — oyuncu yavaşlamayı hissedip de neyin gerçek olduğunu anlayamamalı. Bu, siluet düzeyinde tek bir sonuç doğurur: **hiçbir etkileşim nesnesi, ne gerçek ne sahte, kendine özgü/tanınabilir bir siluet taşıyamaz.** Bir tetikleyicinin diğerlerinden görsel olarak "ilginç" durması, kamuflajı anında çökertir — oyuncu, Formül 2'nin ürettiği hissi görsel bir metal dedektörüne çevirir (bkz. `birinci-sahis-kontrolcu.md` Core Rules, "kasıtlı kamuflaj" notu).

Bu, gerçek otel servis donanımının **kendi, üretimde-değiştirilmemiş** siluetlerine sadık kalmayı zorunlu kılar — bir kapı kolu tasarım olarak bir kapı kolu gibi durur, ne "oyunlaştırılmış" bir tutamaç, ne dramatik bir vurgu-eğrisiyle abartılmış bir form. Section 1'in "hiçbir şey yeniden dekore edilmez" kuralının bu bölümdeki karşılığı budur: **hiçbir nesne, oyun okunabilirliği için biçimini yeniden dekore ettirmez.**

Peki bir servis alanı "gerçek ve anlaşılır bir mekân" gibi mi, yoksa okunmaz bir görsel gürültü yığını gibi mi hissettirilecek (Pillar 3'ün gerektirdiği)? Bu soru **nesne** ölçeğinde değil, **mekân** ölçeğinde çözülür: koridorun, deponun, balo salonunun kendi mimari kütleleri (raf blokları, koridor hacmi, tavan/kolon ritmi) net, basit, okunaklı büyük biçimler taşır — göz mekânı anında kavrar. Ama o mekânın içine serpiştirilmiş küçük ölçekli donanım (kapı kolları, anahtarlar, kutular, tetikleyiciler) kasıtlı olarak **aynı sıradan donanım dilinin** bir parçası olarak "sessiz" kalır. Kısacası: **büyük biçim konuşur, küçük biçim susar.**

*Design test*: Bir prop için "bu biraz daha ilginç/tanınabilir bir siluet olsun mu" diye tartışılırsa — bu prensip her zaman hayır der, çünkü nesnenin ilginçliği neyi taşıdığından (gerçek/sahte) bağımsız olarak değerlendirilmeli; bir nesne diğerlerinden ayrılacaksa bunu yapan şey ışık/vurgu olmalı (Işık/Volume Sistemi'nin alanı), biçim değil. *(Pillar 3 birincil — gerçek bir otel donanımı kataloğuna sadakat; Pillar 5 ikincil — anlam sona saklı kalmalı, siluet erken ipucu vermemeli.)*

### 3.2 Çevre Geometrisi — Köşeli, Modüler, Fonksiyonel

Bu otelin misafir yüzü (lobiler, süitler, kristal avizeler) **bu oyunda hiç görünmez** — oyuncu her zaman kulisin arka tarafındadır. Bu, geometri kararını kendiliğinden veriyor: **köşeli/dikdörtgen geometri baskındır, organik/kavisli form neredeyse yoktur.** Servis koridorları, depo rafları, balo salonu arkası — hepsi 4m×4m modüler ızgaraya hizalı, doksan derece köşeli, yapısal olarak "dürüst" hacimlerdir (kirişler, raf iskeletleri, kapı çerçeveleri hep görünür/mantıklı). Bu sadece bir teknik zorunluluk (statik-flagged modüler kit, bkz. `game-concept.md` Technical Considerations) değil, aynı zamanda Pillar 3'ün (Görev Gerçekliği) doğrudan bir görsel ifadesi: **emek, kutular arasında kutular gibi kurulmuş bir mekânda geçer.**

Kavis/organik form tamamen yasak değil — ama nereye ait olduğu net: **sadece taşınan/depolanan lüks eşyaya aittir**, mimariye değil. Sandalye yığınları, rulo halılar, katlanmış masa örtüleri, bir avize sandığı — balo salonunun *misafire ait olacak* eşyaları, servis alanında hâlâ kasa/örtü/ambalaj hâlindeyken bile bu tek kavis/yumuşaklık dozunu taşır. İlke şu: **oda bir kutudur; kutunun içinden geçen şey eğridir.** Bu ayrım, oyuncuya hiç söylenmeden "burası hazırlık alanı, sahne değil" der.

| Alan | Baskın geometri | Neden |
|---|---|---|
| Depo | Sert köşeli, raf-ızgara tekrarı | Envanter mantığı — Pillar 3, ölçülebilir/istiflenebilir gerçeklik |
| Servis Koridoru | Uzun dikdörtgen prizma, tekrarlayan kapı/apliğ ritmi | Geçiş hacmi — modüler kitin en saf tekrar birimi |
| Balo Salonu (arka/hazırlık tarafı) | Büyük açık dikdörtgen hacim, sert kolon/kiriş ritmi | Misafir tarafı hiç görünmez; bu sadece "sahne arkası" |
| Taşınan/depolanan lüks eşya (sandalye yığını, örtü, avize sandığı) | Tek kavis/yumuşaklık istisnası | Misafire ait geleceği, henüz kullanılmadığı için "yabancı" kalması gerekir |

*Design test*: Bir modülün 4m×4m ızgarayı bozan (eğik duvar, yuvarlatılmış köşe, dekoratif kemer) bir versiyonu önerilirse — bu prensip reddeder, çünkü Bölüm 1 Prensip 1'in ("gerçekçi geometri, öznel ışık") temelini sarsar: geometri bir anı anını *hiçbir zaman* çözmemeli, bu yüzden geometrinin kendisi baştan sona sıkıcı/tutarlı/köşeli kalmalı ki hiçbir zaman şüpheli bir "değişkenlik" alanı gibi okunmasın. *(Pillar 3 birincil.)*

### 3.3 UI Şekil Grameri — Dünyadan Ayrı, Kasıtlı Nötr Bir Dil

Crosshair/prompt, dünyanın gerçekçi-stilize estetiğini **yankılamaz** — kasıtlı olarak ayrı, meta bir katmandır. Sebebi doğrudan Pillar 1'in kendi istisnasında yatıyor (`etkilesim-sistemi.md`, "Pillar 1 muafiyeti"): crosshair diegetik değil, her zaman güvenilir bir okuma aracı — dünyanın öznelliğine hiç girmiyorsa, biçimi de dünyanın biçim diline hiç girmemeli. Otelin donanım estetiğinden (pirinç, ahşap, endüstriyel metal) ödünç alınmış hiçbir detay, hiçbir "otel logosu hissi" crosshaira sızmaz.

Bunun yerine: **saf geometrik ilkel** — nokta veya ince bir daire, tek bir çizgi kalınlığı, renk-bağımsız bir durum değişimi (erişilebilirlik notu, `etkilesim-sistemi.md` UI Requirements). Idle→Focused geçişi bir *patlama* ya da *parlama* değil, küçük bir ölçek/opaklık kayması olmalı — Pillar 2'nin "şok değil" ilkesi burada kelimenin tam anlamıyla crosshaira kadar iniyor: **crosshair'in kendisi bile şok üretmemeli.** Hold-doldurma göstergesi de aynı disiplini taşır — ince bir halka, dramatik bir radial-wipe veya renk sıçraması değil.

Sonuç: dünyanın şekil dili "gerçekçi, dokunulmuş, kullanılmış" iken; UI'ın şekil dili "soyut, dokunulmamış, jenerik"tir. Bu iki dilin birbirine hiç değmemesi, oyuncunun neyin "gerçek dünya" neyin "arayüz" olduğunu hiç sorgulamadan ayırt etmesini sağlar — tuhaf bir şekilde, bu netlik Pillar 1'in (Öznel Gerçeklik) dünya tarafında yaratmaya çalıştığı belirsizliğin önkoşuludur: arayüz belirsizleşirse, oyuncu artık dünyanın mı yoksa oyunun mu yalan söylediğini ayırt edemez.

*Design test*: Bir UI elemanı için "otelin karakterinden bir motif alalım, daha 'diegetik' hissettirir" önerisi gelirse — bu prensip reddeder; diegetik hissettirme çabası, crosshair'in Pillar 1 muafiyetini bulanıklaştırır ve güvenilirliğini riske atar. *(Pillar 2 birincil.)*

### 3.4 Kahraman Biçim vs. Destekleyici Biçim — Ters Çevrilmiş Hiyerarşi

Standart şekil-dili mantığı şunu söyler: önemli nesne görsel olarak öne çıksın. Bu oyunun çekirdek mekaniği ise tam tersini zorunlu kılıyor — en önemli etkileşim nesneleri (anı-tetikleyiciler) **özellikle** öne çıkmamalı, çünkü öne çıkarlarsa kamuflaj çöker ve Pillar 5 (Anlam Sona Saklı) ile Pillar 1'in (Öznel Gerçeklik) test edilebilirliği birlikte yıkılır. Bu bölümün geri kalanı bu gerilimi çözüyor, yok saymıyor:

**Kahraman biçim, etkileşim nesnesi katmanından tamamen çekilir.** Ne bir tetikleyici ne bir dekor-nesne asla "bak buraya" diyen bir siluet kazanır — 3.1'in kuralı burada mutlaktır, istisnasız. Hiyerarşi ihtiyacı ortadan kalkmıyor, sadece **kanal değiştiriyor**: önem artık biçimde değil, Bölüm 1-2'nin zaten sahiplendiği ışık/sıcaklık/vurgu ekseninde yaşıyor. Bir sanatçı "bu nesne önemli, öyle hissettirsin" isteğini hiç biçimle çözmemeli — bu isteğin tek meşru cevabı Işık/Volume Sistemi'ne devredilir.

Kahraman biçim tamamen yok değil — sadece iki dar, kasıtlı istisnaya sıkıştırılmış, ikisi de **etkileşim/kamuflaj sisteminin dışında**:

- **Mimari yönlendirme**: Koridorun ucu, kapı çerçevesi, asansör kapısı gibi büyük mimari kütleler net/basit siluetleriyle göz gezdirmeye yardımcı olabilir — ama bu "burada ilginç bir şey var" demek değil, sadece "buradan gidilir" demektir. Wayfinding, anlam sızdırmaz.
- **Taşınan yük (kol/el rigi)**: Oyuncunun taşıdığı eşya, tanımı gereği önemlidir (Pillar 3, Görev Gerçekliği'nin somut nesnesi) ve kol/el rigi üzerinde sürekli görünür kalır. Ama burada bile biçim hiçbir zaman abartılmaz — tek statik tutuş pozu, blend-tree yok (`gorev-tasima-dongusu.md` Visual Requirements). Ve daha da önemlisi: bu nesnenin "önemi" bile round ilerledikçe **biçimle değil ışık/çerçeve-vurgusuyla** söner (`Highlight(round)` eğrisi, mesh/materyal hiç değişmeden) — "Dikkatin Göçü" fantazisinin taşıyıcısı burada da ışıktır, biçim değil. Bu, 3.4'ün kuralının projedeki en somut kanıtı: oyunun kendi çekirdek sistemi bile önemi biçimden ışığa taşımayı zaten seçmiş durumda.

*Design test*: Bir prop/level tasarımcısı "oyuncunun bunu fark etmesini istiyorum, biraz daha büyük/farklı biçimlendireyim mi" diye sorarsa — cevap her zaman hayır'dır; doğru soru "bunu nasıl aydınlatayım/çerçeveleyeyim" olmalı. Biçim kanalı bu oyunda önem taşımaz, sadece kimlik taşır (bu bir kapı kolu, bu bir kutu, bu bir termostat) — önemin kendisi başka bir sistemin işi. *(Pillar 1 ve Pillar 5 birincil — kamuflaj ve anlamın sona saklanması; Pillar 3 ikincil — taşınan yükün istisnai ama sınırlı görünürlüğü.)*

### Ayrım Notu: Şekil Sessiz, Işık Konuşur

Bu bölümün dört maddesi tek bir cümleye indirgenebilir ve bir prop/çevre sanatçısı elinde bundan başka hiçbir şeye ihtiyaç duymamalı: **bu oyunda biçim asla dikkat çekmenin aracı değildir — biçim sadece bir nesnenin ne olduğunu söyler, ne kadar önemli olduğunu asla.** Önem her zaman Bölüm 1-2'nin sahiplendiği ışık/sıcaklık kanalından gelir. Bir sanatçı bir nesneyi öne çıkarmak istediğinde önce kendine "bunu ışıkla mı çözüyorum, biçimle mi" diye sormalı — cevap bu belgede her zaman ışıktır.

## 4. Color System

### Renk Kuralı

> **Renk asla süslemez; renk sadece hangi gerçeklik katmanında olduğunu söyler.**

Bölüm 1'in kuralı ("mekân asla yalan söylemez, yalnızca ışık yalan söyler") burada somutlaşıyor: bu oyunda renk, ışığın taşıdığı *tek* bilgi kanalıdır ve taşıdığı bilgi her zaman aynı sorunun cevabıdır — **"şu an hangi durumdayım: gerçeklik mi, anı mı, yoksa muayene mi?"** Bu belge Anti-Pillar'ın zaten kilitlediği bir gerçeği renk diline çeviriyor: bu oyunda kombat, ekonomi, sağlık/hasar yok — dolayısıyla renk hiçbir zaman *tehlike*, *ödül* ya da *ilerleme* bildirmez. Bir sanatçı bu bölümü tek cümleyle özetleyebilmeli: **renk bir durum bildirimidir, bir değer yargısı değil.**

### Destekleyici Prensipler

**1. Renk bir durum sinyalidir, bir tehdit/ödül sinyali değil** — *Pillar 1 (Öznel Gerçeklik) birincil*

Standart oyun renk semantiği (kırmızı=tehlike, altın=ödül, yeşil=sağlık) bu projede **yok** — çünkü bunların hiçbirine karşılık gelen bir mekanik yok. Bu belgede tanımlanan her renk, oyuncuya sadece "neredeyim" sorusunun cevabını verir; "iyi mi kötü mü" sorusunun cevabını asla vermez.

*Design test*: Bir sistem tasarımcısı "bu tetikleyiciyi biraz kırmızıya çeksek, daha tehditkâr hissettirir mi" diye sorarsa — bu prensip reddeder. Tehditkârlık bu oyunda sesle ve kompozisyon daralmasıyla (bkz. Bölüm 2.2) taşınır, hiçbir zaman renk-kodlu bir "uyarı" ile değil.

**2. İki soğuk, iki ayrı gramer — renk sisteminin kendi sorumluluğu** — *Pillar 1 birincil*

Bölüm 2'nin "Ayrım Notu"su (anı-soğukluğu vs. psikiyatri ofisi) bu bölümün üzerine kurulduğu temel kısıttır: iki soğuk paletin birbirine asla hue/doygunluk düzeyinde yaklaşmaması, bu bölümün tanımladığı palet tablosunun **birinci görevidir**, ikincil bir sonucu değil.

*Design test*: Yeni bir anı-tetikleyici renk varyantı önerilirse — bu prensip, önerilen rengin Muayene Teali'nden (4.1) hue ve gölge-sertliği ekseninde yeterince uzak olup olmadığını sorar; uzak değilse reddedilir.

**3. Dünyanın rengi ile arayüzün rengi hiç dokunmaz** — *Pillar 2 ikincil, Bölüm 3.3'ün doğrudan devamı*

Bölüm 3.3 UI'ın şekil dilini dünyadan ayırmıştı ("saf geometrik ilkel, renk-bağımsız durum değişimi"); bu bölüm aynı ayrımı renk ekseninde kilitliyor — UI paleti (4.4), dünya paletinin (amber/mavi/yeşil/teal) hiçbir üyesiyle karışabilecek bir ton taşımaz.

*Design test*: Bir UI elemanına "biraz amber tonu katalım, dünyayla daha bütünleşik hissettirir" önerisi gelirse — reddedilir; bu, Bölüm 3.3'ün zaten kurduğu Pillar 1 muafiyetini bulanıklaştırır.

### 4.1 Ana Palet

Aşağıdaki değerlerin çoğu `isik-volume-durum-sistemi.md`'de zaten kilitli — bu tablo onları **isimlendirip** üretim diline çeviriyor, yeni bir değer icat etmiyor. **Önemli teknik ayrım**: Amber/Anı/Sodyum satırları bir `Light` bileşeninin `BaseColor`/`MemoryColor` değerleridir (Volume/ışık hedefi) — bir yüzeyin **materyal albedosu değil**. Bir depo rafının boyası kendi albedosunu taşır; ışık ona çarptığında çarpımsal olarak birleşir. Bu karışıklığı önlemek için "Temel Malzeme Grisi" ayrı bir kategori olarak aşağıda tanımlanıyor.

| # | Ad | Hex (yaklaşık) | Kaynak | Ne anlama gelir |
|---|---|---|---|---|
| **Gerçeklik Ailesi** | | | | |
| 1 | **Vardiya Amberi** | `#FFBF80` (255,191,128) | Kilitli — `LightBlend` formülünün `BaseColor` referans değeri | Kanıksanmış tetiktelik (Bölüm 2.1). "İyisin" demez — "bu iş, bu saat tanıdık" der (Bölüm 1, Prensip 3: sıcaklık bir bağ sinyali, güvenlik sinyali değil). |
| 2 | **Temel Malzeme Grisi** | `#B5A897` (181,168,151) | **Yeni — bu belgede ilk kez sabitleniyor** | Modüler otel kitinin kendi, ışıksız albedosu: boyalı sıva/beton, sıcak-nötr bir "kirli bej". Bölüm 3.2'nin "dürüst, üretimde-değiştirilmemiş" geometrisinin somut rengi — kendi başına ne sıcak ne soğuk, sadece **gerçek ve sıradan**; kimliğini Vardiya Amberi'nin üzerine düşmesiyle kazanır. |
| 3 | **Gerçeklik Gölgesi** | `#14100C` (20,16,12) | **Yeni — bu belgede ilk kez sabitleniyor** | Amber ailesinin en koyu ucu — sıcak-nötr, neredeyse siyah. **Kural**: gerçeklik durumunda hiçbir gölge mavi/soğuk bir siyaha kaymaz; kayarsa, oyuncu bilinçsizce bir anı-sızıntısı okur (bkz. Prensip 2). Ölü Zaman'ın (Bölüm 2.3) "az ışık, tek zayıf kaynak" hissinin taban tonu budur. |
| **Anı Ailesi** | | | | |
| 4 | **Anı Mavisi** | `#80B3FF` (128,179,255) | Kilitli — `memory_color_blue` (registry) | Birincil `MemoryColor` hedefi — "duygusal/soğuk anı" tetikleyicileri (level designer seçer, bkz. `isik-volume-durum-sistemi.md`). Zemin Kayıyor hissinin (Bölüm 2.2) rengi: bir gerçeklik testi başarısızlığı, bir tehlike değil. |
| 5 | **Sodyum Yeşili** | `#96CC99` (150,204,153) | Kilitli — `memory_color_sodium_green` (registry) | Alternatif `MemoryColor` hedefi — "endüstriyel/yapay anı" tetikleyicileri. Anı Mavisi ile **eşdeğer** bir sinyal, "daha kötü" ya da "daha iyi" bir varyant değil — sadece anının **kaynağının karakteri** farklı. |
| **Muayene Ailesi** | | | | |
| 6 | **Muayene Teali** | `#7A9496` (122,148,150) | Nitel olarak kilitli ("teal-gri"), **sayısal değeri bu belgede ilk kez formalize ediliyor** | Psikiyatri ofisinin sabit, tek mekân-kimliği rengi (Bölüm 2.4). Anı Mavisi'nden **kasıtlı olarak** hue uzaklığı büyük tutulur (mavi-yeşil arası, desatüre) — Bölüm 2'nin "Ayrım Notu"suna göre asıl ayrım zaten gölge sertliğinde yaşıyor, ama renk de yardımcı bir ikinci kanıt olmalı. |

**Not (art-director)**: Vardiya Amberi ile Temel Malzeme Grisi arasındaki ilişki kasıtlı — grinin kendisi hiçbir zaman "tasarlanmış" bir sıcaklık taşımaz, tüm sıcaklığı üstüne düşen ışıktan alır. Bir sanatçı bir duvar materyaline doğrudan amber/turuncu bir albedo boyarsa bu, "ışık yalan söyler" kuralını (Bölüm 1) ihlal eder — çünkü o zaman ışık kapansa bile duvar hâlâ "sıcak" görünür, bu da anı-kaymasının kontrastını sulandırır.

### 4.2 Anlamsal Kullanım — Renk Ne Söyler, Ne Söylemez

Bu oyunda kırmızı=tehlike, altın=ödül, yeşil=sağlık semantiği **yok**, çünkü karşılık geldikleri mekanikler yok (Anti-Pillar: NOT kombat sistemi, ekonomi yok). Renk burada tek bir eksende çalışır: **gerçeklik durumu**.

| Renk ailesi | SÖYLER | SÖYLEMEZ |
|---|---|---|
| Amber (Gerçeklik) | "Şu an gerçekliktesin, bu iş/an tanıdık" | "Buradasın, güvendesin" (Bölüm 1, Prensip 3 — güvenlik sinyali değil) |
| Anı Mavisi / Sodyum Yeşili | "Bu an bir öznel-gerçeklik sızıntısı, tanık ol" | "Tehlike var", "kaçmalısın", "bu kötü bir şey" — bir alarm değil, bir fısıltı (Player Fantasy) |
| Muayene Teali | "Sabit, ayrı bir yerdesin — otel değil" | Bir "mood" ya da o anki duygusal yoğunluk (Bölüm 2.4: iki farklı bitiş bile aynı paleti kullanır — palet asla duygu şiddetini kodlamaz, sadece **yer**i kodlar) |
| (Hiçbiri) | — | Mekanik tehlike, ödül, sağlık, ilerleme, doğru/yanlış seçim. Bu oyunda bunların karşılığı yok; bir renk bunlardan birini ima ederse üretim hatasıdır. |

*Design test*: Bir level designer "oyuncu bu anı-tetikleyiciyi kaçırırsa kaybeder, rengi biraz daha 'önemli' göstersek mi" diye sorarsa — bu prensip reddeder. Önem, Bölüm 3.4'ün zaten kurduğu gibi **ışık/vurgu** ekseninde yaşar, biçimde yaşamıyordu; burada da renk **sadece durumu** taşır, "bunu kaçırma" gibi bir öncelik sinyalini asla üstlenmez — kamuflaj mekaniği (Bölüm 3.1) bunu zaten yasaklıyor.

### 4.3 Alan Başına Renk Sıcaklığı — Depo, Servis Koridoru, Balo Salonu

`isik-volume-durum-sistemi.md`'nin `LightBlend` formülü `BaseColor`'ı açıkça **ışık başına** (per light) tanımlıyor — paylaşılan tek varlık sadece anı-kayması Volume Profile'ıdır (WB -60/Tint+10, PE-0.5/Sat-20), **BaseColor değil**. Bu, üç alanın kendi pratik ışıklarına farklı `BaseColor` değeri vermesinin hiçbir kilitli kuralı ihlal etmediği anlamına gelir — ve gerçek bir otelin servis alanları zaten hiçbir zaman tek bir armatürle aydınlatılmaz. Pillar 3 (Görev Gerçekliği) bunu talep ediyor: her alan kendi, gerçek bir bakım/iş bütçesiyle aydınlatılmış hissetmeli.

| Alan | BaseColor (yaklaşık) | Gerekçe (gerçek otel pratiği) |
|---|---|---|
| **Depo** | `#FF9E4D` (255,158,77) — Vardiya Amberi'nden daha doygun/kızıl | En ucuz, en eski armatürler burada yaşar — çıplak akkor ampul ya da yüksek-basınçlı sodyum buharlı lamba, düşük CRI, düz/sert ışık. Envanter alanı "dekore edilmeyen" ilk yerdir. |
| **Servis Koridoru** | `#FFBF80` (255,191,128) — **kilitli kanonik değer, referans noktası** | Floresan tüp + duvar apliği karışımı (Bölüm 2.1'de zaten "koridor apliği" olarak geçiyor) — sıradan bina-bakım floresanı genelde ~3000K sıcak-beyazdır, akkor kadar kızıl değil. Bu yüzden bu alan paletin **orta** noktasıdır. |
| **Balo Salonu (hazırlık tarafı)** | `#FFD9A8` (255,217,168) — en açık/en nötr | Misafir tarafına en yakın arka alan; hazırlık ekibi ince iş (masa düzeni, kumaş) için genelde daha temiz/beyaza yakın iş lambası veya halojen kullanır — kaba depo aydınlatmasından daha "temiz" ama hâlâ kesinlikle sıcak. |

**Guardrail (zorunlu)**: Bu üç varyant her zaman Amber ailesinin dar bir hue bandında kalır (~±10-15°) — hiçbiri mavi/yeşile kaymaz, hiçbiri Muayene Teali'nin doygunluk/parlaklık aralığına girmez. Bu üç ton arasındaki fark **sadece** "hangi oda" sorusuna cevap verir; hiçbiri bir anı-kaymasıyla karıştırılabilecek kadar soğumaz.

*Design test*: Bir lighting artist Balo Salonu hazırlık alanını Servis Koridoru'ndan "daha havalı/misafir-hissi versin" diye beyaza yaklaştırmak isterse — bu prensip sınırlı bir evet der (yukarıdaki `#FFD9A8` aralığına kadar), ama tam nötr/beyaza (misafir aydınlatması hissi) geçmesine izin vermez — Bölüm 3.2'nin "misafir yüzü hiç görünmez" kuralı burada da geçerli: hazırlık alanı hiçbir zaman "sahne" gibi aydınlatılmaz.

### 4.4 UI Paleti

`etkilesim-sistemi.md`'nin UI Requirements'ı zaten kilitliyor: crosshair durum değişikliği renge dayanamaz (şekil/boyut da değişmeli) — ve Bölüm 3.3 crosshair'i dünyanın şekil dilinden tamamen ayırmıştı. Bu belge şimdi rengi tamamlıyor.

**Karar: UI tamamen akromatik (renksiz-nötr) kalır — hiçbir hue taşımaz.**

Gerekçe: dünya paleti amber-baskın + soğuk-istila (mavi/yeşil) ikili bir sistemdir. UI için seçilecek **herhangi bir hue**, iki dünya durumundan biriyle kaçınılmaz olarak akraba okunur — sıcak bir ton amber/gerçeklik ile, soğuk bir ton anı/muayene ile karışır. Tek karışmaz seçenek: hue'suz, sadece parlaklık/opaklık ekseninde çalışan bir nötr gri-beyaz.

| Durum | Hex | Not |
|---|---|---|
| Idle crosshair | `#E4E4E0`, ~%65 opaklık | Hafif kırık-beyaz, ince çizgi — hem amber hem anı-mavisi/yeşili arka planında okunur kalır (her iki dünya durumunda da yüksek karşıtlık: amber koyu-doygun, anı-mavisi de orta-doygun, ikisi de bu neredeyse-beyaz tona karşı kontrast üretir). |
| Focused crosshair | `#FFFFFF`, %100 opaklık + hafif ölçek artışı | Bölüm 3.3'ün "patlama değil, ölçek/opaklık kayması" kuralına uyar — renk değişmez, sadece parlaklık ve boyut değişir (erişilebilirlik kuralının UI'daki karşılığı). |
| Hold-doldurma halkası | `#FFFFFF`, ince çizgi | Aynı nötr aile — nesnenin kendi ek VFX'i (varsa) kendi rengini taşıyabilir, ama çekirdek UI hep nötr kalır. |
| Crosshair/halka outline | Siyah, ~%40-60 opaklık, 1-2px | **Bölüm 7.5'te eklendi (ux-designer kontrast bulgusu)** — statik, animasyonsuz kontur; Vardiya Amberi ve Anı Mavisi/Sodyum Yeşili arka planlarının ikisine karşı da okunabilirlik garantisi, bir renk/parlama kararı değil. |

*Design test*: "Focused durumunda hafif bir amber parlama eklesek, sıcaklık hissini pekiştirir mi" önerisi gelirse — reddedilir; bu, crosshair'in Pillar 1 muafiyetini (her zaman güvenilir, dünyanın öznelliğine hiç girmeyen bir katman) tam da onu güvenilir kılan nötrlüğünden koparır.

### 4.5 Renk Körü Güvenliği

`MemoryIntensityMultiplier` varsayılanının ≥0.6 olması zaten kilitli bir kısmi çözüm (registry: `memory_intensity_multiplier_default`) — bu, `LightBlend` formülünün rengi ve yoğunluğu **aynı `ShiftProgress` ile kilitli adımda** hareket ettirmesi sayesinde (Formulas bölümü, "Output Range" notu) çalışır: bir anı-kayması asla sadece hue değiştirmez, her zaman ölçülebilir bir parlaklık düşüşü + `Saturation -20` doygunluk düşüşü de taşır. Bu belge bunu genel bir kurala çeviriyor:

**Genel kural**: Bu projede renk-tabanlı hiçbir durum sinyali **sadece hue** üzerinden taşınamaz — her zaman en az bir ek, hue-bağımsız kanal (parlaklık, doygunluk veya gölge sertliği/kompozisyon) aynı yönde hareket etmeli. Yeni bir sistem/artist bir renk-durumu eklerken kendine şunu sormalı: *"Bu sinyali hue kanalını tamamen kapatıp (griye çevirip) hâlâ okuyabilir miyim?"* Cevap hayırsa, tasarım onaylanmaz.

Bu kuralın projedeki mevcut örnekleri denetlendi:

| Sinyal | Hue-bağımsız yedek kanal | Durum |
|---|---|---|
| Gerçeklik ↔ Anı (Amber ↔ Mavi/Yeşil) | Parlaklık (`MemoryIntensityMultiplier`) + doygunluk (`Saturation -20`) aynı anda düşer | **Güvenli** — kilitli formül tarafından garanti |
| Anı ↔ Muayene Teali (iki "soğuk") | Gölge sertliği: yumuşak/dağınık (anı) vs. sert/tek-kaynaklı+jaluzi (ofis), ayrıca sabit/kilitli kamera vs. serbest bakış (Bölüm 2, "Ayrım Notu") | **Güvenli** — ayrım zaten renkte değil kompozisyonda yaşıyor |
| Crosshair Idle ↔ Focused | Opaklık + ölçek (4.4) | **Güvenli** — `etkilesim-sistemi.md` UI Requirements zaten zorunlu kılıyor |
| **Anı Mavisi ↔ Sodyum Yeşili** (hangi "tat" anı) | Yok — ikisi de aynı doygunluk/parlaklık formülünden geçiyor, tek fark hue | **Kısmi risk, ama mekanik değil** — bkz. aşağıda |

**Anı Mavisi/Sodyum Yeşili çifti üzerine not**: Bu ikisi arasındaki seçim (`isik-volume-durum-sistemi.md`'de level designer'a bırakılmış, "endüstriyel/yapay" vs. "duygusal/soğuk" niyet farkı) **hiçbir bulmaca/mekanik cevabın parçası değil** — hiçbir `ClueDefinition` ya da ilerleme koşulu oyuncunun bu iki tonu doğru ayırt etmesini şart koşmuyor, sadece içerik-yazımının kendi flavor katmanı. Bu yüzden şiddetli deuteranopi/protanopi altında bu iki tonun birbirine yakınsaması bir **erişilebilirlik hatası değil**, sadece bir içerik-inceliği kaybıdır — kabul edilebilir. Yine de üretim tavsiyesi: iki tonu sadece hue'da değil hafif bir parlaklık farkıyla da ayırın (Sodyum Yeşili biraz daha parlak/canlı tutulabilir) — zorunlu değil, ama ücretsiz bir tutarlılık kazancı.

*Design test*: Gelecekte yeni bir `MemoryColor` varyantı önerilirse (ör. üçüncü bir "tat") — bu prensip, önerilen rengin mevcut ikiliyle aynı parlaklık/doygunluk formülünden geçip geçmediğini sorar (geçmeli, `LightBlend` formülü zaten bunu zorunlu kılıyor) ve hiçbir mekanik/bulmaca cevabının bu üçünü ayırt etmeye bağlı olmadığını doğrular — bağlıysa, tasarım reddedilir çünkü bu ilkeyi (renk körü oyuncu için mekanik-nötr olmalı) ihlal eder.

## 5. Character Design Direction

### Karakter Kuralı

> **Bu belgede "karakter tasarımı," bir yüz ya da bir beden tasarlamak değildir — MVP'de tasarlanacak tek şey, Bölüm 3.4'ün zaten tanımladığı tek istisnanın (kol/el rigi) üzerine düşen ışıktır.**

Bölüm 1'in kuralı ("mekân asla yalan söylemez, yalnızca ışık yalan söyler") ve Bölüm 3'ün kuralı ("hiyerarşi biçimde değil ışıkta yaşar") burada aynı sonuca varıyor: MVP'de oyuncunun kendisi bir *karakter* olarak var olmuyor, çünkü bu oyunun tek "diğer insanı" da hiç bir model olarak var değil — otelin kendisi üzerinden hissediliyor (`birinci-sahis-kontrolcu.md`, `gorev-tasima-dongusu.md`). Bu bölüm bu boşluğu doldurmaya çalışmıyor; onun neden zaten doğru bir boşluk olduğunu açıklıyor, ve gelecekte doldurulacak iki gerçek karakter (arkadaş, psikiyatrist) için hiçbir taahhüt vermeden zemin hazırlıyor.

### 5.1 Oyuncu Karakterinin Görsel Arketipi — Eksik Beden Bir Boşluk Değil, Bir Karar

MVP'de oyuncunun görünür tek bedeni yok — `birinci-sahis-kontrolcu.md` ne bir gövde, ne bir gölge-model, ne bir ayna-yansıması tanımlıyor; tek görünür parça, Görev/Taşıma Döngüsü'nün `SetCarrying(true)` ile açtığı kol/el rigi. Bu bir eksiklik değil: Pillar 1'in (Öznel Gerçeklik) mantığı zaten "beden" kavramını **hissedilen** bir şeye indirgiyor — "Bedenin Hafızası," "Eller Zaten Biliyor" (`gorev-tasima-dongusu.md` Player Fantasy) hiçbiri görsel bir gövde gerektirmiyor, tam tersine bir gövdenin *görünmemesi* oyuncunun kendi bedeniyle değil otelin tepkisiyle özdeşleşmesini kolaylaştırıyor. Oyuncu karakteri burada bir mesh değil, otelin ışığa verdiği tepkinin öznesidir — bu, Bölüm 1-4'ün zaten kurduğu "önem ışıkta yaşar" ilkesinin en radikal uygulaması: karakterin kendisi bile biçim kanalını hiç kullanmıyor.

*Design test*: Bir sahneye ayna, parlak metal yüzey ya da yansıtıcı cam eklenmek istenirse — bu, oyuncunun yansımasını göstermeye zorlayacak bir açıdaysa reddedilir; MVP kapsamında hiçbir gövde modellenmeyecek, bu yüzden yansıtıcı yüzeyler ya kadraj dışına ya da bulanık/pratik-olmayan bir açıya yerleştirilmelidir. *(Pillar 1 birincil.)*

### 5.2 Ayırt Edici Özellik Kuralları — MVP'de Boş Bir Küme, Vertical Slice'a Devredilen Zemin

MVP'de birbirinden ayırt edilmesi gereken başka hiçbir karakter yok — bu alt bölümün klasik işi (iki karakteri siluet/renk/detay yoluyla ayırt etme) MVP kapsamında **uygulanacak bir kural değil**. Bu belgenin bu bölümdeki tek gerçek işi, Vertical Slice'ta gelecek arkadaş karakteri (Pillar: Fellowship/Bağ, 5/5 ile projenin en yüksek puanlı duygusal hedefi) ve psikiyatrist için **erken bağlanmamak**: onların tasarımı ayrı bir GDD/art geçişinin işi. Burada sabitlenen tek şey, o iş başladığında hangi kısıtların zaten miras alınacağı — Bölüm 3.4'ün "kahraman biçim etkileşim/kamuflaj katmanının dışında" kuralı ve Bölüm 2-4'ün ışık/renk grameri, gelecekteki karakterler için de geçerli kalacak: önemleri de biçim abartısından değil, ışık ve sıcaklıktan (Bölüm 1, Prensip 3 — "sıcaklık bağ sinyalidir, güvenlik değil") gelecek.

*Design test*: Şu an bu bölüm için bir prop/karakter tasarımcısı somut bir soru sorarsa (ör. "arkadaş karakteri nasıl görünsün") — cevap bu belgenin işi değildir; gelecekteki bir karakter GDD'si ve kendi art geçişi bekler. Bu belgenin verdiği tek taahhüt, o tasarımın Bölüm 1-4'ün kurallarıyla çelişemeyeceğidir.

### 5.3 İfade/Poz Stili — Kol/El Rigi İçin Zaten Cevaplanmış Bir Soru

MVP'nin tek karakter-benzeri varlığı olan kol/el rigi için "ifade" kelimesi klasik anlamda geçerli değil — Bölüm 3.4 ve `gorev-tasima-dongusu.md` (Visual Requirements, "Kol/El Rigi") bunu zaten kilitledi: **eşya başına tek statik tutuş pozu, blend-tree yok, ayrı animasyon state machine'i yok.** Tek hareket bileşeni, Birinci Şahıs Kontrolcü'nün mesafe-bazlı faz biriktiricisinden okunan hafif yürüyüş sallantısı (soket-offset, ayrı bir zamanlayıcı değil) — bu bile bir "ifade" değil, bir fiziksel tepki. CD-GDD-ALIGN'ın kendi notu bu gerilimi zaten adlandırmış: rigin sürekli görünürlüğü "Dikkatin Göçü" fantazisiyle (dikkat görevden ayrılırken elin geri çekmemesi gerekir) çatışabilirdi — bu çatışma, kesin bir animasyon-cilası yasağıyla çözülmüş durumda. Bu bölümün bu alt başlıkta yapacağı yeni bir tasarım işi yok; sadece bu kısıtı bir Art Bible kararı olarak teyit ediyor, ki hiçbir sanatçı ileride "biraz kişilik katalım" diye bunu yumuşatmasın.

*Design test*: Rig'e yeni bir davranış önerilirse (bekleme animasyonu, çevreye tepki, "canlılık" hissi için küçük bir fikir) — tek soru: bu bir ikinci poz veya bir state machine mi gerektiriyor? Evetse reddedilir; kısıt "statik poz + tek faz-bağlı sallantı" olarak kalır. `CarryItemDef` başına poz varyasyonu (tepsi iki elle, kutu tek kolda) bir içerik sorusudur, `/asset-spec` aşamasında çözülür — bu bir ifade tasarımı değil, bir envanter tasarımıdır.

### 5.4 LOD Felsefesi — Her Zaman Ekranda, Mesafe Hiç Değişmiyor

Kol/el rigi ve taşınan eşya, oyunun kamera soketine bağlı olduğu için **mesafe-bazlı bir LOD zincirine hiç ihtiyaç duymuyor** — oyuncu ondan asla uzaklaşamaz, o yüzden klasik "uzaktan basitleşir" mantığı burada anlamsız. Bunun yerine gerçek soru tersine döner: bu, oyuncunun her karede, ekranın en ön planında gördüğü tek asset sınıfı olduğu için, düşük poligon/materyal sayısı görünürlüğü etkilemez ama **çizim çağrısı ve materyal geçiş maliyeti** her zaman aktiftir (60fps/16.6ms bütçesi ve ~2000 çizim çağrısı tavanı, `.claude/docs/technical-preferences.md`). Görev/Taşıma Döngüsü'nün kendi tasarımı zaten bunu destekliyor: N (2-4) önceden havuzlanmış temsil, sahne başına bir kez oluşturuluyor, her alımda instantiate/destroy yok (`gorev-tasima-dongusu.md` Core Rules) — bu, bu bölümün önerebileceği herhangi bir performans disiplinini zaten önden çözmüş durumda. Kesin poligon/doku bütçesi bu belgenin işi değil — Bölüm 8 (Asset Standards) henüz yazılmadı ve o sayıyı orada sahiplenecek; bu bölüm sadece kısıtın var olduğunu ve rigin "her zaman LOD0" bir asset sınıfı olduğunu not düşüyor.

*Design test*: Bir sanatçı kol/el rigi veya taşınan eşya için "uzak LOD" varyantı önerirse — reddedilir, çünkü mesafe hiç değişmiyor; asıl disiplin poligon/materyal sayısını **tek** LOD seviyesinde düşük tutmaktır, kesin sayı Bölüm 8'i bekler.

### Açık Sorular

- **Psikiyatrist NPC'nin görsel temsili tanımsız.** Bölüm 2.4, AD-ART-BIBLE (2026-08-05) revizyonundan sonra geçici bir varsayılan taşıyor — "duyulur, illa görülmez," oyuncu-POV'una yakın bir kadraj — ama bu **provizyonel**, kilitli değil. Psikiyatristin tam modellenmiş bir karakter mi, bir siluet mi, kadraj-dışı bir ses mi, yoksa soyut bir temsil mi olacağı hâlâ tanımsız. Bu belge bir cevap uydurmuyor — sahip: ileride yazılacak karakter GDD'si / narrative-director geçişi. O GDD yazıldığında Bölüm 2.4'ün kadraj notu buna göre güncellenmeli.
- **Arkadaş karakterinin görsel arketipi tamamen tasarlanmamış.** Pillar olarak (Fellowship, 5/5) en yüksek öncelikli duygusal hedef olsa da, `birinci-sahis-kontrolcu.md` bile onun arayüzünün "o GDD yazılınca netleşeceğini" söylüyor. Bu belge yalnızca Bölüm 3-4'ün kurallarının o tasarıma miras kalacağını sabitliyor, karakterin kendisini tasarlamıyor.

## 6. Environment Design Language

### Çevre Kuralı

> **Çevre asla anlatmaz, yalnızca tutar — anlam nesnede değil, aynı mekânın ikinci kez nasıl göründüğünde yaşar.**

"Otel Senin Yerine Hatırlıyor" ana yönü (`game-concept.md` Visual Identity Anchor) burada üretim diline çevriliyor. Bir çevre sanatçısı bu bölümü şu tek cümleyle özetleyebilmeli: **görev, bir alanı "ilginç" ya da "hikâye dolu" kılmak değil, o alanı gerçek ve unutulmamış bırakmak — hikâyenin kendisi, aynı geometrinin Bölüm 2'nin dört durumu arasında nasıl yeniden göründüğünden çıkar, çevre sanatının kendi başına eklediği hiçbir vurgudan değil.** Bu, çoğu anlatı-odaklı oyunun environment art mantığının (her odaya bir "okunacak" sahne kur) kasıtlı tersidir — sebebi Bölüm 3.1'in zaten kilitlediği kamuflaj kısıtı: anı-tetikleyiciler ve sahte/dekor `IInteractable` nesneleri (`ani-tetikleyici-etkilesim.md` Core Rules, "kasıtlı kamuflaj") aynı sıradan donanım diline gömülü kalmak zorunda. Bu bölümün dört alt başlığı bu tek kısıtın mimari, doku, prop yoğunluğu ve anlatı katmanlarındaki karşılığıdır.

### 6.1 Mimari Stil — Kültür/Tarih İlişkisi

Bölüm 3.2 zaten şunu kilitledi: misafir yüzü (lobi, süit, avize) hiç görünmez, oyuncu her zaman kulisin arka tarafındadır. Bu tek gerçek, mimarinin kültürel/tarihsel ilişkisini de kendiliğinden belirliyor: **oyuncunun gördüğü mimari, bir "Türk/Akdeniz otel dekoru" değil, uluslararası zincir otelciliğin kendi işletme kültürüdür.** Gerçek dünya referansı (Antalya DoubleTree by Hilton, `game-concept.md` Inspiration and References) burada süsleme düzeyinde değil, **tipoloji** düzeyinde çalışır: 1990'lar-2010'lar Akdeniz kıyı turizm patlamasının tipik betonarme-iskelet inşaat mantığı (geniş açıklıklı kolon-kiriş ızgarası, yangın yönetmeliğine göre genişlikte servis koridorları, envanteri düşünülerek planlanmış depo hacimleri) — bu, Bölüm 3.2'nin zaten tanımladığı "4m×4m ızgaraya hizalı, doksan derece köşeli, yapısal olarak dürüst" geometrinin somut, gerçek-dünya kaynağıdır.

Bunun anlatısal sonucu kasıtlı ve önemli: **kültürel/bölgesel kimlik, oyuncunun göreceği hiçbir yüzeyde ifade edilmiyor.** Zincir otelciliğin arka-alan mimarisi dünyanın hangi ülkesinde olursa olsun büyük ölçüde aynı işletme mantığından (verimlilik, kod uyumu, standart modüler inşaat) doğar — Antalya'daki bir DoubleTree'nin servis koridoru, başka bir kıtadaki bir zincir otelin servis koridoruna şaşırtıcı derecede benzer. Bu "yüzsüzlük" bir eksiklik değil, temanın kendisine hizmet eder: mekân kasıtlı olarak **herhangi bir yer** gibi hissettirir, ama oyuncunun kişisel travması tam da bu herhangi-bir-yerin içine kazınmıştır — korku, "tuhaf/egzotik bir mekân" değil, **herkesin tanıdığı bir koridorun** kendisidir. Otel kimliğinin ne kadar tanınabilir (marka logosu, üniforma nakışı, spesifik zincir donanımı) tutulacağı — gerçek DoubleTree markasına sadık mı, yoksa kurgusal-jenerik bir eşdeğere mi çekilecek — bu belgenin cevaplayacağı bir soru değil; bkz. Açık Sorular.

*Design test*: Bir çevre sanatçısı bir servis alanına "buranın Türkiye'de olduğunu belli eden" dekoratif bir detay (bölgesel motif, yerel malzeme dokusu, kültürel referans obje) eklemek isterse — bu prensip reddeder; kültürel kimlik bu oyunun görünür katmanında yaşamıyor, çünkü misafir tarafı zaten hiç görünmüyor (Bölüm 3.2) ve arka-alan mimarisi kasıtlı olarak jenerik/uluslararası kalmalı. *(Pillar 3 birincil — Görev Gerçekliği, gerçek bir zincir otelin arka-alan mantığına sadakat.)*

### 6.2 Doku Felsefesi — PBR Birincil, Boyalı Değil, "Stilize" Sadece Kalibrasyonda

`game-concept.md` Technical Considerations'ın Art Style satırı zaten yönü veriyor: **"3D stilize-gerçekçi, modüler otel kiti."** Bu bölüm bunu doku üretimine çeviriyor ve tek bir kritik ayrımı netleştiriyor: **"stilize" burada bir shading model tercihi değil (cel-shading, hand-painted stroke, toon-ramp yok), sadece bir kalibrasyon derecesidir** — fiziksel olarak gerçekçi PBR malzeme tepkisi (albedo/metallic/smoothness, URP standart Lit shader) korunur, ama her yüzey fotogrametrik gürültü/yıpranma ile aşırı detaylandırılmaz. Sebebi doğrudan bütçe değil (o Bölüm 8'in işi), **anlatı disiplini**: Bölüm 1'in "hiçbir şey yeniden dekore edilmez" kuralı ve Bölüm 3.1'in kamuflaj kısıtı, her yüzeyin "tasarlanmış" değil "gerçek ve sıradan" okunmasını zorunlu kılıyor — aşırı-detaylı, "ilginç" bir doku bile 3.1'in ihlal ettiği türden bir sessiz vurgu haline gelebilir.

**Boyalı (hand-painted/baked lighting-into-albedo) doku neden dışlanıyor**: `isik-volume-durum-sistemi.md`'nin kendi notu (Formulas bölümü, Mixed ışık modu üzerine) zaten bir riski işaretlemiş durumda — bu projenin static-flagged modüler kiti baked/Mixed aydınlatma kullanıyor (`game-concept.md` Art Pipeline Complexity: "modüller baked lighting için static-flagged olmalı, ikinci UV kanalı gerektirir"), yani GI/bounce ışığı statik kalıyor, sadece direkt ışık konisi anı-kaymasıyla değişiyor. Bir albedo dokusuna elle boyanmış yön-bağımlı gölge/AO gömülürse, bu gölge bir anı-kayması sırasında **asla değişmez** — oyuncu ışık soğusa bile dokunun içindeki "sahte sıcak gölge"yi görmeye devam eder. Bu, Bölüm 1'in temel kuralını (yalnızca ışık yalan söyler) doğrudan ihlal eder: yalan artık ışıkta değil, dokunun kendisinde donmuş olur. Bölüm 4.1'in "Temel Malzeme Grisi" notu (`#B5A897`, "kendi başına ne sıcak ne soğuk... kimliğini üzerine düşen ışıktan alır") bu kuralın rengi için zaten sabitlediği ilkeyi, bu bölüm dokunun **gölgelendirmesi** için tekrar ediyor: **albedo yönsüz/düz kalmalı, tüm yön-bağımlı gölgeyi gerçek zamanlı ışık üretmeli**, sadece çok yakın-mesafe, yönsüz mikro-detay (kir birikintisi, çizik, aşınma — hiçbiri belirli bir ışık yönüne bağlı olmayan) albedoya gömülebilir.

| Doku katmanı | Yaklaşım | Neden |
|---|---|---|
| Albedo (baz renk) | PBR, düz/yönsüz, gerçekçi ama "temiz" — fotogrametrik gürültü değil | Bölüm 1: yalan ışıkta yaşamalı, dokuda donmamalı |
| Yön-bağımlı gölge/AO | Gerçek zamanlı (baked GI + dinamik direkt ışık), asla albedoya elle boyanmaz | Aynı — anı-kayması sırasında donmuş bir "yanlış" gölge kamuflajı ve Pillar 1'i kırar |
| Mikro-detay (kir, çizik, aşınma) | Serbest — albedo/normal/roughness haritasında yönsüz doku olarak var olabilir | Gerçekçilik/Pillar 3'e katkı, ışığın yalanına müdahale etmiyor |
| Metallic/Smoothness | PBR standart, gerçek malzeme referansına sadık (paslı metal raf, mat sıva, cilalı ahşap) | "Gerçekçi-stilize" ifadesinin gerçekçi tarafı burada yaşıyor |

*Design test*: Bir doku sanatçısı bir rafa/duvara elle boyanmış, "atmosferik" bir gölge/vinyet eklemek isterse (ör. bir köşeyi doğal olarak karartmak için) — bu prensip reddeder; karartma her zaman gerçek zamanlı ışık/GI'dan gelmeli, yoksa doku anı-kaymasına tepkisiz kalan sabit bir katman haline gelir. *(Pillar 1 birincil.)*

### 6.3 Prop Yoğunluğu Kuralları — Alan Tipine Göre

Prop yoğunluğu bu projede iki bağımsız güç tarafından belirleniyor ve bu iki güç bazen aynı yöne, bazen zıt yöne çekiyor: **(a) o alanın gerçek işletme mantığı** (Pillar 3) ve **(b) kamuflaj ihtiyacı** (Bölüm 3.1 — anı-tetikleyicilerin gömülü kalabileceği bir "sıradan nesne gürültüsü" olması gerekiyor, `birinci-sahis-kontrolcu.md` AC'sinin zaten zorunlu kıldığı "her alanda en az bir sahte/dekor `IInteractable`" içerik gereksinimi bunu somutlaştırıyor). Üçüncü bir sınır ise teknik: static-flagged modüler kit + ~2000 çizim çağrısı tavanı (`technical-preferences.md`) yoğunluğu sonsuz artıramaz — modül tekrarı/prop havuzlama zorunlu.

| Alan | Yoğunluk | Gerekçe |
|---|---|---|
| **Depo** | **En yüksek.** İstiflenmiş kutu, envanter rafı, kullanılmış malzeme — gerçek bir depo hiç boş durmaz. | Pillar 3 (envanter mantığı, Bölüm 3.2 tablosu) ve kamuflaj ikisi de aynı yönde: yoğun, sıradan nesne kalabalığı hem gerçekçi hem de tetikleyiciyi gömmek için ideal zemin. |
| **Servis Koridoru** | **En düşük / bilinçli seyrek.** Kapı/aplik ritmi tekrar eder ama zemin/duvar büyük ölçüde açık kalır. | Gerçek bir otelde servis koridorları yangın/tahliye yönetmeliği gereği açık tutulur, ayrıca bu Bölüm 3.2'nin "modüler kitin en saf tekrar birimi" tanımını görsel olarak destekler — burada kamuflaj yoğunluktan değil, **tekrardan** gelir: her kapı kolu/anahtar diğerleriyle aynı görünür, tek bir öne çıkan yok. |
| **Balo Salonu (hazırlık tarafı)** | **Orta, dalgalı.** Yarım kurulmuş masalar, sandalye yığınları, rulo halılar — iş "ortasında yakalanmış" (Bölüm 2.1). | Bu, misafir tarafına en yakın nokta olduğu için yoğunluk sabit değil, o gecenin kurulum ilerlemesine göre değişken hissettirilmeli — Bölüm 3.2'nin tek kavis istisnası (taşınan lüks eşya) da burada en çok görünür. |
| **Asansör** | **Neredeyse sıfır.** Kabin içi sabit, minimal, tamamen "camera-safe." | Bölüm 2.3'ün zaten kilitlediği kural: kabin her açıdan tam kapalı/gerçek görünmeli, hiçbir render açığı olmamalı — gevşek/serbest prop eklemek bu tamlığı riske atar ve Ölü Zaman'ın işlevsel boşluğuyla çelişir. |

**Kamuflaj-yoğunluk gerilimi üzerine not**: Depo ve Balo Salonu'nda yüksek/orta yoğunluk kamuflaja doğal olarak yardım ederken, Servis Koridoru'nun **düşük** yoğunluğu tam tersi bir stratejiye dayanıyor — az sayıda ama **birbirinin birebir aynısı** donanım parçası (tekrarlayan kapı kolu/anahtar modülü). Bir çevre sanatçısı koridoru "daha az sıkıcı" kılmak için varyasyon eklemeye kalkarsa, bu aslında kamuflajı zayıflatır: varyasyon, hangi kapı kolunun "farklı" (=belki gerçek) olduğunu ima etmeye başlar. Bu, Bölüm 3.1'in kuralının (küçük ölçekli donanım "sessiz" kalmalı) yoğunluk eksenindeki doğrudan uzantısıdır.

*Design test*: Bir level designer bir alanı "daha ilgi çekici" kılmak için prop yoğunluğunu artırmak isterse — önce sorulması gereken soru "bu, o alanın gerçek işletme mantığına mı uyuyor, yoksa sadece görsel zenginlik için mi" olmalı; ikincisiyse reddedilir (Bölüm 1: hiçbir şey yeniden dekore edilmez). Servis Koridoru özelinde ek soru: yeni prop **tekrarlayan** mı, yoksa **tekil/farklı** mı — tekilse, kamuflaj riski nedeniyle level designer ve narrative-director onayı gerekir.

### 6.4 Çevresel Anlatı İlkeleri — Metinsiz, Vurgusuz, Sona Saklı

Bu oyunun environmental storytelling'i, türün kendi referans noktalarından (`game-concept.md` Inspiration and References — Gone Home, What Remains of Edith Finch) **kasıtlı olarak** ayrılıyor: o oyunlarda hikâye, oyuncunun bulduğu **tekil, öne çıkan** nesneler üzerinden ilerler (bir mektup, bir eşya, "bak bu önemli"). Bölüm 3.1 ve 3.4'ün zaten kilitlediği kamuflaj kuralı bu modeli bu oyunda **yasaklıyor** — hiçbir nesne "burada hikâye var" diyen bir siluet/vurgu taşıyamaz. Sonuç: bu oyunun environmental storytelling'i **mekân** ölçeğinde çalışmak zorunda, **nesne** ölçeğinde değil.

**İlke 1 — Hikâye tekrardan doğar, keşiften değil.** Oyuncu aynı koridoru, aynı depoyu gece boyunca defalarca geçer (Görev/Taşıma Döngüsü). Anlatı katmanı bu tekrarın üzerine kurulu: bir alan ilk geçişte "sadece iş" okunur, ama Bölüm 2.2'nin ışık kayması onu ikinci/üçüncü geçişte farklı gösterdiğinde, oyuncunun zaten tanıdığı bir mekân aniden "yeniden okunur." Bu, Bölüm 1'in "hiçbir şey yeniden dekore edilmez" kuralının anlatı karşılığıdır: çevre sanatçısı hikâyeyi **yeni bir nesne ekleyerek değil, mevcut nesnenin ışık altında nasıl göründüğünü değiştirerek** anlatır.

**İlke 2 — Gerçek dağınıklık, birincil anlatı katmanıdır.** Bölüm 2.1'in zaten talep ettiği "gerçek, sahnelenmemiş dağınıklık" (istiflenmiş kutular, yarım masa düzenleri, kullanılmış servis arabaları) bu oyunun **tek metinsiz anlatı aracı** olarak işlev görüyor — kimin ne kadar yorgun/acele çalıştığını, gecenin hangi saatinde olunduğunu, işin ne kadarının bittiğini; hiçbiri bir "not" ya da "günlük" olmadan, sadece iş yerinin kendi fiziksel izinden okunuyor. Bu belge yeni bir okunabilir-belge mekaniği önermiyor (böyle bir mekanik hiçbir GDD'de tanımlı değil) — anlatı, nesnelerin **varlığından**, hiçbirinin **söylediğinden** değil.

**İlke 3 — Kişisel dokunuş, kamuflajın içine gizlenir, asla kendi başına durmaz.** Protagonistin hipervijilansını/geçmiş ilişkisini ima edebilecek küçük insani detaylar (düzenli katlanmış bir mont, bir şarj kablosu, yarım kalmış bir kahve) mümkün — ama bunlar Bölüm 3.1'in kuralına tabi: gerçek bir gece vardiyası çalışanının bırakacağı türden, **sıradan** nesneler olarak durmalı, asla "bak bu onun" diyen bir çerçeveleme/vurgu almamalı. Anlam, oyuncu bu detayı fark etsin ya da etmesin, sadece **birikerek** çalışır — Pillar 5'in (Anlam Sona Saklı) çevre sanatındaki karşılığı budur.

**İlke 4 — Alan-başına renk sıcaklığı (Bölüm 4.3), sessiz bir ikincil anlatı katmanıdır.** Depo'nun en doygun/kızıl ışığı, Servis Koridoru'nun orta tonu, Balo Salonu'nun en açık/nötr tonu — bu üçü zaten "hangi alan misafire ne kadar yakın" sorusunu ışıkla cevaplıyordu (Bölüm 4.3 gerekçesi). Bu bölüm bunu bir anlatı ilkesi olarak teyit ediyor: oyuncu hiçbir metin görmeden, sadece ışığın "temizliği" arttıkça misafir tarafına yaklaştığını hisseder — bu, otelin kendi hiyerarşisinin (kim görünür, kim görünmez) sessiz bir çevirisidir.

*Design test*: Bir level designer/narrative-director bir alana "oyuncunun kesinlikle fark etmesi gereken" bir anlatı ipucu yerleştirmek isterse — önce sorulmalı: bu ipucu bir nesnenin **varlığıyla** mı, yoksa nesnenin **vurgusuyla/çerçevelenmesiyle** mi taşınıyor? İkincisiyse reddedilir (Bölüm 3.1/3.4 ihlali). Doğru soru "bunu ilk geçişte mi, yoksa bir ışık-kayması sonrası ikinci geçişte mi okutuyorum" olmalı — çoğu durumda doğru cevap ikincisidir. *(Pillar 5 birincil, Pillar 1 ikincil.)*

### Açık Sorular

- **Otel markasının tanınabilirlik derecesi tanımsız.** Gerçek dünya referansı (Antalya DoubleTree by Hilton) ilham kaynağı olarak `game-concept.md`'de zaten kayıtlı, ama üretim düzeyinde marka logosu, üniforma nakışı, zincire özgü donanım deseni gibi tanınabilir detayların doğrudan kullanılıp kullanılmayacağı (ya da kurgusal-jenerik bir eşdeğere çekilip çekilmeyeceği) bu belgenin cevaplayacağı bir soru değil — hem yasal/marka kullanım hem de anlatısal mesafe (gerçek bir zincirin adını taşımak, kurgusal "herhangi bir otel" hissini nasıl etkiler) açısından ayrı bir karar gerektiriyor. Sahip: creative-director + hukuki inceleme.
- **Prop yoğunluğu için sayısal bir tavan henüz yok.** Bölüm 6.3'ün alan-başına yoğunluk sıralaması (Depo > Balo Salonu > Servis Koridoru > Asansör) niteliksel bir hiyerarşi kuruyor, ama bunu somut prop-sayısı/draw-call bütçesine çevirmek Bölüm 8'in (Asset Standards) işi — bu belge sadece sıralamayı ve gerekçesini sabitliyor, sayıyı icat etmiyor.

## 7. UI/HUD Visual Direction

### UI Kuralı

> **Arayüz dünyayı taklit etmez; arayüz sadece güvenilir olur.**

**Kapsam notu**: Bu oyunun MVP'sinde tek bir UI yüzeyi var — crosshair/prompt ve ona bağlı hold-doldurma göstergesi (`etkilesim-sistemi.md` Core Rules). Envanter, sağlık göstergesi, minimap, görev/round takipçisi **yok** — `gorev-tasima-dongusu.md` Core Rules bunu kasıtlı olarak "sıfır ekran-uzayı HUD elemanı" diye adlandırıyor; taşınan yük sayısı sinyali kol/el rigi'nin kendi round-bazlı ışık/çerçeve sönmesiyle taşınıyor (Bölüm 3.4, `Highlight(round)`; Bölüm 5.3, statik-poz disiplini), ayrı bir HUD sayısı olarak değil. Bu bölüm bu yüzden alışılmadık dar: aşağıdaki dört başlık, aslında **tek bir görsel nesnenin** dört yönünü tarif ediyor. Ayarlar/duraklatma/ana menü için henüz bir GDD yok — bu belge onlar için tasarım üretmiyor, kapsam dışı olarak not düşüyor.

### 7.1 Diegetik mi, Ekran-Uzayı mı — Sorunun Zaten Tek Cevabı Var

Bu oyunda diegetik bir UI **mümkün değil**, ve bu bir tercih değil, Bölüm 3.3'ün kendi mantığının doğrudan sonucu. Crosshair, Pillar 1'in (Öznel Gerçeklik) tek istisnasıdır — her zaman doğru okunmalı, hiçbir zaman dünyanın öznelliğine girmemeli (`etkilesim-sistemi.md`, "Pillar 1 muafiyeti"). Bir UI elemanı dünyanın **bir parçası** gibi görünürse (bir duvara kazınmış gibi, bir nesnenin üstünde asılıymış gibi), o an dünyanın öznel/yalancı ışığının bir parçası olma riskini taşır — ve muafiyetin tüm amacı bunu engellemekti. Yani diegetik-vs-ekran-uzayı tartışması bu projede gerçek bir seçenek değil: **%100 ekran-uzayı, %100 overlay.** Crosshair asla bir 3D sahne nesnesi olarak render edilmez, asla kamera derinliğinde bir yüzeye oturtulmaz — daima ekranın kendi sabit katmanında yaşar.

*Design test*: Biri "crosshair'i hafifçe 3D dünyaya oturtsak, daha 'içine girmiş' hissettirir mi" diye sorarsa — hayır; bu, Pillar 1 muafiyetinin var oluş sebebini (her koşulda güvenilir okuma) çökertir.

### 7.2 Tipografi Yönü

Tek metin yüzeyi, `IInteractable.PromptText`'in gösterdiği kısa eylem kelimeleridir ("Al", "Çek", "Tut") — bu yüzden bir tipografi *hiyerarşisi* kurmuyoruz, bir tipografi *karakteri* kuruyoruz. Yönerge: **işlevsel grotesk/sans, orta ağırlık (regular/medium — asla bold, asla ince-ekstra-light).** Bold, Bölüm 3.3/1'in reddettiği "şok/alarm" hissine yaklaşır — bir prompt kelimesi bağırmamalı, sadece bilgilendirmeli. Ekstra-ince ağırlık ise crosshair'in "her koşulda güvenilir okunmalı" gerekliliğiyle çatışır (düşük kontrastta erir). Serif, el yazısı, otel tabelası/logo hissi veren hiçbir font ailesi düşünülmez — Bölüm 3.3'ün "otel donanım estetiğinden ödünç alınmış hiçbir detay UI'a sızmaz" kuralı fonta da uygulanır. Boyut: tek bir okuma mesafesi/tek bir bağlam olduğundan (crosshair'in hemen altı/yanı), çoklu boyut skalası kurmaya gerek yok — bir boyut, sabit, ekran çözünürlüğünden bağımsız güvenli bir asgari punto (kesin punto değeri henüz belirlenmedi, bkz. Açık Sorular).

*Design test*: Bir prompt kelimesi "önemini vurgulamak için" büyütülsün/kalınlaştırılsın önerisi gelirse — reddedilir; önem burada da (Bölüm 3.4'ün genel ilkesiyle aynı mantık) biçim/tipografi kanalından değil, zaten Focused durumunun kendi opaklık/ölçek kaymasından gelir.

### 7.3 İkonografi Stili

Bu oyunda klasik anlamda bir "ikon seti" yok — tek görsel-sembolik eleman, Bölüm 3.3'ün zaten kilitlediği **saf geometrik ilkel** (nokta/ince daire, tek çizgi kalınlığı) ve ona eşlik eden hold-doldurma halkası. Stil ekseni tartışmasız: **flat/geometrik**, outlined bile değil (outline bir "çizilmiş" karar taşır, doğrudan çizgi kalınlığı yeterli) — illüstratif ya da fotogerçekçi bir yön (`gorev-tasima-dongusu.md`'nin reddettiği türden bir "cila") baştan dışarıda, çünkü bu ilkelin tek işi kimlik değil güvenilirlik taşımak. Yeni bir ikon (ör. farklı etkileşim tipleri için farklı sembol) ihtiyacı MVP kapsamında yok — `IInteractable.PromptText` metinle çözüyor, sembolle değil; bu belge var olmayan bir ikon dili icat etmiyor.

*Design test*: Bir etkileşim tipi için (Instant vs Hold) ayrı bir sembol/ikon önerilirse — reddedilir; ayrım zaten hold-doldurma halkasının varlığı/yokluğuyla ve prompt metniyle taşınıyor, yeni bir görsel kanal açmaya gerek yok.

### 7.4 Animasyon Hissi

**Yumuşak, asla mekanik-keskin, asla organik-abartılı — ortada, sessiz bir orta nokta.** İki referans nokta zaten kilitli: Idle→Focused geçişi küçük bir ölçek/opaklık kayması (`#E4E4E0` ~%65 → `#FFFFFF` %100, Bölüm 4.4), **patlama/parlama değil** (Bölüm 3.3). Bu, easing tercihini kendiliğinden belirliyor: yumuşak bir ease-in-out (smoothstep ailesi, Bölüm 2.2'nin zaten kullandığı "3x²-2x³" eğrisiyle aynı aile), kısa süre (birkaç yüz milisaniye bandı) — spring/bounce/elastic easing kesinlikle yok, çünkü bu bir "canlılık" göstergesi değil bir durum bildirimi. Hold-doldurma halkası ise farklı bir disipline tabi: doğrudan etkileşimin kendi hesapladığı `t` (0→1) değerine bağlı, **1:1 lineer okuma** — kendi başına ayrı bir easing eğrisi taşımaz, çünkü halkanın tek görevi ilerlemeyi *dürüstçe* göstermek (crosshair'in Pillar 1 muafiyetinin doğrudan uzantısı: gösterge de yalan söylememeli, gerçek ilerlemeyi kırıp bükmeden yansıtmalı). `SuppressDefaultHoldFill=true` durumunda (anı-tetikleyicileri, `etkilesim-sistemi.md`) bu animasyon tarifinin tamamı devre dışıdır — bu, eksik bir uygulama değil, zaten kilitli bir anlatı kararı; sanat ekibi burada "sessizliği bir şekilde telafi edelim" diye alternatif bir mikro-animasyon icat etmemeli.

*Design test*: Bir UI geçişi için "biraz daha hissedilir/tatmin edici olsun, hafif bir overshoot/bounce ekleyelim" önerisi gelirse — reddedilir; overshoot bir *karakter* iması taşır, crosshair'in tek görevi karaktersiz kalmaktır (Bölüm 3.3, Pillar 2).

### 7.5 UX Uyum Kontrolü — Kontrast Garantisi ve Açık Sorular

Bu bölüm, art-director taslağıyla paralel çalışan bir ux-designer geçişinden (accessibility checklist: renk-bağımsızlık, klavye/gamepad-only kullanılabilirlik, minimum okunabilir punto, flaş-yok) gelen tek somut bulguyu kapatıyor ve iki gerçek boşluğu açıkça bırakıyor — icat etmiyor.

**Kontrast garantisi (yeni kilitli kural)**: Bölüm 4.4/7.1-7.4'ün tanımladığı achromatic, opacity/scale-only crosshair (`#E4E4E0` ~%65 → `#FFFFFF` %100) hiçbir outline/stroke olmadan, parlak/düşük-kontrastlı sıcak-amber duvar dokusu (Bölüm 4.3 BaseColor aileleri) önünde gerçek bir okunabilirlik riski taşıyordu — bu bir varsayım değil, iki paletin de (sıcak amber ve soğuk anı-mavisi/yeşili) crosshair'in neredeyse-beyaz tonuna yeterince yakın parlaklık bantlarına girebileceği somut bir kontrast sorunu. Çözüm, achromatic/no-flash kuralını hiç bozmadan kapanıyor: **crosshair ve hold-doldurma halkası, ince bir koyu outline/drop-shadow taşır — ~%40-60 opaklıkta siyah, 1-2px.** Bu bir renk değil (nötr siyah, iki dünya paletinden de bağımsız), bir parlama değil (statik bir kontur, animasyonsuz) — sadece bir okunabilirlik garantisidir, Bölüm 4.4'ün UI paleti tablosuna eklenen tek yeni satır.

*Design test*: Bir sanatçı outline'ı "biraz daha yumuşak/görünmez" yapmak isterse — kontrast garantisi test edilmeden (hem Vardiya Amberi hem Anı Mavisi/Sodyum Yeşili arka planına karşı) reddedilir; outline'ın tek görevi her iki dünya durumunda da okunabilirliği garanti etmektir, estetik bir incelik değildir.

**Açık Sorular (ux-designer geçişinden, cevap uydurulmadı)**:
- **Prompt metni font boyutu/ölçekleme tanımsız.** Hiçbir GDD veya bu belge, `IInteractable.PromptText` için minimum okunabilir punto ya da çözünürlükten-bağımsız ölçekleme değeri tanımlamıyor. Sahip: `/asset-spec` veya gelecekte yazılacak `design/ux/accessibility-requirements.md`.
- **Stinger-caption erişilebilirlik mekanizmasının kesin metin/stil kararı açık.** `adaptif-ses-sistemi.md` (AC14a) işitme engelli oyuncular için anı-tetikleyici stinger'ının bir caption/altyazı mekanizmasına bağlanması gerektiğini zaten işaretlemiş, ama bu henüz yazılmamış `design/ux/accessibility-requirements.md`'ye bağlı — bu belge bir stil/görsel karar üretmiyor, sadece bu bağımlılığın var olduğunu teyit ediyor. Not: `SuppressDefaultHoldFill=true` (anı-tetikleyicilerin sıfır-geri-bildirim kararı) kendi başına sorgulanmıyor — kasıtlı ve kilitli bir anlatı kararı — ama tamamlanma onayının asıl taşıyıcısı olan ışık+ses kanalının erişilebilirlik tarafı bu üç madde (font boyutu, caption, ayarlar menüsü yokluğu) üzerinden aynı bekleyen dosyaya bağlanıyor; ayrı ayrı değil, birlikte ele alınmalı.

## 8. Asset Standards

### Üretim Kuralı

> **Bir asset'in disiplini, ekranda ne kadar kaldığından gelir — ne kadar "önemli" olduğundan değil.**

Bölüm 3-7'nin kurduğu ilke burada üretim diline çevriliyor: önem bu oyunda hiç biçim/detay kanalından gelmiyordu (Bölüm 3.4), her zaman ışıktan geliyordu. Bu bölüm aynı mantığı asset bütçelemesine taşıyor — bir nesnenin ne kadar yüksek çözünürlük/detay hakkı olduğunu belirleyen şey onun anlatısal "önemi" değil, oyuncunun kameraya olan mesafesi ve o nesneyle geçirdiği kümülatif ekran-süresidir. Bir sanatçı bu bölümü tek cümleyle özetleyebilmeli: **bütçe önemden değil, görünürlükten dağıtılır.**

Bu bölüm iki katmandan oluşuyor: 8.1-8.5 sanat yönetmenliğinin tercih/felsefe katmanı, 8.6-8.11 teknik sanatçının kesin sayı katmanı — ikisi paralel yazıldı, birbirini tamamlıyor, hiçbir çelişki yok. Kesin rakamlar bu projede ilk kez burada sabitleniyor; `.claude/docs/technical-preferences.md`'nin kilitli tavanlarından (~2000 çizim çağrısı, 4GB bellek, 16.6ms kare bütçesi, Unity 6.3 LTS/URP, PC-only) geriye doğru bölünüyor.

### 8.1 Dosya Formatı Felsefesi

Kaynak/teslim ayrımı net tutulmalı: doku üretiminde katmanlı, non-destrüktif kaynak dosyalar (Substance/Photoshop) korunur, ama motora giren teslimat her zaman **kayıpsız** formattadır (PNG tercih edilir, JPG'nin blok-sıkıştırma artefaktları reddedilir). Gerekçe doğrudan Bölüm 6.2'ye bağlanıyor: albedo yönsüz/düz kalmak zorunda (baked gölge yasağı) — lossy sıkıştırmanın ürettiği hafif blok/banding deseni, özellikle düz/nötr Temel Malzeme Grisi (`#B5A897`) üzerinde, gerçek zamanlı ışığın okuduğu yüzeyde sahte bir doku-varyasyonu gibi algılanabilir; bu, ışığın tek yalancı olması gereken bir yüzeye ikinci, istenmeyen bir "gürültü katmanı" ekler. Mesh'ler için Unity-standart FBX tercih edilir — hem statik modüler kit hem de kol/el rigi için tek, tutarlı format; rig dosyası, Bölüm 5.3'ün "tek statik poz, blend-tree yok" kararını export düzeyinde de yansıtmalı — gereksiz animasyon eğrisi/blend shape taşımamalı.

*Design test*: Bir doku için "biraz sıkıştırıp JPG olarak teslim etsek, disk alanı kazanırız" önerisi gelirse — bellek/disk kararı teknik-artist'in işi, ama albedo katmanı özelinde kayıpsız format zorunlu kalır; Bölüm 6.2'nin yönsüzlük garantisi format seçimine bağlı bir garanti.

### 8.2 İsimlendirme Yönü

`art-director.md`'nin proje-geneli kalıbı burada bu oyunun gerçek asset kategorilerine uygulanıyor: **`[kategori]_[isim]_[varyant]_[boyut].[uzantı]`**

| Kategori | Örnek | Kapsar |
|---|---|---|
| `env_` | `env_koridorduvar_standart_large.png` | Modüler kit parçaları (4m×4m ızgara, Bölüm 3.2) |
| `prop_` | `prop_kutu_yigin_medium.png`, `prop_kapikolu_standart_small.png` | Gerçek ve sahte/dekor `IInteractable` nesneleri, set dekoru — Bölüm 3.1'in kuralı gereği **aynı adlandırma disiplini** hem gerçek tetikleyicilere hem sahtelerine eşit uygulanır, isimden bile ayırt edilebilirlik sızmamalı |
| `char_` | `char_kolelrigi_tutus_01.png` | Kol/el rigi — tek karakter-benzeri asset sınıfı (Bölüm 5) |
| `ui_` | `ui_crosshair_idle.png`, `ui_holdring_fill.png` | Section 7'nin tek görsel asset sınıfı |

`vfx_` kategorisi şimdilik açılmıyor — projede henüz tasarlanmış bir VFX sistemi yok; ihtiyaç doğduğunda kalıba eklenir, icat edilmez.

*Design test*: Bir dosya adında gerçek/sahte tetikleyici ayrımı sızdıran bir ipucu bulunursa (`prop_gercektetik_...` gibi) — reddedilir; dosya adı bile Bölüm 3.1'in kamuflaj kısıtına tabidir.

### 8.3 Doku Çözünürlük Katmanları — Öncelik, Sayı Değil

Bu alt bölüm hangi asset sınıfının hangi katmanı hak ettiğini sıralıyor; kesin texel/piksel tavanları 8.7'de:

| Öncelik | Sınıf | Gerekçe |
|---|---|---|
| **En yüksek** | Kol/el rigi + taşınan eşya | Bölüm 5.4: mesafe hiç değişmiyor, her zaman ekranın en ön planında — LOD zinciri yok, tek katman en yüksek disiplini taşımalı |
| **Yüksek** | Depo hero yüzeyleri (raf/kutu tekrar birimleri) | Bölüm 6.3: en yoğun alan, en çok kümülatif ekran-süresi |
| **Orta** | Balo Salonu hazırlık yüzeyleri, Servis Koridoru hero modülleri | Orta yoğunluk / tekrar-ağırlıklı görünürlük |
| **Düşük, ama tutarlı** | Küçük ölçekli donanım (kapı kolu, anahtar, termostat — gerçek ve sahte tetikleyiciler dahil) | Bölüm 3.1: bu sınıfın **kendi içinde** çözünürlük eşitliği zorunlu — bir tetikleyici diğerlerinden daha keskin/detaylı dokuya sahipse, bu kamuflajı biçim kadar güvenilir biçimde çökertir |
| **Özel durum** | UI (crosshair/hold-ring) | Bölüm 3.3/7.3: düz vektör-stil, muhtemelen basit doku ya da prosedürel şekil |
| **Asansör** | Kabin içi, tam-kapalı | Bölüm 2.3: yoğunluk neredeyse sıfır olsa da kabin *her açıdan* camera-safe olmalı — küçük hacim, düşük sayı ama asla düşük özen |

*Design test*: Bir sahte `IInteractable` için "zaten önemsiz, düşük çözünürlük yeter" denirse — reddedilir; çözünürlük eşitliği, gerçek tetikleyicinin görsel olarak ayırt edilememesinin ön koşuludur (Bölüm 3.1).

### 8.4 LOD Beklentileri — Niteliksel

**Çevre propları mesafe-bazlı LOD zincirine ihtiyaç duyar; kol/el rigi duymaz** (Bölüm 5.4'ün zaten kilitlediği ayrım — kesin kademe/mesafe 8.10'da). Oyuncu çoğu geometrinin yanından yürüyerek geçtiği için standart bir çok-katmanlı yaklaşım (yakın/orta/uzak) felsefi olarak doğru. Sanat tarafının tek şartı: **geçişler görünmez olmalı** — Bölüm 1'in "yalnızca ışık yalan söyler" kuralı, LOD pop'unu da kapsar; bir mesh'in gözle görülür biçimde aniden basitleşmesi, geometrinin de "yalan söylediği" bir an yaratır ki bu kural dışıdır.

İkinci, daha ince bir kısıt: **Depo'da LOD, kamuflajı seyreltmemeli.** Bölüm 6.3'ün prop yoğunluğu kamuflajın bir parçasıydı — uzak LOD kademelerinde propları agresifçe kaldırmak, o alanın "sıradan nesne gürültüsü" işlevini de kaldırır. Servis Koridoru'nda ise tam tersi güvenli: modüller zaten birbirinin **birebir aynısı** (Bölüm 6.3), bu yüzden agresif LOD burada kamuflajı bozmaz, sadece tekrarı sadeleştirir. Asansör kabini, Bölüm 2.3'ün "her açıdan camera-safe" şartı yüzünden pratikte kol/el rigi gibi davranır — LOD zinciri gerekmez, her zaman tam detay.

*Design test*: Bir çevre sanatçısı Depo'da uzak LOD'da tüm küçük kutu/kalıntı propları silmeyi önerirse — önce sorulmalı: bu, o mesafede zaten görünmeyen bir tetikleyicinin kamuflaj katmanını mı inceltiyor? Evetse, LOD1'de bile minimum bir "gürültü" yoğunluğu korunmalı.

### 8.5 Export Ayarları Felsefesi

İki zorunluluk sanat tarafından geliyor: **(1) ikinci UV kanalı** her modüler kit parçasında export'tan önce garanti altına alınmalı, çakışmasız/lightmap-hazır (baked/Mixed aydınlatma zorunluluğu, `game-concept.md` Art Pipeline Complexity) — bu adım atlanırsa Bölüm 6.2'nin "gölge her zaman gerçek zamanlı" garantisi teknik olarak imkânsızlaşır. **(2) pivot/grid hizası**: her modül pivotu 4m×4m ızgaraya endekslenmiş export edilmeli (Bölüm 3.2) — ızgara dışı bir pivot, sahada görünmeyen ama üretimi kesintiye uğratan bir tutarsızlık yaratır. Materyal tarafında tek shader ailesine (URP Lit) sadakat export ayarlarına da yansımalı — özel shader graph varyantları, "stilize kalibrasyon" çizgisini (Bölüm 6.2) proje bazında karşılıksız aşındırabilir; yeni bir shader ihtiyacı önce bu belgeye/teknik-artist'e sorulmalı, sessizce export edilmemeli.

*Design test*: Bir modül UV2 hazır olmadan sahneye eklenirse — reddedilir; bu, Bölüm 6.2'nin donmuş-gölge yasağını export zincirinin en başında zaten ihlal eder.

---

### 8.6 Poligon Bütçeleri

| Kategori | Bütçe (tri) | Gerekçe |
|---|---|---|
| Modüler kit — standart parça (duvar/tavan/zemin, 4×4m) | 300–800 | Bölüm 3.2'nin "yapısal olarak dürüst" kutusal geometrisi zaten düşük poli — süs yok. |
| Modüler kit — trim/özellik parçası (kapı çerçevesi, kolon, kapı kanadı) | 800–1.800 | Tekrar eden ama yakından görülen parça; Bölüm 6.3'ün "Servis Koridoru = tekrar birimi" tanımını destekler. |
| Sahne dekoru prop (arka plan, tekil) | 200–800 | Bölüm 6.4 İlke 2: dağınıklık nicelik ister, tekil obje kalitesi değil. |
| Tekrarlayan küçük donanım (kapı kolu, anahtar, apliğ) | 50–150 | Aynı mesh onlarca kez kullanılır — GPU instancing/SRP Batcher ile maliyeti tekil değil, toplu. |
| Etkileşim/kamuflaj nesnesi (gerçek **veya** sahte `IInteractable`) | 300–900, **her ikisi de aynı bant** | Bölüm 3.1 kuralı poligon sayısına da uygulanır: gerçek/sahte poli farkı taşımaz, kamuflaj çökmesin. |
| Kol/el rig + taşınan eşya (tek LOD kademesi) | 6.000–10.000 (ikisi toplam) | Bölüm 5.4: mesafe LOD'u yok, ama sahnede **aynı anda tek nesne** var (havuzlanmış N=2-4, hepsi aynı anda render edilmiyor) — agregat sahne bütçesine etkisi düşük, bu yüzden bireysel kaliteyi arka plan propundan ~10× yüksek tutmak güvenli. |
| UI (crosshair + hold-ring) | Trivial / procedural quad | Tek küçük doku ya da shader-çizimi; poligon bütçesi anlamsız (bkz. 8.7). |

### 8.7 Doku Çözünürlük Tavanları (albedo/normal/mask seti)

| Kategori | Maks. çözünürlük | Not |
|---|---|---|
| Modüler kit — ana yüzeyler (tiling) | 2048×2048 | PC-only headroom kullanılıyor; 4K'ya gerek yok, kit büyük ölçüde tekrar/tiling. |
| Modüler kit — trim/detay parçası | 1024×1024 | |
| Hero/etkileşim prop (gerçek + sahte, eşit) | 1024–2048 | Bölüm 3.1 gereği ikisi aynı bütçede. |
| Arka plan/set dekoru prop | 512–1024 | |
| Tekrarlayan küçük donanım | 256–512 | Küçük ekran alanı, yüksek tekrar. |
| Kol/el rig + taşınan eşya | 2048 (rig), 1024–2048 (eşya başına) | Bölüm 5.4 gerekçesiyle aynı — tek nesne, yüksek prominans. |
| UI (crosshair+ring) | 128×128–256×256 | Achromatic, alpha-mask ağırlıklı; bu kadarı bile büyük ihtimalle fazla — küçük atlas yeterli. |
| **Lightmap texel yoğunluğu** | ~8–10 texel/m (Depo/Koridor), ~12 texel/m (Balo Salonu hazırlık) | **Ayrı bütçe hattı** — materyalin kendi doku çözünürlüğünden bağımsız. İkinci UV kanalı zorunluluğu (`game-concept.md` Art Pipeline Complexity) burada devreye girer; bake grubu başına atlas tavanı 2048×2048, sahne belleğini 4GB tavanı içinde tutmak için. |

### 8.8 Materyal Slot Rehberi

Modüler kit parçası başına **1 slot** (paylaşılan master materyal + atlas/tiling doku) — bu, SRP Batcher uyumluluğunu korur; her ek slot potansiyel bir ek çizim çağrısıdır. Arka plan propu **1 slot**. Hero/etkileşim propu ve taşınan eşya en fazla **2 slot** (gövde + metal/trim ayrımı). Kol/el rig toplamda **3 slot tavanı** (deri/kumaş, metal, eşya). Hiçbir asset kategorisi 3 slotu geçmez — geçen her öneri, `technical-preferences.md`'nin ~2000 çizim çağrısı tavanını doğrudan tüketir.

### 8.9 Importer Kısıtları (Unity 6.3 LTS/URP)

- Tüm modüler kit meshleri: **UV2 (lightmap UV) zorunlu**, çakışmasız — Baked/Mixed aydınlatmanın hard kısıtı (`game-concept.md`). Hero parçalarda elle açılmış UV2, otomatik unwrap'e tercih edilir (seam kalitesi).
- Mesh Compression: **Medium** — statik kit için yeterli, "High" lightmap UV hassasiyetini bozabilir.
- Read/Write: **kapalı** (disabled) — hiçbir modüler/prop mesh runtime'da CPU erişimi gerektirmiyor, bellek tasarrufu.
- Shader standardı: **URP Lit (Metallic workflow)** — özel çok-geçişli/Complex Lit shader'lardan kaçının, her farklı shader varyantı SRP Batcher'ı böler.
- Unity 6.3'e özgü fırsat: **GPU Resident Drawer** açık tutulmalı — static-flagged modüler kit + tekrarlayan donanım bu özellikten otomatik yararlanır, elle batching işini azaltır.
- Doku sıkıştırma: PC hedefi — albedo **BC7**, normal/mask **BC5**; mobil formatlara (ASTC/Crunch) gerek yok.

### 8.10 LOD Kademeleri — Çevre Geometrisi

Bölüm 5.4'ün aksine (kol/el rigi tek LOD), çevre geometrisi mesafeye göre değişir çünkü oyuncu koridor boyunca ilerler. Ama bu **içerde, koridor-ve-oda** bir oyun — açık dünya görüş mesafesi yok, gerçekçi olmak gerekiyor:

| Kademe | Mesafe | Detay |
|---|---|---|
| LOD0 | 0–12m | Tam kit detayı, tüm trim geometrisi. |
| LOD1 | 12–25m | ~%50 tri azaltımı, küçük donanım (kapı kolu vb.) düzleştirilmiş/mesh'e gömülü. |
| Cull / Occlusion | 25m+ | Koridor/kapı geometrisi zaten doğal occluder — 25m ötesi çoğu sahnede zaten görünmez; agresif view-distance culling yeterli, ayrı bir LOD2 gerekmez. |

*Design test*: Bir sanatçı uzun bir servis koridorunda LOD1 geçişinin "pop" ettiğini fark ederse — çözüm mesafe eşiğini büyütmek değil, geçiş bölgesini oyuncunun doğal duruş noktalarından (kapı, köşe) uzağa denk getirmektir; 25m tavanı bu oyunun iç mekân ölçeği için zaten cömert, büyütmek gereksiz bellek/draw-call maliyeti demektir.

### 8.11 Alan-Başına Prop ve Çizim Çağrısı Tavanları (Bölüm 6.3'ün sayısal karşılığı)

| Alan | Prop tavanı | Çizim çağrısı payı | Not |
|---|---|---|---|
| Depo | 40–60 / oda | ~250 | En yüksek yoğunluk (Bölüm 6.3), ama çoğu tekrarlayan kutu/raf tipi — instancing ile pay kontrol altında. |
| Balo Salonu (hazırlık) | 20–35 / oda | ~150 | Değişken/dalgalı yoğunluk. |
| Servis Koridoru | 8–12 / 10m segment | ~100 | Düşük prop sayısı ama Bölüm 6.3'ün "aynı donanım tekrarı" notu burada bütçeyi **rahatlatır** — az sayıda benzersiz mesh+materyal kombinasyonu, yüksek tekrar. |
| Asansör | 3–5 / kabin | ~10 | Near-zero, sabit kapalı hacim. |
| Kol/el rig + eşya | 1 nesne (havuzlanmış) | ~15–20 | Bölüm 5.4. |
| UI | 1 asset | ~1–3 | 8.6/8.7. |
| **Rezerv** (VFX, dinamik ışık, post-process) | — | ~%15 pay (~300) | Sahne başına beklenmeyen maliyetler için tampon. |

*Design test*: Bir level designer Servis Koridoru'na "biraz daha ilginç" görünmesi için **farklı** bir donanım parçası eklemek isterse — bu hem Bölüm 6.3'ün kamuflaj-yoğunluk notunu hem de bu tablonun düşük-ama-tekrarlayan bütçe mantığını ihlal eder: yeni/benzersiz mesh+materyal kombinasyonu hem çizim çağrısı payını hem kamuflaj bütünlüğünü aynı anda kırar — reddedilir, çözüm ışıkla/kompozisyonla aranmalı (Bölüm 3'ün zaten kurduğu ilke).

## 9. Reference Direction

### Referans Kuralı

> **Hiçbir referans taklit edilmek için burada durmaz; her biri bu belgenin zaten kilitlediği bir kuralı doğrulamak ya da ondan kasıtlı olarak ayrılmak için seçildi.**

`game-concept.md`'nin Inspiration and References tablosu üç referansı zaten sabitlemişti (Silent Hill 2, Gone Home, What Remains of Edith Finch) — bu bölüm onları atmaz, ama Bölüm 1-8'in artık bildiği somut üretim kararlarıyla (kamuflaj kısıtı, doku disiplini, ışık+ses bileşik bulgusu, mimari tipoloji) her birinin "ne alıp ne almadığını" yeniden keskinleştirir ve iki yeni referans ekler — biri prototip bulgusunun açtığı boşluk için (Bölüm 1, Prensip 2), biri Bölüm 6.1'in "yüzsüz uluslararası zincir oteli" tipolojisi için. Beş referans beş ayrı eksende çalışır; hiçbiri aynı yöne işaret etmez (bkz. kapanış notu).

### 9.1 Silent Hill 2

**Ne alıyoruz**: Korkunun kaynağı bir canavar değil, karakterin kendi bastırdığı suçluluğu — bu proje bunu doğrudan devralır (BPD ilişkisi, katil değil). Ayrıca sahne/kamera disiplini: SH2 dehşeti abartılı bir kompozisyonla değil, sıradan bir figürün sıradan bir odadaki duruşuyla taşır — Bölüm 2.1'in "sabit göz hizası, sinematik pan yok" kuralının soy kökeni burada.

**Neyden ayrılıyoruz**: SH2'nin sis/statik-radyo ikilisi bir **tehlike-yakınlığı sinyali** — düşman ne kadar yakınsa radyo o kadar bozuluyor. Bizim ışık+ses eşleşmemiz Bölüm 4.2'nin zaten yasakladığı bir şeyi asla yapmamalı: bir "tehlike var, kaç" anlamı taşımak. Ayrıca SH2'nin dokuya gömülü çürüme/pas katmanları (psişik durumu malzemeye boyayan bir teknik) Bölüm 6.2'nin kilitlediği kuralı ihlal eder — bizde yalan hep gerçek zamanlı ışıkta yaşar, dokuda donmaz.

*Design test*: Bir sanatçı "SH2'deki gibi sis ekleyelim, atmosfer versin" derse — hayır; Bölüm 3.2'nin büyük-biçim-okunabilirliği kuralını kırar.

### 9.2 Gone Home

**Ne alıyoruz**: Sınırlı, tek bir mekânın oyuncu tarafından defalarca içselleştirilmesi — geniş değil, **tanıdık** bir alan. Bu, Bölüm 6.4 İlke 1'in ("hikâye tekrardan doğar, keşiften değil") doğrudan atası.

**Neyden ayrılıyoruz**: Gone Home'un çekirdek mekaniği — günlük/ses kaydı toplayıp okuma, "bak bu önemli" diye çerçevelenmiş nesneler — Bölüm 3.1/3.4/6.4'ün kamuflaj kuralıyla doğrudan çatışır. Bizde hiçbir nesne öne çıkarılmış bir siluetle "beni oku" demez; anlam mekânın **ikinci geçişte** nasıl göründüğünden gelir, ilk geçişte bulunan bir objeden değil.

### 9.3 What Remains of Edith Finch

**Ne alıyoruz**: Her bölümün (bizde: her gece/katman) kendi tam formal diline geçebilme izni — Edith Finch'in her vinyeti farklı bir oyun-hissi taşır. Bu, Bölüm 2.4'ün psikiyatri ofisinin (kilitli/sabit kamera, keşif segmentlerinden bilinçli kopmuş enerji) neden var olduğunun meşrulaştırıcısı.

**Neyden ayrılıyoruz**: Edith Finch bunu genelde bir nesnenin (bıçak, bir görüntüleyici) doğrudan mekaniğe dönüşmesiyle yapar — nesne kendisi anlatının taşıyıcısı olur. Bu, Bölüm 3.1'in mutlak kuralını ihlal eder: hiçbir etkileşim nesnesi "şimdi hikâye buradan akıyor" diyen bir öne çıkma kazanamaz. Bizde format değişir (ışık/kamera/ses), nesne hiç öne çıkmaz.

### 9.4 Session 9 (2001, yönetmen Brad Anderson)

Bölüm 6.1'in "yüzsüz kurumsal mimari korku mekânı olarak" boşluğu için — terk edilmiş bir akıl hastanesinde asbest temizliği yapan bir ekibin, canavarsız, tamamen içsel bir çöküşü anlatan filmi.

**Ne alıyoruz**: Sahne boyunca **sadece pratik/motivasyonlu ışık kaynaklarıyla** çekim disiplini — hiçbir "korku aydınlatması" rig'i, sahnenin kendi lambası ne varsa o. Bu, Bölüm 6.2'nin ve Bölüm 2.1'in "işlevsel, dekoratif değil" kuralının sinematografik kanıtı. Ayrıca: dehşetin tamamen protagonistin kendi zihninde yaşaması, mekânın pasif bir kap olarak kalması — Bölüm 1'in "mekân asla yalan söylemez" kuralının en net dış-dünya örneği.

**Neyden ayrılıyoruz**: Film gerçekten terk edilmiş, çürümüş bir kurumu çeker — bizim otelimiz **çalışan, personelli** bir mekân (Pillar 3, Görev Gerçekliği); çürüme/ihmal estetiğini kit'in PBR temizliğine sızdırmayız (Bölüm 6.2'nin "fotogrametrik gürültü değil" kuralı). Film ayrıca gerçek karanlığı bir dehşet aracı olarak kullanır — bizde `MemoryIntensityMultiplier ≥0.6` erişilebilirlik tabanı bunu yasaklıyor. Üçüncüsü: film üçüncü perdede nesnel bir açıklamaya yaklaşır — bizim finalimiz Pillar 5 gereği hiçbir zaman objektif gerçeği doğrulamaz.

### 9.5 Inside (Playdead)

Bölüm 1 Prensip 2'nin prototip bulgusunun (ışık tek başına yetmiyor, bkz. `prototypes/yankilar-lighting-concept/REPORT.md`) açtığı boşluk için — ışık ve sesi gerçek bir bileşik dehşet motoru olarak kullanan, canavarsız bir oyun.

**Ne alıyoruz**: Ses karışımında bilinçli **sessizlik bütçesi** — mix çoğunlukla boş bırakılır ki bir sting/alçak frekans dalgası göreceli olarak büyük düşsün; bu tam olarak bizim prototipimizin bulduğu şey (ışık "güzel" okundu, "yanlış" okunmadı, ses eksikti). İkincisi: alan başına motivasyonlu, gerçek kaynaklı pratik ışık paleti — Bölüm 4.3'ün alan-başına `BaseColor` tablosunun aynı disiplini.

**Neyden ayrılıyoruz**: Inside'ın istirahat paleti baştan sona düşük doygunluklu/gri-mavi — bizim "gerçeklik" durumumuz amber-baskın ve sıcak, desatürasyon sadece **geçici** bir anı-kayması deltası (Bölüm 2.2). Bu paleti baz alırsak Bölüm 2'nin sıcak/soğuk kontrastı çöker. Ayrıca Inside'ın kovalama/yakalanma dizileri bir başarısızlık durumu içerir — Anti-Pillar ("NOT kombat sistemi") ve `game-concept.md`'nin "cezalandırıcı başarısızlık yok" notu bunu MVP'de yasaklıyor; tekniği alıyoruz, paketini almıyoruz.

---

Bu beş referans beş farklı eksende çalıştığı için üst üste binmiyor: SH2 **korkunun kökeni**ni (canavar değil, suçluluk), Gone Home **mekân yapısı**nı (sınırlı, tekrarlanan), Edith Finch **katman-formu**nu (her bölüm kendi diline sahip), Session 9 **sinematografik ışık disiplini**ni (sadece pratik kaynak, tamamen içsel tehdit) ve Inside **ışık+ses'in bileşik mühendisliğini** (sessizlik bütçesi, motivasyonlu palet) temsil ediyor. Bir sanatçı bu beşini asla tek bir "genel atmosfer" kaynağı gibi karıştırmamalı — her biri, belgenin daha önceki bir bölümünün *neden* o kararı verdiğinin dışarıdan bir kanıtı; hiçbiri kendi başına bir stil rehberi değil.
