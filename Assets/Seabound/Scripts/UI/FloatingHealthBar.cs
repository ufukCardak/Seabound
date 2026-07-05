using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    private Transform mainCamera;

    private HealthComponent healthComponent;

    private void Start()
    {
        var cam = Camera.main;
        if (cam != null)
            mainCamera = cam.transform;
        
        healthComponent = GetComponentInParent<HealthComponent>();
        if (healthComponent != null)
        {
            if (slider != null)
            {
                slider.maxValue = 100;
                slider.value = healthComponent.CurrentHealth;
            }
            healthComponent.OnHealthChanged += UpdateHealth;
        }
    }

    private void OnDestroy()
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= UpdateHealth;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.rotation * Vector3.forward,
                             mainCamera.rotation * Vector3.up);
        }
    }

    public void UpdateHealth(int currentHealth)
    {
        if (slider != null)
        {
            slider.value = currentHealth;
        }
    }
}
