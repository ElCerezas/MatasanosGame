using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ColliderInteractItem : NetworkBehaviour
{
    [SerializeField] public ColliderInteractableType itemType;
}
