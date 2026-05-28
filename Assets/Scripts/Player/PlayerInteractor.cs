using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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

    [Header("Rigging")]
    [SerializeField] Animator anim;
    [SerializeField] TwoBoneIKConstraint leftHandIK;
    [SerializeField] TwoBoneIKConstraint rightHandIK;

    IGrabbable currentlyGrabbedItem;
    InteractableOutline currentOutlined;
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

    void Update()
    {
        if (!IsOwner || isRagdoll) return;
        InteractableOutline newOutline = null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            newOutline = hit.collider.GetComponent<InteractableOutline>() ?? null;
        }

        if (newOutline == currentOutlined) return;

        currentOutlined?.SetHighlight(false);
        currentOutlined = newOutline;
        currentOutlined?.SetHighlight(true);
    }

    void TryGrabOrThrow()
    {
        if (isRagdoll) return;

        if (currentlyGrabbedItem != null)
        {
            StartCoroutine(BlendIKWeight(0f));
            anim.SetBool("IsGrabbing", false);
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
                    StartCoroutine(BlendIKWeight(1f));
                    anim.SetBool("IsGrabbing", true);
                    GrabObjectServerRpc(grabbable.GetNetworkObjectID());
                }
            }
        }
    }
    IEnumerator BlendIKWeight(float target)
    {
        float start = leftHandIK.weight;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            float w = Mathf.Lerp(start, target, t);
            leftHandIK.weight = w;
            rightHandIK.weight = w;
            yield return null;
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
        NetworkObject playerNetObj = client.PlayerObject;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out IGrabbable grabbable))
                grabbable.AddGrabber(senderClientId, playerNetObj);
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