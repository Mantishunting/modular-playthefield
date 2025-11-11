using UnityEngine;
using System.Collections;

public class LeafProduction : MonoBehaviour
{
    [Header("Sun Settings")]
    [SerializeField] private bool requireSunlight = true;
    [Tooltip("If false, produces food constantly like before sun system")]

    [Header("Raycast Settings")]
    [SerializeField] private Vector2 boxCastSize = new Vector2(0.6f, 0.6f);
    [SerializeField] private float originOffset = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private HumanClick humanClick;
    private BlockType myBlockType;
    private Sun sun;

    private bool isProducing = false;
    private float lightCheckInterval = 0.25f;

    private float timeAlive = 0f;
    private bool isAlive = true;

    void Start()
    {
        humanClick = GetComponent<HumanClick>();
        if (humanClick == null)
        {
            Debug.LogError("LeafProduction requires HumanClick component!");
            return;
        }

        myBlockType = humanClick.GetBlockType();
        if (myBlockType == null)
        {
            Debug.LogError("LeafProduction: BlockType is null!");
            return;
        }

        sun = FindObjectOfType<Sun>();
        if (sun == null)
        {
            Debug.LogWarning("LeafProduction: No Sun found in scene! Production will not work.");
        }

        if (myBlockType.producesResources)
        {
            StartCoroutine(LightCheckingLoop());

            if (showDebugLogs)
            {
                Debug.Log($"LeafProduction started on {myBlockType.blockName} at {transform.position}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"Block type {myBlockType.blockName} does not produce resources, production disabled.");
            }
        }

        if (myBlockType.lifespanSeconds > 0)
        {
            StartCoroutine(LifespanTimer());

            if (showDebugLogs)
            {
                Debug.Log($"{myBlockType.blockName} at {transform.position} will die after {myBlockType.lifespanSeconds} seconds");
            }
        }
    }

    IEnumerator LightCheckingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(lightCheckInterval);

            bool isLit = IsSunShining();

            if (isLit && !isProducing)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Leaf at {transform.position} detected sunlight, starting production");
                }

                StartCoroutine(ProductionLoop());
            }
        }
    }

    IEnumerator ProductionLoop()
    {
        isProducing = true;

        while (true)
        {
            yield return new WaitForSeconds(myBlockType.productionRate);

            bool isStillLit = IsSunShining();

            if (isStillLit)
            {
                ProduceFood();
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Leaf at {transform.position} no longer lit, stopping production");
                }

                isProducing = false;
                yield break;
            }
        }
    }

    bool IsSunShining()
    {
        if (!requireSunlight) return true;
        if (sun == null) return false;

        Vector3 toSun = -sun.GetLightDirection();
        float rayDistance = Vector3.Distance(transform.position, Vector3.zero) + sun.GetOrbitRadius() + 100f;

        Vector3 rayOrigin = transform.position + (toSun * originOffset);
        float angle = 0f;

        int blockingLayers = LayerMask.GetMask("Default");
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxCastSize, angle, toSun, rayDistance, blockingLayers);

        if (showDebugLogs)
        {
            Vector2 perp = Vector2.Perpendicular(toSun).normalized;
            Vector2 right = perp * (boxCastSize.x * 0.5f);
            Vector2 up = toSun * (boxCastSize.y * 0.5f);

            Vector2 topLeftStart = (Vector2)rayOrigin - right + up;
            Vector2 topRightStart = (Vector2)rayOrigin + right + up;
            Vector2 bottomLeftStart = (Vector2)rayOrigin - right - up;
            Vector2 bottomRightStart = (Vector2)rayOrigin + right - up;

            Vector2 move = toSun * rayDistance;

            Color guideColor = hit.collider != null ? Color.red : Color.green;

            Debug.DrawLine(topLeftStart, topLeftStart + move, guideColor, 0.25f);
            Debug.DrawLine(topRightStart, topRightStart + move, guideColor, 0.25f);
            Debug.DrawLine(bottomLeftStart, bottomLeftStart + move, guideColor, 0.25f);
            Debug.DrawLine(bottomRightStart, bottomRightStart + move, guideColor, 0.25f);

            Debug.DrawLine(topLeftStart, topRightStart, Color.cyan, 0.25f);
            Debug.DrawLine(topRightStart, bottomRightStart, Color.cyan, 0.25f);
            Debug.DrawLine(bottomRightStart, bottomLeftStart, Color.cyan, 0.25f);
            Debug.DrawLine(bottomLeftStart, topLeftStart, Color.cyan, 0.25f);

            if (hit.collider != null)
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.red, 0.25f);
                Debug.Log($"[LeafProduction] BoxCast HIT {hit.collider.name} at {hit.point}");
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + toSun * rayDistance, Color.green, 0.25f);
                Debug.Log("[LeafProduction] BoxCast hit nothing — sunlit");
            }
        }

        if (hit.collider != null)
        {
            bool hitIsSelf = hit.collider.transform.IsChildOf(this.transform);

            if (!hitIsSelf)
            {
                return false;
            }
        }

        return true;
    }

    void ProduceFood()
    {
        if (Resources.Instance == null)
        {
            Debug.LogError("Resources.Instance is null! Cannot produce food.");
            return;
        }

        Resources.Instance.AddFood(myBlockType.productionAmount);

        if (showDebugLogs)
        {
            Debug.Log($"{myBlockType.blockName} block at {transform.position} produced {myBlockType.productionAmount} food!");
        }
    }

    IEnumerator LifespanTimer()
    {
        while (isAlive)
        {
            timeAlive += Time.deltaTime;

            if (timeAlive >= myBlockType.lifespanSeconds)
            {
                DieOfOldAge();
                yield break;
            }

            yield return null;
        }
    }

    void DieOfOldAge()
    {
        isAlive = false;

        if (showDebugLogs)
        {
            Debug.Log($"{myBlockType.blockName} at {transform.position} died of old age after {timeAlive:F1} seconds");
        }

        if (humanClick != null)
        {
            humanClick.Die();
        }
    }

    void OnDestroy()
    {
        if (showDebugLogs && myBlockType != null)
        {
            Debug.Log($"LeafProduction stopped on {myBlockType.blockName} at {transform.position}");
        }
    }
}