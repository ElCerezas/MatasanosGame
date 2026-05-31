using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WinConditionManager : NetworkBehaviour
{
    [SerializeField] private int tarasHealedCount = 0;
    public NetworkVariable<int> dienteCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> woundFocoCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> woundVendaCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[WinCondition] OnNetworkSpawn | IsServer:{IsServer} | IsClient:{IsClient}");

        dienteCount.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[WinCondition] dienteCount changed: {oldVal} → {newVal}");
            EventBus.Publish(new OnDientesCountChanged { value = newVal });
        };
        woundFocoCount.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[WinCondition] woundFocoCount changed: {oldVal} → {newVal}");
            EventBus.Publish(new OnWoundFocoCountChanged { value = newVal });
        };
        woundVendaCount.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[WinCondition] woundVendaCount changed: {oldVal} → {newVal}");
            EventBus.Publish(new OnWoundVendaCountChanged { value = newVal });
        };

        Debug.Log($"[WinCondition] Publishing initial values → dientes:{dienteCount.Value} focos:{woundFocoCount.Value} vendas:{woundVendaCount.Value}");
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
        Debug.Log($"[WinCondition] OnTaraCreated | Type:{e.Type} | antes → dientes:{dienteCount.Value} focos:{woundFocoCount.Value} vendas:{woundVendaCount.Value}");

        switch (e.Type)
        {
            case WoundType.Diente:      dienteCount.Value++;      break;
            case WoundType.HeridaFoco:  woundFocoCount.Value++;   break;
            case WoundType.HeridaVenda: woundVendaCount.Value++;  break;
            default:
                Debug.LogWarning($"[WinCondition] OnTaraCreated | WoundType no reconocido: {e.Type}");
                break;
        }

        Debug.Log($"[WinCondition] OnTaraCreated | después → dientes:{dienteCount.Value} focos:{woundFocoCount.Value} vendas:{woundVendaCount.Value}");
    }

    private IEnumerator CountExistingTarasNextFrame()
    {
        yield return null;

        var allTaras = FindObjectsByType<TaraBase>(FindObjectsSortMode.None);
        Debug.Log($"[WinCondition] CountExistingTaras | TaraBase encontradas en escena: {allTaras.Length}");

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
                Debug.LogWarning($"[WinCondition] Tara '{tara.name}' IGNORADA — no está spawneada en red");
                skippedNotSpawned++;
                continue;
            }
            if (tara.WasHealed)
            {
                Debug.Log($"[WinCondition] Tara '{tara.name}' IGNORADA — ya fue curada");
                skippedHealed++;
                continue;
            }

            Debug.Log($"[WinCondition] Tara '{tara.name}' CONTADA | Type:{tara.type}");

            if (tara.type == WoundType.Diente)
                dienteCount.Value++;
            
            if (tara.type == WoundType.HeridaFoco)
                woundFocoCount.Value++;
            
            if (tara.type == WoundType.HeridaVenda)
                woundVendaCount.Value++;

            counted++;
        }

        Debug.Log($"[WinCondition] CountExistingTaras RESUMEN | contadas:{counted} | skippedNotSpawned:{skippedNotSpawned} | skippedHealed:{skippedHealed}");
        Debug.Log($"[WinCondition] Valores finales → dientes:{dienteCount.Value} focos:{woundFocoCount.Value} vendas:{woundVendaCount.Value}");
    }

    private void OnTaraHealed(TaraHealedEvent e)
    {
        Debug.Log($"[WinCondition] OnTaraHealed | Type:{e.Type} | antes → dientes:{dienteCount.Value} focos:{woundFocoCount.Value} vendas:{woundVendaCount.Value} | tarasHealedCount:{tarasHealedCount}");

        switch (e.Type)
        {
            case WoundType.Diente:      dienteCount.Value--;      break;
            case WoundType.HeridaFoco:  woundFocoCount.Value--;   break;
            case WoundType.HeridaVenda: woundVendaCount.Value--;  break;
            default:
                Debug.LogWarning($"[WinCondition] OnTaraHealed | WoundType no reconocido: {e.Type}");
                break;
        }

        tarasHealedCount++;
        Debug.Log($"[WinCondition] OnTaraHealed | después → dientes:{dienteCount.Value} focos:{woundFocoCount.Value} vendas:{woundVendaCount.Value} | tarasHealedCount:{tarasHealedCount}");

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        bool allZero = dienteCount.Value == 0 && woundFocoCount.Value == 0 && woundVendaCount.Value == 0;
        Debug.Log($"[WinCondition] CheckWinCondition | allZero:{allZero} | tarasHealedCount:{tarasHealedCount}");

        if (allZero && tarasHealedCount > 0)
        {
            Debug.Log("[WinCondition] ¡VICTORIA! Enviando NotifyVictoryClientRpc");
            NotifyVictoryClientRpc();
        }
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        Debug.Log("NotifyVictoryClientRpc received!");
        EventBus.Publish(new VictoryEvent());
    }
}