using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkDisconnectHandler : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }
    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId) return;
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}