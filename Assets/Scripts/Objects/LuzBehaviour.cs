using Unity.Netcode;
using UnityEngine;

public class LuzBehaviour : NetworkBehaviour
{
    [Header("Raycast Settings")]
    public Transform originPoint;
    public float rayDistance = 10f;
    public LayerMask alienLayer;

    [Header("Timing Settings")]
    public float timeToCreateWound = 2f;
    public float timeToHeal = 1.5f;

    private float alienTimer = 0f;
    private WoundFocoBehaviour currentWoundTarget = null;
    private AlienWoundManager currentAlienTarget = null;

    void Update()
    {
        if (!IsOwner) return;

        EmitLight();
    }

    void EmitLight()
    {
        Ray ray = new Ray(originPoint.position, originPoint.forward);
        Debug.DrawRay(originPoint.position, originPoint.forward * rayDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, alienLayer))
        {
            if (hit.collider.TryGetComponent<WoundFocoBehaviour>(out var herida))
            {
                currentAlienTarget = null;
                alienTimer = 0f;

                currentWoundTarget = herida;
                NotifyHealWoundServerRpc(herida.NetworkObjectId);
            }
            else if (hit.collider.TryGetComponent<AlienWoundManager>(out var alien))
            {
                currentWoundTarget = null;
                currentAlienTarget = alien;

                alienTimer += Time.deltaTime;
                if (alienTimer >= timeToCreateWound)
                {
                    alienTimer = 0f;
                    RequestNewWoundServerRpc(hit.point, hit.normal, alien.NetworkObjectId);
                }
            }
            else
            {
                ResetTargets();
            }
        }
        else
        {
            ResetTargets();
        }
    }

    void ResetTargets()
    {
        currentWoundTarget = null;
        currentAlienTarget = null;
        alienTimer = 0f;
    }

    [ServerRpc]
    void NotifyHealWoundServerRpc(ulong woundNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(woundNetId, out var obj))
        {
            if (obj.TryGetComponent<WoundFocoBehaviour>(out var wound))
            {
                wound.ReceiveHealTick();
            }
        }
    }

    [ServerRpc]
    void RequestNewWoundServerRpc(Vector3 pos, Vector3 norm, ulong alienId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(alienId, out var obj))
        {
            if (obj.TryGetComponent<AlienWoundManager>(out var alien))
            {
                alien.GenerateWound(pos, norm);
                GetComponent<AlienStateManager>().WoundCreated();
            }
        }
    }
}