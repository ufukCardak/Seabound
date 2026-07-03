using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    private float _jumpTimer;
    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.Jumping;
        player.ApplyJumpForce();

        _jumpTimer = 1;
    }

    public override void Update(PlayerController player)
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        player.MovePlayer(moveX, moveZ);

        if (_jumpTimer > 0)
        {
            _jumpTimer -= Time.deltaTime;
            return;
        }

        if (player.Rb.linearVelocity.y < -0.1f)
        {
            if (player.IsGrounded())
            {
                player.ChangeState(player.IdleState);
            }
        }
        else if (player.IsGrounded() && player.Rb.linearVelocity.y <= 0.1f)
        {
            if (moveX == 0 && moveZ == 0)
                player.ChangeState(player.IdleState);
            else
                player.ChangeState(player.WalkingState);
        }
    }

    public override void Exit(PlayerController player) { }
}