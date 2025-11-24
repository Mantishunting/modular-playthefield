using UnityEngine;
using System;

[RequireComponent(typeof(HumanClick))]
public class BlockGeneration : MonoBehaviour
{
    // ============================================================
    // --- GLOBAL TRACKING SYSTEM (THIS WAS MISSING) ---
    // ============================================================
    /// <summary>
    /// Tracks the highest generation/depth reached by any block in the scene.
    /// Used by PhysicsConnector to calculate distance from the tip.
    /// </summary>
    public static int GlobalMaxGeneration { get; private set; } = 0;

    /// <summary>
    /// Event triggered when GlobalMaxGeneration increases (The "tree grew" shout).
    /// </summary>
    public static event Action OnTreeGrew;

    /// <summary>
    /// Helper to reset the global state (Call this when restarting the level!).
    /// </summary>
    public static void ResetGlobalState()
    {
        GlobalMaxGeneration = 0;
    }
    // ============================================================

    /// <summary>
    /// Event triggered when THIS block's generation changes
    /// </summary>
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

        int oldGeneration = generation;

        if (parent != null)
        {
            BlockGeneration parentGenScript = parent.GetComponent<BlockGeneration>();
            // If parent has a script, we are parent + 1. Otherwise default to 1.
            generation = (parentGenScript != null) ? parentGenScript.GetGeneration() + 1 : 1;
        }
        else
        {
            generation = 0; // Root
        }

        isInitialized = true;

        // --- CHECK IF WE ARE THE LEADER ---
        if (generation > GlobalMaxGeneration)
        {
            GlobalMaxGeneration = generation;
            OnTreeGrew?.Invoke(); // Shout to the physics connectors!
        }

        // --- IF OUR GENERATION CHANGED, NOTIFY LISTENERS AND CASCADE TO CHILDREN ---
        if (oldGeneration != generation)
        {
            OnGenerationChanged?.Invoke();
            PropagateToChildren();
        }
    }

    /// <summary>
    /// Tell all children to recalculate (cascade effect for insertions)
    /// </summary>
    private void PropagateToChildren()
    {
        // Check all four child directions
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