using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloodPuddle : NetworkBehaviour
{
    [SerializeField] private float effectDuration = 5f;

    private NetworkVariable<bool> isActive = new NetworkVariable<bool>(true);
    private DecalProjector decalProjector;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        decalProjector = GetComponentInChildren<DecalProjector>();
        if (!IsServer) return;
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

    public void Clean()
    {
        if (!IsServer) return;
        if (!isActive.Value) return;
        Debug.Log("!isActive");
        isActive.Value = false;
        CleanPuddleClientRpc();
    }

    [ClientRpc]
    private void CleanPuddleClientRpc()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    public bool IsActive() => isActive.Value;
}