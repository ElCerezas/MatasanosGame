using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        Debug.Log($"[NetworkDisconnect] Evento OnClientDisconnected disparado. ClientId: {clientId}");
        if (NetworkManager.Singleton == null) return;

        // Si somos el Host/Servidor y el que se desconecta es un cliente (ej. ID 1), limpiamos de lobby y continúa
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[NetworkDisconnect] El Cliente {clientId} ha abandonado la partida. El Host sigue jugando.");
            
            // Destruir explícitamente el PlayerObject del cliente desconectado
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    Debug.Log($"[NetworkDisconnect] Destruyendo PlayerObject del cliente {clientId}");
                    client.PlayerObject.Despawn();
                    Object.Destroy(client.PlayerObject.gameObject);
                }
            }
            
            // Limpiar de LobbyManager solo si estamos en el lobby (WaitingRoom)
            LobbyManager lobbyManager = Object.FindFirstObjectByType<LobbyManager>();
            if (lobbyManager != null)
            {
                Debug.Log($"[NetworkDisconnect] Limpiando cliente {clientId} del lobby.");
                lobbyManager.RemovePlayerFromReady(clientId);
            }
            else
            {
                Debug.Log($"[NetworkDisconnect] LobbyManager no encontrado (no estamos en WaitingRoom). Cliente {clientId} se desconectó durante el juego.");
            }
            return;
        }

        // Si llegamos aquí:
        // 1. Somos el Host apagando nuestro propio servidor (clientId == 0).
        // 2. O somos un Cliente que ha perdido la conexión / ha sido desconectado por el Servidor.

        Debug.Log("[NetworkDisconnect] Desconexión válida detectada. Procediendo a volver al menú...");
        _ = ReturnToMainMenu(mainMenuSceneName);
    }
    public static async Task ReturnToMainMenu(string sceneName)
    {
        if (isReturningToMenu) return;
        isReturningToMenu = true;

        try
        {
            // Limpiar Input y Cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Si existe player object, deshabilitar inputs
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var input = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInput>();
                if (input != null) 
                {
                    input.DisableInputs();
                }
            }

            // Esperar un poco para que se procesen los cambios
            await Task.Delay(50);

            // Cargar escena del menú
            // NOTA: No llamamos a Shutdown() porque Unity Services Lobbies maneja su propio ciclo de vida
            SceneManager.LoadScene(sceneName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkDisconnect] Error en ReturnToMainMenu: {ex}");
            SceneManager.LoadScene(sceneName);
        }
        finally
        {
            isReturningToMenu = false;
        }
    }
}