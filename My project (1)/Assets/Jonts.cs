using UnityEngine;

[RequireComponent(typeof(FixedJoint2D))]
[RequireComponent(typeof(BlockGeneration))]
public class Joint : MonoBehaviour
{
    [Header("Strength Settings")]
    [Tooltip("How many generations from the root should be perfectly rigid (Frequency 0)?")]
    [SerializeField] private int rigidGenerations = 3;

    [Tooltip("The stiffness for the rest of the tree.")]
    [SerializeField] private float baseFrequency = 50f;

    [Tooltip("How much stiffness to remove per generation (makes tips floppier).")]
    [SerializeField] private float decayPerGen = 2f;

    [Tooltip("Minimum stiffness to prevent it from becoming liquid.")]
    [SerializeField] private float minFrequency = 5f;

    private FixedJoint2D joint;
    private BlockGeneration genScript;

    void Awake()
    {
        joint = GetComponent<FixedJoint2D>();
        genScript = GetComponent<BlockGeneration>();
    }

    void OnEnable()
    {
        // Listen for changes! If the tree grows/shifts, update stiffness.
        if (genScript != null)
        {
            genScript.OnGenerationChanged += ApplyReinforcement;
        }
    }

    void OnDisable()
    {
        if (genScript != null)
        {
            genScript.OnGenerationChanged -= ApplyReinforcement;
        }
    }

    void Start()
    {
        // Initial setup
        ApplyReinforcement();
    }

    public void ApplyReinforcement()
    {
        if (joint == null || genScript == null) return;

        int myGen = genScript.GetGeneration();

        // STEP 1: Handle the "Trunk" (Perfectly Rigid)
        // If we are close to the root, act like a weld (Frequency 0).
        if (myGen <= rigidGenerations)
        {
            joint.frequency = 0;
            joint.dampingRatio = 0;
            return;
        }

        // STEP 2: Handle the "Branches" (Springy)
        // Calculate stiffness based on distance from root.
        float stiffness = baseFrequency - ((myGen - rigidGenerations) * decayPerGen);

        // Clamp so it never goes below the minimum safe stiffness
        stiffness = Mathf.Max(stiffness, minFrequency);

        joint.frequency = stiffness;
        joint.dampingRatio = 0.8f; // High damping prevents jitter
    }
}