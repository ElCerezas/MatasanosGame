using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;

using UnityEngine;

[RequireComponent(typeof(ClientNetworkTransform))]
public class ToolItem : PhysicalItem
{
    private Transform localHoldPoint;

    private void FixedUpdate()
    {
        if (!IsOwner || localHoldPoint == null) return;
        rb.MovePosition(localHoldPoint.position);
        Vector3 forward = localHoldPoint.forward;
        forward.y = 0f;
        if (forward != Vector3.zero)
            rb.MoveRotation(Quaternion.LookRotation(forward));
    }

    public override void AddGrabber(ulong clientId, Transform holdPoint)
    {
        base.AddGrabber(clientId, holdPoint);
        NetworkObject.ChangeOwnership(clientId);
        EnableToolPhysicsClientRpc(clientId);

        ulong holderNetId = holdPoint.GetComponentInParent<NetworkObject>().NetworkObjectId;
        SetLocalHoldPointClientRpc(holderNetId, clientId);
        TeleportToHoldPointClientRpc(holdPoint.position, holdPoint.rotation, clientId);
        NotifyHoldingToolClientRpc(true, clientId);
    }

    public override void RemoveGrabber(ulong clientId)
    {
        base.RemoveGrabber(clientId);
        NetworkObject.RemoveOwnership();
        ClearLocalHoldPointClientRpc(clientId);
        NotifyHoldingToolClientRpc(false, clientId);
    }

    [ClientRpc]
    private void EnableToolPhysicsClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    [ClientRpc]
    private void SetLocalHoldPointClientRpc(ulong holderNetId, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(holderNetId, out NetworkObject holderNetObj))
            localHoldPoint = holderNetObj.GetComponent<PlayerInteractor>().holdPoint;
    }

    [ClientRpc]
    private void ClearLocalHoldPointClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        localHoldPoint = null;
        rb.isKinematic = false;
        rb.useGravity = true;
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
}