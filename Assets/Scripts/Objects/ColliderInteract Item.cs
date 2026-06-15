using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class ColliderInteractItem : NetworkBehaviour
{
    [SerializeField] public ColliderItemType itemType;

    [Serializable]
    public class OnEntry
    {
        public ColliderDetectorType detectorType;
        public UnityEvent onEnter;
        public UnityEvent onExit;
    }

    [SerializeField] private List<OnEntry> collisionEntries = new();

    private Dictionary<ColliderDetectorType, OnEntry> diccionario;

    private void Awake()
    {
        diccionario = new Dictionary<ColliderDetectorType, OnEntry>();
        foreach (OnEntry entry in collisionEntries)
        {
            diccionario.TryAdd(entry.detectorType, entry);
        }
    }

    public void OnEnter(ColliderDetectorType type)
    {
        /*if (diccionario.TryGetValue(type, out OnEntry entry))
        entry.onEnter?.Invoke();*/
        OnEnterServerRpc(type);
    }
    public void OnExit(ColliderDetectorType type)
    {
        OnExitServerRpc(type);
    }
    [ServerRpc]
    void OnEnterServerRpc(ColliderDetectorType type)
    {
        if (diccionario.TryGetValue(type, out OnEntry entry))
            entry.onEnter?.Invoke();
    }
    [ServerRpc]
    void OnExitServerRpc(ColliderDetectorType type)
    {
        if (diccionario.TryGetValue(type, out OnEntry entry))
            entry.onExit?.Invoke();
    }

}
