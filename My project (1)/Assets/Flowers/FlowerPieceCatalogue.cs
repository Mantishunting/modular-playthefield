using UnityEngine;

/// <summary>
/// The fixed set of flower parts the genome may use. Create ONE asset and put it in a
/// <b>Resources</b> folder named exactly <c>FlowerPieceCatalogue</c> (e.g.
/// <c>Assets/Resources/FlowerPieceCatalogue.asset</c>) so <see cref="GenomeService"/> can
/// <c>Resources.Load</c> it. Assign the part patterns in the inspector.
///
/// <para><see cref="root"/> is always the bloom root (GBass). <see cref="slotParts"/> are the parts a
/// RandomPiece slot may be filled with (GCross, GCurl, GFron, GPettle) — picked with even odds.
/// GBass is deliberately NOT in <see cref="slotParts"/> (it is root-only). Extend <see cref="slotParts"/>
/// to add new flower parts later.</para>
/// </summary>
[CreateAssetMenu(menuName = "Flowers/Piece Catalogue", fileName = "FlowerPieceCatalogue")]
public class FlowerPieceCatalogue : ScriptableObject
{
    [Tooltip("The bloom root part. Always GBass.")]
    public GrowthPattern root;

    [Tooltip("Parts a slot may be filled with (even odds). GBass is NOT here — it is root-only.")]
    public GrowthPattern[] slotParts;

    [Header("Evolution budget")]
    [Tooltip("Points granted each evolution (one evolution every PollinationsPerEvolution pollinations). " +
             "Each point is spent, by the weights below, on one monotonic change.")]
    public int pointsPerEvolution = 4;

    [Tooltip("Relative chance a point ADDS A PART (fills the next empty slot with a random part).")]
    public float weightAddPart = 2f;

    [Tooltip("Relative chance a point adds +1 G to a random part (grows its GMove lengths).")]
    public float weightAddG = 1f;

    [Tooltip("Relative chance a point adds +1 colour tick to a random part (shifts its hue).")]
    public float weightAddColour = 1f;
}
