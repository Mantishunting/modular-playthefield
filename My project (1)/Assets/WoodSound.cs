using UnityEngine;

/// <summary>
/// Controls the audio for a Wood block.
/// Plays a unique sound once with a 1 in 6 chance when the block is created,
/// ONLY if no other Wood sound is currently playing.
/// Uses Invoke and OnDestroy to ensure the lock is never permanently jammed.
/// </summary>
public class WoodSound : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Chance to play sound on creation (1 in X). Higher number = rarer sound.")]
    [SerializeField] private int chanceToPlay = 6;

    // Static flag to enforce exclusive playback across ALL WoodSound instances
    private static bool isAnyWoodSoundPlaying = false;

    // Audio State
    private AudioSource audioSource;
    private AudioClip myWoodClip;
    private float myPitch;
    private float myVolume;

    void Start()
    {
        // 1. Setup Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 2. Get Permanent Voice from Manager
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.AssignWoodVoice(
                gameObject,
                out myWoodClip,
                out myPitch,
                out myVolume
            );

            audioSource.pitch = myPitch;
            audioSource.playOnAwake = false;

            // 3. Attempt to play the sound immediately upon start!
            TryPlaySoundOnStart();
        }
    }

    void TryPlaySoundOnStart()
    {
        // 1. GLOBAL CHECK: Only proceed if no other Wood sound is currently playing
        if (isAnyWoodSoundPlaying)
        {
            return;
        }

        // Safety check
        if (myWoodClip == null) return;

        // 2. DICE ROLL (e.g., 1 in 6)
        if (Random.Range(0, chanceToPlay) != 0) return;

        // 3. SUCCESS: Set the global lock, play, and schedule the release
        isAnyWoodSoundPlaying = true;

        audioSource.volume = myVolume;
        audioSource.PlayOneShot(myWoodClip);

        // Schedule the release of the lock after the clip's length
        if (myWoodClip != null)
        {
            // The object will wait 'myWoodClip.length' seconds before calling ReleaseWoodSoundLock
            Invoke("ReleaseWoodSoundLock", myWoodClip.length);
        }
    }

    /// <summary>
    /// Called by Invoke after the sound clip has finished its natural playback duration.
    /// </summary>
    void ReleaseWoodSoundLock()
    {
        isAnyWoodSoundPlaying = false;
    }

    void OnDestroy()
    {
        // This is the CRITICAL safety net for when blocks are destroyed frequently.

        // 1. Prevent a stale 'ReleaseWoodSoundLock' from running later on a different block's timer.
        CancelInvoke("ReleaseWoodSoundLock");

        // 2. If this block was the one playing the sound and got destroyed, 
        // it must immediately free the static lock so other blocks can play.
        // We assume that if this block is destroyed, the sound has been cut short.
        if (isAnyWoodSoundPlaying)
        {
            isAnyWoodSoundPlaying = false;
        }
    }
}