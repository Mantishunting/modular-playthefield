using UnityEngine;

/// <summary>
/// Defines what triggers advancement to the next step
/// NOTE: Next button ALWAYS advances regardless of trigger type (allows players to skip)
/// These trigger types define the ACTION that also advances the step
/// </summary>
public enum TriggerType
{
    NextButtonOnly,          // Only Next button advances (no action required)
    BlockCountIncrease,      // Placing blocks also advances
    BlockCountDecrease,      // Deleting blocks also advances
    LeafButtonClicked,       // Selecting Leaf block type also advances
    FlowerButtonClicked,     // Selecting Flower block type also advances (not currently used)
    BeeVisitedFlower        // Bee visiting flower also advances
}

/// <summary>
/// Data for a single tutorial step
/// This is a Serializable class so it can be edited in the Unity Inspector
/// </summary>
[System.Serializable]
public class TutorialStep
{
    [Header("Message")]
    [TextArea(2, 4)]
    public string messageText = "Tutorial message here";
    public int requiredBlockCount = -1;

    [Header("UI Elements")]
    public bool showNextButton = true;
    public bool showArrow = false;

    [Header("Arrow Settings")]
    [Tooltip("Is this arrow in world space (pointing at blocks) or UI space (pointing at buttons)?")]
    public bool arrowInWorldSpace = true; // Default to world space for blocks

    [Tooltip("Rotation in degrees: 0=right, 90=up, 180=left, 270=down")]
    public float arrowRotation = 0f;

    [Tooltip("World position where arrow should point (used if arrowInWorldSpace = true)")]
    public Vector3 arrowTargetPosition = Vector3.zero;

    [Tooltip("UI position where arrow should point (used if arrowInWorldSpace = false)")]
    public Vector2 arrowUIPosition = Vector2.zero;

    [Tooltip("Should the arrow bounce/animate?")]
    public bool arrowShouldBounce = false;

    [Header("Advancement")]
    public TriggerType triggerType = TriggerType.NextButtonOnly;

    [Header("Notes (not used by code)")]
    [TextArea(1, 3)]
    public string designerNotes = "Add notes about this step here for reference";
}