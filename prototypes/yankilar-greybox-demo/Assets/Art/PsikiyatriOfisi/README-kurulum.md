# Psikiyatri Ofisi — Unity Kurulum Notu

Kaynak: `assets/art/PsikiyatriOfisi_Blockout.blend` (Blender 5.2). Bu klasördeki
`SM_PsikiyatriOfisi.fbx` modifier'ları uygulanmış, metre ölçekli, Y-up export.
Referans görüntü: `assets/art/PsikiyatriOfisi_ref.png` — ışık/kadraj hedefi budur.
Sanat spec'i: `design/art/art-bible.md` Bölüm 2.4 (teal-gri, tek sert kaynak, jaluzi).

## FBX Import Ayarları
- Scale Factor: 1 (zaten metre), Convert Units: açık
- Generate Colliders: KAPALI (cutscene odası, fizik yok)
- Read/Write: kapalı, Normals: Import, Blend Shapes: kapalı

## Malzemeler (URP Lit, hepsi düz renk — texture yok)
FBX slot isimleri Blender'dakiyle aynı. Renkler linear RGB; Unity color picker'da
yaklaşık sRGB karşılıkları:

| Slot | Linear RGB | Yaklaşık sRGB | Not |
|---|---|---|---|
| M_Duvar | 0.50, 0.60, 0.61 | #BBCBCC | Smoothness ~0.1 |
| M_Zemin | 0.20, 0.26, 0.27 | #7D8E90 | Smoothness ~0.4 (linolyum) |
| M_Mobilya | 0.12, 0.16, 0.17 | #647376 | Smoothness ~0.3 |
| M_Metal | 0.33, 0.39, 0.40 | #9AA7A9 | Metallic 0.8, Smoothness 0.6 |
| M_Jaluzi | 0.62, 0.66, 0.66 | #D0D6D6 | Smoothness 0.5 |
| M_Kagit | 0.85, 0.87, 0.86 | #EEF0EF | Smoothness 0.05 |
| M_Abajur | koyu + emission | — | Emission (0.85,0.92,0.92) × ~3 |
| M_Kapi / M_Kitap1-3 | koyu teal-gri tonlar | — | M_Mobilya'ya yakın |

**Palet çapası**: Muayene Teali `#7A9496` (art bible kilitli değer). Hiçbir malzeme
sıcak tona (amber/sarı/turuncu) kaymayacak — o aile başka sahnelerin kimliği.

## Işık — TEK kaynak
- 1 adet **Spot Light**: pozisyon **(-0.3, 1.14, 0.76)**, hedef: **(0.6, 0.7, 1.9)**
  yönüne baktır (pencere duvarı + masa)
- Spot Angle: Unity'de 150° pratik değil; **Outer 120 / Inner 80** ile başla,
  duvar koni kenarı referans PNG'ye benzesin
- Renk: (0.72, 0.87, 0.87) — soğuk teal-beyaz. Intensity: göz kararı, referans
  PNG'deki kontrast hedef (duvarlar okunur, gölgeler simsiyaha yakın)
- **Shadows: Hard**, resolution High, bias düşük — jaluzi kanatlarının çizgili
  gölgesi duvara NET düşmeli (bu sahnenin imzası). İnce kanatlar gölge
  üretmiyorsa: Shadow Near Plane'i küçült / bias'ı düşür; olmadı **light cookie**
  (çizgili desen texture'ı) ile aynı etki alınır — spec "fiziksel jaluzi + çizgi
  deseni" istiyor, cookie kabul edilebilir fallback
- **Fill light / ambient YOK**: Environment Lighting'i çok koyu teal'e çek
  (Intensity ~0.05). İkinci ışık kaynağı eklenmeyecek (art bible kuralı)

## Kamera — kilitli
- Pozisyon: **(0.2, 1.18, -1.05)** (hasta koltuğu, oturma göz hizası)
- Bakış hedefi: **(0.2, 0.95, 1.6)** → LookAt ile döndür
- **Vertical FOV: 30.6°** (Blender 37mm eşdeğeri, 16:9)
- Kamera scriptle OYNAMAZ — sabit/kilitli (sahne kimliğinin parçası,
  `sahne-kesmeli-anlati` hard-cut ile girilir/çıkılır)

## Sonrası
- Volume/post: hafif teal grading kabul, ama iki bitiş varyantı arasında ışık
  DEĞİŞMEZ (fark ses kanalında — art bible 2.4 notu)
- Psikiyatrist NPC temsili açık soru (art bible Bölüm 5) — koltuk kadrajda,
  karakter eklemek için sahneyi değiştirmek gerekmiyor
