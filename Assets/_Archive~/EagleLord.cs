using UnityEngine;
using System.Collections;

public class EagleLord : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float leftBound = -5f;
    [SerializeField] private float rightBound = 5f;
    [SerializeField] private float offScreenMoveSpeed = 5f;
    [SerializeField] private float offScreenY = 10f;
    [SerializeField] private float normalY = 0f;
    [SerializeField] private float offScreenDuration = 3f;
    private int moveDirection = 1;

    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;
    private int totalEncounterHealth;
    private int currentEncounterHealth;

    [Header("Turrets")]
    [SerializeField] private GameObject leftTurret;
    [SerializeField] private GameObject rightTurret;
    [SerializeField] private GameObject middleTurret;
    private bool leftTurretDestroyed = false;
    private bool rightTurretDestroyed = false;

    [Header("Eagle Barrage")]
    [SerializeField] private EagleSpawner eagleSpawner;
    [SerializeField] private float barrageWaitTime = 15f;

    [Header("Final Stand - Eagle Spam")]
    [SerializeField] private GameObject regularEagleLockOn;
    [SerializeField] private float maxEagleSpawnInterval = 3f;
    [SerializeField] private float minEagleSpawnInterval = 0.5f;
    private bool finalStandActive = false;
    private float eagleSpawnTimer = 0f;
    private float currentEagleSpawnInterval;

    [Header("Boss UI")]
    [SerializeField] private BossHealthBarUI bossHealthBarUI;

    private enum BossPhase { TurretsActive, MainHP, FinalStand, OffScreen }
    private BossPhase currentPhase = BossPhase.TurretsActive;

    private Collider2D bossCollider;
    private bool isInOffScreenSequence = false;

    void Start()
    {
        currentHealth = maxHealth;
        bossCollider = GetComponent<Collider2D>();

        // Calculate total encounter health (turrets + boss)
        int turretHealth = 0;
        EagleLordTurret[] turrets = GetComponentsInChildren<EagleLordTurret>();
        foreach (EagleLordTurret turret in turrets)
        {
            turretHealth += turret.GetMaxHealth();
        }

        totalEncounterHealth = turretHealth + maxHealth;
        currentEncounterHealth = totalEncounterHealth;

        // Just initialize health, don't show bar (BossIntro handles that)
        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.UpdateHealth(currentEncounterHealth, totalEncounterHealth);
        }

        Debug.Log("Boss started with total encounter HP: " + totalEncounterHealth);
    }

    void Update()
    {
        if (currentPhase == BossPhase.TurretsActive || currentPhase == BossPhase.MainHP || currentPhase == BossPhase.FinalStand)
        {
            HandleMovement();
        }

        if (finalStandActive && currentPhase != BossPhase.OffScreen)
        {
            HandleFinalStandEagles();
        }
    }

    void HandleMovement()
    {
        transform.position += Vector3.right * moveDirection * moveSpeed * Time.deltaTime;

        if (moveDirection == 1 && transform.position.x >= rightBound)
        {
            moveDirection = -1;
        }
        else if (moveDirection == -1 && transform.position.x <= leftBound)
        {
            moveDirection = 1;
        }
    }

    void HandleFinalStandEagles()
    {
        float healthPercent = (float)currentHealth / maxHealth;
        currentEagleSpawnInterval = Mathf.Lerp(minEagleSpawnInterval, maxEagleSpawnInterval, healthPercent);

        eagleSpawnTimer += Time.deltaTime;
        if (eagleSpawnTimer >= currentEagleSpawnInterval)
        {
            SpawnRandomEagle();
            eagleSpawnTimer = 0f;
        }
    }

    void SpawnRandomEagle()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && regularEagleLockOn != null)
        {
            Instantiate(regularEagleLockOn, playerObj.transform.position, Quaternion.identity);
        }
    }

    void SetTurretsActive(bool active)
    {
        EagleLordTurret[] turrets = GetComponentsInChildren<EagleLordTurret>();
        foreach (EagleLordTurret turret in turrets)
        {
            if (turret != null)
            {
                if (active)
                {
                    turret.EnableShooting();
                }
                else
                {
                    turret.DisableShooting();
                }
            }
        }
    }

    public void UpdateEncounterHealth(int damage)
    {
        currentEncounterHealth -= damage;

        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.UpdateHealth(currentEncounterHealth, totalEncounterHealth);
        }
    }

    public void TurretDestroyed(string turretSide)
    {
        if (turretSide == "Left")
        {
            leftTurretDestroyed = true;
            Debug.Log("Left turret destroyed - triggering barrage");
            StartCoroutine(OffScreenSequence());
        }
        else if (turretSide == "Right")
        {
            rightTurretDestroyed = true;
            Debug.Log("Right turret destroyed - triggering barrage");
            StartCoroutine(OffScreenSequence());
        }

        CheckTurretPhaseComplete();
    }

    void CheckTurretPhaseComplete()
    {
        if (leftTurretDestroyed && rightTurretDestroyed)
        {
            currentPhase = BossPhase.MainHP;
            Debug.Log("Boss is now vulnerable!");
        }
    }

    IEnumerator OffScreenSequence()
    {
        if (isInOffScreenSequence)
        {
            yield break;
        }

        isInOffScreenSequence = true;

        BossPhase previousPhase = currentPhase;
        currentPhase = BossPhase.OffScreen;

        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }
        SetTurretsActive(false);

        string originalTag = gameObject.tag;
        gameObject.tag = "Untagged";

        EagleLordTurret[] turrets = GetComponentsInChildren<EagleLordTurret>();
        foreach (EagleLordTurret turret in turrets)
        {
            if (turret != null && turret.gameObject != null)
            {
                turret.gameObject.tag = "Untagged";
            }
        }

        Vector3 centerPos = new Vector3(0, transform.position.y, transform.position.z);
        while (Mathf.Abs(transform.position.x) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, centerPos, offScreenMoveSpeed * Time.deltaTime);
            yield return null;
        }

        Vector3 offScreenPos = new Vector3(0, offScreenY, transform.position.z);
        while (transform.position.y < offScreenY - 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, offScreenPos, offScreenMoveSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(offScreenDuration);

        if (eagleSpawner != null)
        {
            eagleSpawner.SpawnFullBarrage();
        }

        yield return new WaitForSeconds(barrageWaitTime);

        Vector3 returnPos = new Vector3(0, normalY, transform.position.z);
        while (transform.position.y > normalY + 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, returnPos, offScreenMoveSpeed * Time.deltaTime);
            yield return null;
        }

        if (bossCollider != null)
        {
            bossCollider.enabled = true;
        }
        SetTurretsActive(true);
        gameObject.tag = originalTag;

        foreach (EagleLordTurret turret in turrets)
        {
            if (turret != null && turret.gameObject != null)
            {
                turret.gameObject.tag = "Obstacle";
            }
        }

        if (leftTurretDestroyed && rightTurretDestroyed)
        {
            currentPhase = BossPhase.FinalStand;
            finalStandActive = true;
            Debug.Log("Both turrets destroyed - Final Stand activated! Eagles incoming!");
        }
        else
        {
            currentPhase = previousPhase;
        }

        isInOffScreenSequence = false;
    }

    public void TakeDamage(int damage)
    {
        if (currentPhase == BossPhase.TurretsActive || currentPhase == BossPhase.OffScreen)
        {
            return;
        }

        currentHealth -= damage;
        currentEncounterHealth -= damage;

        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.UpdateHealth(currentEncounterHealth, totalEncounterHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Eagle Lord defeated!");
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}