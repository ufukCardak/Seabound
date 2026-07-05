using Unity.Netcode;
using UnityEngine;
using PrimeTween;

public class PlayerChestInteraction : NetworkBehaviour
{
    private PlayerController player;

    private void Awake() => player = GetComponent<PlayerController>();

    public void RequestPickUp(ChestController chest)
    {
        RequestPickUpServerRpc(chest);
    }

    public void RequestDrop()
    {
        if (player.CurrentChest == null) return;
        Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f;
        RequestDropServerRpc(player.CurrentChest, dropPos);
        player.SetCurrentChest(null);
        player.Movement.SpeedMultiplier = 1f;
        player.ChangeState(player.IdleState);
    }

    public void SellCarried()
    {
        SellCarriedServerRpc();
    }

    [ServerRpc]
    private void SellCarriedServerRpc()
    {
        SellCarriedInternal();
    }

    [ServerRpc]
    private void RequestPickUpServerRpc(NetworkBehaviourReference chestRef, ServerRpcParams rpc = default)
    {
        if (!chestRef.TryGet(out ChestController chest)) return;
        if (chest.IsCarried.Value) return;

        chest.PickUp(player);
        player.SetCurrentChest(chest);
        
        if (player.TryGetComponent<PlayerWeaponVisuals>(out var visuals))
        {
            visuals.IsWeaponVisible.Value = false;
        }

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { rpc.Receive.SenderClientId }
            }
        };
        ConfirmPickUpClientRpc(chest, sendParams);
    }

    [ClientRpc]
    private void ConfirmPickUpClientRpc(NetworkBehaviourReference chestRef, ClientRpcParams _ = default)
    {
        if (!chestRef.TryGet(out ChestController chest)) return;
        player.SetCurrentChest(chest);
        player.Movement.SpeedMultiplier = 0.5f;
        player.ChangeState(player.CarryingState);
        
        Tween.PunchLocalPosition(player.HoldPoint, new Vector3(0f, -0.3f, 0f), 0.35f);
    }

    [ServerRpc]
    private void RequestDropServerRpc(NetworkBehaviourReference chestRef, Vector3 dropPos)
    {
        if (!chestRef.TryGet(out ChestController chest)) return;
        chest.Drop(dropPos);
        player.SetCurrentChest(null);
        
        if (player.TryGetComponent<PlayerWeaponVisuals>(out var visuals))
        {
            visuals.IsWeaponVisible.Value = true;
        }
    }

    private void SellCarriedInternal()
    {
        if (player.CurrentChest != null)
        {
            var no = player.CurrentChest.GetComponent<NetworkObject>();
            no?.Despawn();
        }

        var inv = GetComponent<PlayerInventory>();
        inv?.AddPersonalGold(100);

        player.SetCurrentChest(null);
        
        if (player.TryGetComponent<PlayerWeaponVisuals>(out var visuals))
        {
            visuals.IsWeaponVisible.Value = true;
        }
        
        ResetStateClientRpc();
    }

    [ClientRpc]
    private void ResetStateClientRpc()
    {
        player.Movement.SpeedMultiplier = 1f;
        player.ChangeState(player.IdleState);
    }
}
