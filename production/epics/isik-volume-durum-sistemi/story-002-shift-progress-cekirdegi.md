# Story 002: Shift progress çekirdeği + guard rail'ler (saf)

> **Epic**: Işık/Volume Durum Sistemi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-09

## Context

**GDD**: `design/gdd/isik-volume-durum-sistemi.md` (Formulas bölümü — tamamı)
**Requirement**: `TR-isik-003`, `TR-isik-008`, `TR-isik-010`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0005 (primary)
**ADR Decision Summary**: Koşan durum HER ZAMAN `x`'tir (asla ElapsedTime/ShiftProgress değil); `ShiftProgress = 3x²−2x³` her kare taze; interrupt = `x` delta işaretinin flip'i, süreklilik garantili (pop yok); guard rail'ler kodda zorunlu (tasarımcı disiplinine bırakılmaz).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Saf C# — Unity API yok; `[Test]`-only.

**Control Manifest Rules (bu katman)**:
- Required: saf state-machine sınıfı (test edilebilir, MonoBehaviour'sız); epsilon sabitleri proje geneli tek yerde
- Forbidden: `ShiftProgress`'i saklamak/tersine çözmek; guard'sız Inspector değeri tüketmek
- Guardrail: per-tick hesap allocation'sız

## Acceptance Criteria

- [ ] Saf `ShiftProgressMachine` (ya da eşdeğeri): `x` birikimi (`+= dt/Duration` in, `-= dt/Duration` out, 0-1 clamp), `ShiftProgress` her çağrıda taze `3x²−2x³`
- [ ] Yön-flip interrupt: Shifting-Out'ta Trigger → mevcut `x`'ten In'e; Shifting-In'de Revert → mevcut `x`'ten Out'a; hiçbir durumda `x` sıfırlanmaz
- [ ] Epsilon sabitleri tek statik evde: `TIME_EPSILON=0.01f`, `RADIUS_EPSILON=0.01f`, `HYSTERESIS_EPSILON=0.001f`
- [ ] Guard'lar: `Duration ≥ TIME_EPSILON`; `k_hysteresis ≥ 1.0+HYSTERESIS_EPSILON` (tam 1.0 da clamp'lenir); `MemoryIntensityMultiplier ∈ [0.0, 1.0)`; `R_trigger ≥ RADIUS_EPSILON`
- [ ] Formül yardımcıları: `R_exit = R_trigger × k_hysteresis`; `BoxHalfExtentMin = R_exit + PlayerMaxSpeed×Duration + SafetyBuffer`; `LightColor/LightIntensity` lerp çifti (aynı ShiftProgress'ten — lockstep yapısal)
- [ ] AC12/13/14c2 sayısal doğrulamaları testte: x=0.25→0.15625(±0.001); örnek renk/yoğunluk (±1/±0.01); 4.6+4.8+0.9=10.3(±0.01)

## Implementation Notes

- `PlayerMaxSpeed` (1.6) parametre olarak alınır — FPC sabitine bağlama birinci-sahis epic'inde (TR-isik-018'in wiring yarısı orada; formül burada).
- GDD Formulas bölümündeki tablolar/örnekler test verisinin kaynağı — kendi örnek uydurma.

## Out of Scope

- Story 003: coroutine/Volume/Light sürüşü — bu story yalnız saf hesap
- FPC sabit wiring'i

## QA Test Cases

*(QL-STORY-READY atlandı.)*

- **AC-1 (otomatik)**: smoothstep örnekleri — x=0.25→0.15625; x=0.5→0.5; uçlarda türev ~0 (x=0.01 ile lineerden yavaş)
- **AC-2 (otomatik)**: flip sürekliliği — In'de x=0.6'da Revert → Out, ShiftProgress aynı değerden azalmaya başlar (kare farkı ≤ tek tick delta)
- **AC-4 (otomatik)**: dört guard'ın her biri — dejenere girdi → clamp'li değer (0 Duration→0.01; k=1.0→1.001; M=-0.5→0.0; M=1.0→<1.0; R=0→0.01)
- **AC-5/6 (otomatik)**: formül sayıları GDD örnekleriyle birebir

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/isik_volume_progress_test.cs`
**Status**: [x] Created — 14 test, EditMode süiti 53/53 (2026-08-09)

## Dependencies

- Depends on: Story 001 (ShiftState/ShiftConfig tipleri)
- Unlocks: Story 003

## Completion Notes
**Completed**: 2026-08-09
**Criteria**: 6/6 passing (tüm sayısal beklentiler GDD Formulas örneklerinden — QA lead cross-check'li)
**Deviations**: ADVISORY — (1) `Mathf`/`Color` kullanımı Engine Notes'un "Saf C#, Unity API yok" harfine aykırı, özünde uyumlu (deterministik struct/statik, MonoBehaviour/sahne yok, [Test]-only; `Color` AC-5 lerp-çifti sözleşmesinin gereği). (2) `MemoryIntensityCeiling` = 0.999 bağımsız sabit (GDD "<1.0" için sayı vermiyordu; hysteresis'ten BİLEREK türetilmedi — retune bağımsızlığı yorumda). İleri bayrak: `BoxHalfExtentMin`'in `SafetyBuffer`/`rExit` girdi guard'ları wiring/validation sahasında (Story 003/006) ele alınacak.
**Test Evidence**: Logic — `game/Assets/Tests/EditMode/isik_volume_progress_test.cs` (14 test); süit EditMode 53/53, PlayMode 9/9
**Code Review**: Complete — LP-CODE-REVIEW: APPROVE, QL-TEST-COVERAGE: ADEQUATE (full mod; reviewer önerileri kapanış öncesi uygulandı: başlangıç-durumu + guard-kompozisyon + negatif-girdi testleri, toleranslı Color assert'leri, bağımsız tavan sabiti)
