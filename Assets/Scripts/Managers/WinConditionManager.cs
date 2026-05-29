// WinConditionManager.cs
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
        dienteCount.OnValueChanged += (_, newVal) =>
            EventBus.Publish(new OnDientesCountChanged { value = newVal });
        woundFocoCount.OnValueChanged += (_, newVal) =>
            EventBus.Publish(new OnWoundFocoCountChanged { value = newVal });
        woundVendaCount.OnValueChanged += (_, newVal) =>
            EventBus.Publish(new OnWoundVendaCountChanged { value = newVal });

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
            case WoundType.Diente:
                dienteCount.Value++;
                break;
            case WoundType.HeridaFoco:
                woundFocoCount.Value++;
                break;
            case WoundType.HeridaVenda:
                woundVendaCount.Value++;
                break;
        }
    }
    private IEnumerator CountExistingTarasNextFrame()
    {
        yield return null;

        dienteCount.Value = 0;
        woundFocoCount.Value = 0;
        woundVendaCount.Value = 0;

        foreach (var tara in FindObjectsByType<TaraBase>(FindObjectsSortMode.None))
        {
            if (!tara.IsSpawned || tara.WasHealed) continue;

            if (tara.type == WoundType.Diente)
                dienteCount.Value++;
        }
    }
    private void OnTaraHealed(TaraHealedEvent e)
    {
        switch (e.Type)
        {
            case WoundType.Diente:
                dienteCount.Value--;
                break;
            case WoundType.HeridaFoco:
                woundFocoCount.Value--;
                break;
            case WoundType.HeridaVenda:
                woundVendaCount.Value--;
                break;
        }
        tarasHealedCount++;
        //Debug.Log($"Tara healed, activeTarasCount: {activeTarasCount}, tarasHealedCount: {tarasHealedCount}");
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (dienteCount.Value == 0 && woundFocoCount.Value == 0 && woundVendaCount.Value == 0 && tarasHealedCount > 0)
            NotifyVictoryClientRpc();
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        Debug.Log("NotifyVictoryClientRpc received!");
        EventBus.Publish(new VictoryEvent());
    }
}