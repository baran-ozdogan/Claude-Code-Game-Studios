using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// seviye-sahne-gecisi Story 001: saf durum makinesi çekirdeği
/// (TR-sahne-gecisi-001, -009, -011).
///
/// **Bu dosyanın TAMAMI düz `[Test]` — `AddComponent` YOK, `[UnityTest]` YOK.**
/// Ayrımın (saf çekirdek + ince sürücü) bütün gerekçesi bu: ADR-0008'in ilk
/// taslağı durumu `MonoBehaviour`'a koyduğu için bu davranışların hepsi PlayMode
/// testi gerektirecekti. Testin buradan PlayMode'a taşınması gerekmesi, ayrımın
/// kaybolduğunun ilk işareti olur.
/// </summary>
public class SahneGecisiDurumMakinesiTest
{
    private readonly List<(TransitionState state, TransitionType type)> _observed =
        new List<(TransitionState, TransitionType)>();

    private SceneTransitionState _state;

    [SetUp]
    public void SetUp()
    {
        _observed.Clear();
        _state = new SceneTransitionState();
        _state.OnTransitionStateChanged += (state, type) => _observed.Add((state, type));
    }

    private List<TransitionState> ObservedStates() => _observed.Select(o => o.state).ToList();

    // ── AC-1: Enum/arayüz sözleşmesi ──

    [Test]
    public void TransitionState_HasExactlyTheSixDocumentedValues()
    {
        CollectionAssert.AreEquivalent(
            new[]
            {
                TransitionState.Idle, TransitionState.Preloading, TransitionState.Ready,
                TransitionState.Swapping, TransitionState.Complete, TransitionState.Failed,
            },
            Enum.GetValues(typeof(TransitionState)).Cast<TransitionState>().ToList(),
            "GDD States and Transitions TAM OLARAK altı durum tanımlıyor — fazlası da eksiği de sözleşme ihlali.");
    }

    [Test]
    public void TransitionType_HasExactlySoftAndHard()
    {
        CollectionAssert.AreEquivalent(
            new[] { TransitionType.Soft, TransitionType.Hard },
            Enum.GetValues(typeof(TransitionType)).Cast<TransitionType>().ToList());
    }

    [Test]
    public void ISceneTransitionManager_MatchesTheGddInterfaceVerbatim()
    {
        // ADR-0008 Requirements bu arayüzün GDD'nin "Dışa açılan arayüz"
        // bölümüyle BİREBİR eşleşmesini şart koşuyor. Eksik bir üye, tüketici
        // epic'lerin (Asansör, Sahne Kesmeli Anlatı, Adaptif Ses) derlenmemesi
        // demek — ama o epic'ler henüz yazılmadığı için burada yakalanmazsa
        // aylar sonra yakalanır.
        Type contract = typeof(ISceneTransitionManager);

        Assert.IsNotNull(contract.GetProperty(nameof(ISceneTransitionManager.CurrentState)));
        Assert.AreEqual(typeof(TransitionState),
            contract.GetProperty(nameof(ISceneTransitionManager.CurrentState)).PropertyType);

        Assert.IsFalse(contract.GetProperty(nameof(ISceneTransitionManager.CurrentState)).CanWrite,
            "GDD: `TransitionState CurrentState` (SALT OKUNUR) — setter durumu dışarıdan yazılabilir kılar.");

        AssertMethod(contract, "RequestSoftTransition", typeof(void),
            new[] { "fromScene", "toScene", "config", "onComplete", "onFailed" },
            typeof(string), typeof(string), typeof(SoftTransitionConfig), typeof(Action), typeof(Action<string>));
        AssertMethod(contract, "RequestHardCut", typeof(void),
            new[] { "toScene", "config", "onComplete", "onFailed" },
            typeof(string), typeof(HardCutConfig), typeof(Action), typeof(Action<string>));
        AssertMethod(contract, "PreloadHardCut", typeof(void), new[] { "toScene" }, typeof(string));
        AssertMethod(contract, "GetCurrentHardCutAbrupt", typeof(bool), new string[0]);

        Assert.IsNotNull(contract.GetEvent(nameof(ISceneTransitionManager.OnTransitionStateChanged)));
        Assert.AreEqual(typeof(Action<TransitionState, TransitionType>),
            contract.GetEvent(nameof(ISceneTransitionManager.OnTransitionStateChanged)).EventHandlerType,
            "Event TransitionType TAŞIMALI — o parametre olmadan Adaptif Ses HARD CUT sting'ini " +
            "sıradan asansör geçişlerinde de çalardı (GDD N6 düzeltmesi).");

        Assert.IsNotNull(contract.GetEvent(nameof(ISceneTransitionManager.OnSoftTransitionRejected)));
        Assert.AreEqual(typeof(Action<string>),
            contract.GetEvent(nameof(ISceneTransitionManager.OnSoftTransitionRejected)).EventHandlerType,
            "reason string'i taşınmalı — Asansör ret türünü ayırt edebilmeli.");
    }

