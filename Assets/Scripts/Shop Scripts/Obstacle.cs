using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{

    //Random Spin

    public float maxSpinSpeed = 10f;

    //Randmon Size

    public float minSize = 0.5f;
    public float maxSize = 2.0f;
    
    // Random Asteroid
    

    //Random Speed
     
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public GameObject ExplosionEffect;
    // Components

    Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float randomSize = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(randomSize, randomSize, 1);

        rb = GetComponent<Rigidbody2D>();

        float randomSpeed = Random.Range(minSpeed, maxSpeed) / randomSize;
        Vector2 randomDirection = Random.insideUnitCircle;
        rb.AddForce(randomDirection * randomSpeed);

        float randomTorque = Random.Range(-maxSpinSpeed, maxSpeed);
        rb.AddTorque(randomTorque);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
             Instantiate(ExplosionEffect, transform.position, transform.rotation);
        }
       
    }
}


