using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Hareket/kamera/input sürücüsü (ADR-0003 primary; GDD Core Rules). `PlayerStateProvider`'dan
/// BİLEREK AYRI `MonoBehaviour` — kendi `Awake()`'inde `GetComponent&lt;PlayerStateProvider&gt;()`
/// ile aynı GameObject'teki instance'a bağlanır (`Awake()` sırası garantisi yok ama hazard değil:
/// `GetComponent` component VARLIĞINA bağlıdır, hedefin kendi `Awake()`'inin koşup koşmadığına
/// değil — ADR-0003 "confirmed non-issue"). Story 002'nin saf formüllerini TÜKETİR, yeniden
/// türetmez (control manifest BLOCKING kural). Ham `CharacterController` hiçbir public erişimciyle
/// dışa açılmaz (AC-10) — dış sistemler pozisyonu etkilemek isterse Story 004'ün kilitleyeceği
/// sarmalanmış bir Move()/RepositionTo() API'sini kullanacak (bu story yalnız yapısal kısıtı sağlar).
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(PlayerStateProvider))]
public sealed class FirstPersonController : MonoBehaviour
{
    /// <summary>Kamera pitch kelepçesi (derece) — Core Rules kilitli sabit, yaw'a UYGULANMAZ.</summary>
    public const float PitchClampDegrees = 80f;

    /// <summary>
    /// CharacterController step offset (m) — GDD: kitin en küçük eşiğinin (~2cm) ALTINDA
    /// (eşit değil; eşit olsaydı her eşik geçişinde kamera zıplardı — GDD Edge Case).
    /// </summary>
    public const float LockedStepOffset = 0.018f;

    /// <summary>GDD'nin "en küçük eşik" referansı (m) — `LockedStepOffset` bunun ALTINDA olmalı.</summary>
    public const float SmallestThresholdHeight = 0.02f;

    /// <summary>CharacterController skin width, kapsül yarıçapının oranı — GDD Tuning Knob kilitli varsayılan (%10).</summary>
    public const float SkinWidthToRadiusRatio = 0.1f;

    /// <summary>Gamepad çubuğu ORAN girdisidir (mouse delta değil) — derece/saniye olarak ölçeklenir.</summary>
    public const float GamepadLookDegreesPerSecond = 180f;

    /// <summary>
    /// Bir adımlık mesafe (m) — faz akümülatörünün ayak sesi/bob dönemselliğini
    /// mesafeye bağlar (SAAT ZAMANINA değil). Tuning Knob adayı.
    /// </summary>
    public const float StrideLength = 0.75f;

