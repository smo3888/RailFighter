using UnityEngine;
using System.Collections;

public class Mimic : MonoBehaviour
{
    [Header("Health - Phase System")]
    [SerializeField] private int maxPhase1Health = 20;
    [SerializeField] private int maxPhase2Health = 15;
    [SerializeField] private int maxPhase3Health = 15;
    [SerializeField] private int maxPhase4Health = 10;
    private int currentPhaseHealth;
    private int currentPhase = 1;
    private int totalEncounterHealth;
    private int currentEncounterHealth;

    [Header("Spawn Cutscene")]
    [SerializeField] private GameObject beamPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float beamSpawnHeight = 8f;
    [SerializeField] private float beamDuration = 2f;
    [SerializeField] private float postBeamDelay = 0.5f;
    private bool cutsceneComplete = false;

    [Header("Sprite Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] damageSprites;
    [SerializeField] private Sprite phase3SerpentSprite;
    [SerializeField] private Sprite phase3IdleSprite;
    [SerializeField] private Sprite phase3MovementSprite;
    [SerializeField] private Sprite phase3MeteorSummonSprite;
    private int currentFrame = 0;

    [Header("Rail System")]
    [SerializeField] private Transform[] rails;
    [SerializeField] private int[] railRows;
    [SerializeField] private int[] railColumns;
    private int currentRailIndex = 0;
    private Transform currentRail;
    private float railMinX, railMaxX;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 15f;
    private bool isJumping = false;
    private Vector3 jumpTarget;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 1.5f;
    private float lastFireTime;
    private bool allowShooting = true;

    [Header("AI Behavior")]
    [SerializeField] private float railDecisionCooldown = 1f;
    [SerializeField] private float initialRailStayDuration = 3f;
    private float lastRailDecisionTime;
    private int moveDirection = 1;
    private float cutsceneEndTime = -999f;

    [Header("Phase 2 - Egg Laying")]
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private Transform[] eggSpawnPoints;
    [SerializeField] private Transform waitSpot;
    [SerializeField] private float layingPauseDuration = 2f;
    [SerializeField] private float eggLayMoveSpeed = 25f;
    [SerializeField] private float eggFloatHeight = 0.5f;
    private int activeEggCount = 0;
    private bool isLayingEggs = false;
    private bool isWaitingForBabies = false;
    private bool eggLay75Triggered = false;
    private bool eggLay50Triggered = false;
    private bool eggLay20Triggered = false;

    [Header("Phase 3 - Ship Throw QTE")]
    [SerializeField] private GameObject shipProjectilePrefab;
    [SerializeField] private Transform centerPosition;
    [SerializeField] private float shipThrowSpeed = 10f;
    [SerializeField] private int shipDamage = 1;
    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float floatBobSpeed = 1f;
    [SerializeField] private float floatBobAmount = 0.3f;
    private bool phase3Activated = false;
    private bool isFloating = false;
    private bool isInCutscene = false;
    private float floatStartY;

    [Header("Phase 3 - Movement AI")]
    [SerializeField] private Transform[] phase3Waypoints;
    [SerializeField] private float phase3MoveInterval = 3f;
    private float lastPhase3MoveTime = -999f;
    private bool isMovingToPosition = false;

    [Header("Phase 3 - Meteor Attack")]
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private GameObject meteorBordersPrefab;
    [SerializeField] private float borderAlpha = 0.6f;
    [SerializeField] private float borderScale = 0.3f;
    [SerializeField] private float borderOffsetX = 0f;
    [SerializeField] private float borderOffsetY = 0f;
    [SerializeField] private float meteorScale = 3f;
    [SerializeField] private float meteorSpawnInterval = 0.8f;

    [Header("50% Health Meteor Pattern")]
    [SerializeField] private Transform[] meteorSpawnSequence50;
    [SerializeField] private float meteorStartSpeed50 = 4f;
    [SerializeField] private float meteorEndSpeed50 = 8f;

    [Header("30% Health Meteor Pattern")]
    [SerializeField] private Transform[] meteorSpawnSequence30;
    [SerializeField] private float meteorStartSpeed30 = 5f;
    [SerializeField] private float meteorEndSpeed30 = 10f;

    [Header("Phase 4 - Final Desperation (RANDOMIZED)")]
    [SerializeField] private Transform leftMeteorSpawn;
    [SerializeField] private Transform rightMeteorSpawn;
    [SerializeField] private float meteorStartSpeed10 = 6f;
    [SerializeField] private float meteorEndSpeed10 = 12f;

    private bool isSummoningMeteors = false;
    private GameObject activeBorders;
    private bool meteorShower50Triggered = false;
    private bool meteorShower30Triggered = false;
    private bool inFinalDesperation = false;

    [Header("Boss UI")]
    [SerializeField] private BossHealthBarUI bossHealthBarUI;

    private PlayerControllerRailFighter player;

    void Start()
    {
        totalEncounterHealth = maxPhase1Health + maxPhase2Health + maxPhase3Health + maxPhase4Health;
        currentEncounterHealth = totalEncounterHealth;

        currentPhaseHealth = maxPhase1Health;
        player = FindAnyObjectByType<PlayerControllerRailFighter>();

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        currentRail = rails[currentRailIndex];
        UpdateRailBounds();

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }
        else
        {
            transform.position = new Vector3(currentRail.position.x, currentRail.position.y, 0);
        }

        lastFireTime = Time.time;
        lastRailDecisionTime = Time.time;

        if (spriteRenderer) spriteRenderer.enabled = false;
        if (bossHealthBarUI) bossHealthBarUI.gameObject.SetActive(false);

        UpdateSpriteBasedOnHealth();

        Debug.Log($"Mimic Boss started - Total Encounter HP: {totalEncounterHealth}");

        StartCoroutine(SpawnCutscene());
    }

    IEnumerator SpawnCutscene()
    {
        if (player)
        {
            player.autoFireEnabled = false;
            player.SetCanShoot(false);
        }

        Debug.Log("MIMIC SPAWN CUTSCENE - BEAM IN!");

        Vector3 beamPosition = new Vector3(transform.position.x, beamSpawnHeight, transform.position.z);
        GameObject beam = null;

        if (beamPrefab != null)
        {
            beam = Instantiate(beamPrefab, beamPosition, Quaternion.identity);
        }

        yield return new WaitForSeconds(beamDuration);

        if (spriteRenderer) spriteRenderer.enabled = true;

        if (beam != null) Destroy(beam);

        yield return new WaitForSeconds(postBeamDelay);

        if (bossHealthBarUI)
        {
            bossHealthBarUI.gameObject.SetActive(true);
            bossHealthBarUI.UpdateHealth(currentEncounterHealth, totalEncounterHealth);
        }

        if (player)
        {
            player.autoFireEnabled = true;
            player.SetCanShoot(true);
        }

        cutsceneComplete = true;
        cutsceneEndTime = Time.time;
        Debug.Log("MIMIC SPAWN CUTSCENE COMPLETE - FIGHT BEGINS!");
    }

    void Update()
    {
        if (!cutsceneComplete) return;

        if (currentPhase == 3 && isFloating && !isInCutscene)
        {
            HandleFloating();
            HandlePhase3Attacks();
        }
        else if (currentPhase == 4 && !isInCutscene)
        {
            HandleFloating();
        }
        else if (currentPhase == 2 && isWaitingForBabies)
        {
            HandleFloating();
        }
        else if (currentPhase < 3 && !isLayingEggs && !isWaitingForBabies)
        {
            HandleRailJumping();
            HandleHorizontalMovement();
            HandleAI();
        }

        if (currentPhase == 2 && !isLayingEggs && !isWaitingForBabies)
        {
            float healthPercent = (float)currentPhaseHealth / maxPhase2Health;

            if (!eggLay75Triggered && healthPercent <= 0.75f)
            {
                eggLay75Triggered = true;
                StartCoroutine(Phase2EggLayingSequence());
            }
            else if (!eggLay50Triggered && healthPercent <= 0.50f)
            {
                eggLay50Triggered = true;
                StartCoroutine(Phase2EggLayingSequence());
            }
            else if (!eggLay20Triggered && healthPercent <= 0.20f)
            {
                eggLay20Triggered = true;
                StartCoroutine(Phase2EggLayingSequence());
            }
        }

        HandleShooting();
    }

    int GetMaxHealthForCurrentPhase()
    {
        if (currentPhase == 1) return maxPhase1Health;
        if (currentPhase == 2) return maxPhase2Health;
        if (currentPhase == 3) return maxPhase3Health;
        return maxPhase4Health;
    }

    void UpdateSpriteBasedOnHealth()
    {
        if (currentPhase != 1 || !spriteRenderer || damageSprites.Length != 5) return;

        float healthPercent = (float)currentPhaseHealth / maxPhase1Health;
        int targetFrame = healthPercent > 0.75f ? 0 : (healthPercent > 0.50f ? 1 : (healthPercent > 0.25f ? 2 : (healthPercent > 0.01f ? 3 : 4)));

        if (targetFrame != currentFrame)
        {
            currentFrame = targetFrame;
            spriteRenderer.sprite = damageSprites[targetFrame];
            Debug.Log($"SPRITE SWAP: Frame {currentFrame} at Phase 1 HP {currentPhaseHealth}/{maxPhase1Health}");
        }
    }

    void SetSpriteDirection(Vector3 targetPosition)
    {
        if (currentPhase != 3) return;
        if (isSummoningMeteors) return;

        float direction = targetPosition.x - transform.position.x;

        if (Mathf.Abs(direction) > 0.1f)
        {
            if (phase3MovementSprite) spriteRenderer.sprite = phase3MovementSprite;
            transform.localScale = new Vector3((direction < 0 ? -1 : 1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            if (phase3IdleSprite) spriteRenderer.sprite = phase3IdleSprite;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void HandleFloating()
    {
        if (isMovingToPosition && isLayingEggs && inFinalDesperation)
        {
            return;
        }

        if (isWaitingForBabies && currentPhase == 2)
        {
            float newY = floatStartY + Mathf.Sin(Time.time * floatBobSpeed) * floatBobAmount;
            transform.position = new Vector3(transform.position.x, newY, 0);
            return;
        }

        float bobY = floatStartY + Mathf.Sin(Time.time * floatBobSpeed) * floatBobAmount;
        transform.position = new Vector3(transform.position.x, bobY, 0);
    }

    void HandlePhase3Attacks()
    {
        if (!isLayingEggs && !isSummoningMeteors && !isMovingToPosition && Time.time >= lastPhase3MoveTime + phase3MoveInterval)
        {
            lastPhase3MoveTime = Time.time;

            if (phase3Waypoints != null && phase3Waypoints.Length > 0)
            {
                Transform randomWaypoint = phase3Waypoints[Random.Range(0, phase3Waypoints.Length)];
                StartCoroutine(MoveToPositionPhase3(randomWaypoint.position));
            }
        }

        float healthPercent = (float)currentPhaseHealth / maxPhase3Health;

        if (!meteorShower50Triggered && healthPercent <= 0.5f && !isLayingEggs && !isMovingToPosition)
        {
            meteorShower50Triggered = true;
            StartCoroutine(MeteorShowerAttack(meteorSpawnSequence50, meteorStartSpeed50, meteorEndSpeed50));
        }

        if (!meteorShower30Triggered && healthPercent <= 0.3f && !isLayingEggs && !isMovingToPosition && !isSummoningMeteors)
        {
            meteorShower30Triggered = true;
            StartCoroutine(MeteorShowerAttack(meteorSpawnSequence30, meteorStartSpeed30, meteorEndSpeed30));
        }
    }

    IEnumerator MoveToPositionPhase3(Vector3 targetPosition)
    {
        isMovingToPosition = true;

        Vector3 startPos = transform.position;
        float moveDuration = Vector3.Distance(startPos, targetPosition) / jumpSpeed;
        float elapsed = 0f;

        SetSpriteDirection(targetPosition);

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / moveDuration);
            yield return null;
        }

        transform.position = targetPosition;
        floatStartY = targetPosition.y;
        SetSpriteDirection(targetPosition);

        isMovingToPosition = false;
    }

    IEnumerator MeteorShowerAttack(Transform[] pattern, float startSpeed, float endSpeed)
    {
        isSummoningMeteors = true;

        if (player) player.SetCanShoot(false);

        Debug.Log("METEOR SHOWER ATTACK!");

        yield return StartCoroutine(MoveToPositionPhase3(centerPosition.position));

        if (phase3MeteorSummonSprite)
            spriteRenderer.sprite = phase3MeteorSummonSprite;

        yield return new WaitForSeconds(0.5f);

        if (pattern != null && pattern.Length > 0 && meteorPrefab)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                Transform spawnPoint = pattern[i];

                float progress = (float)i / pattern.Length;
                float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, progress);

                GameObject meteor = Instantiate(meteorPrefab, spawnPoint.position, Quaternion.identity);
                meteor.transform.localScale = Vector3.one * meteorScale;
                MeteorProjectile meteorScript = meteor.GetComponent<MeteorProjectile>();
                if (meteorScript)
                {
                    meteorScript.Initialize(currentSpeed, 1);
                }

                yield return new WaitForSeconds(meteorSpawnInterval);
            }
        }

        if (phase3IdleSprite)
            spriteRenderer.sprite = phase3IdleSprite;

        isSummoningMeteors = false;

        if (player) player.SetCanShoot(true);

        Debug.Log("Meteor shower complete!");
    }

