# Story 006: Çakışma kuralları — asimetrik kuyruk ve ret event'i

> **Epic**: Seviye/Sahne Geçişi
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M (~3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: —

## Context

**GDD**: `design/gdd/seviye-sahne-gecisi.md` (Edge Cases — aynı-tür reddi, cross-type bekleyen slot, "Kapsam genişletmesi"; AC-3, AC-4, AC-5, AC-6, AC-7)
**Requirement**: `TR-sahne-gecisi-008`
*(Requirement metni `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0008 (primary — `RequestSoftTransition`/`RequestHardCut` hakemliği, `_pendingHardCut`, `TryFirePendingHardCut`)
**ADR Decision Summary**: Asimetri KASITLIDIR. HARD CUT anlatısal olarak kritiktir ve kaybolmamalıdır → aktif bir SOFT sırasında istenirse TEK slotluk kuyruğa alınır ve `Idle`'a ulaşıldığı an otomatik ateşlenir. SOFT ise oyuncu tarafından başlatılır (asansör düğmesi) → ASLA kuyruklanmaz, reddedilir, ve oyuncu isterse tekrar basar. `OnSoftTransitionRejected(reason)`, bir `RequestSoftTransition`'ın reddedildiği HER durumda fırlar.

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Bu story'nin neredeyse tamamı saf C# hakemliğidir — Story 001'in `SceneTransitionState`'ine `TryBeginSoft`/`TryBeginHard`/`TryFirePendingHardCut` olarak eklenir ve düz EditMode `[Test]`'lerle kapanır. Yalnız "kuyruklanmış HARD CUT gerçekten sıfır ek gecikmeyle ateşleniyor" iddiası uçtan uca bir `[UnityTest]` gerektirir.

**Control Manifest Rules (bu katman)**:
- Required: durum makinesi mantığı BLOCKING birim testli
- Forbidden: SOFT için herhangi bir kuyruk mekanizması (asimetri bilinçlidir, "tutarlılık" adına simetrik yapılmamalı)
- Guardrail: —

---

## Acceptance Criteria

- [ ] **GDD AC-3**: aktif bir SOFT sırasında (`Preloading`/`Ready`/`Swapping`) ikinci bir `RequestSoftTransition` → no-op reddedilir, uyarı loglanır, devam eden geçiş etkilenmeden `Idle`'a tamamlanır, VE `OnSoftTransitionRejected("AlreadyTransitioningSoft")` TAM BİR KEZ fırlar
- [ ] **GDD AC-7**: aktif bir HARD CUT sırasında `RequestSoftTransition` → reddedilir, KUYRUĞA ALINMAZ, VE `OnSoftTransitionRejected("HardCutActive")` TAM BİR KEZ fırlar. `reason` iki vakayı ayırt eder ama event'in fırlaması bu ayrıma ASLA bağlı değildir (GDD "Kapsam genişletmesi")
- [ ] **GDD AC-4**: aktif bir HARD CUT sırasında ikinci bir `RequestHardCut` → no-op reddedilir, devam eden HARD CUT etkilenmeden tamamlanır
- [ ] **GDD AC-5**: aktif bir SOFT sırasında `RequestHardCut` → REDDEDİLMEZ, tek bir bekleyen slota kabul edilir, ve `CurrentState` `Idle`'a ulaştığı an çağıranın yeniden çağrısına gerek kalmadan OTOMATİK ateşlenir. Kuyruklanan HARD CUT zaten `Ready` ise sıfır ek gecikmeyle ateşlenir (Story 005'in fast-path'i)
- [ ] **GDD AC-6**: bekleyen slot doluyken ikinci bir `RequestHardCut` → no-op reddedilir; tek slot vardır, çok öğeli kuyruk DEĞİL
- [ ] **Reddedilen çağrının KENDİ callback'leri asla çağrılmaz**: reddedilen bir `RequestSoftTransition`'ın `onComplete`'i de `onFailed`'i de HİÇ çağrılmaz — yalnız `OnSoftTransitionRejected` fırlar. Devam eden geçişin kendi callback'leri bundan etkilenmez ve kendi tamamlanmasında normal fırlar *(QL-STORY-READY bulgusu: GDD reddedilme durumunda `onFailed`'in fırladığını hiçbir yerde söylemiyor; belirsizlik açıkça kapatıldı)*

---

## Implementation Notes

*ADR-0008'in hakemlik taslağı doğrultusunda:*

- Hakemlik `SceneTransitionState`'e **talimat dönen** metotlar olarak eklenir (`ElevatorStateMachine.TryCall()` şekli): `TryBeginSoft` → `Rejected(reason)` | `Start`; `TryBeginHard` → `Rejected` | `Queued` | `SwapDirectly` | `Start`. Sürücü talimatı uygular (coroutine başlatır / `DoSwap` çağırır), kararı vermez.
- `TryFirePendingHardCut()` `Idle`'a her ulaşıldığında çağrılır — hem SOFT hem HARD yollarının sonunda, hem de fast-path'in sonunda.
- Ret gerekçesi `_activeType`'tan türetilir: `Soft` → `"AlreadyTransitioningSoft"`, `Hard` → `"HardCutActive"`. Gerekçe string'leri sabittir; Asansör bunları diegetik tepkisizlik göstergesine eşleyecek.
- **AC-6'nın senaryosu mevcut çağıran mimarisinde ASLA gerçekleşmez** — Sahne Kesmeli Anlatı kendi `HasTriggeredThisNight` guard'ıyla gece başına tam bir kez `RequestHardCut` çağırıyor. Yine de savunmacı davranış tanımlanır (gelecekte farklı bir çağıran eklenirse). Bu, GDD'nin kendi açıkladığı gerekçe — testin "gerçekçi değil" diye atlanmaması için buraya yazıldı.

---

## Out of Scope

- Geçişlerin kendisi (Story 003 SOFT, Story 005 HARD CUT) — bu story yalnız hangi isteğin KABUL edildiğine karar verir
- `SafeInvoke` ve callback karşılıklı dışlayıcılığının kendisi (Story 007) — burada yalnız "reddedilen çağrının callback'i hiç çağrılmaz" iddiası
- Asansör'ün diegetik tepkisizlik göstergesi (asansör epic'i)

---

## QA Test Cases

- **AC-1 (otomatik, EditMode)**: SOFT-üstüne-SOFT reddi
  - Given: `_activeType == Soft` ile `Preloading`/`Ready`/`Swapping`'deki çekirdek (üç durum için ayrı vaka)
  - When: `TryBeginSoft` çağrılır
  - Then: `Rejected` döner, `OnSoftTransitionRejected("AlreadyTransitioningSoft")` tam bir kez fırlar, `CurrentState` DEĞİŞMEZ
  - Edge cases: üç durumun ÜÇÜ de test edilir — yalnız `Preloading`'i test etmek `Swapping` sızıntısını kaçırır

- **AC-2 (otomatik, EditMode)**: HARD-üstüne-SOFT reddi, farklı gerekçe
  - Given: `_activeType == Hard` ile aktif durum
  - When: `TryBeginSoft` çağrılır
  - Then: `Rejected`, `OnSoftTransitionRejected("HardCutActive")` tam bir kez
  - Edge cases: gerekçe string'i AC-1'inkinden FARKLI olmalı — aynı string dönen bir implementasyon Asansör'ün ayırt etme yeteneğini sessizce yok eder

- **AC-3 (otomatik, EditMode)**: HARD-üstüne-HARD reddi
  - Given: aktif bir HARD CUT
  - When: ikinci `TryBeginHard`
  - Then: `Rejected`; bekleyen slot BOŞ kalır (kuyruğa alınmaz — kuyruk yalnız SOFT sırasında)

- **AC-4 (otomatik, EditMode)**: SOFT sırasında HARD kuyruklanır
  - Given: aktif SOFT
  - When: `TryBeginHard`
  - Then: `Queued`; slot dolu
  - Edge cases: slot doluyken ÜÇÜNCÜ bir `TryBeginHard` → `Rejected` (AC-6), slot içeriği DEĞİŞMEZ

- **AC-5 (otomatik, EditMode + PlayMode)**: Otomatik ateşleme
  - Given: dolu bekleyen slot
  - When: `CurrentState` `Idle`'a ulaşır
  - Then (EditMode): `TryFirePendingHardCut` slotu tüketir ve bir başlatma talimatı döner; slot boşalır
  - Then (PlayMode, uçtan uca): SOFT gerçekten tamamlanınca kuyruklanmış HARD CUT çağıranın hiçbir ek çağrısı olmadan koşar; preload zaten `Ready` ise ek kare gecikmesi yoktur
  - Edge cases: kuyruklanmış HARD CUT'ın ateşlenmesi de kendi `TryFirePendingHardCut`'ını çağırmalı (zincirleme değil — slot tek, ama `Idle`'a her ulaşım kontrol etmeli)

- **AC-6 (otomatik, EditMode)**: Reddedilen çağrının callback'leri
  - Given: aktif bir geçiş; reddedilecek bir `RequestSoftTransition` için gözcü `onComplete`/`onFailed`
  - When: istek reddedilir
  - Then: gözcülerin İKİSİ de HİÇ çağrılmaz; yalnız `OnSoftTransitionRejected` fırlar
  - Edge cases: devam eden geçişin KENDİ `onComplete`'i kendi tamamlanmasında normal fırlar — ayrı assertion, aksi hâlde "hiçbir callback çağrılmadı" testi yanlış nedenle geçebilir

---

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/sahne_gecisi_cakisma_test.cs` + `game/Assets/Tests/PlayMode/sahne_gecisi_kuyruk_test.cs`
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 005 (kuyruklanmış HARD CUT'ın "sıfır ek gecikme" iddiası fast-path'e dayanır)
- Unlocks: Story 007 (epic'in son story'si)
