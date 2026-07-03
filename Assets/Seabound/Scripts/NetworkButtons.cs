using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;

    private void Start()
    {
        if (startHostButton != null)
            startHostButton.onClick.AddListener(StartHostGame);

        if (startClientButton != null)
            startClientButton.onClick.AddListener(StartClientGame);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartHostGame();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            StartClientGame();
        }
    }

    private void StartHostGame()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartHost();
            HideUI();
        }
    }

    private void StartClientGame()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartClient();
            HideUI();
        }
    }

    private void HideUI()
    {
        gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}