using UnityEngine;

/// <summary>
/// Scales block width (X) based on distance from tree tip.
/// Blocks closer to root are wider. Scale only ever increases, never decreases.
/// </summary>
[RequireComponent(typeof(BlockGeneration))]
public class BlockScaler : MonoBehaviour
{
    [Tooltip("The minimum scale (used at the tip of the tree)")]
    [SerializeField] private float baseScale = 1f;

    [Tooltip("How much wider each generation step toward the root adds")]
    [SerializeField] private float scalePerGeneration = 0.1f;

    [Tooltip("Maximum scale a block can ever reach (optional cap)")]
    [SerializeField] private float maxScale = 3f;

    // Track the widest this block has ever been (never shrink below this)
    private float personalMaxScaleReached = 1f;

    private BlockGeneration blockGeneration;

    void Awake()
    {
        blockGeneration = GetComponent<BlockGeneration>();
        personalMaxScaleReached = baseScale;
    }

    void OnEnable()
    {
        BlockGeneration.OnTreeGrew += RecalculateScale;
    }

    void OnDisable()
    {
        BlockGeneration.OnTreeGrew -= RecalculateScale;
    }

    void Start()
    {
        RecalculateScale();
    }

    private void RecalculateScale()
    {
        int myGeneration = blockGeneration.GetGeneration();
        int maxGeneration = BlockGeneration.GlobalMaxGeneration;

        // Distance from tip: root has highest distance, tip has 0
        int distanceFromTip = maxGeneration - myGeneration;

        // Calculate target scale
        float targetScale = baseScale + (distanceFromTip * scalePerGeneration);

        // Clamp to max
        targetScale = Mathf.Min(targetScale, maxScale);

        // Only grow, never shrink
        if (targetScale > personalMaxScaleReached)
        {
            personalMaxScaleReached = targetScale;
            ApplyScale(personalMaxScaleReached);
        }
    }

    private void ApplyScale(float xScale)
    {
        Vector3 currentScale = transform.localScale;
        transform.localScale = new Vector3(xScale, currentScale.y, currentScale.z);
    }

    /// <summary>
    /// Force a recalculation (useful if generation changes outside of tree growth)
    /// </summary>
    public void ForceRecalculate()
    {
        RecalculateScale();
    }
}