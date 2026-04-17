using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : NetworkBehaviour, IInteractable
{
    public UnityEvent onInteract;

    public void Interact(ulong clientID)
    {
        onInteract?.Invoke();
    }
    public ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }
}
