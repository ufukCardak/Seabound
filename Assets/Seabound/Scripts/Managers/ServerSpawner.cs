using Unity.Netcode;
using UnityEngine;

public class ServerSpawner : MonoBehaviour
{
    public GameObject GameManagerPrefab;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnManager;
    }

    private void SpawnManager()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            GameObject gm = Instantiate(GameManagerPrefab);

            gm.GetComponent<NetworkObject>().Spawn();
        }
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnManager;
        }
    }
}