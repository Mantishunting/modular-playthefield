using UnityEngine;

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
                newBlock = spawner.SpawnBlockAt(spawnPosition);
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

                }
            }
            else if (difference.x < -halfSize)
            {
                spawnPosition = blockCenter + new Vector3(-blockSize, 0, 0);
                moveDirection = new Vector3(-blockSize, 0, 0);
                childToMove = westChild;

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
                newBlock = spawner.SpawnBlockAt(spawnPosition);
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
                newBlock = spawner.SpawnBlockAt(spawnPosition);
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

                }
            }
            else if (difference.y < -halfSize)
            {
                spawnPosition = blockCenter + new Vector3(0, -blockSize, 0);
                moveDirection = new Vector3(0, -blockSize, 0);
                childToMove = southChild;

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
                newBlock = spawner.SpawnBlockAt(spawnPosition);
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