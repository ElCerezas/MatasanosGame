using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PhysicalItem))]
[RequireComponent(typeof(Rigidbody))]
public class SnappableItem : NetworkBehaviour, ISnappable
{
    public SnapType itemType;
    public bool isSnapped { get; private set; }
    public SnapZone currentZone { get; private set; }

    private Rigidbody rb;
    private PhysicalItem physicalItem;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        physicalItem = GetComponent<PhysicalItem>();
    }

    public void SnapTo(SnapZone zone)
    {
        if (!IsServer) return;

        isSnapped = true;
        currentZone = zone;

        rb.isKinematic = true;

        //if (zone.NetworkObject != null)
        //    NetworkObject.TrySetParent(zone.NetworkObject, false);

        transform.position = zone.snapAnchor.position;
        transform.rotation = zone.snapAnchor.rotation;


    }

    public void Unsnap()
    {
        if (!IsServer || !isSnapped) return;
        //NetworkObject.TryRemoveParent();

        isSnapped = false;
        currentZone.ReleaseItem();
        currentZone = null;

        rb.isKinematic = false;
    }
}
