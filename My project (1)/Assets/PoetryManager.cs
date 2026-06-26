using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class PoetryManager : MonoBehaviour
{
    public static PoetryManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Folder relative to project assets where poem txt files are stored.")]
    [SerializeField] private string poemsFolder = "Poems";
    
    [Tooltip("Prefab for the hanging poem word physics object. If left null, words will be constructed dynamically.")]
    [SerializeField] private GameObject wordPrefab;

    [Tooltip("Delay in seconds between activating adjacent parts in the chain.")]
    [SerializeField] private float cascadeDelay = 0.25f;

    private List<string[]> loadedPoems = new List<string[]>();
    
    // Tracks the assigned poem words and assignment index per root flower block
    private Dictionary<HumanClick, string[]> flowerPoems = new Dictionary<HumanClick, string[]>();
    private Dictionary<HumanClick, int> flowerWordIndices = new Dictionary<HumanClick, int>();
    private HashSet<HumanClick> activatedFlowerRoots = new HashSet<HumanClick>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPoems();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Flower.OnFlowerVisited += HandleFlowerVisited;
    }

    private void OnDisable()
    {
        Flower.OnFlowerVisited -= HandleFlowerVisited;
    }

    private void LoadPoems()
    {
        string dirPath = Path.Combine(Application.dataPath, poemsFolder);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            // Create a default file so there is sample text out of the box
            File.WriteAllText(Path.Combine(dirPath, "sample.txt"), 
                "The bee bumps the bell, / A word begins to fall, / Hanging flat and still, / Down the flower wall.");
        }

        string[] files = Directory.GetFiles(dirPath, "*.txt");
        foreach (string file in files)
        {
            string rawText = File.ReadAllText(file);
            // Split by whitespace and remove punctuation
            string[] words = Regex.Split(rawText, @"\s+");
            List<string> cleanWords = new List<string>();
            foreach (string w in words)
            {
                string clean = Regex.Replace(w, @"[^\w'-]", "");
                if (!string.IsNullOrEmpty(clean))
                {
                    cleanWords.Add(clean);
                }
            }
            if (cleanWords.Count > 0)
            {
                loadedPoems.Add(cleanWords.ToArray());
            }
        }
        Debug.Log($"PoetryManager: Loaded {loadedPoems.Count} poems.");
    }

    public string GetNextWordForFlower(HumanClick root)
    {
        if (loadedPoems.Count == 0) return "";

        if (!flowerPoems.ContainsKey(root))
        {
            // Assign a random poem to this flower structure
            string[] chosenPoem = loadedPoems[Random.Range(0, loadedPoems.Count)];
            flowerPoems[root] = chosenPoem;
            flowerWordIndices[root] = 0;
            Debug.Log($"PoetryManager: Assigned poem of {chosenPoem.Length} words to root block {root.name}");
        }

        string[] poem = flowerPoems[root];
        int index = flowerWordIndices[root];
        string word = poem[index % poem.Length];
        
        flowerWordIndices[root] = index + 1;
        return word;
    }

    private void HandleFlowerVisited(Flower flower)
    {
        HumanClick hc = flower.GetComponent<HumanClick>();
        if (hc == null) return;

        HumanClick baseFlower = FindFlowerRoot(hc);
        if (baseFlower == null) return;

        if (activatedFlowerRoots.Contains(baseFlower)) return;
        activatedFlowerRoots.Add(baseFlower);

        Debug.Log($"PoetryManager: Bee visited flower. Starting cascade on root {baseFlower.name}");
        StartCoroutine(ExecutePoemCascade(baseFlower));
    }

    public HumanClick FindFlowerRoot(HumanClick current)
    {
        HumanClick parent = current.GetParent();
        if (parent != null && parent.GetComponent<Flower>() != null)
        {
            return FindFlowerRoot(parent);
        }
        return current;
    }

    private IEnumerator ExecutePoemCascade(HumanClick root)
    {
        List<HumanClick> flowerTree = GetFlowerTreeBFS(root);

        foreach (HumanClick block in flowerTree)
        {
            FlowerPoemNode node = block.GetComponent<FlowerPoemNode>();
            if (node != null)
            {
                node.ActivateWord(wordPrefab);
            }

            yield return new WaitForSeconds(cascadeDelay);
        }
    }

    private List<HumanClick> GetFlowerTreeBFS(HumanClick root)
    {
        List<HumanClick> list = new List<HumanClick>();
        Queue<HumanClick> queue = new Queue<HumanClick>();
        HashSet<HumanClick> visited = new HashSet<HumanClick>();

        queue.Enqueue(root);
        visited.Add(root);

        while (queue.Count > 0)
        {
            HumanClick current = queue.Dequeue();
            list.Add(current);

            CheckChild(current.GetNorthChild(), queue, visited);
            CheckChild(current.GetSouthChild(), queue, visited);
            CheckChild(current.GetEastChild(), queue, visited);
            CheckChild(current.GetWestChild(), queue, visited);
        }
        return list;
    }

    private void CheckChild(HumanClick child, Queue<HumanClick> queue, HashSet<HumanClick> visited)
    {
        if (child != null && !visited.Contains(child) && child.GetComponent<Flower>() != null)
        {
            queue.Enqueue(child);
            visited.Add(child);
        }
    }
}
