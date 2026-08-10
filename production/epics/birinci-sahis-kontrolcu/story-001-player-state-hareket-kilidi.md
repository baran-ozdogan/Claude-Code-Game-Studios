# Story 001: IPlayerState + PlayerStateProvider (referans-sayımlı hareket kilidi)

> **Epic**: Birinci Şahıs Kontrolcü
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/birinci-sahis-kontrolcu.md` ("Interactions with Other Systems" — `IPlayerState`; Edge Cases — kilit)
**Requirement**: `TR-fpc-001`, `TR-fpc-002`, `TR-fpc-003`, `TR-fpc-005`, `TR-fpc-006`, `TR-fpc-007`, `TR-fpc-008`, `TR-fpc-009`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0003 (primary — Key Interfaces bloğu bu story'nin implementasyon kaynağı)
**ADR Decision Summary**: `PlayerStateProvider : MonoBehaviour, IPlayerState` — `FirstPersonController`'dan AYRI (test edilebilirlik: `AddComponent<PlayerStateProvider>()` çıplak bir GameObject'te, `CharacterController`/`Camera` gerekmez). `Current` static accessor, `[RuntimeInitializeOnLoadMethod]` reset YOK (Player sahnesi süreç başına bir kez yüklenir, "önceki oturumdan bayat veri" sorunu yok — ADR-0001'in tam desenini BİLEREK kullanmaz). Kilit: iki ayrı `HashSet<object>` (_fullLockHolders/_moveOnlyLockHolders), "en kısıtlayıcı kazanır" `_fullLockHolders.Count > 0` kontrolüyle O(1).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Saf C# + `MonoBehaviour`/`HashSet` — post-cutoff API yok. Duplicate-guard KOŞULSUZ `Debug.LogError` (asla `Debug.Assert` — `[Conditional("UNITY_ASSERTIONS")]` shipping build'de tamamen derlenmez, sıfır koruma verirdi; ADR-0003 unity-specialist BLOCKING düzeltmesi).

**Control Manifest Rules (bu katman)**:
- Required: `Dereference X.Instance kullanım noktasında canlı` (buradaki karşılığı: `PlayerStateProvider.Current`); public write API'ler idempotent varsayılan
- Forbidden: `Debug.Assert`-tabanlı duplicate-guard (yalnız `Debug.LogError` + `Destroy` + early-return)
- Guardrail: kilit sorguları (`MovementLocked`/`IsLocked`) allocation'sız, her karede çoklu tüketici tarafından okunabilir O(1)

## Acceptance Criteria

- [x] `IPlayerState` arayüzü GDD'nin alan listesiyle BİREBİR: `Transform EyeCamera` (salt okunur), `Vector3 Velocity`, `bool IsGrounded`, `bool MovementLocked` (salt okunur), `bool IsCarrying`, `bool IsLocked` (salt okunur), `event Action MovementLockChanged` (TR-fpc-001)
- [x] `PlayerStateProvider.Awake()`: duplicate-instance guard KOŞULSUZ — `Current != null` ise `Debug.LogError` + `Destroy(gameObject)` + early return; `Current`, yalnız İLK instance'ta set edilir
- [x] `RequestMovementLock(object requester, MovementLockScope scope=Full)`/`ReleaseMovementLock(object requester)` — iki ayrı `HashSet<object>` (`_fullLockHolders`/`_moveOnlyLockHolders`), `Full` varsayılan parametre (TR-fpc-002)
- [x] **N eşzamanlı AYNI-kapsam kilit sahibi (referans sayımı)**: A ve B ikisi de bağımsız `Request(_, Full)` çağırır; A release ederse `MovementLocked` `true` kalır, B da release edene kadar (TR-fpc-003'ün sayım yarısı — sıra-bağımsız, B-önce-A de aynı sonucu verir)
- [x] **Kapsamlar-arası en-kısıtlayıcı-kazanır**: A `MoveOnly`, B `Full` tutuyorsa `EffectiveScope()` → `Full`; yalnız `MoveOnly` sahipleri varsa → `MoveOnly` (TR-fpc-003'ün tip yarısı)
- [x] Aynı-kapsam double-Request aynı requester'dan, Release'siz: tek girdi, `MovementLockChanged` yalnız ilk çağrıda fırlar, TEK `ReleaseMovementLock` tamamen açar (TR-fpc-006, GDD Edge Case)
- [x] **Kapsamlar-arası sticky kilit (KASITLI, "düzeltilecek" bir hata DEĞİL)**: `Request(A,Full)` sonra `Request(A,MoveOnly)` (Release'siz) → A her iki HashSet'te de yer alır, `EffectiveScope()` `Full` kalır TEK `Release(A)` ikisini de temizleyene kadar (ADR-0003 TD-ADR review — "sticky most-restrictive," bilinçli: bir istekçi kendi tutuşunu yanlışlıkla gevşetemez); ters sıra (önce MoveOnly sonra Full) da aynı sonucu verir (TR-fpc-007)
- [x] `ReleaseMovementLock`, kilit sahibi OLMAYAN bir requester'dan çağrılırsa sessiz no-op — exception yok, durum değişmez, diğer sahipler etkilenmez (GDD Edge Case, çifte-release/yarış senaryosu)
- [x] `MovementLockChanged`, YALNIZ kilitli/kilitsiz durum GERÇEKTEN değişince fırlar (0→1 fırlar; 1→2 fırlamaz; 2→1 fırlamaz; 1→0 fırlar) (TR-fpc-008)
- [x] `IsLocked`, `MovementLocked`'ın ucuz (allocation'sız) bir alias'ı (TR-fpc-009)

## Implementation Notes

- ADR-0003'ün Key Interfaces kod bloğu implementasyon kaynağıdır — kopyala, türetme.
- **İleri bayrak (Story 003'e)**: ADR-0003'ün kendi Risks bölümü, UI ve Player kalıcı sahnelerinin eşzamanlı yüklenmesinde `Awake()` sırası garantisi olmadığını belgeliyor (gizli risk, şu an hiçbir tüketici `Awake()` içinde ihtiyaç duymadığı için yalnız gizli). `PlayerStateProvider`/gelecekteki `FirstPersonController`, UI-sahne durumunu ASLA `Awake()`/`OnEnable()` içinde okumamalı — bu kısıt Story 003'e de taşınacak.
- Test edilebilirlik: `AddComponent<PlayerStateProvider>()` çıplak bir `GameObject`'te — `CharacterController`/`Camera` gerekmez (ADR-0003 Alternative 1'in düzeltilmiş gerekçesi).

## Out of Scope

- `FirstPersonController` (hareket/kamera/input sürücüsü — Story 003)
- Player sahnesinin kalıcı-sahne yerleşimi + SOFT transition (Story 004)
- Hareket matematiği formülleri (Story 002)

## QA Test Cases

*(QL-STORY-READY full modda koştu; 6 story'lik epic'in tamamı tek gate çağrısında değerlendirildi — bu story'nin N-holder AC'si gate bulgusuyla ayrı bir maddeye bölündü.)*

- **AC-1 (otomatik)**: `IPlayerState` alan listesi verbatim
  - Given: `AddComponent<PlayerStateProvider>()` çıplak GameObject'te
  - When: public yüzey incelenir
  - Then: tam olarak 7 üye — EyeCamera/Velocity/IsGrounded/MovementLocked/IsCarrying/IsLocked/MovementLockChanged

- **AC-2 (otomatik)**: Duplicate-instance guard
  - Given: bir `PlayerStateProvider` mevcut, `Current` ona işaret ediyor
  - When: ikinci bir tane başka yerde `AddComponent` edilir, `Awake()` koşar
  - Then: `Debug.LogError` fırlar (koşulsuz), ikinci GameObject yok edilir, `Current` hâlâ ilkini gösterir

- **AC-3 (otomatik)**: N eşzamanlı aynı-kapsam kilit sahibi
  - Given: A ve B bağımsız `Request(_, Full)` çağırdı
  - When: A release eder
  - Then: `MovementLocked` hâlâ true; B de release edince false
  - Edge cases: sıra-bağımsızlık (B-önce-A aynı sonuç)

- **AC-4 (otomatik)**: Kapsamlar-arası en-kısıtlayıcı-kazanır
  - Given: A `MoveOnly`, B `Full` tutuyor
  - When: `EffectiveScope()` okunur
  - Then: `Full` döner
  - Edge cases: yalnız `MoveOnly` sahipleri → `MoveOnly`

- **AC-5 (otomatik)**: Aynı-kapsam double-Request no-op
  - Given: A `Request(A, Full)` çağırdı
  - When: A aynı kapsamla tekrar çağırır (Release'siz)
  - Then: tam bir girdi, `MovementLockChanged` bir kez fırladı, tek `Release(A)` tam açar
  - Edge cases: `MoveOnly` için de tekrarla

- **AC-6 (otomatik)**: Kapsamlar-arası sticky kilit
  - Given: A `Request(A, Full)` çağırdı
  - When: A sonra `Request(A, MoveOnly)` çağırır (Release'siz)
  - Then: A iki HashSet'te birden, `EffectiveScope()` `Full` kalır, TEK `Release(A)` ikisini de temizler
  - Edge cases: ters sıra (önce MoveOnly sonra Full) — aynı sonuç; bu davranışın KASITLI olduğu assert edilir, "düzeltilmez"

- **AC-7 (otomatik)**: Kilit-sahibi-olmayan Release sessiz no-op
  - Given: A kilidi tutuyor, B hiç istemedi
  - When: B `Release(B)` çağırır
  - Then: exception yok, durum değişmez, A etkilenmez
  - Edge cases: tekrarlanan B release'leri de no-op

- **AC-8 (otomatik)**: MovementLockChanged yalnız gerçek geçişte
  - Given: kilitsiz durum
  - When: A request eder (0→1)
  - Then: event bir kez fırlar
  - When: B de request eder (1→2)
  - Then: event fırlamaz
  - When: A release eder (2→1)
  - Then: event fırlamaz
  - When: B release eder (1→0)
  - Then: event bir kez fırlar

- **AC-9 (otomatik)**: IsLocked ucuz alias
  - Given: herhangi bir kilit konfigürasyonu
  - When: `IsLocked` ve `MovementLocked` okunur
  - Then: her zaman eşit; okuma allocation üretmez

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/fpc_player_state_lock_test.cs` (AC-1, AC-3–AC-9) + `game/Assets/Tests/PlayMode/fpc_player_state_duplicate_guard_test.cs` (AC-2 — bkz. Completion Notes, Awake() Play Mode dışında hiç ateşlemiyor)
**Status**: [x] Created — EditMode 13/13, PlayMode 1/1