    private const float GroundedStickVelocity = -2f;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _eyeCamera;
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] internal LayerMask _controllerHitInterestMask;
    [Range(0.15f, 0.25f)]
    [SerializeField] private float _rampTime = 0.2f;
    [SerializeField] private float _mouseSensitivity = 0.12f;

    /// <summary>A_max (m) — GDD Tuning Knob, 1-3cm.</summary>
    [Range(0.01f, 0.03f)]
    [SerializeField] private float _maxBobAmplitude = 0.025f;

    // S (0-100) — erişilebilirlik motion yoğunluk kaydırıcısı.
    // ÇELİŞKİ (çözülmedi, kullanıcıya bildirildi): `accessibility-requirements.md` §5
    // "varsayılan %100" diyor, GDD Core Rules "~%40". İkisi de onaylı belge. Kod GDD'yi
    // izliyor (daha muhafazakâr — motion-sickness riski düşük). Gerçek Ayarlar yüzeyi
    // henüz YOK (§7 "bilinen boşluk") — değer o epic'te bağlanınca çelişki çözülmeli.
    [Range(0f, 100f)]
    [SerializeField] private float _motionIntensityPercent = 40f;

    private PlayerStateProvider _state;
    private InputAction _moveAction;
    private InputAction _lookAction;

    private float _currentSpeed;
    private float _pitch;
    private float _verticalVelocity;
    private Vector3 _lastMoveDirection = Vector3.forward;

    private readonly MovementPhaseAccumulator _phaseAccumulator = new MovementPhaseAccumulator();
    private Vector3 _eyeRestLocalPosition;
    private int _lastFootstepIndex;

    // Kayıt yokken taper etkisiz — TaperRadius yapısal olarak 1.0 çarpan verir.
    private float _currentTaperDistance = MovementMathFormulas.TaperRadius;
    private float _lastTickDeltaTime;

    /// <summary>
    /// Paylaşılan mesafe-tabanlı faz (m) — bob eğrisi VE ayak sesi tetiklemeleri
    /// AYNI bu değerden türer (TR-fpc-014). Adaptif Ses'in `PlayFootstep` çağrıları
    /// bunu tüketecek; ayrı bir zamanlayıcı ASLA kurulmamalı (desync yasak).
    /// </summary>
    public float MovementPhase => _phaseAccumulator.Phase;

    /// <summary>Kaçıncı adımda olunduğu — faz/StrideLength'in tam kısmı. Ayak sesi bu değer arttığında tetiklenir.</summary>
    public int FootstepIndex => Mathf.FloorToInt(_phaseAccumulator.Phase / StrideLength);

    /// <summary>Ayak sesi tetiklendi (Adaptif Ses aboneliği için). Payload: o andaki hız (m/s).</summary>
    public event System.Action<float> FootstepTriggered;

    /// <summary>Erişilebilirlik motion yoğunluğu (S, 0-100). Ayarlar epic'i gelene kadar tek yazma yolu.</summary>
    internal float MotionIntensityPercent
    {
        get => _motionIntensityPercent;
        set => _motionIntensityPercent = Mathf.Clamp(value, 0f, 100f);
    }

    /// <summary>Bu karedeki head-bob genliği (m) — Story 002'nin Formül 3'ünden.</summary>
    internal float CurrentBobAmplitude =>
        MovementMathFormulas.HeadBobAmplitude(_currentSpeed, _motionIntensityPercent, _maxBobAmplitude);

    /// <summary>Test-gözlemlenebilirlik — ilgi-maskesindeki bir çarpışmada artar (ADR-0004 `internal` sapma deseniyle aynı).</summary>
    internal int InterestedHitCount { get; private set; }

    private void Awake()
    {
        _state = GetComponent<PlayerStateProvider>();
        _state.EyeCamera = _eyeCamera;
        _eyeRestLocalPosition = _eyeCamera.localPosition;

        _characterController.stepOffset = LockedStepOffset;
        _characterController.skinWidth = _characterController.radius * SkinWidthToRadiusRatio;

        // Asset'in KOPYASI üzerinde çalış — paylaşılan instance'ı Enable/Disable etmek
        // yıkıcı bir yan etki üretiyordu: duplicate-guard'ın yok ettiği ikinci oyuncunun
        // OnDisable'ı, HAYATTA KALAN oyuncunun da girdisini kapatıyordu (ADR-0003'ün
        // kendi kurtarma yolunu bozan sessiz tam-girdi kaybı — LP-CODE-REVIEW bulgusu).
        _inputActions = Instantiate(_inputActions);
    }

    private void OnEnable()
    {
        InputActionMap map = _inputActions.FindActionMap("Gameplay");
        if (map == null)
        {
            Debug.LogError("'Gameplay' action map bulunamadı — girdi ölü.", this);
            return;
        }

        _moveAction = map.FindAction("Move");
        _lookAction = map.FindAction("Look");
        if (_moveAction == null || _lookAction == null)
        {
            Debug.LogError("'Move'/'Look' eylemleri bulunamadı — girdi ölü.", this);
            return;
        }

        map.Enable();
    }

    private void OnDisable()
    {
        _inputActions.FindActionMap("Gameplay")?.Disable();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    /// <summary>
    /// Gerçek kare güncellemesi — Input System'den okur, cihaza göre NORMALİZE eder, çekirdeğe devreder.
    /// Mouse `delta` zaten kare-başı bir DELTA'dır; gamepad çubuğu ise [-1,1] bir ORAN — ikisi aynı
    /// ölçekle çarpılırsa gamepad bakışı hem çok yavaş hem kare-hızına bağımlı olur (LP-CODE-REVIEW
    /// bulgusu). Cihaz ayrımı BURADA yapılır; `TickWithInput` saf delta sözleşmesiyle kalır.
    /// </summary>
    private void Tick(float deltaTime)
    {
        if (_moveAction == null || _lookAction == null)
        {
            return;
        }

        Vector2 rawLook = _lookAction.ReadValue<Vector2>();
        bool isRateBasedDevice = _lookAction.activeControl?.device is UnityEngine.InputSystem.Gamepad;
        Vector2 lookDelta = isRateBasedDevice
            ? rawLook * (GamepadLookDegreesPerSecond * deltaTime / Mathf.Max(ProjectEpsilon.TIME_EPSILON, _mouseSensitivity))
            : rawLook;

        TickWithInput(_moveAction.ReadValue<Vector2>(), lookDelta, deltaTime);
    }

    /// <summary>
    /// Çekirdek karelik mantık — ham Move/Look girdisini + `deltaTime`'ı parametre alır, Input
    /// System/`Time.deltaTime`'a bağımlı değil (thin-driver/testability, control manifest BLOCKING
    /// kural). Kilit-kapılama BURADA uygulanır — girdi kaynağı ne olursa olsun (gerçek InputAction
    /// ya da test) aynı kurallar geçerli.
    /// </summary>
    internal void TickWithInput(Vector2 rawMoveInput, Vector2 rawLookInput, float deltaTime)
    {
        bool movementLocked = _state.MovementLocked;
        MovementLockScope scope = movementLocked ? _state.EffectiveScope() : MovementLockScope.MoveOnly;
        bool moveBlocked = movementLocked;
        bool lookBlocked = movementLocked && scope == MovementLockScope.Full;

        ApplyLook(lookBlocked ? Vector2.zero : rawLookInput);
        ApplyMove(moveBlocked ? Vector2.zero : rawMoveInput, deltaTime);
    }

    private void ApplyLook(Vector2 lookInput)
    {
        float yawDelta = lookInput.x * _mouseSensitivity;
        float pitchDelta = -lookInput.y * _mouseSensitivity;

        transform.Rotate(Vector3.up, yawDelta);
        _pitch = ClampPitch(_pitch, pitchDelta, PitchClampDegrees);
        _eyeCamera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void ApplyMove(Vector2 moveInput, float deltaTime)
    {
        Vector3 inputDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            _lastMoveDirection = inputDirection.normalized;
        }

        float carryMultiplier = _state.IsCarrying ? MovementMathFormulas.CarryMultiplier : 1f;
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        // Story 005: `d` artık GERÇEK registry'den. Girdi yokken (boşta VEYA hareket kilitli —
        // ikisinde de moveInput sıfırdır) taramayı ATLA: `Snapshot()` her yeni karede
        // `_live.ToArray()` yapar, yani koşulsuz çağrı kalıcı per-frame çöp üretirdi.
        // ADR-0004'ün "no unconditional per-frame ToArray()" garantisi böyle korunur (LP bulgusu).
        if (inputMagnitude > 0f)
        {
            _currentTaperDistance = DistanceToNearestInteractable();
        }

        float maxTargetSpeed = MovementMathFormulas.TargetSpeed(carryMultiplier, _currentTaperDistance);
        float targetSpeed = maxTargetSpeed * inputMagnitude;

        _currentSpeed = MovementMathFormulas.SpeedSmooth(_currentSpeed, targetSpeed, deltaTime, _rampTime);

        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = GroundedStickVelocity;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * deltaTime;
        }

        Vector3 horizontalVelocity = _lastMoveDirection * _currentSpeed;
        Vector3 commandedVelocity = horizontalVelocity + Vector3.up * _verticalVelocity;

        // GERÇEKLEŞEN hız yayınlanır, KOMUT EDİLEN değil (LP/QL bulgusu): komut edilen
        // değer (a) yere yapıştırma sabitini (-2 m/s) taşıdığı için DURAN oyuncuda bile
        // |Velocity|=2 okunurdu, (b) duvara dayalı oyuncuda 1.6 m/s yalanını söylerdi.
        // Tüketiciler (Adaptif Ses PlayFootstep, Işık/Volume PlayerMaxSpeed) `.magnitude`
        // okuyacak — sözleşme gerçek yer değiştirme olmalı.
        // `CharacterController.velocity` KULLANILMAZ: onu Unity kendi kare delta'sıyla
        // hesaplar, bizim `deltaTime` parametremizle değil — sürücünün tick'i dışarıdan
        // sürüldüğünde (testler) tutarsız/şişkin değerler veriyor (ampirik).
        Vector3 positionBeforeMove = transform.position;
        _characterController.Move(commandedVelocity * deltaTime);
        Vector3 achievedDisplacement = transform.position - positionBeforeMove;

        // Sıfıra-bölme guard'ı — TIME_EPSILON (0.01s) KULLANILMAZ: 120fps'te kare
        // süresi 0.0083s'dir ve hız sessizce sıfırlanırdı. Yalnız gerçek sıfır dışlanır.
        _state.Velocity = deltaTime > 1e-6f ? achievedDisplacement / deltaTime : Vector3.zero;
        _state.IsGrounded = _characterController.isGrounded;

        _lastTickDeltaTime = deltaTime;
        AdvancePhaseAndApplyBob(achievedDisplacement);
    }

    /// <summary>
    /// Faz akümülatörünü GERÇEKLEŞEN YATAY yer değiştirmeyle ilerletir (saat zamanıyla
    /// DEĞİL — duvara dayanmış oyuncu adım atmaz), sonra bob eğrisini VE ayak sesi
    /// tetiklemesini AYNI faz değerinden türetir (TR-fpc-014, desync yapısal olarak imkânsız).
    ///
    /// Erişilebilirlik bağımsızlığı: `S` yalnız `HeadBobAmplitude`'un çıktısını ölçekler;
    /// akümülatörün ilerlemesi ve `FootstepIndex` eşikleri `S`'yi HİÇ görmez — S=%0'da
    /// görsel bob sıfırdır ama ayak sesi zamanlaması değişmez (manifest guardrail,
    /// accessibility-requirements.md §5/§8).
    /// </summary>
    private void AdvancePhaseAndApplyBob(Vector3 achievedDisplacement)
    {
        achievedDisplacement.y = 0f;
        float horizontalDistance = achievedDisplacement.magnitude;
        _phaseAccumulator.Advance(horizontalDistance);

        // `>` (`!=` değil): faz monotondur, ama bu niyeti ifade eder ve ileride bir reset
        // ya da işaretli ilerleme eklenirse sahte adım tetiklemez. Bir karede birden çok
        // stride sınırı geçilirse TEK event fırlar — hitch'te doğru davranış.
        int footstepIndex = FootstepIndex;
        if (footstepIndex > _lastFootstepIndex)
        {
            _lastFootstepIndex = footstepIndex;
            // GERÇEKLEŞEN yatay hız yayınlanır (komut edilen DEĞİL) — `Velocity` sözleşmesiyle
            // tutarlı: duvara sürtünerek ilerleyen oyuncu 1.6 m/s yalanını söylememeli
            // (Adaptif Ses ayak sesi şiddetini bu değerle ölçekleyecek).
            float achievedSpeed = _lastTickDeltaTime > 1e-6f ? horizontalDistance / _lastTickDeltaTime : 0f;
            FootstepTriggered?.Invoke(achievedSpeed);
        }

        // Bob: adım başına tam bir döngü. `-cos` (sin DEĞİL) — çukur TAM ayak basışına denk
        // gelir; `sin` onu yarım-stride kaydırıp "bedenin işi bildiği" hissini bozardı.
        float bobOffset = CurrentBobAmplitude * -Mathf.Cos(_phaseAccumulator.Phase / StrideLength * 2f * Mathf.PI);

        // TEK YAZICI: `_eyeCamera.localPosition`'ı bu satırdan başka hiçbir yer yazmamalı
        // (manifest'in `Volume.weight` için uyguladığı aynı kural). ADR-0013'ün carry-sway'i
        // aynı akümülatörü paylaşacak — geldiğinde buraya TOPLAMSAL bir ofset sözleşmesiyle
        // katılmalı, ayrı bir yazıcı olarak DEĞİL (yoksa sessizce ezilir).
        _eyeCamera.localPosition = _eyeRestLocalPosition + Vector3.up * bobOffset;
    }

    /// <summary>
    /// Formül 2'nin `d` girdisi — kayıtlı interactable'lara olan YATAY mesafelerin MİNİMUMU
    /// (GDD Edge Case: "en son izlenen nesne" değil; minimum geçiş noktalarında sürekli
    /// olduğu için TaperMult asla sıçramaz).
    ///
    /// **YATAY (xz) mesafe — 3B DEĞİL (LP+QL gate bulgusu, bağımsız olarak iki gate).**
    /// Oyuncu Transform'u kapsülün TABANINDA (prefab: center y=0.9, height 1.8). 3B mesafeyle,
    /// GDD'nin kendi kamuflaj decoy'ları — kapı kolu, ışık anahtarı, termostat, hepsi el
    /// yüksekliğinde (~1.0-1.2m) — `d`'yi ASLA ~1.2'nin altına indiremezdi: TaperMult ≈ 0.97,
    /// yani tasarlanan %30 yavaşlama yerine %3. Bu, taper'ı gerçek içerikte gürültüye
    /// indirger ve CD-GDD-ALIGN'ın kapattığı "metal dedektörü" istismarını kısmen geri açardı
    /// (yer seviyesindeki nesne %30, el yüksekliğindeki %3 yavaşlatırsa fark ipucu olur).
    /// Yatay mesafe ayrıca `d`'yi keyfî prop pivot yüksekliğinden bağımsız kılar.
    ///
    /// Pozisyon kaynağı: `IInteractable` arayüzünde pozisyon YOK (interactable-registry
    /// epic'i tanımlamadı) — story'nin açık noktası. Arayüze alan eklemek Out of Scope
    /// (ayrı ADR kararı), bu yüzden `Component` cast'i kullanılır: gerçek interactable'lar
    /// sahnedeki MonoBehaviour'lardır. Component OLMAYAN kayıtlar sessizce atlanır; Unity
    /// fake-null kontrolü de gerekli — `Snapshot()` kare-başı cache olduğundan aynı kare
    /// içinde YOK EDİLMİŞ bir obje hâlâ listede olabilir (ADR-0004 Risks).
    ///
    /// SOFT co-residency guard (manifest ZORUNLU kuralı): yalnız AKTİF sahnedeki
    /// interactable'lar sayılır — geçiş sırasında iki seviye sahnesi eşzamanlı yüklüdür ve
    /// terk edilen sahnenin propları oyuncuyu yavaşlatmamalıdır.
    /// </summary>
    private float DistanceToNearestInteractable()
    {
        IReadOnlyList<IInteractable> snapshot = InteractableRegistry.Snapshot();
        Scene activeScene = SceneManager.GetActiveScene();
        Vector3 playerPosition = transform.position;
        float nearestSqr = float.PositiveInfinity;

        for (int i = 0; i < snapshot.Count; i++)
        {
            if (snapshot[i] is not Component component || component == null)
            {
                continue;
            }

            if (component.gameObject.scene != activeScene)
            {
                continue;
            }

            Vector3 delta = component.transform.position - playerPosition;
            delta.y = 0f; // YATAY — yukarıdaki gerekçe.
            float sqrDistance = delta.sqrMagnitude;
            if (sqrDistance < nearestSqr)
            {
                nearestSqr = sqrDistance;
            }
        }

        // Hiç kayıt yoksa taper etkisiz: TaperRadius, TaperMultiplier'ı yapısal olarak tam 1.0 yapar.
        return float.IsPositiveInfinity(nearestSqr) ? MovementMathFormulas.TaperRadius : Mathf.Sqrt(nearestSqr);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Guardrail — açık ilgi-maskesinde olmayan katmanlara karşı erken çıkış (statik-geometri spam'i önlenir).
        if ((_controllerHitInterestMask.value & (1 << hit.collider.gameObject.layer)) == 0)
        {
            return;
        }

        // İlgi-maskesindeki (hareketli platform/dinamik tehlike) katmanlara karşı mantık — henüz
        // hiçbir tüketici yok (Asansör/Kat-Erişim epic'i platform-delta enjeksiyonuna ihtiyaç
        // duymuyor, GDD Edge Case zaten retract etti); guard'ın kendisi bu story'nin kapsamı.
        // InterestedHitCount yalnız test-gözlemlenebilirlik için (guard'ın gerçekten geçtiğini kanıtlar).
        InterestedHitCount++;
    }

    /// <summary>
    /// SANCTIONED repozisyon primitifi (Story 004, TR-fpc-010) — "ham `CharacterController`
    /// dışa açılmaz" kısıtının TEK istisnası. SOFT transition'da oyuncuyu hedef sahnenin
    /// `SoftTransitionAnchor`'ına taşır: YALNIZ translation + rotation kopyalanır, GameObject
    /// ASLA yok edilip yeniden yaratılmaz (`PlayerStateProvider.Current` aynı kalır).
    ///
    /// Örtük reset YOK — `Velocity`, `IsGrounded`, `IsCarrying`, koşan hız ve kamera pitch'i
    /// dokunulmadan kalır ("Bedenin Sürekliliği": görünür pop/süreksizlik olmamalı, ADR-0003
    /// Context). `seviye-sahne-gecisi` epic'i bunu ÇAĞIRACAK, kendi `transform.position=`
    /// yazımını icat ETMEYECEK (ADR-0002'nin "3 ADR aynı soruyu ayrı ayrı icat etmesin"
    /// disiplini) — bu story yalnız primitifi kilitler, entegrasyonu yapmaz.
    /// </summary>
    /// <param name="anchor">Hedef anchor; null ise çağrı no-op'tur.</param>
    public void RepositionTo(Transform anchor)
    {
        if (anchor == null)
        {
            Debug.LogError("RepositionTo(null) — repozisyon atlandı.", this);
            return;
        }

        ApplyPose(anchor.position, anchor.rotation);
    }

    /// <summary>
    /// SOFT transition'ın GERÇEK sözleşmesi — GÖRELİ (kabin-yerel) süreklilik. Oyuncunun
    /// `sourceAnchor`'a göre yerel pozisyon/rotasyonu ölçülür ve `targetAnchor` altında
    /// birebir yeniden uygulanır. Yukarıdaki tek-anchor formu MUTLAK bir snap'tir ve SOFT
    /// için YANLIŞTIR: oyuncunun kabin içindeki serbest konumu/bakışı (ride sırasında kilit
    /// `MoveOnly`, yani Look serbest — ADR-0011) silinir ve GDD'nin açıkça yasakladığı
    /// "pozisyon sıçraması/dönüş atlaması" oluşur.
    ///
    /// Kaynaklar: `seviye-sahne-gecisi.md` koordinat-çerçevesi hizalama kuralı ("kopyalanan,
    /// dünya-uzayı pozisyonu değil, kabin-yerel pozisyon/rotasyondur"); ADR-0008'in
    /// `CopySoftTransitionAnchorTransform(fromScene, toScene)` imzası (İKİ sahne alır);
    /// ADR-0015: "SOFT's anchor-copy semantics are relative continuity FROM a source scene."
    ///
    /// Tek-anchor formu terk edilmiş değildir — ADR-0015'in boot spawn'ı (`InitialSpawnAnchor`,
    /// kaynak sahne YOK) tam olarak onu ister. İki form iki farklı gerçek çağıranı karşılar.
    ///
    /// FAZ İNVARYANTI (her iki form): ışınlanma bir ADIM DEĞİLDİR — repozisyon `ApplyMove`'u
    /// atlar, bu yüzden `MovementPhase` ilerlemez ve SOFT geçişte hayalet ayak sesi çalmaz.
    /// Bu, çağrı yerinin tesadüfü değil bilinçli sözleşmedir; testle kilitlidir.
    /// </summary>
    public void RepositionTo(Transform sourceAnchor, Transform targetAnchor)
    {
        if (sourceAnchor == null || targetAnchor == null)
        {
            Debug.LogError("RepositionTo(source, target) — anchor null, repozisyon atlandı.", this);
            return;
        }

        // Oyuncunun kaynak anchor'a göre YEREL pozu.
        Vector3 localPosition = sourceAnchor.InverseTransformPoint(transform.position);
        Quaternion localRotation = Quaternion.Inverse(sourceAnchor.rotation) * transform.rotation;

        ApplyPose(targetAnchor.TransformPoint(localPosition), targetAnchor.rotation * localRotation);
    }

    /// <summary>
    /// İki repozisyon formunun ortak gövdesi. Gövde rotasyonu YAW'a düzleştirilir:
    /// `FirstPersonController` gövdeyi yapısal olarak yalnız yaw'da döndürür (pitch kamerada,
    /// `ApplyLook`), bu yüzden eğik bir anchor kalıcı ve KURTARILAMAZ bir gövde eğimi üretirdi
    /// — `transform.Rotate(Vector3.up, ...)` Space.Self olduğundan eğim asla düzelmez ve
    /// `transform.forward`/`right` dikey bileşen kazanıp hareket yönünü bozar. Guard KODDA
    /// zorunludur, tasarımcı disiplinine bırakılmaz (projenin `IsikVolumeFormulas` emsali).
    /// </summary>
    private void ApplyPose(Vector3 position, Quaternion rotation)
    {
        Quaternion yawOnlyRotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);

        // Gövdenin dünya-uzayı dönüş farkı: korunan hareket yönü bununla birlikte döner
        // (aşağıya bak). Yazımdan ÖNCE hesaplanmalı.
        Quaternion rotationDelta = yawOnlyRotation * Quaternion.Inverse(transform.rotation);

        // CharacterController etkinken doğrudan Transform yazımıyla YARIŞIR (kendi sweep
        // durumunu korur ve bir sonraki Move'da geri çekebilir) — yazım süresince kapatılır.
        // Bu aynı zamanda `Physics.autoSyncTransforms=false` altında da doğrudur: yeniden
        // etkinleştirme PhysX controller'ını Transform'DAN yeniden kurar.
        bool controllerWasEnabled = _characterController.enabled;
        _characterController.enabled = false;
        transform.SetPositionAndRotation(position, yawOnlyRotation);
        _characterController.enabled = controllerWasEnabled;

        // Korunan yön DÜNYA uzayındadır; gövde döndüyse onunla birlikte döner. Aksi hâlde
        // hız henüz sönmemişken oyuncu eski dünya yönüne doğru kayardı — tam olarak
        // önlenmesi istenen görünür süreksizlik. Bu bir reset DEĞİL, sürekliliğin korunması.
        // (`_state.Velocity` neden döndürülmüyor: o her karede gerçekleşen yer değiştirmeden
        // YENİDEN YAZILIYOR, yani bayat kalamaz; `_lastMoveDirection` ise girdi sıfırken
        // `_rampTime` boyunca GERÇEKTEN saklanır — asimetri kasıtlı.)
        _lastMoveDirection = rotationDelta * _lastMoveDirection;
    }

    /// <summary>Test-gözlemlenebilirlik: hareket girdisi canlı mı (Story 003'ün paylaşılan-asset bug'ının regresyon kancası).</summary>
    internal bool InputActive => _moveAction != null && _moveAction.enabled;

    /// <summary>Test-gözlemlenebilirlik: korunan dünya-uzayı hareket yönü (repozisyonda döner).</summary>
    internal Vector3 LastMoveDirection => _lastMoveDirection;

    /// <summary>Test-gözlemlenebilirlik: bu karede kullanılan `d` (Formül 2 girdisi, yatay mesafe).</summary>
    internal float CurrentTaperDistance => _currentTaperDistance;

    /// <summary>Pitch kelepçesi — saf, `Camera`/Input gerekmeden test edilebilir (thin-driver split).</summary>
    internal static float ClampPitch(float currentPitch, float pitchDelta, float clampDegrees) =>
        Mathf.Clamp(currentPitch + pitchDelta, -clampDegrees, clampDegrees);
}
