using Unity.Netcode;
using UnityEngine;
public abstract class TaraBase : NetworkBehaviour
{
    private bool _wasHealed = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            EventBus.Publish(new TaraGeneratedEvent { TaraID = NetworkObjectId });
    }

    protected void MarkAsHealed()
    {
        if (!IsServer || _wasHealed) return;
        _wasHealed = true;
        EventBus.Publish(new TaraHealedEvent { TaraID = NetworkObjectId });
    }
}