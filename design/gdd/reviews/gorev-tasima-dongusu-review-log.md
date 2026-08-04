# Review Log: Görev/Taşıma Döngüsü

## Review — 2026-08-02 — Verdict: NEEDS REVISION → aynı oturumda revize edildi
Scope signal: L
Specialists: game-designer, systems-designer, qa-lead, audio-director, gameplay-programmer, unity-specialist, ux-designer, creative-director (senior synthesis)
Blocking items: 9 | Recommended: 9

Summary: Full-mode review 7 uzman ajan + creative-director sentezi kullandı.
Üç madde çekirdek tasarımı etkiliyordu: (1) taşınan eşyanın görsel modeli
Core Rules (tek mesh-swap) ile UI Requirements (N'e kadar istiflenmiş
görünüm) arasında doğrudan çelişiyordu, (2) dokümanın merkezi "Görev
emeği → Öznel gerçeklik kayması" iddiasının tek somut mekanizması
(round-bazlı ışık sönmesi) sayısal olarak tanımsızdı, (3) depo↔balo salonu
sahne geçişinde taşınan state'in kalıcılığı hiç ele alınmamıştı (unity-specialist:
"en büyük implementasyon mayını"). Ayrıca edit-time validasyon mekanizması
belirsizdi (OnValidate vs. build-blocking), N=0/0-item floor guard'ı yoktu,
ve dokümanın kendi "AC seviyesinde zorunluluk" dediği birkaç kısıt (Eller
Dolu gate, no-blend-tree, jostle round-bağımsızlığı) hiç Acceptance
Criteria'ya yazılmamıştı.

