using UnityEngine;
using UnityEngine.InputSystem;

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

    private const float GroundedStickVelocity = -2f;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _eyeCamera;
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] internal LayerMask _controllerHitInterestMask;
    [Range(0.15f, 0.25f)]
    [SerializeField] private float _rampTime = 0.2f;
    [SerializeField] private float _mouseSensitivity = 0.12f;

    private PlayerStateProvider _state;
    private InputAction _moveAction;
    private InputAction _lookAction;

    private float _currentSpeed;
    private float _pitch;
    private float _verticalVelocity;
    private Vector3 _lastMoveDirection = Vector3.forward;

    /// <summary>Test-gözlemlenebilirlik — ilgi-maskesindeki bir çarpışmada artar (ADR-0004 `internal` sapma deseniyle aynı).</summary>
    internal int InterestedHitCount { get; private set; }

    private void Awake()
    {
        _state = GetComponent<PlayerStateProvider>();
        _state.EyeCamera = _eyeCamera;

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
        // Story 005'e kadar taper etkisiz — d, TaperRadius'ta sabitlenir (InteractableRegistry
        // henüz okunmuyor; TaperMultiplier(TaperRadius) yapısal olarak tam 1.0 verir).
        float maxTargetSpeed = MovementMathFormulas.TargetSpeed(carryMultiplier, MovementMathFormulas.TaperRadius);
        float targetSpeed = maxTargetSpeed * Mathf.Clamp01(moveInput.magnitude);

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

    /// <summary>Pitch kelepçesi — saf, `Camera`/Input gerekmeden test edilebilir (thin-driver split).</summary>
    internal static float ClampPitch(float currentPitch, float pitchDelta, float clampDegrees) =>
        Mathf.Clamp(currentPitch + pitchDelta, -clampDegrees, clampDegrees);
}
