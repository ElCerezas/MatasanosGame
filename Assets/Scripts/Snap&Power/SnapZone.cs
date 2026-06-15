using System.Data.Common;
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

    [Header("Audio Settings")]
    [SerializeField] FMODUnity.EventReference bloodBagSnapSound;
    [SerializeField] FMODUnity.EventReference bloodBagUnsnapSound;
    [SerializeField] FMODUnity.EventReference dienteSnapSound;
    [SerializeField] FMODUnity.EventReference dienteUnsnapSound;
    [SerializeField] FMODUnity.EventReference enchufeSnapSound;
    [SerializeField] FMODUnity.EventReference enchufeUnsnapSound;
    [SerializeField] FMODUnity.EventReference defaultSnapSound;
    [SerializeField] FMODUnity.EventReference defaultUnsnapSound;

    bool firstActivation = true;
    private BloodBag currentBloodBag;

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
        if (snapAnchor == null)
        {
            snapAnchor = gameObject.transform;
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

        if (acceptedType == SnapType.Bloodbag)
            currentBloodBag = currentItem.GetComponentInParent<BloodBag>();
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
        PlaySnapSoundClientRpc(true);

        if (acceptedType == SnapType.Bloodbag)
        {
            currentBloodBag = currentItem.GetComponentInParent<BloodBag>();

            if (gameObject.CompareTag("EmptyBag")) 
            {
                if (currentBloodBag != null)
                    currentBloodBag.SetRefillingState(true);
                
                OnObjectSnapped?.Invoke();
            }
            else
            {
                EventBus.Publish(new OnBloodBagSnapped
                {
                    BloodBagID = currentItem.GetComponentInParent<NetworkObject>().NetworkObjectId
                });
            }
        }
        else if (acceptedType == SnapType.Diente)
        {
            EventBus.Publish(new OnDienteSnap { ID = NetworkObjectId, currentItem = currentItem });
        }
        else
        {
            OnObjectSnapped?.Invoke();
        }
    }

    public void ReleaseItem()
    {
        if (currentItem == null) return;
        if (!currentItem.isUnsnappable) return;

        var item = currentItem;
        currentItem = null;
        PlaySnapSoundClientRpc(false);

        if (acceptedType == SnapType.Bloodbag)
        {
            if (gameObject.CompareTag("EmptyBag"))
            {
                /*
                if (currentBloodBag != null)
                    currentBloodBag.SetRefillingState(false);
                    */
            }
            else
            {
                EventBus.Publish(new OnBloodBagDetached
                {
                    BloodBagID = item.GetComponentInParent<NetworkObject>().NetworkObjectId
                });
            }
            currentBloodBag = null;
        }
        else if (acceptedType == SnapType.Diente)
        {
            EventBus.Publish(new OnDienteUnSnap
            {
                SnapZoneID = NetworkObjectId,
                DienteID = item.GetComponentInParent<NetworkObject>().NetworkObjectId,
                UnSnappedTooth = item.gameObject
            });
        }

        item.Unsnap();
        OnObjectUnsnapped?.Invoke();
    }

    [ClientRpc]
    private void PlaySnapSoundClientRpc(bool isSnap)
    {
        FMODUnity.EventReference soundToPlay;
        switch (acceptedType)
        {
            case SnapType.Bloodbag:
                soundToPlay = isSnap ? bloodBagSnapSound : bloodBagUnsnapSound;
                break;
            case SnapType.Diente:
                soundToPlay = isSnap ? dienteSnapSound : dienteUnsnapSound;
                break;
            case SnapType.Enchufe:
                soundToPlay = isSnap ? enchufeSnapSound : enchufeUnsnapSound;
                break;
            default:
                soundToPlay = isSnap ? defaultSnapSound : defaultUnsnapSound;
                break;
        }

        if (!soundToPlay.IsNull)
        {
            AudioManager.instance.PlayOneShotAtPosition(soundToPlay, snapAnchor.position);
        }
    }
}