using UnityEngine;

public class MobileControls : MonoBehaviour
{
    public static bool leftPressed = false;
    public static bool rightPressed = false;
    public static bool swipedUp = false;
    public static bool swipedDown = false;

    // Swipe detection
    private Vector2 swipeTouchStartPos;
    private int swipeTouchId = -1;
    private bool swipeTriggered = false;
    public float swipeThreshold = 30f;

    void Update()
    {
        // Reset each frame
        leftPressed = false;
        rightPressed = false;
        swipedUp = false;
        swipedDown = false;

        float screenThird = Screen.width / 3f;

        // Process ALL touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            bool inLeftZone = touch.position.x < screenThird;
            bool inRightZone = touch.position.x > Screen.width - screenThird;
            bool inMiddleZone = !inLeftZone && !inRightZone;

            // Movement - left/right zones
            if (inLeftZone && (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began))
            {
                leftPressed = true;
            }
            else if (inRightZone && (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began))
            {
                rightPressed = true;
            }

            // Swipe - middle zone only
            if (inMiddleZone)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    swipeTouchStartPos = touch.position;
                    swipeTouchId = touch.fingerId;
                    swipeTriggered = false;
                }
                else if (touch.phase == TouchPhase.Moved && touch.fingerId == swipeTouchId && !swipeTriggered)
                {
                    Vector2 swipeDelta = touch.position - swipeTouchStartPos;

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
                else if (touch.phase == TouchPhase.Ended && touch.fingerId == swipeTouchId)
                {
                    swipeTouchId = -1;
                    swipeTriggered = false;
                }
            }
        }
    }
}