using UnityEngine;
using System.Collections;

public class PlayerControllerRailFighter : MonoBehaviour, IDamageable
{
    public float moveSpeed = 8f;
    [Tooltip("Move speed used when useLegacyMovement is enabled. Represents the older, slower baseline speed before tuning.")]
    public float legacyMoveSpeed = 8f;

    [Header("Movement Smoothing")]
    [Tooltip("Seconds to ramp from 0 to full input. Higher = floatier.")]
    public float accelTime = 0.15f;
    [Tooltip("Seconds to ramp back to 0 when releasing input.")]
    public float decelTime = 0.08f;
    [Tooltip("Seconds to flip from full one direction to full the other. Lower = snappier turns.")]
    public float reverseTime = 0.06f;

    private float smoothedInputH = 0f;
    private float smoothedInputV = 0f;

    [Header("Rail Jump - Search Tolerances")]
    public float verticalHorizontalTolerance = 4f;
    public float horizontalVerticalTolerance = 2f;
    public float verticalMaxDistance = 10f;
    public float horizontalMaxDistance = 10f;

    [Header("Rail Jump - Edge Detection")]
    public float edgeCancelBuffer = 0.5f;

    [Header("Comparison Mode (A/B Testing)")]
    [Tooltip("DEBUG: When enabled, disables all recent movement polish — auto-chain, landing momentum, jump velocity carry, and cross-axis dash. Use this to compare 'before vs after' feel, or for the before/after dev video. Toggle live in play mode to feel the difference instantly.")]
    public bool useLegacyMovement = false;

    [Header("Rail Jump - Feel")]
    [Tooltip("Time to traverse from one rail to another. Higher = weightier jumps.")]
    public float jumpDuration = 0.15f;
    [Tooltip("Momentum preserved on landing. 0 = hard stop, 1 = full speed continuation.")]
    [Range(0f, 1f)]
    public float landingMomentum = 0.5f;
    [Tooltip("Momentum applied to the NEW axis on cross-orientation jumps (H→V or V→H). Higher than landingMomentum gives a 'dash' feeling on cross-axis transitions, helping bridge the moment where the player's hand hasn't yet remapped to the new axis. Scales with pre-jump speed, so slow approaches still feel light.")]
    [Range(0f, 2f)]
    public float landingMomentumCrossAxis = 0.8f;
    [Tooltip("How much pre-jump velocity carries through the jump arc. 0 = stiff (player stops to jump), 1 = smooth (player keeps moving forward through the jump).")]
    [Range(0f, 1f)]
    public float jumpCarryStrength = 1f;

    [Header("Rail Jump - Auto-Chain")]
    [Tooltip("When enabled, holding a direction key auto-fires rail jumps when conditions are met (at edge for parallel, on landing for perpendicular). Initial tap is still required to start; release stops chaining.")]
    public bool autoChainJumps = true;
    [Tooltip("Minimum time after landing before an auto-chain jump can re-fire. Preserves the weight of landing instead of machine-gunning rail jumps. ~0.08s feels good.")]
    public float autoChainCooldown = 0.08f;

    [Header("Magnet Flight (Power-Up)")]
    [Tooltip("When equipped, perpendicular presses on a rail enter zero-gravity float mode (between-two-rails required).")]
    public bool magnetFlightActive = false;
    [Tooltip("Gravity strength when float is on the VERTICAL axis (entered from H rail, W/S toggle).")]
    public float floatGravity = 30f;
    [Tooltip("Gravity strength when float is on the HORIZONTAL axis (entered from V rail, A/D toggle). Tune separately to give horizontal pulls a stronger grip if needed.")]
    public float floatGravityHorizontal = 45f;
    [Tooltip("Maximum velocity in float mode (per-axis clamp). Both X and Y velocities clamp to this.")]
    public float floatMaxSpeed = 12f;
    [Tooltip("Direct velocity on the non-gravity (lateral) axis. Slower than normal rail movement — keeps positioning solid without zero-grav fight.")]
    public float floatLateralSpeed = 6f;
    [Tooltip("Distance to a rail at which the player snaps onto it and exits float mode.")]
    public float floatLandingRadius = 0.5f;
    [Tooltip("Initial velocity given to the player when entering float mode, in the press direction. Gives a magnetic 'snap toward rail' feel instead of starting from zero.")]
    public float floatEntryImpulse = 10f;
    [Tooltip("Max distance at which a rail can be a magnet anchor. Larger than normal jump search — magnet rails reach further.")]
    public float magnetSearchRange = 20f;
    [Tooltip("Magnitude of random sprite jitter while floating. World units. 0 disables.")]
    public float floatJitterAmount = 0.05f;
    [Tooltip("Magnitude of jitter applied to the rail anchor visuals (the contact points). World units. 0 disables.")]
    public float anchorJitterAmount = 0.04f;
    [Tooltip("Size of the placeholder anchor visual squares, in world units.")]
    public float anchorVisualSize = 0.4f;
    [Tooltip("Sorting layer name for anchor visuals. Leave empty for Default.")]
    public string anchorSortingLayer = "Gameplay";
    [Tooltip("Sorting order within the anchor's sorting layer.")]
    public int anchorSortingOrder = 10;
    [Tooltip("Optional sprite child to apply jitter to. If empty, jitter is applied to a SpriteRenderer found in children.")]
    public Transform jitterTarget;

    public GameObject GameOver;

    [Header("Spawn")]
    [Tooltip("Drag an empty GameObject here to set the default spawn point in the dungeon.")]
    public Transform defaultSpawnPoint;

    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;
    public float invincibilityTime = 1.5f;
    private float lastHitTime = -999f;

    [Header("QTE Mode (Boss Cutscenes)")]
    public bool qteMode = false;

    private bool isSlowed = false;
    private float slowTimer = 0f;
    private float normalMoveSpeed;
    private Color normalColor;
    private SpriteRenderer playerSpriteRenderer;

