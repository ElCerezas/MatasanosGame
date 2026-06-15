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
            Debug.Log("[ClientDisconnectNotifier] ALT F4 botificacion");
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

public class ClientDisconnectNotifierRpc : NetworkBehaviour
{
    [ServerRpc]
    public void NotifyDisconnectServerRpc(ulong clientId)
    {
        Debug.Log($"[ClientDisconnectNotifier] Server ha recivido que el cliente: {clientId} se ha desconectado.");
        
        LobbyManager lobbyManager = FindFirstObjectByType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.RemovePlayerFromReady(clientId);
            Debug.Log($"[ClientDisconnectNotifier] Cliente {clientId} echado del lobby.");
        }
    }
}
