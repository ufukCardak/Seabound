using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("State Management")]
    private PlayerBaseState currentState;

    public NetworkVariable<PlayerState> SyncState = new NetworkVariable<PlayerState>(
        PlayerState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    [Header("Components")]
    public Rigidbody Rb;
    public CapsuleCollider Capsule;
    public Animator animator;

    [Header("Movement Settings")]
    public float Speed = 5f;
    public float RotationSpeed = 10f;
    public float JumpForce = 5f;

    [Header("Grounded Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Boat Interaction")]
    public float InteractRange = 4f;
    public BoatController currentBoat;

    [Header("Carrying Settings")]
    public Transform HoldPoint;
    public ChestController currentChest;

    [Header("Combat Settings")]
    public Transform FirePoint;
    public GameObject BulletPrefab;
    public float BulletSpeed = 20f;

    private Transform _mainCameraTransform;

    public PlayerIdleState IdleState = new PlayerIdleState();
    public PlayerWalkingState WalkingState = new PlayerWalkingState();
    public PlayerJumpingState JumpingState = new PlayerJumpingState();
    public PlayerDrivingState DrivingState = new PlayerDrivingState();
    public PlayerCarryingState CarryingState = new PlayerCarryingState();
    public override void OnNetworkSpawn()
    {
        currentState = IdleState;

        if (!IsOwner)
            return;

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnTransform = spawnPoints[randomIndex].transform;

            transform.position = spawnTransform.position;
            transform.rotation = spawnTransform.rotation;

            if (Rb != null)
            {
                Rb.position = spawnTransform.position;
                Rb.rotation = spawnTransform.rotation;
            }
        }

        currentState.Enter(this);
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }

        var camController = GameObject.FindAnyObjectByType<CameraController>();
        if (camController != null)
        {
            camController.Target = this.transform;
        }
    }

    private void Update()
    {
        if (!IsOwner) 
            return;

        currentState.Update(this);

        Vector3 horizontalVelocity = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("IsGrounded", IsGrounded());

        if (Input.GetMouseButtonDown(0))
        {
            FireServerRpc(FirePoint.position, FirePoint.rotation);
        }

        if (Input.GetButtonDown("Jump") && IsGrounded() && currentState != JumpingState)
        {
            ChangeState(JumpingState);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentState == DrivingState)
            {
                LeaveBoat();
            }
            else if (currentState == CarryingState)
            {
                DropChest();
            }
            else if (IsGrounded())
            {
                TryInteract();
            }
        }
    }

    public void ChangeState(PlayerBaseState newState)
    {
        if (currentState == newState)
            return;

        currentState.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }
    public void TakeDamage(int damage)
    {
        if (!IsServer) 
            return;

        Health.Value -= damage;

        if (Health.Value <= 0)
        {
            Debug.Log("Oyuncu Öldü! (İleride buraya yeniden doğma eklenebilir)");
        }
    }
    [Rpc(SendTo.Server)]
    private void FireServerRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        GameObject bullet = Instantiate(BulletPrefab, spawnPos, spawnRot);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = bullet.transform.forward * BulletSpeed;

        bullet.GetComponent<NetworkObject>().Spawn();
    }
    public void ApplyJumpForce()
    {
        Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
        Rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);

        animator.SetTrigger("JumpTrigger");
    }
    public bool IsGrounded()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        bool hit = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);

        Debug.DrawRay(rayOrigin, Vector3.down * (groundCheckDistance + 0.1f), hit ? Color.green : Color.red);

        return hit;
    }
    public void MovePlayer(float moveX, float moveZ)
    {
        Vector3 camForward = _mainCameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = _mainCameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDirection = (camRight * moveX + camForward * moveZ).normalized;
        Vector3 targetVelocity = moveDirection * Speed;
        Rb.linearVelocity = new Vector3(targetVelocity.x, Rb.linearVelocity.y, targetVelocity.z);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }
    }
    private void TryInteract()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, InteractRange);
        foreach (var hit in hitColliders)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }
            BoatController boat = hit.GetComponentInParent<BoatController>();
            if (boat != null && !boat.IsDriven.Value)
            {
                RequestDriveBoatServerRpc(boat.GetComponent<NetworkObject>().NetworkObjectId);
                return;
            }
        }
    }
    public void SellCarriedChestServer()
    {
        if (!IsServer) 
            return;

        if (HoldPoint.childCount > 0)
        {
            NetworkObject chestNetObj = HoldPoint.GetChild(0).GetComponent<NetworkObject>();
            if (chestNetObj != null)
            {
                chestNetObj.Despawn();
            }
        }

        ResetPlayerStateRpc();
    }

    [Rpc(SendTo.Everyone)]
    public void ResetPlayerStateRpc()
    {
        ChangeState(IdleState);
    }
    public void DropChest()
    {
        if (currentChest != null)
        {
            Vector3 dropPos = transform.position + transform.forward * 1.5f;

            RequestDropChestServerRpc(currentChest.GetComponent<NetworkObject>().NetworkObjectId, dropPos);

            currentChest = null;
            ChangeState(IdleState);
        }
    }

    [ServerRpc]
    public void RequestPickUpChestServerRpc(ulong chestId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(chestId, out NetworkObject chestNetObj))
        {
            ChestController chest = chestNetObj.GetComponent<ChestController>();
            if (chest != null && !chest.IsCarried.Value)
            {
                chest.PickUp(this);

                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { rpcParams.Receive.SenderClientId } }
                };
                PickUpChestClientRpc(chestId, clientRpcParams);
            }
        }
    }

    [ClientRpc]
    private void PickUpChestClientRpc(ulong chestId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(chestId, out NetworkObject chestNetObj))
        {
            currentChest = chestNetObj.GetComponent<ChestController>();
            ChangeState(CarryingState);
        }
    }

    [ServerRpc]
    public void RequestDropChestServerRpc(ulong chestId, Vector3 dropPos)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(chestId, out NetworkObject chestNetObj))
        {
            ChestController chest = chestNetObj.GetComponent<ChestController>();
            if (chest != null)
            {
                chest.Drop(dropPos);
            }
        }
    }
    public void StopPlayer()
    {
        Rb.linearVelocity = new Vector3(0f, Rb.linearVelocity.y, 0f);
    }
    private void TryEnterBoat()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, InteractRange);
        foreach (var hit in hitColliders)
        {
            BoatController boat = hit.GetComponentInParent<BoatController>();
            if (boat != null && !boat.IsDriven.Value)
            {
                RequestDriveBoatServerRpc(boat.GetComponent<NetworkObject>().NetworkObjectId);
                break;
            }
        }
    }

    private void LeaveBoat()
    {
        if (currentBoat != null)
        {
            RequestLeaveBoatServerRpc(currentBoat.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [ServerRpc]
    private void RequestDriveBoatServerRpc(ulong boatNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(boatNetworkObjectId, out NetworkObject boatNetObj))
        {
            BoatController boat = boatNetObj.GetComponent<BoatController>();
            if (boat != null && !boat.IsDriven.Value)
            {
                boat.StartDriving(rpcParams.Receive.SenderClientId);

                EnterBoatClientRpc(boatNetworkObjectId);
            }
        }
    }

    [ServerRpc]
    private void RequestLeaveBoatServerRpc(ulong boatNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(boatNetworkObjectId, out NetworkObject boatNetObj))
        {
            BoatController boat = boatNetObj.GetComponent<BoatController>();
            if (boat != null)
            {
                boat.StopDriving();
                LeaveBoatClientRpc();
            }
        }
    }

    [ClientRpc]
    private void EnterBoatClientRpc(ulong boatNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(boatNetworkObjectId, out NetworkObject boatNetObj))
        {
            currentBoat = boatNetObj.GetComponent<BoatController>();
            ChangeState(DrivingState);
        }
    }

    [ClientRpc]
    private void LeaveBoatClientRpc()
    {
        currentBoat = null;
        ChangeState(IdleState);
    }
}