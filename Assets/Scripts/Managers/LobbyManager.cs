using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Multiplayer;

public class LobbyManager : InteractableItem
{
    private NetworkVariable<List<ulong>> playersReady = new NetworkVariable<List<ulong>>();
    private NetworkVariable<bool> isLobbyLocked = new NetworkVariable<bool>(false);
    private int numberOfPlayers = 1;
    private int numberOfPlayersText = 1;
    [SerializeField] private string m_GameSceneName = "MapaGold";
    [SerializeField] private TextMeshProUGUI m_TextMeshProUGUI;

    [Header("Audio (FMOD)")]
    [SerializeField] private FMODUnity.EventReference sonidoPartidaIniciada;

    private Camera localCamera;

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

        if (numberOfPlayers != numberOfPlayersText) UpdateTextClientRpc(playersReady.Value.Count, numberOfPlayers);

        numberOfPlayersText = numberOfPlayers;
    }

    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
        CheckClientsReadyServerRpc(clientID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CheckClientsReadyServerRpc(ulong clientID, ServerRpcParams rpcParams = default)
    {
        List<ulong> updatedList = new List<ulong>(playersReady.Value);

        if (updatedList.Contains(clientID))
            updatedList.Remove(clientID);
        else
            updatedList.Add(clientID);

        playersReady.Value = updatedList;
        UpdateTextClientRpc(updatedList.Count, numberOfPlayers);

        if (playersReady.Value.Count == numberOfPlayers)
        {
            isLobbyLocked.Value = true;
            PlayStartGameSoundClientRpc();

            UpdateSessionStateToStarted();

            NetworkManager.Singleton.SceneManager.LoadScene(m_GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    private async void UpdateSessionStateToStarted()
    {
        if (!IsServer || UIMultiplayerSessionManager.CurrentSession == null) return;

        try
        {
            var hostSession = UIMultiplayerSessionManager.CurrentSession.AsHost();
            hostSession.SetProperty("GameStarted", new SessionProperty("true", VisibilityPropertyOptions.Public));
            await hostSession.SavePropertiesAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[LobbyManager] Error al actualizar: {ex.Message}");
        }
    }

    [ClientRpc]
    private void UpdateTextClientRpc(int readyCount, int totalPlayers)
    {
        m_TextMeshProUGUI.text = $"{readyCount}/{totalPlayers} Listos";
    }

    public bool IsLobbyLocked()
    {
        return isLobbyLocked.Value;
    }

    public void RemovePlayerFromReady(ulong clientId)
    {
        if (!IsServer) return;

        List<ulong> updatedList = new List<ulong>(playersReady.Value);
        if (updatedList.Contains(clientId))
        {
            updatedList.Remove(clientId);
            playersReady.Value = updatedList;
            UpdateTextClientRpc(updatedList.Count, numberOfPlayers);        }
    }

    [ClientRpc]
    private void PlayStartGameSoundClientRpc()
    {
        if (!sonidoPartidaIniciada.IsNull)
        {
            FMOD.Studio.EventInstance instInicio = FMODUnity.RuntimeManager.CreateInstance(sonidoPartidaIniciada);
            instInicio.start();
            instInicio.release();
        }
    }
}