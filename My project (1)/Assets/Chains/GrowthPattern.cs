using UnityEngine;

[CreateAssetMenu(menuName = "Flowers/Growth Pattern", fileName = "NewGrowthPattern")]
public class GrowthPattern : ScriptableObject
{
    [Tooltip("Ordered steps to perform.")]
    public GrowthStep[] steps;

    [Tooltip("Loop when the pattern ends.")]
    public bool loop = false;

    [Tooltip("Default interval between steps (seconds).")]
    public float intervalSeconds = 0.2f;
}
