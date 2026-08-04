# Game Concept: Yankılar (Echoes)

*Created: 2026-08-01*
*Status: Draft*

> **Creative Director Review (CD-PILLARS)**: CONCERNS (revised and accepted) 2026-08-01
> **Art Director Review (AD-CONCEPT-VISUAL)**: CONCEPTS (Direction 1 + Direction 3 blend selected) 2026-08-01
> **Technical Director Review (TD-FEASIBILITY)**: CONCERNS (accepted, URP + baked lighting guidance adopted) 2026-08-01
> **Producer Review (PR-SCOPE)**: OPTIMISTIC (accepted, MVP is committed target, Vertical Slice is stretch) 2026-08-01

---

## Elevator Pitch

> Büyük, sessiz bir lüks otelde gece vardiyasında düğün organizasyonu için malzeme
> taşırken, ana karakterin bastırdığı ağır bir psikolojik travmanın izleri otelin
> katmanlarında yavaş yavaş ortaya çıkıyor — bu bir canavar hikayesi değil, bir
> ilişkinin bıraktığı hipervijilansın hikayesi.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Psikolojik Korku / Dram, Keşif-Anlatı (Environmental Storytelling) |
| **Platform** | PC (Steam / Epic) |
| **Target Audience** | Storyteller/Explorer oyuncular; psikolojik anlatı ve "sanat oyunu" severler |
| **Player Count** | Single-player |
| **Session Length** | 30-60 dk |
| **Monetization** | Premium (tek seferlik satın alma) |
| **Estimated Scope** | Small–Medium (3–4 ay, ikili takım — part-time) |
| **Comparable Titles** | Gone Home, What Remains of Edith Finch, Silent Hill 2 |

---

## Core Fantasy

Gerçek, somut bir işi (malzeme taşıma) tamamlamaya çalışırken (design-review,
2026-08-04 — verification bulgusuyla "zaman baskısı altında" ifadesi
kaldırıldı — hiçbir MVP sisteminde gerçek bir saat/bedel yok, bkz. Core
Loop notu), kendi bastırılmış anılarınızın parçalarını yeniden bir araya
getirmek. Otelin hiçbir köşesinde tam güvende hissetmiyorsunuz — çünkü
mesele otelde değil, kendi zihninizde.

---

## Unique Hook

Gone Home ve What Remains of Edith Finch gibi çevresel anlatıya dayalı, AMA her kat
inen bir anı katmanı. Silent Hill 2'nin soy ağacında — korku bir katilden değil,
bastırılmış bir travmadan doğuyor.

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** (sensory pleasure) | 4 | Işık/renk sıcaklığı geçişleri, mekansal ses tasarımı |
| **Fantasy** (make-believe, role-playing) | 3 | Protagonistin bozulmuş algısına girmiş olma hissi |
| **Narrative** (drama, story arc) | 1 | Anı parçalarından örülen, plot twist'e giden katmanlı anlatı |
| **Challenge** (obstacle course, mastery) | N/A | Kombat/başarısızlık odaklı zorluk yok |
| **Fellowship** (social connection) | 5 | Arkadaş karakteriyle kurulan güvenilir insan bağı |
| **Discovery** (exploration, secrets) | 2 | Anı-tetikleyici nesneleri bulma, otelin "dilini" okuma |
| **Expression** (self-expression, creativity) | N/A | — |
| **Submission** (relaxation, comfort zone) | N/A | — |

### Key Dynamics (Emergent player behaviors)

Oyuncular şüpheli nesnelere yaklaşırken doğal olarak tempo yavaşlatacak; odaklı
(hızlı görev tamamlama) ile dikkat-dağıtıcı (yan odalara sapma) arasında sürekli
seçim yapacak (design-review, 2026-08-04 — verification bulgusuyla "güvenli/riskli"
dilinden düzeltildi, bkz. Core Loop notu — hiçbir mekanik bedel yok, seçim
tempo/dikkat üzerine); psikiyatri sahnelerini az önce gördükleriyle aktif
olarak eşleştirmeye çalışacak.

### Core Mechanics (Systems we build)

