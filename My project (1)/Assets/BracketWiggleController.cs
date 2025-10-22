using UnityEngine;

public class BracketWiggleController : MonoBehaviour
{
    [Header("Material Reference")]
    [Tooltip("Assign the material using the BracketAtlasShader")]
    public Material bracketMaterial;

    [Header("Wiggle Presets")]
    [SerializeField] private WigglePreset level0 = new WigglePreset(0f, 0f, 0f, 0f);
    [SerializeField] private WigglePreset level1 = new WigglePreset(1.5f, 0.03f, 5f, 0.03f);
    [SerializeField] private WigglePreset level2 = new WigglePreset(2.5f, 0.08f, 15f, 0.08f);
    [SerializeField] private WigglePreset level3 = new WigglePreset(4f, 0.15f, 30f, 0.15f);
    [SerializeField] private WigglePreset level4 = new WigglePreset(6f, 0.3f, 60f, 0.3f);

    [Header("Level 4 Settings")]
    [SerializeField] private float level4Duration = 3f;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 5f;

    private WigglePreset currentTarget;
    private WigglePreset currentValues;
    private int currentLevel = 0;
    private bool isLevel4Active = false;
    private float level4Timer = 0f;
    private int previousLevel = 0;

    void Start()
    {
        // Try to find material if not assigned
        if (bracketMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                bracketMaterial = renderer.material;
            }
        }

        if (bracketMaterial == null)
        {
            Debug.LogError("BracketWiggleController: No material assigned! Please assign a material using the BracketAtlasShader.");
            enabled = false;
            return;
        }

        // Initialize to level 0 (stopped)
        currentTarget = level0;
        currentValues = level0;
        ApplyWiggleToMaterial(currentValues);
    }

    void Update()
    {
        HandleInput();
        UpdateLevel4Timer();
        SmoothTransition();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            SetWiggleLevel(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SetWiggleLevel(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SetWiggleLevel(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SetWiggleLevel(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            TriggerLevel4Burst();
        }
    }

    void SetWiggleLevel(int level)
    {
        if (isLevel4Active) return; // Don't interrupt level 4

        currentLevel = level;
        previousLevel = level;

        switch (level)
        {
            case 0:
                currentTarget = level0;
                break;
            case 1:
                currentTarget = level1;
                break;
            case 2:
                currentTarget = level2;
                break;
            case 3:
                currentTarget = level3;
                break;
        }
    }

    void TriggerLevel4Burst()
    {
        isLevel4Active = true;
        level4Timer = level4Duration;
        currentTarget = level4;
        currentLevel = 4;
    }

    void UpdateLevel4Timer()
    {
        if (!isLevel4Active) return;

        level4Timer -= Time.deltaTime;

        if (level4Timer <= 0f)
        {
            isLevel4Active = false;
            // Return to previous level
            SetWiggleLevel(previousLevel);
        }
    }

    void SmoothTransition()
    {
        // Smoothly interpolate current values toward target
        float t = Time.deltaTime * transitionSpeed;

        currentValues.speed = Mathf.Lerp(currentValues.speed, currentTarget.speed, t);
        currentValues.amount = Mathf.Lerp(currentValues.amount, currentTarget.amount, t);
        currentValues.rotation = Mathf.Lerp(currentValues.rotation, currentTarget.rotation, t);
        currentValues.scale = Mathf.Lerp(currentValues.scale, currentTarget.scale, t);

        ApplyWiggleToMaterial(currentValues);
    }

    void ApplyWiggleToMaterial(WigglePreset preset)
    {
        if (bracketMaterial == null) return;

        bracketMaterial.SetFloat("_WiggleSpeed", preset.speed);
        bracketMaterial.SetFloat("_WiggleAmount", preset.amount);
        bracketMaterial.SetFloat("_WiggleRotation", preset.rotation);
        bracketMaterial.SetFloat("_WiggleScale", preset.scale);
    }

    // Helper method to set custom wiggle values
    public void SetCustomWiggle(float speed, float amount, float rotation, float scale)
    {
        currentTarget = new WigglePreset(speed, amount, rotation, scale);
        isLevel4Active = false;
    }

    // Get current wiggle level
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}

[System.Serializable]
public class WigglePreset
{
    public float speed;
    public float amount;
    public float rotation;
    public float scale;

    public WigglePreset(float speed, float amount, float rotation, float scale)
    {
        this.speed = speed;
        this.amount = amount;
        this.rotation = rotation;
        this.scale = scale;
    }
}
