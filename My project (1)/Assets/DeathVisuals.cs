using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns a death effect prefab when this object is destroyed.
/// This requires ZERO changes to HumanClick.cs.
/// It works by listening for the OnDestroy() message, which is
/// called by Unity right before the object is destroyed.
/// </summary>
public class DeathVisuals : MonoBehaviour
{
    [Header("Death Effect")]
    [Tooltip("The prefab (with Rigidbody and animation) to spawn on death")]
    [SerializeField] private GameObject deathEffectPrefab;

    private HumanClick humanClick; // We only get this to make sure we're on the right object

    void Start()
    {
        // This script still needs to be on the same object as HumanClick
        humanClick = GetComponent<HumanClick>();
        if (humanClick == null)
        {
            Debug.LogError("DeathVisuals: No HumanClick component found on " + gameObject.name);
            enabled = false;
        }
    }

    /// <summary>
    /// This is called by Unity automatically when HumanClick.Die()
    /// (or anything else) calls Destroy(gameObject) on this object.
    /// </summary>
    void OnDestroy()
    {
        // Check if the prefab is assigned and that we're not just
        // quitting the application (which also fires OnDestroy)
        if (deathEffectPrefab != null && gameObject.scene.isLoaded)
        {
            // Spawn the prefab at this block's position and rotation
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }
    }
}