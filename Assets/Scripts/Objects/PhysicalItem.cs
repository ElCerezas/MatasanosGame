using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    Rigidbody rb;
    public Transform holdPoint;
    [SerializeField] private bool isTool = false;
    float damping = 5f;
    float springForce = 100f;
    float breakDistance = 5f;
    public Dictionary<ulong, Transform> grabbers = new Dictionary<ulong, Transform>();

    private Transform localHoldPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isTool)
        {
            if (!IsOwner || localHoldPoint == null) return;
            rb.MovePosition(localHoldPoint.position);
            Vector3 forward = localHoldPoint.forward;
            forward.y = 0f;
            if (forward != Vector3.zero)
                rb.MoveRotation(Quaternion.LookRotation(forward));
            return;
        }

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
            TryGetComponent(out NetworkObject netObj);
            netObj.ChangeOwnership(clientId);

            if (isTool)
            {
                EnableToolPhysicsClientRpc(clientId);
                TeleportToHoldPointClientRpc(holdPoint.position, holdPoint.rotation, clientId);

                ulong holderNetId = holdPoint.GetComponentInParent<NetworkObject>().NetworkObjectId;
                string holdPointPath = GetRelativePath(holdPoint, holdPoint.GetComponentInParent<NetworkObject>().transform);
                SetLocalHoldPointClientRpc(holderNetId, holdPointPath, clientId);
            }
            else
            {
                GetComponent<ClientNetworkTransform>().Teleport(holdPoint.position, holdPoint.rotation, transform.localScale);
            }
        }
        if (isTool)
            NotifyHoldingToolClientRpc(true, clientId);
    }

    public void RemoveGrabber(ulong clientId)
    {
        if (grabbers.ContainsKey(clientId))
            grabbers.Remove(clientId);
        if (isTool)
        {
            ClearLocalHoldPointClientRpc(clientId);
            NotifyHoldingToolClientRpc(false, clientId);
        }
    }

    private string GetRelativePath(Transform target, Transform root)
    {
        if (target == root) return "";
        string path = target.name;
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    [ClientRpc]
    private void SetLocalHoldPointClientRpc(ulong holderNetId, string holdPointPath, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(holderNetId, out NetworkObject holderNetObj))
        {
            if (string.IsNullOrEmpty(holdPointPath))
                localHoldPoint = holderNetObj.transform;
            else
                localHoldPoint = holderNetObj.transform.Find(holdPointPath);
        }
    }

    [ClientRpc]
    private void ClearLocalHoldPointClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        localHoldPoint = null;
    }

    [ClientRpc]
    private void EnableToolPhysicsClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        rb.isKinematic = false;
    }

    [ClientRpc]
    private void TeleportToHoldPointClientRpc(Vector3 position, Quaternion rotation, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        StartCoroutine(TeleportWhenOwner(position, rotation));
    }

    private IEnumerator TeleportWhenOwner(Vector3 position, Quaternion rotation)
    {
        yield return new WaitUntil(() => IsOwner);
        rb.position = position;
        rb.rotation = rotation;
    }

    [ClientRpc]
    private void NotifyHoldingToolClientRpc(bool holding, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            PlayerCamera cam = localPlayer.GetComponentInChildren<PlayerCamera>();
            if (cam != null)
                cam.SetHoldingTool(holding);
        }
    }

    public ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }
}