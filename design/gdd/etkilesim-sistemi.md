# Etkileşim Sistemi (Interaction System)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`)
> **Author**: user + agents
> **Last Updated**: 2026-08-03
> **Implements Pillar**: Pillar 3 (Görev Gerçekliği)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (revised) 2026-08-02 — Overview'daki asansör örneği Dependencies'teki hariç tutmayla çelişiyordu, düzeltildi; UI Requirements'a Pillar 1 muafiyeti notu eklendi
> **`/review-all-gdds` (2026-08-03)**: `MovementLockScope.MoveOnly` çağrıları eklendi, kilit-ön-kontrol mekanizması (`IsLocked`) netleştirildi, `OnHoldBlocked()` `IInteractable` arayüzüne eklendi, stale "henüz tasarlanmadı" etiketleri düzeltildi — bkz. `design/gdd/gdd-cross-review-2026-08-03-verification.md`

## Overview

Etkileşim Sistemi, oyuncunun otel içindeki nesnelerle kurduğu tüm doğrudan
temasın tek giriş noktasıdır: Birinci Şahıs Kontrolcü'nün `EyeCamera`'sından
yapılan bir raycast, menzil içindeki etkileşim nesnelerini tespit eder,
ekranda bir crosshair/prompt gösterir, ve `Interact` girdisi geldiğinde
nesnenin kendi mantığını tetikler. Sistem iki etkileşim türünü destekler —
anlık (tek basış) ve basılı-tutmalı (Birinci Şahıs Kontrolcü'nün referans
sayaçlı hareket kilidini kullanan) — ve her etkileşim nesnesini aynı
zamanda Birinci Şahıs Kontrolcü'nün yaklaşma-yavaşlaması formülünün
okuduğu ortak bir "etkileşilebilir" bayrağıyla işaretler.

Oyuncu için bu, otelin somut, gerçek bir iş yeri olarak hissettirilmesinin
ilk adımıdır (Pillar 3: Görev Gerçekliği) — bir kapı koluna basmak, bir
malzeme kutusunu kaldırmak hep aynı basit, güvenilir eylem diliyle
gerçekleşir (asansöre binmek gibi otomatik/trigger-tabanlı istisnalar
kasıtlıdır — bkz. Dependencies). Bu sistem olmadan oyuncu hiçbir şeyle
doğrudan temas kuramaz — Görev/Taşıma Döngüsü'nün taşıma eylemleri ve
Anı-Tetikleyici Etkileşim'in tetikleyicileri de dahil, otel içindeki her
"buna dokunuyorum" anı bu sistem üzerinden akar.

## Player Fantasy

**Bu fantazi fiziksel icraya dairdir, o anın anlamına değil (design-review,
2026-08-04 — verification design-theory bulgusu, netleştirildi; bkz.
aşağıdaki not)**: Oyuncunun eli **nasıl** hareket ettiğini zaten bilir —
E'ye basmak ile kutunun kalkması, kapı kolunun çekilmesi arasında acemi
bir "bunu nasıl yapacaktım" duraksaması yok; beden nesneyi tanır, hareket
akıcı ve mekanik olarak güvenli çalışır (**Eller Zaten Biliyor**, Pillar
3: Görev Gerçekliği). Bu, Birinci Şahıs Kontrolcü'nün "Bedenin Hafızası"
fantazisinin doğal uzantısıdır — orada adımlar tanıdık bir ritimde
düşüyordu, burada eller aynı güvenle çalışıyor. Bu, oyuncunun **bir şeyi
yapmayı seçip seçmediği** sorusuna karışmaz — sadece seçtiği şeyi nasıl
**icra ettiğine** dair; belirli bir Hold nesnesi (ör. Anı-Tetikleyici
Etkileşim) kendi üzerine bilinçli bir seçim/tereddüt anlamı katman olarak
ekleyebilir, bu genel sistemin fiziksel-icra fantazisiyle çelişmez —
"eller nasıl yapacağını zaten biliyor" ile "zihin bunu yapmayı bilerek
seçiyor" aynı anda doğru olabilir.

Bu yetkinlik rahatlatıcı değil, hafifçe rahatsız edicidir: hiç
acemilik yok, hiç "bu ilk seferim" hissi yok — sanki bu hareket binlerce
kez yapılmış. Otelin gerçekliği sorgulanmaya başladığında (Pillar 1),
bu sorunun gölgesi buraya da düşer: eğer eller düşünceden önce
biliyorsa, başka neyi önceden biliyor olabilirler? Etkileşim sistemi bu
soruyu asla yüksek sesle sormaz — sadece akıcı, tereddütsüz eylem
diliyle onu fark ettirmeden ekiyor.

## Detailed Design