    /// <summary>
    /// Dönüş tipini VE parametre ADLARINI da pinler. `GetMethod(name, Type[])`
    /// tek başına ikisini de kaçırır — ve `RequestSoftTransition`'ın ilk iki
    /// parametresi (`fromScene`, `toScene`) İKİSİ DE `string` olduğu için yer
    /// değiştirmeleri tip kontrolüne GÖRÜNMEZDİ ama yükleme yönünü tersine
    /// çevirirdi (QL gate bulgusu).
    /// </summary>
    private static void AssertMethod(Type contract, string name, Type returnType,
        string[] parameterNames, params Type[] parameterTypes)
    {
        MethodInfo method = contract.GetMethod(name, parameterTypes);
        Assert.IsNotNull(method,
            $"ISceneTransitionManager.{name}({string.Join(", ", parameterTypes.Select(t => t.Name))}) yok.");
        Assert.AreEqual(returnType, method.ReturnType, $"{name} dönüş tipi GDD ile eşleşmiyor.");
        CollectionAssert.AreEqual(parameterNames, method.GetParameters().Select(p => p.Name).ToList(),
            $"{name} parametre ADLARI GDD imzasıyla birebir olmalı.");
    }

    [Test]
    public void ISceneTransitionManager_ExposesNothingBeyondTheGddList()
    {
        // "BİREBİR" İKİ YÖNLÜDÜR: fazla üye de eksik üye kadar sözleşme kaymasıdır.
        // Yeni bir üye önce GDD'nin "Dışa açılan arayüz" bölümünde yaşamalı.
        CollectionAssert.AreEquivalent(
            new[]
            {
                "get_CurrentState", "RequestSoftTransition", "RequestHardCut", "PreloadHardCut",
                "GetCurrentHardCutAbrupt",
                "add_OnTransitionStateChanged", "remove_OnTransitionStateChanged",
                "add_OnSoftTransitionRejected", "remove_OnSoftTransitionRejected",
            },
            typeof(ISceneTransitionManager).GetMethods().Select(m => m.Name).ToList());
    }

    [Test]
    public void HardCutConfig_CarriesTheAbruptFlagAsBool()
    {
        // GDD "Dışa açılan arayüz" `HardCutConfig.Abrupt` (bool)'u AÇIKÇA listeliyor
        // ve `GetCurrentHardCutAbrupt()`'ın döndüreceği TEK şey bu — Adaptif Ses'in
        // iki bitiş tonunu ayırt eden yegâne sinyal. Alan kaybolsa arayüz testi
        // yine yeşil kalırdı (QL gate bulgusu).
        PropertyInfo abrupt = typeof(HardCutConfig).GetProperty(nameof(HardCutConfig.Abrupt));

        Assert.IsNotNull(abrupt, "HardCutConfig.Abrupt yok.");
        Assert.AreEqual(typeof(bool), abrupt.PropertyType);
        Assert.IsTrue(new HardCutConfig(true).Abrupt, "Sistem bayrağı YORUMLAMAZ, taşır.");
        Assert.IsFalse(new HardCutConfig(false).Abrupt);
    }

    // ── AC-2: Saf kurulabilirlik ──

    private static readonly string[] EngineAssemblyPrefixes = { "UnityEngine", "UnityEditor", "Unity." };

    private static bool IsEngineType(Type t)
    {
        if (t == null) return false;
        if (t.HasElementType && IsEngineType(t.GetElementType())) return true;
        if (t.IsGenericType && t.GetGenericArguments().Any(IsEngineType)) return true;
        return EngineAssemblyPrefixes.Any(p => t.Assembly.GetName().Name.StartsWith(p, StringComparison.Ordinal));
    }

