using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ChestController : NetworkBehaviour, IInteractable
{
    public NetworkVariable<bool> IsCarried = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;
    private Collider col;
    private NetworkTransform netTransform;

    private PlayerController carryingPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        netTransform = GetComponent<NetworkTransform>();
    }

    private void Update()
    {
        if (IsCarried.Value && carryingPlayer != null)
        {
            transform.position = carryingPlayer.HoldPoint.position;
            transform.rotation = carryingPlayer.HoldPoint.rotation;
        }
    }

    public void Interact(PlayerController player)
    {
        if (!IsCarried.Value)
        {
            player.RequestPickUpChestServerRpc(GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    public void PickUp(PlayerController player)
    {
        if (!IsServer) return;

        IsCarried.Value = true;

        LockToPlayerClientRpc(player.NetworkObjectId);
    }

    [ClientRpc]
    private void LockToPlayerClientRpc(ulong playerNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject playerObj))
        {
            carryingPlayer = playerObj.GetComponent<PlayerController>();

            if (netTransform != null) 
                netTransform.enabled = false;

            rb.isKinematic = true;
            col.enabled = false;
        }
    }

    public void Drop(Vector3 dropPosition)
    {
        if (!IsServer) return;

        IsCarried.Value = false;

        UnlockFromPlayerClientRpc(dropPosition);
    }

    [ClientRpc]
    private void UnlockFromPlayerClientRpc(Vector3 dropPosition)
    {
        carryingPlayer = null;

        if (netTransform != null) netTransform.enabled = true;
        rb.isKinematic = false;
        col.enabled = true;

        transform.position = dropPosition;
    }
}