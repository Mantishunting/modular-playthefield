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
            Debug.LogError("TutorialManager: No tutorial steps configured!");
            enabled = false;
            return;
        }

        // Disable legacy tutorial if present
        TutorialMessages oldTutorial = FindObjectOfType<TutorialMessages>();
        if (oldTutorial != null)
        {
            oldTutorial.enabled = false;
        }

        SubscribeToEvents();
        ShowStep(0);
    }

    void SubscribeToEvents()
    {
        HumanClick.OnBlockPlaced += HandleBlockPlaced;
        HumanClick.OnBlockDestroyed += HandleBlockDestroyed;
        BlockTypeManager.OnBlockTypeSelected += HandleBlockTypeSelected;
        BeeVisitTracker.OnVisitRegistered += HandleBeeVisit;
    }

    void OnDestroy()
    {
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

        // Enable any components this step releases
        if (step.componentsToEnable != null)
        {
            foreach (Behaviour comp in step.componentsToEnable)
            {
                if (comp != null)
                {
                    comp.enabled = true;
                    if (showDebugLogs)
                        Debug.Log($"Tutorial: Enabled {comp.GetType().Name} on {comp.gameObject.name}");
                }
            }
        }

        // Activate any GameObjects this step releases
        if (step.objectsToActivate != null)
        {
            foreach (GameObject obj in step.objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    if (showDebugLogs)
                        Debug.Log($"Tutorial: Activated {obj.name}");
                }
            }
        }

        tutorialUI.DisplayStep(step);

        // Store baseline counts for increase steps
        if (step.triggerType == TriggerType.BlockCountIncrease)
        {
            blockCountAtStepStart = GetTotalBlockCount();
            stemCountAtStepStart = GetBlockCountOfType("Wood");
            leafCountAtStepStart = GetBlockCountOfType("Leaf");
            flowerCountAtStepStart = GetBlockCountOfType("Flower");
        }

        HandleSpecialStepSetup(stepIndex);
    }

    void HandleSpecialStepSetup(int stepIndex)
    {
        if (stepIndex == 13)
        {
            Debug.Log("TODO: Animate leaf transparency");
        }

        if (stepIndex == 14)
        {
            Debug.Log("TODO: Mark shaded leaf");
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
    }

    // ===== EVENT HANDLERS =====

    void HandleBlockPlaced(BlockType blockType)
    {
        if (!tutorialActive) return;
        if (blockType == null) return;

        TutorialStep step = tutorialSteps[currentStepIndex];

        if (step.triggerType == TriggerType.BlockCountIncrease)
        {
            CheckBlockTargetCount(step);
        }
    }



    void CheckBlockTargetCount(TutorialStep step)
    {
        int currentTotal = GetTotalBlockCount();
        int currentStems = GetBlockCountOfType("Wood");
        int currentLeaves = GetBlockCountOfType("Leaf");
        int currentFlowers = GetBlockCountOfType("Flower");

        // Count difference from step start
        int diffTotal = currentTotal - blockCountAtStepStart;
        int diffStems = currentStems - stemCountAtStepStart;
        int diffLeaves = currentLeaves - leafCountAtStepStart;
        int diffFlowers = currentFlowers - flowerCountAtStepStart;

        bool shouldAdvance = false;

        //  New: target-based behaviour
        if (step.requiredBlockCount >= 0)
        {
            // Step completes ONLY when reaching or passing required value
            shouldAdvance = currentTotal >= step.requiredBlockCount;
        }
        else
        {
            // Legacy behaviour (any increase)
            if (diffTotal > 0 || diffStems > 0 || diffLeaves > 0 || diffFlowers > 0)
            {
                shouldAdvance = true;
            }
        }

        if (shouldAdvance)
        {
            AdvanceStep();
        }
    }
    // END OF NEW BLOCK LOGIC 

    void HandleBlockDestroyed(BlockType blockType)
    {
        if (!tutorialActive) return;

        TutorialStep step = tutorialSteps[currentStepIndex];

        if (step.triggerType == TriggerType.BlockCountDecrease)
        {
            AdvanceStep();
        }
    }

    void HandleBlockTypeSelected(BlockType blockType)
    {
        if (!tutorialActive) return;
        if (blockType == null) return;

        TutorialStep step = tutorialSteps[currentStepIndex];

        if (step.triggerType == TriggerType.LeafButtonClicked &&
            blockType.blockName == "Leaf")
        {
            AdvanceStep();
        }
    }

    void HandleBeeVisit(int totalVisits)
    {
        if (!tutorialActive) return;

        TutorialStep step = tutorialSteps[currentStepIndex];

        if (step.triggerType == TriggerType.BeeVisitedFlower)
        {
            AdvanceStep();
        }
    }

    // ===== CALLED BY UI =====

    public void OnNextButtonClicked()
    {
        if (!tutorialActive) return;
        AdvanceStep();
    }

    // ===== UTILITY =====

    int GetTotalBlockCount()
    {
        return FindObjectsOfType<HumanClick>().Length;
    }

    int GetBlockCountOfType(string typeName)
    {
        HumanClick[] blocks = FindObjectsOfType<HumanClick>();
        int count = 0;

        foreach (HumanClick h in blocks)
        {
            BlockType t = h.GetBlockType();
            if (t != null && t.blockName == typeName)
                count++;
        }

        return count;
    }
}