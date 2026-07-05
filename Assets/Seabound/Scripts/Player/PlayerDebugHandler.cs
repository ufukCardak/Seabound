using Unity.Netcode;
using UnityEngine;

public enum DebugCheatType { GiveGold, GiveAmmo, RestoreHealth, SpawnChest, SpawnBoat, KillEnemies }

public class PlayerDebugHandler : NetworkBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDebugCheatServerRpc(DebugCheatType cheatType)
    {
        if (DebugManager.Instance == null) return;
        
        if (OwnerClientId != NetworkManager.ServerClientId && !DebugManager.Instance.allowClientDebug) return;

        switch (cheatType)
        {
            case DebugCheatType.GiveGold:
                var inv = GetComponent<PlayerInventory>();
                if (inv != null) inv.AddPersonalGold(1000);
                break;
            case DebugCheatType.GiveAmmo:
                var inv2 = GetComponent<PlayerInventory>();
                if (inv2 != null)
                {
                    inv2.Ammo.Value += 100;
                    inv2.BlunderbussAmmo.Value += 50;
                    inv2.Bandages.Value += 10;
                }
                break;
            case DebugCheatType.RestoreHealth:
                player.Health.Value = HUDManager.Instance != null ? HUDManager.Instance.maxHp : 100;
                break;
            case DebugCheatType.SpawnChest:
                if (DebugManager.Instance.chestPrefab != null)
                {
                    Vector3 spawnPos = transform.position + transform.forward * 2f + Vector3.up * 1f;
                    GameObject chest = Instantiate(DebugManager.Instance.chestPrefab, spawnPos, Quaternion.identity);
                    chest.GetComponent<NetworkObject>().Spawn();
                }
                break;
            case DebugCheatType.SpawnBoat:
                if (DebugManager.Instance.enemyBoatPrefab != null)
                {
                    Vector2 rnd = UnityEngine.Random.insideUnitCircle.normalized * 30f;
                    Vector3 spawnPos = new Vector3(transform.position.x + rnd.x, 0f, transform.position.z + rnd.y);
                    GameObject boat = Instantiate(DebugManager.Instance.enemyBoatPrefab, spawnPos, Quaternion.identity);
                    boat.GetComponent<NetworkObject>().Spawn();
                }
                break;
            case DebugCheatType.KillEnemies:
                var aiBases = FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None);
                foreach(var ai in aiBases)
                {
                    var damageable = ai.GetComponent<IDamageable>();
                    if (damageable != null) damageable.TakeDamage(9999);
                }
                break;
        }
    }
}
