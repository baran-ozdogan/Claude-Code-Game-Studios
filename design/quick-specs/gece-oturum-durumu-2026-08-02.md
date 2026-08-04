# Quick Design Spec: Gece/Oturum Durumu

**Type**: New Small System
**Scope**: Gece/oturum bookkeeping'i (hangi gece, hangi tetikleyiciler ateşlendi, hangi shift'ler kalıcı) — anlatı bayrağı semantiği taşımaz (o Anlatı Durum'un işi).
**Date**: 2026-08-02
**Estimated Implementation**: ~1-2 gün

## Overview

Gece/Oturum Durumu, bir "gece" oturumunun ne zaman aktif olduğunu ve o oturum
içinde hangi anı-tetikleyicilerin ateşlendiğini/kalıcı hale geldiğini izleyen
saf bir bookkeeping servisidir. Diğer sistemler (Asansör, Görev Döngüsü,
Işık/Volume) bu duruma sorgu atar ya da bildirim gönderir; bu sistem kendisi
hiçbir karar vermez.

## Core Rules

- **In-memory tekil servis** — sahne yüklemeleri arasında hayatta kalır,
  diske yazmaz (disk kalıcılığı Vertical Slice-tier Çoklu Gece İlerlemesi
  sisteminin işi, veri modeli değişmeden genişletilecek).
- Tuttuğu veri:
  - `bool IsSessionActive` — gece başında `true`, psikiyatri kesme sahnesi
    başlayınca `false`
  - `int CurrentNightNumber` — MVP'de sabit `1`
  - `HashSet<string> FiredTriggerIds` — bu oturumda ateşlenen `shiftId`'ler
    (sadece "ateşlendi mi" — hangi ipucunun anlatı olarak "bilindiği"
    burada tutulmaz)
  - `Dictionary<string,bool> PersistentShiftIds` — bir shift
    `Persistent=true` ile Held-Persistent olduğunda kaydedilir
- **Asansör sorgusu**: Asansör sistemi sadece `IsSessionActive`'i okur,
  kendi kullanılabilirlik mantığını üzerine kurar — bu sistem karar vermez.
- **Işık/Volume kalıcılık sorusu çözüldü**: MVP'de tek gece = tek oturum
  olduğu için "önceki oturumdan kalıcı shift" senaryosu oluşmaz; gerçek
  senaryo aynı oturum içinde bir sahnenin yeniden yüklenmesidir (asansörle
  kat değişimi). Basit in-memory dictionary yeterli, serileştirme gerekmez.
- **`PersistentShiftIds`'in yazarı ve zamanlaması netleştirildi (design-review,
  2026-08-03 — `/review-all-gdds` bulgusu, eklendi, kritik bulgu)**: Önceki
  taslak bu alanı Işık/Volume Durum Sistemi'nin `OnShiftStateChanged`'ine
  abone olarak dolduruyormuş gibi ima ediyordu (bkz. aşağıdaki AC), ama bu
  sistem hiçbir Dependencies bölümü taşımıyordu ve Işık/Volume kendi
  GDD'sinde açıkça "bu bilgiyi sadece okur, kendisi yazmaz" diyordu —
  sonuç: hiçbir sistem `PersistentShiftIds`'i fiilen yazmıyordu.
  Düzeltme: **bu sistem** `OnShiftStateChanged(shiftId, newState,
  zoneCenter, radius)`'a doğrudan abone olur (bkz. Dependencies, yeni
  eklendi) ve `newState == Shifting-In` olduğunda Işık/Volume'un
  `IsShiftPersistent(shiftId)` sorgusunu çağırıp `true` dönerse
  `PersistentShiftIds[shiftId]=true`'yu kaydeder — **`Held`'i beklemeden,
  `Shifting-In`'e girer girmez**. **Düzeltme notu (design-review,
  2026-08-03 — verification N2 bulgusu)**: Bu satır önceden "`config.Persistent`"
  diye bir alandan okuyormuş gibi yazılıydı, ama `OnShiftStateChanged`
  event'i hiçbir zaman `config`/`Persistent` taşımadı (event imzasına
  bakınız — sadece `shiftId, newState, zoneCenter, radius`) — bu sistem
  kendi atandığı Core Rule'u yapısal olarak yerine getiremiyordu. Şimdi
  Işık/Volume'un yeni `IsShiftPersistent(shiftId)` sorgusunu (bkz.
  `isik-volume-durum-sistemi.md` Interactions with Other Systems)
  event'i aldığı aynı karede çağırarak okur. Işık/Volume'un kendi
  garantisi gereği bir Persistent shift başladıktan sonra her zaman
  Held'e ulaşır (asla geri dönmez), bu yüzden `Shifting-In`'de
  işaretlemek güvenlidir ve önceki ~3 saniyelik boşluğu kapatır. Bu
  zamanlama, Anı-Tetikleyici Etkileşim'in kendi `FiredTriggerIds`
  yazma anıyla (`OnHoldComplete()`, ki bu da `Shifting-In`'i
  tetikleyen aynı `TriggerShift` çağrısıyla aynı karede olur) artık
  hizalıdır — iki kayıt da pratikte aynı karede yazılır, sahne
  yeniden yüklemesi hangi anda gerçekleşirse gerçekleşsin "yarım"
  bir durum (biri Committed, diğeri Dormant) artık oluşamaz.
