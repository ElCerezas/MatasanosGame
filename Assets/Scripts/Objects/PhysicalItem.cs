using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    [SerializeField] bool isTool;
    public Transform holdPoint;

    float damping = 5f;
    float springForce = 100f;
    float breakDistance = 5f;
    float toolRotationSpeed = 15f;

    protected Rigidbody rb;
    public Dictionary<ulong, Transform> grabbers = new Dictionary<ulong, Transform>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!IsServer || grabbers.Count == 0) return;
        Vector3 netForce = Vector3.zero;
        List<ulong> brokenGrabs = new List<ulong>();
        Transform singleGrabberPoint = null;

        foreach (var kvp in grabbers)
        {
            Transform hPoint = kvp.Value;
            if (hPoint == null)
            {
                brokenGrabs.Add(kvp.Key);
                continue;
            }

            Vector3 directionToTarget = hPoint.position - rb.position;
            float distance = directionToTarget.magnitude;

            if (distance > breakDistance)
            {
                brokenGrabs.Add(kvp.Key);
                continue;
            }
            singleGrabberPoint = hPoint;

            Vector3 fResult = directionToTarget * springForce;
            netForce += fResult;
        }

        foreach (ulong clientId in brokenGrabs)
            RemoveGrabber(clientId);

        if (grabbers.Count > 0)
        {
            netForce += -rb.linearVelocity * damping;
            rb.AddForce(netForce, ForceMode.Force);

            if (isTool && grabbers.Count == 1 && singleGrabberPoint != null)
            {
                Quaternion targetRotation = singleGrabberPoint.rotation;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * toolRotationSpeed));
            }
        }
    }
    public void AddGrabber(ulong clientId, Transform holdPoint)
    {
        if (!grabbers.ContainsKey(clientId))
        {
            if (TryGetComponent(out SnappableItem snappable))
            {
                if (snappable.isSnapped && snappable.currentZone != null)
                {
                    Debug.Log("Release");
                    snappable.currentZone.ReleaseItem();
                    rb.isKinematic = false;
                }
            }
            grabbers.Add(clientId, holdPoint);
        }
    }
    public virtual void RemoveGrabber(ulong clientId)
    {
        if (grabbers.ContainsKey(clientId))
        {
            grabbers.Remove(clientId);
        }
    }
    public ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }
}