using UnityEngine;
using System.Collections.Generic;

public class HumanClick : MonoBehaviour
{
    private BlockSpawner spawner;
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float clickRangeMultiplier = 1.5f;
    [SerializeField] private float clickDepthMultiplier = 1.5f;
    [SerializeField] private float wobbleDuration = 0.3f;
    [SerializeField] private float wobbleAmount = 0.1f;

    private Camera mainCamera;
    private int blockId;
    private static int nextId = 0;
    private static bool isSpawning = false;
    private static bool checkCollisions = true;

    // Block type tracking
    private BlockType myBlockType;

    // Parent tracking
    private HumanClick parent;

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

        if (isWobbling)
        {
            UpdateWobble();
        }
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

                // If adding (no child) and position occupied, abort
                if (childToMove == null && IsPositionOccupied(spawnPosition))
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
                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();
                    eastChild = newChild;
                    newChild.parent = this;

                    if (childToMove != null)
                    {
                        newChild.eastChild = childToMove;
                        childToMove.parent = newChild;
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

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

                // If adding (no child) and position occupied, abort
                if (childToMove == null && IsPositionOccupied(spawnPosition))
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
                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();
                    westChild = newChild;
                    newChild.parent = this;

                    if (childToMove != null)
                    {
                        newChild.westChild = childToMove;
                        childToMove.parent = newChild;
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

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

                // If adding (no child) and position occupied, abort
                if (childToMove == null && IsPositionOccupied(spawnPosition))
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
                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();
                    northChild = newChild;
                    newChild.parent = this;

                    if (childToMove != null)
                    {
                        newChild.northChild = childToMove;
                        childToMove.parent = newChild;
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

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

                // If adding (no child) and position occupied, abort
                if (childToMove == null && IsPositionOccupied(spawnPosition))
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
                newBlock = spawner.SpawnBlockAt(spawnPosition, selectedType);
                if (newBlock != null)
                {
                    HumanClick newChild = newBlock.GetComponent<HumanClick>();
                    southChild = newChild;
                    newChild.parent = this;

                    if (childToMove != null)
                    {
                        newChild.southChild = childToMove;
                        childToMove.parent = newChild;
                    }

                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // Validate that pushed Leaf blocks are still within range of Wood
                    if (childToMove != null)
                    {
                        ValidateAndRemoveInvalidLeafs(childToMove);
                    }

                }
            }
        }
    }

    void CheckAndKillCollisions(HumanClick blockTree)
    {
        if (blockTree == null) return;

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

        if (checkCollisions && IsPositionOccupied(transform.position))
        {
            HumanClick blockAtPosition = GetBlockAtMyPosition();
            if (blockAtPosition != null)
            {
                blockAtPosition.Die();
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
        return parent;
    }

    public HumanClick GetNorthChild() { return northChild; }
    public HumanClick GetSouthChild() { return southChild; }
    public HumanClick GetEastChild() { return eastChild; }
    public HumanClick GetWestChild() { return westChild; }

}