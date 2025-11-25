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

    // **[1] Cooldown settings**
    [Header("Cooldown Settings")]
    [Tooltip("Seconds before this flower can be visited again.")]
    [SerializeField] private float visitCooldown = 2f;

    // **[2] Cooldown lock**
    private bool onCooldown = false;

    // Audio fields
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

        // Audio Setup
        if (VoiceManager.Instance != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();

            VoiceManager.Instance.AssignFlowerVoice(
                gameObject,
                out flowerClip,
                out flowerPitch,
                out flowerVolume
            );

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
        // **[3] Block repeated hits during cooldown**
        if (onCooldown) return;

        // **[4] Enter cooldown**
        onCooldown = true;
        Invoke(nameof(ResetCooldown), visitCooldown);

        timesVisited++;

        if (BeeVisitTracker.Instance != null)
        {
            BeeVisitTracker.Instance.RegisterVisit();
        }

        OnFlowerVisited?.Invoke(this);

        if (flowerClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(flowerClip, flowerVolume);
        }

        Debug.Log($"Flower visited! {gameObject.name} - Local: {timesVisited}");
    }

    // **[5] Cooldown reset function**
    void ResetCooldown()
    {
        onCooldown = false;
    }
}
