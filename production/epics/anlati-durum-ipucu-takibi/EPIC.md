# Epic: Anlatı Durum/İpucu Takibi

> **Layer**: Foundation
> **GDD**: design/gdd/anlati-durum-ipucu-takibi.md (Approved)
> **Architecture Module**: Anlatı Durum/İpucu Takibi (`AnlatiDurumIpucuTakibi` static facade)
> **Governing ADRs**: ADR-0007 (+ADR-0015 in-place rejim)
> **Engine Risk**: LOW — projenin ilk Addressables tüketicisi (mekanizma stabil, kullanım yeni)
> **Control Manifest Version**: 2026-08-09
> **Status**: Complete
> **Stories**: 5 stories

## Overview

`ClueDefinition`/`ClueRegistry` ScriptableObject veri modeli (N:1, ALL-semantiği), `Held`-only `OnShiftStateChanged` işleyicisi, shiftId→ClueDefinition ters indeksi, Addressables lazy-load (`EnsureRegistryLoaded`, constructor dışında — ilk gerçek Held'de), idempotent `MarkClueKnown` + `OnClueKnown`, iki-katmanlı editör doğrulaması (boş requiredShiftIds/çift clueId/çözülmeyen Addressable key build-blocking; orphaned shiftId Editor-only uyarı). Sıralama/zaman verisi asla açılmaz (Pillar 1/5).

## GDD Requirements

| TR-ID | Requirement (kısa) | ADR Coverage |
|-------|--------------------|--------------|
| TR-anlati-001 | Statik singleton kalıcılık | ADR-0001/0007 ✅ |
| TR-anlati-002..004 | ClueDefinition ALL-semantiği; idempotent Mark; sorgu yüzeyi | ADR-0007 ✅ |
| TR-anlati-005..006 | Held-only işleme; first-access abonelik | ADR-0007 ✅ |
| TR-anlati-007 | Sıralama verisi yok | ADR-0007 ✅ |
| TR-anlati-008 | İki-katmanlı editör doğrulaması | ADR-0007 ✅ (utility: proje-kurulumu) |
| TR-anlati-009 | `"ClueRegistry"` Addressable anahtarının build-time çözümlenebilirliği | ADR-0007 ✅ (Risks mitigasyonu — story yazımında mint edildi) |
| TR-isik-021 (AC22 yarısı) | Automatic bölgenin shiftId'si hiçbir `ClueDefinition`'da olamaz | isik-volume Story 006'dan DEVRALINDI → Story 004 |

**Untraced Requirements**: None

## Definition of Done

- Tüm story'ler kapandı; ADR-0007 Validation Criteria testleri geçiyor (çift-Mark tek event; sıra-bağımsız tamamlanma; build-fail üçlüsü; Persistent re-fire çift-event yok; Addressables ilk-Held smoke)
- ClueRegistry cache'i oturumlar arası korunuyor (in-place reset, Addressables'a ResetOnLoad'da asla dokunulmuyor)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Facade çekirdeği — IAnlatiDurumState + AnlatiDurumState + statik facade | Logic | Complete | ADR-0007 (+0001/0015) |
| 002 | ClueDefinition/ClueRegistry + ters indeks + Held handler mantığı | Logic | Complete | ADR-0007 |
| 003 | Addressables lazy-load + gerçek Işık/Volume aboneliği | Integration | Complete | ADR-0007 (+0015) |
| 004 | Build-blocking doğrulama dörtlüsü | Logic | Complete | ADR-0007 (+0014) |
| 005 | Orphaned shiftId uyarısı (build-time aggregate, non-blocking) | Logic | Complete | ADR-0007 (+0014) |

**Bağımlılık grafiği** (düz zincir DEĞİL): 001 → 002 → {003, 004, 005} — son üçü 002'ye bağlı ama BİRBİRİNE bağlı değil, paralel ilerleyebilir.

## Next Step

**EPIC TAMAMLANDI — 5/5 story Complete** (2026-08-10). EditMode 239/239, PlayMode 84/84.

Story 003 bu projenin İLK gerçek Addressables tüketicisiydi: `Assets/AddressableAssetsData/` + `Assets/Settings/ClueRegistry.asset` (anahtar `"ClueRegistry"`) artık repoda. Story 004+005 beş build check’i ekledi (dördü bloklayan, biri non-blocking uyarı).

**Kapanış öncesi TEK borç: ADR-0007 addendum’u** — aşağıdaki iki madde `/architecture-decision` ile açılmalı. Sapmalar kodda, README’de ve ADR’ın kendisinde işaretli, ama addendum resmileşmedi.

### ADR-0007 addendum borcu (epic kapanışında tek pakette açılacak)

`/architecture-decision` ile açılacak addendum'un İKİ maddesi var:

1. **Abonelik yeri (Story 001'de tespit, Story 003'te UYGULANDI)**: Işık/Volume
   aboneliği ADR-0007'nin `AnlatiDurumState` constructor'ında DEĞİL, FACADE'ın
   (`AnlatiDurumIpucuTakibi`) static constructor'ında bağlanıyor. Gerekçe:
   State-ctor aboneliği, ADR-0001 deseni gereği taze State kuran her testte
   kalıcı event'e bir handler daha sızdırırdı; proje emsali `GeceOturumDurumu`
   de facade static ctor kullanıyor. Story 003'ün testleri bu şekli sabitliyor
   (`FacadeFirstAccess_SubscribesExactlyOnce_AndSurvivesReset`).
2. **Orphaned-shiftId kontrolünün mekanizması (Story 005'te UYGULANDI)**:
   kullanıcı kararıyla ADR-0007'nin `EditorSceneManager.sceneOpened/sceneSaved`
   tetikleyicisinden `IBuildCheckAggregate` build-yürüyüşüne taşındı (GDD'nin
   proje-geneli iddiası tek-sahne bir kontrolle verilemiyordu — çok-sahneli
   clue'larda yanlış-pozitif). ADR'ın iki kısıtı korundu: non-blocking ve player
   build'ine girmez. Uygulama: `ClueConsistencyValidator.cs`; sapma ADR-0007'nin
   satır 152'sinde ve izlenebilirlik tablosunda işaretli, `MultiSceneClue_DoesNotWarn`
   testi geri alınmasını engelliyor.

### Dışarı çıkan iş kalemleri

| # | Kalem | Nereye |
|---|-------|--------|
| 1 | `IsikVolumeState.RaiseShiftStateChanged` delege-başına try/catch (bir abonenin istisnası multicast'i ve `ShiftZone`'un tick coroutine'ini düşürüyor) | isik-volume EPIC.md → FD-1 |
| 2 | `ClueRegistry.asset` boş ship ediliyor; dört build kontrolü de içeriğin ŞEKLİ hakkında, VARLIĞI hakkında değil — "içerik hiç yazılmadı" sessizce ship olur. Presence check ayrı bir karar (içerik henüz yazılmadı) | İçerik yazımı başlayınca |
| 3 | Player-build content-catalog doğrulaması (`anlati-addressables-smoke-evidence.md` §3) | İlk player build |
| 4 | **CI'da player-build job'ı YOK** (`.github/workflows/tests.yml` yalnız editmode+playmode) → `BuildValidationRunner.OnPreprocessBuild` CI'da HİÇ koşmuyor. Beş epic'in TÜM build-blocking check'leri yalnız yerel/manuel build'de devreye giriyor — bu, projenin tamamını ilgilendiren bir kapsam boşluğu | CI/DevOps iş kalemi |
| 5 | `m_BuildAddressablesWithPlayerBuild: 0` = makineye özel `EditorPref` (varsayılan true ama versiyon kontrolünde değil). Kapalı bir makinede player build Addressables içerik build'i koşmaz; gate yeşil kalır, runtime latch'ler | CI/DevOps iş kalemi |
| 6 | `FindObjectsByType` tabanlı TÜM sahne-scan check'leri (5 adet) yalnız açık sahneyi görür — prefab içindeki ya da build settings'te olmayan ihlaller görünmez | Ortak build-validation iş kalemi |
