# Story 004: Automatic izleme + histerezis + co-residency + OnDestroy garantisi

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/isik-volume-durum-sistemi.md`
**Requirement**: `TR-isik-009`, `TR-isik-010`, `TR-isik-011`, `TR-isik-018`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0005 (primary — "Automatic-zone monitoring" + "Scene-unload completion guarantee" alt bölümleri)
**ADR Decision Summary**: Automatic bölge OnEnable'da pozisyon-monitor coroutine başlatır (Dormant boyunca `Vector3.Distance` per-frame); ManualOnly hiçbir şey başlatmaz (Dormant'ta gerçekten sıfır maliyet); Dormant'tan çıkınca AYNI coroutine R_exit histerezisi + tick'i devralır (iki coroutine değil). `OnDestroy`: mid-transition → terminal duruma zorla-tamamla + event (güvenlik gerekçesi MEKANSAL MESAFE, sahne-aktifliği DEĞİL). Co-residency: sahnesi aktif olmayan bölgede POZİSYON örneklemesi atlanır ama zaman-tabanlı `x` ASLA donmaz.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `SceneManager.GetActiveScene()` per-tick ucuz sorgu (GDD'nin kendi notu); ek event aboneliği YOK.

**Control Manifest Rules (bu katman)**:
- Required: tek coroutine, durum-kapılı iki sorumluluk; oyuncu pozisyonu için `PlayerStateProvider`-benzeri canlı erişim (FPC yoksa test-injected)
- Forbidden: ManualOnly'de self-trigger; `x`'i pozisyon donmasıyla birlikte dondurmak
- Guardrail: R_trigger_min = PlayerMaxSpeed × tick aralığı (belge notu — pratikte bağlayıcı değil)

## Acceptance Criteria

- [ ] Automatic bölge: R_trigger girişinde kendini tetikler (AC2 — Held'e ulaşır, R_exit içinde kaldıkça Held kalır)
- [ ] ManualOnly bölge: R_trigger içinde durulsa bile ASLA self-trigger (AC2a); Dormant'ta hiçbir coroutine koşmaz
- [ ] Held→R_exit dışına çıkış → Shifting-Out (AC3a); R_exit içi gidiş-geliş → kesintisiz Held (AC3b)
- [ ] Işınlanma-çıkışı: tick'ler arası süreksiz pozisyon → sonraki tick "R_exit dışında" tespitiyle normal Shifting-Out; kalıcı Held sızıntısı yok (AC18)
- [ ] Hızlı-atlama: tek tick'te bölgeyi tamamen atlayan hareket → hiç tetiklenmez, event yok (AC19 — belgeli kısıt)
- [ ] Co-residency: bölgenin sahnesi aktif değilken pozisyon örneklemesi atlanır (giriş/çıkış tespiti yok) AMA in-flight `x` her karede ilerler — Held'e kimse izlemese de ulaşır, event normal fırlar
- [ ] `OnDestroy` mid-transition: Shifting-In→Held / Shifting-Out→Dormant anında tamamlanır (weight/ışıklar terminal değere) + terminal event teardown'dan ÖNCE fırlar

## Implementation Notes

- Oyuncu pozisyon kaynağı: FPC henüz yok — `Func<Vector3>` injected sampler (production'da kamera/Player transform'una bağlanacak; birinci-sahis epic'i wiring'i günceller). Test bunu doğrudan sürer.
- Co-residency testi: ikinci bir sahne yaratıp aktif sahneyi değiştirerek (`SceneManager.SetActiveScene`) simüle edilir.

## Out of Scope

- Story 005: Persistent'ın "çıkış kontrolü hiç koşmaz" kuralı burada YALNIZ non-Persistent yolları için yazılır
- FPC gerçek pozisyon wiring'i

## QA Test Cases

*(QL-STORY-READY atlandı.)*

- **AC2/2a/3a/3b (UnityTest)**: sampler'ı hareket ettirerek giriş/histerezis matrisi; ManualOnly'de sampler R_trigger içinde → Dormant kalır
- **AC18 (UnityTest)**: Held'de sampler'ı tek adımda uzağa taşı → Shifting-Out başlar
- **AC-6 (UnityTest)**: co-residency — aktif olmayan sahnedeki bölge Shifting-In'deyken `x` ilerler → Held event'i gelir; ama sampler R_trigger'a girse bile YENİ tetikleme olmaz
- **AC-7 (UnityTest)**: mid-Shifting-In `Destroy(zone)` → Held event'i teardown öncesi tam bir kez; weight=1

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/isik_volume_monitoring_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 003
- Unlocks: Story 005
