using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI buttons for selecting which block type to place.
/// Updates button colors based on affordability and selection state.
/// </summary>
public class BlockSelectionUI : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button woodButton;
    [SerializeField] private Button leafButton;
    [SerializeField] private Button flowerButton;
    [SerializeField] private Button deleteButton;

    [Header("Block Types")]
    [SerializeField] private BlockType woodBlockType;
    [SerializeField] private BlockType leafBlockType;
    [SerializeField] private BlockType flowerBlockType;

    [Header("Colors - Affordable")]
    [SerializeField] private Color woodAffordableColor = new Color(1f, 0.9f, 0.2f); // Yellow
    [SerializeField] private Color leafAffordableColor = new Color(0.2f, 1f, 0.2f); // Green
    [SerializeField] private Color flowerAffordableColor = new Color(1f, 0.4f, 0.8f); // Pink
    [SerializeField] private Color deleteModeColor = new Color(1f, 0.2f, 0.2f); // Red

    [Header("Colors - Unaffordable")]
    [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f); // Gray

    [Header("Colors - Selected")]
    [SerializeField] private Color selectedBorderColor = Color.white;
    [SerializeField] private float selectedBrightness = 1.2f; // Makes selected button brighter

    [Header("References")]
    [SerializeField] private ResourceManager resourcesScript;

    [Header("Settings")]
    [SerializeField] private bool showDebugLogs = false;

    private BlockType currentlySelectedBlockType;

    void Start()
    {
        // Set up button click listeners
        if (woodButton != null)
        {
            woodButton.onClick.AddListener(() => SelectBlockType(woodBlockType));
        }
        if (leafButton != null)
        {
            leafButton.onClick.AddListener(() => SelectBlockType(leafBlockType));
        }
        if (flowerButton != null)
        {
            flowerButton.onClick.AddListener(() => SelectBlockType(flowerBlockType));
        }

        // Auto-find delete button if not assigned
        if (deleteButton == null)
        {
            Transform deleteBtnTransform = transform.Find("DeleteButton");
            if (deleteBtnTransform == null) deleteBtnTransform = transform.Find("Delete Button");
            if (deleteBtnTransform != null) deleteButton = deleteBtnTransform.GetComponent<Button>();
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(SelectDeleteMode);
        }

        // Default to wood
        SelectBlockType(woodBlockType);
    }

    void Update()
    {
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Called when a button is clicked to select a block type
    /// </summary>
    void SelectBlockType(BlockType blockType)
    {
        if (blockType == null) return;

        currentlySelectedBlockType = blockType;

        // Tell the BlockTypeManager which block to place
        if (BlockTypeManager.Instance != null)
        {
            BlockTypeManager.Instance.SetSelectedType(blockType);
        }

        if (showDebugLogs)
        {
            Debug.Log($"BlockSelectionUI: Selected {blockType.blockName}");
        }

        // Update visuals immediately
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Called when the delete button is clicked to enter Delete Mode
    /// </summary>
    void SelectDeleteMode()
    {
        if (BlockTypeManager.Instance != null)
        {
            BlockTypeManager.Instance.SetDeleteModeActive(true);
        }

        if (showDebugLogs)
        {
            Debug.Log("BlockSelectionUI: Selected Delete Mode");
        }

        UpdateButtonVisuals();
    }

    /// <summary>
    /// Updates button colors based on affordability and selection
    /// </summary>
    void UpdateButtonVisuals()
    {
        if (resourcesScript == null) return;

        int currentFood = resourcesScript.GetCurrentFood();
        bool isDeleteMode = BlockTypeManager.Instance != null && BlockTypeManager.Instance.IsDeleteModeActive();

        // Update Wood button using dynamic cost check
        if (woodButton != null && woodBlockType != null)
        {
            int dynamicCost = HumanClick.GetDynamicCostForType(woodBlockType);
            bool canAfford = currentFood >= dynamicCost;
            bool isSelected = !isDeleteMode && currentlySelectedBlockType == woodBlockType;
            UpdateButtonColor(woodButton, woodAffordableColor, canAfford, isSelected);
        }

        // Update Leaf button using dynamic cost check
        if (leafButton != null && leafBlockType != null)
        {
            int dynamicCost = HumanClick.GetDynamicCostForType(leafBlockType);
            bool canAfford = currentFood >= dynamicCost;
            bool isSelected = !isDeleteMode && currentlySelectedBlockType == leafBlockType;
            UpdateButtonColor(leafButton, leafAffordableColor, canAfford, isSelected);
        }

        // Update Flower button using dynamic cost check
        if (flowerButton != null && flowerBlockType != null)
        {
            int dynamicCost = HumanClick.GetDynamicCostForType(flowerBlockType);
            bool canAfford = currentFood >= dynamicCost;
            bool isSelected = !isDeleteMode && currentlySelectedBlockType == flowerBlockType;
            UpdateButtonColor(flowerButton, flowerAffordableColor, canAfford, isSelected);
        }

        // Update Delete button
        if (deleteButton != null)
        {
            UpdateButtonColor(deleteButton, deleteModeColor, true, isDeleteMode);
        }
    }

    /// <summary>
    /// Updates a single button's color based on state
    /// </summary>
    void UpdateButtonColor(Button button, Color affordableColor, bool canAfford, bool isSelected)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) return;

        Color targetColor;

        if (canAfford)
        {
            targetColor = affordableColor;

            // Make selected button brighter
            if (isSelected)
            {
                targetColor *= selectedBrightness;
            }
        }
        else
        {
            targetColor = unaffordableColor;
        }

        buttonImage.color = targetColor;

        // Optional: Disable button interaction if can't afford (Delete is always interactable)
        if (button == deleteButton)
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = canAfford;
        }
    }

    /// <summary>
    /// Public method to get currently selected block type (if other systems need it)
    /// </summary>
    public BlockType GetSelectedBlockType()
    {
        return currentlySelectedBlockType;
    }

    /// <summary>
    /// Public method to programmatically select a block type
    /// </summary>
    public void SetSelectedBlockType(BlockType blockType)
    {
        SelectBlockType(blockType);
    }
}
