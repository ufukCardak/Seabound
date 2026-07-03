using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic; 

public class TreasureSellZone : NetworkBehaviour
{
    private List<ulong> soldChestIds = new List<ulong>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        ChestController chest = other.GetComponent<ChestController>();

        if (chest != null && chest.NetworkObject != null)
        {
            ulong chestId = chest.NetworkObjectId;

            if (soldChestIds.Contains(chestId))
                return;

            soldChestIds.Add(chestId);

            GameManager.Instance.AddGold(100);

            PlayerController carryingPlayer = chest.GetComponentInParent<PlayerController>();
            if (carryingPlayer != null)
            {
                carryingPlayer.ResetPlayerStateRpc();
            }

            chest.NetworkObject.Despawn();
        }
    }
}