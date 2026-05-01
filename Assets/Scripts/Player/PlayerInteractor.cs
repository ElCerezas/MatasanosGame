using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInteractor : NetworkBehaviour
{
    PlayerInput playerInput;
    public Transform holdPoint;
    [SerializeField] Transform cameraTransform;
    private bool isRagdoll = false;

    [Header("Interaction Settings")]
    [SerializeField] float interactRange = 3f;
    [SerializeField] LayerMask interactLayer;
    IGrabbable currentlyGrabbedItem;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerInput.OnInteractPressed += TryInteract;
        playerInput.OnPickUpPressed += TryGrabOrThrow;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        playerInput.OnInteractPressed -= TryInteract;
        playerInput.OnPickUpPressed -= TryGrabOrThrow;
    }
    void TryGrabOrThrow()
    {
        if (isRagdoll) return;
        if (currentlyGrabbedItem != null)
        {
            ReleaseObjectServerRpc(currentlyGrabbedItem.GetNetworkObjectID());
            currentlyGrabbedItem = null;
        }
        else
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                if (hit.collider.TryGetComponent(out IGrabbable grabbable))
                {
                    currentlyGrabbedItem = grabbable;
                    GrabObjectServerRpc(grabbable.GetNetworkObjectID());
                }
            }
        }
    }

    void TryInteract()
    {
        if (isRagdoll) return;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
                InteractServerRpc(interactable.GetNetworkObjectID());
        }
    }

    public void Ragdoll(bool active)
    {
        isRagdoll = active;
        if (active && currentlyGrabbedItem != null)
        {
            ReleaseObjectServerRpc(currentlyGrabbedItem.GetNetworkObjectID());
            currentlyGrabbedItem = null;
        }
    }

    #region ServerCom
    [ServerRpc]
    void InteractServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out IInteractable interact))
                interact.Interact(rpcParams.Receive.SenderClientId);
        }
    }

    [ServerRpc]
    void GrabObjectServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out NetworkClient client)) return;
        Transform serverHoldPoint = client.PlayerObject.GetComponent<PlayerInteractor>().holdPoint;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out IGrabbable grabbable))
                grabbable.AddGrabber(senderClientId, serverHoldPoint);
        }
    }

    [ServerRpc]
    void ReleaseObjectServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out IGrabbable grabbable))
                grabbable.RemoveGrabber(rpcParams.Receive.SenderClientId);
        }
    }
    #endregion
}