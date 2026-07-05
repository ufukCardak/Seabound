using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PrimeTween;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Settings")]
    public int maxHp = 100;
    public string[] weaponNames = new string[] { "Flintlock", "Blunderbuss" };

    [Header("Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Gold")]
    [SerializeField] private TextMeshProUGUI personalGoldText;

    [Header("Inventory Panel")]
    public GameObject inventoryPanel;
    public TextMeshProUGUI inventoryGoldText;
    public TextMeshProUGUI inventoryAmmoText;
    public TextMeshProUGUI inventoryBlunderbussAmmoText;
    public TextMeshProUGUI inventoryBandagesText;

    [Header("Feedback")]
    [SerializeField] private Image hitmarkerImage;
    [SerializeField] private float hitmarkerDuration = 0.2f;
    private float hitmarkerTimer;

    [Header("Interaction")]
    [SerializeField] private GameObject interactPrompt;
    private TextMeshProUGUI interactPromptText;

    private PlayerController player;
    private PlayerInventory inventory;
    private PlayerInteraction interactionDetector;

    private TextMeshProUGUI hotbarText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (hitmarkerImage != null && hitmarkerTimer > 0)
        {
            hitmarkerTimer -= Time.deltaTime;
            Color c = hitmarkerImage.color;
            c.a = Mathf.Clamp01(hitmarkerTimer / hitmarkerDuration);
            hitmarkerImage.color = c;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                Tween.Scale(inventoryPanel.transform, Vector3.zero, 0.2f, Ease.InBack).OnComplete(() => {
                    inventoryPanel.SetActive(false);
                });
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (ShopUIManager.Instance != null && ShopUIManager.Instance.shopPanel != null && ShopUIManager.Instance.shopPanel.activeSelf)
            {
                return;
            }

            if (inventoryPanel != null)
            {
                bool isActive = !inventoryPanel.activeSelf;
                if (isActive)
                {
                    inventoryPanel.SetActive(true);
                    inventoryPanel.transform.localScale = Vector3.zero;
                    Tween.Scale(inventoryPanel.transform, Vector3.one, 0.35f, Ease.OutBack);
                    
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Tween.Scale(inventoryPanel.transform, Vector3.zero, 0.2f, Ease.InBack).OnComplete(() => {
                        inventoryPanel.SetActive(false);
                    });
                    
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    private void OnDestroy() => Detach();

    public void Init(PlayerController localPlayer)
    {
        player = localPlayer;
        inventory = player.GetComponent<PlayerInventory>();
        interactionDetector = player.GetComponent<PlayerInteraction>();

        player.Health.OnValueChanged += OnHealthChanged;
        UpdateHealth(player.Health.Value);

        if (inventory != null)
        {
            inventory.OnGoldChanged += OnPersonalGoldChanged;
            inventory.OnWeaponChanged += OnWeaponChanged;
            inventory.OnAmmoChanged += OnAmmoChanged;
            inventory.OnBlunderbussAmmoChanged += OnBlunderbussAmmoChanged;
            inventory.OnBandagesChanged += OnBandagesChanged;

            UpdatePersonalGold(inventory.PersonalGold.Value);

            OnAmmoChanged(inventory.Ammo.Value);
            OnBlunderbussAmmoChanged(inventory.BlunderbussAmmo.Value);
            OnBandagesChanged(inventory.Bandages.Value);
        }

        if (interactionDetector != null)
        {
            interactionDetector.OnNearbyInteractableChanged += OnInteractableChanged;
        }

        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
            interactPromptText = interactPrompt.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        GameObject hotbarObj = GameObject.Find("HotbarText");
        if (hotbarObj != null)
        {
            hotbarText = hotbarObj.GetComponent<TextMeshProUGUI>();
            UpdateHotbarUI();
        }
    }

    private void Detach()
    {
        if (player != null)
            player.Health.OnValueChanged -= OnHealthChanged;

        if (inventory != null)
        {
            inventory.OnGoldChanged -= OnPersonalGoldChanged;
            inventory.OnWeaponChanged -= OnWeaponChanged;
            inventory.OnAmmoChanged -= OnAmmoChanged;
            inventory.OnBlunderbussAmmoChanged -= OnBlunderbussAmmoChanged;
            inventory.OnBandagesChanged -= OnBandagesChanged;
        }

        if (interactionDetector != null)
        {
            interactionDetector.OnNearbyInteractableChanged -= OnInteractableChanged;
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealth(newValue);
    }

    private void OnPersonalGoldChanged(int newGold)
    {
        UpdatePersonalGold(newGold);
    }

    private void OnWeaponChanged(int newIndex)
    {
    }

    private void OnAmmoChanged(int newAmmo)
    {
        UpdateHotbarUI();
    }

    private void OnBlunderbussAmmoChanged(int newAmmo)
    {
        UpdateHotbarUI();
    }

    private void OnBandagesChanged(int newBandages)
    {
        UpdateHotbarUI();
    }

    private void UpdateHotbarUI()
    {
        if (hotbarText != null && inventory != null)
        {
            int pistolAmmo = inventory.Ammo.Value;
            int bbusAmmo = inventory.BlunderbussAmmo.Value;
            int bandages = inventory.Bandages.Value;

            string controlsText = 
                "<b>Controls:</b>\n" +
                "[W,A,S,D] Move / Drive\n" +
                "[Space] Jump\n" +
                "[F] Interact / Board\n" +
                "[Right Click] Aim\n" +
                "[Left Click] Shoot\n" +
                "[Tab] Inventory\n" +
                "[Esc] Close Menu\n" +
                "[F1] Debug Menu\n\n" +
                "<b>Items:</b>\n";

            hotbarText.text = controlsText + 
                              $"[1] Pistol ({pistolAmmo})\n" +
                              $"[2] Blunderbuss ({bbusAmmo})\n" +
                              $"[3] Bandage ({bandages})";

            if (inventoryAmmoText != null)
                inventoryAmmoText.text = $"<b>PISTOL AMMO:</b> <color=#FFFFFF>{pistolAmmo}</color>";
            if (inventoryBlunderbussAmmoText != null)
                inventoryBlunderbussAmmoText.text = $"<b>BLUNDERBUSS AMMO:</b> <color=#FFFFFF>{bbusAmmo}</color>";
            if (inventoryBandagesText != null)
                inventoryBandagesText.text = $"<b>BANDAGES:</b> <color=#FFFFFF>{bandages}</color>";
                              
            Tween.PunchScale(hotbarText.transform, strength: Vector3.one * 0.1f, duration: 0.2f);
        }
    }

    private void UpdateHealth(int hp)
    {
        healthSlider.value = (float)hp / maxHp;
        healthText.text = $"HP: {hp}/{maxHp}";
    }

    private void UpdatePersonalGold(int gold)
    {
        personalGoldText.text = $"Gold: {gold}";
        if (inventoryGoldText != null)
            inventoryGoldText.text = $"<b>GOLD:</b> <color=#FFD700>{gold}</color>";
            
        Tween.PunchScale(personalGoldText.transform, strength: Vector3.one * 0.3f, duration: 0.3f);
    }

    public void ShowHitmarker()
    {
        if (hitmarkerImage != null)
        {
            hitmarkerTimer = hitmarkerDuration;
            Color c = hitmarkerImage.color;
            c.a = 1f;
            hitmarkerImage.color = c;
            
            hitmarkerImage.transform.localScale = Vector3.one * 1.5f;
            Tween.Scale(hitmarkerImage.transform, Vector3.one, hitmarkerDuration, Ease.OutElastic);
        }
    }

    private void OnInteractableChanged(IInteractable interactable)
    {
        if (interactPrompt != null)
        {
            if (interactable != null)
            {
                if (interactPromptText != null)
                    interactPromptText.text = interactable.GetInteractText();
                interactPrompt.SetActive(true);
            }
            else
            {
                interactPrompt.SetActive(false);
            }
        }
    }
}
