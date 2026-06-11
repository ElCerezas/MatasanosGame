using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [Header("Jump feel")]
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float risingMultiplier = 1.5f;

    Rigidbody rigidBody;
    bool isGrounded = true;
    bool isRagdoll = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rigidBody = GetComponent<Rigidbody>();
        pauseHandler = GetComponent<PauseHandler>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerInput.OnJumpPressed += Jump;
    }

    public void Ragdoll(bool active)
    {
        isRagdoll = active;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (pauseHandler.IsPaused)
        {
            return;
        }
        bool grounded = IsGrounded();
        if (isGrounded != grounded)
        {
            isGrounded = grounded;
            animator.SetBool("Grounded", isGrounded);
        }
        currentInput = playerInput.MovementInput;
    }
    void ApplyJumpGravity()
    {
        if (rigidBody.linearVelocity.y < 0f)
        {
            rigidBody.linearVelocity += Vector3.up
                * Physics.gravity.y
                * (fallMultiplier - 1f)
                * Time.fixedDeltaTime;
        }
        else if (rigidBody.linearVelocity.y > 0f)
        {
            rigidBody.linearVelocity += Vector3.up
                * Physics.gravity.y
                * (risingMultiplier - 1f)
                * Time.fixedDeltaTime;
        }
    }
    void FixedUpdate()
    {
        if (!IsOwner) return;
        ApplyJumpGravity();
        Move();
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
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            animator.SetFloat("Speed", moveDir.magnitude);
        }
    }
    void Jump()
    {
        if (isRagdoll) return;
        if (!isGrounded) return;

        animator.SetTrigger("Jump");
        rigidBody.AddForce(Vector3.up * jumpForce * rigidBody.mass, ForceMode.Impulse);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(playerFeet.position, Vector3.down, 0.2f);
    }


}
