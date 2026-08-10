# Kanıt: Anlatı ClueRegistry Addressables Smoke

> **Story**: `production/epics/anlati-durum-ipucu-takibi/story-003-addressables-isik-volume-aboneligi.md`
> **AC**: AC-5 — DEFERRED manuel smoke
> **Tip**: Integration (kanıt yolu BLOCKING)
> **Durum**: Editor-içi doğrulama TAMAM · Player-build doğrulaması BEKLİYOR (Story 004)
> **Tarih**: 2026-08-10

---

## 1. Neden manuel / neden DEFERRED

AC-5, `"ClueRegistry"` Addressable anahtarının **gerçekten çözüldüğünü** iddia eder.
Bu, otomatik PlayMode testleriyle kapatılamaz — ve gerekçe, story yazıldığındaki
gerekçeden farklı:

- **Story yazılırken kaydedilen gerekçe** ("repoda Addressables altyapısı yok") artık
  **geçersiz**: bu story `Assets/AddressableAssetsData/` ve
  `Assets/Settings/ClueRegistry.asset` dosyalarını commit'ledi.
- **Geçerli gerekçe (QL-TEST-COVERAGE, Story 003)**: Editor içi bir PlayMode yüklemesi
  **AssetDatabase provider**'ından geçer. Bu, *build edilmiş player'ın content
  catalog*'unun anahtarı çözüp çözmediği hakkında **hiçbir şey kanıtlamaz** — ki
  ADR-0007'nin "Verification Required" maddesinin işaret ettiği gerçek risk tam olarak
  budur. Aynı sebeple `EnsureRegistryLoaded()`'ın gövdesi enjekte edilebilir değil:
  `ClueRegistryAddressableKey` sabit, anahtar seam'i yok.

Bu boşluğun kalıcı kapanışı **Story 004**'ün build-blocking anahtar kontrolüdür
(`IPreprocessBuildWithReport` → `BuildValidationRegistry`). Bu dosya, o kontrol
devreye girene kadarki ara kanıttır.

---

## 2. Editor-içi doğrulama (YAPILDI)

### 2.1 Kurulum betiği çıktısı

`AnlatiStory003AddressablesSetup.Run` tek seferlik olarak koşturuldu:

```
[AnlatiStory003AddressablesSetup] 'Assets/Settings/ClueRegistry.asset' Addressable, anahtar='ClueRegistry'.
```

Diskte doğrulandı:

| Yol | Durum |
|---|---|
| `game/Assets/AddressableAssetsData/AddressableAssetSettings.asset` | var |
| `game/Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` | var, `m_Address: ClueRegistry` |
| `game/Assets/Settings/ClueRegistry.asset` | var, GUID grup girdisiyle eşleşiyor |

### 2.2 Dolaylı çalışma-zamanı kanıtı (PlayMode süiti)

`AnlatiDurumIpucuTakibi` süreç genelinde `OnShiftStateChanged`'e abone ve
`isik_volume_*` PlayMode testleri `ShiftZone` üzerinden **gerçek `Held`** olayları
fırlatıyor. Yani bu testler `EnsureRegistryLoaded()` → `WaitForCompletion()` yolunu
zaten tetikliyor.

- **PlayMode süiti yeşil** ve `Debug.LogError` üretilmedi.
- UTF beklenmeyen `LogError`'da testi düşürdüğü için, yeşil süit "anahtar Editor play
  mode'da çözülüyor"un makul kanıtıdır.

> **Bilinen arıza modu — kayda geçirilmiştir**: anahtar bozulursa hata bu dosyanın
> testlerinde DEĞİL, **ilgisiz `isik_volume_*` testlerinde** beklenmeyen bir
> `LogError` olarak yüzeye çıkar. Teşhis sırasında bu tuzağa dikkat.

---

## 3. Player-build doğrulaması (BEKLİYOR)

Story 004 kapandığında yapılacak ve buraya eklenecek:

- [ ] Standalone Windows player build alındı (Addressables content build dahil)
- [ ] Player'da bir `ShiftZone` `Held`'e ulaştırıldı
- [ ] `Player.log`'da `[AnlatiDurumIpucuTakibi]` `LogError`'ı **yok**
- [ ] İpucu bilgisi (`IsClueKnown`) player'da beklendiği gibi `true`
- [ ] Story 004'ün build-blocking anahtar kontrolü, anahtar kasten bozulduğunda
      build'i **düşürüyor** (negatif doğrulama)

---

## 4. Sign-off

| Alan | Değer |
|---|---|
| Editor-içi doğrulama | **PASS** — 2026-08-10 |
| Player-build doğrulaması | **PENDING** — Story 004'e bağlı |
| Story 003 kapanışı için yeterli mi? | **Evet** — kalan risk Story 004'ün build gate'inin açık kapsamı ve orada takip ediliyor |
