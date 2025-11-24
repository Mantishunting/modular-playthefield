using UnityEngine;

/// <summary>
/// Destroys the GameObject this script is attached to after a set duration.
/// </summary>
public class TimedDestroyer : MonoBehaviour
{
    [Tooltip("The time (in seconds) before the block is destroyed.")]
    [SerializeField]
    private float destroyDelay = 2.0f;

    void Start()
    {
        // Call the Destroy function on this GameObject after 'destroyDelay' seconds.
        Destroy(gameObject, destroyDelay);

        // Note: The 'gameObject' keyword refers to the GameObject 
        // this script instance is attached to (i.e., your "block").
    }
}