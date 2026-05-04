using Unity.Netcode;
using UnityEngine;

public class PuddleSpawner : MonoBehaviour
{
    public static PuddleSpawner Instance;
    public GameObject puddleDecalPrefab;
    
    private void Awake() => Instance = this;
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnPuddleServerRpc(Vector3 position, Vector3 normal)
    {
        GameObject puddle = Instantiate(puddleDecalPrefab, position, Quaternion.FromToRotation(Vector3.up, normal));
        NetworkObject networkObject = puddle.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
}