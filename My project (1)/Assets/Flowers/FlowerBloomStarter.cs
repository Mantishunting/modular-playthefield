using UnityEngine;
using System.Collections;

/// <summary>
/// Goes on a CLEAN flower block (one with NO baked <see cref="ChainPatternAgent"/> — just like
/// the wood sub-blocks the BEAN letters spawn). When the block is placed as a bloom ROOT, this
/// adds an agent at runtime and starts it on <see cref="rootPattern"/>. Spawned sub-blocks are
/// left alone — their agents are added by <see cref="ChainPatternAgent.SpawnAgentOnBlock"/>, the
/// same untouched path the title-screen letters use.
///
/// Step 1 of the genome engine: prove clean blocks grow (spawns fire) without touching any shared
/// code. Step 2 will replace the serialized <see cref="rootPattern"/> with a genome-supplied part.
/// </summary>
[DisallowMultipleComponent]
public class FlowerBloomStarter : MonoBehaviour
{
    [Tooltip("Pattern the root agent runs. (Later: supplied by the genome instead.)")]
    public GrowthPattern rootPattern;

    [Tooltip("Block type the bloom grows. Self-referential for the isolated test rig.")]
    public BlockType blockType;

    private HumanClick host;

    private void Awake()
    {
        host = GetComponent<HumanClick>();
    }

    private IEnumerator Start()
    {
        // Let placement + parent/child linking settle (same two-frame wait ChainPatternAgent uses).
        yield return null;
        yield return null;

        if (host == null || rootPattern == null || blockType == null) yield break;

        // Only a bloom ROOT starts here. Skip if an agent already exists on me (I was spawned by
        // SpawnAgentOnBlock). Otherwise climb ALL ancestors: a ChainPatternAgent keeps its
        // component on the bloom root and only moves a logical 'host' pointer as it grows, so the
        // blocks it places have NO agent component. Checking only the immediate parent therefore
        // misfires on every block past the first and recurses. If ANY ancestor has an agent, this
        // block was grown by an existing bloom -> not a root.
        if (GetComponent<ChainPatternAgent>() != null) yield break;
        for (HumanClick p = host.GetParent(); p != null; p = p.GetParent())
            if (p.GetComponent<ChainPatternAgent>() != null) yield break;

        // Genome -> per-bloom agent allowance. Snapshot the budget onto THIS root block; every agent
        // the bloom grows climbs here to claim a slot (see ChainPatternAgent.TryStart). The genome
        // only caps agent count in this slice; rootPattern still decides the shape.
        GenomeService.EnsureExists();
        var budget = gameObject.AddComponent<RoutineBudget>();
        budget.capacity = GenomeService.Current.routineBudget;

        var agent = gameObject.AddComponent<ChainPatternAgent>();
        agent.autoStart = false;                  // we start it explicitly below
        agent.suppressIfParentHasAgent = false;   // it's the root; nothing above to suppress against
        agent.StartWithPattern(rootPattern, blockType);
    }
}
