using Unity.Netcode;
using UnityEngine;
public abstract class TaraBase : NetworkBehaviour
{
    private bool _wasHealed = false;
    public bool WasHealed => _wasHealed;

    protected void MarkAsHealed()
    {
        if (!IsServer || _wasHealed) return;
        _wasHealed = true;
        EventBus.Publish(new TaraHealedEvent { TaraID = NetworkObjectId });
    }
}