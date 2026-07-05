using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damageAmount = 25;

public override void OnNetworkSpawn()
    {
        if (IsServer)
            Invoke(nameof(Despawn), lifeTime);
    }

private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) 
            return;

        var damageable = collision.gameObject.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
            
            if (OwnerClientId != NetworkManager.ServerClientId || !IsHost) 
            {
                ShowHitmarkerRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
            }
            else
            {
                ShowHitmarkerRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
            }

            Transform attacker = GetPlayerByClientId(OwnerClientId);
            if (attacker != null)
            {
                var ai = collision.gameObject.GetComponentInParent<EnemyAIBase>();
                if (ai != null)
                {
                    ai.OnAttackedBy(attacker);
                }
                
                var boat = collision.gameObject.GetComponentInParent<BoatController>();
                if (boat != null)
                {
                    var allGuards = FindObjectsByType<EnemyGuardAI>(FindObjectsSortMode.None);
                    foreach (var g in allGuards)
                    {
                        if (g.AssignedBoat == boat)
                            g.OnAttackedBy(attacker);
                    }
                }
            }
        }

        Despawn();
    }

    private Transform GetPlayerByClientId(ulong clientId)
    {
        foreach (var p in EnemyTargetRegistry.Players)
        {
            if (p != null && p.TryGetComponent<NetworkObject>(out var no))
            {
                if (no.OwnerClientId == clientId) return p;
            }
        }
        return null;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowHitmarkerRpc(RpcParams rpcParams)
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowHitmarker();
    }

private void Despawn()
    {
        NetworkObject.Despawn();
    }
}
