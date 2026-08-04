# Quick Design Spec: Diyalog/Anlatı İçeriği

**Type**: New Small System
**Scope**: Psikiyatri seansı diyalog içeriğinin, bilinen ipuçlarına göre
hangi callback'lerin gösterileceğine karar veren seçim/gösterim mantığı.
Gerçek diyalog metni (writer içeriği) bu spec'in kapsamında değil.
**Date**: 2026-08-02
**Estimated Implementation**: ~2-3 gün

## Overview

Diyalog/Anlatı İçeriği, her psikiyatri seansı sahnesinde hangi diyalog
içeriğinin oynatılacağına karar verir: her sahnenin sabit bir temel
diyaloğu vardır, buna ek olarak Anlatı Durum/İpucu Takibi'nin bildiği
ipuçlarına göre 0 ya da daha fazla "callback" satırı örülür. Bu sistem
içeriği yazmaz — hangi önceden yazılmış callback'in bu sahnede
kullanılabilir olduğuna karar verir ve Anlatı Durum'un zorunlu kıldığı
tempo sınırını uygular.

## Core Rules

- **Sahne yapısı**: Her psikiyatri seansı sahnesi bir `DialogueSceneConfig`
  ile tanımlanır: sabit temel diyalog + o sahneye özgü, önceden yazılmış
  bir `CallbackPool` (her biri hangi `clueId`'ye bağlı olduğunu taşıyan
  callback satırları listesi).
- **Seçim mantığı**: Sahne başladığında, `CallbackPool`'daki her callback
  için `IsClueKnown(clueId)` sorgulanır. Bilinen `clueId`'lere sahip
  callback'ler aday listesine girer.
- **Tempo sınırı (Anlatı Durum GDD'sinin zorunlu gereksinimi)**: Aday
  listesi `MaxCallbacksPerScene` (Tuning Knob) değerini aşarsa, sadece
  yazar tarafından atanmış `Priority` sırasına göre ilk N tanesi
  kullanılır — geri kalanı bu sahnede atlanır (silinmez, sonraki bir
  sahnede tekrar aday olabilir, bkz. Full Vision notu).
- **Tekrar-önleme (bu sistemin kendi bookkeeping'i)**: Kullanılan her
  callback, `HashSet<string> UsedCallbackIds` içinde işaretlenir — Anlatı
  Durum'un `KnownClueIds`'inden ayrı, bu sistemin kendi "bunu zaten
  oynattım mı" durumudur (Anlatı Durum GDD'sinin Core Rules'ında
  öngörülen ayrım). Bir kez kullanılan callback bir daha aday listesine
  girmez.
- **Sıfır-ipucu/eksik-ipucu durumu**: Hiçbir callback koşulu
  sağlanmazsa, sahne sadece temel diyalogla oynar — hata yok, eksik
  içerik olarak işaretlenmez (Anlatı Durum'un "eksik ipucu beklenen
  durumdur" notuyla tutarlı).
- **MVP kapsam notu, düzeltildi (design-review, 2026-08-03 —
  `/review-all-gdds` bulgusu, kritik bulgu)**: Önceki taslak "MVP'de tek
  gece, tek sahne, 2-3 toplam ipucu var — `MaxCallbacksPerScene` pratikte
  MVP'yi kısıtlamaz (aday sayısı zaten kapasitenin altında)" diyordu —
  bu **yanlıştı**: `game-concept.md`'nin MVP içerik üst sınırı 3
  tetikleyicidir, önceki varsayılan `MaxCallbacksPerScene=2`'nin
  üzerindedir, ve MVP'de tek psikiyatri sahnesi olduğu için ("atlanan
  aday sonraki bir sahnede tekrar aday olabilir" kuralının hiç geçerli
  olamayacağı bir yapılandırma) bu, en çok ipucu bulan — projenin en
  ödüllendirmek istediği — oyuncunun 3. callback'ini kalıcı olarak
  hiç görmemesi anlamına gelirdi. Düzeltme: varsayılan
  `MaxCallbacksPerScene`, MVP'nin üst sınırına (3) yükseltildi (bkz.
  Tuning Knobs). Mekanizmanın kendisi Full Vision'ın 4-5 sahne/15-20
  ipucusuna hazırlık için kalıyor (maliyeti neredeyse sıfır).
