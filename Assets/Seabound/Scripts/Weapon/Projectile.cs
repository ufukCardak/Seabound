using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public float lifeTime = 3f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke(nameof(DestroyProjectile), lifeTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) 
            return;

        Debug.Log("collision " + collision.gameObject.name);

        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(25);
        }

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(10);
        }
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}