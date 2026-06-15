using Unity.Netcode;
using UnityEngine;

public class Venda : NetworkBehaviour
{
    NetworkObject netObj;
    public void PonerVenda()
    {
        netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            DespawnVendaServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void DespawnVendaServerRpc()
    {
        netObj.Despawn();
    }
}
