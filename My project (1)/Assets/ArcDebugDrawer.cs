using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class ArcDebugDrawer : MonoBehaviour
{
    public HumanClick target;     // assign in Inspector
    public float lineLength = 2f; // how long the arc lines are

    private void OnDrawGizmos()
    {
        if (target == null) return;
        if (!Application.isPlaying) return;

        DrawArcs();
    }

    private void DrawArcs()
    {
        Vector2 pos = target.transform.position;
        Vector2 forward = target.GetForwardVector();

        float fwdAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float radius = lineLength;

        // Forward arc (green)
        DrawLine(pos, fwdAngle - 40f, radius, Color.green);
        DrawLine(pos, fwdAngle + 40f, radius, Color.green);

        // Right arc (cyan)
        DrawLine(pos, fwdAngle + 40f, radius, Color.cyan);
        DrawLine(pos, fwdAngle + 140f, radius, Color.cyan);

        // Left arc (yellow)
        DrawLine(pos, fwdAngle - 40f, radius, Color.yellow);
        DrawLine(pos, fwdAngle - 140f, radius, Color.yellow);

        // Bottom arc (red)
        DrawLine(pos, fwdAngle + 140f, radius, Color.red);
        DrawLine(pos, fwdAngle - 140f, radius, Color.red);
    }

    private void DrawLine(Vector2 origin, float angleDeg, float length, Color c)
    {
        Gizmos.color = c;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Gizmos.DrawLine(origin, origin + dir * length);
    }
}
