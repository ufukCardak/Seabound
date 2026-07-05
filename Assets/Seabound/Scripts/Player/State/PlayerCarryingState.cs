using UnityEngine;

public class PlayerCarryingState : PlayerBaseState
{
    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.Carrying;

        player.Animator.SetBool("IsCarrying", true);
    }

    public override void Update(PlayerController player)
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        player.MovePlayer(moveX, moveZ);
    }

    public override void Exit(PlayerController player) 
    {
        player.Animator.SetBool("IsCarrying", false);
    }
}
