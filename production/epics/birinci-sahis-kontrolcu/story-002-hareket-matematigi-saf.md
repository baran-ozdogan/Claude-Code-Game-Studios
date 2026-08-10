# Story 002: Hareket matematiği — ivmelenme, taper, head-bob (saf)

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/birinci-sahis-kontrolcu.md` (Formulas bölümü — tamamı: 1. İvmelenme, 2. Yaklaşma-Yavaşlaması Taper'ı, 3. Head-Bob Genliği; Edge Cases — Δt guard)
**Requirement**: `TR-fpc-011` (hesap yarısı), `TR-fpc-014` (hesap yarısı — wiring yarısı Story 005'te)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da; taper'ın `d` girdisi burada soyut bir `float` parametredir — `InteractableRegistry` okuması TR-fpc-004'ün wiring yarısı, Story 005)*

**ADR Governing Implementation**: ADR-0003 (secondary — bu story'nin izlediği saf-C#-matematik + ince-MonoBehaviour-sürücü ayrım desenini kurar; formüllerin kendisi ADR-seviyesi bir karar değil, GDD-kilitli içeriktir)
**ADR Decision Summary**: Yok — control-manifest'in Core katmanı zorunlu kuralı ("Pure C# state machine + thin MonoBehaviour driver split for every state machine — BLOCKING unit-test rule, source: ADR-0003/0010/0011/0013/0015") bu story'nin var oluş gerekçesidir. GDD Formulas bölümü tek doğruluk kaynağıdır.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Saf C# — `UnityEngine.Mathf`/`Vector3` dışında Unity API yok (isik-volume'un `ShiftProgressMachine`/`IsikVolumeFormulas` emsaliyle aynı desen); `[Test]`-only.

**Control Manifest Rules (bu katman)**:
- Required: Pure C# state machine + thin MonoBehaviour driver split (BLOCKING unit-test rule)
- Forbidden: —
- Guardrail: erişilebilirlik motion slider'ı YALNIZ GÖRSEL genliği ölçekler — faz akümülatörünün kendisi (ayak sesi/jostle zamanlaması) ASLA ölçeklenmez (`design/ux/accessibility-requirements.md` §4/§5/§6)

## Acceptance Criteria

- [x] **Formül 1 (ivmelenme)**: `v(t+Δt) = v_target + (v(t)-v_target)×e^(-k·Δt)`, `k=3/T_ramp` — `T_ramp` Tuning Knob aralığında (0.15-0.25s) tüketilir
- [x] **Formül 2 (taper)**: `x=clamp(d/1.5,0,1)`; `ease(x)=x²(3-2x)`; `TaperMult=0.7+0.3×ease(x)`; `v_target=1.6×CarryMult×TaperMult` — çarpımsal bileşim (toplamsal DEĞİL); `d` soyut float parametre (registry okuması Story 005)
- [x] **Formül 3 (head-bob)**: `Amplitude(v,S) = A_max×(v/1.6)×(S/100)` — S=0'da tam 0, v=0'da tam 0
- [x] Paylaşılan mesafe-tabanlı faz akümülatörü — SAAT ZAMANINA değil KAT EDİLEN MESAFEYE göre ilerler; bob eğrisi VE ayak sesi tetiklemeleri aynı paylaşılan faz değerinden okunur, v(t) sürekli değişse de asla birbirinden kaymaz (TR-fpc-014 hesap yarısı)
- [x] **Erişilebilirlik bağımsızlığı**: faz akümülatörünün İLERLEME HIZI, erişilebilirlik slider'ı `S`'den TAMAMEN bağımsızdır — aynı hareket S=0% ve S=100%'de sürülünce faz/ayak-sesi-tetikleme zaman damgaları birebir aynıdır, yalnız `Amplitude` çıktısı farklılaşır (`design/ux/accessibility-requirements.md` §4/§5/§6 — manifest guardrail)
- [x] Δt guard: anormal büyük Δt'de (frame hitch) `e^(-k·Δt)→0`, yani `v(t)` bir sonraki karede doğrudan `v_target`'a atlar — NaN/Infinity/aşma yok
- [x] GDD'nin sayısal örnekleri testte birebir doğrulanır: 1. kare v≈0.35 m/s (60fps, T_ramp=0.2s); ~12 kare (0.2s) sonra v≥1.52 m/s (%95); taşırken d=0.5m → TaperMult≈0.778, v_target≈1.05 m/s; **A_max=2.5cm, S=40, v=1.6 → Amplitude=1.0cm** (Formül 3'ün TEK GDD örneği)

## Implementation Notes

- isik-volume-durum-sistemi'nin `ShiftProgressMachine`/`IsikVolumeFormulas` ayrımıyla aynı desen: saf statik/sınıf metotları, Unity `MonoBehaviour` yaşam döngüsü yok.
- Faz akümülatörü: `Vector3` pozisyon farkının büyüklüğünü (mesafe deltası) girdi alan, saat `Time.deltaTime`'ını DEĞİL biriken mesafeyi ilerleten bir sayaç — Story 003'ün sürücüsü her karede gerçek pozisyon deltasını besler, bu story sentetik mesafe-delta dizileriyle test eder.
- `TR-fpc-014 (hesap yarısı)` + `TR-fpc-011 (hesap yarısı)`: `/architecture-review` traceability matrix'i için bu satır — gerçek wiring (Story 005'in `d` okuması, Story 003'ün gerçek `Time.deltaTime`/`CharacterController` beslemesi) ayrı story'lerde.

## Out of Scope

- `d`'nin `InteractableRegistry.Snapshot()`'tan hesaplanması (Story 005 — burada `d` soyut parametre)
- `CharacterController`/gerçek Input okuma (Story 003)
- Gerçek faz akümülatörü wiring'i (Story 005)

## QA Test Cases

*(QL-STORY-READY full modda koştu — isik-volume Story 002 emsaliyle karşılaştırıldı, sayısal çapaların GDD'nin kendi örnekleriyle birebir eşleşmesi doğrulandı; head-bob örneği gate bulgusuyla eklendi.)*

- **AC-1 (otomatik)**: Formül 1 — ilk-kare çapası
  - Given: v=0, v_target=1.6, T_ramp=0.2s (k=15/s), Δt=1/60s
  - When: bir adım uygulanır
  - Then: v≈0.35 m/s (±0.01)
  - Edge cases: simetrik yavaşlama durumu (v=1.6→v_target=0)

- **AC-2 (otomatik)**: Formül 1 — yakınsama
  - Given: aynı kurulum
  - When: ~12 kare (0.2s) geçer
  - Then: v≥1.52 m/s, hiçbir örneklenen karede 1.6'yı aşmaz
  - Edge cases: yavaşlamada 0.2-0.25s içinde ≤0.08 m/s'ye düşer

- **AC-3 (otomatik)**: Taper d≥1.5m'de etkisiz
  - Given: d=1.5m, CarryMult=1.0
  - When: v_target hesaplanır
  - Then: tam olarak 1.6

- **AC-4 (otomatik)**: Taper d=0'da maksimum
  - Given: d=0, CarryMult=1.0
  - When: hesaplanır
  - Then: tam olarak 1.12

- **AC-5 (otomatik)**: Taşırken çarpımsal bileşim
  - Given: CarryMult=0.84375, d=0.5m
  - When: hesaplanır
  - Then: x=0.333, ease=0.259, TaperMult=0.778(±0.001), v_target≈1.05(±0.01)
  - Edge cases: iki çarpanın sırası sonucu değiştirmez (a×b=b×a)

- **AC-6 (otomatik)**: Head-bob S=0'da sıfır
  - Given: S=0
  - When: herhangi bir v için Amplitude hesaplanır
  - Then: tam olarak 0

- **AC-7 (otomatik)**: Head-bob tavan + GDD örneği
  - Given: S=100, v=1.6
  - When: hesaplanır
  - Then: Amplitude==A_max tam
  - Edge cases: A_max=2.5cm, S=40, v=1.6 → Amplitude=1.0cm(±0.01) — GDD'nin tek Formül-3 örneği, birebir doğrulanır

- **AC-8 (otomatik)**: Frame-hitch guard
  - Given: Δt=0.5s simüle edilmiş hitch
  - When: uygulanır
  - Then: v tam olarak v_target'a atlar, NaN/Infinity/aşma yok
  - Edge cases: Δt=0; Δt=10s

- **AC-9 (otomatik)**: Faz akümülatörü determinizmi
  - Given: sentetik mesafe-delta dizisi
  - When: karelerde sürülür
  - Then: bob-faz ve ayak-sesi-faz aynı paylaşılan değerden okunur; mesafe deltası sıfırken zaman geçse de faz değişmez

- **AC-10 (otomatik)**: Erişilebilirlik bağımsızlığı (manifest guardrail)
  - Given: özdeş hareket dizisi
  - When: S=0% ve S=100%'de ayrı ayrı sürülür
  - Then: faz/ayak-sesi-tetikleme zaman damgaları iki koşuda birebir aynı — yalnız Amplitude çıktısı farklı

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/fpc_movement_math_test.cs`
**Status**: [x] Created — EditMode 21/21

