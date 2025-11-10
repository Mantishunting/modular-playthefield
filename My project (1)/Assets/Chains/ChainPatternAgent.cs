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
    public bool IsRunning => _running; // readable by others if ever needed

    [Header("Chains")]
    public string initialLabel = "MainStem";

    private Dictionary<string, GrowthChain> chains = new();
    private GrowthChain current;
    private HumanClick host;

    void Awake()
    {
        host = GetComponent<HumanClick>();
    }

    void OnEnable()
    {
        if (autoStart)
            StartCoroutine(TryStart());
    }


    IEnumerator TryStart()
    {
        // Let wiring & parent components settle across frames
        yield return null;
        yield return null;

        if (host == null || blockType == null || pattern == null || pattern.steps == null || pattern.steps.Length == 0)
        { enabled = false; yield break; }

        // RACE-PROOF SUPPRESSION:
        // If my parent has ANY ChainPatternAgent (regardless of running), I must not start.
        var parent = host.GetParent();
        if (parent != null && parent.GetComponent<ChainPatternAgent>() != null)
        {
            enabled = false;
            yield break;
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
                if (actor == null) { _running = false; yield break; }

                // If diagnosing economy, temporarily change 'true' to 'false' here
                bool ok = actor.TryPlaceRelative(step.dir, blockType, true);
                if (!ok) { _running = false; yield break; }

                if (step.anchor == Anchor.Tip)
                {
                    // After placing, adopt the new head so a single agent "rides the tip"
                    HumanClick newHead = ChildInDirection(actor, step.dir);
                    if (newHead == null) { _running = false; yield break; }

                    current.tip = newHead;
                    host = newHead; // logical handover; still one coroutine
                }

                float dt = overrideInterval ? intervalSeconds : pattern.intervalSeconds;
                if (dt > 0f) yield return new WaitForSeconds(dt);
                else yield return null;
            }

            i++;
            if (i >= pattern.steps.Length)
            {
                if (!pattern.loop) { _running = false; yield break; }
                i = 0;
            }
        }
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
