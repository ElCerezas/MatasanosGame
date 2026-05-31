using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PhysicalItem))]
[RequireComponent(typeof(Rigidbody))]
public class SnappableItem : NetworkBehaviour, ISnappable
{
    public SnapType itemType;
    public bool isSnapped { get; private set; }
    public SnapZone currentZone { get; private set; }
    public bool isSnappable = true;
    public bool isUnsnappable = true;
    Rigidbody rb;
    Collider col;
    PhysicalItem physicalItem;
    private void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        physicalItem = GetComponent<PhysicalItem>();
    }

    private void LateUpdate()
    {
        if (!isSnapped || currentZone == null) return;
        rb.isKinematic = true;
        transform.position = currentZone.snapAnchor.position;
        transform.rotation = currentZone.snapAnchor.rotation;
    }

    public void SnapTo(SnapZone zone)
    {
        if (!IsServer) return;
        isSnapped = true;
        currentZone = zone;
        rb.isKinematic = true;
        transform.position = zone.snapAnchor.position;
        transform.rotation = zone.snapAnchor.rotation;
        if (currentZone.gameObject.GetComponent<Collider>() != null)
            Physics.IgnoreCollision(col, currentZone.transform.parent.GetComponent<Collider>(), true);
        SnapClientRpc(zone.snapAnchor.position, zone.snapAnchor.rotation, zone.NetworkObject);
    }

    [ClientRpc]
    void SnapClientRpc(Vector3 pos, Quaternion rot, NetworkObjectReference zoneRef)
    {
        if (IsServer) return;
        rb.isKinematic = true;
        if (zoneRef.TryGet(out NetworkObject zoneNetObj))
            currentZone = zoneNetObj.GetComponent<SnapZone>();
        isSnapped = true;
        transform.position = pos;
        transform.rotation = rot;
    }

    public void Unsnap()
    {
        if (!IsServer || !isSnapped) return;
        isSnapped = false;
        var zone = currentZone;
        currentZone = null;
        if (zone.gameObject.GetComponent<Collider>() != null)
            Physics.IgnoreCollision(col, zone.transform.parent.GetComponent<Collider>(), false);
        zone?.ReleaseItem();
        rb.isKinematic = false;
        UnsnapClientRpc();
    }

    [ClientRpc]
    void UnsnapClientRpc()
    {
        if (IsServer) return;
        isSnapped = false;
        currentZone = null;
        rb.isKinematic = false;
    }
}
