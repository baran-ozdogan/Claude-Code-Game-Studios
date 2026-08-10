using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// anlati-durum-ipucu-takibi Story 001: facade çekirdeği (ADR-0001 üçlü deseni,
/// ADR-0007 Data model). Testler taze bir `AnlatiDurumState` kurar ve statik
/// facade'a DOKUNMAZ (manifest Required kuralı) — tek istisna, facade'ın kendi
/// in-place reset ve bootstrap-kaydı davranışını sınayan iki test.
/// </summary>
public class AnlatiDurumFacadeTest
{
    private AnlatiDurumState _state;

    [SetUp]
    public void SetUp()
    {
        _state = new AnlatiDurumState();
    }

    // ── AC-1: Arayüz üye kümesi KAPALI (sıra/zaman verisi sızmaz) ──

    [Test]
    public void IAnlatiDurumState_SurfaceIsClosed_ExactlyFourMembers()
    {
        // GDD AC7 / TR-anlati-007: API'nin kendisi sıra bilgisi taşımadığını
        // doğrular. Kapalı-yüzey assert'i tercih edildi (heuristik ad taraması
        // yerine): ileride eklenecek bir `DateTime LastKnownAt` ya da
        // `GetClueOrder()` bu testi DOĞRUDAN kırar.
        var expected = new[]
        {
            "M:IsClueKnown(String)",
            "M:GetKnownClueIds()",
            "M:MarkClueKnown(String)",
            "E:OnClueKnown",
        };

        // STATIC de taranır: C# 9 (Unity 6.5) arayüzlerde static üyeye izin verir —
        // `static DateTime LastKnownAt` yalnız Instance taramasından kaçardı (gate bulgusu).
        string[] actual = typeof(IAnlatiDurumState)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !(m is MethodInfo method && method.IsSpecialName))
            .Select(Signature)
            .ToArray();

