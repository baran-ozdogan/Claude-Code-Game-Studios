# Interaction Pattern Library

> **Status**: Approved (initial version)
> **Author**: Baran + ux-designer (`/ux-design patterns`, 2026-08-09)
> **Last Updated**: 2026-08-09
> **Template**: Interaction Pattern Library
> **Sources**: No prior UX specs exist — this catalog is extracted from the 12 MVP GDDs and the 15 Accepted ADRs, where this project's interaction patterns are already specified in unusual detail. Each pattern cites its owning documents; this library indexes and names them, it does not redefine them.
> **Input context** (from `.claude/docs/technical-preferences.md`): Keyboard/Mouse primary, Gamepad partial, PC only (Steam/Epic).

---

## Overview

Yankılar'ın etkileşim dili bilinçli olarak dardır: oyuncunun elleriyle yaptığı her şey **tek bir Interact girdisi** üzerinden akar ve UI yüzeyi toplam 4 öğedir (crosshair/prompt + Hold-fill, stinger caption, diyalog altyazısı — ADR-0002). Desenlerin çoğu "UI'sız etkileşim"dir: dünyanın kendisi arayüzdür (Pillar 1/3). Bu kütüphanenin amacı, yeni içerik eklerken var olan deseni yeniden icat etmemek ve desenler arasındaki kasıtlı ayrımları (ör. neden asansör düğmesi crosshair kullanmaz) korumaktır.

---

## Pattern Catalog

| # | Pattern | Category | Used In |
|---|---------|----------|---------|
| 1 | Focus & Prompt (Crosshair) | Feedback | Tüm `IInteractable` nesneler |
| 2 | Instant Interact | Input | Taşıma eşyası pickup'ları, decoy'lar |
| 3 | Hold Interact — Default Fill | Input | Genel Hold etkileşimleri |
| 4 | Hold Interact — Suppressed Feedback | Input | Anı-tetikleyiciler (MVP'nin tek gerçek Hold'u) |
| 5 | Diegetic Button (UI-suz Interact) | Input | Asansör çağrı düğmesi |
| 6 | Walk-in Trigger Zone | Input (pasif) | Teslimat bölgesi, Automatic shift bölgesi, AmbientZoneVolume |
| 7 | Non-diegetic Caption (Stinger) | Overlay | Stinger çalarken |
| 8 | Dialogue Subtitle | Overlay | Psikiyatri sahnesi |
| 9 | Movement Lock Feedback | Feedback | Hold, asansör yolculuğu, HARD CUT |
| 10 | Focusable-but-Rejected ("Eller Dolu") | Feedback | Slot dolu pickup denemesi |
| 11 | Diegetic Carry Representation | Data Display | Taşınan eşyalar (HUD sayacı YOK) |

---

## Patterns

### 1. Focus & Prompt (Crosshair)

**Category**: Feedback · **Used In**: her `IInteractable` · **Source**: `etkilesim-sistemi.md`, `art-bible.md` §4.4/§7, ADR-0010

