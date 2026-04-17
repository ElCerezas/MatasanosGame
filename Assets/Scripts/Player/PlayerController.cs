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
    [SerializeField] float jumpForce = 1.5f;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform playerFeet;

    Rigidbody rigidBody;
    Vector3 velocity;
    bool isGrounded;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rigidBody = GetComponent<Rigidbody>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerInput.OnJumpPressed += Jump;
    }
    void Update()
    {
        if(!IsOwner) return;
        isGrounded = IsGrounded();
        currentInput = playerInput.MovementInput;
    }
    void FixedUpdate()
    {
        if (!IsOwner) return;
        Move();
    }
    void Move()
    {
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
        }
    }
    void Jump()
    {
        if (!isGrounded) return;

        rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(playerFeet.position, Vector3.down, 0.2f);
    }


}
