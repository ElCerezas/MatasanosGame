using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    protected Rigidbody rb;
    public Transform holdPoint;
    float damping = 5f;
    float springForce = 100f;
    float breakDistance = 5f;
    public Dictionary<ulong, Transform> grabbers = new Dictionary<ulong, Transform>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsServer || grabbers.Count == 0) return;
        Vector3 netForce = Vector3.zero;
        List<ulong> brokenGrabs = new List<ulong>();
        foreach (var kvp in grabbers)
        {
            if (kvp.Value == null) continue;
            Transform hPoint = kvp.Value;
            if (hPoint == null)
                brokenGrabs.Add(kvp.Key);
            Vector3 directionToTarget = hPoint.position - rb.position;
            float distance = directionToTarget.magnitude;
            if (distance > breakDistance)
                brokenGrabs.Add(kvp.Key);
            Vector3 fResult = directionToTarget * springForce;
            netForce += fResult;
        }
        foreach (ulong clientId in brokenGrabs)
            RemoveGrabber(clientId);
        if (grabbers.Count > 0)
        {
            netForce += -rb.linearVelocity * damping;
            rb.AddForce(netForce, ForceMode.Force);
        }
    }

    public virtual void AddGrabber(ulong clientId, Transform holdPoint)
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
            TryGetComponent(out NetworkObject netObj);
            netObj.ChangeOwnership(clientId);
        }
    }

    public virtual void RemoveGrabber(ulong clientId)
    {
        if (grabbers.ContainsKey(clientId))
            grabbers.Remove(clientId);
    }

    public ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }
}