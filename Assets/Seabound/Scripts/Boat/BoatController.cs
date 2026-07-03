using Unity.Netcode;
using UnityEngine;

public class BoatController : NetworkBehaviour
{
    public Transform HelmPosition;

    [Header("Boat Settings")]
    public float ForwardSpeed = 15f;
    public float TurnSpeed = 50f;

    [Header("State")]
    public NetworkVariable<bool> IsDriven = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsOwner || !IsDriven.Value) 
            return;

        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");

        SubmitInputRpc(moveInput, turnInput);
    }

    private void FixedUpdate()
    {
        if (IsServer && IsDriven.Value)
        {
            MoveBoat();
            TurnBoat();
        }
    }

    private void MoveBoat()
    {
        Vector3 force = transform.forward * moveInput * ForwardSpeed;
        rb.AddForce(force, ForceMode.Acceleration);

        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVelocity.magnitude > ForwardSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * ForwardSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void TurnBoat()
    {
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float turnMultiplier = moveInput > 0 ? 1f : -1f;
            Quaternion turnRotation = Quaternion.Euler(0f, turnInput * TurnSpeed * turnMultiplier * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }

    public void StartDriving(ulong clientId)
    {
        if (!IsServer) 
            return;

        GetComponent<NetworkObject>().ChangeOwnership(clientId);
        IsDriven.Value = true;
    }

    public void StopDriving()
    {
        if (!IsServer) 
            return;

        IsDriven.Value = false;
        GetComponent<NetworkObject>().RemoveOwnership();
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitInputRpc(float move, float turn, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        moveInput = move;
        turnInput = turn;
    }
}