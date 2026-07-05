using Unity.Netcode;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;

    [Header("Prefabs")]
    public GameObject chestPrefab;
    public GameObject enemyBoatPrefab;

    [Header("Settings")]
    public bool showDebugVisuals = true;
    public bool allowClientDebug = true;

    private bool showMenu = false;
    private Rect windowRect = new Rect(20, 20, 250, 350);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showMenu = !showMenu;
            if (showMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if ((HUDManager.Instance.inventoryPanel == null || !HUDManager.Instance.inventoryPanel.activeSelf) &&
                    (ShopUIManager.Instance.shopPanel == null || !ShopUIManager.Instance.shopPanel.activeSelf))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!showMenu) return;

        windowRect = GUI.Window(999, windowRect, DrawDebugMenu, "DEBUG MENU (F1)");
    }

    private void DrawDebugMenu(int windowID)
    {
        GUILayout.Space(10);

        var player = GetLocalPlayer();
        if (player == null) { GUI.DragWindow(new Rect(0, 0, 10000, 20)); return; }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            allowClientDebug = GUILayout.Toggle(allowClientDebug, " Allow Clients to use Cheats");
            GUILayout.Space(10);
        }

        var debugHandler = player.GetComponent<PlayerDebugHandler>();
        if (debugHandler == null) return;

        if (GUILayout.Button("Give 1000 Gold", GUILayout.Height(30))) debugHandler.RequestDebugCheatServerRpc(DebugCheatType.GiveGold);
        if (GUILayout.Button("Give Full Ammo & Bandages", GUILayout.Height(30))) debugHandler.RequestDebugCheatServerRpc(DebugCheatType.GiveAmmo);
        if (GUILayout.Button("Restore Full Health", GUILayout.Height(30))) debugHandler.RequestDebugCheatServerRpc(DebugCheatType.RestoreHealth);
        
        GUILayout.Space(10);
        showDebugVisuals = GUILayout.Toggle(showDebugVisuals, " Show Debug Rays/Circles");
        GUILayout.Space(10);
        
        if (GUILayout.Button("Spawn Treasure Chest Here", GUILayout.Height(30))) debugHandler.RequestDebugCheatServerRpc(DebugCheatType.SpawnChest);
        if (GUILayout.Button("Spawn Enemy Boat Nearby", GUILayout.Height(30))) debugHandler.RequestDebugCheatServerRpc(DebugCheatType.SpawnBoat);
        if (GUILayout.Button("Kill All Enemies", GUILayout.Height(30))) debugHandler.RequestDebugCheatServerRpc(DebugCheatType.KillEnemies);

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private PlayerController GetLocalPlayer()
    {
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsOwner) return player;
        }
        return null;
    }
}
