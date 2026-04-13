using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;

    private void Update()
    {
        if(!IsOwner) return;

        Move(playerInput.MovementInput);
    }
    private void Move(Vector2 moveInput)
    {
        Debug.Log("Moved");
    }
}
