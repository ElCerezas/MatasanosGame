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

        // Si somos el Host/Servidor y el que se desconecta es un cliente (ej. ID 1), lo ignoramos.
        // (El Host no debe volver al menú solo porque un jugador se haya salido).
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[NetworkDisconnect] El Cliente {clientId} ha abandonado la partida. El Host sigue jugando.");
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
            // 1. Apagar Netcode
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                // Esperar a que Netcode se apague realmente
                await Task.Delay(100);

                // 2. Destruir explícitamente el objeto del NetworkManager
                if (NetworkManager.Singleton != null)
                {
                    Object.Destroy(NetworkManager.Singleton.gameObject);
                }
            }

            // 3. Limpiar Input y Cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null)
            {
                var input = localPlayer.GetComponent<PlayerInput>();
                if (input != null) input.DisableInputs();
            }

            SceneManager.LoadScene(sceneName);
        }
        finally
        {
            isReturningToMenu = false;
        }
    }
}