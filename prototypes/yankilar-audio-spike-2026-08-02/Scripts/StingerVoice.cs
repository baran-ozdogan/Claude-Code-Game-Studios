// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does light+sound together create felt unease where light alone did not?
// Date: 2026-08-02
//
// One-shot stinger. v1-v6 were procedural synthesis attempts (sine, noise,
// harmonic+noise hybrids tuned to measurements of the user's reference audio)
// that kept missing the mark. v7 drops synthesis entirely and plays the
// user's actual reference clip instead — Audio/StingerStrike.wav, a 1.3s
// trim of the first isolated "swell" phrase from their downloaded reference
// file ("Scary Horror Ambience (Intense Violin Strikes)"), faded in/out for
// clean one-shot playback. Placeholder audio is explicitly allowed in
// prototypes (.claude/rules/prototype-code.md) — this is throwaway spike
// code, never shipped.

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StingerVoice : MonoBehaviour
{
    [Tooltip("Assign Audio/StingerStrike.wav here (drag from Project window after importing it into Assets).")]
    public AudioClip stingerClip;

    [Tooltip("Playback gain — separate from the clip's own normalization, so it can be balanced against the ambient hum without re-exporting audio.")]
    public float volume = 0.6f;

    void Awake()
    {
        var source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = 1.5f;
        source.maxDistance = 8f;
    }

    public void PlayStinger(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        var source = GetComponent<AudioSource>();
        if (stingerClip == null)
        {
            Debug.LogWarning("StingerVoice: no stingerClip assigned — import Audio/StingerStrike.wav into Assets and assign it in the Inspector.");
            return;
        }
        source.PlayOneShot(stingerClip, volume);
    }
}
