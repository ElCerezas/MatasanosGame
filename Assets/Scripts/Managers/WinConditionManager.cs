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
    private bool gameEnded = false; // Solo lo usa el servidor para no disparar el final dos veces

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

        gameEnded = false;

        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Subscribe<TaraCreated>(OnTaraCreated);
        EventBus.Subscribe<AlienDeath>(OnAlienDeathServer);
        StartCoroutine(CountExistingTarasNextFrame());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        EventBus.Unsubscribe<TaraHealedEvent>(OnTaraHealed);
        EventBus.Unsubscribe<TaraCreated>(OnTaraCreated);
        EventBus.Unsubscribe<AlienDeath>(OnAlienDeathServer);
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

    // ---------- FINALES (solo se ejecutan en el servidor/host) ----------

    private void TriggerVictory()
    {
        if (!IsServer || gameEnded) return;
        gameEnded = true;

        EventBus.Publish(new VictoryEvent()); // Pantalla en el Host
        NotifyVictoryClientRpc();             // Pantalla en los clientes
    }

    // El servidor escucha AlienDeath (lo publique quien lo publique) y lo retransmite a los clientes
    private void OnAlienDeathServer(AlienDeath e)
    {
        if (!IsServer || gameEnded) return;
        gameEnded = true;

        NotifyDefeatClientRpc();
        // El Host ya recibió el evento localmente, no hace falta volver a publicarlo
        StartCoroutine(EnableExitToMenuAfterDelay(3f));
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        // El Host ya publicó el evento en TriggerVictory
        if (!IsServer) EventBus.Publish(new VictoryEvent());
        StartCoroutine(EnableExitToMenuAfterDelay(3f));
    }

    [ClientRpc]
    private void NotifyDefeatClientRpc()
    {
        // Solo los clientes puros publican; el Host ya lo tiene
        if (!IsServer) EventBus.Publish(new AlienDeath());
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
        TriggerVictory();
    }

    private void Update()
    {
        // Debug: V = victoria, B = derrota (solo Host)
        if (Input.GetKeyDown(KeyCode.V) && IsServer)
        {
            TriggerVictory();
        }
        if (Input.GetKeyDown(KeyCode.B) && IsServer)
        {
            EventBus.Publish(new AlienDeath()); // OnAlienDeathServer hace el resto
        }

        // Si ya terminó la cinemática y el jugador presiona cualquier tecla/clic
        if (canExitToMenu && Input.anyKeyDown)
        {
            ReturnToMainMenu();
        }
    }
}