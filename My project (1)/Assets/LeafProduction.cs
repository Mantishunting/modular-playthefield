using UnityEngine;
using System.Collections;

public class LeafProduction : MonoBehaviour
{
    [Header("Sun Settings")]
    [SerializeField] private bool requireSunlight = true;

    [Header("Rhythm Settings")]
    [Tooltip("Beats Per Minute for the start delay")]
    [SerializeField] private float bpm = 146f;
    [Tooltip("Max delay in seconds allowed before starting")]
    [SerializeField] private float maxDelaySeconds = 3.0f;

    [Header("Raycast Settings")]
    [SerializeField] private Vector2 boxCastSize = new Vector2(0.6f, 0.6f);
    [SerializeField] private float originOffset = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private HumanClick humanClick;
    private BlockType myBlockType;
    private Sun sun;
    private BlockAudioPlayer audioPlayer;

    private bool isProducing = false;
    private float lightCheckInterval = 0.25f;

    private float timeAlive = 0f;
    private bool isAlive = true;

    void Start()
    {
        humanClick = GetComponent<HumanClick>();
        if (humanClick == null)
        {
            Debug.LogError("LeafProduction requires HumanClick component!");
            return;
        }

        myBlockType = humanClick.GetBlockType();
        if (myBlockType == null)
        {
            Debug.LogError("LeafProduction: BlockType is null!");
            return;
        }

        sun = FindObjectOfType<Sun>();
        if (sun == null)
        {
            Debug.LogWarning("LeafProduction: No Sun found in scene! Production will not work.");
        }

        audioPlayer = GetComponent<BlockAudioPlayer>();

        if (myBlockType.producesResources)
        {
            StartCoroutine(LightCheckingLoop());
            if (showDebugLogs) Debug.Log($"LeafProduction started on {myBlockType.blockName}");
        }

        if (myBlockType.lifespanSeconds > 0)
        {
            StartCoroutine(LifespanTimer());
        }
    }

    IEnumerator LightCheckingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(lightCheckInterval);

            bool isLit = IsSunShining();

            if (isLit && !isProducing)
            {
                if (showDebugLogs) Debug.Log($"Leaf at {transform.position} detected sunlight, queueing production");
                StartCoroutine(ProductionLoop());
            }
        }
    }

    IEnumerator ProductionLoop()
    {
        isProducing = true;

        // --- RHYTHM DELAY LOGIC STARTS HERE ---

        // 1. Calculate duration of a single beat (60 / 146 = approx 0.41s)
        float singleBeatDuration = 60f / bpm;

        // 2. Define acceptable multipliers (Fractions and Whole numbers)
        // 0.5 = 8th note, 1 = quarter note, 2 = half note, 4 = whole note, etc.
        float[] beatMultipliers = { 0.5f, 1f, 2f, 4f, 6f, 8f };

        // 3. Pick a random multiplier
        float selectedMult = beatMultipliers[Random.Range(0, beatMultipliers.Length)];

        // 4. Calculate total delay
        float delayTime = singleBeatDuration * selectedMult;

        // 5. Ensure we don't exceed the user's hard limit (3 seconds)
        // If 8 beats is ~3.28s, this checks if we need to cap it.
        if (delayTime > maxDelaySeconds)
        {
            // Fallback to a safe number like 4 beats if 8 was too long
            delayTime = singleBeatDuration * 4f;
        }

        if (showDebugLogs)
        {
            Debug.Log($"Leaf waiting for {selectedMult} beats ({delayTime:F2}s) before starting.");
        }

        // 6. Wait for the rhythm
        yield return new WaitForSeconds(delayTime);

        // --- RHYTHM DELAY LOGIC ENDS HERE ---

        while (true)
        {
            // Note: This waits for the Production Rate defined in the ScriptableObject.
            // If you want the production rate ITSELF to also lock to BPM, 
            // we would need to change this line too. For now, I left it as per your file.
            yield return new WaitForSeconds(myBlockType.productionRate);

            bool isStillLit = IsSunShining();

            if (isStillLit)
            {
                ProduceFood();
            }
            else
            {
                if (showDebugLogs) Debug.Log($"Leaf at {transform.position} no longer lit, stopping production");
                isProducing = false;
                yield break;
            }
        }
    }

    bool IsSunShining()
    {
        if (!requireSunlight) return true;
        if (sun == null) return false;

        Vector3 toSun = -sun.GetLightDirection();
        float rayDistance = Vector3.Distance(transform.position, Vector3.zero) + sun.GetOrbitRadius() + 100f;

        Vector3 rayOrigin = transform.position + (toSun * originOffset);

        // Blocking layers
        int blockingLayers = LayerMask.GetMask("Default");
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxCastSize, 0f, toSun, rayDistance, blockingLayers);

        if (showDebugLogs)
        {
            // (Debug drawing code omitted for brevity, same as original)
        }

        if (hit.collider != null)
        {
            bool hitIsSelf = hit.collider.transform.IsChildOf(this.transform);
            if (!hitIsSelf) return false;
        }

        return true;
    }

    void ProduceFood()
    {
        if (Resources.Instance == null) return;

        Resources.Instance.AddFood(myBlockType.productionAmount);

        // This is where the sound plays
        if (audioPlayer != null)
        {
            audioPlayer.PlayProductionSound();
        }

        if (showDebugLogs)
        {
            Debug.Log($"{myBlockType.blockName} produced food!");
        }
    }

    IEnumerator LifespanTimer()
    {
        while (isAlive)
        {
            timeAlive += Time.deltaTime;
            if (timeAlive >= myBlockType.lifespanSeconds)
            {
                DieOfOldAge();
                yield break;
            }
            yield return null;
        }
    }

    void DieOfOldAge()
    {
        isAlive = false;
        if (humanClick != null) humanClick.Die();
    }

    void OnDestroy()
    {
        // Cleanup if needed
    }
}