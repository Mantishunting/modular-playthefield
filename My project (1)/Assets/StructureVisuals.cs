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

    /// <summary>
    /// Called whenever HumanClick detects a connection change.
    /// This is where we respond to placement/insertion/removal events.
    /// </summary>
    private void OnConnectionsChanged()
    {
        UpdateJunction();

        // TODO: Future shiver wave system hook
        // When implemented, this is where you'd call:
        // TriggerShiverWave(depth: 4);
    }

    /// <summary>
    /// Detects the junction type and updates the visual state accordingly.
    /// </summary>
    public void UpdateJunction()
    {
        JunctionType newJunction = DetectJunction();

        // Only update if junction type changed
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

    /// <summary>
    /// Analyzes parent and children to determine what junction type this block is.
    /// </summary>
    private JunctionType DetectJunction()
    {
        // Get parent direction
        HumanClick.Direction parentDir = humanClick.GetParentDirection();

        // Check which children exist
        bool hasNorth = humanClick.GetNorthChild() != null;
        bool hasSouth = humanClick.GetSouthChild() != null;
        bool hasEast = humanClick.GetEastChild() != null;
        bool hasWest = humanClick.GetWestChild() != null;

        // Count total children
        int childCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0) + 
                         (hasEast ? 1 : 0) + (hasWest ? 1 : 0);

        // === STRAIGHT LINE DETECTION ===
        // Parent and single child are opposite directions
        if (childCount == 1)
        {
            // Vertical straight lines
            if (parentDir == HumanClick.Direction.North && hasSouth)
                return JunctionType.StraightVertical;
            if (parentDir == HumanClick.Direction.South && hasNorth)
                return JunctionType.StraightVertical;

            // Horizontal straight lines
            if (parentDir == HumanClick.Direction.West && hasEast)
                return JunctionType.StraightHorizontal;
            if (parentDir == HumanClick.Direction.East && hasWest)
                return JunctionType.StraightHorizontal;

            // === CORNER DETECTION ===
            // Parent and single child are perpendicular

            // North-East corners (child to north OR east, parent from opposite quadrant)
            if (parentDir == HumanClick.Direction.South && hasEast)
                return JunctionType.CornerNorthEast;
            if (parentDir == HumanClick.Direction.West && hasNorth)
                return JunctionType.CornerNorthEast;

            // North-West corners
            if (parentDir == HumanClick.Direction.South && hasWest)
                return JunctionType.CornerNorthWest;
            if (parentDir == HumanClick.Direction.East && hasNorth)
                return JunctionType.CornerNorthWest;

            // South-East corners
            if (parentDir == HumanClick.Direction.North && hasEast)
                return JunctionType.CornerSouthEast;
            if (parentDir == HumanClick.Direction.West && hasSouth)
                return JunctionType.CornerSouthEast;

            // South-West corners
            if (parentDir == HumanClick.Direction.North && hasWest)
                return JunctionType.CornerSouthWest;
            if (parentDir == HumanClick.Direction.East && hasSouth)
                return JunctionType.CornerSouthWest;
        }

        // === COMPLEX JUNCTIONS ===
        // Multiple children, T-junctions, 4-way junctions, or no children
        // For now, all of these use the Standard state
        return JunctionType.Standard;
    }

    /// <summary>
    /// Applies the appropriate visual state based on junction type.
    /// </summary>
    private void ApplyVisualState(JunctionType junction)
    {
        BracketAnimationState stateToApply = null;

        switch (junction)
        {
            case JunctionType.StraightVertical:
                stateToApply = straightVerticalState;
                break;

            case JunctionType.StraightHorizontal:
                stateToApply = straightHorizontalState;
                break;

            case JunctionType.CornerNorthEast:
                stateToApply = cornerNorthEastState;
                break;

            case JunctionType.CornerNorthWest:
                stateToApply = cornerNorthWestState;
                break;

            case JunctionType.CornerSouthEast:
                stateToApply = cornerSouthEastState;
                break;

            case JunctionType.CornerSouthWest:
                stateToApply = cornerSouthWestState;
                break;

            case JunctionType.Standard:
            default:
                stateToApply = standardState;
                break;
        }

        // Apply the state (will smoothly transition)
        if (stateToApply != null)
        {
            stateController.SetState(stateToApply);
        }
        else
        {
            Debug.LogWarning($"StructureVisuals [{gameObject.name}]: No state assigned for junction type {junction}");
        }
    }

    // ===== PUBLIC API =====

    /// <summary>
    /// Get the current junction type (useful for debugging or external systems)
    /// </summary>
    public JunctionType GetCurrentJunction()
    {
        return currentJunction;
    }

    /// <summary>
    /// Manually force a junction update (normally not needed - events handle this)
    /// </summary>
    public void ForceUpdate()
    {
        UpdateJunction();
    }

    // ===== FUTURE: SHIVER WAVE SYSTEM =====
    // When you implement the shiver wave, here's the structure:
    /*
    /// <summary>
    /// Triggers a "shiver" animation wave that propagates through connected blocks.
    /// Each block decrements the depth counter and passes it to neighbors.
    /// </summary>
    /// <param name="remainingDepth">How many more blocks should receive the shiver</param>
    public void TriggerShiverWave(int remainingDepth)
    {
        if (remainingDepth <= 0) return; // Wave ends here
        
        // TODO: Trigger local shiver animation
        // stateController.TriggerShiver(); // You'd add this method to BracketStateController
        
        // Propagate to parent
        HumanClick parent = humanClick.GetParent();
        if (parent != null)
        {
            StructureVisuals parentVisuals = parent.GetComponent<StructureVisuals>();
            if (parentVisuals != null)
            {
                parentVisuals.TriggerShiverWave(remainingDepth - 1);
            }
        }
        
        // Propagate to children
        HumanClick[] children = new HumanClick[]
        {
            humanClick.GetNorthChild(),
            humanClick.GetSouthChild(),
            humanClick.GetEastChild(),
            humanClick.GetWestChild()
        };
        
        foreach (HumanClick child in children)
        {
            if (child != null)
            {
                StructureVisuals childVisuals = child.GetComponent<StructureVisuals>();
                if (childVisuals != null)
                {
                    childVisuals.TriggerShiverWave(remainingDepth - 1);
                }
            }
        }
    }
    */
}

/// <summary>
/// Defines the different types of block junctions in the tree structure.
/// </summary>
public enum JunctionType
{
    /// <summary>Default state - for complex junctions, multiple children, or fallback</summary>
    Standard,

    /// <summary>Straight line going up/down (parent North↔child South or vice versa)</summary>
    StraightVertical,

    /// <summary>Straight line going left/right (parent West↔child East or vice versa)</summary>
    StraightHorizontal,

    /// <summary>90° corner bending toward North-East</summary>
    CornerNorthEast,

    /// <summary>90° corner bending toward North-West</summary>
    CornerNorthWest,

    /// <summary>90° corner bending toward South-East</summary>
    CornerSouthEast,

    /// <summary>90° corner bending toward South-West</summary>
    CornerSouthWest
}
