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
    [SerializeField] private Button deleteButton; // Legacy/Alias for Prune Button
    [SerializeField] private Button pruneModeButton;
    [SerializeField] private Button inspectModeButton;
    [SerializeField] private Button exitMenuButton;

    [Header("Block Types")]
    [SerializeField] private BlockType woodBlockType;
    [SerializeField] private BlockType leafBlockType;
    [SerializeField] private BlockType flowerBlockType;

    [Header("Colors - Affordable")]
    [SerializeField] private Color woodAffordableColor = new Color(1f, 0.9f, 0.2f); // Yellow
    [SerializeField] private Color leafAffordableColor = new Color(0.2f, 1f, 0.2f); // Green
    [SerializeField] private Color flowerAffordableColor = new Color(1f, 0.4f, 0.8f); // Pink
    [SerializeField] private Color deleteModeColor = new Color(1f, 0.2f, 0.2f); // Red (Prune Mode)
    [SerializeField] private Color inspectModeColor = new Color(0.2f, 0.6f, 1f); // Blue (Inspect Mode)
    [SerializeField] private Color exitMenuColor = new Color(0.9f, 0.9f, 0.9f); // White/Gray (Exit Button)

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
            GameObject deleteBtnGO = GameObject.Find("DeleteButton");
            if (deleteBtnGO == null) deleteBtnGO = GameObject.Find("Delete Button");
            if (deleteBtnGO != null) deleteButton = deleteBtnGO.GetComponent<Button>();
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(SelectPruneMode);
        }

        // Auto-find prune, inspect and exit buttons
        if (pruneModeButton == null)
        {
            GameObject pruneBtnGO = GameObject.Find("PruneButton");
            if (pruneBtnGO == null) pruneBtnGO = GameObject.Find("Prune Button");
            if (pruneBtnGO != null) pruneModeButton = pruneBtnGO.GetComponent<Button>();
        }

        // Dynamic fallback creation for Prune mode button
        if (pruneModeButton == null)
        {
            Button template = flowerButton != null ? flowerButton : woodButton;
            if (template != null)
            {
                RectTransform templateRect = template.GetComponent<RectTransform>();
                Vector2 targetPos = new Vector2(-800f, 73f); // default
                if (templateRect != null)
                {
                    float offset = templateRect.sizeDelta.x * templateRect.localScale.x + 20f;
                    targetPos = templateRect.anchoredPosition - new Vector2(offset, 0f);
                }
                pruneModeButton = CreateDynamicButton(template, "PruneButton", "PRUNE", targetPos);
            }
        }

        if (pruneModeButton != null)
        {
            pruneModeButton.onClick.AddListener(SelectPruneMode);
        }

        if (inspectModeButton == null)
        {
            GameObject inspectBtnGO = GameObject.Find("InspectButton");
            if (inspectBtnGO == null) inspectBtnGO = GameObject.Find("Inspect Button");
            if (inspectBtnGO != null) inspectModeButton = inspectBtnGO.GetComponent<Button>();
        }

        // Dynamic fallback creation for Inspect mode button
        if (inspectModeButton == null)
        {
            Button template = leafButton != null ? leafButton : woodButton;
            if (template != null)
            {
                RectTransform templateRect = template.GetComponent<RectTransform>();
                Vector2 targetPos = new Vector2(800f, 73f); // default
                if (templateRect != null)
                {
                    float offset = templateRect.sizeDelta.x * templateRect.localScale.x + 20f;
                    targetPos = templateRect.anchoredPosition + new Vector2(offset, 0f);
                }
                inspectModeButton = CreateDynamicButton(template, "InspectButton", "INSPECT", targetPos);
            }
        }

        if (inspectModeButton != null)
        {
            inspectModeButton.onClick.AddListener(SelectInspectMode);
        }

        if (exitMenuButton == null)
        {
            GameObject exitBtnGO = GameObject.Find("ExitButton");
            if (exitBtnGO == null) exitBtnGO = GameObject.Find("Exit Button");
            if (exitBtnGO == null) exitBtnGO = GameObject.Find("MenuButton");
            if (exitBtnGO == null) exitBtnGO = GameObject.Find("Menu Button");
            if (exitBtnGO != null) exitMenuButton = exitBtnGO.GetComponent<Button>();
        }

        // Dynamic fallback creation for Exit button
        if (exitMenuButton == null)
        {
            Button restartTemplate = null;
            GameObject restartGO = GameObject.Find("RestartLevelButton");
            if (restartGO == null) restartGO = GameObject.Find("Restart Level Button");
            if (restartGO != null) restartTemplate = restartGO.GetComponent<Button>();

            if (restartTemplate != null)
            {
                exitMenuButton = CreateDynamicButton(restartTemplate, "ExitButton", "EXIT", Vector2.zero);
                if (exitMenuButton != null)
                {
                    RectTransform rect = exitMenuButton.GetComponent<RectTransform>();
                    RectTransform templateRect = restartTemplate.GetComponent<RectTransform>();
                    if (rect != null && templateRect != null)
                    {
                        float offset = templateRect.sizeDelta.x * templateRect.localScale.x + 20f;
                        rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(offset, 0f);
                    }
                }
            }
            else if (woodButton != null)
            {
                exitMenuButton = CreateDynamicButton(woodButton, "ExitButton", "EXIT", new Vector2(400f, 220f));
            }
        }

        if (exitMenuButton != null)
        {
            exitMenuButton.onClick.AddListener(ExitToMainMenu);
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
    /// Called when the prune/delete button is clicked
    /// </summary>
    void SelectPruneMode()
    {
        if (BlockTypeManager.Instance != null)
        {
            BlockTypeManager.Instance.SetInteractionMode(InteractionMode.Prune);
        }

        if (showDebugLogs)
        {
            Debug.Log("BlockSelectionUI: Selected Prune Mode");
        }

        UpdateButtonVisuals();
    }

    /// <summary>
    /// Called when the inspect button is clicked
    /// </summary>
    void SelectInspectMode()
    {
        if (BlockTypeManager.Instance != null)
        {
            BlockTypeManager.Instance.SetInteractionMode(InteractionMode.Inspect);
        }

        if (showDebugLogs)
        {
            Debug.Log("BlockSelectionUI: Selected Inspect Mode");
        }

        UpdateButtonVisuals();
    }

    /// <summary>
    /// Clears game state and loads the main menu/landing scene
    /// </summary>
    void ExitToMainMenu()
    {
        if (showDebugLogs)
        {
            Debug.Log("BlockSelectionUI: Exiting to Main Menu");
        }

        HumanClick.ResetStaticData();

        Reseter reseter = FindObjectOfType<Reseter>();
        string menuScene = reseter != null ? reseter.landingSceneName : "StartScene";
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene);
    }

    /// <summary>
    /// Updates button colors based on affordability and selection
    /// </summary>
    void UpdateButtonVisuals()
    {
        if (resourcesScript == null) return;

        int currentFood = resourcesScript.GetCurrentFood();
        bool isBuildMode = BlockTypeManager.Instance == null || BlockTypeManager.Instance.CurrentMode == InteractionMode.Build;
        bool isPruneMode = BlockTypeManager.Instance != null && BlockTypeManager.Instance.CurrentMode == InteractionMode.Prune;
        bool isInspectMode = BlockTypeManager.Instance != null && BlockTypeManager.Instance.CurrentMode == InteractionMode.Inspect;

        // Update Wood button using dynamic cost check
        if (woodButton != null && woodBlockType != null)
        {
            int dynamicCost = HumanClick.GetDynamicCostForType(woodBlockType);
            bool canAfford = currentFood >= dynamicCost;
            bool isSelected = isBuildMode && currentlySelectedBlockType == woodBlockType;
            UpdateButtonColor(woodButton, woodAffordableColor, canAfford, isSelected);
        }

        // Update Leaf button using dynamic cost check
        if (leafButton != null && leafBlockType != null)
        {
            int dynamicCost = HumanClick.GetDynamicCostForType(leafBlockType);
            bool canAfford = currentFood >= dynamicCost;
            bool isSelected = isBuildMode && currentlySelectedBlockType == leafBlockType;
            UpdateButtonColor(leafButton, leafAffordableColor, canAfford, isSelected);
        }

        // Update Flower button using dynamic cost check
        if (flowerButton != null && flowerBlockType != null)
        {
            int dynamicCost = HumanClick.GetDynamicCostForType(flowerBlockType);
            bool canAfford = currentFood >= dynamicCost;
            bool isSelected = isBuildMode && currentlySelectedBlockType == flowerBlockType;
            UpdateButtonColor(flowerButton, flowerAffordableColor, canAfford, isSelected);
        }

        // Update Delete/Prune button (Legacy button support)
        if (deleteButton != null)
        {
            UpdateButtonColor(deleteButton, deleteModeColor, true, isPruneMode);
        }

        // Update Prune button
        if (pruneModeButton != null)
        {
            UpdateButtonColor(pruneModeButton, deleteModeColor, true, isPruneMode);
        }

        // Update Inspect button
        if (inspectModeButton != null)
        {
            UpdateButtonColor(inspectModeButton, inspectModeColor, true, isInspectMode);
        }

        // Update Exit button (always clickable and default color)
        if (exitMenuButton != null)
        {
            UpdateButtonColor(exitMenuButton, exitMenuColor, true, false);
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

        // Tool buttons and exit button are always interactable
        if (button == deleteButton || button == pruneModeButton || button == inspectModeButton || button == exitMenuButton)
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
    /// <summary>
    /// Helper to dynamically instantiate and position fallback buttons at runtime
    /// </summary>
    private Button CreateDynamicButton(Button template, string name, string labelText, Vector2 localPos)
    {
        if (template == null) return null;

        // Clone the template button under the same parent
        Button newButton = Instantiate(template, template.transform.parent);
        newButton.name = name;

        // Position using RectTransform
        RectTransform rect = newButton.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = localPos;
        }

        // Clear duplicated click callbacks
        newButton.onClick.RemoveAllListeners();

        bool textConfigured = false;

        // Traverse children to find/configure text component and hide icons
        for (int i = 0; i < newButton.transform.childCount; i++)
        {
            Transform child = newButton.transform.GetChild(i);

            TMPro.TextMeshProUGUI tmp = child.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp == null) tmp = child.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

            Text txt = child.GetComponent<Text>();
            if (txt == null) txt = child.GetComponentInChildren<Text>(true);

            if (tmp != null)
            {
                child.gameObject.SetActive(true);
                tmp.text = labelText;
                tmp.color = Color.white;
                textConfigured = true;
            }
            else if (txt != null)
            {
                child.gameObject.SetActive(true);
                txt.text = labelText;
                txt.color = Color.white;
                textConfigured = true;
            }
            else
            {
                // Deactivate the block graphics icon so it doesn't overlap text
                child.gameObject.SetActive(false);
            }
        }

        // If no text component was found in children, create one programmatically
        if (!textConfigured)
        {
            GameObject textGO = new GameObject("Text (TMP)");
            textGO.transform.SetParent(newButton.transform, false);
            TMPro.TextMeshProUGUI tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize = 24f;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
            }
        }

        return newButton;
    }
}
