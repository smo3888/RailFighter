using UnityEngine;

public class Laser : MonoBehaviour
{
    public float speed = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.right * speed * Time.deltaTime;   
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
