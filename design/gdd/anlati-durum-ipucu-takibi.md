# Anlatı Durum/İpucu Takibi (Narrative State/Clue Tracking)

> **Status**: Approved
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **`/review-all-gdds` (2026-08-03)**: Stale Sahne Kesmeli Anlatı çapraz-
> referansları düzeltildi (o sistem artık `OnClueKnown`'a abone değil) —
> tasarım değişikliği değil, bkz. `design/gdd/gdd-cross-review-2026-08-03-verification.md`
> **Implements Pillar**: Pillar 5 (Anlam Sona Saklı)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (kabul edildi, notlar eklendi) 2026-08-02 — tempo riski Diyalog GDD'sine gereksinim olarak taşındı, eksik/sıfır-ipucu final beklenen durum olarak işaretlendi
> **Design Review (2026-08-02)**: İki tur — (1) MAJOR REVISION NEEDED →
> aynı oturumda revize edildi (Player Fantasy'nin MVP-kapsam
> uyumsuzluğu, vacuous-truth boş-liste hatası, adsız kalıcılık
> mekanizması, kendi abonelik zamanlaması, AC#8/AC#12 somutlaştırma);
> (2) re-review NEEDS REVISION (dar kapsam) → kalıcılık-asimetrisi
> sorusunun sahibi düzeltildi, Pillar 5 etiketi çözüldü, AC#12b
> sadeleştirildi; (3) kalan 2 madde (AC-algı boşluğu, abonelik-
> zamanlaması detayı) başka dokümanların sorumluluğu olarak
> değerlendirildi → **APPROVED**, üçüncü bir tam re-review yapılmadı —
> bkz. `design/gdd/reviews/anlati-durum-ipucu-takibi-review-log.md`

## Overview

Anlatı Durum/İpucu Takibi, oyuncunun hikaye içinde "bildiği" şeylerin —
hangi anı parçalarının, ipuçlarının, hangi psikiyatri seansı diyaloğunun
narratif olarak açığa çıktığının — kaydını tutan bir bayrak/durum
sistemidir. Gece/Oturum Durumu'nun tuttuğu "hangi tetikleyici ateşlendi"
ham bookkeeping'inden farklıdır: bu sistem, bir tetiklemenin **anlamsal
sonucunu** (oyuncu artık şunu biliyor) tutar. Diyalog/Anlatı İçeriği,
Çoklu Gece İlerlemesi ve Plot Twist/Final Sekansı, hangi içeriğin
gösterileceğine karar vermek için bu duruma sorgu atar. **(Sahne
Kesmeli Anlatı artık bu listede değil — design-review, 2026-08-03:
o sistem kendi gece-sonu doygunluk sinyalini artık bu sistemden değil,
Gece/Oturum Durumu'nun `SettledTriggerIds.Count`'undan okuyor (design-review,
2026-08-04 üçüncü tur bulgusuyla `FiredTriggerIds.Count`'tan düzeltildi —
bu satır saturation-timing düzeltmesinin taşındığı ikinci ismi hiç
yakalamamıştı), bkz. Dependencies.)**

Oyuncu bu sistemle hiçbir zaman doğrudan etkileşmez — sadece sonuçlarını
(bir psikiyatri seansında beklenmedik bir referans, finaldeki bir callback)
deneyimler. Bu sistem olmadan oyun, her sahneyi birbirinden habersiz,
bağlamsız parçalar olarak sunardı — Pillar 5'in (Anlam Sona Saklı)
kademeli inşası bu sistemin üzerine kurulu.

**Sahiplik notu (design-review, 2026-08-02)**: Bu sistem sadece
"bilinen/bilinmeyen" gerçeğini üretir (`IsClueKnown`) — "otelin
hatırladığı" hissi *bunu tüketen* Diyalog/Anlatı İçeriği'nin gerçek
callback metnini seçme/sunma mantığındadır. Bu doküman o hissi garanti
etmez, sadece onun için gerekli önkoşulu sağlar; Player Fantasy'de
tarif edilen deneyimin fiilen oyuncuya ulaşıp ulaşmadığı bu GDD'nin
Acceptance Criteria'sıyla değil, Diyalog GDD'sinin kendi
doğrulamasıyla ölçülmelidir.

## Player Fantasy

Bu sistem oyuncuya asla görünmez; oyuncunun *bunu fark etmemesi*, tam da
işinin doğru yapıldığının kanıtıdır.

Oyuncu, gecenin ilerleyen bir noktasında (o gecenin kendi psikiyatri
seansında), psikiyatristin gecenin başında rastladığı sıradan bir detayı
("O ışığın rengini fark ettin mi, asansöre binmeden önce?") gündeme
getirdiğinde ürperir — çünkü kendisi bile o anı önemsiz sanıp geçmişti,
ama oyun onu tutmuştu (**"Otel Unutmuyor"**). İlk karşılaşıldığında
tuhaf/anlamsız hissettiren bir detay, aynı gecenin sonunda aniden
yerine oturur — oyuncu "az önce olanı" değil, "birkaç saat önce gördüğü
ve unuttuğu bir şeyi" hatırlıyor gibi hisseder (**Geç Gelen Tanınma**).
Final, büyük bir "aha" değil, alçak sesle
söylenmiş bir "biliyordun zaten" hissiyle toplanan parçaları geri verir —
oyuncu kutlanmaz, sadece görülür (**Sessiz Suç Ortaklığı**, Pillar 4 ile
uyumlu: sistem ödüllendirmez, tanıklık eder).

> **MVP kapsam notu (design-review, 2026-08-02 — netleştirildi, ikinci
> revizyonda Pillar 5 etiketi düzeltildi)**: Yukarıdaki örnek kasıtlı
> olarak **tek gece içinde** gerçekleşir (erken bir tetikleyici → aynı
> gecenin psikiyatri seansındaki callback) — game-concept.md'nin MVP'yi
> tek geceyle sınırladığı ve Pillar 5'in "MVP'de yüzeye çıkmadığı"
> (creative-director, 2026-08-01) notuyla tutarlı. Bu yüzden MVP
> örneğinin kendisi artık Pillar 5 etiketi taşımıyor — "Geç Gelen
> Tanınma" burada sadece bir *mekanizma* (aynı gece içi callback), henüz
> Pillar 5'in tam iddia ettiği deneyim değil. **Full Vision hedefi**:
> Çoklu Gece İlerlemesi (Vertical Slice+) devreye girdiğinde, bu
> sistemin veri modeli (disk serileştirmeye zaten uygun, bkz.
> Dependencies) aynı mekanizmayı **geceler arası** genişletir —
> "üçüncü gecede geçen ay bahsedilen bir detay" versiyonu budur, ve
> **Pillar 5 (Anlam Sona Saklı) etiketini ancak bu noktada hak eder**.
> MVP bu vizyonun bir alt-kümesini (aynı gece içi) teslim eder,
> tamamını değil.

Üçünün ortak paydası: başarı, oyuncunun "bir callback sistemi var" diye
düşünmemesinde, sadece "bu oyun beni hatırlıyor" diye hissetmesinde
yatıyor.

## Detailed Design

### Core Rules

- **Veri modeli**: `HashSet<string> KnownClueIds` — sadece "bilinen ipuçları"
  kümesi, sıra ya da zaman bilgisi tutmaz.
- **shiftId → clueId eşlemesi, veri-tabanlı, N:1**: Bir `clueId`, bir ya da
  daha fazla `shiftId`'yi "gerekli" olarak listeleyen bir
  `ClueDefinition { clueId, requiredShiftIds[] }` kaydıyla tanımlanır (basit
  bir serialized liste, merkezi bir proje-seviyesi ScriptableObject asset'i
  — bkz. Unity implementasyon notu aşağıda). Semantik **ALL**'dur — bir
  `clueId`, `requiredShiftIds` listesindeki her `shiftId` en az bir kez
  Held'e ulaşana kadar bilinmez. **`requiredShiftIds` boş olamaz
  (design-review, 2026-08-02 — düzeltildi)**: `SeenShiftIds ⊇ ∅` matematiksel
  olarak her zaman doğru olduğundan, boş bir liste sistemin kendisi hiç
  başlatılmadan/hiçbir tetikleyici ateşlenmeden "Known" sayılırdı (sessiz
  bir vacuous-truth hatası). Edit-time validasyon, `requiredShiftIds.Count
  == 0` olan herhangi bir `ClueDefinition` için build'i engeller (bkz. Edge
  Cases, Acceptance Criteria).
  **MVP kararı**: game-concept.md'nin 2-3 tetikleyicisiyle her
  `ClueDefinition.requiredShiftIds` tam olarak 1 eleman taşır — yani
  davranışsal olarak 1:1, ama kod bunu varsaymaz. **Full Vision notu**:
  15-20 tetikleyicili senaryoda (ör. bir ipucunun "oturması" için 2 farklı
  anı parçası gerekmesi) sadece `requiredShiftIds` listesine ikinci bir
  `shiftId` eklenir — çalışma zamanı mantığı ya da sözleşme değişmez. Bu
  yüzden zengin model **şimdi** kuruluyor (maliyeti neredeyse sıfır — sadece
  liste vs. tekil string), MVP karmaşıklığı ise listenin her zaman
  tek-elemanlı olmasıyla sınırlı kalıyor; sonradan kırılma riski taşıyan bir
  1:1 varsayımını koda gömmekten kaçınılıyor.
- **İç takip**: `HashSet<string> SeenShiftIds` — Held'e ulaşan her `shiftId`
  burada işaretlenir; bir `ClueDefinition`'ın tüm `requiredShiftIds`'i bu
  kümede olduğunda ilgili `clueId`, `MarkClueKnown`'a düşer. `SeenShiftIds`,
  Gece/Oturum Durumu'nun `FiredTriggerIds`'inden ayrı ve bağımsızdır (farklı
  sahiplik — bkz. sistem sınırı notu, Overview).
- **Idempotency**: `MarkClueKnown(clueId)`, `clueId` zaten biliniyorsa
  sessiz no-op'tur — `OnClueKnown` tekrar fırlamaz.
- **Kalıcılık mekanizması ve merkezi kayıt (design-review, 2026-08-02 —
  netleştirildi)**: `KnownClueIds`/`SeenShiftIds`, `Gece/Oturum
  Durumu`'nun kendi `HashSet<string> FiredTriggerIds` deseniyle birebir
  aynı şekilde bir **statik/singleton, sahneden bağımsız plain C#
  servis**tir (persistent GameObject/`DontDestroyOnLoad` DEĞİL) — sahne
  yüklemeleri arasında hayatta kalır, MVP'de disk kalıcılığı yoktur.
  `ClueDefinition` kayıtları, sahne başına kopyalanmış asset'ler DEĞİL,
  **tek bir proje-seviyesi kayıt** (merkezi bir ScriptableObject listesi
  ya da eşdeğeri) üzerinden yüklenir — bu, aynı `clueId`'nin iki farklı
  sahnede tutarsız tanımlanmasını yapısal olarak imkânsız kılar (tek
  doğruluk kaynağı). İki farklı `ClueDefinition` kaydı aynı `clueId`'yi
  taşırsa, edit-time validasyon (aşağıdaki boş-liste kontrolüyle aynı
  geçişte) hata verir, build engellenir.
- **Abonelik zamanlaması (design-review, 2026-08-02 — netleştirildi)**:
  Bu sistem, yukarıdaki statik/singleton servisin **ilk erişildiği anda**
  (ör. servisin static constructor'ı ya da oyunun başlangıç
  bootstrap'ı — sahne-lokal bir `MonoBehaviour`'ın `Awake`/`OnEnable`'ı
  DEĞİL) `Işık/Volume Durum Sistemi`'nin `OnShiftStateChanged`'ine
  abone olur. Bu, sistemin kendisinin Unity'nin sahne-başına script
  çalıştırma sırasına bağımlı kalmasını önler — servis oyunun tüm
  ömrü boyunca zaten var ve dinliyor olduğundan, bir Persistent shift'in
  sahne-yükleme-sonrası tek seferlik re-fire'ını (bkz.
  `isik-volume-durum-sistemi.md` Edge Cases) kaçırma riski yoktur. Bu
  sistem, aşağı akış abonelerine (Diyalog/Anlatı İçeriği, Sahne Kesmeli
  Anlatı) önerdiği "geç-abonelik olursa kendi init'inde uzlaş" desenini
  kendi Işık/Volume aboneliği için gerektirmez, çünkü kendisi hiç geç
  abone olmaz.

