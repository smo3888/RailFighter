using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// One clickable region on the map editor window.
/// Place these in your _MapEditor scene. Position/size are in PIXEL coordinates
/// of the source map image (e.g. for a 2400x2400 image, valid X/Y are 0..2400).
///
/// The gizmo in Scene view always matches the rect values, so you can either:
///  - tweak the numbers in the Inspector and watch the gizmo move, OR
///  - drag the GameObject in Scene view and the gizmo (and rect) follows.
/// </summary>
[ExecuteAlways]
public class RoomMapButton : MonoBehaviour
{
    [Header("Identity")]
    public string roomName = "Room";

    [Header("Pixel Rect (matches map image)")]
    [Tooltip("Top-left X position in pixels of the source map image.")]
    public float x = 0f;
    [Tooltip("Top-left Y position in pixels of the source map image. (0 = top of image)")]
    public float y = 0f;
    public float width = 100f;
    public float height = 100f;

    [Header("Display")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.5f);

    [Header("Scene to Load")]
    [Tooltip("Drag a scene asset here. Falls back to sceneName string if empty.")]
#if UNITY_EDITOR
    public SceneAsset targetScene;
#endif
    [Tooltip("Used at runtime if no SceneAsset is assigned. Auto-synced from SceneAsset in editor.")]
    public string sceneName = "";

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep the string sceneName in sync with the dragged SceneAsset
        // so it survives at runtime where SceneAsset doesn't exist.
        if (targetScene != null)
        {
            sceneName = targetScene.name;
        }

        // Mirror the rect on the GameObject's transform so dragging in the
        // Scene view also moves the rect. We treat 1 unit = 1 pixel for simplicity.
        // The transform position represents the CENTER of the rect.
        Vector3 center = new Vector3(x + width * 0.5f, -(y + height * 0.5f), 0f);
        if (transform.localPosition != center)
        {
            transform.localPosition = center;
        }
        Vector3 size = new Vector3(Mathf.Max(1f, width), Mathf.Max(1f, height), 1f);
        if (transform.localScale != size)
        {
            transform.localScale = size;
        }
    }

    void OnDrawGizmos()
    {
        // Draw a filled translucent quad + outline around the button rect.
        // Coordinates are in "image pixel space" but mapped to world units 1:1
        // so you can see them in the Scene view.
        Vector3 topLeft = new Vector3(x, -y, 0f);
        Vector3 topRight = new Vector3(x + width, -y, 0f);
        Vector3 bottomLeft = new Vector3(x, -(y + height), 0f);
        Vector3 bottomRight = new Vector3(x + width, -(y + height), 0f);

        // Translucent fill
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.3f);
        Gizmos.DrawCube(
            new Vector3(x + width * 0.5f, -(y + height * 0.5f), 0f),
            new Vector3(width, height, 0.01f)
        );

        // Solid outline
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Label
        Handles.color = Color.white;
        Handles.Label(topLeft + new Vector3(4, -4, 0), roomName);
    }
#endif
}