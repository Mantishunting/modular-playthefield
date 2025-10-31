using UnityEngine;

/// <summary>
/// Drives bracket shader visual states based on leaf production progress.
/// Works alongside LeafProduction.cs to provide visual feedback.
/// </summary>
public class LeafProductionVisuals : MonoBehaviour
{
    [Header("State References")]
    [Tooltip("Calm state when not producing")]
    [SerializeField] private BracketAnimationState idleState;
    
    [Tooltip("Active wiggling state during production")]
    [SerializeField] private BracketAnimationState producingState;
    
    [Tooltip("Completion animation state (spin)")]
    [SerializeField] private BracketAnimationState completeState;
    
    [Header("Timing")]
    [Tooltip("How long to show the completion animation before returning to idle")]
    [SerializeField] private float completionDuration = 1f;
    
    private BracketStateController stateController;
    private LeafProduction leafProduction;
    private BlockType myBlockType;
    
    private bool isProducing = false;
    private float productionTimer = 0f;
    private bool isShowingCompletion = false;
    private float completionTimer = 0f;
    
    void Start()
    {
        // Get required components
        stateController = GetComponent<BracketStateController>();
        leafProduction = GetComponent<LeafProduction>();
        
        if (stateController == null)
        {
            Debug.LogError("LeafProductionVisuals: BracketStateController not found on " + gameObject.name);
            enabled = false;
            return;
        }
        
        if (leafProduction == null)
        {
            Debug.LogError("LeafProductionVisuals: LeafProduction not found on " + gameObject.name);
            enabled = false;
            return;
        }
        
        // Get block type via HumanClick
        HumanClick humanClick = GetComponent<HumanClick>();
        if (humanClick != null)
        {
            myBlockType = humanClick.GetBlockType();
        }
        
        // Validate states
        if (idleState == null || producingState == null || completeState == null)
        {
            Debug.LogError("LeafProductionVisuals: Not all states assigned on " + gameObject.name);
            enabled = false;
            return;
        }
        
        // Start in idle state
        stateController.SetStateImmediate(idleState);
    }
    
    void Update()
    {
        // Handle completion animation timer
        if (isShowingCompletion)
        {
            completionTimer += Time.deltaTime;
            
            if (completionTimer >= completionDuration)
            {
                // Completion animation done, return to appropriate state
                isShowingCompletion = false;
                completionTimer = 0f;
                
                // Return to producing if still active, otherwise idle
                if (isProducing)
                {
                    stateController.SetState(producingState);
                }
                else
                {
                    stateController.SetState(idleState);
                }
            }
            
            return; // Don't check production while showing completion
        }
        
        // Check if we're currently producing
        bool wasProducing = isProducing;
        isProducing = IsCurrentlyProducing();
        
        // State change: started producing
        if (isProducing && !wasProducing)
        {
            OnProductionStarted();
        }
        // State change: stopped producing
        else if (!isProducing && wasProducing)
        {
            OnProductionStopped();
        }
        
        // Update production progress if producing
        if (isProducing)
        {
            UpdateProductionProgress();
        }
    }
    
    /// <summary>
    /// Checks if the leaf is currently in production mode.
    /// Uses reflection to check LeafProduction's private isProducing field.
    /// </summary>
    private bool IsCurrentlyProducing()
    {
        // Access the private isProducing field via reflection
        var field = typeof(LeafProduction).GetField("isProducing", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (bool)field.GetValue(leafProduction);
        }
        
        return false;
    }
    
    /// <summary>
    /// Called when production starts
    /// </summary>
    private void OnProductionStarted()
    {
        productionTimer = 0f;
        stateController.SetState(producingState);
    }
    
    /// <summary>
    /// Called when production stops
    /// </summary>
    private void OnProductionStopped()
    {
        productionTimer = 0f;
        stateController.SetState(idleState);
    }
    
    /// <summary>
    /// Updates visual intensity based on how long we've been producing
    /// </summary>
    private void UpdateProductionProgress()
    {
        productionTimer += Time.deltaTime;
        
        // Check if we just completed a production cycle
        if (myBlockType != null && productionTimer >= myBlockType.productionRate)
        {
            // Food was just produced! Show completion animation
            OnFoodProduced();
            productionTimer = 0f; // Reset for next cycle
        }
    }
    
    /// <summary>
    /// Called when food is produced - triggers completion animation
    /// </summary>
    private void OnFoodProduced()
    {
        isShowingCompletion = true;
        completionTimer = 0f;
        stateController.SetState(completeState);
    }
}