Kullanıcı üç tasarım kararı verdi (N-item gerçek istifleme; placeholder
dimming eğrisi kilitle + playtest-tunable işaretle; rig'i proaktif sinyal
olarak netleştir, ek UI eklemeden) ve tüm 9 blocking madde + ilgili
recommended maddeler aynı oturumda dokümana işlendi: görsel model
düzeltildi (N'e kadar önceden-havuzlanmış slot-temsili), placeholder
dimming eğrisi + Player Fantasy'ye tasarım-hipotezi notu eklendi,
sahne-geçişi kalıcılığı için Gece/Oturum Durumu deseniyle aynı in-memory
servis tanımlandı, edit-time validasyon iki ayrı mekanizmaya (OnValidate +
IPreprocessBuildWithReport) ayrıştırıldı ve floor guard eklendi, 5 yeni
Acceptance Criterion eklendi (AC#3, #4, #9a/9b split, #14, #15, #16),
pickup swap'a fiziksel bir yerleşme darbesi eklendi, carry-sway FPC'nin
faz-biriktiricisine bağlandı, ve jostle SFX spec'i implemente edilebilir
hale getirildi (merdiven referansı kaldırıldı, açısal eşik + min-interval
guard eklendi).

Prior verdict resolved: İlk review (önceki verdict yok)
Next: Kullanıcı yeni bir oturumda (`/clear` sonrası) `/design-review
design/gdd/gorev-tasima-dongusu.md` ile tam re-review talep etti — bu log
o oturumun Phase 1 "prior review check" adımında okunacak.

---

## Review — 2026-08-02 — Verdict: NEEDS REVISION (dar kapsam) → aynı oturumda patch edildi
Scope signal: L
Specialists: game-designer, systems-designer, qa-lead, audio-director, gameplay-programmer, unity-specialist, ux-designer, creative-director (senior synthesis)
Blocking items: 3 | Recommended: 8 (Vertical Slice playtest'e devredildi, bkz. aşağı)

Summary: `/design-review` komutu ile tam bir re-review çalıştırıldı (aynı
oturumda, `/clear` yapılmadan). 7 uzman ajan önceki 9 blocking maddenin
her birini bağımsız olarak (dosyayı yeniden okuyarak, sadece özet
güvenmeyerek) doğruladı: 6/9 gerçekten çözülmüş bulundu (görsel model,
N=0 floor guard, Loading state, InTransit sinyali, AC boşluklarının
çoğu, OnValidate/build-blocking ayrımı). Üç yeni/kalıcı sorun tespit
edildi: (1) **[unity-specialist]** ilk revizyonun kendisi yeni bir
blocking sorun ortaya çıkardı — kalıcılık düzeltmesi state'in hayatta
kaldığını söylüyordu ama depo sahnesinin gerçekten unload/reload olduğu
ve zaten toplanan eşyaların sahne-yazılı varsayılana (aktif) döneceği
hiç ele alınmamıştı ("veri bütünlüğü hatası", CD'nin sözleriyle); (2)
**[game-designer]** dokümanın merkezi "görev listesi küçüldükçe
rahatlama" iddiası hâlâ hiçbir algı kanalına bağlı değildi, ilk
revizyon bu maddeye hiç dokunmamıştı; (3) **[qa-lead, creative-director]**
AC#15/#16 (no-blend-tree, jostle round-bağımsızlığı) advisory/manual
olarak sınıflandırılmıştı ama aslında mekanik olarak test edilebilir
oldukları için dokümanın kendi "zorunluluk" diline göre bu yanlış
sınıflandırmaydı.

CD'nin net ayrımı: round-monotony, rig-görünürlük gerilimi, geri-koyma-yok
riski gibi maddeler (ilk turda recommended/playtest-watch olarak
işaretlenmişti) gerçekten stokastik/playtest-bağımlı risklerdir ve
Open Questions'da bırakılmaları meşrudur — ama yukarıdaki 3 madde
playtest'in çözebileceği türden değildi ("doküman zaten kendi
incelemesinde bunu gösteriyor"). Kullanıcı dar kapsamlı bir patch
turu onayladı (7 uzmanı tekrar çalıştırmadan): depo-reload uzlaşımı
Gece/Oturum Durumu'nun HashSet-deseniyle aynı şekilde tanımlandı
(`CollectedItemIds`, `Awake()`'te registry-kaydından önce kontrol),
görev-listesi rahatlaması aynı round-bazlı dimming sinyaline bağlandı
(ayrı bir UI icat edilmedi), AC#15/#16 Logic-tier otomatik/BLOCKING'e
yükseltildi.

Kalan recommended maddeler (bilinçli olarak bu turda ele alınmadı,
Vertical Slice playtest/sonraki tuning geçişine bırakıldı): pickup
swap'ın somut sayıları, sıfır-VFX teslimat AC'si, session-inactive
teslimat-zone AC'si, Edge Case 6 AC'si, Loading state'in kısmi-teslimat
geçiş yolu, yeni 2 soft bağımlılığın karşı dosyalarda geri
referanslanmaması, Addressables/sharedMaterial notu, drop-off trigger
filtresi, OnTaskListCompleted event lifetime, N/M fallback varsayılan
KAPALI, `design/ux/accessibility-requirements.md` henüz yok.

Prior verdict resolved: Evet — önceki NEEDS REVISION'ın 9 blocking
maddesinden 6'sı doğrulanmış şekilde kapandı, 1'i (kalıcılık) kısmen
kapanıp yeni bir blocking'e dönüştü, 2'si (görsel-model-alt-bulgu,
AC sınıflandırma) bu turda ek olarak kapatıldı.
Next: Kullanıcı kararı bekleniyor — tekrar tam re-review mi, yoksa bu
haliyle Approved mı kabul edilecek.

---

## Karar — 2026-08-02 — Verdict: APPROVED (tam re-review yapılmadan)

Kullanıcı, ikinci turun creative-director sentezindeki ayrımı kabul
etti: kalan tüm açık maddeler (pickup swap somut sayıları, birkaç
eksik AC, Loading state kısmi-teslimat yolu, karşılıklı olmayan 2 soft
bağımlılık, Addressables/erişilebilirlik notları) gerçekten
stokastik/playtest-bağımlı ya da düşük-riskli implementasyon
detaylarıdır — üçüncü bir 7-uzman turu azalan getiri taşırdı (CD'nin
kendi uyarısı). Bu maddeler bu review'in blocking listesinde DEĞİL;
Vertical Slice playtest'inde ve `/dev-story` implementasyon
aşamasında izlenecek.

**Doküman Status**: Approved. Sistem `design/gdd/systems-index.md`'de
Approved olarak güncellendi.