    [Test]
    public void SceneTransitionState_SurfaceBindsToNoEngineType()
    {
        // **Bu, AC-2'nin GERÇEK kanıtı.** Testin düz `[Test]` olarak DERLENMESİ
        // saflığı kanıtlamaz: `EditModeTests` de `Foundation` da UnityEngine'e
        // referanslıdır, yani çekirdeğe `using UnityEngine; private Vector3 _a;`
        // eklense diğer 14 testin HEPSİ yeşil kalırdı (QL gate bulgusu).
        // 2026-08-10 ayrım kararının tamamı bu garantiye dayanıyor.
        Type core = typeof(SceneTransitionState);
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
                                 | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        Assert.AreEqual(typeof(object), core.BaseType,
            "Çekirdek MonoBehaviour/ScriptableObject'ten TÜREYEMEZ — türeseydi düz new ile kurulamazdı.");

        var offenders = new List<string>();
        foreach (FieldInfo f in core.GetFields(All))
        {
            if (IsEngineType(f.FieldType)) offenders.Add($"field {f.Name} : {f.FieldType.FullName}");
        }
        foreach (PropertyInfo p in core.GetProperties(All))
        {
            if (IsEngineType(p.PropertyType)) offenders.Add($"property {p.Name} : {p.PropertyType.FullName}");
        }
        foreach (MethodInfo m in core.GetMethods(All))
        {
            if (IsEngineType(m.ReturnType)) offenders.Add($"method {m.Name} -> {m.ReturnType.FullName}");
            foreach (ParameterInfo par in m.GetParameters())
            {
                if (IsEngineType(par.ParameterType))
                {
                    offenders.Add($"method {m.Name} param {par.Name} : {par.ParameterType.FullName}");
                }
            }
        }

        CollectionAssert.IsEmpty(offenders,
            "AC-2: çekirdeğin imza yüzeyi hiçbir engine tipine bağlanamaz. Bulunanlar:\n"
            + string.Join("\n", offenders));
    }

    [Test]
    public void SceneTransitionStateSource_MentionsUnityEngineOnlyInComments()
    {
        // İmza taraması metot GÖVDELERİNİ göremez: gövdedeki bir `Debug.Log` ya da
        // `Time.time` hiçbir reflection izi bırakmaz. Story 007'nin `SafeInvoke`'u
        // (ve onun `Debug.LogException`'ı) SÜRÜCÜYE ait — buraya sızarsa bu test
        // yakalar.
        string path = UnityEditor.AssetDatabase.FindAssets("SceneTransitionState t:MonoScript")
            .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.EndsWith("/SceneTransitionState.cs", StringComparison.Ordinal));

        Assert.IsNotNull(path,
            "Kaynak BULUNAMADI — bu testin sessizce hiçbir şeyi taramadığı hâli. Dosya taşındıysa " +
            "testi SİLMEYİN, aramayı düzeltin.");

        // Yorumlar çıkarılır: dosyanın kendi doc'u zaten "hiçbir UnityEngine
        // tipine bağlanmaz" cümlesini içeriyor.
        string code = System.Text.RegularExpressions.Regex.Replace(
            System.IO.File.ReadAllText(path), @"/\*.*?\*/", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        code = System.Text.RegularExpressions.Regex.Replace(code, @"//.*?$", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Multiline);

        StringAssert.DoesNotContain("UnityEngine", code);
    }

    [Test]
    public void SceneTransitionState_IsPlainConstructible_AndStartsIdle()
    {
        var fresh = new SceneTransitionState();

        Assert.AreEqual(TransitionState.Idle, fresh.CurrentState);
        Assert.IsFalse(fresh.HasPendingHardCut);
        Assert.AreEqual(TransitionState.Idle, fresh.HardCutPreloadState,
            "Preload durumu CurrentState'ten AYRI ama o da Idle'dan başlar.");
        Assert.IsNull(fresh.HardCutPreloadScene);
    }

    // ── AC-3: Event tam bir kez, doğru type ile ──

    [Test]
    public void SetState_WritesStateAndRaisesExactlyOnce_WithTheCallersType()
    {
        _state.SetState(TransitionState.Preloading, TransitionType.Soft);

        Assert.AreEqual(TransitionState.Preloading, _state.CurrentState);
        Assert.AreEqual(1, _observed.Count);
        Assert.AreEqual((TransitionState.Preloading, TransitionType.Soft), _observed[0]);
        Assert.AreEqual(TransitionType.Soft, _state.ActiveType);
    }

    [Test]
    public void SetState_CarriesHardType_Unchanged()
    {
        // Aynı durum makinesi iki tür için de koşar (GDD AC-2) — type payload'ı
        // dönüştürülmeden taşınmalı, yoksa Adaptif Ses filtresi çalışmaz.
        _state.SetState(TransitionState.Swapping, TransitionType.Hard);

        Assert.AreEqual((TransitionState.Swapping, TransitionType.Hard), _observed[0]);
        Assert.AreEqual(TransitionType.Hard, _state.ActiveType);
    }

