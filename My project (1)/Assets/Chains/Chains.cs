using UnityEngine;

public enum Anchor { Root, Tip }
public enum StepType { Move, Spawn }

[System.Serializable]
public struct GrowthStep
{
    [Tooltip("Move = place a block and optionally advance. Spawn = place a block with its own growth agent.")]
    public StepType type;

    [Tooltip("Root = act from the fixed starting point. Tip = act from the current head.")]
    public Anchor anchor;

    [Tooltip("Direction to place the new block.")]
    public HumanClick.Direction dir;

    [Tooltip("How many times to repeat this step (minimum 1).")]
    public int repeats;

    [Header("Spawn Settings (only used if type = Spawn)")]
    [Tooltip("The pattern the spawned agent should execute. Leave null to use the same blockType but no agent.")]
    public GrowthPattern spawnPattern;

    [Tooltip("If true, the main agent will wait for the spawned agent to finish before continuing.")]
    public bool waitForSpawn;
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