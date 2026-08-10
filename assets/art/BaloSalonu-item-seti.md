# Balo Salonu — Taşıma Döngüsü Item Seti

Kaynak: `assets/art/BaloSalonu.blend`, **`CarryItems`** koleksiyonu (salon hacminin
dışında, y = −6 hattında sıralı; export'ta kendi FBX'ine ayrılır).

Set, kullanıcının gerçek otel balo salonu referans fotoğraflarından çıkarıldı:
**hazırlık hâli** (salonun kurulu geometrisi) ve **giydirilmiş etkinlik hâli**
(bu item'ların yerleşmiş hâli). Yani her item, oyuncunun taşıyıp kuracağı gerçek
bir eşyanın karşılığı — icat edilmiş dolgu prop değil.

Sistem spec'i: `design/gdd/gorev-tasima-dongusu.md`.
Sanat spec'i: `design/art/art-bible.md` Bölüm 2.1, **3.2** (kavis istisnası),
6.3, **8.6** (taşınan eşya poligon bandı), 8.11.

---

## Round Atamaları

GDD'nin kuralı: **her eşya 1 slot**, ağırlık/boyut karmaşıklığı yok; bir `CarryRound`
tek bir tam yük gezisidir. Aşağıdaki **4 round × 3 eşya** önerisi, GDD'nin `N slot`
(2–4) ve round sayısı (3–5) Tuning Knob aralıklarının içinde kalır — kesin değerler
tasarım tarafının kararıdır, bu tablo asset karşılıklarını verir.

Gruplama gerçek etkinlik kurulum sırasını izler (Pillar 3, Görev Gerçekliği):
iskelet → tekstil → sofra → dekor. Bir round içindeki üç eşya aynı işin parçası
olduğu için oyuncu ne taşıdığını sormaz.

| # | Mesh | Boyut (m) | Tri | Materyal | Round |
|---|---|---|---|---|---|
| 01 | `CI_01_MasaTablasi` | 1.80 × 1.80 × 0.03 | 68 | `Ball_MasaUst` | **R1 — İskelet** |
| 02 | `CI_02_AyakTakimi` | 1.24 × 0.22 × 0.20 | 96 | `Ball_MasaAyak`, `CI_Kayis` | R1 |
| 03 | `CI_03_SandalyeDeste` (3 hayalet sandalye) | 0.42 × 0.36 × 1.11 | 2.916 | `Chair_Clear` | R1 |
| 04 | `CI_04_OrtuTopu` (siyah + beyaz katlı) | 0.64 × 0.44 × 0.30 | 48 | `CI_KumasSiyah`, `CI_KumasBeyaz` | **R2 — Tekstil** |
| 05 | `CI_05_PecetheKolisi` (mor peçete) | 0.48 × 0.36 × 0.26 | 120 | `CI_Karton`, `CI_PecetheMor` | R2 |
| 06 | `CI_06_KusakPaketi` | 0.54 × 0.32 × 0.20 | 48 | `CI_KumasBeyaz`, `CI_Cam` | R2 |
| 07 | `CI_07_TabakIstifi` (12 cam charger) | 0.35 × 0.35 × 0.31 | 1.108 | `CI_CamTabak`, `CI_Kece` | **R3 — Sofra** |
| 08 | `CI_08_Samdan` (5 kollu) | 0.57 × 0.57 × 0.84 | 588 | `CI_Gumus`, `CI_MumBeyaz` | R3 |
| 09 | `CI_09_FanusKasasi` (4 cam fanus) | 0.64 × 0.64 × 0.36 | 132 | `CI_AhsapKasa`, `CI_Cam` | R3 |
| 10 | `CI_10_CicekAranjman` (ortanca) | 0.59 × 0.61 × 0.64 | 2.746 | `CI_VazoCam`, `CI_CicekPembe`, `CI_CicekMor`, `CI_Yaprak` | **R4 — Dekor** |
| 11 | `CI_11_AvizeKasasi` | 0.92 × 0.70 × 0.62 | 120 | `CI_AhsapKasa`, `CI_Metal` | R4 |
| 12 | `CI_12_VinilRulo` (baskılı pist) | 2.20 × 0.41 × 0.41 | 116 | `CI_Vinil`, `CI_Kayis` | R4 |

**Toplam 8.106 tri.** Art bible 8.6'nın taşınan-eşya bandı kol/el rigi ile birlikte
6.000–10.000 tri; sahnede aynı anda yalnızca **N adet** (2–4) taşınıyor, dolayısıyla
en ağır kombinasyon bile (03 + 10 = 5.662) bandın içinde kalır. En pahalı iki eşya
(sandalye destesi, çiçek aranjmanı) her zaman ekranın ön planında görülecekleri için
bu maliyeti hak ediyor (8.3'ün "en yüksek öncelik" sınıfı).

### Kavis istisnası (art bible 3.2)
Mimarlık köşeli kalır; **kavis yalnızca taşınan/depolanan lüks eşyaya aittir.**
Bu setteki kavisli parçalar tam olarak o kuralın karşılığıdır: yuvarlak masa tablası,
sandalye kemerli sırtı, şamdan kolları, çiçek kütlesi, cam fanus, vinil rulosu.
Kasa/koli/paket parçaları köşeli kalır — çünkü onlar *ambalajdır*, eşyanın kendisi
değil. **11 numara (`CI_11_AvizeKasasi`) art bible'ın adıyla andığı "avize sandığı"dır.**

---

## Unity Eşleştirmesi

Her item için bir `CarryItemDef` (ScriptableObject) beklenir. Asset tarafının verdiği alanlar:

| CarryItemDef alanı | Kaynak |
|---|---|
| Mesh | Yukarıdaki tablodaki mesh adı (FBX'ten) |
| Materyal(ler) | Tablodaki materyal listesi — URP Lit |
| Taşınan temsil | **Aynı mesh**; GDD "eşyanın kendi küçük temsili" diyor — ayrı bir low-poly varyant gerekmiyor, bu tri bütçeleri zaten ön plan için ayarlandı |
| Stabil item-id | Depodaki sabit spawn noktası (GDD Overview) — mesh adı id olarak kullanılabilir |

- `CarryItemPickup : IInteractable`, `Type=Instant` — GDD Core Rules
- Şeffaf materyaller (`Chair_Clear`, `CI_Cam`, `CI_CamTabak`, `CI_VazoCam`) URP'de
  **Transparent** surface type ister. Taşınırken ekranın ön planında olduklarından
  overdraw kontrollü: aynı anda en fazla N adet sahnede.
- `CI_08_Samdan` ve `CI_10_CicekAranjman` dışındaki hiçbir item emissive taşımaz.
  Mum alevleri **yanmıyor** — hazırlık hâli, henüz kimse yakmadı.

### Spawn / teslim
- **Spawn**: depo tarafında, her item'a sabit bir nokta (`SM_Depo.fbx` sahnesi).
  Bu dosya spawn noktalarını içermez — depo sahnesinin sorumluluğu.
- **Teslim**: balo salonunda `Ball_Araba_*` (platform arabası) + `Ball_TeslimBant_*`
  (zemin bandı) ikilisi drop-off bölgesini görsel olarak işaretler.
  Merkez: **(2.10, 8.00)**, bant alanı x 1.20–3.40 / y 6.70–9.30.
  Servis kapısının (batı duvarı, y 6.60–9.40) hemen içinde — oyuncu asansörden
  gelip ilk bunu görür.

---

## Hedef Durum: Kurulu Örnek Masa

Salonda **(5.20, 11.10)** konumunda tek bir masa tam kurulu bırakıldı
(`Ball_Ornek_*` objeleri): siyah yer-boyu örtü, 10 hayalet sandalye, 10 cam charger
+ mor peçete, gümüş şamdan, ortanca aranjmanı, üç cam fanus.

Gerekçesi ikili: **(a)** gerçek etkinlik ekipleri kuruluma hep bir "show table" ile
başlar — Pillar 3; **(b)** oyuncuya neyi kuracağını **metinsiz** gösterir, art bible
6.4'ün "metinsiz, vurgusuz" çevresel anlatı ilkesine uyar. Bir UI ipucu ya da
tutorial metni gerekmez.

Referans görüntü: `assets/art/BaloSalonu_ornekmasa_ref.png`.

---

## Lisans / Atıf
- Tüm item mesh'leri bu proje için sıfırdan modellendi
- PolyHaven dokuları (`dark_wooden_planks`, `dirty_carpet`): **CC0** — atıf gerekmez
- Halının altın leke deseni prosedürel, bu proje için yazıldı
- Sketchfab / Hyper3D varlığı kullanılmadı — credits'e ekleme gerekmiyor
