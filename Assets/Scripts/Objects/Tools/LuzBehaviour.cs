using Unity.Netcode;
using UnityEngine;

public class LuzBehaviour : PoweredItem
{
    [Header("Raycast Settings")]
    public Transform originPoint;
    public float rayDistance = 10f;
    public LayerMask alienLayer;
    public LayerMask playerLayer;

    [Header("Timing Settings")]
    public float timeToCreateWound = 2f;
    public float timeToHeal = 1.5f;
    public float playerBlindDuration = 2f;

    [Header("Visuals")]
    [SerializeField] Light Light;
    [SerializeField] Light Light2;
    [SerializeField] GameObject Textura;

    private float alienTimer = 0f;
    private WoundFocoBehaviour currentWoundTarget = null;
    private AlienWoundManager currentAlienTarget = null;
    private ulong currentPlayerTarget = 0;

    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
        Light.enabled = (hasPower.Value && isTurnedOn.Value);
        Light2.enabled = (hasPower.Value && isTurnedOn.Value);
        Textura.SetActive(hasPower.Value && isTurnedOn.Value);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (!isTurnedOn.Value) return;
        EmitLight();
    }

    void EmitLight()
    {
        Ray ray = new Ray(originPoint.position, originPoint.forward);
        Debug.DrawRay(originPoint.position, originPoint.forward * rayDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            if (IsLayerInMask(hit.collider.gameObject.layer, playerLayer))
            {
                if (hit.collider.TryGetComponent<NetworkObject>(out var playerNetObj))
                {
                    ResetAlienTargets();
                    if (currentPlayerTarget != playerNetObj.NetworkObjectId)
                    {
                        currentPlayerTarget = playerNetObj.NetworkObjectId;
                        EventBus.Publish(new OnPlayerBlinded
                        {
                            VictimID = playerNetObj.NetworkObjectId,
                            Duration = playerBlindDuration
                        });
                    }
                    return;
                }
            }

            if (hit.collider.TryGetComponent<WoundFocoBehaviour>(out var herida))
            {
                ResetAlienTargets();
                currentPlayerTarget = 0;
                currentWoundTarget = herida;
                NotifyHealWoundServerRpc(herida.NetworkObjectId);
            }
            else if (hit.collider.transform.parent.TryGetComponent<AlienWoundManager>(out var alien))
            {
                currentPlayerTarget = 0;
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
                ResetAllTargets();
            }
        }
        else
        {
            ResetAllTargets();
        }
    }

    void ResetAlienTargets()
    {
        currentWoundTarget = null;
        currentAlienTarget = null;
        alienTimer = 0f;
    }

    void ResetAllTargets()
    {
        currentPlayerTarget = 0;
        ResetAlienTargets();
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return ((mask.value & (1 << layer)) > 0);
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