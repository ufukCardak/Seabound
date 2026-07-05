using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class TreasureSellZone : NetworkBehaviour
{
    private readonly HashSet<ulong> soldChestIds = new HashSet<ulong>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) 
            return;

        var chest = other.GetComponent<ChestController>();
        if (chest == null || chest.NetworkObject == null) return;

        ulong chestId = chest.NetworkObjectId;
        if (!soldChestIds.Add(chestId)) 
            return;

        var player = chest.LastCarryingPlayer;
        if (player == null) player = chest.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            if (chest.IsCarried.Value)
            {
                var chestInteraction = player.GetComponent<PlayerChestInteraction>();
                if (chestInteraction != null)
                    chestInteraction.SellCarried();
            }
            else
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) inv.AddPersonalGold(100);
            }
        }

        chest.NetworkObject.Despawn();
    }
}
