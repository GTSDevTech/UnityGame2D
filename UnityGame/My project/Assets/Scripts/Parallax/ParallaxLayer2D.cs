using UnityEngine;

public class ParallaxLayer2D : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactor = 0.2f;

    [SerializeField] private Transform cam;

    private Vector3 startLayerPos;
    private float startCamX;

    void Start()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (cam == null)
        {
            Debug.LogError("ParallaxLayer2D: No Main Camera found (Tag MainCamera).");
            enabled = false;
            return;
        }

        startLayerPos = transform.position;
        startCamX = cam.position.x;
    }

    void LateUpdate()
    {
        float camDeltaX = cam.position.x - startCamX;

        var p = startLayerPos;
        p.x = startLayerPos.x - camDeltaX * parallaxFactor;
        transform.position = p;
    }
}
