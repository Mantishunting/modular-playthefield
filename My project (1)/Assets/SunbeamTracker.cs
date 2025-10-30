using UnityEngine;

/// <summary>
/// Attach this to the "Sunbeam" object.
/// This script makes the Sunbeam follow a target (the Sun)
/// and point in the opposite direction of the Sun's 'up' vector.
///
/// **IMPORTANT:** Do NOT make the Sunbeam a child of the Sun.
/// </summary>
public class SunbeamTracker : MonoBehaviour
{
    [Tooltip("Drag your 'Sun' object here from the Hierarchy.")]
    [SerializeField] private Transform sunTarget;

    // We use LateUpdate to make sure the sun has
    // finished all its movement from its own 'Update' script.
    void LateUpdate()
    {
        if (sunTarget == null)
        {
            // Do nothing if the sun isn't set, to prevent errors
            return;
        }

        // --- STEP 1: MATCH THE POSITION ---
        // Set our position to be the exact same as the sun's position.
        transform.position = sunTarget.position;

        // --- STEP 2: SET THE ROTATION ---
        // Your Sun.cs script rotates the "Sun" object so its 'up'
        // direction points TOWARD the center.
        // We want to point AWAY.
        // So, we copy the sun's rotation, then add 180 degrees
        // to our own Z-axis to flip ourselves around.

        // Create a 180-degree rotation (around the Z-axis for 2D)
        Quaternion flipRotation = Quaternion.Euler(0, 0, 180);

        // Apply the sun's rotation *first*, then apply our flip.
        // This makes us point perfectly outward.
        transform.rotation = sunTarget.rotation * flipRotation;
    }
}