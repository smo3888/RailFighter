using UnityEngine;
using UnityEngine.Rendering.Universal;

public class UniversalCameraScaler : MonoBehaviour
{
    [Header("Reference Design Resolution")]
    [Tooltip("the resolution designed the game for")]
    public float referenceWidth = 1920f;
    public float referenceHeight = 1080f;

    [Header("Camera Settings")]
    [Tooltip("the orthographic size at reference resolution")]
    public float referenceOrthographicSize = 5f;

    [Header("Safe Area Support")]
    [Tooltip("Enable this for mobile deviceswith notches")]
    public bool useSafeArea = true;

    private Camera cam;
    private float initialOrthographicSize;

 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = GetComponent<Camera>();
        initialOrthographicSize = referenceOrthographicSize;
        AdjustCamera();
    }

    // Update is called once per frame
    void Update()
    {
        //recheck on orientation  change or window resize
        AdjustCamera();
    }

    void AdjustCamera()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Handle safe area for notched devices
        if (useSafeArea && Application.isMobilePlatform)
        { Rect safeArea = Screen.safeArea;
            screenWidth = safeArea.width;
            screenHeight = safeArea.height;
        }

        // Calculate aspect ratios
        float targetAspect = referenceWidth / referenceHeight;
        float currentAspect = screenWidth / screenHeight;

        //Adjust orthgraphic size to fit content
        if (currentAspect > targetAspect)
        {
            //Screen is wider or equal - fit to height 
            cam.orthographicSize = initialOrthographicSize;
        }
        else
        {
            // Screen is taller - scale up to show all content
            float ratio = targetAspect / currentAspect;
            cam.orthographicSize = initialOrthographicSize * ratio;
        }


    }
}
