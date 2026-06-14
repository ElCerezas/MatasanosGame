using System;
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
    public float playerBlindDuration = 2f;

    [Header("Visuals")]
    [SerializeField] Light Light;
    [SerializeField] Light Light2;
    [SerializeField] GameObject Textura;
    [SerializeField] ParticleSystem lightParticles;

    private float alienTimer = 0f;
    private WoundFocoBehaviour currentWoundTarget = null;
    private AlienWoundManager currentAlienTarget = null;
    private ulong currentPlayerTarget = 0;

    public override void OnNetworkSpawn()
    {
        isTurnedOn.OnValueChanged += (_, newVal) => UpdateLightState();
        UpdateLightState();
        base.OnNetworkSpawn();
    }

    private void UpdateLightState()
    {
        bool active = hasPower.Value && isTurnedOn.Value;
        Light.enabled = active;
        Light2.enabled = active;
        if (active)
            lightParticles.Play();
        else
            lightParticles.Stop();
            Textura.SetActive(active);

        if (!active) StopCurrentHealing();
    }

    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
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
                    StopCurrentHealing();
                    ResetAlienTargets();
                    if (currentPlayerTarget != playerNetObj.NetworkObjectId)
                    {
                        currentPlayerTarget = playerNetObj.NetworkObjectId;
                        BlindPlayerServerRpc(playerNetObj.NetworkObjectId, playerBlindDuration); // ← primero al server
                    }
                    return;
                }
            }

            if (hit.collider.TryGetComponent<WoundFocoBehaviour>(out var herida))
            {
                ResetAlienTargets();
                currentPlayerTarget = 0;

                if (currentWoundTarget != herida)
                {
                    StopCurrentHealing();
                    currentWoundTarget = herida;
                    StartHealingServerRpc(herida.NetworkObjectId);
                }
            }
            else if (hit.collider.GetComponentInParent<AlienWoundManager>() is AlienWoundManager alien)
            {
                StopCurrentHealing();
                currentPlayerTarget = 0;
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
                StopCurrentHealing();
                ResetAllTargets();
            }
        }
        else
        {
            StopCurrentHealing();
            ResetAllTargets();
        }
    }

    void StopCurrentHealing()
    {
        if (currentWoundTarget != null)
        {
            StopHealingServerRpc(currentWoundTarget.NetworkObjectId);
            currentWoundTarget = null;
        }
    }

    [ServerRpc]
    void BlindPlayerServerRpc(ulong victimNetObjId, float duration)
    {
        BlindPlayerClientRpc(victimNetObjId, duration);
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
    void StartHealingServerRpc(ulong woundNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(woundNetId, out var obj))
            obj.GetComponent<WoundFocoBehaviour>()?.StartHealing();
    }

    [ServerRpc]
    void StopHealingServerRpc(ulong woundNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(woundNetId, out var obj))
            obj.GetComponent<WoundFocoBehaviour>()?.StopHealing();
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