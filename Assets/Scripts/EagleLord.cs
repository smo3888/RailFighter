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
    [SerializeField] private GameObject bossHealthBarUI;

    private enum BossPhase { TurretsActive, MainHP, FinalStand, OffScreen }
    private BossPhase currentPhase = BossPhase.TurretsActive;

    private Collider2D bossCollider;
    private bool isInOffScreenSequence = false;

    void Start()
    {
        currentHealth = maxHealth;
        bossCollider = GetComponent<Collider2D>();
        Debug.Log("Boss started with HP: " + currentHealth + ", Tag: " + gameObject.tag);
    }

    void Update()
    {
        if (currentPhase == BossPhase.TurretsActive || currentPhase == BossPhase.MainHP || currentPhase == BossPhase.FinalStand)
        {
            HandleMovement();
        }

        // Final Stand - continuous eagle spawning
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
        // Calculate spawn interval based on HP (lower HP = faster spawns)
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
            Debug.Log("Final Stand: Spawned eagle at player position");
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
            Debug.Log("Already in off-screen sequence, skipping...");
            yield break;
        }

        isInOffScreenSequence = true;
        Debug.Log("Starting off-screen sequence");

        BossPhase previousPhase = currentPhase;
        currentPhase = BossPhase.OffScreen;

        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }
        SetTurretsActive(false);

        string originalTag = gameObject.tag;
        Debug.Log("Boss tag before off-screen: " + originalTag);
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

        Debug.Log("Boss reached off-screen position");
        yield return new WaitForSeconds(offScreenDuration);

        // Trigger eagle barrage
        if (eagleSpawner != null)
        {
            Debug.Log("Spawning eagle barrage");
            eagleSpawner.SpawnFullBarrage();
        }

        // Wait for all 5 eagle waves to complete
        yield return new WaitForSeconds(barrageWaitTime);

        Debug.Log("Barrage complete, boss returning");
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
        Debug.Log("Boss tag after off-screen: " + gameObject.tag);

        foreach (EagleLordTurret turret in turrets)
        {
            if (turret != null && turret.gameObject != null)
            {
                turret.gameObject.tag = "Obstacle";
            }
        }

        // Check if both turrets destroyed - activate Final Stand
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
        Debug.Log("Off-screen sequence complete. Current phase: " + currentPhase);
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("TakeDamage called! Current phase: " + currentPhase + ", Tag: " + gameObject.tag + ", HP: " + currentHealth);

        if (currentPhase == BossPhase.TurretsActive || currentPhase == BossPhase.OffScreen)
        {
            Debug.Log("Boss is invulnerable!");
            return;
        }

        currentHealth -= damage;
        Debug.Log("Boss took damage! New HP: " + currentHealth);

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
        Debug.Log("Boss collision detected! Hit by: " + other.gameObject.name + " with tag: " + other.tag);

        if (other.CompareTag("Projectile"))
        {
            Debug.Log("Confirmed projectile hit - calling TakeDamage");
            TakeDamage(1);
            Destroy(other.gameObject);
        }
        else
        {
            Debug.Log("Not a projectile, ignoring");
        }
    }
}