1. **Malzeme taşıma görev döngüsü** — depo (-5) ↔ balo salonu (-2), asansör
   kısıtlı (design-review, 2026-08-04 — "zaman baskılı" kaldırıldı, bkz.
   Core Loop notu)
2. **Anı-tetikleyici nesne etkileşimi** — ışık/renk sıcaklığı tabanlı öznel
   gerçeklik kayması (amber "gerçeklik" → soğuk/yeşilimsi "anı")
3. **Sahne kesmeli anlatı yapısı** — tansiyon doruğa ulaştığında psikiyatri
   seansı cutscene'ine kesme
4. **Adaptif ses sistemi** — ambiyans katmanları + anı-tetikleyici geçişiyle
   eşleştirilmiş sting/ton kayması (prototip bulgusuna göre birinci sınıf
   sistem, sonradan eklenen bir katman değil)

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** | Rota/tempo seçimi, tetikleyici odalara sapıp sapmama kararı | Supporting |
| **Competence** | Otelin "dilini" okumayı öğrenme — gerçek ipucu ile yanıltıcıyı ayırt etme | Supporting |
| **Relatedness** | Arkadaş karakteriyle kurulan somut bağ; eski ilişkiyle kurulan anı-bağı | Core |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Explorers** (discovery, understanding systems, finding secrets) — How: Anı parçalarını bulma, otelin katmanlarını okuma
- [ ] **Achievers** — N/A, ilerleme/koleksiyon sistemi yok
- [x] **Socializers** (relationships, cooperation, community) — How: Arkadaşla kurulan bağ, ilişki dinamiklerinin merkezi teması
- [ ] **Killers/Competitors** — N/A

### Flow State Design

- **Onboarding curve**: İlk 10 dakika basit bir taşıma turu ile mekanikleri
  öğretir, minimal UI
- **Difficulty scaling**: Klasik zorluk eğrisi yok — gerilim, sahne/tetikleyici
  yoğunluğuyla kontrol edilir
- **Feedback clarity**: Işık/renk sıcaklığı değişimi ve ses, oyuncuya "bir şey
  değişti" sinyalini verir
- **Recovery from failure**: Cezalandırıcı başarısızlık yok — kaçış/ölüm
  mekaniği yok, oyuncu asla "kaybetmiyor"

---

## Core Loop

### Moment-to-Moment (30 saniye)

Depo-balo salonu arası malzeme taşıma. Her turda küçük bir seçim: görevi
doğrudan bitir (odaklı, az keşif) ya da yan koridora sap (dikkat dağıtıcı,
anı parçası bulma ihtimali). **Not (design-review, 2026-08-04 — verification
design-theory bulgusu, düzeltildi)**: bu satır önceden "güvenli"/"riskli"
diyordu — bu, gerçek bir bedel/ceza ima ediyordu ama hiçbir MVP sisteminde
saat, kaynak tükenmesi ya da başarısızlık durumu yok (bkz. Risks and Open
Questions, "Cezalandırıcı başarısızlık yok" notu — bu kasıtlı bir tasarım
kararı, boşluk değil). Sapma hiçbir mekanik bedel taşımaz — seçim risk
üzerine değil, **nereye dikkat vereceğin** üzerine kurulu. Meditatif ama
huzursuz bir tempo.

### Short-Term (5-15 dakika)

Bir taşıma turu = bir mikro-döngü. "Bir sonraki anı parçası ne olabilir?"
merakı oyuncuyu ileri çeker. Kısa, öz turlar; doğal durma noktaları var.

### Session-Level (30-120 dakika)

Bir "gece" = bir oturum. Görev listesiyle başlar, 3-5 taşıma turu, turlar
ilerledikçe ortam gerilimi birikir (design-review, 2026-08-04 — verification
design-theory bulgusuyla somutlaştırıldı: bu iddia önceden hiçbir sistemde
karşılığı yoktu — artık Adaptif Ses Sistemi'nin round-bazlı gerilim
birikimi mekanizması bunu taşıyor, bkz. `adaptif-ses-sistemi.md` Core
Rules), gece bir psikiyatri seansı sahnesiyle kapanır — bu sahne az önce
olanları yeniden çerçeveler ve bir ipucu bırakır.

