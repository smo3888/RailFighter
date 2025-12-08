using UnityEngine;
using UnityEngine.UIElements;
public class RailFIghterEnemy : MonoBehaviour
{
    
    public GameObject GameOver;
    public UIDocument HealthBar;
    private ProgressBar Health;
    public float moveSpeed = 3f;
    public Transform player;
    public float health = 5f;
    private float maxHealth = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var root = HealthBar.rootVisualElement;
        Health = root.Q<ProgressBar>("Health");

        Health.value = 3f;

        player = GameObject.FindGameObjectWithTag("Player").transform;
        GameOver = GameObject.FindGameObjectWithTag("GameOver");
    }

    // Update is called once per frame
    void Update()
    {

        if (player == null)
        {
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        UpdateHealthBarPosition();

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(collision.gameObject);
            GameOver.SetActive(true);

        }

        if (collision.gameObject.CompareTag("Projectile"))
            {
            health -= 1;
            Health.value -= 1;
            } 


        if (health < 0)
        {
            Destroy(gameObject);
            Die();
        }

    }

    void UpdateHealthBarPosition()
    {
        
        Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0.5f, 0));

        Health.style.left = screenPos.x - 25;
        Health.style.top = Screen.height - screenPos.y;
        Health.value = health;
        
    }

    void Die()
    {
        // Notify wave manager
        FindObjectOfType<WaveManager>().EnemyKilled();

        // Destroy enemy
        Destroy(gameObject);
    }

}



