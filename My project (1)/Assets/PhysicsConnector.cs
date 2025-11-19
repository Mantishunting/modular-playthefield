using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FixedJoint2D))]
[RequireComponent(typeof(HumanClick))]
[RequireComponent(typeof(BlockGeneration))] // Added dependency
public class PhysicsConnector : MonoBehaviour
{
    private Rigidbody2D rb;
    private FixedJoint2D joint;
    private HumanClick humanClick;
    private BlockGeneration blockGeneration;

    // We track this to know if we actually need to refresh
    private HumanClick currentParent;

    [Header("Stiffness Settings")]
    [Tooltip("The generation number where the tree reaches maximum flexibility.")]
    [SerializeField] private float maxFlexibleGeneration = 10f;

    [Tooltip("Stiffness (Frequency) at the root (Generation 0).")]
    [SerializeField] private float maxFrequency = 20f;

    [Tooltip("Stiffness (Frequency) at the tips (Generation >= MaxFlexibleGeneration).")]
    [SerializeField] private float minFrequency = 5f;

    [Tooltip("Damping at the root (Generation 0).")]
    [SerializeField] private float maxDamping = 1.0f;

    [Tooltip("Damping at the tips (Generation >= MaxFlexibleGeneration).")]
    [SerializeField] private float minDamping = 0.5f;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
        humanClick = GetComponent<HumanClick>();
        blockGeneration = GetComponent<BlockGeneration>();
    }

    void Start()
    {
        // Initial setup
        RefreshConnection();
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

    // This runs automatically whenever HumanClick changes connections
    private void OnConnectionsChanged()
    {
        // Check if our parent has actually changed
        HumanClick newParent = humanClick.GetParent();

        if (newParent != currentParent)
        {
            RefreshConnection();
        }
    }

    private void RefreshConnection()
    {
        // 1. Disconnect immediately
        joint.connectedBody = null;

        // 2. Get the new parent info
        currentParent = humanClick.GetParent();

        if (currentParent != null)
        {
            // === WE ARE A CHILD ===
            rb.bodyType = RigidbodyType2D.Dynamic;

            Rigidbody2D parentRb = currentParent.GetComponent<Rigidbody2D>();
            if (parentRb != null)
            {
                // --- DYNAMIC STIFFNESS CALCULATION ---
                int currentGeneration = blockGeneration.GetGeneration();

                // Calculate normalized factor: 0.0 at Root, 1.0 at Tip
                float normalizedGen = Mathf.InverseLerp(0f, maxFlexibleGeneration, currentGeneration);

                // Invert it for stiffness: 1.0 at Root (Stiff), 0.0 at Tip (Flexible)
                float stiffnessFactor = 1.0f - normalizedGen;

                // Lerp values based on stiffness factor
                float dynamicFrequency = Mathf.Lerp(minFrequency, maxFrequency, stiffnessFactor);
                float dynamicDamping = Mathf.Lerp(minDamping, maxDamping, stiffnessFactor);

                // 3. Apply the dynamic settings
                joint.frequency = dynamicFrequency;
                joint.dampingRatio = dynamicDamping;

                // 4. Connect to the new parent
                joint.connectedBody = parentRb;
                joint.enabled = true;
            }
        }
        else
        {
            // === WE ARE A ROOT ===
            // Anchor to world
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Disable joint since we have nothing to hold onto
            if (joint != null)
            {
                joint.enabled = false;
            }
        }
    }
}