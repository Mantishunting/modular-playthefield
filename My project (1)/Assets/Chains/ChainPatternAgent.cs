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

    [Header("Handedness (auto)")]
    [Tooltip("Auto-computed at start from this block's place in the tree: a branch hanging off " +
             "a WEST slot builds left-handed (East<->West mirrored); the root, a pure vertical " +
             "stem, or an EAST branch builds right-handed. Shown for debugging; the saved value " +
             "is overwritten at runtime.")]
    [SerializeField] private bool mirrorHorizontal = false;

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

        // Handedness is established by where this branch hangs in the tree (see IsLeftHanded).
        mirrorHorizontal = IsLeftHanded(host);

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

                // Apply handedness: a left-handed agent flips East<->West before placing.
                HumanClick.Direction dir = mirrorHorizontal ? MirrorDir(step.dir) : step.dir;

                // Place the block
                bool ok = actor.TryPlaceRelative(dir, blockType, true);
                if (!ok)
                {
                    MarkFinished();
                    yield break;
                }

                // Get reference to newly placed block (must use the SAME mirrored dir)
                HumanClick newBlock = ChildInDirection(actor, dir);
                if (newBlock == null)
                {
                    MarkFinished();
                    yield break;
                }

                Debug.Log($"Step {i}: type={step.type}, dir={dir} (raw {step.dir}, mirror={mirrorHorizontal}), spawnPattern={step.spawnPattern}");


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
        // Note: the sub-agent computes its OWN handedness from its bud block in TryStart,
        // so we do not copy mirrorHorizontal here.

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

    // Handedness from the tree: a block in a parent's WEST slot has an eastParent (parent is
    // to its east) => it's a LEFT block. In the EAST slot it has a westParent => RIGHT. Vertical
    // (N/S) links carry no handedness, so we climb until we meet the first horizontal link.
    // The root / a pure vertical stem reaches the top with none => right-handed (no mirror).
    static bool IsLeftHanded(HumanClick block)
    {
        HumanClick b = block;
        while (b != null)
        {
            if (b.GetEastParent() != null) return true;   // west-slot child  -> left
            if (b.GetWestParent() != null) return false;  // east-slot child  -> right

            HumanClick up = b.GetNorthParent();
            if (up == null) up = b.GetSouthParent();
            b = up;                                        // vertical link -> climb
        }
        return false; // root / vertical stem -> right-handed
    }

    // Flip East<->West for left/right-handed builds. North/South/None pass through.
    static HumanClick.Direction MirrorDir(HumanClick.Direction d)
    {
        if (d == HumanClick.Direction.East) return HumanClick.Direction.West;
        if (d == HumanClick.Direction.West) return HumanClick.Direction.East;
        return d;
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