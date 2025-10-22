using System.Collections;
using UnityEngine;

public class BlockShudder : MonoBehaviour
{
    [Header("Shudder Presets")]
    [SerializeField] private WigglePreset level0 = new WigglePreset(0f, 0f, 0f, 0f);
    [SerializeField] private WigglePreset level1 = new WigglePreset(1.5f, 0.03f, 5f, 0.03f);
    [SerializeField] private WigglePreset level2 = new WigglePreset(2.5f, 0.08f, 15f, 0.08f);
    [SerializeField] private WigglePreset level3 = new WigglePreset(4f, 0.15f, 30f, 0.15f);
    [SerializeField] private WigglePreset level4 = new WigglePreset(6f, 0.3f, 60f, 0.3f);

    [Header("Settings")]
    [SerializeField] private float shudderDuration = 3f;
    [SerializeField] private float transitionSpeed = 5f;

    private Material materialInstance;
    private HumanClick humanClick;
    private WigglePreset currentTarget;
    private WigglePreset currentValues;
    private Coroutine activeShudderCoroutine;

    void Start()
    {
        InitializeMaterial();
        SetUniqueSeed();

        // Start shudder on spawn
        StartShudder();
    }

    void Update()
    {
        SmoothTransition();

        // Manual testing controls
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetLevel(0);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetLevel(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetLevel(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetLevel(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetLevel(4);
    }

    void InitializeMaterial()
    {
        humanClick = GetComponent<HumanClick>();

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            materialInstance = renderer.material;
            Debug.Log($"BlockShudder: Material instance created for block {(humanClick != null ? humanClick.GetBlockId().ToString() : "unknown")}");
        }
        else
        {
            Debug.LogError("BlockShudder: No Renderer found!");
            enabled = false;
            return;
        }

        // Initialize to rest state
        currentTarget = level0;
        currentValues = level0;
        ApplyShudderToMaterial(currentValues);
    }

    void SetUniqueSeed()
    {
        if (materialInstance == null) return;

        float seed;
        if (humanClick != null)
        {
            seed = humanClick.GetBlockId() * 17.3f;
        }
        else
        {
            seed = Random.Range(0f, 1000f);
        }

        materialInstance.SetFloat("_Seed", seed);
        Debug.Log($"BlockShudder: Set seed to {seed}");
    }

    void StartShudder()
    {
        if (activeShudderCoroutine != null)
        {
            StopCoroutine(activeShudderCoroutine);
        }
        activeShudderCoroutine = StartCoroutine(ShudderSequence());
    }

    IEnumerator ShudderSequence()
    {
        // Go to level 3
        currentTarget = level3;
        Debug.Log("BlockShudder: Starting shudder at level 3");

        // Wait for duration
        yield return new WaitForSeconds(shudderDuration);

        // Go to level 1
        currentTarget = level1;
        Debug.Log("BlockShudder: Shudder complete, going to level 1");

        activeShudderCoroutine = null;
    }

    void SetLevel(int level)
    {
        currentTarget = GetPresetForLevel(level);
        Debug.Log($"BlockShudder: Manual set to level {level}");
    }

    // ============================================
    // PUBLIC METHOD FOR CASCADING WAVE SYSTEM
    // ============================================

    /// <summary>
    /// Triggers a shudder at the specified level.
    /// Called by HumanClick during wave propagation.
    /// </summary>
    /// <param name="level">Shudder intensity level (0-4)</param>
    public void TriggerShudderLevel(int level)
    {
        // Stop any existing automatic shudder
        if (activeShudderCoroutine != null)
        {
            StopCoroutine(activeShudderCoroutine);
            activeShudderCoroutine = null;
        }

        // Set to the requested level
        currentTarget = GetPresetForLevel(level);
        Debug.Log($"BlockShudder: Triggered wave shudder at level {level} for block {(humanClick != null ? humanClick.GetBlockId().ToString() : "unknown")}");
    }

    void SmoothTransition()
    {
        if (materialInstance == null) return;

        float t = Time.deltaTime * transitionSpeed;

        currentValues.speed = Mathf.Lerp(currentValues.speed, currentTarget.speed, t);
        currentValues.amount = Mathf.Lerp(currentValues.amount, currentTarget.amount, t);
        currentValues.rotation = Mathf.Lerp(currentValues.rotation, currentTarget.rotation, t);
        currentValues.scale = Mathf.Lerp(currentValues.scale, currentTarget.scale, t);

        ApplyShudderToMaterial(currentValues);
    }

    void ApplyShudderToMaterial(WigglePreset preset)
    {
        if (materialInstance == null) return;

        materialInstance.SetFloat("_WiggleSpeed", preset.speed);
        materialInstance.SetFloat("_WiggleAmount", preset.amount);
        materialInstance.SetFloat("_WiggleRotation", preset.rotation);
        materialInstance.SetFloat("_WiggleScale", preset.scale);
    }

    WigglePreset GetPresetForLevel(int level)
    {
        switch (level)
        {
            case 0: return level0;
            case 1: return level1;
            case 2: return level2;
            case 3: return level3;
            case 4: return level4;
            default: return level0;
        }
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}