using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages a dynamic pool of audio clips (voices) based on active block count.
/// Assigns each block a permanent voice + pitch combination.
/// More blocks = more unique voices available for assignment.
/// </summary>
public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    [Header("Audio Clip Pool")]
    [Tooltip("All available voice audio clips. Pool will use up to maxPoolSize of these.")]
    [SerializeField] private AudioClip[] allVoiceClips;

    [Header("Per-Voice Volume Controls")]
    [Tooltip("Volume multiplier for each voice (matches allVoiceClips array). 1.0 = normal, 0.5 = half volume, 0 = muted")]
    [SerializeField] private float[] voiceVolumes = new float[] { 1f, 1f, 1f, 1f, 1f };

    [Header("Master Volume & Ducking")]
    [Tooltip("Overall volume multiplier for all production sounds (turn this down if 100s of leaves are too loud)")]
    [SerializeField] private float masterVolume = 1f;

    [Tooltip("When music is playing, multiply leaf volume by this amount (0.3 = 30% volume during music)")]
    [SerializeField] private float musicDuckingAmount = 0.3f;

    [Tooltip("How fast to fade volume when music starts/stops (seconds)")]
    [SerializeField] private float duckingFadeTime = 0.5f;

    [Header("Pool Scaling Settings")]
    [SerializeField] private int minPoolSize = 1;
    [SerializeField] private int maxPoolSize = 5;
    [SerializeField] private int blocksPerVoice = 10;
    [SerializeField] private float poolUpdateInterval = 1f;

    [Header("Pitch Variation Settings")]
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.15f;

    [Header("Flower Voice Clips")]
    [SerializeField] private AudioClip[] flowerVoiceClips;

    // --- WOOD VOICE CONTROLS MODIFIED HERE ---
    [Header("Wood Voice Clips")]
    [SerializeField] private AudioClip[] woodVoiceClips;
    [Tooltip("Volume multiplier for all wood voice clips.")]
    [SerializeField] private float woodClipsVolume = 1.0f; // New volume control
    [Tooltip("The main wood sound that plays most of the time.")]
    [SerializeField] private AudioClip primaryWoodVoiceClip; // New primary clip
    [Tooltip("The chance (0.0 to 1.0) that the primary voice clip will be chosen.")]
    [SerializeField] private float primaryVoicePlayChance = 0.8f; // New chance setting
    // -------------------------------

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private int currentPoolSize = 0;
    private float poolUpdateTimer = 0f;

    // Track assigned voice/pitch for each block
    private Dictionary<GameObject, VoiceAssignment> blockVoiceAssignments = new Dictionary<GameObject, VoiceAssignment>();

    // Music ducking state
    private bool isMusicPlaying = false;
    private float currentDuckingMultiplier = 1f;
    private float targetDuckingMultiplier = 1f;

    private struct VoiceAssignment
    {
        public AudioClip clip;
        public float pitch;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (allVoiceClips == null || allVoiceClips.Length == 0)
        {
            Debug.LogError("VoiceManager: No voice clips assigned! Please add AudioClips to the array.");
            return;
        }

        // Clamp max pool size to available clips
        maxPoolSize = Mathf.Min(maxPoolSize, allVoiceClips.Length);

        UpdateVoicePool();

        if (showDebugLogs)
        {
            Debug.Log($"VoiceManager initialized with {currentPoolSize} voices available (out of {allVoiceClips.Length} total clips)");
        }
    }

    void Update()
    {
        // Periodically update the voice pool based on active block count
        poolUpdateTimer += Time.deltaTime;
        if (poolUpdateTimer >= poolUpdateInterval)
        {
            UpdateVoicePool();
            poolUpdateTimer = 0f;
        }

        // Smoothly fade ducking multiplier
        if (currentDuckingMultiplier != targetDuckingMultiplier)
        {
            float fadeSpeed = 1f / duckingFadeTime;
            currentDuckingMultiplier = Mathf.MoveTowards(
                currentDuckingMultiplier,
                targetDuckingMultiplier,
                fadeSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Call this when music starts playing to duck (lower) leaf production sounds.
    /// </summary>
    public void SetMusicPlaying(bool isPlaying)
    {
        if (isMusicPlaying == isPlaying) return;

        isMusicPlaying = isPlaying;
        targetDuckingMultiplier = isPlaying ? musicDuckingAmount : 1f;

        if (showDebugLogs)
        {
            Debug.Log($"VoiceManager: Music {(isPlaying ? "started" : "stopped")} - ducking to {targetDuckingMultiplier:F2}");
        }
    }

    /// <summary>
    /// Returns the current master volume including ducking.
    /// </summary>
    public float GetCurrentMasterVolume()
    {
        return masterVolume * currentDuckingMultiplier;
    }

    /// <summary>
    /// Assigns a permanent voice + pitch to a block.
    /// Call this once when the block spawns. (Used for Leaves/Stems that use the dynamic pool)
    /// </summary>
    public void AssignVoiceToBlock(GameObject block, out AudioClip assignedClip, out float assignedPitch, out float assignedVolume)
    {
        // If already assigned, return existing assignment
        if (blockVoiceAssignments.ContainsKey(block))
        {
            VoiceAssignment existing = blockVoiceAssignments[block];
            assignedClip = existing.clip;
            assignedPitch = existing.pitch;
            assignedVolume = GetVolumeForClip(existing.clip);
            return;
        }

        // Pick a random voice from the currently available pool
        int voiceIndex = Random.Range(0, Mathf.Min(currentPoolSize, allVoiceClips.Length));
        AudioClip selectedClip = allVoiceClips[voiceIndex];

        // Pick a random pitch
        float selectedPitch = Random.Range(minPitch, maxPitch);

        // Get the volume for this voice
        float selectedVolume = GetVolumeForClip(selectedClip);

        // Store the assignment
        VoiceAssignment assignment = new VoiceAssignment
        {
            clip = selectedClip,
            pitch = selectedPitch
        };
        blockVoiceAssignments[block] = assignment;

        assignedClip = selectedClip;
        assignedPitch = selectedPitch;
        assignedVolume = selectedVolume;

        if (showDebugLogs)
        {
            Debug.Log($"VoiceManager: Assigned voice '{selectedClip.name}' at pitch {selectedPitch:F2}, volume {selectedVolume:F2} to block (pool size: {currentPoolSize})");
        }
    }

    /// <summary>
    /// Assigns a random flower chime from the dedicated pool.
    /// Used by Flower.cs.
    /// </summary>
    public void AssignFlowerVoice(GameObject block, out AudioClip clip, out float pitch, out float volume)
    {
        if (flowerVoiceClips == null || flowerVoiceClips.Length == 0)
        {
            clip = null;
            pitch = 1f;
            volume = 1f;
            return;
        }

        // Pick a random flower clip and pitch
        int idx = Random.Range(0, flowerVoiceClips.Length);
        clip = flowerVoiceClips[idx];
        pitch = Random.Range(minPitch, maxPitch);

        // Volume is based on master and ducking (no per-voice multiplier used here)
        volume = masterVolume * currentDuckingMultiplier;
    }

    /// <summary>
    /// Assigns a random wood sound from the dedicated pool, favoring a primary clip.
    /// Used by WoodSound.cs.
    /// </summary>
    public void AssignWoodVoice(GameObject block, out AudioClip clip, out float pitch, out float volume)
    {
        // --- VOLUME MODIFICATION IS AFTER CLIP SELECTION ---

        // 1. Roll the dice to see if the Primary Clip should be used (the "normal sound")
        if (primaryWoodVoiceClip != null && Random.value < primaryVoicePlayChance)
        {
            clip = primaryWoodVoiceClip;
        }
        // 2. Otherwise, select a random clip from the array (the "strange one")
        else if (woodVoiceClips != null && woodVoiceClips.Length > 0)
        {
            int idx = Random.Range(0, woodVoiceClips.Length);
            clip = woodVoiceClips[idx];
        }
        // 3. Fallback if no clips are set
        else
        {
            clip = null;
            pitch = 1f;
            volume = 1f;
            return;
        }

        // Random pitch
        pitch = Random.Range(minPitch, maxPitch);

        // Combine: woodClipsVolume × master × ducking
        volume = woodClipsVolume * masterVolume * currentDuckingMultiplier;
        // --------------------------------
    }

    /// <summary>
    /// Gets the volume multiplier for a specific clip (includes per-voice volume + master + ducking).
    /// </summary>
    private float GetVolumeForClip(AudioClip clip)
    {
        float perVoiceVolume = 1f;

        // Find the index of this clip in the array
        for (int i = 0; i < allVoiceClips.Length; i++)
        {
            if (allVoiceClips[i] == clip)
            {
                // Get the corresponding volume (default to 1.0 if array is too short)
                if (i < voiceVolumes.Length)
                {
                    perVoiceVolume = voiceVolumes[i];
                }
                break;
            }
        }

        // Combine: per-voice × master × ducking
        return perVoiceVolume * masterVolume * currentDuckingMultiplier;
    }

    /// <summary>
    /// Gets the assigned voice for a block. Returns null if not assigned.
    /// </summary>
    public bool GetBlockVoice(GameObject block, out AudioClip clip, out float pitch)
    {
        if (blockVoiceAssignments.TryGetValue(block, out VoiceAssignment assignment))
        {
            clip = assignment.clip;
            pitch = assignment.pitch;
            return true;
        }

        clip = null;
        pitch = 1f;
        return false;
    }

    /// <summary>
    /// Releases a block's voice assignment when destroyed.
    /// </summary>
    public void ReleaseBlockVoice(GameObject block)
    {
        if (blockVoiceAssignments.Remove(block))
        {
            if (showDebugLogs)
            {
                Debug.Log($"VoiceManager: Released voice assignment for block");
            }
        }
    }

    /// <summary>
    /// Updates the voice pool size based on current block count.
    /// More blocks = more sounds in the pool.
    /// </summary>
    private void UpdateVoicePool()
    {
        int activeBlockCount = GetActiveBlockCount();

        // Calculate how many voices should be available based on block count
        int desiredPoolSize = Mathf.Clamp(
            minPoolSize + (activeBlockCount / blocksPerVoice),
            minPoolSize,
            maxPoolSize
        );

        // Update if size changed
        if (desiredPoolSize != currentPoolSize)
        {
            int oldSize = currentPoolSize;
            currentPoolSize = desiredPoolSize;

            if (showDebugLogs)
            {
                Debug.Log($"VoiceManager: Pool resized from {oldSize} to {currentPoolSize} voices ({activeBlockCount} active blocks)");
            }
        }
    }

    /// <summary>
    /// Gets the current count of active blocks.
    /// </summary>
    private int GetActiveBlockCount()
    {
        // Note: Assuming HumanClick is present on all blocks that use the VoiceManager's pool.
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        return allBlocks.Length;
    }

    /// <summary>
    /// Returns how many different sounds are currently available.
    /// </summary>
    public int GetCurrentPoolSize()
    {
        return currentPoolSize;
    }

    /// <summary>
    /// Returns how many total clips are loaded.
    /// </summary>
    public int GetTotalClipCount()
    {
        return allVoiceClips != null ? allVoiceClips.Length : 0;
    }
}