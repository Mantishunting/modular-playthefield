using UnityEngine;

public enum Anchor { Root, Tip }

[System.Serializable]
public struct GrowthStep
{
    public Anchor anchor;                 // Root = push from anchor; Tip = follow the head
    public HumanClick.Direction dir;      // North/East/South/West
    public int repeats;                   // how many times to apply this step (>=1)
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
