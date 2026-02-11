using UnityEngine;

public class DriftLoopX : MonoBehaviour
{
    public float speed = 0.2f;     
    public float minX = -30f;      
    public float maxX = 30f;       

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < minX)
        {
            var p = transform.position;
            p.x = maxX;
            transform.position = p;
        }
    }
}
