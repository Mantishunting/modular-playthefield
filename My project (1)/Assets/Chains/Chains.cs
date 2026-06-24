using UnityEngine;

public enum Anchor { Root, Tip }

// Step kinds. Values are FROZEN for serialization back-compat: existing assets store `type: 1`
// (the old "Spawn"), which is now SpawnPiece — same value, so they keep working. Only ever APPEND
// new kinds with new values.
public enum StepType
{
    Move = 0,        // place a block, advance the head
    SpawnPiece = 1,  // place a block + give it a hard-coded sub-agent (was "Spawn"). FREE/unregulated.
    GMove = 2,       // like Move, but length scales with the genome G (math lands in slice B)
    RandomPiece = 3  // a genome-filled SLOT: the genome's child for this slot grows here (or nothing)
}

[System.Serializable]
public struct GrowthStep
{
    [Tooltip("Move = place+advance. GMove = Move whose length scales with genome G. " +
             "SpawnPiece = place + hard-coded sub-agent (free). RandomPiece = a genome-filled slot.")]
    public StepType type;

    [Tooltip("Root = act from the fixed starting point. Tip = act from the current head.")]
    public Anchor anchor;

    [Tooltip("Direction to place the new block.")]
    public HumanClick.Direction dir;

    [Tooltip("How many times to repeat this step (minimum 1). For RandomPiece, each repeat is one slot.")]
    public int repeats;

    [Header("SpawnPiece settings (only used if type = SpawnPiece)")]
    [Tooltip("The pattern the hard-coded sub-agent runs. RandomPiece ignores this — the genome supplies the part.")]
    public GrowthPattern spawnPattern;

    [Tooltip("If true, the main agent waits for the spawned sub-agent to finish before continuing.")]
    public bool waitForSpawn;

    [Header("GMove settings (only used if type = GMove)")]
    [Tooltip("Growth coefficient: length = round(repeats + G * gCoeff). Behaviour lands in slice B; the field exists now.")]
    public float gCoeff;
}

public class GrowthChain
{
    public string label;
    public HumanClick root;   // fixed anchor for push mode
    public HumanClick tip;    // moving head for follow mode

    public GrowthChain(string label, HumanClick start)
    {
        this.label = label;
        this.root = start;
        this.tip = start;
    }
}
