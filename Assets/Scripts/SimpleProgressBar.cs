using UnityEngine;

public class SimpleProgressBar : MonoBehaviour
{
    [Header("References")]
    public Transform fillBar;

    [Header("Settings")]
    public float maxValue = 100f;
    private float currentValue;

    void Start()
    {
        currentValue = maxValue;
        UpdateBar();
    }

    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, maxValue);
        UpdateBar();
    }

    public void AddValue(float amount)
    {
        currentValue = Mathf.Clamp(currentValue + amount, 0f, maxValue);
        UpdateBar();
    }

    public void SubtractValue(float amount)
    {
        currentValue = Mathf.Clamp(currentValue - amount, 0f, maxValue);
        UpdateBar();
    }

    public void SetMaxValue(float newMax)
    {
        maxValue = newMax;
        UpdateBar();
    }

    public float GetValue()
    {
        return currentValue;
    }

    public float GetPercent()
    {
        return currentValue / maxValue;
    }

    private void UpdateBar()
    {
        float percent = currentValue / maxValue;
        Vector3 scale = fillBar.localScale;
        scale.x = percent;
        fillBar.localScale = scale;
    }
}