using UnityEngine;

public class Bee : MonoBehaviour
{
    [Header("Base Movement")]
    public float speed = 5f;

    [Header("Large Sine Wave")]
    public float waveAmplitude = 2f;
    public float waveFrequency = 1f;

    [Header("Medium Sine Wave")]
    public float mediumWaveAmplitude = 0.8f;
    public float mediumWaveFrequency = 4f;

    [Header("Small Noise")]
    public float noiseAmplitude = 0.5f;
    public float noiseFrequency = 10f;

    private float timeAlive;
    private float noiseOffsetX;
    private float noiseOffsetY;
    private Vector2 direction;
    private Vector2 perpendicular;

    void Awake()
    {
        timeAlive = Random.Range(0f, 100f);
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetY = Random.Range(0f, 1000f);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        perpendicular = new Vector2(-direction.y, direction.x);
    }

    void Update()
    {
        timeAlive += Time.deltaTime;

        float bigWave = Mathf.Sin(timeAlive * waveFrequency) * waveAmplitude;
        float mediumWave = Mathf.Sin(timeAlive * mediumWaveFrequency) * mediumWaveAmplitude;

        float noiseX = (Mathf.PerlinNoise(timeAlive * noiseFrequency, noiseOffsetX) - 0.5f) * 2f * noiseAmplitude;
        float noiseY = (Mathf.PerlinNoise(timeAlive * noiseFrequency, noiseOffsetY) - 0.5f) * 2f * noiseAmplitude;

        Vector2 movement = direction * speed;
        movement += perpendicular * (bigWave + mediumWave);
        movement += new Vector2(noiseX, noiseY);

        transform.position += (Vector3)(movement * Time.deltaTime);

        if (LevelBounds.Instance != null && LevelBounds.Instance.IsOutside(transform.position))
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        direction = -direction;
        direction = Quaternion.Euler(0, 0, Random.Range(-30f, 30f)) * direction;
        direction = direction.normalized;
        perpendicular = new Vector2(-direction.y, direction.x);
    }
}