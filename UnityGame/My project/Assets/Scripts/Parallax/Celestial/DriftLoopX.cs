using UnityEngine;

public class DriftLoopX : MonoBehaviour
{
    public float speed = 0.2f;

    // Rango LOCAL respecto a la posición inicial
    public float minX = -30f;
    public float maxX = 30f;

    private Vector3 startLocalPos;

    void Awake()
    {
        startLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        var p = transform.localPosition;

        p.x += (-speed) * Time.deltaTime;

        float localMin = startLocalPos.x + minX;
        float localMax = startLocalPos.x + maxX;

        if (p.x < localMin)
            p.x = localMax;

        transform.localPosition = p;
    }
}