### Core Rules

- **Odaklanma tespiti**: Her karede `EyeCamera.position`'dan `EyeCamera.forward`
  yönünde bir `SphereCast` (ince bir ray değil — ~0.05m yarıçap, "Eller Zaten
  Biliyor" fantazisi oyuncunun küçük bir kapı koluna nişan almasıyla anında
  bozulur), menzil **2.0m**. Bir `IInteractable` çarptığında ve
  `CanInteract==true` ise, sistem **Focused** durumuna geçer.
- **Ortak "etkileşilebilir" kayıt defteri (statik registry)**: Her
  `IInteractable`, `OnEnable`'da kendini statik bir `InteractableRegistry`'ye
  ekler, `OnDisable`'da çıkarır. Bu sistemin odaklanma taraması (SphereCast)
  ile Birinci Şahıs Kontrolcü'nün yaklaşma-yavaşlaması taraması (en yakın
  mesafe) **aynı registry'yi** okur — ikinci bir "bayrak" bileşenine gerek
  yok, bir `IInteractable` olmak zaten flaglanmış olmak demektir. **Sahiplik
  notu**: Bu registry bu GDD'de tanımlanır ve sahiplenilir; Birinci Şahıs
  Kontrolcü'nün GDD'sinin Dependencies bölümü buna referans vermeli (çift
  yönlü tutarlılık).
