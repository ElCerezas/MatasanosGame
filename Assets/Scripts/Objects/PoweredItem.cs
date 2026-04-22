using Unity.Netcode;
public class PoweredItem : InteractableItem
{

    public int energyLoad = 1;
    public NetworkVariable<bool> isTurnedOn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasPower = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    public void SetPowered(bool powered)
    {
        if (!IsServer) return;
        hasPower.Value = powered;
        if (!powered)
            isTurnedOn.Value = false;
    }
    public override void Interact(ulong clientID)
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;
        isTurnedOn.Value = !isTurnedOn.Value;
    }
    public virtual void Update()
    {
        if (!IsServer)return;
        if(!isTurnedOn.Value) return;

        //Acción
    }
}