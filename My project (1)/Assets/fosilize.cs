using UnityEngine;

[RequireComponent(typeof(BlockGeneration))]
[RequireComponent(typeof(Rigidbody2D))]
public class Fossilize : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How many blocks at the tip should remain Dynamic (moving)?")]
    [SerializeField] private int activeTipLength = 8;

    private BlockGeneration genScript;
    private Rigidbody2D rb;
    private FixedJoint2D joint;

    void Awake()
    {
        genScript = GetComponent<BlockGeneration>();
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
    }

    void OnEnable()
    {
        // Subscribe to the global event that fires whenever a new block is placed
        BlockGeneration.OnTreeGrew += CheckState;

        // Also check if our own generation changes (e.g. insertion)
        genScript.OnGenerationChanged += CheckState;
    }

    void OnDisable()
    {
        BlockGeneration.OnTreeGrew -= CheckState;
        genScript.OnGenerationChanged -= CheckState;
    }

    void Start()
    {
        CheckState();
    }

    public void CheckState()
    {
        // 1. Calculate how far this block is from the newest tip
        int myGen = genScript.GetGeneration();
        int globalMax = BlockGeneration.GlobalMaxGeneration;
        int distanceFromTip = globalMax - myGen;

        // 2. Decide: Freeze or Move?
        if (distanceFromTip > activeTipLength)
        {
            // --- BECOME STONE ---
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;

                // Optional: Stop calculating velocity to ensure it stops INSTANTLY
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            // --- STAY ALIVE ---
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    // SAFETY NET: If this block dies/breaks, we need to make sure 
    // children don't get stuck floating in the air.
    // Ideally, your 'HumanClick.Die()' handles recursive destruction, 
    // so this might not be needed, but it's good safety.
    void OnJointBreak2D(Joint2D brokenJoint)
    {
        // If our joint breaks, we should probably wake up and fall (if we weren't destroyed)
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
}