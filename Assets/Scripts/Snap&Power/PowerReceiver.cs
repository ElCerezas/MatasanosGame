using Unity.Netcode;
using UnityEngine;
using System;
using System.Linq;

[RequireComponent(typeof(SnappableItem))]
public class PowerReceiver : NetworkBehaviour
{
    public NetworkVariable<bool> hasPower = new NetworkVariable<bool>(false);

    [Header("Wired Connections")]
    [SerializeField] PowerEmitter[] wiredEmiters = new PowerEmitter [0];
    [SerializeField] PoweredItem toolToPower;

    public void SetPower(bool isPowered, PowerEmitter emiter)
    {
        if (!IsServer) return;
        if (wiredEmiters.Contains(emiter)) return;
        hasPower.Value = isPowered;
        if (wiredEmiters.Length >= 0)
        {
            foreach (PowerEmitter p in wiredEmiters)
            {
                p.SetEmitting(hasPower.Value);
            }
        }
        if (toolToPower != null)
            toolToPower.hasPower.Value = hasPower.Value;
    }
}
