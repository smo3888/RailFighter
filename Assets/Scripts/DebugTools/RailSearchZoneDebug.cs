using UnityEngine;

/// <summary>
/// Visualizes the search zones for this rail — the regions in which it can find target
/// rails in each direction. Drawn only when this rail is selected in the editor.
///
/// Tolerance values are read live from PlayerControllerRailFighter on every draw, so any
/// change you make on the player controller's inspector reflects here immediately.
/// Falls back to defaults if no PlayerControllerRailFighter exists in the scene.
///
/// Attach to a rail GameObject (or to your rail prefab so all rails get it).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class RailSearchZoneDebug : MonoBehaviour
{
    [Header("Display")]
    public Color upColor = new Color(0.4f, 1f, 0.4f, 0.18f);
    public Color downColor = new Color(1f, 0.4f, 0.4f, 0.18f);
    public Color leftColor = new Color(1f, 1f, 0.4f, 0.18f);
    public Color rightColor = new Color(0.4f, 0.7f, 1f, 0.18f);
    [Tooltip("Draw filled rectangles. If off, only outlines are shown.")]
    public bool drawFill = true;

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

    void OnDrawGizmosSelected()
    {
        SyncFromController();

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        bool isVert = IsVerticalRail();

        if (isVert)
        {
            DrawZone(
                new Vector2(transform.position.x - verticalHorizontalTolerance, b.max.y),
                new Vector2(transform.position.x + verticalHorizontalTolerance, b.max.y + verticalMaxDistance),
                upColor
            );
            DrawZone(
                new Vector2(transform.position.x - verticalHorizontalTolerance, b.min.y - verticalMaxDistance),
                new Vector2(transform.position.x + verticalHorizontalTolerance, b.min.y),
                downColor
            );
            DrawZone(
                new Vector2(transform.position.x - horizontalMaxDistance, b.min.y - horizontalVerticalTolerance),
                new Vector2(transform.position.x, b.max.y + horizontalVerticalTolerance),
                leftColor
            );
            DrawZone(
                new Vector2(transform.position.x, b.min.y - horizontalVerticalTolerance),
                new Vector2(transform.position.x + horizontalMaxDistance, b.max.y + horizontalVerticalTolerance),
                rightColor
            );
        }
        else
        {
            DrawZone(
                new Vector2(b.min.x - verticalHorizontalTolerance, transform.position.y),
                new Vector2(b.max.x + verticalHorizontalTolerance, transform.position.y + verticalMaxDistance),
                upColor
            );
            DrawZone(
                new Vector2(b.min.x - verticalHorizontalTolerance, transform.position.y - verticalMaxDistance),
                new Vector2(b.max.x + verticalHorizontalTolerance, transform.position.y),
                downColor
            );
            DrawZone(
                new Vector2(b.min.x - horizontalMaxDistance, transform.position.y - horizontalVerticalTolerance),
                new Vector2(b.min.x, transform.position.y + horizontalVerticalTolerance),
                leftColor
            );
            DrawZone(
                new Vector2(b.max.x, transform.position.y - horizontalVerticalTolerance),
                new Vector2(b.max.x + horizontalMaxDistance, transform.position.y + horizontalVerticalTolerance),
                rightColor
            );
        }
    }

    void DrawZone(Vector2 min, Vector2 max, Color color)
    {
        Vector3 bottomLeft = new Vector3(min.x, min.y, 0);
        Vector3 bottomRight = new Vector3(max.x, min.y, 0);
        Vector3 topRight = new Vector3(max.x, max.y, 0);
        Vector3 topLeft = new Vector3(min.x, max.y, 0);

        if (drawFill)
        {
            Gizmos.color = color;
            Vector3 center = (bottomLeft + topRight) * 0.5f;
            Vector3 size = new Vector3(max.x - min.x, max.y - min.y, 0.001f);
            Gizmos.DrawCube(center, size);
        }

        Gizmos.color = new Color(color.r, color.g, color.b, 1f);
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    bool IsVerticalRail()
    {
        if (CompareTag(VERTICAL_RAIL_TAG)) return true;
        if (CompareTag(HORIZONTAL_RAIL_TAG)) return false;
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) return col.bounds.size.y > col.bounds.size.x;
        return false;
    }
}