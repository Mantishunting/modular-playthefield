using UnityEngine;

public class BlockSocket : MonoBehaviour
{
    [Tooltip("Which direction is this socket relative to the parent?")]
    public HumanClick.Direction direction;

    [Tooltip("Assign the main HumanClick component of this block here.")]
    public HumanClick parentBlock;
}