using UnityEngine;

public class EnemyAnimEvents : MonoBehaviour
{
    EnemyAI_Shooter enemy;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyAI_Shooter>();
    }

    // --- Disparo (compatibilidad total con clips del Player) ---
    public void FireBullet()
    {
        // En el enemy todo dispara como proyectil
        if (enemy != null) enemy.FireProjectile();
    }

    public void FireProjectile()
    {
        if (enemy != null) enemy.FireProjectile();
    }

    public void Anim_FireBullet()
    {
        // Muchos clips llaman a este nombre en vez de FireBullet
        if (enemy != null) enemy.FireProjectile();
    }

    // --- Recarga (compatibilidad; no rompe si el clip lo llama) ---
    public void Anim_ReloadComplete()
    {
        // Si tu EnemyAI_Shooter no tiene un método específico para esto,
        // lo dejamos como NO-OP para que no falle el Animation Event.
        // (Si luego quieres, lo conectamos a tu lógica de recarga)
    }

    public void Anim_PlayReloadSFX()
    {
        // Igual que en PlayerAnimEvents, pero adaptado al enemy
        Debug.Log("[EVENT] Anim_PlayReloadSFX (EnemyAnimEvents) llamado");

        if (enemy == null)
        {
            Debug.LogWarning("[EVENT] enemy es NULL");
            return;
        }

        // Si tienes reloadSFX público en EnemyAI_Shooter, lo reproducimos aquí
        if (enemy.reloadSFX != null) enemy.reloadSFX.Play();
    }

    // --- Melee (compatibilidad) ---
    public void Anim_MeleeHit()
    {
        // EnemyAI_Shooter no tiene melee por AnimEvent -> NO-OP
    }
}