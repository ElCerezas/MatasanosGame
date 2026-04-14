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

    [Header("Physics Grab Settings")]
    [SerializeField] float springForce = 15f;
    [SerializeField] float damping = 5f;
    [SerializeField] float throwForce = 15f;

    IGrabbable currentGrabbedItem;
    Rigidbody grabbedRb;
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerInput.OnInteractPressed += TryInteract;
        playerInput.OnPickUpPressed += TryGrabOrThrow;
    }
    private void FixedUpdate()
    {
        if (!IsOwner || currentGrabbedItem == null || grabbedRb == null) return;

        //Llei de hook tete => ForçaFinal = springForce * direction
        Vector3 directionToTarget = holdPoint.position - grabbedRb.position;
        float distance = directionToTarget.magnitude;

        Vector3 springVelocity = directionToTarget * springForce;
        grabbedRb.linearVelocity = Vector3.Lerp(grabbedRb.linearVelocity, springVelocity, Time.fixedDeltaTime * damping);
        grabbedRb.rotation = Quaternion.Slerp(grabbedRb.rotation, holdPoint.rotation, Time.fixedDeltaTime * 10f);
    }
    void TryGrabOrThrow()
    {
        if (currentGrabbedItem != null)
        {
            //Tirar obj
            currentGrabbedItem = null;
            grabbedRb = null;
        }
        else
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                if (hit.collider.TryGetComponent(out IGrabbable grabbable))
                {
                    if (hit.collider.TryGetComponent(out NetworkObject netObj))
                    {
                        //Agafar obj
                    }
                }
            }
        }
    }
    void TryInteract()
    {

    }
}
