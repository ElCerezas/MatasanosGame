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
                var playerNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                if (playerNetObj != null)
                {
                    ResetAlienTargets();
                    if (currentPlayerTarget != playerNetObj.NetworkObjectId)
                    {
                        currentPlayerTarget = playerNetObj.NetworkObjectId;
                        BlindPlayerClientRpc(playerNetObj.NetworkObjectId, playerBlindDuration);
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
            else if (hit.collider.GetComponentInParent<AlienWoundManager>() is AlienWoundManager alien)
            {
                currentPlayerTarget = 0;
                currentWoundTarget = null;
                currentAlienTarget = alien;
                alienTimer += Time.deltaTime;
                if (alienTimer >= timeToCreateWound)
                {
                    alienTimer = 0f;
                    var netObj = alien.GetComponent<NetworkObject>();
                    if (netObj != null)
                        RequestNewWoundServerRpc(hit.point, hit.normal, netObj.NetworkObjectId);
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

    [ClientRpc]
    void BlindPlayerClientRpc(ulong victimNetObjId, float duration)
    {
        EventBus.Publish(new OnPlayerBlinded
        {
            VictimID = victimNetObjId,
            Duration = duration
        });
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
            AlienWoundManager alien = obj.GetComponent<AlienWoundManager>();
            if (alien == null) alien = obj.GetComponentInChildren<AlienWoundManager>();

            if (alien != null)
            {
                alien.GenerateWound(pos, norm);
                alien.GetComponent<AlienStateManager>()?.WoundCreated();
            }
        }
    }
}