using TMPro;
using UnityEngine;

public class InteractionHintUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private string hintMessage = "[F] Interact";

    private PlayerInteraction detector;

    private void Awake() => detector = GetComponent<PlayerInteraction>();

    private void OnEnable()
    {
        detector.OnNearbyInteractableChanged += HandleInteractableChanged;
        SetHintVisible(false);
    }

    private void OnDisable()
    {
        detector.OnNearbyInteractableChanged -= HandleInteractableChanged;
    }

    private void HandleInteractableChanged(IInteractable interactable)
    {
        bool hasTarget = interactable != null;
        SetHintVisible(hasTarget);
        if (hasTarget && hintText != null)
            hintText.text = hintMessage;
    }

    private void SetHintVisible(bool visible)
    {
        if (hintText != null)
            hintText.gameObject.SetActive(visible);
    }
}
