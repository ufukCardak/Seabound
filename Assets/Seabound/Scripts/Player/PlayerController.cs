using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Cinemachine;

public class PlayerController : NetworkBehaviour, IDamageable
{

    public NetworkVariable<int> Health = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int CurrentHealth => Health.Value;
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    public NetworkVariable<PlayerState> SyncState = new NetworkVariable<PlayerState>(
        PlayerState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

[Header("Components")]
    public Rigidbody Rb;
    public CapsuleCollider Capsule;
    public Animator Animator;

    [Header("Carry Point")]
    public Transform HoldPoint;

public PlayerMovement Movement;
    public WeaponComponent Weapon;
    public PlayerBoatInteraction BoatInteraction;
    public PlayerChestInteraction ChestInteraction;
    public PlayerInteraction Detector;
    public LayerMask interactLayer = 1 << 8;

public readonly PlayerIdleState IdleState = new PlayerIdleState();
    public readonly PlayerWalkingState WalkingState = new PlayerWalkingState();
    public readonly PlayerJumpingState JumpingState = new PlayerJumpingState();
    public readonly PlayerDrivingState DrivingState = new PlayerDrivingState();
    public readonly PlayerCarryingState CarryingState = new PlayerCarryingState();
    public readonly PlayerPassengerState PassengerState = new PlayerPassengerState();
    public readonly PlayerDeadState DeadState = new PlayerDeadState();

    public PlayerBaseState CurrentState;

public BoatController CurrentBoat;
    public ChestController CurrentChest;

private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Weapon = GetComponent<WeaponComponent>();
        BoatInteraction = GetComponent<PlayerBoatInteraction>();
        ChestInteraction = GetComponent<PlayerChestInteraction>();
        Detector = GetComponent<PlayerInteraction>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.Init(this);
            }
        }

        var inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.OnWeaponChanged += Weapon.EquipWeaponByIndex;
            Weapon.EquipWeaponByIndex(inventory.EquippedWeaponIndex.Value);
        }

        ChangeState(IdleState);

        EnemyTargetRegistry.Register(transform);

        if (!IsOwner) 
            return;

        CurrentState.Enter(this);

        Transform spawnPoint = SpawnManager.Instance != null
            ? SpawnManager.Instance.GetRandomSpawnPoint()
            : null;

        if (spawnPoint != null)
        {
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            Rb.position = spawnPoint.position; 
            Rb.rotation = spawnPoint.rotation;
        }

        Movement.SetCamera(Camera.main.transform);

        var cam = FindAnyObjectByType<CameraController>();
        cam.Target = transform;
    }

    public override void OnNetworkDespawn()
    {
        EnemyTargetRegistry.Unregister(transform);
        
        var inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.OnWeaponChanged -= Weapon.EquipWeaponByIndex;
        }
    }

    private void Update()
    {
        if (CurrentState == DrivingState && CurrentBoat != null)
        {
            transform.position = CurrentBoat.HelmPosition.position;
            transform.rotation = CurrentBoat.HelmPosition.rotation;
        }
        else if (CurrentState == PassengerState && CurrentBoat != null)
        {
            var seat = CurrentBoat.PassengerSeatPosition;
            if (seat != null)
            {
                transform.position = seat.position;
                transform.rotation = seat.rotation;
            }
        }

        if (!IsOwner) return;

        CurrentState.Update(this);

        Animator.SetFloat("Speed", Movement.HorizontalSpeed);
        Animator.SetBool("IsGrounded", Movement.IsGrounded());

        var aimIK = GetComponent<PlayerAimIK>();
        if (aimIK != null && Camera.main != null)
        {
            Vector3 targetPos = Camera.main.transform.position + Camera.main.transform.forward * 50f;
            bool canAim = CurrentState == IdleState || CurrentState == WalkingState;
            bool isAiming = Input.GetMouseButton(1) && canAim;
            aimIK.SetAimInput(targetPos, isAiming);

            if (isAiming)
            {
                Movement.SpeedMultiplier = 0.5f;
            }
            else
            {

                Movement.SpeedMultiplier = (CurrentState == CarryingState) ? 0.5f : 1f;
            }
        }
    }

public bool IsUIOpen()
    {
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.shopPanel != null && ShopUIManager.Instance.shopPanel.activeSelf) return true;
        if (HUDManager.Instance != null && HUDManager.Instance.inventoryPanel != null && HUDManager.Instance.inventoryPanel.activeSelf) return true;
        return false;
    }

public void ChangeState(PlayerBaseState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit(this);
        CurrentState = newState;
        CurrentState.Enter(this);
    }

public void SetCurrentBoat(BoatController boat) => CurrentBoat = boat;
    public void SetCurrentChest(ChestController chest) => CurrentChest = chest;

public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        Health.Value = Mathf.Max(0, Health.Value - damage);

        if (Health.Value <= 0)
            TriggerDeathClientRpc();
        else
            TakeDamageClientRpc();
    }

    [ClientRpc]
    private void TakeDamageClientRpc()
    {
        if (IsOwner && CameraController.Instance != null)
        {
            CameraController.Instance.AddDamageShake();
        }
    }

    [ClientRpc]
    private void TriggerDeathClientRpc() => ChangeState(DeadState);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RespawnServerRpc()
    {
        Health.Value = 100;

        Transform sp = SpawnManager.Instance.GetRandomSpawnPoint();
        Vector3 pos;
        Quaternion rot;

        if (sp != null)
        {
            pos = sp.position;
            rot = sp.rotation;
        }
        else
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
        }

        TeleportClientRpc(pos, rot);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
        Rb.position = pos; 
        Rb.rotation = rot; 
        Rb.linearVelocity = Vector3.zero;
        ChangeState(IdleState);
    }

public void StopPlayer() => Movement.Stop();
    public void ApplyJumpForce() { Movement.TryJump(); Animator.SetTrigger("JumpTrigger"); }
    public bool IsGrounded() => Movement.IsGrounded();

    public void MovePlayer(float x, float z) => Movement.Move(x, z);

    public void IgnoreBoatCollisions(BoatController boat, bool ignore)
    {
        if (boat == null || Capsule == null) return;
        var boatColliders = boat.GetComponentsInChildren<Collider>();
        foreach (var bc in boatColliders)
        {
            Physics.IgnoreCollision(Capsule, bc, ignore);
        }
    }

}
