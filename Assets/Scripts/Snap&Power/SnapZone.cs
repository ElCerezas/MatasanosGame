using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkTransform))]
public class SnapZone : NetworkBehaviour
{
    public SnapType acceptedType;
    public Transform snapAnchor;

    public UnityEvent OnObjectSnapped;
    public UnityEvent OnObjectUnsnapped;
    public SnappableItem currentItem;
    float cooldown = 5f;
    float lastSnapTime;

    bool firstActivation = true;
    private void Awake()
    {
        lastSnapTime = Time.time;
    }
    private void Update()
    {
        if (!firstActivation)
        {
            currentItem.SnapTo(this);
            firstActivation = true;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer ||currentItem == null) return;
        //Debug.Log($"Start plug {gameObject.name} has pluged {currentItem.name}");
        if (currentItem != null) firstActivation = false;
        currentItem.SnapTo(this);     
        OnObjectSnapped?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (Time.time - lastSnapTime < cooldown) return;
        if (currentItem != null) return;
        if (other.TryGetComponent(out SnappableItem snappable))
        {
            if (!snappable.isSnappable) return;
            if (snappable.itemType == acceptedType && !snappable.isSnapped)
            {
                currentItem = snappable;
                currentItem.SnapTo(this);
                if (acceptedType == SnapType.Bloodbag)
                {
                    EventBus.Publish(new OnBloodBagSnapped { BloodBagID = currentItem.GetComponentInParent<NetworkObject>().NetworkObjectId });
                }
                else if (acceptedType == SnapType.Diente)
                {
                    EventBus.Publish(new OnDienteSnap { ID = NetworkObjectId, currentItem = currentItem    });
                }
                else
                {
                    OnObjectSnapped?.Invoke();
                }
                
            }
        }
    }
    public void ReleaseItem()
    {
        if (!currentItem.isUnsnappable) return;
        if (currentItem != null)
        {
            if (acceptedType == SnapType.Bloodbag)
            {
                EventBus.Publish(new OnBloodBagDetached { BloodBagID = currentItem.GetComponentInParent<NetworkObject>().NetworkObjectId });
            }
            if (acceptedType == SnapType.Diente)
            {
                EventBus.Publish(new OnDienteUnSnap { SnapZoneID = NetworkObjectId, DienteID = currentItem.GetComponentInParent<NetworkObject>().NetworkObjectId, UnSnappedTooth = currentItem.gameObject });
            }
            lastSnapTime = Time.time;
            currentItem.Unsnap();
            currentItem = null;
            OnObjectUnsnapped?.Invoke();
        }
    }
}