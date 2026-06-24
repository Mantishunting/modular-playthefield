using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HumanClick : MonoBehaviour
{
    public enum Direction { None, North, South, East, West }

    public event System.Action OnConnectionsChanged;

    private BlockSpawner spawner;
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float clickRangeMultiplier = 2.5f;
    [SerializeField] private float clickDepthMultiplier = 2.5f;
    [SerializeField] private float wobbleDuration = 0.3f;
    [SerializeField] private float wobbleAmount = 0.1f;

    [Header("Continuous Placement")]
    [SerializeField] private float continuousPlacementDelay = 0.1f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask occupancyLayer;

    public static event System.Action<BlockType> OnBlockPlaced;
    public static event System.Action<BlockType> OnBlockDestroyed;

    public static void ResetStaticData()
    {
        nextId = 0;
        totalBlockCount = 0;
        isSpawning = false;
        checkCollisions = true;
        anyBlockShowedPreviewThisFrame = false;
        DeletePreviewSystem.ResetStaticData();
    }

    private Camera mainCamera;
    private int blockId;
    private static int nextId = 0;
    private static bool isSpawning = false;
    private static bool checkCollisions = true;

    private static int totalBlockCount = 0;
    public static int TotalBlockCount => totalBlockCount;

    public static int GetDynamicCostForType(BlockType blockType)
    {
        if (blockType == null) return 0;
        if (blockType.blockName == "Wood")
            return 5 + totalBlockCount;
        else if (blockType.blockName == "Leaf")
            return 1 + Mathf.CeilToInt(totalBlockCount / 10f);
        return blockType.cost;
    }

    private static bool anyBlockShowedPreviewThisFrame = false;
    private static int lastPreviewFrame = -1;
    private static Vector3 cachedMouseWorld; // mouse world pos, computed once per frame (shared by all blocks)

    private BlockType myBlockType;

    public HumanClick northParent;
    public HumanClick southParent;
    public HumanClick eastParent;
    public HumanClick westParent;

    public HumanClick northChild;
    public HumanClick southChild;
    public HumanClick eastChild;
    public HumanClick westChild;

    private bool isWobbling = false;
    private float wobbleTimer = 0f;
    private Vector3 originalScale;

    private float lastPlacementTime = 0f;
    private bool isHoldingLeftClick = false;
    private float leftClickDownTime = 0f;
    private Vector3 leftClickStartPos;
    private bool hasTriggeredLeftDelete = false;

    private float rightClickDownTime = 0f;
    private Vector3 rightClickStartPos;
    private bool hasTriggeredRightDelete = false;

    [SerializeField] private float clickThreshold = 0.3f;


    void Start()
    {
        mainCamera = Camera.main;
        spawner = FindObjectOfType<BlockSpawner>();
        blockId = nextId;
        nextId++;
        originalScale = transform.localScale;
        totalBlockCount++;
    }

    void Update()
    {
        // Don't place blocks if we are zooming/panning, or if touch count is not exactly 1 (e.g. 2 touches for pan/zoom)
        bool blockPlacement = CameraController.IsPanning || Input.touchCount > 1;
        bool isDeleteMode = BlockTypeManager.Instance != null && BlockTypeManager.Instance.IsDeleteModeActive();

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        float currentScale = transform.localScale.x;
        float radius = (blockSize / 2f) * currentScale;
        bool isMouseOverMe = Vector3.Distance(mousePos, transform.position) < radius;

        // 1. LEFT CLICK HANDLING (Placement or Delete Mode)
        if (Input.GetMouseButtonDown(0))
        {
            if (isDeleteMode && isMouseOverMe)
            {
                leftClickDownTime = Time.time;
                leftClickStartPos = Input.mousePosition;
                hasTriggeredLeftDelete = false;

                DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
                if (previewSystem != null)
                {
                    previewSystem.TriggerRevealOnly();
                }
            }
            else if (!isDeleteMode && !blockPlacement)
            {
                isHoldingLeftClick = true;
                lastPlacementTime = 0f;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isHoldingLeftClick = false;

            if (isDeleteMode && !hasTriggeredLeftDelete && leftClickDownTime > 0f)
            {
                // Released in < 0.3s -> Keep reveal
                leftClickDownTime = 0f;
            }
        }

        // Active holding for left-click Delete Mode
        if (isDeleteMode && leftClickDownTime > 0f && !hasTriggeredLeftDelete)
        {
            // If dragging (moved mouse/finger > 5px), cancel delete hold and restore
            if (Vector3.Distance(Input.mousePosition, leftClickStartPos) > 5f)
            {
                leftClickDownTime = 0f;
                DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
                if (previewSystem != null) previewSystem.ForceRestore();
            }
            else if (Time.time - leftClickDownTime > clickThreshold)
            {
                hasTriggeredLeftDelete = true;
                leftClickDownTime = 0f;
                TriggerDeleteAction();
            }
        }

        // Continuous placement for Left click
        if (isHoldingLeftClick && !isSpawning && !isDeleteMode && !blockPlacement)
        {
            if (Time.time - lastPlacementTime >= continuousPlacementDelay)
            {
                HandleClick();
                lastPlacementTime = Time.time;
            }
        }

        if (Input.GetMouseButtonDown(0) && !isDeleteMode && !isMouseOverMe && DeletePreviewSystem.HasPendingPreview())
        {
            DeletePreviewSystem.CancelPreview();
        }

        // 2. RIGHT CLICK HANDLING
        if (Input.GetMouseButtonDown(1) && isMouseOverMe)
        {
            rightClickDownTime = Time.time;
            rightClickStartPos = Input.mousePosition;
            hasTriggeredRightDelete = false;

            DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
            if (previewSystem != null)
            {
                previewSystem.TriggerRevealOnly();
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (!hasTriggeredRightDelete && rightClickDownTime > 0f)
            {
                // Released in < 0.3s -> Keep reveal
                rightClickDownTime = 0f;
            }
        }

        // Active holding for right-click Delete
        if (rightClickDownTime > 0f && !hasTriggeredRightDelete)
        {
            // If dragging (moved mouse > 5px), cancel delete hold and restore
            if (Vector3.Distance(Input.mousePosition, rightClickStartPos) > 5f)
            {
                rightClickDownTime = 0f;
                DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
                if (previewSystem != null) previewSystem.ForceRestore();
            }
            else if (Time.time - rightClickDownTime > clickThreshold)
            {
                hasTriggeredRightDelete = true;
                rightClickDownTime = 0f;
                TriggerDeleteAction();
            }
        }

        if (!isSpawning)
        {
            UpdateHoverPreview();
        }

        if (isWobbling)
        {
            UpdateWobble();
        }
    }

    private void TriggerDeleteAction()
    {
        BlockType bt = GetBlockType();
        if (!ResourceManager.Instance.AllowPlayerDestroyWood && bt != null && bt.blockName == "Wood")
        {
            if (ResourceManager.Instance.ShowDebugLogs) Debug.Log("Player attempted to destroy Wood, but policy disallows it.");
            DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
            if (previewSystem != null) previewSystem.ForceRestore();
            return;
        }

        Die();
    }

    private void NotifyConnectionsChanged()
    {
        OnConnectionsChanged?.Invoke();
    }

    // ==========================================================
    // ARC MATH HELPERS, now updating this to change with orientation. this is good now
    // ==========================================================

    public Vector2 GetForwardVector()
    {
        // "Forward" = the block's LOCAL axis (up/down/left/right, in world space)
        // that points AWAY from the parent. This is both:
        //   • rotation-aware — it's one of the block's actual axes, so attachment
        //     points stay at right angles to the block after it rotates, and
        //   • parent-aware — the arc system's forbidden "bottom" arc (the opposite
        //     of forward) therefore always points back at the parent, never at an
        //     arbitrary world direction. That's what keeps the genuinely-open sides
        //     (including the underside of a horizontal/leaning stem) buildable.
        // Root / unparented blocks default to local up.
        HumanClick parent = GetParent();
        if (parent == null) return transform.up;

        Vector2 away = ((Vector2)(transform.position - parent.transform.position));
        if (away.sqrMagnitude < 0.0001f) return transform.up;
        away.Normalize();

        // Snap "away from parent" to this block's nearest local axis.
        Vector2 up = transform.up;
        Vector2 right = transform.right;
        Vector2 best = up;
        float bestDot = Vector2.Dot(up, away);

        float d = Vector2.Dot(-up, away);    if (d > bestDot) { bestDot = d; best = -up; }
        d = Vector2.Dot(right, away);         if (d > bestDot) { bestDot = d; best = right; }
        d = Vector2.Dot(-right, away);        if (d > bestDot) { bestDot = d; best = -right; }

        return best;
    }

    // --- Debug/visualization helpers: the clickable "node" radii ---
    // Body radius: this block's own circle (right-click delete / occupancy).
    public float GetBodyRadius()
    {
        return (blockSize / 2f) * transform.localScale.x;
    }

    // Click range: the larger area where clicking near this block places a new one.
    public float GetClickRadius()
    {
        return GetBodyRadius() * clickRangeMultiplier;
    }

    // Debug: the empty, valid grid cells next to this block where 'type' could be
    // placed (forward / left / right arcs; never backward toward the parent).
    // Reuses the real placement rules so it matches what a click would allow.
    public void GetOpenPlacementPositions(BlockType type, List<Vector3> results)
    {
        if (type == null) return;

        Vector2 forward = GetForwardVector();
        Vector3 fwd = new Vector3(forward.x, forward.y, 0f).normalized;
        Vector3 right = new Vector3(forward.y, -forward.x, 0f).normalized;
        Vector3 left = new Vector3(-forward.y, forward.x, 0f).normalized;

        TryAddOpenSlot(fwd, type, results);
        TryAddOpenSlot(right, type, results);
        TryAddOpenSlot(left, type, results);
    }

    private void TryAddOpenSlot(Vector3 dir, BlockType type, List<Vector3> results)
    {
        HumanClick childToMove = GetChildInDirection(dir);
        if (childToMove != null) return; // cell already filled

        Vector3 spawnPos = transform.position + dir * blockSize;
        if (IsPositionOccupied(spawnPos) || IsHazard(spawnPos)) return;
        if (!IsValidPlacement(type, childToMove)) return;

        results.Add(spawnPos);
    }

    // Debug: given a mouse world position, returns the exact snap position where a
    // new block of 'type' would land off this block (mirrors the hover preview).
    public bool TryGetPlacementPosition(Vector3 mouseWorld, BlockType type, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (type == null) return false;

        Vector3 blockCenter = transform.position;
        float scaledHalfSize = (blockSize / 2f) * transform.localScale.x;
        if (Vector3.Distance(mouseWorld, blockCenter) > scaledHalfSize * clickRangeMultiplier)
            return false;

        Vector2 forward = GetForwardVector();
        List<BlockGeom> neighbors = GetNeighbors();
        PlacementResult result = ArcMath.SolvePlacement(blockCenter, forward, scaledHalfSize, neighbors, mouseWorld);
        if (result.type == PlacementResultType.None) return false;

        Vector3 spawnDirection = GetDirectionFromArc(forward, result.arc);
        if (spawnDirection == Vector3.zero) return false;

        Vector3 pos = blockCenter + (spawnDirection * blockSize);
        HumanClick childToMove = GetChildInDirection(spawnDirection);
        if (!IsValidPlacement(type, childToMove)) return false;
        if (childToMove == null && (IsPositionOccupied(pos) || IsHazard(pos))) return false;

        spawnPosition = pos;
        return true;
    }

    private List<BlockGeom> GetNeighbors()
    {
        List<BlockGeom> neighbors = new List<BlockGeom>();

        void AddIfExist(HumanClick block)
        {
            if (block != null)
            {
                // Fetch ACTUAL scale of neighbor (set by BlockScaler)
                float neighborScale = block.transform.localScale.x;
                float neighborRadius = (blockSize / 2f) * neighborScale;

                neighbors.Add(new BlockGeom
                {
                    position = block.transform.position,
                    radius = neighborRadius,
                    generation = 0
                });
            }
        }

        AddIfExist(northChild);
        AddIfExist(southChild);
        AddIfExist(eastChild);
        AddIfExist(westChild);
        AddIfExist(northParent);
        AddIfExist(southParent);
        AddIfExist(eastParent);
        AddIfExist(westParent);

        return neighbors;
    }

    private Vector3 GetDirectionFromArc(Vector2 forward, AddArc arc)
    {
        if (arc == AddArc.Bottom) return -forward;

        if (arc == AddArc.Forward) return forward;

        // Grid Logic: 
        // Right is 90 degrees Clockwise: (x, y) -> (y, -x)
        // Left is 90 degrees Counter-Clockwise: (x, y) -> (-y, x)

        if (arc == AddArc.Right)
            return new Vector3(forward.y, -forward.x, 0);

        if (arc == AddArc.Left)
            return new Vector3(-forward.y, forward.x, 0);

        return Vector3.zero;
    }

    // ==========================================================
    // VISUALIZATION LOGIC
    // ==========================================================

    void UpdateHoverPreview()
    {
        if (BlockTypeManager.Instance != null && BlockTypeManager.Instance.IsDeleteModeActive())
        {
            if (PreviewBlockManager.Instance != null)
            {
                PreviewBlockManager.Instance.HidePreview();
            }
            return;
        }

        int currentFrame = Time.frameCount;
        if (lastPreviewFrame != currentFrame)
        {
            lastPreviewFrame = currentFrame;
            if (!anyBlockShowedPreviewThisFrame && PreviewBlockManager.Instance != null)
            {
                PreviewBlockManager.Instance.HidePreview();
            }
            anyBlockShowedPreviewThisFrame = false;

            // The mouse world position is identical for every block this frame, so compute
            // it once here (first block to run) instead of once per block.
            cachedMouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            cachedMouseWorld.z = 0;
        }

        if (PreviewBlockManager.Instance == null) return;

        Vector3 mousePos = cachedMouseWorld;

        Vector3 blockCenter = transform.position;
        float currentScale = transform.localScale.x;
        float scaledHalfSize = (blockSize / 2f) * currentScale;

        // Range Check
        if (Vector3.Distance(mousePos, blockCenter) > scaledHalfSize * clickRangeMultiplier)
        {
            return;
        }

        Vector2 forward = GetForwardVector();
        List<BlockGeom> neighbors = GetNeighbors();

        PlacementResult result = ArcMath.SolvePlacement(
            blockCenter,
            forward,
            scaledHalfSize,
            neighbors,
            mousePos
        );

        if (result.type == PlacementResultType.None) return;

        Vector3 spawnDirection = GetDirectionFromArc(forward, result.arc);
        if (spawnDirection == Vector3.zero) return;

        Vector3 spawnPosition = blockCenter + (spawnDirection * blockSize);

        HumanClick childToMove = GetChildInDirection(spawnDirection);

        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null) return;

        int dynamicCost = GetDynamicCost(selectedType);
        bool canAfford = ResourceManager.Instance.CanAfford(dynamicCost);

        bool isPlacementValid = IsValidPlacement(selectedType, childToMove);

        if (childToMove == null && IsPositionOccupied(spawnPosition))
        {
            isPlacementValid = false;
        }

        if (isPlacementValid)
        {
            // Pass 'this' as the parent block so PreviewBlockManager can highlight it
            PreviewBlockManager.Instance.ShowPreview(spawnPosition, selectedType, canAfford, dynamicCost, this);
            anyBlockShowedPreviewThisFrame = true;
        }
    }


    // ==========================================================
    // CLICK/SPAWN LOGIC
    // ==========================================================

    private HumanClick DetectInsertTarget(Vector3 mousePos)
    {
        float radius = (blockSize / 2f) * transform.localScale.x * 1.1f;

        if (northChild != null && Vector3.Distance(mousePos, northChild.transform.position) < radius)
            return northChild;

        if (southChild != null && Vector3.Distance(mousePos, southChild.transform.position) < radius)
            return southChild;

        if (eastChild != null && Vector3.Distance(mousePos, eastChild.transform.position) < radius)
            return eastChild;

        if (westChild != null && Vector3.Distance(mousePos, westChild.transform.position) < radius)
            return westChild;

        return null;
    }

    // ==========================================================
    // INSERT HANDLER (NEW)
    // ==========================================================
    private void DoInsert(HumanClick childToShift)
    {
        Vector3 dir = (childToShift.transform.position - transform.position).normalized;

        Vector3 newPos = transform.position + dir * blockSize;
        Vector3 moveStep = dir * blockSize;

        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null) return;

        if (!IsValidPlacement(selectedType, childToShift)) return;
        
        int dynamicCost = GetDynamicCost(selectedType);
        if (!ResourceManager.Instance.CanAfford(dynamicCost)) return;

        if (!ResourceManager.Instance.TrySpendFood(dynamicCost)) return;

        // Move the existing child (and its entire subtree) out of the way first
        childToShift.Move(moveStep);

        GameObject newBlock = spawner.SpawnBlockAt(newPos, selectedType);
        if (newBlock == null) return;

        HumanClick newChild = newBlock.GetComponent<HumanClick>();

        LinkChild(dir, newChild);
        newChild.LinkChild(dir, childToShift);

        newChild.SetBlockType(selectedType);
        OnBlockPlaced?.Invoke(selectedType);

        // Check for collisions after the move
        CheckAndKillCollisions(childToShift);
        ValidateAndRemoveInvalidLeafs(childToShift);
    }

    void HandleClick()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 blockCenter = transform.position;

        float currentScale = transform.localScale.x;
        float scaledHalfSize = (blockSize / 2f) * currentScale;

        if (Vector3.Distance(mousePos, blockCenter) > scaledHalfSize * clickRangeMultiplier)
            return;

        // NEW INSERT CHECK — ADD THIS BLOCK 
        HumanClick insertTarget = DetectInsertTarget(mousePos);
        if (insertTarget != null)
        {
            DoInsert(insertTarget);
            return;
        }

        // (existing ADD / arc logic follows)
        Vector2 forward = GetForwardVector();
        List<BlockGeom> neighbors = GetNeighbors();

        PlacementResult result = ArcMath.SolvePlacement(
            blockCenter,
            forward,
            scaledHalfSize,
            neighbors,
            mousePos
        );

        if (result.type == PlacementResultType.None) return;

        Vector3 spawnDirection = GetDirectionFromArc(forward, result.arc);
        if (spawnDirection == Vector3.zero) return;

        Vector3 spawnPosition = blockCenter + (spawnDirection * blockSize);
        HumanClick childToMove = GetChildInDirection(spawnDirection);

        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null) return;

        int dynamicCost = GetDynamicCost(selectedType);

        if (!ResourceManager.Instance.CanAfford(dynamicCost)) return;

        if (!IsValidPlacement(selectedType, childToMove)) return;

        if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition))) return;

        if (IsPositionOccupied(spawnPosition) && childToMove != null)
        {
            checkCollisions = false;
            childToMove.Move(spawnDirection);
        }

        isSpawning = true;
        StartWobble();

        if (!ResourceManager.Instance.TrySpendFood(dynamicCost))
        {
            isSpawning = false;
            return;
        }

        GameObject newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
        if (newBlock != null)
        {
            // Inherit the parent's rotation so the child's LOCAL axes line up with the
            // rotation-aware spawnDirection. Otherwise the child spawns
            // world-aligned (Quaternion.identity), the joint auto-configures a mismatched
            // relative orientation, and it visibly swings before settling. Runs before
            // PhysicsConnector.Start() configures the joint.
            newBlock.transform.rotation = transform.rotation;

            HumanClick newChild = newBlock.GetComponent<HumanClick>();

            LinkChild(spawnDirection, newChild);

            if (childToMove != null)
            {
                newChild.LinkChild(spawnDirection, childToMove);
            }

            checkCollisions = true;
            CheckAndKillCollisions(childToMove);

            if (childToMove != null)
            {
                ValidateAndRemoveInvalidLeafs(childToMove);
            }

            newChild.SetBlockType(selectedType);
            OnBlockPlaced?.Invoke(selectedType);
        }
    }

    private HumanClick GetChildInDirection(Vector3 dir)
    {
        Vector2 d = dir.normalized;

        // Compare against this block's LOCAL axes (rotation-aware), not the world grid.
        Vector2 up = transform.up;
        Vector2 right = transform.right;

        float eastDot = Vector2.Dot(d, right);
        float westDot = Vector2.Dot(d, -right);
        float northDot = Vector2.Dot(d, up);
        float southDot = Vector2.Dot(d, -up);

        float max = Mathf.Max(eastDot, westDot, northDot, southDot);

        if (Mathf.Approximately(max, eastDot)) return eastChild;
        if (Mathf.Approximately(max, westDot)) return westChild;
        if (Mathf.Approximately(max, northDot)) return northChild;
        if (Mathf.Approximately(max, southDot)) return southChild;

        return null;
    }
    private void LinkChild(Vector3 dir, HumanClick child)
    {
        Vector2 d = dir.normalized;

        // Assign to the slot matching this block's LOCAL axes (rotation-aware).
        Vector2 up = transform.up;
        Vector2 right = transform.right;

        float eastDot = Vector2.Dot(d, right);
        float westDot = Vector2.Dot(d, -right);
        float northDot = Vector2.Dot(d, up);
        float southDot = Vector2.Dot(d, -up);

        float max = Mathf.Max(eastDot, westDot, northDot, southDot);

        // First, clear the child's old parent reference (a child can only have one parent)
        child.ClearAllParents();

        if (Mathf.Approximately(max, eastDot))
        {
            // Clear any existing child in this slot
            if (eastChild != null && eastChild != child)
            {
                eastChild.westParent = null;
                eastChild.NotifyConnectionsChanged();
            }
            eastChild = child;
            child.westParent = this;
        }
        else if (Mathf.Approximately(max, westDot))
        {
            if (westChild != null && westChild != child)
            {
                westChild.eastParent = null;
                westChild.NotifyConnectionsChanged();
            }
            westChild = child;
            child.eastParent = this;
        }
        else if (Mathf.Approximately(max, northDot))
        {
            if (northChild != null && northChild != child)
            {
                northChild.southParent = null;
                northChild.NotifyConnectionsChanged();
            }
            northChild = child;
            child.southParent = this;
        }
        else if (Mathf.Approximately(max, southDot))
        {
            if (southChild != null && southChild != child)
            {
                southChild.northParent = null;
                southChild.NotifyConnectionsChanged();
            }
            southChild = child;
            child.northParent = this;
        }

        NotifyConnectionsChanged();
        child.NotifyConnectionsChanged();
    }

    // Clears all parent references - used before assigning a new parent
    private void ClearAllParents()
    {
        if (northParent != null)
        {
            northParent.southChild = null;
            northParent.NotifyConnectionsChanged();
            northParent = null;
        }
        if (southParent != null)
        {
            southParent.northChild = null;
            southParent.NotifyConnectionsChanged();
            southParent = null;
        }
        if (eastParent != null)
        {
            eastParent.westChild = null;
            eastParent.NotifyConnectionsChanged();
            eastParent = null;
        }
        if (westParent != null)
        {
            westParent.eastChild = null;
            westParent.NotifyConnectionsChanged();
            westParent = null;
        }
    }

    // ==========================================================
    // EXISTING LOGIC
    // ==========================================================

    int GetDynamicCost(BlockType blockType)
    {
        return GetDynamicCostForType(blockType);
    }

    bool IsValidPlacement(BlockType selectedType, HumanClick childToMove)
    {
        if (selectedType.blockName == "Wood" && myBlockType != null &&
            (myBlockType.blockName == "Leaf" || myBlockType.blockName == "Flower"))
            return false;

        if ((selectedType.blockName == "Leaf" || selectedType.blockName == "Flower") &&
            childToMove != null &&
            myBlockType != null && myBlockType.blockName == "Wood" &&
            childToMove.GetBlockType() != null && childToMove.GetBlockType().blockName == "Wood")
            return false;

        if ((selectedType.blockName == "Leaf" || selectedType.blockName == "Flower") &&
            childToMove != null &&
            childToMove.GetBlockType() != null && childToMove.GetBlockType().blockName == "Wood")
            return false;

        if (selectedType.blockName == "Leaf")
        {
            List<HumanClick> nearbyBlocks = TreeLooker.GetBlocksInRadius(transform.position, 3f);
            bool woodFound = false;
            foreach (HumanClick block in nearbyBlocks)
            {
                BlockType blockType = block.GetBlockType();
                if (blockType != null && blockType.blockName == "Wood")
                {
                    woodFound = true;
                    break;
                }
            }
            if (!woodFound) return false;
        }
        return true;
    }

    void CheckAndKillCollisions(HumanClick blockTree)
    {
        if (blockTree == null) return;
        if (IsHazard(blockTree.transform.position))
        {
            blockTree.Die();
            return;
        }
        if (blockTree.northChild != null) CheckAndKillCollisions(blockTree.northChild);
        if (blockTree.southChild != null) CheckAndKillCollisions(blockTree.southChild);
        if (blockTree.eastChild != null) CheckAndKillCollisions(blockTree.eastChild);
        if (blockTree.westChild != null) CheckAndKillCollisions(blockTree.westChild);
    }

    void ValidateAndRemoveInvalidLeafs(HumanClick block)
    {
        if (block == null) return;
        BlockType blockType = block.GetBlockType();
        if (blockType != null && blockType.blockName == "Leaf")
        {
            List<HumanClick> nearbyBlocks = TreeLooker.GetBlocksInRadius(block.transform.position, 3f);
            bool hasWoodNearby = false;
            foreach (HumanClick nearbyBlock in nearbyBlocks)
            {
                BlockType nearbyType = nearbyBlock.GetBlockType();
                if (nearbyType != null && nearbyType.blockName == "Wood")
                {
                    hasWoodNearby = true;
                    break;
                }
            }
            if (!hasWoodNearby)
            {
                block.Die();
                return;
            }
        }
        if (block.northChild != null) ValidateAndRemoveInvalidLeafs(block.northChild);
        if (block.southChild != null) ValidateAndRemoveInvalidLeafs(block.southChild);
        if (block.eastChild != null) ValidateAndRemoveInvalidLeafs(block.eastChild);
        if (block.westChild != null) ValidateAndRemoveInvalidLeafs(block.westChild);
    }

    private const string HazardTag = "NoGrow";
    private bool IsHazard(Vector3 position)
    {
        // CIRCLE COLLIDER CHECK
        float radius = (blockSize / 2f) * 0.95f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag(HazardTag))
                return true;
        }
        return false;
    }

    bool IsPositionOccupied(Vector3 position)
    {
        // A cell counts as occupied only if ANOTHER block's CENTRE sits in it (or a
        // non-block collider overlaps it). We compare block centres rather than raw
        // collider overlap, because BlockScaler inflates blocks up to 3x — a neighbour
        // a full cell away still overlaps this point, which used to falsely block every
        // adjacent side. Centre distance is immune to that scaling.
        float searchRadius = blockSize;            // gather anything that could reach the cell
        float centreThreshold = 0.5f * blockSize;  // ...but only a block centred IN the cell counts

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, searchRadius, occupancyLayer);
        foreach (Collider2D hit in hits)
        {
            HumanClick hc = hit.GetComponentInParent<HumanClick>();

            if (hc == this) continue;     // ignore ourself (collider may be on a child object)
            if (hc == null) return true;  // a solid non-block collider → genuinely blocked

            if (Vector2.Distance(hc.transform.position, position) < centreThreshold)
                return true;              // another block actually occupies this cell
        }
        return false;
    }

    public void Move(Vector3 direction)
    {
        transform.position += direction;
        if (northChild != null) northChild.Move(direction);
        if (southChild != null) southChild.Move(direction);
        if (eastChild != null) eastChild.Move(direction);
        if (westChild != null) westChild.Move(direction);

        if (checkCollisions)
        {
            if (IsHazard(transform.position))
            {
                Die();
                return;
            }
        }
    }

    public HumanClick GetBlockAtMyPosition()
    {
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        foreach (HumanClick block in allBlocks)
        {
            if (block != this && Vector3.Distance(block.transform.position, transform.position) < 0.1f)
            {
                return block;
            }
        }
        return null;
    }

    public void Die()
    {
        DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
        if (previewSystem != null) previewSystem.ForceRestore();

        totalBlockCount--;

        if (northChild != null) northChild.Die();
        if (southChild != null) southChild.Die();
        if (eastChild != null) eastChild.Die();
        if (westChild != null) westChild.Die();

        if (myBlockType != null) OnBlockDestroyed?.Invoke(myBlockType);

        Destroy(gameObject);
    }

    void StartWobble()
    {
        isWobbling = true;
        wobbleTimer = 0f;
    }

    void UpdateWobble()
    {
        wobbleTimer += Time.deltaTime;
        if (wobbleTimer < wobbleDuration)
        {
            float wobble = Mathf.Sin(wobbleTimer * Mathf.PI * 2 / wobbleDuration) * wobbleAmount;
            transform.localScale = originalScale * (1f + wobble);
        }
        else
        {
            transform.localScale = originalScale;
            isWobbling = false;
            isSpawning = false;
        }
    }

    public void SetBlockType(BlockType blockType)
    {
        myBlockType = blockType;
        Debug.Log($"Block {blockId} set to type: {blockType.blockName}");
    }

    public BlockType GetBlockType() => myBlockType;
    public int GetBlockId() => blockId;

    public HumanClick GetParent()
    {
        if (northParent != null) return northParent;
        if (southParent != null) return southParent;
        if (eastParent != null) return eastParent;
        if (westParent != null) return westParent;
        return null;
    }

    public Direction GetParentDirection()
    {
        if (northParent != null) return Direction.North;
        if (southParent != null) return Direction.South;
        if (eastParent != null) return Direction.East;
        if (westParent != null) return Direction.West;
        return Direction.None;
    }

    // External getters
    public HumanClick GetNorthParent() => northParent;
    public HumanClick GetSouthParent() => southParent;
    public HumanClick GetEastParent() => eastParent;
    public HumanClick GetWestParent() => westParent;

    public HumanClick GetNorthChild() => northChild;
    public HumanClick GetSouthChild() => southChild;
    public HumanClick GetEastChild() => eastChild;
    public HumanClick GetWestChild() => westChild;

    public bool TryPlaceRelative(Direction dir, BlockType type, bool spendResources = true)
    {
        if (spawner == null) spawner = FindObjectOfType<BlockSpawner>();
        if (type == null || spawner == null) return false;

        Vector3 step = Vector3.zero;
        HumanClick childToMove = null;

        switch (dir)
        {
            case Direction.East: step = new Vector3(blockSize, 0f, 0f); childToMove = eastChild; break;
            case Direction.West: step = new Vector3(-blockSize, 0f, 0f); childToMove = westChild; break;
            case Direction.North: step = new Vector3(0f, blockSize, 0f); childToMove = northChild; break;
            case Direction.South: step = new Vector3(0f, -blockSize, 0f); childToMove = southChild; break;
            default: return false;
        }

        Vector3 spawnPosition = transform.position + step;

        if (!IsValidPlacement(type, childToMove)) return false;
        if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition))) return false;

        int dynamicCost = GetDynamicCost(type);
        if (spendResources && !ResourceManager.Instance.CanAfford(dynamicCost)) return false;

        if (IsPositionOccupied(spawnPosition) && childToMove != null)
            childToMove.Move(step);

        if (spendResources && !ResourceManager.Instance.TrySpendFood(dynamicCost)) return false;

        GameObject newBlock = spawner.SpawnBlockAt(spawnPosition, type);
        if (newBlock == null) return false;

        HumanClick newChild = newBlock.GetComponent<HumanClick>();
        if (newChild == null) return false;
        newChild.SetBlockType(type);
        OnBlockPlaced?.Invoke(type);

        LinkChild(step, newChild);
        if (childToMove != null) newChild.LinkChild(step, childToMove);

        CheckAndKillCollisions(childToMove);
        if (childToMove != null) ValidateAndRemoveInvalidLeafs(childToMove);

        return true;
    }

    // ----- Placement diagnostics (debug only) -----
    // Reports, for all four of this block's LOCAL sides, why a block can or can't be
    // placed there, plus the block's actual orientation. Lets a debug overlay show
    // the REAL reason a side is rejected instead of us guessing.
    public enum SlotStatus { Open, HasChild, Occupied, Hazard, Invalid }

    public struct SlotDiag
    {
        public Vector3 pos;        // candidate cell position
        public SlotStatus status;  // why it's open / rejected
    }

    public void GetPlacementDiagnostics(BlockType type, List<SlotDiag> results,
                                        out Vector3 localUp, out Vector3 localRight)
    {
        localUp = transform.up;
        localRight = transform.right;

        AddDiag(transform.up, type, results);
        AddDiag(transform.right, type, results);
        AddDiag(-transform.up, type, results);
        AddDiag(-transform.right, type, results);
    }

    private void AddDiag(Vector3 dir, BlockType type, List<SlotDiag> results)
    {
        dir = dir.normalized;
        Vector3 pos = transform.position + dir * blockSize;

        SlotStatus status;
        if (GetChildInDirection(dir) != null) status = SlotStatus.HasChild;
        else if (IsHazard(pos)) status = SlotStatus.Hazard;
        else if (IsPositionOccupied(pos)) status = SlotStatus.Occupied;
        else if (type != null && !IsValidPlacement(type, null)) status = SlotStatus.Invalid;
        else status = SlotStatus.Open;

        results.Add(new SlotDiag { pos = pos, status = status });
    }
}