### States and Transitions

Bir `clueId` için sadece iki durum vardır: **Unknown → Known**, tek yönlü,
geri dönüşü yok (oyuncu bir şeyi "unutmaz"). Üçüncü bir ara durum ("bilinen
ama henüz diyaloğa bağlanmamış") **kasıtlı olarak eklenmedi**: Player
Fantasy'deki "Geç Gelen Tanınma" ve "Sessiz Suç Ortaklığı" hissi, *ne zaman*
ve *nasıl* referans verileceğine dair bir karardan doğuyor — bu, tüketen
sistemin (Diyalog/Anlatı İçeriği) kendi "bu satırı zaten oynattım mı"
bookkeeping'idir, tıpkı bu sistemin kendi "known" yarısını Gece/Oturum'un
"fired" yarısından ayrı tuttuğu gibi (bkz. Overview, sistem sınırı). Bu
sistem sadece *bilgiyi* tutar; *o bilginin ne zaman anlatıya döküleceğini*
tutmaz.

Bir `ClueDefinition` seviyesinde (tekil `shiftId`'lerden ayrı) örtük bir ara
durum vardır: bazı ama tüm değil `requiredShiftIds` görülmüş — ama bu hiçbir
yerde adlandırılmış bir "state" değildir, sadece `SeenShiftIds ∩
requiredShiftIds`'in `requiredShiftIds`'e eşit olup olmadığının anlık
kontrolüdür; dışarıya hiçbir API bu ara durumu sızdırmaz (`IsClueKnown`
sadece tam tamamlanmış sonucu döner).

### Interactions with Other Systems

**Genel sorgu/yazma sözleşmesi** (tüm tüketiciler için):
- `void MarkClueKnown(string clueId)` — `clueId`'yi doğrudan bilindi olarak
  işaretler (Işık/Volume dışı yollarla da tetiklenebilir, ör. gelecekte bir
  diyalog seçimi bir ipucunu doğrudan açığa çıkarırsa); zaten biliniyorsa
  no-op.
- `bool IsClueKnown(string clueId)` — bilinmeyen bir `clueId` sorgulanırsa
  `false` döner (hata fırlatmaz).
- `IReadOnlyCollection<string> GetKnownClueIds()` — Plot Twist/Final
  Sekansı'nın tüm birikmiş durumu okuması için.
- `event OnClueKnown(string clueId)` — bir kez, tam olarak `Unknown→Known`
  geçişinde fırlar. **Diyalog/Anlatı İçeriği** hem bu event'e abone
  olabilir hem de sahne girişinde `IsClueKnown`/`GetKnownClueIds` ile
  anlık sorgu atabilir (iki kullanım deseni de sözleşmenin parçası).
  **(Sahne Kesmeli Anlatı artık bu event'e abone değil — design-review,
  2026-08-03, bkz. Overview ve Dependencies)**

**Işık/Volume Durum Sistemi'ne abonelik**: Bu sistem `OnShiftStateChanged
(shiftId, newState, zoneCenter, radius)`'a abone olur ve **sadece
`newState == Held`** geçişlerini işler (Shifting-In/Out, Dormant yok
sayılır — "bilinmek" için oyuncunun anıyı *tam olarak* deneyimlemiş olması
gerekir, yarım geçiş yetmez). Handler: `shiftId`'yi `SeenShiftIds`'e ekle →
bu `shiftId`'yi `requiredShiftIds` listesinde taşıyan her `ClueDefinition`'ı
bul → o `clueId`'nin tüm `requiredShiftIds`'i artık `SeenShiftIds` içindeyse
`MarkClueKnown(clueId)` çağır. `zoneCenter`/`radius` bu sistem tarafından
kullanılmaz (tüketilmeden yok sayılır) — sadece mekansal ses/görsel
tüketiciler için taşınıyor. **Persistent shift'ler için not**: bir
`Persistent=true` shift zaten Held-Persistent iken sahne yeniden
yüklendiğinde Işık/Volume, Edge Case kuralı gereği yükleme sonrası bir kez
daha `OnShiftStateChanged(Held)` fırlatır — bu sistem bunu `SeenShiftIds`'e
tekrar eklemeyi dener ama `HashSet.Add` zaten idempotent olduğundan ve
`MarkClueKnown` no-op olduğundan zararsızdır, ekstra bir koruma gerekmez.
**Bu "zararsız" iddiası, Core Rules'ta artık adlandırılan kalıcılık
mekanizmasına dayanır (design-review, 2026-08-02 — netleştirildi)**:
`KnownClueIds`/`SeenShiftIds` sahne-lokal DEĞİL statik/singleton bir
servis olduğundan, sahne reload'ları arasında zaten hayatta kalırlar —
re-fire edilen event sadece zaten-bilinen bir kümeye idempotent bir
ekleme dener. Sistem sahne-lokal implemente edilseydi, reload sonrası
`IsClueKnown` sorgulayan downstream sistemler re-fire event'i işlenene
kadar yanlış-negatif alabilirdi — Core Rules'taki mekanizma seçimi bu
riski tanım gereği ortadan kaldırır.

**Çoklu Gece İlerlemesi** (Vertical Slice): geceler arası okuma/yazma bu
sistemin `KnownClueIds`/`SeenShiftIds`'ini serileştirip geri yükleyecek —
bu GDD'nin MVP kapsamında disk kalıcılığı yoktur (Gece/Oturum Durumu ile
aynı desen), ama veri modeli (iki düz `HashSet<string>`) serileştirmeye
zaten uygundur; genişletme bu sistemde yapısal bir değişiklik gerektirmez.

## Formulas

**N/A** — bu sistem saf bayrak/küme mantığı taşır, sayısal hesaplama,
eğri ya da "feel" parametresi yok. `ClueDefinition` eşleşmesi bir
küme-kapsama kontrolüdür (`SeenShiftIds ⊇ requiredShiftIds` — ALL
semantiği, bkz. Core Rules), matematiksel bir formül değil.

## Edge Cases

- **Eğer bir `ClueDefinition.requiredShiftIds`, hiçbir Işık/Volume
  tetikleyicisinin ateşlemediği bir `shiftId` listelerse**: O `clueId`
  abonelik yolundan asla Known'a ulaşamaz — `SeenShiftIds` bu girişi hiç
  kazanmaz, küme-kapsama kontrolü asla başarılı olmaz. Bu sessiz bir yazım
  hatasıdır; **sahne-yükleme tutarlılık kontrolü (design-review, 2026-08-02
  — somutlaştırıldı)**: `ClueConsistencyValidator.ValidateScene(sceneId)`,
  sahne yüklendiğinde çalışır, tüm `ClueDefinition.requiredShiftIds`'i o
  sahnede yapılandırılmış tetikleyici `shiftId`'leriyle çapraz kontrol
  eder; eşleşmeyen her `(clueId, shiftId)` çifti
  `GetOrphanedClueIds()`'e eklenir ve bir `Debug.LogWarning` basılır
  (build-blocking DEĞİL — bu bir content-authoring uyarısıdır, çalışma
  zamanı davranışını bozmaz, sadece "sonsuza kadar tamamlanamaz" bir
  ipucuyu erken tespit eder).
- **Eğer bir `ClueDefinition.requiredShiftIds` boş bir liste olarak
  yapılandırılırsa (design-review, 2026-08-02 — eklendi)**: Runtime'da
  clamp/varsayılan atama YAPILMAZ. Bu, edit-time validasyonun
  (`requiredShiftIds.Count == 0` kontrolü) yakalayıp build'i engellediği
  bir tasarım hatasıdır — bkz. Core Rules, "requiredShiftIds boş olamaz."
- **Eğer iki farklı `ClueDefinition` kaydı aynı `clueId`'yi taşırsa
  (design-review, 2026-08-02 — eklendi)**: Edit-time validasyon hata
  verir, build engellenir — merkezi tek-kayıt modeli (bkz. Core Rules)
  bunu zaten yapısal olarak nadir kılar, ama içerik-yazım hatasına karşı
  açık bir kontrol yine de gereklidir.
- **Eğer iki farklı `ClueDefinition`, `requiredShiftIds`'te aynı
  `shiftId`'yi paylaşırsa**: Tam desteklenir, özel bir işlem gerekmez —
  handler, ateşlenen `shiftId`'yi içeren *her* `ClueDefinition`'ı gezer, bu
  yüzden tek bir Held geçişi aynı karede sıfır, bir ya da birden fazla
  ipucunu tamamlayabilir. `SeenShiftIds` paylaşılır, ipucuna göre
  kapsamlanmaz.
- **Eğer `MarkClueKnown(clueId)`, `requiredShiftIds`'i henüz tamamen
  ateşlenmemiş bir `clueId` için doğrudan çağrılırsa**: Başarılı olur ve
  ipucuyu yine de Known işaretler — metod `ClueDefinition`'dan habersizdir
  ve `SeenShiftIds`'e karşı doğrulama yapmaz. Bu kasıtlıdır (Core Rules
  gereği, diyalog-seçimi çağıranları shift gereksinimini tamamen
  atlayabilir), ama karşılanmamış ön koşullu bir ipucuyu doğrudan
  işaretlemek, normal yoldan "kazanılmış" bir ipucudan ayırt edilemez —
  hangi yoldan geldiğine dair bir denetim izi yok.
- **Eğer `GetKnownClueIds()`, hiçbir ipucu bilinmeden önce çağrılırsa**:
  Boş bir `IReadOnlyCollection<string>` döner, asla null — çağıranlar
  (Plot Twist/Final Sekansı) null kontrolü olmadan güvenle
  iterate/`.Count` yapabilir.
- **Eğer `IsClueKnown`, hiçbir eşleşen `ClueDefinition`'ı olmayan bir
  `clueId` için sorgulanırsa** (yazım hatası, yanlış ID): Meşru olarak
  bilinmeyen bir ipucuyla aynı şekilde `false` döner — ayrı bir "tanımsız
  ipucu" sinyali yoktur, bu yüzden tüketici bir sistemdeki yazım hatalı
  `clueId` sessizce "henüz bilinmiyor" olarak başarısız olur, hata
  vermez.
- **Eğer bir abone, bir ipucu daha önce sahne yüklemesinde zaten
  bilindi olarak işaretlendikten sonra `OnClueKnown`'a bağlanırsa**: O
  fırlamayı tamamen kaçırır — event'ler tekrar oynatılmaz. Init sırası
  Işık/Volume'un erken Held tetiklemelerinin gerisinde kalan herhangi bir
  sistem, sadece event'e güvenmek yerine kendi init'inde
  `GetKnownClueIds()`/`IsClueKnown` ile uzlaşmalıdır (Detailed Design
  zaten Diyalog/Anlatı İçeriği için bu ikili deseni işaretliyor).
- **Eğer `requiredShiftIds` yinelenen bir `shiftId` içeriyorsa** (yazım
  hatası, aynı ID iki kez): İşlevsel bir etkisi yoktur — `HashSet`
  kapsaması yinelenenleri zaten tekilleştirir, tamamlanma yine de sadece
  tekil kümenin görülmesini gerektirir.
- **Eğer aynı sahnede iki (ya da daha fazla) ipucu Known olursa**:
  Aralarında kodlanmış bir nedensel/zamansal sıra yoktur — bu
  **kasıtlıdır** (bkz. Core Rules/Formulas: sıra bilgisi bilerek
  tutulmuyor, Pillar 1'in "objektif gerçeklik yok" ve Pillar 5'in "sıra
  değil, birikim" gerekçesiyle); tüketici sistemler ipuçları arasında sıra
  çıkarımı yapmamalıdır.

## Dependencies

**Bağımlıdır**:
- **Işık/Volume Durum Sistemi** — `OnShiftStateChanged` event'ine abone
  olur (sadece `Held` durumu işlenir)

**Kendisine bağımlı olanlar**:
- **Diyalog/Anlatı İçeriği** *(2026-08-02, Quick Spec olarak tasarlandı)*
  — `IsClueKnown`/`GetKnownClueIds` sorgular, `OnClueKnown`'a abone
  olabilir

**Not — çapraz-referans düzeltildi (design-review, 2026-08-03 —
`/review-all-gdds` verification bulgusu)**: **Sahne Kesmeli Anlatı
artık bu listede değil.** Önceki taslakta bu sistemin `OnClueKnown`
event'ine abone olduğu belirtiliyordu; o sistemin 2026-08-03
revizyonu bu aboneliği tamamen kaldırdı — kendi gece-sonu doygunluk
sinyalini artık Gece/Oturum Durumu'nun `SettledTriggerIds.Count`'undan
okuyor (design-review, 2026-08-04 üçüncü tur bulgusuyla
`FiredTriggerIds.Count`'tan düzeltildi, bkz.
`design/quick-specs/sahne-kesmeli-anlati-2026-08-02.md`
Core Rules ve Dependencies). Bu sistem artık Sahne Kesmeli Anlatı'nın
hiçbir bağımlılığı değil.

- **Çoklu Gece İlerlemesi** (Vertical Slice, henüz tasarlanmadı) —
  geceler arası `KnownClueIds`/`SeenShiftIds` serileştirmesini
  üstlenecek
- **Plot Twist/Final Sekansı** (Full Vision, henüz tasarlanmadı) —
  `GetKnownClueIds()` ile tüm birikmiş durumu okur

**Not**: Gece/Oturum Durumu ile kasıtlı olarak bağımsızdır — ayrı
"yarım"lar tutarlar (bkz. Overview, sistem sınırı notu), birbirine sorgu
atmazlar.

## Tuning Knobs

**N/A** — ayarlanabilir bir "feel" parametresi yok. `ClueDefinition`
kayıtları (`clueId`, `requiredShiftIds`) içerik yapılandırmasıdır, tuning
knob değil — her yeni ipucu için elle tanımlanır (bkz. Core Rules).

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

1. **GIVEN** `requiredShiftIds = [A, B]` olan bir `ClueDefinition`,
   **WHEN** sadece A shift'i Held'e ulaşır, **THEN**
   `IsClueKnown(clueId)` `false` döner, `OnClueKnown` fırlamaz.
2. **GIVEN** `requiredShiftIds = [A, B]` ve A zaten Held, **WHEN** B
   Held'e ulaşır, **THEN** `IsClueKnown(clueId)` `true` döner,
   `OnClueKnown(clueId)` tam olarak bir kez fırlar.
3. **GIVEN** zaten Known işaretli bir ipucu, **WHEN**
   `MarkClueKnown(clueId)` tekrar çağrılır (doğrudan ya da yinelenen
   Held re-fire yoluyla), **THEN** sessiz no-op olur, `OnClueKnown`
   ikinci kez fırlamaz.
4. **GIVEN** abone olunan bir shift, **WHEN** Shifting-In, Shifting-Out
   ya da Dormant'a geçer, **THEN** hiçbir `SeenShiftIds` girişi
   eklenmez, hiçbir ipucu-tamamlanma kontrolü çalışmaz (sadece
   `newState==Held` işlenir).
5. **GIVEN** hiçbir ipucu bilinmiyor, **WHEN** `GetKnownClueIds()`
   çağrılır, **THEN** boş, null-olmayan bir koleksiyon döner.
6. **GIVEN** eşleşen bir `ClueDefinition`'ı olmayan bir `clueId`
   (yazım hatası), **WHEN** `IsClueKnown(clueId)` çağrılır, **THEN**
   istisna/ayrı bir hata olmadan `false` döner — meşru bilinmeyen bir
   ipucundan ayırt edilemez.
7. **GIVEN** aynı sahnede iki `ClueDefinition` de Known olur, **WHEN**
   `GetKnownClueIds()`/`IsClueKnown` API yüzeyi incelenir, **THEN** hiçbir
   metod/property sıra ya da zaman damgası verisi döndürmez — API'nin
   kendisi sıra bilgisi taşımadığını doğrular. *(design-review,
   2026-08-02: önceki hali test edilebilir bir API-yüzeyi iddiasını
   tüketici sistemlerin davranışına yönelik test edilemez bir yönergeyle
   ["tüketiciler sıra çıkarımı yapmamalı"] birleştiriyordu — ayrıldı, bu
   yönerge Dependencies bölümünde bir sözleşme notu olarak kalır.)*
8. **GIVEN** bir `ClueDefinition.requiredShiftIds`, sahnede hiçbir
   yapılandırılmış tetikleyicinin ateşlemediği bir `shiftId` listeler,
   **WHEN** sahne yüklenir ve `ClueConsistencyValidator.ValidateScene(sceneId)`
   çalışır, **THEN** o `clueId`/`shiftId` çifti `GetOrphanedClueIds()`'e
   eklenir ve bir `Debug.LogWarning` basılır — build engellenmez (bu bir
   content-authoring uyarısıdır), ama sorun sessizce kaybolmaz.
   *(design-review, 2026-08-02: önceki "erişilemez olarak işaretlenir"
   ifadesi mekanizma, zamanlama ya da gözlemlenebilir çıktı belirtmiyordu
   — somutlaştırıldı.)*
8a. **GIVEN** bir `ClueDefinition.requiredShiftIds` boş bir liste olarak
   yapılandırılmış, **WHEN** edit-time validasyon çalışır, **THEN** hata
   verilir, build engellenir — vacuous-truth "anında Known" hatası
   hiçbir zaman runtime'a ulaşmaz.
8b. **GIVEN** iki farklı `ClueDefinition` aynı `clueId`'yi taşıyor,
   **WHEN** edit-time validasyon çalışır, **THEN** hata verilir, build
   engellenir, hata mesajı çakışan iki kaydı işaret eder.
9. **GIVEN** `requiredShiftIds`'i tam ateşlenmemiş bir ipucu, **WHEN**
   `MarkClueKnown(clueId)` doğrudan çağrılır, **THEN** ipucu yine de
   Known olur — metod `SeenShiftIds`'e karşı doğrulama yapmaz.
10. **GIVEN** iki farklı `ClueDefinition`, `requiredShiftIds`'te aynı
    `shiftId`'yi paylaşır, **WHEN** o shift Held'e ulaşır, **THEN** her
    `ClueDefinition` bağımsız değerlendirilir, aynı event'te sıfır, bir
    ya da ikisi de tamamlanabilir.
11. **GIVEN** sahne yeniden yüklemeden önce zaten Held olan bir
    `Persistent=true` shift, **WHEN** sahne yeniden yüklenir ve
    Işık/Volume edge-case kuralı gereği `OnShiftStateChanged(Held)`
    tekrar fırlar, **THEN** hiçbir yinelenen `OnClueKnown` fırlamaz,
    hata oluşmaz.
12. **[design-review, 2026-08-02 — ERTELENDİ durumundan çıkarıldı, her
    iki blocker GDD de artık mevcut]** **GIVEN** bir ipucu Known olmuş
    (gecenin başında bir tetikleyici ateşlenmiş) ve sahne yeniden
    yüklenmiş (ör. asansörle kat değişimi), **WHEN** Diyalog/Anlatı
    İçeriği sahne başlangıcında kendi seçim mantığını çalıştırır
    (`IsClueKnown(clueId)` sorgusuyla, bkz.
    `design/quick-specs/diyalog-anlati-icerigi-2026-08-02.md` Core
    Rules), **THEN** doğru sonucu alır — event'e hiç abone olmadan,
    doğrudan sorgu yoluyla, reload/geç-abonelik riskinden tamamen
    bağımsız.
12b. **[design-review, 2026-08-02 — ikinci revizyon: WHEN sadeleştirildi,
    tüketici sistem detayı kaldırıldı]** **GIVEN** gecenin son
    `clueId`'sinin son gerekli `shiftId`'si Held'e ulaşır, **WHEN**
    bu geçiş işlenir, **THEN** `OnClueKnown` bu ipucu için tam olarak
    bir kez fırlar (AC#2 ile tutarlı) — bu sistem hangi tüketicinin
    dinlediğinden habersizdir, sadece kendi event sözleşmesini
    doğrular. **(Örnek düzeltildi — design-review, 2026-08-03: önceki
    metin burada Sahne Kesmeli Anlatı'nın bu event'i "doygunluk
    sayacı" olarak kullandığını örnek veriyordu — o sistem artık bu
    event'e hiç abone değil, bkz. Dependencies. Bu AC'nin kendi
    doğrulaması etkilenmez, sadece örnek tüketici referansı bayattı;
    genel ilke aynı kalır: bu sistem hangi tüketicinin dinlediğinden
    habersizdir.)**

## Open Questions

- **Eksik ipucu senaryosu**: Oyuncu MVP'nin 2-3 tetikleyicisinden birini
  hiç ateşlemezse (gerçekçi bir olasılık, nadir bir edge case değil),
  Plot Twist/Final Sekansı'nın bir "eksik ipucu" içerik dalı olmalı mı,
  yoksa MVP'de callback'siz bir final kabul edilebilir mi? Sahip: Plot
  Twist/Final Sekansı GDD'si (Full Vision).
- **Erken-tamamlanma tempo riski (CD-GDD-ALIGN, yükseltildi — gereksinim
  notu)**: Bu sistem hiçbir zamansal durum tutmuyor, bu yüzden tempo
  kapısını hiçbir aşağı akış sistemi kendi zaman/oturum takibini icat
  etmeden uygulayamaz. **Diyalog/Anlatı İçeriği GDD'si yazılırken açık bir
  gereksinim olarak ele alınmalı**: oyuncu 2-3 tetikleyiciyi de ilk birkaç
  dakikada bulursa, Pillar 5'in "kademeli birikim" hissini korumak için
  bir tempo kısıtı (ör. gece-başına maksimum callback sayısı) tanımlanmalı.
  Sahip: Diyalog/Anlatı İçeriği GDD'si (zorunlu gereksinim, açık soru
  değil).
- **Eksik/sıfır-ipucu playthrough'u (CD-GDD-ALIGN notu)**: MVP'nin
  no-fail tasarımı gereği, oyuncu bir ya da tüm ipuçlarını kaçırarak
  oynaması **beklenen bir durum** olarak ele alınmalı, kenar durum değil
  — sistem kendi içinde zaten tutarlı (boş küme, hata yok). Plot
  Twist/Final Sekansı GDD'si bunu bu şekilde tasarlamalı. Sahip: Plot
  Twist/Final Sekansı GDD'si.
- **Kalıcılık asimetrisi riski — Diyalog/Anlatı İçeriği'ne karşı
  (design-review, 2026-08-02, narrative-director bulgusu)**: Bu
  sistemin `KnownClueIds`/`SeenShiftIds`'i Çoklu Gece İlerlemesi
  geldiğinde geceler arası kalıcı olacak şekilde tasarlanmış (bkz.
  Dependencies). Ama Diyalog/Anlatı İçeriği'nin kendi ayrı bookkeeping'i
  (`UsedCallbackIds` — "bu callback'i zaten oynattım mı") için hiçbir
  kalıcılık planı o sistemin quick-spec'inde yok. Eğer bir ipucu
  geceler arası Known kalırken "zaten oynatıldı" bilgisi her gece
  sıfırlanırsa, aynı callback satırı ikinci bir gecede birebir tekrar
  oynayabilir — bu, "Otel Unutmuyor" vaadini tam da gerçekleşmesi
  gereken anda kırar. **Sahip düzeltmesi (design-review, 2026-08-02 —
  ikinci revizyon)**: Önceki taslak bu sorunu henüz yazılmamış Çoklu
  Gece İlerlemesi GDD'sine atıyordu — ama kusurlu kod yolu
  (`UsedCallbackIds`) **bugün zaten** `diyalog-anlati-icerigi-2026-08-02.md`'de
  var ve hiçbir kalıcılık planı yok. Aşağıdaki "Erken-tamamlanma tempo
  riski" sorusuyla aynı desene uyularak, sahip doğrudan mevcut dokümana
  atandı. Sahip: **Diyalog/Anlatı İçeriği quick-spec'i** (mevcut dosya —
  kendi kalıcılık planı, Çoklu Gece İlerlemesi'nin `KnownClueIds`
  serileştirmesiyle aynı geçişte netleştirilmeli, o GDD henüz
  yazılmadan önce bu spec'e bir not düşülebilir).
- **`MarkClueKnown` bypass'ının denetim izi yokluğu (design-review,
  2026-08-02 — Edge Cases'ten taşındı)**: Doğrudan `MarkClueKnown`
  çağrısıyla işaretlenen bir ipucu, normal shift-yoluyla "kazanılmış"
  bir ipucudan ayırt edilemez (bkz. Edge Cases). MVP için zararsız, ama
  gelecekte bir analytics/anti-cheat/debug aracı bu ayrımı isterse
  (ör. "oyuncu bu ipucuyu gerçekten mi tetikledi" sorusu), şu anki veri
  modeli buna cevap veremez. Sahip: gelecekteki herhangi bir analytics/
  anti-cheat tüketicisi — şimdilik aksiyon gerekmiyor.
