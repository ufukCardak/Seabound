using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform Target;

    [Header("Distance Settings")]
    public float Distance = 3f;
    public float Height = 1.5f;

    [Header("Mouse Settings")]
    public float Sensitivity = 2f;
    public float ClampAngleMin = -20f;
    public float ClampAngleMax = 60f;

    [Header("Smoothness")]
    public float SmoothTime = 15f;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private Vector3 _currentVelocity;

    private void Start()
    {
        Vector3 currentAngles = transform.eulerAngles;
        _rotationY = currentAngles.y;
        _rotationX = currentAngles.x;
    }

    private void LateUpdate()
    {
        if (Target == null) 
            return;

        _rotationY += Input.GetAxis("Mouse X") * Sensitivity;
        _rotationX -= Input.GetAxis("Mouse Y") * Sensitivity;
        _rotationX = Mathf.Clamp(_rotationX, ClampAngleMin, ClampAngleMax);

        Quaternion targetRotation = Quaternion.Euler(_rotationX, _rotationY, 0f);

        Vector3 targetPosition = Target.position + (Vector3.up * Height) - (targetRotation * Vector3.forward * Distance);

        transform.rotation = targetRotation;
        transform.position = targetPosition;
    }
}