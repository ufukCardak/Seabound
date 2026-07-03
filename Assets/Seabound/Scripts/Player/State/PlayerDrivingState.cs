using UnityEngine;

public class PlayerDrivingState : PlayerBaseState
{
    public override void Enter(PlayerController player)
    {
        player.animator.SetFloat("Speed", 0f);

        player.Rb.linearVelocity = Vector3.zero;
        player.Rb.isKinematic = true;
        player.Capsule.enabled = false;
    }

    public override void Update(PlayerController player)
    {
        player.transform.position = player.currentBoat.HelmPosition.position;
        player.transform.rotation = player.currentBoat.HelmPosition.rotation;
    }

    public override void Exit(PlayerController player)
    {
        player.Rb.isKinematic = false;
        player.Capsule.enabled = true;
    }
}