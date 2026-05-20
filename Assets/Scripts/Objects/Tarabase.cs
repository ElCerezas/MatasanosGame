using Unity.Netcode;
using UnityEngine;
public abstract class TaraBase : NetworkBehaviour
{
    private bool _wasHealed = false;
    public bool WasHealed => _wasHealed;

    [SerializeField] public WoundType type;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    protected void MarkAsHealed()
    {
        if (!IsServer || _wasHealed) return;
        _wasHealed = true;
        EventBus.Publish(new TaraHealedEvent { TaraID = NetworkObjectId, Type = type });
    }

    protected void UnmarkAsHealed()
    {
        if (!IsServer || !_wasHealed) return;
        _wasHealed = false;
        EventBus.Publish(new TaraCreated { TaraID = NetworkObjectId, Type = type });
    }
}