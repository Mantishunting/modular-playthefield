using UnityEngine;

/// <summary>
/// Manages preview "ghost" blocks that show where a block will be placed.
/// Maintains separate previews for Wood and Leaf types.
/// </summary>
public class PreviewBlockManager : MonoBehaviour
{
    public static PreviewBlockManager Instance { get; private set; }

    [Header("Preview Prefabs")]
    [Tooltip("Prefab for Wood preview block (should have BracketStateController)")]
    [SerializeField] private GameObject woodPreviewPrefab;

    [Tooltip("Prefab for Leaf preview block (should have BracketStateController)")]
    [SerializeField] private GameObject leafPreviewPrefab;

    [Header("Visual States - Wood")]
    [Tooltip("Wood preview when player can afford")]
    [SerializeField] private BracketAnimationState woodCanAffordState;

    [Tooltip("Wood preview when player cannot afford (gray)")]
    [SerializeField] private BracketAnimationState woodCantAffordState;

    [Header("Visual States - Leaf")]
    [Tooltip("Leaf preview when player can afford")]
    [SerializeField] private BracketAnimationState leafCanAffordState;

    [Tooltip("Leaf preview when player cannot afford (gray)")]
    [SerializeField] private BracketAnimationState leafCantAffordState;

    [Header("Settings")]
    [Tooltip("Vertical offset for cost text above preview block")]
    [SerializeField] private float costTextOffset = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Pooled preview instances
    private GameObject woodPreviewInstance;
    private GameObject leafPreviewInstance;
    private BracketStateController woodPreviewController;
    private BracketStateController leafPreviewController;

    // Currently active preview
    private GameObject activePreview;
    private BlockType activeBlockType;

    // Cost display (TODO: Implement UI text when ready)
    private int displayedCost = 0;

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
            return;
        }

        InitializePreviews();
    }

    void InitializePreviews()
    {
        // Create Wood preview
        if (woodPreviewPrefab != null)
        {
            woodPreviewInstance = Instantiate(woodPreviewPrefab, Vector3.zero, Quaternion.identity);
            woodPreviewInstance.name = "WoodPreview";
            woodPreviewController = woodPreviewInstance.GetComponent<BracketStateController>();
            
            if (woodPreviewController == null)
            {
                Debug.LogError("PreviewBlockManager: Wood preview prefab missing BracketStateController!");
            }

            // Disable HumanClick if it exists (previews shouldn't be clickable)
            HumanClick woodClick = woodPreviewInstance.GetComponent<HumanClick>();
            if (woodClick != null)
            {
                woodClick.enabled = false;
            }

            woodPreviewInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PreviewBlockManager: No Wood preview prefab assigned!");
        }

        // Create Leaf preview
        if (leafPreviewPrefab != null)
        {
            leafPreviewInstance = Instantiate(leafPreviewPrefab, Vector3.zero, Quaternion.identity);
            leafPreviewInstance.name = "LeafPreview";
            leafPreviewController = leafPreviewInstance.GetComponent<BracketStateController>();
            
            if (leafPreviewController == null)
            {
                Debug.LogError("PreviewBlockManager: Leaf preview prefab missing BracketStateController!");
            }

            // Disable HumanClick if it exists
            HumanClick leafClick = leafPreviewInstance.GetComponent<HumanClick>();
            if (leafClick != null)
            {
                leafClick.enabled = false;
            }

            // Disable LeafProduction if it exists (previews shouldn't produce food)
            LeafProduction leafProd = leafPreviewInstance.GetComponent<LeafProduction>();
            if (leafProd != null)
            {
                leafProd.enabled = false;
            }

            leafPreviewInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PreviewBlockManager: No Leaf preview prefab assigned!");
        }
    }

    /// <summary>
    /// Show a preview block at the specified position
    /// </summary>
    /// <param name="position">World position where block would be placed</param>
    /// <param name="blockType">Type of block being previewed</param>
    /// <param name="canAfford">Can the player afford this block?</param>
    /// <param name="cost">Cost to display above the preview</param>
    public void ShowPreview(Vector3 position, BlockType blockType, bool canAfford, int cost)
    {
        if (blockType == null)
        {
            HidePreview();
            return;
        }

        // Determine which preview to use
        GameObject previewToShow = null;
        BracketStateController controller = null;
        BracketAnimationState stateToApply = null;

        if (blockType.blockName == "Wood")
        {
            previewToShow = woodPreviewInstance;
            controller = woodPreviewController;
            stateToApply = canAfford ? woodCanAffordState : woodCantAffordState;
        }
        else if (blockType.blockName == "Leaf")
        {
            previewToShow = leafPreviewInstance;
            controller = leafPreviewController;
            stateToApply = canAfford ? leafCanAffordState : leafCantAffordState;
        }

        // Validation
        if (previewToShow == null || controller == null || stateToApply == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"PreviewBlockManager: Cannot show preview for {blockType.blockName} - missing setup");
            }
            return;
        }

        // Hide the other preview if it's active
        if (activePreview != null && activePreview != previewToShow)
        {
            activePreview.SetActive(false);
        }

        // Position and activate the correct preview
        previewToShow.transform.position = position;
        previewToShow.SetActive(true);
        
        // Apply the appropriate visual state
        controller.SetState(stateToApply);

        // Update tracking
        activePreview = previewToShow;
        activeBlockType = blockType;
        displayedCost = cost;

        if (showDebugLogs)
        {
            Debug.Log($"PreviewBlockManager: Showing {blockType.blockName} preview at {position}, cost: {cost}, canAfford: {canAfford}");
        }

        // TODO: Update cost text UI when implemented
        // UpdateCostDisplay(position + Vector3.up * costTextOffset, cost, canAfford);
    }

    /// <summary>
    /// Hide the currently active preview
    /// </summary>
    public void HidePreview()
    {
        if (activePreview != null)
        {
            activePreview.SetActive(false);
            activePreview = null;
            activeBlockType = null;
            displayedCost = 0;

            if (showDebugLogs)
            {
                Debug.Log("PreviewBlockManager: Preview hidden");
            }
        }

        // TODO: Hide cost text UI when implemented
        // HideCostDisplay();
    }

    /// <summary>
    /// Check if a preview is currently being shown
    /// </summary>
    public bool IsPreviewActive()
    {
        return activePreview != null && activePreview.activeSelf;
    }

    /// <summary>
    /// Get the current preview cost (for external UI systems)
    /// </summary>
    public int GetDisplayedCost()
    {
        return displayedCost;
    }

    void OnDestroy()
    {
        // Clean up preview instances
        if (woodPreviewInstance != null)
        {
            Destroy(woodPreviewInstance);
        }
        if (leafPreviewInstance != null)
        {
            Destroy(leafPreviewInstance);
        }
    }

    // ===== FUTURE: COST TEXT UI =====
    /*
    private TextMeshPro costText; // or UI Text
    
    void UpdateCostDisplay(Vector3 position, int cost, bool canAfford)
    {
        if (costText != null)
        {
            costText.transform.position = position;
            costText.text = cost.ToString();
            costText.color = canAfford ? Color.white : Color.red;
            costText.gameObject.SetActive(true);
        }
    }
    
    void HideCostDisplay()
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(false);
        }
    }
    */
}
