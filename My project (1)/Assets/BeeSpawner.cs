using UnityEngine;

public class BeeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject beePrefab;
    public float beesPerSecond = 0.5f;

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

        Vector2 inwardDirection;
        Vector2 spawnPoint = LevelBounds.Instance.GetRandomPointOnEdge(out inwardDirection);

        inwardDirection = Quaternion.Euler(0, 0, Random.Range(-30f, 30f)) * inwardDirection;

        GameObject bee = Instantiate(beePrefab, spawnPoint, Quaternion.identity);
        bee.GetComponent<Bee>().SetDirection(inwardDirection);
    }
}