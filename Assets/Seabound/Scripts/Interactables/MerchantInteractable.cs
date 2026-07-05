using UnityEngine;
using Unity.Netcode;

public class MerchantInteractable : MonoBehaviour, IInteractable
{
    private void OnEnable()
    {
        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
    public string GetInteractText()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject?.GetComponent<PlayerController>();
            if (localPlayer != null && localPlayer.CurrentState == localPlayer.CarryingState)
            {
                return "[F] Sell Chest";
            }
        }
        return "[F] Open Shop";
    }

    public void Interact(PlayerController player)
    {

        if (player.CurrentState == player.CarryingState)
        {
            var chestInteraction = player.GetComponent<PlayerChestInteraction>();
            if (chestInteraction != null)
            {
                chestInteraction.SellCarried();
                return;
            }
        }

        if (ShopUIManager.Instance != null)
        {
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.IsOwner)
            {
                ShopUIManager.Instance.OpenShop(inventory);
            }
        }
    }
}
