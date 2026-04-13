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

    [Header("Movement Variables")]
    public Vector2 MovementInput { get; private set; }
    private Vector2 rawInput;
    [SerializeField] float smoothTime = 0.1f;

    //[Header("Jump Variables")]

    //[Header("Pick Variables")]
    public event Action OnPickUpPressed;

    //[Header("Interact Variables")]
    public event Action OnInteractPressed;



    void Update()
    {
        if (!IsOwner) return;

        rawInput = movementReference.action.ReadValue<Vector2>();
        MovementInput = Vector2.MoveTowards(MovementInput, rawInput, smoothTime); //Que el taclat acceleri suaument

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            OnPickUpPressed?.Invoke();
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnInteractPressed?.Invoke();
        }
    }

}