### Long-Term Progression

Her gece bir "bölüm". Anı parçaları birikip ilişkinin resmini yavaşça ortaya
çıkarır; başta "sabit" olan alanlar ilerleyen gecelerde ince değişimler
göstermeye başlar. Son gece = plot twist finali.

### Retention Hooks

- **Curiosity**: Bir sonraki anı parçasının ne olduğunu öğrenme merakı
- **Investment**: Şimdiye kadar toplanan anı parçalarının anlamını tamamlama isteği
- **Social**: Arkadaş karakteriyle kurulan bağın nasıl gelişeceğini görme isteği
- **Mastery**: Otelin "dilini" (gerçek ipucu vs. yanıltıcı) daha iyi okuma

---

## Game Pillars

### Pillar 1: Öznel Gerçeklik (Subjective Reality)

Oyuncu dünyayı her zaman ana karakterin bozulmuş algısından deneyimler;
hiçbir zaman "objektif" gerçeklik sunulmaz.

*Design test*: "Bu gerçekten mi oluyor, yoksa karakterin zihninde mi?" sorusuna
net cevap vermeli miyiz diye tartışırsak — bu pillar "hayır, belirsizliği koru" der.

### Pillar 2: Sessiz Gerilim, Şok Değil (Quiet Dread, Not Shock)

Korku, ani jump-scare'lerden değil, birikimli huzursuzluktan gelir.

*Design test*: Bir korku anını ani şokla mı yoksa yavaş inşa edilen belirsizlikle
mi çözeceğimize karar verirken — ikincisini seçer.

### Pillar 3: Görev Gerçekliği (Grounded Labor)

Oyunun iskeleti gerçek, somut bir iş (malzeme taşıma) olmalı; fantastik
soyutlamalar bu gerçekliği kırmaz.

*Design test*: Bir mekanik "havalı" ama gerçekçi değilse — reddedilir.

### Pillar 4: Bağ, Güvenlik Değil (Connection, Not Safety)

Arkadaşla olan ilişki duygusal bir çıpa sağlar ama oyuncuyu tam güvende
hissettirmez.

*Design test*: Bir mekanik, arkadaşın oyuncuyu bir anı-tetikleyici karşılaşmadan
tamamen koruyup kurtarmasına izin veriyorsa — bu pillar'ı ihlal eder.

### Pillar 5: Anlam Sona Saklı (Meaning Deferred)

Oyuncu tam resmi ancak sona doğru anlar; tek tek anlar parçalı/tuhaf
hissettirebilir, bu kasıtlı.

*Design test*: Bir ipucu çok erken çok net olur mu diye tartışırsak — "hayır,
sakla" der. Final twist, anlamı yeniden çerçevelemeli ama objektif gerçeği
doğrulamamalı — aksi halde Pillar 1'in belirsizliği son anda çöker.

### Anti-Pillars (What This Game Is NOT)

- **NOT ucuz jump-scare'ler**: Bu, "Sessiz Gerilim" pillarını zedeler.
- **NOT BPD'yi ya da BPD'li karakteri bir tanı etiketine/canavara indirgeme**:
  İlişkideki gerçek davranışlar (öngörülemezlik, terk edilme korkusu tepkileri
  vb.) ve bunların protagonist üzerindeki etkisi hikayenin gerçek malzemesi —
  ama karakter asla insanlık dışı bir arketipe indirgenmez. Sınır davranışı
  göstermek değil, karakteri canavarlaştırmaktır.
- **NOT kombat sistemi**: Bu bir hayatta kalma/dövüş oyunu değil, "Görev
  Gerçekliği" pillarını zedeler.
- **NOT büyük açık-dünya/free-roam otel haritası**: Kapsam riski taşır ve
  "Anlam Sona Saklı" için sıkı kontrollü mekan tasarımı gerekli.

---

## Visual Identity Anchor

**Seçilen yön**: "Otel Senin Yerine Hatırlıyor" (ana yön) + "İki Oda, Tek Işık"
(psikiyatri kesitlerinde tamamlayıcı motif)

