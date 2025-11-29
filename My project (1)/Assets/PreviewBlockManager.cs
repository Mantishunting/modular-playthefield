using UnityEngine;

/// <summary>
/// Manages preview "ghost" blocks that show where a block will be placed.
/// Maintains separate previews for Wood, Leaf, and Flower types.
/// Also highlights the parent block being added to.
/// </summary>
public class PreviewBlockManager : MonoBehaviour
{
    public static PreviewBlockManager Instance { get; private set; }

    [Header("Preview Prefabs")]
    [Tooltip("Prefab for Wood preview block (should have BracketStateController)")]
    [SerializeField] private GameObject woodPreviewPrefab;

    [Tooltip("Prefab for Leaf preview block (should have BracketStateController)")]
    [SerializeField] private GameObject leafPreviewPrefab;

    [Tooltip("Prefab for Flower preview block (should have BracketStateController)")]
    [SerializeField] private GameObject flowerPreviewPrefab;

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

    [Header("Visual States - Flower")]
    [Tooltip("Flower preview when player can afford")]
    [SerializeField] private BracketAnimationState flowerCanAffordState;

    [Tooltip("Flower preview when player cannot afford (gray)")]
    [SerializeField] private BracketAnimationState flowerCantAffordState;

    [Header("Parent Highlight")]
    [Tooltip("Animation state to apply to the parent block being added to (uses same style as delete preview wobble)")]
    [SerializeField] private BracketAnimationState parentHighlightState;

    [Header("Settings")]
    [Tooltip("Vertical offset for cost text above preview block")]
    [SerializeField] private float costTextOffset = 0.7f;

    [Header("Cost Display")]
    [Tooltip("Font size for cost text")]
    [SerializeField] private float costTextSize = 0.5f;

    [Tooltip("Color for cost text when affordable")]
    [SerializeField] private Color affordableColor = Color.white;

    [Tooltip("Color for cost text when not affordable")]
    [SerializeField] private Color unaffordableColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Pooled preview instances
    private GameObject woodPreviewInstance;
    private GameObject leafPreviewInstance;
    private GameObject flowerPreviewInstance;
    private BracketStateController woodPreviewController;
    private BracketStateController leafPreviewController;
    private BracketStateController flowerPreviewController;

    // Cost text display
    private GameObject costTextObject;
    private TextMesh costTextMesh;

    // Currently active preview
    private GameObject activePreview;
    private BlockType activeBlockType;

    // Parent highlight tracking
    private HumanClick highlightedParent;
    private BracketStateController highlightedParentController;
    private BracketAnimationState savedParentState;