## Dependencies

- Depends on: None (ADR-0003'ün doğrudan implementasyonu, yukarı bağımlılık yok)
- Unlocks: Story 003 (FirstPersonController, `PlayerStateProvider`'ı okur/sürer)

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 9/9 AC, EditMode 98/98 (proje geneli), PlayMode 35/35 (proje geneli).

**Önemli mühendislik bulgusu (ADR-0003'ün bir varsayımını düzeltiyor)**: ADR-0003'ün test mandatı "Awake() edit-mode'da senkron koşar" varsayımıyla yazılmıştı. Ampirik olarak YANLIŞ çıktı — Unity, `[ExecuteAlways]` olmadıkça Play Mode DIŞINDA (Edit Mode dahil) `MonoBehaviour.Awake()`'i HİÇ çağırmaz. Doğrudan `internal` bir bayrakla doğrulandı (`Awake()` içine konan flag, `yield return null` sonrasında bile `false` kaldı). Sonuç: AC-2'nin (duplicate-instance guard) testi EditMode'dan PlayMode'a taşındı (`fpc_player_state_duplicate_guard_test.cs`) — `UIRootStaleInstanceTest` ile aynı, zaten bu projede çalışan desen. Diğer 8 AC, `_provider` üzerinde doğrudan instance metodu çağırdığı için Awake()'den bağımsız — EditMode'da kalabildi.

**Gate'ler (full mod)**: LP-CODE-REVIEW **APPROVE** (ADR'ye birebir, manifest uyumlu, sıfır düzeltme gerektiren bulgu). QL-TEST-COVERAGE **GAPS→giderildi**: (1) `RequestMovementLock_ScopeOmitted_DefaultsToFull` eklendi — `scope` parametresi hiç test edilmemiş varsayılan değeriyle çağrılmıyordu; (2) `RequestMovementLock_SameScopeTwice_MoveOnly_IsNoOp_SingleReleaseFullyUnlocks` eklendi — AC-5'in kendi QA Test Case'i "MoveOnly için de tekrarla" diyordu ama yalnız Full test edilmişti; (3) PlayMode testine duplicate-guard olayı SONRASI hayatta kalan instance'ın kilit API'sinin çalıştığını doğrulayan 3 satır eklendi (önceden örtük varsayımdı).

**İleri bayraklar (advisory, kapanış engellemiyor)**: N=3 holder senaryosu test edilmedi (jenerik `HashSet` desenine düşük risk); allocation-free iddiası mekanik olarak assert edilmedi (inceleme ile doğru); double-release-aynı-eski-sahipten ayrı test edilmedi (kod yolu AC-7'nin non-holder testiyle aynı).

**Test Evidence**: `game/Assets/Tests/EditMode/fpc_player_state_lock_test.cs` (13 test) + `game/Assets/Tests/PlayMode/fpc_player_state_duplicate_guard_test.cs` (1 test).
