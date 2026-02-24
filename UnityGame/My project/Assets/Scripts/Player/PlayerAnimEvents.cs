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

    public void Anim_ReloadComplete()
    {
        if (pm != null) pm.Anim_ReloadComplete();
    }

    public void Anim_PlayReloadSFX()
    {
        Debug.Log("[EVENT] Anim_PlayReloadSFX (PlayerAnimEvents) llamado");
        if (pm != null) pm.Anim_PlayReloadSFX();
        else Debug.LogWarning("[EVENT] pm es NULL");
    }

    // (si en tu animación melee tienes un event Anim_MeleeHit)
    public void Anim_MeleeHit()
    {
        if (pm != null) pm.Anim_MeleeHit();
    }
}