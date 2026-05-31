using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TaraCountUI : NetworkBehaviour
{
    [SerializeField] private RawImage FocoCount;
    [SerializeField] private RawImage DienteCount;
    [SerializeField] private RawImage VendaCount;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        EventBus.Subscribe<OnDientesCountChanged>(DientesCountChanged);
        EventBus.Subscribe<OnWoundFocoCountChanged>(WoundFocoCountChanged);
        EventBus.Subscribe<OnWoundVendaCountChanged>(WoundVendaCountChanged);

        var manager = FindFirstObjectByType<WinConditionManager>();
        if (manager == null)
        {
            return;
        }


        if (manager.IsSpawned)
        {
            UpdateDienteUI(manager.dienteCount.Value);
            UpdateFocoUI(manager.woundFocoCount.Value);
            UpdateVendaUI(manager.woundVendaCount.Value);
        }
        else
        {
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        EventBus.Unsubscribe<OnDientesCountChanged>(DientesCountChanged);
        EventBus.Unsubscribe<OnWoundFocoCountChanged>(WoundFocoCountChanged);
        EventBus.Unsubscribe<OnWoundVendaCountChanged>(WoundVendaCountChanged);
    }

    private void WoundVendaCountChanged(OnWoundVendaCountChanged changed)
    {
        UpdateVendaUI(changed.value);
    }

    private void WoundFocoCountChanged(OnWoundFocoCountChanged changed)
    {
        UpdateFocoUI(changed.value);
    }

    private void DientesCountChanged(OnDientesCountChanged changed)
    {
        UpdateDienteUI(changed.value);
    }

    private void UpdateFocoUI(int value)
    {
        bool active = value > 0;
        FocoCount.gameObject.SetActive(active);
    }

    private void UpdateDienteUI(int value)
    {
        bool active = value > 0;
        DienteCount.gameObject.SetActive(active);
    }

    private void UpdateVendaUI(int value)
    {
        bool active = value > 0;
        VendaCount.gameObject.SetActive(active);
    }
}