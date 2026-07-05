using UnityEngine;

public class PlayerPassengerState : PlayerBaseState
{
    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.Passenger;

        player.StopPlayer();
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

        Transform seat = player.CurrentBoat.PassengerSeatPosition;
        if (seat != null)
        {
            player.transform.position = seat.position;
            player.transform.rotation = seat.rotation;
        }
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
