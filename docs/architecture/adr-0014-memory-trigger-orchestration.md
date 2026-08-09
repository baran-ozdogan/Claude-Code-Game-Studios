# ADR-0014: Memory Trigger Orchestration

> **Unity Specialist Validation**: BLOCKING (2 findings, both resolved by user decision) 2026-08-08 — (1) The draft's "post-complete lingering focus is cosmetic" claim was **false**: traced against ADR-0010's actual `Tick()`, a player who simply keeps holding the button on a just-committed trigger re-enters `Holding` indefinitely (the `Focused` branch never re-polls `CanInteract`; `interactHeld` is still true at the completion instant) — movement lock churned every cycle, prompt pinned, the GDD's "tam bir kez çağrılır" AC violated. Resolved: a companion revision to ADR-0010 adds a `CanInteract` re-poll at the `Focused` branch's top (itself a fidelity *restoration* — `etkilesim-sistemi.md`'s own Focused row already lists "hedef devre dışı kalır" as an exit ADR-0010 under-implemented). (2) The draft's "transitive `FiredTriggerIds` write" model was wrong: ADR-0006's body defines no Fired handler branch, and the quick spec locks a direct-write model in three places — the transitive reading would either never write Fired (Committed-restore permanently broken) or pollute it with the `Automatic` ambient zone's id, tripping saturation one trigger early. Resolved: `GeceOturumDurumu.InternalInstance.AddFiredTrigger(shiftId)`, `SetRoundState`'s twin, called in `OnHoldComplete` alongside `TriggerShift`. Plus 4 MINOR (the `IsikVolumeDurumSistemi.Instance.TriggerShift` call shape marked as consuming ADR-0005's deferred lookup layer, not defining it; three `FindAssets` caveats — sub-asset convention, `CreateInstance`-only test fixtures, night-blind count formula; `ShiftConfig`'s ScriptableObject-asset nature cited since ADR-0005 never declares the type; ADR-0006's stale "never any other path" data-model comment queued for the same follow-up edit).
> **Technical Director Review (TD-ADR)**: CONCERNS (revised) 2026-08-08 — 3 mandatory findings, fixed: stale pre-fix claims ("transitive... zero new API surface", "honored transitively", "cosmetic note") survived the first fix pass in Consequences/Related Decisions — the exact stale-claim class this project's reviews hunt. 5 minor findings, fixed: the quick spec's own `Awake()` Dependencies line added to the undercounted sync list; the `scene_object_state_restore_timing` registry entry's api wording revised to cover both restore variants (its current letter mandates `SetActive(false)`, which this ADR's stay-visible variant deliberately omits); a confusing cross-reference to the unrelated layer-mask registry entry corrected; the GDD's AC6 `RevertShift` CI lint explicitly noted as a separate unowned mechanism; and a **reachability hole** closed — the count check guarantees the number of defs, not that each is placed in a scene, so an authored-but-never-placed def would silently kill the saturation ending; the scene-scan gains a sixth check (def with no matching-`shiftId` scene object → build error). Verified sound explicitly: the registry write_access revision is a legitimate correction (the quick spec locks direct-write in three independent places; the registry line over-generalized), the ADR-0010 revise-in-place call (consumer sweep clean — ADR-0013's slots-full case unaffected by design), AC1's exactly-once now genuinely holding, and the count-resolution's coherence with ADR-0006's deferral and ADR-0013's night-begin ordering constraint.

## Status
Accepted (2026-08-09 — status flip per `/architecture-review` 2026-08-09 follow-up, user-approved; unity-specialist validation and TD-ADR review were already complete in-file)

