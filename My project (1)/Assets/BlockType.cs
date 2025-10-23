using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockType", menuName = "Block System/Block Type")]
public class BlockType : ScriptableObject
{
    [Header("Basic Info")]
    public string blockName;
    public Color blockColor = Color.white;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Future Properties")]
    [Tooltip("Will this block produce resources? (Not implemented yet)")]
    public bool producesResources = false;

    [Tooltip("Resource production rate (Not implemented yet)")]
    public float productionRate = 0f;
}