- **Build-time tutarlılık kontrolü (design-review, 2026-08-03 —
  eklendi)**: Eğer bir gece için yalnızca **tek** psikiyatri sahnesi
  yapılandırılmışsa (MVP'nin durumu), `MaxCallbacksPerScene`, o gece
  için yapılandırılmış toplam ipucu sayısından **küçük olamaz** — aksi
  halde en az bir callback hiçbir zaman gösterilme şansı bulamaz
  (tek sahne = "sonraki sahnede tekrar aday olma" kaçış yolu yok).
  Bu, `IPreprocessBuildWithReport` tabanlı bir edit-time kontrolle
  zorlanır (bu projenin diğer GDD'lerinde zaten kurulu desenle aynı —
  bkz. `ani-tetikleyici-etkilesim.md` Core Rules).

## Dependencies

*(design-review, 2026-08-04 — verification bulgusu, eklendi: bu Quick
Spec önceden bir Dependencies bölümü hiç taşımıyordu, iki başka
dokümanda dependent olarak listeleniyordu ama kendisi bunu hiç
doğrulamıyordu.)*

**Bağımlıdır**:
- **Anlatı Durum/İpucu Takibi** — `IsClueKnown(clueId)` sorgusunu her
  `CallbackPool` girdisi için çağırır (bkz. Core Rules, "Seçim mantığı")

**Kendisine bağımlı olanlar**:
- Yok — bu sistem sadece psikiyatri seansı sahnesinin kendi diyalog
  seçim mantığıdır, hiçbir sistem buna bağımlı değil (Sahne Kesmeli
  Anlatı sahnenin yüklü olmasını garanti eder ama bu sistemin
  API'sine hiç çağrı yapmaz).

## Open Questions

*(design-review, 2026-08-04 — verification bulgusu, eklendi: bu Quick
Spec önceden bir Open Questions bölümü hiç taşımıyordu, ama
`anlati-durum-ipucu-takibi.md`'nin kendi Open Questions'ı bu dosyaya
bir madde miras bırakmıştı.)*

- **`UsedCallbackIds`'in kalıcılık planı yok**: `Anlatı Durum/İpucu
  Takibi`'nin `KnownClueIds`/`SeenShiftIds`'i Çoklu Gece İlerlemesi
  geldiğinde geceler arası kalıcı olacak şekilde tasarlanmış, ama bu
  sistemin kendi `UsedCallbackIds`'i için hiçbir kalıcılık planı yok —
  eğer bir ipucu geceler arası "Known" kalırken "zaten oynatıldı"
  bilgisi her gece sıfırlanırsa, aynı callback satırı ikinci bir gecede
  birebir tekrar edebilir (bkz. `anlati-durum-ipucu-takibi.md` Open
  Questions, "Sahip düzeltmesi" — sahip bu dokümana zaten atanmış).
  MVP'de tek gece olduğu için bloklamıyor. **Owner**: Çoklu Gece
  İlerlemesi tasarımı (Vertical Slice), henüz çözülmedi.

## Tuning Knobs

| Knob | Default | Range | Category | Rationale |
|------|---------|-------|----------|-----------|
| MaxCallbacksPerScene | 3 (design-review 2026-08-03 — düzeltildi, önceki 2'ydi ve MVP'nin 3-ipucu üst sınırıyla çelişiyordu) | 1–4 | Tempo | MVP'nin tek sahnesinde tüm yapılandırılmış ipuçlarını (üst sınır 3) barındırabilmeli — build-time kontrolle zorlanır (bkz. Core Rules); Full Vision'da sahne başına birikimi kontrol eder |

## Acceptance Criteria

- [ ] GIVEN hiçbir ipucu bilinmiyor, WHEN sahne oynar, THEN sadece temel
      diyalog gösterilir, hata/eksik-içerik işareti yok
- [ ] GIVEN aday callback sayısı `MaxCallbacksPerScene`'i aşıyor, WHEN
      sahne callback seçer, THEN sadece `Priority` sırasına göre ilk N
      tanesi kullanılır, geri kalanı atlanır (silinmez)
- [ ] GIVEN bir callback daha önce kullanıldı, WHEN aynı `clueId` hâlâ
      bilinir durumda ve sahne tekrar değerlendirilir (Full Vision
      çoklu-gece senaryosu), THEN o callback tekrar aday olmaz
- [ ] **[design-review, 2026-08-03 — düzeltildi: önceki hali yanlış bir
      iddiayı (varsayılan 2, MVP'yi kısıtlamaz) test ediyordu]** GIVEN
      MVP'nin üst sınırı 3 toplam ipucu VE tek psikiyatri sahnesi, WHEN
      varsayılan `MaxCallbacksPerScene=3` uygulanır, THEN 3 ipucunun
      hepsi aynı sahnede aday olabilir — hiçbiri yapısal olarak
      kalıcı-erişilemez kalmaz
- [ ] **[design-review, 2026-08-03 — eklendi]** GIVEN tek sahne
      yapılandırılmış bir gece için `MaxCallbacksPerScene`, o gece için
      yapılandırılmış toplam ipucu sayısından küçük ayarlanır, WHEN
      build-time tutarlılık kontrolü çalışır, THEN hata verilir, build
      engellenir

## Systems Index

Bu sistem zaten `design/gdd/systems-index.md`'de #9 olarak kayıtlı
(Narrative, MVP, Quick Spec). Bu spec tamamlanınca durumu "Designed"
olarak güncellenecek.
