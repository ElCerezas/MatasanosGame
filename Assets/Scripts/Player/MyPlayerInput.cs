using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;

public class MyPlayerInput : NetworkBehaviour
{
    [SerializeField] InputActionReference movementReference;
    public Vector2 MovementInput { get; private set; }
    public event Action OnPickUpPressed;
    public event Action OnInteractPressed;

    private Vector2 rawInput;
    [SerializeField] float smoothTime = 0.1f;

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
