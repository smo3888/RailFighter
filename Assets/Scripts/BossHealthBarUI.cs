using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private RectTransform healthBarContainer;

    [Header("Animation")]
    [SerializeField] private float slideInDuration = 0.5f;
    [SerializeField] private Vector2 targetPosition = new Vector2(200, 100);
    [SerializeField] private Vector2 startPosition = new Vector2(200, -200);

    void OnEnable()
    {
        // This runs automatically when the GameObject is enabled
        StartCoroutine(SlideIn());
    }

    IEnumerator SlideIn()
    {
        healthBarContainer.anchoredPosition = startPosition;

        float elapsed = 0f;

        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideInDuration;
            t = t * t * (3f - 2f * t);

            healthBarContainer.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        healthBarContainer.anchoredPosition = targetPosition;
    }

    public void UpdateHealth(float currentHP, float maxHP)
    {
        if (healthFillImage != null)
        {
            float fillAmount = currentHP / maxHP;
            healthFillImage.fillAmount = fillAmount;
        }
    }
}