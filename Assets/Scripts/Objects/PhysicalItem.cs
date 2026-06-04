using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    public Transform holdPoint;
    [SerializeField] bool isTool;

    float damping = 5f;
    float springForce = 100f;
    float breakDistance = 5f;
    float toolRotationSpeed = 15f;

    protected Rigidbody rb;
    public Dictionary<ulong, Transform> grabbers = new Dictionary<ulong, Transform>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        var no = GetComponent<NetworkObject>();
        no.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
        no.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);

        GetComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
    }

    void FixedUpdate()
    {
        if (!IsOwner || grabbers.Count == 0) return;

        Vector3 netForce = Vector3.zero;
        List<ulong> brokenGrabs = new List<ulong>();
        Transform singleGrabTransform = null;

        foreach (var kvp in grabbers)
        {
            if (kvp.Value == null)
            {
                brokenGrabs.Add(kvp.Key);
                continue;
            }

            Vector3 directionToTarget = kvp.Value.position - rb.position;
            float distance = directionToTarget.magnitude;

            if (distance > breakDistance)
            {
                brokenGrabs.Add(kvp.Key);
                continue;
            }

            netForce += directionToTarget * springForce;

            if (grabbers.Count == 1)
                singleGrabTransform = kvp.Value;
        }

        foreach (ulong clientId in brokenGrabs)
            RemoveGrabber(clientId);

        if (grabbers.Count > 0)
        {
            netForce += -rb.linearVelocity * damping;
            rb.AddForce(netForce, ForceMode.Force);

            if (isTool && grabbers.Count == 1 && singleGrabTransform != null)
            {
                Quaternion targetRotation = singleGrabTransform.rotation;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                    Time.fixedDeltaTime * toolRotationSpeed));
            }
        }
    }

    #region Grab

    public virtual void AddGrabber(ulong clientId, NetworkObject playerNetObj)
    {
        RequestGrabServerRpc(clientId, playerNetObj);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestGrabServerRpc(ulong clientId, NetworkObjectReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out NetworkObject playerNetObj)) return;

        PlayerInteractor interactor = playerNetObj.GetComponent<PlayerInteractor>();
        if (interactor == null)
        {
            Debug.LogError("[PhysicalItem] PlayerInteractor no encontrado.");
            return;
        }

        if (!grabbers.ContainsKey(clientId))
            grabbers.Add(clientId, interactor.holdPoint);

        if (TryGetComponent(out SnappableItem snappable))
        {
            if (snappable.isSnapped && snappable.currentZone != null)
                snappable.currentZone.ReleaseItem();
        }

        UpdateOwnership();
        ConfirmGrabClientRpc(clientId, playerNetObjRef);
    }

    [ClientRpc]
    void ConfirmGrabClientRpc(ulong clientId, NetworkObjectReference playerNetObjRef)
    {
        if (!playerNetObjRef.TryGet(out NetworkObject playerNetObj)) return;

        PlayerInteractor interactor = playerNetObj.GetComponent<PlayerInteractor>();
        if (interactor == null) return;

        if (!grabbers.ContainsKey(clientId))
        {
            grabbers.TryAdd(clientId, interactor.holdPoint);

            if (TryGetComponent(out SnappableItem snappable))
            {
                if (snappable.isSnapped && snappable.currentZone != null)
                {
                    snappable.currentZone.ReleaseItem();
                    rb.isKinematic = false;
                }
            }
        }
    }

    #endregion

    #region Drop

    public virtual void RemoveGrabber(ulong clientId)
    {
        RequestDropServerRpc(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDropServerRpc(ulong clientId)
    {
        grabbers.Remove(clientId);
        UpdateOwnership();
        ConfirmDropClientRpc(clientId);
    }

    [ClientRpc]
    private void ConfirmDropClientRpc(ulong clientId)
    {
        grabbers.Remove(clientId);
    }

    #endregion

    void UpdateOwnership()
    {
        ulong newOwner;

        if (grabbers.Count == 1)
        {
            newOwner = grabbers.Keys.First();
            NetworkObject.ChangeOwnership(newOwner);
        }
        else
        {
            newOwner = NetworkManager.ServerClientId;
            NetworkObject.RemoveOwnership();
        }

        PropagateOwnershipToSnapZones(newOwner);
    }

    void PropagateOwnershipToSnapZones(ulong newOwner)
    {
        foreach (var zone in GetComponentsInChildren<SnapZone>(includeInactive: true))
        {
            if (!zone.NetworkObject.IsSpawned) continue;

            if (zone.NetworkObject.OwnerClientId != newOwner)
                zone.NetworkObject.ChangeOwnership(newOwner);

            if (zone.currentItem != null &&
                zone.currentItem.NetworkObject.IsSpawned &&
                zone.currentItem.NetworkObject.OwnerClientId != newOwner)
            {
                zone.currentItem.NetworkObject.ChangeOwnership(newOwner);
            }
        }
    }

    public ulong GetNetworkObjectID() => NetworkObjectId;
}