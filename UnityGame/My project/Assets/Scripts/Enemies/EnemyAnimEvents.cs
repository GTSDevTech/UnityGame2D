using UnityEngine;

public class EnemyAnimEvents : MonoBehaviour
{
    EnemyAI_Shooter enemy;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyAI_Shooter>();
    }

    // Tiene que llamarse EXACTO como tu Animation Event
    public void FireProjectile()
    {
        if (enemy != null) enemy.FireProjectile();
    }

    // Por si tu clip llama a FireBullet o Anim_FireBullet, etc.
    public void FireBullet()
    {
        if (enemy != null) enemy.FireProjectile();
    }
}
