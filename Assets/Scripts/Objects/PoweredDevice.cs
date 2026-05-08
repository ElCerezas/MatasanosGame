using Unity.Netcode;
public abstract class PoweredDevice : NetworkBehaviour
{
    protected NetworkVariable<bool> hasPower = new NetworkVariable<bool>(true);
    public override void OnNetworkSpawn()
    {   
        EventBus.Subscribe<GeneratorEvent>(OnPowerUpdated);
    }
    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<GeneratorEvent>(OnPowerUpdated);
    }
    void OnPowerUpdated(GeneratorEvent s)
    {
        hasPower.Value = s.IsGeneratorOn;
        Powered();
    }
    public abstract void Powered();

}