- **Etkileşim türleri**: `IInteractable.Type` iki değerden biri — `Instant`
  (tek basış, anlık) veya `Hold` (basılı-tutma, `HoldDuration` boyunca).
  Anlık örnek: bir malzeme kutusunu almak. Basılı-tutma örneği: Anı-Tetikleyici
  Etkileşim (tasarlandı — design-review, 2026-08-04 verification
  bulgusuyla "henüz tasarlanmamış" etiketi düzeltildi, bu etiket
  2026-08-03'te bir kez düzeltilmiş ama bu ikinci kopya gözden kaçmıştı).
- **Crosshair/prompt**: Bu sistem sahiplenir (Birinci Şahıs Kontrolcü'nün UI
  Requirements'ına göre). Crosshair sadece Idle↔Focused geçişinde durum
  değiştirir (her karede değil); prompt metni `IInteractable.PromptText`'ten
  gelir.
- **Varsayılan Hold-doldurma göstergesi (design-review, 2026-08-04 —
  verification design-theory bulgusu, eklendi, kritik bulgu)**: Önceki
  taslak, ilerleme görselleştirmesini "isteyen nesnenin kendi
  sorumluluğu" sayıyordu (bkz. eski "Hold-progress görseli" notu) — ama
  hiçbir nesne bunu hiç uygulamıyordu (Anı-Tetikleyici Etkileşim açıkça
  reddediyordu), sonuç olarak her Hold etkileşiminde 0.6-1.5sn boyunca
  **hiçbir görsel geri bildirim yoktu**. Düzeltme: bu sistem, zaten
  sahiplendiği crosshair'in bir parçası olarak **sade, dramatik olmayan
  bir doldurma halkası/çubuğu** çizer — `Holding` durumundayken kendi
  hesapladığı `t` değerinden doğrudan sürülür (nesnenin `OnHoldProgress(t)`
  callback'ini beklemesine gerek yok, aynı karede zaten hesaplanmış
  veri). Bu **varsayılan ve otomatiktir** — hiçbir Hold nesnesi bunu
  kazanmak için bir şey yapmaz, crosshair zaten var olduğundan gösterge
  de ücretsiz gelir. Bir nesne kendi ek/özel sunumunu istiyorsa
  (`OnHoldProgress(t)`'i dinleyip kendi VFX/ses'ini sürerek), bu
  varsayılanın **üzerine** eklenir, yerine geçmez.
  **Devre dışı bırakma seçeneği (design-review, 2026-08-04 — verification
  bulgusu, eklendi, kritik bulgu)**: Bazı nesneler için "hiçbir görsel
  geri bildirim olmaması" **kasıtlı ve tartışılmış bir tasarım kararı**
  olabilir (ör. Anı-Tetikleyici Etkileşim — bkz. o GDD'nin Player Fantasy'si,
  "his oyuncunun içinde yaşar, oyun tarafından aracılanmaz" argümanı).
  Evrensel bir varsayılan bu tür bir kararı sessizce geçersiz kılmamalı.
  `IInteractable` arayüzüne yeni bir salt-okunur `bool SuppressDefaultHoldFill`
  alanı eklenir (varsayılan `false` — çoğu nesne bunu hiç düşünmeden
  varsayılanı alır); `true` döndüren bir nesne için crosshair'in doldurma
  göstergesi **hiç çizilmez**, gerçek bir sıfır-geri-bildirim garantisi
  sağlanır. Bu, "her Hold'un bir varsayılanı olsun" ile "belirli bir Hold'un
  kasıtlı olarak hiçbir şeyi olmasın" ihtiyaçlarının ikisini de karşılar.
- **Girdi**: Mevcut "Gameplay" action map'indeki `Interact` (Button) —
  `Instant` için `WasPressedThisFrame`, `Hold` için `IsPressed`.
- **Yarış durumu koruması**: Bir nesne `OnInteract()` içinde kendini
  deaktive/yok ederse, registry kaydı `OnDisable`'da senkron olarak
  temizlenir; hem bu sistemin hem Birinci Şahıs Kontrolcü'nün registry
  taramaları Unity'nin nesne-null karşılaştırmasını kullanır (çiğ C# null
  değil) — aynı karede yok edilen bir nesneye erişim
  `MissingReferenceException` fırlatmaz.

### States and Transitions

| Durum | Giriş Koşulu | Çıkış / Sonraki Durum |
|---|---|---|
| **Idle** | SphereCast geçerli bir `IInteractable` bulamıyor | → **Focused** (geçerli hedef bulununca) |
| **Focused** | Geçerli hedef, buton basılı değil | → **Idle** (bakış ayrılır/menzil dışı/hedef devre dışı kalır); `Instant` tipte buton basışında `OnInteract()` çağrılır ve **Focused**'da kalınır; `Hold` tipte buton basılı tutulunca → **Holding** |
| **Holding** | `Hold` tipi bir hedefte `Interact` basılı tutuluyor | Girişte `RequestMovementLock(this, MovementLockScope.MoveOnly)` çağrılır (design-review, 2026-08-03 — `/review-all-gdds` verification bulgusu, düzeltildi: FPC'nin yeni varsayılanı `Full`'dür, bu `Look`'u da dondururdu ve bu sistemin kendi Hold-iptal yolunu (bakış çevirerek SphereCast hedefini kaybetme) imkânsız kılardı — istekçi kimliği: Etkileşim Sistemi'nin kendisi, aynı anda tek bir basılı-tutma olabilir). t=1'de `OnHoldComplete()` + `ReleaseMovementLock(this)` ile **Focused**'a döner; buton erken bırakılırsa **veya** SphereCast hedefi kaybederse (**hangisi önce olursa**) `OnHoldCancelled()` + `ReleaseMovementLock(this)` ile **Focused**'a döner |

### Interactions with Other Systems

**Birinci Şahıs Kontrolcü'ye çağrılar**: `EyeCamera` okunur (raycast için);
`RequestMovementLock(this, MovementLockScope.MoveOnly)`/
`ReleaseMovementLock(this)` (sadece `Hold` etkileşimleri sırasında —
`MoveOnly`, `Look`'u serbest bırakır, aşağıdaki Hold-iptal yolunun
çalışabilmesi için zorunludur).

**Aşağı akış arayüzü** (`IInteractable`):
```csharp
public interface IInteractable {
    InteractionType Type { get; }      // Instant | Hold
    float HoldDuration { get; }        // Hold değilse yok sayılır
    bool CanInteract { get; }
    string PromptText { get; }
    void OnFocusEnter(); void OnFocusExit();
    void OnInteract();                                        // sadece Instant
    void OnHoldProgress(float t); void OnHoldComplete(); void OnHoldCancelled();  // sadece Hold
    void OnHoldBlocked();                                     // sadece Hold — design-review 2026-08-03 eklendi (bkz. Edge Cases, kilit-zaten-tutuluyor durumu); önceki taslakta Edge Cases/AC#8 bu callback'i gerektiriyordu ama arayüzde hiç yoktu
    bool SuppressDefaultHoldFill { get; }                     // sadece Hold, varsayılan false — design-review 2026-08-04 eklendi (bkz. Core Rules, "Varsayılan Hold-doldurma göstergesi"); true dönerse crosshair'in doldurma göstergesi hiç çizilmez
}
```
Görev/Taşıma Döngüsü `Instant`'ı uygular (kutular); Anı-Tetikleyici
Etkileşim `Hold`'u uygular (design-review, 2026-08-04 — verification
bulgusuyla düzeltildi: bu satır önceden "OnHoldProgress'i kasıtlı olarak
kullanmaz" diyordu — artık doğru olan bu değil, bu sistemin varsayılan
crosshair doldurma göstergesi zaten otomatik çalışıyor; Anı-Tetikleyici
`OnHoldProgress`'i kendi **ek** bir VFX/ses için hâlâ kullanmıyor, ama
bu artık "sıfır geri bildirim" değil, "varsayılanın üzerine ekleme yok"
anlamına geliyor — bkz. o GDD'nin kendi Core Rules'ı).

