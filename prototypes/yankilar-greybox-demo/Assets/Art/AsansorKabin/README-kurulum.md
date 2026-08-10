# Asansör Kabini — Unity Kurulum Notu

Kaynak: `assets/art/AsansorKabin.blend`. Bu klasördeki `SM_AsansorKabin.fbx`
kabinin tüm mesh'leri (26 adet, 2.152 tri), metre ölçekli, Y-up, **texture'lar
FBX içine gömülü** (~10 MB). Referans görüntü: `assets/art/AsansorKabin_ref.png`.

Sanat spec'i: `design/art/art-bible.md` Bölüm **2.3** (Ölü Zaman), 4.1 (palet),
6.3 (prop yoğunluğu), 8.6/8.7/8.9/8.11 (bütçeler).
Sistem spec'i: `design/gdd/asansor-kat-erisim-sistemi.md`, `docs/architecture/adr-0011-elevator-state-machine.md`.

> **Kapsam sınırı**: Bu asset yalnızca **kabin içi**. Kat tarafı (kat kapıları,
> çağrı düğmesi plakası + görsel ışık, kapı kasası) bu FBX'te **yok** — ayrı bir
> asset olarak modellenecek. Kat trigger-zone'ları GDD'nin "Core Rules"unda.

## Ölçüler ve Yerleşim

| | |
|---|---|
| İç hacim | 1.80 (X) × 2.10 (Z) × 2.30 (Y) m |
| Net kapı geçişi | 0.90 × 2.10 m |
| Dış ayak izi | 2.00 (X) × 2.33 (Z) m |
| Zemin | Unity Y = 0 |
| Kapı yönü | Unity **−Z**'ye bakar (oyuncu −Z'den girer) |
| Oyuncu göz hizası referansı | 1.65 m |

Blender koordinatı (x, y, z) → Unity (x, z, y).

