using UnityEngine;

public class PlayerCarryingState : PlayerBaseState
{
    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.Walking;
    }

    public override void Update(PlayerController player)
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        player.MovePlayer(moveX, moveZ);

        if (Input.GetKeyDown(KeyCode.G))
        {
            player.DropChest();
        }
    }

    public override void Exit(PlayerController player)
    {
    }
}