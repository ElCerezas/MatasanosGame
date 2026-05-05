using Unity.Netcode;
using UnityEngine;

public interface IGrabbable
{
    virtual void AddGrabber(ulong clientId, NetworkObject playerNetObj) {}
    virtual void RemoveGrabber(ulong clientId) { }
    virtual ulong GetNetworkObjectID() { return 0; }
}
