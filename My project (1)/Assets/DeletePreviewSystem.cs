using UnityEngine;

/// <summary>
/// Attach to each block. Manages danger-state preview with cascade and auto-timeout.
/// Completely self-contained - each block handles its own state.
/// </summary>
public class DeletePreviewSystem : MonoBehaviour
{
    [Header("Danger State Settings")]
    [SerializeField] private BracketAnimationState dangerWobbleState;
    [SerializeField] private float dangerTimeout = 2.0f;
    [SerializeField] private float cascadeDelay = 0.05f;
    [SerializeField] private float minTimeBetweenClicks = 0.3f; // Minimum time before confirm is allowed

    // Instance state
    private bool isInDangerState = false;
    private float dangerStateStartTime;
    private BracketAnimationState savedOriginalState;

    // Cached references
    private BracketStateController bracketController;
    private HumanClick humanClick;

    // Static marker - which block is currently marked for deletion
    private static DeletePreviewSystem blockMarkedForDeletion = null;

    // Frame guard - prevent multiple blocks processing the same click
    private static int lastRightClickFrame = -1;

    /// <summary>
    /// Call this when resetting the game to clear static state.
    /// </summary>
    public static void ResetStaticData()
    {
        blockMarkedForDeletion = null;
        lastRightClickFrame = -1;
    }

    // Track if we've already started cascading to children (prevents double-cascade)
    private bool hasCascadedToChildren = false;

    void Awake()
    {
        bracketController = GetComponent<BracketStateController>();
        humanClick = GetComponent<HumanClick>();

        if (bracketController == null)
        {
            Debug.LogError($"DeletePreviewSystem on {gameObject.name}: Missing BracketStateController!");
        }
    }

    void Update()
    {
        // Auto-timeout: each block is responsible for restoring itself
        if (isInDangerState && Time.time - dangerStateStartTime > dangerTimeout)
        {
            RestoreFromDangerState();
        }
    }

    /// <summary>
    /// Call this when the block is right-clicked.
    /// Returns true if this was a "confirm delete" (second click on same block that is ALREADY in danger state).
    /// Returns false if this was a "preview" (first click or click on different block).
    /// </summary>
    public bool HandleRightClick()
    {
        int myId = gameObject.GetInstanceID();

        // FRAME GUARD: Only one block can process a right-click per frame
        // This prevents issues if multiple overlapping blocks detect the same click
        if (Time.frameCount == lastRightClickFrame)
        {
            Debug.Log($"[DeletePreview] BLOCKED duplicate right-click on {gameObject.name} (ID:{myId}) - same frame {Time.frameCount}");
            return false; // Another block already handled this frame's click
        }
        lastRightClickFrame = Time.frameCount;

        Debug.Log($"[DeletePreview] HandleRightClick called on {gameObject.name} (ID:{myId}), frame {Time.frameCount}, isMarked={(blockMarkedForDeletion == this)}, isInDanger={isInDangerState}");

        // CRITICAL SAFETY CHECK: Only confirm deletion if:
        // 1. This block is the marked block AND
        // 2. This block is ACTUALLY in danger state (visual confirmation was shown) AND
        // 3. Enough time has passed since entering danger state (prevents accidental double-clicks)
        if (blockMarkedForDeletion == this && isInDangerState)
        {
            float timeInDanger = Time.time - dangerStateStartTime;
            if (timeInDanger < minTimeBetweenClicks)
            {
                // Too fast! User probably didn't mean to double-click
                Debug.Log($"[DeletePreview] BLOCKED too-fast confirm on {gameObject.name} (ID:{myId}) - only {timeInDanger:F2}s since preview, need {minTimeBetweenClicks}s");
                return false;
            }

            // Second click on same block that's visually in danger - confirm deletion
            Debug.Log($"[DeletePreview] CONFIRM deletion of {gameObject.name} (ID:{myId}) (was in danger state for {timeInDanger:F2}s)");
            ClearGlobalMarker();
            return true; // Caller should proceed with Die()
        }
        else
        {
            // First click OR click on different block OR block wasn't actually in danger state
            if (blockMarkedForDeletion == this && !isInDangerState)
            {
                // Edge case: marked but not in danger state - treat as first click
                Debug.LogWarning($"[DeletePreview] Block {gameObject.name} (ID:{myId}) was marked but NOT in danger state - treating as first click");
            }

            // Clear any existing marker (that block chain will auto-restore via timeout)
            ClearGlobalMarker();

            // Mark this block and start danger preview
            blockMarkedForDeletion = this;
            EnterDangerState();

            Debug.Log($"[DeletePreview] PREVIEW started for {gameObject.name} (ID:{myId}), now isInDanger={isInDangerState}");
            return false; // Don't delete yet
        }
    }

