using UnityEngine;

/// <summary>
/// Gets assigned a permanent voice + pitch on Start.
/// Always plays the same sound at the same pitch when producing food.
/// </summary>
public class BlockAudioPlayer : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float baseVolume = 1f;
    [Tooltip("This gets multiplied by the per-voice volume from VoiceManager")]

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Permanent assignment for this block
    private AudioClip myVoiceClip;
    private float myPitch;
    private float myVoiceVolume; // Volume from VoiceManager
    private bool voiceAssigned = false;

    void Start()
    {
        // Get or create audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Get permanent voice assignment from VoiceManager
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.AssignVoiceToBlock(gameObject, out myVoiceClip, out myPitch, out myVoiceVolume);
            voiceAssigned = true;

            // Set the audio source pitch permanently
            audioSource.pitch = myPitch;

            if (showDebugLogs)
            {
                Debug.Log($"Block at {transform.position} assigned voice '{myVoiceClip.name}' at pitch {myPitch:F2}, volume {myVoiceVolume:F2}");
            }
        }
        else
        {
            Debug.LogWarning("BlockAudioPlayer: VoiceManager not found in scene!");
        }
    }

    /// <summary>
    /// Plays this block's assigned voice.
    /// Call this when the block produces food.
    /// </summary>
    public void PlayProductionSound()
    {
        if (!voiceAssigned || myVoiceClip == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("BlockAudioPlayer: No voice assigned or clip is null!");
            }
            return;
        }

        // Get current volume from VoiceManager (includes real-time ducking)
        float currentMasterVolume = VoiceManager.Instance != null ?
            VoiceManager.Instance.GetCurrentMasterVolume() : 1f;

        // Play this block's specific voice at its specific pitch and volume
        float finalVolume = baseVolume * myVoiceVolume * currentMasterVolume;
        audioSource.PlayOneShot(myVoiceClip, finalVolume);

        if (showDebugLogs)
        {
            Debug.Log($"Block played its voice '{myVoiceClip.name}' at pitch {myPitch:F2}, volume {finalVolume:F2}");
        }
    }

    void OnDestroy()
    {
        // Release voice assignment when destroyed
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.ReleaseBlockVoice(gameObject);
        }
    }

    /// <summary>
    /// Public getter for this block's voice (for debugging/visualization)
    /// </summary>
    public AudioClip GetMyVoice() => myVoiceClip;

    /// <summary>
    /// Public getter for this block's pitch (for debugging/visualization)
    /// </summary>
    public float GetMyPitch() => myPitch;
}