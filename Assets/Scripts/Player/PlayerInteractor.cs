using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInteractor : NetworkBehaviour
{
    PlayerInput playerInput;
    [SerializeField] Transform holdPoint;
    [SerializeField] Transform cameraTransform;

    [Header("Interaction Settings")]
    [SerializeField] float interactRange = 3f;
    [SerializeField] LayerMask interactLayer;

    PhysicalItem currentlyGrabbedItem;

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
        
        if (currentlyGrabbedItem != null)
        {
            ReleaseObjectServerRpc(currentlyGrabbedItem.NetworkObjectId);
            currentlyGrabbedItem = null;
        }
        else
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                if (hit.collider.TryGetComponent(out PhysicalItem grabbable))
                {
                    currentlyGrabbedItem = grabbable;
                    GrabObjectServerRpc(grabbable.NetworkObjectId);
                }
            }
        }
    }

    void TryInteract()
    {
        //TO DO - Metodo para que "apriete un boton"
    }
    #region ServerCom
    [ServerRpc] void GrabObjectServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out PhysicalItem grabbable))
            {
                grabbable.AddGrabber(rpcParams.Receive.SenderClientId, holdPoint);
            }
        }
    }
    [ServerRpc] void ReleaseObjectServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out PhysicalItem grabbable))
            {
                grabbable.RemoveGrabber(rpcParams.Receive.SenderClientId);
            }
        }
    }
    #endregion
}
