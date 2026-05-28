using System.Collections;
using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(Rigidbody))]

public class PlayerStateManager : NetworkBehaviour
{
    PlayerController controller;
    PlayerInteractor interactor;
    PlayerCamera playerCamera;
    Rigidbody rb;
    [Header("Ragdoll Settings")]
    [SerializeField] Animator animator;
    [SerializeField] float stunDuration = 0f;
    [SerializeField] float invulnerableTime = 3f;
    [SerializeField] float minimumForceToRagdoll;
    [SerializeField] private ParticleSystem bloodSlipParticles;
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
        EventBus.Subscribe<OnInject>(OnPlayerInjected);
        EventBus.Subscribe<OnInsectExplosion>(OnInsectExplosion);
    }
    public override void OnNetworkDespawn()
    {

        EventBus.Unsubscribe<OnInject>(OnPlayerInjected);
        EventBus.Unsubscribe<OnInsectExplosion>(OnInsectExplosion);
        base.OnNetworkDespawn();
    }
    private void OnPlayerInjected(OnInject data)
    {
        if (data.VictimID != NetworkObjectId) return;
        ApplyInyeccion(data.Type);
    }
    public void EnterRagdollPublic(float ragdollTime)
    {
        EnterRagdoll(ragdollTime, true);
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
        animator.enabled = false;

    }
    void RecoverJump()
    {
        if (stunDuration > 0f) return;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.freezeRotation = true;
        invulnerableCountdown = invulnerableTime;
        animator.enabled = false;

        ExitRagdoll();
    }
    void ExitRagdoll()
    {
        controller.Ragdoll(false);
        interactor.Ragdoll(false);
        playerCamera.Ragdoll(false);

        animator.enabled = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.freezeRotation = true;
    }

    public void ApplyInyeccion(LiquidType type)
    {
        //Debug.Log($"Applying inyeccion of type {type} to player");
        switch (type)
        {
            case LiquidType.Calmante:
                EnterRagdoll(5f, true);
                break;
            case LiquidType.Estimulante:

                break;
        }
    }

    public void OnInsectExplosion(OnInsectExplosion data)
    {
        if (data.VictimID != NetworkObjectId) return;
        //Debug.Log($"Player {gameObject.name} was exploted by an insect");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stunDuration > 0f) return;
        Vector3 relativeVelocity = collision.relativeVelocity;
        if (relativeVelocity.magnitude > minimumForceToRagdoll)
        {
            EnterRagdoll(3f);
            rb.AddForce(relativeVelocity / 2, ForceMode.Impulse);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void SlipServerRpc(float stunTime)
    {
        Slip(stunTime);
    }

    public void Slip(float stunTime)
    {
        if (stunDuration > 0f) return;
        EnterRagdoll(stunTime);

        Vector3 feetPosition = transform.position + Vector3.down * 0.5f;

        rb.AddForce(Vector3.up * 120f, ForceMode.Impulse);
        rb.AddForceAtPosition(transform.forward * 250f, feetPosition, ForceMode.Impulse);
        SlipVFXClientRpc();
    }
    [ClientRpc]
    public void SlipVFXClientRpc()
    {
        StartCoroutine(PlaySlipParticles());
    }

    private IEnumerator PlaySlipParticles()
    {
        Transform originalParent = bloodSlipParticles.transform.parent;

        bloodSlipParticles.transform.SetParent(null, worldPositionStays: true);
        bloodSlipParticles.Play();

        yield return new WaitForSeconds(bloodSlipParticles.main.duration + bloodSlipParticles.main.startLifetime.constantMax);

        bloodSlipParticles.transform.SetParent(originalParent, worldPositionStays: false);
    }
}
