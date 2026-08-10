# Build Validation — Paylaşılan Doğrulama Utility'si (Story 006)

Projenin **tek** `IPreprocessBuildWithReport` implementasyonu (`BuildValidationRunner`).
İkinci bir bağımsız implementasyon **yasak** (control manifest, ADR-0014).

## Nasıl check eklenir

1. Sisteminin kendi epic'inde `IBuildCheck` implement et (Name, Phase, Run).
2. Instance'ını `BuildValidationRegistry.Checks` dizisine ekle — başka hiçbir yere.
3. İhlalde `context.Fail("mesaj — offending asset/sahne yolu dahil")` çağır
   (`BuildFailedException` fırlatır, build durur — asla runtime clamp).
4. `Phase`:
   - `AssetScan` — sahne AÇMAYAN, ucuz, her zaman önce koşar. Bu bir **maliyet
     sınıfı**, API reçetesi değil: `AssetDatabase.FindAssets` kullanmak ZORUNLU
     değildir. Merkezi bir kaydın içine bakmak (anlati'nın `ClueRegistry.Definitions`
     üçlüsü) da AssetScan'dir — ve ADR kapsamı bunu böyle kilitlemişse
     `FindAssets`'e sürüklenmek İHLALDİR.
     `FindAssets` kullanılıyorsa: dosya-başına GUID döndürür — sub-asset yasağı konvansiyonu.
   - `SceneScan` — pahalı; yalnız sahne-bağımlı check'ler için. Runner
     `EditorBuildSettings.scenes`'i tek tek açar; sahne-scan check'i kayıtlı
     değilse hiçbir sahne açılmaz.
5. **Sahneler-arası TOPLAM iddia** ("taranan sahnelerin en az birinde X olmalı"):
   check ayrıca `IBuildCheckAggregate` implement eder — `BeginWalk` yürüyüş
   BAŞINDA gözlemi sıfırlar (zorunlu: ortada patlayan yürüyüş bayat gözlemi
   sonraki build'e sızdıramaz), `Run` sahne başına biriktirir, `FinalizeWalk`
   yürüyüş sonunda (sıfır sahnede de) değerlendirir. Yalnız SceneScan için.
6. Test fixture'ları `ScriptableObject.CreateInstance` — **asla on-disk asset**
   (asset-scan'ler tüm projeyi tarar, test verisi build'i kırar — ADR-0014 kaydı).

## Kayıtlı check'ler

| Sistem | Check'ler | ADR / TR |
|---|---|---|
| Işık/Volume (Story 006, 2026-08-09) | `IsikVolume/LightModeMixed`, `IsikVolume/NoSharedLights`, `IsikVolume/NoBoxOverlap`, `IsikVolume/AutomaticZonePresence` (aggregate) | ADR-0005 / TR-isik-016/020/021 |
| Birinci Şahıs Kontrolcü (Story 006, 2026-08-10) | `Fpc/DecoyPresence` — her MVP seviye sahnesinde (`Depot`/`Ballroom`) en az bir `DecoyInteractable`, hepsi dolu `PromptText` ile | ADR-0003 / TR-fpc-016 (GDD AC17) |
| Anlatı Durum/İpucu Takibi (Story 004, 2026-08-10) | `Anlati/ClueRegistryKeyResolves`, `Anlati/RequiredShiftIdsNotEmpty`, `Anlati/UniqueClueIds` (üçü AssetScan, `ClueRegistry.Definitions` üzerinde — `FindAssets` DEĞİL), `Anlati/AutomaticZoneNotClueBearing` (SceneScan, AC22 çaprazı) | ADR-0007 / TR-anlati-008/009, TR-isik-021 |

> **Anlatı notu**: orphaned `requiredShiftId` uyarısı bu tabloda YOKTUR ve bir
> `IBuildCheck` DEĞİLDİR — build'i bloklamayan bir uyarıdır (Story 005, ADR-0007).
>
> **Kayıt konumu**: anlati'nın üç içerik check'i `ClueRegistry`'yi Addressable
> ADRESİ üzerinden bulur (`entry.address` → `entry.AssetPath` → `LoadAssetAtPath`),
> sabit yol sabitiyle değil. Entry GUID'e bağlı olduğu için asset taşınırsa adres
> sağ kalır ama sabit yol ölürdü: anahtar check'i yeşil kalırken diğer üçü
> sessizce fail-open olurdu.
>
> **Bilinen kör nokta (tüm SceneScan check'leri için ortak)**: `FindObjectsByType`
> yalnız açık sahneyi görür — yalnız bir prefab içinde ya da
> `EditorBuildSettings.scenes`'te olmayan/disabled bir sahnedeki ihlal görünmez.

## Planlanan check sahipleri (kendi epic'lerinde eklenecek)

| Sistem (epic) | Check'ler | ADR / TR |
|---|---|---|
| Görev/Taşıma Döngüsü | `TaskListDef` vs sahne per-round item-count cross-check | ADR-0013 / TR-gorev-018 |
| Anı-Tetikleyici Etkileşim | 6'lı set: def/scene eşleme, reachability (yerleşmemiş def → error), count formülü | ADR-0014 / TR-ani-tetik-007/010 |
| Diyalog/Anlatı İçeriği | `ValidateMaxCallbacksPerScene` | ADR-0012 / TR-diyalog-005 |
| Sahne Kesmeli Anlatı | `NightConfigDef` tutarlılık | ADR-0015 |
