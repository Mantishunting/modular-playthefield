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

    [Header("Refund Settings")]
    [SerializeField] private bool enableDeathRefund = true;
    [SerializeField, Range(0f, 2f)] private float deathRefundMultiplier = 0.25f; // adjustable in Inspector
    [SerializeField] private bool showRefundLogs = false;

    // **1️⃣ New Section — Destruction Rules**
    [Header("Destruction Rules")]
    [Tooltip("If unchecked, players cannot right-click to destroy Wood blocks.")]
    [SerializeField] private bool allowPlayerDestroyWood = true;
    [Tooltip("If unchecked, starvation and upkeep penalties will never target Wood blocks.")]
    [SerializeField] private bool allowLowResourceDestroyWood = true;

    // **2️⃣ Public Read-only Properties**
    public bool AllowPlayerDestroyWood => allowPlayerDestroyWood;
    public bool AllowLowResourceDestroyWood => allowLowResourceDestroyWood;

    // Internal tracker
    private int lastBlockCount = -1;

    private int frameCounter = 0;

    private float lastFoodAmount = -1f;
    private float foodHoldTimer = 0f;
    [SerializeField] private float foodHoldThresholdSeconds = 5f; // how long food can stay unchanged before forcing a kill

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    public bool ShowDebugLogs => showDebugLogs;

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

    void Start()
    {
        // Initialise baseline for death-refund tracking
        lastBlockCount = FindObjectsOfType<HumanClick>().Length;
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

        // --- Detect stagnant food levels (failsafe for runaway upkeep) ---
        if (enableStarvation)
        {
            // If food hasn't changed since last frame
            if (Mathf.Approximately(currentFood, lastFoodAmount))
            {
                foodHoldTimer += Time.deltaTime;

                if (foodHoldTimer >= foodHoldThresholdSeconds)
                {
                    if (showDebugLogs)
                        Debug.LogWarning("⚠️ Food stagnant for 5 s — triggering starvation block kill.");

                    CheckStarvation();   // Re-use existing kill routine
                    foodHoldTimer = 0f;  // reset timer after a kill
                }
            }
            else
            {
                // Food changed — reset timer
                foodHoldTimer = 0f;
                lastFoodAmount = currentFood;
            }
        }

        // --- Refund on detected deaths (any cause) ---
        if (enableDeathRefund)
        {
            int liveBlocks = FindObjectsOfType<HumanClick>().Length;

            // First frame init safeguard
            if (lastBlockCount < 0) lastBlockCount = liveBlocks;

            // Positive delta = deaths since last frame
            int deaths = lastBlockCount - liveBlocks;
            if (deaths > 0)
            {
                // Choose whether to price “after” or “before” the deaths:
                // After-death price (uses current count):
                int woodPriceNow = GetCurrentWoodBuyCost();

                // If you'd rather use price BEFORE the deaths, use lastBlockCount instead:
                // int woodPriceBefore = 5 + lastBlockCount;

                int refundPer = Mathf.Max(0, Mathf.RoundToInt(woodPriceNow * deathRefundMultiplier));
                int totalRefund = refundPer * deaths;

                AddFood(totalRefund);

                if (showRefundLogs)
                    Debug.Log($"Refund: {deaths} deaths × {refundPer} = +{totalRefund} Food (multiplier {deathRefundMultiplier:F2})");
            }

            // Update baseline
            lastBlockCount = liveBlocks;
        }
    }

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

    public int GetCurrentFood()
    {
        return currentFood;
    }

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

    public bool CanAfford(int amount)
    {
        return currentFood >= amount;
    }

    void CheckStarvation()
    {
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();

        if (allBlocks.Length == 0)
        {
            return;
        }

        System.Collections.Generic.List<HumanClick> childlessBlocks = new System.Collections.Generic.List<HumanClick>();

        foreach (HumanClick block in allBlocks)
        {
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

        int blocksKilled = 0;
        int totalRefund = 0;

        for (int i = 0; i < Mathf.Min(blocksToKillPerCheck, childlessBlocks.Count); i++)
        {
            if (childlessBlocks[i] != null)
            {
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

        if (totalRefund > 0)
        {
            AddFood(totalRefund);
            Debug.Log($"⚠️ STARVATION REFUND: +{totalRefund} food from {blocksKilled} blocks | New Total: {currentFood}");
        }

        if (showDebugLogs)
        {
            Debug.Log($"Starvation check complete: Killed {blocksKilled} childless blocks. {childlessBlocks.Count - blocksKilled} childless blocks remain.");
        }
    }

    private int GetCurrentWoodBuyCost()
    {
        int liveBlocks = FindObjectsOfType<HumanClick>().Length;
        return 5 + liveBlocks;
    }
}
