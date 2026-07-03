using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.Idle;
        player.StopPlayer();
    }

    public override void Update(PlayerController player)
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        if (moveX != 0 || moveZ != 0)
        {
            player.ChangeState(player.WalkingState);
        }
    }

    public override void Exit(PlayerController player) { }
}