using UnityEngine;

public class PlayerLaserScript : MonoBehaviour
{

    public float speed = 10f;
    private Vector3 direction;
    private Rigidbody2D rb;
    private Transform ignoreRail;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        rb.linearVelocity = direction * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
       
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }




        if (collision.transform == ignoreRail)
        {
            return;
        }
        
        
        if (collision.gameObject.CompareTag("Rail") ||
            collision.gameObject.CompareTag("Obstacle"))
        { 
            Destroy(gameObject);
        }
    }

    public void SetIgnoreRail(Transform rail)
    {
        ignoreRail = rail;
    }

}
