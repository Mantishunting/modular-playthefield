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
    [Tooltip("Will this block produce resources? (Not implemented yet)")]
    public bool producesResources = false;

    [Tooltip("Resource production rate (Not implemented yet)")]
    public float productionRate = 0f;
}