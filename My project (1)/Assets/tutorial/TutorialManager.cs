using UnityEngine;

/// <summary>
/// Core tutorial system - manages tutorial progression by listening to game events
/// Subscribes to existing events from HumanClick, BeeVisitTracker, BlockTypeManager
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Steps")]
    [SerializeField] private TutorialStep[] tutorialSteps;
    
    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    
    [Header("Tracking")]
    private int currentStepIndex = 0;
    private bool tutorialActive = true;
    
    // Block count tracking for "increase by X" steps
    private int blockCountAtStepStart = 0;
    private int stemCountAtStepStart = 0;
    private int leafCountAtStepStart = 0;
    private int flowerCountAtStepStart = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    void Start()
    {
        if (tutorialUI == null)
        {
            Debug.LogError("TutorialManager: No TutorialUI assigned!");
            enabled = false;
            return;
        }
        
        if (tutorialSteps == null || tutorialSteps.Length == 0)
        {
            Debug.LogError("TutorialManager: No tutorial steps configured! Use TutorialStepGenerator to create them.");
            enabled = false;
            return;
        }
        
        // Disable old tutorial system if it exists
        TutorialMessages oldTutorial = FindObjectOfType<TutorialMessages>();
        if (oldTutorial != null)
        {
            oldTutorial.enabled = false;
            if (showDebugLogs) Debug.Log("TutorialManager: Disabled old TutorialMessages system");
        }
        
        // Subscribe to all game events
        SubscribeToEvents();
        
        // Start first step
        ShowStep(0);
    }
    
    void SubscribeToEvents()
    {
        // Block placement/deletion from HumanClick
        HumanClick.OnBlockPlaced += HandleBlockPlaced;
        HumanClick.OnBlockDestroyed += HandleBlockDestroyed;
        
        // Block type selection from BlockTypeManager
        BlockTypeManager.OnBlockTypeSelected += HandleBlockTypeSelected;
        
        // Bee visits from BeeVisitTracker
        BeeVisitTracker.OnVisitRegistered += HandleBeeVisit;
        
        if (showDebugLogs) Debug.Log("TutorialManager: Subscribed to all game events");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from all events to prevent memory leaks
        HumanClick.OnBlockPlaced -= HandleBlockPlaced;
        HumanClick.OnBlockDestroyed -= HandleBlockDestroyed;
        BlockTypeManager.OnBlockTypeSelected -= HandleBlockTypeSelected;
        BeeVisitTracker.OnVisitRegistered -= HandleBeeVisit;
    }
    
    // ===== STEP MANAGEMENT =====
    
    void ShowStep(int stepIndex)
    {
        if (stepIndex >= tutorialSteps.Length)
        {
            EndTutorial();
            return;
        }
        
        currentStepIndex = stepIndex;
        TutorialStep step = tutorialSteps[stepIndex];
        
        if (showDebugLogs) 
            Debug.Log($"TutorialManager: Showing step {stepIndex}: {step.messageText}");
        
        // Tell UI to display this step
        tutorialUI.DisplayStep(step);
        
        // Store current counts if this step tracks increases
        if (step.triggerType == TriggerType.BlockCountIncrease)
        {
            blockCountAtStepStart = GetTotalBlockCount();
            stemCountAtStepStart = GetBlockCountOfType("Wood");
            leafCountAtStepStart = GetBlockCountOfType("Leaf");
            flowerCountAtStepStart = GetBlockCountOfType("Flower");
            
            if (showDebugLogs)
                Debug.Log($"Step start counts - Total:{blockCountAtStepStart} Stem:{stemCountAtStepStart} Leaf:{leafCountAtStepStart} Flower:{flowerCountAtStepStart}");
        }
        
        // Special handling for certain steps
        HandleSpecialStepSetup(stepIndex);
    }
    
    void HandleSpecialStepSetup(int stepIndex)
    {
        // Step 14 (index 13): Animate leaf transparency
        if (stepIndex == 13)
        {
            // TODO: Trigger leaf transparency animation
            // You'll need to hook this up to your LeafProduction or visual system
            if (showDebugLogs) Debug.Log("TODO: Animate leaf transparency for step 14 (index 13)");
        }
        
        // Step 15 (index 14): Spawn/identify shaded leaf
        if (stepIndex == 14)
        {
            // TODO: Spawn or mark a shaded leaf for demonstration
            if (showDebugLogs) Debug.Log("TODO: Spawn shaded leaf for step 15 (index 14)");
        }
    }
    
    void AdvanceStep()
    {
        ShowStep(currentStepIndex + 1);
    }
    
    void EndTutorial()
    {
        tutorialActive = false;
        tutorialUI.Hide();
        
        if (showDebugLogs) Debug.Log("Tutorial complete! Waiting for bee visit to load next level...");
        
        // The game's normal win condition (BeeVisitTracker) will handle level progression
    }
    
    // ===== EVENT HANDLERS =====
    
    void HandleBlockPlaced(BlockType blockType)
    {
        if (!tutorialActive) return;
        if (blockType == null) return;
        
        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (showDebugLogs)
            Debug.Log($"TutorialManager: Block placed ({blockType.blockName}) on step {currentStepIndex}");
        
        // Check if this placement satisfies current step
        if (currentStep.triggerType == TriggerType.BlockCountIncrease)
        {
            CheckBlockCountIncrease(currentStep, blockType);
        }
    }
    
    void CheckBlockCountIncrease(TutorialStep step, BlockType placedType)
    {
        int currentTotal = GetTotalBlockCount();
        int currentStems = GetBlockCountOfType("Wood");
        int currentLeaves = GetBlockCountOfType("Leaf");
        int currentFlowers = GetBlockCountOfType("Flower");
        
        int totalIncrease = currentTotal - blockCountAtStepStart;
        int stemIncrease = currentStems - stemCountAtStepStart;
        int leafIncrease = currentLeaves - leafCountAtStepStart;
        int flowerIncrease = currentFlowers - flowerCountAtStepStart;
        
        if (showDebugLogs)
            Debug.Log($"Count increases - Total:+{totalIncrease} Stem:+{stemIncrease} Leaf:+{leafIncrease} Flower:+{flowerIncrease}");
        
        bool shouldAdvance = false;
        
        // Check which step we're on and what it requires
        switch (currentStepIndex)
        {
            case 1: // Step 2: First stem placement
                shouldAdvance = stemIncrease >= 1 && placedType.blockName == "Wood";
                break;
            
            case 2: // Step 3: Another stem block
                shouldAdvance = stemIncrease >= 1 && placedType.blockName == "Wood";
                break;
                
            case 3: // Step 4: Insert stem (teaches insertion)
                shouldAdvance = totalIncrease >= 1;
                break;
                
            case 4: // Step 5: Click and hold (3 blocks)
                shouldAdvance = totalIncrease >= 3;
                break;
                
            case 8: // Step 9: Two leaves on both sides
                shouldAdvance = leafIncrease >= 2 && placedType.blockName == "Leaf";
                break;
                
            case 17: // Step 18: Place a flower
                shouldAdvance = flowerIncrease >= 1 && placedType.blockName == "Flower";
                break;
                
            default:
                // Other steps use different trigger types (BlockCountDecrease, LeafButtonClicked, etc.)
                // This is expected and not an error
                break;
        }
        
        if (shouldAdvance)
        {
            if (showDebugLogs) Debug.Log($"Step {currentStepIndex} complete: Block count requirement met");
            AdvanceStep();
        }
    }
    
    void HandleBlockDestroyed(BlockType blockType)
    {
        if (!tutorialActive) return;
        
        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (showDebugLogs)
            Debug.Log($"TutorialManager: Block destroyed ({blockType?.blockName}) on step {currentStepIndex}");
        
        if (currentStep.triggerType == TriggerType.BlockCountDecrease)
        {
            // Step 6 (index 5): First deletion tutorial
            // Step 16 (index 15): Delete shaded leaf
            if (showDebugLogs) Debug.Log($"Step {currentStepIndex} complete: Block deleted");
            AdvanceStep();
        }
    }
    
    void HandleBlockTypeSelected(BlockType blockType)
    {
        if (!tutorialActive) return;
        if (blockType == null) return;
        
        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (showDebugLogs)
            Debug.Log($"TutorialManager: Block type selected ({blockType.blockName}) on step {currentStepIndex}");
        
        // Step 7 (index 6): Switching to Leaf advances
        if (currentStep.triggerType == TriggerType.LeafButtonClicked)
        {
            if (blockType.blockName == "Leaf")
            {
                if (showDebugLogs) Debug.Log("Step 7 (index 6) complete: Switched to Leaf");
                AdvanceStep();
            }
        }
    }
    
    void HandleBeeVisit(int totalVisits)
    {
        if (!tutorialActive) return;
        
        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (showDebugLogs)
            Debug.Log($"TutorialManager: Bee visit registered (total: {totalVisits}) on step {currentStepIndex}");
        
        // Step 20 (index 19): Bee visit ends tutorial and triggers level load
        if (currentStep.triggerType == TriggerType.BeeVisitedFlower)
        {
            if (showDebugLogs) Debug.Log("Step 20 (index 19) complete: Bee visited flower - Tutorial ending!");
            AdvanceStep(); // This will call EndTutorial()
            
            // The game's normal NextLevelLoader will handle progression
            // since tutorial is now inactive
        }
    }
    
    // ===== CALLED BY TUTORIAL UI =====
    
    public void OnNextButtonClicked()
    {
        if (!tutorialActive) return;
        
        // Next button ALWAYS works - allows players to skip ahead if they want
        if (showDebugLogs) Debug.Log($"Step {currentStepIndex} complete: Next button clicked (skip ahead)");
        AdvanceStep();
    }
    
    // ===== UTILITY FUNCTIONS =====
    
    int GetTotalBlockCount()
    {
        return FindObjectsOfType<HumanClick>().Length;
    }
    
    int GetBlockCountOfType(string typeName)
    {
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        int count = 0;
        
        foreach (HumanClick block in allBlocks)
        {
            BlockType blockType = block.GetBlockType();
            if (blockType != null && blockType.blockName == typeName)
            {
                count++;
            }
        }
        
        return count;
    }
}
