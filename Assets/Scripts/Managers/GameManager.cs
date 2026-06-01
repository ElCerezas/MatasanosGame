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

        NetworkManager.OnClientConnectedCallback += SpawnPlayerWithDefaultLogic;
        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;
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