**Asansör/Kat-Erişim Sistemi**: Bu sistemi kullanmaz — kendi trigger-zone
mantığına sahip olacak (sistemler indeksindeki mevcut bağımlılık grafiğiyle
tutarlı).

## Formulas

The `hold_progress` formula is defined as:

`t = clamp(elapsedHoldTime / HoldDuration, 0, 1)`

**Variables:**

| Variable | Meaning |
|---|---|
| `elapsedHoldTime` | saniye cinsinden, buton basılı tutulmaya başladığından beri geçen süre; iptalde sıfırlanır |
| `HoldDuration` | saniye, Tuning Knob, tipik 0.1–3.0s, her etkileşim nesnesi kendi değerini tanımlar |
| `t` | normalize edilmiş ilerleme, `OnHoldProgress(t)`'e her karede iletilir |

**Output Range:** 0 ile 1 arası, kelepçeli, monoton artan; t=1'de `OnHoldComplete()` çağrılır.
**Example:** `HoldDuration=1.2s`. `elapsed=0.6s`'de `t=0.6/1.2=0.5`.

**Neden doğrusal, eased değil**: Player Fantasy "eller nasıl yapacağını
zaten biliyor" diyor (design-review, 2026-08-04 — fiziksel/anlamsal ayrım
netleştirildikten sonra güncellendi) — dramatik bir "şarj olma" değil,
güvenli/mekanik bir **icra**. Ease-in özellikle acemi bir dirençle
karşılaşma hissi verirdi; doğrusal ilerleme, tam istendiği gibi çalışan
istikrarlı bir mekanizma hissi verir. Bu, o eylemin **anlamının**
tereddütsüz olduğunu iddia etmez — sadece elin fiziksel olarak akıcı
çalıştığını. Çekirdek sistem sinyali doğrusal kalmalı — belirli bir
etkileşim nesnesi "ani" ya da "beklenti" hissi istiyorsa, bu `t`'nin
üzerine aşağı akışta uygulanan bir sunum-katmanı eğrisi olmalı, çekirdek
sisteme gömülmemeli.

**SphereCast yarıçapı/menzili**: Formül gerektirmez — ~0.05m yarıçap ve 2.0m
menzil sabit değerlerdir (Tuning Knob), oyuncu hızı ya da kamera FOV'u gibi
bir değişkenle ölçeklenmez.

**Aşağı akış sözleşmesi**: `OnHoldProgress(t)` her zaman ham doğrusal `t`
değerini alır. Görev/Taşıma Döngüsü ve Anı-Tetikleyici Etkileşim, sürdükleri
efekt için `t`'yi kendi easing/eğrileriyle yeniden eşleyebilir — ama bu
yeniden eşleme kendi GDD'lerinde tanımlanır, burada değil.

## Edge Cases

- **Eğer Hold devam ederken hedef `Destroy()` edilir veya `SetActive(false)`
  ile disable olursa (registry'den çıkar)**: Hold anında iptal edilir,
  `OnHoldComplete()` ÇAĞRILMAZ, `RequestMovementLock` hemen serbest
  bırakılır ve state **Idle**'a döner (Focused'a değil — hedef artık yok).
  Bu, oyuncunun sadece bakışını çevirdiği (SphereCast hedefi kaybeder ama
  nesne sahnede geçerli kalır) durumdan ayrıdır; o durumda state
  **Focused**'a döner.
- **Eğer buton bırakma ve SphereCast'in hedefi kaybetmesi aynı frame'de
  gerçekleşirse**: İptal kontrolü sabit sırayla yapılır: önce hedef
  geçerliliği (registry/SphereCast), sonra buton durumu. Hangisi
  tetiklenirse tetiklensin sonuç aynıdır, ama `OnHoldProgress(t)` o frame
  için hiçbir koşulda çağrılmaz — iptal kontrolü her zaman progress
  güncellemesinden önce işlenir.
- **Eğer `HoldDuration <= 0` yapılandırılmışsa**: Sistem bunu Instant'a
  dönüştürmez; ilk karede `t` hesaplanmadan (sıfıra bölünmeyi önlemek için
  `HoldDuration <= 0` kontrolü bölme işleminden ÖNCE yapılır) doğrudan
  `OnHoldComplete()` çağrılır ve Editor'da `Debug.LogWarning` basılır.
