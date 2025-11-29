using UnityEngine;

public class BlockInitializer : MonoBehaviour
{
    private HumanClick humanClick;

    void Awake()
    {
        // 1. Get the HumanClick component on this same GameObject
        humanClick = GetComponent<HumanClick>();

        // 2. If successful, reset its connections immediately
        if (humanClick != null)
        {
            ResetConnections();
        }
    }

    private void ResetConnections()
    {
        // This is the core logic that was previously suggested for HumanClick.
        // It uses the public fields of HumanClick to reset the links.
        humanClick.northParent = null;
        humanClick.southParent = null;
        humanClick.eastParent = null;
        humanClick.westParent = null;

        humanClick.northChild = null;
        humanClick.southChild = null;
        humanClick.eastChild = null;
        humanClick.westChild = null;
    }
}