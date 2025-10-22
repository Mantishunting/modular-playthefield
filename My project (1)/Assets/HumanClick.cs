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
                    newChild.parent = this; // Set parent

                    if (childToMove != null)
                    {
                        newChild.eastChild = childToMove;
                        childToMove.parent = newChild; // Update moved child's parent
                    }

                    // Check collisions FIRST
                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // THEN trigger wave along the affected branch
                    StartCoroutine(TriggerWaveAfterFrame(newChild, childToMove));

                }
            }
            else if (difference.x < -halfSize)
            {
                spawnPosition = blockCenter + new Vector3(-blockSize, 0, 0);
                moveDirection = new Vector3(-blockSize, 0, 0);
                childToMove = westChild;

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
                    newChild.parent = this; // Set parent

                    if (childToMove != null)
                    {
                        newChild.westChild = childToMove;
                        childToMove.parent = newChild; // Update moved child's parent
                    }

                    // Check collisions FIRST
                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // THEN trigger wave along the affected branch
                    StartCoroutine(TriggerWaveAfterFrame(newChild, childToMove));

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
                    newChild.parent = this; // Set parent

                    if (childToMove != null)
                    {
                        newChild.northChild = childToMove;
                        childToMove.parent = newChild; // Update moved child's parent
                    }

                    // Check collisions FIRST
                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // THEN trigger wave along the affected branch
                    StartCoroutine(TriggerWaveAfterFrame(newChild, childToMove));

                }
            }
            else if (difference.y < -halfSize)
            {
                spawnPosition = blockCenter + new Vector3(0, -blockSize, 0);
                moveDirection = new Vector3(0, -blockSize, 0);
                childToMove = southChild;

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
                    newChild.parent = this; // Set parent

                    if (childToMove != null)
                    {
                        newChild.southChild = childToMove;
                        childToMove.parent = newChild; // Update moved child's parent
                    }

                    // Check collisions FIRST
                    checkCollisions = true;
                    CheckAndKillCollisions(childToMove);

                    // THEN trigger wave along the affected branch
                    StartCoroutine(TriggerWaveAfterFrame(newChild, childToMove));

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

    /// <summary>
    /// Helper coroutine to trigger wave after a frame delay.
    /// This ensures the new block's Start() method has run first.
    /// </summary>
    private System.Collections.IEnumerator TriggerWaveAfterFrame(HumanClick block, HumanClick affectedChild)
    {
        yield return null; // Wait one frame
        if (block != null)
        {
            block.PropagateBranchShudder(affectedChild, 4, 0.1f);
        }
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

    // ============================================
    // CASCADING SHUDDER WAVE SYSTEM
    // ============================================

    /// <summary>
    /// Triggers a cascading shudder wave that propagates through parent and children.
    /// Starts at the specified level and counts down with each hop.
    /// </summary>
    /// <param name="startLevel">Starting shudder intensity (0-4)</param>
    /// <param name="delayBetweenBlocks">Time delay between each block's shudder</param>
    public void PropagateShudderWave(int startLevel = 4, float delayBetweenBlocks = 0.1f)
    {
        StartCoroutine(CascadeShudder(startLevel, delayBetweenBlocks));
    }

    private System.Collections.IEnumerator CascadeShudder(int currentLevel, float delay)
    {
        // Trigger this block's shudder
        BlockShudder shudder = GetComponent<BlockShudder>();
        if (shudder != null)
        {
            shudder.TriggerShudderLevel(currentLevel);
        }

        // Stop propagating if we've reached level 0
        if (currentLevel <= 0) yield break;

        // Wait before propagating to neighbors
        yield return new WaitForSeconds(delay);

        int nextLevel = currentLevel - 1;

        // Propagate to parent (if exists)
        if (parent != null)
        {
            StartCoroutine(parent.CascadeShudder(nextLevel, delay));
        }

        // Propagate to all children
        if (northChild != null)
        {
            StartCoroutine(northChild.CascadeShudder(nextLevel, delay));
        }
        if (southChild != null)
        {
            StartCoroutine(southChild.CascadeShudder(nextLevel, delay));
        }
        if (eastChild != null)
        {
            StartCoroutine(eastChild.CascadeShudder(nextLevel, delay));
        }
        if (westChild != null)
        {
            StartCoroutine(westChild.CascadeShudder(nextLevel, delay));
        }
    }

    // ============================================
    // PUBLIC GETTERS
    // ============================================

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

    // ============================================
    // CASCADING SHUDDER WAVE SYSTEM
    // ============================================

    /// <summary>
    /// Triggers a cascading shudder wave along the affected branch only.
    /// Propagates upstream to parent chain and downstream to the specified child chain.
    /// </summary>
    /// <param name="affectedChild">The specific child that was part of the insert (can be null if no child)</param>
    /// <param name="startLevel">Starting shudder intensity (0-4)</param>
    /// <param name="delayBetweenBlocks">Time delay between each block's shudder</param>
    public void PropagateBranchShudder(HumanClick affectedChild, int startLevel = 4, float delayBetweenBlocks = 0.1f)
    {
        StartCoroutine(CascadeBranchShudder(affectedChild, startLevel, delayBetweenBlocks));
    }

    private System.Collections.IEnumerator CascadeBranchShudder(HumanClick affectedChild, int currentLevel, float delay)
    {
        // Trigger this block's shudder
        BlockShudder shudder = GetComponent<BlockShudder>();
        if (shudder != null)
        {
            shudder.TriggerShudderLevel(currentLevel);
        }

        // Stop propagating if we've reached level 0
        if (currentLevel <= 0) yield break;

        // Wait before propagating
        yield return new WaitForSeconds(delay);

        int nextLevel = currentLevel - 1;

        // Propagate UPSTREAM to parent
        if (parent != null)
        {
            StartCoroutine(parent.CascadeBranchShudder(null, nextLevel, delay));
        }

        // Propagate DOWNSTREAM only to the affected child (if any)
        if (affectedChild != null)
        {
            StartCoroutine(affectedChild.CascadeBranchShudder(null, nextLevel, delay));
        }
    }
}