    [Test]
    public void SetState_IsNotIdempotent_RepeatedSameStateRaisesAgain()
    {
        // BİLİNÇLİ: bastırma, sürücünün aynı adımı iki kez yayınlaması gibi bir
        // hatayı sessizce gizlerdi ve dizi testleri onu yakalayamazdı.
        _state.SetState(TransitionState.Ready, TransitionType.Soft);
        _state.SetState(TransitionState.Ready, TransitionType.Soft);

        Assert.AreEqual(2, _observed.Count);
    }

    [Test]
    public void SetState_WithNoSubscriber_DoesNotThrow()
    {
        var unobserved = new SceneTransitionState();

        Assert.DoesNotThrow(() => unobserved.SetState(TransitionState.Preloading, TransitionType.Soft));
        Assert.AreEqual(TransitionState.Preloading, unobserved.CurrentState);
    }

    // ── AC-4: Dizi hiçbir durumu atlamaz ──

    [Test]
    public void HappyPathSequence_IsExactlyPreloadingReadySwappingCompleteIdle()
    {
        DriveHappyPath(TransitionType.Soft);

        CollectionAssert.AreEqual(
            new[]
            {
                TransitionState.Preloading, TransitionState.Ready, TransitionState.Swapping,
                TransitionState.Complete, TransitionState.Idle,
            },
            ObservedStates(),
            "GDD AC-1: hiçbir durum atlanmadan/sırası değişmeden. SON DURUMU değil TÜM DİZİYİ " +
            "assert ediyoruz — yalnız Idle'a bakan bir test atlanan bir durumu kaçırır.");
    }

    [Test]
    public void HappyPathSequence_CarriesTheSameTypeThroughout()
    {
        DriveHappyPath(TransitionType.Hard);

        CollectionAssert.AreEqual(
            Enumerable.Repeat(TransitionType.Hard, 5).ToList(),
            _observed.Select(o => o.type).ToList(),
            "Tür geçiş boyunca değişmez — Adaptif Ses yalnız Swapping+Hard'ı işliyor.");
    }

    private void DriveHappyPath(TransitionType type)
    {
        _state.SetState(TransitionState.Preloading, type);
        _state.SetState(TransitionState.Ready, type);
        _state.SetState(TransitionState.Swapping, type);
        _state.SetState(TransitionState.Complete, type);
        _state.SetState(TransitionState.Idle, type);
    }

    // ── AC-5: Failed otomatik Idle'a döner ──

    [Test]
    public void Fail_PublishesFailedThenAutomaticallyReturnsToIdle()
    {
        _state.SetState(TransitionState.Preloading, TransitionType.Soft);

        _state.Fail(TransitionType.Soft);

        CollectionAssert.AreEqual(
            new[] { TransitionState.Preloading, TransitionState.Failed, TransitionState.Idle },
            ObservedStates(),
            "GDD AC-11a: Failed KALICI DEĞİL. Failed'de takılan bir implementasyon, tek bozuk " +
            "sahne referansıyla oturumun geri kalanındaki her geçişi soft-lock'lar — GDD'nin " +
            "kendi ifadesiyle 'üretim durdurucu bir boşluk'.");
        Assert.AreEqual(TransitionState.Idle, _state.CurrentState);
    }

    [Test]
    public void Fail_RunsTheCallbackHook_BETWEEN_FailedAndIdle()
    {
        // Sıra sözleşmenin KENDİSİ: onFailed içinde durumu örnekleyen bir çağıran
        // Failed görmeli, ama callback'ten hemen sonra gönderdiği yeni istek
        // kabul edilmeli. Hook Failed'den ÖNCE ya da Idle'dan SONRA koşarsa
        // çağıranın gördüğü durum yanlış olur.
        TransitionState stateSeenInsideCallback = TransitionState.Complete; // kasten yanlış tohum
        int hookCalls = 0;

        _state.Fail(TransitionType.Hard, () =>
        {
            hookCalls++;
            stateSeenInsideCallback = _state.CurrentState;
        });

        Assert.AreEqual(TransitionState.Failed, stateSeenInsideCallback,
            "Callback Failed ile Idle ARASINDA koşmalı.");
        Assert.AreEqual(1, hookCalls,
            "GDD onComplete/onFailed'i 'TAM OLARAK bir kez' diye bağlıyor — çift invoke sayaç olmadan yeşil geçerdi.");
        Assert.AreEqual(TransitionState.Idle, _state.CurrentState);
    }

