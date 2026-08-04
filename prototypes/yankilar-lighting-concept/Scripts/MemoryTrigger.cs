// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a lighting/color-temperature shift alone create felt unease?
// Date: 2026-08-01

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class MemoryTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("The Global Volume whose weight ramps from 0 (real) to 1 (memory-cold). Configure the Volume Profile's White Balance / Color Adjustments overrides in the Editor to define the cold look.")]
    public Volume targetVolume;

    [Tooltip("The 'reality' lights in this room — their color/intensity lerp toward coldColor as the shift progresses.")]
    public Light[] realityLights;

    public float shiftDuration = 3f;
    public Color coldColor = new Color(0.56f, 0.66f, 0.76f); // cold blue; try a sodium-green (~#8FA88C) too
    public float coldIntensityMultiplier = 0.6f;

    private Color[] _originalLightColors;
    private float[] _originalLightIntensities;
    private bool _triggered;

    void Start()
    {
        if (realityLights == null || realityLights.Length == 0) return;

        _originalLightColors = new Color[realityLights.Length];
        _originalLightIntensities = new float[realityLights.Length];
        for (int i = 0; i < realityLights.Length; i++)
        {
            _originalLightColors[i] = realityLights[i].color;
            _originalLightIntensities[i] = realityLights[i].intensity;
        }
    }

    public void OnInteract()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(ShiftRoutine());
    }

    private IEnumerator ShiftRoutine()
    {
        float t = 0f;
        while (t < shiftDuration)
        {
            t += Time.deltaTime;
            float p = t / shiftDuration;

            if (targetVolume != null)
                targetVolume.weight = p;

            if (realityLights != null)
            {
                for (int i = 0; i < realityLights.Length; i++)
                {
                    realityLights[i].color = Color.Lerp(_originalLightColors[i], coldColor, p);
                    realityLights[i].intensity = Mathf.Lerp(
                        _originalLightIntensities[i],
                        _originalLightIntensities[i] * coldIntensityMultiplier,
                        p);
                }
            }

            yield return null;
        }
    }
}
