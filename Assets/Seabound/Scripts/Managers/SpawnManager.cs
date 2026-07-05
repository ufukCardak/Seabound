using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    private readonly List<Transform> spawnPoints = new List<Transform>();

private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RefreshCache();
    }

public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    public void RefreshCache()
    {
        spawnPoints.Clear();
        foreach (var go in GameObject.FindGameObjectsWithTag("SpawnPoint"))
            spawnPoints.Add(go.transform);

        Debug.Log($"[SpawnManager] Cached {spawnPoints.Count} spawn points.");
    }
}
