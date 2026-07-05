using UnityEngine;

public class PlayerDrivingState : PlayerBaseState
{
    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.InBoat;

        player.Animator.SetFloat("Speed", 0f);
        player.Animator.SetBool("IsSitting", true);
        player.Rb.linearVelocity = Vector3.zero;
        player.Rb.isKinematic = true;
        player.IgnoreBoatCollisions(player.CurrentBoat, true);
        
        if (player.TryGetComponent<Unity.Netcode.Components.NetworkRigidbody>(out var nrb))
            nrb.enabled = false;
        if (player.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var nt))
            nt.enabled = false;
    }

    public override void Update(PlayerController player)
    {
        if (player.CurrentBoat == null) return;

        player.transform.position = player.CurrentBoat.HelmPosition.position;
        player.transform.rotation = player.CurrentBoat.HelmPosition.rotation;
    }

    public override void Exit(PlayerController player)
    {
        player.Animator.SetBool("IsSitting", false);
        
        player.transform.position += Vector3.up * 1f;
        
        player.Rb.isKinematic = false;
        player.IgnoreBoatCollisions(player.CurrentBoat, false);
        
        if (player.TryGetComponent<Unity.Netcode.Components.NetworkRigidbody>(out var nrb))
            nrb.enabled = true;
        if (player.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var nt))
            nt.enabled = true;
    }
}
