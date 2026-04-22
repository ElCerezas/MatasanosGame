using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(Rigidbody))]

public class PlayerStateManager : NetworkBehaviour, IEffectable
{
    PlayerController controller;
    PlayerInteractor interactor;
    PlayerCamera playerCamera;
    Rigidbody rb;
    [Header("Ragdoll Settings")]
    [SerializeField] float stunDuration = 0f;
    [SerializeField] float invulnerableTime = 3f;
    [SerializeField] float minimumForceToRagdoll;
    float invulnerableCountdown;
    

    [Header("Slip Variables")]
    [SerializeField] float slipForce = 10f;
    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        interactor = GetComponent<PlayerInteractor>();
        playerCamera = GetComponent<PlayerCamera>();
        rb = GetComponent<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        gameObject.GetComponent<PlayerInput>().OnJumpPressed += RecoverJump;
        gameObject.GetComponent<PlayerInput>().OnSlipPressed += Slip;
        EventBus.Subscribe<OnInject>(OnPlayerInjected);
    }
    public override void OnNetworkDespawn()
    {

        EventBus.Unsubscribe<OnInject>(OnPlayerInjected);
        base.OnNetworkDespawn();
    }
    private void OnPlayerInjected(OnInject data)
    {
        if (data.VictimID != NetworkObjectId) return;
        Debug.Log($"Player {gameObject.name} injected with type {data.Type}");
        ApplyInyeccion(data.Type);
    }

    public void Slip(/*float stunTime*/)
    {
        EnterRagdoll(3f);
        //ragrollForce
    }
    private void Update()
    {
        if (stunDuration > 0f)
            stunDuration -= Time.deltaTime;

        if (invulnerableCountdown > 0f)
            invulnerableCountdown -= Time.deltaTime;
    }
    void EnterRagdoll(float ragdollTime, bool overrideInvulnerability = false)
    {
        if (!overrideInvulnerability)
            if (invulnerableCountdown > 0) return;

        stunDuration = ragdollTime;
        controller.Ragdoll(true);
        interactor.Ragdoll(true);
        playerCamera.Ragdoll(true);
        rb.constraints = RigidbodyConstraints.None;
        rb.freezeRotation = false;

    }
    void RecoverJump()
    {
        if (stunDuration > 0f) return;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.freezeRotation = true;
        invulnerableCountdown = invulnerableTime;

        //Salto para recuperar stand

        ExitRagdoll();
    }
    void ExitRagdoll()
    {
        controller.Ragdoll(false);
        interactor.Ragdoll(false);
        playerCamera.Ragdoll(false);

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.freezeRotation = true;
    }

    public void ApplyInyeccion(InyeccionType type)
    {
        Debug.Log($"Applying inyeccion of type {type} to player");
        switch (type)
        {
            case InyeccionType.Calmante:
                EnterRagdoll(5f, true);
                break;
            case InyeccionType.Estimulante:

                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 relativeVelocity = collision.relativeVelocity;
        Debug.Log(relativeVelocity.magnitude);
        if (relativeVelocity.magnitude > minimumForceToRagdoll)
        {
            EnterRagdoll(3f);
            rb.AddForce(relativeVelocity/2, ForceMode.Impulse);
        }
    }

    public void ApplyEffect(string effectType, float duration)
    {
        switch (effectType)
        {
            case "Blood":
                EnterRagdoll(duration);
                break;
        }
    }
}
