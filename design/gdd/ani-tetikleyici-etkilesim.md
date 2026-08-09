# Anı-Tetikleyici Etkileşim (Memory-Trigger Interaction)

> **Status**: Needs Revision (bkz. `design/gdd/gdd-cross-review-2026-08-04-verification.md`)
> — 2026-08-04'ün ilk turu bu dosyada blocking bulgular bulmuştu:
> `TriggerMode` edit-time validasyonu, Etkileşim'in Hold Player
> Fantasy'siyle çelişki, ve birkaç bayat referans (OnClueKnown ×2,
> rejection-semantics argümanı). **İkisi de aynı gün çözüldü** (bkz. Core
> Rules — `TriggerMode` kontrolü artık iki adımlı asset+sahne taraması
> olarak somutlaştırıldı; Hold çelişkisi `SuppressDefaultHoldFill`
> mekanizmasıyla kapandı, bkz. Visual/Audio Requirements) — bu header
> 2026-08-04'ün ikinci (full re-verification) turuna kadar hiç
> güncellenmemişti, aynı sınıf header-staleness hatası (design-review,
> 2026-08-04 ikinci tur bulgusuyla düzeltildi). Status hâlâ `Needs
> Revision` olarak bırakıldı çünkü bu dosyada ayrıca iki bayat
> çapraz-referans daha bulunup düzeltildi (Dependencies ve Open
> Questions'taki `FiredTriggerIds`→`SettledTriggerIds` güncellemeleri,
> bkz. o bölümler) — tam bir sonraki `/review-all-gdds` turu temiz
> çıkana kadar Approved'a dönülmüyor, bu projenin kendi disiplini gereği.
> **Author**: user + agents
> **Last Updated**: 2026-08-04
> **Implements Pillar**: Pillar 1 (Öznel Gerçeklik)
> **Creative Director Review (CD-GDD-ALIGN)**: CONCERNS (revised) 2026-08-02
> **Design Review (2026-08-03)**: NEEDS REVISION → aynı oturumda revize
> edildi (Committed durumunun Gece/Oturum Durumu üzerinden kalıcılığı
> eklendi — sahne yeniden yükleme restore'u; edit-time validasyon
> mekanizması `IPreprocessBuildWithReport` olarak somutlaştırıldı;
> AC listesi temizlendi — AC6 kapsam notuna taşındı, AC7[eski] BLOCKING
> CI kontrolüne yeniden sınıflandırıldı, AC8[eski] için Blocked
> Acceptance Criteria tablosu eklendi; algılanabilirlik riski
> audio-paired spike'a somut bir kapanış tetikleyicisiyle bağlandı;
> stale Sahne Kesmeli Anlatı açık sorusu çözüldü olarak işaretlendi) →
> **APPROVED** (re-review yapılmadan, kullanıcı kararı) — bkz.
> `design/gdd/reviews/ani-tetikleyici-etkilesim-review-log.md`

## Overview

Anı-Tetikleyici Etkileşim, oyuncunun kendi eliyle başlattığı tek anı-kayması
noktasıdır: otelde belirli, sabit nesneler (ör. bir çekmece, bir fotoğraf
çerçevesi) Etkileşim Sistemi'nin `Hold` tipini uygular; oyuncu basılı-tutma
komple olduğunda, bu sistem Işık/Volume Durum Sistemi'nin `TriggerShift`'ini
çağırarak o nesnenin bağlı olduğu bölgeyi "gerçeklik"ten "anı"ya kaydırır.
Veri katmanı olarak bu, tek bir sorumluluk taşır: hangi nesnenin hangi
`shiftId`/`ShiftConfig`'e bağlı olduğunu tanımlamak ve `OnHoldProgress`
tamamlandığında doğru çağrıyı yapmak — kendi ses veya anlatı mantığını
yazmaz, bunlar zaten Işık/Volume'un `OnShiftStateChanged` event'ine bağımsız
olarak abone.

