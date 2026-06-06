using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : InteractableItem
{
    private NetworkVariable<List<ulong>> playersReady = new NetworkVariable<List<ulong>>();
    private int numberOfPlayers = 1;
    private int numberOfPlayersText =1;
    [SerializeField] private string m_GameSceneName = "MapaBeta";
    [SerializeField] private TextMeshProUGUI m_TextMeshProUGUI;

    private Camera localCamera;

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
    void LateUpdate()
    {
        numberOfPlayers = NetworkManager.ConnectedClientsIds.Count;
        if (localCamera == null)
        {
            localCamera = Camera.main;
            return;
        }

        if(numberOfPlayers != numberOfPlayersText) UpdateTextClientRpc(playersReady.Value.Count, numberOfPlayers);

        m_TextMeshProUGUI.transform.LookAt(localCamera.transform.position - new Vector3(0,-1,0));
        transform.Rotate(0, 180f, 0);

        numberOfPlayersText = numberOfPlayers;
    }
    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
        CheckClientsReadyServerRpc(clientID);
    }
    [ServerRpc]
    private void CheckClientsReadyServerRpc(ulong clientID, ServerRpcParams rpcParams = default)
    {
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
        UpdateTextClientRpc(updatedList.Count, numberOfPlayers);

        if (playersReady.Value.Count == numberOfPlayers)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(m_GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    [ClientRpc]
    private void UpdateTextClientRpc(int readyCount, int totalPlayers)
    {
        m_TextMeshProUGUI.text = $"{readyCount}/{totalPlayers} Listos";
    }
}
