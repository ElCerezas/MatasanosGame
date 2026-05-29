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
        if (manager != null && manager.IsSpawned)
        {
            UpdateDienteUI(manager.dienteCount.Value);
            UpdateFocoUI(manager.woundFocoCount.Value);
            UpdateVendaUI(manager.woundVendaCount.Value);
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
        // FocoCount.text = value.ToString();
        FocoCount.gameObject.SetActive(value > 0);
    }

    private void UpdateDienteUI(int value)
    {
        // DienteCount.text = value.ToString();
        DienteCount.gameObject.SetActive(value > 0);
    }

    private void UpdateVendaUI(int value)
    {
        //VendaCount.text = value.ToString();
        VendaCount.gameObject.SetActive(value > 0);
    }
}