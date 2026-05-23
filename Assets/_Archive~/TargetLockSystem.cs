using UnityEngine;

public class TargetLockSystem : MonoBehaviour
{
    [Header("Lock Settings")]
    public Material outlineMaterial;
    public LayerMask enemyLayer;

    private GameObject lockedTarget;
    private Material originalMaterial;
    private SpriteRenderer lockedRenderer;

    // Flag to tell other systems if this click was used for targeting
    private bool clickUsedForTargeting = false;

    void Update()
    {
        clickUsedForTargeting = false; // Reset each frame

        // Detect click/tap
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        // Check if locked target still exists
        if (lockedTarget != null && lockedTarget.activeInHierarchy == false)
        {
            ClearLock();
        }
    }

    void HandleClick()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // Raycast to see if we hit an enemy
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, enemyLayer);

        if (hit.collider != null && hit.collider.CompareTag("Obstacle"))
        {
            // Clicked on an enemy - consume this click
            clickUsedForTargeting = true;

            GameObject clickedEnemy = hit.collider.gameObject;

            if (clickedEnemy == lockedTarget)
            {
                // Clicked same enemy - unlock it
                ClearLock();
            }
            else
            {
                // Lock onto new enemy
                ClearLock();
                LockOnto(clickedEnemy);
            }
        }
        // Don't clear lock on empty clicks - only when enemy dies or player clicks locked enemy again
    }

    void LockOnto(GameObject enemy)
    {
        lockedTarget = enemy;
        lockedRenderer = enemy.GetComponent<SpriteRenderer>();

        if (lockedRenderer != null && outlineMaterial != null)
        {
            originalMaterial = lockedRenderer.material;
            lockedRenderer.material = outlineMaterial;
        }
    }

    public void ClearLock()
    {
        if (lockedTarget != null && lockedRenderer != null && originalMaterial != null)
        {
            lockedRenderer.material = originalMaterial;
        }

        lockedTarget = null;
        lockedRenderer = null;
        originalMaterial = null;
    }

    public GameObject GetLockedTarget()
    {
        return lockedTarget;
    }

    public bool HasLockedTarget()
    {
        return lockedTarget != null;
    }

    public bool ClickWasUsedForTargeting()
    {
        return clickUsedForTargeting;
    }
}