    IEnumerator RandomMeteorShowerAttack()
    {
        isSummoningMeteors = true;
        inFinalDesperation = true;

        if (player) player.SetCanShoot(false);

        Debug.Log("PHASE 4 - FINAL DESPERATION - RANDOMIZED METEOR SHOWER!");

        yield return StartCoroutine(MoveToPositionPhase3(centerPosition.position));

        isMovingToPosition = true;
        isLayingEggs = true;

        if (phase3MeteorSummonSprite)
            spriteRenderer.sprite = phase3MeteorSummonSprite;

        if (meteorBordersPrefab)
        {
            activeBorders = Instantiate(meteorBordersPrefab, transform.position, Quaternion.identity, transform);
            activeBorders.transform.localScale = Vector3.one * borderScale;
            activeBorders.transform.localPosition = new Vector3(borderOffsetX, borderOffsetY, 0);

            SpriteRenderer[] borderSprites = activeBorders.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in borderSprites)
            {
                Color color = sr.color;
                color.a = borderAlpha;
                sr.color = color;
            }

            Transform[] childBorders = new Transform[] {
                activeBorders.transform.Find("TopBorder"),
                activeBorders.transform.Find("BottomBorder"),
                activeBorders.transform.Find("LeftBorder"),
                activeBorders.transform.Find("RightBorder")
            };

            foreach (Transform border in childBorders)
            {
                if (border != null)
                {
                    BoxCollider2D col = border.GetComponent<BoxCollider2D>();
                    if (col == null)
                    {
                        col = border.gameObject.AddComponent<BoxCollider2D>();
                    }
                    col.isTrigger = true;

                    Rigidbody2D rb = border.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        rb = border.gameObject.AddComponent<Rigidbody2D>();
                    }
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;

                    HolyBorder borderScript = border.gameObject.AddComponent<HolyBorder>();

                    Debug.Log($"Border setup: {border.name} - Collider: {col != null}, Trigger: {col.isTrigger}, Rigidbody: {rb != null}");
                }
            }

