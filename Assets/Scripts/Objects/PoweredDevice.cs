using UnityEngine;
using Unity.Netcode;

public abstract class PoweredDevice : NetworkBehaviour
{
    protected NetworkVariable<bool> hasPower = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        hasPower.OnValueChanged += OnPowerStateChanged;
        if (IsServer)
        {
            EventBus.Subscribe<GeneratorEvent>(OnPowerUpdated);
        }
        Powered();
    }

    public override void OnNetworkDespawn()
    {
        hasPower.OnValueChanged -= OnPowerStateChanged;
        if (IsServer)
            EventBus.Unsubscribe<GeneratorEvent>(OnPowerUpdated);
    }
    private void OnPowerStateChanged(bool previousValue, bool newValue)
    {
        Powered();
    }

    void OnPowerUpdated(GeneratorEvent s)
    {
        hasPower.Value = s.IsGeneratorOn;
    }

    public abstract void Powered();
}