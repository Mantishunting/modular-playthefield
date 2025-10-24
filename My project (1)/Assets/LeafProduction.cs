using UnityEngine;
using System.Collections;

public class LeafProduction : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private HumanClick humanClick;
    private BlockType myBlockType;
    private Sun sun;

    private bool isProducing = false;
    private float lightCheckInterval = 0.25f; // Check 4 times per second

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
    /// For now, just returns true (placeholder for Phase 3 raycast)
    /// </summary>
    bool IsSunShining()
    {
        // Phase 2: Simple placeholder - always return true for testing
        // Phase 3: Will add raycast blocking logic here

        if (sun == null)
        {
            return false; // No sun, no light
        }

        // TODO Phase 3: Add raycast check here
        return true; // For now, always lit
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

    void OnDestroy()
    {
        // Both coroutines automatically stop when GameObject is destroyed
        if (showDebugLogs && myBlockType != null)
        {
            Debug.Log($"LeafProduction stopped on {myBlockType.blockName} at {transform.position}");
        }
    }
}