## FBX Import
- Scale Factor 1, Convert Units açık
- **Materials sekmesi → Extract Textures + Extract Materials**
- Mesh Compression: **Medium** (art bible 8.9)
- **Read/Write kapalı**
- Generate Colliders **kapalı** — elle kur (aşağıya bak)
- Normals: Import (FBX'te smoothing group + tangent var, n-gon yok)
- UV: `UVMap` (dünya ölçekli kutu projeksiyonu, **1 UV birimi = 1 m**) +
  **`UV2` lightmap kanalı** her mesh'te mevcut, çakışmasız (art bible 8.5/8.9 zorunluluğu)

## Materyaller (6 slot — art bible 8.11 asansör payı ~10 çizim çağrısı)

Hepsi **URP Lit / Metallic workflow**.

| Materyal | Kullanan | Değerler |
|---|---|---|
| `M_Kabin_Panel` | duvarlar, kapı kanatları, ön dönüş panelleri, eşik, korkuluk, derz çubukları, COP plakası | Albedo `#8C8C8E` düz, Metallic **0.85**, normal `env_kabinpanel_normal_small.png` (strength **0.22**), roughness `env_kabinpanel_rough_small.png` **0.37–0.44** aralığına sıkıştırılmış, **tiling 4** (0.25 m/tile) |
| `M_Kabin_Zemin` | kabin zemini | PolyHaven `metal_plate` 2k (baklava sac), tiling **1** (1 m/tile), albedo desatüre 0.35 / value 0.40 |
| `M_Kabin_Boyali` | tavan | Albedo `#B5A897` (Temel Malzeme Grisi), Metallic 0, Rough 0.72 |
| `M_Kabin_Kaucuk` | kapı operatörü kutusu, yan cepler, eşik oluğu, tampon fitili, COP göstergesi | Albedo `#2A2724`, Metallic 0, Rough 0.92 |
| `M_Kabin_Difuzor` | armatür difüzörü | **Emission açık**, renk `#FFBF80`, strength 0.9 |
| `M_Kabin_Kumas` | koruma yorganları | Albedo düz koyu (lineer ≈0.048), doku **yalnızca** normal (strength 0.35) + roughness — PolyHaven `fabric_pattern_07`, tiling 9 |

> **Guardrail (art bible 4.1)**: hiçbir albedo'ya sıcak/amber ton boyanmaz.
> Kabinin tüm sıcaklığı tek pratik ışıktan gelir; ışık kapanınca yüzeyler nötr
> kalmalı. Bir de hiçbir yüzey soğuk/teal'e kaymaz — o palet psikiyatri ofisine
> kilitli (art bible Bölüm 2 "Ayrım Notu").

## Işık — Tek Zayıf Pratik Kaynak (art bible 2.3)

Işık FBX'te **yok**, Unity'de kurulacak. Blender'daki kurulum:

| Işık | Tip | Unity poz | Renk | Not |
|---|---|---|---|---|
| Kabin armatürü | Area Rectangle 0.34×0.34, **18 W** | (0, **2.225**, **0.30**) | `#FFBF80` (Vardiya Amberi) | URP realtime area yok → **aşağı bakan Spot** ya da baked Area |

- Kaynak bilinçli olarak **arkaya kaydırılmış** (Z=+0.30) — kapı tarafı daha loş
  kalsın, "kapalı kutu" hissi kapıya doğru derinleşsin.
- Environment/ambient: neredeyse siyah **sıcak-nötr** `#14100C` (Gerçeklik
  Gölgesi), çok düşük şiddet. Gölgeler asla mavi/soğuk siyaha kaymaz.
- Yüksek kontrast hedefi: tavan ve zemin köşeleri pratikte siyah okur — bu
  **doğru**, art bible 2.3'ün "az ışık, neredeyse karanlığa yakın" kilidi.
- İkinci bir ışık **eklenmez** ("tek zayıf pratik kaynak").

## Kapı Animasyonu

Ortadan açılır çift kanat. Kanatlar ön dönüş panellerinin **arkasında** kayar —
tam açıkken hiçbir bakış açısından kanat kenarı görünmez (test edildi).

| Obje | Kapalı local X | Açık local X |
|---|---|---|
| `Kabin_Kapi_Sol` | **−0.2315** | **−0.6900** |
| `Kabin_Kapi_Sag` | **+0.2315** | **+0.6900** |

Stroke **0.4585 m**, her iki kanat için simetrik. Blender X → Unity X (işaret aynı).
Bu değerler FBX objelerine custom property olarak da gömülü (`kapali_x`, `acik_x`,
`stroke_m`).

Zamanlamalar GDD'nin Tuning Knob'ları (`asansor-kat-erisim-sistemi.md`):
`DoorOpenAnim` ~1.5 s → `DwellTime` 4–6 s → `DoorCloseAnim` ~1.5 s;
`ArrivalDuration` 3–6 s. **Kabin fiziksel olarak hiç hareket etmez** — yükselme
hissi tamamen kozmetik kamera-uzayı sarsıntısı + uğultu.

## Collider

- Zemin, üç duvar, ön cephe (dönüş panelleri + kapı üstü kirişi): **Box Collider**
  — kutusal geometri, Mesh Collider gereksiz
- Kapı kanatları: kendi Box Collider'ları, animasyonla birlikte hareket eder
  (`DoorsOpen` durumunda geçişi tıkamamalı)
- Tavan: Box Collider (oyuncu taşıdığı eşyayla yukarı bakabiliyor)
- Prop'lar (tampon, korkuluk, armatür, COP, yorgan): collider **yok** — dekor

## Obje Grupları

- **Kabuk**: `Kabin_Zemin`, `Kabin_Tavan`, `Kabin_Duvar_Arka/Sol/Sag`,
  `Kabin_On_Sol/Sag/Ust`, `Kabin_KapiOperatoru`, `Kabin_Cep_Sol/Sag`,
  `Kabin_Esik_Ic/Dis/Oluk`
- **Kapı**: `Kabin_Kapi_Sol`, `Kabin_Kapi_Sag`
- **Prop (5 — art bible 6.3 tavanı 3–5)**: `Kabin_Tampon`, `Kabin_Korkuluk`,
  `Kabin_Armatur`+`Kabin_Difuzor`, `Kabin_Panel_COP`+`Kabin_COP_Gosterge`,
  `Kabin_YorganAskisi`+`Kabin_Yorgan_A/B`
- **Detay**: `Kabin_Derz` (panel derz çubukları + süpürgelik, tek mesh)

`Kabin_KapiOperatoru` ve `Kabin_Cep_Sol/Sag` **kapatıcı hacimlerdir** — kapı
açıkken boşluğa bakılmasını engeller. Silinmemeli.

## Bilinen Açık Uçlar

- **COP'ta 4 kat düğmesi + 1 alarm** modellendi. Oyunun kesin kat sayısı GDD'de
  sabitlenmemiş — kat sayısı değişirse düğme sütunu güncellenmeli
  (`Kabin_Panel_COP`, Blender'da z = 0.98 / 1.10 / 1.20 / 1.30 / 1.40).
- COP göstergesi (`Kabin_COP_Gosterge`) şu an düz koyu plaka. Kat göstergesi
  diegetik bir ekran olacaksa emissive/UI materyali gerekir — GDD bunu
  kilitlemiyor.

## Lisans / Atıf
- PolyHaven `metal_plate`, `fabric_pattern_07`: **CC0** — atıf gerekmez
- `env_kabinpanel_normal_small.png` / `env_kabinpanel_rough_small.png`: bu proje
  için prosedürel üretildi (`assets/art/textures/`), tiling/seamless
- Sketchfab veya Hyper3D varlığı **kullanılmadı** — credits'e ekleme gerekmiyor
