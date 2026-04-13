using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] float speed = 5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float jumpForce = 1.5f;
    [SerializeField] Transform cameraTransform;

    CharacterController controller;
    Vector3 velocity;
    bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        //Subscripción a eventos de acciones
        playerInput.OnJumpPressed += Jump;
    }
    void Update()
    {
        if(!IsOwner) return;
        Move();
    }
    void Move()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f; 

        Vector2 input = playerInput.MovementInput;
        Vector3 move = cameraTransform.right * input.x + cameraTransform.forward * input.y;

        move.y = 0f;
        controller.Move(move * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    void Jump()
    {
        if (!isGrounded) return;

        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }
}
