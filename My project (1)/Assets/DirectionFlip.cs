using UnityEngine;

public class BeeAutoFlip : MonoBehaviour
{
    private Vector3 originalScale;
    private bool isFacingRight = true;
    private Vector3 lastPosition;

    void Start()
    {
        originalScale = transform.localScale;
        lastPosition = transform.position;
    }

    void Update()
    {
        // Auto-flip based on movement direction
        Vector3 movement = transform.position - lastPosition;

        if (movement.x > 0.01f && !isFacingRight)
        {
            Flip();
        }
        else if (movement.x < -0.01f && isFacingRight)
        {
            Flip();
        }

        lastPosition = transform.position;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(
            originalScale.x * (isFacingRight ? 1 : -1),
            originalScale.y,
            originalScale.z
        );
    }
}