using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FixedJoint2D))]
[RequireComponent(typeof(HumanClick))]
public class PhysicsConnector : MonoBehaviour
{
    private Rigidbody2D rb;
    private FixedJoint2D joint;
    private HumanClick humanClick;

    // We track this so we don't refresh unnecessarily
    private HumanClick currentParent;

    [Header("Joint Settings")]
    [Tooltip("Strength of the joint. Higher = Stiffer. Try 20.")]
    [SerializeField] private float frequency = 20f;

    [Tooltip("Shock absorption. 0 = Bouncy, 1 = No Bounce. Try 0.5.")]
    [SerializeField] private float dampingRatio = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
        humanClick = GetComponent<HumanClick>();
    }

    void Start()
    {
        // Connect as soon as we spawn
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

    private void OnConnectionsChanged()
    {
        // Only refresh if the parent actually changed (e.g. insertion)
        if (humanClick.GetParent() != currentParent)
        {
            RefreshConnection();
        }
    }

    private void RefreshConnection()
    {
        // 1. Reset
        joint.connectedBody = null;
        joint.enabled = false;

        // 2. Get Parent
        currentParent = humanClick.GetParent();

        if (currentParent != null)
        {
            // === CHILD MODE ===
            // We need physics to move/wobble
            rb.bodyType = RigidbodyType2D.Dynamic;

            Rigidbody2D parentRb = currentParent.GetComponent<Rigidbody2D>();
            if (parentRb != null)
            {
                joint.connectedBody = parentRb;
                joint.autoConfigureConnectedAnchor = true; // Lock to relative position
                joint.frequency = frequency;
                joint.dampingRatio = dampingRatio;
                joint.enabled = true;
            }
        }
        else
        {
            // === ROOT MODE ===
            // We have no parent, so we are the anchor.
            // Kinematic means "I do not move, but others can attach to me."
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Stop any momentum
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // Helper to update settings in Play Mode without restarting
    void OnValidate()
    {
        if (joint != null && joint.enabled)
        {
            joint.frequency = frequency;
            joint.dampingRatio = dampingRatio;
        }
    }
}