    // Rail state
    private Transform currentRail;
    private float railMin;
    private float railMax;
    private bool isOnVerticalRail = false;
    private bool isJumping = false;
    private Coroutine activeJumpCoroutine;

    // Timestamp of the most recent landing. Auto-chain skips re-firing for autoChainCooldown
    // seconds after landing to preserve the weight of contact before the next launch.
    private float lastLandingTime = -999f;

    // Magnet flight state (zero-gravity float mode)
    private bool isFloating = false;
    private Vector2 floatVelocity = Vector2.zero;
    private bool floatVerticalAxis = true;   // true: gravity is on Y (W/S toggle), lateral is X. False: rotated 90°.
    private int floatGravitySign = 0;        // -1 or +1 along the gravity axis.
    private Transform floatSourceRail;       // The rail we entered float from. Excluded from landing until we've moved away.
    private Transform floatTargetRail;       // The rail in the press direction (the second anchor). Defines vicinity.
    private bool floatHasLeftSource;         // True once we've moved clear of the source rail's landing range.
    private float floatVicinityMinX, floatVicinityMaxX, floatVicinityMinY, floatVicinityMaxY;
    private Vector3 jitterRestPos;           // Original local position of jitter target — restored on float exit.
    private GameObject sourceAnchorObj;      // Tracking object that follows the player's anchor point on the source rail.
    private GameObject targetAnchorObj;      // Tracking object that follows the player's anchor point on the target rail.
    private Transform sourceAnchorVisual;    // Child of source anchor — jitters independently of tracking.
    private Transform targetAnchorVisual;    // Child of target anchor — jitters independently of tracking.
    private Animator animator;

    private const string HORIZONTAL_RAIL_TAG = "RailHorizontal";
    private const string VERTICAL_RAIL_TAG = "RailVertical";

    // ── Power-Up State ───────────────────────────────────────────────────────
    private bool isShielded = false;
    private bool isInvincible = false;

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        currentHealth = maxHealth;

        // Optional: Animator on player or sprite child for magnet flight float animation.
        animator = GetComponentInChildren<Animator>();

