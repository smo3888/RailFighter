using UnityEngine;

public class MobileControls : MonoBehaviour
{
    public static bool leftPressed = false;
    public static bool rightPressed = false;
    public static bool swipedUp = false;
    public static bool swipedDown = false;

    [Header("Movement Settings")]
    public float movementZoneWidth = 0.33f;

    [Header("Swipe Settings")]
    public float swipeThreshold = 50f;

    private int movementTouchId = -1;
    private Vector2 movementTouchStart;
    private bool movementLocked = false;
    
    private int swipeTouchId = -1;
    private Vector2 swipeTouchStart;
    private bool swipeTriggered = false;

    void Update()
    {
        leftPressed = false;
        rightPressed = false;
        swipedUp = false;
        swipedDown = false;

        float leftZone = Screen.width * movementZoneWidth;
        float rightZone = Screen.width * (1 - movementZoneWidth);

        if (Input.touchCount >= 2)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.phase == TouchPhase.Began)
                {
                    if (movementTouchId == -1 && (touch.position.x < leftZone || touch.position.x > rightZone))
                    {
                        movementTouchId = touch.fingerId;
                        movementTouchStart = touch.position;
                        movementLocked = true;
                    }
                    else if (swipeTouchId == -1 && touch.fingerId != movementTouchId)
                    {
                        swipeTouchId = touch.fingerId;
                        swipeTouchStart = touch.position;
                        swipeTriggered = false;
                    }
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (touch.fingerId == movementTouchId)
                    {
                        if (movementTouchStart.x < leftZone)
                        {
                            leftPressed = true;
                        }
                        else if (movementTouchStart.x > rightZone)
                        {
                            rightPressed = true;
                        }
                    }
                    else if (touch.fingerId == swipeTouchId && !swipeTriggered)
                    {
                        Vector2 swipeDelta = touch.position - swipeTouchStart;

                        if (Mathf.Abs(swipeDelta.y) > swipeThreshold && Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
                        {
                            if (swipeDelta.y > 0)
                            {
                                swipedDown = true;
                            }
                            else
                            {
                                swipedUp = true;
                            }
                            swipeTriggered = true;
                        }
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (touch.fingerId == movementTouchId)
                    {
                        movementTouchId = -1;
                        movementLocked = false;
                        
                        // Check if swipe finger should become movement finger
                        if (swipeTouchId != -1)
                        {
                            if (swipeTouchStart.x < leftZone || swipeTouchStart.x > rightZone)
                            {
                                movementTouchId = swipeTouchId;
                                movementTouchStart = swipeTouchStart;
                                movementLocked = true;
                                swipeTouchId = -1;
                                swipeTriggered = false;
                            }
                        }
                    }
                    else if (touch.fingerId == swipeTouchId)
                    {
                        swipeTouchId = -1;
                        swipeTriggered = false;
                    }
                }
            }
        }
        else if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                movementTouchId = touch.fingerId;
                movementTouchStart = touch.position;
                movementLocked = false;
                swipeTriggered = false;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 swipeDelta = touch.position - movementTouchStart;

                if (!swipeTriggered && !movementLocked && Mathf.Abs(swipeDelta.y) > swipeThreshold && Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
                {
                    if (swipeDelta.y > 0)
                    {
                        swipedDown = true;
                    }
                    else
                    {
                        swipedUp = true;
                    }
                    swipeTriggered = true;
                }
                else if (!swipeTriggered)
                {
                    if (movementTouchStart.x < leftZone)
                    {
                        leftPressed = true;
                        movementLocked = true;
                    }
                    else if (movementTouchStart.x > rightZone)
                    {
                        rightPressed = true;
                        movementLocked = true;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                movementTouchId = -1;
                movementLocked = false;
                swipeTriggered = false;
            }
        }
        else
        {
            movementTouchId = -1;
            swipeTouchId = -1;
            movementLocked = false;
            swipeTriggered = false;
        }

        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButton(0))
            {
                if (Input.mousePosition.x < leftZone)
                {
                    leftPressed = true;
                }
                else if (Input.mousePosition.x > rightZone)
                {
                    rightPressed = true;
                }
            }
        }
    }
}