using UnityEngine;

public class FlowerPoemNode : MonoBehaviour
{
    public string assignedWord { get; private set; }
    private bool isWordActivated = false;
    private GameObject spawnedWord;

    private void Start()
    {
        HumanClick hc = GetComponent<HumanClick>();
        if (hc == null) return;

        // Auto-bootstrap the PoetryManager if it hasn't been created yet
        if (PoetryManager.Instance == null)
        {
            GameObject pmGo = new GameObject("PoetryManager");
            pmGo.AddComponent<PoetryManager>();
        }

        // Try to obtain the next word for this block from the PoetryManager
        if (PoetryManager.Instance != null)
        {
            HumanClick root = PoetryManager.Instance.FindFlowerRoot(hc);
            assignedWord = PoetryManager.Instance.GetNextWordForFlower(root);
            Debug.Log($"FlowerPoemNode: Block {gameObject.name} assigned word: '{assignedWord}'");
        }
        else
        {
            Debug.LogWarning("FlowerPoemNode: PoetryManager.Instance is null! Word assignment skipped.");
        }
    }

    public void ActivateWord(GameObject wordPrefab)
    {
        if (isWordActivated || string.IsNullOrEmpty(assignedWord)) return;
        isWordActivated = true;

        if (wordPrefab != null)
        {
            spawnedWord = Instantiate(wordPrefab, transform.position + Vector3.down, Quaternion.identity);
        }
        else
        {
            spawnedWord = new GameObject("PoemWord_" + assignedWord);
            spawnedWord.transform.position = transform.position + Vector3.down;
        }

        HangingPoemWord hangingScript = spawnedWord.GetComponent<HangingPoemWord>();
        if (hangingScript == null)
        {
            hangingScript = spawnedWord.AddComponent<HangingPoemWord>();
        }

        hangingScript.Initialize(GetComponent<HumanClick>(), assignedWord);
        Debug.Log($"FlowerPoemNode: Activated word '{assignedWord}' hanging from block {gameObject.name}");
    }
}
