using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

public class InyeccionItem : NetworkBehaviour
{
    [SerializeField] private InyeccionType inyeccionType;
    [SerializeField] private InyeccionCollider inyeccionCollider;


    public void Inject(GameObject victim)
    {
        if (victim.TryGetComponent<NetworkObject>(out var netObj))
        {
            EventBus.Publish<OnInject>(new OnInject
            {
                VictimID = netObj.NetworkObjectId,
                Type = inyeccionType
            });
        }
    }

}