using UnityEngine;

public class BlockTypeManager : MonoBehaviour
{
    public static BlockTypeManager Instance { get; private set; }

    [Header("Available Block Types")]
    [Tooltip("Add your BlockType assets here in the Inspector")]
    public BlockType[] availableTypes;

    private int currentTypeIndex = 0;

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

    void Update()
    {
        // Press L for Leaf
        if (Input.GetKeyDown(KeyCode.L))
        {
            SelectTypeByName("Leaf");
        }

        // Press S for Wood (Stem)
        if (Input.GetKeyDown(KeyCode.S))
        {
            SelectTypeByName("Wood");
        }
    }

    void SelectTypeByName(string typeName)
    {
        for (int i = 0; i < availableTypes.Length; i++)
        {
            if (availableTypes[i].blockName.Equals(typeName, System.StringComparison.OrdinalIgnoreCase))
            {
                currentTypeIndex = i;
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

    public string GetSelectedTypeName()
    {
        BlockType selected = GetSelectedType();
        return selected != null ? selected.blockName : "None";
    }
}