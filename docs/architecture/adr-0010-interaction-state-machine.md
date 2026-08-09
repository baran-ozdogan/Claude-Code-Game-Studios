# ADR-0010: Interaction State Machine (Focus/Hold)

> **Unity Specialist Validation**: BLOCKING (2 findings, found and fixed) 2026-08-07 — (1) `InteractableRegistry.Instance.Snapshot()` was called throughout the draft, but ADR-0004 deliberately made `InteractableRegistry` a bare static class with no `.Instance` facade (an explicit rejected-alternative in that ADR) — fixed all 4 occurrences to the correct `InteractableRegistry.Snapshot()` static call, and noted the missing `using System.Linq;` its `.Contains()` extension method requires. (2) `ClosestThenLowestInstanceIdComparer` was referenced but never defined, and even a lambda/method-group wouldn't satisfy `Array.Sort`'s range-limited `(array, index, length, IComparer<T>)` overload (that form only accepts `IComparer<T>`, not `Comparison<T>`) — defined explicitly via `Comparer<RaycastHit>.Create(...)`. Also fixed 2 MINOR findings: `InteractionController`'s scene placement (persistent "Player" GameObject, ADR-0003) was never stated even though a Risk bullet's safety argument depended on it, and `UIRoot.Instance`'s Reload-Scene staleness risk was covered in Validation Criteria but missing its matching Risks bullet — added both.
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-07 — 2 major findings, fixed: (1) `movementLockAvailable` carried a defensive `|| CurrentState == Holding` clause and a Risk bullet claiming it prevented the system from self-blocking its own in-progress Hold — proven dead code, since `Tick()`'s `Holding` branch never reads `movementLockAvailable` and the switch's cases are mutually exclusive, making the described self-block scenario structurally impossible regardless; simplified to the plain `!IsLocked` check and corrected the Risk/Validation Criteria prose to describe the real structural guarantee instead of a false defensive narrative. (2) `UpdateCrosshairAndHoldFill()` was an empty stub while surrounding prose asserted specific implementation facts (cached lookup, `etkilesim-*` prefix, change-only toggling) nowhere reflected in code, undermining this ADR's own stated purpose as the concrete precedent ADR-0012 is expected to copy — filled in with real field declarations, an `OnEnable()` performing cached null-checked `Q<VisualElement>()` lookups (matching ADR-0002's own worked-example timing), and working update logic. 2 minor findings, also fixed: the Decision section's "more cheaply, a check against the already-iterated `InteractableRegistry.Snapshot()` set" claim was unsubstantiated (`Snapshot()` is `List`/array-backed, an O(n) `.Contains()` scan, not obviously cheaper than one `GetComponent` call, and the actual code sketch never used this alternative anyway) — removed; and `UIRoot.Instance`'s registry candidacy (a `live_monobehaviour_state_static_accessor` interface contract for ADR-0012 to anchor against) was not yet proposed for `architecture.yaml` — carried forward to this ADR's Step 6 registry-update proposal, below.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-07

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (with Physics, Input, and UI Toolkit sub-aspects) |
| **Knowledge Risk** | LOW — `Physics.SphereCastNonAlloc`/`LayerMask`, the new Input System's `WasPressedThisFrame`/`IsPressed`, and UI Toolkit's `UIDocument`/`VisualElement`/`root.Q<T>()` are all confirmed stable, non-deprecated Unity 6.3 APIs (cross-checked against `docs/engine-reference/unity/modules/physics.md` and ADR-0002's own already-validated UI Toolkit usage). |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/unity/modules/physics.md`, `docs/engine-reference/unity/deprecated-apis.md`, `docs/architecture/adr-0002-ui-framework-ui-toolkit.md`, `docs/architecture/adr-0003-player-state-and-movement-lock.md`, `docs/architecture/adr-0004-interactableregistry-foundation-ownership.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None new. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0002 (UI Toolkit) — crosshair/Hold-fill rendering. ADR-0003 (Player State) — `EyeCamera`, `RequestMovementLock`/`ReleaseMovementLock`/`IsLocked` via `PlayerStateProvider.Current`. ADR-0004 (InteractableRegistry) — `Snapshot()`/`IInteractable`. |
| **Enables** | Any story implementing `Görev/Taşıma Döngüsü`'s `Instant` pickups or `Anı-Tetikleyici Etkileşim`'s `Hold` triggers (both implement `IInteractable` against this ADR's contract, already locked by the GDD, not renamed here). Establishes `UIRoot.Instance` (see Decision) — the lookup mechanism ADR-0002 explicitly deferred to "ADRs #9, #10, #12" — for the future Dialogue Callback Selection Timing ADR (#12) to reuse. |
| **Blocks** | Any story implementing `Etkileşim Sistemi` itself; any `Görev/Taşıma Döngüsü` or `Anı-Tetikleyici Etkileşim` story (both depend on this system's `IInteractable` contract already being implementable). |
| **Ordering Note** | None beyond the three Depends-On ADRs already being written (all Accepted-pending, per this project's status lifecycle). |

## Context

### Problem Statement

`etkilesim-sistemi.md` (Approved, though — like most GDDs in this project — still carrying a "Needs Revision" header artifact from an earlier review round, not a blocker per this session's established precedent) fully specifies a `SphereCast`-based focus-detection loop feeding a `Idle → Focused → Holding` state machine, a `hold_progress` linear formula, a default crosshair Hold-fill indicator (with an explicit per-object opt-out, `SuppressDefaultHoldFill`), and a `IInteractable` contract already implemented in principle by two Feature-layer systems (`Görev/Taşıma Döngüsü`'s `Instant` pickups, `Anı-Tetikleyici Etkileşim`'s `Hold` triggers). `architecture.md`'s own Module Ownership row (line 93) confirms this system consumes `InteractableRegistry`+`IPlayerState` (Foundation) and uses `Physics.SphereCastNonAlloc`/UI Toolkit — but, like every other Core/Feature system, has no concrete implementation mechanism yet.

The GDD's own two Open Questions are both in scope for this ADR to resolve, but differently: **Open Question #1** (`InteractableRegistry` ownership/file location) was already answered by ADR-0004 (Foundation ownership) — this ADR closes it by reference, not by re-deciding it, and flags that `etkilesim-sistemi.md`'s own text still needs a small update to point at the new ownership. **Open Question #2** (SphereCast occlusion/layer mask) was genuinely unresolved and is this ADR's own decision to make (see Decision).

### Constraints

- Must not deviate from `etkilesim-sistemi.md`'s already-Approved Core Rules, States and Transitions, and Edge Cases — this ADR formalizes, it does not redesign. In particular: the cancel-check-before-progress-update ordering (Edge Cases), the `HoldDuration<=0` guard (checked *before* division, not after), the closest-`hit.distance`-then-lowest-`InstanceID` tie-break, and the snapshot-based registry iteration (already established by ADR-0004's own `Snapshot()` API).
- Must satisfy `coding-standards.md`'s BLOCKING unit-test requirement for state-machine logic — the Focus/Holding transitions and `hold_progress` calculation must be testable without a live `Camera`/`Collider`/`UIDocument`.
- Must reuse `PlayerStateProvider.Current` (ADR-0003) for `EyeCamera`/lock calls, `InteractableRegistry.Snapshot()` (ADR-0004) for the candidate set, and UI Toolkit (ADR-0002) for rendering — no new persistence/UI mechanism.
- `IInteractable`'s member signatures (already locked by the GDD, quoted verbatim in Key Interfaces) must not be renamed — `Görev/Taşıma Döngüsü` and `Anı-Tetikleyici Etkileşim` both already design against these exact names.

### Requirements

- The Focus/Holding state machine's logic must live in a plain, Unity-decoupled C# class, separate from the `MonoBehaviour` that drives it — same testability split ADR-0003 already used for `PlayerStateProvider` vs. `FirstPersonController`.
- SphereCast occlusion (Open Question #2) must be resolved with a concrete `LayerMask` design, not left implicit.
- The crosshair/Hold-fill UI must establish `UIRoot.Instance`, the static-accessor lookup mechanism ADR-0002 explicitly steered "ADRs #9, #10, #12" toward, so this becomes the first, reusable precedent rather than three independently-invented answers.

## Decision

### Resolving Open Question #2: SphereCast layer mask includes both Interactable and Environment

**Confirmed by the user (`AskUserQuestion`, 2026-08-07)**: the focus-detection `SphereCast` checks a combined `LayerMask` — an `"Interactable"` layer (every `IInteractable`'s collider) **and** a general `"Environment"` layer (walls, doors, and any other solid, non-interactable geometry, including visually-transparent-but-physically-solid surfaces like glass). This means a solid physical obstacle between the player and an `IInteractable` correctly blocks focus on that interactable, even if the interactable's own collider would otherwise have been in range — matching Pillar 3 (Görev Gerçekliği)'s general disposition toward physically plausible reachability, and resolving the GDD's own "cam gibi görsel-olarak-şeffaf-ama-fiziksel-engelleyici" example directly: visual transparency is irrelevant, only collider presence matters, and a collider on the `Environment` layer blocks exactly like one on the `Interactable` layer would.

**Mechanism**: `Physics.SphereCastNonAlloc` against `interactableLayerMask | environmentLayerMask`, results sorted by `hit.distance` (ties broken by `collider.GetInstanceID()`, per the GDD's own existing tie-break rule — this rule already generalizes cleanly to "closest hit of any type," not just interactable-vs-interactable ties). The **closest** hit, regardless of layer, determines the outcome: if its `GameObject` implements `IInteractable` (a `TryGetComponent<IInteractable>()` lookup — see `ResolveFocusTarget()` below), that's the focus candidate (subject to `CanInteract`); if it does not, the cast is treated as fully occluded — no focus, even if a real `IInteractable` exists further along the same ray. (Corrected during TD-ADR review, 2026-08-07: an earlier draft additionally suggested checking `InteractableRegistry.Snapshot()` as a "cheaper" alternative — `Snapshot()` is `List`/array-backed, making `.Contains()` an O(n) linear scan, not obviously cheaper than one `TryGetComponent` call, and the code sketch never actually used it for this purpose. Removed as unsubstantiated.)

### Data model: pure C# state machine + thin `MonoBehaviour` driver

```csharp
public enum InteractionState { Idle, Focused, Holding }

// Plain C# class, no Unity types, no MonoBehaviour — testable via [Test]
// constructing it directly and feeding it synthetic IInteractable mocks,
// per coding-standards.md's BLOCKING unit-test rule for state-machine
// logic. Same testability split ADR-0003 used for PlayerStateProvider
// vs. FirstPersonController.
public sealed class InteractionStateMachine {
    public InteractionState CurrentState { get; private set; } = InteractionState.Idle;
    public IInteractable CurrentTarget { get; private set; }
    public float CurrentHoldProgress { get; private set; }

    // Fired on the exact Focused→Holding and Holding→(Focused|Idle)
    // transitions — the MonoBehaviour driver subscribes to these for the
    // actual RequestMovementLock/ReleaseMovementLock side effects (kept
    // out of this class entirely, so it never touches PlayerStateProvider
    // or any other Unity/Foundation type).
    public event Action OnHoldStarted;
    public event Action OnHoldEnded;

    private float _elapsedHoldTime;

    // sphereCastTarget: the closest IInteractable this frame's SphereCast
    // resolved to, or null if occluded/nothing hit (see Decision's layer-
    // mask resolution — computed by the MonoBehaviour, passed in here).
    // movementLockAvailable: PlayerStateProvider.Current.IsLocked negated,
    // read by the MonoBehaviour, passed in — keeps this class Foundation-
    // decoupled while still able to make the correct Focused→Holding gate
    // decision (GDD Edge Cases, "kilit BAŞKA bir sistem tarafından...").
    // currentTargetStillRegistered: whether CurrentTarget is still present
    // in this frame's InteractableRegistry snapshot (i.e. hasn't been
    // Destroy()'d/SetActive(false)'d) — computed by the MonoBehaviour,
    // NOT derivable inside this pure class. Deliberately a separate input
    // from sphereCastTarget: GDD Edge Cases distinguishes "target destroyed
    // mid-Hold" (→Idle, this parameter false) from "target still valid but
    // SphereCast moved off it" (→Focused, this parameter true even though
    // sphereCastTarget != CurrentTarget that same frame). A naive `target
    // == null` check inside this class can't tell these apart reliably
    // even if it could see Unity types, because comparing a destroyed
    // UnityEngine.Object through a plain IInteractable-typed reference
    // does NOT get Unity's overridden "fake null" equality unless the
    // caller explicitly casts to UnityEngine.Object first — one more
    // reason this check belongs in the MonoBehaviour, not here.
    public void Tick(IInteractable sphereCastTarget, bool currentTargetStillRegistered,
                      bool interactPressed, bool interactHeld,
                      bool movementLockAvailable, float deltaTime) {
        switch (CurrentState) {
            case InteractionState.Idle:
                TryEnterFocused(sphereCastTarget);
                break;

            case InteractionState.Focused:
                // Corrected during ADR-0014's unity-specialist review
                // (2026-08-08): re-poll CanInteract for the ALREADY-
                // focused target. Without this, a target whose
                // CanInteract goes false while focused (a memory
                // trigger committing mid-Hold-completion, a session
                // ending) stayed focused with its prompt pinned — and
                // since interactHeld is still true at a Hold's
                // completion instant, the branch below re-entered
                // Holding on the now-inert target indefinitely (a real
                // phantom re-Hold loop, movement lock churned every
                // cycle). This is a fidelity RESTORATION, not a
                // redesign: etkilesim-sistemi.md's own Focused row
                // already lists "hedef devre dışı kalır" as a
                // Focused→Idle exit this branch under-implemented.
                // Path mirrors the target-changed exit exactly
                // (deterministic, same-frame re-entry attempt; for a
                // CanInteract=false target TryEnterFocused refuses,
                // landing in Idle). ADR-0013's slots-full CarryItemPickup
                // is unaffected by design — its CanInteract stays true.
                if (sphereCastTarget != CurrentTarget || !CurrentTarget.CanInteract) {
                    CurrentTarget?.OnFocusExit();
                    CurrentTarget = null;
                    CurrentState = InteractionState.Idle;
                    TryEnterFocused(sphereCastTarget);
                    break;
                }
                if (CurrentTarget.Type == InteractionType.Instant && interactPressed) {
                    CurrentTarget.OnInteract();  // stays Focused, per GDD States and Transitions
                } else if (CurrentTarget.Type == InteractionType.Hold && interactHeld) {
                    if (!movementLockAvailable) {
                        CurrentTarget.OnHoldBlocked();  // stays Focused — no state change (GDD Edge Cases)
                    } else {
                        _elapsedHoldTime = 0f;
                        CurrentState = InteractionState.Holding;
                        OnHoldStarted?.Invoke();
                    }
                }
                break;

            case InteractionState.Holding:
                // Cancel check FIRST, always — GDD Edge Cases: "iptal
                // kontrolü her zaman progress güncellemesinden önce
                // işlenir." Target-lost (registry dropped it, e.g.
                // Destroy()) is distinguished from target-still-valid-
                // but-SphereCast-moved-away, per GDD Edge Cases' Idle-
                // vs-Focused distinction on cancel — using the caller-
                // supplied currentTargetStillRegistered, not a null check.
                if (sphereCastTarget != CurrentTarget || !interactHeld) {
                    var cancelledTarget = CurrentTarget;
                    cancelledTarget.OnHoldCancelled();
                    OnHoldEnded?.Invoke();
                    CurrentState = currentTargetStillRegistered ? InteractionState.Focused : InteractionState.Idle;
                    CurrentTarget = currentTargetStillRegistered ? cancelledTarget : null;
                    CurrentHoldProgress = 0f;
                    break;
                }
                _elapsedHoldTime += deltaTime;
                if (CurrentTarget.HoldDuration <= 0f) {
                    // GDD Edge Cases: checked BEFORE division, to avoid a
                    // divide-by-zero — completes immediately, first frame.
                    CompleteHold();
                    break;
                }
                CurrentHoldProgress = Mathf.Clamp01(_elapsedHoldTime / CurrentTarget.HoldDuration);
                CurrentTarget.OnHoldProgress(CurrentHoldProgress);
                if (CurrentHoldProgress >= 1f) CompleteHold();
                break;
        }
    }

    private void TryEnterFocused(IInteractable target) {
        if (target == null || !target.CanInteract) return;
        CurrentTarget = target;
        CurrentState = InteractionState.Focused;
        CurrentTarget.OnFocusEnter();
    }

    private void CompleteHold() {
        CurrentTarget.OnHoldComplete();
        OnHoldEnded?.Invoke();
        CurrentState = InteractionState.Focused;
        CurrentHoldProgress = 0f;
    }
}
```

```csharp
// using System.Linq; — required for Snapshot().Contains() below;
// using UnityEngine.InputSystem; elided along with UnityEngine/UI Toolkit
// usings throughout this ADR's sketches.
//
// Lives on the persistent "Player" GameObject (ADR-0003), alongside
// FirstPersonController/PlayerStateProvider — corrected during unity-
// specialist validation (2026-08-07): an earlier draft never stated this
// explicitly, even though the Risks section's "same lifetime" argument
// for the Awake()-subscribed lambdas below depends on it (this object is
// never destroyed/recreated across a scene swap, same guarantee
// PlayerStateProvider itself relies on).
public sealed class InteractionController : MonoBehaviour {
    [SerializeField] private LayerMask _interactableLayerMask, _environmentLayerMask;
    [SerializeField] private float _sphereCastRadius = 0.05f, _sphereCastRange = 2f;
    [SerializeField] private InputActionReference _interactAction;  // "Interact" (Button), illustrative —
                                                                      // pending a future Input System ADR

    private readonly InteractionStateMachine _stateMachine = new();
    private readonly RaycastHit[] _hitsBuffer = new RaycastHit[8];  // SphereCastNonAlloc, no per-frame GC

    // Corrected during TD-ADR review (2026-08-07): an earlier draft left
    // UpdateCrosshairAndHoldFill() as an empty stub while its surrounding
    // prose asserted specific implementation facts (cached lookup,
    // etkilesim-* prefix, change-only toggling) nowhere reflected in code
    // — a real gap, since this ADR's own stated purpose is to be the
    // concrete precedent the future Dialogue ADR (#12) copies. Filled in
    // below, following ADR-0002's own worked example shape (OnEnable-time
    // cached, null-checked Q<VisualElement>() lookups) exactly.
    private VisualElement _crosshair, _holdFillRing;
    private InteractionState _lastRenderedState = InteractionState.Idle;

    private void OnEnable() {
        var root = UIRoot.Instance.Root;
        _crosshair = root.Q<VisualElement>("crosshair");
        _holdFillRing = root.Q<VisualElement>("hold-fill-ring");
        // Defensive, per ADR-0002's own shared-UXML mitigation — a
        // malformed edit elsewhere in the shared document degrades to
        // "my element is missing" (logged once), not an unhandled
        // exception on every subsequent frame.
        if (_crosshair == null) Debug.LogError("UIRoot's UXML is missing #crosshair.", this);
        if (_holdFillRing == null) Debug.LogError("UIRoot's UXML is missing #hold-fill-ring.", this);
    }

    // Corrected during unity-specialist validation (2026-08-07): an
    // earlier draft used a lambda/method-group directly, which does not
    // satisfy Array.Sort's (array, index, length, IComparer<T>) overload
    // (the range-limited form only accepts IComparer<T>, not
    // Comparison<T> — that delegate form only exists on the no-range
    // overload). Built explicitly as IComparer<RaycastHit> instead.
    private static readonly IComparer<RaycastHit> ClosestThenLowestInstanceIdComparer =
        Comparer<RaycastHit>.Create((a, b) => {
            int byDistance = a.distance.CompareTo(b.distance);
            return byDistance != 0 ? byDistance : a.collider.GetInstanceID().CompareTo(b.collider.GetInstanceID());
        });

    private void Awake() {
        _stateMachine.OnHoldStarted += () => PlayerStateProvider.Current.RequestMovementLock(this, MovementLockScope.MoveOnly);
        _stateMachine.OnHoldEnded += () => PlayerStateProvider.Current.ReleaseMovementLock(this);
    }

    private void Update() {
        var eyeCamera = PlayerStateProvider.Current.EyeCamera;
        var target = ResolveFocusTarget(eyeCamera);
        // Corrected during TD-ADR review (2026-08-07): an earlier draft
        // added "|| CurrentState == Holding" here, reasoning it prevented
        // the system from self-blocking its own in-progress Hold. That
        // clause was dead code — Tick()'s Holding branch never reads
        // movementLockAvailable at all (only the Focused branch's entry
        // gate does), and the switch's cases are mutually exclusive, so
        // no self-block scenario can occur regardless of this value once
        // CurrentState is already Holding. Simplified to the plain check;
        // see Risks for the real (structural, not defensive) reasoning.
        bool movementLockAvailable = !PlayerStateProvider.Current.IsLocked;
        // Computed here, not inside InteractionStateMachine — see that
        // class's own Tick() doc comment for why a plain null/reference
        // check can't reliably tell "destroyed" from "still valid, just
        // not this frame's SphereCast hit" through an interface-typed
        // reference — comparing an interface-typed variable to null does
        // NOT get Unity's overridden "fake null" behavior the way a
        // UnityEngine.Object-typed one would, so this reads the current
        // frame's registry snapshot instead of trusting a null check.
        bool currentTargetStillRegistered = _stateMachine.CurrentTarget == null
            || InteractableRegistry.Snapshot().Contains(_stateMachine.CurrentTarget);
        _stateMachine.Tick(target, currentTargetStillRegistered,
                            _interactAction.action.WasPressedThisFrame(), _interactAction.action.IsPressed(),
                            movementLockAvailable, Time.deltaTime);
        UpdateCrosshairAndHoldFill();
    }

    private IInteractable ResolveFocusTarget(Transform eyeCamera) {
        int count = Physics.SphereCastNonAlloc(eyeCamera.position, _sphereCastRadius, eyeCamera.forward,
                                                 _hitsBuffer, _sphereCastRange,
                                                 _interactableLayerMask | _environmentLayerMask);
        if (count == 0) return null;
        System.Array.Sort(_hitsBuffer, 0, count, ClosestThenLowestInstanceIdComparer);
        var closest = _hitsBuffer[0];
        // Closest hit determines the outcome regardless of layer — an
        // Environment hit occludes even a further-away real IInteractable
        // (Decision, "Resolving Open Question #2"). NOTE (unity-specialist
        // validation, 2026-08-07): TryGetComponent only checks the exact
        // GameObject the collider is on, not parents/children — an
        // IInteractable script and its collider MUST live on the same
        // GameObject (a control-manifest rule to add at /create-control-manifest
        // time, not enforced by this code).
        return closest.collider.TryGetComponent<IInteractable>(out var interactable) ? interactable : null;
    }

    private void UpdateCrosshairAndHoldFill() {
        if (_crosshair == null) return;  // UXML gap already logged in OnEnable(); degrade silently per-frame

        // Toggle Idle/Focused USS class only on state CHANGE (GDD Core
        // Rules: "her karede değil") — etkilesim- prefix per ADR-0002's
        // USS class-name-collision mitigation.
        if (_stateMachine.CurrentState != _lastRenderedState) {
            bool focused = _stateMachine.CurrentState != InteractionState.Idle;
            _crosshair.EnableInClassList("etkilesim-crosshair--focused", focused);
            _lastRenderedState = _stateMachine.CurrentState;
        }

        bool showHoldFill = _stateMachine.CurrentState == InteractionState.Holding
                             && _stateMachine.CurrentTarget?.SuppressDefaultHoldFill == false;  // AC14/AC14a
        _holdFillRing.style.display = showHoldFill ? DisplayStyle.Flex : DisplayStyle.None;
        if (showHoldFill) {
            _holdFillRing.style.width = Length.Percent(_stateMachine.CurrentHoldProgress * 100f);
        }
    }
}
```

### Crosshair and default Hold-fill: establishing `UIRoot.Instance`

ADR-0002 explicitly deferred "the exact lookup mechanism" for the shared `UIDocument` to "ADRs #9, #10, #12," steering toward "a small `UIRoot.Instance` static accessor" rather than `GameObject.Find` or three independently-invented answers. ADR-0009 (Audio) didn't need it (its stinger-caption UI was explicitly out of scope, deferred to a future dialogue/UI pass). **This ADR is therefore the first to actually need it, and establishes it**:

```csharp
public sealed class UIRoot : MonoBehaviour {
    // Duplicate-instance guard — same shape as every other persistent-
    // scene singleton in this project (PlayerStateProvider ADR-0003,
    // SceneTransitionManager ADR-0008, AdaptifSesController ADR-0009).
    public static UIRoot Instance { get; private set; }
    [SerializeField] private UIDocument _uiDocument;
    public VisualElement Root => _uiDocument.rootVisualElement;

    private void Awake() {
        if (Instance != null) { Debug.LogError("Duplicate UIRoot.", this); Destroy(gameObject); return; }
        Instance = this;
    }
}
```

Living on the persistent "UI" scene's root `GameObject` (ADR-0002), alongside the `UIDocument` component that already exists there. `InteractionController` queries and caches `UIRoot.Instance.Root.Q<VisualElement>("crosshair")`/`"hold-fill-ring"` once, in `OnEnable()` (matching ADR-0002's own worked-example timing exactly, not an unexplained "first access"), with a defensive null-check per element (ADR-0002's shared-UXML mitigation — a malformed edit elsewhere degrades to one logged error, not an exception every frame). `UpdateCrosshairAndHoldFill()` (Decision code block above, now filled in rather than left as a stub — corrected during TD-ADR review, 2026-08-07) applies the `etkilesim-*` USS class-name prefix, toggles the crosshair's Focused/Idle class only on state *change* (GDD Core Rules — "her karede değil"), and drives the fill ring's width directly from `CurrentHoldProgress` — hidden entirely if `CurrentTarget.SuppressDefaultHoldFill == true` (GDD AC14a). `PromptText` display is a small additional element this same method would also drive (`CurrentTarget.PromptText`, shown/hidden with the crosshair's Focused state) — omitted from the code block for brevity, not a different mechanism.

## Alternatives Considered

### Alternative 1: SphereCast layer mask includes only `Interactable` (Open Question #2, rejected option)
- **Description**: SphereCast checks only the `Interactable` layer, ignoring general solid geometry entirely.
- **Pros**: Simpler — one layer, no closest-hit-type disambiguation logic; never accidentally blocks a legitimate interactable behind a thin decorative prop mistakenly placed on the `Environment` layer.
- **Cons**: Physically implausible — a player could focus and interact with an object through a solid closed door or wall, directly undermining Pillar 3 (Görev Gerçekliği)'s reachability expectations; the GDD's own Open Question explicitly raised the glass-occlusion case as a concern, implying the GDD authors expected *some* physical blocking to exist, not none.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-07): realism/reachability was judged more valuable than the marginal simplicity gain, and the risk (a mis-layered prop accidentally blocking something) is a content-authoring bug class, not an architecture one — same category of risk this project already accepts for e.g. `AmbientZoneVolume` trigger placement.

### Alternative 2: State machine and `MonoBehaviour` combined into one class
- **Description**: Skip the `InteractionStateMachine`/`InteractionController` split — implement the Focus/Holding transitions directly inside the `MonoBehaviour`'s `Update()`.
- **Pros**: One fewer type; no event-based hand-off between the pure logic and the Unity-touching side effects (`RequestMovementLock` calls could just happen inline at the transition point).
- **Cons**: Directly violates `coding-standards.md`'s BLOCKING unit-test requirement for state-machine logic ("dependency injection over singletons") — a `MonoBehaviour`-embedded state machine can't be exercised by a `[Test]` without a live `Camera`/`Collider`/scene, the same problem ADR-0003 already solved once for `PlayerStateProvider` vs. `FirstPersonController`.
- **Rejection Reason**: This project has a consistent, already-established precedent (ADR-0003) for exactly this situation; deviating here would be inconsistent without a reason specific to this system that ADR-0003's didn't already share.

### Alternative 3: `GameObject.Find("UIRoot")` instead of `UIRoot.Instance` (ADR-0002's own illustrative shorthand)
- **Description**: Keep ADR-0002's worked-example lookup (`GameObject.Find("UIRoot").GetComponent<UIDocument>()`) as the actual mechanism, rather than introducing a new static accessor.
- **Pros**: Zero new code — ADR-0002's sketch already shows this working.
- **Cons**: ADR-0002 itself explicitly labels this "illustrative shorthand," not a real recommendation, and separately steers toward a static accessor "rather than `GameObject.Find` or three independently-invented lookup strategies across ADR-9/10/12." `GameObject.Find` is also a documented Unity performance anti-pattern for anything called more than rarely (string-based hierarchy search) — acceptable for a one-time illustrative snippet, not for a pattern three ADRs are expected to reuse.
- **Rejection Reason**: ADR-0002's own explicit steering; implementing the recommended pattern now (rather than `GameObject.Find` now and a "fix later" migration) avoids the exact "three answers to one question" inconsistency ADR-0002 was written to prevent.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #10.
- Resolves `etkilesim-sistemi.md`'s Open Question #2 (SphereCast occlusion) with a concrete, physically-plausible mechanism, and closes Open Question #1 by reference to ADR-0004 rather than re-deciding it.
- Establishes `UIRoot.Instance` — the reusable lookup mechanism ADR-0002 explicitly asked a future ADR to settle — as a clean, single precedent for the future Dialogue Callback Selection Timing ADR (#12) to reuse, avoiding the "three independently-invented answers" risk ADR-0002 flagged.
- The state-machine/`MonoBehaviour` split directly satisfies `coding-standards.md`'s BLOCKING unit-test rule with a pattern this project has already validated once (ADR-0003), not a novel approach needing its own scrutiny.

### Negative
- `etkilesim-sistemi.md`'s own text still says `InteractableRegistry` ownership is an open question — this ADR does not edit the GDD itself (out of scope for an ADR to rewrite GDD prose), so a small follow-up GDD edit is still owed, tracked here rather than silently assumed done.
- The closest-hit-of-any-layer occlusion rule (Decision) means a single mis-layered decorative prop (accidentally placed on `Environment` instead of a genuinely non-colliding layer) could silently block a real interactable — a content-authoring risk this ADR accepts rather than solves architecturally (see Alternative 1's rejection reasoning).
- `UIRoot` is a second persistent-scene singleton this ADR introduces alongside `PlayerStateProvider`, but living in the "UI" scene rather than the "Player" or "Foundation" scenes — a fourth distinct persistent-scene-singleton location in this project (UI: `UIRoot`, this ADR; Player: `PlayerStateProvider`, ADR-0003; Foundation: `SceneTransitionManager`+`AdaptifSesController`, ADR-0008/0009) — consistent with each scene hosting the singleton(s) that conceptually belong to it, not a new inconsistency.

### Risks
- **Risk**: `Physics.SphereCastNonAlloc`'s fixed-size `_hitsBuffer` (8 elements) could silently drop legitimate hits if more than 8 colliders (Interactable + Environment combined) are ever within the cast's path simultaneously in a single dense area. **Mitigation**: 8 is generous for this project's modular, sparsely-populated interior spaces (per `technical-preferences.md`'s draw-call budget context implying moderate geometric density); revisit only if a specific dense area's content later needs more, not a concern to solve preemptively.
- **Risk (corrected, TD-ADR review, 2026-08-07)**: an earlier draft added a defensive `|| CurrentState == Holding` clause to `movementLockAvailable`, reasoning it prevented the system from self-blocking its own in-progress Hold the moment `RequestMovementLock` makes `IsLocked` become `true`. This was a false narrative, not a real fix — `Tick()`'s `Holding` branch never reads `movementLockAvailable` at all (only `Focused`'s entry gate does), and `Tick()`'s `switch (CurrentState)` cases are mutually exclusive, so the self-block scenario described cannot occur regardless of this value once `CurrentState` is already `Holding` — the protection is structural (the switch shape itself), not something this flag needed to defend. **Mitigation**: none needed — `movementLockAvailable` is now the plain `!IsLocked` check (Decision, above); a `[Test]` should still assert a Hold interaction, once started, is never spuriously cancelled, but as a confirmation of the switch's structural guarantee, not as coverage for a bug that was never real.
- **Risk**: `InteractionController` calling `RequestMovementLock(this, MovementLockScope.MoveOnly)`/`ReleaseMovementLock(this)` from lambda closures subscribed in `Awake()` means the event subscriptions themselves live for the `MonoBehaviour`'s full lifetime, never unsubscribed — not a leak (the `InteractionStateMachine` instance is itself a field of this same `MonoBehaviour`, same lifetime, and — corrected during unity-specialist validation, 2026-08-07 — this `MonoBehaviour` itself lives on the persistent Player `GameObject`, ADR-0003, never destroyed/recreated across a scene swap, per the explicit placement statement added to the Decision code sketch above), but worth noting this pattern shouldn't be copied for a case where the event source outlives the subscriber.
- **Risk (unity-specialist validation, 2026-08-07 — Validation Criteria already claimed this coverage, but the Risk itself was missing from an earlier draft)**: `UIRoot.Instance` is set exclusively in `Awake()`, with no separate reset hook — structurally identical to `PlayerStateProvider.Current` (ADR-0003), `SceneTransitionManager._instance` (ADR-0008), and `AdaptifSesController.Instance` (ADR-0009), all of which document the same hazard: Unity's independent "Reload Scene" Enter Play Mode Setting can suppress `Awake()` re-execution on a surviving persistent-scene object across a Play-mode Stop→Play boundary, leaving `Instance` stale (possibly pointing at a destroyed object). **Mitigation**: same as those three precedents — a correctness risk only under the non-default "Reload Scene: Off" Editor setting (real player builds always fully reload); the boot sequence must guarantee the UI scene is freshly loaded each genuine session, covered by the `[UnityTest]` already listed in Validation Criteria below.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `etkilesim-sistemi.md` | `SphereCast` focus detection, 0.05m radius, 2.0m range, `IInteractable`+`CanInteract` gate | `InteractionController.ResolveFocusTarget`, serialized radius/range fields |
| `etkilesim-sistemi.md` | Open Question #2 — SphereCast occlusion/layer mask | Resolved: combined `Interactable`+`Environment` `LayerMask`, closest-hit-of-any-type wins (see Decision) |
| `etkilesim-sistemi.md` | Idle/Focused/Holding state machine, cancel-before-progress ordering, `HoldDuration<=0` guard, `RequestMovementLock(MoveOnly)`/`ReleaseMovementLock` on Hold entry/exit | `InteractionStateMachine.Tick`, exactly as Core Rules/Edge Cases/States and Transitions specify |
| `etkilesim-sistemi.md` | `IsLocked` pre-check before entering Holding — `OnHoldBlocked()` if another system holds the lock | `movementLockAvailable` parameter, computed by `InteractionController` from `PlayerStateProvider.Current.IsLocked` |
| `etkilesim-sistemi.md` | Default crosshair Hold-fill indicator, driven by this system's own `t`, with `SuppressDefaultHoldFill` opt-out (AC14/AC14a) | `UpdateCrosshairAndHoldFill()`, reads `CurrentHoldProgress`/`CurrentTarget.SuppressDefaultHoldFill` |
| `etkilesim-sistemi.md` | Registry iterated via snapshot, not live collection (Edge Cases) | Reuses `InteractableRegistry.Snapshot()` as-is (ADR-0004) — no new snapshot mechanism |
| `etkilesim-sistemi.md` Open Questions #1 | `InteractableRegistry` ownership | Already resolved by ADR-0004 (Foundation ownership); this ADR closes it by reference, flags the GDD's own text as still needing a small update (Consequences → Negative) |
| `architecture.md` | Module Ownership row (line 93) — `SphereCast`, Hold state machine, crosshair/Hold-fill, `InteractableRegistry`+`IPlayerState`+`RequestMovementLock` consumption | Implemented as designed |

## Performance Implications
- **CPU**: One `SphereCastNonAlloc` call per frame (no per-frame allocation, fixed 8-element buffer), one small state-machine `Tick()` call, one or two USS class toggles only on state *change* — negligible against the 16.6ms frame budget.
- **Memory**: Negligible — one `InteractionStateMachine` instance, one 8-`RaycastHit` buffer, cached `VisualElement` references (queried once).
- **Load Time**: N/A — no asset loading.
- **Network**: N/A — no networking in this project.

## Migration Plan
No existing code to migrate (`Etkileşim Sistemi` is not yet implemented).

## Validation Criteria
- A `[Test]` constructs a fresh `InteractionStateMachine` and a mock `IInteractable`, drives `Tick()` through a full `Idle→Focused→Holding→Focused` cycle, and asserts `OnFocusEnter`/`OnHoldProgress`/`OnHoldComplete` fire in the GDD's exact sequence (AC4) — no `Camera`/`Collider`/`MonoBehaviour` involved.
- `elapsed={-1,0,D/2,D,1.5D}` produces `t={0,0,0.5,1,1}` (AC5) — a pure `[Test]` against `InteractionStateMachine.Tick`'s progress calculation.
- A Hold interaction, once started, is never spuriously cancelled by its own just-acquired movement lock — confirms the `Tick()` switch's structural guarantee (Consequences → Risks, corrected 2026-08-07) rather than any longer defensive check.
- `HoldDuration<=0` produces no `NaN`/divide-by-zero and calls `OnHoldComplete()` on the first `Tick()` after entering Holding (AC7).
- The combined-layer-mask occlusion rule: a solid `Environment`-layer collider between the camera and a real `IInteractable` prevents focus, even though the interactable's own collider is within range — a `[UnityTest]` with a physical scene setup, since this is `SphereCastNonAlloc`/layer behavior, not pure state-machine logic.
- `UIRoot.Instance` has the same Reload-Scene staleness coverage as every other persistent-scene singleton in this project (ADR-0003/0008/0009 precedent) — a `[UnityTest]` with Reload Scene disabled, two simulated sessions, confirming `Instance` is never stale.

## Related Decisions
- ADR-0002 (UI Framework) — source of the shared `UIDocument`/`"crosshair"`/`"hold-fill-ring"` elements this ADR renders into; this ADR establishes `UIRoot.Instance`, the lookup mechanism ADR-0002 deferred.
- ADR-0003 (Player State) — source of `PlayerStateProvider.Current`/`EyeCamera`/`RequestMovementLock`/`ReleaseMovementLock`/`IsLocked`; source of the state-machine/`MonoBehaviour` testability split this ADR reuses.
- ADR-0004 (InteractableRegistry) — source of `IInteractable`/`InteractableRegistry.Snapshot()`; this ADR closes `etkilesim-sistemi.md`'s Open Question #1 by reference to it.
- Future "Dialogue Callback Selection Timing" ADR (Required ADR #12) — expected to reuse `UIRoot.Instance` for the dialogue subtitle element, per ADR-0002's own steering.
