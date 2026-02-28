using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class BeatEmUpDepth2D : MonoBehaviour
{
    [Header("Depth (delante/detrás) -> Z")]
    public bool enableDepth = true;
    public float depthSpeed = 4f;

    [Tooltip("Límites de profundidad (Z). Ajusta a tu escenario.")]
    public float zMin = -3f;
    public float zMax = -0.1089621f;

    [Header("Orden en capa (para simular profundidad visual)")]
    public bool autoSortingOrder = true;
    public int sortingBase = 0;
    public int sortingPerUnit = 50; // cuanto cambia por 1 unidad de Z

    [Header("Input")]
    [Tooltip("Acción Vector2 'Move' del PlayerInputActions.")]
    public string moveActionName = "Move";

    PlayerInput input;
    InputAction move;

    SpriteRenderer[] renderers;
    float currentZ;

    public float CurrentZ => currentZ;

    void Awake()
    {
        input = GetComponent<PlayerInput>();
        move = input.actions.FindAction(moveActionName, throwIfNotFound: false);

        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        currentZ = transform.position.z;
    }

    void OnEnable()
    {
        // No forzamos actionmap; PlayerMovement2D ya lo hace.
        // Pero por seguridad, si existe la acción, la habilitamos.
        move?.Enable();
    }

    void OnDisable()
    {
        move?.Disable();
    }

    /// <summary>
    /// Llamar desde PlayerMovement2D (en FixedUpdate) para actualizar la Z de forma robusta.
    /// moveY: normalmente Move.y
    /// dt: Time.fixedDeltaTime
    /// </summary>
    public void Tick(float moveY, float dt)
    {
        if (!enableDepth) return;

        currentZ += moveY * depthSpeed * dt;
        currentZ = Mathf.Clamp(currentZ, zMin, zMax);
    }

    /// <summary>
    /// Aplica Z al transform SIN tocar X/Y (que los lleva el Rigidbody2D)
    /// </summary>
    public void ApplyZ(Transform t, float x, float y)
    {
        if (!enableDepth) return;
        t.position = new Vector3(x, y, currentZ);

        if (autoSortingOrder)
            ApplySorting();
    }

    public void ForceSetZ(float z)
    {
        currentZ = Mathf.Clamp(z, zMin, zMax);
        if (autoSortingOrder) ApplySorting();
    }

    public float ReadMoveY()
    {
        if (move == null) return 0f;
        Vector2 m = move.ReadValue<Vector2>();
        return m.y;
    }

    void ApplySorting()
    {
        // cuanto más cerca de la cámara (z más alto, menos negativo) => más delante.
        // Con z negativo: -0.1 es “delante” de -3
        int order = sortingBase + Mathf.RoundToInt((-currentZ) * sortingPerUnit);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = order;
        }
    }
}