using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public void DisconnectClient()
    {
        NetworkManager.Shutdown();
        Debug.Log("Player Disconnected");
    }
    public void StartClient()
    {
        NetworkManager.StartClient();
        Debug.Log("Player Joined");
    }
    public void StartHost()
    {
        NetworkManager.StartHost();
        Debug.Log("Player Hosting");
    }
}
