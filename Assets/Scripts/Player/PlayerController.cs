using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] float speed = 5f;
    [SerializeField] float jumpForce = 1.5f;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform playerFeet;

    Rigidbody rigidBody;
    Vector3 velocity;
    bool isGrounded;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
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
        isGrounded = IsGrounded();
        if (isGrounded && velocity.y < 0) velocity.y = 0f; 

        Vector2 input = playerInput.MovementInput;
        Vector3 move = cameraTransform.right * input.x + cameraTransform.forward * input.y;

         move.y = 0f;
        rigidBody.MovePosition(transform.position+move*speed*Time.deltaTime);
    }
    void Jump()
    {
        if (!isGrounded) return;

        rigidBody.AddForce(0,jumpForce,0);
    }

    bool IsGrounded()
    {
        bool grounded;

        if(Physics.Raycast(playerFeet.position,Vector3.down, 0.1f)) grounded = true;
        else grounded = false;

        return grounded;
    }


}
