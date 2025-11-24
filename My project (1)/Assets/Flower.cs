using UnityEngine;

public class Flower : MonoBehaviour
{
    public static event System.Action<Flower> OnFlowerVisited;

    [Header("Detection Mode")]
    [Tooltip("If true, bees pass through. If false, bees bounce off.")]
    [SerializeField] private bool usesTrigger = true;

    [Header("State")]
    [SerializeField] private int timesVisited = 0;

    public int TimesVisited => timesVisited;

    //  Audio fields
    private AudioSource audioSource;
    private AudioClip flowerClip;
    private float flowerPitch;
    private float flowerVolume;

    void Start()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = usesTrigger;
        }

        //  Audio Setup
        if (VoiceManager.Instance != null)
        {
            // 1. Add AudioSource automatically
            audioSource = gameObject.AddComponent<AudioSource>();

            // 2. Assign unique voice
            VoiceManager.Instance.AssignFlowerVoice(
                gameObject,
                out flowerClip,
                out flowerPitch,
                out flowerVolume
            );

            // 3. Configure AudioSource
            audioSource.pitch = flowerPitch;
            audioSource.playOnAwake = false;
        }
        else
        {
            Debug.LogWarning("Flower: VoiceManager not found. Cannot assign audio.");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (usesTrigger) return;

        if (collision.gameObject.GetComponent<Bee>() != null)
        {
            RegisterVisit();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!usesTrigger) return;

        if (other.GetComponent<Bee>() != null)
        {
            RegisterVisit();
        }
    }

    void RegisterVisit()
    {
        timesVisited++;

        if (BeeVisitTracker.Instance != null)
        {
            //  CRITICAL FIX: UNCOMMENTED this line to register the global visit count!
            BeeVisitTracker.Instance.RegisterVisit();
        }

        OnFlowerVisited?.Invoke(this);

        //  Playback
        if (flowerClip != null && audioSource != null)
        {
            // Use PlayOneShot with the volume multiplier
            audioSource.PlayOneShot(flowerClip, flowerVolume);
        }

        Debug.Log($"Flower visited! {gameObject.name} - Local: {timesVisited}");
    }
}