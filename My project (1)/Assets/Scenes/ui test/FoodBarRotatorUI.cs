using UnityEngine;

public class FoodBarRotatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FoodDisplayBarUI foodDisplayBar;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [Tooltip("When true, bar is upright for gaining and upside-down for losing. When false, it's reversed.")]
    [SerializeField] private bool normalOrientation = true;

    private RectTransform rectTransform;
    private bool wasGaining = true;
    private float currentRotation = 0f;
    private float targetRotation = 0f;

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
        if (foodDisplayBar != null)
        {
            wasGaining = foodDisplayBar.IsGaining;
            targetRotation = GetTargetRotation(wasGaining);
            currentRotation = targetRotation;
            
            // Set initial rotation immediately
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.Euler(0, 0, currentRotation);
            }
        }
    }

    private void Update()
    {
        if (foodDisplayBar == null || rectTransform == null) return;

        bool isCurrentlyGaining = foodDisplayBar.IsGaining;

        // Check if state changed
        if (isCurrentlyGaining != wasGaining)
        {
            wasGaining = isCurrentlyGaining;
            targetRotation = GetTargetRotation(isCurrentlyGaining);
        }

        // Smoothly rotate toward target
        currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSpeed);
        rectTransform.localRotation = Quaternion.Euler(0, 0, currentRotation);
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

    // Optional: Method to instantly snap to correct orientation (useful for initialization)
    public void SnapToOrientation()
    {
        if (foodDisplayBar != null && rectTransform != null)
        {
            bool isGaining = foodDisplayBar.IsGaining;
            float rotation = GetTargetRotation(isGaining);
            currentRotation = rotation;
            targetRotation = rotation;
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }
    }
}
