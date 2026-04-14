using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Transform holdPoint;

    IGrabbable currentGrabbedItem;
    [SerializeField] float throwForce;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerInput.OnInteractPressed += TryInteract;
        playerInput.OnPickUpPressed += TryGrabOrThrow;
    }
    void TryGrabOrThrow()
    {

    }
    void TryInteract()
    {

    }
}
