using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<GameObject> m_SpawnPositions = new List<GameObject>();
    private int currentSpawnIndex = 0;

    bool isCursorLocked = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        // Registrar callback de aprobación de conexión
        NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;
        
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerWithDefaultLogic;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;
    }
    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        int spawnIndex = 0;

        foreach (ulong clientId in clientsCompleted)
        {
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform selectedPoint = spawnPoints[spawnIndex % spawnPoints.Length].transform;
                spawnPos = selectedPoint.position;
                spawnRot = selectedPoint.rotation;
                spawnIndex++;
            }
            SpawnPlayer(clientId, spawnPos, spawnRot);
        }
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        /* Obtener LobbyManager para verificar si está bloqueado
        LobbyManager lobbyManager = FindFirstObjectByType<LobbyManager>();
        
        if (lobbyManager != null && lobbyManager.IsLobbyLocked())
        {
            Debug.Log($"[GameManager] Cliente {request.ClientNetworkId} rechazado. Lobby está bloqueado.");
            response.Approved = false;
            response.Reason = "Lobby is locked";
            return;
        }

        response.Approved = true;
        Debug.Log($"[GameManager] Cliente {request.ClientNetworkId} aprobado.");*/
        response.Approved = true;
        response.CreatePlayerObject = true;

        response.Position = GetNextSpawnPoint();
    }
    private Vector3 GetNextSpawnPoint()
    {
        if (m_SpawnPositions.Count == 0)
        {
            return new Vector3(UnityEngine.Random.Range(-3, 3), 2, UnityEngine.Random.Range(-3, 3));
        }

        Transform spawn = m_SpawnPositions[currentSpawnIndex].transform;
        currentSpawnIndex = (currentSpawnIndex + 1) % m_SpawnPositions.Count;
        return spawn.position;
    }
    private void SpawnPlayerWithDefaultLogic(ulong clientID)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
            spawnPos = randomPoint.position;
            spawnRot = randomPoint.rotation;
        }

        SpawnPlayer(clientID, spawnPos, spawnRot);
    }
    private void SpawnPlayer(ulong clientID, Vector3 position, Quaternion rotation)
    {
        if (NetworkManager.ConnectedClients[clientID].PlayerObject != null) return;

        GameObject player = Instantiate(playerPrefab, position, rotation);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.ConnectionApprovalCallback -= ApproveConnection;
            NetworkManager.OnClientConnectedCallback -= SpawnPlayerWithDefaultLogic;
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;
        }
        base.OnNetworkDespawn();
    }

    public void DisconnectClient()
    {
        NetworkManager.Shutdown();
    }

    public void StartClient()
    {
        NetworkManager.StartClient();
    }

    public void StartHost()
    {
        NetworkManager.StartHost();
    }

    public void Update()
    {
       
    }
}