- **Oturum sonlandırma (public API) — design-review, 2026-08-02, Sahne
  Kesmeli Anlatı quick-spec'inden eklendi**: `void EndSession()` —
  `IsSessionActive`'i `false` yapar. Sadece Sahne Kesmeli Anlatı çağırır
  (`RequestHardCut`'ın `onComplete`'i içinde, psikiyatri kesme sahnesi
  başlarken). Tersi yön (`true`'ya döndürme) MVP'de yok — tek gece,
  oturum bir kez başlar bir kez biter.
- **`event Action<string> OnTriggerFired` (design-review, 2026-08-03 —
  verification N5 bulgusu, eklendi)**: Bir `shiftId`, Anı-Tetikleyici
  Etkileşim'in `OnHoldComplete()`'i tarafından `FiredTriggerIds`'e
  eklendiği **aynı karede** fırlar (parametre eklenen `shiftId`) — yani
  `Shifting-In`'e girildiği karede, `Held`'e ulaşmadan çok önce (bkz.
  aşağıdaki `OnTriggerSettled` ile farkı). Bu sistem daha önce hiçbir
  event fırlatmıyordu — sadece pasif bir sorgu/yazma hedefiydi.
  **Artık Sahne Kesmeli Anlatı'nın doygunluk koşulunu tetiklemek için
  kullanılmıyor** — bkz. aşağıdaki düzeltme notu. `FiredTriggerIds`/
  `OnTriggerFired`'in kendisi hâlâ var ve anlamlı (Anı-Tetikleyici
  Etkileşim'in Committed-restore sorgusu buna dayanır, bkz. o GDD'nin
  Core Rules'ı) — sadece gece-sonu doygunluk sinyali olarak artık
  kullanılmıyor.
- **`HashSet<string> SettledTriggerIds` + `event Action<string> OnTriggerSettled`
  (design-review, 2026-08-04 — full re-verification bulgusu, eklendi,
  en kritik bulgu, saturation-timing sorununu çözer)**: `FiredTriggerIds`/
  `OnTriggerFired`, bir tetikleyicinin Hold'u tamamlandığı anı (`Shifting-In`
  girişi) işaretler — ışığın ~3sn'lik rampası ve stinger'ın çalması henüz
  yeni başlamışken. `Sahne Kesmeli Anlatı`'nın doygunluk koşulu önceden
  bu erken sinyali kullanıyordu, bu da aynı karede `RequestHardCut`
  tetiklenmesine ve ışık+ses bileşik etkisinin, ilgili ipucunun (`Anlatı
  Durum` sadece `Held`'i işler) ve dolayısıyla o gecenin en son
  callback'inin **her zaman** kesilmesine yol açıyordu (bkz.
  `gdd-cross-review-2026-08-04-verification.md`, en ciddi bulgu — üç
  bağımsız inceleme yönteminin de aynı sonuca vardığı tek madde).
  **Düzeltme**: bu sistem, zaten abone olduğu `OnShiftStateChanged`
  event'ini artık `Shifting-In` (Persistent tespiti için, değişmedi)
  YANINDA `newState == Held` için de işler — bir `shiftId`,
  `FiredTriggerIds`'te olduğu VE `Held`'e ulaştığı karede
  `SettledTriggerIds`'e eklenir ve `OnTriggerSettled(shiftId)` fırlar.
  `MemoryTriggerDef`-bağlı her shift zaten `Persistent=true` olduğundan
  (bkz. `ani-tetikleyici-etkilesim.md` Core Rules), Işık/Volume'un kendi
  garantisi gereği bu `Held`'e her zaman ulaşır — yani `SettledTriggerIds`
  her zaman `FiredTriggerIds`'e ~3sn gecikmeyle "yetişir", hiçbir zaman
  kalıcı olarak geride kalmaz. Ek abone gerekmez — mevcut `OnShiftStateChanged`
  aboneliğinin aynı handler'ına bir `else if (newState == Held)` dalı
  eklemek yeterlidir.

