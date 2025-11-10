using UnityEngine;

/// <summary>
/// Detects the junction type of a block based on its parent and children,
/// and updates the visual state accordingly using BracketStateController.
///
/// This component should be attached to blocks that need visual updates
/// based on their tree structure (currently: Wood blocks).
/// </summary>
public class StructureVisuals : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Reference to the HumanClick component (auto-found if null)")]
    [SerializeField] private HumanClick humanClick;

    [Tooltip("Reference to the BracketStateController (auto-found if null)")]
    [SerializeField] private BracketStateController stateController;

    [Header("Visual States - Assign Your BracketAnimationState Assets")]
    [Tooltip("State for vertical straight connections (parent South, child North or vice versa)")]
    [SerializeField] private BracketAnimationState straightVerticalState;

    [Tooltip("State for horizontal straight connections (parent West, child East or vice versa)")]
    [SerializeField] private BracketAnimationState straightHorizontalState;

    [Tooltip("State for North-East corner (parent from South or West, child to North or East)")]
    [SerializeField] private BracketAnimationState cornerNorthEastState;

    [Tooltip("State for North-West corner (parent from South or East, child to North or West)")]
    [SerializeField] private BracketAnimationState cornerNorthWestState;

    [Tooltip("State for South-East corner (parent from North or West, child to South or East)")]
    [SerializeField] private BracketAnimationState cornerSouthEastState;

    [Tooltip("State for South-West corner (parent from North or East, child to South or West)")]
    [SerializeField] private BracketAnimationState cornerSouthWestState;

    [Tooltip("Default/fallback state (for complex junctions, multiple children, or leaf blocks)")]
    [SerializeField] private BracketAnimationState standardState;

    [Header("Debug")]
    [Tooltip("Log junction changes to console?")]
    [SerializeField] private bool showDebugLogs = false;

    // Current detected junction type (for debugging)
    private JunctionType currentJunction = JunctionType.Standard;

    void Start()
    {
        // Auto-find components if not assigned
        if (humanClick == null)
        {
            humanClick = GetComponent<HumanClick>();
            if (humanClick == null)
            {
                Debug.LogError("StructureVisuals: No HumanClick component found on " + gameObject.name);
                enabled = false;
                return;
            }
        }

        if (stateController == null)
        {
            stateController = GetComponent<BracketStateController>();
            if (stateController == null)
            {
                Debug.LogError("StructureVisuals: No BracketStateController component found on " + gameObject.name);
                enabled = false;
                return;
            }
        }

        // Subscribe to connection change events
        humanClick.OnConnectionsChanged += OnConnectionsChanged;

        // Do initial junction detection
        UpdateJunction();
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (humanClick != null)
        {
            humanClick.OnConnectionsChanged -= OnConnectionsChanged;
        }
    }

    private void OnConnectionsChanged()
    {
        UpdateJunction();
        // TODO: shiver wave hook lives here later
    }

    public void UpdateJunction()
    {
        JunctionType newJunction = DetectJunction();

        if (newJunction != currentJunction)
        {
            currentJunction = newJunction;
            ApplyVisualState(newJunction);

            if (showDebugLogs)
            {
                Debug.Log($"StructureVisuals [{gameObject.name}]: Junction changed to {newJunction}");
            }
        }
    }

    private JunctionType DetectJunction()
    {
        HumanClick.Direction parentDir = humanClick.GetParentDirection();

        bool hasNorth = humanClick.GetNorthChild() != null;
        bool hasSouth = humanClick.GetSouthChild() != null;
        bool hasEast = humanClick.GetEastChild() != null;
        bool hasWest = humanClick.GetWestChild() != null;

        int childCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0) + (hasEast ? 1 : 0) + (hasWest ? 1 : 0);

        // STRAIGHTS
        if (childCount == 1)
        {
            if ((parentDir == HumanClick.Direction.North && hasSouth) ||
                (parentDir == HumanClick.Direction.South && hasNorth))
                return JunctionType.StraightVertical;

            if ((parentDir == HumanClick.Direction.West && hasEast) ||
                (parentDir == HumanClick.Direction.East && hasWest))
                return JunctionType.StraightHorizontal;

            // CORNERS
            if ((parentDir == HumanClick.Direction.South && hasEast) ||
                (parentDir == HumanClick.Direction.West && hasNorth))
                return JunctionType.CornerNorthEast;

            if ((parentDir == HumanClick.Direction.South && hasWest) ||
                (parentDir == HumanClick.Direction.East && hasNorth))
                return JunctionType.CornerNorthWest;

            if ((parentDir == HumanClick.Direction.North && hasEast) ||
                (parentDir == HumanClick.Direction.West && hasSouth))
                return JunctionType.CornerSouthEast;

            if ((parentDir == HumanClick.Direction.North && hasWest) ||
                (parentDir == HumanClick.Direction.East && hasSouth))
                return JunctionType.CornerSouthWest;
        }

        return JunctionType.Standard;
    }

    private void ApplyVisualState(JunctionType junction)
    {
        BracketAnimationState stateToApply = null;
        float rotationDeg = 0f; // <-- new: per-junction rotation sent to shader

        switch (junction)
        {
            case JunctionType.StraightVertical:
                stateToApply = straightVerticalState;
                rotationDeg = 0f;     // up/down
                break;

            case JunctionType.StraightHorizontal:
                stateToApply = straightHorizontalState;
                rotationDeg = 90f;    // left/right
                break;

            case JunctionType.CornerNorthEast:
                stateToApply = cornerNorthEastState;
                rotationDeg = 0f;
                break;

            case JunctionType.CornerSouthEast:
                stateToApply = cornerSouthEastState;
                rotationDeg = 90f;
                break;

            case JunctionType.CornerSouthWest:
                stateToApply = cornerSouthWestState;
                rotationDeg = 180f;
                break;

            case JunctionType.CornerNorthWest:
                stateToApply = cornerNorthWestState;
                rotationDeg = 270f;
                break;

            case JunctionType.Standard:
            default:
                stateToApply = standardState;
                rotationDeg = 0f;
                break;
        }

        if (stateToApply != null)
        {
            // NEW: tell controller the exact rotation before/after setting the state.
            stateController.SetExtraRotation(rotationDeg);
            stateController.SetState(stateToApply);
        }
        else
        {
            Debug.LogWarning($"StructureVisuals [{gameObject.name}]: No state assigned for junction type {junction}");
        }
    }

    public JunctionType GetCurrentJunction() => currentJunction;

    public void ForceUpdate() => UpdateJunction();
}

/// <summary>Junction enum</summary>
public enum JunctionType
{
    Standard,
    StraightVertical,
    StraightHorizontal,
    CornerNorthEast,
    CornerNorthWest,
    CornerSouthEast,
    CornerSouthWest
}
