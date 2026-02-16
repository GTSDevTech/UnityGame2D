using UnityEngine;

public class FollowCameraX : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField] private float fixedY = 0f;

    void Awake()
    {
        if (!cam && Camera.main) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!cam) return;

        var p = transform.position;
        p.x = cam.position.x;
        p.y = fixedY;
        transform.position = p;
    }

    // Para que puedas setearlo desde inspector o por código
    public void SetFixedY(float y) => fixedY = y;
}
