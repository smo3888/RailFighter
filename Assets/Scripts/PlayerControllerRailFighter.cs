using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PlayerControllerRailFighter : MonoBehaviour
{
    public float moveSpeed = 8f;
    public Transform[] rails;
    public int[] railRows;
    public int[] railColumns;
    public int currentRailIndex = 0;
    public GameObject PlayerLaser ;
    public GameObject GameOver;
    public float jumpCooldown = 1f;
    private float lastJumpTime = 0f;

    public UIDocument cooldownUI;
    private ProgressBar cooldownBar;

    private Transform currentRail;
    private float railMinX;
    private float railMaxX;

    void Start()
    {
        currentRail = rails[currentRailIndex];
        UpdateRailBounds();
        transform.position = new Vector3(currentRail.position.x, currentRail.position.y, 0);

        var root = cooldownUI.rootVisualElement;
        cooldownBar = root.Q<ProgressBar>("CooldownBar");
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        transform.position += new Vector3(horizontal * moveSpeed * Time.deltaTime, 0, 0);

        float clampedX = Mathf.Clamp(transform.position.x, railMinX, railMaxX);
        transform.position = new Vector3(clampedX, currentRail.position.y, 0);

        if (Input.GetMouseButtonDown(0))
        {
            CheckRailClick();
        }

        UpdateCooldownBar();


        if (Input.GetMouseButtonDown(0))
        {
            ShootLaser();
        }

        

    }

    void UpdateCooldownBar()
    {
        float timePassed = Time.time - lastJumpTime;

        

        if (timePassed < jumpCooldown)
        {
            cooldownBar.value = timePassed / jumpCooldown;
        }
        else
        {
            cooldownBar.value = 1;
        }
    }

    void CheckRailClick()
    {
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Rail"))
        {
            Transform clickedRail = hit.collider.transform;
            int clickedIndex = GetRailIndex(clickedRail);

            if (clickedIndex != -1 && IsAdjacent(currentRailIndex, clickedIndex))
            {
                currentRailIndex = clickedIndex;
                currentRail = rails[currentRailIndex];
                UpdateRailBounds();

                transform.position = new Vector3(mousePos.x, currentRail.position.y, 0);

                lastJumpTime = Time.time;
            }
        }
    }

    bool IsAdjacent(int fromIndex, int toIndex)
    {
        int rowDiff = Mathf.Abs(railRows[fromIndex] - railRows[toIndex]);
        int colDiff = Mathf.Abs(railColumns[fromIndex] - railColumns[toIndex]);

        if (rowDiff == 0 && colDiff == 1) return true;
        if (rowDiff == 1) return true;

        return false;
    }

    int GetRailIndex(Transform rail)
    {
        for (int i = 0; i < rails.Length; i++)
        {
            if (rails[i] == rail)
            {
                return i;
            }
        }
        return -1;
    }

    void UpdateRailBounds()
    {
        BoxCollider2D railCollider = currentRail.GetComponent<BoxCollider2D>();
        float railWidth = railCollider.size.x * currentRail.localScale.x;

        railMinX = currentRail.position.x - (railWidth / 2);
        railMaxX = currentRail.position.x + (railWidth / 2);
    }

   


    void ShootLaser()
    {
        
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        Vector3 direction = (mouseWorldPos - transform.position).normalized;
        Vector3 spawnPos = transform.position + (direction * 0.5f);
        GameObject laser = Instantiate(PlayerLaser, spawnPos, Quaternion.identity);
        laser.GetComponent<PlayerLaserScript>().SetDirection(direction);
    
        
        
    }

  void OnDestroy()
    {
        GameOver.SetActive(true);    
    }
}

