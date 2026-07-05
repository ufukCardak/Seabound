using UnityEngine;
using TMPro;
using PrimeTween;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    public GameObject shopPanel;
    public TextMeshProUGUI feedbackText;

    private PlayerInventory currentCustomer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (shopPanel != null)
        {
            var btnAmmo = shopPanel.transform.Find("BtnAmmo");
            if (btnAmmo != null) btnAmmo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(BuyAmmo);

            var btnBlunderbuss = shopPanel.transform.Find("BtnBlunderbuss");
            if (btnBlunderbuss != null) btnBlunderbuss.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(BuyBlunderbussAmmo);

            var btnBandage = shopPanel.transform.Find("BtnBandage");
            if (btnBandage != null) btnBandage.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(BuyBandage);

            shopPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (shopPanel != null && shopPanel.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)))
        {
            CloseShop();
        }
    }

    public void OpenShop(PlayerInventory inventory)
    {
        currentCustomer = inventory;
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            shopPanel.transform.localScale = Vector3.zero;
            Tween.Scale(shopPanel.transform, Vector3.one, 0.35f, Ease.OutBack);
        }
            
        if (feedbackText != null)
            feedbackText.text = "Welcome, Captain!";
            
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            Tween.Scale(shopPanel.transform, Vector3.zero, 0.2f, Ease.InBack).OnComplete(() => shopPanel.SetActive(false));
        }
        currentCustomer = null;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            Tween.PunchScale(feedbackText.transform, strength: Vector3.one * 0.2f, duration: 0.3f);
        }
    }

    public void BuyAmmo()
    {
        if (currentCustomer == null) return;

        if (currentCustomer.PersonalGold.Value >= 20)
        {
            currentCustomer.AddPersonalGoldServerRpc(-20);
            currentCustomer.AddAmmoServerRpc(10);
            ShowFeedback("Purchased 10 Pistol Ammo!", Color.green);
        }
        else
        {
            ShowFeedback("Not enough Gold!", Color.red);
        }
    }

    public void BuyBlunderbussAmmo()
    {
        if (currentCustomer == null) return;

        if (currentCustomer.PersonalGold.Value >= 30)
        {
            currentCustomer.AddPersonalGoldServerRpc(-30);
            currentCustomer.AddBlunderbussAmmoServerRpc(5);
            ShowFeedback("Purchased 5 Blunderbuss Shells!", Color.green);
        }
        else
        {
            ShowFeedback("Not enough Gold!", Color.red);
        }
    }

    public void BuyBandage()
    {
        if (currentCustomer == null) return;

        if (currentCustomer.PersonalGold.Value >= 50)
        {
            currentCustomer.AddPersonalGoldServerRpc(-50);
            currentCustomer.AddBandageServerRpc(1);
            ShowFeedback("Purchased 1 Bandage!", Color.green);
        }
        else
        {
            ShowFeedback("Not enough Gold!", Color.red);
        }
    }
}
