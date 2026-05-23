using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Visualizes rail relationships from a source position (defaults to the player).
/// For each of the four jump directions, draws a colored line to the rail the
/// algorithm would select. Replicates FindNearestRailInDirection logic so the
/// viz reflects exactly what the controller would do.
///
/// Tolerance values are read live from PlayerControllerRailFighter on every draw,
/// so any change you make on the player controller's inspector reflects here
/// immediately. Falls back to defaults if no PlayerControllerRailFighter exists.
///
/// Drop on any GameObject in the scene. Visible in Scene view always; visible
/// in Game view when "Gizmos" is enabled in the Game window.
/// </summary>
public class RailRelationshipDebug : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("If empty, auto-finds the player. Lines draw from this position.")]
    public Transform sourceOverride;

    [Header("Direction Colors")]
    public Color upColor = new Color(0.4f, 1f, 0.4f);     // green
    public Color downColor = new Color(1f, 0.4f, 0.4f);   // red
    public Color leftColor = new Color(1f, 1f, 0.4f);     // yellow
    public Color rightColor = new Color(0.4f, 0.7f, 1f);  // blue

    [Header("Display")]
    [Tooltip("Highlight the source's current rail.")]
    public bool highlightCurrentRail = true;
    public Color currentRailColor = new Color(1f, 1f, 1f, 0.4f);
    [Tooltip("Show all rails that pass the direction filter, not just the winner.")]
    public bool showAllCandidates = false;
    public Color candidateColor = new Color(1f, 1f, 1f, 0.12f);
    [Tooltip("Sphere size at rail anchor points.")]
    public float anchorSize = 0.18f;

    private const string HORIZONTAL_RAIL_TAG = "RailHorizontal";
    private const string VERTICAL_RAIL_TAG = "RailVertical";

    // Synced from PlayerControllerRailFighter on every draw.
    private float verticalHorizontalTolerance = 4f;
    private float horizontalVerticalTolerance = 2f;
    private float verticalMaxDistance = 10f;
    private float horizontalMaxDistance = 10f;

    private PlayerControllerRailFighter cachedController;

    void SyncFromController()
    {
        if (cachedController == null)
            cachedController = FindObjectOfType<PlayerControllerRailFighter>();
        if (cachedController == null) return;

        verticalHorizontalTolerance = cachedController.verticalHorizontalTolerance;
        horizontalVerticalTolerance = cachedController.horizontalVerticalTolerance;
        verticalMaxDistance = cachedController.verticalMaxDistance;
        horizontalMaxDistance = cachedController.horizontalMaxDistance;
    }

    void OnDrawGizmos()
    {
        SyncFromController();

        Vector3 from = GetSourcePosition();
        Transform currentRail = FindCurrentRail(from);

        if (highlightCurrentRail && currentRail != null)
        {
            Gizmos.color = currentRailColor;
            DrawRailSegment(currentRail);
        }

        DrawNeighbor(from, currentRail, Vector2.up, upColor);
        DrawNeighbor(from, currentRail, Vector2.down, downColor);
        DrawNeighbor(from, currentRail, Vector2.left, leftColor);
        DrawNeighbor(from, currentRail, Vector2.right, rightColor);
    }

    Vector3 GetSourcePosition()
    {
        if (sourceOverride != null) return sourceOverride.position;
        if (cachedController != null) return cachedController.transform.position;
        return transform.position;
    }

    void DrawNeighbor(Vector3 from, Transform currentRail, Vector2 direction, Color color)
    {
        if (showAllCandidates)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, candidateColor.a);
            foreach (Transform candidate in FindAllCandidatesInDirection(from, currentRail, direction))
            {
                Vector3 anchor = NearestPointOnRail(from, candidate, IsVerticalRail(candidate));
                Gizmos.DrawLine(from, anchor);
            }
        }

        Transform winner = FindNearestInDirection(from, currentRail, direction);
        if (winner == null) return;

        Vector3 nearest = NearestPointOnRail(from, winner, IsVerticalRail(winner));
        Gizmos.color = color;
        Gizmos.DrawLine(from, nearest);
        Gizmos.DrawWireSphere(nearest, anchorSize);
    }

    // ── Mirrors of PlayerControllerRailFighter logic ─────────────────────────

    Transform FindCurrentRail(Vector3 from)
    {
        Transform nearest = null;
        float nearestDist = Mathf.Infinity;
        string[] tags = { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };

        foreach (string tag in tags)
        {
            foreach (GameObject r in GameObject.FindGameObjectsWithTag(tag))
            {
                bool isVert = IsVerticalRail(r.transform);
                float dist = DistanceToRailEdge(from, r.transform, isVert);
                if (dist < nearestDist) { nearestDist = dist; nearest = r.transform; }
            }
        }
        return nearest;
    }

    Transform FindNearestInDirection(Vector3 fromPos, Transform currentRail, Vector2 direction)
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;
        string[] tags = { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };

        foreach (string tag in tags)
        {
            foreach (GameObject railObj in GameObject.FindGameObjectsWithTag(tag))
            {
                Transform rail = railObj.transform;
                if (rail == currentRail) continue;
                if (!PassesDirectionFilter(fromPos, rail, direction)) continue;

                float dist = DistanceToRailEdge(fromPos, rail, IsVerticalRail(rail));
                if (dist < bestDist) { bestDist = dist; best = rail; }
            }
        }
        return best;
    }

    List<Transform> FindAllCandidatesInDirection(Vector3 fromPos, Transform currentRail, Vector2 direction)
    {
        List<Transform> result = new List<Transform>();
        string[] tags = { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };

        foreach (string tag in tags)
        {
            foreach (GameObject railObj in GameObject.FindGameObjectsWithTag(tag))
            {
                Transform rail = railObj.transform;
                if (rail == currentRail) continue;
                if (PassesDirectionFilter(fromPos, rail, direction))
                    result.Add(rail);
            }
        }
        return result;
    }

    bool PassesDirectionFilter(Vector3 fromPos, Transform rail, Vector2 direction)
    {
        bool railIsVert = IsVerticalRail(rail);
        Vector3 nearestPt = NearestPointOnRail(fromPos, rail, railIsVert);
        float nearDx = nearestPt.x - fromPos.x;
        float nearDy = nearestPt.y - fromPos.y;

        const float dirEps = 0.01f;
        if (direction == Vector2.up && nearDy <= dirEps) return false;
        if (direction == Vector2.down && nearDy >= -dirEps) return false;
        if (direction == Vector2.left && nearDx >= -dirEps) return false;
        if (direction == Vector2.right && nearDx <= dirEps) return false;

        if (direction == Vector2.up || direction == Vector2.down)
        {
            if (Mathf.Abs(nearDy) > verticalMaxDistance) return false;

            if (railIsVert)
            {
                if (Mathf.Abs(nearDx) > verticalHorizontalTolerance) return false;
            }
            else
            {
                BoxCollider2D railCol = rail.GetComponent<BoxCollider2D>();
                if (railCol != null)
                {
                    float jumpTolerance = 1.5f;
                    float railLeft = rail.position.x - (railCol.size.x * rail.localScale.x / 2f) - jumpTolerance;
                    float railRight = rail.position.x + (railCol.size.x * rail.localScale.x / 2f) + jumpTolerance;
                    if (fromPos.x < railLeft || fromPos.x > railRight) return false;
                }
            }
        }
        else
        {
            if (Mathf.Abs(nearDy) > horizontalVerticalTolerance) return false;
            if (Mathf.Abs(nearDx) > horizontalMaxDistance) return false;
        }

        return true;
    }

    bool IsVerticalRail(Transform rail)
    {
        if (rail.CompareTag(VERTICAL_RAIL_TAG)) return true;
        if (rail.CompareTag(HORIZONTAL_RAIL_TAG)) return false;
        BoxCollider2D col = rail.GetComponent<BoxCollider2D>();
        if (col != null) return col.bounds.size.y > col.bounds.size.x;
        return false;
    }

    Vector3 NearestPointOnRail(Vector3 from, Transform rail, bool railIsVert)
    {
        BoxCollider2D col = rail.GetComponent<BoxCollider2D>();
        if (col == null) return rail.position;

        if (railIsVert)
        {
            float clampedY = Mathf.Clamp(from.y, col.bounds.min.y, col.bounds.max.y);
            return new Vector3(rail.position.x, clampedY, 0);
        }
        else
        {
            float clampedX = Mathf.Clamp(from.x, col.bounds.min.x, col.bounds.max.x);
            return new Vector3(clampedX, rail.position.y, 0);
        }
    }

    float DistanceToRailEdge(Vector3 from, Transform rail, bool railIsVert)
    {
        Vector3 nearest = NearestPointOnRail(from, rail, railIsVert);
        return Vector2.Distance(from, nearest);
    }

    void DrawRailSegment(Transform rail)
    {
        BoxCollider2D col = rail.GetComponent<BoxCollider2D>();
        if (col == null) { Gizmos.DrawSphere(rail.position, 0.2f); return; }

        bool isVert = IsVerticalRail(rail);
        if (isVert)
        {
            Gizmos.DrawLine(
                new Vector3(rail.position.x, col.bounds.min.y, 0),
                new Vector3(rail.position.x, col.bounds.max.y, 0)
            );
        }
        else
        {
            Gizmos.DrawLine(
                new Vector3(col.bounds.min.x, rail.position.y, 0),
                new Vector3(col.bounds.max.x, rail.position.y, 0)
            );
        }
    }
}