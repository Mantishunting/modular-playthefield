using UnityEngine;

[CreateAssetMenu(fileName = "NewBracketState", menuName = "Bracket/Animation State")]
public class BracketAnimationState : ScriptableObject
{
    [Header("State Info")]
    [Tooltip("Optional description to help identify this state")]
    public string stateDescription = "";
    
    [Header("Grid Settings")]
    [Tooltip("Distance between grid points")]
    [Range(0.05f, 0.5f)]
    public float spacing = 0.15f;
    
    [Tooltip("Base size of each bracket")]
    [Range(0.01f, 0.3f)]
    public float bracketSize = 0.1f;
    
    [Tooltip("How many grid cells extend from center in each direction")]
    [Range(1, 20)]
    public int gridCount = 8;
    
    [Header("Edge Culling")]
    [Tooltip("Don't spawn brackets in leftmost X% of texture")]
    [Range(0f, 0.5f)]
    public float leftEdge = 0f;
    
    [Tooltip("Don't spawn brackets in rightmost X% of texture")]
    [Range(0f, 0.5f)]
    public float rightEdge = 0f;
    
    [Tooltip("Don't spawn brackets in topmost X% of texture")]
    [Range(0f, 0.5f)]
    public float topEdge = 0f;
    
    [Tooltip("Don't spawn brackets in bottommost X% of texture")]
    [Range(0f, 0.5f)]
    public float bottomEdge = 0f;
    
    [Header("Scale Range")]
    [Tooltip("Minimum random scale for brackets")]
    [Range(0.1f, 3.0f)]
    public float minScale = 0.5f;
    
    [Tooltip("Maximum random scale for brackets")]
    [Range(0.1f, 3.0f)]
    public float maxScale = 1.5f;
    
    [Header("Rotation")]
    [Tooltip("Random rotation range for each bracket (±degrees)")]
    [Range(0f, 180f)]
    public float localRotationRange = 30f;
    
    [Tooltip("Global rotation applied to all brackets (degrees)")]
    [Range(0f, 360f)]
    public float globalRotation = 0f;
    
    [Header("Position Randomness")]
    [Tooltip("Static random offset from perfect grid position")]
    [Range(0f, 1f)]
    public float staticJitter = 0.3f;
    
    [Tooltip("Amount of animated wiggle movement")]
    [Range(0f, 1f)]
    public float wiggleAmount = 0.1f;
    
    [Tooltip("Speed of wiggle animation")]
    [Range(0f, 10f)]
    public float wiggleSpeed = 2.0f;
}