## Date
2026-08-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.0f1) |
| **Domain** | Core (thin gameplay orchestration, ScriptableObject data, editor validation) |
| **Knowledge Risk** | LOW — `ScriptableObject`, the `IInteractable` Hold contract (ADR-0004/0010), `TriggerShift` (ADR-0005), the ADR-0013 restore pattern, and `IPreprocessBuildWithReport`/`AssetDatabase.FindAssets` (the GDD's own already-specified mechanism) are all validated project patterns. No new engine mechanism introduced. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/architecture/adr-0004-interactableregistry-foundation-ownership.md`, `docs/architecture/adr-0005-isik-volume-rendering-architecture.md`, `docs/architecture/adr-0006-session-state-service-and-round-counter-ownership.md`, `docs/architecture/adr-0010-interaction-state-machine.md`, `docs/architecture/adr-0013-carry-loop-and-round-state.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | The Committed-restore path (`OnEnable`-top query, object stays visible but never registers) should be exercised in Play mode across a scene reload after a fired trigger — including under "Reload Scene: Off" (the exact scenario the ADR-0013 restore pattern exists for). |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0004 (InteractableRegistry) — registration semantics. ADR-0005 (Işık/Volume) — `TriggerShift(shiftId, shiftConfig)`, the `Persistent` skip-Shifting-Out rule. ADR-0006 (Session State) — `FiredTriggerIds` (written via the new `AddFiredTrigger` internal path this ADR's follow-up edit adds), plus the `SetTotalConfiguredTriggerCountForNight` follow-up edit. ADR-0010 (Interaction) — the Hold contract, `SuppressDefaultHoldFill`. ADR-0013 — the `scene_object_state_restore_timing` pattern (registry-MANDATED for this ADR). |
| **Enables** | ADR-0015 (End-Condition Orchestration) — its saturation condition reads `SettledTriggerIds.Count == TotalConfiguredTriggerCountForNight`, whose write path this ADR defines. |
| **Blocks** | Any story implementing `Anı-Tetikleyici Etkileşim`; ADR-0015's saturation stories. |
| **Ordering Note** | The `TotalConfiguredTriggerCountForNight` write call is owned by the night-begin orchestrator whose caller ADR-0015 owes (same owed caller as ADR-0013's `StartNight()` — one orchestration point, two setup calls). |

## Context

### Problem Statement

`ani-tetikleyici-etkilesim.md` (Approved) specifies the player-initiated memory-shift system: fixed hotel objects implement `IInteractable.Hold`; `OnHoldComplete()` calls `Işık/Volume`'s `TriggerShift(shiftId, shiftConfig)` exactly once; the object becomes permanently `Committed` (session-persistent via `Gece/Oturum Durumu`'s `FiredTriggerIds`); `Persistent=true` is mandatory (the "artık geri alamam" guarantee rests entirely on it); `RevertShift` is never called; and a four-check edit-time validation (`IPreprocessBuildWithReport`) is the primary — in one case the *only* — defense line. The GDD is a deliberately thin orchestration layer: every hard mechanism (Hold detection, shift rendering, session persistence, stinger audio, clue marking) lives in an already-ADR'd system.

This ADR resolves: **(1)** the concrete component/data split; **(2)** the Committed-restore mechanism — the GDD says `Awake()`-time, but ADR-0013's `scene_object_state_restore_timing` registry entry MANDATES the `OnEnable`-top shape for exactly this restore (QQ-07's Reload-Scene hazard), with a GDD sync edit; **(3)** the `FiredTriggerIds` write path — reconciling the GDD's "eklettirir" (has it added) wording, ADR-0006's registry framing, and the quick spec's own locked direct-write model; **(4)** `TotalConfiguredTriggerCountForNight`'s ownership and write mechanism, explicitly deferred to this ADR by ADR-0006.

### Constraints

