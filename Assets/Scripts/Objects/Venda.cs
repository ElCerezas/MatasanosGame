using Unity.Netcode;
using UnityEngine;

public class Venda : NetworkBehaviour
{
    public void PonerVenda()
    {
        NetworkObject parentNetObject = GetComponent<NetworkObject>();
        if (parentNetObject != null && parentNetObject.IsSpawned)
        {
            parentNetObject.Despawn();
        }
    }
}