**Görsel kural**: Hiçbir şey yeniden dekore edilmez — ışık ve göz yalan söyler,
ama korkuyu taşıyan asıl şey sesle birleştiğinde ortaya çıkar.

> **Prototip bulgusu (2026-08-01)**: Işık/renk geçişi tek başına test edildi
> (bkz. `prototypes/yankilar-lighting-concept/REPORT.md`) — teknik olarak temiz
> çalıştı ama tek başına rahatsız edicilik yaratmadı. Pillar 2 (Sessiz Gerilim,
> Şok Değil) artık **ışık + ses bileşik bir etki** olarak tanımlanıyor, sadece
> ışık değil. Bu, aşağıdaki sistem GDD'lerinde ses tasarımının birinci sınıf
> bir sistem olarak ele alınmasını gerektirir.

**Destekleyici prensipler**:
- **Gerçekçi geometri, öznel ışık**: Otel geometrisi (koridorlar, balo salonu)
  gerçekçi ve sabit kalır; distorsiyon tamamen ışık/renk sıcaklığında yaşar.
  *Design test*: Bir anı-tetikleyici anı için yeni bir mesh/geometri mi yoksa
  ışık/renk değişimi mi gerekiyor diye tartışırsak — bu prensip ikincisini seçer.
- **İki sabit palet, bir sızıntı**: Otel için sıcak amber "gerçeklik" paleti,
  psikiyatri ofisi için soğuk teal-gri palet — biri diğerine sızdıkça oyuncu
  "hangi katmanda" olduğunu hisseder.

**Renk felsefesi**: Sıcak amber servis ışığı temel gerçeklik; anı istilaları
paleti sodyum-buhar yeşilimsi-beyaz ya da tek bir desatüre maviye kaydırır —
renk sıcaklığının kendisi gerçekliğin kaydığının işareti olur.

**Palet ayrımı (art-director notu, 2026-08-01)**: Anı-soğukluğu ve psikiyatri
ofisi paleti ikisi de "soğuk" olduğu için, kasıtlı olarak ayrıştırılmaları
gerekiyor — aksi halde oyuncu hangi katmanda olduğunu ayırt edemez:
- **Anı-soğukluğu**: Mavi ağırlıklı desatürasyon (White Balance Temperature
  ~-60), yumuşak/dağınık gölgeler — kaynağı belirsiz, çevresel bir kayma.
- **Psikiyatri ofisi**: Teal-gri, sert/tek kaynaklı gölge (tek pratik lamba +
  jaluzi çizgileri) — kaynağı net, sabit bir mekan imzası.
Ortak payda "soğuk" olsa da, gölge sertliği ve ışık kaynağı netliği ikisini
görsel olarak ayırt edilebilir kılar.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| Silent Hill 2 | Korkunun içsel suçluluk/travmadan doğması | Bizim travmamız bir BPD ilişkisinden, bir katilden değil | Bu soy ağacında özgün bir yer kanıtlıyor |
| Gone Home | Environmental storytelling, düşük aksiyon riski | Anı katmanları kat-kat inen bir yapı oluşturuyor | Küçük bir ekip için üretilebilir bir format |
| What Remains of Edith Finch | Her alan/an farklı bir hikaye anlatıyor | Bizde her "gece" bir anlatı katmanı | Anlatı parçalanmışlığı meşru, denenmiş bir format |