        CollectionAssert.AreEquivalent(expected, actual,
            "IAnlatiDurumState yüzeyi değişti — sıra/zaman verisi sızdıran bir üye eklenmiş olabilir (GDD AC7).");
    }

    [Test]
    public void IAnlatiDurumState_HasNoBaseInterface_AndNoNestedType()
    {
        // EN GERÇEKÇİ KAÇAK (gate bulgusu): `GetMembers` TABAN arayüzleri gezmez.
        // `IAnlatiDurumState : IClueOrdering` yazılsaydı her tüketici aynı referans
        // üzerinden bir sıralama üyesi alırdı ve kapalı-yüzey testi hâlâ "tam dört
        // üye" görüp GEÇERDİ. Nested tip de aynı şekilde görünmez.
        CollectionAssert.IsEmpty(typeof(IAnlatiDurumState).GetInterfaces(),
            "IAnlatiDurumState hiçbir arayüzü miras almamalı — taban arayüz kapalı-yüzey testini atlatır (GDD AC7).");

        CollectionAssert.IsEmpty(typeof(IAnlatiDurumState).GetNestedTypes(BindingFlags.Public),
            "IAnlatiDurumState nested public tip taşımamalı.");
    }

    [Test]
    public void IAnlatiDurumState_HasNoOrderingOrTimestampNamedMember()
    {
        // İkincil savunma: yukarıdaki kapalı-yüzey testini biri gevşetirse bu
        // yakalar. NOT: bu test `GetKnownClueIds()`'in DÖNDÜĞÜ HashSet'in fiilî
        // enumerasyon sırasından bir tüketicinin sıra çıkarmasını KANITLAYAMAZ —
        // o risk kapsam dışıdır (GDD'nin kendi AC7 revizyonu o yarımı Dependencies
        // sözleşme notuna bıraktı), code review'ın işidir.
        string[] forbidden = { "order", "sequence", "time", "index", "when", "timestamp" };

        foreach (MemberInfo member in typeof(IAnlatiDurumState).GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            foreach (string token in forbidden)
            {
                Assert.IsFalse(member.Name.ToLowerInvariant().Contains(token),
                    $"'{member.Name}' sıra/zaman çağrıştıran bir üye — GDD AC7 bunu yasaklıyor.");
            }
        }
    }

    [Test]
    public void GetKnownClueIds_ReturnType_IsReadOnlyCollection_NotReadOnlySet()
    {
        // Manifest'in adlandırılmış YASAK deseni: IReadOnlySet<T> .NET 5+ tipi,
        // Unity'nin Api Compatibility profillerinde garanti değil (ADR-0006'da
        // bir kez yakalandı, ADR-0007 proaktif düzeltti).
        MethodInfo method = typeof(IAnlatiDurumState).GetMethod(nameof(IAnlatiDurumState.GetKnownClueIds));

        Assert.AreEqual(typeof(IReadOnlyCollection<string>), method.ReturnType);
    }

    // ── AC-2: MarkClueKnown idempotent, OnClueKnown tam bir kez ──

    [Test]
    public void MarkClueKnown_CalledTwice_FiresEventExactlyOnce()
    {
        // Arrange
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        // Act
        _state.MarkClueKnown("clue-a");
        _state.MarkClueKnown("clue-a");

        // Assert
        Assert.IsTrue(_state.IsClueKnown("clue-a"));
        Assert.AreEqual(1, eventCount, "OnClueKnown yalnız Unknown→Known geçişinde fırlamalı (GDD AC3).");
    }

    [Test]
    public void MarkClueKnown_DifferentClues_FireSeparateEvents()
    {
        // Event clue BAŞINA fırlar — tek bir "artık bir şey biliniyor" sinyali değil.
        var received = new List<string>();
        _state.OnClueKnown += received.Add;

        _state.MarkClueKnown("clue-a");
        _state.MarkClueKnown("clue-b");

        CollectionAssert.AreEqual(new[] { "clue-a", "clue-b" }, received);
    }

    [Test]
    public void OnClueKnown_CarriesTheClueId()
    {
        string captured = null;
        _state.OnClueKnown += id => captured = id;

        _state.MarkClueKnown("clue-x");

        Assert.AreEqual("clue-x", captured);
    }

    // ── AC-3: Bilinmeyen clueId sessizce false ──

    [Test]
    public void IsClueKnown_UnknownOrTypoId_ReturnsFalse_WithoutThrowing()
    {
        // GDD AC6: tanımsız bir ipucu ile meşru olarak bilinmeyen bir ipucu
        // AYIRT EDİLEMEZ — ayrı bir "tanımsız" sinyali YOK (kasıtlı).
        Assert.DoesNotThrow(() => _state.IsClueKnown("boyle-bir-clue-yok"));
        Assert.IsFalse(_state.IsClueKnown("boyle-bir-clue-yok"));
    }

    // ── AC-4: Boş sorgu yüzeyi null değil ──

    [Test]
    public void GetKnownClueIds_BeforeAnyClueKnown_ReturnsEmptyNonNullCollection()
    {
        // GDD AC5: çağıranlar (Plot Twist/Final) null kontrolü OLMADAN güvenle
        // iterate/.Count yapabilmeli.
        IReadOnlyCollection<string> known = _state.GetKnownClueIds();

        Assert.IsNotNull(known);
        Assert.AreEqual(0, known.Count);
        Assert.DoesNotThrow(() => { foreach (string _ in known) { } });
    }

    [Test]
    public void GetKnownClueIds_ReflectsMarkedClues()
    {
        _state.MarkClueKnown("clue-a");
        _state.MarkClueKnown("clue-b");

        CollectionAssert.AreEquivalent(new[] { "clue-a", "clue-b" }, _state.GetKnownClueIds());
    }

    // ── AC-5: Doğrudan Mark ön koşul doğrulamaz ──

    [Test]
    public void MarkClueKnown_WithNoSeenShiftIds_StillMarksKnown()
    {
        // GDD AC9 — KASITLI: metod ClueDefinition'dan habersizdir ve
        // _seenShiftIds'e karşı doğrulama yapmaz (diyalog-seçimi çağıranları
        // shift gereksinimini tamamen atlayabilir).
        Assert.AreEqual(0, _state.SeenShiftIds.Count, "Ön koşul: hiçbir shift görülmemiş.");

        _state.MarkClueKnown("clue-hic-tetiklenmedi");

        Assert.IsTrue(_state.IsClueKnown("clue-hic-tetiklenmedi"));
    }

    // ── Guard'lar ve sözleşme sabitleme (gate bulguları) ──

    [Test]
    public void MarkClueKnown_NullOrBlankId_IsSilentNoOp_NoNullElementLeaks()
    {
        // Yazılmamış bir ClueDefinition alanı null gelirse koleksiyona null
        // eleman girer ve onu gezen tüketici (Plot Twist/Final) NRE alırdı.
        int eventCount = 0;
        _state.OnClueKnown += _ => eventCount++;

        _state.MarkClueKnown(null);
        _state.MarkClueKnown(string.Empty);
        _state.MarkClueKnown("   ");

        Assert.AreEqual(0, eventCount, "null/boş clueId event üretmemeli.");
        Assert.AreEqual(0, _state.GetKnownClueIds().Count, "Koleksiyona hiçbir şey girmemeli.");
        CollectionAssert.DoesNotContain(_state.GetKnownClueIds(), null);
    }

    [Test]
    public void MarkClueKnown_SubscriberThrows_WriteIsNotRolledBack()
    {
        // `AddFiredTrigger` emsaliyle aynı şekil ve aynı bilinçli sözleşme. Burada
        // sonucu daha ağır: geçiş TEK YÖNLÜ olduğundan patlayan abone o bildirimi
        // bir daha ALAMAZ — davranış bilerek sabitleniyor.
        _state.OnClueKnown += _ => throw new InvalidOperationException("abone patladı");

        Assert.Throws<InvalidOperationException>(() => _state.MarkClueKnown("clue-a"));
        Assert.IsTrue(_state.IsClueKnown("clue-a"), "Yazma event'ten önce tamamlanır, geri alınmaz.");
    }

    [Test]
    public void GetKnownClueIds_ReturnsLiveView_ReflectsLaterMarksAndReset()
    {
        // ADR-0007 birebir canlı görünüm döndürüyor — davranış BİLİNÇLİ, testle
        // sabitleniyor ki ileride biri farkında olmadan kopyaya çevirmesin
        // (ya da tersine, kopya bekleyen bir tüketici yazmasın).
        IReadOnlyCollection<string> view = _state.GetKnownClueIds();
        Assert.AreEqual(0, view.Count);

        _state.MarkClueKnown("clue-a");
        Assert.AreEqual(1, view.Count, "Görünüm canlı — sonradan işaretlenen ipucu görünmeli.");

        _state.ResetOnLoad();
        Assert.AreEqual(0, view.Count, "Reset canlı görünümü de boşaltır.");
    }

    // ── AC-6: In-place reset + bootstrap sırası ──

    [Test]
    public void ResetOnLoad_ClearsBothSets_InPlace()
    {
        _state.MarkClueKnown("clue-a");
        _state.MarkShiftSeen("shift-a");

        _state.ResetOnLoad();

        Assert.AreEqual(0, _state.GetKnownClueIds().Count);
        Assert.AreEqual(0, _state.SeenShiftIds.Count);
        Assert.IsFalse(_state.HasSeenShift("shift-a"));
    }

    [Test]
    public void ResetOnLoad_SubscriberSurvives_AndReceivesPostResetEvents()
    {
        // In-place rejimin TÜM VAROLUŞ SEBEBİ bu: süreç-ömürlü aboneler oturum
        // sınırını sağ geçer (gece-oturum emsalinde de bu testin karşılığı var).
        // Burada ek bir katman daha kanıtlanıyor: reset ipucunu Unknown'a
        // döndürdüğü için AYNI ipucu yeniden işaretlenince event TEKRAR fırlar —
        // tek-yönlü geçiş × reset × tam-bir-kez etkileşimi.
        var received = new List<string>();
        _state.OnClueKnown += received.Add;

        _state.MarkClueKnown("clue-a");
        _state.ResetOnLoad();
        _state.MarkClueKnown("clue-a");

        CollectionAssert.AreEqual(new[] { "clue-a", "clue-a" }, received,
            "Abone reset'i sağ geçmeli ve reset sonrası yeniden işaretlemede event yeniden fırlamalı.");
    }

    [Test]
    public void Facade_ResetOnLoad_KeepsTheSameInstance()
    {
        // ADR-0015 rejimi: instance ASLA değiştirilmez — aksi hâlde Story 003'te
        // facade static ctor'ında bağlanacak abonelik her reset'te yetim kalırdı.
        IAnlatiDurumState before = AnlatiDurumIpucuTakibi.Instance;
        try
        {
            AnlatiDurumIpucuTakibi.InternalInstance.MarkClueKnown("clue-facade");

            // NEGATİF KONTROL: yazma gerçekten `Instance`'ta görünüyor mu? Bu
            // olmadan, InternalInstance ile Instance farklı nesneler olsa bile
            // reset sonrası "Count == 0" totolojik olarak geçerdi (gate bulgusu).
            Assert.IsTrue(AnlatiDurumIpucuTakibi.Instance.IsClueKnown("clue-facade"),
                "Ön koşul: InternalInstance ve Instance AYNI nesne olmalı.");

            AnlatiDurumIpucuTakibi.ResetOnLoad();

            Assert.AreSame(before, AnlatiDurumIpucuTakibi.Instance, "Reset instance'ı DEĞİŞTİRMEMELİ (in-place).");
            Assert.AreEqual(0, AnlatiDurumIpucuTakibi.Instance.GetKnownClueIds().Count);
        }
        finally
        {
            // Statik durumu her hâlükârda temiz bırak — assert patlasa bile
            // "clue-facade" süitin geri kalanına sızmasın.
            AnlatiDurumIpucuTakibi.ResetOnLoad();
        }
    }

    [Test]
    public void FoundationBootstrap_IncludesThisService_AfterIsikVolume()
    {
        // Sıra kuralı: bu servis Işık/Volume'a constructor-abone olacak (Story 003),
        // bu yüzden onun reset'inden SONRA gelmeli (ADR-0001/0006).
        IReadOnlyList<string> order = FoundationBootstrap.ActiveResetOrder;

        int isikIndex = order.ToList().IndexOf("IsikVolumeDurumSistemi");
        int anlatiIndex = order.ToList().IndexOf("AnlatiDurumIpucuTakibi");

        Assert.Greater(anlatiIndex, -1, "AnlatiDurumIpucuTakibi reset sırasına eklenmiş olmalı.");
        Assert.Greater(anlatiIndex, isikIndex, "Işık/Volume'dan SONRA sıfırlanmalı.");
    }

    /// <summary>Üye imzası — ad + tür + (metotsa) parametre tipleri.</summary>
    private static string Signature(MemberInfo member)
    {
        if (member is MethodInfo method)
        {
            string parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name));
            return $"M:{method.Name}({parameters})";
        }

        return member switch
        {
            PropertyInfo property => $"P:{property.Name}:{property.PropertyType.Name}",
            EventInfo evt => $"E:{evt.Name}",
            _ => $"?:{member.Name}",
        };
    }
}
