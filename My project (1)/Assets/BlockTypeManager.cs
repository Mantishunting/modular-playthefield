using UnityEngine;

public enum InteractionMode { Build, Prune, Inspect }

public class BlockTypeManager : MonoBehaviour
{
    public static BlockTypeManager Instance { get; private set; }

    [Header("Available Block Types")]
    [Tooltip("Add your BlockType assets here in the Inspector")]
    public BlockType[] availableTypes;

    private int currentTypeIndex = 0;
    public static event System.Action<BlockType> OnBlockTypeSelected;

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
        if (availableTypes.Length > 0)
        {
            Debug.Log($"Starting with block type: {availableTypes[currentTypeIndex].blockName}");
        }
        else
        {
            Debug.LogError("No block types assigned to BlockTypeManager!");
        }
    }

    private InteractionMode currentMode = InteractionMode.Build;
    public static event System.Action<InteractionMode> OnInteractionModeChanged;

    public InteractionMode CurrentMode => currentMode;

    public bool IsDeleteModeActive() => currentMode == InteractionMode.Prune;

    public void SetDeleteModeActive(bool active)
    {
        SetInteractionMode(active ? InteractionMode.Prune : InteractionMode.Build);
    }

    public void SetInteractionMode(InteractionMode mode)
    {
        currentMode = mode;
        Debug.Log($"BlockTypeManager: Interaction Mode set to {currentMode}");
        OnInteractionModeChanged?.Invoke(currentMode);
    }

    void Update()
    {
        // Press L for Leaf
        if (Input.GetKeyDown(KeyCode.L))
        {
            SelectTypeByName("Leaf");
        }

        // Press W for Wood (Stem)
        if (Input.GetKeyDown(KeyCode.W))
        {
            SelectTypeByName("Wood");
        }

        // Press B for Flower
        if (Input.GetKeyDown(KeyCode.B))
        {
            SelectTypeByName("Flower");
        }

        // Press D, X, or Delete for Delete/Prune Mode
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete))
        {
            SetInteractionMode(currentMode == InteractionMode.Prune ? InteractionMode.Build : InteractionMode.Prune);
        }

        // Press I for Inspect Mode
        if (Input.GetKeyDown(KeyCode.I))
        {
            SetInteractionMode(currentMode == InteractionMode.Inspect ? InteractionMode.Build : InteractionMode.Inspect);
        }
    }

    void SelectTypeByName(string typeName)
    {
        for (int i = 0; i < availableTypes.Length; i++)
        {
            if (availableTypes[i].blockName.Equals(typeName, System.StringComparison.OrdinalIgnoreCase))
            {
                currentTypeIndex = i;
                currentMode = InteractionMode.Build;
                OnInteractionModeChanged?.Invoke(InteractionMode.Build);
                Debug.Log($"Selected block type: {availableTypes[currentTypeIndex].blockName} (Color: {availableTypes[currentTypeIndex].blockColor})");
                return;
            }
        }

        Debug.LogWarning($"Block type '{typeName}' not found in available types!");
    }

    public BlockType GetSelectedType()
    {
        if (availableTypes.Length == 0)
        {
            Debug.LogError("No block types available!");
            return null;
        }

        return availableTypes[currentTypeIndex];
    }

    /// <summary>
    /// Sets the selected block type (called by UI buttons)
    /// </summary>
    public void SetSelectedType(BlockType blockType)
    {
        if (blockType == null)
        {
            Debug.LogWarning("Attempted to set null block type!");
            return;
        }

        // Reset mode to Build when selecting a block type
        currentMode = InteractionMode.Build;
        OnInteractionModeChanged?.Invoke(InteractionMode.Build);

        // Find the index of this block type in our array
        for (int i = 0; i < availableTypes.Length; i++)
        {
            if (availableTypes[i] == blockType)
            {
                currentTypeIndex = i;
                Debug.Log($"BlockTypeManager: Selected {blockType.blockName}");
                OnBlockTypeSelected?.Invoke(blockType);
                return;
            }
        }

        Debug.LogWarning($"Block type '{blockType.blockName}' not found in availableTypes array!");
    }

    public string GetSelectedTypeName()
    {
        if (currentMode == InteractionMode.Prune) return "Prune";
        if (currentMode == InteractionMode.Inspect) return "Inspect";
        BlockType selected = GetSelectedType();
        return selected != null ? selected.blockName : "None";
    }
}