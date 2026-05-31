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
        Debug.Log($"[TaraCountUI] OnNetworkSpawn | IsServer:{IsServer} | IsClient:{IsClient}");

        EventBus.Subscribe<OnDientesCountChanged>(DientesCountChanged);
        EventBus.Subscribe<OnWoundFocoCountChanged>(WoundFocoCountChanged);
        EventBus.Subscribe<OnWoundVendaCountChanged>(WoundVendaCountChanged);

        var manager = FindFirstObjectByType<WinConditionManager>();
        if (manager == null)
        {
            Debug.LogWarning("[TaraCountUI] WinConditionManager NO encontrado en escena");
            return;
        }

        Debug.Log($"[TaraCountUI] WinConditionManager encontrado | IsSpawned:{manager.IsSpawned} | dientes:{manager.dienteCount.Value} focos:{manager.woundFocoCount.Value} vendas:{manager.woundVendaCount.Value}");

        if (manager.IsSpawned)
        {
            UpdateDienteUI(manager.dienteCount.Value);
            UpdateFocoUI(manager.woundFocoCount.Value);
            UpdateVendaUI(manager.woundVendaCount.Value);
        }
        else
        {
            Debug.LogWarning("[TaraCountUI] WinConditionManager existe pero aún no está spawneado, UI no se inicializará");
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
        Debug.Log($"[TaraCountUI] WoundVendaCountChanged | value:{changed.value}");
        UpdateVendaUI(changed.value);
    }

    private void WoundFocoCountChanged(OnWoundFocoCountChanged changed)
    {
        Debug.Log($"[TaraCountUI] WoundFocoCountChanged | value:{changed.value}");
        UpdateFocoUI(changed.value);
    }

    private void DientesCountChanged(OnDientesCountChanged changed)
    {
        Debug.Log($"[TaraCountUI] DientesCountChanged | value:{changed.value}");
        UpdateDienteUI(changed.value);
    }

    private void UpdateFocoUI(int value)
    {
        bool active = value > 0;
        Debug.Log($"[TaraCountUI] UpdateFocoUI | value:{value} | SetActive:{active} | FocoCount null:{FocoCount == null}");
        FocoCount.gameObject.SetActive(active);
    }

    private void UpdateDienteUI(int value)
    {
        bool active = value > 0;
        Debug.Log($"[TaraCountUI] UpdateDienteUI | value:{value} | SetActive:{active} | DienteCount null:{DienteCount == null}");
        DienteCount.gameObject.SetActive(active);
    }

    private void UpdateVendaUI(int value)
    {
        bool active = value > 0;
        Debug.Log($"[TaraCountUI] UpdateVendaUI | value:{value} | SetActive:{active} | VendaCount null:{VendaCount == null}");
        VendaCount.gameObject.SetActive(active);
    }
}