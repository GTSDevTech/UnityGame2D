using UnityEngine;

public class ParallaxLayer2D : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactor = 0.2f;

    private Transform cam;
    private Vector3 lastCamPos;

    void Start()
    {
        cam = Camera.main ? Camera.main.transform : null;
        if (cam == null)
        {
            Debug.LogError("ParallaxLayer2D: No Main Camera found (Tag MainCamera).");
            enabled = false;
            return;
        }
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, 0f, 0f);
        lastCamPos = cam.position;
    }
}
