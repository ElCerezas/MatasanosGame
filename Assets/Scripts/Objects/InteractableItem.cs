using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : NetworkBehaviour, IInteractable
{
    public UnityEvent onInteract;

    public virtual void Interact(ulong clientID)
    {
        onInteract?.Invoke();
    }
    public virtual ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }
}
