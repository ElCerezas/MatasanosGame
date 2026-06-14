using Unity.Netcode;
using UnityEngine;

public class PowerReceiver : NetworkBehaviour
{
    [SerializeField] private PoweredItem targetItem;
    [SerializeField] private PowerEmitter[] sockets;

    private void Awake()
    {
        if (targetItem == null)
        {
            targetItem = GetComponent<PoweredItem>();
        }
    }

    public void SetPower(bool hasPower, PowerEmitter emitter)
    {
        if (!IsServer) return;

        if (targetItem != null)
        {
            targetItem.SetPowered(hasPower);
        }
        if(sockets != null)
        {
            foreach (var socket in sockets)
            {
                socket.SetEmitting(hasPower);
            }
        }
    }
}