- **Eğer SphereCast aynı karede birden fazla `IInteractable`'ı vurursa**
  (çakışan collider'lar): `Physics.SphereCastAll` sonuç sırası mesafeye göre
  garanti değildir; odak seçimi açıkça "en küçük `hit.distance`" kuralıyla
  yapılır, mesafe eşitliğinde collider'ın `InstanceID`'si en küçük olan
  kazanır (kareden kareye deterministik).
- **Eğer kilit BAŞKA bir sistem tarafından zaten tutuluyorsa** (örn.
  Sahne Kesmeli Anlatı bir cutscene için): Hold başlatılmaz, Holding
  state'ine hiç girilmez, buton girdisi yok sayılır ve `OnHoldBlocked()`
  callback'i tetiklenir. **Mekanizma netleştirildi (design-review,
  2026-08-03 — `/review-all-gdds` verification bulgusu, düzeltildi)**:
  Bu, `RequestMovementLock`'un kendisinden bir "ret" alarak değil —
  FPC'nin referans-sayaçlı kilidi hiçbir çağrıyı reddetmez, her zaman
  ekler — `Holding`'e girmeden **önce** bir **ön-kontrol** olarak
  uygulanır: `IPlayerState.IsLocked` `true` ise (bu sistemin kendi
  aktif bir Hold'u yoksa, ki `Focused`'dan `Holding`'e geçiş anında
  bu her zaman doğrudur), `RequestMovementLock` hiç çağrılmaz,
  `OnHoldBlocked()` tetiklenir. Referans sayaçlı kilit hiçbir zaman
  override edilmez veya kuyruğa alınmaz.
- **Eğer bir `IInteractable` Holding ortasında disable→enable döngüsüne
  girip registry'ye yeniden kayıt olursa**: Bu "hedef kaybedildi" sayılır
  (1. madde uygulanır). Holding state hiçbir zaman bir OnDisable/OnEnable
  çiftini "bekleyip" sonradan devam ettirmez — yeniden enable olmak yeni
  bir Focused girişi gerektirir.
- **Eğer registry iterasyonu sırasında bir `IInteractable` kendi
  `OnDisable`'ını tetikleyip registry'den kendini silerse** (örn. focus
  olunca kendini kapatan obje): Registry, canlı liste yerine iterasyon
  başında alınan salt-okunur bir snapshot üzerinden okunur; bu,
  koleksiyon-değişirken-iterasyon hatasını ve SphereCast/Birinci Şahıs
  Kontrolcü'nün aynı karede tutarsız sonuç görmesini engeller.
- **Eğer oyuncu Holding state'indeyken SphereCast farklı bir
  `IInteractable`'a denk gelirse** (iki obje arası geçiş açısı): Holding
  sırasında focus hedefi kilitlenir; SphereCast sonucu sadece "mevcut hedef
  hâlâ vuruluyor mu" (evet/hayır) için kullanılır. Otomatik hedef değişimi
  yoktur — yeni hedefe geçiş ancak mevcut Hold iptal/tamamlanıp
  Idle/Focused'a dönüldükten sonraki karede mümkündür.

## Dependencies

**Bağımlıdır** (hard):
- **Birinci Şahıs Kontrolcü** — `EyeCamera` referansını okur (raycast
  kaynağı); `Hold` etkileşimleri sırasında `RequestMovementLock(this,
  MovementLockScope.MoveOnly)`/`ReleaseMovementLock(this)` çağırır, ve
  `IsLocked`'ı ön-kontrol için okur (design-review, 2026-08-03 —
  eklendi). Bu sistem olmadan Etkileşim Sistemi çalışamaz — kamera yönü
  ve hareket kilidi olmadan ne odaklanma tespiti ne de basılı-tutma
  mümkündür.

**Kendisine bağımlı olanlar** *(design-review, 2026-08-03 — düzeltildi:
ikisi de artık tasarlanmış)*:
- **Görev/Taşıma Döngüsü** *(tasarlandı)* — `IInteractable.Instant`
  tipini uygular (malzeme kutularını almak için)
- **Anı-Tetikleyici Etkileşim** *(tasarlandı; design-review, 2026-08-04
  verification turuyla `Needs Revision`'a geri düştü — bkz.
  `gdd-cross-review-2026-08-04.md`)* — `IInteractable.Hold` tipini
  uygular, `OnHoldProgress(t)`'i kasıtlı olarak kullanmaz (bkz. o GDD'nin
  kendi Core Rules'ı) — **bu ret, 2026-08-04 verification bulgusuyla bu
  sistemin Hold Player Fantasy'siyle çeliştiği bulundu, henüz çözülmedi**
- **Birinci Şahıs Kontrolcü** *(design-review, 2026-08-04 — verification
  bulgusu, eklendi — tek yönlü bağımlılık boşluğu kapatıldı)* — Formül
  2'nin (`approach_slow_taper`) `d` değişkeni için bu sistemin
  `InteractableRegistry`'sini okur (bkz. Core Rules yukarıda,
  "yaklaşma-yavaşlaması taraması")

