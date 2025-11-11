using UnityEngine;

public class Resources : MonoBehaviour
{
    public static Resources Instance { get; private set; }

    [Header("Resource Tracking")]
    [SerializeField] private int currentFood = 0;

    [Header("Starvation Settings")]
    [SerializeField] private bool enableStarvation = true;
    [Tooltip("How many childless blocks to kill per starvation check")]
    [SerializeField] private int blocksToKillPerCheck = 1;
    [Tooltip("Check for starvation every X frames")]
    [SerializeField] private int starvationCheckInterval = 10;

    private int frameCounter = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Debug key: Press F to manually add 1 Food for testing
        if (Input.GetKeyDown(KeyCode.F))
        {
            AddFood(1);
            Debug.Log($"[DEBUG] Manual food added. Current Food: {currentFood}");
        }

        // Check for starvation every X frames
        if (enableStarvation && currentFood < 5)
        {
            frameCounter++;
            if (frameCounter >= starvationCheckInterval)
            {
                CheckStarvation();
                frameCounter = 0;
            }
        }
        else
        {
            frameCounter = 0; // Reset counter when we have food
        }
    }

    /// <summary>
    /// Adds food to the total resource pool
    /// </summary>
    public void AddFood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Tried to add non-positive food amount: {amount}");
            return;
        }

        currentFood += amount;

        if (showDebugLogs)
        {
            Debug.Log($"Food added: +{amount} | Total Food: {currentFood}");
        }
    }

    /// <summary>
    /// Gets the current total food count
    /// </summary>
    public int GetCurrentFood()
    {
        return currentFood;
    }

    /// <summary>
    /// For future use: Subtracts food from the pool
    /// </summary>
    public bool TrySpendFood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Tried to spend non-positive food amount: {amount}");
            return false;
        }

        if (currentFood >= amount)
        {
            currentFood -= amount;

            if (showDebugLogs)
            {
                Debug.Log($"Food spent: -{amount} | Total Food: {currentFood}");
            }

            return true;
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"Not enough food! Need: {amount}, Have: {currentFood}");
            }

            return false;
        }
    }

    /// <summary>
    /// For future use: Checks if we can afford an amount without spending it
    /// </summary>
    public bool CanAfford(int amount)
    {
        return currentFood >= amount;
    }

    /// <summary>
    /// Kills childless blocks when food is at or below zero
    /// </summary>
    void CheckStarvation()
    {
        // Find all blocks with HumanClick component
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();

        if (allBlocks.Length == 0)
        {
            return; // No blocks to kill
        }

        // Find childless blocks (blocks with no children in HumanClick)
        System.Collections.Generic.List<HumanClick> childlessBlocks = new System.Collections.Generic.List<HumanClick>();

        foreach (HumanClick block in allBlocks)
        {
            // Check if this block has any children using HumanClick's child tracking
            bool hasChildren = block.northChild != null ||
                              block.southChild != null ||
                              block.eastChild != null ||
                              block.westChild != null;

            if (!hasChildren)
            {
                childlessBlocks.Add(block);
            }
        }

        if (childlessBlocks.Count == 0)
        {
            if (showDebugLogs)
            {
                Debug.Log("Starvation check: No childless blocks to kill");
            }
            return;
        }

        // Kill blocks (up to the limit) and collect refunds
        int blocksKilled = 0;
        int totalRefund = 0;

        for (int i = 0; i < Mathf.Min(blocksToKillPerCheck, childlessBlocks.Count); i++)
        {
            if (childlessBlocks[i] != null)
            {
                // Get the block's cost for refund
                BlockType blockType = childlessBlocks[i].GetBlockType();
                if (blockType != null)
                {
                    totalRefund += blockType.cost;
                }

                if (showDebugLogs)
                {
                    Debug.Log($"Starvation: Killing childless block at {childlessBlocks[i].transform.position}");
                }

                childlessBlocks[i].Die();
                blocksKilled++;
            }
        }

        // Add the refund after killing blocks
        if (totalRefund > 0)
        {
            AddFood(totalRefund);

            // ALWAYS log this, even if showDebugLogs is false
            Debug.Log($"⚠️ STARVATION REFUND: +{totalRefund} food from {blocksKilled} blocks | New Total: {currentFood}");
        }

        if (showDebugLogs)
        {
            Debug.Log($"Starvation check complete: Killed {blocksKilled} childless blocks. {childlessBlocks.Count - blocksKilled} childless blocks remain.");
        }
    }
}