using UnityEngine;
using System.Collections;

public class LeafProduction : MonoBehaviour
{
    [Header("Sun Settings")]
    [SerializeField] private bool requireSunlight = true;
    [Tooltip("If false, produces food constantly like before sun system")]

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private HumanClick humanClick;
    private BlockType myBlockType;
    private Sun sun;

    private bool isProducing = false;
    private float lightCheckInterval = 0.25f; // Check 4 times per second

    // Lifespan tracking
    private float timeAlive = 0f;
    private bool isAlive = true;

    void Start()
    {
        // Get references
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

        // Find the Sun
        sun = FindObjectOfType<Sun>();
        if (sun == null)
        {
            Debug.LogWarning("LeafProduction: No Sun found in scene! Production will not work.");
        }

        // Only start if this block type produces resources
        if (myBlockType.producesResources)
        {
            StartCoroutine(LightCheckingLoop());

            if (showDebugLogs)
            {
                Debug.Log($"LeafProduction started on {myBlockType.blockName} at {transform.position}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"Block type {myBlockType.blockName} does not produce resources, production disabled.");
            }
        }

        // Start lifespan timer if this block has a lifespan
        if (myBlockType.lifespanSeconds > 0)
        {
            StartCoroutine(LifespanTimer());

            if (showDebugLogs)
            {
                Debug.Log($"{myBlockType.blockName} at {transform.position} will die after {myBlockType.lifespanSeconds} seconds");
            }
        }
    }

    /// <summary>
    /// Loop A: Continuously checks if this leaf is lit by the sun (4x per second)
    /// </summary>
    IEnumerator LightCheckingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(lightCheckInterval);

            // Check if we're lit
            bool isLit = IsSunShining();

            // If lit and not already producing, start production
            if (isLit && !isProducing)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Leaf at {transform.position} detected sunlight, starting production");
                }

                StartCoroutine(ProductionLoop());
            }
        }
    }

    /// <summary>
    /// Loop B: Production timer - produces food at intervals while lit
    /// </summary>
    IEnumerator ProductionLoop()
    {
        isProducing = true;

        while (true)
        {
            // Wait for the production interval from BlockType
            yield return new WaitForSeconds(myBlockType.productionRate);

            // Check if we're still lit
            bool isStillLit = IsSunShining();

            if (isStillLit)
            {
                // Produce food
                ProduceFood();

                // Continue loop (will check again after next interval)
            }
            else
            {
                // Not lit anymore, stop producing
                if (showDebugLogs)
                {
                    Debug.Log($"Leaf at {transform.position} no longer lit, stopping production");
                }

                isProducing = false;
                yield break; // Exit this loop, LightCheckingLoop continues
            }
        }
    }

    /// <summary>
    /// Checks if this leaf is currently receiving sunlight
    /// Casts a ray from leaf toward sun to check for blocking
    /// </summary>
    bool IsSunShining()
    {
        // If sun checking is disabled, always return true (old behavior)
        if (!requireSunlight)
        {
            return true;
        }

        if (sun == null)
        {
            return false; // No sun, no light
        }

        // Get direction FROM this leaf TOWARD the sun
        Vector3 toSun = (sun.transform.position - transform.position).normalized;

        // Calculate raycast distance (sun's orbit radius + buffer)
        float rayDistance = Vector3.Distance(transform.position, Vector3.zero) + sun.GetOrbitRadius() + 100f;

        // Offset the ray origin by 0.6 units so it clears our own 1x1 collider
        Vector3 rayOrigin = transform.position + (toSun * 0.6f);

        if (showDebugLogs)
        {
            Debug.Log($"Raycast from {rayOrigin} toward sun at {sun.transform.position}, direction: {toSun}, distance: {rayDistance}");
        }

        // Cast ray from this leaf toward the sun
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, toSun, rayDistance);

        if (showDebugLogs)
        {
            if (hit.collider != null)
            {
                Debug.Log($"Raycast HIT something: {hit.collider.gameObject.name} at {hit.point}");
            }
            else
            {
                Debug.Log($"Raycast hit NOTHING");
            }
        }

        if (hit.collider != null)
        {
            // Ray hit something - check if it's another block
            HumanClick hitBlock = hit.collider.GetComponent<HumanClick>();

            if (hitBlock != null && hitBlock != humanClick)
            {
                // Hit another block - we're blocked!
                if (showDebugLogs)
                {
                    Debug.Log($"Leaf at {transform.position} blocked by block at {hitBlock.transform.position}");
                }
                return false;
            }
        }

        // No blocking, we're lit!
        return true;
    }

    void ProduceFood()
    {
        // Safety check: Make sure Resources exists
        if (Resources.Instance == null)
        {
            Debug.LogError("Resources.Instance is null! Cannot produce food.");
            return;
        }

        // Add food to the resource pool (always 1 for now)
        Resources.Instance.AddFood(1);

        if (showDebugLogs)
        {
            Debug.Log($"{myBlockType.blockName} block at {transform.position} produced 1 food!");
        }
    }

    /// <summary>
    /// Tracks how long this block has been alive and dies when lifespan expires
    /// </summary>
    IEnumerator LifespanTimer()
    {
        while (isAlive)
        {
            timeAlive += Time.deltaTime;

            // Check if lifespan has expired
            if (timeAlive >= myBlockType.lifespanSeconds)
            {
                DieOfOldAge();
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Handles death of this block due to old age
    /// </summary>
    void DieOfOldAge()
    {
        isAlive = false;

        if (showDebugLogs)
        {
            Debug.Log($"{myBlockType.blockName} at {transform.position} died of old age after {timeAlive:F1} seconds");
        }

        // Call Die() which handles killing this block and all descendants
        if (humanClick != null)
        {
            humanClick.Die();
        }
    }

    void OnDestroy()
    {
        // Both coroutines automatically stop when GameObject is destroyed
        if (showDebugLogs && myBlockType != null)
        {
            Debug.Log($"LeafProduction stopped on {myBlockType.blockName} at {transform.position}");
        }
    }
}