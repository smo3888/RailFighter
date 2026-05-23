using UnityEngine;

public class TurretHead : MonoBehaviour
{
    
    public Transform player;
    public GameObject bulletPrefab;
    public Transform firePoints;
    public float fireRate = 2f;

    private float nextFireTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        if (Time.time > nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + (1f / fireRate);
        }

        void Shoot()
        {
           
         Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0, 0, 90);
            Instantiate(bulletPrefab, transform.position, spawnRotation);
        }
    }
}
