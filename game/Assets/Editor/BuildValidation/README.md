# Build Validation — Paylaşılan Doğrulama Utility'si (Story 006)

Projenin **tek** `IPreprocessBuildWithReport` implementasyonu (`BuildValidationRunner`).
İkinci bir bağımsız implementasyon **yasak** (control manifest, ADR-0014).

## Nasıl check eklenir

1. Sisteminin kendi epic'inde `IBuildCheck` implement et (Name, Phase, Run).
2. Instance'ını `BuildValidationRegistry.Checks` dizisine ekle — başka hiçbir yere.
3. İhlalde `context.Fail("mesaj — offending asset/sahne yolu dahil")` çağır
   (`BuildFailedException` fırlatır, build durur — asla runtime clamp).
4. `Phase`:
   - `AssetScan` — `AssetDatabase.FindAssets` tabanlı, ucuz, her zaman önce koşar.
     Dikkat: `FindAssets` dosya-başına GUID döndürür — sub-asset yasağı konvansiyonu.
   - `SceneScan` — pahalı; yalnız sahne-bağımlı check'ler için. Runner
     `EditorBuildSettings.scenes`'i tek tek açar; sahne-scan check'i kayıtlı
     değilse hiçbir sahne açılmaz.
5. Test fixture'ları `ScriptableObject.CreateInstance` — **asla on-disk asset**
   (asset-scan'ler tüm projeyi tarar, test verisi build'i kırar — ADR-0014 kaydı).

## Planlanan check sahipleri (kendi epic'lerinde eklenecek)

| Sistem (epic) | Check'ler | ADR / TR |
|---|---|---|
| Anlatı Durum/İpucu Takibi | ClueDefinition içerik doğrulama, orphaned `requiredShiftId`, Addressable `"ClueRegistry"` key çözümü | ADR-0007 / TR-anlati-008 |
| Işık/Volume Durum Sistemi | Volume-trigger-box overlap, Baked-light, shared-light sahne-scan seti | ADR-0005 / TR-isik-016/021 |
| Görev/Taşıma Döngüsü | `TaskListDef` vs sahne per-round item-count cross-check | ADR-0013 / TR-gorev-018 |
| Anı-Tetikleyici Etkileşim | 6'lı set: def/scene eşleme, reachability (yerleşmemiş def → error), count formülü | ADR-0014 / TR-ani-tetik-007/010 |
| Diyalog/Anlatı İçeriği | `ValidateMaxCallbacksPerScene` | ADR-0012 / TR-diyalog-005 |
| Sahne Kesmeli Anlatı | `NightConfigDef` tutarlılık | ADR-0015 |
| Birinci Şahıs Kontrolcü | Decoy check | TR-fpc-016 |
