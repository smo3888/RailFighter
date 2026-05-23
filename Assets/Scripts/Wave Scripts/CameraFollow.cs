using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("Optional manual override. If empty, auto-finds the player on Start.")]
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Deadzone")]
    [Tooltip("If enabled, camera only moves when target exits the deadzone rectangle around the camera center.")]
    public bool useDeadzone = false;
    [Tooltip("Width/height of the deadzone in world units.")]
    public Vector2 deadzoneSize = new Vector2(2f, 1.5f);

    [Header("Lookahead")]
    [Tooltip("Camera offsets in the direction the player is facing (Hollow Knight style).")]
    public bool useLookahead = false;
    [Tooltip("How far the camera looks ahead horizontally / vertically when player is moving in that direction.")]
    public Vector2 lookaheadDistance = new Vector2(2f, 1f);
    [Tooltip("How quickly the lookahead offset reaches its target. Lower = slower drift.")]
    public float lookaheadSmoothSpeed = 3f;
    [Tooltip("Player must be moving faster than this (units/sec) to update facing direction.")]
    public float facingThreshold = 0.5f;

    [Header("Bounds")]
    [Tooltip("Drag the room's RoomBounds GameObject here. Leave empty for unbounded follow.")]
    public RoomBounds bounds;

    private Camera cam;
    private PlayerControllerRailFighter playerCtrl;  // cached for reading useLegacyMovement

    // Lookahead state
    private Vector2 facingDir = Vector2.right;
    private Vector2 currentLookahead = Vector2.zero;
    private Vector3 lastTargetPos;
    private bool hasLastTarget = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Auto-find the player if no target was assigned in the inspector
        if (target == null)
        {
            PlayerControllerRailFighter player = FindObjectOfType<PlayerControllerRailFighter>();
            if (player != null)
            {
                target = player.transform;
                playerCtrl = player;
            }
            else
                Debug.LogWarning("CameraFollow: no PlayerControllerRailFighter found in scene.");
        }
        else
        {
            // Target was assigned manually; still try to find the controller for legacy-mode sync.
            playerCtrl = target.GetComponent<PlayerControllerRailFighter>();
        }

        // Auto-find bounds in scene if not assigned
        if (bounds == null)
            bounds = FindObjectOfType<RoomBounds>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ── Legacy Mode (A/B Comparison) ─────────────────────────────────────
        // When the player has useLegacyMovement enabled, the camera disables all
        // recent polish: no lookahead, no deadzone, no position smoothing. Just
        // hard-follow with offset, plus room bounds clamping (still wanted for
        // gameplay reasons regardless of mode).
        bool legacy = playerCtrl != null && playerCtrl.useLegacyMovement;
        if (legacy)
        {
            currentLookahead = Vector2.zero;
            Vector3 legacyDesired = target.position + offset;

            // Room bounds still clamp in legacy — these aren't "polish," they're
            // a gameplay constraint to prevent seeing outside the room.
            if (bounds != null && cam != null)
            {
                float camHalfHeight = cam.orthographicSize;
                float camHalfWidth = camHalfHeight * cam.aspect;
                float minX = bounds.min.x + camHalfWidth;
                float maxX = bounds.max.x - camHalfWidth;
                float minY = bounds.min.y + camHalfHeight;
                float maxY = bounds.max.y - camHalfHeight;
                if (minX > maxX) legacyDesired.x = (bounds.min.x + bounds.max.x) * 0.5f;
                else legacyDesired.x = Mathf.Clamp(legacyDesired.x, minX, maxX);
                if (minY > maxY) legacyDesired.y = (bounds.min.y + bounds.max.y) * 0.5f;
                else legacyDesired.y = Mathf.Clamp(legacyDesired.y, minY, maxY);
            }

            transform.position = legacyDesired;
            return;
        }

        // ── Lookahead ────────────────────────────────────────────────────────
        if (useLookahead)
        {
            if (hasLastTarget)
            {
                Vector3 vel = (target.position - lastTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f);

                // Update facing only when motion is meaningful. When player is still,
                // facing direction is held from last movement (no snap-back).
                if (vel.sqrMagnitude > facingThreshold * facingThreshold)
                {
                    // Lock facing to the dominant axis of motion (single-axis on a rail).
                    if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
                        facingDir = new Vector2(Mathf.Sign(vel.x), 0f);
                    else
                        facingDir = new Vector2(0f, Mathf.Sign(vel.y));
                }
            }
            lastTargetPos = target.position;
            hasLastTarget = true;

            Vector2 targetLookahead = new Vector2(
                facingDir.x * lookaheadDistance.x,
                facingDir.y * lookaheadDistance.y
            );
            currentLookahead = Vector2.Lerp(currentLookahead, targetLookahead, lookaheadSmoothSpeed * Time.deltaTime);
        }
        else
        {
            currentLookahead = Vector2.Lerp(currentLookahead, Vector2.zero, lookaheadSmoothSpeed * Time.deltaTime);
        }

        Vector3 fullDesired = target.position + offset + (Vector3)currentLookahead;
        Vector3 desiredPosition = fullDesired;

        // ── Deadzone ─────────────────────────────────────────────────────────
        if (useDeadzone)
        {
            float halfDzX = deadzoneSize.x * 0.5f;
            float halfDzY = deadzoneSize.y * 0.5f;

            float dx = fullDesired.x - transform.position.x;
            float dy = fullDesired.y - transform.position.y;

            desiredPosition.x = Mathf.Abs(dx) > halfDzX
                ? fullDesired.x - Mathf.Sign(dx) * halfDzX
                : transform.position.x;

            desiredPosition.y = Mathf.Abs(dy) > halfDzY
                ? fullDesired.y - Mathf.Sign(dy) * halfDzY
                : transform.position.y;
        }

        // ── Room bounds clamp ────────────────────────────────────────────────
        if (bounds != null && cam != null)
        {
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = camHalfHeight * cam.aspect;

            float minX = bounds.min.x + camHalfWidth;
            float maxX = bounds.max.x - camHalfWidth;
            float minY = bounds.min.y + camHalfHeight;
            float maxY = bounds.max.y - camHalfHeight;

            if (minX > maxX) desiredPosition.x = (bounds.min.x + bounds.max.x) * 0.5f;
            else desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

            if (minY > maxY) desiredPosition.y = (bounds.min.y + bounds.max.y) * 0.5f;
            else desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}