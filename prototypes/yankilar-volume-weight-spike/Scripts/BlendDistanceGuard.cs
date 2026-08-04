// PROTOTYPE - NOT FOR PRODUCTION
// Question 3: does OnValidate on a sibling script catch someone changing the
// Volume's own Blend Distance field later in the Inspector, or only fire for
// edits to its own fields?
// Date: 2026-08-01

using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Volume))]
public class BlendDistanceGuard : MonoBehaviour
{
    private void OnValidate()
    {
        Volume vol = GetComponent<Volume>();
        Debug.Log($"[BlendDistanceGuard] OnValidate fired on '{name}'. " +
                   $"Volume.blendDistance = {vol.blendDistance}. " +
                   "If you just changed Blend Distance on the Volume component " +
                   "directly (not this script) and still see this line, that's " +
                   "expected only if Unity re-validates all sibling components on " +
                   "any Inspector edit in this project/version — note whether that " +
                   "happened or not.");
    }
}
