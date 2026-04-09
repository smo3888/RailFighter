using UnityEngine;
using System.Collections;

public class BossIntro : MonoBehaviour
{
    [Header("Intro Settings")]
    [SerializeField] private float entranceSpeed = 3f;
    [SerializeField] private Vector3 finalPosition = new Vector3(0, 3, 0);
    [SerializeField] private float shakeIntensity = 0.2f;
    [SerializeField] private float arrivalShakeIntensity = 0.5f;
    [SerializeField] private float arrivalShakeDuration = 1f;

    [Header("UI")]
    [SerializeField] private GameObject bossHealthBarContainer;

    private CameraShake cameraShake;
    private PlayerControllerRailFighter player;
    private EagleLord bossScript;
    private Coroutine continuousShake;

    void Start()
    {
        cameraShake = Camera.main.GetComponent<CameraShake>();
        player = FindAnyObjectByType<PlayerControllerRailFighter>();
        bossScript = GetComponent<EagleLord>();

        if (bossScript != null)
        {
            bossScript.enabled = false;
        }

        DisableTurrets();

        StartCoroutine(IntroSequence());
    }

    void DisableTurrets()
    {
        EagleLordTurret[] turrets = GetComponentsInChildren<EagleLordTurret>();
        foreach (EagleLordTurret turret in turrets)
        {
            turret.DisableShooting();
        }
    }

    void EnableTurrets()
    {
        EagleLordTurret[] turrets = GetComponentsInChildren<EagleLordTurret>();
        foreach (EagleLordTurret turret in turrets)
        {
            turret.EnableShooting();
        }
    }

    IEnumerator ContinuousShake(float magnitude)
    {
        Transform cam = Camera.main.transform;
        Vector3 originalPos = cam.localPosition;

        while (true)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            yield return null;
        }
    }

    IEnumerator IntroSequence()
    {
        // Store original camera position
        Vector3 originalCamPos = Camera.main.transform.localPosition;

        if (player != null)
        {
            player.autoFireEnabled = false;
        }

        transform.position = new Vector3(-15, finalPosition.y, 0);

        if (cameraShake != null)
        {
            continuousShake = StartCoroutine(ContinuousShake(shakeIntensity));
        }

        while (Vector3.Distance(transform.position, finalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, finalPosition, entranceSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = finalPosition;

        // Stop continuous shake and restore camera position
        if (continuousShake != null)
        {
            StopCoroutine(continuousShake);
            Camera.main.transform.localPosition = originalCamPos;
        }

        if (cameraShake != null)
        {
            yield return StartCoroutine(cameraShake.Shake(arrivalShakeDuration, arrivalShakeIntensity));
        }

        yield return new WaitForSeconds(0.5f);

        if (player != null)
        {
            player.autoFireEnabled = true;
        }

        if (bossScript != null)
        {
            bossScript.enabled = true;

            // Show health bar - just enable it directly
            if (bossHealthBarContainer != null)
            {
                bossHealthBarContainer.SetActive(true);
                Debug.Log("Health bar enabled!");
            }
        }

        // 3 second grace period before turrets start shooting
        yield return new WaitForSeconds(3f);

        EnableTurrets();

        Destroy(this);
    }
}