using Unity.Netcode;
using UnityEngine;

public class PlayerBoatInteraction : NetworkBehaviour
{
    private PlayerController player;

    private void Awake() => player = GetComponent<PlayerController>();

public void RequestDrive(BoatController boat)
    {
        RequestDriveServerRpc(boat);
    }

    public void RequestBoardAsPassenger(BoatController boat)
    {
        RequestPassengerServerRpc(boat);
    }

    public void RequestLeave()
    {
        if (player.CurrentBoat == null) return;

        if (player.CurrentState == player.PassengerState)
            RequestLeavePassengerServerRpc(player.CurrentBoat);
        else
            RequestLeaveDriverServerRpc(player.CurrentBoat);
    }

[ServerRpc]
    private void RequestDriveServerRpc(NetworkBehaviourReference boatRef, ServerRpcParams rpc = default)
    {
        if (!boatRef.TryGet(out BoatController boat)) return;
        if (boat.IsDriven.Value) return;

        boat.StartDriving(rpc.Receive.SenderClientId);
        EnterBoatClientRpc(boat);
    }

    [ClientRpc]
    private void EnterBoatClientRpc(NetworkBehaviourReference boatRef)
    {
        if (!boatRef.TryGet(out BoatController boat)) return;
        player.SetCurrentBoat(boat);
        player.ChangeState(player.DrivingState);
    }

    [ServerRpc]
    private void RequestLeaveDriverServerRpc(NetworkBehaviourReference boatRef)
    {
        if (!boatRef.TryGet(out BoatController boat)) return;
        boat.StopDriving();
        LeaveBoatClientRpc();
    }

    [ClientRpc]
    private void LeaveBoatClientRpc()
    {
        player.ChangeState(player.IdleState);
        player.SetCurrentBoat(null);
    }

[ServerRpc]
    private void RequestPassengerServerRpc(NetworkBehaviourReference boatRef, ServerRpcParams rpc = default)
    {
        if (!boatRef.TryGet(out BoatController boat)) return;
        
        if (boat.GetPassengerCount() >= 1) return;

        boat.AddPassenger(rpc.Receive.SenderClientId);
        BoardPassengerClientRpc(boat);
    }

    [ClientRpc]
    private void BoardPassengerClientRpc(NetworkBehaviourReference boatRef)
    {
        if (!boatRef.TryGet(out BoatController boat)) return;
        player.SetCurrentBoat(boat);
        player.ChangeState(player.PassengerState);
    }

    [ServerRpc]
    private void RequestLeavePassengerServerRpc(NetworkBehaviourReference boatRef, ServerRpcParams rpc = default)
    {
        if (!boatRef.TryGet(out BoatController boat)) return;
        boat.RemovePassenger(rpc.Receive.SenderClientId);
        LeavePassengerClientRpc();
    }

    [ClientRpc]
    private void LeavePassengerClientRpc()
    {
        player.ChangeState(player.IdleState);
        player.SetCurrentBoat(null);
    }
}
