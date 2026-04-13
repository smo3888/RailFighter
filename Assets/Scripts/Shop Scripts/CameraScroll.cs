using UnityEngine;

// Reusable camera autoscroll system
// Scrolls camera in any direction at customizable speed
// Can be triggered by other scripts
public class CameraScroll : MonoBehaviour
{
    [Header("Scroll Settings")]
    public Vector3 scrollDirection = Vector3.right;  // Which direction to scroll (1,0,0) = right
    public float scrollSpeed = 3f;                    // Units per second
    public float targetPosition = 30f;                // Stop when camera.x reaches this
    public bool useTargetPosition = true;             // Stop at target or scroll forever?

    [Header("Axis Settings")]
    public ScrollAxis scrollAxis = ScrollAxis.X;      // Which axis to check for target

    [Header("State")]
    public bool isScrolling = false;                  // Currently scrolling?

    private Vector3 startPosition;

    public enum ScrollAxis { X, Y, Z }

    void Start()
    {
        // Remember where camera started
        startPosition = transform.position;
    }

    void Update()
    {
        // Only run if scrolling is active
        if (!isScrolling) return;

        // Move camera in scroll direction
        transform.position += scrollDirection.normalized * scrollSpeed * Time.deltaTime;

        // Check if reached target position
        if (useTargetPosition && HasReachedTarget())
        {
            StopScrolling();
        }
    }

    bool HasReachedTarget()
    {
        // Check based on which axis we're scrolling
        switch (scrollAxis)
        {
            case ScrollAxis.X:
                // If scrolling right, check if X >= target
                // If scrolling left, check if X <= target
                return scrollDirection.x > 0 ?
                    transform.position.x >= targetPosition :
                    transform.position.x <= targetPosition;

            case ScrollAxis.Y:
                return scrollDirection.y > 0 ?
                    transform.position.y >= targetPosition :
                    transform.position.y <= targetPosition;

            case ScrollAxis.Z:
                return scrollDirection.z > 0 ?
                    transform.position.z >= targetPosition :
                    transform.position.z <= targetPosition;

            default:
                return false;
        }
    }

    // Call this from other scripts to start scrolling
    public void StartScrolling()
    {
        isScrolling = true;
        Debug.Log("Camera scroll started!");
    }

    // Call this to stop scrolling
    public void StopScrolling()
    {
        isScrolling = false;
        Debug.Log("Camera scroll stopped at position: " + transform.position);
    }

    // Reset camera to start position (if needed)
    public void ResetPosition()
    {
        transform.position = startPosition;
        isScrolling = false;
    }
}