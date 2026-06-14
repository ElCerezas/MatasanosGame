using Unity.Netcode;
using UnityEngine;

public class PoweredItem : InteractableItem
{
    public NetworkVariable<bool> isTurnedOn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasPower = new NetworkVariable<bool>(false);

    [Header("PowerLoad System")]
    [SerializeField] PowerReceiver ownPlug;
    public int energyLoad = 1;

    private bool isConsuming = false;

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

        UpdateLoadStatus();
    }

    public override void Interact(ulong clientID)
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;

        isTurnedOn.Value = !isTurnedOn.Value;

        UpdateLoadStatus();
    }

    private void UpdateLoadStatus()
    {
        bool shouldConsume = hasPower.Value && isTurnedOn.Value;

        if (shouldConsume != isConsuming)
        {
            isConsuming = shouldConsume;
            int loadChange = isConsuming ? energyLoad : -energyLoad;

            EventBus.Publish<AddEnergyLoad>(new AddEnergyLoad { energyLoad = loadChange });
        }
    }
}