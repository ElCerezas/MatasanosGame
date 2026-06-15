using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class ColliderDetector : NetworkBehaviour
{

    [Header("Audio")]
    [SerializeField] private FMODUnity.EventReference sonidoAguaLimpieza;

    [SerializeField] public ColliderDetectorType detectorType;

    [Serializable] public class CollisionEntry
    {
       public ColliderItemType itemType;
       public UnityEvent onEnter;
       public UnityEvent onExit;
    }

    [SerializeField] private List<CollisionEntry> collisionEntries = new();

    private Dictionary<ColliderItemType, CollisionEntry> diccionario;

    private void Awake()
    {
       diccionario = new Dictionary<ColliderItemType, CollisionEntry>();
       foreach (CollisionEntry entry in collisionEntries)
       {
           diccionario.TryAdd(entry.itemType, entry);
       }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryInvoke(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        TryInvoke(other, false);
    }

    private void TryInvoke(Collider other, bool enter)
    {
        if (!other.TryGetComponent<ColliderInteractItem>(out ColliderInteractItem item))
        {
            return;
        }

        if (diccionario.TryGetValue(item.itemType, out CollisionEntry entry))
        {
            if (enter)
            {
                item.OnEnter(detectorType);
                entry.onEnter?.Invoke();
                PlayWaterSFXServerRpc(other.transform.position);
            }
            else
            {
                item.OnExit(detectorType);
                entry.onExit?.Invoke();
            } 
        }

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayWaterSFXServerRpc(Vector3 posicion)
    {
        PlayWaterSFXClientRpc(posicion);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayWaterSFXClientRpc(Vector3 posicion)
    {
        AudioManager.instance.PlayOneShotAtPosition(sonidoAguaLimpieza, posicion);
    }
}
