# Balo Salonu — Unity Kurulum Notu

Kaynak: `assets/art/BaloSalonu.blend`. Metre ölçekli, Y-up, **texture'lar FBX içine gömülü**.
Referans görüntüler: `assets/art/BaloSalonu_ref.png` (hazırlık), `BaloSalonu_kurulu_ref.png`
(giydirilmiş), `BaloSalonu_ornekmasa_ref.png` (örnek masa).
Kullanıcının gerçek otel balo salonu fotoğraflarından modellendi.

Sanat spec'i: `design/art/art-bible.md` Bölüm 2.1, 3.2, **4.3**, 6.3, 6.4, 8.6–8.11.
Sistem spec'leri: `design/gdd/gorev-tasima-dongusu.md`, `seviye-sahne-gecisi.md`,
`asansor-kat-erisim-sistemi.md`, `adaptif-ses-sistemi.md`.
Item seti: **`assets/art/BaloSalonu-item-seti.md`**.

## Dosyalar

| FBX | Mesh | Tri | İçerik |
|---|---|---|---|
| `SM_BaloSalonu.fbx` | 330 | ~122k | **Oyunun gerçek hâli** — kabuk + hazırlık prop'ları (10.8 MB) |
| `SM_BaloSalonu_Kurulu.fbx` | 280 | ~238k | Giydirilmiş etkinlik hâli — **referans/hedef**, sahneye konmaz (2.28 MB) |
| `SM_BaloItemler.fbx` | 14 | ~9k | 12 taşıma eşyası (`CarryItemDef` mesh'leri, 0.64 MB) |

> `SM_BaloSalonu_Kurulu.fbx` oyunun akışında **kullanılmaz**. Oyuncu geceyi kurulum
> yaparak geçirir ama GDD'de fiziksel montaj simülasyonu yok — teslim, eşyayı kaldırır.
> Bu dosya tasarım referansı ve olası bir "sonraki gece" durumu için duruyor.

## Ölçüler ve Yerleşim

| | |
|---|---|
| Oda | 24.0 (X) × 16.0 (Z) m, köşe orijin — pozitif çeyrekte |
| Tavan | Kiriş altı **5.40 m**, kaset gözü tavanı 5.75, döşeme üstü 5.90 |
| Kaset ızgarası | 6 × 4 göz, göz 3.20 × 3.20, kiriş bandı 0.686 (X) / 0.640 (Y) |
| Truss kirişleri | Kesit 0.34, alt kot **3.95** — X boyunca y = 4.4 / 8.0 / 11.6; Y boyunca x = 6 / 12 / 18 |
| Dans pisti | x 7.40–14.60, z(Unity) 4.00–11.20, üst kot 0.035 |
| Sahne | x 18.60–23.90, z(Unity) 3.60–12.40, üst kot **0.42** |
| Servis kapısı | Batı duvarı (x=0), z(Unity) 6.60–9.40, yükseklik 2.42 |
| Teslimat noktası | Araba merkezi **(2.10, 8.00)**, zemin bandı x 1.20–3.40 / z 6.70–9.30 |
| Örnek (show) masa | **(5.20, 11.10)** |

Blender koordinatı (x, y, z) → Unity (x, z, y). Zemin yüzeyi Unity Y = 0.

## FBX Import
- Scale Factor 1, Convert Units açık
- **Materials → Extract Textures + Extract Materials**
- Mesh Compression: **Medium**; Read/Write **kapalı** (art bible 8.9)
- Generate Colliders **kapalı** — elle kur (aşağıya bak)
- **UV**: `UVMap` (dünya ölçekli kutu projeksiyonu, 1 UV birimi = 1 m) +
  **`UV2` lightmap kanalı** 201 benzersiz mesh'in hepsinde açık ve çakışmasız
  (`lightmap_pack`). Unity'de "Generate Lightmap UVs" **kapalı** kalsın — kanal hazır.
- N-gon yok, tangent space export edildi
- Lightmap texel yoğunluğu hedefi: **~12 texel/m** (art bible 8.7, balo salonu hazırlık)

## Işık — Hazırlık Aydınlatması (art bible 4.3: `#FFD9A8`)

Işıklar FBX'te **yok**, Unity'de kurulacak. Blender'daki kurulum:

| Işık | Tip | Adet | Renk | Konum |
|---|---|---|---|---|
| Kaset gözü paneli | Area Square **2.7 m, 150 W** | **14** | `#FFD9A8` | Göz merkezleri, Unity Y = **5.68** |
| Altın kristal avize | Area Square **2.6 m, 340 W** | **2** | `#FFC98A` | (10.06, 6.08) ve (13.94, 6.08), Y = **5.50** |

- URP'de realtime area light yok → **aşağı bakan Spot** ya da baked Area kullan.
- **Doğu ucundaki 8 göz bilinçli olarak sönük** (kaset indeksi i = 4 ve 5, x > 16).
  Art bible 2.1: *"sadece çalışılan hacimler aydınlık"*. Sahne tarafı karanlıkta kalır —
  bu bir eksiklik değil, kural. Panel mesh'leri orada `Ball_TavanPanelKapali` materyalini
  taşır (emission 0).
- Environment/ambient: neredeyse siyah, çok hafif sıcak. Gölgeler **asla** mavi/soğuk
  siyaha kaymaz.
- **Truss fikstürleri (24 moving head, 8 par barı) ışık yaymaz** — hazırlık hâli, etkinlik
  ışıkları henüz yakılmadı. Emissive/ışık bileşeni eklenmemeli.

### Palet uyarısı (önemli)
Referans fotoğraflarda etkinlik aydınlatması **mor/mavi**. Art bible 4.3'ün guardrail'i
bunu yasaklıyor: *"hiçbiri mavi/yeşile kaymaz"* — çünkü mor/mavi yıkama **Anı Mavisi**
(`#80B3FF`) ile karışır ve oyuncu hangi katmanda olduğunu kaybeder (Bölüm 2, "İki Soğuk'un
Kasıtlı Farkı"). Geometri fotoğrafa sadık kuruldu, **aydınlatma Amber ailesinde bırakıldı**.
Pratikte çelişki yok: oyuncu partiyi hiç görmüyor. Mor/mavi bir sahne istenirse bu kilidin
bilerek gevşetilmesi gerekir — sessizce yapılmadı.

## Collider
- Zemin, dört duvar: **Box Collider**
- Sahne podyumu: Box Collider (0.42 m basamak — oyuncu üstüne çıkabilmeli mi? tasarım kararı)
- Dans pisti / halı bordürü: collider **yok** (0.035 m, zemin collider'ı yeter)
- Perdeler: ince Box Collider duvarın 0.20 m önünde — oyuncu kumaşa girmesin
- Sandalye yığınları, katlanmış masalar, kokteyl masaları: **Box Collider** (mesh değil)
- Teslimat arabası: Box Collider; zemin bandı collider'sız
- Truss / moving head / line array: **collider yok** (3.95 m üstü, erişilemez)
- Avize, tavan panelleri, aplikler: collider yok

## Sistem Bağları

- **`seviye-sahne-gecisi`** — Batı duvarındaki servis kapısı asansör/koridor bağlantısı.
  Salon kendi Unity sahnesi olarak additive yüklenir, birleştirilmez (GDD Core Rules).
  Kendi `SceneEnvironmentSettings` profilini taşır.
- **`gorev-tasima-dongusu`** — Teslimat trigger-zone'u araba + zemin bandının kapladığı
  alana yerleşir. Oyuncu asansörden gelip **ilk bunu görür**.
- **`adaptif-ses-sistemi`** — Alan `ZoneChanged` collider'ı gerekiyor (GDD cross-review'da
  açık uç olarak işaretli). Salon profili: yüksek tavan reverb'i + derin HVAC bası.
  Hacim 24 × 16 × 5.4 = 2074 m³ — reverb parametreleri buna göre.
- **`birinci-sahis-kontrolcu`** — Her alanda en az bir **sahte/dekor `IInteractable`**
  zorunlu. Adaylar: kokteyl masası örtüsü, sahnedeki flight case, katlanmış masa istifi.
  Gerçek tetikleyiciyle **aynı poligon ve doku bandında** kalmalı (art bible 3.1/8.6).

## Koleksiyon Yapısı (kaynak dosyada)

| Koleksiyon | Obje | Not |
|---|---|---|
| Sahne kökü | 168 | Kabuk — duvar, tavan, avize, truss, perde, zemin, pist, sahne, kapı, aplik |
| `Hazirlik` | 154 | Oyunun gerçek hâli — **varsayılan açık** |
| `Kurulu` | 280 | Giydirilmiş hâl — `hide_render` **kapalı** |
| `CarryItems` | 18 | 12 taşıma eşyası, y = −6 hattında |

## Üretilmiş Dokular (`assets/art/textures/`)

**Hiçbir materyal prosedürel node'a bağlı değil** — hepsi doku tabanlı, FBX'e gömülü.
Bu dosyalar bu proje için üretildi (PolyHaven değil):

| Doku | Boyut | Kullanım | Tiling |
|---|---|---|---|
| `env_balohali_albedo_large.png` | 1024², **seamless** | `Ball_Carpet` Base Color — krem `#D5CCBA` + donuk oker `#C3B189`, ~%25 leke, yumuşak kenar geçişi | Mapping **0.34** → **2.94 m/tile** (UV dünya ölçekli, 1 UV = 1 m) |
| `env_baloperde_normal_small.png` | 256², seamless | `Ball_Perde` normal — kadife hav | Mapping **5.0** (0.20 m/tile), strength 0.45 |
| `env_baloperde_rough_small.png` | 256², seamless | `Ball_Perde` roughness (0.72–0.97) | aynı |
| `env_balopist_baski_large.png` | 1024², **dekal (tiling değil)** | `K_PistBaski` Base Color — mor monogram madalyonu | UV 0..1, extension **EXTEND** |

- Halının elyaf normal/roughness'ı PolyHaven `dirty_carpet` (tiling 2.5 = 0.40 m/tile,
  normal strength 0.55, roughness 0.82–0.96 aralığına sıkıştırılmış)
- Halı bordür bandı ve altın çizgi **ayrı geometri** (`Ball_HaliBordur_*`,
  `Ball_HaliAltin_*`) — dokuya gömülü değil, düz materyal taşırlar
- `env_balopist_baski_large.png` sadece `Kurulu` koleksiyonunda kullanılıyor;
  oyunun hazırlık hâlinde dans pisti çıplak ahşap kalır

### Sheen kararı (kadife) — kapatıldı
Perde ve tavan drapesi bir ara Blender'ın **Sheen** bileşenini taşıyordu; URP Lit'te
karşılığı yok. Art bible 8.5/8.9 özel Shader Graph varyantını yasaklıyor (SRP Batcher'ı
böler, "yeni shader ihtiyacı sessizce export edilmez"), o yüzden fabric shader yazılmadı.

Bunun yerine katkısı **ölçüldü**: tam kare ortalama parlaklıkta **%0.21**, sheen'in en
güçlü okuduğu sıyırma açısında **%2.2** fark. İkisi de algı eşiğinin altında. Karar:
**Sheen = 0**, fark albedo'ya katıldı (perde ve drape ×1.044) — sapma **%0.58**'e indi.

Sonuç: Blender kaynağı ile URP Lit **birebir aynı** malzeme modelini kullanıyor,
Unity'de sürpriz yok. Hiçbir materyalde Sheen kalmadı. Metal avize gövdesi ve kristale
kazara uygulanmış Sheen de temizlendi (metal/cam için zaten anlamsızdı).

## Lisans / Atıf
- PolyHaven `dark_wooden_planks`, `dirty_carpet`: **CC0** — atıf gerekmez
- Prosedürel dokular ve tüm mesh'ler bu proje için üretildi
- Sketchfab / Hyper3D varlığı kullanılmadı — credits'e ekleme gerekmiyor