Oyuncu-yüzü etkisi ise oyunun kimlik çapasının ("Otel Senin Yerine
Hatırlıyor") **etkin, oyuncu-tetiklediği** versiyonudur. Işık/Volume Durum
Sistemi kendi başına pasif/çevresel bir mekanizmadır (geçişler bölgeye
girme/hysteresis ile tetiklenebilir); bu sistem ise oyuncunun *seçerek*
bir nesneyi tutmasının doğrudan sonucu olan kaymaları ekler — "otel
hatırlıyor" hissi, oyuncunun kendi eliyle bir anıyı sürdürmesiyle
keskinleşir. Bu sistem olmadan, oyunun tüm anı-kaymaları sadece çevresel
kalır — oyuncunun kendi eylemiyle bir şeyi "keşfetmesi" fantezisi
kaybolur.

## Player Fantasy

Işık/Volume'un pasif kaymaları otelin karakteri gafil avlamasıyken, bu
sistem tam tersidir: oyuncunun kendi eli **isteyerek** ister. Fantezi
keşfetmek değil — suçortaklığıdır. "Bir şey buldum" değil, "bunu ben,
bile bile yaptım ve artık geri alamam."

Tutma sırasında (0.6–1.5sn) hiçbir şey olmaz — sadece kendi tuttuğun
nefes ve bırakabileceğin ama bırakmadığının büyüyen tuhaflığı.
Bırakabilirdin. Bırakmadın — bu, Pillar 4'ün (**Bağ, Güvenlik Değil**)
başka bir yüzü: orada güvende olmayan bir *ilişkiydi*, burada güvende
olmayan kendi *seçimindir* — arkadaş sistemine değil, bu tek başına
yeterli, kendi kendine ürettiğin kırılganlığa işaret eder. Oyuncu bir
kapı açmıyor — anıya giriş **izni** veriyor. Kayma tamamlandığındaki an,
zafer ya da kilit-açma tatmini değil, tam salınmayan bir nefesin sessiz
durgunluğudur — artık içine bıraktığın şeyin içinde durman gerekiyor.

Merak "çekmecede ne var" diye sorar — bu çevresel/keşif estetik zaten
başka anlarda karşılanıyor. Bu sistemin ait olduğu an, merakın
bırakacağı noktayı elinin geçip tutmaya devam ettiğini fark ettiğin
anıdır — suçortaklığı, merak değil.

Bu bölüm için dil kasıtlı olarak "açığa çıkardım/kilit açtım/ödül
kazandım" gibi ifadelerden kaçınır (kardeş sistemlerin ses/UI
kararlarıyla tutarlı — "pickup chime" ve ödül-ping'i zaten reddedildi);
bunun yerine "izin verdim," "içeri bıraktım," "doğruladım" dili
kullanılır.

## Detailed Design

### Core Rules

- **Anı-tetikleyici tanımı**: `MemoryTriggerDef` (ScriptableObject,
  `CarryItemDef`'e benzer bir desen) — alanlar: `shiftId` (string),
  `shiftConfig` (ShiftConfig referansı, **her zaman** `Persistent=true`),
  `holdDurationOverride` (0.6–1.5sn, Etkileşim Sistemi'nin genel güvenli
  aralığı 0.1-3.0sn içinde bu sisteme özgü bir alt-aralık), `promptText`
  (ör. "Tut", ödül dili yok). Sahnede her fiziksel örnek bir
  `MemoryTriggerObject : IInteractable` bileşeni taşır, bu tanıma referans
  verir.
- **Tutma hızlı sindirilmez**: `OnHoldProgress(t)` bu sistem tarafından
  **hiçbir amaca kullanılmaz**, VE `SuppressDefaultHoldFill => true`
  döner (design-review, 2026-08-04 — verification bulgusuyla iki kez
  düzeltildi: Etkileşim Sistemi'nin yeni varsayılan crosshair doldurma
  göstergesi [bkz. `etkilesim-sistemi.md` Core Rules] önce bu satırı
  yanlışlıkla "artık geri bildirim var" diye güncellemişti, ama bu
  Player Fantasy'nin "tutma sırasında hiçbir şey olmaz, his oyuncunun
  içinde yaşar" argümanıyla doğrudan çelişiyordu — Etkileşim'in
  `SuppressDefaultHoldFill` devre dışı bırakma seçeneği tam olarak bu
  çatışmayı çözmek için eklendi. Bu sistem onu `true` döndürerek kullanır
  — crosshair göstergesi bu nesne için hiç çizilmez, gerçek bir sıfır-
  geri-bildirim garantisi sağlanır, Player Fantasy'nin kendi iddiası
  artık doğru). Bu sistem sadece **kendi üstüne**, nesneye özel bir yorum
  eklemekten kaçınır — registry'nin izin verdiği kendi easing'ini
  uygulama seçeneği kasıtlı olarak kullanılmaz, varsayılan doğrusal
  gösterge yeterlidir.
- **Tamamlanma**: `OnHoldComplete()` sadece Işık/Volume'un
  `TriggerShift(shiftId, shiftConfig)`'ini çağırır — ek bir guard
  gerekmez (`TriggerShift` zaten aktifse no-op döner). **Bu "guard
  gerekmez" garantisi koşulsuz değildir** — tamamen aşağıdaki maddenin
  kilitlediği `Persistent=true` değişmezine dayanır: `Persistent=true`
  olduğu sürece bu sistemin `shiftId`'leri hiçbir zaman Shifting-Out
  durumuna ulaşamaz (Işık/Volume'un kendi kuralı gereği Persistent
  shift'ler Shifting-Out'u atlar), bu yüzden "zaten aktif" her zaman
  no-op'a eşittir. Bu zincir tek bir bypass edilebilir edit-time
  kontrolüne (aşağıdaki `Persistent` maddesi) yaslanır — design-review,
  2026-08-02, systems-designer bulgusu: önceki hali bunu koşulsuz bir
  Core Rule olarak sunuyordu, gizli tek nokta bağımlılığını
  görünmez kılıyordu.
  Ardından nesne kalıcı olarak `CanInteract=false` olur ve `Hold`
  prompt'u bir daha hiç sunulmaz (**Committed**).
- **Committed durumu Gece/Oturum Durumu üzerinden kalıcıdır (design-review,
  2026-08-02 — unity-specialist bulgusu, eklendi)**: `Committed` durumu
  önceki taslakta sadece sahne-lokal bir `MonoBehaviour` alanı
  (`CanInteract=false`) olarak yaşıyordu — bu, sahne yeniden
  yüklendiğinde (ör. asansörle kat değişimi, bu oturum içinde en sıradan
  yol) nesnenin sessizce **Unfired**'a sıfırlanması ve tekrar
  tutulabilir hale gelmesi demekti, "artık geri alamam" garantisini en
  sık karşılaşılan yolda kırıyordu. Düzeltme: `MemoryTriggerObject`, kendi `OnEnable()`'ının **en
  başında** (`Register` çağrısından önce, aynı gövde içinde — ADR-0014,
  2026-08-08: önceki "`Awake()`" ifadesi ADR-0013'ün
  `scene_object_state_restore_timing` desenine sync edildi, QQ-07 —
  "Reload Scene: Off" ayarında `Awake` yeniden çalışmazken `OnEnable`
  çalışır) kendi `shiftId`'sinin Gece/Oturum Durumu'nun
  `FiredTriggerIds`'inde olup olmadığını sorgular; zaten varsa nesne
  **Unfired**'a hiç girmeden doğrudan **Committed** durumunda başlar
  (`CanInteract=false`, hiç prompt sunulmaz, `Register`'a hiç ulaşılmaz
  — nesne görünür kalır ama registry'ye hiç girmez). `OnHoldComplete()` çağrıldığında, `TriggerShift`
  çağrısıyla aynı adımda, bu sistem Gece/Oturum Durumu'nu `shiftId`'yi
  `FiredTriggerIds`'e eklemesi için bilgilendirir. Bu yeni bir davranış
  icat etmiyor — `gece-oturum-durumu-2026-08-02.md`'nin kendi Acceptance
  Criteria'sı zaten "GIVEN bir shiftId ateşlenmemiş, WHEN Anı-Tetikleyici
  Etkileşim onu ateşler, THEN FiredTriggerIds'e eklenir" diyor; bu
  GDD önceden bu yazma sorumluluğunu hiç adlandırmamıştı (bkz.
  Dependencies — Gece/Oturum Durumu artık bir bağımlılık olarak
  listeleniyor).
- **Fired bayrağı asla `MemoryTriggerDef` asset'ine yazılmaz (design-review,
  2026-08-02 — unity-specialist bulgusu, eklendi)**: `shiftId`
  `MemoryTriggerDef`'te yaşadığı için doğal ama yanlış bir kısayol, "ateş
  edildi" bilgisini doğrudan o ScriptableObject asset'ine yazmak olurdu
  — bu, aynı asset'i paylaşan tüm sahneler/editor session'ları arasında
  paylaşılan veriyi kalıcı olarak bozar (asset diskte kalır, oturuma özgü
  değildir). Fired/Committed durumu **sadece** Gece/Oturum Durumu'nun
  session-kapsamlı `FiredTriggerIds`'inde yaşar, hiçbir zaman asset'in
  kendisinde değil.
- **İptal**: `OnHoldCancelled()` (erken bırakma ya da hedef kaybı)
  tamamen no-op'tur — hiçbir şey olmamıştır, nesne **Unfired** kalır.
- **Persistent her zaman true, RevertShift asla çağrılmaz**: Her
  `MemoryTriggerDef.shiftConfig.Persistent` **zorunlu olarak** `true`
  olmalı — edit-time validasyon (`Persistent != true` ise hata) bunu
  kilitler. Bu sistem `RevertShift`'i hiçbir koşulda çağırmaz — oyuncu-
  tetiklediği bir anının kendiliğinden geri dönmesi, "artık geri
  alamam" fantazisiyle doğrudan çelişir.
- **Bağlı bölge her zaman `TriggerMode=ManualOnly` olmalı (design-review,
  2026-08-03 — `/review-all-gdds` bulgusu, eklendi, kritik bulgu)**: Her
  `MemoryTriggerDef`'in bağlı olduğu Işık/Volume tetikleyici bölgesi
  **zorunlu olarak** `TriggerMode=ManualOnly` olmalı (bkz.
  `isik-volume-durum-sistemi.md` Core Rules) — aynı `IPreprocessBuildWithReport`
  validasyonuna eklenen bir kontrol. Bu olmadan (önceki taslak), bölge
  varsayılan `Automatic` modda kalıyordu — oyuncu Etkileşim Sistemi'nin
  2.0m SphereCast menzilinden Hold'u başlatmadan **önce**, sadece
  Işık/Volume'un kendi `R_trigger` yarıçapına girerek bölgeyi otomatik
  olarak Shifting-In'e sokabiliyordu. Sonuç: stinger çalar, ipucu bilinir
  olur, ışık kayması tamamlanır — hepsi oyuncunun hiçbir eylemi olmadan;
  `OnHoldComplete()` daha sonra `TriggerShift`'i çağırdığında zaten-aktif
  no-op döner ama nesne yine de Committed'a geçer (aşağıdaki duplike-
  shiftId edge case'iyle aynı sessiz-başarı görünümü, ama edit-time
  validasyon bypass'ı olmadan, **her zaman**, normal oynanış yolunda).
  Bu, "bunu ben, bile bile yaptım" fantazisinin öncülünü tamamen ortadan
  kaldırırdı — `/review-all-gdds`'in (2026-08-03) hem consistency hem
  scenario-walkthrough geçişlerinin bağımsız olarak bulduğu bir bulgu
  (bkz. `design/gdd/gdd-cross-review-2026-08-03.md`).
- **Bağlı bölgenin `StingerAudioRadius`'u her zaman `> 0` olmalı
  (design-review, 2026-08-03 — verification N1 bulgusu, eklendi)**: Her
  `MemoryTriggerDef`'in bağlı olduğu Işık/Volume tetikleyici bölgesinin
  `ShiftConfig.StingerAudioRadius` alanı **zorunlu olarak** `> 0` bir
  değer taşımalı (bkz. `isik-volume-durum-sistemi.md` Core Rules ve
  Interactions — `GetStingerAudioRadius`) — aynı `IPreprocessBuildWithReport`
  validasyonuna eklenen üçüncü kontrol. Bu bölgelerin kendi `R_trigger`'ı
  yukarıdaki `TriggerMode=ManualOnly` kuralı ve `Persistent` zorunluluğu
  yüzünden zaten kullanılmaz durumda; `StingerAudioRadius` boş/sıfır
  kalırsa, Adaptif Ses'in `stinger_falloff` formülü (bkz.
  `adaptif-ses-sistemi.md` Formulas) sıfır/tanımsız bir yarıçaptan ses
  düşüşü türetmeye çalışır — sessizce anlamsız bir sonuç (duyulmaz ya da
  her yerde aynı seviyede stinger) yerine build-time'da yakalanır.
- **Edit-time validasyon mekanizması açıkça tanımlanır (design-review,
  2026-08-02 — unity-specialist bulgusu, netleştirildi; 2026-08-04'te
  asset-tarama/sahne-tarama ayrımıyla genişletildi, bkz. aşağıdaki not)**:
  Duplike `shiftId` kontrolü, `Persistent != true` kontrolü, VE
  `StingerAudioRadius <= 0` kontrolü — üçü de `ShiftConfig`/`MemoryTriggerDef`
  asset alanlarını okur — Unity'nin `IPreprocessBuildWithReport` arayüzü
  (`OnPreprocessBuild`) içinde, `AssetDatabase.FindAssets("t:MemoryTriggerDef")`
  ile tüm proje genelinde **asset taraması** yaparak çalışır —
  `BuildFailedException`/`report.SummarizeErrors` ile build gerçekten
  engellenir. **`OnValidate()` bu iş için kullanılamaz** (bir ScriptableObject
  kendi `OnValidate`'inde kardeş asset'leri göremez — duplike kontrolü
  yapısal olarak imkânsız hale gelir; bu, önceki taslakta mekanizma
  adlandırılmadığı için bir implementer'ın düşebileceği doğal bir
  yanlış-tahmindi). Editor içi hızlı geri bildirim için ek olarak bir
  `[MenuItem]` doğrulama komutu sağlanabilir (aynı taramayı çalıştırır,
  ama sadece build zamanı `IPreprocessBuildWithReport` build'i gerçekten
  engeller).
  **`TriggerMode` kontrolü ayrı bir mekanizma gerektirir (design-review,
  2026-08-04 — verification bulgusu, kritik bulgu, kullanıcı kararıyla
  çözüldü)**: Yukarıdaki üç kontrolün aksine, `TriggerMode`
  `MemoryTriggerDef`/`ShiftConfig` asset'inde yaşamaz — Işık/Volume'un
  **sahneye yerleştirilmiş bölge bileşeninin** kendi alanıdır (bkz.
  `isik-volume-durum-sistemi.md` Core Rules). Bunun **zorunlu** bir zone
  alanı olmasının nedeni yapısal: bir `Automatic` bölge, `TriggerShift`
  hiç çağrılmadan **önce**, `Dormant`'tayken kendi proximity tick'inde
  bu kararı vermelidir — bu yüzden `TriggerMode`'u `ShiftConfig`'e taşımak
  (asset-taramasını basitleştirecek en bariz çözüm) çalışmaz: bir
  `ShiftConfig`, zone'a sadece `TriggerShift` çağrıldığında ulaşır, ama
  `Automatic` zone'ların **tam olarak bunu hiç yapmayan** `ManualOnly`
  zone'lardan ayırt edilmesi gereken an bundan önce gelir. **Düzeltme**:
  `MemoryTriggerDef`'e bir zone-referansı eklemek yerine (ScriptableObject
  asset'lerinden sahne nesnelerine doğrudan referans Unity'de kırılgan
  bir anti-pattern'dir — sahne yeniden adlandırmalarında/taşımalarında
  sessizce kopar), her Işık/Volume bölgesinin **zaten taşıması gereken**
  kendi `shiftId` alanı (bkz. `isik-volume-durum-sistemi.md` — `TriggerShift(shiftId,
  config)`'in hedeflediği kimlik) **string eşleştirme anahtarı** olarak
  kullanılır — `MemoryTriggerDef.shiftId` ile birebir aynı sözleşme,
  zaten var. Validasyon iki adıma ayrılır: (1) yukarıdaki asset-taraması,
  (2) **ayrı bir sahne-taraması** — `EditorBuildSettings.scenes`'teki her
  sahne açılır (ya da `AssetDatabase`'in sahne-içi bileşen sorgulama
  API'leri kullanılır), tüm Işık/Volume bölge bileşenleri toplanır, her
  birinin `shiftId`'si `MemoryTriggerDef`'lerinkiyle eşleştirilir, eşleşen
  her çift için `TriggerMode == ManualOnly` doğrulanır. Bu, ilk kontroldeki
  üçten daha pahalı bir taramadır (sahneleri açmayı gerektirir) — build-time
  `IPreprocessBuildWithReport` içinde hâlâ çalışır, sadece asset-taramasından
  ayrı bir adımdır.
  **Paylaşılan araç notu**: `anlati-durum-ipucu-takibi.md`'nin kendi
  `clueId` duplikasyon kontrolü aynı mekanizma boşluğunu taşıyor — bu
  iki kontrol aynı `IPreprocessBuildWithReport` implementasyonunu
  paylaşan tek bir editor utility olarak yazılmalı, iki kez elle
  yazılmamalı.

### States and Transitions

| Durum | Giriş | Çıkış |
|---|---|---|
| **Unfired** | Spawn/yükleme (Gece/Oturum Durumu'nun `FiredTriggerIds`'inde bu `shiftId` YOKSA); `CanInteract=true`; Etkileşim'in Idle→Focused→Holding döngüsü altında normal çalışır | `OnHoldCancelled` → Unfired'da kalır; `OnHoldComplete` → **Committed** |
| **Committed** | `TriggerShift` bir kez çağrıldı **VEYA** `OnEnable()` başında (`Register`'dan önce — ADR-0014, 2026-08-08: `Awake()`'ten sync edildi, bkz. Core Rules) Gece/Oturum Durumu'nun `FiredTriggerIds`'i bu `shiftId`'yi zaten içeriyor (sahne yeniden yükleme restore'u — design-review, 2026-08-02); `CanInteract=false`; artık hiçbir prompt/focus mümkün değil (ADR-0010'un Focused-dal `CanInteract` yeniden-sorgusu, ADR-0014 revizyonu, bunu commit'ten bir kare sonra da garanti eder) | Terminal — oturumun geri kalanı boyunca kalıcı, sahne yeniden yüklemelerinde dahil |

### Interactions with Other Systems

- **Etkileşim Sistemi**: `IInteractable.Hold`'u uygular; `OnHoldProgress`/
  `OnHoldComplete`/`OnHoldCancelled` sözleşmesini kullanır; `HoldDuration`
  0.6–1.5sn alt-aralığından içerik olarak atanır (Etkileşim'in kendi
  tuning knob'unu yeniden tanımlamaz, sadece bir alt-aralık seçer).
- **Işık/Volume Durum Sistemi**: `TriggerShift(shiftId, shiftConfig)`'i
  çağırır; `RevertShift` hiç çağırılmaz; `shiftConfig.Persistent`
  her zaman `true`.
- **Gece/Oturum Durumu (design-review, 2026-08-02 — eklendi; ADR-0014,
  2026-08-08 — restore zamanlaması `OnEnable()` başına sync edildi)**:
  `OnEnable()` başında `FiredTriggerIds`'i sorgular (Committed-restore
  için, `Register`'dan önce); `OnHoldComplete()`'te
  `GeceOturumDurumu.InternalInstance.AddFiredTrigger(shiftId)` ile
  `FiredTriggerIds`'e kendi `shiftId`'sini ekler. Bkz. Core Rules ve
  Dependencies.
- **Adaptif Ses Sistemi**: Doğrudan çağrı YOK — stinger, Işık/Volume'un
  `OnShiftStateChanged`'ine zaten bağımsız abone (design-review,
  2026-08-03 — stinger/ışık zamanlama düzeltmesiyle güncellendi:
  `Shifting-In`'de erken çalar, artık `Held`'i beklemez — bkz.
  `adaptif-ses-sistemi.md` Core Rules).
- **Anlatı Durum/İpucu Takibi**: Doğrudan çağrı YOK — ipucu açığa çıkarma
  aynı event üzerinden zaten bağımsız tetiklenir.

## Formulas

**N/A** — bu sistem saf event-orkestrasyonudur, türetilmiş bir hesaplama
yok. `TriggerShift` çağrısı bir eşik kontrolü değil, tek seferlik bir
tetiklemedir (`OnHoldComplete`). Işık/Volume'un kendi geçiş eğrisi
(`shift_progress`, smoothstep) zaten o sistemin sahipliğinde — bu GDD onu
tekrarlamaz, sadece tetikler. `OnHoldProgress(t)`'in kendi easing'ini
uygulama seçeneği (registry notu) kasıtlı olarak kullanılmadı (Core
Rules), yani burada yeni bir formula yok.

## Edge Cases

- **Eğer iki farklı `MemoryTriggerDef` yanlışlıkla aynı `shiftId`'yi
  taşırsa**: **Edit-time validasyon** (tüm `MemoryTriggerDef` asset'leri
  taranarak düplike `shiftId` kontrolü) zorunlu kılınmalı — hata verip
  build'i engellemeli. Kaçarsa: ilk tetiklenen nesne `TriggerShift`'i
  başarıyla ateşler; ikinci nesne sonradan tutulursa `TriggerShift`
  zaten-aktif olduğu için sessizce no-op döner (`OnShiftStateChanged`
  tekrar ateşlenmez) — ama bu sistem dönüş değerini kontrol etmediği
  için ikinci nesne yine de **Committed**'a geçer. Sonuç: oyuncu
  "başarılı" bir tutma deneyimler ama dünyada hiçbir şey değişmez —
  sessiz ama crash'siz; edit-time kontrol birincil savunmadır.
- **Eğer bir `MemoryTriggerDef`, `Persistent=false` ile yapılandırılırsa**
  (Core Rules'ı ihlal eder): Edit-time validasyon bunu engellemeli. Kaçarsa:
  bu sistem `RevertShift`'i hiç çağırmasa da, Işık/Volume'un kendi
  yarıçap/hysteresis-çıkış mantığı devreye girer ve oyuncu bölgeden
  ayrılınca kayma otomatik geri döner — "artık geri alamam" garantisi
  sessizce kırılır. Bu durumda **runtime'da bir yedek savunma yoktur**
  (geri dönüş Işık/Volume'un içinde olur, bu sistemin kontrolü dışında)
  — edit-time validasyon burada **tek** savunma hattıdır, asla atlanmamalı.
  **Anlatı Durum çapraz-kontrolü (design-review, 2026-08-02 — systems-designer
  bulgusu, eklendi)**: Bu geri-dönüş sonrası bölgeye tekrar girilirse
  (`R_trigger`'a tekrar girmek), `TriggerShift` yeniden çağrılır ve
  Işık/Volume tekrar `OnShiftStateChanged(Held)` fırlatır — bu, Anlatı
  Durum/İpucu Takibi'nin `SeenShiftIds`'ine zaten idempotent bir ekleme
  olduğundan (bkz. `anlati-durum-ipucu-takibi.md` Edge Cases,
  Persistent-restore notu) kendi başına zararsızdır; `MarkClueKnown` da
  idempotent no-op'tur, bu yüzden ilgili ipucu **yanlışlıkla ikinci kez
  "Known" olmaz**. Ama bu, Işık/Volume'un kendi "event tam olarak bir kez
  fırlar" sözleşmesinin (AC15, `isik-volume-durum-sistemi.md`) bu özel
  durumda (Persistent=false bypass'ı) fiilen ihlal edildiği anlamına
  gelir — bu ihlal Anlatı Durum tarafında zararsız absorbe edilir, ama
  bu sadece iki sistemin idempotency tasarımının bir tesadüfüdür, bu
  GDD'nin garanti ettiği bir şey değildir. Edit-time validasyon yine de
  **tek** gerçek savunma hattı olmaya devam eder.
- **Eğer iki bypass aynı anda gerçekleşirse — duplike `shiftId` VE
  `Persistent=false` (design-review, 2026-08-02 — systems-designer
  bulgusu, eklendi)**: Yukarıdaki iki edge case birbirinden bağımsız ele
  alınıyordu; kesişimleri ayrı bir durum yaratır. Eğer ilk nesnenin
  bölgesi (Persistent=false yüzünden) ikinci nesne tutulmadan önce
  Shifting-Out'a geri dönmüşse, ikinci nesnenin `TriggerShift` çağrısı
  Işık/Volume'un Shifting-Out-sırasında-tekrar-tetiklenme kuralına
  girer (bkz. `isik-volume-durum-sistemi.md` Edge Cases) — bu durumda
  **no-op DEĞİL**, yön tersine çevrilip Shifting-In'e yeniden başlar,
  `true` döner ve `OnShiftStateChanged` tekrar fırlar (stinger yeniden
  çalar, ışık yeniden kayar). Bu, yukarıdaki ilk edge case'in "sessiz,
  hiçbir şey değişmez" varsayımıyla doğrudan çelişir. Düşük olasılıklı
  (iki bağımsız edit-time validasyonun aynı anda atlanmasını gerektirir)
  — yeni bir runtime savunması eklenmez, ama bu belgelenmiş bir bileşik
  başarısızlık durumudur, "tek bypass her zaman güvenli" varsayılmamalı.
- **Tek eşzamanlı Hold kuralı nedeniyle soft-lock riski**: **Yapı gereği
  imkânsız** — **düzeltme (design-review, 2026-08-04 — verification
  bulgusu: bu madde `RequestMovementLock`'un bir isteği reddedebildiğini
  varsayıyordu, ama kilit artık referans-sayaçlı ve hiçbir çağrıyı asla
  reddetmiyor — bkz. `birinci-sahis-kontrolcu.md`/`etkilesim-sistemi.md`
  Core Rules)**: gerçek güvenlik `IsLocked` ön-kontrolü + `OnHoldBlocked()`
  üzerinden sağlanır — bir Hold zaten kilitliyken hiç başlamaz
  (`RequestMovementLock`'a hiç ulaşılmaz). Sonuç aynı (soft-lock imkânsız)
  ama mekanizma bu. Başka bir Hold sınırlı sürede (0.1–3.0sn) tamamlanır
  ya da iptal olur, kilit her koşulda serbest kalır — sonsuz-hold yolu
  Etkileşim'in sözleşmesinde yok (Görev/Taşıma Döngüsü'nün kanıtladığı
  desenle aynı).
- **Eğer `MemoryTriggerObject`, Unfired durumdayken (Hold tamamlanmadan)
  destroy/disable olursa**: Etkileşim'in kendi Edge Case'i uygulanır (hold
  iptal, `OnHoldComplete` çağrılmaz) — `TriggerShift` hiç tetiklenmemiş
  olur, o `shiftId` bu oturumda — başka bir nesne aynı `shiftId`'yi
  taşımıyorsa — potansiyel olarak erişilemez kalır. Bu, ilgili Anlatı
  Durum ipucusu için bir içerik-yazarlığı riskidir (level design'a not).
- **Eğer `MemoryTriggerObject`, Committed durumdayken destroy/disable
  olursa**: Etkisiz — kayma zaten kalıcı olarak ateşlenmiş, Işık/Volume
  durumu nesnenin ömrüne bağlı değil.
- **Eğer daha önce Committed olmuş bir `MemoryTriggerObject`'in bulunduğu
  sahne oturum içinde yeniden yüklenirse (design-review, 2026-08-02 —
  unity-specialist bulgusu, eklendi)**: Bu, önceden bu GDD'de hiç
  ele alınmayan ama en sıradan oynanış yoluydu (ör. asansörle kat
  değişimi, aynı kata dönüş). Nesne yeni bir `MonoBehaviour` instance'ı
  olarak yeniden etkinleşir ve `OnEnable()`'ının başındaki (ADR-0014,
  2026-08-09 senkron — önceki "`Awake()`" ifadesi, Core Rules'taki
  restore-zamanlaması düzeltmesiyle tutarlı hale getirildi) Gece/Oturum
  Durumu sorgusu sayesinde `FiredTriggerIds`'te kendi `shiftId`'si bulunduğundan
  doğrudan **Committed**'a başlar (`CanInteract=false`, `Unfired`'a hiç
  girmez, prompt hiç sunulmaz). Bu kontrol olmadan (önceki taslak),
  nesne sessizce **Unfired**'a sıfırlanır ve tekrar tutulabilir hale
  gelirdi — "artık geri alamam" garantisinin en sık karşılaşılan yolda
  sessizce kırılması.
- **Çoklu eşzamanlı görünür Shifted bölge**: Bu GDD'ye yeni bir edge case
  gerekmiyor — bu sistem salt tek-seferlik tetikleyicidir, zamanlama/
  sıralama kontrolü yapmaz; konu tamamen Işık/Volume'un kendi Open
  Questions'ında (level-design sightline) sahiplenilmiş, sadece çapraz-
  referans yeterli.

> **Açık boşluk (Open Questions'a taşındı)**: Oturum duraklarken/biterken
> (`IsSessionActive→false`) Holding'de olan bir oyuncu için Etkileşim
> Sistemi'nin kendi sözleşmesi bir iptal tetikleyicisi tanımlamıyor
> (sadece buton-bırakma/hedef-kaybı iptal eder). Bu, bu GDD'nin
> çözebileceği bir boşluk değil — Etkileşim Sistemi'nin kendi
> sözleşmesindeki bir eksiklik. Bu sistem `IsSessionActive`'i okumaz (yapı
> gereği gerekmiyor), ama üst-sistem davranışı netleşmeden garanti
> verilemez.

## Dependencies

**Bağımlıdır** (hard, doğrudan API çağrısı):
- **Etkileşim Sistemi** — `IInteractable.Hold`'u uygular, `OnHoldProgress`/
  `OnHoldComplete`/`OnHoldCancelled` sözleşmesini kullanır
- **Işık/Volume Durum Sistemi** — `TriggerShift(shiftId, shiftConfig)`'i
  çağırır (`RevertShift` hiç çağrılmaz)
- **Gece/Oturum Durumu** (design-review, 2026-08-02 — eklendi; kısmi,
  Işık/Volume'un bu sistemle olan kısmi bağımlılığıyla aynı desende) —
  `OnEnable()`'ının başında (Register'dan önce — ADR-0014, 2026-08-09
  senkron: önceki "`Awake()`'te" ifadesi restore-zamanlaması
  düzeltmesiyle güncellendi) `FiredTriggerIds`'i Committed-restore için
  sorgular; `OnHoldComplete()`'te kendi `shiftId`'sini `FiredTriggerIds`'e
  eklettirir. Bu, yeni bir sözleşme icat etmiyor —
  `gece-oturum-durumu-2026-08-02.md`'nin kendi Acceptance Criteria'sı bu
  yazma sorumluluğunu zaten bu sisteme atfediyordu (bkz. Core Rules,
  "Committed durumu Gece/Oturum Durumu üzerinden kalıcıdır"), sadece bu
  GDD'de hiç adlandırılmamıştı.

**Dolaylı/decoupled bağımlılık** (doğrudan çağrı YOK, ama bu sistemin
ürettiği event'e tepki verirler):
- **Adaptif Ses Sistemi** — `OnShiftStateChanged`'e zaten bağımsız
  abone (stinger, `Held` VE `Shifting-In` işlenir — design-review
  2026-08-03 stinger/ışık zamanlama düzeltmesiyle güncellendi)
- **Anlatı Durum/İpucu Takibi** — aynı event'e zaten bağımsız abone
  (ipucu açığa çıkarma)

**Not — systems-index.md düzeltmesi uygulandı (design-review, 2026-08-02)**:
`systems-index.md`'nin Systems Enumeration tablosu (satır 11) ve
Dependency Map bölümü, bu GDD'nin direkt-vs-decoupled ayrımını (ve
Gece/Oturum Durumu'nun yeni kısmi bağımlılığını) artık yansıtacak
şekilde güncellendi.

**Kendisine bağımlı olanlar**:
- **Sahne Kesmeli Anlatı** *(design-review, 2026-08-02 — çözüldü, bu
  GDD'nin daha önceki hali burada artık stale bir açık soru
  taşıyordu)*: `sahne-kesmeli-anlati-2026-08-02.md` (aynı gün
  tamamlandı) bu sisteme **doğrudan bağımlı değil** — kendi "anı-
  tetikleyici doygunluğu" bitiş sinyalini **Gece/Oturum Durumu'nun
  `SettledTriggerIds.Count`** toplamı üzerinden okuyor (design-review,
  2026-08-04 ikinci tur full re-verification bulgusuyla düzeltildi: bu
  satır hâlâ 2026-08-04'ün ilk turunda geçerli olan `FiredTriggerIds.Count`
  sinyalini söylüyordu — aynı gün ilerleyen saatlerde saturation-timing
  düzeltmesiyle `SettledTriggerIds.Count`'a taşındı, ama bu satır o
  değişikliği hiç yakalamamıştı; bkz. `sahne-kesmeli-anlati-2026-08-02.md`
  ve `gece-oturum-durumu-2026-08-02.md` Core Rules), bu sistemin
  `OnShiftStateChanged`'ine ya da yeni bir sinyaline hiç ihtiyaç duymadan
  (bu sistemin kendi `OnHoldComplete()`'i zaten `FiredTriggerIds`'i
  dolduruyor, bkz. Dependencies yukarıda). Açık soru bu bağımlı için
  kapandı — bkz. Open Questions.
- **Hibrit Tepkisellik** (Vertical Slice, henüz tasarlanmadı) — aynı
  arayüz belirsizliği burada hâlâ geçerli, tek kalan açık soru.

## Tuning Knobs

| Knob | Güvenli Aralık | Çok Düşük | Çok Yüksek | Etkileşimde Olduğu |
|---|---|---|---|---|
| HoldDuration (anı-tetikleyici alt-aralığı) | 0.6–1.5sn (Etkileşim'in genel 0.1–3.0sn aralığı içinde) | Etkileşim'in `Instant` tipinden ayırt edilemez, "bırakabilirdin ama bırakmadın" gerilimi oluşmaz | Yıpratıcı hale gelir, tekli bir anı tetiklemesi için gereksiz uzun | Core Rules: MemoryTriggerDef.holdDurationOverride |

Başka bir yeni tunable değer yok — `shiftConfig` (renk/süre/Persistent)
Işık/Volume'un kendi sahiplik alanı, bu GDD kendi knob'unu
tanımlamaz/tekrarlamaz, sadece her `shiftId` için içerik olarak bir
değer seçer.

## Visual/Audio Requirements

**Tutma sırasında gerçek sıfır geri bildirim (design-review, 2026-08-04 —
verification bulgusuyla mekanizma netleştirildi)** — Core Rules'daki
"tutma sırasında hiçbir şey olmaz" bir yer tutucu değil, fantazinin
çalışma mekanizmasıdır. "Sadece kendi tuttuğun nefes" satırı, hissin
oyuncunun **içinde** yaşadığını, oyun tarafından aracılanmadığını açıkça
belirtiyor. En küçük bir titreme/desatürasyon/nefes ipucu bile eklense,
bu his oyuncunun bedeninden oyunun geri bildirim kanalına taşınır — bu
da zaten reddedilen "kilit açma" çerçevesinin yumuşatılmış bir versiyonu
olur. Bu artık sadece bir niyet değil, somut bir mekanizmayla korunuyor:
Etkileşim Sistemi'nin tüm Hold etkileşimleri için çizdiği **varsayılan**
crosshair doldurma göstergesi (bkz. `etkilesim-sistemi.md` Core Rules) bu
sistem için `SuppressDefaultHoldFill => true` ile açıkça **bastırılır** —
`OnHoldProgress(t)` ile ölçeklenen bu sisteme özgü HiçBiR VFX/ses zaten
yok, VE artık başka hiçbir kaynaktan (Etkileşim'in varsayılanı dahil) da
görsel bir gösterge gelmez. Bu, "her Hold'un bir varsayılan göstergesi
olmalı" (bkz. `gdd-cross-review-2026-08-04.md`) genel kuralına karşı
**kasıtlı, mekanizmalı bir istisnadır** — geçici bir tutarsızlık değil.

> **CD-GDD-ALIGN notu (2026-08-04'te güncellendi — önceki hali "girdi
> kaydı zaten genel Hold-doldurma UI'ı ile [geri bildirim üretir]"
> diyordu, bu artık yanlış: bu sistem o UI'ı açıkça bastırıyor)**:
> "Sıfır geri bildirim" ifadesi tam olarak budur — girdi kaydının kendisi
> **görünmez** kalır (`SuppressDefaultHoldFill`), tamamlanma ise Işık/Volume'un
> geçişi + Adaptif
> Ses'in stinger'ı ile (ikisi de `OnShiftStateChanged` üzerinden) gerçek
> bir geri bildirim üretir. Bu modelin işlemesi, tamamen o kardeş
> sistemlerin geri bildiriminin algılanabilir olmasına bağımlıdır —
> konsept prototipinin bulgusu (`game-concept.md` Open Questions:
> "ışık/renk geçiş tekniği... tek başına yetersiz; ses gerekiyor",
> bkz. `prototypes/yankilar-lighting-concept/REPORT.md`) burada doğrudan
> geçerlidir. Işık+ses bileşik etkisi bozulursa (ör. ses gecikirse/
> çalmazsa), bu sistemin tamamlanma anı oyuncuya gerçekten sessiz değil,
> **kırık** hissettirir.

> **Risk kapanışı (design-review, 2026-08-02 — game-designer bulgusu,
> creative-director kararı)**: Bu risk önceden belgeleniyordu ama
> kapatılmamıştı — onu doğrulayacak tek AC (aşağıdaki Acceptance
> Criteria #7) tam motor entegrasyonuna kadar ERTELENDİ'ydi, sahibi ya
> da kapanış tetikleyicisi yoktu. İki seçenek değerlendirildi: (a) yeni
> bir sahiplenilmiş fallback ipucusu eklemek, (b) bu AC'yi somut bir
> kapanış tetikleyicisine bağlamak. (a) **reddedildi** — bu bölümün
> kendi "Ek görsel ipucu yok" ilkesiyle ve Player Fantasy'nin "en küçük
> bir titreme/desatürasyon/nefes ipucu bile eklense... reddedilen
> 'kilit açma' çerçevesinin yumuşatılmış bir versiyonu olur" kararıyla
> doğrudan çelişirdi. (b) seçildi: AC#7 artık `systems-index.md`'nin
> zaten planladığı audio-paired follow-up spike'a (`/prototype --spike`)
> bağlı somut bir Blocked Acceptance Criteria girişi taşıyor (bkz.
> Acceptance Criteria, Blocked Acceptance Criteria tablosu) — risk artık
> "faith ile gönderilmiyor," spike tamamlanmadan implementasyon bu
> riski kapatılmış sayamaz.

**Committed durumu için özel bir görsel işaret yok**: `Persistent=true`
zaten ortamın kalıcı kanıtı taşıması demektir. Nesneye geri dönüldüğünde,
etkileşim prompt'unun **yokluğu** (highlight yok, focus yok) tek işarettir
— sessizlik onaydır. (Bir level tasarımcısı isteğe bağlı olarak fiziksel
bir iz — ör. açık bırakılmış bir çekmece — ekleyebilir, ama bu çevresel
anlatı kararıdır, bu GDD'nin sistemik bir gereksinimi değil.)

**Dokunulmadan önceki görünüm**: Tamamen çevreye karışır — şeyler
üzerinde özel bir highlight/glow yok. Keşfedilebilirlik tamamen
Etkileşim Sistemi'nin zaten sağladığı genel odaklanma/prompt
davranışına bırakılır — nesne oyuncunun kendi dikkati onu bulana kadar
sıradan dünya döşemesi gibi görünür.

## UI Requirements

**Yeni UI yok, ama varsayılan dolum göstergesi kasıtlı olarak bastırılır
(design-review, 2026-08-04 — full re-verification bulgusuyla düzeltildi)**:
Bu sistem, Etkileşim Sistemi'nin zaten sağladığı genel crosshair/prompt
UI'ını olduğu gibi kullanır — **ama Etkileşim'in varsayılan Hold-doldurma
göstergesini değil**. Önceki taslak burada "UI'ını olduğu gibi kullanır...
bu artık gerçek bir sözleşme" diyordu; bu, aynı dosyanın Core Rules ve
Visual/Audio Requirements bölümlerinin kararıyla (`SuppressDefaultHoldFill
=> true`, crosshair'in doldurma göstergesi bu nesne için hiç çizilmez)
doğrudan çelişiyordu — düzeltme sırasında sadece crosshair/prompt'un genel
UI'ı doğrulandı, dolum göstergesinin bu sistem için özel olarak
**bastırıldığı** gözden kaçmıştı. Doğru hâli: crosshair ve prompt metni
Etkileşim'in genel bileşeninden aynen kullanılır (yeni bir UI yüzeyi
değil); Hold-doldurma göstergesi ise `SuppressDefaultHoldFill` aracılığıyla
bu nesne için açıkça devre dışı bırakılır (bkz. Core Rules).
`PromptText` (ör. "Tut") ödül dili taşımayan kısa bir metin olacak şekilde
içerik olarak yazılır.

## Acceptance Criteria

> **Kapsam notu (design-review, 2026-08-02 — önceki AC6'dan taşındı,
> qa-lead bulgusu)**: Tek-eşzamanlı-Hold soft-lock riski bu GDD için
> testable bir kriter değildir, kapsam dışıdır — doğruluk Etkileşim
> Sistemi'nin kendi sözleşmesinde test edilmeli, bu sistem sadece ona
> güvenir. Önceki taslakta bu, numaralı Acceptance Criteria listesinde
> kendini test edilemez ilan eden bir madde olarak yer alıyordu; bu,
> listenin "her madde QA tarafından pas/fail doğrulanabilir olmalı"
> sözleşmesini sulandırıyordu — buraya, listenin dışına taşındı.

1. **GIVEN** bir `MemoryTriggerObject` Unfired durumda (`CanInteract=true`),
   **WHEN** oyuncu Hold'u `holdDurationOverride` süresince tamamlar
   (`OnHoldComplete` tetiklenir), **THEN** Işık/Volume'un
   `TriggerShift(shiftId, shiftConfig)`'i, o nesnenin kendi
   `MemoryTriggerDef`'inden gelen doğru `shiftId`/`shiftConfig` ile tam
   bir kez çağrılır; nesne **Committed**'a geçer, `CanInteract` kalıcı
   olarak `false` olur, bir daha hiç prompt/focus sunulmaz.
2. **GIVEN** aynı nesne Unfired, **WHEN** erken bırakma/hedef kaybı ile
   `OnHoldCancelled` tetiklenir, **THEN** `TriggerShift` hiç çağrılmaz,
   nesne Unfired kalır, `CanInteract=true` kalır, oyuncu sınırsız
   yeniden deneyebilir.
3. **GIVEN** iki farklı `MemoryTriggerDef` aynı `shiftId`'yi taşıyor,
   **WHEN** `IPreprocessBuildWithReport` implementasyonu (bkz. Core
   Rules — Edit-time validasyon mekanizması) `AssetDatabase.FindAssets
   ("t:MemoryTriggerDef")` ile taranarak build zamanında çalışır,
   **THEN** `BuildFailedException`/`report.SummarizeErrors` ile hata
   verilir, build engellenir, hata mesajı çakışan iki asset'in yolunu
   (asset path) işaret eder — bir `tests/editor/` EditMode testiyle
   doğrulanır. *(design-review, 2026-08-02 — unity-specialist bulgusu:
   önceki hali "edit-time validasyon çalışır" diyordu, hangi mekanizma
   ya da nerede test edildiği belirtilmiyordu.)*
4. **GIVEN** bir `MemoryTriggerDef.shiftConfig.Persistent=false`, **WHEN**
   aynı `IPreprocessBuildWithReport` taraması çalışır, **THEN** hata
   verilir, build engellenir, hata mesajı ilgili asset'in yolunu işaret
   eder — bir `tests/editor/` EditMode testiyle doğrulanır.
4a. **[design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi,
   kritik bulgu; mekanizma 2026-08-04 verification bulgusuyla düzeltildi]**
   **GIVEN** bir `MemoryTriggerDef`'in `shiftId`'siyle eşleşen `shiftId`'ye
   sahip bir Işık/Volume bölgesi `TriggerMode=Automatic` olarak
   yapılandırılmış (varsayılan, `ManualOnly` değil), **WHEN** aynı
   `IPreprocessBuildWithReport`'un **sahne-taraması adımı** çalışır (bkz.
   Core Rules — bu, `Persistent`/`StingerAudioRadius` kontrollerinin
   kullandığı asset-taramasından **ayrı** bir adımdır, çünkü `TriggerMode`
   bir asset alanı değil, sahne-içi bölge bileşeninin alanıdır), **THEN**
   hata verilir, build engellenir, hata mesajı ilgili bölgenin
   bulunduğu sahnenin ve bölge nesnesinin yolunu işaret eder. Kaçarsa:
   oyuncu sadece `R_trigger` içine girerek (Hold'u hiç başlatmadan)
   bölgeyi otomatik tetikleyebilir — bu, edit-time validasyonun
   engellemesi gereken, "bile bile yaptım" fantazisini kıran
   normal-oynanış-yolu senaryosudur (bkz. Core Rules).
4b. **[design-review, 2026-08-03 — verification N1 bulgusu, eklendi]**
   **GIVEN** bir `MemoryTriggerDef`'in bağlı olduğu Işık/Volume bölgesi
   `ShiftConfig.StingerAudioRadius <= 0` (ya da hiç ayarlanmamış) ile
   yapılandırılmış, **WHEN** aynı `IPreprocessBuildWithReport` taraması
   çalışır, **THEN** hata verilir, build engellenir, hata mesajı ilgili
   bölgenin yolunu işaret eder. Kaçarsa: Adaptif Ses'in `stinger_falloff`
   formülü (bkz. `adaptif-ses-sistemi.md` Formulas) sıfır/tanımsız bir
   yarıçaptan `minDistance`/`maxDistance` türetir — sessizce anlamsız bir
   ses düşüşü sonucu (bkz. Core Rules).
5. **GIVEN** edit-time validasyon atlanmış ve iki nesne aynı `shiftId`'yi
   paylaşıyor, ilki zaten Committed (ve her iki asset de `Persistent=true`
   kalıyor — bkz. Edge Cases'teki bileşik-bypass durumu için ayrı, farklı
   sonuçlu senaryo), **WHEN** oyuncu ikinciyi tamamlar, **THEN**
   `TriggerShift` çağrılır ama Işık/Volume zaten-aktif olduğundan no-op
   döner (`OnShiftStateChanged` tekrar ateşlenmez); ikinci nesne yine de
   Committed'a geçer, crash/hata fırlatılmaz — bu, bypass durumunda
   **kabul edilen** tek davranıştır.
6. **GIVEN** bu sistemin kod tabanı, **WHEN** CI'da bir static-analiz
   adımı (`RevertShift(` çağrısı için proje genelinde bir grep/lint
   kuralı) çalışır, **THEN** hiçbir kod yolunda `RevertShift` çağrısı
   bulunmaz; bulunursa CI adımı başarısız olur. *(design-review,
   2026-08-02 — qa-lead bulgusu: önceki hali bunu "manuel ADVISORY
   gate" olarak sınıflandırıyordu, ama bu deterministik, "feel"
   bileşeni olmayan bir statik değişmez kontrolü — Logic sistemleri
   için BLOCKING sınıfına girer, CI-enforced bir lint kuralı olarak
   otomatikleştirilebilir.)*
7. **GIVEN** gerçek sahnede yerleştirilmiş bir `MemoryTriggerObject`, 4
   bağımlı sistem tam kurulu, **WHEN** oyuncu tutar ve Hold tamamlanır,
   **THEN** shift ateşlenir, stinger çalar, mapped ipucu açığa çıkar —
   **ERTELENDİ**, bkz. aşağıdaki Blocked Acceptance Criteria tablosu.
8. **GIVEN** nesne Unfired, **WHEN** destroy/disable Hold tamamlanmadan
   olur, **THEN** `TriggerShift` hiç çağrılmamıştır. *(İlgili
   `shiftId`'nin bu oturumda potansiyel erişilemez kalması ayrı bir
   level-design riski notudur, test edilebilir bir iddia değildir —
   design-review, 2026-08-02, qa-lead bulgusu: önceki hali bu ikisini
   tek maddede birleştiriyordu; bkz. Edge Cases'teki aynı not.)*
9. **GIVEN** nesne Committed, **WHEN** destroy/disable olur, **THEN**
   Işık/Volume durumu etkilenmez, yan etki oluşmaz.
10. **GIVEN** daha önce Committed olmuş bir `MemoryTriggerObject`'in
    bulunduğu sahne oturum içinde yeniden yüklenir (Gece/Oturum
    Durumu'nun `FiredTriggerIds`'i bu `shiftId`'yi zaten içeriyor),
    **WHEN** nesne yeniden etkinleşir ve `OnEnable()`'ının başındaki
    restore sorgusu çalışır (ADR-0014, 2026-08-09 senkron — önceki
    "yeniden `Awake()` olur" ifadesi restore-zamanlaması düzeltmesiyle
    güncellendi), **THEN** `Unfired`'a hiç
    girmeden doğrudan **Committed** durumunda başlar (`CanInteract=false`,
    hiç prompt sunulmaz, ikinci bir `TriggerShift` çağrısı yapılmaz).
    *(design-review, 2026-08-02 — unity-specialist bulgusu; bkz. Core
    Rules ve Edge Cases, sahne-yeniden-yükleme restore notu.)*

### Blocked Acceptance Criteria (Deferred)

| AC | Blocked By | Closure Trigger | Owner |
|---|---|---|---|
| 7 (uçtan-uca: shift ateşlenir, stinger çalar, ipucu açığa çıkar) | Tam motor build + 4 bağımlı sistemin (Etkileşim, Işık/Volume, Adaptif Ses, Anlatı Durum) hepsinin implemente edilmiş olması | `systems-index.md`'nin zaten planladığı audio-paired follow-up spike (`/prototype --spike`, bkz. systems-index.md "Sequencing note" — bu GDD'nin Formulas/Tuning Knobs'unun kilitlenmesi zaten bu spike'a bağlıydı) tamamlandığında bu AC'nin gerçek sahne testiyle kapanışı doğrulanmalı | Anı-Tetikleyici Etkileşim implementasyon sahibi (systems-index.md sırasına göre, Batch 3) |

*(design-review, 2026-08-02 — qa-lead + creative-director bulgusu: önceki
hali "ERTELENDİ (tam motor build gerektirir)" satır-içi etiketiydi,
Owner ya da Closure Trigger yoktu — `isik-volume-durum-sistemi.md`'nin
zaten kurduğu Blocked-ACs tablo desenine hizalandı. Bu tablo aynı zamanda
Visual/Audio Requirements'taki algılanabilirlik riskinin (bkz. CD-GDD-ALIGN
notu) "sessizce faith ile gönderilmesi" yerine somut bir kapanış
tetikleyicisine bağlanmasını sağlar — creative-director'ın istediği "ya
bir fallback ya da bu AC'yi öne çekilmiş bir prototip-kapısı olarak
gerçek bir kapanış tetikleyicisiyle bağla" seçeneklerinden ikincisi
seçildi, çünkü bir fallback geri-bildirim eklemek Visual/Audio
Requirements'ın kendi "Ek görsel ipucu yok" ilkesiyle doğrudan çelişirdi
— bkz. o bölümdeki güncellenen not.)*

## Open Questions

- **Oturum duraklama/bitişi sırasında Holding'de olma boşluğu**: Etkileşim
  Sistemi'nin kendi sözleşmesi `IsSessionActive→false` olurken bir Hold'u
  iptal etme davranışı tanımlamıyor. Bu GDD'nin çözebileceği birşey değil.
  Sahip: Etkileşim Sistemi (kendi GDD'sinde ele alınmalı).
- **HoldDuration alt-aralığının (0.6–1.5sn) edit-time zorlanması**:
  qa-lead'in bulgusu — her `MemoryTriggerDef` bu aralığın dışına
  çıkarsa bir validasyon hatası mı olsun, yoksa sadece bir içerik
  yönergesi mi (uyarı, hata değil)? Sahip: sonraki bir tuning/
  content-authoring geçişi.
- **Hibrit Tepkisellik'in kesin arayüzü** *(design-review, 2026-08-02 —
  daraltıldı)*: Bu sistem `OnShiftStateChanged`'e Adaptif Ses/Anlatı
  Durum deseniyle mi abone olacak, yoksa bu sistemden "bu bir oyuncu-
  tetiklediği kayma mıydı" bilgisini taşıyan ayrı bir sinyale mi ihtiyaç
  duyacak? Sahip: Hibrit Tepkisellik GDD'si yazılırken çözülecek.
  **Sahne Kesmeli Anlatı için bu soru artık kapalı**: aynı gün
  tamamlanan `sahne-kesmeli-anlati-2026-08-02.md`, bu sisteme hiç
  bağımlı olmadan, kendi bitiş sinyalini **Gece/Oturum Durumu'nun
  `SettledTriggerIds.Count`** toplamı üzerinden okuyarak bu belirsizliği
  çözdü (design-review, 2026-08-04 ikinci tur bulgusuyla düzeltildi —
  bkz. yukarıdaki Dependencies notu — saturation sinyali 2026-08-03'te
  Anlatı Durum'dan Gece/Oturum'a, 2026-08-04'te de `FiredTriggerIds`'ten
  `SettledTriggerIds`'e taşındı) — ne `OnShiftStateChanged`'e abone
  oluyor ne de bu sistemden yeni bir sinyale ihtiyaç duyuyor (bkz.
  Dependencies).
- **HoldDuration'ın hissedilen süresi test edilmedi (design-review,
  2026-08-02 — game-designer bulgusu)**: 0.6–1.5sn'lik Tuning Knob
  aralığı, Player Fantasy'nin tarif ettiği "büyüyen tuhaflık" yayını
  (bırakabilirdin ama bırakmadığının giderek büyüyen farkındalığı)
  taşımak için mekanik olarak yeterli olsa da, hiçbir Acceptance
  Criterion bunun *hissedilen* süre olarak yeterli olup olmadığını
  doğrulamıyor — sadece mekanik doğruluğu (t=0..1) test ediliyor. Sahip:
  ilk playtest geçişi (bu bir tasarım-doğrulama sorusu, kod hatası
  değil).
- **Persistent birikme riski daha erken yük taşıyor**: Bu GDD her
  oyuncu-tetiklediği kaymanın `Persistent=true` olduğunu garanti
  ettiğinden, Işık/Volume'un kendi Open Questions'ındaki "eşzamanlı
  aktif Persistent shift sayısına üst sınır yok" riski artık daha
  erken/daha somut hale geliyor. Sahip: Plot Twist/Final Sekansı GDD'si
  (zaten oradaki sahiplik, sadece bu GDD'den çapraz-referans).
  **CD-GDD-ALIGN notu**: Bu GDD `Persistent=true`/`RevertShift` asla
  çağrılmaz kararını edit-time bir değişmez olarak kilitlediği için,
  gelecekteki çözüm **"bazı kaymaları geri döndür" seçeneğini
  kullanamaz** — bu seçenek bu GDD tarafından zaten kapatılmıştır. Cap
  çözümü Işık/Volume'un kendi görünür-bölge/hysteresis mantığı üzerinden
  (ör. bir üst sınıra ulaşıldığında yeni Persistent shift'leri
  reddetme/kuyruğa alma) aranmalı, reversiyon üzerinden değil.
- **Committed ile algılanabilir kaymanın başlangıcı arasındaki boşluk
  (design-review, 2026-08-02 — creative-director bulgusu)**: Prompt
  `OnHoldComplete`'te anında kaybolur, ama Işık/Volume'un ~3sn'lik
  smoothstep geçişi (ve Adaptif Ses'in stinger'ı) o anda henüz
  algılanabilir olmaya başlamamıştır — kısa bir "sessiz boşluk" oluşur.
  Creative Director bunun yeni bir geri bildirim eklenerek değil,
  Işık/Volume ile aradaki zamanlama sözleşmesi sıkılaştırılarak (ör.
  `TriggerShift` çağrısının Shifting-In'i olabildiğince erken/senkron
  başlatması, boşluğun ölçülüp üst sınırlanması) kapatılmasını önerdi —
  yeni bir VFX/ses ipucu, Visual/Audio Requirements'ın "Ek görsel ipucu
  yok" ilkesini ihlal eder. Sahip: implementasyon aşamasında Işık/Volume
  ile birlikte doğrulanacak bir zamanlama detayı, tasarım değişikliği
  değil.
- **MemoryColor seçim kriteri**: Işık/Volume'un kendi açık sorusu (mavi
  vs. sodyum-yeşil hangi tetikleyicide) — her `MemoryTriggerDef`'in
  `shiftConfig`'i için içerik yazılırken level design aşamasında
  çözülecek, bu GDD'nin kendi sorusu değil, sadece çapraz-referans.
