using Unity.Netcode;
using UnityEngine;

public class PlayerInputHandler : NetworkBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleInput();
    }

    private void HandleInput()
    {
        if (player.CurrentState == player.DeadState) return;

        if (player.IsUIOpen()) return;

        bool canUseWeapons = player.CurrentState == player.IdleState || player.CurrentState == player.WalkingState;

        if (Input.GetMouseButtonDown(0) && Input.GetMouseButton(1) && canUseWeapons)
            player.Weapon.RequestFire();

        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
        {
            if (player.CurrentState == player.IdleState || player.CurrentState == player.WalkingState)
            {
                player.ChangeState(player.JumpingState);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
            HandleInteractKey();

        var inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) inventory.SetWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) inventory.SetWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) inventory.UseBandage();
        }
    }

    private void HandleInteractKey()
    {
        if (player.CurrentState == player.DrivingState || player.CurrentState == player.PassengerState)
        {
            player.BoatInteraction.RequestLeave();
            return;
        }

        if (player.CurrentState == player.CarryingState)
        {
            player.Detector.TryInteractMerchantWhileCarrying(player);
            return;
        }

        if (player.Movement.IsGrounded())
        {
            player.Detector.TryInteract(player);
        }
    }
}
