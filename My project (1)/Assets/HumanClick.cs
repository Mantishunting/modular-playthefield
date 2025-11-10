using UnityEngine;
using System.Collections.Generic;

public class HumanClick : MonoBehaviour
{
    // ADD THIS ENUM FIRST (before any fields)
    public enum Direction
    {
        None,
        North,
        South,
        East,
        West
    }

    // ===== CONNECTION CHANGE EVENT SYSTEM =====
    /// <summary>
    /// Event triggered whenever this block's parent or children connections change.
    /// Subscribe to this to react to connection changes (e.g., visual updates).
    /// </summary>
    public event System.Action OnConnectionsChanged;

    private BlockSpawner spawner;
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float clickRangeMultiplier = 1.5f;
    [SerializeField] private float clickDepthMultiplier = 1.5f;
    [SerializeField] private float wobbleDuration = 0.3f;
    [SerializeField] private float wobbleAmount = 0.1f;

    // === Global placement/destruction events (gameplay hooks) ===
    public static event System.Action<BlockType> OnBlockPlaced;
    public static event System.Action<BlockType> OnBlockDestroyed;


    private Camera mainCamera;
    private int blockId;
    private static int nextId = 0;
    private static bool isSpawning = false;
    private static bool checkCollisions = true;

    // Track total blocks in game for dynamic pricing
    private static int totalBlockCount = 0;

    // Preview system tracking - shared across all blocks
    private static bool anyBlockShowedPreviewThisFrame = false;
    private static int lastPreviewFrame = -1;

    // Block type tracking
    private BlockType myBlockType;

    // Parent tracking (mirrors child structure)
    public HumanClick northParent;
    public HumanClick southParent;
    public HumanClick eastParent;
    public HumanClick westParent;


    // Child references
    public HumanClick northChild;
    public HumanClick southChild;
    public HumanClick eastChild;
    public HumanClick westChild;

    private bool isWobbling = false;
    private float wobbleTimer = 0f;
    private Vector3 originalScale;

    void Start()
    {
        mainCamera = Camera.main;
        spawner = FindObjectOfType<BlockSpawner>();
        blockId = nextId;
        nextId++;
        originalScale = transform.localScale;

        // Increment total block count
        totalBlockCount++;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isSpawning)
        {
            HandleClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            Vector3 blockCenter = transform.position;
            float halfSize = blockSize / 2f;

            if (Mathf.Abs(mousePos.x - blockCenter.x) < halfSize &&
                Mathf.Abs(mousePos.y - blockCenter.y) < halfSize)
            {
                Die();
            }
        }

        // Handle hover preview (every frame while not spawning)
        if (!isSpawning)
        {
            UpdateHoverPreview();
        }