    /// <summary>
    /// Call this on left-click anywhere to cancel any pending deletion preview.
    /// </summary>
    public static void CancelPreview()
    {
        ClearGlobalMarker();
        // Note: blocks will auto-restore via their own timeouts
    }

    /// <summary>
    /// Check if any block is currently marked for deletion preview.
    /// </summary>
    public static bool HasPendingPreview()
    {
        return blockMarkedForDeletion != null;
    }

    /// <summary>
    /// Get the currently marked block (if any).
    /// </summary>
    public static DeletePreviewSystem GetMarkedBlock()
    {
        return blockMarkedForDeletion;
    }

    private void EnterDangerState()
    {
        if (bracketController == null) return;

        // Only save state if we're not already in danger state
        // This prevents overwriting the original with the danger state
        if (!isInDangerState)
        {
            savedOriginalState = bracketController.GetCurrentState();
        }

        isInDangerState = true;
        hasCascadedToChildren = false;
        dangerStateStartTime = Time.time; // Reset timeout on every enter

        if (dangerWobbleState != null)
        {
            bracketController.SetState(dangerWobbleState);
        }
        else
        {
            Debug.LogWarning($"DeletePreviewSystem on {gameObject.name}: No dangerWobbleState assigned!");
        }

        // Start cascade to children after delay
        StartCoroutine(CascadeToChildrenAfterDelay());
    }

    private System.Collections.IEnumerator CascadeToChildrenAfterDelay()
    {
        yield return new WaitForSeconds(cascadeDelay);

        // Guard against double-cascade
        if (hasCascadedToChildren) yield break;
        hasCascadedToChildren = true;

        // Only cascade if we're still in danger state
        if (!isInDangerState) yield break;

        // Get children from HumanClick's directional child references
        if (humanClick != null)
        {
            CascadeToChild(humanClick.GetNorthChild());
            CascadeToChild(humanClick.GetSouthChild());
            CascadeToChild(humanClick.GetEastChild());
            CascadeToChild(humanClick.GetWestChild());
        }
    }

    private void CascadeToChild(HumanClick child)
    {
        if (child == null) return;

        DeletePreviewSystem childPreview = child.GetComponent<DeletePreviewSystem>();
        if (childPreview != null)
        {
            childPreview.EnterDangerState();
        }
    }

    private void RestoreFromDangerState()
    {
        if (!isInDangerState) return;
        if (bracketController == null) return;

        isInDangerState = false;
        hasCascadedToChildren = false;

        if (savedOriginalState != null)
        {
            bracketController.SetState(savedOriginalState);
            savedOriginalState = null;
        }

        // If I was the marked block and I'm restoring, clear the marker
        if (blockMarkedForDeletion == this)
        {
            blockMarkedForDeletion = null;
        }
    }

    /// <summary>
    /// Force immediate restoration (useful when block is about to be destroyed).
    /// </summary>
    public void ForceRestore()
    {
        StopAllCoroutines();
        RestoreFromDangerState();
    }

    /// <summary>
    /// Activates the visual reveal wobble without triggering deletion checks.
    /// </summary>
    public void TriggerRevealOnly()
    {
        ClearGlobalMarker();
        blockMarkedForDeletion = this;
        EnterDangerState();
    }

    private static void ClearGlobalMarker()
    {
        blockMarkedForDeletion = null;
    }

    void OnDestroy()
    {
        // Clean up if this block was the marked one
        if (blockMarkedForDeletion == this)
        {
            blockMarkedForDeletion = null;
        }
        StopAllCoroutines();
    }

    // Debug helper
    public bool IsInDangerState => isInDangerState;
    public float TimeInDangerState => isInDangerState ? Time.time - dangerStateStartTime : 0f;
}