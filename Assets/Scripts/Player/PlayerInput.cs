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
    [SerializeField] InputActionReference pauseReference;

    [SerializeField] InputActionReference TestSlipReference;

    //Chorrada per testear
    public Vector2 LookInput { get; private set; }
    public Vector2 MovementInput { get; private set; }

    private Vector2 rawInput;
    [SerializeField] float smoothTime = 0.1f;

    public event Action OnJumpPressed;
    public event Action OnPickUpPressed;
    public event Action OnInteractPressed;
    public event Action OnPausePressed;

    public event Action OnSlipPressed;

    void OnEnable()
    {
        movementReference.action.Enable();

        //Jump event
        jumpReference.action.performed += OnJump;
        jumpReference.action.Enable();

        //Pick event
        pickReference.action.performed += OnPickUp;
        pickReference.action.Enable();

        //Interact event
        interactReference.action.performed += OnInteract;
        interactReference.action.Enable();

        //TEST SLip
        TestSlipReference.action.performed += OnSlip;
        TestSlipReference.action.Enable();
        //Pause event
        pauseReference.action.performed += OnPause;
        pauseReference.action.Enable();
    }
    
    void OnDisable()
    {
        // Solo desuscribirse de los eventos, NO deshabilitar los InputActions globales
        // porque eso afecta a todos los players, no solo a este
        jumpReference.action.performed -= OnJump;
        pickReference.action.performed -= OnPickUp;
        interactReference.action.performed -= OnInteract;
        TestSlipReference.action.performed -= OnSlip;
        pauseReference.action.performed -= OnPause;
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner) return; // Añadido !IsSpawned

        rawInput = movementReference.action.ReadValue<Vector2>();
        MovementInput = Vector2.MoveTowards(MovementInput, rawInput, smoothTime);

        LookInput = lookReference.action.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsSpawned || !IsOwner) return;
        OnJumpPressed?.Invoke();
    }
    void OnPickUp(InputAction.CallbackContext ctx)
    {
        if (!IsSpawned || !IsOwner) return;
        OnPickUpPressed?.Invoke();
    }
    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!IsSpawned || !IsOwner) return;
        OnInteractPressed?.Invoke();
    }
    void OnSlip(InputAction.CallbackContext ctx)
    {
        if (!IsSpawned || !IsOwner) return;
        OnSlipPressed?.Invoke();
    }
    void OnPause(InputAction.CallbackContext ctx)
    {
        if (!IsSpawned || !IsOwner) return;
        PauseHandler pauseHandler = GetComponent<PauseHandler>();
        if (pauseHandler != null)
        {
            pauseHandler.TogglePause();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Solo desuscribirse de nuestros propios eventos
        if (IsOwner)
        {
            jumpReference.action.performed -= OnJump;
            pickReference.action.performed -= OnPickUp;
            interactReference.action.performed -= OnInteract;
            TestSlipReference.action.performed -= OnSlip;
            pauseReference.action.performed -= OnPause;
        }
        base.OnNetworkDespawn();
    }

    // En PlayerInput.cs
    public void DisableInputs()
    {
        // Solo desuscribirse si es nuestro player
        if (!IsOwner) return;
        
        try
        {
            jumpReference.action.performed -= OnJump;
            pickReference.action.performed -= OnPickUp;
            interactReference.action.performed -= OnInteract;
            TestSlipReference.action.performed -= OnSlip;
            pauseReference.action.performed -= OnPause;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayerInput] Error disabling inputs: {ex.Message}");
        }
    }
}
