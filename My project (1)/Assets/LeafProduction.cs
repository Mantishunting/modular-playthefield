using UnityEngine;
using System.Collections;

public class LeafProduction : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private HumanClick humanClick;
    private BlockType myBlockType;

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

        // Only start production if this block type produces resources
        if (myBlockType.producesResources)
        {
            StartCoroutine(ProductionLoop());

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

    IEnumerator ProductionLoop()
    {
        while (true)
        {
            // Wait for the production interval from BlockType
            yield return new WaitForSeconds(myBlockType.productionRate);

            // Produce food
            ProduceFood();
        }
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
        // Coroutine automatically stops when GameObject is destroyed
        // This is just for debug logging
        if (showDebugLogs && myBlockType != null)
        {
            Debug.Log($"LeafProduction stopped on {myBlockType.blockName} at {transform.position}");
        }
    }
}