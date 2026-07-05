using Unity.Netcode;
using UnityEngine;

public class BoatController : NetworkBehaviour
{
    public Transform HelmPosition;
    public Transform PassengerSeatPosition;

    [Header("Boat Settings")]
    public float ForwardSpeed = 15f;
    public float TurnSpeed = 50f;

    [Header("State")]
    public NetworkVariable<bool> IsDriven = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkList<ulong> passengerClientIds = new NetworkList<ulong>();

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    [Header("Oar Animation")]
    public Transform[] oars;
    public float oarSwingSpeed = 1f;
    public float oarSwingAngle = 40f;
    private Quaternion[] initialOarRotations;
    private float oarPhase = 0f;
    private Vector3 lastPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (oars != null && oars.Length > 0)
        {
            initialOarRotations = new Quaternion[oars.Length];
            for (int i = 0; i < oars.Length; i++)
            {
                if (oars[i] != null)
                    initialOarRotations[i] = oars[i].localRotation;
            }
        }
    }

    private void Update()
    {
        AnimateOars();

        if (!IsOwner || !IsDriven.Value) 
            return;

        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");

        SubmitInputRpc(moveInput, turnInput);
    }

    private void AnimateOars()
    {
        if (oars == null || oars.Length == 0 || initialOarRotations == null) return;

        float speed = rb.linearVelocity.magnitude;
        
        if (speed < 0.1f)
        {
            speed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        }
        lastPosition = transform.position;
        
        if (speed > 1f)
        {
            oarPhase += speed * oarSwingSpeed * Time.deltaTime;
            float angle = Mathf.Sin(oarPhase) * oarSwingAngle;
            
            for (int i = 0; i < oars.Length; i++)
            {
                if (oars[i] != null)
                {
                    oars[i].localRotation = initialOarRotations[i] * Quaternion.Euler(0, angle, 0);
                }
            }
        }
        else
        {
            oarPhase = 0f;
            for (int i = 0; i < oars.Length; i++)
            {
                if (oars[i] != null)
                {
                    oars[i].localRotation = Quaternion.Slerp(oars[i].localRotation, initialOarRotations[i], Time.deltaTime * 3f);
                }
            }
        }
    }

    public void SetInput(float move, float turn)
    {
        if (!IsServer) return;
        moveInput = move;
        turnInput = turn;
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            MoveBoat();
            TurnBoat();
        }
    }

    private void MoveBoat()
    {
        Vector3 targetVelocity = transform.forward * moveInput * ForwardSpeed;
        
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 1.5f);
        
        rb.linearVelocity = new Vector3(smoothedVelocity.x, rb.linearVelocity.y, smoothedVelocity.z);
    }

    private void TurnBoat()
    {
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float turnMultiplier;
            if (moveInput > 0)
            {
                turnMultiplier = 1f;
            }
            else
            {
                turnMultiplier = -1f;
            }
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
        
        moveInput = 0f;
        turnInput = 0f;
    }

public void AddPassenger(ulong clientId)
    {
        if (!IsServer) 
            return;
        if (!passengerClientIds.Contains(clientId))
            passengerClientIds.Add(clientId);
    }

    public void RemovePassenger(ulong clientId)
    {
        if (!IsServer) return;
        if (passengerClientIds.Contains(clientId))
            passengerClientIds.Remove(clientId);
    }

    public bool HasPassenger(ulong clientId)
    {
        return passengerClientIds.Contains(clientId);
    }

    public int GetPassengerCount()
    {
        return passengerClientIds.Count;
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
