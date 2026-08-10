# Story 001: IInteractable arayüzü + Registry çekirdeği + snapshot cache

> **Epic**: InteractableRegistry
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/etkilesim-sistemi.md` (arayüz sözleşmesi, "Aşağı akış arayüzü (IInteractable)" bölümü + registry Core Rules)
**Requirement**: `TR-etkilesim-001`, `TR-etkilesim-002` (register/deregister + snapshot çekirdek mekaniği — cross-session yarısı Story 002'de)
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0004
**ADR Decision Summary**: `InteractableRegistry`, çıplak statik bir sınıf — `List<IInteractable>` (HashSet değil, insertion order korunur), interface/implementasyon ayrımı yok. `_live` liste `OnEnable`/`OnDisable` üzerinden kendi kendini düzeltir (reset hook gerekmez); yalnız kare-başı snapshot cache'i (`_frameSnapshot`/`_snapshotFrame`) `FoundationBootstrap.ResetAll()`'a kayıtlı bir `ResetOnLoad()` gerektirir (cross-session `Time.frameCount` çakışması riski — unity-specialist BLOCKING bulgusu).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Saf C# + `Time.frameCount` — post-cutoff API yok, doğrulama gerekmez.

**Control Manifest Rules (bu katman)**:
- Required: `InteractableRegistry.Register`/`Deregister` yalnız `OnEnable`/`OnDisable`'dan çağrılır; registry `Snapshot()` üzerinden iterasyon (kare-stabil), asla canlı koleksiyon; yeni servis `FoundationBootstrap._resetSequence`'a belgeli sırada eklenir, kendi `[RuntimeInitializeOnLoadMethod]`'unu almaz
- Forbidden: `IInteractable` implementasyonu kalıcı sahnede yaşayamaz (registry'nin kendi-kendini-düzeltme varsayımını kırar)
- Guardrail: `Snapshot()` kare başına bir kez cache'lenir; `Time.frameCount`-anahtarlı cache'ler `ResetOnLoad()` ile temizlenir

## Acceptance Criteria

- [x] `IInteractable` arayüzü, `design/gdd/etkilesim-sistemi.md`'nin üye listesiyle BİREBİR: `InteractionType Type`, `float HoldDuration`, `bool CanInteract`, `string PromptText`, `void OnFocusEnter()`, `void OnFocusExit()`, `void OnInteract()`, `void OnHoldProgress(float t)`, `void OnHoldComplete()`, `void OnHoldCancelled()`, `void OnHoldBlocked()`, `bool SuppressDefaultHoldFill` (TR-etkilesim-001)
- [x] `InteractionType` enum tam olarak `{ Instant, Hold }`
- [x] `InteractableRegistry.Register`/`Deregister`, internal `List<IInteractable>`'a add/remove — insertion order bir mutasyon dizisi boyunca korunur (register A,B,C → deregister B → register D → sıra `[A,C,D]`) (TR-etkilesim-002)
- [x] `Snapshot()`: aynı `Time.frameCount` içinde tekrar çağrılırsa AYNI cache'lenmiş array referansını döner (deregister bile aynı karede yeniden hesaplamayı tetiklemez); ilk-hiç-çağrı (`_snapshotFrame` başlangıç sentinel'inde) yeniden hesaplar
- [x] Kopya-döndürme garantisi `_live`'a KAPSAMLI: dönen referans asla `_live`'ın kendisi değildir ve döndürülen koleksiyonu mutasyona uğratmak `_live`'ın gelecekteki içeriğini asla değiştiremez (aynı-kare cache mutasyonuna karşı tamper-proof iddiası YAPILMAZ — bilinçli, dar kapsam)
- [x] `ResetOnLoad()` YALNIZ `_frameSnapshot`/`_snapshotFrame`'i temizler — ayrı bir assert: reset sonrası `_live`'a önceden kayıtlı nesneler hâlâ `Snapshot()`'ta görünür; `FoundationBootstrap._resetSequence`'a kök konumda (yukarı bağımlılık yok, `IsikVolumeDurumSistemi`'den ÖNCE, belgeli `DocumentedOrder`'a göre) kayıtlı; `FoundationBootstrapOrderTest.ExpectedActiveOrder` aynı değişiklikte güncellenir
- [x] Registry'nin public yüzeyinde (`InteractableRegistry` + `IInteractable`'ın kendisi) gerçek/decoy ayrımını sızdıran hiçbir alan/metot/parametre yok
- [x] Enumerasyon-ortası Deregister güvenliği: bir çağıran zaten alınmış bir `Snapshot()`'ı `foreach` ile gezerken, gezilen nesnelerden biri kendini `Deregister` ederse (`OnDisable` tetiklenerek) — enumerasyon `InvalidOperationException` fırlatmaz, gezilen küme mutasyon-öncesi haliyle tamamlanır (GDD Edge Case, registry'ye ait — "registry iterasyonu sırasında kendini silme")

## Implementation Notes

- ADR-0005'in "addendum" desenine benzer şekilde, ADR-0004'ün Key Interfaces kod bloğu implementasyon kaynağıdır — kopyala, türetme. **TEK bilinçli sapma**: `_frameSnapshot`/`_snapshotFrame` ADR taslağında `private static` ama burada `internal static` olacak — Story 002'nin PlayMode testinin cross-session cache-çakışmasını doğrudan kurabilmesi için (QL-STORY-READY gate bulgusu; `ShiftZone`'un internal alanları ve `FoundationBootstrap.ActiveResetOrder`'la aynı emsal). Bu sapmayı kod yorumunda işaretle.
- `List` (HashSet değil) zorunlu — insertion order, bu story'nin implement ETMEDİĞİ ama `Etkileşim Sistemi` (Core epic) tarafından tüketilecek TR-etkilesim-008'in tie-break rehberi için taşıyıcı altyapıdır.
- Lazy-once-per-frame cache deseni kırılgan: bir kod yorumu ekle — bu deseni farklı tutarlılık gereksinimleri olan bir sisteme kopyalama uyarısı VE `Snapshot()`'ın yalnız ana iş parçacığından/`Update`-döngüsü zamanlamasından çağrılması gerektiği uyarısı (`FixedUpdate`'te kullanılırsa bu varsayım yeniden doğrulanmalı) — ADR-0004 Consequences/Risks bölümlerinin ikisi de bunu açıkça flag'liyor.
- `IInteractable` ve `InteractableRegistry` dosyaları Foundation kökünde flat kalır (`ProjectEpsilon.cs`/`FoundationBootstrap.cs` emsali) — ayrı bir alt klasör gerekmez, sınıf/arayüz sayısı azdır.

## Out of Scope

- `IInteractable`'ı implement eden gerçek `MonoBehaviour` tüketicileri (Görev/Taşıma pickup'ları, Anı-Tetikleyici anı-tetikleyicileri — gelecekteki Feature epic'leri)
- SphereCast odak-tespiti (Etkileşim Sistemi, Core epic)
- FPC'nin `Snapshot()`'ı gerçekten okuyup yaklaşma-yavaşlama `d` değişkenini hesaplaması (TR-fpc-004'ün wiring yarısı — birinci-sahis-kontrolcu epic'i; bu story yalnız registry'yi Foundation-sahipli ve okunabilir yapıp katman-relocate yarısını sağlar)
- Story 002: iki-oturum self-correction + cross-session cache-collision ampirik kanıtı

## QA Test Cases

*(QL-STORY-READY full modda koştu — general-purpose subagent, qa-lead rolünde; GAPS bulundu ve aşağıdaki AC'lere işlendi.)*

- **AC-1**: `IInteractable` üye listesi verbatim
  - Given: `typeof(IInteractable)`, Foundation assembly'sine derlenmiş
  - When: reflection tüm interface üyelerini (property + metot) numaralandırır
  - Then: üye kümesi GDD'nin 12 üyesiyle birebir eşleşir — ne fazla ne eksik, imzalar dahil
  - Edge cases: gelecekte GDD güncellenmeden eklenen 13. üye bu testi kasıtlı kırar (bilerek kırılgan, `FoundationBootstrapOrderTest` felsefesiyle aynı)

- **AC-2**: `InteractionType` enum şekli
  - Given: `typeof(InteractionType)`
  - When: `Enum.GetNames` okunur
  - Then: tam olarak `{ Instant, Hold }`, başka üye yok
  - Edge cases: yalnız arity — yapısal drift guard'ı

- **AC-3**: Register/Deregister, insertion order mutasyon boyunca korunur
  - Given: boş registry (test fixture teardown'da her şeyi deregister eder), 4 sahte `IInteractable` A/B/C/D
  - When: `Register(A)`, `Register(B)`, `Register(C)`; `Deregister(B)`; `Register(D)`
  - Then: sonraki `Snapshot()` tam olarak `[A, C, D]` sırasıyla döner
  - Edge cases: kayıtlı olmayan bir nesneyi `Deregister` etmek sessiz no-op (List.Remove semantiği); aynı instance'ı iki kez `Register` etmek iki ayrı girdi üretir (Set değil, List — bilerek, yorumda belgelenir)

- **AC-4**: `Snapshot()` lazy-once-per-frame, aynı cache'lenmiş instance
  - Given: 3 kayıtlı sahte nesne
  - When: `Snapshot()` aynı `Time.frameCount` içinde iki kez çağrılır (EditMode: aynı senkron çağrı yığını, kare ilerlemez)
  - Then: iki çağrı da TAM AYNI array referansını döner (`ReferenceEquals`, sadece eşit içerik değil)
  - When (devam): bir nesne deregister edilir, `Snapshot()` aynı karede tekrar çağrılır
  - Then: hâlâ orijinal cache'lenmiş referansı döner (deregister aynı-kare yeniden hesaplamayı TETİKLEMEMELİ)
  - Edge cases: ilk-hiç `Snapshot()` çağrısı (`_snapshotFrame` gerçek başlangıç sentinel'i `-1`'de) yeniden hesaplamalı, `Array.Empty` döndürmemeli

- **AC-5**: Kopya-döndürme garantisi, `_live`'a kapsamlı
  - Given: 2 kayıtlı sahte nesne
  - When: `Snapshot()` çağrılır, dönen koleksiyon `IInteractable[]`'a cast edilir, `[0]` elemanı yabancı bir nesneyle değiştirilir
  - Then: 3. bir nesne register edilip YENİ bir `Snapshot()` alınınca (sonraki kare) `_live`'ın gerçek üyeliği görünür (orijinal 2 + yeni 1) — mutasyon asla `_live`'a ulaşmadı
  - Edge cases: mutasyona uğramış array'in aynı kare içinde "kendiliğinden düzeldiği" İDDİA EDİLMEZ (AC-4 gereği aynı cache instance'ı hayatta kalır) — bu test yalnız ADR'ın harfi olan dar garantiyi (`_live` asla döndürülen referans değil, `_live`'ın geleceği korunur) doğrular

- **AC-6**: `ResetOnLoad()` yalnız cache'i temizler + `FoundationBootstrap` kaydı
  - Given: 2 kayıtlı sahte nesne, bir `Snapshot()` zaten alınmış (cache dolu)
  - When: `ResetOnLoad()` doğrudan çağrılır
  - Then (cache temizlendi): sonraki `Snapshot()` yeniden hesaplar (reset-öncesi cache'ten farklı array referansı)
  - Then (`_live` dokunulmadı): yeniden hesaplanan `Snapshot()` hâlâ orijinal kayıtlı 2 nesneyi içerir
  - Then (wiring): `FoundationBootstrap.ActiveResetOrder`, index 0'da `"InteractableRegistry"` içerir; `FoundationBootstrapOrderTest.ExpectedActiveOrder` aynı değişiklikte bunu başa ekleyecek şekilde güncellenir
  - Edge cases: sıfır kayıtlı öğe + zaten-boş cache'le `ResetOnLoad()` çağrısı güvenli no-op (exception yok, bayat referans yok)

- **AC-7**: Yapısal gerçek/decoy ayrım-sızdırmazlığı
  - Given: `typeof(InteractableRegistry)`
  - When: reflection public statik üyeleri numaralandırır
  - Then: tam olarak `Register(IInteractable)`, `Deregister(IInteractable)`, `Snapshot()` — "gerçek"/"decoy" ayrımı yapan hiçbir overload/parametre/property yok
  - Edge cases: `IInteractable`'ın kendisi de (AC-1'in yansıtılmış üye kümesi) böyle bir bayrak taşımadığı ayrıca assert edilir — ikisi birlikte "yapısal ayrım-sızdırmazlığı"nın iki yarısını kapatır

- **AC-8**: Enumerasyon-ortası Deregister güvenliği (registry'ye ait GDD Edge Case)
  - Given: 3 kayıtlı sahte nesne, bu kare içinde bir `Snapshot()` alınmış
  - When: çağıran dönen snapshot'ı `foreach` ile gezer; döngü ortasında gezilen nesnelerden biri kendini `Deregister` eder (bir nesnenin odaklanma sırasında kendini disable etmesini simüle eder)
  - Then: enumerasyon `InvalidOperationException` fırlatmadan tamamlanır, gezilen küme mutasyon-öncesi TAM 3 öğedir (çağıranın elindeki snapshot, eşzamanlı `_live` mutasyonundan etkilenmez)
  - Edge cases: SONRAKİ karenin `Snapshot()`'ı doğru şekilde yalnız kalan 2 öğeyi yansıtır — izolasyonun geçici-by-design olduğunu (kalıcı bir desync değil, an-be-an kopya) kanıtlar

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/interactable_registry_core_test.cs`
**Status**: [x] Created — 15 test, EditMode süiti 83/83 (2026-08-10)

