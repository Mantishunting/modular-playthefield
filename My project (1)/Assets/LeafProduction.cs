using UnityEngine;
using System.Collections;

public class LeafProduction : MonoBehaviour
{
    [Header("Production Settings")]
    [SerializeField] private float productionInterval = 5f;
    [SerializeField] private int foodPerProduction = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    void Start()
    {
        // Start the production loop immediately
        StartCoroutine(ProductionLoop());

        if (showDebugLogs)
        {
            Debug.Log($"LeafProduction started on block at {transform.position}");
        }
    }

    IEnumerator ProductionLoop()
    {
        while (true)
        {
            // Wait for the production interval
            yield return new WaitForSeconds(productionInterval);

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

        // Add food to the resource pool
        Resources.Instance.AddFood(foodPerProduction);

        if (showDebugLogs)
        {
            Debug.Log($"Leaf block at {transform.position} produced {foodPerProduction} food!");
        }
    }

    void OnDestroy()
    {
        // Coroutine automatically stops when GameObject is destroyed
        // This is just for debug logging
        if (showDebugLogs)
        {
            Debug.Log($"LeafProduction stopped on block at {transform.position}");
        }
    }
}