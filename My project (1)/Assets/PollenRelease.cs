using UnityEngine;

/// <summary>
/// Spawns pollen effect prefabs when this flower is visited by a bee.
/// Hooks into the Flower.OnFlowerVisited event system.
/// </summary>
public class PollenRelease : MonoBehaviour
{
    [Header("Pollen Effect")]
    [Tooltip("The prefab (with Rigidbody and animation) to spawn on visit")]
    [SerializeField] private GameObject pollenEffectPrefab;

    [Tooltip("How many pollen particles to spawn per visit")]
    [SerializeField] private int pollenCount = 3;

    [Tooltip("Random spread radius for spawned pollen")]
    [SerializeField] private float spreadRadius = 0.5f;

    [Tooltip("Initial upward force applied to pollen")]
    [SerializeField] private float upwardForce = 2f;

    [Tooltip("Random horizontal force range")]
    [SerializeField] private float horizontalForce = 1f;

    private Flower flower;

    void Start()
    {
        flower = GetComponent<Flower>();
        if (flower == null)
        {
            Debug.LogError("PollenRelease: No Flower component found on " + gameObject.name);
            enabled = false;
        }
    }

    void OnEnable()
    {
        Flower.OnFlowerVisited += HandleFlowerVisited;
    }

    void OnDisable()
    {
        Flower.OnFlowerVisited -= HandleFlowerVisited;
    }

    /// <summary>
    /// Called whenever any flower is visited. Only spawn pollen if THIS flower was visited.
    /// </summary>
    private void HandleFlowerVisited(Flower visitedFlower)
    {
        if (visitedFlower == flower)
        {
            SpawnPollen();
        }
    }

    private void SpawnPollen()
    {
        if (pollenEffectPrefab == null) return;

        for (int i = 0; i < pollenCount; i++)
        {
            // Spawn with slight random offset
            Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * spreadRadius);
            GameObject pollen = Instantiate(pollenEffectPrefab, spawnPos, Quaternion.identity);

            // Add some upward and random horizontal velocity
            Rigidbody2D rb = pollen.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 force = new Vector2(
                    Random.Range(-horizontalForce, horizontalForce),
                    upwardForce
                );
                rb.AddForce(force, ForceMode2D.Impulse);
            }
        }
    }
}