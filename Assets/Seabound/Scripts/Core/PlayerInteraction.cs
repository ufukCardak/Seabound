using System;
using System.Collections;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float detectRadius = 4f;
    [SerializeField] private float checkInterval = 0.15f;
    [SerializeField] private LayerMask interactLayer = 1 << 8;

    public event Action<IInteractable> OnNearbyInteractableChanged;

    public bool HasNearbyInteractable => nearestInteractable != null;

    private IInteractable nearestInteractable;
    private readonly Collider[] overlapBuffer = new Collider[16];

    private void OnEnable() => StartCoroutine(DetectRoutine());
    private void OnDisable() => StopAllCoroutines();

    private IEnumerator DetectRoutine()
    {
        var wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            yield return wait;
            Scan();
        }
    }

    private void Scan()
    {
        IInteractable nearest = null;

        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, interactLayer))
            {
                if (Vector3.Distance(transform.position, hit.point) <= detectRadius)
                {
                    var interactable = hit.collider.GetComponentInParent<IInteractable>();
                    if (interactable != null)
                    {
                        nearest = interactable;
                    }
                }
            }
        }

        if (nearest != nearestInteractable)
        {
            nearestInteractable = nearest;
            OnNearbyInteractableChanged?.Invoke(nearestInteractable);
        }
    }

    public void TryInteract(PlayerController player)
    {
        if (nearestInteractable != null)
        {
            nearestInteractable.Interact(player);
        }
    }

    public void TryInteractMerchantWhileCarrying(PlayerController player)
    {
        if (nearestInteractable is MerchantInteractable merchant)
        {
            merchant.Interact(player);
        }
        else
        {
            player.ChestInteraction.RequestDrop();
        }
    }

    private void OnDrawGizmos()
    {
        if (DebugManager.Instance == null || !DebugManager.Instance.showDebugVisuals) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        if (Camera.main != null)
        {
            Gizmos.color = Color.cyan;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Gizmos.DrawRay(ray.origin, ray.direction * 10f);
        }
    }
}
