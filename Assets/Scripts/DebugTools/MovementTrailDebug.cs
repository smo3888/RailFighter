using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Debug visualizer that draws a trail of recent positions, a marker at the current
/// position, and a velocity arrow. Segments where the player was at/near zero velocity
/// are drawn in stoppedColor — useful for spotting unintended pauses or kinks.
/// Drop on the Player GameObject. Visible in Scene view always; visible in Game view
/// when the "Gizmos" toggle is enabled (top-right of Game window).
/// </summary>
public class MovementTrailDebug : MonoBehaviour
{
    private struct TrailSample
    {
        public Vector3 position;
        public bool stopped;
    }

    [Header("Trail")]
    [Tooltip("Maximum number of positions kept in the trail. At 60fps, 200 ≈ 3.3 seconds of history.")]
    public int trailLength = 200;
    [Tooltip("Color of the trail line when player is moving.")]
    public Color trailColor = new Color(0f, 1f, 1f, 1f); // cyan
    [Tooltip("Color of the trail line where the player was at/near zero velocity.")]
    public Color stoppedColor = new Color(1f, 0f, 0f, 1f); // red
    [Tooltip("Speed below which the player is considered 'stopped'. Set just above zero to ignore floating-point noise.")]
    public float stoppedThreshold = 0.05f;
    [Tooltip("Fade older positions toward transparent so you can see direction of travel.")]
    public bool fadeWithAge = true;

    [Header("Marker")]
    [Tooltip("Draw a wireframe sphere at the current position.")]
    public bool drawMarker = true;
    public float markerSize = 0.2f;

    [Header("Velocity Arrow")]
    [Tooltip("Draw an arrow showing instantaneous velocity. Hidden when player is stopped.")]
    public bool drawVelocity = true;
    [Tooltip("Length scale of the velocity arrow (world units per units-of-speed).")]
    public float velocityArrowScale = 0.05f;
    public Color velocityColor = Color.yellow;

    private Queue<TrailSample> trail = new Queue<TrailSample>();
    private Vector3 lastPos;
    private Vector3 instantVelocity;
    private bool hasLast = false;
    private bool currentlyStopped = false;

    void LateUpdate()
    {
        // Compute instantaneous velocity from frame-to-frame position delta.
        if (hasLast)
        {
            float dt = Time.deltaTime;
            if (dt > 0f)
                instantVelocity = (transform.position - lastPos) / dt;
        }

        currentlyStopped = hasLast && instantVelocity.magnitude < stoppedThreshold;

        // Record every frame so stops appear in the trail too (stacked samples at the same position).
        trail.Enqueue(new TrailSample
        {
            position = transform.position,
            stopped = currentlyStopped
        });
        while (trail.Count > trailLength)
            trail.Dequeue();

        lastPos = transform.position;
        hasLast = true;
    }

    void OnDrawGizmos()
    {
        // Draw trail line.
        if (trail.Count >= 2)
        {
            TrailSample[] samples = trail.ToArray();
            for (int i = 1; i < samples.Length; i++)
            {
                // Color segment red if EITHER endpoint was a stopped sample (catches transitions).
                bool segmentStopped = samples[i].stopped || samples[i - 1].stopped;
                Color baseColor = segmentStopped ? stoppedColor : trailColor;

                if (fadeWithAge)
                {
                    float alpha = (float)i / samples.Length;
                    Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
                }
                else
                {
                    Gizmos.color = baseColor;
                }
                Gizmos.DrawLine(samples[i - 1].position, samples[i].position);
            }
        }

        // Draw current-position marker (red if currently stopped).
        if (drawMarker)
        {
            Gizmos.color = currentlyStopped ? stoppedColor : trailColor;
            Gizmos.DrawWireSphere(transform.position, markerSize);
        }

        // Draw velocity arrow (hidden when stopped — no meaningful direction to show).
        if (drawVelocity && !currentlyStopped && instantVelocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = velocityColor;
            Vector3 arrowEnd = transform.position + instantVelocity * velocityArrowScale;
            Gizmos.DrawLine(transform.position, arrowEnd);

            // Simple 2D arrowhead.
            Vector3 dir = (arrowEnd - transform.position).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0) * 0.15f;
            Vector3 backOff = arrowEnd - dir * 0.25f;
            Gizmos.DrawLine(arrowEnd, backOff + perp);
            Gizmos.DrawLine(arrowEnd, backOff - perp);
        }
    }

    /// <summary>Clear the trail history. Useful for re-runs from the same start position.</summary>
    public void ClearTrail()
    {
        trail.Clear();
        hasLast = false;
    }
}