using Unity.Netcode;
using UnityEngine;

public class InsectSpawner : NetworkBehaviour
{
    [SerializeField] Transform spawnTransform;
    [SerializeField] GameObject InsectPrefab;
    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnAlienParasiteAttack>(SpawnInsects);
    }
    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnAlienParasiteAttack>(SpawnInsects);
    }

    void SpawnInsects(OnAlienParasiteAttack e)
    {
        
    }
}
