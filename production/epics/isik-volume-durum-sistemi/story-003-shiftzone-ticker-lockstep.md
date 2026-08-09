# Story 003: ShiftZone + ticker + lockstep

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: `design/gdd/isik-volume-durum-sistemi.md`
**Requirement**: `TR-isik-001`, `TR-isik-002`, `TR-isik-004`, `TR-isik-005`, `TR-isik-006`, `TR-isik-017`, `TR-isik-019`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0005 (primary)
**ADR Decision Summary**: `ShiftZone` MonoBehaviour: lokal `Volume` (isGlobal=false, paylaşılan `VolumeProfile`, `blendDistance=0`), box collider, `shiftId`, `TriggerMode`, Inspector-atanmış `ZoneLight[]`; per-zone TEK coroutine; `Volume.weight = ShiftProgress` TEK yazıcı; ışıklar aynı ShiftProgress'le lockstep; event yalnız gerçek durum geçişinde, facade'ın `RaiseShiftStateChanged`'i üzerinden.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW (mekanizma spike'la kanıtlı)
**Engine Notes**: URP `Volume`/`VolumeProfile` runtime API stabil; test fixture'ları `CreateInstance`/runtime-created — asla on-disk asset.

**Control Manifest Rules (bu katman)**:
- Required: Story 002'nin saf makinesi coroutine içinde kullanılır (mantık MonoBehaviour'a gömülmez); OnEnable register / OnDisable deregister
- Forbidden: `Volume.weight`'e ikinci yazıcı; Animator/Timeline key'i; `ShiftProgress` saklama
- Guardrail: ışık başına Update değil — bölge başına tek coroutine döngüsü

## Acceptance Criteria

- [ ] `ShiftZone`: `[SerializeField]` shiftId/TriggerMode/ZoneLight[]/Volume; `IShiftZoneHandle` implementasyonu; OnEnable'da facade'a register, OnDisable'da deregister
- [ ] `zoneCenter` açıkça ayarlanmamışsa Awake/OnValidate'te collider `bounds.center`'ına düşer (AC14c-pre), asla Vector3.zero/tanımsız kalmaz
- [ ] Durum makinesi Dormant→Shifting-In→Held→Shifting-Out→Dormant; coroutine her karede `Volume.weight=ShiftProgress` + tüm `ZoneLight` girdileri aynı değerle lockstep (AC13 çifti aynı karede)
- [ ] `TriggerShift` idempotent (aktifken false/no-op — AC6); Shifting-Out'ta çağrı → yön-flip + true (AC7); `RevertShift` Shifting-In'de yön-flip (AC8), inaktifte sessiz no-op (AC9); `IsShiftActive` yalnız Dormant'ta false (AC10); referans sayımı yok (AC11)
- [ ] Event her GERÇEK geçişte tam bir kez, doğru `(shiftId, newState, zoneCenter, R_trigger)` payload'la (AC15); iki bağımsız bölge birbirini etkilemez (AC1)
- [ ] Baked lightmap/lightmap seti koduna hiç dokunulmaz (TR-isik-017 yapısal)

## Implementation Notes

- Spike (`prototypes/yankilar-volume-weight-spike/`) davranışın kanıtı — mekanizma iddiası yapma, Box Safety Margin formülüne göre boyutlandırılmış kutu + blendDistance=0 pratik kuralı yorumda kalsın.
- PlayMode testleri sahneye runtime'da kurulur (GameObject + Volume + fake Light'lar); paylaşılan profil `ScriptableObject.CreateInstance<VolumeProfile>()`.

## Out of Scope

- Story 004: Automatic izleme/histerezis/co-residency/OnDestroy — bu story'de bölgeler yalnız dış çağrıyla sürülür
- Story 005: Persistent/restore — Story 006: build check'leri

## QA Test Cases

*(QL-STORY-READY atlandı; AC numaraları GDD'nin kendi numaralı listesinden.)*

- **AC6/7/8/9/10/11 (UnityTest)**: sözleşme matrisi — idempotentlik, çift yön-flip sürekliliği (weight sıçramaz: ardışık kare delta ≤ tek tick), no-op'lar, aktiflik tanımı, tek Revert yeter
- **AC13 (UnityTest)**: lockstep — herhangi bir karede `light.color` ve `light.intensity` aynı ShiftProgress'ten (örnek değerler ±1/±0.01)
- **AC15 (UnityTest)**: mock abone — geçiş başına tam bir event, payload birebir
- **AC1 (UnityTest)**: iki bölge bağımsız Held; eventler ayrı ayrı

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/isik_volume_shiftzone_test.cs`
**Status**: [x] Created — 9 UnityTest, PlayMode süiti 18/18 (2026-08-09)

## Dependencies

- Depends on: Story 001, Story 002
- Unlocks: Story 004, Story 005, Story 006

## Completion Notes
**Completed**: 2026-08-09
**Criteria**: 6/6 passing (EditMode 53/53, PlayMode 18/18 lokal CLI)
**Deviations**: (1) ZoneLight per-light MUTLAK memoryIntensity (ADR Key Interfaces şekli; GDD'nin config-çarpanına matematiksel eşdeğer, memI = baseI × M — başlık yorumunda belgeli). LP bulgusuyla guard eşdeğerliği de geri getirildi: `ClampMemoryIntensity(mem, base)` mutlak-form guard'ı, ApplyProgress'te (GDD AC14a). (2) zoneCenter fallback `TransformPoint(box.center)` — bounds.center ile aynı nokta, fizik senkronu beklenmeden (AC14c-pre okuma tarzı, testte eşdeğerlik assert'li). (3) Asmdef'lere Unity.RenderPipelines.Core.Runtime eklendi.
**İleri bayraklar**: Story 005 — Out→In flip'te config bilinçli korunuyor (Persistent'lı yeni config benimsenmez; kodda işaretli), yeniden değerlendirilecek. Story 006 — _zoneCenter'ın OnValidate world-space bake'inin taşınmış objede bayatlaması için drift build-check'i. ADR addendum'ın IShiftZoneHandle yorumu AC7 dalını basitleştiriyor (metin düzeltmesi lead-architect kararı).
**Test Evidence**: Integration — `game/Assets/Tests/PlayMode/isik_volume_shiftzone_test.cs` (9 UnityTest). Debug notu: bekleme yardımcılarında Approximately yerine KESİN eşitlik (uçlar clamp'li/tam; Approximately Held-öncesi erken çıkıp geçiş kaçırtıyor — ilk koşu 14/15 bu yüzdendi).
**Code Review**: Complete — LP-CODE-REVIEW: CONCERNS→tümü giderildi (AC14a clamp, reentrancy, doc'lar); QL-TEST-COVERAGE: GAPS→üç test birebir eklendi (orta-In Revert, cycle-sonrası restart, disable/re-enable kurtarma). Full mod, general-purpose subagent gate'leri.