## Dependencies

*(design-review, 2026-08-03 — `/review-all-gdds` bulgusu, eklendi: bu
Quick Spec önceden bir Dependencies bölümü hiç taşımıyordu.)*

**Bağımlıdır**:
- **Işık/Volume Durum Sistemi** — `OnShiftStateChanged` event'ine abone
  olur (`Persistent=true` olan shift'ler için `Shifting-In`'de
  `PersistentShiftIds`'i doldurmak üzere — bkz. Core Rules); **artık
  `IsShiftPersistent(shiftId)` sorgusunu da çağırıyor** (design-review,
  2026-08-03 — verification N2 bulgusuyla eklendi — event'in kendisi
  `Persistent`'i taşımadığı için, bkz. Core Rules'taki düzeltme notu).
  Bu, aşağıdaki "hiç geri çağrı yapmaz" cümlesini artık geçersiz kılıyor —
  önceki taslakta doğruydu, N2 düzeltmesiyle **tek yönlü bir salt-okunur
  sorguya** genişledi (event aboneliği hâlâ tek yönlü; `IsShiftPersistent`
  ayrı, senkron bir sorgu çağrısı, event değil) — Foundation katmanı içi
  bağımlılık sınıflandırması değişmedi (`isik-volume-durum-sistemi.md`'nin
  kendi Gece/Oturum Durumu'na olan kısmi bağımlılığıyla aynı yönde, aynı
  katmanda — bkz. `systems-index.md`'nin "Foundation Layer" tanımı,
  intra-layer bağımlılıklar katman ihlali sayılmaz).

**Kendisine bağımlı olanlar**:
- **Anı-Tetikleyici Etkileşim** — `FiredTriggerIds`'e yazar
  (`OnHoldComplete()`'te), `Awake()`'te okur (Committed-restore)
- **Işık/Volume Durum Sistemi** — `PersistentShiftIds`'i okur (sahne
  yeniden yüklemede Persistent-restore için)
- **Asansör/Kat-Erişim Sistemi** — `IsSessionActive`'i okur
- **Görev/Taşıma Döngüsü** (design-review, 2026-08-04 — verification
  bulgusu, eklendi — tek yönlü bağımlılık boşluğu kapatıldı) —
  `IsSessionActive`'i okur (guard); kendi round/slot state'i de aynı
  in-memory-kalıcı desende tutulur
- **Sahne Kesmeli Anlatı** — `EndSession()`'ı çağırır; `OnTriggerSettled`
  event'ine ve `SettledTriggerIds.Count` sorgusuna abone olur (design-review,
  2026-08-04 — full re-verification bulgusuyla `OnTriggerFired`/
  `FiredTriggerIds`'den değiştirildi, saturation-timing bulgusunu çözer —
  bkz. Core Rules)

## Tuning Knobs

Yok — saf bookkeeping sistemi, "feel" parametresi taşımıyor.

## Acceptance Criteria

- [ ] GIVEN sistem başlatılmamış, WHEN gece sahnesi yüklenir, THEN
      `IsSessionActive=true`, `CurrentNightNumber=1`
- [ ] GIVEN `IsSessionActive=true`, WHEN psikiyatri kesme sahnesi başlar,
      THEN `IsSessionActive=false` olur
