# Review Log: Anlatı Durum/İpucu Takibi

## Review — 2026-08-02 — Verdict: MAJOR REVISION NEEDED → aynı oturumda revize edildi
Scope signal: M
Specialists: game-designer, narrative-director, systems-designer, qa-lead, unity-specialist, creative-director (senior synthesis)
Blocking items: 6 | Recommended: 8

Summary: Full-mode review 5 uzman ajan + creative-director sentezi kullandı
(ilk deneme model kullanılabilirliği nedeniyle başarısız oldu, aynı oturumda
yeniden başlatıldı). Üç bağımsız hata sınıfı üst üste bindi: (1) Player
Fantasy'nin bayrak örneği ("Otel Unutmuyor") çok-geceli bir callback
anlatıyordu ama MVP tek gece ve Pillar 5 MVP'de hiç yüzeye çıkmıyor
(creative-director'ın kendi 2026-08-01 notuyla çelişiyordu) — dokümanın
kendi var olma nedeni kapsamıyla uyuşmuyordu; (2) iki uzmanın bağımsız
olarak bulduğu bir vacuous-truth mantık hatası: boş `requiredShiftIds`
listesi bir `ClueDefinition`'ı hiç tetiklenmeden "Known" yapıyordu; (3) iki
uzmanın bağımsız olarak bulduğu adsız kalıcılık mekanizması — dünkü
`gorev-tasima-dongusu.md` review'inin en büyük bulgusuyla aynı kategori,
artık projede tekrarlayan bir dokümantasyon alışkanlığı sorunu olarak
işaretlendi. Ayrıca sistemin kendi Işık/Volume aboneliğinin zamanlaması hiç
belirtilmemişti (downstream abonelere önerdiği disiplin kendine
uygulanmamıştı), AC#8 mekanizmasız hand-waving'di, ve AC#12'nin erteleme
gerekçesi (iki blocker GDD) her ikisi de artık mevcut olduğu için geçersiz
kalmıştı.

Kullanıcı bir tasarım kararı verdi (Player Fantasy'nin bayrak örneğini
MVP-uygun hale getir — aynı gece içi versiyon ana örnek olsun, çok-geceli
versiyon açıkça "Full Vision hedefi" olarak işaretlensin) ve tüm 6 blocking
madde + ilgili recommended maddeler aynı oturumda dokümana işlendi: Player
Fantasy yeniden çerçevelendi + sahiplik notu eklendi (bu sistem sadece
bilgiyi üretir, "hatırlanma" hissini Diyalog üretir), boş-liste ve
duplicate-clueId için edit-time validasyon + yeni AC'ler eklendi (8a, 8b),
kalıcılık mekanizması Gece/Oturum Durumu'nun HashSet-deseniyle aynı şekilde
adlandırıldı (statik/singleton, merkezi ScriptableObject kaydı), kendi
abonelik zamanlaması netleştirildi (servis ilk erişimde abone olur, geç
abonelik riski yapısal olarak ortadan kalktı), AC#8 somut bir validator
mekanizmasına (ClueConsistencyValidator, warning-only) dönüştürüldü, AC#12
ERTELENDİ durumundan çıkarılıp iki gerçek AC'ye (12, 12b) bölündü, AC#7
test-edilebilir/test-edilemez kısımlara ayrıldı, Dependencies listesindeki
bayat "henüz tasarlanmadı" notları güncellendi, ve Diyalog/Anlatı
İçeriği'ne karşı bir kalıcılık-asimetrisi açık sorusu eklendi.

Prior verdict resolved: İlk review (önceki verdict yok)
Next: Kullanıcı kararı bekleniyor.

---

## Review — 2026-08-02 — Verdict: NEEDS REVISION (dar kapsam) → aynı oturumda patch edildi
Scope signal: M
Specialists: game-designer, narrative-director, systems-designer, qa-lead, unity-specialist, creative-director (senior synthesis)
Blocking items: 2 | Recommended: 2 (bilinçli olarak açık bırakıldı — başka dokümanların sorumluluğu)

Summary: `/design-review` ile tam bir re-review çalıştırıldı (aynı
oturumda). 5 uzman önceki 6 blocking maddenin her birini bağımsız
doğruladı: 4/6 gerçekten kapanmış bulundu (scope-uyumsuzluğu, boş-liste
vacuous-truth, adsız kalıcılık mekanizması, duplicate-clueId, AC#8/AC#12
somutlaştırma — hepsi kaynağa karşı doğrulandı). CD'nin tespiti: kalan
2 madde ("Open Question'a taşındı" diye kapatılmış görünen) aslında
"sahiplik devri" ile "gerçek çözüm" arasındaki farkı gösteriyordu —
biri yanlış sahibe (henüz yazılmamış bir GDD) atanmıştı, diğeri
(Pillar 5 etiketi) CD'nin kendi kararını gerektiriyordu. Ayrıca CD,
bugün üç ayrı dokümanda (dünkü carry-loop GDD'si, bu doküman, ve
zaten Approved olan Işık/Volume GDD'sine bir sıçrama) aynı "adsız
kalıcılık/zamanlama mekanizması" hata sınıfının çıktığını, bunun bir
GDD şablon kuralına dönüştürülmesi gerektiğini not etti.

Kullanıcı dar kapsamlı bir patch turu onayladı: kalıcılık-asimetrisi
Open Question'ının sahibi, henüz yazılmamış Çoklu Gece İlerlemesi
GDD'sinden, kusurlu kod yolunun bugün zaten var olduğu Diyalog/Anlatı
İçeriği quick-spec'ine düzeltildi (kardeş "tempo riski" sorusuyla aynı
doğru desen). Pillar 5 etiketi MVP-uygun örnekten kaldırıldı (Full
Vision versiyonuna taşındı, CD'nin kendi 2026-08-01 notuyla tutarlı).
Ayrıca qa-lead'in iki ucuz temizlik maddesi de uygulandı: MarkClueKnown
bypass'ının denetim-izi notu Open Questions'a taşındı, AC#12b'nin
WHEN'i başka bir sistemin iç mantığını anlatmak yerine kendi event
sözleşmesine odaklanacak şekilde sadeleştirildi.

Kalan 2 madde (AC-algı boşluğu — Diyalog'un ACs'i "hatırlanma" hissini
hiç test etmiyor; abonelik-zamanlaması — "ilk erişim" tetikleyicisi iki
alternatif arasında kesinleşmemiş) bilinçli olarak açık bırakıldı —
uzmanlara göre gerçekten başka dokümanların/aşamaların sorumluluğunda,
bu dokümanı daha fazla bloklamıyor.

Prior verdict resolved: Evet — önceki MAJOR REVISION NEEDED'ın 6
blocking maddesinden 4'ü doğrulanmış şekilde kapandı, 2'si bu turda
(sahip düzeltmesi + Pillar 5 kararı) ek olarak kapatıldı.
Next: Kullanıcı kararı bekleniyor.

---

## Karar — 2026-08-02 — Verdict: APPROVED (üçüncü bir tam re-review yapılmadan)

Kullanıcı, ikinci turun creative-director sentezindeki ayrımı kabul
etti: kalan 2 madde (Diyalog'un ACs'inin "hatırlanma" hissini hiç test
etmemesi, ve abonelik-zamanlamasının "ilk erişim" tetikleyicisinin iki
alternatif arasında kesinleşmemiş olması) gerçekten başka dokümanların/
implementasyon aşamasının sorumluluğunda — üçüncü bir 5-uzman turu
azalan getiri taşırdı. Bu maddeler bu review'in blocking listesinde
DEĞİL; Diyalog GDD'sinin kendi doğrulaması ve implementasyon aşamasında
izlenecek.

**Doküman Status**: Approved. Sistem `design/gdd/systems-index.md`'de
Approved olarak güncellendi.

**Proje-geneli not**: CD, bugün üç ayrı dokümanda aynı "adsız
kalıcılık/zamanlama mekanizması" hata sınıfının çıktığını (dünkü
carry-loop GDD'si, bu doküman, ve zaten Approved olan Işık/Volume
GDD'sine bir sıçrama) tespit etti — bunun tek seferlik bir tesadüf
değil, GDD şablonunun eksik bir gereksinimi olduğunu, dördüncü
tekrardan önce bir template kuralına dönüştürülmesi gerektiğini not
düştü. Bu, `.claude/rules/design-docs.md` güncellemesi olarak ayrı bir
görev.
