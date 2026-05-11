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
        GetComponent<NetworkObject>().SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
        GetComponent<NetworkObject>().SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
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
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * toolRotationSpeed));
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
            Debug.LogError("[PhysicalItem] RequestGrabServerRpc: PlayerInteractor no encontrado.");
            return;
        }
        if (TryGetComponent(out SnappableItem snappable))
        {
            if (snappable.isSnapped && snappable.currentZone != null)
            {
                snappable.currentZone.ReleaseItem();
            }
        }
        if (!grabbers.ContainsKey(clientId))
            grabbers.Add(clientId, interactor.holdPoint);

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
            if (TryGetComponent(out SnappableItem snappable))
            {
                Debug.Log(snappable.isSnapped && snappable.currentZone != null);
                if (snappable.isSnapped && snappable.currentZone != null)
                {
                    snappable.currentZone.ReleaseItem();
                    rb.isKinematic = false;
                }
            }

            grabbers.TryAdd(clientId, interactor.holdPoint);
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
        if (grabbers.Count == 1)
        {
            ulong onlyClient = grabbers.Keys.First();
            NetworkObject.ChangeOwnership(onlyClient);
        }
        else
        {
            NetworkObject.RemoveOwnership();
        }
    }

    public ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }
}