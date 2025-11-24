using UnityEngine;

/// <summary>
/// Scales block size (X and Y) based on distance from tree tip.
/// Blocks closer to root are larger. Scale only ever increases, never decreases.
/// Adjusts shader _Spacing to compensate so bracket density stays consistent.
/// </summary>
[RequireComponent(typeof(BlockGeneration))]
[RequireComponent(typeof(SpriteRenderer))]
public class BlockScaler : MonoBehaviour
{
    [Header("Scale Settings")]
    [Tooltip("The minimum scale (used at the tip of the tree)")]
    public float baseScale = 1f;

    [Tooltip("How much larger each generation step toward the root adds")]
    public float scalePerGeneration = 0.1f;

    [Tooltip("Maximum scale a block can ever reach")]
    public float maxScale = 3f;

    [Header("Tip Buffer")]
    [Tooltip("The youngest N generations stay at base scale (no growth)")]
    public int generationsAtTipUnchanged = 10;

    // Track the largest this block has ever been (never shrink below this)
    private float personalMaxScaleReached;

    // Cached references
    private BlockGeneration blockGeneration;
    private SpriteRenderer spriteRenderer;
    private Material instancedMaterial;

    // Original shader values (cached before any scaling)
    private float originalSpacing;
    private bool hasCachedOriginals = false;

    void Awake()
    {
        blockGeneration = GetComponent<BlockGeneration>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        personalMaxScaleReached = baseScale;

        // Create instanced material and cache original spacing
        CacheOriginalShaderValues();
    }

    private void CacheOriginalShaderValues()
    {
        if (hasCachedOriginals) return;

        // This creates an instance - won't affect other blocks
        instancedMaterial = spriteRenderer.material;

        if (instancedMaterial.HasProperty("_Spacing"))
        {
            originalSpacing = instancedMaterial.GetFloat("_Spacing");
        }
        else
        {
            originalSpacing = 0.15f; // Fallback default
            Debug.LogWarning($"BlockScaler: Material on {gameObject.name} doesn't have _Spacing property!");
        }

        hasCachedOriginals = true;
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

        // Apply tip buffer - only start scaling after N generations from tip
        int effectiveDistance = Mathf.Max(0, distanceFromTip - generationsAtTipUnchanged);

        // Calculate target scale
        float targetScale = baseScale + (effectiveDistance * scalePerGeneration);

        // Clamp to max
        targetScale = Mathf.Min(targetScale, maxScale);

        // Only grow, never shrink
        if (targetScale > personalMaxScaleReached)
        {
            personalMaxScaleReached = targetScale;

            // Step 1: Apply scale to transform
            ApplyTransformScale(personalMaxScaleReached);

            // Step 2: Adjust shader to compensate
            ApplyShaderCompensation(personalMaxScaleReached);
        }
    }

    private void ApplyTransformScale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void ApplyShaderCompensation(float scale)
    {
        if (instancedMaterial == null || !hasCachedOriginals) return;

        // Divide spacing by scale to keep bracket density consistent
        float compensatedSpacing = originalSpacing / scale;
        instancedMaterial.SetFloat("_Spacing", compensatedSpacing);
    }

    /// <summary>
    /// Force a recalculation (useful if generation changes outside of tree growth)
    /// </summary>
    public void ForceRecalculate()
    {
        RecalculateScale();
    }
}