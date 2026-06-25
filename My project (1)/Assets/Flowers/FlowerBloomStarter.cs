using UnityEngine;
using System.Collections;

/// <summary>
/// Goes on a CLEAN flower block (one with NO baked <see cref="ChainPatternAgent"/> — just like
/// the wood sub-blocks the BEAN letters spawn). When the block is placed as a bloom ROOT, this
/// reads the shared genome (<see cref="GenomeService"/>), deep-copies its current part-tree, and
/// grows it: the root agent runs the root part (GBass) and each RandomPiece slot grows the genome's
/// child recursively. Spawned sub-blocks are left alone — their agents are added by
/// <see cref="ChainPatternAgent.SpawnAgentOnBlock"/>, the same untouched path the title letters use.
///
/// Snapshotting a DEEP COPY means evolving the shared genome later never mutates an already-placed
/// bloom. <see cref="rootPattern"/> is now only a fallback if the genome/catalogue isn't ready.
/// </summary>
[DisallowMultipleComponent]
public class FlowerBloomStarter : MonoBehaviour
{
    [Tooltip("Fallback root pattern, used only if the genome has no root part yet (catalogue missing).")]
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

        if (host == null || blockType == null) yield break;

        // Only a bloom ROOT starts here. Skip if an agent already exists on me (I was spawned by
        // SpawnAgentOnBlock). Otherwise climb ALL ancestors: a ChainPatternAgent keeps its
        // component on the bloom root and only moves a logical 'host' pointer as it grows, so the
        // blocks it places have NO agent component. Checking only the immediate parent therefore
        // misfires on every block past the first and recurses. If ANY ancestor has an agent, this
        // block was grown by an existing bloom -> not a root.
        if (GetComponent<ChainPatternAgent>() != null) yield break;
        for (HumanClick p = host.GetParent(); p != null; p = p.GetParent())
            if (p.GetComponent<ChainPatternAgent>() != null) yield break;

        // Genome supplies the bloom. Deep-copy the shared tree so later evolution never mutates this
        // bloom, then grow the root part; RandomPiece slots grow the genome's children recursively.
        GenomeService.EnsureExists();
        GenomeNode rootNode = GenomeService.SnapshotRoot();
        GrowthPattern startPattern = (rootNode != null && rootNode.part != null) ? rootNode.part : rootPattern;
        if (startPattern == null) yield break; // no genome root and no fallback -> nothing to grow

        var agent = gameObject.AddComponent<ChainPatternAgent>();
        agent.autoStart = false;                  // we start it explicitly below
        agent.suppressIfParentHasAgent = false;   // it's the root; nothing above to suppress against
        agent.node = rootNode;                    // the bloom's own (deep-copied) genome tree
        agent.useLocalFrame = true;               // the whole bloom grows in local frames (base = south)
        agent.StartWithPattern(startPattern, blockType);
    }
}
