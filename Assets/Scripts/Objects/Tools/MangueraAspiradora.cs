using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UI.Image;

public class MangueraAspiradora : PoweredItem
{
    [Header("Raycast Settings")]
    [SerializeField] public Transform originPoint;
    [SerializeField] public Aspiradora mainAspiradora;
    [SerializeField] private NetworkVariable<bool> isAttached = new NetworkVariable<bool>(false);
    public float rayDistance = 20f;
    public LayerMask puddleLayer;


    private PhysicalItem toolItem;
    private void Awake()
    {
        toolItem = GetComponent<PhysicalItem>();
    }
    public override void Interact(ulong clientID)
    {
        isTurnedOn.Value = !isTurnedOn.Value;
        if (isTurnedOn.Value )
        {
            Debug.Log("Aspiradora On");
        }
    }
    public virtual void Update()
    {
        if (!IsServer) return;
        if (!isTurnedOn.Value) return;
        Ray ray = new Ray(originPoint.position, originPoint.forward);
        Debug.DrawRay(originPoint.position, originPoint.forward * rayDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, puddleLayer))
        {
            if(hit.transform.TryGetComponent<BloodPuddle>(out  BloodPuddle puddle))
            {
                mainAspiradora.TryAddCapacity(puddle);
            }
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.DrawRay(originPoint.position, originPoint.forward);
    }
}
