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

    bool firstActivation = true;

    private void Awake()
    {
        var no = GetComponent<NetworkObject>();
        if (no != null)
        {
            no.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
            no.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
        }

        var nt = GetComponent<NetworkTransform>();
        if (nt != null)
        {
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            nt.InLocalSpace = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer || currentItem == null) return;
        firstActivation = false;
    }

    private void Update()
    {
        if (!IsServer || firstActivation || currentItem == null) return;
        firstActivation = true;
        currentItem.SnapTo(this);
        OnObjectSnapped?.Invoke();
    }

    protected override void OnOwnershipChanged(ulong previousOwner, ulong newOwner)
    {
        base.OnOwnershipChanged(previousOwner, newOwner);
        if (!IsServer) return;
        if (currentItem == null || !currentItem.NetworkObject.IsSpawned) return;
        if (currentItem.NetworkObject.OwnerClientId != newOwner)
            currentItem.NetworkObject.ChangeOwnership(newOwner);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (currentItem != null) return;

        if (!other.TryGetComponent(out SnappableItem snappable)) return;
        
        if (!snappable.isSnappable || snappable.isSnapped) return;
        if (snappable.itemType != acceptedType) return;

        currentItem = snappable;
        currentItem.SnapTo(this);

        if (acceptedType == SnapType.Bloodbag)
            EventBus.Publish(new OnBloodBagSnapped
            {
                BloodBagID = currentItem.GetComponentInParent<NetworkObject>().NetworkObjectId
            });
        else if (acceptedType == SnapType.Diente)
            EventBus.Publish(new OnDienteSnap { ID = NetworkObjectId, currentItem = currentItem });
        else
            OnObjectSnapped?.Invoke();
    }

    public void ReleaseItem()
    {
        if (currentItem == null) return;
        if (!currentItem.isUnsnappable) return;

        var item = currentItem;
        currentItem = null;

        if (acceptedType == SnapType.Bloodbag)
            EventBus.Publish(new OnBloodBagDetached
            {
                BloodBagID = item.GetComponentInParent<NetworkObject>().NetworkObjectId
            });
        else if (acceptedType == SnapType.Diente)
            EventBus.Publish(new OnDienteUnSnap
            {
                SnapZoneID = NetworkObjectId,
                DienteID = item.GetComponentInParent<NetworkObject>().NetworkObjectId,
                UnSnappedTooth = item.gameObject
            });

        item.Unsnap();
        OnObjectUnsnapped?.Invoke();
    }
}