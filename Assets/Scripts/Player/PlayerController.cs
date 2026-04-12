using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] MyPlayerInput playerInput;
    //[SerializeField] PlayerMover playerMover;

    private void Update()
    {
        if(!IsOwner) return;

        Vector2 movementInput = playerInput.MovementInput;
        //playerMover.Move(movementInput);

    }
}
