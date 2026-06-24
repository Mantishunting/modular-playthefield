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
}
