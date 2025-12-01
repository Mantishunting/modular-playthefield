using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ChainPatternAgent : MonoBehaviour
{
    [Header("Pattern")]
    public GrowthPattern pattern;     // assign in prefab
    public BlockType blockType;       // assign in prefab

    [Header("Timing")]
    public bool overrideInterval = false;
    public float intervalSeconds = 0.2f;

    [Header("Lifecycle")]
    public bool autoStart = true;     // start automatically on enable
    public bool suppressIfParentHasAgent = true; // prevents fan-out without editing HumanClick

    private bool _running = false;
    public bool IsRunning => _running;
    public bool IsFinished { get; private set; } = false;

    [Header("Chains")]
    public string initialLabel = "MainStem";

    private Dictionary<string, GrowthChain> chains = new();
    private GrowthChain current;
    private HumanClick host;

    // Event fired when this agent completes (useful for waitForSpawn)
    public event System.Action OnFinished;

    void Awake()
    {
        host = GetComponent<HumanClick>();
    }

    void OnEnable()
    {
        if (autoStart)
            StartCoroutine(TryStart());
    }

    /// <summary>
    /// Manually start the agent with a specific pattern (used by spawned agents).
    /// </summary>
    public void StartWithPattern(GrowthPattern newPattern, BlockType newBlockType)
    {
        pattern = newPattern;
        blockType = newBlockType;
        autoStart = false; // prevent double-start
        StartCoroutine(TryStart());
    }

    IEnumerator TryStart()
    {
        // Let wiring & parent components settle across frames
        yield return null;
        yield return null;

        if (host == null || blockType == null || pattern == null || pattern.steps == null || pattern.steps.Length == 0)
        {
            MarkFinished();
            enabled = false;
            yield break;
        }

        // RACE-PROOF SUPPRESSION:
        // If my parent has ANY ChainPatternAgent (regardless of running), I must not start.
        // EXCEPTION: If I was manually started via StartWithPattern, skip this check.
        if (suppressIfParentHasAgent)
        {
            var parent = host.GetParent();
            if (parent != null && parent.GetComponent<ChainPatternAgent>() != null)
            {
                MarkFinished();
                enabled = false;
                yield break;
            }
        }

        if (_running) yield break;
        _running = true;

        current = new GrowthChain(initialLabel, host);
        chains[current.label] = current;

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // Safety frame in case something else needs to initialize this tick
        yield return null;

        int i = 0;
        while (true)
        {
            var step = pattern.steps[i];
            int reps = Mathf.Max(1, step.repeats);

            for (int r = 0; r < reps; r++)
            {
                // Choose anchor: Root = push-from-base; Tip = act at the current head
                HumanClick actor = (step.anchor == Anchor.Root) ? current.root : current.tip;
                if (actor == null)
                {
                    MarkFinished();
                    yield break;
                }

                // Place the block
                bool ok = actor.TryPlaceRelative(step.dir, blockType, true);
                if (!ok)
                {
                    MarkFinished();
                    yield break;
                }

                // Get reference to newly placed block
                HumanClick newBlock = ChildInDirection(actor, step.dir);
                if (newBlock == null)
                {
                    MarkFinished();
                    yield break;
                }

                Debug.Log($"Step {i}: type={step.type}, dir={step.dir}, spawnPattern={step.spawnPattern}");


                // Handle spawn vs move
                if (step.type == StepType.Spawn && step.spawnPattern != null)
                {
                    // Spawn a new agent on the newly placed block
                    ChainPatternAgent spawnedAgent = SpawnAgentOnBlock(newBlock, step.spawnPattern);

                    if (step.waitForSpawn && spawnedAgent != null)
                    {
                        // Wait for the spawned agent to complete
                        while (!spawnedAgent.IsFinished)
                        {
                            yield return null;
                        }
                    }

                    // For Spawn steps, we typically don't advance the tip
                    // (the main agent stays where it was, the spawned agent does its thing)
                }
                else
                {
                    // Normal Move behavior: advance the tip if using Tip anchor
                    if (step.anchor == Anchor.Tip)
                    {
                        current.tip = newBlock;
                        host = newBlock; // logical handover; still one coroutine
                    }
                }

                float dt = overrideInterval ? intervalSeconds : pattern.intervalSeconds;
                if (dt > 0f) yield return new WaitForSeconds(dt);
                else yield return null;
            }

            i++;
            if (i >= pattern.steps.Length)
            {
                if (!pattern.loop)
                {
                    MarkFinished();
                    yield break;
                }
                i = 0;
            }
        }
    }

    /// <summary>
    /// Spawns a new ChainPatternAgent on the target block with the given pattern.
    /// </summary>
    ChainPatternAgent SpawnAgentOnBlock(HumanClick targetBlock, GrowthPattern spawnPattern)
    {
        if (targetBlock == null || spawnPattern == null) return null;

        // Check if there's already an agent on this block
        ChainPatternAgent existingAgent = targetBlock.GetComponent<ChainPatternAgent>();
        if (existingAgent != null)
        {
            // Already has an agent - don't add another
            Debug.LogWarning($"Block already has a ChainPatternAgent, skipping spawn.");
            return existingAgent;
        }

        // Add a new agent component
        ChainPatternAgent newAgent = targetBlock.gameObject.AddComponent<ChainPatternAgent>();
        newAgent.suppressIfParentHasAgent = false; // We're intentionally spawning this
        newAgent.autoStart = false;
        newAgent.overrideInterval = overrideInterval;
        newAgent.intervalSeconds = intervalSeconds;

        // Start it with the spawn pattern
        newAgent.StartWithPattern(spawnPattern, blockType);

        return newAgent;
    }

    void MarkFinished()
    {
        _running = false;
        IsFinished = true;
        OnFinished?.Invoke();
    }

    HumanClick ChildInDirection(HumanClick node, HumanClick.Direction dir)
    {
        return dir switch
        {
            HumanClick.Direction.North => node.GetNorthChild(),
            HumanClick.Direction.East => node.GetEastChild(),
            HumanClick.Direction.South => node.GetSouthChild(),
            HumanClick.Direction.West => node.GetWestChild(),
            _ => null
        };
    }
}