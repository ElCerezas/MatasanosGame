using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BloodBag : NetworkBehaviour
{
    [Header("Blood Settings")]
    [SerializeField][Range(0f, 1f)] float initialFillPercentage = 1f;
    [SerializeField] private float bloodBagMaxCapacity = 100f;
    [SerializeField] private float bloodLossQuantity = 1f;
    [SerializeField] private float bloodLossRate = 1f;
    [SerializeField] private float bloodFillRate = 2f;
    public float lerpSpeed = 2f;

    [Header("References")]
    [SerializeField] protected Renderer bloodRenderer;

    [Header("Collision Explosion Settings")]
    [SerializeField] private float velocityThreshold = 5f;
    [SerializeField] private float emptyVelocityThreshold = -1f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private LayerMask explosionLayers;
    [SerializeField] private LayerMask explosionDamageLayers;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private int puddleCount = 5;
    [SerializeField] private float puddleSpreadRadius = 3f;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private FMODUnity.EventReference explosionSound;
    [SerializeField] private FMODUnity.EventReference groundImpactSound;
    [SerializeField] private bool debugCollisions = true;
    [SerializeField] private float groundImpactVelocityThreshold = 2f;

    [Header("Debug View")]
    [SerializeField] private bool showDebugText = true;
    [SerializeField, TextArea(12, 18)] private string inspectorDebugInfo;

    private NetworkVariable<float> bloodBagCurrentCapacity = new NetworkVariable<float>(-1f);
    private NetworkVariable<bool> isAttached = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isRefilling = new NetworkVariable<bool>(false);
    private float visualFillAmount = 1f;
    private float bloodLossTimer = 0f;
    private float collisionCooldown = 0f;
    private bool isEmptySent = false;
    private bool hasInitializedFill = false;
    private ulong parentNetworkObjectId;

    private Material cachedBloodMaterial;
    private PhysicalItem toolItem;
    private Rigidbody rb;
    private int fillAmountParamID;

    public float BloodBagMaxCapacity => bloodBagMaxCapacity;
    public bool IsFull => bloodBagCurrentCapacity.Value >= 0f && bloodBagCurrentCapacity.Value >= bloodBagMaxCapacity;
    public bool IsEmpty => bloodBagCurrentCapacity.Value >= 0f && bloodBagCurrentCapacity.Value <= 0f;

    public float FillRatio => bloodBagCurrentCapacity.Value >= 0f
        ? bloodBagCurrentCapacity.Value / bloodBagMaxCapacity
        : initialFillPercentage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        toolItem = GetComponent<PhysicalItem>();
        fillAmountParamID = Shader.PropertyToID("_FillAmount");

        if (bloodRenderer != null)
        {
            cachedBloodMaterial = bloodRenderer.material;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        var parentNetObj = GetComponentInParent<NetworkObject>();
        if (parentNetObj != null)
            parentNetworkObjectId = parentNetObj.NetworkObjectId;

        if (IsServer)
        {
            bloodBagCurrentCapacity.Value = bloodBagMaxCapacity * initialFillPercentage;
            isEmptySent = bloodBagCurrentCapacity.Value <= 0f;
        }

        visualFillAmount = initialFillPercentage;

        if (cachedBloodMaterial != null)
            cachedBloodMaterial.SetFloat(fillAmountParamID, visualFillAmount);

        EventBus.Subscribe<OnBloodBagSnapped>(OnAttach);
        EventBus.Subscribe<OnBloodBagDetached>(OnDetach);

        isAttached.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal != oldVal) collisionCooldown = 0.5f;
        };
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnBloodBagSnapped>(OnAttach);
        EventBus.Unsubscribe<OnBloodBagDetached>(OnDetach);
        base.OnNetworkDespawn();
    }

    public void SetRefillingState(bool state)
    {
        if (IsServer) isRefilling.Value = state;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (showDebugText) UpdateDebugText();
#endif

        if (bloodBagCurrentCapacity.Value < 0f) return;

        float target = FillRatio;

        if (!hasInitializedFill)
        {
            visualFillAmount = target;
            if (cachedBloodMaterial != null)
                cachedBloodMaterial.SetFloat(fillAmountParamID, visualFillAmount);
            hasInitializedFill = true;
        }
        else if (Mathf.Abs(visualFillAmount - target) > 0.001f)
        {
            visualFillAmount = Mathf.Lerp(visualFillAmount, target, lerpSpeed * Time.deltaTime);
            if (cachedBloodMaterial != null)
            {
                cachedBloodMaterial.SetFloat(fillAmountParamID, visualFillAmount);
            }
        }

        if (!IsServer) return;

        if (isRefilling.Value && !IsFull)
        {
            AddBlood(bloodFillRate * Time.deltaTime);
        }

        if (!isAttached.Value) return;

        if (collisionCooldown > 0) collisionCooldown -= Time.deltaTime;

        HandleBloodLoss();
    }

    public void AddBlood(float amount)
    {
        if (!IsServer || IsFull) return;

        bloodBagCurrentCapacity.Value = Mathf.Min(bloodBagCurrentCapacity.Value + amount, bloodBagMaxCapacity);

        if (bloodBagCurrentCapacity.Value > 0f) isEmptySent = false;
    }

    private void HandleBloodLoss()
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
                    EventBus.Publish(new OnBloodBagEmpty { BloodBagID = parentNetworkObjectId });
                    isEmptySent = true;
                }
            }
        }
    }

    public void OnAttach(OnBloodBagSnapped e)
    {
        if (e.BloodBagID != parentNetworkObjectId) return;
        if (IsServer) isAttached.Value = true;
    }

    public void OnDetach(OnBloodBagDetached e)
    {
        if (e.BloodBagID != parentNetworkObjectId) return;
        if (IsServer) isAttached.Value = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactVelocity = Mathf.Max(rb.linearVelocity.magnitude, collision.relativeVelocity.magnitude);

        bool isGroundCollision = IsLayerInMask(collision.gameObject.layer, groundLayer);

        if (isGroundCollision && impactVelocity >= groundImpactVelocityThreshold)
        {
            PlayGroundImpactSoundServerRpc(collision.contacts[0].point);
        }

        if (isAttached.Value || isEmptySent || collisionCooldown > 0 || toolItem == null || toolItem.grabbers.Count == 0)
            return;

        float currentThreshold = GetCurrentVelocityThreshold(FillRatio);

        if (currentThreshold < 0f || impactVelocity < currentThreshold) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionDamageLayers);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out PlayerInteractor affectedPlayer))
            {
                BloodStainsServerRpc(affectedPlayer.NetworkObjectId);
            }
        }

        BloodbagVFXServerRpc(collision.contacts[0].point);
        HandleCollisionServerRpc(collision.contacts[0].point);
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) > 0;
    }

    private float GetCurrentVelocityThreshold(float fill)
    {
        if (emptyVelocityThreshold < 0f && fill <= 0f)
            return -1f;

        return Mathf.Lerp(emptyVelocityThreshold, velocityThreshold, fill);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HandleCollisionServerRpc(Vector3 impactPoint)
    {
        if (!IsServer) return;

        SpawnBloodPuddles(impactPoint);

        if (NetworkObject != null && IsSpawned)
            NetworkObject.Despawn();
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

    [ServerRpc]
    private void BloodbagVFXServerRpc(Vector3 impactPoint)
    {
        BloodbagVFXClientRpc(impactPoint);
    }

    [ClientRpc]
    private void BloodbagVFXClientRpc(Vector3 impactPoint)
    {
        if (explosionParticles == null) return;

        AudioManager.instance.PlayOneShotAtPosition(explosionSound, impactPoint);

        explosionParticles.transform.position = impactPoint;
        explosionParticles.transform.SetParent(null);
        explosionParticles.gameObject.SetActive(true);
        explosionParticles.Play();
        Destroy(explosionParticles.gameObject, explosionParticles.main.duration + explosionParticles.main.startLifetime.constantMax);
    }

    [ServerRpc]
    private void PlayGroundImpactSoundServerRpc(Vector3 impactPoint)
    {
        PlayGroundImpactSoundClientRpc(impactPoint);
    }

    [ClientRpc]
    private void PlayGroundImpactSoundClientRpc(Vector3 impactPoint)
    {
        AudioManager.instance.PlayOneShotAtPosition(groundImpactSound, impactPoint);
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

#if UNITY_EDITOR
    private void UpdateDebugText()
    {
        float currentCap = bloodBagCurrentCapacity.Value;
        float fillRatio = FillRatio;

        inspectorDebugInfo =
            $"=== NETWORK INFO ===\n" +
            $"Is Spawned: {IsSpawned}\n" +
            $"Is Server/Host: {IsServer}\n" +
            $"Parent NetObj ID: {parentNetworkObjectId}\n\n" +

            $"=== STATE ===\n" +
            $"Is Attached: {isAttached.Value}\n" +
            $"Is Refilling: {isRefilling.Value}\n" +
            $"Is Full: {IsFull}\n" +
            $"Is Empty: {IsEmpty} (Sent: {isEmptySent})\n\n" +

            $"=== CAPACITY ===\n" +
            $"Current Cap: {currentCap:F1} / {bloodBagMaxCapacity:F1}\n" +
            $"Actual Fill: {fillRatio * 100f:F1}%\n" +
            $"Visual Fill: {visualFillAmount * 100f:F1}%\n" +
            $"Blood Loss Timer: {bloodLossTimer:F2}s / {bloodLossRate}s\n\n" +

            $"=== PHYSICS ===\n" +
            $"Velocity Magnitude: {(rb != null ? rb.linearVelocity.magnitude : 0):F2}\n" +
            $"Dyn Explosion Thresh: {GetCurrentVelocityThreshold(fillRatio):F2}\n" +
            $"Collision Cooldown: {collisionCooldown:F2}s\n" +
            $"Grabbed by Player: {(toolItem != null && toolItem.grabbers.Count > 0 ? "Yes" : "No")}";
    }
#endif
}