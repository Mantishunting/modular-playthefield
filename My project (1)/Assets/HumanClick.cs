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
    private static bool anyBlockShowedPreviewThisFrame = false;
    private static int lastPreviewFrame = -1;

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

    private float rightClickDownTime = 0f;
    [SerializeField] private float clickThreshold = 0.25f;

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
        if (Input.GetMouseButtonDown(0))
        {
            isHoldingLeftClick = true;
            lastPlacementTime = 0f;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isHoldingLeftClick = false;
        }

        if (isHoldingLeftClick && !isSpawning)
        {
            if (Time.time - lastPlacementTime >= continuousPlacementDelay)
            {
                HandleClick();
                lastPlacementTime = Time.time;
            }
        }

        if (Input.GetMouseButtonDown(0) && DeletePreviewSystem.HasPendingPreview())
        {
            DeletePreviewSystem.CancelPreview();
        }

        if (Input.GetMouseButtonDown(1))
        {
            rightClickDownTime = Time.time;
        }

        if (Input.GetMouseButtonUp(1))
        {
            float heldTime = Time.time - rightClickDownTime;
            if (heldTime <= clickThreshold)
            {
                HandleRightClickDelete();
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

    private void HandleRightClickDelete()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 blockCenter = transform.position;
        float currentScale = transform.localScale.x;
        float radius = (blockSize / 2f) * currentScale;

        // CIRCLE COLLIDER CHECK: Simple distance comparison
        if (Vector3.Distance(mousePos, blockCenter) < radius)
        {
            BlockType bt = GetBlockType();
            if (!Resources.Instance.AllowPlayerDestroyWood && bt != null && bt.blockName == "Wood")
            {
                if (Resources.Instance.ShowDebugLogs) Debug.Log("Player attempted to destroy Wood, but policy disallows it.");
                return;
            }

            DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
            if (previewSystem != null)
            {
                if (previewSystem.HandleRightClick()) Die();
            }
            else
            {
                Die();
            }
        }
    }

    private void NotifyConnectionsChanged()
    {
        OnConnectionsChanged?.Invoke();
    }

    // ==========================================================
    // ARC MATH HELPERS
    // ==========================================================

    private Vector2 GetForwardVector()
    {
        if (southParent != null) return Vector2.up;
        if (northParent != null) return Vector2.down;
        if (westParent != null) return Vector2.right;
        if (eastParent != null) return Vector2.left;

        return Vector2.up;
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
        int currentFrame = Time.frameCount;
        if (lastPreviewFrame != currentFrame)
        {
            lastPreviewFrame = currentFrame;
            if (!anyBlockShowedPreviewThisFrame && PreviewBlockManager.Instance != null)
            {
                PreviewBlockManager.Instance.HidePreview();
            }
            anyBlockShowedPreviewThisFrame = false;
        }

        if (PreviewBlockManager.Instance == null) return;

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

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
        bool canAfford = Resources.Instance.CanAfford(dynamicCost);

        bool isPlacementValid = IsValidPlacement(selectedType, childToMove);

        if (childToMove == null && IsPositionOccupied(spawnPosition))
        {
            isPlacementValid = false;
        }

        if (isPlacementValid)
        {
            PreviewBlockManager.Instance.ShowPreview(spawnPosition, selectedType, canAfford, dynamicCost);
            anyBlockShowedPreviewThisFrame = true;
        }
    }

    // ==========================================================
    // CLICK/SPAWN LOGIC
    // ==========================================================

    void HandleClick()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 blockCenter = transform.position;

        float currentScale = transform.localScale.x;
        float scaledHalfSize = (blockSize / 2f) * currentScale;

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

        if (!Resources.Instance.CanAfford(dynamicCost)) return;

        if (!IsValidPlacement(selectedType, childToMove)) return;

        if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition))) return;

        if (IsPositionOccupied(spawnPosition) && childToMove != null)
        {
            checkCollisions = false;
            childToMove.Move(spawnDirection);
        }

        isSpawning = true;
        StartWobble();

        if (!Resources.Instance.TrySpendFood(dynamicCost))
        {
            isSpawning = false;
            return;
        }

        GameObject newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
        if (newBlock != null)
        {
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
        if (Mathf.Approximately(dir.x, 1)) return eastChild;
        if (Mathf.Approximately(dir.x, -1)) return westChild;
        if (Mathf.Approximately(dir.y, 1)) return northChild;
        if (Mathf.Approximately(dir.y, -1)) return southChild;
        return null;
    }

    private void LinkChild(Vector3 dir, HumanClick child)
    {
        if (Mathf.Approximately(dir.x, 1)) // East
        {
            eastChild = child;
            child.westParent = this;
        }
        else if (Mathf.Approximately(dir.x, -1)) // West
        {
            westChild = child;
            child.eastParent = this;
        }
        else if (Mathf.Approximately(dir.y, 1)) // North
        {
            northChild = child;
            child.southParent = this;
        }
        else if (Mathf.Approximately(dir.y, -1)) // South
        {
            southChild = child;
            child.northParent = this;
        }

        NotifyConnectionsChanged();
        child.NotifyConnectionsChanged();
    }

    // ==========================================================
    // EXISTING LOGIC
    // ==========================================================

    int GetDynamicCost(BlockType blockType)
    {
        if (blockType.blockName == "Wood")
            return 5 + totalBlockCount;
        else if (blockType.blockName == "Leaf")
            return 1 + Mathf.CeilToInt(totalBlockCount / 10f);
        return blockType.cost;
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
        // CIRCLE COLLIDER CHECK
        float checkRadius = 0.45f * blockSize;

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, checkRadius, occupancyLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.transform.parent == transform || hit.transform == transform) continue;
            return true;
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
        if (spendResources && !Resources.Instance.CanAfford(dynamicCost)) return false;

        if (IsPositionOccupied(spawnPosition) && childToMove != null)
            childToMove.Move(step);

        if (spendResources && !Resources.Instance.TrySpendFood(dynamicCost)) return false;

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
}