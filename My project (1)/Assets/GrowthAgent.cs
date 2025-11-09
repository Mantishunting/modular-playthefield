using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class GrowthAgent : MonoBehaviour
{
    [Header("Growth Settings")]
    public HumanClick.Direction direction = HumanClick.Direction.East;
    public float intervalSeconds = 0.5f;
    public BlockType blockType;   // assign Flower.asset or any BlockType

    private HumanClick root;

    void Awake() => root = GetComponent<HumanClick>();


    void OnEnable()
    {
        if (root != null && blockType != null)
            StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        yield return null; //  wait one frame so HumanClick.Start() runs
        while (true)
        {
            bool ok = root.TryPlaceRelative(direction, blockType, true);
            if (!ok) yield break;
            yield return new WaitForSeconds(intervalSeconds);
        }
    }
}