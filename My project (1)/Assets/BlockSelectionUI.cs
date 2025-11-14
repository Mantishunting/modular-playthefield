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

    [Header("Block Types")]
    [SerializeField] private BlockType woodBlockType;
    [SerializeField] private BlockType leafBlockType;
    [SerializeField] private BlockType flowerBlockType;

    [Header("Colors - Affordable")]
    [SerializeField] private Color woodAffordableColor = new Color(1f, 0.9f, 0.2f); // Yellow
    [SerializeField] private Color leafAffordableColor = new Color(0.2f, 1f, 0.2f); // Green
    [SerializeField] private Color flowerAffordableColor = new Color(1f, 0.4f, 0.8f); // Pink

    [Header("Colors - Unaffordable")]
    [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f); // Gray

    [Header("Colors - Selected")]
    [SerializeField] private Color selectedBorderColor = Color.white;
    [SerializeField] private float selectedBrightness = 1.2f; // Makes selected button brighter

    [Header("References")]
    [SerializeField] private Resources resourcesScript;

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
    /// Updates button colors based on affordability and selection
    /// </summary>
    void UpdateButtonVisuals()
    {
        if (resourcesScript == null) return;

        int currentFood = resourcesScript.GetCurrentFood();

        // Update Wood button
        if (woodButton != null && woodBlockType != null)
        {
            bool canAfford = currentFood >= woodBlockType.cost;
            bool isSelected = currentlySelectedBlockType == woodBlockType;
            UpdateButtonColor(woodButton, woodAffordableColor, canAfford, isSelected);
        }

        // Update Leaf button
        if (leafButton != null && leafBlockType != null)
        {
            bool canAfford = currentFood >= leafBlockType.cost;
            bool isSelected = currentlySelectedBlockType == leafBlockType;
            UpdateButtonColor(leafButton, leafAffordableColor, canAfford, isSelected);
        }

        // Update Flower button
        if (flowerButton != null && flowerBlockType != null)
        {
            bool canAfford = currentFood >= flowerBlockType.cost;
            bool isSelected = currentlySelectedBlockType == flowerBlockType;
            UpdateButtonColor(flowerButton, flowerAffordableColor, canAfford, isSelected);
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

        // Optional: Disable button interaction if can't afford
        button.interactable = canAfford;
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
