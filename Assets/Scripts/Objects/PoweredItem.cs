using Unity.Netcode;
using UnityEngine;
public class PoweredItem : InteractableItem
{
    public NetworkVariable<bool> isTurnedOn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasPower = new NetworkVariable<bool>(false);
    [Header("PowerLoad System")]
    [SerializeField] PowerReceiver ownPlug;
    public int energyLoad = 1;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    public void SetPowered(bool powered)
    {
        if (!IsServer) return;
        hasPower.Value = powered;
        if (!powered)
        {
            isTurnedOn.Value = false;
        }
    }
    public override void Interact(ulong clientID)
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;
        isTurnedOn.Value = !isTurnedOn.Value;
        ownPlug.ConectedConsumerChanged();
    }
    public int GetLoad()
    {
        return (hasPower.Value && isTurnedOn.Value) ? energyLoad : 0;
    }
}