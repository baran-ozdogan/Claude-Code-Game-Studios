# Accessibility Requirements

> **Status**: Approved (initial version)
> **Author**: Baran + ux-designer (`/ux-design`, 2026-08-09 — dört temel karar AskUserQuestion ile kullanıcı tarafından verildi)
> **Last Updated**: 2026-08-09
> **Committed Tier**: **Pragmatik indie set** (aşağıda tanımlı) — WCAG-AA kontrast hedefli
> **Scope**: MVP + Vertical Slice. Bu dosya, GDD'lerin ve art-bible §7.5'in "accessibility-requirements.md yazıldığında çözülecek" diye ertelediği tüm kararların eviydi — o kararlar artık burada bağlayıcıdır.

---

## 1. Taahhüt Edilen Set (Tier Tanımı)

Kullanıcı kararı (2026-08-09): tam WCAG-AA + motor erişim seti değil, **pragmatik indie seti** taahhüt edilir:

| # | Taahhüt | Durum |
|---|---|---|
| A1 | Altyazı/caption her zaman açılabilir (diyalog altyazısı + stinger caption'ı **koşulsuz** — kapatma seçeneği bile MVP'de yok, GDD gereği) | Tasarımda içkin ✅ |
| A2 | Renk-bağımsız iletişim — hiçbir bilgi yalnızca renkle iletilmez | Tasarımda içkin ✅ (bkz. §4) |
| A3 | Yeniden bağlanabilir tuşlar (Input System rebinding) | Ayarlar yüzeyi gerekli — bkz. §7 |
| A4 | Ayarlanabilir fare hassasiyeti | Ayarlar yüzeyi gerekli — bkz. §7 |
| A5 | WCAG-AA kontrast hedefi (4.5:1 normal metin; 3:1 büyük metin/UI bileşenleri) | Tüm UI öğeleri için bağlayıcı |
| A6 | Motion yoğunluk kaydırıcısı (bkz. §5) | Kullanıcı kararı ✅ |
| A7 | Toggle-hold (opt-in, bkz. §6) | Kullanıcı kararı ✅ |
| A8 | Fotosensitivite: flash/strobe içerik yok | Tasarımda içkin ✅ (Pillar 2 "şok yok" — art-bible flash/burst'ü zaten yasaklar) |

Kapsam dışı (bilinçli): ekran okuyucu desteği, tam gamepad eşliği garantisi (gamepad "partial" — tech-prefs), zorluk seçenekleri (oyunda başarısızlık durumu yok — zaten tasarım gereği herkes bitirebilir).

## 2. Altyazı ve Caption Sözleşmesi

İki ayrı kanal, asla aynı öğe/stil paylaşmaz (adaptif-ses AC14/14b'nin beklediği somut belirteçler):

### 2a. Diyalog Altyazısı (`#dialogue-subtitle`, `diyalog-*` öneki)
- Konum: alt-orta; ekran yüksekliğinin ~%8'i üstünde taban çizgisi
- Stil: düz (italik değil) beyaz metin, yarı saydam koyu arka plan şeridi (arka plan sahneden bağımsız AA kontrastı garantiler)
- Minimum font: 1080p'de ≥28px eşdeğeri; satır uzunluğu ≤ 42 karakter hedefi
- Konuşmacı etiketi: MVP'de tek konuşan (psikiyatrist) — etiket yok; VS'de arkadaş karakteri gelirse "İsim:" öneki eklenir

### 2b. Stinger Caption'ı (`#stinger-caption`, `ses-*` öneki)
- **Metin stili: izlenimci/soyut (kullanıcı kararı, 2026-08-09 — adaptif-ses Open Questions #2'yi çözer)**. Caption sesi *adlandırmaz*, *izlenimini* verir: `[uzaktan bir uğultu]`, `[tanıdık olmayan bir tını]`, `[bir yerlerden, belli belirsiz]`. Nesne-adlandıran stil ("[porselen şıngırtısı]") reddedildi — anlamı erken ifşa eder, Pillar 5 (Anlam Sona Saklı) ile çelişir ve CD incelemesinin 2026-08-02'de nesne-adlandıran örnekleri geri çektirme gerekçesiyle tutarlıdır. İşitme engelli oyuncu, işiten oyuncuyla **aynı belirsizliği** yaşar — erişilebilirlik burada bilgi eşitliği değil, deneyim eşitliğidir.
- Görsel ayrım diyalogdan: *italik*, köşeli parantezli, diyalog satırının bir kademe üstünde, ~%80 opacity — aynı anda görünürlerse dikey olarak ayrışırlar (adaptif-ses'in eşzamanlılık açık sorusuna yerleşim cevabı)
- Zamanlama: klip penceresine senkron (ADR-0009 addendum: PlayOneShot ile göster, klip süresi sonunda gizle), koşulsuz gösterim
- Yazım dili: caption metinleri lokalize edilir (TR/EN) — string tablosuna girer

### 2c. Crosshair ve Prompt
- Art-bible §4.4/§7.5'in kilitli değerleri geçerli (1-2px, %40-60 opacity statik dış çizgi = koyu/açık zeminde kontrast emniyeti). Bu dosya o değerleri değiştirmez; AA kontrast doğrulaması Visual QA'de bu değerlerle yapılır (ADR-0002 Validation Criteria).
- "Eller Dolu" prompt'u dahil tüm prompt metinleri §2a'nın font/kontrast tabanına uyar.

## 3. Klavye/Girdi

- Tüm oyun içi eylemler tek "Gameplay" action map'te (TR-fpc-015); **rebinding** Input System'in kendi mekanizmasıyla sunulur (A3)
- Fare hassasiyeti kaydırıcısı (A4); gamepad bakış hassasiyeti VS'de
- MVP'de menü/UI navigasyonu yok denecek kadar az (etkileşimli UI öğesi sıfır — ADR-0002 tespiti); ayarlar paneli geldiğinde klavye-yalnız gezinilebilir olmalı (Tab/ok + Enter), odak göstergesi görünür olmalı

## 4. Renk-Bağımsız İletişim (A2 doğrulaması)

Mevcut tasarım zaten uyumlu — bağlayıcı olarak kaydedilir:
- Bellek kayması **compound ışık+ses** ile iletilir (yalnız renk değil; prototip bulgusu zaten "ışık tek başına yetmez" idi)
- Crosshair durumları opacity/scale ile, asla renkle (art-bible §7.4)
- Slot doluluğu diegetik temsil + prompt metniyle, renk kodlamasız
- **Kural**: gelecekte eklenen hiçbir öğe bilgiyi yalnızca renk kanalıyla iletemez (control-manifest adayı)

## 5. Motion (A6 — kullanıcı kararı: yoğunluk kaydırıcısı)

- **Head-bob/kamera sway/carry-sway görsel genliği 0-100% kaydırıcı** ile ölçeklenir; varsayılan %100
- **Faz akümülatörü her zaman tam çalışır** — ayak sesleri, jostle zamanlaması ve tüm faz-türevi sistemler (TR-fpc-014, TR-gorev-016) etkilenmez; yalnızca görsel genlik çarpanı uygulanır. Tasarımın bedensellik hissi ile motion-sickness erişilebilirliği bu ayrımla aynı anda korunur.
- HARD CUT zaten kesme (fade/kamera savrulması yok) — motion açısından güvenli; SOFT geçişler kabin içi, düşük hareketli
- FOV kaydırıcısı: MVP'de yok; VS aday listesine (yaygın konfor talebi)

## 6. Hold Etkileşimi Motor Erişimi (A7 — kullanıcı kararı: opt-in toggle-hold)

- Varsayılan: basılı tutma (tasarım fantezisi bozulmaz)
- Ayarlardan **"Basılı tutma yerine tek basış"** açılabilir: tek basış Hold'u başlatır, süre aynen işler (0.6-1.5s), ikinci basış veya odak kaybı iptal eder. Kasıt penceresi (süre + iptal edilebilirlik) korunur — anı-tetikleyicinin "bile bile yaptım" anlamı, girdinin fiziksel şeklinden değil sürenin varlığından gelir.
- Implementasyon noktası tek: `InteractionStateMachine`'e girdi çevirisi `InteractionController` seviyesinde yapılır (`interactHeld` sinyalinin kaynağı değişir, makine değişmez) — hiçbir `IInteractable` bunu bilmez
- `SuppressDefaultHoldFill` davranışı aynı kalır: toggle modunda da anı-tetikleyici sıfır görsel geri bildirim verir

## 7. Ayarlar Yüzeyi Bağımlılığı (bilinen boşluk)

A3/A4/A6/A7 bir **ayarlar paneli** gerektirir; MVP'de menü sistemi yok (Ana Menü/Başlangıç Akışı = Vertical Slice). Karar: bu seçenekler **oynanabilir demo yayınlanmadan önce** minimal bir UI Toolkit ayarlar paneliyle (ADR-0002 çerçevesinde, kilitli-sözleşmesiz sıradan öğeler — USS transition serbest) sunulmalı; iç geliştirme build'lerinde geçici olarak config dosyası kabul edilir. Bu, `interaction-patterns.md` Gaps #1 ile aynı maddedir; sahibi VS planlaması.

## 8. Doğrulama

- [ ] Tüm metin öğeleri 1080p'de ≥28px eşdeğeri ve AA kontrast (4.5:1) — Visual QA
- [ ] Stinger caption'ı ile diyalog altyazısı aynı anda göründüğünde çakışmaz ve ayırt edilir (adaptif-ses AC'siyle birlikte test edilir)
- [ ] Motion kaydırıcısı %0'da: görsel bob/sway sıfır, ayak sesi zamanlaması değişmemiş (faz akümülatör testi)
- [ ] Toggle-hold açıkken: anı-tetikleyici Hold'u tek basışla başlar, süre ve iptal semantiği birebir aynı, `HoldDuration` AC'leri geçer
- [ ] Renk körü simülasyonunda (protanopia/deuteranopia) hiçbir bilgi kaybı yok — compound efekt ve opacity-tabanlı crosshair doğrulanır
- [ ] Rebind edilen tuşlarla tam oynanış turu tamamlanabilir

## 9. Senkron Borçları (bu dosyanın yarattığı)

- `adaptif-ses-sistemi.md` AC 14b: "accessibility-requirements.md yazıldığında bu AC somut stil belirteçleriyle yeniden yazılmalı" — artık yazılabilir (bkz. §2b). Sahibi: bir sonraki adaptif-ses dokunuşu.
- `etkilesim-sistemi.md` / ADR-0010: toggle-hold seçeneği `InteractionController` girdi çevirisi olarak not edilmeli (davranışsal değişiklik yok, implementasyon notu).
- Control manifest: §4'ün renk-bağımsızlık kuralı + §5'in "faz akümülatörü asla ölçeklenmez, yalnız görsel genlik" kuralı manifest adayı.
