# Depo — Unity Kurulum Notu

Kaynak: `depo otopark.blend` (kullanıcı masaüstü, OneDrive). Bu klasördeki
`SM_Depo.fbx` yalnızca depo bölgesinin mesh'leri (245 adet), metre ölçekli,
Y-up, **texture'lar FBX içine gömülü** (~20 MB).
Referans görüntü: `assets/art/Depo_ref.png`. Referans fotoğraflar: gerçek otel
etkinlik deposu (kullanıcıda). Sanat spec'i: `design/art/art-bible.md` Bölüm 2.1,
3.2, 4.3.

## FBX Import
- Scale Factor 1, Convert Units açık
- **Materials sekmesi → Extract Textures + Extract Materials** (gömülü
  PolyHaven dokuları: `concrete_floor_worn_001` zemin, `concrete` tavan,
  `beige_wall_001` duvarlar)
- Generate Colliders: depo oynanış alanı — zemin/duvar/kolon gibi statik
  kabuk objelerine Mesh Collider veya basit Box Collider'lar; prop'lara
  gameplay ihtiyacına göre
- Read/Write kapalı

## Işık — Amber Gece Vardiyası (art bible 4.3: BaseColor `#FF9E4D`)
Işıklar FBX'te YOK, Unity'de kurulacak. Blender'daki kurulum (Blender koordinatı
(x,y,z) → Unity (x,z,y)):

| Işık | Tip | Blender poz | Renk (yaklaşık sRGB) | Not |
|---|---|---|---|---|
| 8× floresan | Area 1.1×0.12, 55W | y=2 ve y=10 sıraları, x=2/6/10/14, z=2.92 | `#FFAA59` | URP realtime area yok → **aşağı bakan Spot (120°)** ya da baked Area kullan |
| 2× çıplak ampul | Point 25W | (11.0, 8.8, 1.95) ve (4.8, 4.5, 1.95) | `#FF9E4D`'den kızıl: `#FF6114` civarı | Sert gölge, shadow radius küçük |

- Environment/ambient: neredeyse siyah, çok hafif sıcak (`#080502` gibi)
- Fixture ve ampul mesh'lerinde emissive malzeme var (`Depo_TubeGlow`,
  `Depo_Ampul`) — URP'de Emission açık malzemeyle yeniden kur
- **Guardrail**: hiçbir ışık soğuk/teal tona kaymaz — o palet psikiyatri
  ofisine kilitli (art bible "Ayrım Notu")

## Bölge / Bağlantılar
- Depo alanı: x 0..16, y(Unity z) 0..12, tavan 3.0 m
- Batı duvarında otopark kapısı boşluğu (y 4.8–7.2), doğu duvarında servis
  koridoru geçişi (y 4.8–7.2) — `seviye-sahne-gecisi` sistemine bağlanacak
- Taşıma döngüsü spawn noktaları (`gorev-tasima-dongusu` GDD): şamdan sırası,
  kasa istifleri, kumaş rulolar vb. `CarryItemPickup` adayları — sabit spawn
  konumları sahnede hazır

## Lisans / Atıf
- Ahşap palet: Sketchfab "Wooden Euro Pallet", yazar **sallazarPL**,
  CC Attribution — **oyun credits'ine eklenmeli**
- PolyHaven dokuları: CC0 (atıf gerekmez)
- Çuval yığını + branda yığını: Hyper3D Rodin üretimi
