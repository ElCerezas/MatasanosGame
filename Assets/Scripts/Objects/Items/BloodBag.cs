using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BloodBag : NetworkBehaviour
{
    [SerializeField] private float bloodBagMaxCapacity = 100f;
    [SerializeField] protected Renderer BloodRenderer;
    [SerializeField] private NetworkVariable<float> bloodBagCurrentCapacity = new NetworkVariable<float>(100f);
    [SerializeField] private NetworkVariable<bool> isAttached = new NetworkVariable<bool>(false);
    [SerializeField] private float bloodLossQuantity = 1f;
    [SerializeField] private float bloodLossRate = 1f;
    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    public float lerpSpeed = 2f;

    [Header("Collision Explosion Settings")]
    [SerializeField] private float velocityThreshold = 5f;
    [SerializeField] private LayerMask explosionLayers;
    [SerializeField] private bool debugCollisions = true;
    [SerializeField] private int puddleCount = 5;
    [SerializeField] private float puddleSpreadRadius = 3f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private LayerMask explosionDamageLayers;
    private float bloodLossTimer = 0f;
    private bool isEmptySent = false;
    private float collisionCooldown = 0f;
    private PhysicalItem toolItem;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        toolItem = GetComponent<PhysicalItem>();
        if (IsServer)
        {
            bloodBagCurrentCapacity.Value = bloodBagMaxCapacity;
            isEmptySent = false;
        }
        EventBus.Subscribe<OnBloodBagSnapped>(OnAttach);
        EventBus.Subscribe<OnBloodBagDetached>(OnDetach);

        isAttached.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal != oldVal)
                collisionCooldown = 0.5f;
        };

        bloodBagCurrentCapacity.OnValueChanged += (oldVal, newVal) =>
        {
            targetFillAmount = newVal / bloodBagMaxCapacity;
        };
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnBloodBagSnapped>(OnAttach);
        EventBus.Unsubscribe<OnBloodBagDetached>(OnDetach);
        base.OnNetworkDespawn();
    }

    void Update()
    {
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, lerpSpeed * Time.deltaTime);
        BloodRenderer.sharedMaterial.SetFloat("_FillAmount", currentFillAmount);

        if (!IsServer) return;
        if (!isAttached.Value) return;


        if (collisionCooldown > 0)
            collisionCooldown -= Time.deltaTime;

        HandleBloodLoss();
    }

    void HandleBloodLoss()
    {
        if (bloodBagCurrentCapacity.Value <= 0f) return;
        bloodLossTimer += Time.deltaTime;
        if (bloodLossTimer >= bloodLossRate)
        {
            bloodLossTimer -= bloodLossRate;
            bloodBagCurrentCapacity.Value -= bloodLossQuantity;
            if (bloodBagCurrentCapacity.Value <= 0f)
            {
                bloodBagCurrentCapacity.Value = 0f;
                if (!isEmptySent)
                {
                    BloodBagEmpty();
                    isEmptySent = true;
                }
            }
        }
    }

    public void OnAttach(OnBloodBagSnapped e)
    {
        if (e.BloodBagID != GetComponentInParent<NetworkObject>().NetworkObjectId) return;
        if (toolItem == null)
            toolItem = GetComponent<PhysicalItem>();

        if (IsServer)
            isAttached.Value = true;
    }

    public void OnDetach(OnBloodBagDetached e)
    {
        if (e.BloodBagID != GetComponentInParent<NetworkObject>().NetworkObjectId) return;

        if (IsServer)
            isAttached.Value = false;
    }

    private void BloodBagEmpty()
    {
        EventBus.Publish(new OnBloodBagEmpty { BloodBagID = GetComponentInParent<NetworkObject>().NetworkObjectId });
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isAttached.Value) return;
        if (isEmptySent) return;
        if (collisionCooldown > 0) return;

        if (toolItem == null) return;
        if (toolItem.grabbers.Count == 0) return;

        if (!IsLayerInMask(collision.gameObject.layer, explosionLayers))
        {
            if (debugCollisions)
                Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)}) no puede explotar bloodbag");
            return;
        }

        float impactVelocity = Mathf.Max(
            GetComponent<Rigidbody>().linearVelocity.magnitude,
            collision.relativeVelocity.magnitude
        );

        if (debugCollisions)
        {
            Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] Impact: {impactVelocity} vs Threshold: {velocityThreshold}");
        }

        if (impactVelocity < velocityThreshold) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionDamageLayers);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out PlayerInteractor affectedPlayer))
            {
                BloodStainsServerRpc(affectedPlayer.NetworkObjectId);
            }
        }
        BloodbagVFXClientRpc();

        HandleCollisionServerRpc(collision.contacts[0].point);
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return ((mask.value & (1 << layer)) > 0);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HandleCollisionServerRpc(Vector3 impactPoint)
    {
        if (!IsServer) return;

        SpawnBloodPuddles(impactPoint);
        DespawnBloodBag();
    }
    [ServerRpc]
    private void BloodStainsServerRpc(ulong affectedPlayerNetObjId)
    {
        BloodStainsClientRpc(affectedPlayerNetObjId);
    }
    [ClientRpc] 
    private void BloodStainsClientRpc(ulong affectedPlayerNetObjId)
    {
        EventBus.Publish(new OnPlayerSlipped { VictimID = affectedPlayerNetObjId });
    }
    [ClientRpc]
    private void BloodbagVFXClientRpc()
    {
        explosionParticles.transform.SetParent(null);
        explosionParticles.gameObject.SetActive(true);
        explosionParticles.Play();
        Destroy(explosionParticles.gameObject, explosionParticles.main.duration + explosionParticles.main.startLifetime.constantMax);
    }

    private void SpawnBloodPuddles(Vector3 impactPoint)
    {
        for (int i = 0; i < puddleCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-puddleSpreadRadius, puddleSpreadRadius),
                0,
                UnityEngine.Random.Range(-puddleSpreadRadius, puddleSpreadRadius)
            );

            Vector3 puddlePosition = impactPoint + randomOffset;

            if (Physics.Raycast(puddlePosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                PuddleSpawner.Instance.SpawnPuddleServerRpc(hit.point, hit.normal);
            }
        }
    }

    private void DespawnBloodBag()
    {
        NetworkObject parentNetObject = GetComponent<NetworkObject>();
        if (parentNetObject != null && parentNetObject.IsSpawned)
        {
            parentNetObject.Despawn();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}