    // Cost display
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
        InitializeCostText();
    }

    void InitializeCostText()
    {
        // Create a GameObject for the cost text
        costTextObject = new GameObject("CostText");
        costTextObject.transform.SetParent(transform); // Parent to PreviewBlockManager

        // Add TextMesh component
        costTextMesh = costTextObject.AddComponent<TextMesh>();
        costTextMesh.fontSize = 50; // Will be scaled by transform
        costTextMesh.anchor = TextAnchor.MiddleCenter;
        costTextMesh.alignment = TextAlignment.Center;
        costTextMesh.color = affordableColor;

        // Set scale for desired size
        costTextObject.transform.localScale = Vector3.one * costTextSize;

        // Hide initially
        costTextObject.SetActive(false);

        if (showDebugLogs)
        {
            Debug.Log("PreviewBlockManager: Cost text initialized");
        }
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

            // CRITICAL: Remove or disable collider to prevent blocking clicks
            BoxCollider2D woodCollider = woodPreviewInstance.GetComponent<BoxCollider2D>();
            if (woodCollider != null)
            {
                Destroy(woodCollider); // Remove completely
                Debug.Log("PreviewBlockManager: Removed BoxCollider2D from Wood preview instance");
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

            // CRITICAL: Remove or disable collider to prevent blocking clicks
            BoxCollider2D leafCollider = leafPreviewInstance.GetComponent<BoxCollider2D>();
            if (leafCollider != null)
            {
                Destroy(leafCollider); // Remove completely
                Debug.Log("PreviewBlockManager: Removed BoxCollider2D from Leaf preview instance");
            }

            leafPreviewInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PreviewBlockManager: No Leaf preview prefab assigned!");
        }

        // Create Flower preview
        if (flowerPreviewPrefab != null)
        {
            flowerPreviewInstance = Instantiate(flowerPreviewPrefab, Vector3.zero, Quaternion.identity);
            flowerPreviewInstance.name = "FlowerPreview";
            flowerPreviewController = flowerPreviewInstance.GetComponent<BracketStateController>();

            if (flowerPreviewController == null)
            {
                Debug.LogError("PreviewBlockManager: Flower preview prefab missing BracketStateController!");
            }

            // Disable HumanClick if it exists (previews shouldn't be clickable)
            HumanClick flowerClick = flowerPreviewInstance.GetComponent<HumanClick>();
            if (flowerClick != null)
            {
                flowerClick.enabled = false;
            }

            // CRITICAL: Remove or disable collider to prevent blocking clicks
            BoxCollider2D flowerCollider = flowerPreviewInstance.GetComponent<BoxCollider2D>();
            if (flowerCollider != null)
            {
                Destroy(flowerCollider); // Remove completely
                Debug.Log("PreviewBlockManager: Removed BoxCollider2D from Flower preview instance");
            }

            flowerPreviewInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PreviewBlockManager: No Flower preview prefab assigned!");
        }
    }

    /// <summary>
    /// Show a preview block at the specified position (backwards compatible - no parent highlight)
    /// </summary>
    public void ShowPreview(Vector3 position, BlockType blockType, bool canAfford, int cost)
    {
        ShowPreview(position, blockType, canAfford, cost, null);
    }

    /// <summary>
    /// Show a preview block at the specified position, with optional parent highlight
    /// </summary>
    /// <param name="position">World position where block would be placed</param>
    /// <param name="blockType">Type of block being previewed</param>
    /// <param name="canAfford">Can the player afford this block?</param>
    /// <param name="cost">Cost to display above the preview</param>
    /// <param name="parentBlock">The parent block that will receive the new child (can be null)</param>
    public void ShowPreview(Vector3 position, BlockType blockType, bool canAfford, int cost, HumanClick parentBlock)
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
        else if (blockType.blockName == "Flower")
        {
            previewToShow = flowerPreviewInstance;
            controller = flowerPreviewController;
            stateToApply = canAfford ? flowerCanAffordState : flowerCantAffordState;
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

        // Handle parent highlight
        UpdateParentHighlight(parentBlock);

        if (showDebugLogs)
        {
            Debug.Log($"PreviewBlockManager: Showing {blockType.blockName} preview at {position}, cost: {cost}, canAfford: {canAfford}, parent: {(parentBlock != null ? parentBlock.gameObject.name : "none")}");
        }

        // Update cost text display
        UpdateCostDisplay(position + Vector3.up * costTextOffset, cost, canAfford);
    }

    /// <summary>
    /// Updates the parent block highlight state
    /// </summary>
    private void UpdateParentHighlight(HumanClick newParent)
    {
        // If parent hasn't changed, nothing to do
        if (highlightedParent == newParent)
        {
            return;
        }

        // Restore previous parent's state if there was one
        RestoreParentState();

        // If no new parent or no highlight state configured, we're done
        if (newParent == null || parentHighlightState == null)
        {
            highlightedParent = null;
            highlightedParentController = null;
            savedParentState = null;
            return;
        }

        // Get the new parent's bracket controller
        BracketStateController newParentController = newParent.GetComponent<BracketStateController>();
        if (newParentController == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"PreviewBlockManager: Parent block {newParent.gameObject.name} has no BracketStateController");
            }
            return;
        }

        // Save the parent's current state before applying highlight
        savedParentState = newParentController.GetCurrentState();
        highlightedParent = newParent;
        highlightedParentController = newParentController;

        // Apply the highlight state
        newParentController.SetState(parentHighlightState);

        if (showDebugLogs)
        {
            Debug.Log($"PreviewBlockManager: Highlighted parent block {newParent.gameObject.name}");
        }
    }

    /// <summary>
    /// Restores the previously highlighted parent to its original state
    /// </summary>
    private void RestoreParentState()
    {
        if (highlightedParent != null && highlightedParentController != null && savedParentState != null)
        {
            // Check if the parent still exists (might have been destroyed)
            if (highlightedParent.gameObject != null)
            {
                highlightedParentController.SetState(savedParentState);

                if (showDebugLogs)
                {
                    Debug.Log($"PreviewBlockManager: Restored parent block {highlightedParent.gameObject.name} to original state");
                }
            }
        }

        highlightedParent = null;
        highlightedParentController = null;
        savedParentState = null;
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

        // Restore parent's original state
        RestoreParentState();

        // Hide cost text display
        HideCostDisplay();
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

    /// <summary>
    /// Get the currently highlighted parent block (if any)
    /// </summary>
    public HumanClick GetHighlightedParent()
    {
        return highlightedParent;
    }

    void OnDestroy()
    {
        // Restore parent state before cleanup
        RestoreParentState();

        // Clean up preview instances
        if (woodPreviewInstance != null)
        {
            Destroy(woodPreviewInstance);
        }
        if (leafPreviewInstance != null)
        {
            Destroy(leafPreviewInstance);
        }
        if (flowerPreviewInstance != null)
        {
            Destroy(flowerPreviewInstance);
        }
        if (costTextObject != null)
        {
            Destroy(costTextObject);
        }
    }

    // ===== COST TEXT DISPLAY =====

    /// <summary>
    /// Shows and positions the cost text above the preview
    /// </summary>
    void UpdateCostDisplay(Vector3 position, int cost, bool canAfford)
    {
        if (costTextMesh != null && costTextObject != null)
        {
            costTextObject.transform.position = position;
            costTextMesh.text = cost.ToString();
            costTextMesh.color = canAfford ? affordableColor : unaffordableColor;
            costTextObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the cost text
    /// </summary>
    void HideCostDisplay()
    {
        if (costTextObject != null)
        {
            costTextObject.SetActive(false);
        }
    }
}