            Debug.Log("DESTRUCTIBLE BORDERS SPAWNED! Each has 5 HP!");
        }

        yield return new WaitForSeconds(0.5f);

        isSummoningMeteors = false;

        if (player) player.SetCanShoot(true);

        Debug.Log("PHASE 4 BOSS VULNERABLE! SHOOT THE BOSS OR DESTROY BORDERS!");

        int spawnCount = 0;
        Vector3 lockedPosition = centerPosition.position;

        while (currentPhase == 4)
        {
            transform.position = new Vector3(lockedPosition.x, floatStartY, 0);

            Transform spawnPoint = Random.value > 0.5f ? leftMeteorSpawn : rightMeteorSpawn;

            float progress = Mathf.Min((float)spawnCount / 50f, 1f);
            float currentSpeed = Mathf.Lerp(meteorStartSpeed10, meteorEndSpeed10, progress);

            GameObject meteor = Instantiate(meteorPrefab, spawnPoint.position, Quaternion.identity);
            meteor.transform.localScale = Vector3.one * meteorScale;
            MeteorProjectile meteorScript = meteor.GetComponent<MeteorProjectile>();
            if (meteorScript)
            {
                meteorScript.Initialize(currentSpeed, 1);
            }

            spawnCount++;
            yield return new WaitForSeconds(meteorSpawnInterval);
        }

        isMovingToPosition = false;
        isLayingEggs = false;

        if (activeBorders)
            Destroy(activeBorders);

        if (phase3IdleSprite)
            spriteRenderer.sprite = phase3IdleSprite;
    }

    IEnumerator Phase2EggLayingSequence()
    {
        isLayingEggs = true;
        isJumping = false;

        isSummoningMeteors = true;

        Debug.Log("Phase 2 egg laying triggered!");

        for (int i = 0; i < eggSpawnPoints.Length; i++)
        {
            Vector3 spawnPos = eggSpawnPoints[i].position;

            yield return StartCoroutine(MoveToPositionFast(spawnPos, eggLayMoveSpeed));

            yield return new WaitForSeconds(layingPauseDuration);
            LayEggAt(spawnPos + Vector3.up * eggFloatHeight);
        }

        if (waitSpot != null)
        {
            yield return StartCoroutine(MoveToPositionFast(waitSpot.position, eggLayMoveSpeed));
            floatStartY = waitSpot.position.y;
            isFloating = true;
            isWaitingForBabies = true;
        }

        isLayingEggs = false;

        Debug.Log("Mimic INVULNERABLE at wait spot until babies die...");

        StartCoroutine(WaitForBabiesToDie());
    }

    IEnumerator WaitForBabiesToDie()
    {
        while (true)
        {
            BabyAlien[] babyAliens = FindObjectsOfType<BabyAlien>();
            if (babyAliens.Length == 0)
            {
                isWaitingForBabies = false;
                isFloating = false;
                isSummoningMeteors = false;

                int nearestRailIndex = GetNearestRailIndex(transform.position);
                currentRailIndex = nearestRailIndex;
                currentRail = rails[currentRailIndex];

                Vector3 railPosition = new Vector3(currentRail.position.x, currentRail.position.y, 0);
                yield return StartCoroutine(MoveToPositionFast(railPosition, eggLayMoveSpeed));

                UpdateRailBounds();
                lastRailDecisionTime = Time.time;

                Debug.Log("All babies dead! Mimic VULNERABLE and returning to rail combat!");
                break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MoveToPositionFast(Vector3 targetPosition, float speed)
    {
        Vector3 startPos = transform.position;
        float moveDuration = Vector3.Distance(startPos, targetPosition) / speed;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / moveDuration);
            yield return null;
        }

        transform.position = targetPosition;
    }

    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float moveDuration = Vector3.Distance(startPos, targetPosition) / jumpSpeed;
        float elapsed = 0f;

        SetSpriteDirection(targetPosition);

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / moveDuration);
            yield return null;
        }

        transform.position = targetPosition;
        SetSpriteDirection(targetPosition);
    }

    void LayEggAt(Vector3 position)
    {
        if (!eggPrefab) return;

        GameObject egg = Instantiate(eggPrefab, position, Quaternion.identity);
        MimicEgg eggScript = egg.GetComponent<MimicEgg>();
        if (eggScript) eggScript.SetBoss(this);

        activeEggCount++;
        Debug.Log($"Laid egg! Active count: {activeEggCount}");
    }

    public void OnEggDestroyed()
    {
        activeEggCount--;
        Debug.Log($"Egg destroyed! Active count: {activeEggCount}");
    }

    int GetNearestRailIndex(Vector3 position)
    {
        float closestDist = Mathf.Infinity;
        int closestRail = 0;

        for (int i = 0; i < rails.Length; i++)
        {
            float dist = Vector3.Distance(position, rails[i].position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestRail = i;
            }
        }
        return closestRail;
    }

    void HandleRailJumping()
    {
        if (currentPhase >= 3 || !isJumping) return;

        transform.position = Vector3.MoveTowards(transform.position, jumpTarget, jumpSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, jumpTarget) < 0.1f)
        {
            isJumping = false;
            transform.position = jumpTarget;
            UpdateRailBounds();
        }
    }

    void HandleHorizontalMovement()
    {
        if (currentPhase >= 3 || isJumping) return;

        float newX = Mathf.Clamp(transform.position.x + (moveDirection * moveSpeed * Time.deltaTime), railMinX, railMaxX);
        transform.position = new Vector3(newX, transform.position.y, 0);

        if ((moveDirection == 1 && newX >= railMaxX) || (moveDirection == -1 && newX <= railMinX))
            moveDirection *= -1;
    }

    void HandleAI()
    {
        if (currentPhase >= 3 || !player || isJumping || Time.time < lastRailDecisionTime + railDecisionCooldown) return;

        // PREVENT RAIL JUMPING FOR THE FIRST FEW SECONDS AFTER SPAWN
        if (Time.time < cutsceneEndTime + initialRailStayDuration) return;

        lastRailDecisionTime = Time.time;

        int playerRailIndex = GetPlayerRailIndex();
        int playerRow = railRows[playerRailIndex];
        int playerCol = railColumns[playerRailIndex];
        int currentRow = railRows[currentRailIndex];
        int currentCol = railColumns[currentRailIndex];

        if (currentRow == playerRow && Random.value > 0.3f)
            TryJumpVertical(playerRow == 0 ? 1 : 0);
        else if (currentCol == playerCol && Random.value > 0.3f)
            TryJumpHorizontal(playerCol == 0 ? 1 : 0);

        moveDirection = player.transform.position.x > transform.position.x ? -1 : 1;
    }

    int GetPlayerRailIndex()
    {
        float closestDist = Mathf.Infinity;
        int closestRail = 0;

        for (int i = 0; i < rails.Length; i++)
        {
            float dist = Vector3.Distance(player.transform.position, rails[i].position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestRail = i;
            }
        }
        return closestRail;
    }

    void TryJumpVertical(int targetRow)
    {
        int currentCol = railColumns[currentRailIndex];
        for (int i = 0; i < rails.Length; i++)
            if (railRows[i] == targetRow && railColumns[i] == currentCol)
            {
                JumpToRail(i);
                return;
            }
    }

    void TryJumpHorizontal(int targetCol)
    {
        int currentRow = railRows[currentRailIndex];
        for (int i = 0; i < rails.Length; i++)
            if (railRows[i] == currentRow && railColumns[i] == targetCol)
            {
                JumpToRail(i);
                return;
            }
    }

    void JumpToRail(int railIndex)
    {
        if (isLayingEggs) return;

        currentRailIndex = railIndex;
        currentRail = rails[railIndex];
        jumpTarget = new Vector3(currentRail.position.x, currentRail.position.y, 0);
        isJumping = true;
    }

    void UpdateRailBounds()
    {
        Collider2D railCollider = currentRail.GetComponent<Collider2D>();
        if (railCollider)
        {
            Bounds bounds = railCollider.bounds;
            railMinX = bounds.min.x;
            railMaxX = bounds.max.x;
        }
        else
        {
            railMinX = -8f;
            railMaxX = 8f;
        }
    }

    void HandleShooting()
    {
        if (!allowShooting || isSummoningMeteors || inFinalDesperation || Time.time < lastFireTime + fireRate) return;

        BabyAlien[] babyAliens = FindObjectsOfType<BabyAlien>();
        if (babyAliens.Length > 0)
        {
            return;
        }

        ShootAtPlayer();
        lastFireTime = Time.time;
    }

    void ShootAtPlayer()
    {
        if (!player || !projectilePrefab) return;

        Vector3 direction = (player.transform.position - transform.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        PlayerLaserScript laser = projectile.GetComponent<PlayerLaserScript>();
        if (laser)
        {
            laser.isEnemyLaser = true;
            laser.SetDirection(direction);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isSummoningMeteors) return;

        currentPhaseHealth -= damage;
        currentEncounterHealth -= damage;

        Debug.Log($"Mimic damage! Phase {currentPhase} HP: {currentPhaseHealth}/{GetMaxHealthForCurrentPhase()}, Encounter: {currentEncounterHealth}/{totalEncounterHealth}");

        UpdateSpriteBasedOnHealth();

        if (bossHealthBarUI) bossHealthBarUI.UpdateHealth(currentEncounterHealth, totalEncounterHealth);

        if (currentPhaseHealth <= 0) AdvanceToNextPhase();
    }

    void AdvanceToNextPhase()
    {
        if (currentPhase == 1)
        {
            currentPhase = 2;
            currentPhaseHealth = maxPhase2Health;
            Debug.Log($"PHASE 2! Encounter HP: {currentEncounterHealth}/{totalEncounterHealth}");
        }
        else if (currentPhase == 2)
        {
            currentPhase = 3;
            currentPhaseHealth = maxPhase3Health;
            Debug.Log($"PHASE 3! Ship throw QTE... Encounter HP: {currentEncounterHealth}/{totalEncounterHealth}");

            if (!phase3Activated)
            {
                phase3Activated = true;
                StartCoroutine(Phase3TransitionCutscene());
            }
        }
        else if (currentPhase == 3)
        {
            currentPhase = 4;
            currentPhaseHealth = maxPhase4Health;
            Debug.Log($"PHASE 4 - FINAL DESPERATION! Encounter HP: {currentEncounterHealth}/{totalEncounterHealth}");

            StartCoroutine(RandomMeteorShowerAttack());
        }
        else if (currentPhase == 4)
        {
            Die();
        }
    }

    IEnumerator Phase3TransitionCutscene()
    {
        isInCutscene = true;
        allowShooting = false;

        if (player) player.autoFireEnabled = false;

        Debug.Log("Phase 3: DISAPPEARING (fake death)...");

        transform.position = new Vector3(centerPosition.position.x, 15f, 0);
        if (phase3SerpentSprite) spriteRenderer.sprite = phase3SerpentSprite;

        yield return new WaitForSeconds(2.5f);

        Debug.Log("Phase 3: DESCENDING...");
        yield return StartCoroutine(MoveToPosition(centerPosition.position));
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Phase 3: SHIP THROW (SLOW-MO)!");

        if (player)
        {
            player.autoFireEnabled = true;
            player.EnableQTEMode();
        }

        Time.timeScale = slowMotionScale;

        Vector3 direction = (player.transform.position - transform.position).normalized;
        GameObject ship = Instantiate(shipProjectilePrefab, transform.position, Quaternion.identity);
        MimicShipProjectile shipScript = ship.GetComponent<MimicShipProjectile>();
        if (shipScript) shipScript.Initialize(direction, shipThrowSpeed, shipDamage, player);

        float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout && Time.timeScale < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        if (player) player.DisableQTEMode();

        Debug.Log("Phase 3 complete! Battle begins...");

        if (phase3IdleSprite)
            spriteRenderer.sprite = phase3IdleSprite;

        allowShooting = true;
        floatStartY = transform.position.y;
        isFloating = true;

        isInCutscene = false;
    }

    void Die()
    {
        Debug.Log("Mimic defeated!");

        if (player) player.SetCanShoot(true);

        if (bossHealthBarUI) bossHealthBarUI.gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            PlayerLaserScript laser = other.GetComponent<PlayerLaserScript>();
            if (laser && !laser.isEnemyLaser)
            {
                TakeDamage(1);
                Destroy(other.gameObject);
            }
        }
    }
}