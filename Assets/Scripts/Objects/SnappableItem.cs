using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(PhysicalItem))]
[RequireComponent(typeof(Rigidbody))]
public class SnappableItem : NetworkBehaviour, ISnappable
{
    public SnapType itemType;
    public bool isSnapped { get; private set; }
    public SnapZone currentZone { get; private set; }
    
    [Header("Configuración de Snap")]
    public bool isSnappable = true;
    public bool isUnsnappable = true;
    [SerializeField] float snapCooldownDuration = 1f;
    Rigidbody rb;
    Collider col;
    PhysicalItem physicalItem;
    Coroutine cooldownCoroutine;
    private void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        physicalItem = GetComponent<PhysicalItem>();
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !isSnapped || currentZone == null) return;
        rb.isKinematic = true;
        transform.position = currentZone.snapAnchor.position;
        transform.rotation = currentZone.snapAnchor.rotation;
    }

    public void SnapTo(SnapZone zone)
    {
        if (!IsServer) return;

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        isSnapped = true;
        isSnappable = true; 
        currentZone = zone;
        rb.isKinematic = true;
        transform.position = zone.snapAnchor.position;
        transform.rotation = zone.snapAnchor.rotation;

        if (NetworkObject.IsSpawned)
            NetworkObject.ChangeOwnership(zone.NetworkObject.OwnerClientId);

        SetCollisionWithHolder(zone, ignore: true);

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

        SetCollisionWithHolder(currentZone, ignore: true);
    }

    public void Unsnap()
    {
        if (!IsServer || !isSnapped) return;

        isSnapped = false;
        var zone = currentZone;
        currentZone = null;

        SetCollisionWithHolder(zone, ignore: false);

        zone?.ReleaseItem();
        rb.isKinematic = false;

        if (NetworkObject.IsSpawned)
        {
            if (physicalItem == null) physicalItem = GetComponent<PhysicalItem>();
            if (physicalItem == null || physicalItem.grabbers.Count == 0)
            {
                NetworkObject.RemoveOwnership();
            }
        }

        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(SnapCooldownRoutine());

        UnsnapClientRpc();
    }

    [ClientRpc]
    void UnsnapClientRpc()
    {
        if (IsServer) return;

        var zone = currentZone;

        isSnapped = false;
        currentZone = null;

        SetCollisionWithHolder(zone, ignore: false);

        rb.isKinematic = false;
    }

    IEnumerator SnapCooldownRoutine()
    {
        isSnappable = false;
        yield return new WaitForSeconds(snapCooldownDuration);
        isSnappable = true;
        cooldownCoroutine = null;
    }

    void SetCollisionWithHolder(SnapZone zone, bool ignore)
    {
        if (zone == null || zone.transform.parent == null) return;
        var holderCol = zone.transform.parent.GetComponent<Collider>();
        if (holderCol != null)
            Physics.IgnoreCollision(col, holderCol, ignore);
    }
}