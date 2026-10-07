using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinConditionManager : NetworkBehaviour
{
    [SerializeField] private int tarasHealedCount = 0;
    public NetworkVariable<int> dienteCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> woundFocoCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> woundVendaCount = new NetworkVariable<int>(0);

    private bool canExitToMenu = false;

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

        if (!IsServer) return;

        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Subscribe<TaraCreated>(OnTaraCreated);
        StartCoroutine(CountExistingTarasNextFrame());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        EventBus.Unsubscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Unsubscribe<TaraCreated>(OnTaraCreated);
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

        int skippedNotSpawned = 0;
        int skippedHealed = 0;
        int counted = 0;

        foreach (var tara in allTaras)
        {
            if (!tara.IsSpawned)
            {
                skippedNotSpawned++;
                continue;
            }
            if (tara.WasHealed)
            {
                skippedHealed++;
                continue;
            }

            if (tara.type == WoundType.Diente)
                dienteCount.Value++;

            if (tara.type == WoundType.HeridaFoco)
                woundFocoCount.Value++;

            if (tara.type == WoundType.HeridaVenda)
                woundVendaCount.Value++;

            counted++;
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
            NotifyVictoryServerRpc();
        }
    }

    [ServerRpc]
    private void NotifyVictoryServerRpc()
    {
        EventBus.Publish(new VictoryEvent());
        NotifyVictoryClientRpc();
    }

    [ServerRpc]
    private void NotifyDefeatServerRpc()
    {
        EventBus.Publish(new AlienDeath());
        NotifyDefeatClientRpc();
    }

    [ClientRpc]
    private void NotifyDefeatClientRpc()
    {
        EventBus.Publish(new AlienDeath());
        StartCoroutine(EnableExitToMenuAfterDelay(3f));
    }
    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        EventBus.Publish(new VictoryEvent());
        StartCoroutine(EnableExitToMenuAfterDelay(3f));
    }

    private IEnumerator EnableExitToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canExitToMenu = true;
    }

    private void ReturnToMainMenu()
    {
        canExitToMenu = false; // Desactivar para que no se ejecute múltiples veces al spamear botones

        // Desbloquear y mostrar el cursor para el menú principal
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }

    [ServerRpc]
    public void ForceGameEndServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("[WinConditionManager] Fin de partida forzado por el host.");
        NotifyVictoryClientRpc();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && IsServer)
        {
            NotifyVictoryServerRpc();
        }
        if (Input.GetKeyDown(KeyCode.B) && IsServer)
        {
            NotifyDefeatServerRpc();
        }

        // Si ya terminó la cinemática y el jugador presiona cualquier tecla/clic
        if (canExitToMenu && Input.anyKeyDown)
        {
            ReturnToMainMenu();
        }
    }
}