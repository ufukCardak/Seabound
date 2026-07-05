using UnityEngine;
using Unity.Netcode;

public class PlayerWeaponVisuals : NetworkBehaviour
{
    [Header("Weapon Models")]
    [Tooltip("The visual models for the weapons in order of their inventory index.")]
    [SerializeField] private GameObject[] weaponModels;

    public NetworkVariable<bool> IsWeaponVisible = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    public override void OnNetworkSpawn()
    {
        if (inventory != null)
        {
            inventory.OnWeaponChanged += UpdateVisuals;
            IsWeaponVisible.OnValueChanged += (oldVal, newVal) => UpdateVisuals(inventory.EquippedWeaponIndex.Value);
            UpdateVisuals(inventory.EquippedWeaponIndex.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (inventory != null)
        {
            inventory.OnWeaponChanged -= UpdateVisuals;
            IsWeaponVisible.OnValueChanged -= (oldVal, newVal) => UpdateVisuals(inventory.EquippedWeaponIndex.Value);
        }
    }

    private void UpdateVisuals(int index)
    {
        for (int i = 0; i < weaponModels.Length; i++)
        {
            if (weaponModels[i] != null)
            {
                weaponModels[i].SetActive(i == index && IsWeaponVisible.Value);
            }
        }
    }
}
