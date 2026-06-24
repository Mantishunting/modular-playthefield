/// <summary>
/// The shared flower "seed": a recursive tree of parts (see <see cref="GenomeNode"/>). ONE genome is
/// shared by all flowers (<see cref="GenomeService"/>); it persists across scenes and grows by one
/// slot per evolution. A placed flower deep-copies the current tree and grows it. There is NO numeric
/// budget — the finite tree itself bounds growth.
/// </summary>
[System.Serializable]
public class FlowerGenome
{
    public int seed;          // identity; seeds the slot-fill RNG so all flowers in a run match
    public int generation;    // how many slots have been filled (evolutions)
    public GenomeNode root;   // always a GBass node
}
