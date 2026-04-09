using UnityEngine;

public class MimicShipProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private PlayerControllerRailFighter player;
    private int playerStartRailIndex;
    private bool playerHasJumped = false;

    public void Initialize(Vector3 dir, float spd, int dmg, PlayerControllerRailFighter plyr)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        player = plyr;

      
    }

    void Update()
    {
        // Move in direction - USE UNSCALED TIME so ship moves at normal speed during slow-mo
        transform.position += direction * speed * Time.unscaledDeltaTime;

     
        // Destroy if offscreen
        if (Mathf.Abs(transform.position.x) > 20f || Mathf.Abs(transform.position.y) > 20f)
        {
            EndSlowMotion(); // Failsafe
            Destroy(gameObject);
        }
    }

    void EndSlowMotion()
    {
        Time.timeScale = 1f;
        if (player != null)
        {
            player.DisableQTEMode();
        }
        Debug.Log("Ship QTE: Slow-mo ended!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllerRailFighter hitPlayer = other.GetComponent<PlayerControllerRailFighter>();
            if (hitPlayer != null)
            {
                hitPlayer.TakeDamage(damage);
                Debug.Log("Ship hit player!");
            }
            EndSlowMotion();
            Destroy(gameObject);
        }
    }
}