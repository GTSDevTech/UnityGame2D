using UnityEngine;

public class FollowX2D : MonoBehaviour
{
    public Transform target;
    public float y = 0f;
    public float z = 0f;

    void LateUpdate()
    {
        if (!target) return;
        var p = transform.position;
        p.x = target.position.x;   // sigue en X
        p.y = y;                   // fijo
        p.z = z;
        transform.position = p;
    }
}