## Dependencies

- Depends on: None (Foundation kökü — yukarı bağımlılık yok, ADR-0004)
- Unlocks: Story 002; Etkileşim Sistemi (Core epic), Görev/Taşıma + Anı-Tetikleyici (Feature epic'leri), Birinci Şahıs Kontrolcü'nün yaklaşma-yavaşlama okuması

## Completion Notes
**Completed**: 2026-08-10
**Criteria**: 8/8 passing (EditMode 83/83 lokal CLI — Logic story'nin kendi gereksinimi bu, PlayMode gerekmiyor)
**Deviations**: Tek bilinçli ADR-sapması — `_frameSnapshot`/`_snapshotFrame` ADR taslağında `private`, burada `internal` (Story 002'nin PlayMode testinin cross-session cache-çakışmasını doğrudan kurabilmesi için; LP review: doğru kapsamlı, public yüzeye sızmıyor). Advisory (non-blocking, ikisi de): AC-1'in reflection testi yalnız üye ismi kontrol ediyor, imza değil — derleme-zamanı `FakeInteractable` implementasyonuyla zaten güvence altında.
**Yan bulgu (Story 001'i bloklamıyor)**: PlayMode süiti doğrulama amaçlı 3 kez koşuldu, her seferinde FARKLI, önceden-kapanmış bir isik-volume testinde ortam kaynaklı flake gözlendi (Story 001'in koduna sıfır örtüşme) — ayrı takip görevi olarak bırakıldı (`task_d5aee2cb`, isik-volume PlayMode timing flakiness araştırması).
**Test Evidence**: Logic — `game/Assets/Tests/EditMode/interactable_registry_core_test.cs` (15 test)
**Code Review**: Complete — LP-CODE-REVIEW: APPROVE, QL-TEST-COVERAGE: ADEQUATE (full mod, general-purpose subagent gate'leri)
