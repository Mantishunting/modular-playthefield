using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FixedJoint2D))]
[RequireComponent(typeof(HumanClick))]
[RequireComponent(typeof(BlockGeneration))]
public class PhysicsConnector : MonoBehaviour
{
    private Rigidbody2D rb;
    private FixedJoint2D joint;
    private HumanClick humanClick;
    private BlockGeneration blockGeneration;
    private HumanClick currentParent;

    // === HEARTBEAT CONTROLS ===
    private static bool isHeartbeatActive = false;
    [Tooltip("How often, in seconds, the tree checks itself for hardening.")]
    [SerializeField] private float hardeningCheckInterval = 1.0f;
    // ==========================

    [Header("Trailing Solidity Settings")]
    [Tooltip("How far from the tip does the wood harden? e.g., 4 means the top 4 blocks are flexible, everything below is solid.")]
    [SerializeField] private int hardeningThreshold = 4;

    [Header("Flexible Settings")]
    [Tooltip("Stiffness of the flexible tip.")]
    [SerializeField] private float flexibleFrequency = 10f;
    [Tooltip("Bounciness of the flexible tip.")]
    [SerializeField] private float flexibleDamping = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
        humanClick = GetComponent<HumanClick>();
        blockGeneration = GetComponent<BlockGeneration>();
    }

    void Start()
    {
        RefreshConnection();
    }

    void OnEnable()
    {
        if (humanClick != null) humanClick.OnConnectionsChanged += OnConnectionsChanged;

        // Only the true root block should start the global timer
        if (blockGeneration.GetGeneration() == 0 && !isHeartbeatActive)
        {
            StartHeartbeat();
        }
    }

    void OnDisable()
    {
        if (humanClick != null) humanClick.OnConnectionsChanged -= OnConnectionsChanged;

        // Stop the heartbeat if this was the root block
        if (blockGeneration.GetGeneration() == 0 && isHeartbeatActive)
        {
            CancelInvoke(nameof(GlobalHardeningCheck));
            isHeartbeatActive = false;
        }
    }

    private void OnConnectionsChanged()
    {
        if (humanClick.GetParent() != currentParent)
        {
            RefreshConnection();
        }
    }

    /// <summary>
    /// Checks the current block's age against the global max to determine if it should harden.
    /// This is called periodically by the GlobalHeartbeat.
    /// </summary>
    public void CheckHardeningNow()
    {
        // Only run logic if we have a parent (root is always kinematic)
        if (currentParent == null) return;

        int myGen = blockGeneration.GetGeneration();
        int maxGen = BlockGeneration.GlobalMaxGeneration;

        // Calculate distance from the tip
        int distanceFromTip = maxGen - myGen;

        // Decide if we are old enough to harden
        if (distanceFromTip >= hardeningThreshold)
        {
            // === I AM OLD -> BECOME SOLID (Kinematic) ===
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            // Kinematic bodies don't need the joint, so disable it
            joint.enabled = false;
        }
        else
        {
            // === I AM NEW -> STAY FLEXIBLE (Dynamic) ===
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }

            // Apply the spring settings
            if (joint.frequency != flexibleFrequency)
            {
                joint.frequency = flexibleFrequency;
                joint.dampingRatio = flexibleDamping;
            }
            joint.enabled = true;
        }
    }

    // --- HEARTBEAT IMPLEMENTATION ---

    private void StartHeartbeat()
    {
        // Start the repeating timer on the root block
        InvokeRepeating(nameof(GlobalHardeningCheck), hardeningCheckInterval, hardeningCheckInterval);
        isHeartbeatActive = true;
    }

    private void GlobalHardeningCheck()
    {
        // Find all PhysicsConnectors in the scene and tell them to check their state
        PhysicsConnector[] allConnectors = FindObjectsOfType<PhysicsConnector>();

        foreach (var connector in allConnectors)
        {
            connector.CheckHardeningNow();
        }
    }

    // --- CONNECTION LOGIC ---

    private void RefreshConnection()
    {
        currentParent = humanClick.GetParent();
        joint.connectedBody = null;
        joint.enabled = false;

        if (currentParent != null)
        {
            Rigidbody2D parentRb = currentParent.GetComponent<Rigidbody2D>();

            if (parentRb != null)
            {
                joint.connectedBody = parentRb;
                joint.autoConfigureConnectedAnchor = true;

                // Set initial state (Kinematic/Dynamic) upon being connected
                CheckHardeningNow();
            }
        }
        else
        {
            // Root (always Kinematic)
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}