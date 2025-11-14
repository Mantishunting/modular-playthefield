using UnityEngine;

public class FoodBarRotator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Resources resourcesScript;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;

    private float lastFoodCheck;
    private float timeSinceLastCheck = 0f;
    private bool isGaining = true;

    private void Start()
    {
        if (resourcesScript != null)
        {
            lastFoodCheck = resourcesScript.GetCurrentFood();
        }
    }

    private void Update()
    {
        if (resourcesScript == null) return;

        float currentFood = resourcesScript.GetCurrentFood();

        // Check every second if direction changed
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= 1f)
        {
            float foodChange = currentFood - lastFoodCheck;

            if (foodChange > 0)
            {
                isGaining = true;
            }
            else if (foodChange < 0)
            {
                isGaining = false;
            }

            lastFoodCheck = currentFood;
            timeSinceLastCheck = 0f;
        }

        // Rotate: 0° when gaining (green up), 180° when losing (red up)
        float targetRotation = isGaining ? 0f : 180f;
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.z = Mathf.LerpAngle(currentRotation.z, targetRotation, Time.deltaTime * rotationSpeed);
        transform.localEulerAngles = currentRotation;
    }
}