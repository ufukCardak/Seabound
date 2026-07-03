using UnityEngine;

public class PlayerWalkingState : PlayerBaseState
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

        if (moveX == 0 && moveZ == 0)
        {
            player.ChangeState(player.IdleState);
        }
    }

    public override void Exit(PlayerController player) { }
}