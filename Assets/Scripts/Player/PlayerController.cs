using FMODUnity;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    PlayerInput playerInput;
    Vector2 currentInput;
    [SerializeField] float speed = 5f;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform playerFeet;
    [SerializeField] Animator animator;
    private PauseHandler pauseHandler;

    [Header("Footsteps (Timer-Based)")]
    [SerializeField] private EventReference footstepSound;
    [SerializeField] float stepInterval = 0.35f;
    private float stepTimer = 0f;

    [Header("Jump feel")]
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float risingMultiplier = 1.5f;
    [SerializeField] float landingSoundCooldown = 0.3f;
    [SerializeField] EventReference landingSound;
    float landingSoundCooldownTimer = 0f;
    bool wasGroundedLastFrame = true;
    bool hasJumped = false;

    Rigidbody rigidBody;
    bool isGrounded = true;
    bool isRagdoll = false;

    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkGrounded = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rigidBody = GetComponent<Rigidbody>();
        pauseHandler = GetComponent<PauseHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            FMODUnity.StudioListener fmodListener = GetComponentInChildren<FMODUnity.StudioListener>(true);
            if (fmodListener != null) fmodListener.enabled = false;
            return;
        }
        playerInput.OnJumpPressed += Jump;
    }    
    public override void OnNetworkDespawn()
    {
        if (IsOwner && playerInput != null)
            playerInput.OnJumpPressed -= Jump;
    }

    public void Ragdoll(bool active)
    {
        isRagdoll = active;
    }

    void Update()
    {
        if (!IsSpawned) return;

        animator.SetFloat("Speed", networkSpeed.Value);
        animator.SetBool("Grounded", networkGrounded.Value);

        CheckFootstepsTimer();

        if (!IsOwner) return;

        if (pauseHandler.IsPaused)
        {
            currentInput = Vector2.zero;
            networkSpeed.Value = 0f;
            return;
        }

        if (landingSoundCooldownTimer > 0f)
        {
            landingSoundCooldownTimer -= Time.deltaTime;
        }

        bool grounded = IsGrounded();
        if (isGrounded != grounded)
        {
            isGrounded = grounded;
            networkGrounded.Value = isGrounded;
        }

        if (isGrounded && !wasGroundedLastFrame && landingSoundCooldownTimer <= 0f && hasJumped)
        {
            OnLandingServerRPC(playerFeet.position);
            landingSoundCooldownTimer = landingSoundCooldown;
            hasJumped = false;
        }
        wasGroundedLastFrame = isGrounded;
        currentInput = playerInput.MovementInput;
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner) return;
        if (pauseHandler.IsPaused) return;
        ApplyJumpGravity();
        Move();
    }

    public void OnGamePaused(bool paused)
    {
        if (paused && IsOwner)
        {
            rigidBody.linearVelocity = Vector3.zero;
            networkSpeed.Value = 0f;
            networkGrounded.Value = true;
        }
    }

    void CheckFootstepsTimer()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isWalking = stateInfo.IsName("Caminar");

        if (isWalking)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayFootstepSound();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstepSound()
    {
        if (AudioManager.instance != null && playerFeet != null && !isRagdoll && !pauseHandler.IsPaused)
        {
            AudioManager.instance.PlayOneShotAtPosition(footstepSound, playerFeet.position);
        }
    }

    [ClientRpc]
    void OnLandingClientRPC(Vector3 landingPosition)
    {
        AudioManager.instance.PlayOneShotAtPosition(landingSound, landingPosition);
    }

    [ServerRpc]
    void OnLandingServerRPC(Vector3 landingPosition)
    {
        OnLandingClientRPC(landingPosition);
    }

    void ApplyJumpGravity()
    {
        if (rigidBody.linearVelocity.y < 0f)
        {
            rigidBody.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rigidBody.linearVelocity.y > 0f)
        {
            rigidBody.linearVelocity += Vector3.up * Physics.gravity.y * (risingMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    void Move()
    {
        if (isRagdoll) return;
        Vector3 foward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        foward.y = 0f;
        right.y = 0f;
        foward.Normalize();
        right.Normalize();

        Vector3 moveDir = (foward * currentInput.y + right * currentInput.x).normalized;
        Vector3 currentVelocity = rigidBody.linearVelocity;
        Vector3 targetHorizontalVelocity = moveDir * speed;

        rigidBody.linearVelocity = new Vector3(targetHorizontalVelocity.x, currentVelocity.y, targetHorizontalVelocity.z);

        if (currentInput == Vector2.zero)
        {
            rigidBody.linearVelocity = new Vector3(0, currentVelocity.y, 0);
            networkSpeed.Value = 0f;
        }
        else
        {
            networkSpeed.Value = moveDir.magnitude;
        }
    }

    void Jump()
    {
        if (isRagdoll) return;
        if (!isGrounded) return;
        if (pauseHandler.IsPaused) return;

        hasJumped = true;
        
        PlayJumpAnimationServerRPC();

        rigidBody.AddForce(Vector3.up * jumpForce * rigidBody.mass, ForceMode.Impulse);
    }

    [ServerRpc]
    void PlayJumpAnimationServerRPC()
    {
        PlayJumpAnimationClientRPC();
    }

    [ClientRpc]
    void PlayJumpAnimationClientRPC()
    {
        animator.SetTrigger("Jump");
    }

    bool IsGrounded()
    {
        return Physics.Raycast(playerFeet.position, Vector3.down, 0.2f);
    }
}