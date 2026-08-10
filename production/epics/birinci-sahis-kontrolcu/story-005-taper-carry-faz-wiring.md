# Story 005: Taper wiring + IsCarrying aynası + faz akümülatörü entegrasyonu

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M (~3-4h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

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

- [x] Taper'ın `d` değişkeni `InteractableRegistry.Snapshot()` üzerinden UÇTAN UCA hesaplanır — GDD AC6/7/8'i gerçek wiring'le gerçekleştirir (stub `d` değil) (TR-fpc-004 wiring yarısı)
- [x] **Çoklu bayraklı nesne, minimum kazanır, sıçrama yok**: iki bayraklı nesne aynı anda 1.5m yarıçapındaysa `d`=ikisine olan mesafenin minimumu; oyuncu hareket edip minimum-mesafe geçiş noktası değişince `TaperMult`/`v_target` o karede SIÇRAMAZ (GDD Edge Case, geçiş sürekli)
- [x] `SetCarrying(bool)` — tek-yazıcı yüzey (convention+XML-doc: yalnız Görev/Taşıma Döngüsü çağıracak, henüz yazılmamış Feature epic'i); çağrıldığında `IsCarrying` ANINDA aynalanır; aynı değerle tekrarlanan çağrılar güvenli no-op (TR-fpc-013 — bu story YALNIZ yüzeyi sağlar, `CarrySlotRigController` mekanizması ADR-0013'te)
- [x] Faz akümülatörü gerçek `FirstPersonController`'a bağlanır — bob eğrisi VE ayak sesi tetikleme noktaları aynı paylaşılan faz değerinden okunur, `v(t)` sürekli değişse de asla kaymaz (TR-fpc-014 wiring yarısı)
- [x] **Erişilebilirlik bağımsızlığı, gerçek wiring'de** (Story 002'nin saf-matematik testinin entegrasyon kanıtı): gerçek `FirstPersonController` üzerinde özdeş hareket S=0% ve S=100%'de sürülünce ayak-sesi-tetikleme zaman damgaları birebir aynı — yalnız görsel bob genliği farklı (manifest guardrail, wiring seviyesinde de doğrulanır)

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

**Story Type**: Integration → `game/Assets/Tests/PlayMode/fpc_taper_carry_phase_test.cs` (16 test)
**Status**: [x] Created — EditMode 120/120, PlayMode 77/77

## Dependencies

- Depends on: Story 002 (saf matematik), Story 003 (FirstPersonController sürücüsü), interactable-registry epic (Complete)
- Unlocks: Görev/Taşıma Döngüsü epic'inin `SetCarrying` tüketimi; Anı-Tetikleyici/sıradan etkileşim nesnelerinin taper kamuflajı

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 5/5 AC, EditMode 120/120, PlayMode 77/77.

**Dosyalar**: `FirstPersonController.cs` (+`DistanceToNearestInteractable`, `AdvancePhaseAndApplyBob`, `MovementPhase`/`FootstepIndex`/`FootstepTriggered`/`CurrentBobAmplitude`/`CurrentTaperDistance`/`MotionIntensityPercent`, `StrideLength`), `PlayerStateProvider.cs` (`IsCarrying` → `private set` + `SetCarrying(bool)`), `Tests/PlayMode/fpc_taper_carry_phase_test.cs` (YENİ, 16 test).

**Story'nin AÇIK NOKTASI çözüldü**: `IInteractable`'da pozisyon yok (arayüze eklemek Out of Scope, ayrı ADR kararı) → güvenli `Component` cast'i + Unity fake-null kontrolü. Fake-null kontrolü gerçekten gerekli: `Snapshot()` kare-başı cache olduğundan aynı kare içinde YOK EDİLMİŞ bir obje listede kalabilir (ADR-0004 Risks) — testle kilitlendi.

**BLOCKING GATE BULGUSU (LP ve QL bağımsız olarak, aynı sayılarla)**: `d` 3B mesafe olarak hesaplanıyordu ve oyuncu Transform'u kapsülün TABANINDA. GDD'nin kendi kamuflaj decoy'ları el yüksekliğinde monte (kapı kolu, ışık anahtarı, termostat ~1.0-1.2m) — 3B mesafeyle `d` ASLA ~1.2'nin altına inemez, yani TaperMult ≈ 0.97: tasarlanan %30 yavaşlama yerine %3. Taper gerçek içerikte gürültüye inerdi ve CD-GDD-ALIGN'ın kapattığı "metal dedektörü" istismarı kısmen geri açılırdı (yer seviyesindeki nesne %30, el yüksekliğindeki %3 yavaşlatsa fark ipucu olurdu). Testler bunu görmüyordu çünkü tüm test nesneleri y=0'daydı. **Fix**: YATAY (xz) mesafe + yükseltilmiş prop testi.

**Diğer gate düzeltmeleri**: manifest'in ZORUNLU SOFT co-residency guard'ı eklendi (per-frame scene-aware mantık aktif sahneye kapılmalı — geçişte terk edilen sahnenin propları yavaşlatmamalı); `Snapshot()` girdi yokken çağrılmıyor (koşulsuz per-frame `_live.ToArray()` kalıcı çöp üretiyordu — ADR-0004'ün "no unconditional per-frame ToArray()" garantisi); `FootstepTriggered` payload'ı GERÇEKLEŞEN yatay hız (komut edilen değil — `Velocity` sözleşmesiyle tutarlı); bob `sin` → `-cos` (çukur tam ayak basışına denk gelir); `_eyeCamera.localPosition` tek-yazıcı sözleşmesi belgelendi (ADR-0013 carry-sway toplamsal ofset olarak katılmalı, ayrı yazıcı olarak değil).

**QL test düzeltmeleri**: AC-2'nin "minimum kazanır" yarısı hiç test edilmiyordu (hız üzerinden assert Formül 1'in yumuşatması yüzünden max/ilk/son kurallarını da geçirirdi) → `CurrentTaperDistance` gözlemlenebilirlik kancası + doğrudan `min` assert'i + kesişim taper'ın DİK bölgesine taşındı; kameranın gerçekten hareket ettiği hiç okunmuyordu (bob satırı silinse süit yeşildi) → salınım genliği + S=%0'da tam-dinlenme testi; GDD AC8'in çarpımsal bileşimi wiring'de test edilmiyordu; ışınlanmanın faz ilerletmediği (SOFT geçişte ~66 hayalet ayak sesi riski), geri yürüyüş, mid-motion `SetCarrying` (+dönüş yolu), yok-edilen interactable, Component-olmayan kayıt ve registry sıra-bağımsızlığı ön koşulu eklendi.

**AÇIK KARAR (kullanıcıya soruldu, kod şu an %40)**: motion slider varsayılanı iki onaylı belgede ÇELİŞİYOR — `accessibility-requirements.md` §5 "varsayılan %100" diyor, GDD Core Rules "~%40". Kod GDD'yi izliyor ama §5'i kaynak gösteriyordu (yorum düzeltildi). Gerçek Ayarlar yüzeyi henüz yok (§7 "bilinen boşluk"); değer o epic'te bağlanacak.
