using UnityEngine;

public class LevelBounds : MonoBehaviour
{
    public static LevelBounds Instance { get; private set; }

    public float minX = -30f;
    public float maxX = 30f;
    public float minY = -20f;
    public float maxY = 20f;

    void Awake()
    {
        Instance = this;
    }

    public bool IsOutside(Vector2 position)
    {
        return position.x < minX || position.x > maxX ||
               position.y < minY || position.y > maxY;
    }

    public Vector2 GetRandomPointOnEdge(out Vector2 inwardDirection)
    {
        bool spawnLeft = Random.value > 0.5f;
        Vector2 point;

        if (spawnLeft)
        {
            point = new Vector2(minX, Random.Range(minY, maxY));
            inwardDirection = Vector2.right;
        }
        else
        {
            point = new Vector2(maxX, Random.Range(minY, maxY));
            inwardDirection = Vector2.left;
        }

        return point;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}