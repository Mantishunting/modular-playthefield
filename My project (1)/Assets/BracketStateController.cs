using UnityEngine;

public class BracketStateController : MonoBehaviour
{
    [Header("State Management")]
    [SerializeField] private BracketAnimationState startingState;
    [SerializeField] private BracketAnimationState targetState;

    [Header("Transition Settings")]
    [Range(0.1f, 20f)]
    [SerializeField] private float transitionSpeed = 5f;

    [Header("Material Settings")]
    [Tooltip("Override to set specific seed, leave at -1 for random")]
    [SerializeField] private float seedOverride = -1f;

    // NEW: extra rotation (degrees) supplied by StructureVisuals per junction
    [Header("Orientation")]
    [SerializeField] private float extraRotationDeg = 0f;
    public void SetExtraRotation(float deg) { extraRotationDeg = deg; }

    private Material bracketMaterial;
    private Renderer rend;

    // Current interpolated values (what's actually on the shader)
    private CurrentValues current;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("BracketStateController: No Renderer component found on " + gameObject.name);
            enabled = false;
            return;
        }

        bracketMaterial = rend.material;

        float seed = (seedOverride >= 0) ? seedOverride : Random.Range(0f, 10000f);
        bracketMaterial.SetFloat("_Seed", seed);

        if (startingState != null)
        {
            targetState = startingState;
            current = new CurrentValues(startingState);
            ApplyCurrentValues();
        }
        else
        {
            Debug.LogWarning("BracketStateController: No starting state assigned on " + gameObject.name);
        }
    }

    void Update()
    {
        if (targetState == null || bracketMaterial == null) return;

        float t = Time.deltaTime * transitionSpeed;

        current.spacing = Mathf.Lerp(current.spacing, targetState.spacing, t);
        current.bracketSize = Mathf.Lerp(current.bracketSize, targetState.bracketSize, t);
        current.gridCount = Mathf.Lerp(current.gridCount, targetState.gridCount, t);

        current.leftEdge = Mathf.Lerp(current.leftEdge, targetState.leftEdge, t);
        current.rightEdge = Mathf.Lerp(current.rightEdge, targetState.rightEdge, t);
        current.topEdge = Mathf.Lerp(current.topEdge, targetState.topEdge, t);
        current.bottomEdge = Mathf.Lerp(current.bottomEdge, targetState.bottomEdge, t);

        current.minScale = Mathf.Lerp(current.minScale, targetState.minScale, t);
        current.maxScale = Mathf.Lerp(current.maxScale, targetState.maxScale, t);

        current.localRotationRange = Mathf.Lerp(current.localRotationRange, targetState.localRotationRange, t);
        current.globalRotation = Mathf.Lerp(current.globalRotation, targetState.globalRotation, t);

        current.staticJitter = Mathf.Lerp(current.staticJitter, targetState.staticJitter, t);
        current.wiggleAmount = Mathf.Lerp(current.wiggleAmount, targetState.wiggleAmount, t);
        current.wiggleSpeed = Mathf.Lerp(current.wiggleSpeed, targetState.wiggleSpeed, t);

        ApplyCurrentValues();
    }

    public void SetState(BracketAnimationState newState)
    {
        if (newState == null)
        {
            Debug.LogWarning("BracketStateController: Attempted to set null state");
            return;
        }
        targetState = newState;
    }

    public void SetStateImmediate(BracketAnimationState newState)
    {
        if (newState == null)
        {
            Debug.LogWarning("BracketStateController: Attempted to set null state");
            return;
        }

        targetState = newState;
        current = new CurrentValues(newState);
        ApplyCurrentValues();
    }

    private void ApplyCurrentValues()
    {
        if (bracketMaterial == null) return;

        // Grid Settings
        bracketMaterial.SetFloat("_Spacing", current.spacing);
        bracketMaterial.SetFloat("_BracketSize", current.bracketSize);
        bracketMaterial.SetFloat("_GridCount", current.gridCount);

        // Edge Culling
        bracketMaterial.SetFloat("_LeftEdge", current.leftEdge);
        bracketMaterial.SetFloat("_RightEdge", current.rightEdge);
        bracketMaterial.SetFloat("_TopEdge", current.topEdge);
        bracketMaterial.SetFloat("_BottomEdge", current.bottomEdge);

        // Scale Range
        bracketMaterial.SetFloat("_MinScale", current.minScale);
        bracketMaterial.SetFloat("_MaxScale", current.maxScale);

        // Rotation (NOW includes the supplied per-junction offset)
        bracketMaterial.SetFloat("_LocalRotationRange", current.localRotationRange);
        bracketMaterial.SetFloat("_GlobalRotation", current.globalRotation + extraRotationDeg);

        // Position Randomness
        bracketMaterial.SetFloat("_StaticJitter", current.staticJitter);
        bracketMaterial.SetFloat("_WiggleAmount", current.wiggleAmount);
        bracketMaterial.SetFloat("_WiggleSpeed", current.wiggleSpeed);
    }

    public BracketAnimationState GetCurrentState() => targetState;

    void OnDestroy()
    {
        if (bracketMaterial != null) Destroy(bracketMaterial);
    }
}

[System.Serializable]
public class CurrentValues
{
    public float spacing;
    public float bracketSize;
    public float gridCount;

    public float leftEdge;
    public float rightEdge;
    public float topEdge;
    public float bottomEdge;

    public float minScale;
    public float maxScale;

    public float localRotationRange;
    public float globalRotation;

    public float staticJitter;
    public float wiggleAmount;
    public float wiggleSpeed;

    public CurrentValues(BracketAnimationState state)
    {
        spacing = state.spacing;
        bracketSize = state.bracketSize;
        gridCount = state.gridCount;

        leftEdge = state.leftEdge;
        rightEdge = state.rightEdge;
        topEdge = state.topEdge;
        bottomEdge = state.bottomEdge;

        minScale = state.minScale;
        maxScale = state.maxScale;

        localRotationRange = state.localRotationRange;
        globalRotation = state.globalRotation;

        staticJitter = state.staticJitter;
        wiggleAmount = state.wiggleAmount;
        wiggleSpeed = state.wiggleSpeed;
    }
}
