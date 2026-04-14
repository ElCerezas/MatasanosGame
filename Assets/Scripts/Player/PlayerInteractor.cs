using Unity.Netcode;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Transform holdPoint;
    
}
