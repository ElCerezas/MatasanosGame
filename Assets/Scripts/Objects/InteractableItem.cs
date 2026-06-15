using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InteractableOutline))]
[RequireComponent(typeof(NetworkObject))]
public class InteractableItem : NetworkBehaviour, IInteractable
{
    public UnityEvent onInteract;
    [Header("Audio")]
    [SerializeField] private FMODUnity.EventReference sonidoAlInteractuar;

    public virtual void Interact(ulong clientID)
    {
        onInteract?.Invoke();
        PlayInteractSoundClientRpc();
    }

    public virtual ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }

    [ClientRpc]
    private void PlayInteractSoundClientRpc()
    {
        if (!sonidoAlInteractuar.IsNull)
        {
            FMOD.Studio.EventInstance instInteract = FMODUnity.RuntimeManager.CreateInstance(sonidoAlInteractuar);
            instInteract.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            instInteract.start();
            instInteract.release();
        }
    }
}
