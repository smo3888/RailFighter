using UnityEngine;
using System.Collections.Generic;

public class MobileControls : MonoBehaviour
{
    // ============================================
    // STATIC OUTPUT (read by other scripts)
    // ============================================
    public static bool leftPressed = false;
    public static bool rightPressed = false;
    public static bool topPressed = false;
    public static bool bottomPressed = false;
    public static bool swipedUp = false;
    public static bool swipedDown = false;
    public static bool doubleTapLeft = false;
    public static bool doubleTapRight = false;

    // ============================================
    // SETTINGS
    // ============================================
    [Header("Movement Settings")]
    public float movementZoneWidth = 0.33f;

    [Header("Swipe Settings")]
    public float swipeThreshold = 50f;

    [Header("Double Tap Settings")]
    public float doubleTapTime = 0.3f;

    // ============================================
    // INTERNAL TRACKING
    // ============================================
    private Dictionary<int, Vector2> touchStartPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, bool> touchHasSwiped = new Dictionary<int, bool>();
    private Dictionary<int, bool> touchUsedForTargeting = new Dictionary<int, bool>();

    // Double tap tracking
    private float lastTapTimeLeft = 0f;
    private float lastTapTimeRight = 0f;

    // Mouse fallback
    private bool mouseDown = false;
    private Vector2 mouseStartPos;
    private bool mouseHasSwiped = false;
    private bool mouseUsedForTargeting = false;
    private float lastMouseTapTimeLeft = 0f;
    private float lastMouseTapTimeRight = 0f;

    // ============================================
    // UPDATE
    // ============================================
    void Update()
    {
        // Reset one-frame inputs
        swipedUp = false;
        swipedDown = false;
        doubleTapLeft = false;
        doubleTapRight = false;
        leftPressed = false;
        rightPressed = false;
        topPressed = false;
        bottomPressed = false;

        float leftZone = Screen.width * movementZoneWidth;
        float rightZone = Screen.width * (1 - movementZoneWidth);
        float topZone = Screen.height * movementZoneWidth;
        float bottomZone = Screen.height * (1 - movementZoneWidth);
        float screenMiddle = Screen.width * 0.5f;

        if (Input.touchCount > 0)
        {
            HandleTouches(leftZone, rightZone, topZone, bottomZone, screenMiddle);
        }
        else
        {
            touchStartPositions.Clear();
            touchHasSwiped.Clear();
            touchUsedForTargeting.Clear();

            HandleMouse(leftZone, rightZone, topZone, bottomZone, screenMiddle);
        }
    }

    // ============================================
    // HANDLE ALL TOUCHES
    // ============================================
    void HandleTouches(float leftZone, float rightZone, float topZone, float bottomZone, float screenMiddle)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            int id = touch.fingerId;

            if (touch.phase == TouchPhase.Began)
            {
                // CHECK FOR ENEMY CLICK FIRST
                Vector3 touchPos = Camera.main.ScreenToWorldPoint(touch.position);
                touchPos.z = 0;
                RaycastHit2D hit = Physics2D.Raycast(touchPos, Vector2.zero, Mathf.Infinity);

                if (hit.collider != null && hit.collider.CompareTag("Obstacle"))
                {
                    // Clicked an enemy - mark this touch as used for targeting
                    touchUsedForTargeting[id] = true;
                    continue;
                }

                touchStartPositions[id] = touch.position;
                touchHasSwiped[id] = false;
                touchUsedForTargeting[id] = false;

                // Check for double tap
                if (touch.position.x < screenMiddle)
                {
                    if (Time.time - lastTapTimeLeft < doubleTapTime)
                    {
                        doubleTapLeft = true;
                    }
                    lastTapTimeLeft = Time.time;
                }
                else
                {
                    if (Time.time - lastTapTimeRight < doubleTapTime)
                    {
                        doubleTapRight = true;
                    }
                    lastTapTimeRight = Time.time;
                }
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (!touchStartPositions.ContainsKey(id)) continue;

                // Skip if this touch was used for targeting
                if (touchUsedForTargeting.ContainsKey(id) && touchUsedForTargeting[id])
                {
                    continue;
                }

                // Check for swipe (any finger, anywhere)
                if (!touchHasSwiped[id])
                {
                    Vector2 delta = touch.position - touchStartPositions[id];

                    if (Mathf.Abs(delta.y) > swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    {
                        if (delta.y > 0)
                        {
                            swipedUp = true;
                        }
                        else
                        {
                            swipedDown = true;
                        }
                        touchHasSwiped[id] = true;
                    }
                }

                // Check for movement - use START position
                if (!touchHasSwiped[id])
                {
                    // Horizontal zones (left/right edges)
                    if (touchStartPositions[id].x < leftZone)
                    {
                        leftPressed = true;
                    }
                    else if (touchStartPositions[id].x > rightZone)
                    {
                        rightPressed = true;
                    }

                    // Vertical zones (top/bottom edges)
                    if (touchStartPositions[id].y < topZone)
                    {
                        bottomPressed = true;
                    }
                    else if (touchStartPositions[id].y > bottomZone)
                    {
                        topPressed = true;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                touchStartPositions.Remove(id);
                touchHasSwiped.Remove(id);
                touchUsedForTargeting.Remove(id);
            }
        }
    }

    // ============================================
    // HANDLE MOUSE (Editor + WebGL fallback)
    // ============================================
    void HandleMouse(float leftZone, float rightZone, float topZone, float bottomZone, float screenMiddle)
    {
        if (Input.GetMouseButtonDown(0))
        {
            // CHECK FOR ENEMY CLICK FIRST - before setting any movement flags
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity);

            if (hit.collider != null && hit.collider.CompareTag("Obstacle"))
            {
                // Clicked an enemy - don't process movement AT ALL
                mouseUsedForTargeting = true;
                return;
            }

            mouseStartPos = Input.mousePosition;
            mouseDown = true;
            mouseHasSwiped = false;
            mouseUsedForTargeting = false;

            // Check for double tap - use screen halves
            if (Input.mousePosition.x < screenMiddle)
            {
                if (Time.time - lastMouseTapTimeLeft < doubleTapTime)
                {
                    doubleTapLeft = true;
                }
                lastMouseTapTimeLeft = Time.time;
            }
            else
            {
                if (Time.time - lastMouseTapTimeRight < doubleTapTime)
                {
                    doubleTapRight = true;
                }
                lastMouseTapTimeRight = Time.time;
            }
        }
        else if (Input.GetMouseButton(0) && mouseDown)
        {
            // Skip movement if initial click was used for targeting
            if (mouseUsedForTargeting)
            {
                return;
            }

            // Check for swipe
            if (!mouseHasSwiped)
            {
                Vector2 delta = (Vector2)Input.mousePosition - mouseStartPos;

                if (Mathf.Abs(delta.y) > swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                {
                    if (delta.y > 0)
                    {
                        swipedUp = true;
                    }
                    else
                    {
                        swipedDown = true;
                    }
                    mouseHasSwiped = true;
                }
            }

            // Check for movement - use START position
            if (!mouseHasSwiped)
            {
                // Horizontal zones
                if (mouseStartPos.x < leftZone)
                {
                    leftPressed = true;
                }
                else if (mouseStartPos.x > rightZone)
                {
                    rightPressed = true;
                }

                // Vertical zones
                if (mouseStartPos.y < topZone)
                {
                    bottomPressed = true;
                }
                else if (mouseStartPos.y > bottomZone)
                {
                    topPressed = true;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mouseDown = false;
            mouseHasSwiped = false;
            mouseUsedForTargeting = false;
        }
    }
}