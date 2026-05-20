using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TaraCountUI : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI FocoCount;
    [SerializeField] private TextMeshProUGUI VendaCount;
    [SerializeField] private TextMeshProUGUI DienteCount;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EventBus.Subscribe<OnDientesCountChanged>(DientesCountChanged);
        EventBus.Subscribe<OnWoundFocoCountChanged>(WoundFocoCountChanged);
        EventBus.Subscribe<OnWoundVendaCountChanged>(WoundVendaCountChanged);

        var manager = FindFirstObjectByType<WinConditionManager>();
        if (manager != null)
        {
            DienteCount.text = manager.dienteCount.Value + " Dientes";
            FocoCount.text = manager.woundFocoCount.Value + " Foco";
            VendaCount.text = manager.woundVendaCount.Value + " Venda";
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
        VendaCount.text = changed.value.ToString() + "Venda";
    }

    private void WoundFocoCountChanged(OnWoundFocoCountChanged changed)
    {
        FocoCount.text = changed.value.ToString() + "Foco";
    }

    private void DientesCountChanged(OnDientesCountChanged changed)
    {
        DienteCount.text = changed.value.ToString() + "Dientes";
    }

}
