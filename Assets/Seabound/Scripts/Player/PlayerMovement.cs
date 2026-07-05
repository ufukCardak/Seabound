using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    private Rigidbody rb;
    private Transform cameraTransform;

    [HideInInspector]
    public float SpeedMultiplier = 1f;

    private void Awake() => rb = GetComponent<Rigidbody>();

    public void SetCamera(Transform cameraTransform)
    {
        this.cameraTransform = cameraTransform;
    }

public void Move(float moveX, float moveZ)
    {
        if (cameraTransform == null) return;

        var controller = GetComponent<PlayerController>();
        if (controller != null && controller.IsUIOpen())
        {
            moveX = 0f;
            moveZ = 0f;
        }

        Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;

        Vector3 moveDir = (camRight * moveX + camForward * moveZ).normalized;
        Vector3 targetVel = moveDir * (speed * SpeedMultiplier);

        rb.linearVelocity = new Vector3(targetVel.x, rb.linearVelocity.y, targetVel.z);

        rb.linearVelocity = new Vector3(targetVel.x, rb.linearVelocity.y, targetVel.z);

        bool isAiming = Input.GetMouseButton(1);

        if (isAiming)
        {
            Quaternion targetRot = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * 2f * Time.deltaTime);
        }
        else if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    public void Stop()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public bool TryJump()
    {
        if (!IsGrounded()) return false;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        if (CameraController.Instance != null)
            CameraController.Instance.AddJumpJuice();
            
        return true;
    }

    public bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float dist = groundCheckDistance + 0.1f;
        bool hit = Physics.Raycast(origin, Vector3.down, dist, groundLayer);

        if (hit)
        {
            Debug.DrawRay(origin, Vector3.down * dist, Color.green);
        }
        else
        {
            Debug.DrawRay(origin, Vector3.down * dist, Color.red);
        }
        return hit;
    }

    public float HorizontalSpeed =>
        new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
}
