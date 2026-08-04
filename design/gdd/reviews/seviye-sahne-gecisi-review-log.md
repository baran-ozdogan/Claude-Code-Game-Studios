# Review Log: Seviye/Sahne Geçişi

## Review — 2026-08-03 — Verdict: NEEDS REVISION → aynı oturumda revize edildi
Scope signal: M (systems-index.md'nin mevcut "S" tahmini muhtemelen iyimser — bkz. aşağıdaki not)
Specialists: game-designer, systems-designer, qa-lead, unity-specialist, creative-director (senior synthesis)
Blocking items: 5 | Recommended: 8

Summary: Full-mode review 4 uzman ajan + creative-director sentezi
kullandı. İki güçlü çapraz-uzman yakınsaması bulundu (dört uzmanın
bağımsız çalışıp aynı dikişte takılması, gerçek bir sinyal): (1) üç
uzman (game-designer, unity-specialist, qa-lead) bağımsız olarak
sıfır-kare HARD CUT garantisinin doğrulanmamış/iç-tutarsız/test-edilemez
olduğunu buldu — States and Transitions "Swapping eski sahne unload'ını
da tetikler" derken Tuning Knobs ayrı, gecikmeli bir unload (0.5-2s)
öneriyordu (doğrudan çelişki, `UnloadSceneAsync`'in senkron `OnDestroy`
maliyeti gerçek bir hitch riski), ve AC-9'un "≈0" iddiası hiçbir sayısal
epsilon taşımıyordu; (2) iki uzman (systems-designer, qa-lead) bağımsız
olarak `Failed` terminal durumunun hiçbir çıkış yolu olmadığını buldu —
tek bir bozuk sahne referansı oturumun geri kalanı boyunca her gelecekteki
geçişi kalıcı olarak soft-lock'layabilirdi, üretim durdurucu bir boşluk.
Üçüncü önemli bulgu (game-designer): SOFT'un "yükleme her zaman kapı
açılmadan biter" garantisi yapısal olarak yoktu, Tuning Knobs bunu bir
garanti değil bir *risk* olarak belgeliyordu. Dördüncü (qa-lead): AC-7,
CD-GDD-ALIGN'ın 2026-08-02'de eklediği `OnSoftTransitionRejected`
event'inin fiilen fırladığını hiç test etmiyordu. Beşinci
(unity-specialist): "paylaşılan Environment sahnesi" çözümü bir yer
tutucuydu — `RenderSettings`/`LightmapSettings` Unity'de sahne-başına
global olduğundan, bu çözüm odalar arası farklı baked lighting'i
kaybetme riskini hiç adreslemiyordu.

Kullanıcı tüm 5 blocking madde + doğrudan ilişkili recommended maddeleri
aynı oturumda dokümana işlemeyi onayladı (hiçbiri kullanıcının açık bir
tasarım kararını gerektirmedi — mevcut proje dokümanlarından/kısıtlarından
doğrudan çıkarılabildiler): Swapping/unload çelişkisi giderildi (sadece
`SetActiveScene` zero-frame adımın parçası, unload `Complete`'ten sonra
ayrı bir arka plan işlemi); `SWAP_FRAME_EPSILON` (1 kare, ≤16.6ms)
tanımlandı ve "0 siyah kare" ayrı, epsilon'suz bir değişmez olarak
ayrıldı (AC-9 buna göre yeniden yazıldı, `isik-volume-durum-sistemi.md`'nin
adlandırılmış-epsilon desenine hizalanarak); `Failed` → `Idle` otomatik
çıkışı eklendi (yeni AC-11a); SOFT'un gerçek tamamlanma garantisi
Duration knob'undan yapısal olarak ayrıştırıldığı netleştirildi; AC-3 ve
AC-7 artık `OnSoftTransitionRejected`'ın fiilen fırladığını doğruluyor
(ve bu event'in kapsamı, sadece HARD-CUT-aktifken değil, SOFT'un
reddedildiği her duruma genişletildi — kendi başına ek bir bulgu, aynı-tür
SOFT çakışmasında event'in fırlamayabileceği bir boşluğu kapatıyordu);
RenderSettings/lightmap stratejisi somutlaştırıldı (sahne-başına ayrı
lightmap, script-tabanlı RenderSettings senkronu, Işık/Volume'un kendi
Volume-per-zone deseninden emsal alınarak — "paylaşılan Environment
sahnesi" fikri terk edildi). Ayrıca: `PreloadHardCut`'ın kendi durum
takibinin `CurrentState`'ten ayrı olduğu netleştirildi (SOFT aktifken
HARD CUT preload'unun neden çalışabildiğini açıklayan bir mimari
netleştirme); AC-6'nın (bekleyen slot doluyken ikinci HARD CUT reddi)
kendi kuyruklama gerekçesiyle çelişmediği, çünkü tek amaçlanan çağıranın
(Sahne Kesmeli Anlatı) kendi gece-başına-bir-kez guard'ının bu senaryoyu
zaten önlediği belgelendi; Transform kopyalama için bir koordinat-çerçevesi
hizalama kuralı eklendi; AC-12 için Blocked Acceptance Criteria tablosu
eklendi (Asansör yarısı kendi AC-9'uyla zaten kapalı, Sahne Kesmeli Anlatı
yarısı hâlâ açık olarak işaretlendi); ve `asansor-kat-erisim-sistemi.md`'nin
kendi Open Questions'ındaki bir madde düzeltildi (sahibi "unity-specialist"
olarak yanlış atanmıştı — bu rolün kendi tanımı tasarım kararı vermeyi
yasaklıyor — ve "paylaşılan Environment sahnesi" referansı, bu GDD'nin
kendi revizyonunda terk edildiği için bayatlamıştı).

Tek specialist disagreement yoktu — dört uzman birbirini tamamlayan farklı
açılardan bulgular getirdi, hiçbiri birbiriyle çelişmedi. Creative director
iki bulgunun (AC-6 aynı-tür çelişkisi, `PreloadHardCut`-vs-SOFT çakışması)
ciddiyetini hafifçe düşürdü — gerçek ama yeniden-tasarım değil, bir
anotasyon/guard ile çözülebilir buldu.

Prior verdict resolved: İlk review (önceki verdict yok)
Next: Kullanıcı revizyonları kabul etti, üçüncü bir tam re-review
yapılmadan Approved olarak işaretlendi (bkz. aşağıdaki karar girişi).

---

## Karar — 2026-08-03 — Verdict: APPROVED (re-review yapılmadan)

Kullanıcı, aynı oturumda uygulanan revizyonları kabul etti ve re-review
istemedi — 5 blocking maddenin hepsi somut, doğrulanabilir düzeltmelerle
kapatıldı (state-machine netleştirmeleri, adlandırılmış epsilon sabiti,
yeni AC'ler ve bir Blocked Acceptance Criteria tablosu), hiçbiri açık bir
tasarım kararı gerektirmedi.

**Doküman Status**: Approved. Sistem `design/gdd/systems-index.md`'de
Approved olarak güncellendi.

**Açık kalan, bilinçli olarak bloklamayan maddeler** (implementasyon/
sonraki aşamalara devredildi): Unity 6.3 RenderGraph/multi-scene camera
stacking teknik spike'ı (implementasyon öncesi teknik doğrulama); SOFT
geçiş minimum süresinin kesin değeri (playtest); sıfır-kare HARD CUT'ın
"çalınma değil hata" algısal riski (CD-PLAYTEST geçidi, ses sistemi
entegre edildikten sonra) — artık bir ikincil/ses-bağımsız sinyal
eksikliği notuyla genişletilmiş, sahibi Sahne Kesmeli Anlatı/Diyalog
İçeriği; asansör kabini paylaşım sorusunun düzeltilmiş sahibi
(level-designer + technical-director, `asansor-kat-erisim-sistemi.md`'de).

**Proje-geneli not**: Bu, art arda ikinci GDD review'inde (bugün, bu
oturumda) bir "koruma garantisinin aslında tek bir bypass edilebilir
kontrole dayandığı ama bunun adlandırılmadığı" ve "terminal bir durumdan
çıkış yolunun belgelenmediği" hata sınıflarının tekrarlandığını gösteriyor
— `anlati-durum-ipucu-takibi-review-log.md`'de zaten not edilen "adsız
kalıcılık/zamanlama mekanizması" deseniyle aynı ailede. Bu, GDD şablonuna
kalıcı bir kural olarak eklenmeyi hak ediyor: (1) bir Core Rule başka bir
kuralın koşulsuz olmayan bir sonucuysa, bu bağımlılık açıkça
adlandırılmalı; (2) her terminal/"Failed" durum, oradan bir çıkış yolu
tanımlamalı ya da yokluğunu açıkça gerekçelendirmeli. `.claude/rules/design-docs.md`
güncellemesi olarak ayrı bir görev.
