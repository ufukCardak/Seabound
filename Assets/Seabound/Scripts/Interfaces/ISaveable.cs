public interface ISaveable
{
    string SaveKey { get; }
    string OnSave();
    void OnLoad(string json);
}
