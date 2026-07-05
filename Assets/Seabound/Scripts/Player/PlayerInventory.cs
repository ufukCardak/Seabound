using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour, ISaveable
{

    public NetworkVariable<int> PersonalGold = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> EquippedWeaponIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Ammo = new NetworkVariable<int>(
        30,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> BlunderbussAmmo = new NetworkVariable<int>(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Bandages = new NetworkVariable<int>(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action<int> OnGoldChanged;
    public event Action<int> OnWeaponChanged;
    public event Action<int> OnAmmoChanged;
    public event Action<int> OnBlunderbussAmmoChanged;
    public event Action<int> OnBandagesChanged;

public string SaveKey => $"player_inventory_{OwnerClientId}";

    public string OnSave()
    {
        int currentHealth = 100;
        var player = GetComponent<PlayerController>();
        if (player != null) currentHealth = player.CurrentHealth;

        return JsonUtility.ToJson(new PlayerSaveData
        {
            personalGold = PersonalGold.Value,
            currentWeaponIndex = EquippedWeaponIndex.Value,
            ammo = Ammo.Value,
            blunderbussAmmo = BlunderbussAmmo.Value,
            bandages = Bandages.Value,
            health = currentHealth,
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z
        });
    }

    public void OnLoad(string json)
    {
        if (!IsOwner) return;
        var data = JsonUtility.FromJson<PlayerSaveData>(json);
        if (data == null) return;
        ApplyLoadedDataServerRpc(data.personalGold, data.currentWeaponIndex, data.ammo, data.blunderbussAmmo, data.bandages, data.health,
                                 data.posX, data.posY, data.posZ);
    }

public override void OnNetworkSpawn()
    {
        PersonalGold.OnValueChanged += (_, v) => OnGoldChanged?.Invoke(v);
        EquippedWeaponIndex.OnValueChanged += (_, v) => OnWeaponChanged?.Invoke(v);
        Ammo.OnValueChanged += (_, v) => OnAmmoChanged?.Invoke(v);
        BlunderbussAmmo.OnValueChanged += (_, v) => OnBlunderbussAmmoChanged?.Invoke(v);
        Bandages.OnValueChanged += (_, v) => OnBandagesChanged?.Invoke(v);

        if (!IsOwner) return;

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Register(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveAll();
            SaveSystem.Instance.Unregister(this);
        }
    }

    public void AddPersonalGold(int amount)
    {
        if (!IsServer) return;
        PersonalGold.Value = Mathf.Max(0, PersonalGold.Value + amount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPersonalGoldServerRpc(int amount, ServerRpcParams rpc = default)
    {
        PersonalGold.Value = Mathf.Max(0, PersonalGold.Value + amount);
    }

    public void SetWeapon(int index)
    {
        if (index >= 0 && index < 2) 
        {
            SetWeaponServerRpc(index);
        }
    }

    [ServerRpc]
    private void SetWeaponServerRpc(int index, ServerRpcParams rpc = default)
    {
        EquippedWeaponIndex.Value = index;
    }

    public void UseBandage()
    {
        if (Bandages.Value > 0)
        {
            UseBandageServerRpc();
        }
    }

    [ServerRpc]
    private void UseBandageServerRpc(ServerRpcParams rpc = default)
    {
        if (Bandages.Value <= 0) return;

        var player = GetComponent<PlayerController>();
        if (player != null && player.Health.Value < 100)
        {
            Bandages.Value--;
            player.Health.Value = Mathf.Min(100, player.Health.Value + 25);
        }
    }

    [ServerRpc]
    public void AddAmmoServerRpc(int amount, ServerRpcParams rpc = default)
    {
        Ammo.Value += amount;
    }

    [ServerRpc]
    public void AddBlunderbussAmmoServerRpc(int amount, ServerRpcParams rpc = default)
    {
        BlunderbussAmmo.Value += amount;
    }

    [ServerRpc]
    public void AddBandageServerRpc(int amount, ServerRpcParams rpc = default)
    {
        Bandages.Value += amount;
    }

[ServerRpc]
    private void ApplyLoadedDataServerRpc(int gold, int weaponIndex, int ammo, int blunderbussAmmo, int bandages, int health, float px, float py, float pz,
                                          ServerRpcParams rpc = default)
    {
        if (rpc.Receive.SenderClientId != OwnerClientId) return;

        PersonalGold.Value = gold;
        EquippedWeaponIndex.Value = weaponIndex;
        Ammo.Value = ammo;
        BlunderbussAmmo.Value = blunderbussAmmo;
        Bandages.Value = bandages;

        var player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.Health.Value = health;
        }

        transform.position = new Vector3(px, py, pz);
        if (TryGetComponent<Rigidbody>(out var rb))
            rb.position = transform.position;
    }
}
