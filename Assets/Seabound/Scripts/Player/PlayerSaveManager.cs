using UnityEngine;
using Unity.Netcode;

public class PlayerSaveManager : NetworkBehaviour
{
    private PlayerController player;
    private PlayerInventory inventory;

    private float saveTimer = 0f;
    private const float SaveInterval = 5f;

    private string SaveFilePath => System.IO.Path.Combine(Application.persistentDataPath, "PlayerSaveData.json");

    [System.Serializable]
    public class PlayerSaveData
    {
        public int Health = 100;
        public int Gold = 0;
        public int Ammo = 30;
        public int BlunderbussAmmo = 10;
        public int Bandages = 3;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        inventory = GetComponent<PlayerInventory>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LoadData();
        }
    }

    private void LoadData()
    {
        if (!System.IO.File.Exists(SaveFilePath))
        {

            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(SaveFilePath);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

            RequestApplySaveDataServerRpc(data.Health, data.Gold, data.Ammo, data.BlunderbussAmmo, data.Bandages);
            Debug.Log($"[SaveManager] Loaded save data from JSON: {SaveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load save data: {e.Message}");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestApplySaveDataServerRpc(int health, int gold, int ammo, int bAmmo, int bandages)
    {
        if (player != null) player.Health.Value = health;
        if (inventory != null)
        {
            inventory.PersonalGold.Value = gold;
            inventory.Ammo.Value = ammo;
            inventory.BlunderbussAmmo.Value = bAmmo;
            inventory.Bandages.Value = bandages;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        saveTimer += Time.deltaTime;
        if (saveTimer >= SaveInterval)
        {
            saveTimer = 0f;
            SaveData();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SaveData();
        }
    }

    private void SaveData()
    {
        try
        {
            PlayerSaveData data = new PlayerSaveData();
            
            if (player != null) data.Health = player.Health.Value;
            if (inventory != null)
            {
                data.Gold = inventory.PersonalGold.Value;
                data.Ammo = inventory.Ammo.Value;
                data.BlunderbussAmmo = inventory.BlunderbussAmmo.Value;
                data.Bandages = inventory.Bandages.Value;
            }

            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(SaveFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save data: {e.Message}");
        }
    }
}
