using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class ColliderInteractable : NetworkBehaviour
{
    [Serializable] public class CollisionEntry
    {
        public ColliderInteractableType itemType;
        public UnityEvent onEnter;
        public UnityEvent onExit;
    }

    [SerializeField] private List<CollisionEntry> collisionEntries = new();

    private Dictionary<ColliderInteractableType, CollisionEntry> diccionario;

    private void Awake()
    {
        diccionario = new Dictionary<ColliderInteractableType, CollisionEntry>();
        foreach (var entry in collisionEntries)
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
            if (enter) entry.onEnter?.Invoke();
            else entry.onExit?.Invoke();
        }
    }
}
