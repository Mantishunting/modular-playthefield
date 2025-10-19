using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;

    public GameObject SpawnBlock()
    {
        GameObject newBlock = Instantiate(blockPrefab, transform.position, Quaternion.identity);
        return newBlock;
    }

    public GameObject SpawnBlockAt(Vector3 position)
    {
        GameObject newBlock = Instantiate(blockPrefab, position, Quaternion.identity);
        return newBlock;
    }
}