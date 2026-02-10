using UnityEngine;

public class PlayerAnimEvents : MonoBehaviour
{
    PlayerMovement2D pm;

    void Awake()
    {
        pm = GetComponentInParent<PlayerMovement2D>();
    }

    public void FireBullet()
    {
        if (pm != null) pm.FireBullet();
    }

    public void FireProjectile()
    {
        if (pm != null) pm.FireProjectile();
    }

    public void Anim_FireBullet()
    {
        if (pm != null) pm.Anim_FireBullet();
    }
}