        // Auto-resolve jitter target: prefer a SpriteRenderer on a CHILD object.
        // Never the player root itself — writing to its localPosition would fight
        // with float-mode position updates and freeze the player in place.
        if (jitterTarget == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in srs)
            {
                if (sr.transform != transform)
                {
                    jitterTarget = sr.transform;
                    break;
                }
            }
        }

        bool hasReturnData = false;
        if (GameManager.Instance != null)
        {
            string railName = GameManager.Instance.Data.dungeonReturnRailName;
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            hasReturnData = !string.IsNullOrEmpty(railName) && currentScene == GameManager.Instance.dungeonScene;
        }

        if (hasReturnData)
        {
            float x = GameManager.Instance.Data.dungeonReturnX;
            float y = GameManager.Instance.Data.dungeonReturnY;
            string railName = GameManager.Instance.Data.dungeonReturnRailName;

            Transform nearest = FindNearestRailOfAnyType();
            if (nearest != null)
            {
                currentRail = nearest;
                isOnVerticalRail = IsVerticalRail(currentRail);
                UpdateRailBounds();
            }

            Debug.Log($"Applying dungeon return — rail: {railName}, pos: {x}, {y}");
            StartCoroutine(ApplyReturnPosition(x, y, railName));
        }
        else
        {
            if (defaultSpawnPoint != null)
                transform.position = new Vector3(defaultSpawnPoint.position.x, defaultSpawnPoint.position.y, 0);

            Transform nearest = FindNearestRailOfAnyType();
            if (nearest != null)
            {
                currentRail = nearest;
                isOnVerticalRail = IsVerticalRail(currentRail);
                UpdateRailBounds();
                if (isOnVerticalRail)
                    transform.position = new Vector3(currentRail.position.x, transform.position.y, 0);
                else
                    transform.position = new Vector3(transform.position.x, currentRail.position.y, 0);
            }
        }

        normalMoveSpeed = moveSpeed;
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer)
            normalColor = playerSpriteRenderer.color;
    }

    IEnumerator ApplyReturnPosition(float x, float y, string railName)
    {
        yield return null;

        GameObject railObj = GameObject.Find(railName);

        if (railObj != null)
        {
            currentRail = railObj.transform;
            isOnVerticalRail = IsVerticalRail(currentRail);
            UpdateRailBounds();

            if (isOnVerticalRail)
                transform.position = new Vector3(currentRail.position.x, Mathf.Clamp(y, railMin, railMax), 0);
            else
                transform.position = new Vector3(Mathf.Clamp(x, railMin, railMax), currentRail.position.y, 0);

            Debug.Log($"Return position applied. Rail: {railName}, final pos: {transform.position}");
        }
        else
        {
            Debug.LogWarning($"Rail '{railName}' not found — falling back to nearest rail.");
            Transform fallback = FindNearestRailOfAnyType();
            if (fallback != null)
            {
                currentRail = fallback;
                isOnVerticalRail = IsVerticalRail(currentRail);
                UpdateRailBounds();
                if (isOnVerticalRail)
                    transform.position = new Vector3(currentRail.position.x, Mathf.Clamp(y, railMin, railMax), 0);
                else
                    transform.position = new Vector3(Mathf.Clamp(x, railMin, railMax), currentRail.position.y, 0);
            }
        }

        if (Camera.main != null)
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);

        GameManager.Instance.Data.dungeonReturnX = 0f;
        GameManager.Instance.Data.dungeonReturnY = 0f;
        GameManager.Instance.Data.dungeonReturnRailName = "";
    }

    public string GetCurrentRailName()
    {
        return currentRail != null ? currentRail.name : "";
    }

    void Update()
    {
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
                RemoveSlowDebuff();
        }

        // Magnet flight: float mode replaces normal rail movement and rail-jump input.
        if (isFloating)
        {
            UpdateFloat();
            // Shooting still works during float — fall through to shoot logic below.
        }
        else
        {
            if (!qteMode && !isJumping)
            {
                ApplyRailMovement();
            }

            if (!isJumping)
                HandleRailJumpInput();
        }

        // Combat input handled by PlayerCombat component (to be added)
    }

    // ── Movement Smoothing ───────────────────────────────────────────────────
    float SmoothInput(float current, float target)
    {
        // Legacy mode: instant input response, no acceleration/deceleration/reversal smoothing.
        if (useLegacyMovement) return target;

        float duration;

        // Reversing: current and target have opposite signs (and target isn't zero).
        // Snaps through zero in one motion using its own dedicated time.
        bool reversing = (current * target < 0f);

        if (reversing)
        {
            duration = reverseTime;
        }
        else
        {
            // Same sign (or one is zero) — accel if growing magnitude, decel if shrinking.
            bool accelerating = Mathf.Abs(target) > Mathf.Abs(current);
            duration = accelerating ? accelTime : decelTime;
        }

        if (duration <= 0f) return target;
        return Mathf.MoveTowards(current, target, (1f / duration) * Time.deltaTime);
    }

    // Single step of rail movement. Called from Update and once on landing
    // (to bridge the one-frame gap between coroutine ending and next Update).
    void ApplyRailMovement()
    {
        if (isOnVerticalRail)
        {
            float vertical = Input.GetAxisRaw("Vertical");
            if (MobileControls.topPressed) vertical = 1;
            if (MobileControls.bottomPressed) vertical = -1;

            float effSpeed = useLegacyMovement ? legacyMoveSpeed : moveSpeed;
            smoothedInputV = SmoothInput(smoothedInputV, vertical);
            float newY = transform.position.y + (smoothedInputV * effSpeed * Time.deltaTime);
            newY = Mathf.Clamp(newY, railMin, railMax);
            transform.position = new Vector3(currentRail.position.x, newY, 0);
        }
        else
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            if (MobileControls.leftPressed) horizontal = -1;
            if (MobileControls.rightPressed) horizontal = 1;

            float effSpeed = useLegacyMovement ? legacyMoveSpeed : moveSpeed;
            smoothedInputH = SmoothInput(smoothedInputH, horizontal);
            float newX = transform.position.x + (smoothedInputH * effSpeed * Time.deltaTime);
            newX = Mathf.Clamp(newX, railMin, railMax);
            transform.position = new Vector3(newX, currentRail.position.y, 0);
        }
    }

    // ── Rail Type Detection ──────────────────────────────────────────────────
    bool IsVerticalRail(Transform rail)
    {
        if (rail.CompareTag(VERTICAL_RAIL_TAG)) return true;
        if (rail.CompareTag(HORIZONTAL_RAIL_TAG)) return false;

        BoxCollider2D col = rail.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            float width = col.bounds.size.x;
            float height = col.bounds.size.y;
            return height > width;
        }
        return false;
    }

    // ── Rail Jump System (Single-Press) ──────────────────────────────────────
    void HandleRailJumpInput()
    {
        // Pure hold-to-chain model: initial KeyDown fires immediately, subsequent held frames
        // re-fire HandleDirectionPress as long as conditions are met (cooldown elapsed, target
        // found, edge requirements met for parallel jumps). No engagement tracking — simpler
        // and avoids losing chain state if the player presses a key during a jump animation
        // (where this method doesn't run).
        // Magnet flight entry is unaffected — only the initial tap fires it inside HandleDirectionPress.

        ProcessDirectionInput(
            Vector2.up,
            Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || MobileControls.swipedUp,
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow));

        ProcessDirectionInput(
            Vector2.down,
            Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || MobileControls.swipedDown,
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));

        ProcessDirectionInput(
            Vector2.left,
            Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || MobileControls.doubleTapLeft,
            Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow));

        ProcessDirectionInput(
            Vector2.right,
            Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || MobileControls.doubleTapRight,
            Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow));
    }

    void ProcessDirectionInput(Vector2 direction, bool justTapped, bool isHeld)
    {
        if (justTapped)
        {
            // Initial tap: fire immediately. isInitialTap=true allows magnet flight entry.
            HandleDirectionPress(direction, isInitialTap: true);
        }
        else if (isHeld && autoChainJumps && !useLegacyMovement && Time.time - lastLandingTime >= autoChainCooldown)
        {
            // Held key, chain enabled, past landing cooldown: retry the press. HandleDirectionPress
            // enforces edge requirements for parallel jumps and fails silently if no target found,
            // so this only produces a jump when it's actually valid to do so.
            HandleDirectionPress(direction, isInitialTap: false);
        }
    }

    void HandleDirectionPress(Vector2 direction, bool isInitialTap = true)
    {
        // ── Magnet flight entry (zero-gravity float) ─────────────────────────
        // Perpendicular press from a rail with a SAME-ORIENTATION rail in that direction
        // (the "between two parallel rails" requirement). Source + target form the vicinity.
        // Magnet flight only triggers on the *initial* tap so auto-chained held inputs don't
        // accidentally drop the player into float mode mid-chain.
        if (magnetFlightActive && isInitialTap)
        {
            bool perpendicular = (isOnVerticalRail && (direction == Vector2.left || direction == Vector2.right))
                              || (!isOnVerticalRail && (direction == Vector2.up || direction == Vector2.down));
            if (perpendicular)
            {
                Transform anchor = FindMagnetAnchorInDirection(direction);
                if (anchor != null && IsVerticalRail(anchor) == isOnVerticalRail)
                {
                    // Gravity axis = the press axis. Source rail orientation determines this:
                    // perpendicular press from H rail → vertical gravity; from V rail → horizontal gravity.
                    bool verticalGravity = !isOnVerticalRail;
                    int sign = (direction == Vector2.up || direction == Vector2.right) ? 1 : -1;
                    EnterFloat(verticalGravity, sign, anchor);
                    return;
                }
                // No same-orientation anchor — fall through to normal logic.
            }
        }

        // Edge requirement for parallel jumps (must be at end of current rail).
        // Perpendicular jumps fire from anywhere on the rail.
        float edgeThreshold = 0.15f;
        if (isOnVerticalRail)
        {
            if (direction == Vector2.up || direction == Vector2.down)
            {
                bool atTopEdge = transform.position.y >= railMax - edgeThreshold;
                bool atBottomEdge = transform.position.y <= railMin + edgeThreshold;
                if (direction == Vector2.up && !atTopEdge) return;
                if (direction == Vector2.down && !atBottomEdge) return;
            }
        }
        else
        {
            if (direction == Vector2.left || direction == Vector2.right)
            {
                bool atLeftEdge = transform.position.x <= railMin + edgeThreshold;
                bool atRightEdge = transform.position.x >= railMax - edgeThreshold;
                if (direction == Vector2.left && !atLeftEdge) return;
                if (direction == Vector2.right && !atRightEdge) return;
            }
        }

        Transform target = FindNearestRailInDirection(direction);
        if (target == null) return;

        JumpToRail(target, direction);
    }

    void JumpToRail(Transform target, Vector2 jumpDirection)
    {
        // Cancel any in-flight jump (used by magnet flight mid-air redirection).
        if (activeJumpCoroutine != null)
            StopCoroutine(activeJumpCoroutine);
        activeJumpCoroutine = StartCoroutine(JumpToRailRoutine(target, jumpDirection));
    }

    IEnumerator JumpToRailRoutine(Transform target, Vector2 jumpDirection)
    {
        isJumping = true;

        bool sourceIsVertical = isOnVerticalRail;
        bool targetIsVertical = IsVerticalRail(target);
        bool isCrossAxis = (sourceIsVertical != targetIsVertical);
        Vector3 startPos = transform.position;

        // Capture pre-jump speed magnitude on the source's active axis (for cross-axis transfer).
        float preJumpSpeed = sourceIsVertical ? Mathf.Abs(smoothedInputV) : Mathf.Abs(smoothedInputH);

        // Capture pre-jump velocity (active axis only — only one is meaningful per rail type).
        float effSpeedJump = useLegacyMovement ? legacyMoveSpeed : moveSpeed;
        Vector2 carryVel = sourceIsVertical
            ? new Vector2(0f, smoothedInputV * effSpeedJump)
            : new Vector2(smoothedInputH * effSpeedJump, 0f);

        // Project pre-jump motion into the landing position. For parallel jumps
        // (carry is on target axis), this bakes forward motion into the destination
        // so the visible motion doesn't get "boomeranged" back to a static landing point.
        // For perpendicular jumps, carry is on the wrong axis and projection adds zero —
        // landing falls back to the unprojected position.
        float effJumpCarry = useLegacyMovement ? 0f : jumpCarryStrength;
        float projectedX = startPos.x + carryVel.x * jumpDuration * effJumpCarry;
        float projectedY = startPos.y + carryVel.y * jumpDuration * effJumpCarry;

        // Calculate landing position based on target rail's bounds.
        Vector3 endPos;
        BoxCollider2D col = target.GetComponent<BoxCollider2D>();
        float buffer = 0.1f;
        if (col != null && targetIsVertical)
        {
            float minY = col.bounds.min.y + buffer;
            float maxY = col.bounds.max.y - buffer;
            endPos = new Vector3(target.position.x, Mathf.Clamp(projectedY, minY, maxY), 0);
        }
        else if (col != null)
        {
            float minX = col.bounds.min.x + buffer;
            float maxX = col.bounds.max.x - buffer;
            endPos = new Vector3(Mathf.Clamp(projectedX, minX, maxX), target.position.y, 0);
        }
        else
        {
            endPos = targetIsVertical
                ? new Vector3(target.position.x, projectedY, 0)
                : new Vector3(projectedX, target.position.y, 0);
        }

        // Animate the jump with linear interpolation. Forward motion is baked into endPos
        // (via projection above), so a linear lerp delivers constant velocity through
        // the jump that matches pre-jump speed when jumpCarryStrength = 1. No deceleration
        // into landing — eliminates the perceived pause at touchdown.
        // Legacy mode: effJumpDuration = 0, so the loop is skipped and landing is instant.
        float effJumpDuration = useLegacyMovement ? 0f : jumpDuration;
        float elapsed = 0f;
        while (elapsed < effJumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / effJumpDuration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Land.
        transform.position = endPos;
        currentRail = target;
        isOnVerticalRail = targetIsVertical;
        UpdateRailBounds();

        // Scale momentum by landingMomentum on both axes (preserves parallel-jump speed).
        float effLandingMomentum = useLegacyMovement ? 0f : landingMomentum;
        float effLandingMomentumCrossAxis = useLegacyMovement ? 0f : landingMomentumCrossAxis;
        smoothedInputH *= effLandingMomentum;
        smoothedInputV *= effLandingMomentum;

        // Cross-axis transfer: if the jump crossed orientations (H→V or V→H),
        // seed the new axis with pre-jump speed magnitude, signed by jump direction.
        // Uses landingMomentumCrossAxis (typically higher than landingMomentum) to provide
        // a "dash" feel that bridges the moment of axis-remapping for the player's hand.
        if (isCrossAxis)
        {
            if (targetIsVertical)
                smoothedInputV = preJumpSpeed * jumpDirection.y * effLandingMomentumCrossAxis;
            else
                smoothedInputH = preJumpSpeed * jumpDirection.x * effLandingMomentumCrossAxis;
        }

        isJumping = false;
        lastLandingTime = Time.time;

        // Apply one step of movement immediately, in this same frame. Update has
        // already run for this frame (with isJumping=true → skipped), so without this
        // the player would be visually static until next frame's Update — perceived
        // as a 1-frame pause at landing.
        if (!qteMode)
            ApplyRailMovement();

        if (qteMode) Debug.Log("Player jumped rails during QTE!");
    }

    // ── Magnet Flight (Zero-Gravity Float Mode) ──────────────────────────────
    // Entered by perpendicular press while on a rail with an anchor in that direction
    // (the "between two rails" requirement). The player gains a custom physics state:
    // gravity along the entry axis (toggleable with W/S or A/D depending on entry),
    // free lateral motion on the perpendicular axis. Float ends when the player
    // contacts any rail.
    void EnterFloat(bool verticalAxis, int gravitySign, Transform targetRail)
    {
        isFloating = true;
        floatVerticalAxis = verticalAxis;
        floatGravitySign = gravitySign;

        // Entry impulse: launch the player in the press direction (gravity axis) with
        // momentum already on them. Lateral axis starts at zero — it's direct-controlled,
        // not gravity-driven, so no carryover makes sense there.
        floatVelocity = verticalAxis
            ? new Vector2(0f, gravitySign * floatEntryImpulse)
            : new Vector2(gravitySign * floatEntryImpulse, 0f);

        // Capture the source rail BEFORE nulling currentRail. We exclude this rail
        // from landing detection until the player has moved clear of its landing radius —
        // otherwise we'd snap right back in the first frame (player position == rail position).
        floatSourceRail = currentRail;
        floatTargetRail = targetRail;
        floatHasLeftSource = false;

        // Compute the vicinity (the box defined by source + target rails).
        // Player exiting this box → forced landing on the closer of the two rails.
        ComputeVicinity();

        // Spawn anchor tracking objects on each rail. The root tracks the player's anchor point
        // (cleanly, no jitter). A child holds the visual and jitters on top — replace the
        // placeholder sprite later with real FX without restructuring.
        if (floatSourceRail != null)
        {
            sourceAnchorObj = new GameObject("MagnetAnchor_Source");
            sourceAnchorObj.transform.SetParent(floatSourceRail, worldPositionStays: true);
            sourceAnchorVisual = CreateAnchorVisual(sourceAnchorObj.transform).transform;
        }
        if (floatTargetRail != null)
        {
            targetAnchorObj = new GameObject("MagnetAnchor_Target");
            targetAnchorObj.transform.SetParent(floatTargetRail, worldPositionStays: true);
            targetAnchorVisual = CreateAnchorVisual(targetAnchorObj.transform).transform;
        }

        // Capture jitter rest position so we can restore it cleanly on exit.
        // Skip if jitter target is the player root — writing to its localPosition would freeze movement.
        if (jitterTarget != null && jitterTarget != transform)
            jitterRestPos = jitterTarget.localPosition;

        // Decouple from rail state — the player is no longer on a rail.
        currentRail = null;
        railMin = 0f;
        railMax = 0f;
        smoothedInputH = 0f;
        smoothedInputV = 0f;

        if (animator != null) animator.SetBool("IsMagnetFloating", true);
    }

    void ComputeVicinity()
    {
        // Default to the rail's transform position with a fallback extent if no collider.
        const float fallbackHalfExtent = 5f;

        BoxCollider2D srcCol = floatSourceRail != null ? floatSourceRail.GetComponent<BoxCollider2D>() : null;
        BoxCollider2D tgtCol = floatTargetRail != null ? floatTargetRail.GetComponent<BoxCollider2D>() : null;

        float srcMinX, srcMaxX, srcMinY, srcMaxY;
        if (srcCol != null) { srcMinX = srcCol.bounds.min.x; srcMaxX = srcCol.bounds.max.x; srcMinY = srcCol.bounds.min.y; srcMaxY = srcCol.bounds.max.y; }
        else { srcMinX = floatSourceRail.position.x - fallbackHalfExtent; srcMaxX = floatSourceRail.position.x + fallbackHalfExtent; srcMinY = floatSourceRail.position.y - fallbackHalfExtent; srcMaxY = floatSourceRail.position.y + fallbackHalfExtent; }

        float tgtMinX, tgtMaxX, tgtMinY, tgtMaxY;
        if (tgtCol != null) { tgtMinX = tgtCol.bounds.min.x; tgtMaxX = tgtCol.bounds.max.x; tgtMinY = tgtCol.bounds.min.y; tgtMaxY = tgtCol.bounds.max.y; }
        else { tgtMinX = floatTargetRail.position.x - fallbackHalfExtent; tgtMaxX = floatTargetRail.position.x + fallbackHalfExtent; tgtMinY = floatTargetRail.position.y - fallbackHalfExtent; tgtMaxY = floatTargetRail.position.y + fallbackHalfExtent; }

        // Vicinity = union of both rails' bounding boxes. Player can drift to either rail's
        // outermost lateral extent; gravity-axis bounds are the rails' positions themselves.
        floatVicinityMinX = Mathf.Min(srcMinX, tgtMinX);
        floatVicinityMaxX = Mathf.Max(srcMaxX, tgtMaxX);
        floatVicinityMinY = Mathf.Min(srcMinY, tgtMinY);
        floatVicinityMaxY = Mathf.Max(srcMaxY, tgtMaxY);
    }

    void ExitFloat(Transform landedRail)
    {
        isFloating = false;
        floatVelocity = Vector2.zero;
        floatGravitySign = 0;
        floatSourceRail = null;
        floatTargetRail = null;
        floatHasLeftSource = false;

        // Tear down anchor tracking objects (children destroyed automatically with parent).
        if (sourceAnchorObj != null) { Destroy(sourceAnchorObj); sourceAnchorObj = null; }
        if (targetAnchorObj != null) { Destroy(targetAnchorObj); targetAnchorObj = null; }
        sourceAnchorVisual = null;
        targetAnchorVisual = null;

        // Restore jitter target's resting local position (skip if root — never wrote to it).
        if (jitterTarget != null && jitterTarget != transform)
            jitterTarget.localPosition = jitterRestPos;

        currentRail = landedRail;
        isOnVerticalRail = IsVerticalRail(landedRail);
        UpdateRailBounds();

        // Snap player onto the rail's axis to align cleanly.
        if (isOnVerticalRail)
            transform.position = new Vector3(landedRail.position.x, transform.position.y, transform.position.z);
        else
            transform.position = new Vector3(transform.position.x, landedRail.position.y, transform.position.z);

        // Clamp into rail bounds so we don't sit "off the end" of the rail.
        if (isOnVerticalRail)
            transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, railMin, railMax), transform.position.z);
        else
            transform.position = new Vector3(Mathf.Clamp(transform.position.x, railMin, railMax), transform.position.y, transform.position.z);

        smoothedInputH = 0f;
        smoothedInputV = 0f;

        if (animator != null) animator.SetBool("IsMagnetFloating", false);
    }

    void UpdateFloat()
    {
        // Asymmetric input model:
        //   - Gravity axis (the axis you flew in on): direction keys toggle gravity sign,
        //     velocity accumulates over time. This is the "fly" axis — Terraria gravity-potion feel.
        //   - Lateral axis (perpendicular to entry): direct velocity at floatLateralSpeed.
        //     Solid positioning control with no momentum fight.
        if (floatVerticalAxis)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) floatGravitySign = 1;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) floatGravitySign = -1;

            floatVelocity.y += floatGravitySign * floatGravity * Time.deltaTime;

            float h = Input.GetAxisRaw("Horizontal");
            floatVelocity.x = h * floatLateralSpeed;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) floatGravitySign = -1;
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) floatGravitySign = 1;

            floatVelocity.x += floatGravitySign * floatGravityHorizontal * Time.deltaTime;

            float v = Input.GetAxisRaw("Vertical");
            floatVelocity.y = v * floatLateralSpeed;
        }

        floatVelocity.x = Mathf.Clamp(floatVelocity.x, -floatMaxSpeed, floatMaxSpeed);
        floatVelocity.y = Mathf.Clamp(floatVelocity.y, -floatMaxSpeed, floatMaxSpeed);

        transform.position += (Vector3)(floatVelocity * Time.deltaTime);

        // Sprite jitter — random offset on top of the resting position. Read by
        // any visual layer (the sprite itself, or a child with the FX). Reset on exit.
        // Skip if target is the player root — would override position writes.
        if (jitterTarget != null && jitterTarget != transform && floatJitterAmount > 0f)
        {
            Vector2 jitter = Random.insideUnitCircle * floatJitterAmount;
            jitterTarget.localPosition = jitterRestPos + (Vector3)jitter;
        }

        // Anchor tracking — each anchor object snaps to the nearest point on its rail
        // to the player. As the player drifts, the anchors slide along the rails.
        // Whatever sprite/effect is attached to these objects will follow the contact point.
        if (sourceAnchorObj != null && floatSourceRail != null)
        {
            bool sv = IsVerticalRail(floatSourceRail);
            sourceAnchorObj.transform.position = NearestPointOnRail(transform.position, floatSourceRail, sv);
        }
        if (targetAnchorObj != null && floatTargetRail != null)
        {
            bool tv = IsVerticalRail(floatTargetRail);
            targetAnchorObj.transform.position = NearestPointOnRail(transform.position, floatTargetRail, tv);
        }

        // Anchor visual jitter — tracking root stays clean, visual child shakes on top.
        if (anchorJitterAmount > 0f)
        {
            if (sourceAnchorVisual != null)
                sourceAnchorVisual.localPosition = Random.insideUnitCircle * anchorJitterAmount;
            if (targetAnchorVisual != null)
                targetAnchorVisual.localPosition = Random.insideUnitCircle * anchorJitterAmount;
        }

        // Once the player has moved clear of the source rail's landing range,
        // drop the exclusion so they CAN land on it again (oscillation back to source).
        // 1.5x margin gives hysteresis: must clearly leave before re-landing is allowed.
        if (!floatHasLeftSource && floatSourceRail != null)
        {
            bool sourceVert = IsVerticalRail(floatSourceRail);
            Vector3 sourceNearest = NearestPointOnRail(transform.position, floatSourceRail, sourceVert);
            if (Vector2.Distance(transform.position, sourceNearest) > floatLandingRadius * 1.5f)
                floatHasLeftSource = true;
        }

        // Vicinity check: if player has exited the box defined by source + target rails,
        // force-snap to whichever of the two rails is closer. This is the "stay between
        // the rails" rule — no free-floating outside the magnet zone.
        if (transform.position.x < floatVicinityMinX || transform.position.x > floatVicinityMaxX
            || transform.position.y < floatVicinityMinY || transform.position.y > floatVicinityMaxY)
        {
            Transform closest = ClosestOfSourceAndTarget();
            if (closest != null)
            {
                ExitFloat(closest);
                return;
            }
        }

        // Land on any rail within landing radius (source rail excluded until left).
        Transform target = FindRailInRange(floatLandingRadius);
        if (target != null)
            ExitFloat(target);
    }

    Transform ClosestOfSourceAndTarget()
    {
        if (floatSourceRail == null) return floatTargetRail;
        if (floatTargetRail == null) return floatSourceRail;

        Vector3 srcNearest = NearestPointOnRail(transform.position, floatSourceRail, IsVerticalRail(floatSourceRail));
        Vector3 tgtNearest = NearestPointOnRail(transform.position, floatTargetRail, IsVerticalRail(floatTargetRail));
        float distSrc = Vector2.Distance(transform.position, srcNearest);
        float distTgt = Vector2.Distance(transform.position, tgtNearest);
        return distSrc <= distTgt ? floatSourceRail : floatTargetRail;
    }

    Transform FindRailInRange(float range)
    {
        Transform best = null;
        float bestDist = range;

        string[] tags = new string[] { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };
        foreach (string tag in tags)
        {
            foreach (GameObject railObj in GameObject.FindGameObjectsWithTag(tag))
            {
                Transform rail = railObj.transform;
                // Exclude the source rail until the player has cleared its landing range.
                if (rail == floatSourceRail && !floatHasLeftSource) continue;

                bool railIsVertical = IsVerticalRail(rail);
                Vector3 nearestPt = NearestPointOnRail(transform.position, rail, railIsVertical);
                float dist = Vector2.Distance(transform.position, nearestPt);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = rail;
                }
            }
        }
        return best;
    }

    GameObject CreateAnchorVisual(Transform parent)
    {
        // Placeholder visual: a small colored square so you can see the jitter immediately
        // without needing an asset. Replace by parenting a real sprite/particle child later.
        GameObject vis = new GameObject("Visual");
        vis.transform.SetParent(parent, worldPositionStays: false);
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localScale = new Vector3(anchorVisualSize, anchorVisualSize, 1f);

        SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
        // Build a 1×1 white texture-backed sprite at runtime — no asset needed.
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = new Color(0.6f, 0.9f, 1f, 0.9f); // pale blue, magnet-feel

        // Sorting: configurable so anchors render on the right layer for this project.
        if (!string.IsNullOrEmpty(anchorSortingLayer))
            sr.sortingLayerName = anchorSortingLayer;
        sr.sortingOrder = anchorSortingOrder;

        // Force the Sprites/Default material so this works in both built-in and URP pipelines.
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null) sr.material = new Material(sh);

        return vis;
    }

    Transform FindMagnetAnchorInDirection(Vector2 direction)
    {
        // Magnet-specific rail search: same direction filter as normal rail search,
        // but uses magnetSearchRange for both perpendicular tolerance and max distance.
        // This is independent of the player's normal jump tolerances so magnet reach
        // can be tuned without affecting standard rail-jumping.
        Transform best = null;
        float bestDist = float.MaxValue;

        string[] tags = new string[] { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };
        foreach (string tag in tags)
        {
            foreach (GameObject railObj in GameObject.FindGameObjectsWithTag(tag))
            {
                Transform rail = railObj.transform;
                if (rail == currentRail) continue;

                bool railVert = IsVerticalRail(rail);
                Vector3 nearestPt = NearestPointOnRail(transform.position, rail, railVert);

                float nearDx = nearestPt.x - transform.position.x;
                float nearDy = nearestPt.y - transform.position.y;

                // Direction filter (same as normal search): nearest point must be in the requested direction.
                const float dirEps = 0.01f;
                if (direction == Vector2.up && nearDy <= dirEps) continue;
                if (direction == Vector2.down && nearDy >= -dirEps) continue;
                if (direction == Vector2.left && nearDx >= -dirEps) continue;
                if (direction == Vector2.right && nearDx <= dirEps) continue;

                // Magnet range: a single radius governs both perpendicular tolerance and forward reach.
                if (Mathf.Abs(nearDx) > magnetSearchRange) continue;
                if (Mathf.Abs(nearDy) > magnetSearchRange) continue;

                float dist = Vector2.Distance(transform.position, nearestPt);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = rail;
                }
            }
        }
        return best;
    }

    Transform FindNearestRailInDirection(Vector2 direction)
    {
        string[] tags = new string[] { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (string tag in tags)
        {
            foreach (GameObject railObj in GameObject.FindGameObjectsWithTag(tag))
            {
                Transform rail = railObj.transform;
                if (rail == currentRail) continue;

                bool targetIsVertical = IsVerticalRail(rail);
                Vector3 nearestPt = NearestPointOnRail(transform.position, rail, targetIsVertical);

                // All distance/tolerance checks measure to the nearest point on the rail's
                // segment, not to its arbitrary transform position. This is what makes
                // tall V rails alongside the player register as adjacent (their nearest
                // point is at the player's y), and lets H rails with off-center transforms
                // still rank correctly by actual proximity.
                float nearDx = nearestPt.x - transform.position.x;
                float nearDy = nearestPt.y - transform.position.y;

                // Direction filter: nearest point must be genuinely in the requested direction.
                const float dirEps = 0.01f;
                if (direction == Vector2.up && nearDy <= dirEps) continue;
                if (direction == Vector2.down && nearDy >= -dirEps) continue;
                if (direction == Vector2.left && nearDx >= -dirEps) continue;
                if (direction == Vector2.right && nearDx <= dirEps) continue;

                if (direction == Vector2.up || direction == Vector2.down)
                {
                    if (Mathf.Abs(nearDy) > verticalMaxDistance) continue;

                    if (targetIsVertical)
                    {
                        if (Mathf.Abs(nearDx) > verticalHorizontalTolerance) continue;
                    }
                    else
                    {
                        BoxCollider2D railCol = rail.GetComponent<BoxCollider2D>();
                        if (railCol != null)
                        {
                            float jumpTolerance = 1.5f;
                            float railLeft = rail.position.x - (railCol.size.x * rail.localScale.x / 2f) - jumpTolerance;
                            float railRight = rail.position.x + (railCol.size.x * rail.localScale.x / 2f) + jumpTolerance;
                            if (transform.position.x < railLeft || transform.position.x > railRight) continue;
                        }
                    }
                }
                else
                {
                    if (Mathf.Abs(nearDy) > horizontalVerticalTolerance) continue;
                    if (Mathf.Abs(nearDx) > horizontalMaxDistance) continue;
                }

                float dist = Vector2.Distance(transform.position, nearestPt);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = rail;
                }
            }
        }
        return best;
    }

    // Distance from a point to the nearest position on a rail's segment.
    // Rails are line segments (not points), so distance-to-transform-center misranks
    // candidates when the transform isn't centered on the segment relative to the player.
    float DistanceToRailEdge(Vector3 from, Transform rail, bool railIsVertical)
    {
        return Vector2.Distance(from, NearestPointOnRail(from, rail, railIsVertical));
    }

    // Nearest point on a rail's segment to a given position.
    // For an H rail: clamp from.x to rail's x-extent, lock y to rail's y.
    // For a V rail: clamp from.y to rail's y-extent, lock x to rail's x.
    Vector3 NearestPointOnRail(Vector3 from, Transform rail, bool railIsVertical)
    {
        BoxCollider2D col = rail.GetComponent<BoxCollider2D>();
        if (col == null) return rail.position;

        if (railIsVertical)
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

    Transform FindNearestRailOfAnyType()
    {
        Transform nearest = null;
        float nearestDist = Mathf.Infinity;
        string[] tags = new string[] { HORIZONTAL_RAIL_TAG, VERTICAL_RAIL_TAG };

        foreach (string tag in tags)
        {
            foreach (GameObject r in GameObject.FindGameObjectsWithTag(tag))
            {
                float dist = Vector3.Distance(transform.position, r.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = r.transform;
                }
            }
        }
        return nearest;
    }

    // ── Rail Bounds ──────────────────────────────────────────────────────────
    void UpdateRailBounds()
    {
        BoxCollider2D col = currentRail.GetComponent<BoxCollider2D>();
        if (col == null) return;

        float buffer = 0.1f;
        if (isOnVerticalRail)
        {
            railMin = col.bounds.min.y + buffer;
            railMax = col.bounds.max.y - buffer;
        }
        else
        {
            railMin = col.bounds.min.x + buffer;
            railMax = col.bounds.max.x - buffer;
        }
    }

    // ── QTE ─────────────────────────────────────────────────────────────────
    public void EnableQTEMode()
    {
        qteMode = true;
        Debug.Log("QTE Mode ENABLED");
    }

    public void DisableQTEMode()
    {
        qteMode = false;
        Debug.Log("QTE Mode DISABLED");
    }

    // ── Health ───────────────────────────────────────────────────────────────
    // IDamageable implementation. Lets enemies and projectiles damage the
    // player through the new combat system (DamagePayload-based).
    public bool IsAlive => currentHealth > 0;

    public void TakeDamage(DamagePayload payload)
    {
        // Wraps the existing int-based TakeDamage so the new system speaks
        // the same language as the legacy code. Eventually all damage should
        // flow through this path and the int overload can be removed.
        TakeDamage(Mathf.RoundToInt(payload.amount));
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || isShielded) return;
        if (Time.time < lastHitTime + invincibilityTime) return;
        lastHitTime = Time.time;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public int GetCurrentHealth() { return currentHealth; }

    public void SetHealth(int amount)
    {
        currentHealth = Mathf.Clamp(amount, 0, maxHealth);
    }

    // ── Debuffs ──────────────────────────────────────────────────────────────
    public void ApplySlowDebuff(float duration, float slowPercent, Color tintColor)
    {
        if (isSlowed) return;
        isSlowed = true;
        slowTimer = duration;
        moveSpeed = normalMoveSpeed * (1f - slowPercent);
        if (playerSpriteRenderer) playerSpriteRenderer.color = tintColor;
    }

    void RemoveSlowDebuff()
    {
        isSlowed = false;
        slowTimer = 0f;
        moveSpeed = normalMoveSpeed;
        if (playerSpriteRenderer) playerSpriteRenderer.color = normalColor;
    }

    // ── Death ────────────────────────────────────────────────────────────────
    void Die()
    {
        Debug.Log("PLAYER DIED!");
        if (GameOver != null) GameOver.SetActive(true);
        Destroy(gameObject);
    }

    // ── Collision ────────────────────────────────────────────────────────────
    // Contact damage flows through the new combat system: enemies detect
    // collision with the player on their side and call IDamageable.TakeDamage
    // on this PlayerController. No collision callbacks needed here anymore.

    // ── Power-Up Effects ─────────────────────────────────────────────────────

    public void ActivateShield(float duration)
    {
        StartCoroutine(ShieldRoutine(duration));
    }

    IEnumerator ShieldRoutine(float duration)
    {
        isShielded = true;
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = Color.cyan;
        yield return new WaitForSeconds(duration);
        isShielded = false;
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = normalColor;
    }

    public void ActivateInvincibility(float duration)
    {
        StartCoroutine(InvincibilityRoutine(duration));
    }

    IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = normalColor;
    }

    public void ActivateSpeedBurst(float multiplier, float duration)
    {
        StartCoroutine(SpeedBurstRoutine(multiplier, duration));
    }

    IEnumerator SpeedBurstRoutine(float multiplier, float duration)
    {
        moveSpeed = normalMoveSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = normalMoveSpeed;
    }

    // ─── Power-Up Shooting Stubs ────────────────────────────────────────────
    // These methods are stubs to keep PowerupManager compiling while combat is
    // being rebuilt. The four offensive power-ups (RapidFire, TripleShot,
    // PiercingShot, Overdrive) are not implemented in the new combat system yet.
    // They will be reimplemented as PowerUpSO subclasses when power-ups get
    // refactored in a future session.

    public void ActivateRapidFire(float multiplier, float duration)
    {
        Debug.LogWarning("[PlayerController] ActivateRapidFire — not yet implemented in new combat system.");
    }

    public void ActivateTripleShot(float duration)
    {
        Debug.LogWarning("[PlayerController] ActivateTripleShot — not yet implemented in new combat system.");
    }

    public void ActivatePiercingShot(float duration)
    {
        Debug.LogWarning("[PlayerController] ActivatePiercingShot — not yet implemented in new combat system.");
    }

    public void ActivateOverdrive(float multiplier, float duration)
    {
        Debug.LogWarning("[PlayerController] ActivateOverdrive — not yet implemented in new combat system.");
    }

}