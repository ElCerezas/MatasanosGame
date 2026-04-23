using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    bool isCursorLocked = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback += SpawnPlayer;
        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;
    }
    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in clientsCompleted)
        {
            SpawnPlayer(clientId);
        }
    }
    private void SpawnPlayer(ulong clientID)
    {
        if (NetworkManager.ConnectedClients[clientID].PlayerObject != null) return;
        {
            GameObject player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true); //Se destruye el player al hacer reload de la escena.
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;
        }
        base.OnNetworkDespawn();
    }
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
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (isCursorLocked)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                isCursorLocked = false;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                isCursorLocked = true;
            }
        }
    }
}
