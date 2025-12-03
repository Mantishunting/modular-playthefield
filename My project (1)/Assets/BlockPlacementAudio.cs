using UnityEngine;

public class BlockPlacementSound : MonoBehaviour
{
    [Header("Placement Sounds (4 variations recommended)")]
    [SerializeField] private AudioClip[] placementSounds;   // <--- NOW AN ARRAY

    [Header("Volume")]
    [SerializeField] private float baseVolume = 1.0f;

    [Header("Pitch Randomisation")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 0.10f;
    private float cooldownTimer = 0f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        HumanClick.OnBlockPlaced += HandleBlockPlaced;
    }

    void OnDisable()
    {
        HumanClick.OnBlockPlaced -= HandleBlockPlaced;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void HandleBlockPlaced(BlockType type)
    {
        if (cooldownTimer > 0f) return;
        if (placementSounds == null || placementSounds.Length == 0) return;

        // Pick one of the 4 sounds at random
        AudioClip selectedClip = placementSounds[Random.Range(0, placementSounds.Length)];
        if (selectedClip == null) return;

        // Pitch randomisation
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        // Master ducked volume (same as leaves/flowers)
        float duckedVolume = baseVolume;
        if (VoiceManager.Instance != null)
            duckedVolume *= VoiceManager.Instance.GetCurrentMasterVolume();

        // Play sound
        audioSource.PlayOneShot(selectedClip, duckedVolume);

        cooldownTimer = cooldownTime;
    }
}
