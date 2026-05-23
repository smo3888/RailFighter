using UnityEngine;

public class RoomBounds : MonoBehaviour
{
    [Header("Bounds")]
    public Vector2 min = new Vector2(-10f, -5f);
    public Vector2 max = new Vector2(10f, 5f);

    void OnDrawGizmos()
    {
        // Draws the bounds in the Scene view as a yellow rectangle so you can see what you're editing
        Gizmos.color = Color.yellow;
        Vector3 bl = new Vector3(min.x, min.y, 0);
        Vector3 br = new Vector3(max.x, min.y, 0);
        Vector3 tl = new Vector3(min.x, max.y, 0);
        Vector3 tr = new Vector3(max.x, max.y, 0);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}