**Non-game inspirations**: Gerçek yaşanmış bir ilişki (BPD tanılı bir partnerle
2 yıllık ilişki ve bunun bıraktığı hipervijilans); gerçek bir otel (Antalya
DoubleTree by Hilton) ve orada yapılan gece vardiyası düğün organizasyon işi
(asansörün sadece geceleri personelsizken müsait olması).

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18-35 |
| **Gaming experience** | Mid-core (anlatı odaklı oyunlara aşina) |
| **Time availability** | 30-60 dk oturumlar |
| **Platform preference** | PC |
| **Current games they play** | Gone Home, What Remains of Edith Finch, Silent Hill 2, Hellblade: Senua's Sacrifice |
| **What they're looking for** | Duygusal derinlik, özgün/kişisel hikayeler, atmosferik gerilim |
| **What would turn them away** | Kombat/aksiyon beklentisi, yoğun jump-scare beklentisi |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity (ekip tercihi) — URP kullanılacak, HDRP'den kaçınılacak |
| **Key Technical Challenges** | Işık/renk sıcaklığı tabanlı öznel gerçeklik geçişleri (baked lighting + URP Volume blending); kombatsız gerilimi sürdüren ses tasarımı |
| **Art Style** | 3D stilize-gerçekçi, modüler otel kiti (4m × 4m modül grid — koridor/oda parçaları bu ızgaraya hizalı) |
| **Art Pipeline Complexity** | Medium (custom 3D ama modüler/asset-hafif) — modüller baked lighting için static-flagged olmalı, ikinci UV kanalı (lightmap UV) gerektirir |
| **Audio Needs** | Adaptive — ambiyans katmanları, mekansal ses, sting zamanlaması kritik |
| **Networking** | None |
| **Content Volume** | MVP: 3 alan, 2-3 tetikleyici, ~15-20 dk. Full Vision: 4-5 alan, 15-20 tetikleyici, ~2-4 saat |
| **Procedural Systems** | Yok — el yapımı, sıkı kontrollü mekan tasarımı |

---

## Risks and Open Questions

### Design Risks
- Kombat/kovalama olmadan 2-4 saat boyunca gerilimi sürdürmek zor olabilir
- Final twist'in "objektif gerçeği doğrulamadan anlamı yeniden çerçevelemesi" yazım olarak zorlayıcı

### Technical Risks
- Işık/renk tabanlı öznellik tekniği Unity'de henüz kanıtlanmamış — MVP öncesi
  kısa bir teknik prototip (spike) önerilir
- Baked lighting + Volume blending'in tetikleyicilerle jank olmadan çalışması
- **Donmuş-GI/Mixed-mode koridor riski** (design-review round 2 on
  `isik-volume-durum-sistemi.md`, 2026-08-01): Anı-tetikleyici ışıkları
  Mixed modda olmalı (Baked ışıklar çalışma zamanında renk/yoğunluk
  değişimine hiç tepki vermez), ama Mixed ışıkların baked indirect/GI
  katkısı da çalışma zamanında güncellenmez — sadece direkt ışık konisi
  değişir. Bounce-baskın, kapalı bir koridorda (bu projenin static-flagged
  modüler kitinde beklenen norm, bkz. Technical Considerations yukarıda)
  bu, donmuş-sıcak bir ortam ışığı yanında sadece soğuyan bir direkt koni
  anlamına gelebilir — "oda kaydı" hissi yerine yarım/yıkanmış bir geçiş
  riski taşır. **Level-authoring önerisi**: anı-tetikleyici odalarda
  practical ışıkların doğrudan/direkt katkısı toplam aydınlatmanın baskın
  kısmı olacak şekilde sahnelenmeli (az sayıda güçlü direkt kaynak,
  minimum çoklu-sekme bounce'a güvenme) — bu bir formül düzeltmesiyle
  çözülemez, sanat yönetimi/level design disiplini gerektirir. Sahip:
  art-director, `/map-systems` sonrası level tasarımı aşaması.

### Market Risks
- Niş bir tür (psikolojik dram/anlatı korku) — büyük ticari başarı beklenmemeli
- BPD/ilişki travması temasının bazı oyuncular için çok "ağır" hissettirmesi riski

### Scope Risks
- Full Vision kapsamı, part-time iki kişilik ekip için birkaç aylık süreyi aşabilir (bkz. PR-SCOPE: OPTIMISTIC)
- Ses tasarımı ve seviye cilası (polish) süresi hafife alınabilir

### Open Questions
- ~~Işık/renk geçiş tekniği gerçekten inandırıcı hissettirecek mi?~~ **Cevaplandı
  (2026-08-01)**: Kısmen — teknik olarak temiz çalışıyor ama tek başına
  yetersiz; ses gerekiyor. Bkz. `prototypes/yankilar-lighting-concept/REPORT.md`.
- ~~Ses mimarisi henüz seçilmedi~~ **Cevaplandı (2026-08-02)**: Unity
  dahili AudioMixer + AudioSource — `design/gdd/adaptif-ses-sistemi.md`'de
  karara bağlandı, FMOD/Wwise'a gerek yok (3 bağımsız uzman görüşü aynı
  sonuca vardı).
