# Review Log: Anı-Tetikleyici Etkileşim

## Review — 2026-08-03 — Verdict: NEEDS REVISION → aynı oturumda revize edildi
Scope signal: L (systems-index.md'nin mevcut tahminiyle eşleşiyor — High-Risk System olarak zaten işaretliydi)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, creative-director (senior synthesis)
Blocking items: 4 | Recommended: 6

Summary: Full-mode review 4 uzman ajan + creative-director sentezi kullandı.
Tek ve en kritik bulgu unity-specialist'tendi: `Committed` durumu
(`CanInteract=false`) sadece sahne-lokal bir `MonoBehaviour` alanında
yaşıyordu, Gece/Oturum Durumu hiç dependency olarak listelenmemişti — bu,
sahnenin oturum içinde yeniden yüklenmesinde (asansörle kat değişimi, en
sıradan yol) nesnenin sessizce Unfired'a sıfırlanması ve tekrar
tutulabilir hale gelmesi demekti. Player Fantasy'nin tüm önermesi
("bunu ben, bile bile yaptım ve artık geri alamam") bu bug yüzünden en
sık karşılaşılan yolda kırılıyordu — creative-director bunu "review'in
kendisi" olarak nitelendirdi. İkinci bulgu (unity-specialist +
systems-designer + qa-lead, üçü de bağımsız) edit-time validasyon
mekanizmasının hiç adlandırılmamış olmasıydı — `OnValidate` (kardeş
asset'leri göremez, doğal bir yanlış-tahmin tuzağı) ile
`IPreprocessBuildWithReport` (gerçekten build'i engelleyen tek mekanizma)
arasında GDD hiç seçim yapmıyordu, ve `anlati-durum-ipucu-takibi.md`'nin
clueId validatörü de aynı boşluğu taşıyordu. Üçüncü bulgu (game-designer +
creative-director): sistemin tüm tamamlanma sinyali Işık/Volume + Adaptif
Ses'in algılanabilir olmasına bağımlıydı (GDD'nin kendi CD-GDD-ALIGN notu
bunu zaten prototip bulgusuyla flag'liyordu), ama bunu doğrulayacak tek AC
sahipsiz/kapanış-tetikleyicisiz ERTELENDİ durumundaydı. Dördüncü bulgu
(qa-lead): AC listesi testability sözleşmesini ihlal ediyordu — AC6
kendini test-edilemez ilan eden bir madde olarak numaralı listede
duruyordu, AC7 (RevertShift grep) deterministik bir statik kontrol olduğu
halde "manuel ADVISORY" etiketliydi, AC8 (şimdiki AC7) `isik-volume-
durum-sistemi.md`'nin zaten kurduğu Blocked-ACs tablo desenine sahip
değildi.

Tek specialist disagreement: game-designer, crosshair'ın hold-fill
göstergesinin zaten "sıfır geri bildirim" iddiasını çürüttüğünü savundu;
creative-director katılmadı — bu göstergenin paylaşılan, jenerik
Etkileşim Sistemi chrome'u olduğunu, bu seçime özgü yazarlı bir geri
bildirim olmadığını belirtti. CD, game-designer'ın daha dar bir
versiyonuna (prompt kayboluşu ile Işık/Volume'un ~3sn'lik geçişinin
algılanabilir olmaya başlaması arasındaki "sessiz boşluk") katıldı — bunu
yeni bir ipucu eklenerek değil, zamanlama sözleşmesi sıkılaştırılarak
kapatılmasını önerdi.

Tüm 4 blocking madde + doğrudan ilişkili recommended maddeler aynı
oturumda dokümana işlendi (kullanıcının açık bir tasarım kararı
gerekmiyordu — her ikisi de mevcut proje dokümanlarından/hakim
kısıtlardan doğrudan çıkarılabiliyordu): Committed durumu artık
Gece/Oturum Durumu'nun `FiredTriggerIds`'i üzerinden kalıcı (Core Rules +
yeni Edge Case + yeni AC10, Gece/Oturum Durumu Dependencies'e eklendi);
edit-time validasyon mekanizması `IPreprocessBuildWithReport` olarak
somutlaştırıldı (paylaşılan editor utility notu eklendi); algılanabilirlik
riski, yeni bir fallback ipucusu eklemek yerine (bu, "Ek görsel ipucu yok"
ilkesiyle çelişirdi) AC7'nin zaten planlanan audio-paired spike'a
bağlanan somut bir Blocked Acceptance Criteria girişiyle kapatıldı; AC
listesi temizlendi (AC6 kapsam notuna taşındı, AC7[eski] BLOCKING CI
kontrolüne yeniden sınıflandırıldı, AC9[eski] untestable cümlesi
ayrıldı). Ayrıca: "guard gerekmez" garantisinin `Persistent=true`
değişmezine gizli bağımlılığı için çapraz-referans eklendi, iki bağımsız
bypass'ın (duplike shiftId + Persistent=false) kesişimi için yeni bir
bileşik-başarısızlık Edge Case'i belgelendi, Persistent=false leak'inin
Anlatı Durum'un `SeenShiftIds`'i üzerindeki etkisi için bir çapraz-kontrol
notu eklendi, `MemoryTriggerDef` asset'ine fired-bayrağı yazmayı
yasaklayan bir kural eklendi, Sahne Kesmeli Anlatı'nın kesin arayüzüne
dair artık stale olan açık soru çözüldü olarak işaretlendi (Hibrit
Tepkisellik yarısı açık kaldı), ve GDD'nin kendi "systems-index.md
düzeltmesi gerekiyor" notu fiilen uygulandı (`systems-index.md`'nin
Systems Enumeration ve Dependency Map bölümleri güncellendi).

Prior verdict resolved: İlk review (önceki verdict yok)
Next: Kullanıcı revizyonları kabul etti, üçüncü bir tam re-review
yapılmadan Approved olarak işaretlendi (bkz. aşağıdaki karar girişi).

---

## Karar — 2026-08-03 — Verdict: APPROVED (re-review yapılmadan)

Kullanıcı, aynı oturumda uygulanan revizyonları kabul etti ve üçüncü bir
tam re-review istemedi — 4 blocking maddenin hepsi somut, doğrulanabilir
düzeltmelerle kapatıldı (yeni Core Rules, Edge Cases, Dependencies,
Acceptance Criteria ve bir Blocked Acceptance Criteria tablosu), hiçbiri
açık bir tasarım kararı gerektirmedi (mevcut proje dokümanlarından/
kısıtlarından doğrudan çıkarıldılar).

**Doküman Status**: Approved. Sistem `design/gdd/systems-index.md`'de
Approved olarak güncellendi.

**Açık kalan, bilinçli olarak bloklamayan maddeler** (implementasyon/
sonraki aşamalara devredildi): HoldDuration alt-aralığının edit-time
zorlanması (tuning geçişi), Hibrit Tepkisellik'in kesin arayüzü (o GDD
yazılırken), HoldDuration'ın hissedilen süresi (ilk playtest), Committed-
ile-algılanabilir-kayma-arası zamanlama boşluğu (implementasyon aşamasında
Işık/Volume ile birlikte), Persistent birikme riski (Plot Twist/Final
Sekansı GDD'si — zaten oradaki sahiplik).
