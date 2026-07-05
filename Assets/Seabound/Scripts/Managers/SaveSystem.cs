using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    private const string SaveFileName = "seabound_save.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private readonly Dictionary<string, ISaveable> registry = new Dictionary<string, ISaveable>();
    private SerializableDictionary cachedSaveData;

private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadAll();
    }

    private void OnApplicationQuit()
    {
        SaveAll();
    }

public void Register(ISaveable saveable)
    {
        if (!registry.ContainsKey(saveable.SaveKey))
        {
            registry[saveable.SaveKey] = saveable;

            if (cachedSaveData != null)
            {
                var entry = cachedSaveData.entries.Find(e => e.key == saveable.SaveKey);
                if (entry != null)
                    saveable.OnLoad(entry.value);
            }
        }
    }

    public void Unregister(ISaveable saveable)
    {
        registry.Remove(saveable.SaveKey);
    }

public void SaveAll()
    {
        var dict = new SerializableDictionary();

        foreach (var kvp in registry)
            dict.entries.Add(new SerializableDictionary.Entry { key = kvp.Key, value = kvp.Value.OnSave() });

        string json = JsonUtility.ToJson(dict, prettyPrint: true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"[SaveSystem] Saved {dict.entries.Count} entries to {SaveFilePath}");
    }

    public void LoadAll()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveSystem] No save file found – starting fresh.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        cachedSaveData = JsonUtility.FromJson<SerializableDictionary>(json);

        if (cachedSaveData == null)
            return;

        foreach (var entry in cachedSaveData.entries)
        {
            if (registry.TryGetValue(entry.key, out ISaveable saveable))
                saveable.OnLoad(entry.value);
        }

        Debug.Log($"[SaveSystem] Loaded {cachedSaveData.entries.Count} entries.");
    }

    public void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
            File.Delete(SaveFilePath);
    }

[System.Serializable]
    private class SerializableDictionary
    {
        [System.Serializable]
        public class Entry
        {
            public string key;
            public string value;
        }
        public List<Entry> entries = new List<Entry>();
    }
}