- ~~Işık-durumu yazma modeli ölçeklenebilir mi?~~ **Cevaplandı
  (2026-08-01)**: Sadece post-process (Volume + ışık renk lerp'i), baked
  lightmap seti yok — `design/gdd/isik-volume-durum-sistemi.md`'de
  karara bağlandı. Bölge başına bağımsız Volume, tek paylaşılan Volume
  Profile asset'i.
- 2-4 saatlik gerilim aksiyon olmadan sürdürülebilir mi? → MVP playtest ile
  cevaplanacak

### Dış Test Kullanıcısı (CD-PLAYTEST koşulu)
- Ses eklendikten sonraki takip spike'ı için: **düğün organizasyonu iş
  ortağı** kaydedildi. Not: bu kişi konseptin gerçek kaynağını ve tasarım
  niyetini zaten biliyor — tamamen "naif" bir ilk-izlenim testi değil.
  Daha geniş sonuçlar için habersiz bir üçüncü test kullanıcısı da
  gerekebilir.

---

## MVP Definition

**Core hypothesis**: Oyuncular, kombat olmadan sadece ışık/ses/çevresel anlatıyla
üretilen gerilimi anlamlı ve ilgi çekici buluyor; öznel gerçeklik tekniği
anlaşılır ve etkileyici hissettiriyor.

**Required for MVP**:
1. Depo-balo salonu taşıma döngüsü (3 alan: depo, bir servis koridoru, balo salonu)
2. 2-3 anı-tetikleyici nesne (ışık/renk geçiş tekniği + eşleştirilmiş ses katmanı — bkz. prototip bulgusu)
3. 1 psikiyatri seansı kesme sahnesi
4. Temel adaptif ses sistemi (ambiyans + tetiklemede sting) — MVP-tier sistem, Polish'e ertelenmiyor
5. **En az 1 pasif/çevresel `Automatic` ışık-kayması bölgesi, zorunlu taşıma rotası üzerinde** (design-review,
   2026-08-04 — full re-verification bulgusu, eklendi, kritik bulgu, kullanıcı kararıyla çözüldü):
   Madde 2'deki tüm anı-tetikleyiciler `TriggerMode=ManualOnly` olmak
   zorunda (bkz. `isik-volume-durum-sistemi.md` Core Rules, rıza öncülünü
   korumak için) — bu, MVP'nin **tek** öznel-gerçeklik kayma kaynağının
   oyuncunun kendi seçtiği bir eylem olduğu anlamına gelir. Bir oyuncu
   hiçbir anı-tetikleyiciyi hiç bulmadan/tutmadan (kamuflaj + hiçbir
   ipucu/UI/highlight olmadığı için gerçek bir olasılık, bkz.
   `birinci-sahis-kontrolcu.md` Core Rules ve `etkilesim-sistemi.md`
   Core Rules) sadece kutuları taşıyarak geceyi bitirebilir — bu durumda
   MVP'nin çekirdek hipotezinin (Pillar 1: Öznel Gerçeklik) test edildiği
   **hiçbir an yaşanmamış** olur, ve bir playtest "teknik işe yaramadı"
   ile "teknik hiç tetiklenmedi" sonuçlarını ayırt edemez. **Düzeltme**:
   en az bir `TriggerMode=Automatic` (madde 2'deki anı-tetikleyicilerden
   ayrı, hiçbir `MemoryTriggerDef`'e bağlı olmayan, hiçbir ipucu taşımayan
   pasif/çevresel) ışık-kayması bölgesi, zorunlu taşıma rotası üzerine
   (oyuncunun mutlaka geçeceği bir noktaya) yerleştirilir — bu mod
   `isik-volume-durum-sistemi.md`'de zaten tam olarak bu amaç için
   tanımlıydı ("pasif/çevresel bölgeler için, ör. anı-tetikleyici olmayan
   ortam kaymaları"), sadece hiç MVP içeriği ataması yoktu. Bu bölge
   oyuncunun rızasına bağlı değildir (Anı-Tetikleyici Etkileşim'in "bile
   bile yaptım" ilkesi sadece oyuncu-tetiklediği kaymalar için geçerlidir,
   bu ayrım korunur) — sadece MVP'nin kendi test edebilirliğini garanti
   eder. Somut mekanizma ve içerik gereksinimi
   `isik-volume-durum-sistemi.md`'ye eklendi (bkz. o dosyanın Core Rules
   ve yeni Acceptance Criteria).

**Explicitly NOT in MVP** (defer to later):
- Hibrit tepkisellik (sabit alanların ince değişimi) — Vertical Slice'a ertelendi
- Plot twist finali — Full Vision'a ertelendi
- 2. gece / çoklu gece yapısı

> **Not (creative-director, 2026-08-01)**: MVP, Pillar 1-3'ü (Öznel Gerçeklik,
> Sessiz Gerilim, Görev Gerçekliği) test ediyor. Pillar 4 (Bağ, Güvenlik Değil)
> ve Pillar 5 (Anlam Sona Saklı) MVP'de yüzeye çıkmıyor — bu kasıtlı, ama
> `/map-systems` sırasında arkadaş karakteri ve psikiyatri-sahne yeniden
> çerçeveleme sistemlerinin kapsam dışı bırakılmaması için not düşülüyor.

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 3 alan (depo, koridor, balo salonu) | Çekirdek döngü + 2-3 tetikleyici + 1 psikiyatri sahnesi | ~4-6 hafta (teknik spike dahil) |
| **Vertical Slice** | 4 alan, 2. gece eklenir | Hibrit tepkisellik ilk kez devreye girer, 6-10 tetikleyici, 2 psikiyatri sahnesi | +4-6 hafta (stretch goal) |
| **Alpha** | Tüm alanlar, kaba/yer tutucu | Tüm mekanikler var, cilasız | Full Vision öncesi ara adım |
| **Full Vision** | 4-5 gece, 4-5 alan | 15-20 tetikleyici, tam anlatı yayı + plot twist finali, cilalanmış | Post-timeline stretch (mevcut takvimi muhtemelen aşar) |

