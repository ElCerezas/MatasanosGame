using Unity.Netcode;
using UnityEngine;

public class BloodBag : NetworkBehaviour
{
    [SerializeField] private float bloodBagMaxCapacity = 100f;
    [SerializeField] private NetworkVariable<float> bloodBagCurrentCapacity = new NetworkVariable<float>(100f);
    [SerializeField] private float bloodLossQuantity = 1f;
    [SerializeField] private float bloodLossRate = 1f;
    private float bloodLossTimer = 0f;
    private bool isEmptySent = false;
    private bool IsAttached = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            bloodBagCurrentCapacity.Value = bloodBagMaxCapacity;
            isEmptySent = false;
        }
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
    public void OnAttach()
    {
        IsAttached = true;
    }
    private void BloodBagEmpty()
    {
        EventBus.Publish(new OnBloodBagEmpty { BloodBagID = GetComponentInParent<NetworkObject>().NetworkObjectId });
    }
}
