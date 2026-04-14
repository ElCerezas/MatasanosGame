using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : NetworkBehaviour
{
    [Header("Input References")]
    [SerializeField] InputActionReference movementReference;
    [SerializeField] InputActionReference interactReference;
    [SerializeField] InputActionReference pickReference;
    [SerializeField] InputActionReference jumpReference;
    [SerializeField] InputActionReference lookReference;
    //Chorrada per testear
    public Vector2 LookInput { get; private set; }
    public Vector2 MovementInput { get; private set; }

    private Vector2 rawInput;
    [SerializeField] float smoothTime = 0.1f;

    public event Action OnJumpPressed;
    public event Action OnPickUpPressed;
    public event Action OnInteractPressed;

    void OnEnable()
    {
        movementReference.action.Enable();

        //Jump event
        jumpReference.action.performed += OnJump;
        jumpReference.action.Enable();

        //Pick event
        pickReference.action.performed += OnPickUp;
        pickReference.action.Enable();

        //Pick event
        interactReference.action.performed += OnInteract;
        interactReference.action.Enable();
    }
    void OnDisable()
    {
        movementReference.action.Disable();

        //Jump event
        jumpReference.action.performed -= OnJump;
        jumpReference.action.Disable();

        //Pick event
        pickReference.action.performed -= OnPickUp;
        pickReference.action.Disable();

        //Pick event
        interactReference.action.performed -= OnInteract;
        interactReference.action.Disable();
    }

    void Update()
    {
        if (!IsOwner) return;

        rawInput = movementReference.action.ReadValue<Vector2>();
        MovementInput = Vector2.MoveTowards(MovementInput, rawInput, smoothTime); //Que el taclat acceleri suaument

        LookInput = lookReference.action.ReadValue<Vector2>();
    }
    void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        OnJumpPressed?.Invoke();
    }
    void OnPickUp(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        OnPickUpPressed?.Invoke();
    }
    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        OnInteractPressed?.Invoke();
    }
}
