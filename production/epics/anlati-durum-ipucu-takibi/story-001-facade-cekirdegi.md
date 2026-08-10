# Story 001: Facade çekirdeği — IAnlatiDurumState + AnlatiDurumState + statik facade

> **Epic**: Anlatı Durum/İpucu Takibi
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: S-M (~2-3h)
> **Manifest Version**: 2026-08-09
> **Last Updated**: 2026-08-10

## Context

**GDD**: `design/gdd/anlati-durum-ipucu-takibi.md` (Core Rules — veri modeli/idempotency/kalıcılık; Interactions — genel sorgu/yazma sözleşmesi; Edge Cases — boş koleksiyon, tanımsız clueId, doğrudan Mark)
**Requirement**: `TR-anlati-001`, `TR-anlati-003`, `TR-anlati-004`, `TR-anlati-007`
*(Requirement metinleri `docs/architecture/tr-registry.yaml`'da)*

**ADR Governing Implementation**: ADR-0007 (primary — Data model bloğu bu story'nin kaynağı); ADR-0001 (secondary — üçlü desen), ADR-0015 (secondary — in-place reset rejimi)
**ADR Decision Summary**: ADR-0001'in üçlü deseni birebir: `IAnlatiDurumState` arayüzü + `AnlatiDurumState` plain C# sınıfı + `AnlatiDurumIpucuTakibi` statik facade. İki `HashSet<string>` (`_knownClueIds`/`_seenShiftIds`). `MarkClueKnown`, `HashSet.Add`'in dönüş değerine bakarak idempotent. Reset **in-place** (ADR-0015 rejimi — instance ASLA değiştirilmez, aksi hâlde constructor-time aboneliği olan instance'lar Işık/Volume event'ine yetim abone kalırdı).

**Engine**: Unity 6.5 (6000.5.6f1) | **Risk**: LOW
**Engine Notes**: Saf C# — bu story'de hiçbir engine API'si yok (Addressables Story 003'te, `ScriptableObject` Story 002'de). `GetKnownClueIds()` dönüş tipi **`IReadOnlyCollection<string>`** olmalı, `IReadOnlySet<string>` DEĞİL: .NET 5+ tipi, Unity'nin desteklenen Api Compatibility profillerinde garanti değil ve GDD'nin kendi imzasından sapar — manifest bunu adlandırılmış YASAK desen olarak listeliyor (ADR-0006'da bir kez yakalandı, ADR-0007 proaktif düzeltti).

**Control Manifest Rules (bu katman)**:
- Required: Session-scoped state = arayüz + plain C# sınıf + statik facade (testler `...State`'i doğrudan kurar, facade'a hiç dokunmaz); her facade reset'i `FoundationBootstrap.ResetAll()` üzerinden, kendi `[RuntimeInitializeOnLoadMethod]`'u ASLA; event-açan her facade için **in-place** reset (aynı instance, alanlar temizlenir)
- Forbidden: `IReadOnlySet<T>` (BCL risk + GDD sapması); wholesale state replacement; `ScriptableObject`-backed runtime state
- Guardrail: public write API'ler varsayılan olarak idempotent

## Acceptance Criteria

- [x] `IAnlatiDurumState` arayüzü ADR-0007 Data model bloğuyla BİREBİR — tam olarak 4 üye: `bool IsClueKnown(string)`, `IReadOnlyCollection<string> GetKnownClueIds()`, `void MarkClueKnown(string)`, `event Action<string> OnClueKnown`
- [x] `MarkClueKnown(clueId)` idempotent: `HashSet.Add` false dönerse sessiz no-op, `OnClueKnown` İKİNCİ kez fırlamaz (GDD AC3, TR-anlati-003)
- [x] `OnClueKnown`, bir `clueId` için `Unknown→Known` geçişinde TAM OLARAK bir kez fırlar (GDD AC3/AC12b — AC12b'nin "gecenin son ipucu" kurgusu bu sistemin kendi durumundan ayırt edilemez, ayrı test GEREKTİRMEZ)
- [x] `IsClueKnown`, eşleşen hiçbir `ClueDefinition`'ı olmayan bir `clueId` (yazım hatası) için istisna fırlatmadan `false` döner — meşru bilinmeyen bir ipucundan ayırt edilemez (GDD AC6)
- [x] `GetKnownClueIds()`, hiçbir ipucu bilinmeden BOŞ ve NULL-OLMAYAN bir koleksiyon döner; çağıranlar null kontrolü olmadan iterate/`.Count` yapabilir (GDD AC5, TR-anlati-004)
- [x] `MarkClueKnown`, `requiredShiftIds`'i tamamlanmamış bir `clueId` için doğrudan çağrılırsa ipucu YİNE DE Known olur — metod `_seenShiftIds`'e karşı doğrulama YAPMAZ (GDD AC9, KASITLI: diyalog-seçimi çağıranları shift gereksinimini atlayabilir)
- [x] **API yüzeyi hiçbir sıralama/zaman damgası verisi açmaz** (GDD AC7, TR-anlati-007): `IAnlatiDurumState`'in üye kümesi KAPALI olarak assert edilir — yukarıdaki 4 üye, imzalarıyla, ne eksik ne fazla. (Kapalı-yüzey assert'i tercih edilir; ad-tabanlı denylist [`Order`/`Sequence`/`Time`/`Index`/`When`] yalnız ikincil savunmadır. Testin KANITLAYAMADIĞI şey açıkça belgelenir: `HashSet<string>` enumerasyon sırası CLR'de fiilen ekleme-sıralı olabilir — bir tüketicinin bundan sıra çıkarması bu testin kapsamı DIŞINDADIR, GDD'nin kendi AC7 revizyonu bu yarımı Dependencies sözleşme notuna bıraktı)
- [x] `AnlatiDurumIpucuTakibi.ResetOnLoad()` **in-place**: aynı `AnlatiDurumState` instance'ının iki HashSet'ini temizler, instance'ı ASLA değiştirmez (ADR-0015 rejimi); `FoundationBootstrap._resetSequence`'ta **Işık/Volume'dan SONRA** yer alır ve `foundation_bootstrap_order_test.cs`'in `ExpectedActiveOrder`'ı bu satırla güncellenir (ADR-0006 sırası, ADR-0001 Validation Criteria)

## Implementation Notes

- ADR-0007'nin Data model kod bloğu implementasyon kaynağıdır — kopyala, türetme. **İSTİSNA**: constructor'daki `IsikVolumeDurumSistemi.Instance.OnShiftStateChanged += ...` satırı bu story'de YAZILMAZ (Story 003'ün kapsamı) — bu story parametresiz, aboneliksiz bir constructor teslim eder. Emsal: `gece-oturum-durumu` Story 001 de aboneliksiz constructor teslim etti, gerçek wiring Story 004'te eklendi (LP-CODE-REVIEW APPROVE) — additive, sıfır yeniden-yazım.
- `_byRequiredShiftId` ters indeksi ve `EnsureRegistryLoaded()` bu story'de YOK (Story 002/003).
- Testler `new AnlatiDurumState()` ile doğrudan kurar, statik facade'a hiç dokunmaz (manifest Required kuralı).

## Out of Scope

- `ClueDefinition`/`ClueRegistry` ScriptableObject'leri, ters indeks, Held handler mantığı (Story 002)
- Addressables yüklemesi + gerçek Işık/Volume aboneliği (Story 003)
- Edit-time doğrulama (Story 004/005)

## QA Test Cases

*(QL-STORY-READY full modda ÜÇ LENSLE koştu — testability/scope/GDD-fidelity. Bu story üç lensten de ADEQUATE aldı; iki iyileştirme işlendi: AC7'nin test şekli [kapalı-yüzey] somutlaştırıldı, bootstrap sırası ayrı bir AC maddesine çıkarıldı.)*

- **AC-1 (otomatik)**: Arayüz üye kümesi kapalı
  - Given: `typeof(IAnlatiDurumState)`
  - When: public üyeleri reflection'la listelenir
  - Then: tam olarak 4 üye — `IsClueKnown`/`GetKnownClueIds`/`MarkClueKnown`/`OnClueKnown`, imzalarıyla
  - Edge cases: `GetKnownClueIds`'in dönüş tipi `IReadOnlyCollection<string>` (asla `IReadOnlySet<string>`)

- **AC-2 (otomatik)**: MarkClueKnown idempotent
  - Given: taze `AnlatiDurumState`, `OnClueKnown` sayacı bağlı
  - When: aynı `clueId` ile iki kez `MarkClueKnown` çağrılır
  - Then: `IsClueKnown` true; sayaç tam olarak 1
  - Edge cases: farklı iki clueId → sayaç 2 (event clue başına)

- **AC-3 (otomatik)**: Bilinmeyen clueId sessizce false
  - Given: taze state
  - When: hiç tanımlanmamış bir `clueId` sorgulanır
  - Then: `false` döner, istisna YOK

- **AC-4 (otomatik)**: Boş sorgu yüzeyi null değil
  - Given: hiçbir ipucu bilinmiyor
  - When: `GetKnownClueIds()` çağrılır
  - Then: non-null, `Count == 0`; `foreach` güvenle koşar

- **AC-5 (otomatik)**: Doğrudan Mark, ön koşul doğrulamaz
  - Given: `_seenShiftIds` boş
  - When: `MarkClueKnown("clue-x")` doğrudan çağrılır
  - Then: `IsClueKnown("clue-x")` true — ön koşul kontrolü YOK (kasıtlı)

- **AC-6 (otomatik)**: In-place reset + bootstrap sırası
  - Given: facade'dan alınan instance referansı, birkaç ipucu işaretlenmiş
  - When: `ResetOnLoad()` çağrılır
  - Then: `GetKnownClueIds()` boşalır AMA `AnlatiDurumIpucuTakibi.Instance` AYNI nesneyi gösterir (referans eşitliği)
  - Edge cases: `FoundationBootstrap`'ın sıra dizisi Işık/Volume'dan sonra bu servisi içerir (`ExpectedActiveOrder` güncel)

## Test Evidence

**Story Type**: Logic → `game/Assets/Tests/EditMode/anlati_durum_facade_test.cs` (18 test)
**Status**: [x] Created — EditMode 158/158

## Dependencies

- Depends on: None (ADR-0001 deseninin doğrudan uygulaması)
- Unlocks: Story 002 (ters indeks + handler mantığı bu çekirdeği tüketir)

## Completion Notes

**Verdict**: COMPLETE WITH NOTES — 8/8 AC, EditMode 158/158 (ilk koşuda 153/153 temiz; gate düzeltmeleri sonrası 158).

**Dosyalar**: `Foundation/AnlatiDurumIpucuTakibi/` (YENİ klasör: `IAnlatiDurumState.cs`, `AnlatiDurumState.cs`, `AnlatiDurumIpucuTakibi.cs`), `FoundationBootstrap.cs` (TODO satırı gerçek kayda dönüştü — Işık/Volume ve Gece/Oturum'dan SONRA), `foundation_bootstrap_order_test.cs` (`ExpectedActiveOrder` +1 satır), `Tests/EditMode/anlati_durum_facade_test.cs` (YENİ, 18 test).

**Gate'ler (full mod)**: **LP-CODE-REVIEW CONCERNS→giderildi**, **QL-TEST-COVERAGE GAPS→giderildi**.

**EN ÖNEMLİ BULGU — abonelik yeri iki farklı yerde belgeleniyordu (LP)**: ADR-0007'nin Data model bloğu Işık/Volume aboneliğini `AnlatiDurumState`'in CONSTRUCTOR'ına koyuyor; bu kod tabanı için YANLIŞ. Testler ADR-0001 deseni gereği taze `AnlatiDurumState` kurar (bu story'nin 18 testinin hepsi `[SetUp]`'ta bir tane kuruyor) — State-ctor aboneliği her testte kalıcı `OnShiftStateChanged` event'ine bir handler daha sızdırır ve "saf state" testlerini gerçek facade'a bağımlı kılardı (manifest ihlali). Proje emsali `GeceOturumDurumu` de aboneliği FACADE static ctor'ında tutuyor. Kod zaten her ikisiyle de uyumlu (henüz constructor yok — sapma tamamen additive); düzeltme BELGEDE yapıldı: Story 003'ün AC'si + test şekli facade static ctor'a çevrildi, **ADR-0007 addendum'u gerekiyor** (Story 005'in mekanizma sapmasıyla aynı pakette açılabilir).

**Diğer LP düzeltmeleri**: `internal HashSet<string> SeenShiftIds` ham koleksiyon sızdırıyordu → `MarkShiftSeen(string)` + salt-okunur `SeenShiftIds` görünümü + `HasSeenShift(string)` (emsal `GeceOturumDurumuState` de üyeliği sorgu metotlarıyla verir, asla ham kümeyle); `MarkClueKnown(null/boş)` artık sessiz no-op (yazılmamış bir `ClueDefinition` alanı koleksiyona null eleman sokup `GetKnownClueIds()`'i gezen tüketicide NRE üretirdi); canlı-koleksiyon dönüşünün iki sonucu (canlı görünüm + gezerken `MarkClueKnown` → `InvalidOperationException`) XML doc'a yazıldı; `OnClueKnown` abonelerinin reset'te bilerek temizlenmediği belgelendi.

**QL test eklemeleri**: abonenin reset'i sağ geçmesi + reset sonrası yeniden işaretlemede event'in TEKRAR fırlaması (in-place rejimin tüm varoluş sebebi; gece-oturum emsalinde karşılığı vardı, burada eksikti — üstelik tek-yönlü geçiş × reset × tam-bir-kez etkileşimi burada daha güçlü); abone patlarsa yazmanın geri alınmaması (`AddFiredTrigger` emsali); canlı-görünüm davranışının sabitlenmesi; null/boş guard'ı; facade reset testine negatif kontrol + `finally` temizliği (önceden `InternalInstance` ile `Instance` farklı nesne olsa bile totolojik geçebilirdi).

**AC-7 reflection taramasının kapatılan delikleri (QL)**: `Static` üyeler (C# 9 arayüzlerde static üyeye izin veriyor), nested tipler ve **taban arayüzler** — sonuncusu en gerçekçi kaçaktı: `IAnlatiDurumState : IClueOrdering` yazılsa her tüketici aynı referanstan bir sıralama üyesi alırdı ve kapalı-yüzey testi hâlâ "tam dört üye" görüp geçerdi. `GetInterfaces()` boş assert'i eklendi.

**İleri bayrak**: `GetKnownClueIds()`'in döndürdüğü `HashSet`'in fiilî enumerasyon sırası ekleme-sıralı görünebilir — AC7'nin tip sistemiyle zorlanamayan yarısı. Kopya döndürmek bunu ÇÖZMEZ (dizi kopyası da sırayı korur); tek hafifletme dokümantasyon, yapıldı (`GetKnownClueIds` XML doc'unda "enumerasyon sırası anlam taşımaz").