**Description**: Ekran merkezinde sabit, %100 screen-space crosshair (Pillar 1'in tek muafiyeti — asla diegetik değil). SphereCast (0.05m/2.0m) bir `IInteractable`'a odaklanınca Idle→Focused geçişi **yalnızca opacity/scale** ile iletilir (0.65→1.0 opacity, 1.0→1.1 scale; kısa smoothstep, C#'ta — asla USS transition, asla renk/flash). Focused hedefin `PromptText`'i crosshair'in yanında görünür.

**Specification**: Kilitli değerler art-bible §4.4/§7.5'te (1-2px, %40-60 opacity statik dış çizgi dahil). Durum sınıfı yalnızca değişimde toggle edilir. `etkilesim-*` USS öneki (ADR-0002).
**When to Use**: Oyuncunun eliyle yapacağı her nesne etkileşimi.
**When NOT to Use**: Asansör düğmesi (Desen 5), walk-in bölgeler (Desen 6) — bunlar kasıtlı olarak crosshair diline girmez.

### 2. Instant Interact

**Category**: Input · **Used In**: `CarryItemPickup`, decoy'lar · **Source**: `etkilesim-sistemi.md`, ADR-0010/0013

**Description**: Focused hedefte tek Interact basışı → `OnInteract()`. Durum Focused'da kalır. Onay/animasyon beklemesi yok — Pillar 3'ün "eller işini bilir" hissi.
**When to Use**: Tereddütsüz, maliyetsiz fiziksel eylemler (eşya alma, kapı kolu).
**When NOT to Use**: Anlam yüklü, geri alınamaz eylemler → Desen 3/4 (Hold).

### 3. Hold Interact — Default Fill

**Category**: Input · **Used In**: genel Hold hedefleri · **Source**: `etkilesim-sistemi.md` AC14, ADR-0010

**Description**: Interact basılı tutulur; `hold_progress` **kesin lineer** (easing yok) 0→1 dolar. Varsayılan crosshair fill halkası sistemin kendi `t`'sinden sürülür — nesnenin hiçbir şey implemente etmesi gerekmez. Erken bırakma = `OnHoldCancelled`, sıfırdan başlar. İptal kontrolü her zaman progress'ten önce işlenir.
**When to Use**: Süre gerektiren ama dramatik olmayan her Hold.
**When NOT to Use**: Anı-tetikleyiciler — Desen 4.

### 4. Hold Interact — Suppressed Feedback

**Category**: Input · **Used In**: `MemoryTriggerObject` (MVP'nin tek Hold'u) · **Source**: `ani-tetikleyici-etkilesim.md`, `etkilesim-sistemi.md` AC14a, ADR-0014

**Description**: Desen 3'ün opt-out varyantı: `SuppressDefaultHoldFill=true` → Hold sırasında **sıfır görsel geri bildirim**. Gerilim oyuncunun bedeninde kalır ("en küçük bir titreme bile eklense his oyuncunun bedeninden oyunun geri bildirim kanalına taşınır"). Tamamlanınca compound ışık+ses efekti başlar; `OnHoldCancelled` tam no-op — "hiçbir şey olmamıştır".
**When to Use**: Yalnızca anlamın kendisi geri bildirim olan, geri alınamaz, kasıt-yüklü eylemler.
**When NOT to Use**: Her yerde — bu istisnadır, varsayılan Desen 3'tür.

### 5. Diegetic Button (UI-suz Interact)

**Category**: Input · **Used In**: asansör çağrı düğmesi · **Source**: `asansor-kat-erisim-sistemi.md`, ADR-0011

**Description**: `IInteractable`/crosshair/prompt **kullanılmaz**. ~1.5m yarıçaplı proximity trigger içindeyken doğrudan Interact okunur. Düğme "otelin kendi donanımı"dır — oyun-UI'sının aracılık etmediği, dünyaya ait bir mekanizma. Geri bildirim tamamen diegetik: düğme ışığı, kapı animasyonu, kabin sesi.
**When to Use**: Dünyanın kendi altyapısına ait, oyun-mekaniği-dışı hissettirilmek istenen donanım.
**When NOT to Use**: Oyuncunun "eylem" olarak sahiplenmesi gereken her şey (→ Desen 1-4).

### 6. Walk-in Trigger Zone

**Category**: Input (pasif) · **Used In**: `DropOffZone` (teslimat), Automatic `ShiftZone`, `AmbientZoneVolume` · **Source**: ADR-0005/0009/0013

**Description**: Oyuncunun fiziksel varlığı tetikleyicidir — basış yok, prompt yok, UI yok. Teslimat düğmesizdir (bırakma hissi değil, "varış" hissi); Automatic shift bölgesi fark ettirmeden tetiklenir (Pillar 1); ambiyans bölge geçişleri crossfade'le akar.
**Specification**: Hepsi `CompareTag("Player")` kimlik kontrolü + co-residency guard taşır; collider'lar `ElevatorTriggerZoneRelay`/aynı-GameObject kuralına uyar (ADR-0011/0013).
**When NOT to Use**: Oyuncunun bilinçli seçmesi gereken hiçbir şeyde — pasif tetik, kasıt gerektiren eylem için asla.

### 7. Non-diegetic Caption (Stinger)

**Category**: Overlay · **Used In**: stinger çalarken · **Source**: `adaptif-ses-sistemi.md`, ADR-0009 addendum, `accessibility-requirements.md`

**Description**: Stinger klip penceresine senkron, **koşulsuz** gösterilen kapalı altyazı. Metin stili **izlenimci/soyut** — nesne adlandırmaz ("[uzaktan bir uğultu]" gibi; tam stil sözleşmesi `accessibility-requirements.md`'de). Diyalog altyazısından görsel olarak ayırt edilir. `ses-*` USS öneki.
**When to Use**: Yalnızca stinger'lar. **When NOT to Use**: Ambiyans katmanları caption almaz (sürekli ses, ayrık olay değil).

### 8. Dialogue Subtitle

**Category**: Overlay · **Used In**: psikiyatri sahnesi · **Source**: `diyalog-anlati-icerigi-2026-08-02.md`, ADR-0012, `accessibility-requirements.md`

**Description**: Taban diyalog + seçilmiş callback satırlarının alt-orta altyazısı. `#dialogue-subtitle`, `UIRoot.Instance` üzerinden, `diyalog-*` öneki. Satır ilerletme/zamanlama UX'i henüz tasarlanmadı (ADR-0012'nin bilinçli ertelemesi — bkz. Gaps).
**When NOT to Use**: Stinger caption'la aynı öğe/stil asla paylaşılmaz — iki ayrı kanal.

### 9. Movement Lock Feedback

**Category**: Feedback · **Used In**: Hold (MoveOnly), asansör (MoveOnly), HARD CUT (ton'a göre Full/MoveOnly) · **Source**: ADR-0003/0015, `birinci-sahis-kontrolcu.md`

**Description**: Kilit hiçbir zaman UI ile duyurulmaz — oyuncu onu bedeninden anlar. Kural: **MoveOnly = bakış her zaman serbest** (asansörde etrafına bakabilirsin), **Full = yalnızca doygunluk bitişinin "bedenin çalınması" anı**. En-kısıtlayıcı-kazanır; kilidi yalnızca alan sistem bırakır.
**When NOT to Use**: Full kilit, doygunluk HARD CUT'ı dışında hiçbir yerde — o his o âna saklıdır.

### 10. Focusable-but-Rejected ("Eller Dolu")

**Category**: Feedback · **Used In**: slot doluyken pickup · **Source**: `gorev-tasima-dongusu.md` AC3 (revize), ADR-0013

**Description**: Reddedilecek eylem **odaklanabilir kalır** — prompt "Eller Dolu" gösterir, basış sessizce reddedilir, durum değişmez. `CanInteract=false` (görünmez ret) yalnızca gerçekten "yok hükmünde" durumlar içindir (yanlış round, toplanmış, oturum bitti). Ayrım kasıtlı: oyuncuya "yapamazsın çünkü ellerin dolu" söylenir, "bu nesne yok" davranılmaz.
**When to Use**: Oyuncunun anlaması gereken geçici retler. **When NOT to Use**: Kalıcı/anlatısal retler (Committed tetikleyici sessizce emekli olur — Desen 4'ün devamı).

### 11. Diegetic Carry Representation

**Category**: Data Display · **Used In**: taşınan eşyalar · **Source**: `gorev-tasima-dongusu.md` AC14/15/16, ADR-0013

**Description**: Taşınan yük HUD sayacıyla değil, **elde/kucakta görünen havuzlanmış temsillerle** iletilir. Sway FPC'nin faz akümülatöründen (bağımsız timer yasak), jostle sesleri hareketten türer. `Highlight(round)` eğrisi ilerledikçe temsillerin görsel vurgusunu kısar ama okunabilirliği asla bozmaz.
**When to Use**: Oyuncunun taşıdığı/sahip olduğu her şey. **When NOT to Use**: Sayı/liste olarak gösterilecek hiçbir envanter yok — bu oyunda envanter UI'ı ilkesel olarak yasak (art-bible §7).

---

## Gaps & Patterns Needed

| Gap | Needed For | Owner / When |
|---|---|---|
| **Settings/Options paneli deseni** | `accessibility-requirements.md`'nin seçenekleri (motion kaydırıcısı, toggle-hold, hassasiyet, rebind) bir ayarlar yüzeyi gerektiriyor — MVP'de menü yok | Ana Menü/Başlangıç Akışı (Vertical Slice) quick spec'i; MVP demo için minimal panel kararı → Open Questions |
| **Diyalog satır ilerletme deseni** (auto-advance vs basışla) | Psikiyatri sahnesinin oynanabilir olması | ADR-0012'nin bilinçli ertelemesi; implementasyon öncesi küçük UX kararı |
| **Ana menü / pause desenleri** | Vertical Slice | `systems-index.md` #16, VS kapsamı |
| **Gamepad eşleme tablosu** | Kısmi gamepad desteği (tech-prefs) | Interact/hareket eşlemesi Input System action map'te; VS'de doğrulanır |

## Open Questions

1. **player-journey.md yok** — desenlerin duygusal bağlamı GDD Player Fantasy bölümlerinden türetildi; journey haritası yazıldığında bu kütüphane faz bilgisiyle zenginleştirilmeli. (Şablon: `.claude/docs/templates/player-journey.md`.)
2. **MVP demo'da ayarlar yüzeyi**: erişilebilirlik seçenekleri (bkz. Gap 1) demo'ya minimal bir panelle mi girer, yoksa demo öncesi Ana Menü beklenir mi? Sahip: kullanıcı + VS planlaması.
