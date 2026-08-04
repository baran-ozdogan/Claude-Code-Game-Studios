// PROTOTYPE - NOT FOR PRODUCTION
// Questions 1 & 2: does blendDistance=0 actually decouple the Volume's
// weight from collider distance, and what happens when the player exits the
// physical collider bounds while the scripted weight is still > 0?
// Date: 2026-08-01
//
// Deliberately simplified vs. the real GDD's Formulas (no smoothstep-x
// tracking, no interrupt/resume) — this spike isolates ONE question: does
// Volume.weight direct-write behave the way the GDD's Core Rules claim.

using UnityEngine;
using UnityEngine.Rendering;

public enum SpikeState { Dormant, Held, ShiftingOut }

public class SpikeZoneController : MonoBehaviour
{
    public Transform player;
    public Volume targetVolume;
    public BoxCollider zoneBox;
    public float rTrigger = 4f;
    public float rExit = 4.6f;
    public float shiftOutDuration = 3f;

    private SpikeState _state = SpikeState.Dormant;
    private float _shiftOutTimer;
    private bool _wasInsideBox;
    private float _weight;

    void Update()
    {
        if (player == null || targetVolume == null || zoneBox == null) return;

        float distance = Vector3.Distance(player.position, transform.position);
        bool insideBox = zoneBox.bounds.Contains(player.position);

        switch (_state)
        {
            case SpikeState.Dormant:
                _weight = 0f;
                if (distance <= rTrigger)
                {
                    _state = SpikeState.Held;
                    Debug.Log($"[SpikeZone] -> Held at t={Time.time:F2}, distance={distance:F2}");
                }
                break;

            case SpikeState.Held:
                _weight = 1f;
                if (distance > rExit)
                {
                    _state = SpikeState.ShiftingOut;
                    _shiftOutTimer = 0f;
                    Debug.Log($"[SpikeZone] -> ShiftingOut at t={Time.time:F2}, distance={distance:F2}");
                }
                break;

            case SpikeState.ShiftingOut:
                _shiftOutTimer += Time.deltaTime;
                float x = Mathf.Clamp01(_shiftOutTimer / shiftOutDuration);
                _weight = 1f - (3f * x * x - 2f * x * x * x); // smoothstep, 1 -> 0
                if (x >= 1f)
                {
                    _state = SpikeState.Dormant;
                    _weight = 0f;
                    Debug.Log($"[SpikeZone] -> Dormant (ShiftingOut complete) at t={Time.time:F2}");
                }
                break;
        }

        // The line under test: direct-write every frame, per the GDD's
        // "bypasses Unity's blend entirely" claim.
        targetVolume.weight = _weight;

        if (_wasInsideBox && !insideBox)
        {
            Debug.Log($"[SpikeZone] *** EXITED BOX COLLIDER *** at t={Time.time:F2}, " +
                      $"state={_state}, scriptedWeight={_weight:F3}, distance={distance:F2}, " +
                      $"blendDistance={targetVolume.blendDistance:F2}");
        }
        _wasInsideBox = insideBox;

        _lastDistance = distance;
        _lastInsideBox = insideBox;
    }

    // --- HUD ---
    private float _lastDistance;
    private bool _lastInsideBox;

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 340, 130), "");
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        GUI.Label(new Rect(20, 15, 320, 24), $"State: {_state}", style);
        GUI.Label(new Rect(20, 40, 320, 24), $"Distance to zone center: {_lastDistance:F2} m", style);
        GUI.Label(new Rect(20, 65, 320, 24), $"Inside box collider: {(_lastInsideBox ? "YES" : "NO")}", style);
        GUI.Label(new Rect(20, 90, 320, 24), $"Scripted weight: {_weight:F3}", style);
        GUI.Label(new Rect(20, 115, 320, 24),
            $"ShiftingOut timer: {(_state == SpikeState.ShiftingOut ? _shiftOutTimer.ToString("F2") : "-")} / {shiftOutDuration}s",
            style);
    }
}
