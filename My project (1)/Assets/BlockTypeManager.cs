using UnityEngine;

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

    private bool deleteModeActive = false;
    public static event System.Action<bool> OnDeleteModeToggled;

    public bool IsDeleteModeActive() => deleteModeActive;

    public void SetDeleteModeActive(bool active)
    {
        deleteModeActive = active;
        Debug.Log($"BlockTypeManager: Delete Mode active = {deleteModeActive}");
        OnDeleteModeToggled?.Invoke(deleteModeActive);
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

        // Press D, X, or Delete for Delete Mode
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete))
        {
            SetDeleteModeActive(!deleteModeActive);
        }
    }

    void SelectTypeByName(string typeName)
    {
        for (int i = 0; i < availableTypes.Length; i++)
        {
            if (availableTypes[i].blockName.Equals(typeName, System.StringComparison.OrdinalIgnoreCase))
            {
                currentTypeIndex = i;
                deleteModeActive = false;
                OnDeleteModeToggled?.Invoke(false);
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

        // Disable delete mode when selecting a block type
        deleteModeActive = false;
        OnDeleteModeToggled?.Invoke(false);

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
        if (deleteModeActive) return "Delete";
        BlockType selected = GetSelectedType();
        return selected != null ? selected.blockName : "None";
    }
}