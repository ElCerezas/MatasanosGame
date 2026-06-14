using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloodPuddle : NetworkBehaviour
{
    [SerializeField] private float effectDuration = 5f;
    private NetworkVariable<float> bloodAmount = new NetworkVariable<float>(1f);
    private NetworkVariable<bool> isActive = new NetworkVariable<bool>(true);
    private DecalProjector decalProjector;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        decalProjector = GetComponentInChildren<DecalProjector>();
        bloodAmount.OnValueChanged += (oldVal, newVal) =>
        {
            if (decalProjector != null)
            {
                decalProjector.fadeFactor = newVal;
            }
        };
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!isActive.Value) return;

        if (other.TryGetComponent(out NetworkObject victimNetObj))
        {
            if (victimNetObj.TryGetComponent(out PlayerStateManager stateManager))
            {
                stateManager.SlipServerRpc(effectDuration);
            }
        }
    }

    public void ReduceBlood(float amount)
    {
        ReduceBloodRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReduceBloodRpc(float amount)
    {
        if (!isActive.Value) return;
        
        bloodAmount.Value = Mathf.Max(0f, bloodAmount.Value - amount);
        
        if (bloodAmount.Value <= 0f)
            CleanRpc();
    }

    public void Clean()
    {
        CleanRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CleanRpc()
    {
        if (!isActive.Value) return;
        
        isActive.Value = false;
        
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    public bool IsActive() => isActive.Value;
    public ulong GetNetworkObjectID() => NetworkObjectId;
}