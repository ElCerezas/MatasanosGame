using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent (typeof(PlayerController))]
[RequireComponent (typeof(PlayerInteractor))]
[RequireComponent (typeof(Rigidbody))]

public class PlayerStateManager : NetworkBehaviour
{
    PlayerController controller;
    PlayerInteractor interactor;
    Rigidbody rb;
    [Header("Ragdoll Settings")]
    [SerializeField] float stunDuration = 0f;
    [SerializeField] float inbulnerableTime = 3f;
     float inbulnerableCountdown;

    [Header("Slip Variables")]
    [SerializeField] float slipForce = 10f;
    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        interactor = GetComponent<PlayerInteractor>();
        rb = GetComponent<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        gameObject.GetComponent<PlayerInput>().OnJumpPressed += RecoverJump;
        gameObject.GetComponent<PlayerInput>().OnSlipPressed += Slip;
    }
    public void Slip(/*float stunTime*/)
    {
        EnterRagdoll(3f);
        //ragrollForce
    }
    private void Update()
    {
        if(stunDuration > 0f) 
            stunDuration -= Time.deltaTime;

        if(inbulnerableCountdown > 0f)
            inbulnerableCountdown -= Time.deltaTime;
    }
    void EnterRagdoll(float ragdollTime, bool overrideInbulnerability = false)
    {
        if (!overrideInbulnerability)
            if (inbulnerableCountdown > 0) return;

        stunDuration = ragdollTime;
        controller.Ragdoll(true);
        interactor.Ragdoll(true);
        rb.constraints = RigidbodyConstraints.None;
        rb.freezeRotation = false;
        
    }
    void RecoverJump()
    {
        if (stunDuration > 0f ) return;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.freezeRotation = true;
        inbulnerableCountdown = inbulnerableTime;

        //Salto para recuperar stand

        ExitRagdoll();      
    }
    void ExitRagdoll()
    {
        controller.Ragdoll(false);
        interactor.Ragdoll(false);

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.freezeRotation = true;
    }
    
}
