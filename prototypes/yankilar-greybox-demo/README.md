# Yankılar — Greybox Demo

Bu klasör, ana GDD'lerden ve Claude'un hazırladığı MVP planından bağımsız,
oynanabilir bir Unity 6 URP prototipidir. Amaç; ilk beş dakikanın ritmini
test etmektir: servis koridorunda yük alma, asansör yolculuğu, balo salonuna
teslimat ve aradaki tek hafıza kırılması.

## Çalıştırma

1. Unity Hub'dan bu klasörü açın: `prototypes/yankilar-greybox-demo`.
   Proje, makinede bulunan Unity `6000.5.6f1` URP şablonundan oluşturuldu.
2. Paketlerin ilk importunun bitmesini bekleyin.
3. Menüden **Yankılar > Greybox Demo Sahnesini Kur** seçin.
4. Açılan `Assets/Generated/YankilarGreyboxDemo.unity` sahnesini kaydedin ve
   Play'e basın.

Kontroller: **WASD** yürüyüş, **fare** bakış, **E** etkileşim, **Esc** fareyi
serbest bırakır.

## Bu build'de test edilecekler

- Kutuyu servis deposundan alıp balo salonundaki teslim noktasına taşıma
- Asansörün, yalnızca yük alındıktan sonra kullanılabilmesi
- Koridordaki hafıza objesinin ışığı ve ortam sesini değiştirmesi
- Basit görev metninin akışı anlaşılır kılıp kılmadığı

Bu bir asset veya final-sistem uygulaması değildir. Nomad modelleri daha sonra
`Assets/Art/Props/` altına eklenip mevcut primitive greybox objelerinin yerine
konabilir. Yüksek poligonlu model yerine oyun-içi kullanıma uygun export
(`.fbx`, ölçü metre, uygulanan transform/pivot) tercih edilmelidir.
