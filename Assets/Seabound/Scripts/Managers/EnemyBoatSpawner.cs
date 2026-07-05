using Unity.Netcode;
using UnityEngine;

public class EnemyBoatSpawner : MonoBehaviour
{
    public static EnemyBoatSpawner Instance;

    [Header("Spawner Settings")]
    public GameObject boatPrefab;
    public float spawnInterval = 60f;
    public float minSpawnRadius = 50f;
    public float maxSpawnRadius = 100f;

    private float nextSpawnTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemyBoat();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnEnemyBoat()
    {
        if (boatPrefab == null)
        {
            Debug.LogError("EnemyBoatSpawner: boatPrefab is missing!");
            return;
        }

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        Transform targetPlayer = players[Random.Range(0, players.Length)].transform;

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 spawnPosition = new Vector3(targetPlayer.position.x + randomCircle.x, 0f, targetPlayer.position.z + randomCircle.y);

        GameObject newBoat = Instantiate(boatPrefab, spawnPosition, Quaternion.identity);
        var no = newBoat.GetComponent<NetworkObject>();
        if (no != null)
        {
            no.Spawn();
            Debug.Log($"Enemy Boat spawned at {spawnPosition}");
        }
        else
        {
            Debug.LogError("EnemyBoatSpawner: Prefab does not have a NetworkObject!");
            Destroy(newBoat);
        }
    }
}
