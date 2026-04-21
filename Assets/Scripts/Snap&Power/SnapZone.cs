using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkTransform))]
public class SnapZone : NetworkBehaviour
{
    public SnapType acceptedType;
    public Transform snapAnchor;

    public UnityEvent OnObjectSnapped;
    public UnityEvent OnObjectUnsnapped;
    public SnappableItem currentItem { get; private set; }
    float cooldown = 5f;
    float lastSnapTime;


    private void Start()
    {
        if (!IsServer ||currentItem == null) return;
        
        currentItem.SnapTo(this);
        OnObjectSnapped?.Invoke();
    }

    private void Awake()
    {
        lastSnapTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (Time.time - lastSnapTime < cooldown) return;
        if (currentItem != null) return;
        if (other.TryGetComponent(out SnappableItem snappable))
        {
            if (snappable.itemType == acceptedType && !snappable.isSnapped)
            {
                currentItem = snappable;
                currentItem.SnapTo(this);

                OnObjectSnapped?.Invoke();
            }
        }
    }
    public void ReleaseItem()
    {
        if (currentItem != null)
        {
            lastSnapTime = Time.time;
            currentItem.Unsnap();
            currentItem = null;
            OnObjectUnsnapped?.Invoke();
        }
    }
}