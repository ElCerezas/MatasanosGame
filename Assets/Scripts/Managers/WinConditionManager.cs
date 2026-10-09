using System;
using System.Collections;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinConditionManager : NetworkBehaviour
{
    [SerializeField] private int tarasHealedCount = 0;
    public NetworkVariable<int> dienteCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> woundFocoCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> woundVendaCount = new NetworkVariable<int>(0);

    private bool canExitToMenu = false;
    private bool gameEnded = false; 
    private bool isLeaving = false;

    public override void OnNetworkSpawn()
    {
        dienteCount.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnDientesCountChanged { value = newVal });
        };
        woundFocoCount.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnWoundFocoCountChanged { value = newVal });
        };
        woundVendaCount.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnWoundVendaCountChanged { value = newVal });
        };

        EventBus.Publish(new OnDientesCountChanged { value = dienteCount.Value });
        EventBus.Publish(new OnWoundFocoCountChanged { value = woundFocoCount.Value });
        EventBus.Publish(new OnWoundVendaCountChanged { value = woundVendaCount.Value });

        isLeaving = false;

        if (!IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (!IsServer) return;

        gameEnded = false;

        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Subscribe<TaraCreated>(OnTaraCreated);
        EventBus.Subscribe<AlienDeath>(OnAlienDeathServer);
        StartCoroutine(CountExistingTarasNextFrame());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            if (NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            return;
        }

        EventBus.Unsubscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Unsubscribe<TaraCreated>(OnTaraCreated);
        EventBus.Unsubscribe<AlienDeath>(OnAlienDeathServer);
    }
    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer) return;

        bool hostLeft = clientId == NetworkManager.ServerClientId;
        bool meDisconnected = clientId == NetworkManager.LocalClientId;

        if (hostLeft || meDisconnected)
        {
            ReturnToMainMenu();
        }
    }

    private void OnTaraCreated(TaraCreated e)
    {
        switch (e.Type)
        {
            case WoundType.Diente: dienteCount.Value++; break;
            case WoundType.HeridaFoco: woundFocoCount.Value++; break;
            case WoundType.HeridaVenda: woundVendaCount.Value++; break;
            default:
                break;
        }
    }

    private IEnumerator CountExistingTarasNextFrame()
    {
        yield return null;

        var allTaras = FindObjectsByType<TaraBase>(FindObjectsSortMode.None);

        dienteCount.Value = 0;
        woundFocoCount.Value = 0;
        woundVendaCount.Value = 0;

        foreach (var tara in allTaras)
        {
            if (!tara.IsSpawned) continue;
            if (tara.WasHealed) continue;

            if (tara.type == WoundType.Diente)
                dienteCount.Value++;

            if (tara.type == WoundType.HeridaFoco)
                woundFocoCount.Value++;

            if (tara.type == WoundType.HeridaVenda)
                woundVendaCount.Value++;
        }
    }

    private void OnTaraHealed(TaraHealedEvent e)
    {
        switch (e.Type)
        {
            case WoundType.Diente: dienteCount.Value--; break;
            case WoundType.HeridaFoco: woundFocoCount.Value--; break;
            case WoundType.HeridaVenda: woundVendaCount.Value--; break;
            default:
                break;
        }

        tarasHealedCount++;

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        bool allZero = dienteCount.Value == 0 && woundFocoCount.Value == 0 && woundVendaCount.Value == 0;

        if (allZero && tarasHealedCount > 0)
        {
            TriggerVictory();
        }
    }


    private void TriggerVictory()
    {
        if (!IsServer || gameEnded) return;
        gameEnded = true;

        EventBus.Publish(new VictoryEvent());
        NotifyVictoryClientRpc();
        StartCoroutine(EnableExitToMenuAfterDelay(3f));
    }

    private void OnAlienDeathServer(AlienDeath e)
    {
        if (!IsServer || gameEnded) return;
        gameEnded = true;

        NotifyDefeatClientRpc();
        StartCoroutine(EnableExitToMenuAfterDelay(3f));
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        if (!IsServer)
        {
            EventBus.Publish(new VictoryEvent());
            StartCoroutine(EnableExitToMenuAfterDelay(3f));
        }
    }

    [ClientRpc]
    private void NotifyDefeatClientRpc()
    {
        if (!IsServer)
        {
            EventBus.Publish(new AlienDeath());
            StartCoroutine(EnableExitToMenuAfterDelay(3f));
        }
    }

    private IEnumerator EnableExitToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canExitToMenu = true;
    }
    private async void ReturnToMainMenu()
    {
        if (isLeaving) return;
        isLeaving = true;
        canExitToMenu = false; 

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        await CloseOrLeaveSessionAsync();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private async System.Threading.Tasks.Task CloseOrLeaveSessionAsync()
    {
        var session = UIMultiplayerSessionManager.CurrentSession;
        if (session == null) return;

        try
        {
            if (session.IsHost)
            {
                await session.AsHost().DeleteAsync();
                Debug.Log("[WinConditionManager] Sesión eliminada por el host.");
            }
            else
            {
                await session.LeaveAsync();
                Debug.Log("[WinConditionManager] Sesión abandonada por el cliente.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WinConditionManager] No se pudo cerrar/abandonar la sesión: {ex.Message}");
        }
    }

    [ServerRpc]
    public void ForceGameEndServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("[WinConditionManager] Fin de partida forzado por el host.");
        TriggerVictory();
    }

    private void Update()
    {

        if (canExitToMenu && Input.anyKeyDown)
        {
            ReturnToMainMenu();
        }
    }
}