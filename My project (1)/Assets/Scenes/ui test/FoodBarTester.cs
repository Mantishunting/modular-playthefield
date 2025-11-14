using UnityEngine;

public class FoodBarTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Resources resourcesScript;

    [Header("Test Controls")]
    [SerializeField] private float changeRate = 10f;
    [SerializeField] private bool isGaining = true;

    [Header("Quick Set Buttons")]
    [SerializeField] private bool setToZero;
    [SerializeField] private bool setToTen;
    [SerializeField] private bool setToHundred;
    [SerializeField] private bool setToThousand;

    [Header("Animated Transition")]
    [SerializeField] private bool startTransition;
    [SerializeField] private int fromAmount = 0;
    [SerializeField] private int toAmount = 1000;
    [SerializeField] private float transitionDurationSeconds = 5f;

    [Header("Ramping Food Gain")]
    [SerializeField] private bool startRamping;
    [SerializeField] private float rampStartRate = 1f; // Food per second at start
    [SerializeField] private float rampEndRate = 100f; // Food per second at end
    [SerializeField] private float rampDuration = 10f; // How long to ramp up
    [SerializeField] private AnimationCurve rampCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private int transitionStartValue;
    private int transitionTargetValue;
    private float transitionDuration;

    private bool isRamping = false;
    private float rampTimer = 0f;

    private void Update()
    {
        if (resourcesScript == null)
        {
            Debug.LogError("Resources script is not assigned!");
            return;
        }

        // Handle ramping start
        if (startRamping)
        {
            startRamping = false;
            StartRamping();
        }

        // Update ramping
        if (isRamping)
        {
            UpdateRamping();
            return; // Don't do other updates during ramping
        }

        // Handle animated transition
        if (startTransition)
        {
            startTransition = false;
            StartAnimatedTransition();
        }

        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        // Quick-set buttons
        if (setToZero)
        {
            setToZero = false;
            SetFoodTo(0);
        }
        if (setToTen)
        {
            setToTen = false;
            SetFoodTo(10);
        }
        if (setToHundred)
        {
            setToHundred = false;
            SetFoodTo(100);
        }
        if (setToThousand)
        {
            setToThousand = false;
            SetFoodTo(1000);
        }

        // Continuous change
        int delta = Mathf.RoundToInt(changeRate * Time.deltaTime);
        if (delta > 0)
        {
            if (isGaining)
            {
                resourcesScript.AddFood(delta);
            }
            else
            {
                resourcesScript.TrySpendFood(delta);
            }
        }
    }

    private void StartRamping()
    {
        isRamping = true;
        rampTimer = 0f;
        Debug.Log($"Starting ramp from {rampStartRate}/sec to {rampEndRate}/sec over {rampDuration} seconds");
    }

    private void UpdateRamping()
    {
        rampTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(rampTimer / rampDuration);

        // Use curve to determine current rate
        float curveValue = rampCurve.Evaluate(progress);
        float currentRate = Mathf.Lerp(rampStartRate, rampEndRate, curveValue);

        // Add food at current rate
        int foodToAdd = Mathf.RoundToInt(currentRate * Time.deltaTime);
        if (foodToAdd > 0)
        {
            resourcesScript.AddFood(foodToAdd);
        }

        Debug.Log($"Ramping: Progress {progress:F2}, Rate: {currentRate:F2}/sec");

        // Check if complete
        if (progress >= 1f)
        {
            isRamping = false;
            Debug.Log("Ramping complete!");
        }
    }

    private void StartAnimatedTransition()
    {
        SetFoodTo(fromAmount);

        transitionStartValue = fromAmount;
        transitionTargetValue = toAmount;
        transitionDuration = transitionDurationSeconds;
        transitionTimer = 0f;
        isTransitioning = true;

        Debug.Log($"Starting transition from {fromAmount} to {toAmount} over {transitionDurationSeconds} seconds");
    }

    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(transitionTimer / transitionDuration);

        int targetValue = Mathf.RoundToInt(Mathf.Lerp(transitionStartValue, transitionTargetValue, progress));
        int currentValue = resourcesScript.GetCurrentFood();

        if (targetValue > currentValue)
        {
            resourcesScript.AddFood(targetValue - currentValue);
        }
        else if (targetValue < currentValue)
        {
            resourcesScript.TrySpendFood(currentValue - targetValue);
        }

        if (progress >= 1f)
        {
            isTransitioning = false;
            Debug.Log($"Transition complete! Final value: {resourcesScript.GetCurrentFood()}");
        }
    }

    private void SetFoodTo(int target)
    {
        int current = resourcesScript.GetCurrentFood();
        if (target > current)
        {
            resourcesScript.AddFood(target - current);
        }
        else if (target < current)
        {
            resourcesScript.TrySpendFood(current - target);
        }
    }
}