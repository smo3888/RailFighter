using UnityEngine;

[CreateAssetMenu(fileName = "MovementStationary", menuName = "Rail Fighter/Movement/Stationary")]
public class StationaryMovementSO : MovementPatternSO
{
    public override void Tick(Enemy enemy)
    {
        // Stationary — does nothing.
    }

    public override void DrawGizmos(Enemy enemy)
    {
        // Anchored marker at spawn position. Stationary doesn't move so this
        // is essentially the same as transform.position, but using GizmoAnchor
        // keeps the visualization consistent with other movement patterns.
        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireSphere(enemy.GizmoAnchor, 0.4f);
    }
}