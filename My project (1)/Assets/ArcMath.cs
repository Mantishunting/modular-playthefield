using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlacementResultType
{
    Insert,
    Add,
    None
}

public enum AddArc
{
    Forward,
    Left,
    Right,
    Bottom
}

public struct PlacementResult
{
    public PlacementResultType type;
    public AddArc arc;
    public float angle;
}

public struct BlockGeom
{
    public Vector2 position;
    public float radius;
    public int generation;
}

public static class ArcMath
{
    public static PlacementResult SolvePlacement(
        Vector2 blockPos,
        Vector2 parentDirection,
        float blockRadius,
        List<BlockGeom> neighbours,
        Vector2 clickPos
    )
    {
        PlacementResult result = new PlacementResult
        {
            type = PlacementResultType.None,
            arc = AddArc.Forward,
            angle = 0f
        };

        // 1. Compute click direction
        Vector2 clickDir = (clickPos - blockPos).normalized;
        if (clickDir == Vector2.zero)
            return result;

        // 2. Signed angle relative to parent-direction "forward"
        // In Unity 2D: Positive is Counter-Clockwise (Left), Negative is Clockwise (Right)
        float angle = SignedAngle(parentDirection, clickDir);
        result.angle = angle;

        // 3. Detect INSERT
        BlockGeom? aboveBlock = FindAboveBlock(blockPos, parentDirection, neighbours);

        if (aboveBlock.HasValue)
        {
            BlockGeom ab = aboveBlock.Value;
            float distToAbove = Vector2.Distance(clickPos, ab.position);

            if (distToAbove < ab.radius)
            {
                result.type = PlacementResultType.Insert;
                return result;
            }
        }

        // 4. Build arc ranges
        bool inForward = (angle > -40f && angle < 40f);

        // FIX: Swapped logic. Positive angles (40 to 140) are LEFT in Unity.
        bool inLeft = (angle >= 40f && angle <= 140f);

        // FIX: Negative angles (-140 to -40) are RIGHT in Unity.
        bool inRight = (angle <= -40f && angle >= -140f);

        bool inBottom = (!inForward && !inRight && !inLeft);

        // 5. Occlusion passes
        if (IsOccluded(blockPos, clickDir, neighbours))
        {
            result.type = PlacementResultType.None;
            return result;
        }

        // 6. Determine ADD result
        if (inForward)
        {
            result.type = PlacementResultType.Add;
            result.arc = AddArc.Forward;
            return result;
        }
        else if (inRight)
        {
            result.type = PlacementResultType.Add;
            result.arc = AddArc.Right;
            return result;
        }
        else if (inLeft)
        {
            result.type = PlacementResultType.Add;
            result.arc = AddArc.Left;
            return result;
        }
        else if (inBottom)
        {
            result.type = PlacementResultType.None;
            return result;
        }

        return result;
    }

    private static bool IsOccluded(Vector2 blockPos, Vector2 clickDir, List<BlockGeom> neighbours)
    {
        float clickAngle = Mathf.Atan2(clickDir.y, clickDir.x);

        foreach (var nb in neighbours)
        {
            Vector2 dirN = (nb.position - blockPos).normalized;
            float angleN = Mathf.Atan2(dirN.y, dirN.x);

            float dist = Vector2.Distance(blockPos, nb.position);
            if (dist < 0.001f) continue;

            float beta = Mathf.Asin(Mathf.Clamp(nb.radius / dist, -1f, 1f));

            float min = angleN - beta;
            float max = angleN + beta;

            if (AngleInRange(clickAngle, min, max))
                return true;
        }

        return false;
    }

    private static BlockGeom? FindAboveBlock(Vector2 blockPos, Vector2 forward, List<BlockGeom> neighbours)
    {
        float bestDot = 0.5f;
        BlockGeom? best = null;

        foreach (var nb in neighbours)
        {
            Vector2 dir = (nb.position - blockPos).normalized;
            float d = Vector2.Dot(forward, dir);
            if (d > bestDot)
            {
                bestDot = d;
                best = nb;
            }
        }

        return best;
    }

    public static float SignedAngle(Vector2 a, Vector2 b)
    {
        return Vector2.SignedAngle(a, b);
    }

    private static bool AngleInRange(float angle, float min, float max)
    {
        angle = Mathf.Repeat(angle + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;
        min = Mathf.Repeat(min + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;
        max = Mathf.Repeat(max + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;

        if (min <= max)
            return angle >= min && angle <= max;
        else
            return angle >= min || angle <= max;
    }
}