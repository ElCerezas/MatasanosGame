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
    
    PowerEmitter currentSource;
    public void SetPower(bool isPowered, PowerEmitter emiter)
    {
        if (!IsServer) return;
        if (wiredEmiters.Contains(emiter)) return;

        hasPower.Value = isPowered;
        currentSource = emiter;


        if (wiredEmiters.Length > 0)
        {
            foreach (PowerEmitter p in wiredEmiters)
            {
                p.SetEmitting(hasPower.Value);
            }
        }
        if (toolToPower != null)
            toolToPower.SetPowered(hasPower.Value);

        ConectedConsumerChanged();
    }
    public int GetLoad()
    {
        int totalLoad = 0;
        if (toolToPower != null)
        {
            totalLoad += toolToPower.GetLoad();
        }

        foreach (PowerEmitter p in wiredEmiters)
        {
            totalLoad += p.GetLoad();
        }
        Debug.Log("ToolLoadChecked");
        return totalLoad;
    }
    public void ConectedConsumerChanged()
    {
        if (!IsServer) return;
        if (currentSource != null)
            currentSource.ConectedReciverChanged();
    }
}
