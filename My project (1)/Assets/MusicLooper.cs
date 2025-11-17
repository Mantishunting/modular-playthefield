using UnityEngine;

/// <summary>
/// Plays a music track on a loop with configurable silence before and after.
/// Automatically ducks leaf production sounds when music plays.
/// </summary>
public class MusicLooper : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicTrack;

    [Header("Timing")]
    [Tooltip("Silence before the music starts (in seconds)")]
    [SerializeField] private float delayBeforeMusic = 120f; // 2 minutes default
    
    [Tooltip("Silence after the music ends before looping (in seconds)")]
    [SerializeField] private float delayAfterMusic = 0f;

    [Header("Auto-Start")]
    [SerializeField] private bool startOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float timer = 0f;
    private bool isPlaying = false;
    private bool isWaitingBefore = true;
    private bool isWaitingAfter = false;

    void Start()
    {
        // Get or create audio source
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configure audio source
        musicSource.loop = false; // We handle looping manually
        musicSource.playOnAwake = false;

        if (musicTrack != null)
        {
            musicSource.clip = musicTrack;
        }

        if (startOnAwake)
        {
            StartLoop();
        }
    }

    void Update()
    {
        if (!isPlaying && !isWaitingBefore && !isWaitingAfter)
        {
            return; // System not started
        }

        timer += Time.deltaTime;

        // Waiting before music starts
        if (isWaitingBefore)
        {
            if (timer >= delayBeforeMusic)
            {
                PlayMusic();
            }
        }
        // Music is playing - check if finished
        else if (isPlaying)
        {
            if (!musicSource.isPlaying)
            {
                OnMusicEnded();
            }
        }
        // Waiting after music ended
        else if (isWaitingAfter)
        {
            if (timer >= delayAfterMusic)
            {
                RestartLoop();
            }
        }
    }

    void PlayMusic()
    {
        if (musicTrack == null)
        {
            Debug.LogWarning("MusicLooper: No music track assigned!");
            return;
        }

        isWaitingBefore = false;
        isPlaying = true;
        timer = 0f;

        musicSource.Play();

        // Tell VoiceManager to duck leaf sounds
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.SetMusicPlaying(true);
        }

        if (showDebugLogs)
        {
            Debug.Log($"MusicLooper: Music started (track length: {musicTrack.length:F1}s)");
        }
    }

    void OnMusicEnded()
    {
        isPlaying = false;
        timer = 0f;

        // Tell VoiceManager to restore leaf volume
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.SetMusicPlaying(false);
        }

        if (showDebugLogs)
        {
            Debug.Log($"MusicLooper: Music ended, waiting {delayAfterMusic:F1}s before loop");
        }

        // Start waiting period after music
        if (delayAfterMusic > 0)
        {
            isWaitingAfter = true;
        }
        else
        {
            RestartLoop();
        }
    }

    void RestartLoop()
    {
        isWaitingAfter = false;
        isWaitingBefore = true;
        timer = 0f;

        if (showDebugLogs)
        {
            Debug.Log($"MusicLooper: Restarting loop, waiting {delayBeforeMusic:F1}s before next play");
        }
    }

    /// <summary>
    /// Manually start the music loop
    /// </summary>
    public void StartLoop()
    {
        isWaitingBefore = true;
        isPlaying = false;
        isWaitingAfter = false;
        timer = 0f;

        if (showDebugLogs)
        {
            Debug.Log($"MusicLooper: Loop started, waiting {delayBeforeMusic:F1}s before first play");
        }
    }

    /// <summary>
    /// Stop the music loop
    /// </summary>
    public void StopLoop()
    {
        isWaitingBefore = false;
        isPlaying = false;
        isWaitingAfter = false;

        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        // Restore leaf volume
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.SetMusicPlaying(false);
        }

        if (showDebugLogs)
        {
            Debug.Log("MusicLooper: Loop stopped");
        }
    }

    /// <summary>
    /// Play music immediately (skip current wait)
    /// </summary>
    public void PlayNow()
    {
        timer = 0f;
        PlayMusic();
    }

    /// <summary>
    /// Get time until next music play
    /// </summary>
    public float GetTimeUntilNextPlay()
    {
        if (isWaitingBefore)
        {
            return delayBeforeMusic - timer;
        }
        else if (isPlaying)
        {
            return musicTrack != null ? musicTrack.length - musicSource.time : 0f;
        }
        else if (isWaitingAfter)
        {
            return delayAfterMusic - timer;
        }
        return 0f;
    }

    /// <summary>
    /// Check if music is currently playing
    /// </summary>
    public bool IsMusicPlaying()
    {
        return isPlaying;
    }
}