**Bağımlı DEĞİLDİR**:
- **Asansör/Kat-Erişim Sistemi** *(tasarlandı)* — kendi trigger-zone
  mantığına sahip, bu sistemin `IInteractable` arayüzünü kullanmıyor
  (bkz. Core Rules kararı)

**Not**: Görev/Taşıma Döngüsü ve Anı-Tetikleyici Etkileşim yazıldığında,
her biri kendi Dependencies bölümünde "Etkileşim Sistemi"ni listelemeli
(çift yönlü tutarlılık — bkz. `design/gdd/systems-index.md`). Ayrıca
Birinci Şahıs Kontrolcü'nün GDD'sinin Dependencies bölümü, bu GDD'nin
tanımladığı `InteractableRegistry`'ye referans vermek üzere güncellenmeli
(bkz. Core Rules'daki sahiplik notu).

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| SphereCast yarıçapı | 0.03–0.08 m | Küçük nesnelere nişan alma zorluğu, "eller zaten biliyor" fantazisi bozulur | Bitişik nesneler arasında yanlış-pozitif odaklanma | Core Rules: odaklanma tespiti |
| SphereCast menzili | 1.5–2.5 m | Rahatsız edici yakınlaşma gerektirir | Gerçekçi olmayan mesafeden etkileşim, Pillar 3'ü (Görev Gerçekliği) zayıflatır | Core Rules: odaklanma tespiti |
| HoldDuration (nesne başına) | 0.1–3.0 s | Instant'tan ayırt edilemez, amacı kaybolur | Akışı böler, "tereddütsüz eylem" hissini bozar | Formül: hold_progress |

## Visual/Audio Requirements

**Odaklanma geri bildirimi**: Idle→Focused geçişinde crosshair durumu
değişir (bkz. UI Requirements) — bu sistemin sahip olduğu tek görsel
sinyal. Nesnenin kendi vurgu/highlight görseli (varsa) kendi sistemine
(örn. Görev/Taşıma Döngüsü, Anı-Tetikleyici Etkileşim) aittir, bu GDD
tanımlamaz.

**Ses geri bildirimi**: Bu sistem kendi ses efektine sahip değil —
`OnInteract()`/`OnHoldComplete()` çağrıldığında hangi sesin çalacağına
nesnenin kendisi karar verir (örn. bir kapı kolu kendi "tık" sesini
çalar). Bu, "Eller Zaten Biliyor" fantazisiyle tutarlı: etkileşim
sisteminin kendisi görünmez kalmalı, sadece nesnenin tepkisi duyulur.

**Hold-progress görseli (design-review, 2026-08-04 — verification bulgusuyla
düzeltildi)**: Bu sistem artık crosshair'in bir parçası olarak **varsayılan
bir doldurma göstergesi** çizer (bkz. Core Rules, "Varsayılan Hold-doldurma
göstergesi") — hiçbir nesnenin bunu talep etmesi gerekmez. Bir nesne kendi
**ek** görselleştirmesini istiyorsa (örn. nesneye özel bir parlama), bunu
`OnHoldProgress(t)` üzerinden kendi sorumluluğunda ekler — bu, varsayılanın
yerine değil üzerine gelir.

## UI Requirements

Bu sistem, oyunun tek crosshair/etkileşim-ipucu UI'ını sahiplenir:
- **Crosshair**: Ekran merkezinde minimal bir nokta/daire. Idle durumunda
  nötr stil; Focused durumuna geçince (geçerli bir `IInteractable`
  bulunduğunda) belirgin ama küçük bir stil değişikliği (örn. hafif
  büyüme/parlama) — göze çarpan bir HUD elemanı değil, sessiz bir sinyal
  (Pillar 2: Sessiz Gerilim ile tutarlı, crosshair de "şok" yaratmamalı).
- **Prompt metni**: Focused durumunda, crosshair'ın hemen altında/yanında
  `IInteractable.PromptText` gösterilir (örn. "Al", "Çek", "Tut"). Holding
  sırasında prompt, `t` ilerlemesini yansıtacak şekilde güncellenebilir
  (örn. metin yerine/yanında bir dolum göstergesi) — **bu varsayılan
  dolum göstergesi artık bu sistemin kendi sorumluluğunda** (design-review,
  2026-08-04 — full re-verification bulgusu, düzeltildi: bu satır önceki
  bir taslaktan kalma, "*nesnenin* sorumluluğunda" diyen bir cümleyi
  taşıyordu — Core Rules'taki "Varsayılan Hold-doldurma göstergesi" kararı
  bu sorumluluğu bu sisteme taşıdı, bu bölüm hiç güncellenmemişti; bkz.
  Visual/Audio Requirements). Bir nesne kendi **ek** görselleştirmesini
  istiyorsa bunu kendi sorumluluğunda `OnHoldProgress(t)` üzerinden
  varsayılanın üzerine ekler (bkz. Core Rules) — bu bölüm sadece prompt
  metninin *var olduğu* alanı ve zamanlamasını tanımlar.
- **Erişilebilirlik**: Crosshair durum değişikliği sadece renge
  dayanmamalı (şekil/boyut değişikliği de içermeli) — renk körü
  oyuncular için.

Genel diyalog/altyazı sisteminden bağımsız, ayrı ve minimal bir UI
bileşeni olarak stilize edilmeli.

**Pillar 1 muafiyeti (CD-GDD-ALIGN notu)**: Crosshair her zaman doğru/
güvenilir bir sinyaldir (`CanInteract==true` her zaman görsel değişimle
eşleşir, hiç yanıltmaz) — bu, kasıtlı bir meta/UI-katmanı muafiyetidir.
Pillar 1 (Öznel Gerçeklik) diegetik dünyanın (ışık, ses, anlatı) hiçbir
zaman "objektif gerçeklik" sunmaması gerektiğini söyler; crosshair
diegetik değil, oyuncunun dünyayı okumasını sağlayan bir araçtır ve bu
yüzden istisnadır. Gelecekteki sistemler bunu "her geri bildirim
güvenilmez olmalı" şeklinde yanlış genellememelidir.

📌 **UX Flag — Etkileşim Sistemi**: Bu sistemin UI gereksinimleri var.
Pre-Production fazında, `/ux-design` çalıştırılarak crosshair/prompt için
bir UX spec (`design/ux/hud.md` ya da benzeri) yazılmalı — story'ler
doğrudan bu GDD'ye değil, o UX spec'e referans vermeli.

## Acceptance Criteria

1. **GIVEN** kameradan 2.0m menzil, 0.05m yarıçaplı SphereCast yolunda
   registry'ye kayıtlı bir `IInteractable` var, **WHEN** kare güncellenir,
   **THEN** Idle→Focused geçişi tetiklenir; menzil/yarıçap dışındaysa
   tetiklenmez.
2. **GIVEN** bir `IInteractable` enable/disable ediliyor, **WHEN**
   `OnEnable`/`OnDisable` çağrılır, **THEN** nesne registry'e eklenir/çıkarılır
   ve Birinci Şahıs Kontrolcü'nün yaklaşma-yavaşlaması mesafe kontrolü
   tutarlı kalır.
3. **GIVEN** Focused, hedef `Instant` tipinde, **WHEN** tuşa tek basılır,
   **THEN** `OnInteract()` tam bir kez çağrılır, Holding state'ine hiç
   girilmez.
4. **GIVEN** Focused, hedef `Hold`, `HoldDuration=2.0s`, **WHEN** tuş basılı
   tutulup t=0/1.0/2.0s'de örneklenir, **THEN** `OnHoldProgress(0)`→`(0.5)`→
   `OnHoldComplete()` sırayla bir kez çağrılır; `RequestMovementLock`
   girişte alınır, complete'te bırakılır; state Focused'a döner.
5. **GIVEN** `elapsed={-1,0,D/2,D,1.5D}`, **WHEN**
   `t=clamp(elapsed/D,0,1)` hesaplanır, **THEN** sonuç `{0,0,0.5,1,1}`,
   doğrusal artış.
6. **GIVEN** Holding, **WHEN** (a) `Destroy()` çağrılır, (b) kamera çevrilip
   SphereCast kaybeder ama nesne sahnede kalır, **THEN** her iki durumda
   hold iptal, lock serbest; (a) Idle'a, (b) Focused'a döner.
7. **GIVEN** `HoldDuration<=0`, **WHEN** tuşa basılır, **THEN** NaN/bölme
   hatası oluşmaz; `OnHoldComplete()` ilk karede anında tetiklenir.
8. **GIVEN** movement lock başka sistemde, **WHEN** Hold hedefine basılır,
   **THEN** Holding'e girilmez, `OnHoldBlocked()` çağrılır, diğer sahibin
   kilidi etkilenmez.
9. **GIVEN** Holding, aynı karede hem tuş bırakılır hem hedef kaybolur,
   **WHEN** kare işlenir, **THEN** sabit, deterministik bir sıra
   (hedef-kaybı önce) her koşuda aynı sonucu verir — **ERTELENDİ** (tam
   motor kare-sıralaması gerektirir, izole test değil).
10. **GIVEN** SphereCast birden fazla `IInteractable`'a çarpar, **WHEN**
    hedef seçilir, **THEN** en yakın mesafe kazanır; eşitlikte en küçük
    `InstanceID` seçilir.
11. **GIVEN** Holding, hedef disable→enable döngüsüne giriyor, **WHEN**
    döngü gerçekleşir, **THEN** hedef-kaybı sayılır, hold iptal edilir.
12. **GIVEN** tarama sırasında bir nesne registry'yi mutasyona uğratıyor,
    **WHEN** iterasyon sürüyor, **THEN** istisna/atlama oluşmaz (snapshot
    üzerinden iterasyon).
13. **GIVEN** Holding, SphereCast daha yakın başka bir hedefe çarpıyor,
    **WHEN** kare güncellenir, **THEN** hedef değişmez; auto-switch
    yalnızca Focused'da mümkündür.
14. **[BLOCKING, design-review 2026-08-04 — verification design-theory
    bulgusu, eklendi; önkoşul 2026-08-04 full re-verification bulgusuyla
    eklendi, AC14a ile çelişkiyi giderir]** **GIVEN** herhangi bir `Hold`
    tipi `IInteractable`, **`SuppressDefaultHoldFill == false`**
    döndürüyor, Focused'dan Holding'e girer (nesnenin kendi
    `OnHoldProgress(t)`'i hiçbir şey yapmasa bile), **WHEN** `t` 0'dan
    1'e ilerler, **THEN** crosshair'in varsayılan doldurma göstergesi bu
    sistemin kendi hesapladığı `t`'den doğrudan sürülür, görülebilir
    şekilde ilerler — nesnenin `OnHoldProgress`'i tüketip tüketmediğinden
    bağımsız. Bu, "her Hold etkileşiminde 0.6-1.5sn boyunca hiçbir geri
    bildirim olmaması" riskini kapatır (bkz. `gdd-cross-review-2026-08-04.md`).
    **Kapsam notu (design-review, 2026-08-04 — full re-verification
    bulgusu)**: MVP'nin tek Hold interactable'ı (Anı-Tetikleyici
    Etkileşim) `SuppressDefaultHoldFill=true` döndürür (bkz. AC14a) —
    yani bu AC şu an MVP içeriğinde doğrudan uygulanabilir bir nesneye
    sahip değil; testi otomatik bir mock `IInteractable` ile (gerçek
    sahne içeriği gerekmeden) doğrulanmalı. Evrensel varsayılanın
    kendisi hâlâ doğru bir sözleşmedir (gelecekteki Hold nesneleri için),
    sadece MVP'de fiilen tetiklenmez — bu, "0.6-1.5sn boyunca hiçbir
    geri bildirim yok" riskinin AC14a'nın kasıtlı istisnası dışında hâlâ
    kapatılmadığı anlamına gelir, bkz. AC14a ve `ani-tetikleyici-
    etkilesim.md`'nin kendi Visual/Audio Requirements'ı (kasıtlı sıfır
    geri bildirim, bir hata değil).
14a. **[BLOCKING, design-review 2026-08-04 — verification bulgusu, eklendi]**
    **GIVEN** bir `Hold` tipi `IInteractable`, `SuppressDefaultHoldFill=true`
    döndürüyor (ör. Anı-Tetikleyici Etkileşim — bkz. o GDD'nin Player
    Fantasy'si), **WHEN** Focused'dan Holding'e girilir, **THEN**
    crosshair'in doldurma göstergesi **hiç çizilmez** — AC14'ün
    varsayılanı bu nesne için tamamen bastırılır. Bu, "evrensel varsayılan"
    ile "kasıtlı sıfır-geri-bildirim tasarım kararı" ihtiyaçlarının
    çatışmadığını kanıtlar.

## Open Questions

1. **`InteractableRegistry` sahiplik/dosya konumu netleşmedi.**
   `systems-designer` bunu bir sahiplik boşluğu olarak işaretledi — registry
   bu GDD'de tanımlanıyor ama hem bu sistem hem Birinci Şahıs Kontrolcü
   tarafından okunuyor. Paylaşılan/Foundation script konumunda mı
   yaşamalı, yoksa bu sistemin kendi dosyasında mı? **Owner**:
   unity-specialist (mimari karar). **Hedef çözüm**: implementasyondan
   önce.
2. **SphereCast oklüzyon/engelleme kontrolü tanımlanmadı.** Mevcut Core
   Rules, cam gibi görsel-olarak-şeffaf-ama-fiziksel-engelleyici bir
   yüzeyin arkasındaki bir nesneye SphereCast'in çarpıp çarpmayacağını
   belirtmiyor — layer mask'in hangi katmanları içerdiği/hariç tuttuğu
   netleşmedi. **Owner**: unity-specialist. **Hedef çözüm**: dev-story
   implementasyonundan önce.
