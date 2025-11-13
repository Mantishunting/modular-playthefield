using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FixedJoint2D))]
[RequireComponent(typeof(HumanClick))]
public class PhysicsConnector : MonoBehaviour
{
    private Rigidbody2D rb;
    private FixedJoint2D joint;
    private HumanClick humanClick;

    // We track this to know if we actually need to refresh
    private HumanClick currentParent;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
        humanClick = GetComponent<HumanClick>();
    }

    void Start()
    {
        // Initial setup
        RefreshConnection();
    }

    void OnEnable()
    {
        // Subscribe to the event already in HumanClick.cs
        // This fires whenever parents/children change (Insertion, Placement, etc.)
        if (humanClick != null)
        {
            humanClick.OnConnectionsChanged += OnConnectionsChanged;
        }
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent errors
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
                // 3. Connect to the new parent
                // FixedJoint2D automatically locks the CURRENT relative position
                // which is perfect because HumanClick has usually just moved us.
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
                // We destroy it to be safe, or you can just disable it
                Destroy(joint);
            }
        }
    }
}