using UnityEngine;

// Attach this to a Canvas with a full-screen black Image and a CanvasGroup component
// Set the Canvas sort order high so it renders on top of everything

public class FadeCanvas : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start transparent
        canvasGroup.alpha = 0f;

        // Don't destroy between scenes
        DontDestroyOnLoad(gameObject);
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }
}