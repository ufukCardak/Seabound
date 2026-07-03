using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public NetworkVariable<int> TeamGold = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public TextMeshProUGUI goldText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public override void OnNetworkSpawn()
    {
        GameObject textObj = GameObject.Find("Gold_TMP");
        if (textObj != null)
        {
            goldText = textObj.GetComponent<TextMeshProUGUI>();
            goldText.text = "Gold: " + TeamGold.Value.ToString();
        }

        TeamGold.OnValueChanged += (int previousValue, int newValue) =>
        {
            if (goldText != null)
                goldText.text = "Gold: " + newValue.ToString();
        };
    }

    public void AddGold(int amount)
    {
        if (!IsServer) 
            return;

        TeamGold.Value += amount;
    }
}