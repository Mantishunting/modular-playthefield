using UnityEngine;
using UnityEngine.UI;

public class FoodDisplayBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform barRectTransform;
    [SerializeField] private Image barImage;
    [SerializeField] private RectTransform arrowRectTransform;
    [SerializeField] private Image arrowImage;
    [SerializeField] private Resources resourcesScript;

    [Header("Bar Settings")]
    [SerializeField] private float minHeight = 10f;   // UI units instead of world units
    [SerializeField] private float maxHeight = 400f;  // UI units
    [SerializeField] private float logarithmBase = 10f;
    [SerializeField] private float growthSpeed = 2f;

    [Header("Arrow Settings")]
    [SerializeField] private float arrowRiseSpeed = 10f;
    [SerializeField] private float arrowFallSpeed = 0.5f;

    [Header("Color Tiers - GREEN (Gaining)")]
    [SerializeField] private Color greenTier1 = new Color(0.000f, 0.973f, 0.278f);
    [SerializeField] private Color greenTier2 = new Color(0.000f, 0.906f, 0.004f);
    [SerializeField] private Color greenTier3 = new Color(0.000f, 0.467f, 0.067f);
    [SerializeField] private Color greenTier4 = new Color(0.000f, 0.404f, 0.055f);

    [Header("Color Tiers - RED (Losing)")]
    [SerializeField] private Color redTier1 = new Color(1.0f, 0.3f, 0.0f);
    [SerializeField] private Color redTier2 = new Color(0.9f, 0.2f, 0.0f);
    [SerializeField] private Color redTier3 = new Color(0.6f, 0.1f, 0.0f);
    [SerializeField] private Color redTier4 = new Color(0.4f, 0.05f, 0.0f);

    [Header("Logarithmic Tier Thresholds")]
    [SerializeField] private float tier1Max = 10f;
    [SerializeField] private float tier2Max = 100f;
    [SerializeField] private float tier3Max = 1000f;

    private float displayedFoodAmount;
    private float lastFoodCheck;
    private float timeSinceLastCheck = 0f;
    private bool isGaining = true;
    private float currentBarHeight;

    // Arrow state
    private float currentArrowPercent = 0f;
    private float targetArrowPercent = 0f;

    // Public property for external access (like FoodBarRotatorUI)
    public bool IsGaining => isGaining;

    private void Start()
    {
        if (resourcesScript != null)
        {
            lastFoodCheck = resourcesScript.GetCurrentFood();
            displayedFoodAmount = lastFoodCheck;
        }

        // Initialize bar height
        UpdateBarHeight();
    }

    private void Update()
    {
        if (resourcesScript == null) return;

        float currentFood = resourcesScript.GetCurrentFood();

        // Check food gain every second
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= 1f)
        {
            float foodPerSecond = currentFood - lastFoodCheck;

            if (foodPerSecond > 0)
            {
                isGaining = true;
                // Logarithmic mapping: food/sec → arrow position
                float logValue = Mathf.Log(Mathf.Max(foodPerSecond, 1f), 10);
                targetArrowPercent = Mathf.InverseLerp(0, Mathf.Log(1000f, 10), logValue);
                targetArrowPercent = Mathf.Clamp01(targetArrowPercent);
            }
            else if (foodPerSecond < 0)
            {
                isGaining = false;
                float logValue = Mathf.Log(Mathf.Max(Mathf.Abs(foodPerSecond), 1f), 10);
                targetArrowPercent = Mathf.InverseLerp(0, Mathf.Log(1000f, 10), logValue);
                targetArrowPercent = Mathf.Clamp01(targetArrowPercent);
            }
            else
            {
                targetArrowPercent = 0f;
            }

            lastFoodCheck = currentFood;
            timeSinceLastCheck = 0f;
        }

        // Arrow drifts down
        targetArrowPercent = Mathf.Max(0f, targetArrowPercent - arrowFallSpeed * Time.deltaTime);

        // Move current toward target
        float speed = (targetArrowPercent > currentArrowPercent) ? arrowRiseSpeed : arrowFallSpeed;
        currentArrowPercent = Mathf.Lerp(currentArrowPercent, targetArrowPercent, Time.deltaTime * speed);

        displayedFoodAmount = Mathf.Lerp(displayedFoodAmount, currentFood, Time.deltaTime * growthSpeed);

        UpdateBarHeight();
        UpdateBarColor();
        UpdateArrow();
    }

    private void UpdateBarHeight()
    {
        if (barRectTransform == null) return;

        float logValue = Mathf.Log(Mathf.Max(displayedFoodAmount, 1f), logarithmBase);
        float normalizedHeight = Mathf.InverseLerp(0, Mathf.Log(10000f, logarithmBase), logValue);
        float barHeight = Mathf.Lerp(minHeight, maxHeight, normalizedHeight);

        currentBarHeight = barHeight;

        // For UI, we modify sizeDelta instead of localScale
        Vector2 sizeDelta = barRectTransform.sizeDelta;
        sizeDelta.y = barHeight;
        barRectTransform.sizeDelta = sizeDelta;
    }

    private void UpdateBarColor()
    {
        if (barImage == null) return;

        Color tier1 = isGaining ? greenTier1 : redTier1;
        Color tier2 = isGaining ? greenTier2 : redTier2;
        Color tier3 = isGaining ? greenTier3 : redTier3;
        Color tier4 = isGaining ? greenTier4 : redTier4;

        Color targetColor;

        if (displayedFoodAmount < tier1Max)
        {
            targetColor = tier1;
        }
        else if (displayedFoodAmount < tier2Max)
        {
            float t = Mathf.InverseLerp(tier1Max, tier2Max, displayedFoodAmount);
            targetColor = Color.Lerp(tier1, tier2, t);
        }
        else if (displayedFoodAmount < tier3Max)
        {
            float t = Mathf.InverseLerp(tier2Max, tier3Max, displayedFoodAmount);
            targetColor = Color.Lerp(tier2, tier3, t);
        }
        else
        {
            float t = Mathf.InverseLerp(tier3Max, tier3Max * 10f, displayedFoodAmount);
            targetColor = Color.Lerp(tier3, tier4, t);
        }

        barImage.color = targetColor;
    }

    private void UpdateArrow()
    {
        if (arrowRectTransform == null) return;

        float barBottom = -currentBarHeight / 2f;
        float barTop = currentBarHeight / 2f;

        float yPosition = Mathf.Lerp(barBottom, barTop, currentArrowPercent);

        // For UI, we use anchoredPosition instead of localPosition
        Vector2 anchoredPos = arrowRectTransform.anchoredPosition;
        anchoredPos.y = yPosition;
        arrowRectTransform.anchoredPosition = anchoredPos;

        if (arrowImage != null)
        {
            Color tier1 = isGaining ? greenTier1 : redTier1;
            Color tier2 = isGaining ? greenTier2 : redTier2;
            Color tier3 = isGaining ? greenTier3 : redTier3;
            Color tier4 = isGaining ? greenTier4 : redTier4;

            float normalizedPos = currentArrowPercent;

            Color arrowColor;
            if (normalizedPos < 0.33f)
            {
                arrowColor = Color.Lerp(tier4, tier3, normalizedPos / 0.33f);
            }
            else if (normalizedPos < 0.66f)
            {
                arrowColor = Color.Lerp(tier3, tier2, (normalizedPos - 0.33f) / 0.33f);
            }
            else
            {
                arrowColor = Color.Lerp(tier2, tier1, (normalizedPos - 0.66f) / 0.34f);
            }

            arrowImage.color = arrowColor;
        }
    }
}
