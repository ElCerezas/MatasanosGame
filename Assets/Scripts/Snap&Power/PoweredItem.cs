using UnityEngine;
using Unity.Netcode;

public class PoweredItem : InteractableItem
{
    [SerializeField] PowerReceiver powerReceiver;
    public NetworkVariable<bool> isTurnedOn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasPower = new NetworkVariable<bool>(false);

    private void Awake()
    {
        powerReceiver = GetComponent<PowerReceiver>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    public override void Interact(ulong clientID)
    {
        if (!IsServer) return;
        if (!powerReceiver.hasPower.Value) return;

        isTurnedOn.Value = !isTurnedOn.Value;
    }
}