using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HumanClick : MonoBehaviour
{
    public enum Direction { None, North, South, East, West }

    public event System.Action OnConnectionsChanged;

    private BlockSpawner spawner;
    [SerializeField] private float blockSize = 1f;


    // --- SOCKET SETTINGS ---
    [Header("Socket System")]
    [SerializeField] private LayerMask socketLayer;
    [SerializeField] private float socketCheckRadius = 0.4f;

    [Header("Sockets")]
    // Assign these to the Child Objects in the Inspector
    public GameObject socketNorth;
    public GameObject socketSouth;
    public GameObject socketEast;
    public GameObject socketWest;

    [Header("Wobble Settings")]
    [SerializeField] private float wobbleDuration = 0.3f;
    [SerializeField] private float wobbleAmount = 0.1f;

    [Header("Continuous Placement")]
    [SerializeField] private float continuousPlacementDelay = 0.1f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask occupancyLayer;

    // Events
    public static event System.Action<BlockType> OnBlockPlaced;
    public static event System.Action<BlockType> OnBlockDestroyed;

    // Static Data
    private static int nextId = 0;
    private static bool isSpawning = false;
    private static bool checkCollisions = true;
    private static int totalBlockCount = 0;
    private static bool anyBlockShowedPreviewThisFrame = false;
    private static int lastPreviewFrame = -1;

    public static void ResetStaticData()
    {
        nextId = 0;
        totalBlockCount = 0;
        isSpawning = false;
        checkCollisions = true;
        anyBlockShowedPreviewThisFrame = false;
        lastPreviewFrame = -1;
        DeletePreviewSystem.ResetStaticData();
    }

    private Camera mainCamera;
    private int blockId;

    [SerializeField] private BlockType myBlockType;

    // Relationships
    public HumanClick northParent;
    public HumanClick southParent;
    public HumanClick eastParent;
    public HumanClick westParent;

    public HumanClick northChild;
    public HumanClick southChild;
    public HumanClick eastChild;
    public HumanClick westChild;

    // Internal State
    private bool isWobbling = false;
    private float wobbleTimer = 0f;
    private Vector3 originalScale;

    private float lastPlacementTime = 0f;
    private bool isHoldingLeftClick = false;

    private float rightClickDownTime = 0f;
    [SerializeField] private float clickThreshold = 0.25f;

    private const string HazardTag = "NoGrow";

    void Start()
    {
        mainCamera = Camera.main;
        spawner = FindObjectOfType<BlockSpawner>();

        // --- UNIQUE NAMING LOGIC ---
        blockId = nextId;
        nextId++;
        this.name = $"Block_{blockId}";

        originalScale = transform.localScale;
        totalBlockCount++;

        // Auto-find sockets if not assigned
        if (socketNorth == null) socketNorth = transform.Find("Socket_N")?.gameObject;
        if (socketSouth == null) socketSouth = transform.Find("Socket_S")?.gameObject;
        if (socketEast == null) socketEast = transform.Find("Socket_E")?.gameObject;
        if (socketWest == null) socketWest = transform.Find("Socket_W")?.gameObject;

        UpdateSocketVisuals();
    }

    void Update()
    {
        // --- INPUT HANDLING ---
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

    // ==========================================================
    // SOCKET VISUALS
    // ==========================================================
    public void UpdateSocketVisuals()
    {
        // Hide sockets if a child OR a parent is attached in that direction.
        if (socketNorth) socketNorth.SetActive(northChild == null && northParent == null);
        if (socketSouth) socketSouth.SetActive(southChild == null && southParent == null);
        if (socketEast) socketEast.SetActive(eastChild == null && eastParent == null);
        if (socketWest) socketWest.SetActive(westChild == null && westParent == null);
    }

    // ==========================================================
    // VISUALIZATION LOGIC (SOCKET BASED)
    // ==========================================================
    void UpdateHoverPreview()
    {
        if (PreviewBlockManager.Instance == null) return;

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Collider2D hit = Physics2D.OverlapCircle(mousePos, socketCheckRadius, socketLayer);

        int currentFrame = Time.frameCount;
        if (hit == null)
        {
            if (lastPreviewFrame != currentFrame)
            {
                lastPreviewFrame = currentFrame;
                if (!anyBlockShowedPreviewThisFrame)
                    PreviewBlockManager.Instance.HidePreview();
                anyBlockShowedPreviewThisFrame = false;
            }
            return;
        }

        BlockSocket hitSocket = hit.GetComponent<BlockSocket>();
        if (hitSocket == null || hitSocket.parentBlock == null) return;

        HumanClick parent = hitSocket.parentBlock;
        Vector3 spawnPosition = hitSocket.transform.position;

        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null) return;

        int dynamicCost = parent.GetDynamicCost(selectedType);
        bool canAfford = Resources.Instance.CanAfford(dynamicCost);

        PreviewBlockManager.Instance.ShowPreview(spawnPosition, selectedType, canAfford, dynamicCost, parent);
        anyBlockShowedPreviewThisFrame = true;
    }

    // ==========================================================
    // CLICK / SPAWN LOGIC
    // ==========================================================
    void HandleClick()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // 1. INSERT CHECK
        HumanClick insertTarget = DetectInsertTarget(mousePos);
        if (insertTarget != null)
        {
            DoInsert(insertTarget);
            return;
        }

        // 2. SOCKET CHECK
        Collider2D hit = Physics2D.OverlapCircle(mousePos, socketCheckRadius, socketLayer);
        if (hit == null) return;

        BlockSocket hitSocket = hit.GetComponent<BlockSocket>();
        if (hitSocket == null || hitSocket.parentBlock == null) return;

        HumanClick parent = hitSocket.parentBlock;
        Vector3 spawnPos = hitSocket.transform.position;

        // Determine direction from parent to socket
        Vector3 spawnDirection = (hitSocket.transform.position - parent.transform.position).normalized * blockSize;
        spawnDirection.x = Mathf.Round(spawnDirection.x);
        spawnDirection.y = Mathf.Round(spawnDirection.y);

        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null) return;

        // Validate placement (no child shifting in this path)
        if (!parent.IsValidPlacement(selectedType, null)) return;

        int dynamicCost = parent.GetDynamicCost(selectedType);
        if (!Resources.Instance.CanAfford(dynamicCost)) return;

        if (IsPositionOccupied(spawnPos)) return;

        // Execute placement
        isSpawning = true;
        parent.StartWobble();

        if (!Resources.Instance.TrySpendFood(dynamicCost))
        {
            isSpawning = false;
            return;
        }

        GameObject newBlock = spawner.SpawnBlockAt(spawnPos, selectedType);
        if (newBlock != null)
        {
            HumanClick newChild = newBlock.GetComponent<HumanClick>();
            if (newChild != null)
            {
                parent.LinkChild(spawnDirection, newChild);
                newChild.SetBlockType(selectedType);
                OnBlockPlaced?.Invoke(selectedType);
            }
        }
    }

    // ==========================================================
    // INSERTION LOGIC
    // ==========================================================
    private HumanClick DetectInsertTarget(Vector3 mousePos)
    {
        float radius = (blockSize / 2f) * transform.localScale.x * 1.1f;

        if (northChild != null && Vector3.Distance(mousePos, northChild.transform.position) < radius) return northChild;
        if (southChild != null && Vector3.Distance(mousePos, southChild.transform.position) < radius) return southChild;
        if (eastChild != null && Vector3.Distance(mousePos, eastChild.transform.position) < radius) return eastChild;
        if (westChild != null && Vector3.Distance(mousePos, westChild.transform.position) < radius) return westChild;

        return null;
    }

    private void DoInsert(HumanClick childToShift)
    {
        if (childToShift == null) return;

        Vector3 dir = (childToShift.transform.position - transform.position).normalized;
        Vector3 moveStep = dir * blockSize;
        Vector3 newPos = transform.position + moveStep;

        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null) return;

        if (!IsValidPlacement(selectedType, childToShift))
        {
            if (Resources.Instance.ShowDebugLogs)
                Debug.Log("Insert blocked by placement rules.");
            return;
        }

        int dynamicCost = GetDynamicCost(selectedType);
        if (!Resources.Instance.CanAfford(dynamicCost)) return;
        if (!Resources.Instance.TrySpendFood(dynamicCost)) return;

        // Shift existing child first
        childToShift.Move(moveStep);

        GameObject newBlock = spawner.SpawnBlockAt(newPos, selectedType);
        if (newBlock == null) return;

        HumanClick newChild = newBlock.GetComponent<HumanClick>();
        if (newChild == null) return;

        // Cleanly detach old child from this block
        UnlinkDirectChild(childToShift);

        // Link new child to this, then old child to new child
        LinkChild(moveStep, newChild);
        newChild.LinkChild(moveStep, childToShift);

        newChild.SetBlockType(selectedType);
        OnBlockPlaced?.Invoke(selectedType);

        CheckAndKillCollisions(childToShift);
        ValidateAndRemoveInvalidLeafs(childToShift);
    }

    // ==========================================================
    // LINKING & MOVEMENT
    // ==========================================================
    private void LinkChild(Vector3 dir, HumanClick child)
    {
        if (child == null) return;

        Vector2 d = dir.normalized;
        float eastDot = Vector2.Dot(d, Vector2.right);
        float westDot = Vector2.Dot(d, Vector2.left);
        float northDot = Vector2.Dot(d, Vector2.up);
        float southDot = Vector2.Dot(d, Vector2.down);
        float max = Mathf.Max(eastDot, westDot, northDot, southDot);

        child.ClearAllParents();

        if (Mathf.Approximately(max, eastDot))
        {
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

        UpdateSocketVisuals();
        child.UpdateSocketVisuals();
    }

    private void ClearAllParents()
    {
        if (northParent != null)
        {
            northParent.southChild = null;
            northParent.NotifyConnectionsChanged();
            northParent.UpdateSocketVisuals();
            northParent = null;
        }
        if (southParent != null)
        {
            southParent.northChild = null;
            southParent.NotifyConnectionsChanged();
            southParent.UpdateSocketVisuals();
            southParent = null;
        }
        if (eastParent != null)
        {
            eastParent.westChild = null;
            eastParent.NotifyConnectionsChanged();
            eastParent.UpdateSocketVisuals();
            eastParent = null;
        }
        if (westParent != null)
        {
            westParent.eastChild = null;
            westParent.NotifyConnectionsChanged();
            westParent.UpdateSocketVisuals();
            westParent = null;
        }
    }

    // Cleanly unlinks a direct child from this block without triggering ClearAllParents
    private void UnlinkDirectChild(HumanClick child)
    {
        if (child == null) return;

        if (northChild == child) { northChild = null; child.southParent = null; }
        if (southChild == child) { southChild = null; child.northParent = null; }
        if (eastChild == child) { eastChild = null; child.westParent = null; }
        if (westChild == child) { westChild = null; child.eastParent = null; }
    }

    public void Move(Vector3 direction)
    {
        transform.position += direction;
        if (northChild != null) northChild.Move(direction);
        if (southChild != null) southChild.Move(direction);
        if (eastChild != null) eastChild.Move(direction);
        if (westChild != null) westChild.Move(direction);

        if (checkCollisions && IsHazard(transform.position))
        {
            Die();
        }
    }

    // ==========================================================
    // EXTERNAL API (for other systems)
    // ==========================================================
    public HumanClick GetNorthParent() => northParent;
    public HumanClick GetSouthParent() => southParent;
    public HumanClick GetEastParent() => eastParent;
    public HumanClick GetWestParent() => westParent;

    public HumanClick GetNorthChild() => northChild;
    public HumanClick GetSouthChild() => southChild;
    public HumanClick GetEastChild() => eastChild;
    public HumanClick GetWestChild() => westChild;

    public Direction GetParentDirection()
    {
        if (northParent != null) return Direction.North;
        if (southParent != null) return Direction.South;
        if (eastParent != null) return Direction.East;
        if (westParent != null) return Direction.West;
        return Direction.None;
    }

    // Used by GrowthAgent and ChainPatternAgent to grow automatically
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

    // ==========================================================
    // VALIDATION & RULES
    // ==========================================================
    public int GetDynamicCost(BlockType blockType)
    {
        if (blockType == null) return 0;

        if (blockType.blockName == "Wood")
            return 5 + totalBlockCount;
        else if (blockType.blockName == "Leaf")
            return 1 + Mathf.CeilToInt(totalBlockCount / 10f);

        return blockType.cost;
    }

    public bool IsValidPlacement(BlockType selectedType, HumanClick childToMove)
    {
        if (selectedType == null) return false;

        BlockType parentType = myBlockType;
        BlockType movingType = (childToMove != null) ? childToMove.GetBlockType() : null;

        // -----------------------------------------------------------
        // RULE 1 — Wood cannot grow from Leaf/Flower
        // -----------------------------------------------------------
        if (selectedType.blockName == "Wood")
        {
            if (parentType != null &&
                (parentType.blockName == "Leaf" || parentType.blockName == "Flower"))
            {
                return false;
            }
        }

        // -----------------------------------------------------------
        // RULE 2 — Leaf cannot be inserted between two Wood blocks
        // (only applies when we are INSERTING, i.e. childToMove != null)
        // -----------------------------------------------------------
        if (selectedType.blockName == "Leaf" && childToMove != null)
        {
            if (parentType != null &&
                movingType != null &&
                parentType.blockName == "Wood" &&
                movingType.blockName == "Wood")
            {
                // BLOCK: inserting Leaf between Wood + Wood
                return false;
            }
        }

        // -----------------------------------------------------------
        // SPECIAL CASE — Leaf added directly onto Wood is ALWAYS OK
        // (ADD, not INSERT: childToMove == null)
        // -----------------------------------------------------------
        if (selectedType.blockName == "Leaf" &&
            childToMove == null &&                    // this is an ADD, not insert
            parentType != null &&
            parentType.blockName == "Wood")
        {
            // Parent is Wood, so the "near Wood" requirement is trivially true.
            return true;
        }

        // -----------------------------------------------------------
        // RULE 3 — Leaf requires at least one Wood within radius
        // (for all other Leaf placements: e.g. Leaf from Leaf, etc.)
        // -----------------------------------------------------------
        if (selectedType.blockName == "Leaf")
        {
            List<HumanClick> nearbyBlocks = TreeLooker.GetBlocksInRadius(transform.position, 3f);

            bool woodFound = false;
            foreach (HumanClick block in nearbyBlocks)
            {
                if (block == null) continue;
                BlockType bt = block.GetBlockType();
                if (bt != null && bt.blockName == "Wood")
                {
                    woodFound = true;
                    break;
                }
            }

            if (!woodFound)
                return false;
        }

        return true;
    }


    // ==========================================================
    // HAZARDS, COLLISIONS & LEAF CLEANUP
    // ==========================================================
    private bool IsHazard(Vector3 position)
    {
        float radius = (blockSize / 2f) * 0.95f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag(HazardTag)) return true;
        }
        return false;
    }

    private bool IsPositionOccupied(Vector3 position)
    {
        float checkRadius = 0.45f * blockSize;
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, checkRadius, occupancyLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform == transform || hit.transform.parent == transform) continue;
            return true;
        }
        return false;
    }

    private void CheckAndKillCollisions(HumanClick blockTree)
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

    private void ValidateAndRemoveInvalidLeafs(HumanClick block)
    {
        if (block == null) return;

        BlockType blockType = block.GetBlockType();
        if (blockType != null && blockType.blockName == "Leaf")
        {
            List<HumanClick> nearbyBlocks = TreeLooker.GetBlocksInRadius(block.transform.position, 3f);

            bool hasWoodNearby = false;
            foreach (HumanClick nearbyBlock in nearbyBlocks)
            {
                if (nearbyBlock == null) continue;
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

    // ==========================================================
    // DESTRUCTION & WOBBLE
    // ==========================================================
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

    private void StartWobble()
    {
        isWobbling = true;
        wobbleTimer = 0f;
    }

    private void UpdateWobble()
    {
        wobbleTimer += Time.deltaTime;
        if (wobbleTimer < wobbleDuration)
        {
            float wobble = Mathf.Sin(wobbleTimer * Mathf.PI * 2f / wobbleDuration) * wobbleAmount;
            transform.localScale = originalScale * (1f + wobble);
        }
        else
        {
            transform.localScale = originalScale;
            isWobbling = false;
            isSpawning = false;
        }
    }

    // ==========================================================
    // BLOCK TYPE & ID HELPERS
    // ==========================================================
    public void SetBlockType(BlockType blockType)
    {
        myBlockType = blockType;
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

    // ==========================================================
    // DELETE / RIGHT-CLICK
    // ==========================================================
    private void HandleRightClickDelete()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        float radius = (blockSize / 2f) * transform.localScale.x;

        if (Vector3.Distance(mousePos, transform.position) < radius)
        {
            BlockType bt = GetBlockType();
            if (!Resources.Instance.AllowPlayerDestroyWood &&
                bt != null && bt.blockName == "Wood")
                return;

            DeletePreviewSystem previewSystem = GetComponent<DeletePreviewSystem>();
            if (previewSystem != null)
            {
                if (previewSystem.HandleRightClick())
                    Die();
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
}
