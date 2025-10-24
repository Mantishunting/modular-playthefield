using UnityEngine;

public class Sun : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 500f;
    [SerializeField] private Vector3 centerPoint = Vector3.zero;

    [Header("Rotation Settings")]
    [Tooltip("Starting angle in degrees (0 = right, 90 = up, 180 = left, 270 = down)")]
    [SerializeField] private float startAngle = 90f; // Start at right

    [Tooltip("Ending angle in degrees")]
    [SerializeField] private float stopAngle = 270f; // End at left

    [Tooltip("Time in seconds to complete rotation from start to stop")]
    [SerializeField] private float rotationDuration = 10f;

    [Header("Auto Start")]
    [SerializeField] private bool startOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float currentAngle;
    private float rotationSpeed; // Degrees per second
    private bool isRotating = false;

    void Start()
    {
        // Calculate rotation speed (degrees per second)
        float totalRotation = Mathf.Abs(stopAngle - startAngle);
        rotationSpeed = totalRotation / rotationDuration;

        // Set initial position
        currentAngle = startAngle;
        UpdatePosition();

        if (startOnAwake)
        {
            StartRotation();
        }

        if (showDebugLogs)
        {
            Debug.Log($"Sun initialized: Start={startAngle}°, Stop={stopAngle}°, Duration={rotationDuration}s, Speed={rotationSpeed}°/s");
        }
    }

    void Update()
    {
        if (isRotating)
        {
            // Increment angle based on time
            currentAngle += rotationSpeed * Time.deltaTime;

            // Check if we've reached the stop angle
            if (currentAngle >= stopAngle)
            {
                currentAngle = stopAngle;
                isRotating = false;

                if (showDebugLogs)
                {
                    Debug.Log("Sun reached stop angle, rotation complete");
                }
            }

            UpdatePosition();
        }
    }

    void UpdatePosition()
    {
        // Convert angle to radians
        float angleRad = currentAngle * Mathf.Deg2Rad;

        // Calculate position on circle
        // 0° = right, 90° = up, 180° = left, 270° = down
        float x = centerPoint.x + Mathf.Cos(angleRad) * orbitRadius;
        float y = centerPoint.y + Mathf.Sin(angleRad) * orbitRadius;

        // Update position
        transform.position = new Vector3(x, y, transform.position.z);

        // Rotate to face the center
        Vector3 directionToCenter = centerPoint - transform.position;
        float angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;

        // Adjust rotation so the "long edge" faces down toward center
        // Assuming the sprite/mesh's "up" (local Y) should point toward center
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    public void StartRotation()
    {
        isRotating = true;

        if (showDebugLogs)
        {
            Debug.Log("Sun rotation started");
        }
    }

    public void StopRotation()
    {
        isRotating = false;

        if (showDebugLogs)
        {
            Debug.Log("Sun rotation stopped");
        }
    }

    public void ResetToStart()
    {
        currentAngle = startAngle;
        UpdatePosition();
        isRotating = false;

        if (showDebugLogs)
        {
            Debug.Log("Sun reset to start angle");
        }
    }

    /// <summary>
    /// Gets the direction light rays travel (from sun toward center)
    /// </summary>
    public Vector3 GetLightDirection()
    {
        return (centerPoint - transform.position).normalized;
    }

    /// <summary>
    /// Gets the current angle in degrees
    /// </summary>
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
}