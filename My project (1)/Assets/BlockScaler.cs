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

    [Header("Timing")]
    [Tooltip("How often to check for scale updates (seconds)")]
    public float checkInterval = 1f;

    // Track the largest this block has ever been (never shrink below this)
    private float personalMaxScaleReached;

    // Cached references
    private BlockGeneration blockGeneration;
    private SpriteRenderer spriteRenderer;
    private Material instancedMaterial;

    // Original shader values (cached before any scaling)
    private float originalSpacing;
    private bool hasCachedOriginals = false;

    // Timer
    private float timer;

    void Awake()
    {
        blockGeneration = GetComponent<BlockGeneration>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        personalMaxScaleReached = baseScale;

        CacheOriginalShaderValues();

        // Randomize start time so all blocks don't check at once
        timer = Random.Range(0f, checkInterval);
    }

    private void CacheOriginalShaderValues()
    {
        if (hasCachedOriginals) return;

        instancedMaterial = spriteRenderer.material;

        if (instancedMaterial.HasProperty("_Spacing"))
        {
            originalSpacing = instancedMaterial.GetFloat("_Spacing");
        }
        else
        {
            originalSpacing = 0.15f;
        }

        hasCachedOriginals = true;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = checkInterval;
            RecalculateScale();
        }
    }

    private void RecalculateScale()
    {
        int myGeneration = blockGeneration.GetGeneration();
        int maxGeneration = BlockGeneration.GlobalMaxGeneration;

        // Skip if we look like we're mid-insert (orphaned temporarily)
        if (myGeneration == 0 && maxGeneration > 0)
        {
            return;
        }

        int distanceFromTip = maxGeneration - myGeneration;
        int effectiveDistance = Mathf.Max(0, distanceFromTip - generationsAtTipUnchanged);

        float targetScale = baseScale + (effectiveDistance * scalePerGeneration);
        targetScale = Mathf.Min(targetScale, maxScale);

        if (targetScale > personalMaxScaleReached)
        {
            personalMaxScaleReached = targetScale;
            ApplyTransformScale(personalMaxScaleReached);
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

        float compensatedSpacing = originalSpacing / scale;
        instancedMaterial.SetFloat("_Spacing", compensatedSpacing);
    }
}