using Unity.Netcode;
using UnityEngine;

public class PuddleSpawner : MonoBehaviour
{
    public static PuddleSpawner Instance;
    public GameObject puddlePrefab;
    private void Awake() => Instance = this;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnPuddleServerRpc(Vector3 position, float duration)
    {
        GameObject puddle = Instantiate(puddlePrefab, position, Quaternion.identity);
        NetworkObject networkObject = puddle.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
}
