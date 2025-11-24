using UnityEngine;

public class BeeWingFlapper : MonoBehaviour
{
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;

    [SerializeField] private float flipsPerSecond = 10f;
    [SerializeField] private float upwardMovement = 0.5f; // How far to move up
    [SerializeField] private float flipAngle = 180f; // Angle to flip (180 for full flip)

    private Vector3 leftWingOriginalPosition;
    private Vector3 rightWingOriginalPosition;
    private Vector3 leftWingOriginalScale;
    private Vector3 rightWingOriginalScale;

    private float timer = 0f;
    private bool isFlipped = false;

    void Start()
    {
        // Store original positions and scales
        leftWingOriginalPosition = leftWing.localPosition;
        rightWingOriginalPosition = rightWing.localPosition;
        leftWingOriginalScale = leftWing.localScale;
        rightWingOriginalScale = rightWing.localScale;
    }

    void Update()
    {
        float flipDuration = 1f / (flipsPerSecond * 2f); // Divide by 2 for flip and flip back

        timer += Time.deltaTime;

        if (timer >= flipDuration)
        {
            timer = 0f;
            isFlipped = !isFlipped;

            if (isFlipped)
            {
                // Move up and flip
                leftWing.localPosition = leftWingOriginalPosition + Vector3.up * upwardMovement;
                rightWing.localPosition = rightWingOriginalPosition + Vector3.up * upwardMovement;

                // Flip by inverting scale on Y axis (or X depending on your sprite orientation)
                leftWing.localScale = new Vector3(leftWingOriginalScale.x, -leftWingOriginalScale.y, leftWingOriginalScale.z);
                rightWing.localScale = new Vector3(rightWingOriginalScale.x, -rightWingOriginalScale.y, rightWingOriginalScale.z);
            }
            else
            {
                // Move back down and flip back to original
                leftWing.localPosition = leftWingOriginalPosition;
                rightWing.localPosition = rightWingOriginalPosition;
                leftWing.localScale = leftWingOriginalScale;
                rightWing.localScale = rightWingOriginalScale;
            }
        }
    }
}