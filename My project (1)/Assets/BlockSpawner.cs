using UnityEngine;
public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab; // Keep for backwards compatibility, but won't be used

    public GameObject SpawnBlock()
    {
        GameObject newBlock = Instantiate(blockPrefab, transform.position, Quaternion.identity);
        return newBlock;
    }

    // New method that takes a BlockType
    public GameObject SpawnBlockAt(Vector3 position, BlockType blockType)
    {
        if (blockType == null || blockType.prefab == null)
        {
            Debug.LogError("BlockType or its prefab is null! Cannot spawn block.");
            return null;
        }

        GameObject newBlock = Instantiate(blockType.prefab, position, Quaternion.identity);

        // Set the block's type so it knows what it is
        HumanClick humanClick = newBlock.GetComponent<HumanClick>();
        if (humanClick != null)
        {
            humanClick.SetBlockType(blockType);
        }

        return newBlock;
    }

    // Old method for backwards compatibility (in case it's used elsewhere)
    public GameObject SpawnBlockAt(Vector3 position)
    {
        GameObject newBlock = Instantiate(blockPrefab, position, Quaternion.identity);
        return newBlock;
    }
}