> **Not (producer, 2026-08-01)**: MVP'nin ~4-6 haftalık tahmini, ayrı bir
> cilalama (polish) tamponu içerir — "özellik tamamlandı" ile "MVP bitti"
> aynı şey değil. Vertical Slice'a kaymadan önce bu tampon açıkça korunmalı.

> **GDD yazım sırası (producer, 2026-08-01)**: Görev/taşıma döngüsü GDD'si
> önce yazılmalı; anı-tetikleyici GDD'si **en son** — ses mimarisi kararı
> (bkz. Open Questions) ve ses-eşleştirmeli takip spike'ı bu GDD'nin
> Formulas/Tuning Knobs bölümlerinden önce tamamlanmalı. Ses spike'ı
> `/map-systems` ile paralel yürütülebilir.

---

## Next Steps

- [x] Get concept approval from creative-director, art-director, technical-director, producer
- [x] Fill in CLAUDE.md technology stack based on engine choice (`/setup-engine`) — Unity 6.3 LTS + URP
- [x] **Prototype core idea** (`/prototype`) — ışık/renk tekniği test edildi, PROCEED verdict, bkz. `prototypes/yankilar-lighting-concept/REPORT.md`
- [ ] Ses eşleştirmeli takip spike'ı (`/prototype --spike`) — memory-trigger GDD'sinden önce
- [ ] Decompose concept into systems (`/map-systems`) — görev döngüsü önce, memory-trigger en son
- [ ] Design each system (`/design-system [system-name]`) — use prototype learnings in Tuning Knobs and Formulas sections
- [ ] Build vertical slice in Pre-Production (`/vertical-slice`) — validate full game loop before committing to Production
- [ ] Validate core loop with playtest (`/playtest-report`)
- [ ] Plan first milestone (`/sprint-plan new`)
