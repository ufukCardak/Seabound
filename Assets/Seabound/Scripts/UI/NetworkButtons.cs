using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class NetworkButtons : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private Button deleteSaveButton;

    private void Start()
    {
        if (startHostButton != null)
            startHostButton.onClick.AddListener(StartHostGame);

        if (startClientButton != null)
            startClientButton.onClick.AddListener(StartClientGame);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string savePath = Path.Combine(Application.persistentDataPath, "seabound_save.json");
        
        if (deleteSaveButton != null)
        {
            if (File.Exists(savePath))
            {
                deleteSaveButton.gameObject.SetActive(true);
                deleteSaveButton.onClick.AddListener(() => {
                    File.Delete(savePath);
                    deleteSaveButton.gameObject.SetActive(false);
                });
            }
            else
            {
                deleteSaveButton.gameObject.SetActive(false);
            }
        }
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