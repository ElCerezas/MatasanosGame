using Unity.Netcode;
using UnityEngine;

public class Algodon : NetworkBehaviour
{
    private ColliderInteractItem colliderItem;

    private void Awake()
    {
        colliderItem = gameObject.GetComponent<ColliderInteractItem>();
    }

    public void Desinfectar()
    {
        Debug.Log("Desinfectado algodon");
        colliderItem.itemType = ColliderItemType.AlgodonEstirilizado;
    }
    public void AlgodonInfectado()
    {
        Debug.Log("Algodon infectado");
        colliderItem.itemType = ColliderItemType.Algodon;
    }
}