    [Test]
    public void Fail_StillReachesIdle_WhenTheHookThrows()
    {
        // SOFT-LOCK REGRESYONU (LP gate bulgusu). Hook sarmalanmasaydı, fırlatan
        // bir callback `SetState(Idle)`'ı atlatır ve durum makinesini kalıcı
        // olarak `Failed`'de bırakırdı — GDD'nin "üretim durdurucu boşluk" dediği
        // ve bu story'nin var oluş sebebi olan şeyin ta kendisi. Pencere Story
        // 003-006: sürücü, `SafeInvoke` daha yokken ham bir `onFailed` geçebilir.
        //
        // İstisna YUTULMAZ — dışarı çıkmaya devam eder (yakalama+loglama Story
        // 007'nin SÜRÜCÜDEKİ SafeInvoke'unun işi). Burada garanti edilen tek şey
        // Idle'a dönüşün KOŞULSUZ olduğu.
        Assert.Throws<InvalidOperationException>(() =>
            _state.Fail(TransitionType.Soft, () => throw new InvalidOperationException("callback patladı")));

        Assert.AreEqual(TransitionState.Idle, _state.CurrentState,
            "Fırlatan bir hook'tan sonra bile Idle'a ulaşılmalı.");
        CollectionAssert.AreEqual(
            new[] { TransitionState.Failed, TransitionState.Idle },
            ObservedStates());
    }

    [Test]
    public void Fail_WithoutCallback_StillReachesIdle()
    {
        Assert.DoesNotThrow(() => _state.Fail(TransitionType.Soft));
        Assert.AreEqual(TransitionState.Idle, _state.CurrentState);
    }

    [Test]
    public void AfterFail_StateIsIdle_SoANewRequestWouldBeAccepted()
    {
        // Kabul/ret hakemliğinin KENDİSİ Story 006'nın işi. Burada yalnız
        // hakemliğin dayanacağı ön koşul doğrulanıyor: Failed'den sonra
        // CurrentState gerçekten Idle.
        //
        // `ActiveType` BİLEREK assert EDİLMİYOR: Story 006 ret gerekçesini
        // `_activeType`'tan yalnız `CurrentState != Idle` iken türetiyor, yani
        // `Idle`'a dönüldükten sonra o değer hiçbir tüketici tarafından
        // okunamaz. Assert etmek, Story 006'nın `Idle`'da makul şekilde
        // temizleyebileceği bir detayı pinlerdi (QL gate tavsiyesi).
        _state.SetState(TransitionState.Preloading, TransitionType.Soft);
        _state.Fail(TransitionType.Soft);

        Assert.AreEqual(TransitionState.Idle, _state.CurrentState);
    }

    // ── Preload lane'inin CurrentState'ten ayrılığı ──

    [Test]
    public void DrivingThePublicSequence_NeverTouchesTheHardCutPreloadLane()
    {
        // Bu sistemin en yük taşıyan kararı: preload lane'i `CurrentState`'ten
        // AYRIDIR (GDD Edge Cases) — bir HARD CUT preload'u aktif bir SOFT
        // sürerken arka planda ilerleyebilmeli.
        //
        // TAZE bir örnekte "Idle" görmek bunu KANITLAMAZ, yalnız initializer'ı
        // tekrarlar. Kanıt, tam diziyi sürüp lane'in dokunulmadığını görmektir:
        // `SetState`'e yanlışlıkla `_hardCutPreloadState` yazan bir satır —
        // dosyanın kendi yorumunun uyardığı kopyala-yapıştır hatası — diğer
        // testlerin HİÇBİRİNİ kırmıyordu (QL gate bulgusu).
        DriveHappyPath(TransitionType.Soft);
        _state.Fail(TransitionType.Hard);

        Assert.AreEqual(TransitionState.Idle, _state.HardCutPreloadState,
            "Preload lane'i herkese açık durum yayınıyla YAZILAMAZ.");
        Assert.IsNull(_state.HardCutPreloadScene);
        Assert.IsFalse(_state.HasPendingHardCut,
            "Bekleyen slot yalnız Story 006'nın hakemliğiyle dolar, durum yayınıyla DEĞİL.");
        Assert.IsFalse(_state.HardCutPreloadConfig.Abrupt);
        Assert.IsFalse(_state.ActiveHardCutConfig.Abrupt,
            "Aktif HARD CUT config'i de dokunulmadan kalır — senkron-bekleme fallback'i onu Story 005'te doldurur.");
    }
}
