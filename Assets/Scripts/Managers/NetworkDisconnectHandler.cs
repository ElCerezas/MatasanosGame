using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Multiplayer;
using Object = UnityEngine.Object;

public class NetworkDisconnectHandler : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private static bool isReturningToMenu = false;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    client.PlayerObject.Despawn();
                    Object.Destroy(client.PlayerObject.gameObject);
                }
            }

            LobbyManager lobbyManager = Object.FindFirstObjectByType<LobbyManager>();
            if (lobbyManager != null)
            {
                lobbyManager.RemovePlayerFromReady(clientId);
            }
            return;
        }

        _ = ReturnToMainMenu(mainMenuSceneName);
    }

    public static async Task ReturnToMainMenu(string sceneName)
    {
        if (isReturningToMenu) return;
        isReturningToMenu = true;

        try
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var input = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInput>();
                if (input != null)
                {
                    input.DisableInputs();
                }
            }

            if (MultiplayerService.Instance != null && MultiplayerService.Instance.Sessions != null)
            {
                try
                {
                    var activeSessions = new System.Collections.Generic.List<ISession>(MultiplayerService.Instance.Sessions.Values);
                    foreach (var session in activeSessions)
                    {
                        await session.LeaveAsync();
                    }
                }
                catch
                {
                }
            }

            await Task.Delay(150);
            SceneManager.LoadScene(sceneName);
        }
        catch
        {
            SceneManager.LoadScene(sceneName);
        }
        finally
        {
            isReturningToMenu = false;
        }
    }
}