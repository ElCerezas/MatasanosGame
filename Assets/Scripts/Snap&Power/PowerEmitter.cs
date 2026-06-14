using System;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SnapZone))]
public class PowerEmitter : NetworkBehaviour
{
    public NetworkVariable<bool> hasPower = new NetworkVariable<bool>(false);
    SnapZone zone;
    PowerReceiver connectedReceiver;
    public GameObject soundEmitter;

    private void Awake()
    {
        zone = GetComponent<SnapZone>();
    }

    public override void OnNetworkSpawn()
    {
        zone.OnObjectSnapped.AddListener(OnPlug);
        zone.OnObjectUnsnapped.AddListener(OnUnplug);
    }

    public void OnPlug()
    {
        if (!IsServer) return;
        if (zone.currentItem != null && zone.currentItem.TryGetComponent(out PowerReceiver pr))
        {
            connectedReceiver = pr;
            connectedReceiver.SetPower(hasPower.Value, this);
        }
    }

    public void OnUnplug()
    {
        if (!IsServer) return;
        if (connectedReceiver != null)
        {
            connectedReceiver.SetPower(false, this);
            connectedReceiver = null;
        }
    }

    public void SetEmitting(bool emitting)
    {
        if (!IsServer) return;
        hasPower.Value = emitting;
        if (connectedReceiver != null)
        {
            connectedReceiver.SetPower(hasPower.Value, this);
        }
    }
}