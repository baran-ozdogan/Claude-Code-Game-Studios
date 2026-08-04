// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does light+sound together create felt unease where light alone did not?
// Date: 2026-08-02
//
// Continuous diegetic room-tone: low mechanical hum + a slow irregular
// amplitude wobble (stand-in for "compressor kompresör uğultusu (düzensiz)"
// from design/gdd/adaptif-ses-sistemi.md). Procedurally generated so the
// spike needs zero external audio assets. All frequencies are chosen as
// exact integer cycle counts over the buffer duration so the loop point
// has zero discontinuity (no click).

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbientHum : MonoBehaviour
{
    [Tooltip("Overall ambient gain — kept low; this is room-tone, not music.")]
    public float gain = 0.05f;

    private const int SampleRate = 44100;
    private const float Duration = 4f; // seconds, must keep all frequencies below at integer cycles/Duration

    void Awake()
    {
        var source = GetComponent<AudioSource>();
        source.clip = BuildClip();
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f; // room-tone, not a point source
        source.volume = 1f; // gain is baked into the samples
        source.Play();
    }

    private AudioClip BuildClip()
    {
        int sampleCount = Mathf.RoundToInt(SampleRate * Duration);
        var samples = new float[sampleCount];

        // 55Hz low hum + 110Hz harmonic — 220 / 440 cycles over 4s, both integers.
        const float baseFreq = 55f;
        const float harmonicFreq = 110f;
        // Slow "irregular compressor" wobble — 0.5Hz, 2 cycles over 4s.
        const float wobbleFreq = 0.5f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float wobble = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * wobbleFreq * t);
            float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.7f
                       + Mathf.Sin(2f * Mathf.PI * harmonicFreq * t) * 0.3f;
            samples[i] = wave * wobble * gain;
        }

        var clip = AudioClip.Create("AmbientHum_Procedural", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
