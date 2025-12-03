using UnityEngine;

[RequireComponent(typeof(HumanClick))]
public class BlockDebugger : MonoBehaviour
{
    private HumanClick myBlock;

    void Start()
    {
        myBlock = GetComponent<HumanClick>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            ReportDebugStatus();
        }
    }

    void ReportDebugStatus()
    {
        if (myBlock == null) return;

        // Children
        string nChild = myBlock.northChild != null ? myBlock.northChild.name : "None";
        string sChild = myBlock.southChild != null ? myBlock.southChild.name : "None";
        string eChild = myBlock.eastChild != null ? myBlock.eastChild.name : "None";
        string wChild = myBlock.westChild != null ? myBlock.westChild.name : "None";

        // Parents
        string nParent = myBlock.northParent != null ? myBlock.northParent.name : "None";
        string sParent = myBlock.southParent != null ? myBlock.southParent.name : "None";
        string eParent = myBlock.eastParent != null ? myBlock.eastParent.name : "None";
        string wParent = myBlock.westParent != null ? myBlock.westParent.name : "None";

        // Socket status
        string nSocket = GetSocketStatus(myBlock.socketNorth);
        string sSocket = GetSocketStatus(myBlock.socketSouth);
        string eSocket = GetSocketStatus(myBlock.socketEast);
        string wSocket = GetSocketStatus(myBlock.socketWest);

        string logMessage = $"<b>REPORT FOR {this.name}:</b>\n" +
                            $"   > <b>Children:</b> [N: {nChild}] [S: {sChild}] [E: {eChild}] [W: {wChild}]\n" +
                            $"   > <b>Parents:</b>  [N: {nParent}] [S: {sParent}] [E: {eParent}] [W: {wParent}]\n" +
                            $"   > <b>Sockets:</b>  [N: {nSocket}] [S: {sSocket}] [E: {eSocket}] [W: {wSocket}]";

        Debug.Log(logMessage, this.gameObject);
    }

    private string GetSocketStatus(GameObject socketObj)
    {
        if (socketObj == null) return "NULL";
        return socketObj.activeSelf ? "ACTIVE" : "OFF";
    }
}