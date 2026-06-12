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

    [Header("Joint Settings (fallback when no JointStiffness controller is present)")]
    [Tooltip("Strength of the joint. Higher = Stiffer. Try 20.")]
    [SerializeField] private float frequency = 20f;

    [Tooltip("Shock absorption. 0 = Bouncy, 1 = No Bounce. Try 0.5.")]
    [SerializeField] private float dampingRatio = 0.5f;

    [Header("Stretch Kill (overstretched branches snap)")]
    [Tooltip("How far (in cells) a block may drift from its parent before its branch dies. " +
             "Keep this GENEROUS — normal spacing is 1 cell — so the player never feels cheated. " +
             "Scaled up automatically for larger blocks.")]
    [SerializeField] private float killStretchDistance = 3f;

    [Tooltip("The overstretch must persist this long (seconds) before the branch dies, " +
             "so a momentary leap while settling never kills a branch that would recover.")]
    [SerializeField] private float killStretchGrace = 0.5f;

    // How long we've been continuously overstretched. Resets the instant we're back in range.
    private float overstretchTimer;

    // The stretch check is cheap but runs on every block; a few times a second is plenty.
    private const float kStretchCheckInterval = 0.2f;
    private float stretchCheckTimer;

    // Bumped whenever ANY block's connections change, so every joint recomputes its
    // depth/load-based stiffness (a block added deep in the tree changes ancestors' load).
    private static int sStructureVersion;

    private int lastStructureVersion = -1;
    private int lastSettingsVersion = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
        humanClick = GetComponent<HumanClick>();
    }

    void Start()
    {
        // A freshly spawned child is usually already connected
        // SYNCHRONOUSLY during placement: LinkChild -> NotifyConnectionsChanged ->
        // OnConnectionsChanged -> RefreshConnection, all in the same frame, capturing the
        // clean placement position. If we blindly RefreshConnection() again here (a frame
        // later), autoConfigureConnectedAnchor RE-captures wherever physics has since
        // drifted us to — baking the drift in as the rest pose (long, rotated joint).
        // So only connect from Start if we haven't been connected already.
        if (humanClick.GetParent() == null)
        {
            RefreshConnection(); // unparented -> establish root / kinematic anchor
        }
        else if (joint.connectedBody == null)
        {
            RefreshConnection(); // parented but not yet wired (e.g. scene-authored links)
        }
        // else: already connected cleanly during placement — leave the anchor alone.
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
        // Any structural change anywhere invalidates depth/load for the whole tree.
        sStructureVersion++;

        // Only rebuild our own joint connection if our parent actually changed.
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
                joint.enabled = true;
                ApplyStiffness();
            }
        }
        else
        {
            // === ROOT MODE ===
            // We have no parent, so we are the anchor.
            // Kinematic means "I do not move, but others can attach to me."
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Stop any momentum
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// Sets this joint's stiffness from the global plant controller, based on how deep
    /// this block is from the root and how much mass hangs below it: rigid weld near
    /// the trunk, progressively softer (bendier) toward the tips. Recomputed only on
    /// structural changes or slider tweaks, never per frame.
    /// </summary>
    public void ApplyStiffness()
    {
        if (joint == null || !joint.enabled) return;

        var js = JointStiffness.Instance;
        if (js != null)
        {
            int depth = ComputeDepth();
            float load = ComputeLoad();
            joint.frequency = js.GetFrequency(depth, load);
            joint.dampingRatio = js.GetDampingRatio();

            lastSettingsVersion = js.SettingsVersion;
        }
        else
        {
            joint.frequency = frequency;
            joint.dampingRatio = dampingRatio;
        }

        lastStructureVersion = sStructureVersion;
    }

    // Recompute only when the structure or the tuning slider actually changed.
    void Update()
    {
        if (joint == null || !joint.enabled) return;

        stretchCheckTimer += Time.deltaTime;
        if (stretchCheckTimer >= kStretchCheckInterval)
        {
            CheckStretchKill(stretchCheckTimer);
            stretchCheckTimer = 0f;
        }

        var js = JointStiffness.Instance;
        if (js == null) return; // fallback already applied on connect

        if (js.SettingsVersion != lastSettingsVersion || sStructureVersion != lastStructureVersion)
        {
            ApplyStiffness();
        }
    }

    /// <summary>
    /// If a block gets yanked unreasonably far from its parent (e.g. shoved through tight
    /// spaces while building) and STAYS there, its branch snaps off via Die(). Deliberately
    /// generous and time-gated so a brief settling leap never costs the player a branch.
    /// </summary>
    private void CheckStretchKill(float elapsed)
    {
        if (joint.connectedBody == null) { overstretchTimer = 0f; return; }

        // Allowance grows with block size so big (old) blocks aren't unfairly strict.
        float parentScale = currentParent != null ? currentParent.transform.lossyScale.x : 1f;
        float scale = Mathf.Max(transform.lossyScale.x, parentScale);
        float maxDist = killStretchDistance * Mathf.Max(scale, 1f);

        float dist = Vector2.Distance(transform.position, joint.connectedBody.transform.position);

        if (dist <= maxDist)
        {
            overstretchTimer = 0f; // back in range — forgive instantly
            return;
        }

        overstretchTimer += elapsed;
        if (overstretchTimer >= killStretchGrace)
        {
            Debug.LogWarning($"[PhysicsConnector] {name} overstretched to {dist:0.0} (max {maxDist:0.0}) " +
                             $"for {killStretchGrace}s — pruning branch.");
            humanClick.Die();
        }
    }

    /// <summary>Number of joints between this block and the root (0 = root's child chain start).</summary>
    private int ComputeDepth()
    {
        int depth = 0;
        HumanClick p = humanClick.GetParent();
        while (p != null)
        {
            depth++;
            p = p.GetParent();
        }
        return depth;
    }

    /// <summary>Total mass of every descendant block hanging below this one.</summary>
    private float ComputeLoad()
    {
        return SumSubtreeMass(humanClick);
    }

    private static float SumSubtreeMass(HumanClick node)
    {
        float sum = 0f;
        AddChildMass(node.GetNorthChild(), ref sum);
        AddChildMass(node.GetSouthChild(), ref sum);
        AddChildMass(node.GetEastChild(), ref sum);
        AddChildMass(node.GetWestChild(), ref sum);
        return sum;
    }

    private static void AddChildMass(HumanClick child, ref float sum)
    {
        if (child == null) return;
        Rigidbody2D crb = child.GetComponent<Rigidbody2D>();
        if (crb != null) sum += crb.mass;
        sum += SumSubtreeMass(child);
    }

    // Re-apply when values are tweaked in the Inspector in Play mode.
    void OnValidate()
    {
        ApplyStiffness();
    }
}