## Dependencies

- Depends on: None (saf matematik, yukarı bağımlılık yok)
- Unlocks: Story 003 (CharacterController sürücüsü bu matematiği kullanır); Story 005 (taper/faz akümülatörü wiring'i)

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 8/8 AC (10/10 QA Test Case), EditMode 119/119 (proje geneli, PlayMode değişmedi — saf C#, MonoBehaviour yok).

**Dosyalar**: `Foundation/MovementMathFormulas.cs` (YENİ — Formül 1/2/3 + `WalkSpeed`/`TaperRadius`/`TaperMinMultiplier`/`TaperBonusRange`/`CarrySpeed`/`CarryMultiplier` kilitli sabitleri), `Foundation/MovementPhaseAccumulator.cs` (YENİ — `Advance(float distanceDelta)`, bilerek `deltaTime` almıyor: "mesafeye göre ilerler" garantisi API yüzeyinde yapısal, runtime dallanma değil), `Tests/EditMode/fpc_movement_math_test.cs` (YENİ, 21 test).

**Gate'ler (full mod)**: **LP-CODE-REVIEW CONCERNS→giderildi** — `CarrySpeed`/`CarryMultiplier` isimlendirilmiş sabit olarak eklendi (önceden yalnız test dosyasında çıplak `0.84375f` literal'iydi); `HeadBobAmplitude` girdileri (`speed`/`accessibilitySliderPercent`) isik-volume `IsikVolumeFormulas` emsaliyle aynı defense-in-depth ilkesiyle guard'landı. AC-8'in yorumu (Δt=0.5s "yaklaşık" vs Δt=10s "tam" atlama ayrımı, float32 alt-taşma sınırına dayanarak) LP tarafından doğru mühendislik kararı olarak onaylandı — production kodunda icat edilmiş bir snap-to-target özel-durumu YOK. **QL-TEST-COVERAGE GAPS→giderildi**: T_ramp=0 guard testi eklendi (isik-volume `ClampDuration=0` emsaliyle aynı desen); `HeadBobAmplitude(v=0, ...)` sınır testi eklendi (önceden yalnız S=0 test ediliyordu, v=0 hiç test edilmemişti); AC-10'un bağımsızlık testi tek-koşu-iki-kez-okuma yerine gerçekten İKİ bağımsız simülasyon koşusuna (`SimulateAndSampleAmplitude` helper) genişletildi.

**Test Evidence**: `game/Assets/Tests/EditMode/fpc_movement_math_test.cs` (21 test).
