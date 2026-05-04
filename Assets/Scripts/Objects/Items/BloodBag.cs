using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class BloodBag : NetworkBehaviour
{
    [SerializeField] private float bloodBagMaxCapacity = 100f;
    [SerializeField] private NetworkVariable<float> bloodBagCurrentCapacity = new NetworkVariable<float>(100f);
    [SerializeField] private float bloodLossQuantity = 1f;
    [SerializeField] private float bloodLossRate = 1f;
    [SerializeField] private TextMeshPro capacityText;
    [Header("Collision Explosion Settings")]
    [SerializeField] private float velocityThreshold = 2f;
    [SerializeField] private int puddleCount = 5;
    [SerializeField] private float puddleSpreadRadius = 3f;
    [SerializeField] private LayerMask groundLayer;
    
    private float bloodLossTimer = 0f;
    private bool isEmptySent = false;
    private bool IsAttached = false;
    private ToolItem toolItem;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        toolItem = GetComponent<ToolItem>();
        if (IsServer)
        {
            bloodBagCurrentCapacity.Value = bloodBagMaxCapacity;
            isEmptySent = false;
        }
        EventBus.Subscribe<OnBloodBagSnapped>(OnAttach);
        EventBus.Subscribe<OnBloodBagDetached>(OnDetach);
        bloodBagCurrentCapacity.OnValueChanged += (oldVal, newVal) =>
        {
            capacityText.text = newVal.ToString();
        };
    }

    void Update()
    {
        if (!IsServer) return;
        if (!IsAttached) return;
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
        IsAttached = true;
    }

    public void OnDetach(OnBloodBagDetached e)
    {
        if (e.BloodBagID != GetComponentInParent<NetworkObject>().NetworkObjectId) return;
        IsAttached = false;
    }

    private void BloodBagEmpty()
    {
        EventBus.Publish(new OnBloodBagEmpty { BloodBagID = GetComponentInParent<NetworkObject>().NetworkObjectId });
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (toolItem.grabbers.Count == 0) return;
        if (collision.relativeVelocity.magnitude < velocityThreshold) return;

        SpawnBloodPuddles(collision.contacts[0].point);
        DespawnBloodBag();
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
        NetworkObject parentNetObject = GetComponentInParent<NetworkObject>();
        if (parentNetObject != null && parentNetObject.IsSpawned)
        {
            parentNetObject.Despawn();
        }
    }
}