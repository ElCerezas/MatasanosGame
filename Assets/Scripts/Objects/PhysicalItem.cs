using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    Rigidbody rb;
    Transform currentHoldPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Grab(Transform holdPoint)
    {
        currentHoldPoint = holdPoint;
    }
    public void Throw(Vector3 force)
    {
        currentHoldPoint = null;
        rb.AddForce(force, ForceMode.Impulse);
    }
}
