using UnityEngine;

public class BeeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject beePrefab;
    public float beesPerSecond = 0.5f;

    [Header("Height Distribution")]
    [Tooltip("How strongly bees prefer spawning at higher Y. 1 = uniform, 2 = moderate top bias, 3+ = strong top bias")]
    [Range(1f, 5f)]
    public float heightBias = 2f;

    [Tooltip("Minimum Y as percentage of level height (0 = bottom, 1 = top)")]
    [Range(0f, 1f)]
    public float minHeightPercent = 0.3f;

    [Header("Timing")]
    public Sun sun;
    public float emissionDuration = 50f;
    public float emissionOffset = 0f; // + shifts later, - shifts earlier

    private float spawnTimer;

    void Update()
    {
        if (!ShouldEmit()) return;

        spawnTimer += Time.deltaTime;

        float interval = 1f / beesPerSecond;

        if (spawnTimer >= interval)
        {
            SpawnBee();
            spawnTimer = 0f;
        }
    }

    bool ShouldEmit()
    {
        if (sun == null) return false;

        float dayLength = sun.rotationDuration;
        float midDay = dayLength / 2f;
        float halfEmission = emissionDuration / 2f;

        float emitStart = midDay - halfEmission + emissionOffset;
        float emitEnd = midDay + halfEmission + emissionOffset;

        // Convert current angle to time elapsed
        float angleProgress = sun.GetCurrentAngle() - sun.startAngle;
        float totalAngle = sun.stopAngle - sun.startAngle;
        float timeProgress = (angleProgress / totalAngle) * dayLength;

        return timeProgress >= emitStart && timeProgress <= emitEnd;
    }

    void SpawnBee()
    {
        if (beePrefab == null || LevelBounds.Instance == null) return;

        Vector2 spawnPoint = GetHeightBiasedSpawnPoint(out Vector2 inwardDirection);

        inwardDirection = Quaternion.Euler(0, 0, Random.Range(-30f, 30f)) * inwardDirection;

        GameObject bee = Instantiate(beePrefab, spawnPoint, Quaternion.identity);
        bee.GetComponent<Bee>().SetDirection(inwardDirection);
    }

    Vector2 GetHeightBiasedSpawnPoint(out Vector2 inwardDirection)
    {
        LevelBounds bounds = LevelBounds.Instance;

        // Pick left or right edge
        bool spawnLeft = Random.value > 0.5f;
        float x = spawnLeft ? bounds.minX : bounds.maxX;
        inwardDirection = spawnLeft ? Vector2.right : Vector2.left;

        // Calculate Y range based on minHeightPercent
        float totalHeight = bounds.maxY - bounds.minY;
        float minY = bounds.minY + (totalHeight * minHeightPercent);
        float maxY = bounds.maxY;

        // Use power distribution to bias toward top
        // Random.value^(1/bias) shifts distribution toward 1 (top)
        float t = Mathf.Pow(Random.value, 1f / heightBias);
        float y = Mathf.Lerp(minY, maxY, t);

        return new Vector2(x, y);
    }
}