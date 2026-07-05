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
    public PlayerController LastCarryingPlayer;

    public NetworkVariable<ulong> ParentBoatNetId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [HideInInspector] public Transform ParentBoat;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        netTransform = GetComponent<NetworkTransform>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ParentBoatNetId.OnValueChanged += OnParentBoatChanged;
        
        if (ParentBoatNetId.Value != 0)
        {
            OnParentBoatChanged(0, ParentBoatNetId.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        ParentBoatNetId.OnValueChanged -= OnParentBoatChanged;
        base.OnNetworkDespawn();
    }

    private void OnParentBoatChanged(ulong oldVal, ulong newVal)
    {
        if (newVal != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newVal, out NetworkObject boatObj))
        {
            ParentBoat = boatObj.transform;
            if (rb != null) rb.isKinematic = true;
            
            if (col != null)
            {
                var boatColliders = ParentBoat.GetComponentsInChildren<Collider>();
                foreach (var bc in boatColliders) Physics.IgnoreCollision(col, bc, true);
            }
        }
        else
        {
            if (ParentBoat != null && col != null)
            {
                var boatColliders = ParentBoat.GetComponentsInChildren<Collider>();
                foreach (var bc in boatColliders) Physics.IgnoreCollision(col, bc, false);
            }
            ParentBoat = null;
            if (rb != null && !IsCarried.Value) rb.isKinematic = false;
        }
    }

    private void Update()
    {
        if (IsCarried.Value && carryingPlayer != null)
        {
            if (netTransform != null && netTransform.enabled) netTransform.enabled = false;
            transform.position = carryingPlayer.HoldPoint.position;
            transform.rotation = carryingPlayer.HoldPoint.rotation;
        }
        else if (!IsCarried.Value)
        {
            if (netTransform != null && !netTransform.enabled) netTransform.enabled = true;
        }
    }

    public void AttachToBoat(Transform boat)
    {
        if (!IsServer) return;

        if (boat.TryGetComponent<NetworkObject>(out var boatNet))
        {
            ParentBoatNetId.Value = boatNet.NetworkObjectId;
            GetComponent<NetworkObject>().TrySetParent(boatNet, true);
        }
    }

    public void Interact(PlayerController player)
    {
        if (!IsCarried.Value)
            player.GetComponent<PlayerChestInteraction>()?.RequestPickUp(this);
    }

    public string GetInteractText()
    {
        return "[F] Pick up Chest";
    }

    public void PickUp(PlayerController player)
    {
        if (!IsServer) return;

        if (ParentBoat != null)
        {
            var guards = FindObjectsByType<EnemyGuardAI>(FindObjectsSortMode.None);
            foreach(var g in guards)
            {
                if (g.AssignedBoat == ParentBoat.GetComponent<BoatController>() && !g.IsAggroed)
                {
                    g.OnAttackedBy(player.transform);
                }
            }
        }

        IsCarried.Value = true;
        LockToPlayerClientRpc(player.NetworkObjectId);
    }

    [ClientRpc]
    private void LockToPlayerClientRpc(ulong playerNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject playerObj))
        {
            carryingPlayer = playerObj.GetComponent<PlayerController>();
            if (carryingPlayer != null) LastCarryingPlayer = carryingPlayer;
            
            rb.isKinematic = true;
            col.enabled = false;
        }
    }

    public void Drop(Vector3 dropPosition)
    {
        if (!IsServer) return;

        IsCarried.Value = false;
        
        ulong boatId = 0;
        if (Physics.Raycast(dropPosition + Vector3.up, Vector3.down, out RaycastHit hit, 5f))
        {
            var boat = hit.collider.GetComponentInParent<BoatController>();
            if (boat != null && boat.TryGetComponent<NetworkObject>(out var no))
            {
                boatId = no.NetworkObjectId;
            }
        }
        
        UnlockFromPlayerClientRpc();
        
        if (boatId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(boatId, out NetworkObject boatObj))
        {
            AttachToBoat(boatObj.transform);
        }
        else
        {
            ParentBoatNetId.Value = 0;
            GetComponent<NetworkObject>().TryRemoveParent(true);
        }
    }

    [ClientRpc]
    private void UnlockFromPlayerClientRpc()
    {
        carryingPlayer = null;
        rb.isKinematic = false;
        col.enabled = true;
    }
}
