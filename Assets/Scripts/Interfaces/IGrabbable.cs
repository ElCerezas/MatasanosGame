using UnityEngine;

public interface IGrabbable
{
    void AddGrabber(ulong clientId, Transform holdPoint) { }
    void RemoveGrabber(ulong clientId) { }
}
