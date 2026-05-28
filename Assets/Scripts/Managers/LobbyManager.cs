using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : InteractableItem
{
    private NetworkVariable<List<ulong>> playersReady = new NetworkVariable<List<ulong>>();
    private int numberOfPlayers = 1;
    [SerializeField] private string m_GameSceneName = "MapaBeta";
    /*private void Start()
    {
        playersReady.Value = new List<ulong>() { };
    }*/
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            playersReady.Value = new List<ulong>();
        }
    }
    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
        CheckClientsReadyServerRpc(clientID);
    }
    [ServerRpc]
    private void CheckClientsReadyServerRpc(ulong clientID, ServerRpcParams rpcParams = default)
    {
        numberOfPlayers = NetworkManager.ConnectedClientsIds.Count;

        List<ulong> updatedList = new List<ulong>(playersReady.Value);

        if (updatedList.Contains(clientID))
        {
            updatedList.Remove(clientID);
        }
        else
        {
            updatedList.Add(clientID);
        }

        playersReady.Value = updatedList;   

        if (playersReady.Value.Count == numberOfPlayers)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(m_GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
