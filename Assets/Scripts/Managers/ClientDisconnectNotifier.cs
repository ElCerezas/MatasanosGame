using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class ClientDisconnectNotifier : MonoBehaviour
{
    private bool hasNotifiedServer = false;
    private ClientDisconnectNotifierRpc rpcHandler;

    private void Start()
    {
        // Buscar o crear el handler de RPC
        rpcHandler = FindFirstObjectByType<ClientDisconnectNotifierRpc>();
        if (rpcHandler == null)
        {
            GameObject rpcObject = new GameObject("DisconnectNotifierRPC");
            rpcHandler = rpcObject.AddComponent<ClientDisconnectNotifierRpc>();
        }
    }

    private void OnApplicationQuit()
    {
        if (!hasNotifiedServer && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[ClientDisconnectNotifier] Aplicación cerrando. Notificando al servidor...");
            hasNotifiedServer = true;
            
            if (rpcHandler != null)
            {
                rpcHandler.NotifyDisconnectServerRpc(NetworkManager.Singleton.LocalClientId);
            }
            
            // Esperar un poco a que la RPC se procese
            System.Threading.Thread.Sleep(200);
        }
    }
}

/// <summary>
/// Handler separado para las RPCs. Debe ser NetworkBehaviour.
/// </summary>
public class ClientDisconnectNotifierRpc : NetworkBehaviour
{
    [ServerRpc]
    public void NotifyDisconnectServerRpc(ulong clientId)
    {
        Debug.Log($"[ClientDisconnectNotifier] Servidor recibió notificación de desconexión de cliente {clientId}");
        
        // Limpiar de LobbyManager si estamos en lobby
        LobbyManager lobbyManager = FindFirstObjectByType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.RemovePlayerFromReady(clientId);
            Debug.Log($"[ClientDisconnectNotifier] Cliente {clientId} removido del lobby.");
        }
    }
}
