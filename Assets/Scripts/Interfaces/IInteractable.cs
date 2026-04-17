using UnityEngine;

public interface IInteractable
{
    virtual public void Interact(ulong clientID) { }
    virtual ulong GetNetworkObjectID() { return 0; }
}