using UnityEngine;
using System.Collections;

public class BlockDailyCost : MonoBehaviour
{
    [Header("Upkeep Settings")]
    [Tooltip("How often to charge upkeep (in seconds). E.g., 30 = every 30 seconds")]
    [SerializeField] private float upkeepInterval = 30f;
    
    [Tooltip("Food cost per block per interval. E.g., 0.1 = 1 food per 10 blocks")]
    [SerializeField] private float costPerBlock = 0.1f;
    
    [Header("Penalties")]
    [Tooltip("What happens when you can't afford upkeep?")]
    [SerializeField] private UpkeepPenalty penaltyType = UpkeepPenalty.None;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private float timer = 0f;
    
    public enum UpkeepPenalty
    {
        None,           // Nothing happens, just can't pay
        StopProduction, // Leafs stop producing (future feature)
        KillBlocks      // Random blocks die (harsh!)
    }
    
    void Start()
    {
        if (showDebugLogs)
        {
            Debug.Log($"BlockDailyCost initialized: {costPerBlock} food per block every {upkeepInterval} seconds");
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= upkeepInterval)
        {
            ChargeUpkeep();
            timer = 0f;
        }
    }
    
    void ChargeUpkeep()
    {
        // Get total block count from HumanClick's static tracker
        int totalBlocks = GetTotalBlockCount();
        
        // Calculate upkeep cost
        int upkeepCost = Mathf.CeilToInt(totalBlocks * costPerBlock);
        
        if (upkeepCost <= 0)
        {
            if (showDebugLogs)
            {
                Debug.Log("No blocks, no upkeep cost");
            }
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"Upkeep Due: {totalBlocks} blocks × {costPerBlock} = {upkeepCost} food");
        }
        
        // Try to pay the upkeep
        bool paid = Resources.Instance.TrySpendFood(upkeepCost);
        
        if (!paid)
        {
            HandleUnpaidUpkeep(upkeepCost);
        }
    }
    
    void HandleUnpaidUpkeep(int amountOwed)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"Cannot afford upkeep! Owed: {amountOwed} food");
        }
        
        switch (penaltyType)
        {
            case UpkeepPenalty.None:
                // Just log it, no penalty
                break;
                
            case UpkeepPenalty.StopProduction:
                // Future: Could broadcast event to stop all leaf production
                if (showDebugLogs)
                {
                    Debug.Log("Penalty: Production would stop (not implemented yet)");
                }
                break;
                
            case UpkeepPenalty.KillBlocks:
                KillRandomBlocks(amountOwed);
                break;
        }
    }
    
    void KillRandomBlocks(int numberOfBlocksToKill)
    {
        // Find all blocks
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        
        if (allBlocks.Length == 0)
        {
            return;
        }
        
        // Kill random blocks (up to the amount owed or available blocks)
        int blocksToKill = Mathf.Min(numberOfBlocksToKill, allBlocks.Length);
        
        if (showDebugLogs)
        {
            Debug.LogWarning($"Penalty: Killing {blocksToKill} random blocks due to unpaid upkeep!");
        }
        
        // Shuffle and kill
        for (int i = 0; i < blocksToKill; i++)
        {
            int randomIndex = Random.Range(0, allBlocks.Length);
            if (allBlocks[randomIndex] != null)
            {
                allBlocks[randomIndex].Die();
            }
        }
    }
    
    /// <summary>
    /// Gets the total block count from HumanClick's static tracker
    /// Uses reflection since totalBlockCount is private
    /// </summary>
    int GetTotalBlockCount()
    {
        // Count all HumanClick objects as fallback
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        return allBlocks.Length;
    }
    
    /// <summary>
    /// Public method to manually trigger upkeep (for testing)
    /// </summary>
    public void TriggerUpkeepNow()
    {
        ChargeUpkeep();
        timer = 0f;
    }
}
