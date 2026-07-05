using UnityEngine;
using PrimeTween;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    [Header("Target Settings")]
    public Transform Target;

    [Header("Distance Settings")]
    public float NormalDistance = 3f;
    public float ZoomDistance = 1.2f;
    public float NormalHeight = 1.5f;
    public float ZoomHeight = 1.6f;
    public float NormalSideOffset = 0.5f;
    public float ZoomSideOffset = 0.6f;
    public float ZoomSmoothSpeed = 10f;

    [Header("Mouse Settings")]
    public float NormalSensitivity = 2f;
    public float ZoomSensitivity = 0.8f;
    public float ClampAngleMin = -20f;
    public float ClampAngleMax = 60f;

    private float currentDistance;
    private float currentHeight;
    private float currentSideOffset;
    private float currentSensitivity;

    [Header("Smoothness")]
    public float SmoothTime = 15f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    [Header("Juice offsets")]
    public float recoilOffset = 0f;
    public float fovOffset = 0f;
    public Vector3 shakeOffset = Vector3.zero;
    private Camera cam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        Vector3 currentAngles = transform.eulerAngles;
        rotationY = currentAngles.y;
        rotationX = currentAngles.x;

        currentDistance = NormalDistance;
        currentHeight = NormalHeight;
        currentSideOffset = NormalSideOffset;
        currentSensitivity = NormalSensitivity;
    }

    private void LateUpdate()
    {
        if (Target == null) 
            return;

        bool isZooming = Input.GetMouseButton(1);

        float targetDistance = isZooming ? ZoomDistance : NormalDistance;
        float targetHeight = isZooming ? ZoomHeight : NormalHeight;
        float targetSideOffset = isZooming ? ZoomSideOffset : NormalSideOffset;
        float targetSensitivity = isZooming ? ZoomSensitivity : NormalSensitivity;

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * ZoomSmoothSpeed);
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * ZoomSmoothSpeed);
        currentSideOffset = Mathf.Lerp(currentSideOffset, targetSideOffset, Time.deltaTime * ZoomSmoothSpeed);
        currentSensitivity = Mathf.Lerp(currentSensitivity, targetSensitivity, Time.deltaTime * ZoomSmoothSpeed * 2f);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            rotationY += Input.GetAxis("Mouse X") * currentSensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * currentSensitivity;
        }
        
        rotationX = Mathf.Clamp(rotationX, ClampAngleMin, ClampAngleMax);

        Quaternion targetRotation = Quaternion.Euler(rotationX - recoilOffset, rotationY, 0f);

        Vector3 rightOffset = targetRotation * Vector3.right * currentSideOffset;
        Vector3 targetPosition = Target.position + (Vector3.up * currentHeight) + rightOffset - (targetRotation * Vector3.forward * currentDistance);

        targetPosition += targetRotation * shakeOffset;

        transform.rotation = targetRotation;
        transform.position = targetPosition;

        if (cam != null)
        {
            float baseFov = isZooming ? 40f : 60f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + fovOffset, Time.deltaTime * ZoomSmoothSpeed);
        }
    }

    public void AddRecoil(float strength)
    {
        Tween.Custom(this, strength, 0f, duration: 0.25f, onValueChange: (target, val) => target.recoilOffset = val, ease: Ease.OutQuad);
    }

    public void AddJumpJuice()
    {
        Tween.Custom(this, 0f, 5f, duration: 0.15f, onValueChange: (target, val) => target.fovOffset = val, ease: Ease.OutQuad)
            .Chain(Tween.Custom(this, 5f, 0f, duration: 0.2f, onValueChange: (target, val) => target.fovOffset = val, ease: Ease.InOutSine));
    }

    public void AddDamageShake()
    {
        Tween.Custom(this, 1f, 0f, duration: 0.3f, onValueChange: (target, val) => {
            target.shakeOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f) * val;
        }).OnComplete(() => shakeOffset = Vector3.zero);
    }
}