- [ ] GIVEN bir shift `config.Persistent=true` ile `TriggerShift`
      çağrılır, WHEN `OnShiftStateChanged(shiftId, Shifting-In, ...)`
      fırlar (`Held` değil — bkz. Core Rules'taki zamanlama düzeltmesi,
      design-review 2026-08-03), THEN bu sistem aynı karede
      `IsShiftPersistent(shiftId)` sorgusunu çağırır (design-review,
      2026-08-03 — verification N2 bulgusu, eklendi: event `Persistent`'i
      taşımadığı için bu sorgu zorunlu), `true` döner, ve
      `PersistentShiftIds[shiftId]=true` **hemen** kaydedilir, `Held`'e
      ulaşılmasını beklemez
- [ ] **[design-review, 2026-08-03 — verification N2 bulgusu, eklendi]**
      GIVEN bir shift `config.Persistent=false` (ya da hiç belirtilmemiş)
      ile `TriggerShift` çağrılır, WHEN `OnShiftStateChanged(shiftId,
      Shifting-In, ...)` fırlar, THEN `IsShiftPersistent(shiftId)` `false`
      döner, `PersistentShiftIds`'e hiçbir giriş eklenmez
- [ ] GIVEN `PersistentShiftIds[shiftId]=true` aynı oturumda, WHEN o sahne
      yeniden yüklenir, THEN Işık/Volume sistemi bunu sorgulayıp bölgeyi
      doğrudan Shifted/Held-Persistent'e başlatır (isik-volume-durum-sistemi.md
      Acceptance Criteria **#17**'yi kapatır — design-review 2026-08-03:
      önceki hali yanlışlıkla #14'ü (guard-rail clamp testi) referans
      veriyordu)
- [ ] GIVEN bir `shiftId` ateşlenmemiş, WHEN Anı-Tetikleyici Etkileşim onu
      ateşler, THEN `FiredTriggerIds`'e eklenir
- [ ] **[design-review, 2026-08-03 — verification N5 bulgusu, eklendi]**
      GIVEN yukarıdaki koşul, WHEN `FiredTriggerIds`'e ekleme yapılır,
      THEN `OnTriggerFired(shiftId)` aynı karede tam olarak bir kez
      fırlar
- [ ] **[design-review, 2026-08-04 — full re-verification bulgusu,
      eklendi, saturation-timing düzeltmesinin çekirdek testi]** GIVEN
      bir `shiftId` `FiredTriggerIds`'te (Hold tamamlanmış, `Shifting-In`'e
      girmiş), WHEN Işık/Volume'un `OnShiftStateChanged(shiftId, Held, ...)`'i
      fırlar (~3sn sonra, ışığın rampası tamamlanınca), THEN aynı karede
      `SettledTriggerIds`'e eklenir VE `OnTriggerSettled(shiftId)` tam
      olarak bir kez fırlar — Sahne Kesmeli Anlatı'nın doygunluk koşulunu
      polling olmadan, VE ışık+ses bileşik etkisi tamamlandıktan sonra
      yeniden değerlendirebilmesi bu event'e bağlıdır (bkz. Core Rules)
- [ ] **[design-review, 2026-08-04 — full re-verification bulgusu,
      eklendi]** GIVEN bir `shiftId` henüz `Held`'e ulaşmamış (hâlâ
      `Shifting-In`'de), WHEN `FiredTriggerIds.Count` ile
      `SettledTriggerIds.Count` karşılaştırılır, THEN
      `SettledTriggerIds.Count < FiredTriggerIds.Count` olabilir (geçici
      bir gecikme penceresi, hata değil) — doygunluk kontrolü her zaman
      `SettledTriggerIds.Count`'u kullanmalı, `FiredTriggerIds.Count`'u
      değil
- [ ] GIVEN oyun kapanıp yeniden açılır, WHEN sistem başlatılır, THEN tüm
      state sıfırdan başlar — MVP'de diskten yükleme yok

## Systems Index

Bu sistem zaten `design/gdd/systems-index.md`'de #4 olarak kayıtlı
(Foundation, MVP, Quick Spec). Bu spec tamamlanınca durumu "Designed"
olarak güncellenecek.
