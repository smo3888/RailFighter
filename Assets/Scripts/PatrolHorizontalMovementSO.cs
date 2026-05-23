using UnityEngine;

[CreateAssetMenu(fileName = "MovementPatrolHorizontal", menuName = "Rail Fighter/Movement/Patrol Horizontal")]
public class PatrolHorizontalMovementSO : MovementPatternSO
{
    [Header("Patrol")]
    [Tooltip("Maximum distance the enemy moves left/right of its spawn position")]
    public float patrolRange = 3f;

    [Tooltip("Patrol cycles per second. 0.5 means one full back-and-forth every 2 seconds.")]
    public float frequency = 0.5f;

    public override void Tick(Enemy enemy)
    {
        float t = ComputePhase(enemy);

        Vector3 pos = enemy.spawnPosition;
        pos.x += Mathf.Sin(t) * patrolRange;
        enemy.transform.position = pos;
    }

    public override FacingIntent GetFacingIntent(Enemy enemy)
    {
        float t = ComputePhase(enemy);
        float velocity = Mathf.Cos(t);

        if (Mathf.Abs(velocity) < 0.01f) return FacingIntent.None;
        return velocity > 0f ? FacingIntent.Right : FacingIntent.Left;
    }

    float ComputePhase(Enemy enemy)
    {
        float phaseOffset = enemy.GetInstanceID() * 0.137f;
        return (Time.time + phaseOffset) * frequency * Mathf.PI * 2f;
    }

    public override void DrawGizmos(Enemy enemy)
    {
        // Patrol bounds anchored at spawn — patrol area is a fixed reference frame
        Vector3 anchor = enemy.GizmoAnchor;

        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.8f);
        Vector3 left = anchor + Vector3.left * patrolRange;
        Vector3 right = anchor + Vector3.right * patrolRange;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.2f);
        Gizmos.DrawWireSphere(right, 0.2f);
    }
}