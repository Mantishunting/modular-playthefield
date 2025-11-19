using UnityEngine;

[RequireComponent(typeof(HumanClick))]
public class BlockGeneration : MonoBehaviour
{
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
        if (humanClick != null)
        {
            humanClick.OnConnectionsChanged += OnConnectionsChanged;
        }
    }

    void OnDisable()
    {
        if (humanClick != null)
        {
            humanClick.OnConnectionsChanged -= OnConnectionsChanged;
        }
    }

    void Start()
    {
        // Ensure generation is calculated at start if it hasn't been accessed yet
        if (!isInitialized)
        {
            CalculateGeneration();
        }
    }

    private void OnConnectionsChanged()
    {
        // Recalculate whenever parents might have changed
        CalculateGeneration();
    }

    public void CalculateGeneration()
    {
        HumanClick parent = humanClick.GetParent();

        if (parent != null)
        {
            // We have a parent, try to get its generation
            BlockGeneration parentGenScript = parent.GetComponent<BlockGeneration>();
            if (parentGenScript != null)
            {
                // Use the getter to ensure parent is also initialized
                generation = parentGenScript.GetGeneration() + 1;
            }
            else
            {
                // Fallback: Parent exists but has no script (shouldn't happen)
                generation = 1;
            }
        }
        else
        {
            // No parent = Root
            generation = 0;
        }

        isInitialized = true;
    }

    public int GetGeneration()
    {
        // Lazy initialization: if we are asked for generation before we've calculated it
        // (e.g. by PhysicsConnector running immediately after spawn), calculate it now.
        if (!isInitialized)
        {
            CalculateGeneration();
        }
        return generation;
    }

    // Debug helper if you want to manually force it from another script
    public void SetGeneration(int gen)
    {
        generation = gen;
        isInitialized = true;
    }
}