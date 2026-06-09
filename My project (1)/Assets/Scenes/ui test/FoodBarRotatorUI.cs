using UnityEngine;
using System.Collections.Generic;

public class FoodBarRotatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ResourceManager resourcesScript;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [Tooltip("When true, bar is upright for gaining and upside-down for losing. When false, it's reversed.")]
    [SerializeField] private bool normalOrientation = true;

    [Header("Food Tracking Settings")]
    [Tooltip("How long to track food changes (in seconds). Compares current food to food from this many seconds ago.")]
    [SerializeField] private float trackingPeriod = 10f;

    [Tooltip("How often to sample food amount (in seconds). Lower = more accurate, higher = more efficient")]
    [SerializeField] private float sampleInterval = 1f;

    private RectTransform rectTransform;
    private float currentRotation = 0f;
    private float targetRotation = 0f;

    // Food history tracking
    private Queue<FoodSample> foodHistory = new Queue<FoodSample>();
    private float sampleTimer = 0f;
    private bool isGaining = true;

    private struct FoodSample
    {
        public float amount;
        public float timestamp;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("FoodBarRotatorUI requires a RectTransform component!");
        }
    }

    private void Start()
    {
        // Initialize with first sample
        if (resourcesScript != null)
        {
            AddFoodSample(resourcesScript.GetCurrentFood());
        }

        // Set initial rotation
        targetRotation = GetTargetRotation(isGaining);
        currentRotation = targetRotation;

        if (rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        }
    }

    private void Update()
    {
        if (resourcesScript == null || rectTransform == null) return;

        // Update sample timer
        sampleTimer += Time.deltaTime;

        // Take a new food sample at intervals
        if (sampleTimer >= sampleInterval)
        {
            float currentFood = resourcesScript.GetCurrentFood();
            AddFoodSample(currentFood);

            // Remove old samples outside the tracking period
            float cutoffTime = Time.time - trackingPeriod;
            while (foodHistory.Count > 0 && foodHistory.Peek().timestamp < cutoffTime)
            {
                foodHistory.Dequeue();
            }

            // Compare current food to oldest sample in history
            if (foodHistory.Count > 1)
            {
                float oldestFood = foodHistory.Peek().amount;
                float foodDifference = currentFood - oldestFood;

                // Update gaining/losing state based on difference
                bool wasGaining = isGaining;
                isGaining = foodDifference >= 0;

                // If state changed, update target rotation
                if (isGaining != wasGaining)
                {
                    targetRotation = GetTargetRotation(isGaining);
                }
            }

            // Reset timer
            sampleTimer = 0f;
        }

        // Always smoothly rotate toward target (runs every frame for smooth animation)
        if (!Mathf.Approximately(currentRotation, targetRotation))
        {
            currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSpeed);
            rectTransform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        }
    }

    private void AddFoodSample(float amount)
    {
        foodHistory.Enqueue(new FoodSample
        {
            amount = amount,
            timestamp = Time.time
        });
    }

    private float GetTargetRotation(bool gaining)
    {
        if (normalOrientation)
        {
            // Normal: gaining = 0°, losing = 180°
            return gaining ? 0f : 180f;
        }
        else
        {
            // Reversed: gaining = 180°, losing = 0°
            return gaining ? 180f : 0f;
        }
    }

    /// <summary>
    /// Instantly snap to correct orientation (useful for initialization)
    /// </summary>
    public void SnapToOrientation()
    {
        if (rectTransform != null)
        {
            float rotation = GetTargetRotation(isGaining);
            currentRotation = rotation;
            targetRotation = rotation;
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }
    }
}