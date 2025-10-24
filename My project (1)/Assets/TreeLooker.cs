using UnityEngine;
using System.Collections.Generic;

public class TreeLooker : MonoBehaviour
{
    [Header("Debug Testing")]
    [SerializeField] private bool enableDebugKey = true;
    [SerializeField] private float testRadius = 3f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Debug test: Press L to check blocks near mouse position
        if (enableDebugKey && Input.GetKeyDown(KeyCode.L))
        {
            TestProximityAtMouse();
        }
    }

    void TestProximityAtMouse()
    {
        // Get mouse position in world space
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // Find blocks within radius
        List<HumanClick> nearbyBlocks = GetBlocksInRadius(mousePos, testRadius);

        // Log results
        Debug.Log($"=== TreeLooker Test at {mousePos} ===");
        Debug.Log($"Found {nearbyBlocks.Count} blocks within radius {testRadius}");

        // Count by type
        int woodCount = 0;
        int leafCount = 0;

        foreach (HumanClick block in nearbyBlocks)
        {
            BlockType blockType = block.GetBlockType();
            if (blockType != null)
            {
                if (blockType.blockName == "Wood")
                {
                    woodCount++;
                }
                else if (blockType.blockName == "Leaf")
                {
                    leafCount++;
                }
                Debug.Log($"  - {blockType.blockName} at {block.transform.position}");
            }
        }

        Debug.Log($"Summary: {woodCount} Wood, {leafCount} Leaf");
    }

    /// <summary>
    /// Finds all blocks within a given radius of a center position
    /// </summary>
    /// <param name="center">The center position to search from</param>
    /// <param name="radius">The radius to search within (in world units)</param>
    /// <returns>List of all blocks found within the radius</returns>
    public static List<HumanClick> GetBlocksInRadius(Vector3 center, float radius)
    {
        List<HumanClick> blocksInRange = new List<HumanClick>();
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();

        foreach (HumanClick block in allBlocks)
        {
            float distance = Vector3.Distance(block.transform.position, center);
            if (distance <= radius)
            {
                blocksInRange.Add(block);
            }
        }

        return blocksInRange;
    }
}