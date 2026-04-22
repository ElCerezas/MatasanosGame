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
    private float bloodLossTimer = 0f;
    private bool isEmptySent = false;
    private bool IsAttached = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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

    public bool Equals(TextMeshPro other)
    {
        throw new NotImplementedException();
    }
}
