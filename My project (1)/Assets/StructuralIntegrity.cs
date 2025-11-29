using UnityEngine;

[RequireComponent(typeof(HumanClick))]
[RequireComponent(typeof(FixedJoint2D))]
public class StructuralIntegrity : MonoBehaviour
{
    [Header("Safety Settings")]
    [Tooltip("If the distance to parent exceeds this * blockSize, the block dies.")]
    [SerializeField] private float stretchTolerance = 1.5f;

    [Tooltip("How often to check for breaks (in seconds). 0 = every frame.")]
    [SerializeField] private float checkInterval = 0.2f;

    private HumanClick humanClick;
    private FixedJoint2D joint;
    private float timer;
    private float baseDistance = 1f; // Default, will try to read from HumanClick

    void Awake()
    {
        humanClick = GetComponent<HumanClick>();
        joint = GetComponent<FixedJoint2D>();
    }

    void Start()
    {
        // Try to read the block size from HumanClick to set the correct scale
        // Since blockSize is private in HumanClick, we assume 1.0 or estimation
        // If you make blockSize public in HumanClick, you can access it here:
        // baseDistance = humanClick.blockSize; 

        // For now, we assume 1.0f as standard grid size based on your code
        baseDistance = 1.0f;
    }

    void Update()
    {
        // Optimization: Don't check every single frame if not needed
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        CheckIntegrity();
    }

    void CheckIntegrity()
    {
        // 1. If we have no joint connected, we are either a root or floating. 
        // If floating logic is handled elsewhere, we ignore nulls.
        if (joint.connectedBody == null) return;

        // 2. Calculate distance to the physical parent anchor
        float currentDist = Vector2.Distance(transform.position, joint.connectedBody.transform.position);

        // 3. Determine max allowed length
        // We look at the block's current scale to handle the "Growing" mechanic correctly
        float currentScale = transform.localScale.x;
        float maxAllowedDist = baseDistance * currentScale * stretchTolerance;

        // 4. THE KILL SWITCH
        if (currentDist > maxAllowedDist)
        {
            Debug.LogWarning($"Structure snapped! Block {gameObject.name} stretched to {currentDist} (Max: {maxAllowedDist}). Pruning branch.");

            // This will recursively kill this block and all its children
            humanClick.Die();
        }
    }

    // Optional: Visual debug to see the strain in Scene view
    void OnDrawGizmosSelected()
    {
        if (joint != null && joint.connectedBody != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, joint.connectedBody.transform.position);
        }
    }
}