- Must not deviate from `ani-tetikleyici-etkilesim.md`'s Core Rules/States/Edge Cases — this ADR formalizes, it does not redesign. In particular: `OnHoldCancelled` is a total no-op; `OnHoldComplete` calls `TriggerShift` with no extra guard (the no-op-if-active guarantee rests on the `Persistent=true` invariant, edit-time-locked); the fired flag is NEVER written to the `MemoryTriggerDef` asset; `SuppressDefaultHoldFill` returns `true` (zero-feedback Hold, the GDD's Player Fantasy correction); `HoldDuration` is content-assigned from the 0.6–1.5s sub-range.
- Must adopt ADR-0013's `scene_object_state_restore_timing` pattern (registry-MANDATED): restore query at the top of `OnEnable()`, before `Register` — the GDD's `Awake()` wording gets a sync edit, same as `gorev-tasima-dongusu.md`'s did.
- Must write `FiredTriggerIds` via a **direct, internal write path** — `GeceOturumDurumu.InternalInstance.AddFiredTrigger(shiftId)` (this ADR's follow-up edit to ADR-0006), called in `OnHoldComplete()` in the same step as `TriggerShift`. **Corrected during unity-specialist review (2026-08-08)**: an earlier draft claimed the write happens *transitively* (TriggerShift → `OnShiftStateChanged` → a `GeceOturumDurumu` handler), citing the registry's write_access line — but ADR-0006's own body defines no Fired handler branch (its subscription covers `PersistentShiftIds` at Shifting-In and `SettledTriggerIds` at Held only), and the quick spec (`gece-oturum-durumu-2026-08-02.md`) locks the direct-write model in three separate places (`OnHoldComplete()` writes; `OnTriggerFired` fires "the same frame it is added by `OnHoldComplete()`"). The transitive reading would either never write Fired at all (Committed-restore permanently broken, saturation never trips) or — if a handler blindly added every Shifting-In id — pollute Fired with the night's mandatory `Automatic` ambient zone's id, inflating `SettledCount` and tripping saturation early. The direct write keeps Fired as precisely the Hold-completed-ids filter that gates Settled. User-confirmed; the registry's write_access line is revised in the same Step-6 update.
- Must reuse the shared `IPreprocessBuildWithReport` editor utility (the GDD's own named mechanism; ADR-0012/ADR-0013 already contribute checks) — not a new independent implementation.
- `Register`/`Deregister` are called only from `OnEnable`/`OnDisable` (ADR-0004's locked rule) — a trigger that commits mid-session therefore STAYS registered until its scene unloads; `CanInteract=false` (ADR-0010's focus gate) is what retires it from play, not an ad-hoc deregistration.

### Requirements

- Two states only (`Unfired`/`Committed`), transition one-way, no separate pure C# state machine — the state IS `GeceOturumDurumu.FiredTriggerIds` membership plus a local cached flag; `coding-standards.md`'s state-machine test rule is not triggered (confirmed assumption: the real logic lives in Etkileşim/Işık-Volume, both already tested under their own ADRs). The one testable pure function (`ShouldStartCommitted`) is still extracted for a `[Test]`.
- A Committed trigger's `GameObject` stays visible (it is scenery — a drawer, a photo frame), but must never appear in a post-restore `InteractableRegistry` snapshot.
- `TotalConfiguredTriggerCountForNight` must live on `GeceOturumDurumu` (ADR-0006's anticipated home) with a write path mirroring `SetRoundState`'s shape, and a build-time guarantee it can never desync from the real trigger-asset count.

## Decision

### Data model and component

```csharp
[CreateAssetMenu(menuName = "Beyond The Line/Memory Trigger")]
public sealed class MemoryTriggerDef : ScriptableObject {
    [SerializeField] private string _shiftId;          // duplicate-checked at build time (GDD Edge Cases)
    [SerializeField] private ShiftConfig _shiftConfig; // ScriptableObject ASSET reference — architecture.md
                                                        // Principle #5 lists ShiftConfig among the project's
                                                        // config ScriptableObjects and the Işık/Volume GDD's
                                                        // AC14b calls it a "ShiftConfig asset'i" with its own
                                                        // OnValidate (cited here because ADR-0005 itself never
                                                        // declares the type — unity-specialist review noted a
                                                        // struct misreading would silently copy per-def values).
                                                        // Persistent MUST be true — build-validated, never
                                                        // runtime-checked; the validator also null-checks _def/
                                                        // _shiftConfig with pointed messages (an unassigned ref
                                                        // would otherwise NRE at first focus or in the scan).
    [SerializeField, Range(0.6f, 1.5f)] private float _holdDuration = 1.0f;  // GDD's sub-range of Etkileşim's knob
    [SerializeField] private string _promptText;
    public string ShiftId => _shiftId;
    public ShiftConfig ShiftConfig => _shiftConfig;
    public float HoldDuration => _holdDuration;
    public string PromptText => _promptText;
}
```

```csharp
// Lives in a level scene (never persistent — interactable_in_persistent_scene,
// ADR-0004), on the fixed hotel object (drawer, photo frame). Thin
// orchestration only: no state machine class — the two-state, one-way
// Unfired→Committed "machine" is FiredTriggerIds membership (session
// truth, ADR-0006) plus this component's cached _committed flag.
public sealed class MemoryTriggerObject : MonoBehaviour, IInteractable {
    [SerializeField] private MemoryTriggerDef _def;

    private bool _committed;

    public InteractionType Type => InteractionType.Hold;
    public float HoldDuration => _def.HoldDuration;
    public bool CanInteract => !_committed;              // Committed → never focusable again (ADR-0010 gate)
    public string PromptText => _def.PromptText;
    public bool SuppressDefaultHoldFill => true;         // GDD Core Rules — zero-feedback Hold, the crosshair
                                                          // fill is never drawn for this object (AC14a path)

    // Pure, static, [Test]-able — the one piece of decision logic.
    internal static bool ShouldStartCommitted(string shiftId, IGeceOturumDurumuState session)
        => session.HasFired(shiftId);

    // ADR-0013's scene_object_state_restore_timing pattern (registry-
    // MANDATED for this ADR): restore query at the TOP of OnEnable,
    // before Register — NOT Awake() (the GDD's original wording, sync-
    // edited alongside this ADR; QQ-07's Reload-Scene hazard). Unlike
    // CarryItemPickup, a Committed trigger does NOT SetActive(false):
    // the object is visible scenery and must remain so — it simply
    // never registers, satisfying architecture.md's "a fired object
    // must never appear in a post-restore snapshot" invariant while
    // staying rendered.
    private void OnEnable() {
        _committed = ShouldStartCommitted(_def.ShiftId, GeceOturumDurumu.Instance);
        if (_committed) return;                          // visible, silent, permanently non-interactable
        InteractableRegistry.Register(this);
    }

    private void OnDisable() {
        InteractableRegistry.Deregister(this);           // safe no-op if never registered (ADR-0004)
    }

    public void OnHoldComplete() {
        // GDD Core Rules: TriggerShift with no extra guard (already-
        // active is a safe no-op, guaranteed BY the Persistent=true
        // invariant, edit-time-locked). The facade call shape below is
        // the ADR-0001-standard static surface; the internal shiftId→
        // ShiftZone routing behind it is the lookup layer ADR-0005
        // explicitly deferred to its implementation story — this call
        // site consumes that future surface, it does not define it
        // (unity-specialist review, 2026-08-08).
        IsikVolumeDurumSistemi.Instance.TriggerShift(_def.ShiftId, _def.ShiftConfig);
        // Direct Fired write, same step — the quick spec's own locked
        // model (corrected during unity-specialist review, see
        // Constraints): idempotent, fires OnTriggerFired on first add.
        GeceOturumDurumu.InternalInstance.AddFiredTrigger(_def.ShiftId);
        _committed = true;
        // Stays registered until scene unload — Register/Deregister are
        // OnEnable/OnDisable-only (ADR-0004's locked rule). Retirement
        // from play is CanInteract=false + ADR-0010's Focused-branch
        // CanInteract re-poll (the companion revision this ADR applies
        // to ADR-0010 — see Risks): focus drops the frame after commit,
        // the prompt disappears, and no re-Hold is possible.
    }

    public void OnHoldCancelled() { }                    // total no-op — GDD Core Rules ("hiçbir şey olmamıştır")
    public void OnHoldProgress(float t) { }              // deliberately unused — no per-object easing (GDD)
    public void OnHoldBlocked() { }                      // no reaction — blocked feedback is Etkileşim's concern
    public void OnFocusEnter() { }
    public void OnFocusExit() { }
    public void OnInteract() { }                         // Hold type — Instant path never taken
}
```

### `TotalConfiguredTriggerCountForNight`: owned by `GeceOturumDurumu`, written once at night begin, build-verified

**Confirmed by the user (`AskUserQuestion`, 2026-08-08)** — resolving the question ADR-0006 explicitly deferred to this ADR:

- **Storage**: `int TotalConfiguredTriggerCountForNight` joins `IGeceOturumDurumuState` — the follow-up edit ADR-0006's Related Decisions section already reserved ("should add the field to `IGeceOturumDurumuState` as a small follow-up edit to this ADR rather than inventing a second home"). Applied to ADR-0006's file at write time.
- **Write path**: `GeceOturumDurumu.InternalInstance.SetTotalConfiguredTriggerCountForNight(int)` — the exact `SetRoundState` twin (internal, `InternalInstance`-reached, convention-restricted to one caller, QQ-03's accepted enforcement level). Called **once, at night begin**, by the same night-begin orchestrator that calls ADR-0013's `StartNight()` — one orchestration point, two setup calls, caller definition owed by ADR-0015 (the ordering constraint ADR-0013 already made binding covers both: setup completes before level scenes activate).
- **Source**: a serialized `int` on the night-configuration asset (the asset itself is finalized by ADR-0015's night-begin design; this ADR fixes only the field's home, write path, and validation).
- **Desync-proofing**: the shared `IPreprocessBuildWithReport` utility gains a check — the configured value must equal the project's `MemoryTriggerDef` asset count via `AssetDatabase.FindAssets("t:MemoryTriggerDef")`. A mismatch fails the build. This makes the saturation condition (`SettledTriggerIds.Count == TotalConfiguredTriggerCountForNight`, ADR-0015) structurally incapable of comparing against a stale count — the exact "untestable until assigned" gap two cross-review rounds flagged. Three `FindAssets` caveats (unity-specialist review, 2026-08-08): **(a)** it returns one GUID per asset *file* — sub-asset defs inside a shared `.asset` would be undercounted (and invisible to the duplicate-`shiftId` check), so a "no sub-asset `MemoryTriggerDef`s" convention is enforced by the same pass; **(b)** the scan is project-wide — EditMode test fixtures MUST use `ScriptableObject.CreateInstance` (invisible to `FindAssets`), never on-disk fixture assets, or the real build fails on test data; **(c)** the equality is night-blind (per-night config value vs. project-wide asset total) — exact at MVP's one night, and ADR-0015's night-config work must revisit the formula when multi-night structure exists (same class of flag as ADR-0012's `ValidateMaxCallbacksPerScene`).

### Edit-time validation: the shared utility's five memory-trigger checks

The GDD's own Core Rules already specify the mechanism precisely (`IPreprocessBuildWithReport`, `AssetDatabase.FindAssets("t:MemoryTriggerDef")` asset scan + a separate scene-scan step, `BuildFailedException`; `OnValidate()` explicitly ruled out for cross-asset checks). This ADR adds no new mechanism — it confirms the shared editor utility (ADR-0012/ADR-0013's contributions already live there) hosts the GDD's four checks plus the new fifth:

1. Duplicate `shiftId` across `MemoryTriggerDef` assets (asset scan);
2. `Persistent != true` (asset scan) — the single edit-time defense the "artık geri alamam" guarantee rests on;
3. `StingerAudioRadius <= 0` on the linked zone's `ShiftConfig` (asset scan);
4. `TriggerMode != ManualOnly` on the matching-`shiftId` scene zone (scene scan — the GDD's own 2026-08-04 correction, a separate step because `TriggerMode` is a scene-component field);
5. **NEW**: `TotalConfiguredTriggerCountForNight` config value ≠ actual `MemoryTriggerDef` asset count (asset scan; this ADR's Decision above).
6. **NEW (added during TD-ADR review, 2026-08-08 — reachability hole)**: a `MemoryTriggerDef` with no matching-`shiftId` `MemoryTriggerObject`/`ShiftZone` in any level scene → error. The count check alone guarantees the *number*, not that every def is physically reachable — an authored-but-never-placed def would make saturation (`SettledCount == TotalConfiguredTriggerCountForNight`) permanently unreachable, silently killing end (b) for the night (end (a), task completion, still fires — not a soft-lock, but a dead narrative branch). The scene-scan step already exists for check #4; this extends it.

## Alternatives Considered

### Alternative 1: Pure C# state machine + static facade (ADR-0013's full shape)
- **Description**: A `MemoryTriggerStateMachine` + `AniTetikleyiciEtkilesim` facade, mirroring ADR-0013.
- **Pros**: Maximum consistency with the two prior Core/Feature ADRs.
- **Cons**: There is nothing to put in it — the system has two states with a one-way transition, the session-truth already lives in `GeceOturumDurumu.FiredTriggerIds` (ADR-0006), and every behavior is a single delegated call into an already-ADR'd system. A facade would own no state and forward everything; `coding-standards.md`'s BLOCKING state-machine test rule targets state-machine *logic*, of which there is none beyond `ShouldStartCommitted` (extracted as a pure static function and tested directly).
- **Rejection Reason**: Confirmed assumption (user, 2026-08-08): machinery without content — the GDD itself calls this system "saf event-orkestrasyonu" with N/A Formulas; the thin `MonoBehaviour` is the honest shape.

### Alternative 2: `GeceOturumDurumu` derives `TotalConfiguredTriggerCountForNight` itself (lazy asset count)
- **Description**: The Foundation service counts `MemoryTriggerDef` assets lazily (Addressables), no config field.
- **Pros**: No config value to keep in sync; no new write path.
- **Cons**: Makes a pure-state Foundation service an asset-loading consumer (a second Addressables consumer with `engine_asset_api_call_in_foundation_constructor` lazy-load constraints, per ADR-0007's precedent) for a single MVP-static integer; runtime asset enumeration is the heavyweight version of a number the build already knows and can verify for free.
- **Rejection Reason**: User confirmed (`AskUserQuestion`, 2026-08-08): the write-once-at-night-begin + build-verified config value achieves the same desync-impossibility with zero new runtime machinery.

### Alternative 3: Deregister from `InteractableRegistry` immediately on commit
- **Description**: `OnHoldComplete()` calls `InteractableRegistry.Deregister(this)` so a committed trigger leaves the registry mid-session, not just on scene unload.
- **Pros**: Registry only ever contains genuinely interactable objects.
- **Cons**: Directly violates ADR-0004's locked "Register/Deregister called only from OnEnable/OnDisable" rule (the registry's self-correcting lifecycle argument depends on it); gains nothing — ADR-0010's `TryEnterFocused` already refuses `CanInteract=false` targets, so a registered-but-committed trigger is inert.
- **Rejection Reason**: Contradicts a registered stance for zero behavioral benefit.

## Consequences

### Positive
- Closes `architecture.md`'s Required ADR #14 and the long-open `TotalConfiguredTriggerCountForNight` ownership item (flagged unresolved since 2026-08-03, deferred by ADR-0006, called "untestable until assigned" by two cross-review rounds).
- Second adopter of ADR-0013's restore pattern — the registry's MANDATORY note is discharged, and the pattern now has both variants on record (deactivate-entirely: `CarryItemPickup`; stay-visible-skip-register: `MemoryTriggerObject`).
- The direct `AddFiredTrigger` write restores the quick spec's locked model exactly (corrected during TD-ADR review after a stale "transitive... zero new API surface" claim survived the first fix pass here) — Fired stays precisely the Hold-completed-ids filter that gates Settled, and the new internal API rides the follow-up-edit vehicle this ADR already carries for ADR-0006.

### Negative
- The `Persistent=true` invariant remains a single, bypassable edit-time check with no runtime backstop — unchanged from the GDD (which documents this single-point dependency explicitly); this ADR inherits rather than solves it.
- A committed trigger stays in the `InteractableRegistry` until its scene unloads (Alternative 3's rejection) — snapshot iterators pay one inert entry per committed trigger for the rest of the scene's lifetime; negligible at MVP's 2-3 triggers, noted for Full Vision's 15-20.
- The night-configuration asset holding the trigger-count field is only sketched here — its full shape is ADR-0015's scope; until ADR-0015 lands, the field's source asset is a named placeholder.

### Risks
- **Risk**: The GDD's Committed-restore and States-table wording say `Awake()` — a developer reading the GDD without this ADR would implement the superseded shape. **Mitigation**: GDD sync edits at write time (same pass as ADR-0013's own sync), plus the registry's `scene_object_state_restore_timing` entry already marks the `OnEnable`-top shape as mandatory for exactly this object.
- **Risk (found BLOCKING during unity-specialist review, 2026-08-08 — resolved by a companion revision to ADR-0010, user-confirmed)**: an earlier draft called the post-complete lingering focus "cosmetic" — false. Traced against ADR-0010's actual `Tick()`: after `CompleteHold()` the state returns to `Focused` with `CurrentTarget` still set; the `Focused` branch never re-polls `CanInteract` (only `TryEnterFocused`, the from-`Idle` entry, checks it), and `interactHeld` is by definition still true at the completion instant — so a player who simply keeps holding the button re-enters `Holding` on the now-Committed trigger, indefinitely: movement lock re-taken and re-released each cycle, prompt pinned, `TriggerShift` re-called (a `Persistent`-guaranteed no-op, but the GDD's "tam bir kez çağrılır" AC and "artık hiçbir prompt/focus mümkün değil" rule are both violated). **Resolution**: ADR-0010's `Focused` branch gains a `CanInteract` re-poll at its top — if the focused target's `CanInteract` has gone false, `OnFocusExit()` fires and the state returns to `Idle` (applied to ADR-0010's file with a correction note at this ADR's write time). This retires a committed trigger one frame after commit and does not affect `CarryItemPickup`'s slots-full case (its `CanInteract` deliberately stays `true`, ADR-0013).
- **Risk (corrected by ADR-0015, 2026-08-08)**: `SettledTriggerIds` (which ADR-0015's saturation counts) is written via `GeceOturumDurumu`'s constructor-time subscription to `Işık/Volume`'s `OnShiftStateChanged(Held)` — gated on the id already being in `FiredTriggerIds`. An earlier version of this bullet warned that a `ResetAll()` reordering could bind the subscription stale — **false under ADR-0015's in-place reset regime**: the subscription binds once per process to a never-replaced instance and survives every reset; `ResetAll()` ordering is data-only. The residual truths: the end-to-end chain (Hold → Fired direct write → Held → Settled → saturation) leans on the subscription existing at all (once-per-process construction), and on `Işık/Volume`'s in-place conversion (ADR-0015) keeping it alive — both structural, neither ordering-sensitive.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|---------------------------|
| `ani-tetikleyici-etkilesim.md` | `IInteractable.Hold`, `HoldDuration` 0.6–1.5s sub-range, `SuppressDefaultHoldFill=true`, `OnHoldProgress` easing deliberately unused | `MemoryTriggerObject`'s interface members; `[Range]`-constrained def field |
| `ani-tetikleyici-etkilesim.md` | `OnHoldComplete` → `TriggerShift` only, no extra guard; `OnHoldCancelled` total no-op | `OnHoldComplete()`/`OnHoldCancelled()` exactly as specified |
| `ani-tetikleyici-etkilesim.md` | Committed persistence via `FiredTriggerIds`, restore before registration, never written to the asset | `OnEnable`-top `ShouldStartCommitted` check (ADR-0013 pattern, GDD's `Awake()` wording sync-edited); fired flag lives only in session state |
| `ani-tetikleyici-etkilesim.md` + `gece-oturum-durumu-2026-08-02.md` | `FiredTriggerIds` gains the id in the same frame as the `TriggerShift` call; `OnTriggerFired` fires on first add | `GeceOturumDurumu.InternalInstance.AddFiredTrigger(shiftId)` in `OnHoldComplete()` — the quick spec's locked direct-write model, `SetRoundState`'s twin (corrected during unity-specialist review) |
| `ani-tetikleyici-etkilesim.md` | Four edit-time checks (duplicate `shiftId`, `Persistent`, `StingerAudioRadius`, `TriggerMode` scene-scan), `IPreprocessBuildWithReport`, build-blocking | Hosted in the shared editor utility (ADR-0012/0013's precedent), enumerated in Decision |
| `sahne-kesmeli-anlati-2026-08-02.md` / ADR-0006 | `TotalConfiguredTriggerCountForNight` needs an owner, a write path, and testable saturation ACs | `GeceOturumDurumu` field + `SetTotalConfiguredTriggerCountForNight` internal setter + build-time count-equality check (Decision) |
| `architecture.md` | Module Ownership row — `MemoryTriggerDef` assets, `MemoryTriggerObject` Committed state, thin orchestration, no downstream consumers | Implemented as designed |

## Performance Implications
- **CPU**: Zero per-frame logic — this component has no `Update()`; everything is event-driven through Etkileşim's existing pipeline. Negligible.
- **Memory**: One `MemoryTriggerDef` asset + one `bool` per trigger (MVP: 2-3). Negligible.
- **Load Time**: N/A — plain serialized references.
- **Network**: N/A.

## Migration Plan
No existing code to migrate (`Anı-Tetikleyici Etkileşim` is not yet implemented).

**Cross-file edits at write time**: (1) ADR-0006 — add `TotalConfiguredTriggerCountForNight` to `IGeceOturumDurumuState`, `SetTotalConfiguredTriggerCountForNight` AND `AddFiredTrigger(shiftId)` to the internal surface (the follow-up edit ADR-0006's own Related Decisions reserved for this ADR, extended by the B2 resolution), fix its stale "all mutation happens through `EndSession()` and `SetRoundState()`... never any other path" data-model comment (contradicted its own constructor-subscription writes even before this ADR — unity-specialist review), and add one clarifying clause to its Requirements bullet that lumped Fired under the subscription-driven framing (the likely seed of the registry's over-generalized write_access line — TD-ADR review). (2) ADR-0010 — the `Focused`-branch `CanInteract` re-poll (B1's companion revision) with a correction note; the re-poll path mirrors the existing target-changed path exactly (fire `OnFocusExit`, clear `CurrentTarget`, set `Idle`, then attempt `TryEnterFocused(sphereCastTarget)` in the same `Tick()` — deterministic, so the regression test can assert the same-frame outcome; for a committed trigger `TryEnterFocused` refuses immediately, landing in `Idle`). (3) `ani-tetikleyici-etkilesim.md` — sync the Committed-restore/States-table `Awake()` wording to the `OnEnable`-top shape; `gece-oturum-durumu-2026-08-02.md`'s Dependencies line ("`Awake()`'te okur (Committed-restore)") gets the same sync (added during TD-ADR review — the sync list was undercounted, ADR-0013's own TD finding class). (4) `architecture.md`'s QQ-07 residual note — the "ADR-0014 must adopt" clause is now discharged. (5) Registry — `gece_oturum_durumu_session_state`'s write_access line revised (Fired: direct internal write by Anı-Tetikleyici via `AddFiredTrigger`, not the subscription; Persistent/Settled unchanged).

**Registry note**: at Step 6 — no new state_ownership (this system owns no shared state; Committed truth lives in ADR-0006's entry); `gece_oturum_durumu_session_state`'s interface/write_access revised (new field + two internal writers: `SetTotalConfiguredTriggerCountForNight`, `AddFiredTrigger`) with `referenced_by` gaining this ADR; `scene_object_state_restore_timing` gains this ADR as second adopter AND its api wording is revised (per the registry's own revision procedure: `revised:` date + old-value comment) to cover both variants — deactivate-entirely (`CarryItemPickup`) and stay-active-skip-`Register` (`MemoryTriggerObject`) — since the current letter mandates `SetActive(false)`, which this ADR's Decision deliberately does not do (TD-ADR review); the `Focused`-branch re-poll revision is noted in ADR-0010's file itself; no registry entry is needed for it (it constrains no other system — TD-ADR review corrected an earlier confusing cross-reference to the unrelated layer-mask entry).

## Validation Criteria
- A `[Test]` drives `ShouldStartCommitted` against a stub `IGeceOturumDurumuState`: fired id → `true`, unfired → `false` — the restore decision in isolation.
- A `[UnityTest]` confirms a `MemoryTriggerObject` whose `shiftId` is in `FiredTriggerIds` at scene load: stays active/visible, never appears in an `InteractableRegistry.Snapshot()`, `CanInteract == false` (GDD Committed-restore AC).
- A `[UnityTest]` (Reload Scene disabled, two simulated sessions) confirms the `OnEnable`-top restore re-evaluates across a Play Stop→Play boundary — session 2's cleared `FiredTriggerIds` yields an Unfired, registered trigger (the QQ-07 scenario, ADR-0013-pattern conformance).
- A `[Test]`/integration test confirms `OnHoldComplete` calls `TriggerShift` exactly once with the def's `shiftId`/`shiftConfig`, calls `AddFiredTrigger` with the same id in the same step (observed: `FiredTriggerIds` contains the id and `OnTriggerFired` fired exactly once, same frame), and sets `_committed`.
- A `[Test]` against ADR-0010's revised `InteractionStateMachine` confirms the new `Focused`-branch `CanInteract` re-poll: a focused target whose `CanInteract` goes false is exited to `Idle` (with `OnFocusExit`) on the next `Tick()`, and continued `interactHeld` does NOT re-enter `Holding` — the phantom re-Hold regression test (unity-specialist review, 2026-08-08).
- An idempotency `[Test]` confirms a second `AddFiredTrigger` with the same id is a no-op (no second `OnTriggerFired`) — architecture.md Principle #4.
- An EditMode test confirms the shared `IPreprocessBuildWithReport` utility fails the build for each of the five checks (duplicate `shiftId`, `Persistent=false`, `StingerAudioRadius<=0`, `TriggerMode=Automatic` scene-scan, trigger-count mismatch) — the first four per the GDD's own ACs 3/4/4a/4b, the fifth per this ADR's Decision.
- A `[Test]` confirms `SetTotalConfiguredTriggerCountForNight` is reachable only via `InternalInstance` (compile-surface inspection) and that `TotalConfiguredTriggerCountForNight` reads back the set value through `GeceOturumDurumu.Instance`.
- Note: the GDD's AC6 (`RevertShift(` project-wide CI grep/lint, BLOCKING) is a separate CI-pipeline mechanism, unchanged and unowned by this ADR — listed here so the five build checks + these criteria are not misread as the complete validation story (TD-ADR review).

## Related Decisions
- ADR-0004 (InteractableRegistry) — registration lifecycle this ADR's stay-registered-until-unload decision (Alternative 3's rejection) preserves.
- ADR-0005 (Işık/Volume) — `TriggerShift`/`Persistent` semantics consumed verbatim.
- ADR-0006 (Session State) — single-writer-per-fact model preserved: Fired gains a dedicated internal write path (`AddFiredTrigger`, Anı-Tetikleyici's one entry point) alongside the subscription that writes Persistent/Settled; also receives the `TotalConfiguredTriggerCountForNight` follow-up edit it reserved for this ADR.
- ADR-0010 (Interaction State Machine) — the Hold contract consumed; receives the companion `Focused`-branch `CanInteract` re-poll revision (Risks) that closes the phantom re-Hold path — a fidelity restoration, not a redesign: `etkilesim-sistemi.md`'s own Focused row already lists "hedef devre dışı kalır" as a Focused→Idle exit that ADR-0010's original `Tick()` under-implemented, so no GDD sync is needed for it.
- ADR-0013 (Carry Loop) — source of the MANDATED restore pattern; this ADR is its second adopter (stay-visible variant) and shares the night-begin orchestration point (ADR-0015-owed) for its setup call.
- Future ADR-0015 (End-Condition Orchestration) — consumes the saturation-count write path; owes the night-begin caller for both `StartNight()` and `SetTotalConfiguredTriggerCountForNight`.
