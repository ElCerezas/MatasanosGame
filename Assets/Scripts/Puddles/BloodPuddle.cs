using Unity.Netcode;
using UnityEngine;

public class BloodPuddle : NetworkBehaviour
{
    [SerializeField] private float effectDuration = 5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.TryGetComponent(out IEffectable victim))
        {
            victim.ApplyEffect("BloodPuddle", effectDuration);
        }
    }

    private void DespawnPuddle()
    {
        if (NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}
