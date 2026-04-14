using UnityEngine;

public interface IGrabbable
{
    public void Grab(Transform holdPoint) { }
    public void Throw(Vector3 force) { }
    public void Drop() { }
}
