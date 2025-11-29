using UnityEngine;
using UnityEngine.SceneManagement;
using System;

[RequireComponent(typeof(HumanClick))]
public class BlockGeneration : MonoBehaviour
{
    // ============================================================
    // --- GLOBAL TRACKING SYSTEM ---
    // ============================================================

    static BlockGeneration()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGlobalState();
    }

    public static int GlobalMaxGeneration { get; private set; } = 0;

    public static event Action OnTreeGrew;

    public static void ResetGlobalState()
    {
        GlobalMaxGeneration = 0;
    }

    // ============================================================

    public event Action OnGenerationChanged;

    [Tooltip("The generation depth of this block. 0 = Root. Editable for debug.")]
    [SerializeField] private int generation = 0;

    private HumanClick humanClick;
    private bool isInitialized = false;

    void Awake()
    {
        humanClick = GetComponent<HumanClick>();
    }

    void OnEnable()
    {
        if (humanClick != null) humanClick.OnConnectionsChanged += CalculateGeneration;
    }

    void OnDisable()
    {
        if (humanClick != null) humanClick.OnConnectionsChanged -= CalculateGeneration;
    }

    void Start()
    {
        if (!isInitialized) CalculateGeneration();
    }

    private void OnConnectionsChanged()
    {
        CalculateGeneration();
    }

    public void CalculateGeneration()
    {
        HumanClick parent = humanClick.GetParent();

        Debug.Log($"[{gameObject.name}] CalculateGeneration called. Parent: {(parent != null ? parent.gameObject.name : "null")}");

        int oldGeneration = generation;

        if (parent != null)
        {
            BlockGeneration parentGenScript = parent.GetComponent<BlockGeneration>();
            generation = (parentGenScript != null) ? parentGenScript.GetGeneration() + 1 : 1;
        }
        else
        {
            generation = 0;
        }

        Debug.Log($"[{gameObject.name}] Gen: {oldGeneration} -> {generation}");

        isInitialized = true;

        if (generation > GlobalMaxGeneration)
        {
            GlobalMaxGeneration = generation;
            Debug.Log($"[{gameObject.name}] New GlobalMaxGeneration: {GlobalMaxGeneration}");
            OnTreeGrew?.Invoke();
        }

        if (oldGeneration != generation)
        {
            Debug.Log($"[{gameObject.name}] Generation changed, propagating to children...");
            OnGenerationChanged?.Invoke();
            PropagateToChildren();
        }
    }

    private void PropagateToChildren()
    {
        HumanClick[] children = new HumanClick[]
        {
            humanClick.GetNorthChild(),
            humanClick.GetSouthChild(),
            humanClick.GetEastChild(),
            humanClick.GetWestChild()
        };

        foreach (var child in children)
        {
            if (child != null)
            {
                Debug.Log($"[{gameObject.name}] Propagating to child: {child.gameObject.name}");
                BlockGeneration childGenScript = child.GetComponent<BlockGeneration>();
                if (childGenScript != null)
                {
                    childGenScript.CalculateGeneration();
                }
            }
        }
    }

    public int GetGeneration()
    {
        if (!isInitialized) CalculateGeneration();
        return generation;
    }

    public void SetGeneration(int gen)
    {
        int oldGeneration = generation;

        generation = gen;
        isInitialized = true;

        if (generation > GlobalMaxGeneration)
        {
            GlobalMaxGeneration = generation;
            OnTreeGrew?.Invoke();
        }

        if (oldGeneration != generation)
        {
            OnGenerationChanged?.Invoke();
            PropagateToChildren();
        }
    }
}