        if (isWobbling)
        {
            UpdateWobble();
        }
    }

    /// <summary>
    /// Notifies listeners that this block's connections have changed.
    /// Call this whenever parent or child references are modified.
    /// </summary>
    private void NotifyConnectionsChanged()
    {
        OnConnectionsChanged?.Invoke();
    }

    /// <summary>
    /// Handles hover preview display - shows ghost block where placement would occur
    /// </summary>
    void UpdateHoverPreview()
    {
        // First block to update this frame resets the flag
        int currentFrame = Time.frameCount;
        if (lastPreviewFrame != currentFrame)
        {
            lastPreviewFrame = currentFrame;

            // If no block showed preview last frame, hide it now
            if (!anyBlockShowedPreviewThisFrame && PreviewBlockManager.Instance != null)
            {
                PreviewBlockManager.Instance.HidePreview();
            }

            anyBlockShowedPreviewThisFrame = false;
        }

        // Skip if PreviewBlockManager doesn't exist
        if (PreviewBlockManager.Instance == null)
        {
            return;
        }

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 blockCenter = transform.position;
        Vector3 difference = mousePos - blockCenter;
        float halfSize = blockSize / 2f;

        // Check if mouse is within clickable range
        if (Mathf.Abs(difference.x) > halfSize * clickRangeMultiplier ||
            Mathf.Abs(difference.y) > halfSize * clickDepthMultiplier)
        {
            // Don't call HidePreview here - let other blocks handle it
            // Only the block that shows a preview should manage hiding it
            return;
        }

        // Get selected block type
        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null)
        {
            // Don't hide preview - this is a global issue, not this block's responsibility
            return;
        }

        // Calculate cost and affordability
        int dynamicCost = GetDynamicCost(selectedType);
        bool canAfford = Resources.Instance.CanAfford(dynamicCost);

        // Determine which zone and calculate spawn position
        Vector3 spawnPosition = Vector3.zero;
        HumanClick childToMove = null;
        bool isValidZone = false;

        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            // Horizontal zones
            if (difference.x > halfSize)
            {
                // East
                spawnPosition = blockCenter + new Vector3(blockSize, 0, 0);
                childToMove = eastChild;
                isValidZone = true;
            }
            else if (difference.x < -halfSize)
            {
                // West
                spawnPosition = blockCenter + new Vector3(-blockSize, 0, 0);
                childToMove = westChild;
                isValidZone = true;
            }
        }
        else
        {
            // Vertical zones
            if (difference.y > halfSize)
            {
                // North
                spawnPosition = blockCenter + new Vector3(0, blockSize, 0);
                childToMove = northChild;
                isValidZone = true;
            }
            else if (difference.y < -halfSize)
            {
                // South
                spawnPosition = blockCenter + new Vector3(0, -blockSize, 0);
                childToMove = southChild;
                isValidZone = true;
            }
        }

        // Show preview if in valid zone
        if (isValidZone)
        {
            // Check placement validity
            bool isPlacementValid = IsValidPlacement(selectedType, childToMove);

            // Check if position is already occupied (for additions, not insertions)
            if (childToMove == null && IsPositionOccupied(spawnPosition))
            {
                isPlacementValid = false;
            }

            // Only show preview if placement rules allow it
            if (isPlacementValid)
            {
                PreviewBlockManager.Instance.ShowPreview(spawnPosition, selectedType, canAfford, dynamicCost);
                anyBlockShowedPreviewThisFrame = true; // Mark that a preview is showing
            }
        }
    }

    /// <summary>
    /// Calculates the cost to place a block based on current game state
    /// Wood: 5 + totalBlockCount
    /// Leaf: 1 + Ceiling(totalBlockCount / 10)
    /// </summary>
    int GetDynamicCost(BlockType blockType)
    {
        if (blockType.blockName == "Wood")
        {
            return 5 + totalBlockCount;
        }
        else if (blockType.blockName == "Leaf")
        {
            return 1 + Mathf.CeilToInt(totalBlockCount / 10f);
        }

        // Fallback: use BlockType's cost field
        return blockType.cost;
    }

    bool IsValidPlacement(BlockType selectedType, HumanClick childToMove)
    {
        // Rule 1: Wood cannot be child of Leaf
        if (selectedType.blockName == "Wood" && myBlockType != null && myBlockType.blockName == "Leaf")
        {
            return false;
        }

        // Rule 2: Leaf cannot insert between Wood parent and Wood child
        if (selectedType.blockName == "Leaf" && childToMove != null &&
            myBlockType != null && myBlockType.blockName == "Wood" &&
            childToMove.GetBlockType() != null && childToMove.GetBlockType().blockName == "Wood")
        {
            return false;
        }

        // Rule 3: Leaf cannot insert if it would push Wood (Wood can't be child of Leaf)
        if (selectedType.blockName == "Leaf" && childToMove != null &&
            childToMove.GetBlockType() != null && childToMove.GetBlockType().blockName == "Wood")
        {
            return false;
        }

        // Rule 4: Leaf blocks must be within 3 blocks of a Wood block
        if (selectedType.blockName == "Leaf")
        {
            // Check if any Wood block exists within radius 3
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

            if (!woodFound)
            {
                return false; // No Wood nearby, cannot place Leaf
            }
        }

        // Future rules will go here

        return true;
    }

    void HandleClick()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 blockCenter = transform.position;
        Vector3 difference = mousePos - blockCenter;

        float halfSize = blockSize / 2f;

        if (Mathf.Abs(difference.x) > halfSize * clickRangeMultiplier || Mathf.Abs(difference.y) > halfSize * clickDepthMultiplier)
        {
            return;
        }

        // Get the selected block type from BlockTypeManager
        BlockType selectedType = BlockTypeManager.Instance.GetSelectedType();
        if (selectedType == null)
        {
            Debug.LogError("No block type selected!");
            return;
        }

        // Calculate dynamic cost based on total blocks in game
        int dynamicCost = GetDynamicCost(selectedType);

        // Check if player can afford this block type (early check to save processing)
        if (!Resources.Instance.CanAfford(dynamicCost))
        {
            // Not enough Food - silently fail
            return;
        }

        GameObject newBlock = null;
        Vector3 spawnPosition = Vector3.zero;
        Vector3 moveDirection = Vector3.zero;
        HumanClick childToMove = null;

        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            if (difference.x > halfSize)
            {
                spawnPosition = blockCenter + new Vector3(blockSize, 0, 0);
                moveDirection = new Vector3(blockSize, 0, 0);
                childToMove = eastChild;

                // Validate placement rules
                if (!IsValidPlacement(selectedType, childToMove))
                {
                    return;
                }

                // If adding (no child) and position occupied or in hazard, abort
                if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition)))
                {
                    return;
                }

                if (IsPositionOccupied(spawnPosition) && childToMove != null)
                {
                    checkCollisions = false;
                    childToMove.Move(moveDirection);
                }

                isSpawning = true;
                StartWobble();

                // Spend the Food cost
                if (!Resources.Instance.TrySpendFood(dynamicCost))
                {
                    // This shouldn't happen since we checked earlier, but safety check
                    isSpawning = false;
                    return;
                }

                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();

                    // ===== CONNECTION POINT 1 (EAST) =====
                    eastChild = newChild;
                    newChild.westParent = this;
                    NotifyConnectionsChanged(); // Notify THIS block changed
                    newChild.NotifyConnectionsChanged(); // Notify new block initialized

                    if (childToMove != null)
                    {
                        // ===== CONNECTION POINT 2 (EAST INSERTION) =====
                        newChild.eastChild = childToMove;
                        childToMove.westParent = newChild;
                        newChild.NotifyConnectionsChanged(); // Notify new block gained child
                        childToMove.NotifyConnectionsChanged(); // Notify moved block's parent changed
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

                    newChild.SetBlockType(selectedType);
                    OnBlockPlaced?.Invoke(selectedType);

                }
            }
            else if (difference.x < -halfSize)
            {
                spawnPosition = blockCenter + new Vector3(-blockSize, 0, 0);
                moveDirection = new Vector3(-blockSize, 0, 0);
                childToMove = westChild;

                // Validate placement rules
                if (!IsValidPlacement(selectedType, childToMove))
                {
                    return;
                }

                // If adding (no child) and position occupied or in hazard, abort
                if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition)))
                {
                    return;
                }

                if (IsPositionOccupied(spawnPosition) && childToMove != null)
                {
                    checkCollisions = false;
                    childToMove.Move(moveDirection);
                }

                isSpawning = true;
                StartWobble();

                // Spend the Food cost
                if (!Resources.Instance.TrySpendFood(dynamicCost))
                {
                    // This shouldn't happen since we checked earlier, but safety check
                    isSpawning = false;
                    return;
                }

                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();

                    // ===== CONNECTION POINT 3 (WEST) =====
                    westChild = newChild;
                    newChild.eastParent = this;
                    NotifyConnectionsChanged(); // Notify THIS block changed
                    newChild.NotifyConnectionsChanged(); // Notify new block initialized

                    if (childToMove != null)
                    {
                        // ===== CONNECTION POINT 4 (WEST INSERTION) =====
                        newChild.westChild = childToMove;
                        childToMove.eastParent = newChild;
                        newChild.NotifyConnectionsChanged(); // Notify new block gained child
                        childToMove.NotifyConnectionsChanged(); // Notify moved block's parent changed
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

                    newChild.SetBlockType(selectedType);
                    OnBlockPlaced?.Invoke(selectedType);

                }
            }
        }
        else
        {
            if (difference.y > halfSize)
            {
                spawnPosition = blockCenter + new Vector3(0, blockSize, 0);
                moveDirection = new Vector3(0, blockSize, 0);
                childToMove = northChild;

                // Validate placement rules
                if (!IsValidPlacement(selectedType, childToMove))
                {
                    return;
                }

                // If adding (no child) and position occupied or in hazard, abort
                if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition)))
                {
                    return;
                }

                if (IsPositionOccupied(spawnPosition) && childToMove != null)
                {
                    checkCollisions = false;
                    childToMove.Move(moveDirection);
                }

                isSpawning = true;
                StartWobble();

                // Spend the Food cost
                if (!Resources.Instance.TrySpendFood(dynamicCost))
                {
                    // This shouldn't happen since we checked earlier, but safety check
                    isSpawning = false;
                    return;
                }

                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();

                    // ===== CONNECTION POINT 5 (NORTH) =====
                    northChild = newChild;
                    newChild.southParent = this;
                    NotifyConnectionsChanged(); // Notify THIS block changed
                    newChild.NotifyConnectionsChanged(); // Notify new block initialized

                    if (childToMove != null)
                    {
                        // ===== CONNECTION POINT 6 (NORTH INSERTION) =====
                        newChild.northChild = childToMove;
                        childToMove.southParent = newChild;
                        newChild.NotifyConnectionsChanged(); // Notify new block gained child
                        childToMove.NotifyConnectionsChanged(); // Notify moved block's parent changed
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

                    newChild.SetBlockType(selectedType);
                    OnBlockPlaced?.Invoke(selectedType);

                }
            }
            else if (difference.y < -halfSize)
            {
                spawnPosition = blockCenter + new Vector3(0, -blockSize, 0);
                moveDirection = new Vector3(0, -blockSize, 0);
                childToMove = southChild;

                // Validate placement rules
                if (!IsValidPlacement(selectedType, childToMove))
                {
                    return;
                }

                // If adding (no child) and position occupied or in hazard, abort
                if (childToMove == null && (IsPositionOccupied(spawnPosition) || IsHazard(spawnPosition)))
                {
                    return;
                }

                if (IsPositionOccupied(spawnPosition) && childToMove != null)
                {
                    checkCollisions = false;
                    childToMove.Move(moveDirection);
                }

                isSpawning = true;
                StartWobble();

                // Spend the Food cost
                if (!Resources.Instance.TrySpendFood(dynamicCost))
                {
                    // This shouldn't happen since we checked earlier, but safety check
                    isSpawning = false;
                    return;
                }

                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();

                    // ===== CONNECTION POINT 7 (SOUTH) =====
                    southChild = newChild;
                    newChild.northParent = this;
                    NotifyConnectionsChanged(); // Notify THIS block changed
                    newChild.NotifyConnectionsChanged(); // Notify new block initialized

                    if (childToMove != null)
                    {
                        // ===== CONNECTION POINT 8 (SOUTH INSERTION) =====
                        newChild.southChild = childToMove;
                        childToMove.northParent = newChild;
                        newChild.NotifyConnectionsChanged(); // Notify new block gained child
                        childToMove.NotifyConnectionsChanged(); // Notify moved block's parent changed
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

                    newChild.SetBlockType(selectedType);
                    OnBlockPlaced?.Invoke(selectedType);

                }
            }
        }
    }

    void CheckAndKillCollisions(HumanClick blockTree)
    {
        if (blockTree == null) return;

        // Kill any block that overlaps a NoGrow collider
        if (IsHazard(blockTree.transform.position))
        {
            blockTree.Die();
            return;
        }

        if (IsPositionOccupied(blockTree.transform.position))
        {
            HumanClick blockAtPosition = blockTree.GetBlockAtMyPosition();
            if (blockAtPosition != null)
            {
                blockAtPosition.Die();
            }
        }

        if (blockTree.northChild != null)
        {
            CheckAndKillCollisions(blockTree.northChild);
        }
        if (blockTree.southChild != null)
        {
            CheckAndKillCollisions(blockTree.southChild);
        }
        if (blockTree.eastChild != null)
        {
            CheckAndKillCollisions(blockTree.eastChild);
        }
        if (blockTree.westChild != null)
        {
            CheckAndKillCollisions(blockTree.westChild);
        }
    }

    void ValidateAndRemoveInvalidLeafs(HumanClick block)
    {
        if (block == null) return;

        // Check if this is a Leaf block
        BlockType blockType = block.GetBlockType();
        if (blockType != null && blockType.blockName == "Leaf")
        {
            // Check if still within range of Wood
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

            // If no Wood nearby, this Leaf is invalid - remove it
            if (!hasWoodNearby)
            {
                block.Die();
                return; // Don't check children if this block was removed
            }
        }

        // Recursively validate children
        if (block.northChild != null)
        {
            ValidateAndRemoveInvalidLeafs(block.northChild);
        }
        if (block.southChild != null)
        {
            ValidateAndRemoveInvalidLeafs(block.southChild);
        }
        if (block.eastChild != null)
        {
            ValidateAndRemoveInvalidLeafs(block.eastChild);
        }
        if (block.westChild != null)
        {
            ValidateAndRemoveInvalidLeafs(block.westChild);
        }
    }

    // --- NoGrow hazard detection ---
    private const string HazardTag = "NoGrow";

    private bool IsHazard(Vector3 position)
    {
        Vector2 size = new Vector2(blockSize * 0.95f, blockSize * 0.95f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag(HazardTag))
                return true;
        }
        return false;
    }

    bool IsPositionOccupied(Vector3 position)
    {
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        foreach (HumanClick block in allBlocks)
        {
            if (Vector3.Distance(block.transform.position, position) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }

    

    public void Move(Vector3 direction)
    {
        transform.position += direction;

        if (northChild != null)
        {
            northChild.Move(direction);
        }
        if (southChild != null)
        {
            southChild.Move(direction);
        }
        if (eastChild != null)
        {
            eastChild.Move(direction);
        }
        if (westChild != null)
        {
            westChild.Move(direction);
        }

        if (checkCollisions)
        {
            // If this block enters a NoGrow collider, kill it
            if (IsHazard(transform.position))
            {
                Die();
                return;
            }

            // Normal block-vs-block kill behaviour
            if (IsPositionOccupied(transform.position))
            {
                HumanClick blockAtPosition = GetBlockAtMyPosition();
                if (blockAtPosition != null)
                {
                    blockAtPosition.Die();
                }
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
        // Decrement total block count
        totalBlockCount--;

        if (northChild != null)
        {
            northChild.Die();
        }
        if (southChild != null)
        {
            southChild.Die();
        }
        if (eastChild != null)
        {
            eastChild.Die();
        }
        if (westChild != null)
        {
            westChild.Die();
        }

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

    // New method to set the block type
    public void SetBlockType(BlockType blockType)
    {
        myBlockType = blockType;
        Debug.Log($"Block {blockId} set to type: {blockType.blockName}");
    }

    public BlockType GetBlockType()
    {
        return myBlockType;
    }

    public int GetBlockId()
    {
        return blockId;
    }

    public HumanClick GetParent()
    {
        // Return whichever parent exists (only one will be non-null)
        if (northParent != null) return northParent;
        if (southParent != null) return southParent;
        if (eastParent != null) return eastParent;
        if (westParent != null) return westParent;
        return null;
    }

    /// <summary>
    /// Returns which direction the parent is in (or None if no parent)
    /// </summary>
    public Direction GetParentDirection()
    {
        if (northParent != null) return Direction.North;
        if (southParent != null) return Direction.South;
        if (eastParent != null) return Direction.East;
        if (westParent != null) return Direction.West;
        return Direction.None;
    }

    public HumanClick GetNorthParent() { return northParent; }
    public HumanClick GetSouthParent() { return southParent; }
    public HumanClick GetEastParent() { return eastParent; }
    public HumanClick GetWestParent() { return westParent; }

    public HumanClick GetNorthChild() { return northChild; }
    public HumanClick GetSouthChild() { return southChild; }
    public HumanClick GetEastChild() { return eastChild; }
    public HumanClick GetWestChild() { return westChild; }

    // External placement hook used by growth scripts
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

        // Connect new block
        if (dir == Direction.East) { eastChild = newChild; newChild.westParent = this; }
        if (dir == Direction.West) { westChild = newChild; newChild.eastParent = this; }
        if (dir == Direction.North) { northChild = newChild; newChild.southParent = this; }
        if (dir == Direction.South) { southChild = newChild; newChild.northParent = this; }

        NotifyConnectionsChanged();
        newChild.NotifyConnectionsChanged();
        CheckAndKillCollisions(childToMove);
        if (childToMove != null) ValidateAndRemoveInvalidLeafs(childToMove);

        return true;
    }


}
