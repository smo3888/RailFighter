using UnityEngine;

public class TransitionShadowBreathing : MonoBehaviour
{
    public float minAlpha = 0.7f;
    public float maxAlpha = 0.9f;
    public float speed = 1f;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        Color c = sr.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        sr.color = c;
    }
}