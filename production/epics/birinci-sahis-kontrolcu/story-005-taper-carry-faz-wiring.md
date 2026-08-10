# Story 005: Taper wiring + IsCarrying aynası + faz akümülatörü entegrasyonu

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/birinci-sahis-kontrolcu.md` (Core Rules — yaklaşma-yavaşlaması, IsCarrying; Edge Cases — çoklu bayraklı nesne)
**Requirement**: `TR-fpc-004` (wiring yarısı), `TR-fpc-013`, `TR-fpc-014` (wiring yarısı)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0003 (primary); ADR-0004 (secondary — `InteractableRegistry` okuması)
**ADR Decision Summary**: Formül 2'nin `d` girdisi, `InteractableRegistry.Snapshot()`'taki TÜM kayıtlı `IInteractable`lara olan mesafelerin minimumu olarak hesaplanır (ADR-0004: Foundation'ın Foundation'ı okuması, katman-ihlali yok). `SetCarrying(bool)` — yalnız Görev/Taşıma Döngüsü'nün çağıracağı tek-yazıcı yüzey (mekanizma ADR-0013/Feature epic'inde; bu epic YALNIZ yüzeyi sağlar).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: `InteractableRegistry.Snapshot()` allocation'sız kare-başı cache — bu story'nin mesafe taraması onu tüketir, yeniden implement etmez.

**Control Manifest Rules (bu katman)**:
- Required: `Snapshot()` üzerinden iterasyon (kare-stabil), asla canlı koleksiyon; single-caller writes (convention+XML-doc — `SetCarrying` yalnız Görev/Taşıma çağıracak)
- Forbidden: —
- Guardrail: erişilebilirlik motion slider'ı yalnız görsel genliği ölçekler — akümülatör ilerleme hızı asla ölçeklenmez (gerçek wiring'de de geçerli, yalnız saf matematikte değil)

## Acceptance Criteria

- [ ] Taper'ın `d` değişkeni `InteractableRegistry.Snapshot()` üzerinden UÇTAN UCA hesaplanır — GDD AC6/7/8'i gerçek wiring'le gerçekleştirir (stub `d` değil) (TR-fpc-004 wiring yarısı)
- [ ] **Çoklu bayraklı nesne, minimum kazanır, sıçrama yok**: iki bayraklı nesne aynı anda 1.5m yarıçapındaysa `d`=ikisine olan mesafenin minimumu; oyuncu hareket edip minimum-mesafe geçiş noktası değişince `TaperMult`/`v_target` o karede SIÇRAMAZ (GDD Edge Case, geçiş sürekli)
- [ ] `SetCarrying(bool)` — tek-yazıcı yüzey (convention+XML-doc: yalnız Görev/Taşıma Döngüsü çağıracak, henüz yazılmamış Feature epic'i); çağrıldığında `IsCarrying` ANINDA aynalanır; aynı değerle tekrarlanan çağrılar güvenli no-op (TR-fpc-013 — bu story YALNIZ yüzeyi sağlar, `CarrySlotRigController` mekanizması ADR-0013'te)
- [ ] Faz akümülatörü gerçek `FirstPersonController`'a bağlanır — bob eğrisi VE ayak sesi tetikleme noktaları aynı paylaşılan faz değerinden okunur, `v(t)` sürekli değişse de asla kaymaz (TR-fpc-014 wiring yarısı)
- [ ] **Erişilebilirlik bağımsızlığı, gerçek wiring'de** (Story 002'nin saf-matematik testinin entegrasyon kanıtı): gerçek `FirstPersonController` üzerinde özdeş hareket S=0% ve S=100%'de sürülünce ayak-sesi-tetikleme zaman damgaları birebir aynı — yalnız görsel bob genliği farklı (manifest guardrail, wiring seviyesinde de doğrulanır)

## Implementation Notes

- Story 002'nin saf `TaperMult`/`v_target`/faz-akümülatörü metotlarını doğrudan çağır — matematiği burada yeniden yazma.
- `d` hesaplama: `InteractableRegistry.Snapshot()`'ı gez, her `IInteractable`'ın (varsa) pozisyon kaynağına olan mesafeyi al, minimumu döndür — `IInteractable` arayüzünün kendisinde pozisyon YOK (interactable-registry epic'i bunu tanımlamadı); bu story ya `Component`/`Transform` cast'i ya da ayrı bir pozisyon-sağlayıcı sözleşmesi gerektirebilir — implementasyon sırasında netleştirilecek açık nokta, ADR'da önceden çözülmedi.
- `SetCarrying`'in tek-çağıran kısıtı bu projenin yerleşik deseni (AddFiredTrigger, SetRoundState vb.) — derleme-zamanı zorlanmaz, convention+code review.

## Out of Scope

- `CarrySlotRigController` mekanizması (Görev/Taşıma epic'i, ADR-0013)
- `IInteractable`'a pozisyon alanı eklenmesi (eğer gerekirse, ayrı bir ADR-seviyesi karar — bu story'nin implementasyon notuna bkz.)

## QA Test Cases

*(QL-STORY-READY full modda koştu; erişilebilirlik-bağımsızlığının wiring-seviyesi kanıtı gate bulgusuyla eklendi.)*

- **AC-1 (otomatik)**: `d` uçtan uca gerçek registry'den
  - Given: sahnede gerçek kayıtlı `IInteractable`'lar
  - When: her karede `d` hesaplanır
  - Then: `InteractableRegistry.Snapshot()` üzerindeki minimum mesafeye eşit — GDD AC6/7/8 gerçek wiring'le yeniden üretilir

- **AC-2 (otomatik)**: Çoklu bayraklı nesne, minimum, sıçramasız geçiş
  - Given: iki bayraklı nesne, oyuncu ikisi arasında hareket ediyor, göreli mesafe kesişiyor
  - When: `d` her karede izlenir
  - Then: her zaman minimum; kesişim karesinde `TaperMult`/`v_target` süreksizlik göstermez

- **AC-3 (otomatik)**: SetCarrying yüzeyi
  - Given: `SetCarrying(bool)` çağrılır
  - Then: `IsCarrying` anında aynalanır
  - Edge cases: tekrarlanan aynı-değer çağrılar güvenli no-op

- **AC-4 (otomatik)**: Faz akümülatörü gerçek sürücüye bağlı
  - Given: sürekli değişen v(t)
  - Then: bob ve ayak-sesi tetikleyicileri her karede aynı paylaşılan faz değerinden türer, hiç kaymaz

- **AC-5 (otomatik)**: Erişilebilirlik bağımsızlığı, wiring seviyesi
  - Given: özdeş hareket dizisi gerçek FirstPersonController üzerinde tekrar oynatılır
  - When: S=0% ve S=100%'de ayrı ayrı
  - Then: ayak-sesi-tetikleme zaman damgaları iki koşuda birebir aynı — yalnız görsel bob genliği farklı

## Test Evidence

**Story Type**: Integration → `game/Assets/Tests/PlayMode/fpc_taper_carry_phase_test.cs`
**Status**: [ ] Not yet created

## Dependencies

- Depends on: Story 002 (saf matematik), Story 003 (FirstPersonController sürücüsü), interactable-registry epic (Complete)
- Unlocks: Görev/Taşıma Döngüsü epic'inin `SetCarrying` tüketimi; Anı-Tetikleyici/sıradan etkileşim nesnelerinin taper kamuflajı
