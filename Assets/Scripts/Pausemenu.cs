using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button mainMenuButton;

    [Header("Pixelate")]
    public ScriptableRendererData rendererData;
    [Range(1f, 20f)]
    public float targetPixelSize = 8f;
    public float animationDuration = 0.3f;

    [Header("Mouse Distortion")]
    [Range(0f, 0.5f)]
    public float mouseRadius = 0.15f;
    [Range(0f, 5f)]
    public float distortionStrength = 1.5f;

    private bool isPaused = false;
    private FullScreenPassRendererFeature pixelateFeature;

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (rendererData != null)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is FullScreenPassRendererFeature fsPass)
                {
                    pixelateFeature = fsPass;
                    break;
                }
            }
        }

        if (pixelateFeature != null)
            Debug.Log("FullScreenPassRendererFeature found!");
        else
            Debug.LogWarning("FullScreenPassRendererFeature NOT found!");

        Shader.SetGlobalFloat("_PixelSize", 1f);
        Shader.SetGlobalFloat("_MouseRadius", mouseRadius);
        Shader.SetGlobalFloat("_DistortionStrength", distortionStrength);
        Shader.SetGlobalVector("_MouseUV", Vector2.zero);

        SetPixelateActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }

        if (isPaused)
        {
            // Update mouse UV
            Vector2 mousePos = Input.mousePosition;
            Vector2 mouseUV = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
            Shader.SetGlobalVector("_MouseUV", mouseUV);

            // Animate time for ripple effect (unscaled so it runs while paused)
            Shader.SetGlobalFloat("_Time2", Time.unscaledTime);
        }
    }

    void Pause()
    {
        isPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);

        SetPixelateActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimatePixelate(1f, targetPixelSize, () =>
        {
            Time.timeScale = 0f;
        }));
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(AnimatePixelate(targetPixelSize, 1f, () =>
        {
            SetPixelateActive(false);
        }));
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SetPixelateActive(false);
        if (GameManager.Instance != null) GameManager.Instance.SaveGame();
        SceneManager.LoadScene("Main Menu 1");
    }

    IEnumerator AnimatePixelate(float from, float to, System.Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            Shader.SetGlobalFloat("_PixelSize", Mathf.Lerp(from, to, t));
            yield return null;
        }
        Shader.SetGlobalFloat("_PixelSize", to);
        onComplete?.Invoke();
    }

    void SetPixelateActive(bool active)
    {
        if (pixelateFeature != null)
            pixelateFeature.SetActive(active);
    }
}