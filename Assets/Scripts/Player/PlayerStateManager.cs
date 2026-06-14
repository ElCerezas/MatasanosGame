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

    [Header("Sound Events (3D)")]
    [SerializeField] FMODUnity.EventReference slipSound;
    [SerializeField] FMODUnity.EventReference knockdownSound;

    [Header("Ragdoll Settings")]
    [SerializeField] Animator animator;
    [SerializeField] float stunDuration = 0f;
    [SerializeField] float invulnerableTime = 3f;
    [SerializeField] float minimumForceToRagdoll;
    [SerializeField] private ParticleSystem bloodSlipParticles;
    [SerializeField] private ParticleSystem hitParticles;
    float invulnerableCountdown;
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

    bool EnterRagdoll(float ragdollTime, bool overrideInvulnerability = false)
    {
        if (!overrideInvulnerability)
        {
            if (invulnerableCountdown > 0) return false;
        }

        stunDuration = ragdollTime;
        controller.Ragdoll(true);
        interactor.Ragdoll(true);
        playerCamera.Ragdoll(true);
        rb.constraints = RigidbodyConstraints.None;
        rb.freezeRotation = false;
        animator.enabled = false;

        return true;
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
                GetComponent<PlayerHUDEffects>().TriggerSleep(5f);
                EnterRagdoll(5f, true);
                break;
            case LiquidType.Estimulante:
                break;
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReceiveInjectionServerRpc(int liquidTypeInt)
    {
        ReceiveInjectionClientRpc(liquidTypeInt);
    }

    [ClientRpc]
    private void ReceiveInjectionClientRpc(int liquidTypeInt)
    {
        if (!IsOwner) return;
        ApplyInyeccion((LiquidType)liquidTypeInt);
    }

    public void OnInsectExplosion(OnInsectExplosion data)
    {
        if (data.VictimID != NetworkObjectId) return;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void TriggerKnockdownRpc(float ragdollTime, Vector3 pushForce, Vector3 impactPoint, bool spawnParticles, float impactSpeed)
    {
        if (stunDuration > 0f) return;
        if (impactSpeed < minimumForceToRagdoll) return;
        if (!EnterRagdoll(ragdollTime)) return;
        rb.AddForce(pushForce, ForceMode.Impulse);
        if (!knockdownSound.IsNull)
        {
            AudioManager.instance.PlayOneShotAtPosition(knockdownSound, transform.position);
        }
        if (spawnParticles && hitParticles != null)
        {
            StartCoroutine(PlayHitParticles(impactPoint));
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (stunDuration > 0f) return;

        if (collision.gameObject.TryGetComponent(out PhysicalItem _)) return;
        if (!IsOwner) return;

        Vector3 relativeVelocity = collision.relativeVelocity;
        float impactSpeed = relativeVelocity.magnitude;

        Vector3 impactPoint = collision.GetContact(0).point;
        Vector3 pushForce = relativeVelocity / 2;
        TriggerKnockdownRpc(3f, pushForce, impactPoint, true, impactSpeed);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void SlipServerRpc(float stunTime)
    {
        Slip(stunTime);
    }

    public void Slip(float stunTime)
    {
        if (stunDuration > 0f) return;
        if (!EnterRagdoll(stunTime)) return;
        Vector3 feetPosition = transform.position + Vector3.down * 0.5f;

        rb.AddForce(Vector3.up * 120f, ForceMode.Impulse);
        rb.AddForceAtPosition(transform.forward * 250f, feetPosition, ForceMode.Impulse);
        if (!slipSound.IsNull)
        {
            AudioManager.instance.PlayOneShotAtPosition(slipSound, transform.position);
        }
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
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void HitVFXRpc(Vector3 position)
    {
        StartCoroutine(PlayHitParticles(position));
    }

    private IEnumerator PlayHitParticles(Vector3 position)
    {
        Transform originalParent = hitParticles.transform.parent;
        Vector3 originalLocalPosition = hitParticles.transform.localPosition;
        Quaternion originalLocalRotation = hitParticles.transform.localRotation;

        hitParticles.transform.SetParent(null, worldPositionStays: true);
        hitParticles.transform.position = position;
        hitParticles.Play();

        yield return new WaitForSeconds(hitParticles.main.duration + hitParticles.main.startLifetime.constantMax);

        hitParticles.transform.SetParent(originalParent, worldPositionStays: false);
        hitParticles.transform.localPosition = originalLocalPosition;
        hitParticles.transform.localRotation = originalLocalRotation;
    }
    #region Audio RPCs

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayKnockdownSoundRpc(Vector3 position)
    {
        if (!knockdownSound.IsNull)
        {
            AudioManager.instance.PlayOneShotAtPosition(knockdownSound, position);
        }
    }

    #endregion
}