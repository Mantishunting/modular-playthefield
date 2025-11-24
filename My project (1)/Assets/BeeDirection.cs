using UnityEngine;

public class BeeFaceMovementDirection : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 lastPosition;

    [SerializeField] private float flipThreshold = 0.01f; // Minimum movement to trigger flip

    void Start()
    {
        originalScale = transform.localScale;
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        Vector3 movement = transform.position - lastPosition;

        // Only flip if there's significant horizontal movement
        if (Mathf.Abs(movement.x) > flipThreshold)
        {
            if (movement.x > 0)
            {
                // Moving right - face right (positive scale)
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
            else if (movement.x < 0)
            {
                // Moving left - face left (negative scale)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
        }

        lastPosition = transform.position;
    }
}