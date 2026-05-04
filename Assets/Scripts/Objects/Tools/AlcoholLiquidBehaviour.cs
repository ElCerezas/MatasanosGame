using Unity.Netcode;
using UnityEngine;

public class AlcoholLiquidBehaviour : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<ColliderInteractItem>(out ColliderInteractItem colliderItemType))
        {
            if(colliderItemType.itemType == ColliderInteractableType.Algodon)
            {
                colliderItemType.itemType = ColliderInteractableType.AlgodonEstirilizado;
            }
        }
    }
}
