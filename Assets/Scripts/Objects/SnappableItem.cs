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
        if (!IsServer || !isSnapped || currentZone == null) return;
        transform.position = currentZone.snapAnchor.position;
        transform.rotation = currentZone.snapAnchor.rotation;
    }

    public void SnapTo(SnapZone zone)
    {
        Debug.Log("PSPSPSP");
        if (!IsServer) return;
        Debug.Log("sap");
        isSnapped = true;
        currentZone = zone;

        Physics.IgnoreCollision(col, currentZone.transform.parent.GetComponent<Collider>(), true);
        rb.isKinematic = true;
    }

    public void Unsnap()
    {
        if (!IsServer || !isSnapped) return;

        isSnapped = false;
        currentZone.ReleaseItem();
        Physics.IgnoreCollision(col, currentZone.transform.parent.GetComponent<Collider>(), false);
        currentZone = null;

        rb.isKinematic = false;
    }
}
