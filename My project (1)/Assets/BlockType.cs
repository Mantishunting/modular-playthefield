using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockType", menuName = "Block System/Block Type")]
public class BlockType : ScriptableObject
{
    [Header("Basic Info")]
    public string blockName;
    public Color blockColor = Color.white;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Cost")]
    [Tooltip("How much Food it costs to place this block")]
    public int cost = 1;

    [Header("Future Properties")]
    [Tooltip("Will this block produce resources?")]
    public bool producesResources = false;

    [Tooltip("Production interval in seconds (e.g., 5 = produce every 5 seconds)")]
    public float productionRate = 5f;

    [Header("Lifespan")]
    [Tooltip("How long this block lives in seconds. Set to 0 for immortal (lives forever).")]
    public float lifespanSeconds = 0f; // 0 = immortal
}