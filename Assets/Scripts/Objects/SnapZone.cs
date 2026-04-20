using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class SnapZone : NetworkBehaviour
{
    public SnapType acceptedType;
    public Transform snapAnchor;

    public UnityEvent OnObjectSnapped;
    public UnityEvent OnObjectUnsnapped;
    public SnappableItem currentItem { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(0);
        if (!IsServer) return;
        Debug.Log(1);
        if (currentItem != null) return;
        Debug.Log(2);
        if (other.TryGetComponent(out SnappableItem snappable))
        {
            Debug.Log(3);
            if (snappable.itemType == acceptedType && !snappable.isSnapped)
            {
                Debug.Log(4);
                currentItem = snappable;
                currentItem.SnapTo(this);

                OnObjectSnapped?.Invoke();
            }
        }
    }
    public void ReleaseItem()
    {
        Debug.Log("released");
        if (currentItem != null)
        {
            currentItem.Unsnap();
            currentItem = null;
            OnObjectUnsnapped?.Invoke();
        }
    }
}