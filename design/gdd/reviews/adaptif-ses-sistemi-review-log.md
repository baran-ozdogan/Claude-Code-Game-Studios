# Review Log: Adaptif Ses Sistemi

## Review — 2026-08-03 — Verdict: NEEDS REVISION → aynı oturumda revize edildi
Scope signal: M
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, audio-director, creative-director (senior synthesis)
Blocking items: 3 | Recommended: 13

Summary: Full-mode review 5 uzman ajan (bu GDD'nin ses-domain'ine özgü
olarak audio-director de eklendi) + creative-director sentezi kullandı.
En ciddi ve tek başına duran bulgu systems-designer'dan geldi: stinger'ın
`Idle`/`Playing`/`Cooldown` durumu önceden ~1s'lik Cooldown'un "sahne-
yükleme edge case'ini de yuttuğunu" iddia ediyordu, ama bu matematiksel
olarak yanlıştı — Cooldown ~1s içinde zaten Idle'a döner, gerçek bir
sahne yüklemesi (asansör yolculuğu) çok daha uzun sürer. Işık/Volume'un
kendi reload Edge Case'i, bir Persistent shift zaten Held-Persistent
iken sahne yeniden yüklendiğinde `OnShiftStateChanged(Held)`'i kasıtlı
olarak bir kez daha fırlatır (senkron için) — bu re-fire Idle'ı bulur ve
stinger'ı yeniden çalardı, "tanıdık ama yersiz" sesin oturum boyunca
sadece bir kez duyulması gereken Player Fantasy garantisini doğrudan
kırardı. İkinci blocking madde iki uzmanın farklı açılardan bağımsız
yakınsaması: "asla ambiyans RMS'ini aşmaz" kuralının hiçbir runtime
savunması yoktu — game-designer bunu üretim-süreci riski (2 kişilik
ekip, dedike ses mühendisi yok, zamanla drift riski) olarak, audio-
director ise fizik riski (Balo Salonu'nun derin HVAC bas gürültüsü,
zayıf-bas hoparlörlerde stinger'ı maskeleyebilir) olarak işaretledi —
creative-director iki bulguyu "iki farklı disiplinin aynı boşluğa
yakınsaması, gerçek bir sinyal" olarak nitelendirdi. Üçüncü blocking
madde üç uzmanın farklı açılardan dokunduğu bir konu: stinger altyazı
metninin (nesne-adlandıran mı izlenimci mi) açık soru olarak kalması,
ama dokümanda duran tek somut metnin zaten reddedilmiş taslak olması —
game-designer bunun bir doküman-yapısı tuzağı olduğunu (AC14 içeriği
değil sadece senkronu test ediyordu), qa-lead AC14'ün test-edilebilirlik
sorununu, audio-director ise creative-director'ın kendi önceki
çerçevelemesini düzelterek (izlenimci metin işitme engelli oyuncu için
boşluğu kapatmaz, ayrı bir erişilebilirlik sorunu) işaretledi.

Recommended maddeler arasında öne çıkanlar: eksik `stinger_falloff`
formülü (unity-specialist — projenin kendi design-docs.md standardını
ihlal ediyordu, hiç tanımlanmamıştı), nonexistent `design/registry/
entities.yaml` referansı (systems-designer), nefes ritmi stinger
adayının korku-skorlama dilbilgisi taşıdığı ve Pillar 2'yi ihlal ettiği
(audio-director, creative-director'ın kabul ettiği bir "creative call"),
üçüncü-bölge-crossfade edge case'inin tanımsız olduğu (systems-designer),
ve AC14'ün test-edilebilir/test-edilemez kısımlara ayrılması gerektiği
(qa-lead).

Specialist disagreement yoktu — creative-director kendi önceki
CD-GDD-ALIGN çerçevelemesini audio-director'ın bulgusuyla düzeltti
(kendi kendine düzeltme, uzmanlar arası çelişki değil).

Tüm 3 blocking madde + doğrudan ilişkili recommended maddeler aynı
oturumda dokümana işlendi — hiçbiri kullanıcının açık bir tasarım kararı
gerektirmedi (kalıcılık düzeltmesi mevcut proje deseninden, RMS/limiter
düzeltmesi creative-director'ın kendi kararından, altyazı metni ise
kasıtlı olarak `/ux-design`'a bırakıldı, icat edilmedi). Ayrıca bu
review, `isik-volume-durum-sistemi.md`'de iki bayat çapraz-referans
tespit etti (Anı-Tetikleyici Etkileşim ve bu sistemin ikisi de "henüz
tasarlanmadı" olarak listeleniyordu, ikisi de artık tasarlanmış/Approved)
ve düzeltti — o dokümanın kendi Blocked Acceptance Criteria tablosundaki
AC16 girişi de bu review'in kapanış tetikleyicisi olarak güncellendi.

Prior verdict resolved: İlk review (önceki verdict yok)
Next: Kullanıcı revizyonları kabul etti, re-review yapılmadan Approved
olarak işaretlendi (bkz. aşağıdaki karar girişi).

---

## Karar — 2026-08-03 — Verdict: APPROVED (re-review yapılmadan)

Kullanıcı, aynı oturumda uygulanan revizyonları kabul etti ve re-review
istemedi — 3 blocking maddenin hepsi somut, doğrulanabilir düzeltmelerle
kapatıldı (kalıcı durum takibi + yeni AC'ler, static limiter + tuning
knob, ve subtitle metninin dokümana hiç sabitlenmemesi + doğru
çerçevelenmiş açık soru), hiçbiri açık bir tasarım kararı gerektirmedi.

**Doküman Status**: Approved. Sistem `design/gdd/systems-index.md`'de
Approved olarak güncellendi.

**Açık kalan, bilinçli olarak bloklamayan maddeler**: stinger altyazı
metninin kesin içeriği (`/ux-design`, madde 2); işitme engelli oyuncu
için ayrı erişilebilirlik boşluğu (`/ux-design`, madde 3); diyalog
altyazısıyla eşzamanlı görünürlük kuralı (`/ux-design`, madde 4);
stinger'ın algısal ses-düşüş yarıçapının `radius`'tan bağımsızlaştırılması
(tuning/Vertical Slice geçişi, madde 5); footstep minimum-interval
throttle'ının hangi sistemde uygulanacağı (unity-specialist, dev-story
öncesi, madde 1 — zaten dokümanın kendi self-flagged blocker'ı).

**Proje-geneli not**: Bu, art arda dördüncü GDD review'inde (bugün, bu
oturumda) "koruma garantisinin gerçekte doğru olmadığı ama iddia edildiği"
hata sınıfının bir varyasyonını gösteriyor (Cooldown'un sahne-yüklemeyi
"yuttuğu" iddiası) — önceki üç review'de görülen "adsız kalıcılık
mekanizması" ve "terminal durumdan çıkış yolu yok" desenleriyle aynı
ailede: bir belgenin kendi iddiasının, o iddiayı destekleyen mekanizma
hiç var olmadan yazılması. Dört review'de dört farklı varyasyon —
şablon kuralına dönüştürülmeyi hak eden bir örüntü, `.claude/rules/
design-docs.md` güncellemesi olarak ayrı bir görev (önceki review
loglarında da not edildi).
